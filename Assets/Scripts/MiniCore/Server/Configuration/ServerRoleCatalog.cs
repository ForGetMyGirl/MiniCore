using System;
using System.Collections.Generic;
using MiniCore.Model;

namespace MiniCore.Server
{
    /// <summary>
    /// 保存随不可变制品发布的框架与业务 Role 目录。
    /// </summary>
    public sealed class ServerRoleCatalog
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置目录 Schema 版本。
        /// </summary>
        public int SchemaVersion { get; set; } = 1;

        /// <summary>
        /// 获取或设置所有 Role 目录项。
        /// </summary>
        public ServerRoleCatalogEntry[] Roles { get; set; } = Array.Empty<ServerRoleCatalogEntry>();

        /// <summary>
        /// 校验目录唯一性并将配置 Role 键解析为不透明位集合。
        /// </summary>
        /// <param name="roleKeys">实例配置选择的 Role 键。</param>
        /// <returns>合并后的 Role Mask。</returns>
        public ServerRoleMask ResolveMask(IReadOnlyList<string> roleKeys)
        {
            Validate();
            if (roleKeys == null || roleKeys.Count == 0)
            {
                throw new InvalidOperationException("Dedicated Server 配置必须至少包含一个 Role。");
            }

            ulong value = 0UL;
            for (int keyIndex = 0; keyIndex < roleKeys.Count; keyIndex++)
            {
                string key = roleKeys[keyIndex];
                bool found = false;
                for (int roleIndex = 0; roleIndex < Roles.Length; roleIndex++)
                {
                    ServerRoleCatalogEntry entry = Roles[roleIndex];
                    if (!string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    value |= entry.Value;
                    found = true;
                    break;
                }

                if (!found)
                {
                    throw new InvalidOperationException($"Role Catalog 中不存在配置项：{key ?? "<null>"}。");
                }
            }

            return new ServerRoleMask(value);
        }

        /// <summary>
        /// 校验键和值唯一、位值为单个位并保留 Coordinator 固定值。
        /// </summary>
        public void Validate()
        {
            if (SchemaVersion != 1 || Roles == null || Roles.Length == 0)
            {
                throw new InvalidOperationException("ServerRoleCatalog 必须使用 schemaVersion=1 且至少包含 Coordinator。");
            }

            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var values = new HashSet<ulong>();
            bool hasCoordinator = false;
            for (int index = 0; index < Roles.Length; index++)
            {
                ServerRoleCatalogEntry entry = Roles[index];
                if (entry == null
                    || string.IsNullOrWhiteSpace(entry.Key)
                    || entry.Value == 0UL
                    || (entry.Value & (entry.Value - 1UL)) != 0UL
                    || !keys.Add(entry.Key)
                    || !values.Add(entry.Value))
                {
                    throw new InvalidOperationException("ServerRoleCatalog 包含空键、重复键、重复值或非单个位值。");
                }

                if (entry.Value == ServerRoleMask.CoordinatorValue)
                {
                    hasCoordinator = entry.FrameworkReserved && string.Equals(entry.Key, "Coordinator", StringComparison.Ordinal);
                }
                else if (entry.FrameworkReserved)
                {
                    throw new InvalidOperationException($"只有 Coordinator 可以标记 frameworkReserved：{entry.Key}。");
                }

                if (entry.ClientDiscoverable && string.IsNullOrWhiteSpace(entry.PublicName))
                {
                    throw new InvalidOperationException($"客户端可发现 Role 必须提供 publicName：{entry.Key}。");
                }
            }

            if (!hasCoordinator)
            {
                throw new InvalidOperationException("ServerRoleCatalog 必须保留 key=Coordinator、value=1 的框架目录项。");
            }
        }

        #endregion
    }
}
