using System;
using HybridCLR.Editor.Settings;
using UnityEditorInternal;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 将项目热更新程序集登记同步到 HybridCLR，并在构建前检查全部 DLL 产物。
    /// </summary>
    internal static class HybridClrBuildValidator
    {
        #region Private 私有成员

        private static int artifactGenerationDepth; // 当前由 MiniCore 发起的 HybridCLR 产物生成嵌套深度。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 获取当前是否正在执行 HybridCLR 产物生成流程。
        /// </summary>
        internal static bool IsGeneratingArtifacts => artifactGenerationDepth > 0;

        /// <summary>
        /// 进入 HybridCLR 产物生成阶段，使其内部临时 Player 构建不校验尚未产出的最终资源。
        /// </summary>
        internal static void BeginArtifactGeneration()
        {
            artifactGenerationDepth++;
        }

        /// <summary>
        /// 退出 HybridCLR 产物生成阶段，并恢复正常 Player 构建的最终资源校验。
        /// </summary>
        internal static void EndArtifactGeneration()
        {
            if (artifactGenerationDepth <= 0)
            {
                throw new InvalidOperationException("HybridCLR 产物生成阶段的进入与退出调用不匹配。");
            }

            artifactGenerationDepth--;
        }

        /// <summary>
        /// 将项目登记的全部热更新程序集按依赖顺序写入 HybridCLR 设置。
        /// </summary>
        internal static void EnsureConfigured()
        {
            MiniCoreHotUpdateAssemblySettings projectSettings = MiniCoreHotUpdateAssemblySettings.Current;
            projectSettings.EnsureDefaultEntries();
            if (!projectSettings.TryValidate(out string error))
            {
                throw new InvalidOperationException(error);
            }

            MiniCoreHotUpdateAssemblyEntry[] entries = projectSettings.GetEntriesInLoadOrder();
            string[] assemblyNames = new string[entries.Length];
            for (int index = 0; index < entries.Length; index++)
            {
                assemblyNames[index] = entries[index].AssemblyName;
            }

            HybridCLRSettings settings = HybridCLRSettings.LoadOrCreate();
            if (AreAssemblyNamesEqual(settings.hotUpdateAssemblies, assemblyNames)
                && (settings.hotUpdateAssemblyDefinitions == null
                    || settings.hotUpdateAssemblyDefinitions.Length == 0))
            {
                return;
            }

            settings.hotUpdateAssemblyDefinitions = Array.Empty<AssemblyDefinitionAsset>();
            settings.hotUpdateAssemblies = assemblyNames;
            HybridCLRSettings.Save();
        }

        /// <summary>
        /// 校验当前目标平台已产出且可发布 HotUpdate DLL。
        /// </summary>
        /// <param name="error">校验失败原因。</param>
        /// <returns>DLL 已同步时返回 true。</returns>
        internal static bool Validate(out string error)
        {
            if (!MiniCoreHotUpdateAssemblySettings.Current.TryValidate(out error))
            {
                return false;
            }

            if (!HybridClrYooAssetBuildCommand.ValidateRuntimeArtifacts(out error))
            {
                return false;
            }

            error = null;
            return true;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 比较 HybridCLR 当前名称数组是否与项目登记顺序完全一致。
        /// </summary>
        /// <param name="current">HybridCLR 当前配置。</param>
        /// <param name="expected">项目登记的期望配置。</param>
        /// <returns>长度和每个程序集名称都一致时返回 true。</returns>
        private static bool AreAssemblyNamesEqual(string[] current, string[] expected)
        {
            if (current == null || current.Length != expected.Length)
            {
                return false;
            }

            for (int index = 0; index < current.Length; index++)
            {
                if (!string.Equals(current[index], expected[index], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
