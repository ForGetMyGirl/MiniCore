using System;
using MiniCore.Model;

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
        /// 获取或设置当前业务的持久化模式名称。
        /// </summary>
        public string PersistenceMode { get; set; } = nameof(ServerPersistenceMode.None);

        /// <summary>
        /// 将 Role 名称数组解析为框架 Role 标志。
        /// </summary>
        /// <returns>合并后的 Role 标志。</returns>
        public DedicatedServerRole ParseRoles()
        {
            if (Roles == null || Roles.Length == 0)
            {
                throw new InvalidOperationException("Dedicated Server 配置必须至少包含一个 Role。");
            }

            DedicatedServerRole result = DedicatedServerRole.None;
            for (int index = 0; index < Roles.Length; index++)
            {
                if (!Enum.TryParse(Roles[index], true, out DedicatedServerRole role) || role == DedicatedServerRole.None || (role & ~DedicatedServerRole.All) != 0)
                {
                    throw new InvalidOperationException($"未知 Dedicated Server Role：{Roles[index] ?? "<null>"}。");
                }

                result |= role;
            }

            if (result == DedicatedServerRole.None)
            {
                throw new InvalidOperationException("Dedicated Server 配置必须至少包含一个 Role。");
            }

            return result;
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
