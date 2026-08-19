namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 比赛结果窗口单个玩家的显示数据。
    /// </summary>
    public sealed class MatchResultEntryViewData
    {
        /// <summary>获取最终名次。</summary>
        public int Rank { get; internal set; }
        /// <summary>获取玩家显示名。</summary>
        public string PlayerName { get; internal set; } = string.Empty;
        /// <summary>获取最终得分。</summary>
        public int Score { get; internal set; }
        /// <summary>获取最终击杀数。</summary>
        public int Kills { get; internal set; }
        /// <summary>获取最终死亡数。</summary>
        public int Deaths { get; internal set; }
    }
}
