using UnityEditor;
using UnityEngine;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 在 Project Settings 中维护项目热更新程序集清单和创建入口。
    /// </summary>
    internal static class MiniCoreHotUpdateAssemblySettingsProvider
    {
        #region Private 私有成员

        private const string SettingsPath = "Project/MiniCore/Hot Update Assemblies"; // Project Settings 页面路径。

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 创建项目热更新程序集设置页面。
        /// </summary>
        /// <returns>设置页面。</returns>
        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            return new SettingsProvider(SettingsPath, SettingsScope.Project)
            {
                label = "MiniCore Hot Update Assemblies",
                guiHandler = DrawSettings,
                keywords = new[] { "MiniCore", "HybridCLR", "HotUpdate", "程序集", "YooAsset" }
            };
        }

        /// <summary>
        /// 绘制热更新程序集登记表、验证按钮和模块创建入口。
        /// </summary>
        /// <param name="searchContext">当前设置搜索文本。</param>
        private static void DrawSettings(string searchContext)
        {
            MiniCoreHotUpdateAssemblySettings settings = MiniCoreHotUpdateAssemblySettings.Current;
            var serializedSettings = new SerializedObject(settings);
            SerializedProperty entries = serializedSettings.FindProperty("entries");
            SerializedProperty includeClient = serializedSettings.FindProperty("includeClientAssembliesInDedicatedServer");

            EditorGUILayout.LabelField("热更新程序集", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "清单是 HybridCLR 编译、YooAsset DLL 地址和 Bootstrap 加载顺序的唯一来源。客户端与 Dedicated Server 各有自己的启动入口和程序集过滤结果。",
                MessageType.Info);
            EditorGUILayout.PropertyField(includeClient, new GUIContent("DS 额外包含 Client", "仅在确实需要客户端 UI 或工具代码时启用；默认关闭。"));
            EditorGUILayout.PropertyField(entries, true);
            if (serializedSettings.ApplyModifiedProperties())
            {
                settings.SaveSettings();
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("验证并同步 HybridCLR"))
                {
                    ValidateAndSynchronize();
                }

                if (GUILayout.Button("创建并登记热更新模块"))
                {
                    MiniCoreHotUpdateAssemblyModuleWindow.Open();
                }
            }
        }

        /// <summary>
        /// 校验当前登记并同步 HybridCLR 项目设置。
        /// </summary>
        private static void ValidateAndSynchronize()
        {
            MiniCoreHotUpdateAssemblySettings settings = MiniCoreHotUpdateAssemblySettings.Current;
            if (!settings.TryValidate(out string error))
            {
                EditorUtility.DisplayDialog("热更新程序集配置无效", error, "确定");
                return;
            }

            HybridClrBuildValidator.EnsureConfigured();
            HybridClrYooAssetBuildCommand.RegenerateRuntimeRegistryFromCurrentAssets();
            EditorUtility.DisplayDialog("同步完成", "HybridCLR 设置和 Bootstrap 运行时登记表已经同步。", "确定");
        }

        #endregion
    }
}
