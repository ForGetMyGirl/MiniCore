using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 接收十五赫兹服务器权威战斗快照。
    /// </summary>
    public sealed class MiniBomberBattleSnapshotHandler : AMHandler<MiniBomberBattleSnapshot>
    {
        /// <summary>
        /// 把更新的快照应用到客户端战斗组件。
        /// </summary>
        /// <param name="session">消息会话。</param>
        /// <param name="message">权威战斗快照。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberBattleSnapshot message)
        {
            BattleClientComponent battle = Global.Get<BattleClientComponent>(this);
            try
            {
                battle?.ApplySnapshot(message);
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }
}
