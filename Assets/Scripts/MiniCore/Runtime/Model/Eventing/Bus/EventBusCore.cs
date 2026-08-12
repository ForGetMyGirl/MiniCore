using System;
using System.Collections.Generic;
using MiniCore.Model;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Eventing
{

    /// <summary>
    /// 全部事件频道共享的无 Unity 分发核心。
    /// </summary>
    internal sealed class EventBusCore : IEventBus, IDisposable
    {
        #region Private 私有成员

        private readonly object gate = new object(); // 保护频道映射与频道创建。
        private readonly Dictionary<Type, IEventChannel> channels = new Dictionary<Type, IEventChannel>(); // 事件类型到独立频道的映射。
        private bool disposed; // 当前事件总线是否已经释放。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 注册一个同步对象监听器。
        /// </summary>
        /// <typeparam name="TEvent">同步事件类型。</typeparam>
        /// <param name="handler">已有的对象监听器。</param>
        /// <returns>用于解除订阅的值类型 token。</returns>
        public EventSubscription Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : ISyncEvent
        {
            return GetChannel<TEvent>().AddSyncHandler(handler);
        }

        /// <summary>
        /// 注册一个同步委托监听器。
        /// </summary>
        /// <typeparam name="TEvent">同步事件类型。</typeparam>
        /// <param name="handler">同步处理委托。</param>
        /// <returns>用于解除订阅的值类型 token。</returns>
        public EventSubscription Subscribe<TEvent>(Action<TEvent> handler) where TEvent : ISyncEvent
        {
            return GetChannel<TEvent>().AddSyncCallback(handler);
        }

        /// <summary>
        /// 注册一个异步对象监听器。
        /// </summary>
        /// <typeparam name="TEvent">异步事件类型。</typeparam>
        /// <param name="handler">已有的异步对象监听器。</param>
        /// <returns>用于解除订阅的值类型 token。</returns>
        public EventSubscription SubscribeAsync<TEvent>(IAsyncEventHandler<TEvent> handler) where TEvent : IAsyncEvent
        {
            return GetChannel<TEvent>().AddAsyncHandler(handler);
        }

        /// <summary>
        /// 注册一个异步委托监听器。
        /// </summary>
        /// <typeparam name="TEvent">异步事件类型。</typeparam>
        /// <param name="handler">异步处理委托。</param>
        /// <returns>用于解除订阅的值类型 token。</returns>
        public EventSubscription SubscribeAsync<TEvent>(Func<TEvent, MTask> handler) where TEvent : IAsyncEvent
        {
            return GetChannel<TEvent>().AddAsyncCallback(handler);
        }

        /// <summary>
        /// 同步派发一条同步事件。
        /// </summary>
        /// <typeparam name="TEvent">同步事件类型。</typeparam>
        /// <param name="@event">需要派发的事件数据。</param>
        public void Publish<TEvent>(TEvent @event) where TEvent : ISyncEvent
        {
            GetChannel<TEvent>().Publish(@event);
        }

        /// <summary>
        /// 异步派发一条异步事件。
        /// </summary>
        /// <typeparam name="TEvent">异步事件类型。</typeparam>
        /// <param name="@event">需要派发的事件数据。</param>
        /// <returns>全部异步监听器完成后的任务。</returns>
        public MTask PublishAsync<TEvent>(TEvent @event) where TEvent : IAsyncEvent
        {
            return GetChannel<TEvent>().PublishAsync(@event);
        }

        /// <summary>
        /// 等待下一次指定事件。
        /// </summary>
        /// <typeparam name="TEvent">要等待的事件类型。</typeparam>
        /// <param name="options">可选的等待超时。</param>
        /// <returns>命中的事件数据。</returns>
        public MTask<TEvent> WaitNextAsync<TEvent>(EventWaitOptions options = default) where TEvent : IEvent
        {
            return GetChannel<TEvent>().WaitNextAsync(options);
        }

        /// <summary>
        /// 释放全部事件频道、订阅和等待者。
        /// </summary>
        public void Dispose()
        {
            IEventChannel[] snapshot;
            lock (gate)
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                snapshot = new IEventChannel[channels.Count];
                channels.Values.CopyTo(snapshot, 0);
                channels.Clear();
            }

            for (int index = 0; index < snapshot.Length; index++)
            {
                snapshot[index].Dispose();
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 获取或创建指定事件类型的独立频道。
        /// </summary>
        /// <typeparam name="TEvent">事件类型。</typeparam>
        /// <returns>指定类型的强类型频道。</returns>
        private EventChannel<TEvent> GetChannel<TEvent>() where TEvent : IEvent
        {
            lock (gate)
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(nameof(EventBusCore));
                }

                Type eventType = typeof(TEvent);
                if (typeof(ISyncEvent).IsAssignableFrom(eventType) && typeof(IAsyncEvent).IsAssignableFrom(eventType))
                {
                    throw new InvalidOperationException($"事件类型不能同时实现 ISyncEvent 与 IAsyncEvent：{eventType.FullName}");
                }

                if (!channels.TryGetValue(eventType, out IEventChannel channel))
                {
                    channel = new EventChannel<TEvent>();
                    channels.Add(eventType, channel);
                }

                return (EventChannel<TEvent>)channel;
            }
        }

        #endregion
    }
}
