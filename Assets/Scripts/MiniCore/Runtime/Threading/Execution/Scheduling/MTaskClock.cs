using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MiniCore.Threading
{

    /// <summary>
    /// 将 TimeSpan 转换为 Stopwatch 单调时钟刻度。
    /// </summary>
    internal static class MTaskClock
    {
        #region Internal 内部成员

        /// <summary>
        /// 获取当前单调时钟刻度。
        /// </summary>
        internal static long Timestamp => Stopwatch.GetTimestamp();

        /// <summary>
        /// 将时间间隔转换为单调时钟刻度。
        /// </summary>
        /// <param name="timeSpan">要转换的时间间隔。</param>
        /// <returns>对应的 Stopwatch 刻度。</returns>
        internal static long FromTimeSpan(TimeSpan timeSpan)
        {
            return (long)(timeSpan.TotalSeconds * Stopwatch.Frequency);
        }

        /// <summary>
        /// 将 Stopwatch 刻度转换为毫秒。
        /// </summary>
        /// <param name="ticks">Stopwatch 刻度。</param>
        /// <returns>向上取整后的毫秒数。</returns>
        internal static int ToMilliseconds(long ticks)
        {
            double milliseconds = ticks * 1000d / Stopwatch.Frequency;
            return milliseconds >= int.MaxValue ? int.MaxValue : (int)Math.Ceiling(milliseconds);
        }

        #endregion
    }
}
