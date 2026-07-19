using System;
using Cysharp.Threading.Tasks;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Serialization;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace MiniCore.EditorTests
{
    /// <summary>
    /// Protobuf 完整普通入站包的包头解析、反序列化和 Handler 派发性能基线。
    /// </summary>
    public sealed class ProtobufInboundPacketPerformanceTests
    {
        #region Private 私有成员

        private const int WarmupMeasurementCount = 5; // 预热测量次数。
        private const int ResultMeasurementCount = 20; // 正式测量次数。
        private const int IterationsPerMeasurement = 10000; // 每次测量的包数量。
        private const uint Opcode = 100003; // TestNetworkData 的稳定协议号。
        private readonly ProtobufSerializer serializer = new ProtobufSerializer(); // 当前 Protobuf 编解码器。
        private readonly Handler handler = new Handler(); // 用于验证派发的 Handler。
        private byte[] packet; // 固定复用的完整网络包。
        private TestNetworkData lastMessage; // 最近一次已处理消息。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 构造固定 Protobuf 入站包，不计入性能测量。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            byte[] payload = serializer.Serialize(new TestNetworkData
            {
                Id = 1001,
                Content = "MiniCore protobuf inbound packet performance baseline"
            });
            packet = BuildPacket(Opcode, 0, payload);
            lastMessage = null;
        }

        /// <summary>
        /// 测量完整 Protobuf 普通入站包链路。
        /// </summary>
        [Test, Performance]
        public void ProtobufInboundPacket_ParsesDeserializesAndDispatches()
        {
            Measure.Method(ProcessPacket)
                .SampleGroup("Network.Protobuf.InboundPacket")
                .WarmupCount(WarmupMeasurementCount)
                .MeasurementCount(ResultMeasurementCount)
                .IterationsPerMeasurement(IterationsPerMeasurement)
                .GC()
                .Run();

            Assert.IsNotNull(lastMessage);
            Assert.Greater(handler.HandledCount, 0);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 执行一次与 NetworkService 普通收包分支等价的处理流程。
        /// </summary>
        private void ProcessPacket()
        {
            ReadOnlySpan<byte> data = packet;
            uint opcode = NetBinaryCodec.ReadUInt32BE(data, 0);
            long rpcId = NetBinaryCodec.ReadInt64BE(data, 4);
            if (opcode != Opcode || rpcId != 0)
            {
                throw new InvalidOperationException("Protobuf 入站基准包头无效。");
            }

            var payload = new ReadOnlyMemory<byte>(packet, 12, packet.Length - 12);
            lastMessage = (TestNetworkData)serializer.Deserialize(typeof(TestNetworkData), payload);
            ((INetworkMessageHandlerInvoker)handler).HandleAsync(null, lastMessage).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 组装 MiniCore 固定 12 字节包头和 Protobuf 负载。
        /// </summary>
        /// <param name="opcode">协议号。</param>
        /// <param name="rpcId">RPC 标识。</param>
        /// <param name="payload">Protobuf 消息体。</param>
        /// <returns>完整网络包。</returns>
        private static byte[] BuildPacket(uint opcode, long rpcId, byte[] payload)
        {
            byte[] result = new byte[12 + payload.Length];
            NetBinaryCodec.WriteUInt32BE(result, 0, opcode);
            NetBinaryCodec.WriteInt64BE(result, 4, rpcId);
            Buffer.BlockCopy(payload, 0, result, 12, payload.Length);
            return result;
        }

        /// <summary>
        /// 用于验证无反射分发的最小普通消息 Handler。
        /// </summary>
        private sealed class Handler : AMHandler<TestNetworkData>
        {
            #region Public 公共成员

            /// <summary>
            /// 获取已处理消息数量。
            /// </summary>
            public int HandledCount { get; private set; }

            /// <summary>
            /// 记录一次普通消息处理。
            /// </summary>
            /// <param name="session">关联会话。</param>
            /// <param name="message">已解析消息。</param>
            /// <returns>同步完成任务。</returns>
            public override UniTask HandleAsync(NetworkSession session, TestNetworkData message)
            {
                HandledCount += message.Id;
                return UniTask.CompletedTask;
            }

            #endregion
        }

        #endregion
    }
}
