using System;

namespace MiniCore.Server
{
    /// <summary>
    /// 表示一个 Dedicated Server 部署副本的完整运行配置。
    /// </summary>
    public sealed class MiniCoreServerRuntimeConfig
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置集群内唯一的服务实例标识。
        /// </summary>
        public string InstanceId { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置环境唯一标识。
        /// </summary>
        public string EnvironmentId { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置环境统一发布版本。
        /// </summary>
        public string ReleaseVersion { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置固定控制面兼容版本。
        /// </summary>
        public string ControlProtocolVersion { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置当前进程启用的 Role 名称。
        /// </summary>
        public string[] Roles { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 获取或设置 Coordinator 内网连接参数。
        /// </summary>
        public ServerCoordinatorOptions Coordinator { get; set; } = new ServerCoordinatorOptions();

        /// <summary>
        /// 获取或设置当前进程监听参数。
        /// </summary>
        public ServerListenerOptions Listeners { get; set; } = new ServerListenerOptions();

        /// <summary>
        /// 获取或设置当前进程向目录公布的地址。
        /// </summary>
        public ServerAdvertisedOptions Advertised { get; set; } = new ServerAdvertisedOptions();

        /// <summary>
        /// 获取或设置本机管理控制面参数。
        /// </summary>
        public ServerManagementOptions Management { get; set; } = new ServerManagementOptions();

        /// <summary>
        /// 获取或设置实例日志目录。
        /// </summary>
        public string LogPath { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置独立于发布版本的配置版本。
        /// </summary>
        public string ConfigVersion { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置移除 configSha256 字段后原始 JSON 的 SHA-256。
        /// </summary>
        public string ConfigSha256 { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置当前业务的持久化模式名称。
        /// </summary>
        public string PersistenceMode { get; set; } = nameof(ServerPersistenceMode.None);

        /// <summary>
        /// 将 Role 名称数组解析为框架 Role 标志。
        /// </summary>
        /// <returns>合并后的 Role 标志。</returns>
        /// <param name="catalog">随不可变制品发布的 Role Catalog。</param>
        public MiniCore.Model.ServerRoleMask ParseRoles(ServerRoleCatalog catalog)
        {
            return (catalog ?? throw new ArgumentNullException(nameof(catalog))).ResolveMask(Roles);
        }

        /// <summary>
        /// 解析当前业务持久化模式。
        /// </summary>
        /// <returns>有效持久化模式。</returns>
        public ServerPersistenceMode ParsePersistenceMode()
        {
            if (!Enum.TryParse(PersistenceMode, true, out ServerPersistenceMode result))
            {
                throw new InvalidOperationException($"未知持久化模式：{PersistenceMode ?? "<null>"}。");
            }

            return result;
        }

        #endregion
    }
}
