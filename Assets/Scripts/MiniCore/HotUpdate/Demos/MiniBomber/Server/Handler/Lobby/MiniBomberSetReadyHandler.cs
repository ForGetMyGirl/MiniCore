using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 处理 MiniBomber 准备状态请求。
    /// </summary>
    [MiniBomberServerHandler(MiniBomberServerRole.Lobby)]
    public sealed class MiniBomberSetReadyHandler : ARpcHandler<MiniBomberSetReadyRequest, MiniBomberSetReadyResponse>
    {
        /// <summary>
        /// 更新单个成员准备状态。
        /// </summary>
        /// <param name="session">请求会话。</param>
        /// <param name="request">准备请求。</param>
        /// <param name="response">准备响应。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberSetReadyRequest request, MiniBomberSetReadyResponse response)
        {
            if (!MiniBomberServerHandlerUtility.TryGetRuntime(this, response, out MiniBomberServerRuntimeComponent runtime))
            {
                return MTask.CompletedTask;
            }

            try
            {
                runtime.SetReady(session, request, response);
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }
}
