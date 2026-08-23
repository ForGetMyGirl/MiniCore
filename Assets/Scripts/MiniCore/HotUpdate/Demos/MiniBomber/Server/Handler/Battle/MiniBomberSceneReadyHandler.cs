using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 处理 MiniBomber 客户端战斗场景就绪请求。
    /// </summary>
    [MiniBomberServerHandler(MiniBomberServerRole.Game)]
    public sealed class MiniBomberSceneReadyHandler : ARpcHandler<MiniBomberSceneReadyRequest, MiniBomberSceneReadyResponse>
    {
        #region Public 公共成员

        /// <summary>
        /// 标记加载完成并在全员就绪后创建权威比赛。
        /// </summary>
        /// <param name="session">请求会话。</param>
        /// <param name="request">场景就绪请求。</param>
        /// <param name="response">场景就绪响应。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberSceneReadyRequest request, MiniBomberSceneReadyResponse response)
        {
            if (!MiniBomberServerHandlerUtility.TryGetRuntime(this, response, out MiniBomberServerRuntimeComponent runtime))
            {
                return MTask.CompletedTask;
            }

            try
            {
                runtime.SetSceneReady(session, request, response);
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }

        #endregion
    }
}
