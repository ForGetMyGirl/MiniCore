using System;
using System.Collections.Generic;
using System.Threading;
using MiniCore.Threading;

namespace MiniCore.Service
{

    /// <summary>
    /// UnityWebRequest 等传输层共同使用的 HTTP 请求描述。
    /// </summary>
    public sealed class HttpRequest
    {
        /// <summary>
        /// 获取或设置 HTTP 或 HTTPS 的绝对请求地址。
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 获取或设置 HTTP 方法，例如 GET、POST 或 DELETE。
        /// </summary>
        public string Method { get; set; } = "GET";

        /// <summary>
        /// 获取或设置原始请求正文；空请求正文请保持 null。
        /// </summary>
        public byte[] Body { get; set; }

        /// <summary>
        /// 获取或设置请求内容类型。
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// 获取本请求独有的 Header 集合。
        /// </summary>
        public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 获取或设置超时秒数；小于等于零时使用服务默认值。
        /// </summary>
        public int TimeoutSeconds { get; set; }

        /// <summary>
        /// 获取或设置调用方是否允许对非 GET/HEAD 请求重试。
        /// </summary>
        public bool IsIdempotent { get; set; }
    }
}
