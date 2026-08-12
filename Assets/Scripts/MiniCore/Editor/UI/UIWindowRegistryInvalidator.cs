using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MiniCore.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UI;
using YooAsset.Editor;

namespace MiniCore.EditorTools.UI
{

    /// <summary>
    /// HotUpdate UI 源码或窗口 Prefab 改变时执行二阶段注册表失效。
    /// </summary>
    internal sealed class UIWindowRegistryInvalidator : AssetPostprocessor
    {
        #region Private 私有成员

        private const string HotUpdateUIRoot = "Assets/Scripts/MiniCore/HotUpdate/UI/";
        private const string GeneratedRoot = "Assets/Scripts/MiniCore/HotUpdate/UI/Generated/";

        /// <summary>
        /// 在相关资源变化后先清空直接类型引用，避免改名和删除阻断首轮编译。
        /// </summary>
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (ContainsRelevantChange(imported) || ContainsRelevantChange(deleted) || ContainsRelevantChange(moved) || ContainsRelevantChange(movedFrom))
            {
                UIWindowRegistryGenerator.InvalidateRegistry();
            }
        }

        /// <summary>
        /// 判断路径集合是否包含窗口 Prefab 或非生成 UI 源码。
        /// </summary>
        /// <param name="paths">资源路径集合。</param>
        /// <returns>包含相关变化时返回 true。</returns>
        private static bool ContainsRelevantChange(string[] paths)
        {
            if (paths == null)
            {
                return false;
            }

            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                bool prefab = UIAuthoringUtility.IsWindowPrefabPath(path);
                bool source = path.StartsWith(HotUpdateUIRoot, StringComparison.Ordinal) && !path.StartsWith(GeneratedRoot, StringComparison.Ordinal) && path.EndsWith(".cs", StringComparison.Ordinal);
                if (prefab || source)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
