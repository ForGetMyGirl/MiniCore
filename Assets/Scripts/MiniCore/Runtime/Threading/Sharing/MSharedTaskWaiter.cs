using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
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
}
