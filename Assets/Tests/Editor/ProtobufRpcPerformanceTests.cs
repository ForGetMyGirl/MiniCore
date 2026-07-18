using System;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Serialization;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace MiniCore.EditorTests
{
    /// <summary>
    /// Protobuf RPC 请求解析、运行时 RpcId 写入和响应封包性能基线。
    /// </summary>
    public sealed class ProtobufRpcPerformanceTests
    {
        #region Private 私有成员

        private const int WarmupMeasurementCount = 5; // 预热测量次数。
        private const int ResultMeasurementCount = 20; // 正式测量次数。
        private const int IterationsPerMeasurement = 10000; // 每次测量的 RPC 数量。
        private const uint RequestOpcode = 200001; // DemoRpcRequest 的稳定协议号。
        private const uint ResponseOpcode = 200002; // DemoRpcResponse 的稳定协议号。
        private const long RpcId = 9000001; // 测试使用的包头 RPC 标识。
        private readonly ProtobufSerializer serializer = new ProtobufSerializer(); // 当前 Protobuf 编解码器。
        private byte[] requestPacket; // 固定复用的请求网络包。
        private byte[] lastResponsePacket; // 最近一次封装出的响应网络包。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 构造固定 RPC 请求包，不计入性能测量。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            byte[] payload = serializer.Serialize(new DemoRpcRequest { Payload = "MiniCore protobuf rpc performance baseline" });
            requestPacket = BuildPacket(RequestOpcode, RpcId, payload);
            lastResponsePacket = null;
        }

        /// <summary>
        /// 测量完整 RPC 收包到响应封包的框架路径。
        /// </summary>
        [Test, Performance]
        public void ProtobufRpc_ParsesRequestAndBuildsResponsePacket()
        {
            Measure.Method(ProcessRpc)
                .SampleGroup("Network.Protobuf.Rpc")
                .WarmupCount(WarmupMeasurementCount)
                .MeasurementCount(ResultMeasurementCount)
                .IterationsPerMeasurement(IterationsPerMeasurement)
                .GC()
                .Run();

            Assert.IsNotNull(lastResponsePacket);
            Assert.Greater(lastResponsePacket.Length, 12);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 执行一次与 RPC 请求处理和响应发送等价的编解码流程。
        /// </summary>
        private void ProcessRpc()
        {
            ReadOnlySpan<byte> data = requestPacket;
            uint opcode = NetBinaryCodec.ReadUInt32BE(data, 0);
            long rpcId = NetBinaryCodec.ReadInt64BE(data, 4);
            if (opcode != RequestOpcode || rpcId == 0)
            {
                throw new InvalidOperationException("Protobuf RPC 基准包头无效。");
            }

            var payload = new ReadOnlyMemory<byte>(requestPacket, 12, requestPacket.Length - 12);
            var request = (DemoRpcRequest)serializer.Deserialize(typeof(DemoRpcRequest), payload);
            request.RpcId = rpcId;
            var response = new DemoRpcResponse
            {
                RpcId = request.RpcId,
                Code = 0,
                Msg = "OK",
                Echo = request.Payload
            };
            lastResponsePacket = BuildPacket(ResponseOpcode, rpcId, serializer.Serialize(response));
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

        #endregion
    }
}
