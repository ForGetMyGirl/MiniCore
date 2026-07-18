using MiniCore.HotUpdate;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace MiniCore.EditorTests
{
    /// <summary>
    /// 当前网络场景使用的 Newtonsoft JSON 序列化与反序列化性能基线测试。
    /// 测试使用真实业务协议对象，但不包含 Socket、协议包封装、队列与 Handler 派发。
    /// </summary>
    public sealed class NetworkJsonSerializationPerformanceTests
    {
        #region Private 私有成员

        private const int WarmupMeasurementCount = 5; // 不计入结果的预热组数。
        private const int ResultMeasurementCount = 20; // 计入报告的测量组数。
        private const int SerializationCountPerMeasurement = 10000; // 每组连续序列化或反序列化的次数。
        private const string MediumContent = "MiniCore network serialization benchmark payload. This fixed content represents a medium-sized protocol message and remains unchanged between measurements."; // 固定的中等负载业务文本。
        private INetworkSerializer serializer; // 当前网络场景实际使用的 JSON 序列化器。
        private TestNetworkData message; // 固定复用的真实业务协议对象。
        private byte[] serializedPayload; // 固定复用的 JSON 字节输入。
        private byte[] lastSerializedPayload; // 保留最近一次序列化结果以验证调用实际发生。
        private TestNetworkData lastDeserializedMessage; // 保留最近一次反序列化结果以验证调用实际发生。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 在每个测试开始前准备真实协议对象和固定 JSON 字节，准备过程不计入性能时间。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            serializer = new NewtonsoftJsonSerializer();
            message = new TestNetworkData
            {
                Id = 1001,
                Content = MediumContent
            };
            serializedPayload = serializer.Serialize(message);
        }

        /// <summary>
        /// 测量发送侧将真实业务协议对象编码为 UTF-8 JSON 字节的耗时与 GC 事件数。
        /// </summary>
        [Test, Performance]
        public void NewtonsoftJsonSerializer_SerializesMediumProtocolMessage()
        {
            Measure.Method(SerializeMessage)
                .SampleGroup("Network.JsonSerialize.MediumProtocol")
                .WarmupCount(WarmupMeasurementCount)
                .MeasurementCount(ResultMeasurementCount)
                .IterationsPerMeasurement(SerializationCountPerMeasurement)
                .GC()
                .Run();

            Assert.IsNotNull(lastSerializedPayload, "性能测试未实际执行序列化，结果无效。");
            Assert.Greater(lastSerializedPayload.Length, 0, "序列化结果为空，测试输入无效。");
        }

        /// <summary>
        /// 测量收包侧将 UTF-8 JSON 字节还原为真实业务协议对象的耗时与 GC 事件数。
        /// </summary>
        [Test, Performance]
        public void NewtonsoftJsonSerializer_DeserializesMediumProtocolMessage()
        {
            Measure.Method(DeserializeMessage)
                .SampleGroup("Network.JsonDeserialize.MediumProtocol")
                .WarmupCount(WarmupMeasurementCount)
                .MeasurementCount(ResultMeasurementCount)
                .IterationsPerMeasurement(SerializationCountPerMeasurement)
                .GC()
                .Run();

            Assert.IsNotNull(lastDeserializedMessage, "性能测试未实际执行反序列化，结果无效。");
            Assert.AreEqual(message.Id, lastDeserializedMessage.Id, "反序列化后的消息标识不正确。");
            Assert.AreEqual(message.Content, lastDeserializedMessage.Content, "反序列化后的消息正文不正确。");
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 执行一次发送侧 JSON 序列化，并保留结果防止测试调用被优化掉。
        /// </summary>
        private void SerializeMessage()
        {
            lastSerializedPayload = serializer.Serialize(message);
        }

        /// <summary>
        /// 执行一次收包侧 JSON 反序列化，并保留结果用于正确性断言。
        /// </summary>
        private void DeserializeMessage()
        {
            lastDeserializedMessage = serializer.Deserialize<TestNetworkData>(serializedPayload);
        }

        #endregion
    }
}
