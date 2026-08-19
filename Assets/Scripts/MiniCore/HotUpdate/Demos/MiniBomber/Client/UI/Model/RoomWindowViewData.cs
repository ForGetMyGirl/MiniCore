using System.Collections.Generic;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 房间窗口一次完整刷新的专用显示数据。
    /// </summary>
    public sealed class RoomWindowViewData
    {
        #region Private 私有成员

        private readonly List<RoomMemberViewData> members = new List<RoomMemberViewData>(4); // 复用成员显示条目。

        #endregion

        #region Public 公共成员

        /// <summary>获取房间标识。</summary>
        public long RoomId { get; internal set; }
        /// <summary>获取房间名称。</summary>
        public string RoomName { get; internal set; } = string.Empty;
        /// <summary>获取局时长选项索引。</summary>
        public int DurationIndex { get; internal set; }
        /// <summary>判断当前玩家是否为房主。</summary>
        public bool IsOwner { get; internal set; }
        /// <summary>判断当前玩家是否已经准备。</summary>
        public bool LocalReady { get; internal set; }
        /// <summary>获取成员显示列表。</summary>
        public IReadOnlyList<RoomMemberViewData> Members => members;

        #endregion

        #region Internal 内部成员

        /// <summary>获取仅供 Presenter 投影复用的成员集合。</summary>
        internal List<RoomMemberViewData> MutableMembers => members;

        #endregion
    }
}
