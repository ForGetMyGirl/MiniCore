using System;
using System.Collections.Generic;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 加入权威战斗的玩家初始资料。
    /// </summary>
    public readonly struct MiniBomberBattleParticipant
    {
        #region Public 公共成员

        public long PlayerId { get; }
        public string PlayerName { get; }

        /// <summary>
        /// 创建战斗参与者资料。
        /// </summary>
        /// <param name="playerId">稳定玩家身份。</param>
        /// <param name="playerName">显示名称。</param>
        public MiniBomberBattleParticipant(long playerId, string playerName)
        {
            PlayerId = playerId;
            PlayerName = playerName ?? string.Empty;
        }

        #endregion
    }
}
