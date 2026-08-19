namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 战斗 HUD 单个排名条目的显示数据。
    /// </summary>
    public sealed class BattleRankingItemViewData
    {
        /// <summary>
        /// 获取玩家显示名。
        /// </summary>
        public string PlayerName { get; internal set; } = string.Empty;

        /// <summary>
        /// 获取玩家当前得分。
        /// </summary>
        public int Score { get; internal set; }
    }
}
