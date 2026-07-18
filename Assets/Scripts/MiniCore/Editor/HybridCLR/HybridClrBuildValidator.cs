using HybridCLR.Editor.Settings;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 固定 HybridCLR 的 HotUpdate 程序集配置，并在构建前检查 DLL 产物。
    /// </summary>
    internal static class HybridClrBuildValidator
    {
        #region Private 私有成员

        private const string HotUpdateAssemblyName = "MiniCore.HotUpdate";

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 将项目的唯一 HotUpdate 程序集写入 HybridCLR 设置。
        /// </summary>
        internal static void EnsureConfigured()
        {
            HybridCLRSettings settings = HybridCLRSettings.LoadOrCreate();
            if (settings.hotUpdateAssemblies != null && settings.hotUpdateAssemblies.Length == 1 && settings.hotUpdateAssemblies[0] == HotUpdateAssemblyName)
            {
                return;
            }

            settings.hotUpdateAssemblyDefinitions = null;
            settings.hotUpdateAssemblies = new[] { HotUpdateAssemblyName };
            HybridCLRSettings.Save();
        }

        /// <summary>
        /// 校验当前目标平台已产出且可发布 HotUpdate DLL。
        /// </summary>
        /// <param name="error">校验失败原因。</param>
        /// <returns>DLL 已同步时返回 true。</returns>
        internal static bool Validate(out string error)
        {
            if (!HybridClrYooAssetBuildCommand.ValidateRuntimeArtifacts(out error))
            {
                return false;
            }

            error = null;
            return true;
        }

        #endregion
    }
}
