namespace MiniCore.Server
{
    /// <summary>
    /// 保存向 Coordinator 和调用方公布的可访问地址。
    /// </summary>
    public sealed class ServerAdvertisedOptions
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置其他服务端可访问的内网主机。
        /// </summary>
        public string InnerHost { get; set; } = "127.0.0.1";

        /// <summary>
        /// 获取或设置其他服务端可访问的内网端口。
        /// </summary>
        public int InnerPort { get; set; } = 7100;

        /// <summary>
        /// 获取或设置客户端可访问的完整 WebSocket 地址。
        /// 没有外网入口的 Role 允许留空。
        /// </summary>
        public string OuterWebSocketUrl { get; set; } = string.Empty;

        #endregion
    }
}
