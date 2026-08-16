using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 处理 MiniBomber 断线恢复请求。
    /// </summary>
    [ServerHandler(DedicatedServerRole.Lobby)]
    public sealed class MiniBomberResumeSessionHandler : ARpcHandler<MiniBomberResumeSessionRequest, MiniBomberResumeSessionResponse>
    {
        /// <summary>
        /// 恢复认证、房间和比赛状态。
        /// </summary>
        /// <param name="session">新网络会话。</param>
        /// <param name="request">恢复请求。</param>
        /// <param name="response">恢复响应。</param>
        /// <returns>已完成任务。</returns>
        public override async MTask HandleAsync(NetworkSession session, MiniBomberResumeSessionRequest request, MiniBomberResumeSessionResponse response)
        {
            if (!MiniBomberServerHandlerUtility.TryGetRuntime(this, response, out MiniBomberServerRuntimeComponent runtime))
            {
                return;
            }

            try
            {
                await runtime.ResumeSessionAsync(session, request, response);
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }
}
