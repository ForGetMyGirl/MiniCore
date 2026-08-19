using System.Collections.Generic;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 客户端当前房间的长期业务数据。
    /// </summary>
    public sealed class MiniBomberRoomModel
    {
        #region Private 私有成员

        private readonly List<MiniBomberRoomMemberModel> members = new List<MiniBomberRoomMemberModel>(4); // 当前房间成员。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 判断当前是否持有有效房间数据。
        /// </summary>
        public bool HasRoom => RoomId > 0;

        /// <summary>
        /// 获取房间标识。
        /// </summary>
        public long RoomId { get; internal set; }

        /// <summary>
        /// 获取房间名称。
        /// </summary>
        public string RoomName { get; internal set; } = string.Empty;

        /// <summary>
        /// 获取房主玩家标识。
        /// </summary>
        public long OwnerPlayerId { get; internal set; }

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

        /// <summary>
        /// 获取当前比赛标识。
        /// </summary>
        public long MatchId { get; internal set; }

        /// <summary>
        /// 获取只读房间成员列表。
        /// </summary>
        public IReadOnlyList<MiniBomberRoomMemberModel> Members => members;

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 获取仅供房间组件归并的成员集合。
        /// </summary>
        internal List<MiniBomberRoomMemberModel> MutableMembers => members;

        #endregion
    }
}
