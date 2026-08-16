namespace MiniCore.Server
{
    /// <summary>
    /// 保存 Dedicated Server 本地内外网监听参数。
    /// </summary>
    public sealed class ServerListenerOptions
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置内网监听主机。
        /// </summary>
        public string InnerHost { get; set; } = "0.0.0.0";

        /// <summary>
        /// 获取或设置内网 TCP 监听端口。
        /// </summary>
        public int InnerPort { get; set; } = 7100;

        /// <summary>
        /// 获取或设置外网监听主机。
        /// </summary>
        public string OuterHost { get; set; } = "0.0.0.0";

        /// <summary>
        /// 获取或设置外网 WebSocket 监听端口。
        /// </summary>
        public int OuterPort { get; set; } = 7101;

        /// <summary>
        /// 获取或设置外网 WebSocket 路径。
        /// </summary>
        public string OuterPath { get; set; } = "/minicore";

        #endregion
    }
}
