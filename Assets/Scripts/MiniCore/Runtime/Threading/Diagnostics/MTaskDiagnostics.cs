using System;
using System.Collections.Generic;
using System.Threading;

namespace MiniCore.Threading
{

    /// <summary>
    /// MTask 稳态分配和未退场任务的全局诊断入口。
    /// </summary>
    public static class MTaskDiagnostics
    {
        #region Private 私有成员

        private static int maxRetainedPerType = 256; // 每个具体池默认保留的最大对象数。
        private static long poolHits; // 所有池累计命中次数。
        private static long poolExpansions; // 所有池累计新建次数。
        private static long poolRecycleFailures; // 所有池超容量丢弃次数。
        private static int activeNodes; // 当前任务树活动节点数。
        private static int activeTimers; // 当前执行器活动计时项数。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取或设置每个具体池允许保留的最大对象数。
        /// </summary>
        public static int MaxRetainedPerType
        {
            get => Volatile.Read(ref maxRetainedPerType);
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                Volatile.Write(ref maxRetainedPerType, value);
            }
        }

        /// <summary>
        /// 捕获当前对象池与活动任务计数。
        /// </summary>
        /// <returns>当前诊断快照。</returns>
        public static MTaskDiagnosticsSnapshot Capture()
        {
            return new MTaskDiagnosticsSnapshot(
                Interlocked.Read(ref poolHits),
                Interlocked.Read(ref poolExpansions),
                Interlocked.Read(ref poolRecycleFailures),
                Volatile.Read(ref activeNodes),
                Volatile.Read(ref activeTimers));
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 记录一次对象池命中。
        /// </summary>
        internal static void OnPoolHit()
        {
            Interlocked.Increment(ref poolHits);
        }

        /// <summary>
        /// 记录一次对象池扩容。
        /// </summary>
        internal static void OnPoolExpansion()
        {
            Interlocked.Increment(ref poolExpansions);
        }

        /// <summary>
        /// 记录一次超容量对象回收失败。
        /// </summary>
        internal static void OnPoolRecycleFailure()
        {
            Interlocked.Increment(ref poolRecycleFailures);
        }

        /// <summary>
        /// 记录一个新任务节点进入任务树。
        /// </summary>
        internal static void OnNodeActivated()
        {
            Interlocked.Increment(ref activeNodes);
        }

        /// <summary>
        /// 记录一个任务节点完全离开任务树。
        /// </summary>
        internal static void OnNodeCompleted()
        {
            Interlocked.Decrement(ref activeNodes);
        }

        /// <summary>
        /// 记录一个计时项进入执行器。
        /// </summary>
        internal static void OnTimerActivated()
        {
            Interlocked.Increment(ref activeTimers);
        }

        /// <summary>
        /// 记录一个计时项离开执行器。
        /// </summary>
        internal static void OnTimerCompleted()
        {
            Interlocked.Decrement(ref activeTimers);
        }

        #endregion
    }
}
