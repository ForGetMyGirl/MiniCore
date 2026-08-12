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
}
