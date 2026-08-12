using System;
using System.Collections.Generic;
using MiniCore.Protocol.Generated;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// Dedicated Server 持有的已认证玩家会话。
    /// </summary>
    public sealed class MiniBomberServerPlayerSession
    {
        #region Public 公共成员

        /// <summary>
        /// 稳定玩家身份。
        /// </summary>
        public long PlayerId { get; set; }

        /// <summary>
        /// 玩家显示名。
        /// </summary>
        public string PlayerName { get; set; }

        /// <summary>
        /// 当前网络会话标识。
        /// </summary>
        public string NetworkSessionId { get; set; }

        /// <summary>
        /// 用于断线恢复的随机令牌。
        /// </summary>
        public string SessionToken { get; set; }

        /// <summary>
        /// 当前所在房间身份。
        /// </summary>
        public long RoomId { get; set; }

        /// <summary>
        /// 当前参与比赛身份。
        /// </summary>
        public long MatchId { get; set; }

        /// <summary>
        /// 当前是否在线。
        /// </summary>
        public bool IsOnline { get; set; }

        /// <summary>
        /// 断线宽限截止的单调时间秒数。
        /// </summary>
        public double ReconnectDeadline { get; set; }

        #endregion
    }
}
