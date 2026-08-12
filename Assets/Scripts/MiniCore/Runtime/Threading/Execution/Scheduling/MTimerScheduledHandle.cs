#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MiniCore.Threading
{

    /// <summary>
    /// 同步兜底执行器使用的一次性系统计时器句柄。
    /// </summary>
    internal sealed class MTimerScheduledHandle : IMTaskScheduledHandle
    {
        #region Private 私有成员

        private Timer timer; // 兜底环境的一次性计时器。
        private Action continuation; // 到期后执行的回调。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建一次性计时器。
        /// </summary>
        /// <param name="continuation">到期回调。</param>
        /// <param name="delay">延迟时间。</param>
        internal MTimerScheduledHandle(Action continuation, TimeSpan delay)
        {
            this.continuation = continuation ?? throw new ArgumentNullException(nameof(continuation));
            timer = new Timer(OnTimer, null, delay < TimeSpan.Zero ? TimeSpan.Zero : delay, Timeout.InfiniteTimeSpan);
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 取消一次性计时器。
        /// </summary>
        public void Cancel()
        {
            Interlocked.Exchange(ref continuation, null);
            Interlocked.Exchange(ref timer, null)?.Dispose();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 执行计时器回调并释放底层资源。
        /// </summary>
        /// <param name="state">未使用的计时器状态。</param>
        private void OnTimer(object state)
        {
            Action callback = Interlocked.Exchange(ref continuation, null);
            Interlocked.Exchange(ref timer, null)?.Dispose();
            callback?.Invoke();
        }

        #endregion
    }
}
#endif
