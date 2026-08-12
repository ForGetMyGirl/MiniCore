using System;
using System.Collections.Generic;
using MiniCore.Protocol.Generated;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// Dedicated Server 内存中的房间成员。
    /// </summary>
    public sealed class MiniBomberServerRoomMember
    {
        #region Public 公共成员

        /// <summary>
        /// 成员玩家身份。
        /// </summary>
        public long PlayerId { get; set; }

        /// <summary>
        /// 成员玩家显示名。
        /// </summary>
        public string PlayerName { get; set; }

        /// <summary>
        /// 成员是否已经准备。
        /// </summary>
        public bool IsReady { get; set; }

        /// <summary>
        /// 成员是否在线。
        /// </summary>
        public bool IsOnline { get; set; }

        /// <summary>
        /// 战斗期间的当前得分。
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// 战斗场景是否加载完成。
        /// </summary>
        public bool IsSceneReady { get; set; }

        #endregion
    }
}
