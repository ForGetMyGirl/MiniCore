using System;
using System.Collections.Generic;
using System.Threading;
using MiniCore.Threading;

namespace MiniCore.Service
{

    /// <summary>
    /// 提供原始、JSON 和 Protobuf HTTP 通信能力的系统服务契约。
    /// </summary>
    public interface IHttpService : IAppService
    {
        /// <summary>
        /// 发送原始 HTTP 请求。
        /// </summary>
        /// <param name="request">请求描述。</param>
        /// <returns>原始 HTTP 响应。</returns>
        MTask<HttpResponse> SendAsync(HttpRequest request);

        /// <summary>
        /// 发送 JSON 请求并反序列化 JSON 响应。
        /// </summary>
        /// <typeparam name="TRequest">JSON 请求类型。</typeparam>
        /// <typeparam name="TResponse">JSON 响应类型。</typeparam>
        /// <param name="url">HTTP 或 HTTPS 的绝对请求地址。</param>
        /// <param name="request">请求对象。</param>
        /// <param name="method">HTTP 方法。</param>
        /// <returns>反序列化后的响应对象。</returns>
        MTask<TResponse> SendJsonAsync<TRequest, TResponse>(string url, TRequest request, string method = "POST");

        /// <summary>
        /// 发送 Protobuf 二进制请求并将响应反序列化为指定消息类型。
        /// </summary>
        /// <typeparam name="TResponse">响应消息类型。</typeparam>
        /// <param name="url">HTTP 或 HTTPS 的绝对请求地址。</param>
        /// <param name="requestBody">请求消息序列化后的二进制正文。</param>
        /// <param name="responseParser">将原始响应正文转换为目标消息的函数。</param>
        /// <param name="method">HTTP 方法。</param>
        /// <returns>响应消息。</returns>
        MTask<TResponse> SendProtobufAsync<TResponse>(string url, byte[] requestBody, Func<byte[], TResponse> responseParser, string method = "POST");
    }
}
