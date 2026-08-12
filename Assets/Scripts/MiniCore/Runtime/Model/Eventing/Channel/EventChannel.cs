using System;
using System.Collections.Generic;
using MiniCore.Model;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Eventing
{

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
}
