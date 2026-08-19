namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 客户端战斗即时事件的长期业务数据。
    /// </summary>
    public sealed class MiniBomberBattleEventModel
    {
        #region Public 公共成员

        /// <summary>
        /// 获取事件标识。
        /// </summary>
        public long EventId { get; internal set; }

        /// <summary>
        /// 获取事件类型。
        /// </summary>
        public MiniBomberBattleEventKind Kind { get; internal set; }

        /// <summary>
        /// 获取服务器 Tick。
        /// </summary>
        public long ServerTick { get; internal set; }

        /// <summary>
        /// 获取发起玩家标识。
        /// </summary>
        public long ActorPlayerId { get; internal set; }

        /// <summary>
        /// 获取目标玩家标识。
        /// </summary>
        public long TargetPlayerId { get; internal set; }

        /// <summary>
        /// 获取关联实体标识。
        /// </summary>
        public long EntityId { get; internal set; }

        /// <summary>
        /// 获取格子 X 坐标。
        /// </summary>
        public int CellX { get; internal set; }

        /// <summary>
        /// 获取格子 Z 坐标。
        /// </summary>
        public int CellZ { get; internal set; }

        /// <summary>
        /// 获取关联道具类型。
        /// </summary>
        public MiniBomberPickupKind PickupKind { get; internal set; }

        /// <summary>
        /// 获取发起玩家名称。
        /// </summary>
        public string ActorName { get; internal set; } = string.Empty;

        /// <summary>
        /// 获取目标玩家名称。
        /// </summary>
        public string TargetName { get; internal set; } = string.Empty;

        /// <summary>
        /// 获取事件后的得分。
        /// </summary>
        public int Score { get; internal set; }

        /// <summary>
        /// 获取事件后的击杀数。
        /// </summary>
        public int Kills { get; internal set; }

        /// <summary>
        /// 获取事件后的死亡数。
        /// </summary>
        public int Deaths { get; internal set; }

        #endregion
    }
}
