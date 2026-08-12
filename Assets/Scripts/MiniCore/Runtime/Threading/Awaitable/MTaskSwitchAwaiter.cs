using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
    /// <summary>
    /// MTask.SwitchTo 的 awaiter。
    /// </summary>
    public readonly struct MTaskSwitchAwaiter : ICriticalNotifyCompletion
    {
        #region Private 私有成员

        private readonly IMTaskExecutor executor; // 目标执行器。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取当前是否已经处于目标执行器线程。
        /// </summary>
        public bool IsCompleted => executor.IsCurrentThread;

        /// <summary>
        /// 创建执行器切换 awaiter。
        /// </summary>
        /// <param name="executor">目标执行器。</param>
        public MTaskSwitchAwaiter(IMTaskExecutor executor)
        {
            this.executor = executor;
        }

        /// <summary>
        /// 将续体派发到目标执行器。
        /// </summary>
        /// <param name="continuation">待执行续体。</param>
        public void OnCompleted(Action continuation)
        {
            MTaskRuntime.SwitchCurrentExecutor(executor);
            executor.Post(continuation);
        }

        /// <summary>
        /// 将续体派发到目标执行器且不捕获 ExecutionContext。
        /// </summary>
        /// <param name="continuation">待执行续体。</param>
        public void UnsafeOnCompleted(Action continuation)
        {
            MTaskRuntime.SwitchCurrentExecutor(executor);
            executor.Post(continuation);
        }

        /// <summary>
        /// 完成执行器切换并检查取消状态。
        /// </summary>
        public void GetResult()
        {
            MTaskRuntime.SwitchCurrentExecutor(executor);
            MTask.ThrowIfCancellationRequested();
        }

        #endregion
    }
}
