using System;
using System.Collections.Generic;
using System.IO;
using MiniCore.EditorTools.UI;
using MiniCore.HotUpdate;
using MiniCore.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiniCore.EditorTests
{
    /// <summary>
    /// 验证 UI 框架身份、状态、安全区域、Root、模板、Preset 和生成表约束。
    /// </summary>
    public sealed class UIFrameworkTests
    {
        #region Public 公共成员

        /// <summary>
        /// 验证 WindowId 可以无损往返 Guid 且稳定参与字典查找。
        /// </summary>
        [Test]
        public void WindowId_RoundTripsGuidAndSupportsDictionaryLookup()
        {
            Guid source = Guid.NewGuid();
            UIWindowId id = UIWindowId.FromGuid(source);
            Dictionary<UIWindowId, string> values = new Dictionary<UIWindowId, string> { [id] = "window" };

            Assert.AreEqual(source, id.ToGuid());
            Assert.AreEqual("window", values[UIWindowId.FromGuid(source)]);
        }

        /// <summary>
        /// 验证不同代次句柄不会误操作缓存复用后的新会话。
        /// </summary>
        [Test]
        public void WindowHandle_DifferentGenerationIsNotEqual()
        {
            UIWindowId id = UIWindowId.FromGuid(Guid.NewGuid());
            UIWindowInstanceKey key = new UIWindowInstanceKey(42L);
            UIWindowHandle oldHandle = new UIWindowHandle(new UIWindowInstanceId(id, key, 1U));
            UIWindowHandle currentHandle = new UIWindowHandle(new UIWindowInstanceId(id, key, 2U));

            Assert.AreNotEqual(oldHandle, currentHandle);
            Assert.IsTrue(oldHandle.IsValid);
        }

        /// <summary>
        /// 验证固定状态机允许正常和提前关闭路径并拒绝生命周期倒退。
        /// </summary>
        [Test]
        public void StateMachine_AllowsDocumentedTransitionsOnly()
        {
            Assert.IsTrue(UIWindowStateMachine.CanTransition(UIWindowState.None, UIWindowState.Loading));
            Assert.IsTrue(UIWindowStateMachine.CanTransition(UIWindowState.Loading, UIWindowState.Closing));
            Assert.IsTrue(UIWindowStateMachine.CanTransition(UIWindowState.Closing, UIWindowState.Cached));
            Assert.IsFalse(UIWindowStateMachine.CanTransition(UIWindowState.Active, UIWindowState.Loading));
            Assert.IsFalse(UIWindowStateMachine.CanTransition(UIWindowState.Destroyed, UIWindowState.Active));
        }

        /// <summary>
        /// 验证安全区域像素矩形正确换算为锚点矩形。
        /// </summary>
        [Test]
        public void SafeArea_NormalizesAgainstFullPixelSize()
        {
            Rect normalized = UISafeAreaUtility.Normalize(new Rect(100f, 50f, 1800f, 900f), new Vector2(2000f, 1000f));

            Assert.AreEqual(0.05f, normalized.xMin, 0.0001f);
            Assert.AreEqual(0.95f, normalized.xMax, 0.0001f);
            Assert.AreEqual(0.05f, normalized.yMin, 0.0001f);
            Assert.AreEqual(0.95f, normalized.yMax, 0.0001f);
        }

        /// <summary>
        /// 验证持久 Root 只有两个根 Canvas，且窗口层直接使用无 Scaler 的嵌套 Canvas。
        /// </summary>
        [Test]
        public void ApplicationRootPrefab_HasDirectLayerTopology()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AssetRes/UI/Framework/ApplicationUIRoot.prefab");
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<ApplicationUIRoot>());
            Assert.AreEqual(1, prefab.GetComponentsInChildren<EventSystem>(true).Length);
            Assert.AreEqual(Enum.GetValues(typeof(UILayer)).Length * 2, prefab.GetComponentsInChildren<UILayerHost>(true).Length);
            Assert.IsNotNull(FindChild(prefab.transform, "OverlayRootCanvas"));
            Assert.IsNotNull(FindChild(prefab.transform, "CameraRootCanvas"));
            Assert.IsNull(FindChild(prefab.transform, "FullScreenRoot"));
            Assert.IsNull(FindChild(prefab.transform, "SafeAreaRoot"));
            Assert.IsNull(FindChild(prefab.transform, "WindowMount"));
            Assert.IsNull(FindChild(prefab.transform, "PoolRoot"));
            Assert.IsNull(FindChild(prefab.transform, "WorldSpace"));

            UILayerHost[] hosts = prefab.GetComponentsInChildren<UILayerHost>(true);
            for (int i = 0; i < hosts.Length; i++)
            {
                Assert.IsNotNull(hosts[i].GetComponent<Canvas>(), hosts[i].name);
                Assert.IsNull(hosts[i].GetComponent<CanvasScaler>(), hosts[i].name);
            }
        }

        /// <summary>
        /// 验证 Root 实例化后 Overlay 与 Camera 是根 Canvas，全部 Layer 是隔离重建的嵌套 Canvas。
        /// </summary>
        [Test]
        public void ApplicationRootPrefab_InstantiatesTwoRenderRoots()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AssetRes/UI/Framework/ApplicationUIRoot.prefab");
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                Canvas.ForceUpdateCanvases();
                Canvas[] canvases = instance.GetComponentsInChildren<Canvas>(true);
                int rootCanvasCount = 0;
                for (int i = 0; i < canvases.Length; i++)
                {
                    rootCanvasCount += canvases[i].isRootCanvas ? 1 : 0;
                }

                Assert.AreEqual(2, rootCanvasCount);
                UILayerHost[] hosts = instance.GetComponentsInChildren<UILayerHost>(true);
                for (int i = 0; i < hosts.Length; i++)
                {
                    Assert.IsFalse(hosts[i].GetComponent<Canvas>().isRootCanvas, hosts[i].name);
                    Assert.AreEqual(Vector3.one, hosts[i].transform.localScale, hosts[i].name);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        /// <summary>
        /// 验证两个根 CanvasScaler 使用默认设计配置，Layer 顺序与固定间隔完全一致。
        /// </summary>
        [Test]
        public void ApplicationRootPrefab_HasExpectedScalerAndLayerOrders()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AssetRes/UI/Framework/ApplicationUIRoot.prefab");
            CanvasScaler[] scalers = prefab.GetComponentsInChildren<CanvasScaler>(true);
            Assert.AreEqual(2, scalers.Length);
            for (int i = 0; i < scalers.Length; i++)
            {
                Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scalers[i].uiScaleMode);
                Assert.AreEqual(new Vector2(1920f, 1080f), scalers[i].referenceResolution);
                Assert.AreEqual(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight, scalers[i].screenMatchMode);
                Assert.AreEqual(0.5f, scalers[i].matchWidthOrHeight, 0.0001f);
                Assert.AreEqual(100f, scalers[i].referencePixelsPerUnit, 0.0001f);
            }

            Dictionary<UILayer, int> expectedOrders = new Dictionary<UILayer, int>
            {
                [UILayer.Background] = 0,
                [UILayer.Screen] = 100,
                [UILayer.Hud] = 200,
                [UILayer.Window] = 300,
                [UILayer.Popup] = 400,
                [UILayer.Tooltip] = 500,
                [UILayer.Toast] = 600,
                [UILayer.Guide] = 700,
                [UILayer.Drag] = 800,
                [UILayer.Transition] = 900,
                [UILayer.System] = 1000,
                [UILayer.Debug] = 1100
            };
            UILayerHost[] hosts = prefab.GetComponentsInChildren<UILayerHost>(true);
            for (int i = 0; i < hosts.Length; i++)
            {
                Assert.AreEqual(expectedOrders[hosts[i].Layer], hosts[i].SortingOrder, $"{hosts[i].RenderSpace}/{hosts[i].Layer}");
            }
        }

        /// <summary>
        /// 验证 Screen 模板只创建有明确职责的背景和安全内容节点。
        /// </summary>
        [Test]
        public void WindowTemplate_ScreenCreatesBoundContentRoot()
        {
            GameObject root = new GameObject("TemplateWindow", typeof(RectTransform), typeof(CanvasGroup));
            try
            {
                KcpTestWindowView view = root.AddComponent<KcpTestWindowView>();
                UIAuthoringUtility.CreateTemplateHierarchy(root, view, UIWindowTemplate.Screen);

                Assert.IsNotNull(FindChild(root.transform, "BackgroundRoot"));
                Assert.IsNotNull(FindChild(root.transform, "ContentRoot"));
                Assert.AreSame(FindChild(root.transform, "ContentRoot"), view.SafeAreaTarget);
                Assert.IsInstanceOf<UIPresetTransition>(view.GetTransitionDriver());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// 验证 Transition 原生 Preset 只复制动画参数，不覆盖窗口对象引用。
        /// </summary>
        [Test]
        public void TransitionPreset_PreservesTargetReferences()
        {
            Preset preset = AssetDatabase.LoadAssetAtPath<Preset>("Assets/Settings/MiniCore/UI/Presets/Transition_PopupScale.preset");
            Assert.IsNotNull(preset);
            GameObject root = new GameObject("TransitionWindow", typeof(RectTransform), typeof(CanvasGroup), typeof(UIPresetTransition));
            GameObject targetObject = new GameObject("PanelRoot", typeof(RectTransform));
            targetObject.transform.SetParent(root.transform, false);
            try
            {
                UIPresetTransition transition = root.GetComponent<UIPresetTransition>();
                CanvasGroup group = root.GetComponent<CanvasGroup>();
                RectTransform target = targetObject.GetComponent<RectTransform>();
                SerializedObject serialized = new SerializedObject(transition);
                serialized.FindProperty("target").objectReferenceValue = target;
                serialized.FindProperty("canvasGroup").objectReferenceValue = group;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.IsTrue(preset.ApplyTo(transition));
                serialized.Update();
                Assert.AreSame(target, serialized.FindProperty("target").objectReferenceValue);
                Assert.AreSame(group, serialized.FindProperty("canvasGroup").objectReferenceValue);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// 验证 Profile 不允许将项目默认安全区继续配置为 Inherit。
        /// </summary>
        [Test]
        public void ProjectProfile_RejectsInheritedDefaultSafeAreaPolicy()
        {
            UIProjectProfile profile = ScriptableObject.CreateInstance<UIProjectProfile>();
            try
            {
                SerializedObject serialized = new SerializedObject(profile);
                serialized.FindProperty("defaultSafeAreaPolicy").enumValueIndex = (int)UISafeAreaPolicy.Inherit;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.IsFalse(profile.Validate(out string error));
                StringAssert.Contains("不能继续使用 Inherit", error);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        /// <summary>
        /// 验证缓存 View 在最终 Layer 原地禁用，复用时返回同一对象且不改变父节点。
        /// </summary>
        [Test]
        public void UIService_CachesViewInFinalLayer()
        {
            GameObject layerObject = new GameObject("ScreenLayer", typeof(RectTransform));
            GameObject viewObject = new GameObject("CachedWindow", typeof(RectTransform), typeof(CanvasGroup), typeof(KcpTestWindowView));
            viewObject.transform.SetParent(layerObject.transform, false);
            KcpTestWindowView view = viewObject.GetComponent<KcpTestWindowView>();
            UIWindowDefinition definition = new UIWindowDefinition(
                UIWindowId.FromGuid(Guid.NewGuid()),
                typeof(KcpTestWindow),
                "CacheTestWindow",
                "CacheTestWindow",
                UIRenderSpace.ScreenSpaceOverlay,
                UILayer.Screen,
                UIInstancePolicy.Singleton,
                UIDuplicateOpenPolicy.Focus,
                UICachePolicy.CacheOnClose,
                UISafeAreaPolicy.Ignore,
                false,
                false,
                1,
                "Main",
                instance => instance.GetComponent<KcpTestWindowView>(),
                () => new KcpTestWindowPresenter());
            MiniCore.Service.UIService service = new MiniCore.Service.UIService();
            try
            {
                service.ReleaseView(definition, view);
                Assert.IsFalse(viewObject.activeSelf);
                Assert.AreSame(layerObject.transform, viewObject.transform.parent);

                AUIWindowView reused = service.AcquireViewAsync(definition).GetAwaiter().GetResult();
                Assert.AreSame(view, reused);
                Assert.AreSame(layerObject.transform, reused.transform.parent);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(viewObject);
                UnityEngine.Object.DestroyImmediate(layerObject);
            }
        }

        /// <summary>
        /// 验证 KCP 示例 View 内置配置与生成路由、直接注册表完全一致。
        /// </summary>
        [Test]
        public void GeneratedRegistry_MatchesWindowView()
        {
            Assert.IsTrue(UIWindowRegistryGenerator.Validate(out string error), error);
        }

        /// <summary>
        /// 验证 UI 框架源码不存在按 BatchMode 或 Dedicated Server 禁用 UI 的分支。
        /// </summary>
        [Test]
        public void UIFramework_DoesNotContainRunModeDisableBranch()
        {
            string[] files = Directory.GetFiles(Path.GetFullPath("Assets/Scripts/MiniCore/Unity/UI"), "*.cs", SearchOption.AllDirectories);
            string[] hotUpdateFiles = Directory.GetFiles(Path.GetFullPath("Assets/Scripts/MiniCore/HotUpdate/UI"), "*.cs", SearchOption.AllDirectories);
            AssertNoRunModeDisable(files);
            AssertNoRunModeDisable(hotUpdateFiles);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 深度优先查找指定名称子节点。
        /// </summary>
        /// <param name="root">当前节点。</param>
        /// <param name="name">目标名称。</param>
        /// <returns>匹配节点；未找到时返回 null。</returns>
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
        /// 验证一组 UI 源码不包含框架级运行形态裁剪判断。
        /// </summary>
        /// <param name="files">待检查 C# 文件。</param>
        private static void AssertNoRunModeDisable(string[] files)
        {
            for (int i = 0; i < files.Length; i++)
            {
                string content = File.ReadAllText(files[i]);
                Assert.IsFalse(content.Contains("Application.isBatchMode"), files[i]);
                Assert.IsFalse(content.Contains("UNITY_SERVER"), files[i]);
            }
        }

        #endregion
    }
}
