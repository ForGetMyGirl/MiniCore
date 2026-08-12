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
    /// 脚本重载成功后执行二阶段注册表重建。
    /// </summary>
    internal static class UIWindowRegistryReloadGenerator
    {
        #region Private 私有成员

        /// <summary>
        /// 编译完成后重新扫描当前有效类型并生成直接注册表。
        /// </summary>
        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorApplication.delayCall += GenerateAfterImport;
        }

        /// <summary>
        /// 等待当前导入批次结束并同步刷新 AssetDatabase 后生成窗口表。
        /// </summary>
        private static void GenerateAfterImport()
        {
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                UIWindowRegistryGenerator.Generate();
            }
            catch (Exception exception)
            {
                Debug.LogError($"UI Window Registry 自动生成失败：{exception.Message}");
            }
        }

        #endregion
    }
}
