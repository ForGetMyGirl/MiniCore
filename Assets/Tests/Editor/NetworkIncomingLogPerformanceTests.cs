using System;
using MiniCore.Model;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace MiniCore.EditorTests
{
    /// <summary>
    /// 网络收包日志在日志关闭状态下的字符串分配性能基线测试。
    /// 测试只覆盖日志时间格式化和字符串插值，不包含协议反序列化、队列或业务 Handler。
    /// </summary>
    public sealed class NetworkIncomingLogPerformanceTests
    {
        #region Private 私有成员

        private const int WarmupMeasurementCount = 5; // 不计入最终报告的预热组数。
        private const int ResultMeasurementCount = 20; // 计入最终报告的测量组数。
        private const int LogCountPerMeasurement = 10000; // 每组连续模拟的收包日志数量。
        private const uint Opcode = 100001; // 测试使用的普通协议号。
        private const long RpcId = 9000001; // 测试使用的 RPC 标识。
        private const int PayloadLength = 512; // 测试使用的业务包正文长度。
        private const string SessionSide = "客户端"; // 测试使用的会话方向文本。
        private bool originalEnableLog; // 保存测试开始前的全局日志开关状态。
        private int guardedLogBuildCount; // 确认关闭日志时优化路径没有创建日志文本。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 在每个测试开始前关闭日志输出，并保存原始开关状态。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            originalEnableLog = LogSwitch.EnableLog;
            LogSwitch.EnableLog = false;
            guardedLogBuildCount = 0;
        }

        /// <summary>
        /// 在每个测试结束后恢复调用方原有的全局日志开关状态。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            LogSwitch.EnableLog = originalEnableLog;
        }

        /// <summary>
        /// 测量当前收包实现即使日志已关闭仍会创建时间和插值字符串的成本。
        /// </summary>
        [Test, Performance]
        public void IncomingLog_BuildsStrings_WhenLoggingDisabled()
        {
            Measure.Method(BuildIncomingLogWithoutOuterGuard)
                .SampleGroup("Network.IncomingLog.Disabled.Legacy")
                .WarmupCount(WarmupMeasurementCount)
                .MeasurementCount(ResultMeasurementCount)
                .IterationsPerMeasurement(LogCountPerMeasurement)
                .GC()
                .Run();
        }

        /// <summary>
        /// 测量在日志调用前先判断开关、跳过字符串创建后的理论热路径成本。
        /// </summary>
        [Test, Performance]
        public void IncomingLog_SkipsStrings_WhenLoggingDisabled()
        {
            Measure.Method(SkipIncomingLogWhenDisabled)
                .SampleGroup("Network.IncomingLog.Disabled.Guarded")
                .WarmupCount(WarmupMeasurementCount)
                .MeasurementCount(ResultMeasurementCount)
                .IterationsPerMeasurement(LogCountPerMeasurement)
                .GC()
                .Run();

            Assert.Zero(guardedLogBuildCount, "日志已关闭时不应创建收包日志字符串。");
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 按当前收包实现构造日志文本后再调用日志方法。
        /// 即使日志方法内部提前返回，时间字符串和插值字符串已经产生。
        /// </summary>
        private void BuildIncomingLogWithoutOuterGuard()
        {
            string receiveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            LogSwitch.Info($"[{receiveTime}] [{SessionSide}] 收到消息 opcode:{Opcode} rpcId:{RpcId} len:{PayloadLength}");
        }

        /// <summary>
        /// 在创建任何日志字符串前检查日志开关，模拟优化后的关闭日志热路径。
        /// </summary>
        private void SkipIncomingLogWhenDisabled()
        {
            if (!LogSwitch.EnableLog)
            {
                return;
            }

            guardedLogBuildCount++;
            string receiveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            LogSwitch.Info($"[{receiveTime}] [{SessionSide}] 收到消息 opcode:{Opcode} rpcId:{RpcId} len:{PayloadLength}");
        }

        #endregion
    }
}
