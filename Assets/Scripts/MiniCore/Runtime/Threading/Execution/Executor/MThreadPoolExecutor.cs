#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MiniCore.Threading
{

    /// <summary>
    /// 将无状态短续体投递到 CLR 线程池的 MTask 执行器。
    /// </summary>
    public sealed class MThreadPoolExecutor : IMTaskExecutor
    {
        #region Private 私有成员

        private static readonly WaitCallback ExecuteWorkItem = Execute; // 避免每次投递创建 WaitCallback 委托。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取执行器诊断名称。
        /// </summary>
        public string Name => "ThreadPool";

        /// <summary>
        /// CLR 线程池没有固定线程归属，因此始终要求异步投递。
        /// </summary>
        public bool IsCurrentThread => false;

        /// <summary>
        /// 将续体投递到 CLR 线程池。
        /// </summary>
        /// <param name="continuation">需要在线程池工作线程执行的续体。</param>
        public void Post(Action continuation)
        {
            if (continuation == null)
            {
                throw new ArgumentNullException(nameof(continuation));
            }

            MThreadPoolWorkItem workItem = MThreadPoolWorkItem.Rent(continuation);
            ThreadPool.QueueUserWorkItem(ExecuteWorkItem, workItem);
        }

        /// <summary>
        /// 在延迟到期后将续体投递到 CLR 线程池。
        /// </summary>
        /// <param name="continuation">需要在线程池工作线程执行的续体。</param>
        /// <param name="delay">非负延迟时间。</param>
        /// <returns>可撤销延迟派发的句柄。</returns>
        public IMTaskScheduledHandle Schedule(Action continuation, TimeSpan delay)
        {
            if (continuation == null)
            {
                throw new ArgumentNullException(nameof(continuation));
            }

            return MThreadPoolScheduledHandle.Rent(this, continuation, delay);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 在线程池工作线程执行并回收工作项。
        /// </summary>
        /// <param name="state">池化的工作项。</param>
        private static void Execute(object state)
        {
            MThreadPoolWorkItem workItem = (MThreadPoolWorkItem)state;
            try
            {
                workItem.Invoke();
            }
            catch (Exception exception)
            {
                MTaskSupervisor.Report(exception, "ThreadPool");
            }
            finally
            {
                workItem.Return();
            }
        }

        #endregion
    }
}
#endif
