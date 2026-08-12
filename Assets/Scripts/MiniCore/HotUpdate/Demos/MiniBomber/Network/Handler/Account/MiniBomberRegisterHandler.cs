using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 处理 MiniBomber 注册请求。
    /// </summary>
    public sealed class MiniBomberRegisterHandler : ARpcHandler<MiniBomberRegisterRequest, MiniBomberRegisterResponse>
    {
        /// <summary>
        /// 验证版本并持久化新账号。
        /// </summary>
        /// <param name="session">请求会话。</param>
        /// <param name="request">注册请求。</param>
        /// <param name="response">注册响应。</param>
        /// <returns>注册完成任务。</returns>
        public override async MTask HandleAsync(NetworkSession session, MiniBomberRegisterRequest request, MiniBomberRegisterResponse response)
        {
            if (!MiniBomberServerHandlerUtility.TryGetRuntime(this, response, out MiniBomberServerRuntimeComponent runtime))
            {
                return;
            }

            try
            {
                await runtime.RegisterAsync(session, request, response);
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }
}
