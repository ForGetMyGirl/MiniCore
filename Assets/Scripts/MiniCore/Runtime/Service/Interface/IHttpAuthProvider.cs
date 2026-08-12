using System;
using System.Collections.Generic;
using System.Threading;
using MiniCore.Threading;

namespace MiniCore.Service
{

    /// <summary>
    /// 可选的 HTTP 鉴权服务。实现可在请求发送前写入令牌、签名或渠道标识。
    /// </summary>
    public interface IHttpAuthProvider : IAppService
    {
        /// <summary>
        /// 将鉴权信息追加到本次请求头集合。
        /// </summary>
        /// <param name="headers">可修改的请求头集合。</param>
        /// <returns>鉴权信息准备完成任务。</returns>
        MTask ApplyAsync(IDictionary<string, string> headers);
    }
}
