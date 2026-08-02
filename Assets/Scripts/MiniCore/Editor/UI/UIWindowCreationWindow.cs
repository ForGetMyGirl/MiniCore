using System;
using System.IO;
using System.Linq;
using System.Text;
using MiniCore.UI;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace MiniCore.EditorTools.UI
{
    /// <summary>
    /// 以二阶段流程创建 View、Presenter 和窗口 Prefab 的项目向导。
    /// </summary>
    public sealed class UIWindowCreationWindow : EditorWindow
    {
        #region Private 私有成员

        private const string PendingNameKey = "MiniCore.UI.Pending.Name";
        private const string PendingTemplateKey = "MiniCore.UI.Pending.Template";
        private const string PendingInstanceKey = "MiniCore.UI.Pending.Instance";
        private const string ScriptRoot = "Assets/Scripts/MiniCore/HotUpdate/UI/Windows";
        private string windowName = "NewWindow"; // 不含 View 或 Presenter 后缀的窗口名。
        private UIWindowTemplate template = UIWindowTemplate.Screen; // 创建模板。
        private UIInstancePolicy instancePolicy = UIInstancePolicy.Singleton; // 实例策略。

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 打开商用 UI 窗口创建向导。
        /// </summary>
        [MenuItem("MiniCore/UI/Create Window", priority = 2000)]
        private static void Open()
        {
            GetWindow<UIWindowCreationWindow>(true, "Create UI Window").Show();
        }

        /// <summary>
        /// 绘制创建窗口所需的最小配置。
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Window Authoring", EditorStyles.boldLabel);
            windowName = EditorGUILayout.TextField("Window Name", windowName);
            template = (UIWindowTemplate)EditorGUILayout.EnumPopup("Template", template);
            instancePolicy = (UIInstancePolicy)EditorGUILayout.EnumPopup("Instance Policy", instancePolicy);
            EditorGUILayout.HelpBox("向导先生成 View/Presenter；脚本编译后自动创建 Prefab、补齐必需组件并生成强类型 Registry。", MessageType.Info);
            if (GUILayout.Button("Create", GUILayout.Height(32f)))
            {
                CreateScripts();
            }
        }

        /// <summary>
        /// 校验名称并生成 UTF-8 无 BOM 的 View 与 Presenter 源码。
        /// </summary>
        private void CreateScripts()
        {
            if (!IsValidIdentifier(windowName))
            {
                EditorUtility.DisplayDialog("创建失败", "Window Name 必须是合法 C# 标识符，且不包含 View/Presenter 后缀。", "确定");
                return;
            }

            UIAuthoringUtility.EnsureAssetFolder(ScriptRoot + "/View");
            UIAuthoringUtility.EnsureAssetFolder(ScriptRoot + "/Presenter");
            UIAuthoringUtility.EnsureAssetFolder(UIAuthoringUtility.WindowsRoot);
            string viewPath = $"{ScriptRoot}/View/{windowName}View.cs";
            string presenterPath = $"{ScriptRoot}/Presenter/{windowName}Presenter.cs";
            if (File.Exists(Path.GetFullPath(viewPath)) || File.Exists(Path.GetFullPath(presenterPath)))
            {
                EditorUtility.DisplayDialog("创建失败", "同名 View 或 Presenter 已存在。", "确定");
                return;
            }

            File.WriteAllText(Path.GetFullPath(viewPath), BuildViewSource(), new UTF8Encoding(false));
            File.WriteAllText(Path.GetFullPath(presenterPath), BuildPresenterSource(), new UTF8Encoding(false));
            SessionState.SetString(PendingNameKey, windowName);
            SessionState.SetInt(PendingTemplateKey, (int)template);
            SessionState.SetInt(PendingInstanceKey, (int)instancePolicy);
            AssetDatabase.Refresh();
            Close();
        }

        /// <summary>
        /// 生成被动 View 基类源码。
        /// </summary>
        /// <returns>完整 C# 文件文本。</returns>
        private string BuildViewSource()
        {
            return $@"using MiniCore.UI;

namespace MiniCore.HotUpdate
{{
    /// <summary>
    /// {windowName} 的被动 Unity View。
    /// </summary>
    public sealed class {windowName}View : AUIWindowView
    {{
        #region UnityProperty Unity 引用属性

        // 在此声明并由 Prefab 绑定 UGUI 控件。

        #endregion
    }}
}}
";
        }

        /// <summary>
        /// 生成 Presenter 生命周期模板源码。
        /// </summary>
        /// <returns>完整 C# 文件文本。</returns>
        private string BuildPresenterSource()
        {
            return $@"using MiniCore.UI;

namespace MiniCore.HotUpdate
{{
    /// <summary>
    /// {windowName} 的业务 Presenter。
    /// </summary>
    public sealed class {windowName}Presenter : AUIWindowPresenter<{windowName}View>
    {{
        #region Protected 受保护成员

        /// <summary>
        /// 登记控件和业务事件绑定，并完成首次渲染。
        /// </summary>
        protected override void OnBind()
        {{
        }}

        #endregion
    }}
}}
";
        }

        /// <summary>
        /// 判断文本是否为简单合法 C# 标识符。
        /// </summary>
        /// <param name="value">待验证名称。</param>
        /// <returns>名称可直接用于生成类型时返回 true。</returns>
        private static bool IsValidIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.EndsWith("View", StringComparison.Ordinal) || value.EndsWith("Presenter", StringComparison.Ordinal))
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (i == 0 ? !char.IsLetter(character) && character != '_' : !char.IsLetterOrDigit(character) && character != '_')
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 脚本重载后完成挂起的 Prefab 创建阶段。
        /// </summary>
        [DidReloadScripts]
        private static void CompletePendingCreation()
        {
            string name = SessionState.GetString(PendingNameKey, string.Empty);
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            Type viewType = TypeCache.GetTypesDerivedFrom<AUIWindowView>().FirstOrDefault(type => string.Equals(type.FullName, $"MiniCore.HotUpdate.{name}View", StringComparison.Ordinal));
            Type logicType = UIAuthoringUtility.GetLogicTypes().FirstOrDefault(type => string.Equals(type.FullName, $"MiniCore.HotUpdate.{name}Presenter", StringComparison.Ordinal));
            if (viewType == null || logicType == null)
            {
                Debug.LogError($"UI 窗口 {name} 的脚本未成功编译，Prefab 创建已暂停。修复编译错误后会再次尝试。");
                return;
            }

            SessionState.EraseString(PendingNameKey);
            UIWindowTemplate pendingTemplate = (UIWindowTemplate)SessionState.GetInt(PendingTemplateKey, (int)UIWindowTemplate.Screen);
            UIInstancePolicy pendingInstance = (UIInstancePolicy)SessionState.GetInt(PendingInstanceKey, (int)UIInstancePolicy.Singleton);
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            try
            {
                AUIWindowView view = (AUIWindowView)root.AddComponent(viewType);
                SerializedObject serialized = new SerializedObject(view);
                serialized.FindProperty("routeName").stringValue = name;
                serialized.FindProperty("template").enumValueIndex = (int)pendingTemplate;
                serialized.FindProperty("instancePolicy").enumValueIndex = (int)pendingInstance;
                serialized.FindProperty("logicTypeName").stringValue = logicType.AssemblyQualifiedName;
                serialized.FindProperty("assetAddress").stringValue = name;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                UIAuthoringUtility.EnsureRequiredComponents(root, view);
                UIAuthoringUtility.CreateTemplateHierarchy(root, view, pendingTemplate);
                string prefabPath = $"{UIAuthoringUtility.WindowsRoot}/{name}.prefab";
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                UIWindowRegistryGenerator.Generate();
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Debug.Log($"已创建 UI 窗口：{prefabPath}，View：{view.GetType().FullName}。");
            }
            finally
            {
                DestroyImmediate(root);
            }
        }

        #endregion
    }
}
