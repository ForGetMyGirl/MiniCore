using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 保存项目级热更新程序集清单，并作为 HybridCLR、YooAsset 与 Bootstrap 的唯一登记来源。
    /// </summary>
    [FilePath("ProjectSettings/MiniCoreHotUpdateAssemblySettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class MiniCoreHotUpdateAssemblySettings : ScriptableSingleton<MiniCoreHotUpdateAssemblySettings>
    {
        #region Private 私有成员

        private const string ProtocolAssemblyName = "MiniCore.Protocol"; // 框架协议热更新程序集名称。
        private const string ProtocolAssemblyPath = "Assets/Scripts/MiniCore/Protocol/MiniCore.Protocol.asmdef"; // 协议 asmdef 路径。
        private const string StartupAssemblyName = "MiniCore.HotUpdate"; // 默认启动程序集名称。
        private const string StartupAssemblyPath = "Assets/Scripts/MiniCore/HotUpdate/MiniCore.HotUpdate.asmdef"; // 默认启动 asmdef 路径。
        private const string DefaultStartupTypeName = "MiniCore.HotUpdate.MiniCoreStartup"; // 默认启动类型完整名称。
        private const string DefaultStartupMethodName = "StartAsync"; // 默认启动方法名称。

        [SerializeField] private List<MiniCoreHotUpdateAssemblyEntry> entries = new List<MiniCoreHotUpdateAssemblyEntry>(4); // 项目热更新程序集登记表。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取当前项目设置实例。
        /// </summary>
        public static MiniCoreHotUpdateAssemblySettings Current => instance;

        /// <summary>
        /// 获取当前登记顺序下的只读热更新程序集列表。
        /// </summary>
        public IReadOnlyList<MiniCoreHotUpdateAssemblyEntry> Entries => entries;

        /// <summary>
        /// 按 LoadOrder 与程序集名称生成稳定的依赖优先快照。
        /// </summary>
        /// <returns>调用方可独立使用的有序登记数组。</returns>
        public MiniCoreHotUpdateAssemblyEntry[] GetEntriesInLoadOrder()
        {
            MiniCoreHotUpdateAssemblyEntry[] result = entries.ToArray();
            Array.Sort(result, CompareEntries);
            return result;
        }

        /// <summary>
        /// 登记一个新的热更新程序集并立即持久化项目设置。
        /// </summary>
        /// <param name="entry">已经创建 asmdef 的程序集记录。</param>
        public void Register(MiniCoreHotUpdateAssemblyEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            for (int index = 0; index < entries.Count; index++)
            {
                MiniCoreHotUpdateAssemblyEntry current = entries[index];
                if (string.Equals(current.AssemblyName, entry.AssemblyName, StringComparison.Ordinal)
                    || string.Equals(
                        NormalizeAssetPath(current.AssemblyDefinitionPath),
                        NormalizeAssetPath(entry.AssemblyDefinitionPath),
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"热更新程序集已经登记：{entry.AssemblyName}");
                }
            }

            entries.Add(entry);
            Save(true);
        }

        /// <summary>
        /// 保存由 Project Settings 界面修改的程序集清单。
        /// </summary>
        public void SaveSettings()
        {
            Save(true);
        }

        /// <summary>
        /// 校验程序集名称、asmdef 路径、加载顺序、依赖顺序和唯一启动入口。
        /// </summary>
        /// <param name="error">失败时返回可直接展示的原因。</param>
        /// <returns>当前登记可安全用于构建与启动时返回 true。</returns>
        public bool TryValidate(out string error)
        {
            if (entries == null || entries.Count == 0)
            {
                error = "尚未登记任何热更新程序集。";
                return false;
            }

            var byName = new Dictionary<string, MiniCoreHotUpdateAssemblyEntry>(entries.Count, StringComparer.Ordinal);
            var byPath = new HashSet<string>(StringComparer.Ordinal);
            var loadOrders = new HashSet<int>();
            int startupCount = 0;
            bool containsProtocol = false;
            bool containsDefaultStartup = false;
            for (int index = 0; index < entries.Count; index++)
            {
                MiniCoreHotUpdateAssemblyEntry entry = entries[index];
                if (entry == null || string.IsNullOrWhiteSpace(entry.AssemblyName))
                {
                    error = $"第 {index + 1} 条热更新程序集名称为空。";
                    return false;
                }

                string assemblyPath = NormalizeAssetPath(entry.AssemblyDefinitionPath);
                if (!IsAssetsAssemblyDefinitionPath(assemblyPath) || !File.Exists(assemblyPath))
                {
                    error = $"热更新程序集 {entry.AssemblyName} 的 asmdef 路径无效：{entry.AssemblyDefinitionPath}";
                    return false;
                }

                if (byName.ContainsKey(entry.AssemblyName))
                {
                    error = $"热更新程序集名称重复：{entry.AssemblyName}";
                    return false;
                }

                byName.Add(entry.AssemblyName, entry);

                if (!byPath.Add(assemblyPath))
                {
                    error = $"多个登记指向同一个 asmdef：{assemblyPath}";
                    return false;
                }

                if (!loadOrders.Add(entry.LoadOrder))
                {
                    error = $"热更新程序集 LoadOrder 重复：{entry.LoadOrder}";
                    return false;
                }

                if (!TryReadAssemblyDefinition(assemblyPath, out AssemblyDefinitionData definition, out error))
                {
                    return false;
                }

                if (!string.Equals(definition.name, entry.AssemblyName, StringComparison.Ordinal))
                {
                    error = $"登记名称 {entry.AssemblyName} 与 asmdef 名称 {definition.name} 不一致：{assemblyPath}";
                    return false;
                }

                containsProtocol |= string.Equals(entry.AssemblyName, ProtocolAssemblyName, StringComparison.Ordinal);
                containsDefaultStartup |= string.Equals(entry.AssemblyName, StartupAssemblyName, StringComparison.Ordinal);
                if (entry.IsStartup)
                {
                    startupCount++;
                    if (string.IsNullOrWhiteSpace(entry.StartupTypeName)
                        || string.IsNullOrWhiteSpace(entry.StartupMethodName))
                    {
                        error = $"启动程序集 {entry.AssemblyName} 必须配置启动类型和静态方法。";
                        return false;
                    }
                }
            }

            if (!containsProtocol || !containsDefaultStartup)
            {
                error = "MiniCore.Protocol 与 MiniCore.HotUpdate 都必须登记为热更新程序集。";
                return false;
            }

            if (startupCount != 1)
            {
                error = $"热更新程序集必须且只能配置一个启动入口，当前数量：{startupCount}。";
                return false;
            }

            MiniCoreHotUpdateAssemblyEntry[] ordered = GetEntriesInLoadOrder();
            for (int index = 0; index < ordered.Length; index++)
            {
                MiniCoreHotUpdateAssemblyEntry entry = ordered[index];
                TryReadAssemblyDefinition(
                    NormalizeAssetPath(entry.AssemblyDefinitionPath),
                    out AssemblyDefinitionData definition,
                    out _);
                string[] references = definition.references ?? Array.Empty<string>();
                for (int referenceIndex = 0; referenceIndex < references.Length; referenceIndex++)
                {
                    string referencedName = ResolveAssemblyReferenceName(references[referenceIndex]);
                    if (referencedName != null
                        && byName.TryGetValue(referencedName, out MiniCoreHotUpdateAssemblyEntry dependency)
                        && dependency.LoadOrder >= entry.LoadOrder)
                    {
                        error = $"热更新程序集 {entry.AssemblyName} 依赖 {dependency.AssemblyName}，但依赖未排在其之前。";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        /// <summary>
        /// 查找输出目录继承的最近 asmdef，并判断该程序集是否已登记为热更新程序集。
        /// </summary>
        /// <param name="assetsOutputDirectory">Assets 下的代码输出目录。</param>
        /// <param name="entry">成功时返回匹配的热更新程序集记录。</param>
        /// <param name="nearestAssemblyDefinitionPath">找到的最近 asmdef 路径；没有 asmdef 时为空。</param>
        /// <returns>最近 asmdef 已登记时返回 true。</returns>
        public static bool TryGetRegisteredAssemblyForOutputDirectory(
            string assetsOutputDirectory,
            out MiniCoreHotUpdateAssemblyEntry entry,
            out string nearestAssemblyDefinitionPath)
        {
            entry = null;
            nearestAssemblyDefinitionPath = FindNearestAssemblyDefinitionPath(assetsOutputDirectory);
            if (nearestAssemblyDefinitionPath == null)
            {
                return false;
            }

            IReadOnlyList<MiniCoreHotUpdateAssemblyEntry> currentEntries = Current.Entries;
            for (int index = 0; index < currentEntries.Count; index++)
            {
                MiniCoreHotUpdateAssemblyEntry candidate = currentEntries[index];
                if (candidate != null
                    && string.Equals(
                        NormalizeAssetPath(candidate.AssemblyDefinitionPath),
                        nearestAssemblyDefinitionPath,
                        StringComparison.Ordinal))
                {
                    entry = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断 Assets 输出目录归属的最近 asmdef 是否已登记，并返回程序集名称。
        /// </summary>
        /// <param name="assetsOutputDirectory">Assets 下的代码输出目录。</param>
        /// <param name="assemblyName">成功时返回登记的程序集名称。</param>
        /// <returns>输出目录归属已登记热更新程序集时返回 true。</returns>
        public static bool IsOutputDirectoryRegistered(string assetsOutputDirectory, out string assemblyName)
        {
            if (TryGetRegisteredAssemblyForOutputDirectory(
                    assetsOutputDirectory,
                    out MiniCoreHotUpdateAssemblyEntry entry,
                    out _))
            {
                assemblyName = entry.AssemblyName;
                return true;
            }

            assemblyName = null;
            return false;
        }

        /// <summary>
        /// 判断资源路径是否位于任一已登记热更新程序集的目录下。
        /// 该方法只比较登记的 asmdef 目录，可用于删除或移出后已无法查询最近 asmdef 的旧路径。
        /// </summary>
        /// <param name="assetPath">待判断的 Assets 相对路径。</param>
        /// <returns>路径位于已登记程序集目录或就是登记 asmdef 时返回 true。</returns>
        public static bool IsPathUnderRegisteredAssembly(string assetPath)
        {
            string normalizedPath = NormalizeAssetPath(assetPath);
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return false;
            }

            IReadOnlyList<MiniCoreHotUpdateAssemblyEntry> currentEntries = Current.Entries;
            for (int index = 0; index < currentEntries.Count; index++)
            {
                MiniCoreHotUpdateAssemblyEntry entry = currentEntries[index];
                if (entry == null)
                {
                    continue;
                }

                string assemblyDefinitionPath = NormalizeAssetPath(entry.AssemblyDefinitionPath);
                string assemblyDirectory = NormalizeAssetPath(Path.GetDirectoryName(assemblyDefinitionPath));
                if (string.IsNullOrEmpty(assemblyDirectory))
                {
                    continue;
                }

                if (string.Equals(normalizedPath, assemblyDefinitionPath, StringComparison.Ordinal)
                    || string.Equals(normalizedPath, assemblyDirectory, StringComparison.Ordinal)
                    || normalizedPath.StartsWith(assemblyDirectory + "/", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 首次创建项目设置时登记协议程序集和默认启动程序集。
        /// </summary>
        public void EnsureDefaultEntries()
        {
            if (entries != null && entries.Count > 0)
            {
                return;
            }

            entries = new List<MiniCoreHotUpdateAssemblyEntry>(2)
            {
                new MiniCoreHotUpdateAssemblyEntry(ProtocolAssemblyName, ProtocolAssemblyPath, 100),
                new MiniCoreHotUpdateAssemblyEntry(
                    StartupAssemblyName,
                    StartupAssemblyPath,
                    1000,
                    true,
                    DefaultStartupTypeName,
                    DefaultStartupMethodName)
            };
            Save(true);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 编辑器加载时确保项目至少拥有框架默认热更新程序集清单。
        /// </summary>
        [InitializeOnLoadMethod]
        private static void InitializeProjectSettings()
        {
            Current.EnsureDefaultEntries();
        }

        /// <summary>
        /// 按加载顺序和程序集名称比较两条登记记录。
        /// </summary>
        /// <param name="left">左侧登记。</param>
        /// <param name="right">右侧登记。</param>
        /// <returns>适用于稳定排序的比较结果。</returns>
        private static int CompareEntries(
            MiniCoreHotUpdateAssemblyEntry left,
            MiniCoreHotUpdateAssemblyEntry right)
        {
            int orderComparison = left.LoadOrder.CompareTo(right.LoadOrder);
            return orderComparison != 0
                ? orderComparison
                : string.Compare(left.AssemblyName, right.AssemblyName, StringComparison.Ordinal);
        }

        /// <summary>
        /// 查找目录向上继承的最近 asmdef。
        /// </summary>
        /// <param name="assetsOutputDirectory">Assets 下的输出目录。</param>
        /// <returns>最近 asmdef 路径；不存在或目录无效时返回空。</returns>
        private static string FindNearestAssemblyDefinitionPath(string assetsOutputDirectory)
        {
            string current = NormalizeAssetPath(assetsOutputDirectory);
            if (string.IsNullOrWhiteSpace(current)
                || (!string.Equals(current, "Assets", StringComparison.Ordinal)
                    && !current.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                return null;
            }

            string extension = Path.GetExtension(current);
            if ((string.Equals(extension, ".cs", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(extension, ".asmdef", StringComparison.OrdinalIgnoreCase))
                && !Directory.Exists(current))
            {
                current = NormalizeAssetPath(Path.GetDirectoryName(current));
            }

            while (!string.IsNullOrEmpty(current))
            {
                if (Directory.Exists(current))
                {
                    string[] definitions = Directory.GetFiles(current, "*.asmdef", SearchOption.TopDirectoryOnly);
                    if (definitions.Length > 0)
                    {
                        Array.Sort(definitions, StringComparer.Ordinal);
                        return NormalizeAssetPath(definitions[0]);
                    }
                }

                if (string.Equals(current, "Assets", StringComparison.Ordinal))
                {
                    break;
                }

                current = NormalizeAssetPath(Path.GetDirectoryName(current));
            }

            return null;
        }

        /// <summary>
        /// 读取 asmdef 的名称和引用字段。
        /// </summary>
        /// <param name="assemblyPath">asmdef 项目相对路径。</param>
        /// <param name="definition">成功时返回最小解析模型。</param>
        /// <param name="error">失败时返回错误原因。</param>
        /// <returns>文件可读取且包含程序集名称时返回 true。</returns>
        private static bool TryReadAssemblyDefinition(
            string assemblyPath,
            out AssemblyDefinitionData definition,
            out string error)
        {
            try
            {
                definition = JsonUtility.FromJson<AssemblyDefinitionData>(File.ReadAllText(assemblyPath));
            }
            catch (Exception exception)
            {
                definition = null;
                error = $"无法读取 asmdef：{assemblyPath}。{exception.Message}";
                return false;
            }

            if (definition == null || string.IsNullOrWhiteSpace(definition.name))
            {
                error = $"asmdef 缺少程序集名称：{assemblyPath}";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// 将 asmdef 引用解析为程序集名称。
        /// </summary>
        /// <param name="reference">名称或 GUID 形式的 asmdef 引用。</param>
        /// <returns>可识别的程序集名称；无法解析时返回空。</returns>
        private static string ResolveAssemblyReferenceName(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                return null;
            }

            if (!reference.StartsWith("GUID:", StringComparison.Ordinal))
            {
                return reference;
            }

            string path = AssetDatabase.GUIDToAssetPath(reference.Substring("GUID:".Length));
            return TryReadAssemblyDefinition(path, out AssemblyDefinitionData definition, out _)
                ? definition.name
                : null;
        }

        /// <summary>
        /// 判断路径是否为 Assets 下的 asmdef。
        /// </summary>
        /// <param name="path">规范化后的项目路径。</param>
        /// <returns>路径位于 Assets 且扩展名正确时返回 true。</returns>
        private static bool IsAssetsAssemblyDefinitionPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && path.StartsWith("Assets/", StringComparison.Ordinal)
                && string.Equals(Path.GetExtension(path), ".asmdef", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 将项目路径统一为正斜杠并移除尾部分隔符。
        /// </summary>
        /// <param name="path">待规范化路径。</param>
        /// <returns>规范化后的项目相对路径。</returns>
        private static string NormalizeAssetPath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? null
                : path.Replace('\\', '/').TrimEnd('/');
        }

        /// <summary>
        /// asmdef 校验所需的最小 JSON 模型。
        /// </summary>
        [Serializable]
        private sealed class AssemblyDefinitionData
        {
            [SerializeField] internal string name; // 程序集名称。
            [SerializeField] internal string[] references; // 直接程序集引用。
        }

        #endregion
    }
}
