using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MiniCore.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Callbacks;

namespace MiniCore.EditorTools.Deploy
{
    /// <summary>
    /// 从业务枚举字段生成不可变 Role Catalog 和客户端公开服务常量。
    /// </summary>
    internal static class ServerRoleCatalogGenerator
    {
        #region Private 私有成员

        private const string CatalogPath = "Server/DedicatedServer/Config/ServerRoleCatalog.json"; // Role Catalog 输出。
        private const string ClientOutputPath = "Assets/Scripts/MiniCore/HotUpdate/Client/Generated/PublicServiceIds.Generated.cs"; // 客户端公开常量输出。
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false); // 固定无 BOM 编码。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 扫描业务 Role 元数据，校验稳定值并同步 JSON 与客户端常量。
        /// </summary>
        internal static void Generate()
        {
            List<RoleDefinition> roles = DiscoverRoles();
            WriteIfChanged(CatalogPath, BuildCatalog(roles));
            WriteIfChanged(ClientOutputPath, BuildClientConstants(roles));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        /// <summary>
        /// 业务脚本重新编译后自动同步 Role Catalog，避免桌面发布工具读取到过期目录。
        /// </summary>
        [DidReloadScripts]
        private static void SynchronizeAfterScriptReload()
        {
            EditorApplication.delayCall -= Generate;
            EditorApplication.delayCall += Generate;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 发现所有带 ServerRoleDefinitionAttribute 的业务枚举字段。
        /// </summary>
        /// <returns>按位值排序的 Role 定义。</returns>
        private static List<RoleDefinition> DiscoverRoles()
        {
            var result = new List<RoleDefinition>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int assemblyIndex = 0; assemblyIndex < assemblies.Length; assemblyIndex++)
            {
                Type[] types;
                try
                {
                    types = assemblies[assemblyIndex].GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    types = exception.Types.Where(type => type != null).ToArray();
                }

                for (int typeIndex = 0; typeIndex < types.Length; typeIndex++)
                {
                    Type type = types[typeIndex];
                    if (type == null || !type.IsEnum)
                    {
                        continue;
                    }

                    FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
                    for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
                    {
                        FieldInfo field = fields[fieldIndex];
                        ServerRoleDefinitionAttribute attribute = field.GetCustomAttribute<ServerRoleDefinitionAttribute>();
                        if (attribute == null)
                        {
                            continue;
                        }

                        ulong value = Convert.ToUInt64(field.GetRawConstantValue(), System.Globalization.CultureInfo.InvariantCulture);
                        result.Add(new RoleDefinition(attribute.Key, attribute.DisplayName, value, attribute.ClientDiscoverable, attribute.PublicName));
                    }
                }
            }

            result.Sort((left, right) => left.Value.CompareTo(right.Value));
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var values = new HashSet<ulong>();
            var publicNames = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < result.Count; index++)
            {
                RoleDefinition role = result[index];
                if (role.Value <= ServerRoleMask.CoordinatorValue
                    || role.Value == FrameworkServiceIds.Database
                    || (role.Value & (role.Value - 1UL)) != 0UL
                    || !keys.Add(role.Key)
                    || !values.Add(role.Value))
                {
                    throw new InvalidOperationException($"业务 Role 必须使用唯一、非保留的单个位值：{role.Key}={role.Value}。");
                }

                if (role.ClientDiscoverable && (string.IsNullOrWhiteSpace(role.PublicName) || !publicNames.Add(role.PublicName)))
                {
                    throw new InvalidOperationException($"客户端公开 Role 必须提供唯一 publicName：{role.Key}。");
                }
            }

            return result;
        }

        /// <summary>
        /// 生成包含框架 Coordinator 和所有业务 Role 的 JSON。
        /// </summary>
        /// <param name="roles">业务 Role。</param>
        /// <returns>格式化 JSON。</returns>
        private static string BuildCatalog(IReadOnlyList<RoleDefinition> roles)
        {
            var entries = new JArray
            {
                new JObject
                {
                    ["key"] = "Coordinator",
                    ["value"] = ServerRoleMask.CoordinatorValue,
                    ["displayName"] = "Coordinator",
                    ["frameworkReserved"] = true,
                    ["clientDiscoverable"] = false,
                    ["publicName"] = string.Empty
                }
            };
            for (int index = 0; index < roles.Count; index++)
            {
                RoleDefinition role = roles[index];
                entries.Add(new JObject
                {
                    ["key"] = role.Key,
                    ["value"] = role.Value,
                    ["displayName"] = role.DisplayName,
                    ["frameworkReserved"] = false,
                    ["clientDiscoverable"] = role.ClientDiscoverable,
                    ["publicName"] = role.PublicName
                });
            }

            return new JObject { ["schemaVersion"] = 1, ["roles"] = entries }.ToString(Formatting.Indented) + Environment.NewLine;
        }

        /// <summary>
        /// 生成仅包含 clientDiscoverable Role 的客户端常量类。
        /// </summary>
        /// <param name="roles">业务 Role。</param>
        /// <returns>C# 源码。</returns>
        private static string BuildClientConstants(IReadOnlyList<RoleDefinition> roles)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// Auto-generated by ServerRoleCatalogGenerator. Do not modify by hand.");
            builder.AppendLine("namespace MiniCore.HotUpdate");
            builder.AppendLine("{");
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 仅向客户端公开允许发现的业务服务标识。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    public static class PublicServiceIds");
            builder.AppendLine("    {");
            for (int index = 0; index < roles.Count; index++)
            {
                RoleDefinition role = roles[index];
                if (!role.ClientDiscoverable)
                {
                    continue;
                }

                builder.AppendLine("        /// <summary>");
                builder.AppendLine($"        /// {role.DisplayName} 服务标识。");
                builder.AppendLine("        /// </summary>");
                builder.AppendLine($"        public const ulong {role.PublicName} = {role.Value}UL;");
            }

            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        /// <summary>
        /// 内容变化时以 UTF-8 无 BOM 写入文件。
        /// </summary>
        /// <param name="relativePath">项目相对路径。</param>
        /// <param name="content">目标内容。</param>
        private static void WriteIfChanged(string relativePath, string content)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relativePath));
            string directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(fullPath) || !string.Equals(File.ReadAllText(fullPath), content, StringComparison.Ordinal))
            {
                File.WriteAllText(fullPath, content, Utf8WithoutBom);
            }
        }

        /// <summary>
        /// 保存生成器内部使用的不可变 Role 数据。
        /// </summary>
        private sealed class RoleDefinition
        {
            /// <summary>
            /// 创建生成器 Role 数据。
            /// </summary>
            /// <param name="key">稳定键。</param>
            /// <param name="displayName">显示名称。</param>
            /// <param name="value">位值。</param>
            /// <param name="clientDiscoverable">是否公开给客户端。</param>
            /// <param name="publicName">客户端常量名。</param>
            public RoleDefinition(string key, string displayName, ulong value, bool clientDiscoverable, string publicName)
            {
                Key = key;
                DisplayName = displayName;
                Value = value;
                ClientDiscoverable = clientDiscoverable;
                PublicName = publicName;
            }

            public string Key { get; }
            public string DisplayName { get; }
            public ulong Value { get; }
            public bool ClientDiscoverable { get; }
            public string PublicName { get; }
        }

        #endregion
    }
}
