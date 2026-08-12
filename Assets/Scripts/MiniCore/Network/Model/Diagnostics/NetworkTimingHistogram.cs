using System;
using System.Diagnostics;
using System.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// 为压测诊断提供固定容量、无逐样本分配的耗时百分位直方图。
    /// </summary>
    internal sealed class NetworkTimingHistogram
    {
        #region Private 私有成员

        private const double BucketWidthMilliseconds = 0.25d; // 每个常规时间桶覆盖的毫秒宽度。
        private const double MaximumTrackedMilliseconds = 1024d; // 常规时间桶可精确覆盖的最大耗时。
        private const int OverflowBucketIndex = (int)(MaximumTrackedMilliseconds / BucketWidthMilliseconds); // 超出常规范围的溢出桶下标。

        private readonly int[] bucketCounts = new int[OverflowBucketIndex + 1]; // 预分配的时间桶计数，最后一个为溢出桶。
        private long sampleCount; // 当前统计周期内记录的有效样本数量。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 记录一条 Stopwatch 耗时样本。
        /// </summary>
        /// <param name="elapsedTicks">需要记录的非负 Stopwatch tick 耗时。</param>
        internal void Record(long elapsedTicks)
        {
            if (elapsedTicks < 0)
            {
                return;
            }

            double elapsedMilliseconds = elapsedTicks * 1000d / Stopwatch.Frequency;
            int bucketIndex = elapsedMilliseconds >= MaximumTrackedMilliseconds
                ? OverflowBucketIndex
                : Math.Min(OverflowBucketIndex - 1, (int)(elapsedMilliseconds / BucketWidthMilliseconds));
            Interlocked.Increment(ref bucketCounts[bucketIndex]);
            Interlocked.Increment(ref sampleCount);
        }

        /// <summary>
        /// 清空当前统计周期，复用已分配的时间桶数组。
        /// </summary>
        internal void Reset()
        {
            Array.Clear(bucketCounts, 0, bucketCounts.Length);
            Interlocked.Exchange(ref sampleCount, 0);
        }

        /// <summary>
        /// 读取当前时间桶的 P50、P95、P99 汇总。
        /// </summary>
        /// <returns>带有固定桶精度的百分位快照。</returns>
        internal NetworkTimingPercentileSummary GetSummary()
        {
            long count = Interlocked.Read(ref sampleCount);
            if (count <= 0)
            {
                return default;
            }

            return new NetworkTimingPercentileSummary(
                count,
                FindPercentileMilliseconds(count, 0.50d),
                FindPercentileMilliseconds(count, 0.95d),
                FindPercentileMilliseconds(count, 0.99d));
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 在固定时间桶中读取指定最近秩百分位的上界毫秒值。
        /// </summary>
        /// <param name="count">当前有效样本总数。</param>
        /// <param name="percentile">零到一之间的目标百分位。</param>
        /// <returns>该百分位所在时间桶的上界；溢出时返回最大跟踪值。</returns>
        private double FindPercentileMilliseconds(long count, double percentile)
        {
            long targetRank = Math.Max(1L, (long)Math.Ceiling(count * percentile));
            long accumulated = 0;
            for (int index = 0; index < bucketCounts.Length; index++)
            {
                accumulated += Volatile.Read(ref bucketCounts[index]);
                if (accumulated >= targetRank)
                {
                    return index == OverflowBucketIndex
                        ? MaximumTrackedMilliseconds
                        : (index + 1) * BucketWidthMilliseconds;
                }
            }

            return MaximumTrackedMilliseconds;
        }

        #endregion
    }
}
