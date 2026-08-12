using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
    /// <summary>
    /// MTask.Yield 的 awaiter。
    /// </summary>
    public readonly struct MTaskYieldAwaiter : ICriticalNotifyCompletion
    {
        #region Private 私有成员

        private readonly IMTaskExecutor executor; // 续体执行器。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取让出操作始终需要异步调度。
        /// </summary>
        public bool IsCompleted => false;

        /// <summary>
        /// 创建让出操作 awaiter。
        /// </summary>
        /// <param name="executor">续体执行器。</param>
        public MTaskYieldAwaiter(IMTaskExecutor executor)
        {
            this.executor = executor;
        }

        /// <summary>
        /// 将续体放到执行器队列尾部。
        /// </summary>
        /// <param name="continuation">待执行续体。</param>
        public void OnCompleted(Action continuation)
        {
            executor.Post(continuation);
        }

        /// <summary>
        /// 将续体放到执行器队列尾部且不捕获 ExecutionContext。
        /// </summary>
        /// <param name="continuation">待执行续体。</param>
        public void UnsafeOnCompleted(Action continuation)
        {
            executor.Post(continuation);
        }

        /// <summary>
        /// 在恢复时检查当前任务取消状态。
        /// </summary>
        public void GetResult()
        {
            MTask.ThrowIfCancellationRequested();
        }

        #endregion
    }
}
