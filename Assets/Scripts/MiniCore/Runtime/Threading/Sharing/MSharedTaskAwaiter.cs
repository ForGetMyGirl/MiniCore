using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
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
}
