namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 单个玩家的最终比赛成绩。
    /// </summary>
    public sealed class MiniBomberMatchResultEntryModel
    {
        #region Public 公共成员

        /// <summary>
        /// 获取最终名次。
        /// </summary>
        public int Rank { get; internal set; }

        /// <summary>
        /// 获取玩家标识。
        /// </summary>
        public long PlayerId { get; internal set; }

        /// <summary>
        /// 获取玩家名称。
        /// </summary>
        public string PlayerName { get; internal set; } = string.Empty;

        /// <summary>
        /// 获取最终得分。
        /// </summary>
        public int Score { get; internal set; }

        /// <summary>
        /// 获取最终击杀数。
        /// </summary>
        public int Kills { get; internal set; }

        /// <summary>
        /// 获取最终死亡数。
        /// </summary>
        public int Deaths { get; internal set; }

        /// <summary>
        /// 判断结算时玩家是否在线。
        /// </summary>
        public bool IsOnline { get; internal set; }

        #endregion
    }
}
