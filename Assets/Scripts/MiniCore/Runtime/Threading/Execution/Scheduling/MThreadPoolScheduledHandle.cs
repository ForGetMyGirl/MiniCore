#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MiniCore.Threading
{

    /// <summary>
    /// CLR 线程池执行器使用的一次性延迟派发句柄。
    /// </summary>
    internal sealed class MThreadPoolScheduledHandle : IMTaskScheduledHandle
    {
        #region Private 私有成员

        private static readonly TimerCallback TimerCallback = OnTimer; // 避免每次延迟任务创建计时器回调委托。

        private MThreadPoolExecutor executor; // 到期后负责投递续体的线程池执行器。
        private Action continuation; // 延迟到期后需要执行的续体。
        private Timer timer; // 一次性系统计时器。
        private int canceled; // 是否已经取消或执行完毕。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建延迟句柄并启动计时器。
        /// </summary>
        /// <param name="executor">到期后负责投递续体的执行器。</param>
        /// <param name="value">到期后需要执行的续体。</param>
        /// <param name="delay">非负延迟时间。</param>
        /// <returns>已启动的一次性延迟句柄。</returns>
        internal static MThreadPoolScheduledHandle Rent(MThreadPoolExecutor executor, Action value, TimeSpan delay)
        {
            MThreadPoolScheduledHandle handle = new MThreadPoolScheduledHandle();

            handle.executor = executor;
            handle.continuation = value;
            handle.canceled = 0;
            handle.timer = new Timer(TimerCallback, handle, delay < TimeSpan.Zero ? TimeSpan.Zero : delay, Timeout.InfiniteTimeSpan);
            return handle;
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 取消尚未到期的延迟派发。
        /// </summary>
        public void Cancel()
        {
            if (Interlocked.Exchange(ref canceled, 1) != 0)
            {
                return;
            }

            Release();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 处理计时器到期事件并将续体投递到 CLR 线程池。
        /// </summary>
        /// <param name="state">当前延迟句柄。</param>
        private static void OnTimer(object state)
        {
            MThreadPoolScheduledHandle handle = (MThreadPoolScheduledHandle)state;
            if (Interlocked.Exchange(ref handle.canceled, 1) == 0)
            {
                MThreadPoolExecutor target = handle.executor;
                Action callback = handle.continuation;
                handle.Release();
                target?.Post(callback);
            }
        }

        /// <summary>
        /// 释放计时器和业务引用。
        /// </summary>
        private void Release()
        {
            Interlocked.Exchange(ref timer, null)?.Dispose();
            executor = null;
            continuation = null;
        }

        #endregion
    }
}
#endif
