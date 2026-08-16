using System;
using UnityEditor;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 在任一已登记热更新程序集源码变更时预先移除旧 Handler 直接类型引用，保证后续脚本编译可以完成。
    /// </summary>
    internal sealed class OpcodeHandlerRegistryInvalidator : AssetPostprocessor
    {
        #region Private 私有成员

        private const string ClientGeneratedRegistryPath = "Assets/Scripts/MiniCore/HotUpdate/Generated/Network/HotUpdateHandlerRegistration.Generated.cs"; // 客户端注册表自身不触发递归失效。
        private const string ServerGeneratedRegistryPath = "Assets/Scripts/MiniCore/HotUpdate/Server/Generated/Network/ServerHotUpdateHandlerRegistration.Generated.cs"; // 服务端注册表自身不触发递归失效。

        /// <summary>
        /// 在 Unity 导入、删除或移动已登记热更新程序集源码后使旧 Handler 直接注册表失效。
        /// </summary>
        /// <param name="importedAssets">本次新导入或修改的资源路径。</param>
        /// <param name="deletedAssets">本次删除的资源路径。</param>
        /// <param name="movedAssets">本次移动后的资源路径。</param>
        /// <param name="movedFromAssetPaths">本次移动前的资源路径。</param>
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (!ContainsRegisteredSourceChange(importedAssets, false)
                && !ContainsRegisteredSourceChange(deletedAssets, true)
                && !ContainsRegisteredSourceChange(movedAssets, false)
                && !ContainsRegisteredSourceChange(movedFromAssetPaths, true))
            {
                return;
            }

            OpcodeRegistryGenerator.InvalidateGeneratedHandlerRegistry();
        }

        /// <summary>
        /// 判断一组资源路径中是否包含会影响已登记热更新 Handler 扫描结果的源码。
        /// </summary>
        /// <param name="assetPaths">需要检查的资源路径集合。</param>
        /// <param name="useRegisteredRootFallback">删除或移出后的路径是否按登记 asmdef 根目录回退判断。</param>
        /// <returns>包含相关 C# 或 asmdef 文件时返回 true。</returns>
        private static bool ContainsRegisteredSourceChange(
            string[] assetPaths,
            bool useRegisteredRootFallback)
        {
            if (assetPaths == null)
            {
                return false;
            }

            for (int index = 0; index < assetPaths.Length; index++)
            {
                if (IsRegisteredSourcePath(assetPaths[index], useRegisteredRootFallback))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断单个资源路径是否为需要触发 Handler 注册表失效的已登记程序集源码。
        /// </summary>
        /// <param name="assetPath">待检查的资源路径。</param>
        /// <param name="useRegisteredRootFallback">路径已不存在时是否按登记根目录判断。</param>
        /// <returns>路径属于已登记热更新程序集且不是生成注册表自身时返回 true。</returns>
        private static bool IsRegisteredSourcePath(string assetPath, bool useRegisteredRootFallback)
        {
            if (string.IsNullOrEmpty(assetPath)
                || string.Equals(assetPath, ClientGeneratedRegistryPath, StringComparison.Ordinal)
                || string.Equals(assetPath, ServerGeneratedRegistryPath, StringComparison.Ordinal)
                || (!assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    && !assetPath.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            if (MiniCoreHotUpdateAssemblySettings.TryGetRegisteredAssemblyForOutputDirectory(
                    assetPath,
                    out _,
                    out _))
            {
                return true;
            }

            return useRegisteredRootFallback
                && MiniCoreHotUpdateAssemblySettings.IsPathUnderRegisteredAssembly(assetPath);
        }

        #endregion
    }
}
