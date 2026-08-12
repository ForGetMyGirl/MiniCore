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
}
