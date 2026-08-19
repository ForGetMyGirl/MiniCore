using System;
using System.IO;
using System.Linq;
using MiniCore.UI;
using UnityEditor;
using UnityEngine;

namespace MiniCore.EditorTools.UI
{
    /// <summary>
    /// UI View、创建向导、生成器和构建校验共用的编辑器操作。
    /// </summary>
    public static class UIAuthoringUtility
    {
        #region Public 公共成员

        /// <summary>
        /// 项目窗口 Prefab 的唯一收集根目录。
        /// </summary>
        public const string WindowsRoot = "Assets/AssetRes/UI/Windows";

        /// <summary>
        /// Demo 自包含窗口 Prefab 的收集根目录。
        /// </summary>
        public const string DemoWindowsRoot = "Assets/AssetRes/Demos/MiniBomber/UI";

        /// <summary>
        /// 注册表生成器扫描的正式窗口和 Demo 窗口根目录。
        /// </summary>
        public static readonly string[] WindowSearchRoots = { WindowsRoot, DemoWindowsRoot };

        /// <summary>
        /// 判断资产路径是否位于正式或 Demo 窗口收集根目录。
        /// </summary>
        /// <param name="path">Unity 资产路径。</param>
        /// <returns>属于可生成窗口 Prefab 时返回 true。</returns>
        public static bool IsWindowPrefabPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab", StringComparison.Ordinal))
            {
                return false;
            }

