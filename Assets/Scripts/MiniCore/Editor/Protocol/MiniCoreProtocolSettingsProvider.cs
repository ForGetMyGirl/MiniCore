using System;
using UnityEditor;
using UnityEngine;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 在 Project Settings 中提供业务 Proto 输出目录选择入口。
    /// </summary>
    internal static class MiniCoreProtocolSettingsProvider
    {
        #region Private 私有成员

        private const string SettingsPath = "Project/MiniCore/Protocol"; // Project Settings 页面路径。

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 创建项目协议生成设置页面。
        /// </summary>
        /// <returns>设置页面。</returns>
        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            return new SettingsProvider(SettingsPath, SettingsScope.Project)
            {
                label = "MiniCore Protocol",
                guiHandler = DrawSettings,
                keywords = new[] { "MiniCore", "Proto", "Protobuf", "输出目录" }
            };
        }

        /// <summary>
        /// 绘制业务 Proto 输出目录设置。
        /// </summary>
        /// <param name="searchContext">当前设置搜索文本。</param>
        private static void DrawSettings(string searchContext)
        {
            MiniCoreProtocolSettings settings = MiniCoreProtocolSettings.instance;
            EditorGUILayout.LabelField("项目业务 Proto", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("一个项目只使用一个输出目录。目录必须位于 Assets 下，并归属于已登记的热更新程序集。框架内部 ClientSettings.proto 始终输出到 MiniCore.Unity，不受此设置影响。", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.TextField("C# 输出目录", settings.ProjectOutputDirectory);
                if (GUILayout.Button("选择", GUILayout.Width(72f)))
                {
                    SelectOutputDirectory(settings);
                }
            }

            if (GUILayout.Button("生成全部项目协议"))
            {
                ProtoCodeGenerator.Generate();
            }
        }

        /// <summary>
        /// 选择并保存 Assets 下的业务协议输出目录。
        /// </summary>
        /// <param name="settings">目标项目设置。</param>
        private static void SelectOutputDirectory(MiniCoreProtocolSettings settings)
        {
            string selected = EditorUtility.OpenFolderPanel("选择项目业务 Proto 输出目录", Application.dataPath, string.Empty);
            if (string.IsNullOrWhiteSpace(selected))
            {
                return;
            }

            string normalized = selected.Replace('\\', '/').TrimEnd('/');
            string assetsRoot = Application.dataPath.Replace('\\', '/').TrimEnd('/');
            if (!normalized.StartsWith(assetsRoot + "/", StringComparison.Ordinal) && !string.Equals(normalized, assetsRoot, StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog("目录无效", "业务 Proto 输出目录必须位于当前项目 Assets 目录下。", "确定");
                return;
            }

            string relative = "Assets" + normalized.Substring(assetsRoot.Length);
            settings.SetProjectOutputDirectory(relative);
        }

        #endregion
    }
}
