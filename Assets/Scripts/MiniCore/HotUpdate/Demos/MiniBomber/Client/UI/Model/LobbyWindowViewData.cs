using System.Collections.Generic;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 大厅窗口一次完整刷新的专用显示数据。
    /// </summary>
    public sealed class LobbyWindowViewData
    {
        #region Private 私有成员

        private readonly List<LobbyRoomItemViewData> rooms = new List<LobbyRoomItemViewData>(32); // 复用房间显示条目。

        #endregion

        #region Public 公共成员

        /// <summary>获取当前玩家显示名。</summary>
        public string PlayerName { get; internal set; } = string.Empty;
        /// <summary>获取在线玩家数量。</summary>
        public int OnlinePlayerCount { get; internal set; }
        /// <summary>获取当前房间显示列表。</summary>
        public IReadOnlyList<LobbyRoomItemViewData> Rooms => rooms;

        #endregion

        #region Internal 内部成员

        /// <summary>获取仅供 Presenter 投影复用的房间集合。</summary>
        internal List<LobbyRoomItemViewData> MutableRooms => rooms;

        #endregion
    }
}
