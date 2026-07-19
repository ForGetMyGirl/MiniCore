using System;
using System.Collections.Generic;
using System.Threading;

namespace MiniCore.Threading
{
    /// <summary>
    /// MTask 对象池和运行中任务的诊断快照。
    /// </summary>
    public readonly struct MTaskDiagnosticsSnapshot
    {
        #region Public 公共成员

        /// <summary>
        /// 获取对象池命中次数。
        /// </summary>
        public long PoolHits { get; }

        /// <summary>
        /// 获取对象池扩容创建次数。
        /// </summary>
        public long PoolExpansions { get; }

        /// <summary>
        /// 获取超出每类型容量而未回池的对象数。
        /// </summary>
        public long PoolRecycleFailures { get; }

        /// <summary>
        /// 获取当前仍在任务树中运行的节点数。
        /// </summary>
        public int ActiveNodes { get; }

        /// <summary>
        /// 获取当前仍注册在执行器中的计时项数。
        /// </summary>
        public int ActiveTimers { get; }

        /// <summary>
        /// 创建一份不可变的 MTask 诊断快照。
        /// </summary>
        /// <param name="poolHits">对象池命中次数。</param>
        /// <param name="poolExpansions">对象池扩容次数。</param>
        /// <param name="poolRecycleFailures">对象回池失败次数。</param>
        /// <param name="activeNodes">活动任务节点数。</param>
        /// <param name="activeTimers">活动计时项数。</param>
        public MTaskDiagnosticsSnapshot(
            long poolHits,
            long poolExpansions,
            long poolRecycleFailures,
            int activeNodes,
            int activeTimers)
        {
            PoolHits = poolHits;
            PoolExpansions = poolExpansions;
            PoolRecycleFailures = poolRecycleFailures;
            ActiveNodes = activeNodes;
            ActiveTimers = activeTimers;
        }

        #endregion
    }

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

    /// <summary>
    /// 为每个具体类型提供有容量上限的无闭包共享对象池。
    /// </summary>
    /// <typeparam name="T">池化引用类型。</typeparam>
    internal static class MTaskObjectPool<T> where T : class
    {
        #region Private 私有成员

        private static readonly object Gate = new object(); // 保护数组栈，避免 ConcurrentStack 回池节点分配。
        private static readonly Stack<T> Pool = new Stack<T>(16); // 当前具体类型的共享池。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 尝试从当前类型池中获取对象。
        /// </summary>
        /// <param name="value">成功获取的对象。</param>
        /// <returns>命中对象池时返回 true。</returns>
        internal static bool TryRent(out T value)
        {
            lock (Gate)
            {
                if (Pool.Count > 0)
                {
                    value = Pool.Pop();
                    MTaskDiagnostics.OnPoolHit();
                    return true;
                }
            }

            value = null;
            MTaskDiagnostics.OnPoolExpansion();
            return false;
        }

        /// <summary>
        /// 在容量限制内将对象归还当前类型池。
        /// </summary>
        /// <param name="value">已清理的对象。</param>
        internal static void Return(T value)
        {
            lock (Gate)
            {
                if (Pool.Count < MTaskDiagnostics.MaxRetainedPerType)
                {
                    Pool.Push(value);
                    return;
                }
            }

            MTaskDiagnostics.OnPoolRecycleFailure();
        }

        #endregion
    }
}
