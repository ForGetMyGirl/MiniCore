using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 处理 MiniBomber 登录请求。
    /// </summary>
    public sealed class MiniBomberLoginHandler : ARpcHandler<MiniBomberLoginRequest, MiniBomberLoginResponse>
    {
        /// <summary>
        /// 验证账号并绑定服务器会话。
        /// </summary>
        /// <param name="session">请求会话。</param>
        /// <param name="request">登录请求。</param>
        /// <param name="response">登录响应。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberLoginRequest request, MiniBomberLoginResponse response)
        {
            if (!MiniBomberServerHandlerUtility.TryGetRuntime(this, response, out MiniBomberServerRuntimeComponent runtime))
            {
                return MTask.CompletedTask;
            }

            try
            {
                runtime.Login(session, request, response);
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }
}
