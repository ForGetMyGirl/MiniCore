using System;
using System.Collections.Generic;
using MiniCore.Threading;
using MiniCore.HotUpdate;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace MiniCore.EditorTests
{
    /// <summary>
    /// 完整普通业务包进入主线程后的解析、反序列化和 Handler 派发性能基线测试。
    /// 测试不包含 Socket 收发、跨线程队列和日志，仅覆盖 NetworkService.HandleIncoming 的普通消息成功分支。
    /// </summary>
    public sealed class NetworkInboundPacketPerformanceTests
    {
        #region Private 私有成员

        private const int WarmupMeasurementCount = 5; // 不计入最终报告的预热组数。
        private const int ResultMeasurementCount = 20; // 计入最终报告的测量组数。
        private const int PacketCountPerMeasurement = 10000; // 每组连续处理的完整业务包数量。
        private const uint TestNetworkDataOpcode = 100003; // OpcodeRegistry 中 TestNetworkData 的稳定协议号。
        private const string MediumContent = "MiniCore inbound packet benchmark payload. This fixed message represents a medium-sized normal protocol packet and remains unchanged between measurements."; // 固定的中等业务正文。
        private readonly Dictionary<uint, InboundHandlerRegistration> handlers = new Dictionary<uint, InboundHandlerRegistration>(); // 模拟 NetworkService 的 opcode 到 Handler 映射。
        private readonly BenchmarkInboundHandler handler = new BenchmarkInboundHandler(); // 承接反序列化消息的无日志基准 Handler。
        private INetworkSerializer serializer; // 当前运行时实际使用的 JSON 序列化器。
        private byte[] inboundPacket; // 在测量前生成并复用的完整业务包。
        private TestNetworkData lastHandledMessage; // 保留最近一次处理的消息以验证完整链路实际执行。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 在每个测试开始前生成固定 packet，并建立与运行时一致的 opcode Handler 映射。
        /// 准备过程不计入性能时间。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            serializer = new NewtonsoftJsonSerializer();
            handlers.Clear();
            handlers.Add(TestNetworkDataOpcode, new InboundHandlerRegistration(typeof(TestNetworkData), handler));

            TestNetworkData message = new TestNetworkData
            {
                Id = 1001,
                Content = MediumContent
            };
            byte[] payload = serializer.Serialize(message);
            inboundPacket = BuildPacket(TestNetworkDataOpcode, 0, payload);
            lastHandledMessage = null;
        }

        /// <summary>
        /// 测量完整普通业务包的包头读取、opcode 查表、运行时类型反序列化与无反射 Handler 派发耗时和 GC 事件数。
        /// </summary>
        [Test, Performance]
        public void InboundPacket_ParsesDeserializesAndDispatchesNormalMessage()
        {
            Measure.Method(ProcessInboundPacket)
                .SampleGroup("Network.InboundPacket.NormalMessage")
                .WarmupCount(WarmupMeasurementCount)
                .MeasurementCount(ResultMeasurementCount)
                .IterationsPerMeasurement(PacketCountPerMeasurement)
                .GC()
                .Run();

            Assert.IsNotNull(lastHandledMessage, "性能测试未完成反序列化与 Handler 派发，结果无效。");
            Assert.AreEqual(1001, lastHandledMessage.Id, "完整入站包处理后的消息标识不正确。");
            Assert.AreEqual(MediumContent, lastHandledMessage.Content, "完整入站包处理后的消息正文不正确。");
            Assert.Greater(handler.HandledCount, 0, "性能测试未实际调用 Handler，结果无效。");
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 模拟普通消息在 NetworkService.HandleIncoming 中的成功处理路径。
        /// </summary>
        private void ProcessInboundPacket()
        {
            ReadOnlySpan<byte> packetSpan = inboundPacket;
            uint opcode = NetBinaryCodec.ReadUInt32BE(packetSpan, 0);
            long rpcId = NetBinaryCodec.ReadInt64BE(packetSpan, 4);
            if (rpcId != 0 || !handlers.TryGetValue(opcode, out InboundHandlerRegistration registration))
            {
                throw new InvalidOperationException("基准 packet 未命中普通消息 Handler，测试配置无效。");
            }

            ReadOnlyMemory<byte> payload = new ReadOnlyMemory<byte>(inboundPacket, 12, inboundPacket.Length - 12);
            if (!(serializer.Deserialize(registration.MessageType, payload) is TestNetworkData message))
            {
                throw new InvalidOperationException("基准 packet 反序列化后的消息类型不正确。");
            }

            registration.Invoker.HandleAsync(null, message).GetAwaiter().GetResult();
            lastHandledMessage = message;
        }

        /// <summary>
        /// 按当前网络业务包格式创建固定的普通消息 packet。
        /// </summary>
        /// <param name="opcode">写入 packet 头的普通消息协议号。</param>
        /// <param name="rpcId">写入 packet 头的 RPC 标识；普通消息固定为零。</param>
        /// <param name="payload">写入 packet 头之后的 JSON 正文。</param>
        /// <returns>包含 12 字节业务包头和正文的完整 packet。</returns>
        private static byte[] BuildPacket(uint opcode, long rpcId, byte[] payload)
        {
            byte[] packet = new byte[12 + payload.Length];
            NetBinaryCodec.WriteUInt32BE(packet, 0, opcode);
            NetBinaryCodec.WriteInt64BE(packet, 4, rpcId);
            Buffer.BlockCopy(payload, 0, packet, 12, payload.Length);
            return packet;
        }

        /// <summary>
        /// 模拟普通消息 opcode 映射项，缓存消息运行时类型与无反射派发入口。
        /// </summary>
        private sealed class InboundHandlerRegistration
        {
            #region Public 公共成员

            /// <summary>
            /// 创建普通消息的运行时类型和无反射派发器映射项。
            /// </summary>
            /// <param name="messageType">与 opcode 对应的协议运行时类型。</param>
            /// <param name="invoker">负责调用具体 Handler 的无反射派发入口。</param>
            public InboundHandlerRegistration(Type messageType, INetworkMessageHandlerInvoker invoker)
            {
                MessageType = messageType;
                Invoker = invoker;
            }

            /// <summary>
            /// 与 opcode 对应的协议运行时类型。
            /// </summary>
            public Type MessageType { get; }

            /// <summary>
            /// 已缓存的无反射 Handler 派发入口。
            /// </summary>
            public INetworkMessageHandlerInvoker Invoker { get; }

            #endregion
        }

        /// <summary>
        /// 不包含日志和业务 I/O 的普通消息 Handler，用于隔离入站框架处理成本。
        /// </summary>
        private sealed class BenchmarkInboundHandler : AMHandler<TestNetworkData>
        {
            #region Private 私有成员

            private int handledCount; // 累计处理次数，用于验证 Handler 实际执行。

            #endregion

            #region Public 公共成员

            /// <summary>
            /// 已完成的普通消息处理次数。
            /// </summary>
            public int HandledCount => handledCount;

            /// <summary>
            /// 执行最小业务操作，确保基准覆盖实际 Handler 调用而不是空分支。
            /// </summary>
            /// <param name="session">本次消息关联的网络会话，基准测试中为空。</param>
            /// <param name="message">已完成反序列化的普通消息。</param>
            /// <returns>同步完成的 MTask。</returns>
            public override MTask HandleAsync(NetworkSession session, TestNetworkData message)
            {
                handledCount += message.Id;
                return MTask.CompletedTask;
            }

            #endregion
        }

        #endregion
    }
}
