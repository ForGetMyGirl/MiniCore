using System;
using UnityEditor;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 在 HotUpdate 源码变更时预先移除旧 Handler 生成表中的直接类型引用，保证后续脚本编译可以完成。
    /// </summary>
    internal sealed class OpcodeHandlerRegistryInvalidator : AssetPostprocessor
    {
        #region Private 私有成员

        private const string HotUpdateSourceRoot = "Assets/Scripts/MiniCore/HotUpdate/";
        private const string GeneratedRegistryPath = "Assets/Scripts/MiniCore/HotUpdate/Generated/Network/HotUpdateHandlerRegistry.Generated.cs";

        /// <summary>
        /// 在 Unity 导入、删除或移动 HotUpdate 源码后使旧 Handler 直接注册表失效。
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
            if (!ContainsHotUpdateSourceChange(importedAssets) &&
                !ContainsHotUpdateSourceChange(deletedAssets) &&
                !ContainsHotUpdateSourceChange(movedAssets) &&
                !ContainsHotUpdateSourceChange(movedFromAssetPaths))
            {
                return;
            }

            OpcodeRegistryGenerator.InvalidateGeneratedHandlerRegistry();
        }

        /// <summary>
        /// 判断一组资源路径中是否包含会影响 HotUpdate Handler 扫描结果的 C# 源文件。
        /// </summary>
        /// <param name="assetPaths">需要检查的资源路径集合。</param>
        /// <returns>包含相关 HotUpdate C# 源文件时返回 true。</returns>
        private static bool ContainsHotUpdateSourceChange(string[] assetPaths)
        {
            if (assetPaths == null)
            {
                return false;
            }

            foreach (string assetPath in assetPaths)
            {
                if (IsHotUpdateSourcePath(assetPath))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断单个资源路径是否为需要触发 Handler 注册表失效的 HotUpdate C# 源文件。
        /// </summary>
        /// <param name="assetPath">待检查的资源路径。</param>
        /// <returns>路径属于 HotUpdate 源码且不是生成注册表自身时返回 true。</returns>
        private static bool IsHotUpdateSourcePath(string assetPath)
        {
            return !string.IsNullOrEmpty(assetPath) &&
                   assetPath.StartsWith(HotUpdateSourceRoot, StringComparison.Ordinal) &&
                   assetPath.EndsWith(".cs", StringComparison.Ordinal) &&
                   !string.Equals(assetPath, GeneratedRegistryPath, StringComparison.Ordinal);
        }

        #endregion
    }
}
