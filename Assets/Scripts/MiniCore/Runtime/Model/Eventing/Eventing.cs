using System;
using System.Collections.Generic;
using MiniCore.Model;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Eventing
{
    /// <summary>
    /// 标记可由 MiniCore 事件频道派发的强类型事件。
    /// 事件应使用不可变数据表达业务事实，不能再使用字符串或整数充当事件标识；频道本身不缓存历史事件。
    /// </summary>
    public interface IEvent
    {
    }

    /// <summary>
    /// 标记仅允许同步派发和同步监听的事件。
    /// 同一事件类型不能同时实现 <see cref="IAsyncEvent"/>。
    /// </summary>
    public interface ISyncEvent : IEvent
    {
    }

    /// <summary>
    /// 标记仅允许异步派发和异步监听的事件。
    /// 同一事件类型不能同时实现 <see cref="ISyncEvent"/>。
    /// </summary>
    public interface IAsyncEvent : IEvent
    {
    }

    /// <summary>
    /// 接收同步事件的无分配对象监听器契约。
    /// 性能敏感路径应优先让已有对象实现此接口，而不是创建委托或 lambda。
    /// </summary>
    /// <typeparam name="TEvent">要处理的同步事件类型。</typeparam>
    public interface IEventHandler<in TEvent> where TEvent : IEvent
    {
        /// <summary>
        /// 处理一条同步事件。
        /// </summary>
        /// <param name="@event">本次派发的事件数据。</param>
        void Handle(TEvent @event);
    }

    /// <summary>
    /// 接收异步事件的对象监听器契约。
    /// 事件频道会按注册顺序等待每个监听器返回的任务。
    /// </summary>
    /// <typeparam name="TEvent">要处理的异步事件类型。</typeparam>
    public interface IAsyncEventHandler<in TEvent> where TEvent : IEvent
    {
        /// <summary>
        /// 异步处理一条事件。
        /// </summary>
        /// <param name="@event">本次派发的事件数据。</param>
        /// <returns>处理完成任务。</returns>
        MTask HandleAsync(TEvent @event);
    }

    /// <summary>
    /// 描述一次性事件等待的可选限制。
    /// 默认值表示不设置超时，等待会在事件频道销毁或当前任务取消时结束。
    /// </summary>
    public readonly struct EventWaitOptions
    {
        #region Public 公共成员

        /// <summary>
        /// 获取等待的最大时长；零表示不设置超时。
        /// </summary>
        public TimeSpan Timeout { get; }

        /// <summary>
        /// 判断当前等待是否配置了超时。
        /// </summary>
        public bool HasTimeout => Timeout > TimeSpan.Zero;

        /// <summary>
        /// 使用指定超时创建等待选项。
        /// </summary>
        /// <param name="timeout">大于零的最长等待时长。</param>
        public EventWaitOptions(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), "事件等待超时必须大于零。");
            }

            Timeout = timeout;
        }

        #endregion
    }

    /// <summary>
    /// 表示一次事件订阅。
    /// 订阅 token 为值类型，调用方应保存它并在自身生命周期结束时调用 <see cref="Dispose"/>。
    /// </summary>
    public readonly struct EventSubscription : IDisposable
    {
        #region Private 私有成员

        private readonly IEventSubscriptionOwner owner; // 实际持有订阅槽位的频道。
        private readonly int slotId; // 订阅在频道中的槽位编号。
        private readonly uint generation; // 槽位复用保护版本。
        private readonly byte kind; // 同步或异步订阅类别。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建一个绑定频道槽位的订阅 token。
        /// </summary>
        /// <param name="owner">订阅所在频道。</param>
        /// <param name="slotId">订阅槽位编号。</param>
        /// <param name="generation">槽位当前版本。</param>
        /// <param name="kind">订阅类别。</param>
        internal EventSubscription(IEventSubscriptionOwner owner, int slotId, uint generation, byte kind)
        {
            this.owner = owner;
            this.slotId = slotId;
            this.generation = generation;
            this.kind = kind;
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 解除本次订阅。
        /// 重复调用或频道已经销毁时保持安全且无副作用。
        /// </summary>
        public void Dispose()
        {
            owner?.RemoveSubscription(slotId, generation, kind);
        }

        #endregion
    }

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

    /// <summary>
    /// 标记由 Global 模块注册表按需创建的应用级事件频道。
    /// 应用级频道仅用于跨模块、低频的应用通知。
    /// </summary>
    public interface IApplicationEventBus : IEventBus, IAppModule
    {
    }

    internal interface IEventSubscriptionOwner
    {
        void RemoveSubscription(int slotId, uint generation, byte kind);
    }

    internal interface IEventChannel : IDisposable
    {
    }

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

    /// <summary>
    /// 保存单一事件类型订阅和等待者的强类型频道。
    /// </summary>
    /// <typeparam name="TEvent">当前频道唯一处理的事件类型。</typeparam>
    internal sealed class EventChannel<TEvent> : IEventChannel, IEventSubscriptionOwner where TEvent : IEvent
    {
        #region Private 私有成员

        private const byte SyncKind = 1; // 同步订阅 token 类型。
        private const byte AsyncKind = 2; // 异步订阅 token 类型。

        private readonly object gate = new object(); // 保护槽位集合与等待者集合。
        private readonly List<SyncSlot> syncSlots = new List<SyncSlot>(4); // 同步监听器槽位。
        private readonly List<AsyncSlot> asyncSlots = new List<AsyncSlot>(4); // 异步监听器槽位。
        private readonly List<Waiter> waiters = new List<Waiter>(2); // 一次性等待者集合。
        private int syncFreeHead = -1; // 同步空闲槽位链表头。
        private int asyncFreeHead = -1; // 异步空闲槽位链表头。
        private long nextSyncRegistrationOrder; // 下一个同步监听器的稳定注册序号。
        private long nextAsyncRegistrationOrder; // 下一个异步监听器的稳定注册序号。
        private bool disposed; // 当前频道是否已经销毁。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 添加同步对象监听器。
        /// </summary>
        /// <param name="handler">对象监听器。</param>
        /// <returns>订阅 token。</returns>
        internal EventSubscription AddSyncHandler(IEventHandler<TEvent> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            lock (gate)
            {
                ThrowIfDisposed();
                int slotId = RentSyncSlot(out uint generation);
                SyncSlot slot = syncSlots[slotId];
                slot.ObjectHandler = handler;
                slot.Callback = null;
                slot.RegistrationOrder = ++nextSyncRegistrationOrder;
                slot.Active = true;
                syncSlots[slotId] = slot;
                return new EventSubscription(this, slotId, generation, SyncKind);
            }
        }

        /// <summary>
        /// 添加同步委托监听器。
        /// </summary>
        /// <param name="handler">同步处理委托。</param>
        /// <returns>订阅 token。</returns>
        internal EventSubscription AddSyncCallback(Action<TEvent> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            lock (gate)
            {
                ThrowIfDisposed();
                int slotId = RentSyncSlot(out uint generation);
                SyncSlot slot = syncSlots[slotId];
                slot.ObjectHandler = null;
                slot.Callback = handler;
                slot.RegistrationOrder = ++nextSyncRegistrationOrder;
                slot.Active = true;
                syncSlots[slotId] = slot;
                return new EventSubscription(this, slotId, generation, SyncKind);
            }
        }

        /// <summary>
        /// 添加异步对象监听器。
        /// </summary>
        /// <param name="handler">异步对象监听器。</param>
        /// <returns>订阅 token。</returns>
        internal EventSubscription AddAsyncHandler(IAsyncEventHandler<TEvent> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            lock (gate)
            {
                ThrowIfDisposed();
                int slotId = RentAsyncSlot(out uint generation);
                AsyncSlot slot = asyncSlots[slotId];
                slot.ObjectHandler = handler;
                slot.Callback = null;
                slot.RegistrationOrder = ++nextAsyncRegistrationOrder;
                slot.Active = true;
                asyncSlots[slotId] = slot;
                return new EventSubscription(this, slotId, generation, AsyncKind);
            }
        }

        /// <summary>
        /// 添加异步委托监听器。
        /// </summary>
        /// <param name="handler">异步处理委托。</param>
        /// <returns>订阅 token。</returns>
        internal EventSubscription AddAsyncCallback(Func<TEvent, MTask> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            lock (gate)
            {
                ThrowIfDisposed();
                int slotId = RentAsyncSlot(out uint generation);
                AsyncSlot slot = asyncSlots[slotId];
                slot.ObjectHandler = null;
                slot.Callback = handler;
                slot.RegistrationOrder = ++nextAsyncRegistrationOrder;
                slot.Active = true;
                asyncSlots[slotId] = slot;
                return new EventSubscription(this, slotId, generation, AsyncKind);
            }
        }

        /// <summary>
        /// 同步派发事件并在全部监听器执行后汇总错误。
        /// </summary>
        /// <param name="@event">需要派发的事件数据。</param>
        internal void Publish(TEvent @event)
        {
            ValidateEvent(@event);
            SyncSlot[] snapshot = CaptureSyncSnapshot();
            CompleteWaiters(@event);
            List<Exception> exceptions = null;
            for (int index = 0; index < snapshot.Length; index++)
            {
                try
                {
                    snapshot[index].Invoke(@event);
                }
                catch (Exception exception)
                {
                    (exceptions ??= new List<Exception>(1)).Add(exception);
                }
            }

            ThrowAggregate(exceptions);
        }

        /// <summary>
        /// 异步派发事件并在全部监听器执行后汇总错误。
        /// </summary>
        /// <param name="@event">需要派发的事件数据。</param>
        /// <returns>全部监听器完成后的任务。</returns>
        internal async MTask PublishAsync(TEvent @event)
        {
            ValidateEvent(@event);
            AsyncSlot[] snapshot = CaptureAsyncSnapshot();
            CompleteWaiters(@event);
            List<Exception> exceptions = null;
            for (int index = 0; index < snapshot.Length; index++)
            {
                try
                {
                    await snapshot[index].InvokeAsync(@event);
                }
                catch (Exception exception)
                {
                    (exceptions ??= new List<Exception>(1)).Add(exception);
                }
            }

            ThrowAggregate(exceptions);
        }

        /// <summary>
        /// 注册一次性等待者，并在命中、超时或取消后自动移除。
        /// </summary>
        /// <param name="options">可选的等待超时。</param>
        /// <returns>命中的事件数据。</returns>
        internal async MTask<TEvent> WaitNextAsync(EventWaitOptions options)
        {
            Waiter waiter;
            lock (gate)
            {
                ThrowIfDisposed();
                waiter = new Waiter();
                waiters.Add(waiter);
            }

            if (options.HasTimeout)
            {
                MTask timeoutTask = StartTimeoutAsync(waiter, options.Timeout);
            }

            try
            {
                return await waiter.Completion.Task;
            }
            finally
            {
                RemoveWaiter(waiter);
            }
        }

        /// <summary>
        /// 移除指定订阅槽位。
        /// </summary>
        /// <param name="slotId">订阅槽位编号。</param>
        /// <param name="generation">订阅时记录的槽位版本。</param>
        /// <param name="kind">同步或异步订阅类型。</param>
        void IEventSubscriptionOwner.RemoveSubscription(int slotId, uint generation, byte kind)
        {
            lock (gate)
            {
                if (disposed)
                {
                    return;
                }

                if (kind == SyncKind)
                {
                    ReleaseSyncSlot(slotId, generation);
                }
                else if (kind == AsyncKind)
                {
                    ReleaseAsyncSlot(slotId, generation);
                }
            }
        }

        /// <summary>
        /// 取消全部活动等待并清空引用。
        /// </summary>
        public void Dispose()
        {
            Waiter[] waiterSnapshot;
            lock (gate)
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                waiterSnapshot = waiters.ToArray();
                waiters.Clear();
                syncSlots.Clear();
                asyncSlots.Clear();
                syncFreeHead = -1;
                asyncFreeHead = -1;
            }

            for (int index = 0; index < waiterSnapshot.Length; index++)
            {
                waiterSnapshot[index].Completion.TrySetCanceled();
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 在超时后尝试使仍然活动的等待者失败。
        /// </summary>
        /// <param name="waiter">需要超时的等待者。</param>
        /// <param name="timeout">等待时长。</param>
        /// <returns>超时监督任务。</returns>
        private async MTask StartTimeoutAsync(Waiter waiter, TimeSpan timeout)
        {
            await MTask.Delay(timeout);
            bool removed = RemoveWaiter(waiter);
            if (removed)
            {
                waiter.Completion.TrySetException(new TimeoutException($"等待事件 {typeof(TEvent).FullName} 超时：{timeout}。"));
            }
        }

        /// <summary>
        /// 获取当前同步监听器的稳定快照。
        /// </summary>
        /// <returns>本次派发需要执行的同步槽位。</returns>
        private SyncSlot[] CaptureSyncSnapshot()
        {
            lock (gate)
            {
                ThrowIfDisposed();
                int count = CountActiveSyncSlots();
                if (count == 0)
                {
                    return Array.Empty<SyncSlot>();
                }

                SyncSlot[] snapshot = new SyncSlot[count];
                int cursor = 0;
                for (int index = 0; index < syncSlots.Count; index++)
                {
                    SyncSlot slot = syncSlots[index];
                    if (slot.Active)
                    {
                        snapshot[cursor++] = slot;
                    }
                }

                if (snapshot.Length > 1)
                {
                    Array.Sort(snapshot, SyncSlotRegistrationOrderComparer.Instance);
                }

                return snapshot;
            }
        }

        /// <summary>
        /// 获取当前异步监听器的稳定快照。
        /// </summary>
        /// <returns>本次派发需要执行的异步槽位。</returns>
        private AsyncSlot[] CaptureAsyncSnapshot()
        {
            lock (gate)
            {
                ThrowIfDisposed();
                int count = CountActiveAsyncSlots();
                if (count == 0)
                {
                    return Array.Empty<AsyncSlot>();
                }

                AsyncSlot[] snapshot = new AsyncSlot[count];
                int cursor = 0;
                for (int index = 0; index < asyncSlots.Count; index++)
                {
                    AsyncSlot slot = asyncSlots[index];
                    if (slot.Active)
                    {
                        snapshot[cursor++] = slot;
                    }
                }

                if (snapshot.Length > 1)
                {
                    Array.Sort(snapshot, AsyncSlotRegistrationOrderComparer.Instance);
                }

                return snapshot;
            }
        }

        /// <summary>
        /// 唤醒当前事件类型的全部一次性等待者。
        /// </summary>
        /// <param name="@event">需要交给等待者的事件数据。</param>
        private void CompleteWaiters(TEvent @event)
        {
            Waiter[] snapshot;
            lock (gate)
            {
                if (waiters.Count == 0)
                {
                    return;
                }

                snapshot = waiters.ToArray();
                waiters.Clear();
            }

            for (int index = 0; index < snapshot.Length; index++)
            {
                snapshot[index].Completion.TrySetResult(@event);
            }
        }

        /// <summary>
        /// 从活动等待集合中移除指定等待者。
        /// </summary>
        /// <param name="waiter">要移除的等待者。</param>
        /// <returns>等待者仍然活动并被本次调用移除时返回 true。</returns>
        private bool RemoveWaiter(Waiter waiter)
        {
            lock (gate)
            {
                int index = waiters.IndexOf(waiter);
                if (index < 0)
                {
                    return false;
                }

                waiters.RemoveAt(index);
                return true;
            }
        }

        /// <summary>
        /// 租用一个同步订阅槽位。
        /// </summary>
        /// <param name="generation">租用后的槽位版本。</param>
        /// <returns>槽位编号。</returns>
        private int RentSyncSlot(out uint generation)
        {
            if (syncFreeHead >= 0)
            {
                int slotId = syncFreeHead;
                SyncSlot slot = syncSlots[slotId];
                syncFreeHead = slot.NextFree;
                slot.NextFree = -1;
                slot.Generation++;
                if (slot.Generation == 0)
                {
                    slot.Generation = 1;
                }

                syncSlots[slotId] = slot;
                generation = slot.Generation;
                return slotId;
            }

            SyncSlot created = new SyncSlot { Generation = 1, NextFree = -1 };
            syncSlots.Add(created);
            generation = created.Generation;
            return syncSlots.Count - 1;
        }

        /// <summary>
        /// 租用一个异步订阅槽位。
        /// </summary>
        /// <param name="generation">租用后的槽位版本。</param>
        /// <returns>槽位编号。</returns>
        private int RentAsyncSlot(out uint generation)
        {
            if (asyncFreeHead >= 0)
            {
                int slotId = asyncFreeHead;
                AsyncSlot slot = asyncSlots[slotId];
                asyncFreeHead = slot.NextFree;
                slot.NextFree = -1;
                slot.Generation++;
                if (slot.Generation == 0)
                {
                    slot.Generation = 1;
                }

                asyncSlots[slotId] = slot;
                generation = slot.Generation;
                return slotId;
            }

            AsyncSlot created = new AsyncSlot { Generation = 1, NextFree = -1 };
            asyncSlots.Add(created);
            generation = created.Generation;
            return asyncSlots.Count - 1;
        }

        /// <summary>
        /// 按版本释放一个同步订阅槽位。
        /// </summary>
        /// <param name="slotId">槽位编号。</param>
        /// <param name="generation">订阅时记录的槽位版本。</param>
        private void ReleaseSyncSlot(int slotId, uint generation)
        {
            if (slotId < 0 || slotId >= syncSlots.Count)
            {
                return;
            }

            SyncSlot slot = syncSlots[slotId];
            if (!slot.Active || slot.Generation != generation)
            {
                return;
            }

            slot.Active = false;
            slot.ObjectHandler = null;
            slot.Callback = null;
            slot.NextFree = syncFreeHead;
            syncFreeHead = slotId;
            syncSlots[slotId] = slot;
        }

        /// <summary>
        /// 按版本释放一个异步订阅槽位。
        /// </summary>
        /// <param name="slotId">槽位编号。</param>
        /// <param name="generation">订阅时记录的槽位版本。</param>
        private void ReleaseAsyncSlot(int slotId, uint generation)
        {
            if (slotId < 0 || slotId >= asyncSlots.Count)
            {
                return;
            }

            AsyncSlot slot = asyncSlots[slotId];
            if (!slot.Active || slot.Generation != generation)
            {
                return;
            }

            slot.Active = false;
            slot.ObjectHandler = null;
            slot.Callback = null;
            slot.NextFree = asyncFreeHead;
            asyncFreeHead = slotId;
            asyncSlots[slotId] = slot;
        }

        /// <summary>
        /// 统计活动同步订阅数量。
        /// </summary>
        /// <returns>活动同步订阅数量。</returns>
        private int CountActiveSyncSlots()
        {
            int count = 0;
            for (int index = 0; index < syncSlots.Count; index++)
            {
                if (syncSlots[index].Active)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 统计活动异步订阅数量。
        /// </summary>
        /// <returns>活动异步订阅数量。</returns>
        private int CountActiveAsyncSlots()
        {
            int count = 0;
            for (int index = 0; index < asyncSlots.Count; index++)
            {
                if (asyncSlots[index].Active)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 验证事件实例和频道状态。
        /// </summary>
        /// <param name="@event">待派发事件。</param>
        private void ValidateEvent(TEvent @event)
        {
            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event));
            }

            lock (gate)
            {
                ThrowIfDisposed();
            }
        }

        /// <summary>
        /// 在存在监听器失败时抛出聚合异常。
        /// </summary>
        /// <param name="exceptions">执行期间收集的异常。</param>
        private static void ThrowAggregate(List<Exception> exceptions)
        {
            if (exceptions != null)
            {
                throw new AggregateException(exceptions);
            }
        }

        /// <summary>
        /// 在频道已销毁时阻止继续操作。
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(typeof(TEvent).FullName);
            }
        }

        #endregion

        #region Private 私有类型

        private struct SyncSlot
        {
            internal IEventHandler<TEvent> ObjectHandler;
            internal Action<TEvent> Callback;
            internal uint Generation;
            internal long RegistrationOrder;
            internal int NextFree;
            internal bool Active;

            internal void Invoke(TEvent @event)
            {
                if (ObjectHandler != null)
                {
                    ObjectHandler.Handle(@event);
                }
                else
                {
                    Callback?.Invoke(@event);
                }
            }
        }

        private struct AsyncSlot
        {
            internal IAsyncEventHandler<TEvent> ObjectHandler;
            internal Func<TEvent, MTask> Callback;
            internal uint Generation;
            internal long RegistrationOrder;
            internal int NextFree;
            internal bool Active;

            internal MTask InvokeAsync(TEvent @event)
            {
                if (ObjectHandler != null)
                {
                    return ObjectHandler.HandleAsync(@event);
                }

                return Callback != null ? Callback(@event) : MTask.CompletedTask;
            }
        }

        private sealed class Waiter
        {
            internal readonly MTaskCompletionSource<TEvent> Completion = new MTaskCompletionSource<TEvent>();
        }

        /// <summary>
        /// 按同步监听器注册序号比较快照槽位。
        /// </summary>
        private sealed class SyncSlotRegistrationOrderComparer : IComparer<SyncSlot>
        {
            internal static readonly SyncSlotRegistrationOrderComparer Instance = new SyncSlotRegistrationOrderComparer(); // 无分配的同步快照比较器。

            /// <summary>
            /// 比较两个同步槽位的注册先后。
            /// </summary>
            /// <param name="left">左侧同步槽位。</param>
            /// <param name="right">右侧同步槽位。</param>
            /// <returns>负数表示左侧先注册，正数表示右侧先注册。</returns>
            public int Compare(SyncSlot left, SyncSlot right)
            {
                return left.RegistrationOrder.CompareTo(right.RegistrationOrder);
            }
        }

        /// <summary>
        /// 按异步监听器注册序号比较快照槽位。
        /// </summary>
        private sealed class AsyncSlotRegistrationOrderComparer : IComparer<AsyncSlot>
        {
            internal static readonly AsyncSlotRegistrationOrderComparer Instance = new AsyncSlotRegistrationOrderComparer(); // 无分配的异步快照比较器。

            /// <summary>
            /// 比较两个异步槽位的注册先后。
            /// </summary>
            /// <param name="left">左侧异步槽位。</param>
            /// <param name="right">右侧异步槽位。</param>
            /// <returns>负数表示左侧先注册，正数表示右侧先注册。</returns>
            public int Compare(AsyncSlot left, AsyncSlot right)
            {
                return left.RegistrationOrder.CompareTo(right.RegistrationOrder);
            }
        }

        #endregion
    }

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

    /// <summary>
    /// 可放入 GlobalScope 或 ComponentGroup 的局部事件频道组件。
    /// 不同 Scope 或 Group 创建的实例互不共享监听器与等待者。
    /// </summary>
    public sealed class ScopedEventBus : AComponent, IEventBus
    {
        #region Private 私有成员

        private readonly EventBusCore core = new EventBusCore(); // 当前局部范围的实际事件分发核心。

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
        /// 在 Scope 或 Group 释放时终止本地频道的全部活动等待。
        /// </summary>
        protected override void OnDispose()
        {
            core.Dispose();
        }

        #endregion
    }
}
