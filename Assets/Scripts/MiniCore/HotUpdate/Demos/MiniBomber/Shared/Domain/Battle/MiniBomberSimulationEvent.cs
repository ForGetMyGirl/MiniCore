using System;
using System.Collections.Generic;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 权威模拟单 Tick 产生的离散事件。
    /// </summary>
    public readonly struct MiniBomberSimulationEvent
    {
        #region Public 公共成员

        public long EventId { get; }
        public MiniBomberSimulationEventType Type { get; }
        public long ServerTick { get; }
        public long ActorPlayerId { get; }
        public long TargetPlayerId { get; }
        public long EntityId { get; }
        public int CellX { get; }
        public int CellZ { get; }

        /// <summary>
        /// 创建权威模拟事件。
        /// </summary>
        /// <param name="eventId">本局递增事件编号。</param>
        /// <param name="type">事件类型。</param>
        /// <param name="serverTick">事件发生的服务器 Tick。</param>
        /// <param name="actorPlayerId">行为发起玩家。</param>
        /// <param name="targetPlayerId">行为目标玩家。</param>
        /// <param name="entityId">炸弹等实体编号。</param>
        /// <param name="cellX">事件横向格坐标。</param>
        /// <param name="cellZ">事件纵向格坐标。</param>
        public MiniBomberSimulationEvent(long eventId, MiniBomberSimulationEventType type, long serverTick, long actorPlayerId, long targetPlayerId, long entityId, int cellX, int cellZ)
        {
            EventId = eventId;
            Type = type;
            ServerTick = serverTick;
            ActorPlayerId = actorPlayerId;
            TargetPlayerId = targetPlayerId;
            EntityId = entityId;
            CellX = cellX;
            CellZ = cellZ;
        }

        #endregion
    }
}
