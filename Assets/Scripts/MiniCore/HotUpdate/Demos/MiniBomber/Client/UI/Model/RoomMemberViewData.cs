namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 房间窗口单个成员的显示数据。
    /// </summary>
    public sealed class RoomMemberViewData
    {
        /// <summary>获取成员显示名。</summary>
        public string PlayerName { get; internal set; } = string.Empty;
        /// <summary>判断成员是否为房主。</summary>
        public bool IsOwner { get; internal set; }
        /// <summary>判断成员是否已经准备。</summary>
        public bool IsReady { get; internal set; }
        /// <summary>判断成员是否在线。</summary>
        public bool IsOnline { get; internal set; }
    }
}
