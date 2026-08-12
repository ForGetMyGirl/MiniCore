using System;
using System.Diagnostics;
using System.Threading;

namespace MiniCore.Model
{

    /// <summary>
    /// 表示固定时间桶采样器的百分位快照。
    /// </summary>
    internal readonly struct NetworkTimingPercentileSummary
    {
        #region Public 公共成员

        /// <summary>
        /// 获取参与百分位计算的有效样本数量。
        /// </summary>
        public long SampleCount { get; }

        /// <summary>
        /// 获取 P50 耗时，单位为毫秒。
        /// </summary>
        public double P50Milliseconds { get; }

        /// <summary>
        /// 获取 P95 耗时，单位为毫秒。
        /// </summary>
        public double P95Milliseconds { get; }

        /// <summary>
        /// 获取 P99 耗时，单位为毫秒。
        /// </summary>
        public double P99Milliseconds { get; }

        /// <summary>
        /// 使用固定时间桶的统计值创建百分位快照。
        /// </summary>
        /// <param name="sampleCount">参与计算的有效样本数量。</param>
        /// <param name="p50Milliseconds">P50 耗时。</param>
        /// <param name="p95Milliseconds">P95 耗时。</param>
        /// <param name="p99Milliseconds">P99 耗时。</param>
        internal NetworkTimingPercentileSummary(long sampleCount, double p50Milliseconds, double p95Milliseconds, double p99Milliseconds)
        {
            SampleCount = sampleCount;
            P50Milliseconds = p50Milliseconds;
            P95Milliseconds = p95Milliseconds;
            P99Milliseconds = p99Milliseconds;
        }

        #endregion
    }
}
