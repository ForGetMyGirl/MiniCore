using System;
using System.Collections.Generic;
using MiniCore.Model;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Eventing
{

    /// <summary>
    /// 提供强类型同步、异步和一次性等待事件能力的频道契约。
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 注册一个同步对象监听器。
        /// </summary>
        /// <typeparam name="TEvent">同步事件类型。</typeparam>
        /// <param name="handler">已有的对象监听器。</param>
        /// <returns>用于解除订阅的值类型 token。</returns>
        EventSubscription Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : ISyncEvent;

        /// <summary>
        /// 注册一个同步委托监听器。
        /// 委托或 lambda 仅适合低频场景，调用方必须保存返回 token。
        /// </summary>
        /// <typeparam name="TEvent">同步事件类型。</typeparam>
        /// <param name="handler">同步处理委托。</param>
        /// <returns>用于解除订阅的值类型 token。</returns>
        EventSubscription Subscribe<TEvent>(Action<TEvent> handler) where TEvent : ISyncEvent;

        /// <summary>
        /// 注册一个异步对象监听器。
        /// </summary>
        /// <typeparam name="TEvent">异步事件类型。</typeparam>
        /// <param name="handler">已有的异步对象监听器。</param>
        /// <returns>用于解除订阅的值类型 token。</returns>
        EventSubscription SubscribeAsync<TEvent>(IAsyncEventHandler<TEvent> handler) where TEvent : IAsyncEvent;

        /// <summary>
        /// 注册一个异步委托监听器。
        /// 委托或 lambda 仅适合低频场景，调用方必须保存返回 token。
        /// </summary>
        /// <typeparam name="TEvent">异步事件类型。</typeparam>
        /// <param name="handler">异步处理委托。</param>
        /// <returns>用于解除订阅的值类型 token。</returns>
        EventSubscription SubscribeAsync<TEvent>(Func<TEvent, MTask> handler) where TEvent : IAsyncEvent;

        /// <summary>
        /// 同步派发一条同步事件。
        /// 所有监听器都会按注册顺序执行；失败会在全部监听器执行后以聚合异常抛出。
        /// </summary>
        /// <typeparam name="TEvent">同步事件类型。</typeparam>
        /// <param name="@event">需要派发的事件数据。</param>
        void Publish<TEvent>(TEvent @event) where TEvent : ISyncEvent;

        /// <summary>
        /// 异步派发一条异步事件。
        /// 所有监听器会按注册顺序等待；失败会在全部监听器执行后以聚合异常抛出。
        /// </summary>
        /// <typeparam name="TEvent">异步事件类型。</typeparam>
        /// <param name="@event">需要派发的事件数据。</param>
        /// <returns>全部异步监听器完成后的任务。</returns>
        MTask PublishAsync<TEvent>(TEvent @event) where TEvent : IAsyncEvent;

        /// <summary>
        /// 等待当前频道中下一次派发的指定事件。
        /// 此方法不缓存历史事件，也不提供跨实体或跨会话筛选。
        /// </summary>
        /// <typeparam name="TEvent">要等待的事件类型。</typeparam>
        /// <param name="options">可选的等待超时。</param>
        /// <returns>命中的事件数据。</returns>
        MTask<TEvent> WaitNextAsync<TEvent>(EventWaitOptions options = default) where TEvent : IEvent;
    }
}
