namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 客户端战斗炸弹的长期业务数据。
    /// </summary>
    public sealed class MiniBomberBattleBombModel
    {
        #region Public 公共成员

        /// <summary>
        /// 获取炸弹标识。
        /// </summary>
        public long BombId { get; internal set; }

        /// <summary>
        /// 获取所属玩家标识。
        /// </summary>
        public long OwnerPlayerId { get; internal set; }

        /// <summary>
        /// 获取格子 X 坐标。
        /// </summary>
        public int CellX { get; internal set; }

        /// <summary>
        /// 获取格子 Z 坐标。
        /// </summary>
        public int CellZ { get; internal set; }

        /// <summary>
        /// 获取爆炸范围。
        /// </summary>
        public int Range { get; internal set; }

        /// <summary>
        /// 获取爆炸服务器 Tick。
        /// </summary>
        public long ExplodeTick { get; internal set; }

        /// <summary>
        /// 判断炸弹所属玩家能否继续穿过。
        /// </summary>
        public bool OwnerCanPass { get; internal set; }

        #endregion
    }
}
