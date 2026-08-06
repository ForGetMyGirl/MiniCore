using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>接收大厅房间列表修订通知。</summary>
    public sealed class MiniBomberLobbyChangedHandler : AMHandler<MiniBomberLobbyChangedNotice>
    {
        /// <summary>
        /// 通知大厅组件服务器修订号已经变化。
        /// </summary>
        /// <param name="session">消息会话。</param>
        /// <param name="message">大厅修订通知。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberLobbyChangedNotice message)
        {
            LobbyComponent lobby = Global.Get<LobbyComponent>(this);
            try
            {
                lobby?.ApplyChangedNotice(message.Revision);
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }

    /// <summary>接收当前房间权威快照。</summary>
    public sealed class MiniBomberRoomSnapshotHandler : AMHandler<MiniBomberRoomSnapshotNotice>
    {
        /// <summary>
        /// 把服务器快照应用到房间组件。
        /// </summary>
        /// <param name="session">消息会话。</param>
        /// <param name="message">房间快照通知。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberRoomSnapshotNotice message)
        {
            RoomComponent room = Global.Get<RoomComponent>(this);
            try
            {
                room?.ApplySnapshot(message.Room);
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }

    /// <summary>接收比赛场景加载参数。</summary>
    public sealed class MiniBomberMatchPrepareHandler : AMHandler<MiniBomberMatchPrepareNotice>
    {
        #region Override 重写实现

        /// <summary>
        /// 将场景切换任务交给客户端流程监督，立即释放串行收包队列以接收后续就绪 RPC 响应。
        /// </summary>
        /// <param name="session">消息会话。</param>
        /// <param name="message">比赛准备通知。</param>
        /// <returns>通知接收完成任务；场景切换由流程组件的任务域继续监督。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberMatchPrepareNotice message)
        {
            MiniBomberClientFlowComponent flow = Global.Get<MiniBomberClientFlowComponent>(this);
            try
            {
                flow?.HandleMatchPrepareAsync(message).Forget();
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }

        #endregion
    }

    /// <summary>接收服务器统一比赛倒计时。</summary>
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

    /// <summary>接收十五赫兹服务器权威战斗快照。</summary>
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

    /// <summary>接收十到十五赫兹房间级玩家动态增量。</summary>
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

    /// <summary>接收炸弹、爆炸、击杀和复活即时事件。</summary>
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

    /// <summary>接收服务器唯一比赛成绩。</summary>
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

    /// <summary>接收服务器主动断开原因。</summary>
    public sealed class MiniBomberDisconnectHandler : AMHandler<MiniBomberDisconnectNotice>
    {
        /// <summary>
        /// 根据服务器标记进入重连或登录流程。
        /// </summary>
        /// <param name="session">消息会话。</param>
        /// <param name="message">断开通知。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberDisconnectNotice message)
        {
            MiniBomberClientFlowComponent flow = Global.Get<MiniBomberClientFlowComponent>(this);
            try
            {
            flow?.HandleDisconnected(message.Reason, message.MayResume);
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }
}
