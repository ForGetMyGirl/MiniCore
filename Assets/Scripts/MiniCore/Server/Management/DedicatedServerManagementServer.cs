using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MiniCore.Server
{
    /// <summary>
    /// 在后台接受只来自回环地址的 HTTP 管理请求，并交给主线程组件处理。
    /// </summary>
    internal sealed class DedicatedServerManagementServer : IDisposable
    {
        #region Private 私有成员

        private readonly HttpListener listener = new HttpListener(); // 回环 HTTP 监听器。
        private readonly ConcurrentQueue<HttpListenerContext> requests = new ConcurrentQueue<HttpListenerContext>(); // 主线程待处理请求。
        private readonly string token; // 从实例本地受限文件读取的 Bearer Token。
        private bool disposed; // 是否已经停止监听。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建回环管理服务器并读取本地 Token。
        /// </summary>
        /// <param name="options">管理端配置。</param>
        internal DedicatedServerManagementServer(ServerManagementOptions options)
        {
            if (options == null || !string.Equals(options.Host, "127.0.0.1", StringComparison.Ordinal))
            {
                throw new InvalidDataException("管理服务器只允许监听 127.0.0.1。");
            }

            token = File.ReadAllText(options.TokenFile).Trim();
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidDataException("Dedicated Server 管理 Token 文件为空。");
            }

            listener.Prefixes.Add($"http://127.0.0.1:{options.Port}/");
        }

        /// <summary>
        /// 启动后台接收循环。
        /// </summary>
        internal void Start()
        {
            listener.Start();
            ListenLoopAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// 尝试取得一个已经完成认证的管理请求。
        /// </summary>
        /// <param name="context">成功时返回 HTTP 上下文。</param>
        /// <returns>存在待处理请求时返回 true。</returns>
        internal bool TryDequeue(out HttpListenerContext context)
        {
            return requests.TryDequeue(out context);
        }

        /// <summary>
        /// 写入 JSON 响应并关闭当前请求。
        /// </summary>
        /// <param name="context">HTTP 上下文。</param>
        /// <param name="statusCode">HTTP 状态码。</param>
        /// <param name="json">JSON 响应。</param>
        internal static void Respond(HttpListenerContext context, int statusCode, string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json ?? "{}");
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.OutputStream.Close();
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 停止监听并拒绝尚未进入主线程的请求。
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            listener.Close();
            while (requests.TryDequeue(out HttpListenerContext context))
            {
                Respond(context, 503, "{\"error\":\"server_stopping\"}");
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 持续接受请求，在后台完成 Token 校验后排入主线程队列。
        /// </summary>
        /// <returns>监听生命周期任务。</returns>
        private async Task ListenLoopAsync()
        {
            while (!disposed)
            {
                HttpListenerContext context;
                try
                {
                    context = await listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (HttpListenerException) when (disposed)
                {
                    return;
                }
                catch (ObjectDisposedException) when (disposed)
                {
                    return;
                }

                string authorization = context.Request.Headers["Authorization"] ?? string.Empty;
                if (!FixedTimeEquals(authorization, "Bearer " + token))
                {
                    Respond(context, 401, "{\"error\":\"unauthorized\"}");
                    continue;
                }

                requests.Enqueue(context);
            }
        }

        /// <summary>
        /// 使用固定时间字符比较避免根据首个差异位置泄漏 Token 信息。
        /// </summary>
        /// <param name="left">请求值。</param>
        /// <param name="right">期望值。</param>
        /// <returns>完全相同时返回 true。</returns>
        private static bool FixedTimeEquals(string left, string right)
        {
            int difference = left.Length ^ right.Length;
            int length = Math.Max(left.Length, right.Length);
            for (int index = 0; index < length; index++)
            {
                char leftValue = index < left.Length ? left[index] : '\0';
                char rightValue = index < right.Length ? right[index] : '\0';
                difference |= leftValue ^ rightValue;
            }

            return difference == 0;
        }

        #endregion
    }
}
