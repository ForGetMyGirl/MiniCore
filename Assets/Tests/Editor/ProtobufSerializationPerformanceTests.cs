using MiniCore.Protocol.Generated;
using MiniCore.Serialization;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace MiniCore.EditorTests
{
    /// <summary>
    /// Protobuf 协议序列化性能基线，使用与 Newtonsoft JSON 基线相同的协议输入进行比较。
    /// </summary>
    public sealed class ProtobufSerializationPerformanceTests
    {
        #region Private 私有成员

        private const int WarmupMeasurementCount = 5; // 预热测量次数。
        private const int ResultMeasurementCount = 20; // 正式测量次数。
        private const int IterationsPerMeasurement = 10000; // 每组连续编解码次数。
        private readonly ProtobufSerializer serializer = new ProtobufSerializer(); // 当前 Protobuf 序列化器。
        private TestNetworkData message; // 与 JSON 基准一致的固定协议消息。
        private byte[] payload; // 反序列化测试使用的预编码消息。
        private byte[] lastSerializedPayload; // 保留最近一次序列化结果以验证调用实际发生。
        private TestNetworkData lastDeserializedMessage; // 保留最近一次反序列化结果以验证调用实际发生。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 预编码一份测试消息，隔离反序列化性能。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            message = NetworkSerializationBenchmarkPayload.CreateMediumMessage();
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

            Assert.IsNotNull(lastSerializedPayload, "性能测试未实际执行 Protobuf 序列化，结果无效。");
            Assert.Greater(lastSerializedPayload.Length, 0, "Protobuf 序列化结果为空，测试输入无效。");
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

            Assert.IsNotNull(lastDeserializedMessage, "性能测试未实际执行 Protobuf 反序列化，结果无效。");
            Assert.AreEqual(message.Id, lastDeserializedMessage.Id, "Protobuf 反序列化后的消息标识不正确。");
            Assert.AreEqual(message.Content, lastDeserializedMessage.Content, "Protobuf 反序列化后的消息正文不正确。");
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 执行一次序列化基准操作。
        /// </summary>
        private void Serialize()
        {
            lastSerializedPayload = serializer.Serialize(message);
        }

        /// <summary>
        /// 执行一次反序列化基准操作。
        /// </summary>
        private void Deserialize()
        {
            lastDeserializedMessage = serializer.Deserialize<TestNetworkData>(payload);
        }

        #endregion
    }
}