            for (int index = 0; index < WindowSearchRoots.Length; index++)
            {
                if (path.StartsWith(WindowSearchRoots[index] + "/", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 自动生成注册表文件路径。
        /// </summary>
        public const string RegistryPath = "Assets/Scripts/MiniCore/HotUpdate/UI/Generated/ProjectUIWindowRegistration.Generated.cs";

        /// <summary>
        /// 自动生成路由文件路径。
        /// </summary>
        public const string RoutesPath = "Assets/Scripts/MiniCore/HotUpdate/UI/Generated/UIWindowRoutes.Generated.cs";

        /// <summary>
        /// 为窗口根节点补齐唯一必需的 CanvasGroup 和可选拖拽组件。
        /// </summary>
        /// <param name="root">窗口 Prefab 根节点。</param>
        /// <param name="view">根节点 View。</param>
        /// <returns>本次是否修改对象。</returns>
        public static bool EnsureRequiredComponents(GameObject root, AUIWindowView view)
        {
            if (root == null || view == null)
            {
                return false;
            }

            bool changed = AddIfMissing<CanvasGroup>(root);
            if (view.Template == UIWindowTemplate.FloatingWindow && root.GetComponentInChildren<UIWindowDragHandler>(true) == null)
            {
                Undo.AddComponent<UIWindowDragHandler>(root);
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(root);
                EditorUtility.SetDirty(view);
            }

            return changed;
        }

        /// <summary>
        /// 推导并写入 Prefab 文件名地址和逻辑类型。
        /// </summary>
        /// <param name="view">待更新 View。</param>
        /// <param name="assetPath">Prefab 资产路径。</param>
        /// <param name="logicType">Presenter 类型。</param>
        public static void ConfigureView(AUIWindowView view, string assetPath, Type logicType)
        {
            SerializedObject serialized = new SerializedObject(view);
            serialized.FindProperty("assetAddress").stringValue = Path.GetFileNameWithoutExtension(assetPath);
            serialized.FindProperty("logicTypeName").stringValue = logicType?.AssemblyQualifiedName ?? string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
        }

        /// <summary>
        /// 为新窗口创建模板所需的最小语义节点并绑定安全区和默认动画。
        /// </summary>
        /// <param name="root">窗口根节点。</param>
        /// <param name="view">根节点 View。</param>
        /// <param name="template">业务模板。</param>
        public static void CreateTemplateHierarchy(GameObject root, AUIWindowView view, UIWindowTemplate template)
        {
            RectTransform rootRect = root.transform as RectTransform;
            Stretch(rootRect);
            SerializedObject serializedView = new SerializedObject(view);
            SerializedProperty layer = serializedView.FindProperty("layer");
            SerializedProperty policy = serializedView.FindProperty("safeAreaPolicy");
            SerializedProperty modal = serializedView.FindProperty("modal");
            RectTransform contentRoot = null;
            RectTransform transitionTarget = rootRect;

            switch (template)
            {
                case UIWindowTemplate.Screen:
                    CreateStretchRect("BackgroundRoot", root.transform);
                    contentRoot = CreateStretchRect("ContentRoot", root.transform);
                    layer.enumValueIndex = (int)UILayer.Screen;
                    break;
                case UIWindowTemplate.Hud:
                    contentRoot = CreateStretchRect("ContentRoot", root.transform);
                    layer.enumValueIndex = (int)UILayer.Hud;
                    break;
                case UIWindowTemplate.FloatingWindow:
                    contentRoot = CreateStretchRect("ContentRoot", root.transform);
                    transitionTarget = CreateCenteredRect("PanelRoot", contentRoot, new Vector2(960f, 640f));
                    layer.enumValueIndex = (int)UILayer.Window;
                    break;
                case UIWindowTemplate.ModalPopup:
                    contentRoot = CreateStretchRect("ContentRoot", root.transform);
                    transitionTarget = CreateCenteredRect("PanelRoot", contentRoot, new Vector2(960f, 640f));
                    layer.enumValueIndex = (int)UILayer.Popup;
                    modal.boolValue = true;
                    break;
                case UIWindowTemplate.Toast:
                    contentRoot = CreateStretchRect("ContentRoot", root.transform);
                    transitionTarget = CreateCenteredRect("ToastRoot", contentRoot, new Vector2(900f, 140f));
                    layer.enumValueIndex = (int)UILayer.Toast;
                    break;
                case UIWindowTemplate.Guide:
                    CreateStretchRect("BackgroundRoot", root.transform);
                    contentRoot = CreateStretchRect("ContentRoot", root.transform);
                    layer.enumValueIndex = (int)UILayer.Guide;
                    break;
                case UIWindowTemplate.System:
                    CreateStretchRect("BackgroundRoot", root.transform);
                    contentRoot = CreateStretchRect("ContentRoot", root.transform);
                    layer.enumValueIndex = (int)UILayer.System;
                    break;
                case UIWindowTemplate.Custom:
                    layer.enumValueIndex = (int)UILayer.Screen;
                    policy.enumValueIndex = (int)UISafeAreaPolicy.Ignore;
                    break;
            }

            serializedView.FindProperty("safeAreaTarget").objectReferenceValue = contentRoot;
            serializedView.ApplyModifiedPropertiesWithoutUndo();
            if (template != UIWindowTemplate.Custom)
            {
                UIPresetTransition transition = root.AddComponent<UIPresetTransition>();
                SerializedObject serializedTransition = new SerializedObject(transition);
                serializedTransition.FindProperty("target").objectReferenceValue = transitionTarget;
                serializedTransition.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
                serializedTransition.ApplyModifiedPropertiesWithoutUndo();
                serializedView.Update();
                serializedView.FindProperty("transitionDriver").objectReferenceValue = transition;
                serializedView.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// 获取全部可直接构造的窗口 Presenter 类型。
        /// </summary>
        /// <returns>按完整类型名排序的候选数组。</returns>
        public static Type[] GetLogicTypes()
        {
            return TypeCache.GetTypesDerivedFrom<IUIWindowLogic>()
                .Where(type => !type.IsAbstract && !type.IsGenericTypeDefinition && type.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// 将程序集限定名解析为当前已编译类型。
        /// </summary>
        /// <param name="assemblyQualifiedName">View 保存的类型名。</param>
        /// <returns>匹配类型；无法解析时返回 null。</returns>
        public static Type ResolveType(string assemblyQualifiedName)
        {
            return string.IsNullOrWhiteSpace(assemblyQualifiedName) ? null : Type.GetType(assemblyQualifiedName, false);
        }

        /// <summary>
        /// 创建缺失的 Assets 子目录。
        /// </summary>
        /// <param name="assetFolder">以 Assets 开头的目录。</param>
        public static void EnsureAssetFolder(string assetFolder)
        {
            string[] parts = assetFolder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        /// <summary>
        /// 使用 UTF-8 无 BOM 写入发生变化的生成文件。
        /// </summary>
        /// <param name="assetPath">目标资产路径。</param>
        /// <param name="content">完整文件内容。</param>
        /// <returns>文件内容发生变化时返回 true。</returns>
        public static bool WriteGeneratedFile(string assetPath, string content)
        {
            string fullPath = Path.GetFullPath(assetPath);
            string oldContent = File.Exists(fullPath) ? File.ReadAllText(fullPath) : null;
            if (string.Equals(oldContent, content, StringComparison.Ordinal))
            {
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? throw new InvalidOperationException("生成文件目录无效。"));
            File.WriteAllText(fullPath, content, new System.Text.UTF8Encoding(false));
            return true;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 在支持 Undo 的前提下补齐指定组件。
        /// </summary>
        /// <typeparam name="T">必需 Unity 组件类型。</typeparam>
        /// <param name="root">目标根节点。</param>
        /// <returns>实际添加组件时返回 true。</returns>
        private static bool AddIfMissing<T>(GameObject root) where T : Component
        {
            if (root.GetComponent<T>() != null)
            {
                return false;
            }

            Undo.AddComponent<T>(root);
            return true;
        }

        /// <summary>
        /// 创建并返回全拉伸 RectTransform。
        /// </summary>
        /// <param name="name">节点名称。</param>
        /// <param name="parent">父节点。</param>
        /// <returns>创建的节点。</returns>
        private static RectTransform CreateStretchRect(string name, Transform parent)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)child.transform;
            rect.SetParent(parent, false);
            Stretch(rect);
            return rect;
        }

        /// <summary>
        /// 创建并返回居中的固定尺寸 RectTransform。
        /// </summary>
        /// <param name="name">节点名称。</param>
        /// <param name="parent">父节点。</param>
        /// <param name="size">设计坐标尺寸。</param>
        /// <returns>创建的节点。</returns>
        private static RectTransform CreateCenteredRect(string name, Transform parent, Vector2 size)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)child.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>
        /// 将 RectTransform 设置为全拉伸布局。
        /// </summary>
        /// <param name="rect">目标节点。</param>
        private static void Stretch(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        #endregion
    }
}
