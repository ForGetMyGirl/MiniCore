using System;
using System.Diagnostics;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// 收集单轮网络压测的端到端延迟样本，并在结束时计算百分位结果。
    /// 仅由 Unity 主线程的压测执行器访问，不在收发热路径分配集合节点。
    /// </summary>
    public sealed class NetworkBenchmarkLatencyCollector
    {
        #region Private 私有成员

        private readonly long[] latencyTicks; // 预分配的延迟 tick 样本数组。
        private int sampleCount; // 当前有效延迟样本数量。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取可记录的最大延迟样本数量。
        /// </summary>
        public int Capacity => latencyTicks.Length;

        /// <summary>
        /// 获取当前已记录的有效延迟样本数量。
        /// </summary>
        public int Count => sampleCount;

        /// <summary>
        /// 使用固定容量创建延迟样本收集器。
        /// </summary>
        /// <param name="capacity">单轮压测可能产生的最大样本数。</param>
        public NetworkBenchmarkLatencyCollector(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            latencyTicks = new long[capacity];
        }

        /// <summary>
        /// 清空上一轮压测记录的样本计数，保留已分配数组以供下一轮复用。
        /// </summary>
        public void Reset()
        {
            sampleCount = 0;
        }

        /// <summary>
        /// 记录一条已完成的端到端延迟样本。
        /// </summary>
        /// <param name="elapsedTicks">从发送开始到收到业务处理通知的 Stopwatch tick 数。</param>
        public void Add(long elapsedTicks)
        {
            if (elapsedTicks < 0 || sampleCount >= latencyTicks.Length)
            {
                return;
            }

            latencyTicks[sampleCount] = elapsedTicks;
            sampleCount++;
        }

        /// <summary>
        /// 计算当前样本的 P50、P95、P99 与最大延迟，单位为毫秒。
        /// 计算后样本数组会按升序排列，调用方应在下一轮开始前执行 Reset。
        /// </summary>
        /// <returns>当前样本的延迟汇总；没有样本时所有耗时为零。</returns>
        public NetworkBenchmarkLatencySummary CalculateSummary()
        {
            if (sampleCount == 0)
            {
                return new NetworkBenchmarkLatencySummary(0, 0d, 0d, 0d, 0d);
            }

            Array.Sort(latencyTicks, 0, sampleCount);
            return new NetworkBenchmarkLatencySummary(
                sampleCount,
                ToMilliseconds(latencyTicks[GetPercentileIndex(0.50d)]),
                ToMilliseconds(latencyTicks[GetPercentileIndex(0.95d)]),
                ToMilliseconds(latencyTicks[GetPercentileIndex(0.99d)]),
                ToMilliseconds(latencyTicks[sampleCount - 1]));
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 将百分位数转换为当前已排序数组中的最近秩索引。
        /// </summary>
        /// <param name="percentile">取值范围为零到一的目标百分位。</param>
        /// <returns>对应的零基数组索引。</returns>
        private int GetPercentileIndex(double percentile)
        {
            int index = (int)Math.Ceiling(sampleCount * percentile) - 1;
            return Math.Max(0, Math.Min(sampleCount - 1, index));
        }

        /// <summary>
        /// 将 Stopwatch tick 换算为毫秒。
        /// </summary>
        /// <param name="ticks">需要换算的 Stopwatch tick 数。</param>
        /// <returns>对应的毫秒数。</returns>
        private static double ToMilliseconds(long ticks)
        {
            return ticks * 1000d / Stopwatch.Frequency;
        }

        #endregion
    }

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
