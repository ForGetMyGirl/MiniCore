using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
    /// <summary>
    /// 显式允许多个等待者观察同一结果的无返回值任务。
    /// </summary>
    public sealed class MSharedTask
    {
        #region Private 私有成员

        private readonly object gate = new object(); // 保护共享完成状态与等待者。
        private readonly Action completeAction; // 消费底层 MTask 的稳定回调。
        private MTaskAwaiter underlyingAwaiter; // 被共享的单次消费 awaiter。
        private List<MSharedTaskWaiter> waiters; // 当前已挂起的多个等待者。
        private Exception exception; // 底层任务的失败或取消原因。
        private int completed; // 共享结果是否已经确定。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 创建一个显式共享任务并立即接管底层任务的唯一消费权。
        /// </summary>
        /// <param name="task">需要共享的单次消费任务。</param>
        public MSharedTask(MTask task)
        {
            completeAction = CompleteUnderlying;
            underlyingAwaiter = task.GetAwaiter();
            if (underlyingAwaiter.IsCompleted)
            {
                CompleteUnderlying();
            }
            else
            {
                underlyingAwaiter.UnsafeOnCompleted(completeAction);
            }
        }

        /// <summary>
        /// 获取支持独立取消的共享任务等待器。
        /// </summary>
        /// <returns>当前调用方专用的等待器。</returns>
        public MSharedTaskAwaiter GetAwaiter()
        {
            return new MSharedTaskAwaiter(this, MSharedTaskWaiter.Rent(this));
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 获取共享结果是否已经完成。
        /// </summary>
        internal bool IsCompleted => Volatile.Read(ref completed) != 0;

        /// <summary>
        /// 注册一个需要在共享结果完成时唤醒的等待者。
        /// </summary>
        /// <param name="waiter">当前调用方等待者。</param>
        internal void Register(MSharedTaskWaiter waiter)
        {
            bool completeNow;
            lock (gate)
            {
                completeNow = completed != 0;
                if (!completeNow)
                {
                    (waiters ??= new List<MSharedTaskWaiter>(2)).Add(waiter);
                }
            }

            if (completeNow)
            {
                waiter.Complete();
            }
        }

        /// <summary>
        /// 传播底层共享任务的最终结果。
        /// </summary>
        internal void GetResult()
        {
            Exception value = exception;
            if (value != null)
            {
                throw value;
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 唯一一次消费底层 MTask 并唤醒全部共享等待者。
        /// </summary>
        private void CompleteUnderlying()
        {
            try
            {
                underlyingAwaiter.GetResult();
            }
            catch (Exception value)
            {
                exception = value;
            }

            List<MSharedTaskWaiter> snapshot;
            lock (gate)
            {
                completed = 1;
                snapshot = waiters;
                waiters = null;
            }

            if (snapshot == null)
            {
                return;
            }

            for (int i = 0; i < snapshot.Count; i++)
            {
                snapshot[i].Complete();
            }
        }

        #endregion
    }

    /// <summary>
    /// 显式允许多个等待者观察同一结果的带返回值任务。
    /// </summary>
    /// <typeparam name="T">共享结果类型。</typeparam>
    public sealed class MSharedTask<T>
    {
        #region Private 私有成员

        private readonly object gate = new object(); // 保护共享完成状态与等待者。
        private readonly Action completeAction; // 消费底层 MTask 的稳定回调。
        private MTaskAwaiter<T> underlyingAwaiter; // 被共享的单次消费 awaiter。
        private List<MSharedTaskWaiter<T>> waiters; // 当前已挂起的多个等待者。
        private Exception exception; // 底层任务的失败或取消原因。
        private T result; // 底层任务的成功结果。
        private int completed; // 共享结果是否已经确定。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 创建一个显式共享任务并立即接管底层任务的唯一消费权。
        /// </summary>
        /// <param name="task">需要共享的单次消费任务。</param>
        public MSharedTask(MTask<T> task)
        {
            completeAction = CompleteUnderlying;
            underlyingAwaiter = task.GetAwaiter();
            if (underlyingAwaiter.IsCompleted)
            {
                CompleteUnderlying();
            }
            else
            {
                underlyingAwaiter.UnsafeOnCompleted(completeAction);
            }
        }

        /// <summary>
        /// 获取支持独立取消的共享任务等待器。
        /// </summary>
        /// <returns>当前调用方专用的等待器。</returns>
        public MSharedTaskAwaiter<T> GetAwaiter()
        {
            return new MSharedTaskAwaiter<T>(this, MSharedTaskWaiter<T>.Rent(this));
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 获取共享结果是否已经完成。
        /// </summary>
        internal bool IsCompleted => Volatile.Read(ref completed) != 0;

        /// <summary>
        /// 注册一个需要在共享结果完成时唤醒的等待者。
        /// </summary>
        /// <param name="waiter">当前调用方等待者。</param>
        internal void Register(MSharedTaskWaiter<T> waiter)
        {
            bool completeNow;
            lock (gate)
            {
                completeNow = completed != 0;
                if (!completeNow)
                {
                    (waiters ??= new List<MSharedTaskWaiter<T>>(2)).Add(waiter);
                }
            }

            if (completeNow)
            {
                waiter.Complete();
            }
        }

        /// <summary>
        /// 传播底层共享任务的最终结果。
        /// </summary>
        /// <returns>底层任务成功返回的值。</returns>
        internal T GetResult()
        {
            Exception value = exception;
            if (value != null)
            {
                throw value;
            }

            return result;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 唯一一次消费底层 MTask 并唤醒全部共享等待者。
        /// </summary>
        private void CompleteUnderlying()
        {
            try
            {
                result = underlyingAwaiter.GetResult();
            }
            catch (Exception value)
            {
                exception = value;
            }

            List<MSharedTaskWaiter<T>> snapshot;
            lock (gate)
            {
                completed = 1;
                snapshot = waiters;
                waiters = null;
            }

            if (snapshot == null)
            {
                return;
            }

            for (int i = 0; i < snapshot.Count; i++)
            {
                snapshot[i].Complete();
            }
        }

        #endregion
    }

    /// <summary>
    /// 无返回值共享任务的等待器。
    /// </summary>
    public readonly struct MSharedTaskAwaiter : ICriticalNotifyCompletion
    {
        #region Private 私有成员

        private readonly MSharedTask owner; // 共享结果所有者。
        private readonly MSharedTaskWaiter waiter; // 当前 await 专用等待者。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建共享任务等待器。
        /// </summary>
        /// <param name="owner">共享任务。</param>
        /// <param name="waiter">当前调用方等待者。</param>
        internal MSharedTaskAwaiter(MSharedTask owner, MSharedTaskWaiter waiter)
        {
            this.owner = owner;
            this.waiter = waiter;
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取共享结果或当前等待者是否已取消。
        /// </summary>
        public bool IsCompleted => owner.IsCompleted || waiter.IsCancellationRequested;

        /// <summary>
        /// 注册共享任务完成续体。
        /// </summary>
        /// <param name="continuation">完成续体。</param>
        public void OnCompleted(Action continuation)
        {
            waiter.Register(continuation);
        }

        /// <summary>
        /// 注册不捕获 ExecutionContext 的完成续体。
        /// </summary>
        /// <param name="continuation">完成续体。</param>
        public void UnsafeOnCompleted(Action continuation)
        {
            waiter.Register(continuation);
        }

        /// <summary>
        /// 完成当前等待并传播共享结果。
        /// </summary>
        public void GetResult()
        {
            waiter.GetResult();
            owner.GetResult();
        }

        #endregion
    }

    /// <summary>
    /// 带返回值共享任务的等待器。
    /// </summary>
    /// <typeparam name="T">共享结果类型。</typeparam>
    public readonly struct MSharedTaskAwaiter<T> : ICriticalNotifyCompletion
    {
        #region Private 私有成员

        private readonly MSharedTask<T> owner; // 共享结果所有者。
        private readonly MSharedTaskWaiter<T> waiter; // 当前 await 专用等待者。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建共享任务等待器。
        /// </summary>
        /// <param name="owner">共享任务。</param>
        /// <param name="waiter">当前调用方等待者。</param>
        internal MSharedTaskAwaiter(MSharedTask<T> owner, MSharedTaskWaiter<T> waiter)
        {
            this.owner = owner;
            this.waiter = waiter;
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取共享结果或当前等待者是否已取消。
        /// </summary>
        public bool IsCompleted => owner.IsCompleted || waiter.IsCancellationRequested;

        /// <summary>
        /// 注册共享任务完成续体。
        /// </summary>
        /// <param name="continuation">完成续体。</param>
        public void OnCompleted(Action continuation)
        {
            waiter.Register(continuation);
        }

        /// <summary>
        /// 注册不捕获 ExecutionContext 的完成续体。
        /// </summary>
        /// <param name="continuation">完成续体。</param>
        public void UnsafeOnCompleted(Action continuation)
        {
            waiter.Register(continuation);
        }

        /// <summary>
        /// 完成当前等待并传播共享结果。
        /// </summary>
        /// <returns>底层共享任务的返回值。</returns>
        public T GetResult()
        {
            waiter.GetResult();
            return owner.GetResult();
        }

        #endregion
    }

    /// <summary>
    /// 无返回值共享任务的池化单等待者。
    /// </summary>
    internal sealed class MSharedTaskWaiter
    {
        #region Private 私有成员

        private readonly Action cancelAction; // 当前结构化节点取消回调。
        private MSharedTask owner; // 所属共享任务。
        private MTaskNode waitingNode; // 当前等待方节点。
        private Action continuation; // 等待方完成续体。
        private IMTaskExecutor executor; // 等待方捕获的执行器。
        private int outcome; // 0 等待中，1 共享完成，2 等待方取消。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建等待者并缓存稳定取消委托。
        /// </summary>
        private MSharedTaskWaiter()
        {
            cancelAction = Cancel;
        }

        /// <summary>
        /// 获取当前等待方是否已经取消。
        /// </summary>
        internal bool IsCancellationRequested => outcome == 2 || (waitingNode?.IsCancellationRequested ?? false);

        /// <summary>
        /// 从池中获取当前 await 专用等待者。
        /// </summary>
        /// <param name="owner">所属共享任务。</param>
        /// <returns>初始化后的等待者。</returns>
        internal static MSharedTaskWaiter Rent(MSharedTask owner)
        {
            if (!MTaskObjectPool<MSharedTaskWaiter>.TryRent(out MSharedTaskWaiter waiter))
            {
                waiter = new MSharedTaskWaiter();
            }

            waiter.owner = owner;
            waiter.waitingNode = MTaskRuntime.CurrentNode;
            waiter.continuation = null;
            waiter.executor = null;
            waiter.outcome = 0;
            return waiter;
        }

        /// <summary>
        /// 注册当前等待方的续体和取消唤醒。
        /// </summary>
        /// <param name="value">完成续体。</param>
        internal void Register(Action value)
        {
            continuation = value ?? throw new ArgumentNullException(nameof(value));
            executor = MTaskRuntime.CurrentExecutor ?? MTaskExecutors.Inline;
            waitingNode?.SetCancellationContinuation(cancelAction);
            owner.Register(this);
        }

        /// <summary>
        /// 标记共享结果完成并唤醒当前等待方。
        /// </summary>
        internal void Complete()
        {
            if (Interlocked.CompareExchange(ref outcome, 1, 0) == 0)
            {
                Schedule();
            }
        }

        /// <summary>
        /// 完成本次等待并在取消时抛出域级异常。
        /// </summary>
        internal void GetResult()
        {
            MTaskNode node = waitingNode;
            bool canceled = outcome == 2 || (node?.IsCancellationRequested ?? false);
            node?.ClearCancellationContinuation(cancelAction);
            owner = null;
            waitingNode = null;
            continuation = null;
            executor = null;
            MTaskObjectPool<MSharedTaskWaiter>.Return(this);
            if (canceled)
            {
                throw node?.Domain.CancellationException ?? MTaskRuntime.GetCancellationException();
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 响应当前等待方所属节点的取消。
        /// </summary>
        private void Cancel()
        {
            if (Interlocked.CompareExchange(ref outcome, 2, 0) == 0)
            {
                Schedule();
            }
        }

        /// <summary>
        /// 在等待方捕获的执行器上派发续体。
        /// </summary>
        private void Schedule()
        {
            Action callback = continuation;
            IMTaskExecutor target = executor ?? MTaskExecutors.Inline;
            if (target.IsCurrentThread)
            {
                callback?.Invoke();
            }
            else if (callback != null)
            {
                target.Post(callback);
            }
        }

        #endregion
    }

    /// <summary>
    /// 带返回值共享任务的池化单等待者。
    /// </summary>
    /// <typeparam name="T">共享结果类型。</typeparam>
    internal sealed class MSharedTaskWaiter<T>
    {
        #region Private 私有成员

        private readonly Action cancelAction; // 当前结构化节点取消回调。
        private MSharedTask<T> owner; // 所属共享任务。
        private MTaskNode waitingNode; // 当前等待方节点。
        private Action continuation; // 等待方完成续体。
        private IMTaskExecutor executor; // 等待方捕获的执行器。
        private int outcome; // 0 等待中，1 共享完成，2 等待方取消。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建等待者并缓存稳定取消委托。
        /// </summary>
        private MSharedTaskWaiter()
        {
            cancelAction = Cancel;
        }

        /// <summary>
        /// 获取当前等待方是否已经取消。
        /// </summary>
        internal bool IsCancellationRequested => outcome == 2 || (waitingNode?.IsCancellationRequested ?? false);

        /// <summary>
        /// 从池中获取当前 await 专用等待者。
        /// </summary>
        /// <param name="owner">所属共享任务。</param>
        /// <returns>初始化后的等待者。</returns>
        internal static MSharedTaskWaiter<T> Rent(MSharedTask<T> owner)
        {
            if (!MTaskObjectPool<MSharedTaskWaiter<T>>.TryRent(out MSharedTaskWaiter<T> waiter))
            {
                waiter = new MSharedTaskWaiter<T>();
            }

            waiter.owner = owner;
            waiter.waitingNode = MTaskRuntime.CurrentNode;
            waiter.continuation = null;
            waiter.executor = null;
            waiter.outcome = 0;
            return waiter;
        }

        /// <summary>
        /// 注册当前等待方的续体和取消唤醒。
        /// </summary>
        /// <param name="value">完成续体。</param>
        internal void Register(Action value)
        {
            continuation = value ?? throw new ArgumentNullException(nameof(value));
            executor = MTaskRuntime.CurrentExecutor ?? MTaskExecutors.Inline;
            waitingNode?.SetCancellationContinuation(cancelAction);
            owner.Register(this);
        }

        /// <summary>
        /// 标记共享结果完成并唤醒当前等待方。
        /// </summary>
        internal void Complete()
        {
            if (Interlocked.CompareExchange(ref outcome, 1, 0) == 0)
            {
                Schedule();
            }
        }

        /// <summary>
        /// 完成本次等待并在取消时抛出域级异常。
        /// </summary>
        internal void GetResult()
        {
            MTaskNode node = waitingNode;
            bool canceled = outcome == 2 || (node?.IsCancellationRequested ?? false);
            node?.ClearCancellationContinuation(cancelAction);
            owner = null;
            waitingNode = null;
            continuation = null;
            executor = null;
            MTaskObjectPool<MSharedTaskWaiter<T>>.Return(this);
            if (canceled)
            {
                throw node?.Domain.CancellationException ?? MTaskRuntime.GetCancellationException();
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 响应当前等待方所属节点的取消。
        /// </summary>
        private void Cancel()
        {
            if (Interlocked.CompareExchange(ref outcome, 2, 0) == 0)
            {
                Schedule();
            }
        }

        /// <summary>
        /// 在等待方捕获的执行器上派发续体。
        /// </summary>
        private void Schedule()
        {
            Action callback = continuation;
            IMTaskExecutor target = executor ?? MTaskExecutors.Inline;
            if (target.IsCurrentThread)
            {
                callback?.Invoke();
            }
            else if (callback != null)
            {
                target.Post(callback);
            }
        }

        #endregion
    }
}
