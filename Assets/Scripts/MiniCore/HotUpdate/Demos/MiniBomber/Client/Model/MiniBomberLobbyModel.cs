using System.Collections.Generic;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 客户端大厅的长期业务数据。
    /// </summary>
    public sealed class MiniBomberLobbyModel
    {
        #region Private 私有成员

        private readonly List<MiniBomberLobbyRoomModel> rooms = new List<MiniBomberLobbyRoomModel>(32); // 当前大厅房间摘要。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取大厅修订号。
        /// </summary>
        public long Revision { get; internal set; }

        /// <summary>
        /// 获取服务器报告的在线人数。
        /// </summary>
        public int OnlinePlayerCount { get; internal set; }

        /// <summary>
        /// 获取只读房间摘要列表。
        /// </summary>
        public IReadOnlyList<MiniBomberLobbyRoomModel> Rooms => rooms;

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 获取仅供大厅组件归并的房间集合。
        /// </summary>
        internal List<MiniBomberLobbyRoomModel> MutableRooms => rooms;

        #endregion
    }
}
