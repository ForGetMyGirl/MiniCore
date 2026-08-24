using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 只向 Dedicated Server Player 注入与实例无关的 Role Catalog，并阻止配置泄漏到客户端。
    /// </summary>
    public sealed class DedicatedServerConfigBuildProcessor : BuildPlayerProcessor, IPreprocessBuildWithReport
    {
        #region Private 私有成员

        private const string SourceRoleCatalogPath = "Server/DedicatedServer/Config/ServerRoleCatalog.json"; // 项目业务 Role 目录。
        private const string RuntimeConfigFileName = "MiniCoreServerRuntime.json"; // Player 中固定文件名。
        private const string HotUpdateAssetDirectory = "Assets/AssetRes/Dlls/HotUpdate"; // 当前目标热更新 DLL 资源目录。
        private const string ClientAssemblyDefinitionPath = "Assets/Scripts/MiniCore/HotUpdate/Client/MiniCore.HotUpdate.Client.asmdef"; // 客户端业务程序集定义。
        private const string ServerAssemblyDefinitionPath = "Assets/Scripts/MiniCore/HotUpdate/Server/MiniCore.HotUpdate.Server.asmdef"; // 服务端业务程序集定义。
        private const string InnerAssemblyDefinitionPath = "Assets/Scripts/MiniCore/Protocol/Generated/Inner/MiniCore.Protocol.Inner.asmdef"; // Inner 协议程序集定义。
        private const string ControlInnerAssemblyDefinitionPath = "Assets/Scripts/MiniCore/Protocol/Control/Generated/Inner/MiniCore.Protocol.Control.Inner.asmdef"; // 固定控制面 Inner 程序集定义。
        private const string ClientConstraint = "UNITY_EDITOR || !UNITY_SERVER"; // Editor 与非 Server Player 编译客户端程序集。
        private const string ServerConstraint = "UNITY_EDITOR || UNITY_SERVER"; // Editor 与 Server Player 编译服务端程序集。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取客户端泄漏校验执行顺序。
        /// </summary>
        public override int callbackOrder => -1000;

        /// <summary>
        /// Dedicated Server 构建时只向 StreamingAssets 注入不可变 Role Catalog。
        /// </summary>
        /// <param name="buildPlayerContext">当前 Player 构建上下文。</param>
        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            if (!IsDedicatedServerBuild())
            {
                return;
            }

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new BuildFailedException("无法确定 Unity 项目根目录。");
            string fullPath = Path.GetFullPath(Path.Combine(projectRoot, SourceRoleCatalogPath));
            if (!File.Exists(fullPath))
            {
                throw new BuildFailedException($"缺少 Dedicated Server Role Catalog：{SourceRoleCatalogPath}");
            }

            buildPlayerContext.AddAdditionalPathToStreamingAssets(fullPath);
        }

        /// <summary>
        /// 普通客户端构建前阻止 Assets/StreamingAssets 中出现服务端配置。
        /// </summary>
        /// <param name="report">当前构建报告。</param>
        public void OnPreprocessBuild(BuildReport report)
        {
            ValidateAssemblyConstraints();
            if (HybridClrBuildValidator.IsGeneratingArtifacts)
            {
                return;
            }

            ValidateAotControlHotUpdateAssets();
            if (IsDedicatedServerBuild())
            {
                return;
            }

            string leakedPath = Path.Combine("Assets", "StreamingAssets", RuntimeConfigFileName);
            if (File.Exists(leakedPath))
            {
                throw new BuildFailedException($"客户端构建禁止包含 Dedicated Server 配置：{leakedPath}");
            }

            ValidateClientAssemblySelection();
            ValidateClientHotUpdateAssets();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 判断当前编辑器构建设置是否选择 Standalone Dedicated Server 子目标。
        /// </summary>
        /// <returns>当前为 DS 子目标时返回 true。</returns>
        private static bool IsDedicatedServerBuild()
        {
            return EditorUserBuildSettings.standaloneBuildSubtarget == StandaloneBuildSubtarget.Server;
        }

        /// <summary>
        /// 确认客户端业务热更新清单中不存在 Server 程序集。
        /// </summary>
        private static void ValidateClientAssemblySelection()
        {
            MiniCoreHotUpdateAssemblyEntry[] entries = MiniCoreHotUpdateAssemblySettings.Current.GetEntriesInLoadOrder(HotUpdateAssemblyRuntimeTargets.Client);
            for (int index = 0; index < entries.Length; index++)
            {
                string name = entries[index].AssemblyName;
                if (string.Equals(name, "MiniCore.HotUpdate.Server", StringComparison.Ordinal)
                    || string.Equals(name, "MiniCore.Protocol.Inner", StringComparison.Ordinal))
                {
                    throw new BuildFailedException($"客户端热更新清单禁止包含服务端程序集：{name}");
                }
            }
        }

        /// <summary>
        /// 确认物理程序集边界不会把 Server/Inner 编入客户端 Player，或把 Client 编入默认 DS Player。
        /// </summary>
        private static void ValidateAssemblyConstraints()
        {
            ValidateAssemblyConstraint(ClientAssemblyDefinitionPath, ClientConstraint);
            ValidateAssemblyConstraint(ServerAssemblyDefinitionPath, ServerConstraint);
            ValidateAssemblyConstraint(InnerAssemblyDefinitionPath, ServerConstraint);
            ValidateAssemblyConstraint(ControlInnerAssemblyDefinitionPath, ServerConstraint);
        }

        /// <summary>
        /// 校验指定 asmdef 只包含期望的目标表达式。
        /// </summary>
        /// <param name="path">程序集定义路径。</param>
        /// <param name="expected">期望约束表达式。</param>
        private static void ValidateAssemblyConstraint(string path, string expected)
        {
            if (!File.Exists(path))
            {
                throw new BuildFailedException($"缺少运行目标程序集定义：{path}");
            }

            AssemblyDefinitionData definition = JsonUtility.FromJson<AssemblyDefinitionData>(File.ReadAllText(path));
            if (definition?.defineConstraints == null
                || definition.defineConstraints.Length != 1
                || !string.Equals(definition.defineConstraints[0], expected, StringComparison.Ordinal))
            {
                throw new BuildFailedException($"程序集 {path} 的 Define Constraints 必须为：{expected}");
            }
        }

        /// <summary>
        /// 确认热更新资源目录没有错误携带固定 AOT 控制面程序集。
        /// </summary>
        private static void ValidateAotControlHotUpdateAssets()
        {
            if (!Directory.Exists(HotUpdateAssetDirectory))
            {
                return;
            }

            string[] forbiddenNames =
            {
                "MiniCore.Protocol.Control.dll.bytes",
                "MiniCore.Protocol.Control.Inner.dll.bytes"
            };
            for (int index = 0; index < forbiddenNames.Length; index++)
            {
                string path = Path.Combine(HotUpdateAssetDirectory, forbiddenNames[index]);
                if (File.Exists(path))
                {
                    throw new BuildFailedException($"热更新资源目录禁止包含固定 AOT 控制面程序集：{path}");
                }
            }
        }

        /// <summary>
        /// 确认客户端待发布目录不存在 Inner、服务端或拆分前的混合 DLL。
        /// </summary>
        private static void ValidateClientHotUpdateAssets()
        {
            if (!Directory.Exists(HotUpdateAssetDirectory))
            {
                return;
            }

            string[] forbiddenNames =
            {
                "MiniCore.Protocol.Inner.dll.bytes",
                "MiniCore.HotUpdate.Server.dll.bytes",
                "MiniCore.Protocol.dll.bytes",
                "MiniCore.HotUpdate.dll.bytes"
            };
            for (int index = 0; index < forbiddenNames.Length; index++)
            {
                string path = Path.Combine(HotUpdateAssetDirectory, forbiddenNames[index]);
                if (File.Exists(path))
                {
                    throw new BuildFailedException($"客户端热更新资源目录包含 Inner、服务端、AOT 控制面或旧混合程序集：{path}");
                }
            }
        }

        /// <summary>
        /// 只反序列化构建校验需要的 asmdef 字段。
        /// </summary>
        [Serializable]
        private sealed class AssemblyDefinitionData
        {
            public string[] defineConstraints; // Unity asmdef 的目标约束。
        }

        #endregion
    }
}
