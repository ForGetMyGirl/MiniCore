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
    /// 保存一次窗口打开周期中的 UI 控件与事件解绑操作。
    /// </summary>
    public sealed class UIBindingSet : IDisposable
    {
        #region Private 私有成员

        private readonly List<Action> removers = new List<Action>(8); // 关闭窗口时顺序执行的解绑动作。
        private readonly List<EventSubscription> subscriptions = new List<EventSubscription>(4); // 强类型事件订阅。
        private bool disposed; // 当前绑定集合是否已经释放。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 登记 Button 点击监听并在释放时自动移除。
        /// </summary>
        /// <param name="button">目标按钮。</param>
        /// <param name="listener">命名点击监听器。</param>
        public void Add(Button button, UnityAction listener)
        {
            ThrowIfDisposed();
            if (button == null || listener == null)
            {
                return;
            }

            button.onClick.AddListener(listener);
            removers.Add(() => button.onClick.RemoveListener(listener));
        }

        /// <summary>
        /// 登记 Toggle 值变化监听并在释放时自动移除。
        /// </summary>
        /// <param name="toggle">目标开关。</param>
        /// <param name="listener">命名值变化监听器。</param>
        public void Add(Toggle toggle, UnityAction<bool> listener)
        {
            ThrowIfDisposed();
            if (toggle == null || listener == null)
            {
                return;
            }

            toggle.onValueChanged.AddListener(listener);
            removers.Add(() => toggle.onValueChanged.RemoveListener(listener));
        }

        /// <summary>
        /// 登记 TMP 输入框值变化监听并在释放时自动移除。
        /// </summary>
        /// <param name="input">目标输入框。</param>
        /// <param name="listener">命名值变化监听器。</param>
        public void Add(TMP_InputField input, UnityAction<string> listener)
        {
            ThrowIfDisposed();
            if (input == null || listener == null)
            {
                return;
            }

            input.onValueChanged.AddListener(listener);
            removers.Add(() => input.onValueChanged.RemoveListener(listener));
        }

        /// <summary>
        /// 登记强类型事件订阅并在释放时自动解除。
        /// </summary>
        /// <param name="subscription">事件订阅 token。</param>
        public void Add(EventSubscription subscription)
        {
            ThrowIfDisposed();
            subscriptions.Add(subscription);
        }

        /// <summary>
        /// 登记自定义解绑动作。
        /// </summary>
        /// <param name="remove">关闭窗口时执行的无异常解绑动作。</param>
        public void Add(Action remove)
        {
            ThrowIfDisposed();
            if (remove != null)
            {
                removers.Add(remove);
            }
        }

        /// <summary>
        /// 解除全部 UI 与强类型事件绑定。
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            for (int i = removers.Count - 1; i >= 0; i--)
            {
                removers[i]?.Invoke();
            }

            for (int i = subscriptions.Count - 1; i >= 0; i--)
            {
                subscriptions[i].Dispose();
            }

            removers.Clear();
            subscriptions.Clear();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 阻止已释放绑定集合继续登记监听。
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(UIBindingSet));
            }
        }

        #endregion
    }

    /// <summary>
    /// Presenter 或 ViewModel 在一次窗口会话中可访问的最小上下文。
    /// </summary>
    public sealed class UIWindowContext
    {
        #region Private 私有成员

        private readonly Action<object> submitResult; // 向当前会话提交强类型结果的入口。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取当前窗口句柄。
        /// </summary>
        public UIWindowHandle Handle { get; }

        /// <summary>
        /// 获取本次打开参数；无参数窗口返回 null。
        /// </summary>
        public object Arguments { get; }

        /// <summary>
        /// 获取当前窗口统一解绑集合。
        /// </summary>
        public UIBindingSet Bindings { get; }

        /// <summary>
        /// 获取当前会话拥有的任务域。
        /// </summary>
        public MTaskDomain Domain { get; }

        /// <summary>
        /// 获取窗口服务接口，用于关闭或聚焦当前窗口。
        /// </summary>
        public IUIService Service { get; }

        /// <summary>
        /// 创建只包含窗口生命周期能力的上下文。
        /// </summary>
        /// <param name="handle">当前窗口句柄。</param>
        /// <param name="arguments">本次打开参数。</param>
        /// <param name="bindings">统一解绑集合。</param>
        /// <param name="domain">窗口任务域。</param>
        /// <param name="service">窗口服务。</param>
        /// <param name="resultWriter">可选的窗口结果提交入口。</param>
        public UIWindowContext(UIWindowHandle handle, object arguments, UIBindingSet bindings, MTaskDomain domain, IUIService service, Action<object> resultWriter = null)
        {
            Handle = handle;
            Arguments = arguments;
            Bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            Domain = domain ?? throw new ArgumentNullException(nameof(domain));
            Service = service ?? throw new ArgumentNullException(nameof(service));
            submitResult = resultWriter;
        }

        /// <summary>
        /// 获取并验证强类型打开参数。
        /// </summary>
        /// <typeparam name="TArgs">期望参数类型。</typeparam>
        /// <returns>匹配的打开参数。</returns>
        public TArgs GetArguments<TArgs>()
        {
            if (Arguments is TArgs value)
            {
                return value;
            }

            throw new InvalidOperationException($"窗口参数类型不匹配，期望 {typeof(TArgs).FullName}，实际 {Arguments?.GetType().FullName ?? "<null>"}。");
        }

        /// <summary>
        /// 向 ShowAsync 调用方提交一次窗口结果。
        /// </summary>
        /// <typeparam name="TResult">窗口结果类型。</typeparam>
        /// <param name="result">要提交的业务结果。</param>
        public void SubmitResult<TResult>(TResult result)
        {
            if (submitResult == null)
            {
                throw new InvalidOperationException("当前窗口不是通过 ShowAsync 打开，不能提交结果。");
            }

            submitResult(result);
        }

        #endregion
    }

    /// <summary>
    /// WindowSession 可创建和释放的窗口逻辑统一契约。
    /// </summary>
    public interface IUIWindowLogic : IDisposable, IMTaskOwner
    {
        /// <summary>
        /// 绑定窗口上下文与实际 View。
        /// </summary>
        /// <param name="context">当前窗口上下文。</param>
        /// <param name="view">当前窗口 View。</param>
        void Bind(UIWindowContext context, AUIWindowView view);

        /// <summary>
        /// 执行窗口进入 Active 前的业务激活逻辑。
        /// </summary>
        /// <returns>业务激活完成任务。</returns>
        MTask ActivateAsync();

        /// <summary>
        /// 执行窗口离开 Active 时的业务退场逻辑。
        /// </summary>
        /// <returns>业务退场完成任务。</returns>
        MTask DeactivateAsync();
    }

    /// <summary>
    /// 可在重复打开时原地接收新参数并刷新显示的窗口逻辑。
    /// </summary>
    public interface IUIWindowRefreshable
    {
        /// <summary>
        /// 使用新的强类型打开参数刷新当前活动窗口。
        /// </summary>
        /// <param name="arguments">新的窗口打开参数。</param>
        /// <returns>刷新完成任务。</returns>
        MTask RefreshAsync(object arguments);
    }

    /// <summary>
    /// 强类型被动 View Presenter 基类。
    /// </summary>
    /// <typeparam name="TView">Presenter 对应的 View 类型。</typeparam>
    public abstract class AUIWindowPresenter<TView> : IUIWindowLogic where TView : AUIWindowView
    {
        #region Private 私有成员

        private UIWindowContext context; // 当前窗口上下文。
        private TView view; // 当前绑定 View。
        private bool disposed; // Presenter 是否已释放。

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 获取当前窗口上下文。
        /// </summary>
        protected UIWindowContext Context => context ?? throw new InvalidOperationException("Presenter 尚未绑定窗口上下文。");

        /// <summary>
        /// 获取当前强类型 View。
        /// </summary>
        protected TView View => view ?? throw new InvalidOperationException("Presenter 尚未绑定 View。");

        /// <summary>
        /// 获取由 WindowSession 自动释放的绑定集合。
        /// </summary>
        protected UIBindingSet Bindings => Context.Bindings;

        /// <summary>
        /// 派生 Presenter 在此登记 View 事件并完成首次渲染。
        /// </summary>
        protected abstract void OnBind();

        /// <summary>
        /// 执行可选的异步业务激活逻辑。
        /// </summary>
        /// <returns>业务激活完成任务。</returns>
        protected virtual MTask OnActivateAsync() => MTask.CompletedTask;

        /// <summary>
        /// 执行可选的异步业务退场逻辑。
        /// </summary>
        /// <returns>业务退场完成任务。</returns>
        protected virtual MTask OnDeactivateAsync() => MTask.CompletedTask;

        /// <summary>
        /// 在自动解绑完成前释放 Presenter 自身持有的非 UI 状态。
        /// </summary>
        protected virtual void OnDispose()
        {
        }

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 绑定当前窗口上下文与强类型 View。
        /// </summary>
        /// <param name="windowContext">当前窗口上下文。</param>
        /// <param name="windowView">当前窗口 View。</param>
        public void Bind(UIWindowContext windowContext, AUIWindowView windowView)
        {
            if (context != null)
            {
                throw new InvalidOperationException($"{GetType().FullName} 已经绑定窗口。");
            }

            context = windowContext ?? throw new ArgumentNullException(nameof(windowContext));
            view = windowView as TView ?? throw new InvalidOperationException($"Presenter {GetType().FullName} 需要 View {typeof(TView).FullName}。");
            OnBind();
        }

        /// <summary>
        /// 执行窗口业务激活逻辑。
        /// </summary>
        /// <returns>业务激活完成任务。</returns>
        public MTask ActivateAsync() => OnActivateAsync();

        /// <summary>
        /// 执行窗口业务退场逻辑。
        /// </summary>
        /// <returns>业务退场完成任务。</returns>
        public MTask DeactivateAsync() => OnDeactivateAsync();

        /// <summary>
        /// 返回 WindowSession 提供的任务域。
        /// </summary>
        /// <returns>当前窗口任务域。</returns>
        public MTaskDomain GetMTaskDomain() => Context.Domain;

        /// <summary>
        /// 释放 Presenter 获取的 Global 引用和窗口绑定。
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            OnDispose();
            MiniCore.Core.Global.ReleaseAll(this);
            view = null;
            context = null;
        }

        #endregion
    }

    /// <summary>
    /// 具有与 Presenter 相同生命周期但允许显式状态绑定的 ViewModel 基类。
    /// </summary>
    /// <typeparam name="TView">ViewModel 对应的 View 类型。</typeparam>
    public abstract class AUIWindowViewModel<TView> : AUIWindowPresenter<TView> where TView : AUIWindowView
    {
    }

    /// <summary>
    /// 拥有独立激活任务域并可安全进入缓存池的窗口 View 基类。
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
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
        [SerializeField, HideInInspector] private string logicTypeName; // Presenter 或 ViewModel 程序集限定名。
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
        /// 为新的打开周期创建任务域并恢复基本交互状态。
        /// </summary>
        public void PrepareForOpen()
        {
            activationDomain?.Dispose();
            activationDomain = new MTaskDomain($"{GetType().FullName}.Activation", MTaskExecutors.Unity);
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
