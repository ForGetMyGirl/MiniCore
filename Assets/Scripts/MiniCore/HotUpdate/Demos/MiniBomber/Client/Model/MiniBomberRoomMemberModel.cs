namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 当前房间中的成员业务数据。
    /// </summary>
    public sealed class MiniBomberRoomMemberModel
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
        /// 判断玩家是否为房主。
        /// </summary>
        public bool IsOwner { get; internal set; }

        /// <summary>
        /// 判断玩家是否已经准备。
        /// </summary>
        public bool IsReady { get; internal set; }

        /// <summary>
        /// 判断玩家是否在线。
        /// </summary>
        public bool IsOnline { get; internal set; }

        /// <summary>
        /// 获取玩家当前得分。
        /// </summary>
        public int Score { get; internal set; }

        #endregion
    }
}
