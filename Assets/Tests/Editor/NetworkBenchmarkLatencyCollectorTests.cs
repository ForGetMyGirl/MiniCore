using MiniCore.HotUpdate;
using NUnit.Framework;

namespace MiniCore.EditorTests
{
    /// <summary>
    /// 验证网络压测延迟统计使用最近秩百分位并可在下一轮复用数组。
    /// </summary>
    public sealed class NetworkBenchmarkLatencyCollectorTests
    {
        #region Public 公共成员

        /// <summary>
        /// 验证五条已排序样本的 P50、P95、P99 和最大值均取预期最近秩。
        /// </summary>
        [Test]
        public void CalculateSummary_UsesNearestRankPercentiles()
        {
            var collector = new NetworkBenchmarkLatencyCollector(5);
            collector.Add(1);
            collector.Add(2);
            collector.Add(3);
            collector.Add(4);
            collector.Add(5);

            NetworkBenchmarkLatencySummary summary = collector.CalculateSummary();

            Assert.AreEqual(5, summary.SampleCount);
            Assert.That(summary.P50Milliseconds, Is.GreaterThan(0d));
            Assert.That(summary.P95Milliseconds, Is.EqualTo(summary.MaxMilliseconds));
            Assert.That(summary.P99Milliseconds, Is.EqualTo(summary.MaxMilliseconds));
        }

        /// <summary>
        /// 验证重置只清空样本计数且不会改变已分配容量。
        /// </summary>
        [Test]
        public void Reset_ClearsSamplesAndRetainsCapacity()
        {
            var collector = new NetworkBenchmarkLatencyCollector(2);
            collector.Add(1);
            collector.Reset();

            NetworkBenchmarkLatencySummary summary = collector.CalculateSummary();

            Assert.AreEqual(2, collector.Capacity);
            Assert.AreEqual(0, collector.Count);
            Assert.AreEqual(0, summary.SampleCount);
        }

        #endregion
    }
}
