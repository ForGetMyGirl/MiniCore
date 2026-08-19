namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 大厅中的单个房间摘要数据。
    /// </summary>
    public sealed class MiniBomberLobbyRoomModel
    {
        #region Public 公共成员

        /// <summary>
        /// 获取房间标识。
        /// </summary>
        public long RoomId { get; internal set; }

        /// <summary>
        /// 获取房间名称。
        /// </summary>
        public string RoomName { get; internal set; } = string.Empty;

        /// <summary>
        /// 获取房主名称。
        /// </summary>
        public string OwnerName { get; internal set; } = string.Empty;

        /// <summary>
        /// 获取当前玩家数量。
        /// </summary>
        public int PlayerCount { get; internal set; }

        /// <summary>
        /// 获取最大玩家数量。
        /// </summary>
        public int MaxPlayerCount { get; internal set; }

        /// <summary>
        /// 获取单局时长秒数。
        /// </summary>
        public int DurationSeconds { get; internal set; }

        /// <summary>
        /// 获取当前房间状态。
        /// </summary>
        public MiniBomberRoomStatus Status { get; internal set; }

        /// <summary>
        /// 获取房间修订号。
        /// </summary>
        public long Revision { get; internal set; }

        #endregion
    }
}
