namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 客户端战斗道具的长期业务数据。
    /// </summary>
    public sealed class MiniBomberBattlePickupModel
    {
        #region Public 公共成员

        /// <summary>
        /// 获取道具标识。
        /// </summary>
        public long PickupId { get; internal set; }

        /// <summary>
        /// 获取道具类型。
        /// </summary>
        public MiniBomberPickupKind Kind { get; internal set; }

        /// <summary>
        /// 获取格子 X 坐标。
        /// </summary>
        public int CellX { get; internal set; }

        /// <summary>
        /// 获取格子 Z 坐标。
        /// </summary>
        public int CellZ { get; internal set; }

        #endregion
    }
}
