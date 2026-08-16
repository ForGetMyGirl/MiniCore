using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 接收炸弹、爆炸、击杀和复活即时事件。
    /// </summary>
    public sealed class MiniBomberBattleEventHandler : AMHandler<MiniBomberBattleEventBatch>
    {
        /// <summary>
        /// 把即时事件应用到客户端战斗组件。
        /// </summary>
        /// <param name="session">消息会话。</param>
        /// <param name="message">即时事件批次。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberBattleEventBatch message)
        {
            BattleClientComponent battle = Global.Get<BattleClientComponent>(this);
            try
            {
                battle?.ApplyEvents(message);
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }
}
