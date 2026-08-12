using System;
using System.Collections.Generic;
using MiniCore.Model;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Eventing
{

    /// <summary>
    /// 全局应用级事件频道模块。
    /// 模块不保存业务状态，仅在跨模块通知确有需要时按 owner 引用创建。
    /// </summary>
    [AppModule(typeof(IApplicationEventBus), Description = "提供跨模块的强类型同步、异步与一次性事件通知。")]
    public sealed class ApplicationEventBusModule : AAppModule, IApplicationEventBus
    {
        #region Private 私有成员

        private readonly EventBusCore core = new EventBusCore(); // 实际事件分发核心。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 注册同步对象监听器。
        /// </summary>
        /// <typeparam name="TEvent">同步事件类型。</typeparam>
        /// <param name="handler">对象监听器。</param>
        /// <returns>订阅 token。</returns>
        public EventSubscription Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : ISyncEvent => core.Subscribe(handler);

        /// <summary>
        /// 注册同步委托监听器。
        /// </summary>
        /// <typeparam name="TEvent">同步事件类型。</typeparam>
        /// <param name="handler">同步处理委托。</param>
        /// <returns>订阅 token。</returns>
        public EventSubscription Subscribe<TEvent>(Action<TEvent> handler) where TEvent : ISyncEvent => core.Subscribe(handler);

        /// <summary>
        /// 注册异步对象监听器。
        /// </summary>
        /// <typeparam name="TEvent">异步事件类型。</typeparam>
        /// <param name="handler">异步对象监听器。</param>
        /// <returns>订阅 token。</returns>
        public EventSubscription SubscribeAsync<TEvent>(IAsyncEventHandler<TEvent> handler) where TEvent : IAsyncEvent => core.SubscribeAsync(handler);

        /// <summary>
        /// 注册异步委托监听器。
        /// </summary>
        /// <typeparam name="TEvent">异步事件类型。</typeparam>
        /// <param name="handler">异步处理委托。</param>
        /// <returns>订阅 token。</returns>
        public EventSubscription SubscribeAsync<TEvent>(Func<TEvent, MTask> handler) where TEvent : IAsyncEvent => core.SubscribeAsync(handler);

        /// <summary>
        /// 派发同步事件。
        /// </summary>
        /// <typeparam name="TEvent">同步事件类型。</typeparam>
        /// <param name="@event">事件数据。</param>
        public void Publish<TEvent>(TEvent @event) where TEvent : ISyncEvent => core.Publish(@event);

        /// <summary>
        /// 派发异步事件。
        /// </summary>
        /// <typeparam name="TEvent">异步事件类型。</typeparam>
        /// <param name="@event">事件数据。</param>
        /// <returns>监听器完成任务。</returns>
        public MTask PublishAsync<TEvent>(TEvent @event) where TEvent : IAsyncEvent => core.PublishAsync(@event);

        /// <summary>
        /// 等待下一次指定事件。
        /// </summary>
        /// <typeparam name="TEvent">事件类型。</typeparam>
        /// <param name="options">可选超时。</param>
        /// <returns>命中的事件数据。</returns>
        public MTask<TEvent> WaitNextAsync<TEvent>(EventWaitOptions options = default) where TEvent : IEvent => core.WaitNextAsync<TEvent>(options);

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 在模块最终释放时取消全部等待者并解除所有订阅。
        /// </summary>
        protected override void OnDispose()
        {
            core.Dispose();
        }

        #endregion
    }
}
