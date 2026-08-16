using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 处理客户端战斗基线不匹配后的重同步请求。
    /// </summary>
    [ServerHandler(DedicatedServerRole.Game)]
    public sealed class MiniBomberBattleResyncHandler : ARpcHandler<MiniBomberBattleResyncRequest, MiniBomberBattleResyncResponse>
    {
        #region Public 公共成员

        /// <summary>
        /// 校验比赛身份并安排单会话完整关键帧。
        /// </summary>
        /// <param name="session">请求会话。</param>
        /// <param name="request">客户端同步基线。</param>
        /// <param name="response">请求接受状态。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberBattleResyncRequest request, MiniBomberBattleResyncResponse response)
        {
            if (!MiniBomberServerHandlerUtility.TryGetRuntime(this, response, out MiniBomberServerRuntimeComponent runtime))
            {
                return MTask.CompletedTask;
            }

            try
            {
                runtime.RequestBattleResync(session, request, response);
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
