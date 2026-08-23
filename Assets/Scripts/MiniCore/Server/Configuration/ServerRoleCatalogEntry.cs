namespace MiniCore.Server
{
    /// <summary>
    /// 描述一个框架或业务定义的稳定 Role 目录项。
    /// </summary>
    public sealed class ServerRoleCatalogEntry
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置配置和发布工具使用的稳定键。
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置唯一单个位值。
        /// </summary>
        public ulong Value { get; set; }

        /// <summary>
        /// 获取或设置界面显示名称。
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置是否属于框架保留项。
        /// </summary>
        public bool FrameworkReserved { get; set; }

        /// <summary>
        /// 获取或设置是否允许生成客户端发现常量。
        /// </summary>
        public bool ClientDiscoverable { get; set; }

        /// <summary>
        /// 获取或设置客户端公开常量名称。
        /// </summary>
        public string PublicName { get; set; } = string.Empty;

        #endregion
    }
}
