using System;
using MiniCore.UI;
using UnityEditor;
using UnityEngine;

namespace MiniCore.EditorTools.UI
{

    /// <summary>
    /// Prefab 保存时同步窗口 View 的必需组件和生成元数据。
    /// </summary>
    [InitializeOnLoad]
    internal static class UIWindowPrefabSaveProcessor
    {
        #region Private 私有成员

        /// <summary>
        /// 注册 Prefab 保存前自动同步回调。
        /// </summary>
        static UIWindowPrefabSaveProcessor()
        {
            UnityEditor.SceneManagement.PrefabStage.prefabSaving += OnPrefabSaving;
        }

        /// <summary>
        /// 对包含 AUIWindowView 的 Prefab 根节点补齐必需组件并同步地址。
        /// </summary>
        /// <param name="root">即将保存的 Prefab 根节点。</param>
        private static void OnPrefabSaving(GameObject root)
        {
            AUIWindowView view = root != null ? root.GetComponent<AUIWindowView>() : null;
            if (view == null)
            {
                return;
            }

            UIAuthoringUtility.EnsureRequiredComponents(root, view);
            string assetPath = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage()?.assetPath;
            if (!string.IsNullOrEmpty(assetPath))
            {
                Type logic = UIAuthoringUtility.ResolveType(view.LogicTypeName);
                UIAuthoringUtility.ConfigureView(view, assetPath, logic);
            }
        }

        #endregion
    }
}
