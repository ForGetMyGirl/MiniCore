using System;

namespace MiniCore.Eventing
{
    /// <summary>
    /// 分析器命令行测试使用的事件总标记。
    /// </summary>
    public interface IEvent
    {
    }

    /// <summary>
    /// 分析器命令行测试使用的同步事件标记。
    /// </summary>
    public interface ISyncEvent : IEvent
    {
    }

    /// <summary>
    /// 分析器命令行测试使用的异步事件标记。
    /// </summary>
    public interface IAsyncEvent : IEvent
    {
    }

    /// <summary>
    /// 分析器命令行测试使用的订阅 token。
    /// </summary>
    public struct EventSubscription
    {
        /// <summary>
        /// 释放测试 token。
        /// </summary>
        public void Dispose()
        {
        }
    }

    /// <summary>
    /// 分析器命令行测试使用的总线契约。
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 注册同步处理器。
        /// </summary>
        /// <typeparam name="TEvent">同步事件类型。</typeparam>
        /// <param name="handler">同步处理器。</param>
        /// <returns>订阅 token。</returns>
        EventSubscription Subscribe<TEvent>(Action<TEvent> handler) where TEvent : ISyncEvent;
    }
}

namespace MiniCore.Eventing.Diagnostics.Tests
{
    using MiniCore.Eventing;

    /// <summary>
    /// 同时声明两种派发标记，预期产生 MCEVT003。
    /// </summary>
    internal sealed class DualMarkedEvent : ISyncEvent, IAsyncEvent
    {
    }

    /// <summary>
    /// 正常同步事件，不应产生标记诊断。
    /// </summary>
    internal sealed class SyncEvent : ISyncEvent
    {
    }

    /// <summary>
    /// 构造直接 lambda、丢弃 token 与正常命名方法订阅的测试代码。
    /// </summary>
    internal sealed class AnalyzerFixture
    {
        /// <summary>
        /// 触发并对比分析器诊断。
        /// </summary>
        /// <param name="bus">测试事件总线。</param>
        internal void Exercise(IEventBus bus)
        {
            bus.Subscribe<SyncEvent>(_ => { });
            EventSubscription subscription = bus.Subscribe<SyncEvent>(Handle);
            subscription.Dispose();
        }

        /// <summary>
        /// 正常的命名方法监听器。
        /// </summary>
        /// <param name="@event">同步测试事件。</param>
        private static void Handle(SyncEvent @event)
        {
        }
    }
}
