using System.IO;
using UnityEditor;
using UnityEngine;
using MiniCore.Core;
using MiniCore.Model;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// Simple UI window script generator.
    /// Supports picking output folders and generating View/Presenter scripts.
    /// Optional templates can use {VIEW_CLASS} and {PRESENTER_CLASS} placeholders.
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
            GUILayout.Label("Generator Settings", EditorStyles.boldLabel);

            uiName = EditorGUILayout.TextField("UI Name", uiName);

            EditorGUILayout.Space();
            DrawFolderField("View Output Folder", ref viewFolder);
            DrawFolderField("Presenter Output Folder", ref presenterFolder);

            EditorGUILayout.Space();
            GUILayout.Label("Optional Templates ({VIEW_CLASS} / {PRESENTER_CLASS})", EditorStyles.boldLabel);
            DrawFileField("View Template", ref viewTemplatePath, "Select View Template");
            DrawFileField("Presenter Template", ref presenterTemplatePath, "Select Presenter Template");

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate Scripts", GUILayout.Height(32)))
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
                    // Try convert to Assets-relative path for project usage.
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
                EditorUtility.DisplayDialog("Generate Failed", "Please input UI Name first.", "OK");
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
            EditorUtility.DisplayDialog("Generate Done", $"Generated:\n{viewPath}\n{presenterPath}", "OK");
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
                content = "using MiniCore.Model;\\n\\npublic class {PRESENTER_CLASS} : APresenter<{VIEW_CLASS}>\\n{\\n    protected override void OnBind()\\n    {\\n        // TODO: initialize presenter logic\\n    }\\n}\\n";
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
                LogSwitch.Warning($"Template read failed: {fullPath} => {ex.Message}");
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

            // If absolute path, try converting back to Assets-relative path.
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
