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

        private const string SharedAssemblyName = "MiniCore.HotUpdate.Shared"; // 两侧共用业务热更新程序集名称。
        private const string SharedAssemblyPath = "Assets/Scripts/MiniCore/HotUpdate/MiniCore.HotUpdate.asmdef"; // 两侧共用业务 asmdef 路径。
        private const string ClientAssemblyName = "MiniCore.HotUpdate.Client"; // 客户端业务热更新程序集名称。
        private const string ClientAssemblyPath = "Assets/Scripts/MiniCore/HotUpdate/Client/MiniCore.HotUpdate.Client.asmdef"; // 客户端业务 asmdef 路径。
        private const string ServerAssemblyName = "MiniCore.HotUpdate.Server"; // 服务端业务热更新程序集名称。
        private const string ServerAssemblyPath = "Assets/Scripts/MiniCore/HotUpdate/Server/MiniCore.HotUpdate.Server.asmdef"; // 服务端业务 asmdef 路径。
        private const string DefaultStartupTypeName = "MiniCore.HotUpdate.MiniCoreStartup"; // 默认启动类型完整名称。
        private const string DefaultStartupMethodName = "StartAsync"; // 默认启动方法名称。
        private const string CommonProtocolAssemblyName = "MiniCore.Protocol.Common"; // 业务 Common 热更新程序集名称。
        private const string CommonProtocolAssemblyPath = "Assets/Scripts/MiniCore/Protocol/Generated/Common/MiniCore.Protocol.Common.asmdef"; // 业务 Common asmdef 路径。
        private const string OuterProtocolAssemblyName = "MiniCore.Protocol.Outer"; // 业务 Outer 热更新程序集名称。
        private const string OuterProtocolAssemblyPath = "Assets/Scripts/MiniCore/Protocol/Generated/Outer/MiniCore.Protocol.Outer.asmdef"; // 业务 Outer asmdef 路径。
        private const string InnerProtocolAssemblyName = "MiniCore.Protocol.Inner"; // 业务 Inner 热更新程序集名称。
        private const string InnerProtocolAssemblyPath = "Assets/Scripts/MiniCore/Protocol/Generated/Inner/MiniCore.Protocol.Inner.asmdef"; // 业务 Inner asmdef 路径。
        private const string ControlProtocolAssemblyName = "MiniCore.Protocol.Control"; // 固定 AOT 控制面程序集名称。
        private const string ControlInnerProtocolAssemblyName = "MiniCore.Protocol.Control.Inner"; // 固定 AOT 控制面 Inner 程序集名称。
        private static readonly string[] AotFrameworkAssemblyDefinitionPaths =
        {
            "Assets/Scripts/MiniCore/Runtime/MiniCore.Runtime.asmdef",
            "Assets/Scripts/MiniCore/Serialization/MiniCore.Serialization.asmdef",
            "Assets/Scripts/MiniCore/Network/MiniCore.Network.asmdef",
            "Assets/Scripts/MiniCore/Platform/Browser/MiniCore.Platform.Browser.asmdef",
            "Assets/Scripts/MiniCore/Protocol/Control/MiniCore.Protocol.Control.asmdef",
            "Assets/Scripts/MiniCore/Protocol/Control/Generated/Inner/MiniCore.Protocol.Control.Inner.asmdef",
            "Assets/Scripts/MiniCore/Unity/MiniCore.Unity.asmdef",
            "Assets/Scripts/MiniCore/Unity/YooAsset/MiniCore.Unity.YooAsset.asmdef",
            "Assets/Scripts/MiniCore/Server/MiniCore.Server.asmdef",
            "Assets/Scripts/Project/Bootstrap/Project.Bootstrap.asmdef"
        }; // 会进入 Player 的固定 AOT 框架程序集定义。

        [SerializeField] private List<MiniCoreHotUpdateAssemblyEntry> entries = new List<MiniCoreHotUpdateAssemblyEntry>(6); // 项目业务热更新程序集登记表。
        [SerializeField] private bool includeClientAssembliesInDedicatedServer; // DS 是否额外携带纯客户端热更新程序集。

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
        /// 按加载顺序取得指定运行目标真正需要携带的程序集。
        /// </summary>
        public MiniCoreHotUpdateAssemblyEntry[] GetEntriesInLoadOrder(HotUpdateAssemblyRuntimeTargets target)
        {
            bool includeClient = target == HotUpdateAssemblyRuntimeTargets.DedicatedServer
                && includeClientAssembliesInDedicatedServer;
            MiniCoreHotUpdateAssemblyEntry[] result = entries.FindAll(entry => entry != null
                && (entry.Supports(target)
                    || includeClient && entry.RuntimeTargets == HotUpdateAssemblyRuntimeTargets.Client)).ToArray();
            Array.Sort(result, CompareEntries);
            return result;
        }

        /// <summary>
        /// 获取当前 Unity 构建目标对应的热更新运行侧。
        /// </summary>
        public static HotUpdateAssemblyRuntimeTargets ActiveRuntimeTarget =>
            EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneOSX
            || EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows
            || EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64
            || EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneLinux64
                ? EditorUserBuildSettings.standaloneBuildSubtarget == StandaloneBuildSubtarget.Server
                    ? HotUpdateAssemblyRuntimeTargets.DedicatedServer
                    : HotUpdateAssemblyRuntimeTargets.Client
                : HotUpdateAssemblyRuntimeTargets.Client;

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
            int clientStartupCount = 0;
            int serverStartupCount = 0;
            bool containsShared = false;
            bool containsClient = false;
            bool containsServer = false;
            bool containsCommonProtocol = false;
            bool containsOuterProtocol = false;
            bool containsInnerProtocol = false;
            for (int index = 0; index < entries.Count; index++)
            {
                MiniCoreHotUpdateAssemblyEntry entry = entries[index];
                if (entry == null || string.IsNullOrWhiteSpace(entry.AssemblyName))
                {
                    error = $"第 {index + 1} 条热更新程序集名称为空。";
                    return false;
                }

                if (IsAotProtocolAssembly(entry.AssemblyName))
                {
                    error = $"协议程序集 {entry.AssemblyName} 属于 AOT 契约，禁止加入 HybridCLR 热更新清单。";
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

                if (entry.RuntimeTargets == HotUpdateAssemblyRuntimeTargets.None)
                {
                    error = $"热更新程序集 {entry.AssemblyName} 没有运行目标。";
                    return false;
                }

                if ((string.Equals(entry.AssemblyName, CommonProtocolAssemblyName, StringComparison.Ordinal)
                     || string.Equals(entry.AssemblyName, OuterProtocolAssemblyName, StringComparison.Ordinal))
                    && entry.RuntimeTargets != HotUpdateAssemblyRuntimeTargets.All)
                {
                    error = $"业务协议 {entry.AssemblyName} 必须同时提供给客户端和 Dedicated Server。";
                    return false;
                }

                if (string.Equals(entry.AssemblyName, InnerProtocolAssemblyName, StringComparison.Ordinal)
                    && entry.RuntimeTargets != HotUpdateAssemblyRuntimeTargets.DedicatedServer)
                {
                    error = "业务 Inner 协议只能登记到 Dedicated Server。";
                    return false;
                }

                containsShared |= string.Equals(entry.AssemblyName, SharedAssemblyName, StringComparison.Ordinal);
                containsClient |= string.Equals(entry.AssemblyName, ClientAssemblyName, StringComparison.Ordinal);
                containsServer |= string.Equals(entry.AssemblyName, ServerAssemblyName, StringComparison.Ordinal);
                containsCommonProtocol |= string.Equals(entry.AssemblyName, CommonProtocolAssemblyName, StringComparison.Ordinal);
                containsOuterProtocol |= string.Equals(entry.AssemblyName, OuterProtocolAssemblyName, StringComparison.Ordinal);
                containsInnerProtocol |= string.Equals(entry.AssemblyName, InnerProtocolAssemblyName, StringComparison.Ordinal);
                if (entry.IsStartup)
                {
                    if (string.IsNullOrWhiteSpace(entry.StartupTypeName)
                        || string.IsNullOrWhiteSpace(entry.StartupMethodName))
                    {
                        error = $"启动程序集 {entry.AssemblyName} 必须配置启动类型和静态方法。";
                        return false;
                    }

                    clientStartupCount += entry.IsStartupFor(HotUpdateAssemblyRuntimeTargets.Client) ? 1 : 0;
                    serverStartupCount += entry.IsStartupFor(HotUpdateAssemblyRuntimeTargets.DedicatedServer) ? 1 : 0;
                }
            }

            if (!containsShared
                || !containsClient
                || !containsServer
                || !containsCommonProtocol
                || !containsOuterProtocol
                || !containsInnerProtocol)
            {
                error = "业务 Common、Outer、Inner 与 Shared、Client、Server 六个基础热更新程序集都必须登记。";
                return false;
            }

            if (clientStartupCount != 1 || serverStartupCount != 1)
            {
                error = $"客户端和 Dedicated Server 必须各有且仅有一个启动入口，当前为 {clientStartupCount}/{serverStartupCount}。";
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

            if (!ValidateAotFrameworkDependencies(byName, out error))
            {
                return false;
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
        /// 确保六个框架基础热更新程序集使用当前架构规定的名称、路径和运行目标。
        /// 开发者额外登记的业务程序集会保留，旧的混合程序集登记会被移除。
        /// </summary>
        public void EnsureDefaultEntries()
        {
            MiniCoreHotUpdateAssemblyEntry[] requiredEntries = CreateRequiredEntries();
            if (!RequiresDefaultEntryRepair(requiredEntries))
            {
                return;
            }

            int existingCount = entries?.Count ?? 0;
            var repairedEntries = new List<MiniCoreHotUpdateAssemblyEntry>(existingCount + requiredEntries.Length);
            repairedEntries.AddRange(requiredEntries);
            if (entries != null)
            {
                for (int index = 0; index < entries.Count; index++)
                {
                    MiniCoreHotUpdateAssemblyEntry entry = entries[index];
                    if (entry != null && !IsFrameworkManagedAssemblyName(entry.AssemblyName))
                    {
                        repairedEntries.Add(entry);
                    }
                }
            }

            entries = repairedEntries;
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
        /// 创建当前架构要求的六个框架基础热更新程序集条目。
        /// </summary>
        /// <returns>按稳定加载顺序排列的基础条目。</returns>
        private static MiniCoreHotUpdateAssemblyEntry[] CreateRequiredEntries()
        {
            return new[]
            {
                new MiniCoreHotUpdateAssemblyEntry(CommonProtocolAssemblyName, CommonProtocolAssemblyPath, 100),
                new MiniCoreHotUpdateAssemblyEntry(OuterProtocolAssemblyName, OuterProtocolAssemblyPath, 200),
                new MiniCoreHotUpdateAssemblyEntry(
                    InnerProtocolAssemblyName,
                    InnerProtocolAssemblyPath,
                    300,
                    false,
                    null,
                    null,
                    HotUpdateAssemblyRuntimeTargets.DedicatedServer),
                new MiniCoreHotUpdateAssemblyEntry(SharedAssemblyName, SharedAssemblyPath, 500),
                new MiniCoreHotUpdateAssemblyEntry(
                    ClientAssemblyName,
                    ClientAssemblyPath,
                    1000,
                    true,
                    DefaultStartupTypeName,
                    DefaultStartupMethodName,
                    HotUpdateAssemblyRuntimeTargets.Client,
                    HotUpdateAssemblyRuntimeTargets.Client),
                new MiniCoreHotUpdateAssemblyEntry(
                    ServerAssemblyName,
                    ServerAssemblyPath,
                    1100,
                    true,
                    "MiniCore.HotUpdate.Server.MiniCoreServerStartup",
                    DefaultStartupMethodName,
                    HotUpdateAssemblyRuntimeTargets.DedicatedServer,
                    HotUpdateAssemblyRuntimeTargets.DedicatedServer)
            };
        }

        /// <summary>
        /// 判断当前序列化清单是否需要恢复框架管理的基础条目。
        /// </summary>
        /// <param name="requiredEntries">当前架构要求的基础条目。</param>
        /// <returns>存在缺失、重复、旧路径或旧混合程序集条目时返回 true。</returns>
        private bool RequiresDefaultEntryRepair(IReadOnlyList<MiniCoreHotUpdateAssemblyEntry> requiredEntries)
        {
            if (entries == null)
            {
                return true;
            }

            for (int requiredIndex = 0; requiredIndex < requiredEntries.Count; requiredIndex++)
            {
                MiniCoreHotUpdateAssemblyEntry required = requiredEntries[requiredIndex];
                int matchCount = 0;
                for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                {
                    MiniCoreHotUpdateAssemblyEntry current = entries[entryIndex];
                    if (current != null && string.Equals(current.AssemblyName, required.AssemblyName, StringComparison.Ordinal))
                    {
                        matchCount++;
                        if (!HasSameFrameworkConfiguration(current, required))
                        {
                            return true;
                        }
                    }
                }

                if (matchCount != 1)
                {
                    return true;
                }
            }

            for (int index = 0; index < entries.Count; index++)
            {
                MiniCoreHotUpdateAssemblyEntry entry = entries[index];
                if (entry != null && IsObsoleteMixedAssemblyName(entry.AssemblyName))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 比较现有基础条目与当前架构要求是否完全一致。
        /// </summary>
        /// <param name="current">当前序列化条目。</param>
        /// <param name="required">当前架构要求的条目。</param>
        /// <returns>路径、顺序、运行目标和启动入口都一致时返回 true。</returns>
        private static bool HasSameFrameworkConfiguration(
            MiniCoreHotUpdateAssemblyEntry current,
            MiniCoreHotUpdateAssemblyEntry required)
        {
            return string.Equals(
                       NormalizeAssetPath(current.AssemblyDefinitionPath),
                       NormalizeAssetPath(required.AssemblyDefinitionPath),
                       StringComparison.Ordinal)
                && current.LoadOrder == required.LoadOrder
                && current.RuntimeTargets == required.RuntimeTargets
                && current.IsStartup == required.IsStartup
                && string.Equals(current.StartupTypeName, required.StartupTypeName, StringComparison.Ordinal)
                && string.Equals(current.StartupMethodName, required.StartupMethodName, StringComparison.Ordinal)
                && current.IsStartupFor(HotUpdateAssemblyRuntimeTargets.Client)
                    == required.IsStartupFor(HotUpdateAssemblyRuntimeTargets.Client)
                && current.IsStartupFor(HotUpdateAssemblyRuntimeTargets.DedicatedServer)
                    == required.IsStartupFor(HotUpdateAssemblyRuntimeTargets.DedicatedServer);
        }

        /// <summary>
        /// 判断程序集名称是否由框架基础清单统一管理。
        /// </summary>
        /// <param name="assemblyName">待检查的程序集名称。</param>
        /// <returns>属于当前六个基础程序集或旧混合程序集时返回 true。</returns>
        private static bool IsFrameworkManagedAssemblyName(string assemblyName)
        {
            return string.Equals(assemblyName, CommonProtocolAssemblyName, StringComparison.Ordinal)
                || string.Equals(assemblyName, OuterProtocolAssemblyName, StringComparison.Ordinal)
                || string.Equals(assemblyName, InnerProtocolAssemblyName, StringComparison.Ordinal)
                || string.Equals(assemblyName, SharedAssemblyName, StringComparison.Ordinal)
                || string.Equals(assemblyName, ClientAssemblyName, StringComparison.Ordinal)
                || string.Equals(assemblyName, ServerAssemblyName, StringComparison.Ordinal)
                || IsObsoleteMixedAssemblyName(assemblyName);
        }

        /// <summary>
        /// 判断程序集名称是否属于拆分前已废弃的混合程序集。
        /// </summary>
        /// <param name="assemblyName">待检查的程序集名称。</param>
        /// <returns>属于旧 Protocol 或 HotUpdate 混合程序集时返回 true。</returns>
        private static bool IsObsoleteMixedAssemblyName(string assemblyName)
        {
            return string.Equals(assemblyName, "MiniCore.Protocol", StringComparison.Ordinal)
                || string.Equals(assemblyName, "MiniCore.HotUpdate", StringComparison.Ordinal);
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
        /// 判断程序集是否为固定 AOT 控制面协议契约。
        /// </summary>
        /// <param name="assemblyName">待检查的程序集名称。</param>
        /// <returns>属于 Control 或 Control.Inner 时返回 true。</returns>
        private static bool IsAotProtocolAssembly(string assemblyName)
        {
            return string.Equals(assemblyName, ControlProtocolAssemblyName, StringComparison.Ordinal)
                || string.Equals(assemblyName, ControlInnerProtocolAssemblyName, StringComparison.Ordinal);
        }

        /// <summary>
        /// 校验固定 AOT 框架程序集没有反向引用任何已登记热更新程序集。
        /// </summary>
        /// <param name="hotUpdateEntries">按程序集名称索引的热更新登记。</param>
        /// <param name="error">失败时返回非法依赖说明。</param>
        /// <returns>全部固定 AOT 框架程序集依赖合法时返回 true。</returns>
        private static bool ValidateAotFrameworkDependencies(
            IReadOnlyDictionary<string, MiniCoreHotUpdateAssemblyEntry> hotUpdateEntries,
            out string error)
        {
            for (int pathIndex = 0; pathIndex < AotFrameworkAssemblyDefinitionPaths.Length; pathIndex++)
            {
                string path = AotFrameworkAssemblyDefinitionPaths[pathIndex];
                if (!TryReadAssemblyDefinition(path, out AssemblyDefinitionData definition, out error))
                {
                    return false;
                }

                string[] references = definition.references ?? Array.Empty<string>();
                for (int referenceIndex = 0; referenceIndex < references.Length; referenceIndex++)
                {
                    string referencedName = ResolveAssemblyReferenceName(references[referenceIndex]);
                    if (referencedName != null && hotUpdateEntries.ContainsKey(referencedName))
                    {
                        error = $"固定 AOT 程序集 {definition.name} 禁止引用热更新程序集 {referencedName}：{path}";
                        return false;
                    }
                }
            }

            error = null;
            return true;
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
