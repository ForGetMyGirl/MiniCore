using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 接收服务器统一比赛倒计时。
    /// </summary>
    public sealed class MiniBomberMatchCountdownHandler : AMHandler<MiniBomberMatchCountdownNotice>
    {
        /// <summary>
        /// 应用权威倒计时并进入战斗流程。
        /// </summary>
        /// <param name="session">消息会话。</param>
        /// <param name="message">比赛倒计时。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberMatchCountdownNotice message)
        {
            MiniBomberClientFlowComponent flow = Global.Get<MiniBomberClientFlowComponent>(this);
            try
            {
                flow?.ApplyCountdown(message);
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }
}
