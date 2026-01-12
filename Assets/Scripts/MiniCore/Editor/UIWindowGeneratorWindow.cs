using System.IO;
using UnityEditor;
using UnityEngine;
using MiniCore.Core;
using MiniCore.Model;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 简单的 UI 窗口代码生成器。
    /// 支持选择 View/Presenter 输出目录，填写界面名，一键生成 View 和 Presenter 脚本。
    /// 可选外部模板（使用占位符 {VIEW_CLASS} 和 {PRESENTER_CLASS}）。
    /// </summary>
    public class UIWindowGeneratorWindow : EditorWindow
    {
        private string uiName = "NewWindow";
        private string viewFolder = "Assets/Scripts/MiniCore/HotUpdate/UI/Window";
        private string presenterFolder = "Assets/Scripts/MiniCore/HotUpdate/UI/Presenter";
        private string viewTemplatePath = "Assets/Scripts/MiniCore/Templates/ViewTemplate.txt";
        private string presenterTemplatePath = "Assets/Scripts/MiniCore/Templates/PresenterTemplate.txt";

        [MenuItem("MiniCore/UI Window Generator", priority = 2000)]
        private static void Open()
        {
            GetWindow<UIWindowGeneratorWindow>(true, "UI Window Generator").Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("生成设置", EditorStyles.boldLabel);

            uiName = EditorGUILayout.TextField("界面名", uiName);

            EditorGUILayout.Space();
            DrawFolderField("View 输出目录", ref viewFolder);
            DrawFolderField("Presenter 输出目录", ref presenterFolder);

            EditorGUILayout.Space();
            GUILayout.Label("可选模板（占位符 {VIEW_CLASS} / {PRESENTER_CLASS}）", EditorStyles.boldLabel);
            DrawFileField("View 模板", ref viewTemplatePath, "选择 View 模板");
            DrawFileField("Presenter 模板", ref presenterTemplatePath, "选择 Presenter 模板");

            EditorGUILayout.Space();
            if (GUILayout.Button("生成脚本", GUILayout.Height(32)))
            {
                GenerateScripts();
            }
        }

        private void DrawFolderField(string label, ref string path)
        {
            EditorGUILayout.BeginHorizontal();
            path = EditorGUILayout.TextField(label, path);
            if (GUILayout.Button("...", GUILayout.Width(35)))
            {
                string selected = EditorUtility.OpenFolderPanel(label, Application.dataPath, string.Empty);
                if (!string.IsNullOrEmpty(selected))
                {
                    // 尝试转为相对 Assets 的路径，便于项目内使用
                    if (selected.StartsWith(Application.dataPath))
                    {
                        path = "Assets" + selected.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        path = selected;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFileField(string label, ref string path, string panelTitle)
        {
            EditorGUILayout.BeginHorizontal();
            path = EditorGUILayout.TextField(label, path);
            if (GUILayout.Button("...", GUILayout.Width(35)))
            {
                string selected = EditorUtility.OpenFilePanel(panelTitle, Application.dataPath, "cs");
                if (!string.IsNullOrEmpty(selected))
                {
                    if (selected.StartsWith(Application.dataPath))
                    {
                        path = "Assets" + selected.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        path = selected;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void GenerateScripts()
        {
            if (string.IsNullOrEmpty(uiName))
            {
                EditorUtility.DisplayDialog("生成失败", "请先输入界面名", "OK");
                return;
            }

            string viewClass = uiName + "View";
            string presenterClass = uiName + "Presenter";

            string viewDir = EnsureFolder(viewFolder);
            string presenterDir = EnsureFolder(presenterFolder);

            string viewPath = Path.Combine(viewDir, viewClass + ".cs");
            string presenterPath = Path.Combine(presenterDir, presenterClass + ".cs");

            WriteFile(viewPath, BuildViewContent(viewClass, presenterClass));
            WriteFile(presenterPath, BuildPresenterContent(viewClass, presenterClass));

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("生成完成", $"已生成\n{viewPath}\n{presenterPath}", "OK");
        }

        private string BuildViewContent(string viewClass, string presenterClass)
        {
            string content = TryLoadTemplate(viewTemplatePath);
            if (string.IsNullOrEmpty(content))
            {
                content = "using Cysharp.Threading.Tasks;\\nusing MiniCore.Model;\\nusing UnityEngine;\\n\\n[UIWindow(typeof({PRESENTER_CLASS}))]\\npublic class {VIEW_CLASS} : AUIBase\\n{\\n    public override UniTask OpenAsync()\\n    {\\n        return UniTask.CompletedTask;\\n    }\\n\\n    public override UniTask CloseAsync()\\n    {\\n        return UniTask.CompletedTask;\\n    }\\n}\\n";
            }
            return ApplyTokens(content, viewClass, presenterClass);
        }

        private string BuildPresenterContent(string viewClass, string presenterClass)
        {
            string content = TryLoadTemplate(presenterTemplatePath);
            if (string.IsNullOrEmpty(content))
            {
                content = "using MiniCore.Model;\\n\\npublic class {PRESENTER_CLASS} : APresenter<{VIEW_CLASS}>\\n{\\n    protected override void OnBind()\\n    {\\n        // TODO: 初始化 Presenter 逻辑\\n    }\\n}\\n";
            }
            return ApplyTokens(content, viewClass, presenterClass);
        }

        private string ApplyTokens(string template, string viewClass, string presenterClass)
        {
            string result = template.Replace("{VIEW_CLASS}", viewClass);
            if (!string.IsNullOrEmpty(presenterClass))
            {
                result = result.Replace("{PRESENTER_CLASS}", presenterClass);
            }
            return result;
        }

        private string TryLoadTemplate(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            string fullPath = path;
            if (path.StartsWith("Assets"))
            {
                fullPath = Path.GetFullPath(path);
            }

            try
            {
                if (File.Exists(fullPath))
                {
                    return File.ReadAllText(fullPath);
                }
            }
            catch (System.Exception ex)
            {
                EventCenter.Broadcast(GameEvent.LogWarning, $"读取模板失败: {fullPath} => {ex.Message}");
            }
            return null;
        }

        private string EnsureFolder(string folder)
        {
            string fullPath = folder.StartsWith("Assets") ? Path.GetFullPath(folder) : folder;
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            if (folder.StartsWith("Assets"))
            {
                return folder;
            }
            // 如果是绝对路径，尝试转回相对 Assets
            if (fullPath.StartsWith(Application.dataPath))
            {
                return "Assets" + fullPath.Substring(Application.dataPath.Length);
            }
            return fullPath;
        }

        private void WriteFile(string assetRelativePath, string content)
        {
            string fullPath = assetRelativePath.StartsWith("Assets") ? Path.GetFullPath(assetRelativePath) : assetRelativePath;
            File.WriteAllText(fullPath, content);
        }
    }
}
