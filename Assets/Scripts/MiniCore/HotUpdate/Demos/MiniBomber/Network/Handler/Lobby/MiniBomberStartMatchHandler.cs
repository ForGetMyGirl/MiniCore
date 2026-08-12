using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 处理 MiniBomber 房主开始比赛请求。
    /// </summary>
    public sealed class MiniBomberStartMatchHandler : ARpcHandler<MiniBomberStartMatchRequest, MiniBomberStartMatchResponse>
    {
        /// <summary>
        /// 校验开局条件并进入场景加载阶段。
        /// </summary>
        /// <param name="session">请求会话。</param>
        /// <param name="request">开局请求。</param>
        /// <param name="response">开局响应。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberStartMatchRequest request, MiniBomberStartMatchResponse response)
        {
            if (!MiniBomberServerHandlerUtility.TryGetRuntime(this, response, out MiniBomberServerRuntimeComponent runtime))
            {
                return MTask.CompletedTask;
            }

            try
            {
                runtime.StartMatch(session, request, response);
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }
}
