using System;
using System.Collections.Generic;
using MiniCore.Eventing;
using MiniCore.Threading;
using MiniCore.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MiniCore.UI
{

    /// <summary>
    /// 拥有独立激活任务域并可安全进入缓存池的窗口 View 基类。
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public abstract class AUIWindowView : AMTaskBehaviour
    {
        #region UnityProperty Unity 引用属性

        [SerializeField] private MonoBehaviour transitionDriver; // 可选窗口动画驱动。
        [SerializeField] private RectTransform safeAreaTarget; // ConstrainContent 使用的交互内容根节点。
        private CanvasGroup canvasGroup; // 窗口根 CanvasGroup。

        #endregion

        #region Private 私有成员

        [SerializeField, HideInInspector] private string windowId; // 编辑器生成的稳定 128 位身份。
        [SerializeField] private string routeName; // 生成强类型路由使用的稳定名称。
        [SerializeField, HideInInspector] private string logicTypeName; // Presenter 程序集限定名。
        [SerializeField, HideInInspector] private string assetAddress; // YooAsset 运行时地址。
        [SerializeField] private UIWindowTemplate template = UIWindowTemplate.Screen; // 窗口模板。
        [SerializeField] private UIRenderSpace renderSpace = UIRenderSpace.ScreenSpaceOverlay; // 渲染空间。
        [SerializeField] private UILayer layer = UILayer.Screen; // 逻辑显示层。
        [SerializeField] private UIInstancePolicy instancePolicy = UIInstancePolicy.Singleton; // 实例策略。
        [SerializeField] private UIDuplicateOpenPolicy duplicateOpenPolicy = UIDuplicateOpenPolicy.Focus; // 重复打开策略。
        [SerializeField] private UICachePolicy cachePolicy = UICachePolicy.CacheOnClose; // View 缓存策略。
        [SerializeField] private UISafeAreaPolicy safeAreaPolicy = UISafeAreaPolicy.ConstrainContent; // 安全区域策略。
        [SerializeField] private bool modal; // 是否阻断下层输入。
        [SerializeField] private bool closeOnMaskClick; // 点击遮罩是否关闭。
        [SerializeField, Min(0)] private int maxCacheCount = 1; // 最大缓存 View 数量。
        [SerializeField] private string navigationGroup = "Main"; // Screen 导航组。
        private MTaskDomain activationDomain; // 当前打开周期的任务域。
        private UIResolutionService safeAreaService; // 当前窗口绑定的分辨率服务。
        private UISafeAreaPolicy effectiveSafeAreaPolicy; // 当前会话解析后的安全区策略。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取窗口根 CanvasGroup。
        /// </summary>
        public CanvasGroup CanvasGroup => canvasGroup ??= GetComponent<CanvasGroup>();

        /// <summary>
        /// 获取序列化窗口身份文本。
        /// </summary>
        public string WindowIdText => windowId;

        /// <summary>
        /// 获取稳定路由名称。
        /// </summary>
        public string RouteName => routeName;

        /// <summary>
        /// 获取逻辑类型程序集限定名。
        /// </summary>
        public string LogicTypeName => logicTypeName;

        /// <summary>
        /// 获取 YooAsset 地址。
        /// </summary>
        public string AssetAddress => assetAddress;

        /// <summary>
        /// 获取窗口模板。
        /// </summary>
        public UIWindowTemplate Template => template;

        /// <summary>
        /// 获取渲染空间。
        /// </summary>
        public UIRenderSpace RenderSpace => renderSpace;

        /// <summary>
        /// 获取逻辑显示层。
        /// </summary>
        public UILayer Layer => layer;

        /// <summary>
        /// 获取实例策略。
        /// </summary>
        public UIInstancePolicy InstancePolicy => instancePolicy;

        /// <summary>
        /// 获取重复打开策略。
        /// </summary>
        public UIDuplicateOpenPolicy DuplicateOpenPolicy => duplicateOpenPolicy;

        /// <summary>
        /// 获取缓存策略。
        /// </summary>
        public UICachePolicy CachePolicy => cachePolicy;

        /// <summary>
        /// 获取安全区域策略。
        /// </summary>
        public UISafeAreaPolicy SafeAreaPolicy => safeAreaPolicy;

        /// <summary>
        /// 获取序列化的可选动画组件，用于编辑器校验其接口类型。
        /// </summary>
        public MonoBehaviour TransitionDriver => transitionDriver;

        /// <summary>
        /// 获取 ConstrainContent 使用的安全区目标。
        /// </summary>
        public RectTransform SafeAreaTarget => safeAreaTarget;

        /// <summary>
        /// 判断窗口是否为模态窗口。
        /// </summary>
        public bool Modal => modal;

        /// <summary>
        /// 判断点击模态遮罩是否关闭窗口。
        /// </summary>
        public bool CloseOnMaskClick => closeOnMaskClick;

        /// <summary>
        /// 获取最大缓存 View 数量。
        /// </summary>
        public int MaxCacheCount => maxCacheCount;

        /// <summary>
        /// 获取导航组名称。
        /// </summary>
        public string NavigationGroup => navigationGroup;

        /// <summary>
        /// 解析并返回稳定窗口身份。
        /// </summary>
        /// <returns>窗口身份；文本非法时返回空身份。</returns>
        public UIWindowId GetWindowId()
        {
            return Guid.TryParse(windowId, out Guid value) ? UIWindowId.FromGuid(value) : default;
        }

        /// <summary>
        /// 获取 Prefab 指定的可选动画驱动。
        /// </summary>
        /// <returns>实现动画接口的组件；未配置时返回 null。</returns>
        public IUITransitionDriver GetTransitionDriver()
        {
            return transitionDriver as IUITransitionDriver;
        }

        /// <summary>
        /// 绑定安全区域服务并立即应用当前窗口策略。
        /// </summary>
        /// <param name="service">ApplicationUIRoot 分辨率服务。</param>
        /// <param name="policy">已解析 Inherit 的最终安全区策略。</param>
        public void BindSafeArea(UIResolutionService service, UISafeAreaPolicy policy)
        {
            UnbindSafeArea();
            if (policy == UISafeAreaPolicy.Inherit)
            {
                throw new ArgumentException("窗口绑定安全区域前必须先解析 Inherit 策略。", nameof(policy));
            }

            effectiveSafeAreaPolicy = policy;
            if (policy == UISafeAreaPolicy.Ignore || policy == UISafeAreaPolicy.Custom)
            {
                return;
            }

            if (policy == UISafeAreaPolicy.ConstrainContent && safeAreaTarget == null)
            {
                throw new InvalidOperationException($"窗口 {name} 使用 ConstrainContent，但未配置 SafeAreaTarget。");
            }

            safeAreaService = service ?? throw new ArgumentNullException(nameof(service));
            safeAreaService.Changed += ApplySafeArea;
            ApplySafeArea(safeAreaService.Current);
        }

        /// <summary>
        /// 解除当前安全区域服务订阅。
        /// </summary>
        public void UnbindSafeArea()
        {
            if (safeAreaService != null)
            {
                safeAreaService.Changed -= ApplySafeArea;
                safeAreaService = null;
            }
        }

        /// <summary>
        /// 获取当前打开周期使用的任务域。
        /// </summary>
        /// <returns>窗口激活域；尚未打开时返回对象生命周期域。</returns>
        public override MTaskDomain GetMTaskDomain() => activationDomain ?? base.GetMTaskDomain();

        /// <summary>
        /// 为新的打开周期创建任务域，将窗口根恢复为全 Layer 拉伸并恢复基本交互状态。
        /// </summary>
        public void PrepareForOpen()
        {
            activationDomain?.Dispose();
            activationDomain = new MTaskDomain($"{GetType().FullName}.Activation", MTaskExecutors.Unity);
            RectTransform rectTransform = (RectTransform)transform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            CanvasGroup.alpha = 1f;
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;
            OnPrepareForOpen();
        }

        /// <summary>
        /// 执行派生 View 的打开逻辑。
        /// </summary>
        /// <returns>View 打开任务。</returns>
        public MTask OpenAsync() => OnOpenAsync();

        /// <summary>
        /// 执行派生 View 的关闭逻辑并终止激活任务域。
        /// </summary>
        /// <returns>View 关闭任务。</returns>
        public MTask CloseAsync() => CloseActivationAsync();

        /// <summary>
        /// 将 View 恢复为可安全放入缓存池的状态。
        /// </summary>
        public void ResetForPool()
        {
            CanvasGroup.alpha = 1f;
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;
            OnResetForPool();
        }

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 在每次打开前同步恢复派生 View 状态。
        /// </summary>
        protected virtual void OnPrepareForOpen()
        {
        }

        /// <summary>
        /// 执行派生 View 的异步打开逻辑。
        /// </summary>
        /// <returns>打开完成任务。</returns>
        protected virtual MTask OnOpenAsync() => MTask.CompletedTask;

        /// <summary>
        /// 执行派生 View 的异步关闭逻辑。
        /// </summary>
        /// <returns>关闭完成任务。</returns>
        protected virtual MTask OnCloseAsync() => MTask.CompletedTask;

        /// <summary>
        /// 在进入缓存池前清理 View 自身临时显示状态。
        /// </summary>
        protected virtual void OnResetForPool()
        {
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 销毁 View 时终止激活任务域和对象任务域。
        /// </summary>
        protected override void OnDestroy()
        {
            UnbindSafeArea();
            activationDomain?.Dispose();
            activationDomain = null;
            base.OnDestroy();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 在编辑器校验阶段补齐稳定身份和默认路由。
        /// </summary>
        private void OnValidate()
        {
            if (!Guid.TryParse(windowId, out _))
            {
                windowId = Guid.NewGuid().ToString("N");
            }

            if (string.IsNullOrWhiteSpace(routeName))
            {
                routeName = gameObject.name;
            }
        }

        /// <summary>
        /// 将安全区域指标应用到窗口根或 ContentRoot。
        /// </summary>
        /// <param name="metrics">最新分辨率指标。</param>
        private void ApplySafeArea(UIResolutionMetrics metrics)
        {
            RectTransform target = effectiveSafeAreaPolicy == UISafeAreaPolicy.ConstrainWindow ? transform as RectTransform : safeAreaTarget;
            UISafeAreaUtility.Apply(target, metrics);
        }

        /// <summary>
        /// 等待关闭钩子完成并保证激活任务域被释放。
        /// </summary>
        /// <returns>完整关闭任务。</returns>
        private async MTask CloseActivationAsync()
        {
            try
            {
                await OnCloseAsync();
            }
            finally
            {
                activationDomain?.Dispose();
                activationDomain = null;
            }
        }

        #endregion
    }
}
