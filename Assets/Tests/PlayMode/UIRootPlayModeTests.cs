using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MiniCore.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MiniCore.PlayModeTests
{
    /// <summary>
    /// 验证运行时 Root 的 EventSystem 接管、渲染根和直接 Layer 父节点行为。
    /// </summary>
    public sealed class UIRootPlayModeTests
    {
        #region Public 公共成员

        /// <summary>
        /// 验证 Root 初始化后禁用重复 EventSystem，销毁后恢复并返回直接 Layer 节点。
        /// </summary>
        /// <returns>跨帧销毁验证迭代器。</returns>
        [UnityTest]
        public IEnumerator Root_ClaimsEventSystemAndReturnsDirectLayer()
        {
            GameObject duplicateObject = new GameObject("SceneEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            GameObject rootObject = CreateRoot(out ApplicationUIRoot root, out RectTransform overlayLayer);
            UIProjectProfile profile = ScriptableObject.CreateInstance<UIProjectProfile>();
            root.Initialize(profile);
            yield return null;

            Assert.IsFalse(duplicateObject.GetComponent<EventSystem>().enabled);
            Assert.AreSame(overlayLayer, root.GetWindowParent(UIRenderSpace.ScreenSpaceOverlay, UILayer.Screen));
            Assert.IsNull(overlayLayer.GetComponent<CanvasScaler>());
            Assert.IsFalse(overlayLayer.GetComponent<Canvas>().isRootCanvas);

            Object.Destroy(rootObject);
            yield return null;
            Assert.IsTrue(duplicateObject.GetComponent<EventSystem>().enabled);

            Object.Destroy(duplicateObject);
            Object.Destroy(profile);
        }

        /// <summary>
        /// 验证误放进场景 Canvas 的 Root 会在初始化时提升为场景根节点。
        /// </summary>
        /// <returns>等待 Root 初始化和销毁的跨帧迭代器。</returns>
        [UnityTest]
        public IEnumerator Root_DetachesFromCanvasParentOnInitialize()
        {
            GameObject canvasParent = new GameObject("SceneCanvas", typeof(RectTransform), typeof(Canvas));
            GameObject rootObject = CreateRoot(out ApplicationUIRoot root, out _);
            rootObject.transform.SetParent(canvasParent.transform, false);
            UIProjectProfile profile = ScriptableObject.CreateInstance<UIProjectProfile>();
            root.Initialize(profile);
            yield return null;

            Assert.IsNull(rootObject.transform.parent);
            Assert.IsTrue(root.OverlayRootCanvas.isRootCanvas);
            Assert.IsTrue(root.CameraRootCanvas.isRootCanvas);

            Object.Destroy(rootObject);
            Object.Destroy(canvasParent);
            Object.Destroy(profile);
        }

        /// <summary>
        /// 验证 Content、Window 和 Ignore 三种安全区策略只修改各自约定的目标。
        /// </summary>
        /// <returns>等待测试对象销毁的跨帧迭代器。</returns>
        [UnityTest]
        public IEnumerator WindowView_AppliesSafeAreaToConfiguredTargetOnly()
        {
            GameObject serviceObject = new GameObject("ResolutionService", typeof(UIResolutionService));
            UIResolutionService service = serviceObject.GetComponent<UIResolutionService>();
            UIProjectProfile profile = ScriptableObject.CreateInstance<UIProjectProfile>();
            service.Initialize(profile);

            GameObject viewObject = new GameObject("SafeAreaWindow", typeof(RectTransform), typeof(CanvasGroup), typeof(UIRootTestWindowView));
            RectTransform viewRect = (RectTransform)viewObject.transform;
            GameObject contentObject = new GameObject("ContentRoot", typeof(RectTransform));
            RectTransform contentRect = (RectTransform)contentObject.transform;
            contentRect.SetParent(viewRect, false);
            UIRootTestWindowView view = viewObject.GetComponent<UIRootTestWindowView>();
            FieldInfo targetField = typeof(AUIWindowView).GetField("safeAreaTarget", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(targetField);
            targetField.SetValue(view, contentRect);

            Rect normalized = UISafeAreaUtility.Normalize(service.Current.SafeArea, service.Current.PixelSize);
            view.BindSafeArea(service, UISafeAreaPolicy.ConstrainContent);
            Assert.AreEqual(normalized.min, contentRect.anchorMin);
            Assert.AreEqual(normalized.max, contentRect.anchorMax);

            contentRect.anchorMin = new Vector2(0.2f, 0.25f);
            contentRect.anchorMax = new Vector2(0.8f, 0.75f);
            view.BindSafeArea(service, UISafeAreaPolicy.ConstrainWindow);
            Assert.AreEqual(normalized.min, viewRect.anchorMin);
            Assert.AreEqual(normalized.max, viewRect.anchorMax);
            Assert.AreEqual(new Vector2(0.2f, 0.25f), contentRect.anchorMin);
            Assert.AreEqual(new Vector2(0.8f, 0.75f), contentRect.anchorMax);

            viewRect.anchorMin = new Vector2(0.1f, 0.15f);
            viewRect.anchorMax = new Vector2(0.9f, 0.85f);
            view.BindSafeArea(service, UISafeAreaPolicy.Ignore);
            Assert.AreEqual(new Vector2(0.1f, 0.15f), viewRect.anchorMin);
            Assert.AreEqual(new Vector2(0.9f, 0.85f), viewRect.anchorMax);

            Object.Destroy(viewObject);
            Object.Destroy(serviceObject);
            Object.Destroy(profile);
            yield return null;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 创建 PlayMode 测试所需的最小完整 ApplicationUIRoot。
        /// </summary>
        /// <param name="root">创建的 Root 组件。</param>
        /// <param name="overlayLayer">Overlay Screen 层。</param>
        /// <returns>Root GameObject。</returns>
        private static GameObject CreateRoot(out ApplicationUIRoot root, out RectTransform overlayLayer)
        {
            GameObject rootObject = new GameObject("ApplicationUIRootTest", typeof(RectTransform), typeof(UIResolutionService), typeof(UISafeAreaService), typeof(ApplicationUIRoot));
            GameObject eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventObject.transform.SetParent(rootObject.transform, false);

            Canvas overlayCanvas = CreateRootCanvas("OverlayRootCanvas", rootObject.transform, RenderMode.ScreenSpaceOverlay, null);
            UILayerHost overlayHost = CreateLayer("ScreenLayer", overlayCanvas.transform, UIRenderSpace.ScreenSpaceOverlay, UILayer.Screen);
            overlayLayer = overlayHost.Root;

            GameObject cameraObject = new GameObject("UICamera", typeof(Camera));
            cameraObject.transform.SetParent(rootObject.transform, false);
            Camera camera = cameraObject.GetComponent<Camera>();
            Canvas cameraCanvas = CreateRootCanvas("CameraRootCanvas", rootObject.transform, RenderMode.ScreenSpaceCamera, camera);
            UILayerHost cameraHost = CreateLayer("ScreenLayer", cameraCanvas.transform, UIRenderSpace.ScreenSpaceCamera, UILayer.Screen);

            root = rootObject.GetComponent<ApplicationUIRoot>();
            root.Configure(
                rootObject.GetComponent<UIResolutionService>(),
                rootObject.GetComponent<UISafeAreaService>(),
                eventObject.GetComponent<EventSystem>(),
                overlayCanvas,
                cameraCanvas,
                null,
                new List<UILayerHost> { overlayHost, cameraHost });
            return rootObject;
        }

        /// <summary>
        /// 创建一个测试用根 Canvas 和唯一 CanvasScaler。
        /// </summary>
        /// <param name="name">根节点名称。</param>
        /// <param name="parent">父节点。</param>
        /// <param name="mode">渲染模式。</param>
        /// <param name="camera">Camera 模式使用的相机。</param>
        /// <returns>创建的根 Canvas。</returns>
        private static Canvas CreateRootCanvas(string name, Transform parent, RenderMode mode, Camera camera)
        {
            GameObject value = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            RectTransform rect = (RectTransform)value.transform;
            rect.SetParent(parent, false);
            Stretch(rect);
            Canvas canvas = value.GetComponent<Canvas>();
            canvas.renderMode = mode;
            canvas.worldCamera = camera;
            return canvas;
        }

        /// <summary>
        /// 创建一个直接承载窗口的嵌套 Layer Canvas。
        /// </summary>
        /// <param name="name">节点名称。</param>
        /// <param name="parent">渲染根。</param>
        /// <param name="renderSpace">渲染空间。</param>
        /// <param name="layer">逻辑层。</param>
        /// <returns>创建的 Layer Host。</returns>
        private static UILayerHost CreateLayer(string name, Transform parent, UIRenderSpace renderSpace, UILayer layer)
        {
            GameObject value = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(UILayerHost));
            RectTransform rect = (RectTransform)value.transform;
            rect.SetParent(parent, false);
            Stretch(rect);
            UILayerHost host = value.GetComponent<UILayerHost>();
            host.Configure(renderSpace, layer, 100);
            return host;
        }

        /// <summary>
        /// 将 RectTransform 设置为全拉伸。
        /// </summary>
        /// <param name="rect">目标节点。</param>
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        #endregion
    }

    /// <summary>
    /// PlayMode 安全区测试使用的最小窗口 View。
    /// </summary>
    public sealed class UIRootTestWindowView : AUIWindowView
    {
    }
}
