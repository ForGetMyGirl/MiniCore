namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 客户端战斗玩家的长期业务数据。
    /// </summary>
    public sealed class MiniBomberBattlePlayerModel
    {
        #region Public 公共成员

        /// <summary>
        /// 获取玩家标识。
        /// </summary>
        public long PlayerId { get; internal set; }

        /// <summary>
        /// 获取玩家名称。
        /// </summary>
        public string PlayerName { get; internal set; } = string.Empty;

        /// <summary>
        /// 获取权威 X 毫米坐标。
        /// </summary>
        public int PositionXMillimeters { get; internal set; }

        /// <summary>
        /// 获取权威 Z 毫米坐标。
        /// </summary>
        public int PositionZMillimeters { get; internal set; }

        /// <summary>
        /// 获取量化朝向 X。
        /// </summary>
        public int FacingX { get; internal set; }

        /// <summary>
        /// 获取量化朝向 Z。
        /// </summary>
        public int FacingZ { get; internal set; }

        /// <summary>
        /// 判断玩家当前是否存活。
        /// </summary>
        public bool IsAlive { get; internal set; }

        /// <summary>
        /// 获取复活服务器 Tick。
        /// </summary>
        public long RespawnTick { get; internal set; }

        /// <summary>
        /// 获取无敌结束服务器 Tick。
        /// </summary>
        public long InvulnerableUntilTick { get; internal set; }

        /// <summary>
        /// 获取当前得分。
        /// </summary>
        public int Score { get; internal set; }

        /// <summary>
        /// 获取击杀次数。
        /// </summary>
        public int Kills { get; internal set; }

        /// <summary>
        /// 获取死亡次数。
        /// </summary>
        public int Deaths { get; internal set; }

        /// <summary>
        /// 获取炸弹容量。
        /// </summary>
        public int BombCapacity { get; internal set; }

        /// <summary>
        /// 获取爆炸范围。
        /// </summary>
        public int BombRange { get; internal set; }

        /// <summary>
        /// 获取服务器已确认输入序号。
        /// </summary>
        public long AcknowledgedInputSequence { get; internal set; }

        /// <summary>
        /// 判断玩家当前是否在线。
        /// </summary>
        public bool IsOnline { get; internal set; }

        #endregion
    }
}
