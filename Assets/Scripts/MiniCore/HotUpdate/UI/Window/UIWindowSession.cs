using System;
using MiniCore.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace MiniCore.UI
{

    /// <summary>
    /// 管理一次窗口打开周期的状态、任务域、逻辑、绑定、结果和 View。
    /// </summary>
    internal sealed class UIWindowSession
    {
        #region Private 私有成员

        private const float DefaultModalMaskAlpha = 0.8f; // 自动 Modal 遮罩的默认不透明度。
        private readonly IUIWindowSessionHost host; // 窗口运行时宿主。
        private readonly MTaskDomain domain; // Presenter/ViewModel 共用的会话任务域。
        private readonly UIBindingSet bindings = new UIBindingSet(); // 本次会话的统一解绑集合。
        private readonly MTaskCompletionSource<UIWindowHandle> activeCompletion = new MTaskCompletionSource<UIWindowHandle>(); // Active 完成源。
        private readonly MSharedTask<UIWindowHandle> activeTask; // 并发打开共享等待任务。
        private readonly MTaskCompletionSource<bool> closedCompletion = new MTaskCompletionSource<bool>(); // 终态完成源。
        private readonly MSharedTask<bool> closedTask; // Queue 策略共享等待任务。
        private readonly object arguments; // 本次打开参数。
        private readonly IUIWindowResultChannel resultChannel; // 可选业务结果通道。
        private AUIWindowView view; // 当前会话使用的 View。
        private IUIWindowLogic logic; // 当前 Presenter 或 ViewModel。
        private IUITransitionDriver transition; // 当前 View 的动画驱动。
        private GameObject modalMask; // 当前会话拥有的模态遮罩。
        private bool closeRequested; // 加载或动画阶段收到的关闭请求。
        private bool completed; // 是否已经进入唯一终态。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取窗口定义。
        /// </summary>
        public UIWindowDefinition Definition { get; }

        /// <summary>
        /// 获取当前代窗口句柄。
        /// </summary>
        public UIWindowHandle Handle { get; }

        /// <summary>
        /// 获取当前窗口状态。
        /// </summary>
        public UIWindowState State { get; private set; }

        /// <summary>
        /// 获取活动 View 的 GameObject；未完成加载时返回 null。
        /// </summary>
        public GameObject ViewObject => view != null ? view.gameObject : null;

        /// <summary>
        /// 创建一个尚未开始加载的窗口会话。
        /// </summary>
        /// <param name="sessionHost">窗口运行时宿主。</param>
        /// <param name="definition">生成的窗口定义。</param>
        /// <param name="handle">当前代窗口句柄。</param>
        /// <param name="openArguments">强类型打开参数。</param>
        /// <param name="channel">可选结果通道。</param>
        public UIWindowSession(IUIWindowSessionHost sessionHost, UIWindowDefinition definition, UIWindowHandle handle, object openArguments, IUIWindowResultChannel channel)
        {
            host = sessionHost ?? throw new ArgumentNullException(nameof(sessionHost));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Handle = handle ?? throw new ArgumentNullException(nameof(handle));
            arguments = openArguments;
            resultChannel = channel;
            domain = new MTaskDomain($"UIWindow:{definition.RouteName}:{Handle.InstanceId.Generation}", MTaskExecutors.Unity);
            activeTask = activeCompletion.Task.Share();
            closedTask = closedCompletion.Task.Share();
            State = UIWindowState.None;
        }

        /// <summary>
        /// 开始窗口状态机；调用方通过 WaitUntilActiveAsync 共享等待结果。
        /// </summary>
        public void Start()
        {
            OpenInternalAsync().Forget();
        }

        /// <summary>
        /// 等待当前会话进入 Active 或传播加载错误。
        /// </summary>
        /// <returns>活动窗口句柄。</returns>
        public async MTask<UIWindowHandle> WaitUntilActiveAsync()
        {
            return await activeTask;
        }

        /// <summary>
        /// 等待当前会话进入关闭、失败或销毁终态。
        /// </summary>
        /// <returns>终态到达任务。</returns>
        public async MTask WaitUntilClosedAsync()
        {
            await closedTask;
        }

        /// <summary>
        /// 对支持刷新的窗口逻辑应用新打开参数。
        /// </summary>
        /// <param name="newArguments">重复打开传入的新参数。</param>
        /// <returns>刷新完成任务。</returns>
        public MTask RefreshAsync(object newArguments)
        {
            return logic is IUIWindowRefreshable refreshable ? refreshable.RefreshAsync(newArguments) : MTask.CompletedTask;
        }

        /// <summary>
        /// 请求关闭；Loading 中会先取消任务域再统一回收。
        /// </summary>
        /// <returns>关闭收敛完成任务。</returns>
        public MTask CloseAsync()
        {
            closeRequested = true;
            if (State == UIWindowState.Loading || State == UIWindowState.Staging || State == UIWindowState.Opening)
            {
                domain.Cancel();
            }

            return CloseInternalAsync();
        }

        /// <summary>
        /// 将当前窗口移动到同层最前方。
        /// </summary>
        /// <returns>存在活动 View 时返回 true。</returns>
        public bool Focus()
        {
            if (State != UIWindowState.Active || view == null)
            {
                return false;
            }

            if (modalMask != null)
            {
                modalMask.transform.SetAsLastSibling();
            }

            view.transform.SetAsLastSibling();
            return true;
        }

        /// <summary>
        /// 服务退出时立即打断动画并同步释放会话拥有的对象。
        /// </summary>
        public void Abort()
        {
            if (completed)
            {
                return;
            }

            closeRequested = true;
            transition?.Interrupt(UITransitionInterruptMode.RestoreOriginal);
            activeCompletion.TrySetException(new InvalidOperationException($"UIService 已释放，窗口 {Definition.RouteName} 被终止。"));
            Cleanup(true);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 串行执行窗口固定状态机并将所有异常收敛到清理路径。
        /// </summary>
        /// <returns>内部打开任务。</returns>
        private async MTask OpenInternalAsync()
        {
            try
            {
                SetState(UIWindowState.Loading);
                view = await host.AcquireViewAsync(Definition);
                if (closeRequested || completed)
                {
                    AUIWindowView unusedView = view;
                    view = null;
                    host.ReleaseView(Definition, unusedView);
                    activeCompletion.TrySetException(new InvalidOperationException($"窗口 {Definition.RouteName} 在加载完成前已关闭。"));
                    return;
                }

                SetState(UIWindowState.Staging);
                PrepareView();
                logic = Definition.CreateLogic();
                UIWindowContext context = new UIWindowContext(Handle, arguments, bindings, domain, host.Service, SubmitResult);
                logic.Bind(context, view);
                await view.OpenAsync();
                await logic.ActivateAsync();
                Canvas.ForceUpdateCanvases();
                if (closeRequested)
                {
                    await CloseInternalAsync();
                    return;
                }

                SetState(UIWindowState.Opening);
                transition?.ResetToEnterState();
                if (transition != null)
                {
                    await transition.PlayEnterAsync();
                }

                if (closeRequested)
                {
                    await CloseInternalAsync();
                    return;
                }

                SetState(UIWindowState.Active);
                view.CanvasGroup.interactable = true;
                view.CanvasGroup.blocksRaycasts = true;
                activeCompletion.TrySetResult(Handle);
            }
            catch (Exception exception)
            {
                activeCompletion.TrySetException(exception);
                await FailAndCleanupAsync();
            }
        }

        /// <summary>
        /// 配置父节点、布局、安全区域、动画和可选模态遮罩。
        /// </summary>
        private void PrepareView()
        {
            UISafeAreaPolicy safePolicy = Definition.SafeAreaPolicy == UISafeAreaPolicy.Inherit ? host.Profile.DefaultSafeAreaPolicy : Definition.SafeAreaPolicy;
            RectTransform parent = host.Root.GetWindowParent(Definition.RenderSpace, Definition.Layer);
            view.transform.SetParent(parent, false);
            view.transform.SetAsLastSibling();
            view.gameObject.SetActive(true);
            view.PrepareForOpen();
            transition = view.GetTransitionDriver();
            view.BindSafeArea(host.Root.ResolutionService, safePolicy);
            UIResponsiveLayout[] responsiveLayouts = view.GetComponentsInChildren<UIResponsiveLayout>(true);
            for (int i = 0; i < responsiveLayouts.Length; i++)
            {
                responsiveLayouts[i].Bind(host.Root.ResolutionService);
            }

            if (Definition.Modal)
            {
                CreateModalMask(parent);
            }
        }

        /// <summary>
        /// 创建由当前会话拥有并随会话关闭的透明输入遮罩。
        /// </summary>
        /// <param name="parent">窗口所在层父节点。</param>
        private void CreateModalMask(RectTransform parent)
        {
            modalMask = new GameObject($"{Definition.RouteName}.ModalMask", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform rect = (RectTransform)modalMask.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = modalMask.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, DefaultModalMaskAlpha);
            Button button = modalMask.GetComponent<Button>();
            button.interactable = Definition.CloseOnMaskClick;
            if (Definition.CloseOnMaskClick)
            {
                bindings.Add(button, OnMaskClicked);
            }

            modalMask.transform.SetSiblingIndex(view.transform.GetSiblingIndex());
            view.transform.SetAsLastSibling();
        }

        /// <summary>
        /// 响应允许关闭的模态遮罩点击。
        /// </summary>
        private void OnMaskClicked()
        {
            CloseAsync().Forget();
        }

        /// <summary>
        /// 向可选结果通道提交 Presenter 结果。
        /// </summary>
        /// <param name="result">业务结果。</param>
        private void SubmitResult(object result)
        {
            resultChannel?.SetResult(result);
        }

        /// <summary>
        /// 执行关闭动画、逻辑退场和唯一清理路径。
        /// </summary>
        /// <returns>关闭完成任务。</returns>
        private async MTask CloseInternalAsync()
        {
            if (completed || State == UIWindowState.Closing)
            {
                return;
            }

            SetState(UIWindowState.Closing);
            if (view != null)
            {
                view.CanvasGroup.interactable = false;
                view.CanvasGroup.blocksRaycasts = false;
            }

            try
            {
                transition?.Interrupt(UITransitionInterruptMode.KeepCurrent);
                if (transition != null && view != null)
                {
                    await transition.PlayExitAsync();
                }

                if (logic != null)
                {
                    await logic.DeactivateAsync();
                }

                if (view != null)
                {
                    await view.CloseAsync();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                Cleanup(false);
            }
        }

        /// <summary>
        /// 打开失败后回收已创建对象并进入失败终态。
        /// </summary>
        /// <returns>失败清理完成任务。</returns>
        private MTask FailAndCleanupAsync()
        {
            Cleanup(true);
            return MTask.CompletedTask;
        }

        /// <summary>
        /// 释放任务、绑定、逻辑、遮罩和 View，并通知宿主一次。
        /// </summary>
        /// <param name="failed">是否因加载或生命周期异常结束。</param>
        private void Cleanup(bool failed)
        {
            if (completed)
            {
                return;
            }

            completed = true;
            activeCompletion.TrySetException(new InvalidOperationException($"窗口 {Definition.RouteName} 在进入 Active 前结束。"));
            bindings.Dispose();
            logic?.Dispose();
            logic = null;
            domain.Dispose();
            if (modalMask != null)
            {
                UnityEngine.Object.Destroy(modalMask);
                modalMask = null;
            }

            if (view != null)
            {
                view.UnbindSafeArea();
                UIResponsiveLayout[] responsiveLayouts = view.GetComponentsInChildren<UIResponsiveLayout>(true);
                for (int i = 0; i < responsiveLayouts.Length; i++)
                {
                    responsiveLayouts[i].Unbind();
                }

                transition?.RestoreOriginalState();
                view.ResetForPool();
                host.ReleaseView(Definition, view);
                view = null;
            }

            resultChannel?.CloseWithoutResult();
            SetState(failed ? UIWindowState.Failed : Definition.CachePolicy == UICachePolicy.DestroyOnClose ? UIWindowState.Destroyed : UIWindowState.Cached);
            closedCompletion.TrySetResult(true);
            host.CompleteSession(this);
        }

        /// <summary>
        /// 校验并应用一次固定窗口状态迁移。
        /// </summary>
        /// <param name="next">目标状态。</param>
        private void SetState(UIWindowState next)
        {
            if (!UIWindowStateMachine.CanTransition(State, next))
            {
                throw new InvalidOperationException($"窗口 {Definition.RouteName} 状态迁移非法：{State} -> {next}。");
            }

            State = next;
        }

        #endregion
    }
}
