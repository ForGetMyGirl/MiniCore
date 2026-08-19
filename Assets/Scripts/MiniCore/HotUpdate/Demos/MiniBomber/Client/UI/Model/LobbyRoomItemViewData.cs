namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 大厅窗口单个房间条目的显示数据。
    /// </summary>
    public sealed class LobbyRoomItemViewData
    {
        /// <summary>获取房间标识。</summary>
        public long RoomId { get; internal set; }
        /// <summary>获取房间名称。</summary>
        public string RoomName { get; internal set; } = string.Empty;
        /// <summary>获取当前玩家数量。</summary>
        public int PlayerCount { get; internal set; }
        /// <summary>获取最大玩家数量。</summary>
        public int MaxPlayerCount { get; internal set; }
        /// <summary>获取单局时长秒数。</summary>
        public int DurationSeconds { get; internal set; }
        /// <summary>获取房主显示名。</summary>
        public string OwnerName { get; internal set; } = string.Empty;
    }
}
