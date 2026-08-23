namespace MiniCore.Server
{
    /// <summary>
    /// 描述只监听回环地址的 Dedicated Server 管理控制面。
    /// </summary>
    public sealed class ServerManagementOptions
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置管理监听地址；仅允许回环地址。
        /// </summary>
        public string Host { get; set; } = "127.0.0.1";

        /// <summary>
        /// 获取或设置管理端口。
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 获取或设置目标服务器本地 Token 文件。
        /// </summary>
        public string TokenFile { get; set; } = string.Empty;

        #endregion
    }
}
