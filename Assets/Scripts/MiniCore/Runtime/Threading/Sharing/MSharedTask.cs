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
}
