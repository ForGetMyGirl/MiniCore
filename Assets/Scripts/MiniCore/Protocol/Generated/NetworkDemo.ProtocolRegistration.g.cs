// Auto-generated from Proto/NetworkDemo.proto. Do not edit by hand.
using MiniCore.Model;
using MiniCore.Serialization;

namespace MiniCore.Protocol.Generated
{
    /// <summary>
    /// 注册 NetworkDemo Proto 中的全部网络消息。
    /// </summary>
    public static class NetworkDemoProtocolRegistration
    {
        /// <summary>
        /// 将消息、Opcode、角色和 Parser 写入协议构建器。
        /// </summary>
        /// <param name="builder">目标协议构建器。</param>
        public static void Register(NetworkProtocolBuilder builder)
        {
            builder.RegisterMessage<DemoNormalMessage>(100001u, NetworkMessageRole.Normal, new ProtobufMessageParser<DemoNormalMessage>(DemoNormalMessage.Parser));
            builder.RegisterMessage<DemoRpcRequest>(200001u, NetworkMessageRole.RpcRequest, new ProtobufMessageParser<DemoRpcRequest>(DemoRpcRequest.Parser));
            builder.RegisterMessage<DemoRpcResponse>(200002u, NetworkMessageRole.RpcResponse, new ProtobufMessageParser<DemoRpcResponse>(DemoRpcResponse.Parser));
            builder.RegisterMessage<DisconnectNotice>(100002u, NetworkMessageRole.Normal, new ProtobufMessageParser<DisconnectNotice>(DisconnectNotice.Parser));
            builder.RegisterMessage<TestNetworkData>(100003u, NetworkMessageRole.Normal, new ProtobufMessageParser<TestNetworkData>(TestNetworkData.Parser));
        }
    }
}
