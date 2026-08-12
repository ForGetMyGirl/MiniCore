using System;
using System.Collections.Generic;
using System.Threading;
using MiniCore.Threading;

namespace MiniCore.Service
{

    /// <summary>
    /// HTTP 请求完成后的原始响应。
    /// </summary>
    public sealed class HttpResponse
    {
        /// <summary>
        /// 获取 HTTP 状态码；传输失败时通常为零。
        /// </summary>
        public long StatusCode { get; set; }

        /// <summary>
        /// 获取响应正文。
        /// </summary>
        public byte[] Body { get; set; }

        /// <summary>
        /// 获取响应头集合。
        /// </summary>
        public IReadOnlyDictionary<string, string> Headers { get; set; }

        /// <summary>
        /// 获取传输层错误文本；成功时为 null。
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// 获取本请求是否成功完成。
        /// </summary>
        public bool IsSuccess => string.IsNullOrEmpty(Error) && StatusCode >= 200 && StatusCode <= 299;
    }
}
