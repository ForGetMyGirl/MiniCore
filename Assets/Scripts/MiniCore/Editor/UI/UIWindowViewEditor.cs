using System;
using MiniCore.UI;
using UnityEditor;
using UnityEngine;

namespace MiniCore.EditorTools.UI
{
    /// <summary>
    /// 提供窗口模板、实例策略、动画、安全区和高级覆盖项的可验证 Inspector。
    /// </summary>
    [CustomEditor(typeof(AUIWindowView), true)]
    public sealed class UIWindowViewEditor : UnityEditor.Editor
    {
        #region Private 私有成员

        private static readonly string[] FrameworkPropertyNames =
        {
            "m_Script",
            "transitionDriver",
            "safeAreaTarget",
            "windowId",
            "routeName",
            "logicTypeName",
            "assetAddress",
            "template",
            "renderSpace",
            "layer",
            "instancePolicy",
            "duplicateOpenPolicy",
            "cachePolicy",
            "safeAreaPolicy",
            "modal",
            "closeOnMaskClick",
            "maxCacheCount",
            "navigationGroup"
        }; // 已由框架 Authoring 区单独绘制的序列化字段。
        private bool showAdvanced; // 是否展开高级覆盖项。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 绘制精简默认项、逻辑类型下拉和可选高级配置。
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            AUIWindowView view = (AUIWindowView)target;
            UIAuthoringUtility.EnsureRequiredComponents(view.gameObject, view);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Window ID", serializedObject.FindProperty("windowId").stringValue);
                EditorGUILayout.TextField("Asset Address", serializedObject.FindProperty("assetAddress").stringValue);
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("routeName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("template"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("instancePolicy"));
            DrawLogicPopup();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("transitionDriver"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("safeAreaTarget"));
            DrawViewBindings();

            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced", true);
            if (showAdvanced)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("renderSpace"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("layer"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("duplicateOpenPolicy"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cachePolicy"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("safeAreaPolicy"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("modal"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("closeOnMaskClick"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxCacheCount"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("navigationGroup"));
            }

            DrawValidationMessages(view);

            serializedObject.ApplyModifiedProperties();
            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(view.gameObject);
            if (!string.IsNullOrEmpty(assetPath))
            {
                Type logic = UIAuthoringUtility.ResolveType(serializedObject.FindProperty("logicTypeName").stringValue);
                UIAuthoringUtility.ConfigureView(view, assetPath, logic);
            }
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 判断序列化字段是否属于派生 View 自己声明的控件绑定。
        /// </summary>
        /// <param name="propertyName">序列化字段名称。</param>
        /// <returns>字段不属于框架 Authoring 配置时返回 true。</returns>
        internal static bool IsViewBindingProperty(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            for (int index = 0; index < FrameworkPropertyNames.Length; index++)
            {
                if (string.Equals(FrameworkPropertyNames[index], propertyName, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 检查当前窗口是否含有需要在 Inspector 显示的派生 View 字段。
        /// </summary>
        /// <param name="viewObject">目标窗口序列化对象。</param>
        /// <returns>至少存在一个派生 View 字段时返回 true。</returns>
        internal static bool HasViewBindingProperties(SerializedObject viewObject)
        {
            if (viewObject == null)
            {
                throw new ArgumentNullException(nameof(viewObject));
            }

            SerializedProperty iterator = viewObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (IsViewBindingProperty(iterator.name))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 绘制派生 View 声明的按钮、文本和业务对象引用。
        /// </summary>
        private void DrawViewBindings()
        {
            if (!HasViewBindingProperties(serializedObject))
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("View Bindings", EditorStyles.boldLabel);
            DrawPropertiesExcluding(serializedObject, FrameworkPropertyNames);
        }

        /// <summary>
        /// 绘制可直接构造逻辑类型下拉并保存程序集限定名。
        /// </summary>
        private void DrawLogicPopup()
        {
            SerializedProperty property = serializedObject.FindProperty("logicTypeName");
            Type[] types = UIAuthoringUtility.GetLogicTypes();
            string[] names = new string[types.Length + 1];
            names[0] = "<未选择>";
            for (int i = 0; i < types.Length; i++)
            {
                names[i + 1] = types[i].FullName;
            }

            int selected = 0;
            for (int i = 0; i < types.Length; i++)
            {
                if (string.Equals(types[i].AssemblyQualifiedName, property.stringValue, StringComparison.Ordinal))
                {
                    selected = i + 1;
                    break;
                }
            }

            int next = EditorGUILayout.Popup("Logic", selected, names);
            property.stringValue = next > 0 ? types[next - 1].AssemblyQualifiedName : string.Empty;
        }

        /// <summary>
        /// 在保存或构建前直接提示当前 View 的确定性配置错误。
        /// </summary>
        /// <param name="view">当前检查的窗口 View。</param>
        private void DrawValidationMessages(AUIWindowView view)
        {
            UISafeAreaPolicy policy = (UISafeAreaPolicy)serializedObject.FindProperty("safeAreaPolicy").enumValueIndex;
            if (policy == UISafeAreaPolicy.Inherit)
            {
                UIProjectProfile profile = AssetDatabase.LoadAssetAtPath<UIProjectProfile>("Assets/AssetRes/UI/Profiles/UIProjectProfile.asset");
                if (profile != null)
                {
                    policy = profile.DefaultSafeAreaPolicy;
                }
            }

            if (policy == UISafeAreaPolicy.ConstrainContent && serializedObject.FindProperty("safeAreaTarget").objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("ConstrainContent 必须配置窗口 Prefab 内的 Safe Area Target。", MessageType.Error);
            }

            MonoBehaviour driver = serializedObject.FindProperty("transitionDriver").objectReferenceValue as MonoBehaviour;
            if (driver != null && !(driver is IUITransitionDriver))
            {
                EditorGUILayout.HelpBox($"Transition Driver {driver.GetType().FullName} 未实现 IUITransitionDriver。", MessageType.Error);
            }
        }

        #endregion
    }

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
