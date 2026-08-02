using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiniCore.UI
{
    /// <summary>
    /// 当前屏幕和安全区域使用的无分配布局指标。
    /// </summary>
    public readonly struct UIResolutionMetrics
    {
        #region Public 公共成员

        /// <summary>
        /// 获取屏幕像素尺寸。
        /// </summary>
        public Vector2 PixelSize { get; }

        /// <summary>
        /// 获取安全区域像素矩形。
        /// </summary>
        public Rect SafeArea { get; }

        /// <summary>
        /// 获取当前屏幕宽高比。
        /// </summary>
        public float AspectRatio { get; }

        /// <summary>
        /// 判断当前是否为竖屏。
        /// </summary>
        public bool Portrait { get; }

        /// <summary>
        /// 获取当前响应式断点名称。
        /// </summary>
        public string Breakpoint { get; }

        /// <summary>
        /// 创建一份不可变分辨率指标。
        /// </summary>
        /// <param name="pixelSize">屏幕像素尺寸。</param>
        /// <param name="safeArea">安全区域。</param>
        /// <param name="aspectRatio">宽高比。</param>
        /// <param name="portrait">是否竖屏。</param>
        /// <param name="breakpoint">响应式断点。</param>
        public UIResolutionMetrics(Vector2 pixelSize, Rect safeArea, float aspectRatio, bool portrait, string breakpoint)
        {
            PixelSize = pixelSize;
            SafeArea = safeArea;
            AspectRatio = aspectRatio;
            Portrait = portrait;
            Breakpoint = breakpoint;
        }

        #endregion
    }

    /// <summary>
    /// 只在屏幕、安全区域或响应式断点变化时广播布局指标。
    /// </summary>
    public sealed partial class UIResolutionService : MonoBehaviour
    {
        #region Private 私有成员

        private UIProjectProfile profile; // 当前项目 Profile。
        private int lastWidth = -1; // 上次屏幕宽度。
        private int lastHeight = -1; // 上次屏幕高度。
        private Rect lastSafeArea; // 上次安全区域。
        private UIResolutionMetrics current; // 当前布局指标。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 分辨率、安全区域或断点发生变化时触发。
        /// </summary>
        public event Action<UIResolutionMetrics> Changed;

        /// <summary>
        /// 获取最近一次计算结果。
        /// </summary>
        public UIResolutionMetrics Current => current;

        /// <summary>
        /// 使用项目 Profile 初始化并立即计算布局指标。
        /// </summary>
        /// <param name="projectProfile">当前项目 UI Profile。</param>
        public void Initialize(UIProjectProfile projectProfile)
        {
            profile = projectProfile ?? throw new ArgumentNullException(nameof(projectProfile));
            Refresh(true);
        }

        /// <summary>
        /// 强制重新计算并广播当前布局指标。
        /// </summary>
        public void Refresh()
        {
            Refresh(true);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 每帧只比较值类型屏幕指标，未变化时不执行布局计算。
        /// </summary>
        private void Update()
        {
            Refresh(false);
        }

        /// <summary>
        /// 在输入值变化或强制请求时重新计算布局指标。
        /// </summary>
        /// <param name="force">是否忽略缓存强制广播。</param>
        private void Refresh(bool force)
        {
            if (profile == null)
            {
                return;
            }

            int width = Mathf.Max(1, Screen.width);
            int height = Mathf.Max(1, Screen.height);
            Rect safeArea = Screen.safeArea;
            if (!force && width == lastWidth && height == lastHeight && safeArea == lastSafeArea)
            {
                return;
            }

            lastWidth = width;
            lastHeight = height;
            lastSafeArea = safeArea;
            float aspectRatio = width / (float)height;
            bool portrait = height > width;
            current = new UIResolutionMetrics(new Vector2(width, height), safeArea, aspectRatio, portrait, profile.ResolveBreakpoint(aspectRatio, portrait));
            Changed?.Invoke(current);
        }

        #endregion
    }

    /// <summary>
    /// ApplicationUIRoot 中一个固定渲染空间和排序的 Canvas 层。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(Canvas))]
    public sealed partial class UILayerHost : MonoBehaviour
    {
        #region UnityProperty Unity 引用属性

        [SerializeField] private Canvas canvas; // 当前层 Canvas。

        #endregion

        #region Private 私有成员

        [SerializeField] private UIRenderSpace renderSpace; // 当前渲染空间。
        [SerializeField] private UILayer layer; // 当前逻辑层。
        [SerializeField] private int sortingOrder; // 当前 Canvas 排序值。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取当前渲染空间。
        /// </summary>
        public UIRenderSpace RenderSpace => renderSpace;

        /// <summary>
        /// 获取当前逻辑层。
        /// </summary>
        public UILayer Layer => layer;

        /// <summary>
        /// 获取当前排序值。
        /// </summary>
        public int SortingOrder => sortingOrder;

        /// <summary>
        /// 获取窗口直接挂载的当前层 RectTransform。
        /// </summary>
        public RectTransform Root => transform as RectTransform;

        /// <summary>
        /// 为编辑器生成器配置渲染空间、逻辑层和排序。
        /// </summary>
        /// <param name="space">渲染空间。</param>
        /// <param name="layerValue">逻辑层。</param>
        /// <param name="order">Canvas 排序值。</param>
        public void Configure(UIRenderSpace space, UILayer layerValue, int order)
        {
            renderSpace = space;
            layer = layerValue;
            sortingOrder = order;
            ApplyCanvasSettings();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 在 Unity 校验阶段同步当前层排序配置。
        /// </summary>
        private void OnValidate()
        {
            ApplyCanvasSettings();
        }

        /// <summary>
        /// 惰性获取 Canvas 并应用嵌套层排序。
        /// </summary>
        private void ApplyCanvasSettings()
        {
            canvas ??= GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
        }

        #endregion
    }

    /// <summary>
    /// 持久化的全局 UI Root，统一管理渲染根、窗口层级、分辨率和 EventSystem。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIResolutionService), typeof(UISafeAreaService))]
    public sealed class ApplicationUIRoot : MonoBehaviour
    {
        #region UnityProperty Unity 引用属性

        [SerializeField] private UIResolutionService resolutionService; // 分辨率服务。
        [SerializeField] private UISafeAreaService safeAreaService; // 安全区域服务。
        [SerializeField] private EventSystem eventSystem; // 框架拥有的 EventSystem。
        [SerializeField] private Canvas overlayRootCanvas; // Screen Space Overlay 根 Canvas。
        [SerializeField] private Canvas cameraRootCanvas; // Screen Space Camera 根 Canvas。
        [SerializeField] private UILoadingOverlay loadingOverlay; // 全局延迟 Loading 反馈。
        [SerializeField] private List<UILayerHost> layerHosts = new List<UILayerHost>(); // 全部渲染空间固定层。

        #endregion

        #region Private 私有成员

        private readonly Dictionary<int, UILayerHost> layerMap = new Dictionary<int, UILayerHost>(); // 渲染空间和层到宿主映射。
        private readonly List<EventSystem> disabledEventSystems = new List<EventSystem>(); // 被框架暂时禁用的场景 EventSystem。
        private bool initialized; // Root 是否完成初始化。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取统一分辨率服务。
        /// </summary>
        public UIResolutionService ResolutionService => resolutionService;

        /// <summary>
        /// 获取设备安全区域服务。
        /// </summary>
        public UISafeAreaService SafeAreaService => safeAreaService;

        /// <summary>
        /// 获取 Screen Space Overlay 根 Canvas。
        /// </summary>
        public Canvas OverlayRootCanvas => overlayRootCanvas;

        /// <summary>
        /// 获取 Screen Space Camera 根 Canvas。
        /// </summary>
        public Canvas CameraRootCanvas => cameraRootCanvas;

        /// <summary>
        /// 获取全局加载反馈组件。
        /// </summary>
        public UILoadingOverlay LoadingOverlay => loadingOverlay;

        /// <summary>
        /// 初始化持久 Root、固定 Canvas 层和唯一 EventSystem。
        /// </summary>
        /// <param name="profile">当前项目 UI Profile。</param>
        public void Initialize(UIProjectProfile profile)
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            DetachFromCanvasParent();
            DontDestroyOnLoad(gameObject);
            resolutionService ??= GetComponent<UIResolutionService>();
            safeAreaService ??= GetComponent<UISafeAreaService>();
            RebuildLayerMap();
            TakeEventSystemOwnership();
            loadingOverlay?.Initialize(profile);
            resolutionService.Initialize(profile);
            safeAreaService.Initialize(resolutionService);
        }

        /// <summary>
        /// 根据渲染空间和逻辑层返回窗口直接父节点。
        /// </summary>
        /// <param name="renderSpace">目标渲染空间。</param>
        /// <param name="layer">目标逻辑层。</param>
        /// <returns>承载窗口的层 RectTransform。</returns>
        public RectTransform GetWindowParent(UIRenderSpace renderSpace, UILayer layer)
        {
            if (!layerMap.TryGetValue(GetLayerKey(renderSpace, layer), out UILayerHost host))
            {
                throw new InvalidOperationException($"ApplicationUIRoot 未配置 {renderSpace}/{layer} 层。");
            }

            return host.Root;
        }

        /// <summary>
        /// 为编辑器生成的 Root 配置运行时节点引用。
        /// </summary>
        /// <param name="resolution">分辨率服务。</param>
        /// <param name="safeArea">安全区域服务。</param>
        /// <param name="ownedEventSystem">框架 EventSystem。</param>
        /// <param name="overlayCanvas">Overlay 根 Canvas。</param>
        /// <param name="cameraCanvas">Camera 根 Canvas。</param>
        /// <param name="overlay">全局 Loading 反馈。</param>
        /// <param name="hosts">全部渲染空间固定层。</param>
        public void Configure(
            UIResolutionService resolution,
            UISafeAreaService safeArea,
            EventSystem ownedEventSystem,
            Canvas overlayCanvas,
            Canvas cameraCanvas,
            UILoadingOverlay overlay,
            List<UILayerHost> hosts)
        {
            resolutionService = resolution;
            safeAreaService = safeArea;
            eventSystem = ownedEventSystem;
            overlayRootCanvas = overlayCanvas;
            cameraRootCanvas = cameraCanvas;
            loadingOverlay = overlay;
            layerHosts = hosts ?? new List<UILayerHost>();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 将渲染空间和逻辑层压缩为无分配字典键。
        /// </summary>
        /// <param name="renderSpace">渲染空间。</param>
        /// <param name="layer">逻辑层。</param>
        /// <returns>稳定字典键。</returns>
        private static int GetLayerKey(UIRenderSpace renderSpace, UILayer layer)
        {
            return ((int)renderSpace << 16) | ((int)layer & 0xFFFF);
        }

        /// <summary>
        /// 从序列化层列表重建运行时快速查找表。
        /// </summary>
        private void RebuildLayerMap()
        {
            layerMap.Clear();
            for (int i = 0; i < layerHosts.Count; i++)
            {
                UILayerHost host = layerHosts[i];
                if (host == null)
                {
                    continue;
                }

                int key = GetLayerKey(host.RenderSpace, host.Layer);
                if (!layerMap.ContainsKey(key))
                {
                    layerMap.Add(key, host);
                }
            }
        }

        /// <summary>
        /// 将错误嵌入其他 Canvas 的 Root 提升为场景根节点，确保两个渲染根保持独立。
        /// </summary>
        private void DetachFromCanvasParent()
        {
            if (transform.parent != null && transform.parent.GetComponentInParent<Canvas>() != null)
            {
                transform.SetParent(null, false);
            }
        }

        /// <summary>
        /// Root 销毁时恢复此前被禁用的外部 EventSystem。
        /// </summary>
        private void OnDestroy()
        {
            for (int i = 0; i < disabledEventSystems.Count; i++)
            {
                EventSystem disabled = disabledEventSystems[i];
                if (disabled != null)
                {
                    disabled.enabled = true;
                }
            }

            disabledEventSystems.Clear();
        }

        /// <summary>
        /// 启用框架 EventSystem 并停用场景中的重复实例。
        /// </summary>
        private void TakeEventSystemOwnership()
        {
            eventSystem ??= GetComponentInChildren<EventSystem>(true);
            if (eventSystem == null)
            {
                throw new InvalidOperationException("ApplicationUIRoot 缺少 EventSystem。");
            }

            EventSystem[] systems = FindObjectsOfType<EventSystem>();
            for (int i = 0; i < systems.Length; i++)
            {
                EventSystem candidate = systems[i];
                if (candidate == null || candidate == eventSystem)
                {
                    continue;
                }

                candidate.enabled = false;
                disabledEventSystems.Add(candidate);
            }

            eventSystem.enabled = true;
        }

        #endregion
    }
}
