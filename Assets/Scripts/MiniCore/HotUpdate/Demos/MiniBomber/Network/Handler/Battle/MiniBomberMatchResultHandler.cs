using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 接收服务器唯一比赛成绩。
    /// </summary>
    public sealed class MiniBomberMatchResultHandler : AMHandler<MiniBomberMatchResultNotice>
    {
        /// <summary>
        /// 原样应用服务器排名，不在客户端重新计算。
        /// </summary>
        /// <param name="session">消息会话。</param>
        /// <param name="message">最终成绩。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberMatchResultNotice message)
        {
            BattleClientComponent battle = Global.Get<BattleClientComponent>(this);
            try
            {
                battle?.ApplyResult(message);
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }
}
