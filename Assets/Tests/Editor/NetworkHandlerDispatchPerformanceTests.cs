using Cysharp.Threading.Tasks;
using MiniCore.Model;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace MiniCore.EditorTests
{
    /// <summary>
    /// 网络普通消息 Handler 无反射派发的性能基线测试。
    /// 测试范围仅包含已反序列化消息到具体 Handler 的调用，不包含 Socket、队列和序列化开销。
    /// </summary>
    public sealed class NetworkHandlerDispatchPerformanceTests
    {
        #region Private 私有成员

        private const int WarmupMeasurementCount = 5; // 不计入结果的预热组数。
        private const int ResultMeasurementCount = 20; // 计入报告的测量组数。
        private const int DispatchCountPerMeasurement = 1000000; // 每组连续派发的消息数量，保证单组计时有足够精度。
        private readonly BenchmarkMessageHandler handler = new BenchmarkMessageHandler(); // 用于承接派发的基准 Handler。
        private readonly BenchmarkMessage message = new BenchmarkMessage { Value = 1 }; // 每次复用的已反序列化消息。
        private INetworkMessageHandlerInvoker handlerInvoker; // 缓存 Handler 的无反射派发契约。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 在每个测试执行前缓存无反射派发入口，确保测试体只测量消息派发。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            handlerInvoker = handler;
        }

        /// <summary>
        /// 测量普通消息从非泛型派发契约到具体 Handler 的直接调用耗时与 GC 事件数。
        /// </summary>
        [Test, Performance]
        public void NormalHandlerInvoker_DispatchesMessage_WithoutReflection()
        {
            Measure.Method(DispatchNormalMessage)
                .SampleGroup("Network.NormalHandlerDispatch")
                .WarmupCount(WarmupMeasurementCount)
                .MeasurementCount(ResultMeasurementCount)
                .IterationsPerMeasurement(DispatchCountPerMeasurement)
                .GC()
                .Run();

            Assert.Greater(handler.HandledCount, 0, "性能测试未实际调用 Handler，结果无效。");
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 执行一次已反序列化普通消息的无反射派发。
        /// </summary>
        private void DispatchNormalMessage()
        {
            handlerInvoker.HandleAsync(null, message).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 用于性能基线的普通协议对象。
        /// </summary>
        private sealed class BenchmarkMessage : IProtocol
        {
            /// <summary>
            /// 基准测试使用的普通消息协议号。
            /// </summary>
            public uint Opcode => 100001;

            /// <summary>
            /// 用于确保 Handler 实际执行的测试值。
            /// </summary>
            public int Value { get; set; }
        }

        /// <summary>
        /// 不包含日志、序列化或业务 I/O 的普通消息 Handler，用于隔离派发成本。
        /// </summary>
        private sealed class BenchmarkMessageHandler : AMHandler<BenchmarkMessage>
        {
            private int handledCount; // 用于确认测试调用实际发生的累计次数。

            /// <summary>
            /// 已处理消息的累计次数。
            /// </summary>
            public int HandledCount => handledCount;

            /// <summary>
            /// 执行最小业务操作，以避免测试只测得空调用路径。
            /// </summary>
            /// <param name="session">本次消息关联的网络会话，基准测试中为空。</param>
            /// <param name="message">已完成反序列化的普通消息。</param>
            /// <returns>同步完成的 UniTask。</returns>
            public override UniTask HandleAsync(NetworkSession session, BenchmarkMessage message)
            {
                handledCount += message.Value;
                return UniTask.CompletedTask;
            }
        }

        #endregion
    }
}
