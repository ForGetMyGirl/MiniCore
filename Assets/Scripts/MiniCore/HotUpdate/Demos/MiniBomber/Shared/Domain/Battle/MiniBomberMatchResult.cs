using System;
using System.Collections.Generic;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 比赛结束后由服务器生成的稳定排名项。
    /// </summary>
    public readonly struct MiniBomberMatchResult
    {
        #region Public 公共成员

        public int Rank { get; }
        public long PlayerId { get; }
        public string PlayerName { get; }
        public int Score { get; }
        public int Kills { get; }
        public int Deaths { get; }
        public bool IsOnline { get; }

        /// <summary>
        /// 创建服务器最终排名项。
        /// </summary>
        /// <param name="rank">从一开始的名次。</param>
        /// <param name="playerId">玩家编号。</param>
        /// <param name="playerName">玩家显示名。</param>
        /// <param name="score">最终得分。</param>
        /// <param name="kills">击杀数。</param>
        /// <param name="deaths">死亡数。</param>
        /// <param name="isOnline">结束时是否在线。</param>
        public MiniBomberMatchResult(
            int rank,
            long playerId,
            string playerName,
            int score,
            int kills,
            int deaths,
            bool isOnline)
        {
            Rank = rank;
            PlayerId = playerId;
            PlayerName = playerName;
            Score = score;
            Kills = kills;
            Deaths = deaths;
            IsOnline = isOnline;
        }

        #endregion
    }
}
