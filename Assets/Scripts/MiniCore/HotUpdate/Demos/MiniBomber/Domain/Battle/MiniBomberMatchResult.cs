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
        /// <param name="player">权威玩家状态。</param>
        public MiniBomberMatchResult(int rank, MiniBomberPlayerState player)
        {
            Rank = rank;
            PlayerId = player.PlayerId;
            PlayerName = player.PlayerName;
            Score = player.Score;
            Kills = player.Kills;
            Deaths = player.Deaths;
            IsOnline = player.IsOnline;
        }

        #endregion
    }
}
