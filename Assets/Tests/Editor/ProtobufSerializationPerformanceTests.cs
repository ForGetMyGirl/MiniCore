using MiniCore.Protocol.Generated;
using MiniCore.Serialization;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace MiniCore.EditorTests
{
    /// <summary>
    /// Protobuf 协议序列化性能基线，用于与保留的 Newtonsoft JSON 基线并列比较。
    /// </summary>
    public sealed class ProtobufSerializationPerformanceTests
    {
        #region Private 私有成员

        private const int WarmupMeasurementCount = 5; // 预热测量次数。
        private const int ResultMeasurementCount = 20; // 正式测量次数。
        private const int IterationsPerMeasurement = 10000; // 每组连续编解码次数。
        private readonly ProtobufSerializer serializer = new ProtobufSerializer(); // 当前 Protobuf 序列化器。
        private readonly DemoNormalMessage message = new DemoNormalMessage { Content = "MiniCore protobuf performance baseline" }; // 复用的测试消息。
        private byte[] payload; // 反序列化测试使用的预编码消息。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 预编码一份测试消息，隔离反序列化性能。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            payload = serializer.Serialize(message);
        }

        /// <summary>
        /// 测量 Protobuf 消息序列化耗时与 GC。
        /// </summary>
        [Test, Performance]
        public void ProtobufSerializer_SerializesMessage()
        {
            Measure.Method(Serialize)
                .SampleGroup("Network.Protobuf.Serialize")
                .WarmupCount(WarmupMeasurementCount)
                .MeasurementCount(ResultMeasurementCount)
                .IterationsPerMeasurement(IterationsPerMeasurement)
                .GC()
                .Run();
        }

        /// <summary>
        /// 测量 Protobuf 消息反序列化耗时与 GC。
        /// </summary>
        [Test, Performance]
        public void ProtobufSerializer_DeserializesMessage()
        {
            Measure.Method(Deserialize)
                .SampleGroup("Network.Protobuf.Deserialize")
                .WarmupCount(WarmupMeasurementCount)
                .MeasurementCount(ResultMeasurementCount)
                .IterationsPerMeasurement(IterationsPerMeasurement)
                .GC()
                .Run();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 执行一次序列化基准操作。
        /// </summary>
        private void Serialize()
        {
            serializer.Serialize(message);
        }

        /// <summary>
        /// 执行一次反序列化基准操作。
        /// </summary>
        private void Deserialize()
        {
            serializer.Deserialize<DemoNormalMessage>(payload);
        }

        #endregion
    }
}
