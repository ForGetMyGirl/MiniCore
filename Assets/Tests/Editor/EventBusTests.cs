using System;
using System.Collections.Generic;
using NUnit.Framework;
using MiniCore.Core;
using MiniCore.Eventing;
using MiniCore.Model;
using MiniCore.Threading;

namespace MiniCore.Tests.Editor
{
    /// <summary>
    /// 验证强类型事件频道的基础派发与订阅生命周期语义。
    /// </summary>
    public sealed class EventBusTests
    {
        #region Public 公共成员

        /// <summary>
        /// 为每个用例创建隔离的全局运行时。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            Global.Shutdown();
            Global.Initialize();
        }

        /// <summary>
        /// 清理当前用例创建的全局组件与模块注册。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            Global.Shutdown();
        }

        /// <summary>
        /// 验证同步监听器按注册顺序执行，重复释放 token 不会影响其他监听器。
        /// </summary>
        [Test]
        public void Publish_SyncHandlers_RunInRegistrationOrderAndRespectTokens()
        {
            ScopedEventBus bus = new ScopedEventBus();
            SyncHandler first = new SyncHandler();
            SyncHandler second = new SyncHandler();
            EventSubscription firstSubscription = bus.Subscribe<SyncTestEvent>(first);
            EventSubscription secondSubscription = bus.Subscribe<SyncTestEvent>(second);

            bus.Publish(new SyncTestEvent(7));
            firstSubscription.Dispose();
            firstSubscription.Dispose();
            bus.Publish(new SyncTestEvent(9));

            Assert.AreEqual(7, first.LastValue);
            Assert.AreEqual(9, second.LastValue);
            Assert.AreEqual(2, second.CallCount);

            secondSubscription.Dispose();
            bus.Dispose();
        }

        /// <summary>
        /// 验证异步事件会等待注册的异步处理器完成。
        /// </summary>
        [Test]
        public void PublishAsync_AsyncHandler_IsAwaited()
        {
            ScopedEventBus bus = new ScopedEventBus();
            AsyncHandler handler = new AsyncHandler();
            EventSubscription subscription = bus.SubscribeAsync<AsyncTestEvent>(handler);

            bus.PublishAsync(new AsyncTestEvent("ok")).GetAwaiter().GetResult();

            Assert.AreEqual("ok", handler.LastValue);
            Assert.AreEqual(1, handler.CallCount);

            subscription.Dispose();
            bus.Dispose();
        }

        /// <summary>
        /// 验证两个局部频道不会交叉派发相同事件类型。
        /// </summary>
        [Test]
        public void ScopedBuses_IsolateTheSameEventType()
        {
            ScopedEventBus firstBus = new ScopedEventBus();
            ScopedEventBus secondBus = new ScopedEventBus();
            SyncHandler firstHandler = new SyncHandler();
            SyncHandler secondHandler = new SyncHandler();
            EventSubscription firstSubscription = firstBus.Subscribe<SyncTestEvent>(firstHandler);
            EventSubscription secondSubscription = secondBus.Subscribe<SyncTestEvent>(secondHandler);

            firstBus.Publish(new SyncTestEvent(11));

            Assert.AreEqual(11, firstHandler.LastValue);
            Assert.AreEqual(0, secondHandler.CallCount);

            firstSubscription.Dispose();
            secondSubscription.Dispose();
            firstBus.Dispose();
            secondBus.Dispose();
        }

        /// <summary>
        /// 验证单个监听器失败不会中断后续监听器，最后会抛出聚合异常。
        /// </summary>
        [Test]
        public void Publish_HandlerFailure_ContinuesAndThrowsAggregateException()
        {
            ScopedEventBus bus = new ScopedEventBus();
            ThrowingHandler throwingHandler = new ThrowingHandler();
            SyncHandler succeedingHandler = new SyncHandler();
            EventSubscription firstSubscription = bus.Subscribe<SyncTestEvent>(throwingHandler);
            EventSubscription secondSubscription = bus.Subscribe<SyncTestEvent>(succeedingHandler);

            Assert.Throws<AggregateException>(() => bus.Publish(new SyncTestEvent(3)));
            Assert.AreEqual(3, succeedingHandler.LastValue);
            Assert.AreEqual(1, succeedingHandler.CallCount);

            firstSubscription.Dispose();
            secondSubscription.Dispose();
            bus.Dispose();
        }

        /// <summary>
        /// 验证复用早期释放的槽位不会破坏剩余监听器的注册顺序。
        /// </summary>
        [Test]
        public void Publish_ReusedSlot_PreservesRegistrationOrder()
        {
            ScopedEventBus bus = new ScopedEventBus();
            var receivedOrder = new List<int>();
            OrderedHandler firstHandler = new OrderedHandler(1, receivedOrder);
            OrderedHandler secondHandler = new OrderedHandler(2, receivedOrder);
            OrderedHandler thirdHandler = new OrderedHandler(3, receivedOrder);
            EventSubscription firstSubscription = bus.Subscribe<SyncTestEvent>(firstHandler);
            EventSubscription secondSubscription = bus.Subscribe<SyncTestEvent>(secondHandler);

            firstSubscription.Dispose();
            EventSubscription thirdSubscription = bus.Subscribe<SyncTestEvent>(thirdHandler);
            bus.Publish(new SyncTestEvent(0));

            CollectionAssert.AreEqual(new[] { 2, 3 }, receivedOrder);

            secondSubscription.Dispose();
            thirdSubscription.Dispose();
            bus.Dispose();
        }

        /// <summary>
        /// 验证等待者仅接收注册之后的下一条同类型事件。
        /// </summary>
        [Test]
        public void WaitNextAsync_CompletesWhenNextEventIsReceived()
        {
            ScopedEventBus bus = new ScopedEventBus();
            MTask<SyncTestEvent> waiting = bus.WaitNextAsync<SyncTestEvent>();

            bus.Publish(new SyncTestEvent(17));

            Assert.AreEqual(17, waiting.GetAwaiter().GetResult().Value);
            bus.Dispose();
        }

        /// <summary>
        /// 验证 ComponentGroup 销毁局部频道时会取消仍在等待的调用方。
        /// </summary>
        [Test]
        public void ComponentGroup_Dispose_CancelsScopedBusWaiters()
        {
            ComponentGroup group = Global.CreateGroup("EventBusTests", 91001);
            ScopedEventBus bus = group.GetOrAdd<ScopedEventBus>();
            MTask<SyncTestEvent> waiting = bus.WaitNextAsync<SyncTestEvent>();

            group.Dispose();

            Assert.Throws<OperationCanceledException>(() => waiting.GetAwaiter().GetResult());
        }

        /// <summary>
        /// 验证应用模块可由注册表按接口取得，并随最后一个 owner 释放。
        /// </summary>
        [Test]
        public void ApplicationModule_GetOrAdd_ReleasesAfterOwnerRelease()
        {
            object owner = new object();
            Global.RegisterAppModule<IApplicationEventBus, ApplicationEventBusModule>();

            IApplicationEventBus bus = Global.GetOrAddModule<IApplicationEventBus>(owner);

            Assert.IsFalse(((ApplicationEventBusModule)bus).IsDisposed);
            Global.ReleaseAll(owner);
            Assert.IsTrue(((ApplicationEventBusModule)bus).IsDisposed);
        }

        #endregion

        #region Private 私有类型

        private sealed class SyncTestEvent : ISyncEvent
        {
            internal int Value { get; }

            /// <summary>
            /// 创建同步测试事件。
            /// </summary>
            /// <param name="value">测试载荷。</param>
            internal SyncTestEvent(int value)
            {
                Value = value;
            }
        }

        private sealed class AsyncTestEvent : IAsyncEvent
        {
            internal string Value { get; }

            /// <summary>
            /// 创建异步测试事件。
            /// </summary>
            /// <param name="value">测试载荷。</param>
            internal AsyncTestEvent(string value)
            {
                Value = value;
            }
        }

        private sealed class SyncHandler : IEventHandler<SyncTestEvent>
        {
            internal int CallCount { get; private set; }
            internal int LastValue { get; private set; }

            /// <summary>
            /// 保存最近收到的同步事件数据。
            /// </summary>
            /// <param name="@event">本次收到的同步测试事件。</param>
            public void Handle(SyncTestEvent @event)
            {
                CallCount++;
                LastValue = @event.Value;
            }
        }

        /// <summary>
        /// 用于验证异常不会阻断同次派发后续监听器的处理器。
        /// </summary>
        private sealed class ThrowingHandler : IEventHandler<SyncTestEvent>
        {
            /// <summary>
            /// 始终抛出测试异常。
            /// </summary>
            /// <param name="@event">本次同步测试事件。</param>
            public void Handle(SyncTestEvent @event)
            {
                throw new InvalidOperationException("事件监听器测试异常。");
            }
        }

        /// <summary>
        /// 按预设编号记录派发先后的同步监听器。
        /// </summary>
        private sealed class OrderedHandler : IEventHandler<SyncTestEvent>
        {
            private readonly int marker; // 当前监听器用于断言顺序的编号。
            private readonly List<int> receivedOrder; // 测试共享的实际派发顺序。

            /// <summary>
            /// 创建指定编号的顺序记录监听器。
            /// </summary>
            /// <param name="marker">当前监听器的顺序编号。</param>
            /// <param name="receivedOrder">共享的实际派发顺序集合。</param>
            internal OrderedHandler(int marker, List<int> receivedOrder)
            {
                this.marker = marker;
                this.receivedOrder = receivedOrder;
            }

            /// <summary>
            /// 记录当前监听器被调用的顺序。
            /// </summary>
            /// <param name="@event">本次同步测试事件。</param>
            public void Handle(SyncTestEvent @event)
            {
                receivedOrder.Add(marker);
            }
        }

        private sealed class AsyncHandler : IAsyncEventHandler<AsyncTestEvent>
        {
            internal int CallCount { get; private set; }
            internal string LastValue { get; private set; }

            /// <summary>
            /// 保存最近收到的异步事件数据。
            /// </summary>
            /// <param name="@event">本次收到的异步测试事件。</param>
            /// <returns>同步完成的处理任务。</returns>
            public MTask HandleAsync(AsyncTestEvent @event)
            {
                CallCount++;
                LastValue = @event.Value;
                return MTask.CompletedTask;
            }
        }

        #endregion
    }
}
