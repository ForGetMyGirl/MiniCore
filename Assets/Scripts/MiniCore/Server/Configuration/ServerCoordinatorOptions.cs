namespace MiniCore.Server
{
    /// <summary>
    /// 保存 Dedicated Server 访问 Coordinator 内网监听所需的部署参数。
    /// </summary>
    public sealed class ServerCoordinatorOptions
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置 Coordinator 内网主机名或地址。
        /// </summary>
        public string InnerHost { get; set; } = "127.0.0.1";

        /// <summary>
        /// 获取或设置 Coordinator 内网端口。
        /// </summary>
        public int InnerPort { get; set; } = 7000;

        #endregion
    }
}
