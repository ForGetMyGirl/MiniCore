using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Threading;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace MiniCore.Service
{
    /// <summary>
    /// HTTP 服务的启动配置参数。
    /// </summary>
    public sealed class HttpServiceInitArgs : ComponentInitArgs
    {
        /// <summary>
        /// 获取或设置单次请求默认超时秒数。
        /// </summary>
        public int DefaultTimeoutSeconds { get; set; } = 15;

        /// <summary>
        /// 获取或设置幂等请求最大重试次数。
        /// </summary>
        public int MaxRetryCount { get; set; } = 2;

        /// <summary>
        /// 获取或设置首次重试的退避毫秒数。
        /// </summary>
        public int RetryBackoffMilliseconds { get; set; } = 250;
    }

    /// <summary>
    /// 使用 UnityWebRequest 实现的 HTTP 应用服务。
    /// </summary>
    [AppService("HTTP", typeof(IHttpService), Description = "基于 UnityWebRequest 发送 HTTP 请求，并支持超时和重试。", InitArgsType = typeof(HttpServiceInitArgs))]
    public sealed class UnityWebRequestHttpService : AAppService, IHttpService
    {
        #region Private 私有成员

        private int defaultTimeoutSeconds = 15; // 默认单次请求超时。
        private int maxRetryCount = 2; // 幂等请求最大重试次数。
        private int retryBackoffMilliseconds = 250; // 首次退避时长。
        private IHttpAuthProvider authProvider; // 可选鉴权服务。
        private ITelemetryService telemetry; // 可选遥测服务。

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 使用无参默认配置初始化 HTTP 服务。
        /// </summary>
        public override void Awake()
        {
            InitializeServices();
        }

        /// <summary>
        /// 使用启动配置初始化 HTTP 服务。
        /// </summary>
        /// <param name="args">HTTP 服务启动参数。</param>
        public override void Awake(ComponentInitArgs args)
        {
            if (!(args is HttpServiceInitArgs httpArgs))
            {
                throw new ArgumentException("HTTP 服务必须使用 HttpServiceInitArgs 初始化。", nameof(args));
            }

            defaultTimeoutSeconds = Math.Max(1, httpArgs.DefaultTimeoutSeconds);
            maxRetryCount = Math.Max(0, httpArgs.MaxRetryCount);
            retryBackoffMilliseconds = Math.Max(1, httpArgs.RetryBackoffMilliseconds);
            InitializeServices();
        }

        /// <summary>
        /// 释放可选服务引用。
        /// </summary>
        protected override void OnDispose()
        {
            authProvider = null;
            telemetry = null;
        }

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 发送原始 HTTP 请求，并仅对安全或显式幂等请求执行退避重试。
        /// </summary>
        /// <param name="request">请求描述。</param>
        /// <returns>原始响应。</returns>
        public async MTask<HttpResponse> SendAsync(HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            bool retryable = request.IsIdempotent || string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase) || string.Equals(request.Method, "HEAD", StringComparison.OrdinalIgnoreCase);
            int attempts = retryable ? maxRetryCount + 1 : 1;
            HttpResponse response = null;
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                MTask.ThrowIfCancellationRequested();
                response = await SendOnceAsync(request);
                if (response.IsSuccess || !ShouldRetry(response) || attempt == attempts - 1)
                {
                    return response;
                }

                telemetry?.Increment("http.retry");
                int delay = retryBackoffMilliseconds * (1 << Math.Min(attempt, 6));
                await MTask.Delay(delay);
            }

            return response;
        }

        /// <summary>
        /// 将请求对象序列化为 JSON 后发送，并将成功响应反序列化为目标对象。
        /// </summary>
        /// <typeparam name="TRequest">请求对象类型。</typeparam>
        /// <typeparam name="TResponse">响应对象类型。</typeparam>
        /// <param name="url">HTTP 或 HTTPS 的绝对请求地址。</param>
        /// <param name="request">请求对象。</param>
        /// <param name="method">HTTP 方法。</param>
        /// <returns>响应对象。</returns>
        public async MTask<TResponse> SendJsonAsync<TRequest, TResponse>(string url, TRequest request, string method = "POST")
        {
            HttpResponse response = await SendAsync(new HttpRequest
            {
                Url = url,
                Method = method,
                ContentType = "application/json",
                Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request))
            });
            EnsureSuccess(response);
            return JsonConvert.DeserializeObject<TResponse>(Encoding.UTF8.GetString(response.Body ?? Array.Empty<byte>()));
        }

        /// <summary>
        /// 发送 Protobuf 二进制正文并交由调用方解析成功响应。
        /// </summary>
        /// <typeparam name="TResponse">响应消息类型。</typeparam>
        /// <param name="url">HTTP 或 HTTPS 的绝对请求地址。</param>
        /// <param name="requestBody">序列化请求正文。</param>
        /// <param name="responseParser">响应解析函数。</param>
        /// <param name="method">HTTP 方法。</param>
        /// <returns>响应消息。</returns>
        public async MTask<TResponse> SendProtobufAsync<TResponse>(string url, byte[] requestBody, Func<byte[], TResponse> responseParser, string method = "POST")
        {
            if (responseParser == null)
            {
                throw new ArgumentNullException(nameof(responseParser));
            }

            HttpResponse response = await SendAsync(new HttpRequest
            {
                Url = url,
                Method = method,
                ContentType = "application/x-protobuf",
                Body = requestBody
            });
            EnsureSuccess(response);
            return responseParser(response.Body ?? Array.Empty<byte>());
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 获取可选鉴权和遥测服务，未绑定时维持匿名、无遥测运行。
        /// </summary>
        private void InitializeServices()
        {
            Global.TryGetService(this, out authProvider);
            Global.TryGetService(this, out telemetry);
        }

        /// <summary>
        /// 发送一次 UnityWebRequest 并转换为统一响应对象。
        /// </summary>
        /// <param name="request">请求描述。</param>
        /// <returns>本次传输响应。</returns>
        private async MTask<HttpResponse> SendOnceAsync(HttpRequest request)
        {
            CancellationToken token = MTaskExternal.GetCancellationToken();
            Dictionary<string, string> headers = new Dictionary<string, string>(request.Headers, StringComparer.OrdinalIgnoreCase);
            await ApplyAuthenticationAsync(headers);
            string url = ValidateAbsoluteUrl(request.Url);
            Stopwatch stopwatch = Stopwatch.StartNew();
            using UnityWebRequest unityRequest = new UnityWebRequest(url, request.Method ?? "GET")
            {
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = request.TimeoutSeconds > 0 ? request.TimeoutSeconds : defaultTimeoutSeconds
            };

            if (request.Body != null)
            {
                unityRequest.uploadHandler = new UploadHandlerRaw(request.Body);
            }

            if (!string.IsNullOrWhiteSpace(request.ContentType))
            {
                unityRequest.SetRequestHeader("Content-Type", request.ContentType);
            }

            foreach (KeyValuePair<string, string> pair in headers)
            {
                unityRequest.SetRequestHeader(pair.Key, pair.Value);
            }

            using (token.Register(unityRequest.Abort))
            {
                UnityWebRequestAsyncOperation operation = unityRequest.SendWebRequest();
                await WaitForCompletionAsync(operation, token);
            }

            stopwatch.Stop();
            HttpResponse response = new HttpResponse
            {
                StatusCode = unityRequest.responseCode,
                Body = unityRequest.downloadHandler?.data,
                Headers = unityRequest.GetResponseHeaders(),
                Error = unityRequest.result == UnityWebRequest.Result.Success ? null : unityRequest.error
            };
            telemetry?.Increment("http.request");
            telemetry?.Gauge("http.duration_ms", stopwatch.Elapsed.TotalMilliseconds);
            telemetry?.Increment("http.response_bytes", response.Body?.Length ?? 0);
            if (!response.IsSuccess)
            {
                telemetry?.Increment("http.failure");
            }

            return response;
        }

        /// <summary>
        /// 等待 Unity 的异步操作完成，同时让取消令牌可立即中断等待。
        /// </summary>
        /// <param name="operation">Unity 网络异步操作。</param>
        /// <returns>完成任务。</returns>
        private static Task WaitForCompletionAsync(UnityWebRequestAsyncOperation operation, CancellationToken token)
        {
            TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
            operation.completed += _ => completion.TrySetResult(true);
            if (operation.isDone)
            {
                completion.TrySetResult(true);
            }

            if (!token.CanBeCanceled)
            {
                return completion.Task;
            }

            return WaitWithCancellationAsync(completion.Task, token);
        }

        /// <summary>
        /// 将普通任务与取消令牌组合为单一等待任务。
        /// </summary>
        /// <param name="task">待等待任务。</param>
        /// <returns>可取消的等待任务。</returns>
        private static async Task WaitWithCancellationAsync(Task task, CancellationToken token)
        {
            TaskCompletionSource<bool> cancellation = new TaskCompletionSource<bool>();
            using (token.Register(() => cancellation.TrySetCanceled(token)))
            {
                Task completed = await Task.WhenAny(task, cancellation.Task);
                await completed;
            }
        }

        /// <summary>
        /// 对可选鉴权服务应用请求头。
        /// </summary>
        /// <param name="headers">待修改请求头。</param>
        /// <returns>鉴权完成任务。</returns>
        private MTask ApplyAuthenticationAsync(IDictionary<string, string> headers)
        {
            return authProvider == null ? MTask.CompletedTask : authProvider.ApplyAsync(headers);
        }

        /// <summary>
        /// 验证请求地址为 HTTP 或 HTTPS 绝对地址。
        /// </summary>
        /// <param name="url">调用方提供的完整请求地址。</param>
        /// <returns>可发送的绝对地址。</returns>
        private static string ValidateAbsoluteUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("HTTP 请求地址不能为空。", nameof(url));
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("HTTP 请求地址必须是 HTTP 或 HTTPS 的绝对地址。", nameof(url));
            }

            return url;
        }

        /// <summary>
        /// 判断错误是否属于可重试的临时网络或服务端失败。
        /// </summary>
        /// <param name="response">本次请求响应。</param>
        /// <returns>可重试时返回 true。</returns>
        private static bool ShouldRetry(HttpResponse response)
        {
            return response.StatusCode == 0 || response.StatusCode == 408 || response.StatusCode == 429 || response.StatusCode >= 500;
        }

        /// <summary>
        /// 将失败响应转换为附带状态码的明确异常。
        /// </summary>
        /// <param name="response">待验证响应。</param>
        private static void EnsureSuccess(HttpResponse response)
        {
            if (response == null || !response.IsSuccess)
            {
                throw new InvalidOperationException($"HTTP 请求失败，状态码：{response?.StatusCode ?? 0}，错误：{response?.Error ?? "unknown"}。");
            }
        }

        #endregion
    }
}
