using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Serialization;
using NUnit.Framework;

namespace MiniCore.EditorTests
{
    /// <summary>
    /// 验证 JSON 与 Protobuf 性能基准使用等价协议输入，并记录可影响网络传输成本的编码长度。
    /// </summary>
    public sealed class NetworkSerializationComparisonTests
    {
        #region Private 私有成员

        private readonly INetworkSerializer jsonSerializer = new NewtonsoftJsonSerializer(); // JSON 基准所用序列化器。
        private readonly INetworkSerializer protobufSerializer = new ProtobufSerializer(); // Protobuf 基准所用序列化器。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 使用与性能测试相同的消息完成双向编解码，并确认 Protobuf 的网络正文更小。
        /// </summary>
        [Test]
        public void NetworkSerializers_RoundTripEquivalentMessage_AndProtobufPayloadIsSmaller()
        {
            TestNetworkData message = NetworkSerializationBenchmarkPayload.CreateMediumMessage();
            byte[] jsonPayload = jsonSerializer.Serialize(message);
            byte[] protobufPayload = protobufSerializer.Serialize(message);
            TestNetworkData jsonMessage = jsonSerializer.Deserialize<TestNetworkData>(jsonPayload);
            TestNetworkData protobufMessage = protobufSerializer.Deserialize<TestNetworkData>(protobufPayload);

            AssertMessageEquals(message, jsonMessage, "JSON");
            AssertMessageEquals(message, protobufMessage, "Protobuf");
            Assert.Less(protobufPayload.Length, jsonPayload.Length, "固定中等载荷下 Protobuf 正文应小于 JSON 正文。");
            TestContext.WriteLine($"Network serialization payload bytes - JSON: {jsonPayload.Length}, Protobuf: {protobufPayload.Length}");
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 断言反序列化后的协议字段与基准输入完全一致。
        /// </summary>
        /// <param name="expected">性能基准使用的原始协议消息。</param>
        /// <param name="actual">序列化器往返后得到的协议消息。</param>
        /// <param name="serializerName">用于输出断言上下文的序列化器名称。</param>
        private static void AssertMessageEquals(TestNetworkData expected, TestNetworkData actual, string serializerName)
        {
            Assert.IsNotNull(actual, $"{serializerName} 反序列化结果为空。");
            Assert.AreEqual(expected.Id, actual.Id, $"{serializerName} 反序列化后的消息标识不正确。");
            Assert.AreEqual(expected.Content, actual.Content, $"{serializerName} 反序列化后的消息正文不正确。");
        }

        #endregion
    }
}
