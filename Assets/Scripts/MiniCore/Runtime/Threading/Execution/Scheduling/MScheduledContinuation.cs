using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MiniCore.Threading
{

    /// <summary>
    /// 可池化的延迟续体记录。
    /// </summary>
    internal sealed class MScheduledContinuation : IMTaskScheduledHandle
    {
        #region Private 私有成员

        private Action continuation; // 到期后执行的续体。
        private int canceled; // 是否已经取消。

        #endregion

        #region Internal 内部成员

        internal long DueTimestamp; // 到期时间戳。

        /// <summary>
        /// 从池中获取一条延迟记录。
        /// </summary>
        /// <param name="continuation">到期后执行的续体。</param>
        /// <param name="delay">延迟时间。</param>
        /// <returns>初始化后的延迟记录。</returns>
        internal static MScheduledContinuation Rent(Action continuation, TimeSpan delay)
        {
            if (!MTaskObjectPool<MScheduledContinuation>.TryRent(out MScheduledContinuation scheduled))
            {
                scheduled = new MScheduledContinuation();
            }

            scheduled.continuation = continuation ?? throw new ArgumentNullException(nameof(continuation));
            scheduled.canceled = 0;
            scheduled.DueTimestamp = MTaskClock.Timestamp + MTaskClock.FromTimeSpan(delay < TimeSpan.Zero ? TimeSpan.Zero : delay);
            MTaskDiagnostics.OnTimerActivated();
            return scheduled;
        }

        /// <summary>
        /// 获取延迟项是否已经取消。
        /// </summary>
        internal bool IsCanceled => Volatile.Read(ref canceled) != 0;

        /// <summary>
        /// 尝试取得尚未取消的续体。
        /// </summary>
        /// <param name="callback">成功取得的回调。</param>
        /// <returns>存在可执行回调时返回 true。</returns>
        internal bool TryTake(out Action callback)
        {
            if (IsCanceled)
            {
                callback = null;
                return false;
            }

            callback = Interlocked.Exchange(ref continuation, null);
            return callback != null;
        }

        /// <summary>
        /// 清理状态并归还共享池。
        /// </summary>
        internal void Return()
        {
            continuation = null;
            DueTimestamp = 0;
            MTaskDiagnostics.OnTimerCompleted();
            MTaskObjectPool<MScheduledContinuation>.Return(this);
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 取消尚未触发的延迟项。
        /// </summary>
        public void Cancel()
        {
            Interlocked.Exchange(ref canceled, 1);
            Interlocked.Exchange(ref continuation, null);
        }

        #endregion
    }
}
