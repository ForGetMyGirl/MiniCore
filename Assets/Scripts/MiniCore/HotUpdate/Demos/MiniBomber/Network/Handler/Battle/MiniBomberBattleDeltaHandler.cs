using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 接收十到十五赫兹房间级玩家动态增量。
    /// </summary>
    public sealed class MiniBomberBattleDeltaHandler : AMHandler<MiniBomberBattleDelta>
    {
        /// <summary>
        /// 将连续增量应用到客户端复制状态。
        /// </summary>
        /// <param name="session">消息会话。</param>
        /// <param name="message">房间级动态增量。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberBattleDelta message)
        {
            BattleClientComponent battle = Global.Get<BattleClientComponent>(this);
            try
            {
                battle?.ApplyDelta(message);
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }
}
