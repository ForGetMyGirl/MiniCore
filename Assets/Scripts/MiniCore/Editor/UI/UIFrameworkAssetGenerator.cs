using System;
using System.Collections.Generic;
using System.Linq;
using MiniCore.HotUpdate;
using MiniCore.Service;
using MiniCore.UI;
using MiniCore.Unity;
using UnityEditor;
using UnityEditor.Presets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiniCore.EditorTools.UI
{
    /// <summary>
    /// 创建或修复项目 UI Profile、Preset、持久 Root，并迁移现有 KCP 示例。
    /// </summary>
    public static class UIFrameworkAssetGenerator
    {
        #region Private 私有成员

        private const string FrameworkRoot = "Assets/AssetRes/UI/Framework";
        private const string ProfilesRoot = "Assets/AssetRes/UI/Profiles";
        private const string PresetsRoot = "Assets/Settings/MiniCore/UI/Presets";
        private const string RootPrefabPath = FrameworkRoot + "/ApplicationUIRoot.prefab";
        private const string ProfilePath = ProfilesRoot + "/UIProjectProfile.asset";
        private const string OldKcpPath = "Assets/AssetRes/Prefabs/UI/KcpTestWindow.prefab";
        private const string KcpPath = "Assets/AssetRes/UI/Windows/KcpTestWindow.prefab";
        private const string KcpWindowId = "de825d1d848b45f193a2fbb15432c4e9";
        private const string MainScenePath = "Assets/Scenes/HotScene/MainScene.unity";
        private static readonly LayerDefinition[] LayerDefinitions =
        {
            new LayerDefinition(UILayer.Background, 0),
            new LayerDefinition(UILayer.Screen, 100),
            new LayerDefinition(UILayer.Hud, 200),
            new LayerDefinition(UILayer.Window, 300),
            new LayerDefinition(UILayer.Popup, 400),
            new LayerDefinition(UILayer.Tooltip, 500),
            new LayerDefinition(UILayer.Toast, 600),
            new LayerDefinition(UILayer.Guide, 700),
            new LayerDefinition(UILayer.Drag, 800),
            new LayerDefinition(UILayer.Transition, 900),
            new LayerDefinition(UILayer.System, 1000),
            new LayerDefinition(UILayer.Debug, 1100)
        };

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 生成或更新框架资产、示例窗口、主场景和启动配置。
        /// </summary>
        [MenuItem("MiniCore/UI/Generate Project UI Assets", priority = 2002)]
        public static void GenerateProjectAssets()
        {
            EnsureProjectAssetFolders();
            CreateNativePresets();
            CreateApplicationRootPrefab(true);
            CreateProjectProfile();
            MigrateKcpWindow();
            RemoveLegacyMainSceneUI();
            ConfigureStartup();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UIWindowRegistryGenerator.Generate();
            Debug.Log("MiniCore UI 项目资产、原生 Preset 和示例迁移完成。");
        }

        /// <summary>
        /// 从 Hierarchy 右键菜单创建或修复项目级 RootCanvas Prefab。
        /// </summary>
        [MenuItem("GameObject/MiniCore/RootCanvas", false, 10)]
        public static void CreateRootCanvasFromHierarchy()
        {
            CreateOrSelectRootCanvas();
        }

        /// <summary>
        /// 从 MiniCore 顶部菜单创建或修复项目级 RootCanvas Prefab。
        /// </summary>
        [MenuItem("MiniCore/UI/RootCanvas", priority = 1999)]
        public static void CreateRootCanvasFromMenu()
        {
            CreateOrSelectRootCanvas();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 创建或修复唯一项目级 RootCanvas Prefab，并定位生成资源。
        /// </summary>
        private static void CreateOrSelectRootCanvas()
        {
            EnsureProjectAssetFolders();
            CreateNativePresets();
            CreateApplicationRootPrefab(true);
            CreateProjectProfile();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            GameObject rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabPath);
            Selection.activeObject = rootPrefab;
            EditorGUIUtility.PingObject(rootPrefab);
            Debug.Log("MiniCore RootCanvas 已创建或修复。它只由 UIService 通过 YooAsset 加载，不会实例化到当前场景。");
        }

        /// <summary>
        /// 确保 UI Root、Profile、Preset 和窗口资源目录已存在。
        /// </summary>
        private static void EnsureProjectAssetFolders()
        {
            UIAuthoringUtility.EnsureAssetFolder(FrameworkRoot);
            UIAuthoringUtility.EnsureAssetFolder(ProfilesRoot);
            UIAuthoringUtility.EnsureAssetFolder(PresetsRoot);
            UIAuthoringUtility.EnsureAssetFolder(UIAuthoringUtility.WindowsRoot);
        }

        /// <summary>
        /// 创建只包含渲染根、直接 Layer Canvas 和唯一 EventSystem 的持久 Root Prefab。
        /// </summary>
        /// <param name="preserveScalerSettings">是否保留现有两个根 CanvasScaler 的合法配置。</param>
        private static void CreateApplicationRootPrefab(bool preserveScalerSettings)
        {
            CanvasScalerSettings overlaySettings = default;
            CanvasScalerSettings cameraSettings = default;
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabPath);
            if (preserveScalerSettings && existing != null)
            {
                overlaySettings = CanvasScalerSettings.Capture(FindChildComponent<CanvasScaler>(existing.transform, "OverlayRootCanvas"));
                cameraSettings = CanvasScalerSettings.Capture(FindChildComponent<CanvasScaler>(existing.transform, "CameraRootCanvas"));
            }

            GameObject rootObject = new GameObject("ApplicationUIRoot", typeof(RectTransform), typeof(UIResolutionService), typeof(UISafeAreaService), typeof(ApplicationUIRoot));
            try
            {
                Stretch((RectTransform)rootObject.transform);
                UIResolutionService resolution = rootObject.GetComponent<UIResolutionService>();
                UISafeAreaService safeArea = rootObject.GetComponent<UISafeAreaService>();
                GameObject eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                eventObject.transform.SetParent(rootObject.transform, false);
                EventSystem eventSystem = eventObject.GetComponent<EventSystem>();

                List<UILayerHost> hosts = new List<UILayerHost>(LayerDefinitions.Length * 2);
                Canvas overlayCanvas = CreateRenderRoot(rootObject.transform, "OverlayRootCanvas", UIRenderSpace.ScreenSpaceOverlay, null, overlaySettings, hosts, out UILayerHost overlayTransitionHost);

                GameObject cameraObject = new GameObject("UICamera", typeof(Camera));
                cameraObject.transform.SetParent(rootObject.transform, false);
                Camera uiCamera = cameraObject.GetComponent<Camera>();
                uiCamera.clearFlags = CameraClearFlags.Depth;
                uiCamera.cullingMask = 1 << 5;
                uiCamera.orthographic = true;
                uiCamera.depth = 100f;
                Canvas cameraCanvas = CreateRenderRoot(rootObject.transform, "CameraRootCanvas", UIRenderSpace.ScreenSpaceCamera, uiCamera, cameraSettings, hosts, out _);

                UILoadingOverlay loading = CreateLoadingOverlay(overlayTransitionHost.Root);
                ApplicationUIRoot applicationRoot = rootObject.GetComponent<ApplicationUIRoot>();
                applicationRoot.Configure(resolution, safeArea, eventSystem, overlayCanvas, cameraCanvas, loading, hosts);
                PrefabUtility.SaveAsPrefabAsset(rootObject, RootPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        /// <summary>
        /// 创建一个根 Canvas 及其全部直接 Layer Canvas。
        /// </summary>
        /// <param name="parent">ApplicationUIRoot。</param>
        /// <param name="name">渲染根名称。</param>
        /// <param name="renderSpace">渲染空间。</param>
        /// <param name="uiCamera">Camera 模式使用的相机；Overlay 传 null。</param>
        /// <param name="settings">需要恢复的 CanvasScaler 设置。</param>
        /// <param name="hosts">全部 Layer Host 输出列表。</param>
        /// <param name="transitionHost">创建的 Transition 层。</param>
        /// <returns>创建的根 Canvas。</returns>
        private static Canvas CreateRenderRoot(
            Transform parent,
            string name,
            UIRenderSpace renderSpace,
            Camera uiCamera,
            CanvasScalerSettings settings,
            List<UILayerHost> hosts,
            out UILayerHost transitionHost)
        {
            GameObject rootObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            rootObject.transform.SetParent(parent, false);
            Stretch((RectTransform)rootObject.transform);
            Canvas canvas = rootObject.GetComponent<Canvas>();
            canvas.renderMode = renderSpace == UIRenderSpace.ScreenSpaceCamera ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
            if (uiCamera != null)
            {
                canvas.worldCamera = uiCamera;
                canvas.planeDistance = 100f;
            }

            CanvasScaler scaler = rootObject.GetComponent<CanvasScaler>();
            ConfigureDefaultCanvasScaler(scaler);
            settings.Apply(scaler);
            transitionHost = null;
            for (int i = 0; i < LayerDefinitions.Length; i++)
            {
                LayerDefinition definition = LayerDefinitions[i];
                UILayerHost host = CreateLayerHost(rootObject.transform, renderSpace, definition.Layer, definition.SortingOrder);
                hosts.Add(host);
                if (definition.Layer == UILayer.Transition)
                {
                    transitionHost = host;
                }
            }

            return canvas;
        }

        /// <summary>
        /// 创建一个直接承载窗口且不带 CanvasScaler 的嵌套 Canvas 层。
        /// </summary>
        /// <param name="parent">所属渲染根。</param>
        /// <param name="renderSpace">所属渲染空间。</param>
        /// <param name="layer">逻辑层。</param>
        /// <param name="order">排序值。</param>
        /// <returns>完成配置的层宿主。</returns>
        private static UILayerHost CreateLayerHost(Transform parent, UIRenderSpace renderSpace, UILayer layer, int order)
        {
            GameObject layerObject = new GameObject(layer + "Layer", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(UILayerHost));
            layerObject.transform.SetParent(parent, false);
            Stretch((RectTransform)layerObject.transform);
            UILayerHost host = layerObject.GetComponent<UILayerHost>();
            host.Configure(renderSpace, layer, order);
            return host;
        }

        /// <summary>
        /// 在 Overlay Transition 层直接创建延迟 Loading 输入遮罩。
        /// </summary>
        /// <param name="parent">Transition Layer。</param>
        /// <returns>Loading 组件。</returns>
        private static UILoadingOverlay CreateLoadingOverlay(RectTransform parent)
        {
            GameObject loadingObject = new GameObject("LoadingOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(UILoadingOverlay));
            RectTransform rect = (RectTransform)loadingObject.transform;
            rect.SetParent(parent, false);
            Stretch(rect);
            loadingObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.38f);
            return loadingObject.GetComponent<UILoadingOverlay>();
        }

        /// <summary>
        /// 为新渲染根写入默认 1920×1080 CanvasScaler 配置。
        /// </summary>
        /// <param name="scaler">目标根 CanvasScaler。</param>
        private static void ConfigureDefaultCanvasScaler(CanvasScaler scaler)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
        }

        /// <summary>
        /// 创建当前项目可编辑的 UI Profile；已存在时保持用户配置。
        /// </summary>
        private static void CreateProjectProfile()
        {
            UIProjectProfile existing = AssetDatabase.LoadAssetAtPath<UIProjectProfile>(ProfilePath);
            if (existing != null)
            {
                EditorUtility.SetDirty(existing);
                return;
            }

            UIProjectProfile profile = ScriptableObject.CreateInstance<UIProjectProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }

        /// <summary>
        /// 创建 CanvasScaler 和窗口动画使用的 Unity 原生 Preset 资产。
        /// </summary>
        private static void CreateNativePresets()
        {
            CreateCanvasScalerPreset("CanvasScaler_Landscape_1920x1080", new Vector2(1920f, 1080f), 0.5f);
            CreateCanvasScalerPreset("CanvasScaler_Portrait_1080x1920", new Vector2(1080f, 1920f), 0.5f);
            CreateCanvasScalerPreset("CanvasScaler_Tablet_2048x1536", new Vector2(2048f, 1536f), 0.5f);
            CreateCanvasScalerPreset("CanvasScaler_CameraSpace", new Vector2(1920f, 1080f), 0.5f);
            CreateTransitionPreset("Transition_Fade", 0.2f, 0.15f, true, false, false, Vector3.one, Vector2.zero);
            CreateTransitionPreset("Transition_PopupScale", 0.2f, 0.15f, true, true, false, new Vector3(0.92f, 0.92f, 1f), Vector2.zero);
            CreateTransitionPreset("Transition_SlideLeft", 0.25f, 0.2f, true, false, true, Vector3.one, new Vector2(-160f, 0f));
            CreateTransitionPreset("Transition_SlideRight", 0.25f, 0.2f, true, false, true, Vector3.one, new Vector2(160f, 0f));
            CreateTransitionPreset("Transition_Toast", 0.18f, 0.18f, true, false, true, Vector3.one, new Vector2(0f, -80f));
        }

        /// <summary>
        /// 创建一个 CanvasScaler 原生 Preset；已存在时保持用户版本。
        /// </summary>
        /// <param name="name">Preset 文件名。</param>
        /// <param name="referenceResolution">设计分辨率。</param>
        /// <param name="match">宽高混合权重。</param>
        private static void CreateCanvasScalerPreset(string name, Vector2 referenceResolution, float match)
        {
            string path = $"{PresetsRoot}/{name}.preset";
            if (AssetDatabase.LoadAssetAtPath<Preset>(path) != null)
            {
                return;
            }

            GameObject temporary = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            try
            {
                CanvasScaler scaler = temporary.GetComponent<CanvasScaler>();
                ConfigureDefaultCanvasScaler(scaler);
                scaler.referenceResolution = referenceResolution;
                scaler.matchWidthOrHeight = match;
                AssetDatabase.CreateAsset(new Preset(scaler), path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporary);
            }
        }

        /// <summary>
        /// 创建一个 UIPresetTransition 原生 Preset，并排除场景对象引用字段。
        /// </summary>
        /// <param name="name">Preset 文件名。</param>
        /// <param name="enterDuration">入场时长。</param>
        /// <param name="exitDuration">退场时长。</param>
        /// <param name="alpha">是否动画透明度。</param>
        /// <param name="scale">是否动画缩放。</param>
        /// <param name="position">是否动画位移。</param>
        /// <param name="enterScale">入场缩放。</param>
        /// <param name="enterOffset">入场位移。</param>
        private static void CreateTransitionPreset(
            string name,
            float enterDuration,
            float exitDuration,
            bool alpha,
            bool scale,
            bool position,
            Vector3 enterScale,
            Vector2 enterOffset)
        {
            string path = $"{PresetsRoot}/{name}.preset";
            if (AssetDatabase.LoadAssetAtPath<Preset>(path) != null)
            {
                return;
            }

            GameObject temporary = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(UIPresetTransition));
            try
            {
                UIPresetTransition transition = temporary.GetComponent<UIPresetTransition>();
                SerializedObject serialized = new SerializedObject(transition);
                serialized.FindProperty("enterDuration").floatValue = enterDuration;
                serialized.FindProperty("exitDuration").floatValue = exitDuration;
                serialized.FindProperty("animateAlpha").boolValue = alpha;
                serialized.FindProperty("animateScale").boolValue = scale;
                serialized.FindProperty("animatePosition").boolValue = position;
                serialized.FindProperty("enterScale").vector3Value = enterScale;
                serialized.FindProperty("enterOffset").vector2Value = enterOffset;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Preset preset = new Preset(transition)
                {
                    excludedProperties = new[] { "target", "canvasGroup" }
                };
                AssetDatabase.CreateAsset(preset, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporary);
            }
        }

        /// <summary>
        /// 移动 KCP 示例、迁移 View 内置配置并创建标准 ContentRoot。
        /// </summary>
        private static void MigrateKcpWindow()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(KcpPath) == null && AssetDatabase.LoadAssetAtPath<GameObject>(OldKcpPath) != null)
            {
                string moveError = AssetDatabase.MoveAsset(OldKcpPath, KcpPath);
                if (!string.IsNullOrEmpty(moveError))
                {
                    throw new InvalidOperationException(moveError);
                }
            }

            GameObject prefab = PrefabUtility.LoadPrefabContents(KcpPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"KCP 示例 Prefab 不存在：{KcpPath}。");
            }

            try
            {
                AUIWindowView view = prefab.GetComponent<AUIWindowView>();
                if (view == null)
                {
                    throw new InvalidOperationException("KCP 示例 Prefab 缺少 KcpTestWindowView。");
                }

                CanvasGroup canvasGroup = prefab.GetComponent<CanvasGroup>() ?? prefab.AddComponent<CanvasGroup>();
                UIPresetTransition transition = prefab.GetComponent<UIPresetTransition>() ?? prefab.AddComponent<UIPresetTransition>();
                RectTransform contentRoot = FindDirectChild(prefab.transform, "ContentRoot") as RectTransform;
                if (contentRoot == null)
                {
                    int childCount = prefab.transform.childCount;
                    Transform[] children = new Transform[childCount];
                    for (int i = 0; i < childCount; i++)
                    {
                        children[i] = prefab.transform.GetChild(i);
                    }

                    contentRoot = CreateStretchRoot("ContentRoot", prefab.transform);
                    for (int i = 0; i < children.Length; i++)
                    {
                        children[i].SetParent(contentRoot, false);
                    }
                }

                SerializedObject serialized = new SerializedObject(view);
                SerializedProperty id = serialized.FindProperty("windowId");
                id.stringValue = KcpWindowId;
                serialized.FindProperty("routeName").stringValue = "KcpTestWindow";
                serialized.FindProperty("logicTypeName").stringValue = typeof(KcpTestWindowPresenter).AssemblyQualifiedName;
                serialized.FindProperty("assetAddress").stringValue = "KcpTestWindow";
                serialized.FindProperty("template").enumValueIndex = (int)UIWindowTemplate.Screen;
                serialized.FindProperty("renderSpace").enumValueIndex = (int)UIRenderSpace.ScreenSpaceOverlay;
                serialized.FindProperty("layer").enumValueIndex = (int)UILayer.Screen;
                serialized.FindProperty("instancePolicy").enumValueIndex = (int)UIInstancePolicy.Singleton;
                serialized.FindProperty("cachePolicy").enumValueIndex = (int)UICachePolicy.CacheOnClose;
                serialized.FindProperty("safeAreaPolicy").enumValueIndex = (int)UISafeAreaPolicy.ConstrainContent;
                serialized.FindProperty("safeAreaTarget").objectReferenceValue = contentRoot;
                serialized.FindProperty("transitionDriver").objectReferenceValue = transition;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject serializedTransition = new SerializedObject(transition);
                serializedTransition.FindProperty("target").objectReferenceValue = contentRoot;
                serializedTransition.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
                serializedTransition.ApplyModifiedPropertiesWithoutUndo();
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                RemoveLegacyWindowComponents(prefab);
                PrefabUtility.SaveAsPrefabAsset(prefab, KcpPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        /// <summary>
        /// 删除旧 Authoring、Fitter 和已经失去脚本的 MonoBehaviour。
        /// </summary>
        /// <param name="root">待迁移 Prefab 根节点。</param>
        private static void RemoveLegacyWindowComponents(GameObject root)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                GameObject current = transforms[i].gameObject;
                MonoBehaviour[] behaviours = current.GetComponents<MonoBehaviour>();
                for (int j = behaviours.Length - 1; j >= 0; j--)
                {
                    MonoBehaviour behaviour = behaviours[j];
                    if (behaviour == null)
                    {
                        continue;
                    }

                    string typeName = behaviour.GetType().FullName;
                    if (string.Equals(typeName, "MiniCore.UI.UIWindowAuthoring", StringComparison.Ordinal) ||
                        string.Equals(typeName, "MiniCore.UI.UISafeAreaFitter", StringComparison.Ordinal) ||
                        string.Equals(typeName, "MiniCore.UI.UINoneTransition", StringComparison.Ordinal))
                    {
                        UnityEngine.Object.DestroyImmediate(behaviour);
                    }
                }

                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(current);
            }
        }

        /// <summary>
        /// 从业务主场景移除旧 MainCanvas 和重复 EventSystem；Bootstrap 更新场景不处理。
        /// </summary>
        private static void RemoveLegacyMainSceneUI()
        {
            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                RemoveNamedObject(roots[i].transform, "MainCanvas");
                if (roots[i] != null)
                {
                    RemoveNamedObject(roots[i].transform, "EventSystem");
                }
            }

            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// 在层级中删除指定名称的旧节点。
        /// </summary>
        /// <param name="current">当前遍历节点。</param>
        /// <param name="targetName">待删除名称。</param>
        private static void RemoveNamedObject(Transform current, string targetName)
        {
            if (string.Equals(current.name, targetName, StringComparison.Ordinal))
            {
                UnityEngine.Object.DestroyImmediate(current.gameObject);
                return;
            }

            for (int i = current.childCount - 1; i >= 0; i--)
            {
                RemoveNamedObject(current.GetChild(i), targetName);
            }
        }

        /// <summary>
        /// 同步启动配置、启用 UIService、写入 ProfileAddress 并重新生成共享启动代码。
        /// </summary>
        private static void ConfigureStartup()
        {
            MiniCoreStartupSettings settings = MiniCoreStartupCodeGenerator.GetOrCreateSettings();
            MiniCoreStartupCodeGenerator.SynchronizeSettings(settings);
            MiniCoreAppServiceSettings ui = settings.Services.FirstOrDefault(item => item.AssemblyQualifiedTypeName.StartsWith(typeof(UIService).FullName + ",", StringComparison.Ordinal));
            if (ui == null)
            {
                throw new InvalidOperationException("启动配置未发现 UIService。");
            }

            ui.Enabled = true;
            MiniCoreStartupArgumentSettings profileArgument = ui.Arguments.FirstOrDefault(item => string.Equals(item.MemberName, nameof(UIServiceInitArgs.ProfileAddress), StringComparison.Ordinal));
            if (profileArgument == null)
            {
                throw new InvalidOperationException("UIService 启动参数缺少 ProfileAddress。");
            }

            profileArgument.UseCodeDefault = false;
            profileArgument.Value = "UIProjectProfile";
            EditorUtility.SetDirty(settings);
            if (!MiniCoreStartupCodeGenerator.Generate(settings, out string error))
            {
                throw new InvalidOperationException(error);
            }
        }

        /// <summary>
        /// 创建拉伸填充父节点的 RectTransform。
        /// </summary>
        /// <param name="name">节点名称。</param>
        /// <param name="parent">父节点。</param>
        /// <returns>创建的节点。</returns>
        private static RectTransform CreateStretchRoot(string name, Transform parent)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)child.transform;
            rect.SetParent(parent, false);
            Stretch(rect);
            return rect;
        }

        /// <summary>
        /// 将 RectTransform 锚点和偏移设置为全拉伸。
        /// </summary>
        /// <param name="rect">目标节点。</param>
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 查找指定名称节点上的组件。
        /// </summary>
        /// <typeparam name="T">目标组件类型。</typeparam>
        /// <param name="root">查找根节点。</param>
        /// <param name="name">目标节点名。</param>
        /// <returns>找到的组件；不存在时返回 null。</returns>
        private static T FindChildComponent<T>(Transform root, string name) where T : Component
        {
            Transform child = FindChild(root, name);
            return child != null ? child.GetComponent<T>() : null;
        }

        /// <summary>
        /// 深度优先查找指定名称节点。
        /// </summary>
        /// <param name="root">查找根节点。</param>
        /// <param name="name">目标节点名。</param>
        /// <returns>找到的节点；不存在时返回 null。</returns>
        private static Transform FindChild(Transform root, string name)
        {
            if (string.Equals(root.name, name, StringComparison.Ordinal))
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChild(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// 查找根节点的直接子节点。
        /// </summary>
        /// <param name="root">父节点。</param>
        /// <param name="name">子节点名称。</param>
        /// <returns>匹配直接子节点；不存在时返回 null。</returns>
        private static Transform FindDirectChild(Transform root, string name)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (string.Equals(child.name, name, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        /// <summary>
        /// 定义一个稳定 Layer 排序项。
        /// </summary>
        private readonly struct LayerDefinition
        {
            #region Public 公共成员

            /// <summary>
            /// 获取逻辑层。
            /// </summary>
            public UILayer Layer { get; }

            /// <summary>
            /// 获取排序值。
            /// </summary>
            public int SortingOrder { get; }

            /// <summary>
            /// 创建一个 Layer 排序定义。
            /// </summary>
            /// <param name="layer">逻辑层。</param>
            /// <param name="sortingOrder">Canvas 排序值。</param>
            public LayerDefinition(UILayer layer, int sortingOrder)
            {
                Layer = layer;
                SortingOrder = sortingOrder;
            }

            #endregion
        }

        /// <summary>
        /// 保存一次根 CanvasScaler 的可序列化设置快照。
        /// </summary>
        private readonly struct CanvasScalerSettings
        {
            #region Private 私有成员

            private readonly bool valid; // 是否捕获到有效根缩放器。
            private readonly CanvasScaler.ScaleMode scaleMode; // 缩放模式。
            private readonly Vector2 referenceResolution; // 设计分辨率。
            private readonly CanvasScaler.ScreenMatchMode matchMode; // 屏幕匹配模式。
            private readonly float match; // 宽高匹配权重。
            private readonly float referencePixelsPerUnit; // 参考像素密度。

            #endregion

            #region Private 私有成员

            /// <summary>
            /// 创建一份 CanvasScaler 设置快照。
            /// </summary>
            /// <param name="isValid">是否有效。</param>
            /// <param name="mode">缩放模式。</param>
            /// <param name="resolution">设计分辨率。</param>
            /// <param name="screenMatchMode">屏幕匹配模式。</param>
            /// <param name="matchValue">宽高匹配权重。</param>
            /// <param name="pixelsPerUnit">参考像素密度。</param>
            private CanvasScalerSettings(
                bool isValid,
                CanvasScaler.ScaleMode mode,
                Vector2 resolution,
                CanvasScaler.ScreenMatchMode screenMatchMode,
                float matchValue,
                float pixelsPerUnit)
            {
                valid = isValid;
                scaleMode = mode;
                referenceResolution = resolution;
                matchMode = screenMatchMode;
                match = matchValue;
                referencePixelsPerUnit = pixelsPerUnit;
            }

            #endregion

            #region Public 公共成员

            /// <summary>
            /// 捕获一个合法根 CanvasScaler 的设置。
            /// </summary>
            /// <param name="scaler">待捕获缩放器。</param>
            /// <returns>可稍后恢复的设置快照。</returns>
            public static CanvasScalerSettings Capture(CanvasScaler scaler)
            {
                if (scaler == null || scaler.referenceResolution.x <= 0f || scaler.referenceResolution.y <= 0f)
                {
                    return default;
                }

                return new CanvasScalerSettings(
                    true,
                    scaler.uiScaleMode,
                    scaler.referenceResolution,
                    scaler.screenMatchMode,
                    scaler.matchWidthOrHeight,
                    scaler.referencePixelsPerUnit);
            }

            /// <summary>
            /// 将有效快照恢复到新根 CanvasScaler。
            /// </summary>
            /// <param name="scaler">目标缩放器。</param>
            public void Apply(CanvasScaler scaler)
            {
                if (!valid || scaler == null)
                {
                    return;
                }

                scaler.uiScaleMode = scaleMode;
                scaler.referenceResolution = referenceResolution;
                scaler.screenMatchMode = matchMode;
                scaler.matchWidthOrHeight = match;
                scaler.referencePixelsPerUnit = referencePixelsPerUnit;
            }

            #endregion
        }

        #endregion
    }
}
