using System;
using System.Diagnostics;

namespace MiniCore.HotUpdate
{

    /// <summary>
    /// 表示一轮网络压测已完成延迟样本的百分位汇总。
    /// </summary>
    public struct NetworkBenchmarkLatencySummary
    {
        #region Public 公共成员

        /// <summary>
        /// 获取参与计算的有效样本数量。
        /// </summary>
        public int SampleCount { get; }

        /// <summary>
        /// 获取 P50 端到端延迟，单位为毫秒。
        /// </summary>
        public double P50Milliseconds { get; }

        /// <summary>
        /// 获取 P95 端到端延迟，单位为毫秒。
        /// </summary>
        public double P95Milliseconds { get; }

        /// <summary>
        /// 获取 P99 端到端延迟，单位为毫秒。
        /// </summary>
        public double P99Milliseconds { get; }

        /// <summary>
        /// 获取最大端到端延迟，单位为毫秒。
        /// </summary>
        public double MaxMilliseconds { get; }

        /// <summary>
        /// 使用延迟统计值创建汇总结果。
        /// </summary>
        /// <param name="sampleCount">参与计算的有效样本数量。</param>
        /// <param name="p50Milliseconds">P50 端到端延迟。</param>
        /// <param name="p95Milliseconds">P95 端到端延迟。</param>
        /// <param name="p99Milliseconds">P99 端到端延迟。</param>
        /// <param name="maxMilliseconds">最大端到端延迟。</param>
        public NetworkBenchmarkLatencySummary(int sampleCount, double p50Milliseconds, double p95Milliseconds, double p99Milliseconds, double maxMilliseconds)
        {
            SampleCount = sampleCount;
            P50Milliseconds = p50Milliseconds;
            P95Milliseconds = p95Milliseconds;
            P99Milliseconds = p99Milliseconds;
            MaxMilliseconds = maxMilliseconds;
        }

        #endregion
    }
}
