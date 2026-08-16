// Auto-generated from Proto/Control/Inner/CoordinatorInner.proto. Do not edit by hand.
using MiniCore.Model;
using MiniCore.Serialization;

namespace MiniCore.Protocol.Generated
{
    /// <summary>
    /// 注册 CoordinatorInner Proto 中的全部网络消息。
    /// </summary>
    public static class CoordinatorInnerProtocolRegistration
    {
        /// <summary>
        /// 将消息、Opcode、角色和 Parser 写入协议构建器。
        /// </summary>
        /// <param name="builder">目标协议构建器。</param>
        public static void Register(NetworkProtocolBuilder builder)
        {
            builder.RegisterMessage<RegisterServerRequest>(200027u, NetworkMessageRole.RpcRequest, new ProtobufMessageParser<RegisterServerRequest>(RegisterServerRequest.Parser));
            builder.RegisterMessage<RegisterServerResponse>(200028u, NetworkMessageRole.RpcResponse, new ProtobufMessageParser<RegisterServerResponse>(RegisterServerResponse.Parser));
            builder.RegisterMessage<ServerHeartbeatRequest>(200029u, NetworkMessageRole.RpcRequest, new ProtobufMessageParser<ServerHeartbeatRequest>(ServerHeartbeatRequest.Parser));
            builder.RegisterMessage<ServerHeartbeatResponse>(200030u, NetworkMessageRole.RpcResponse, new ProtobufMessageParser<ServerHeartbeatResponse>(ServerHeartbeatResponse.Parser));
            builder.RegisterMessage<SetServerStateRequest>(200031u, NetworkMessageRole.RpcRequest, new ProtobufMessageParser<SetServerStateRequest>(SetServerStateRequest.Parser));
            builder.RegisterMessage<SetServerStateResponse>(200032u, NetworkMessageRole.RpcResponse, new ProtobufMessageParser<SetServerStateResponse>(SetServerStateResponse.Parser));
            builder.RegisterMessage<ResolveInnerServiceRequest>(200033u, NetworkMessageRole.RpcRequest, new ProtobufMessageParser<ResolveInnerServiceRequest>(ResolveInnerServiceRequest.Parser));
            builder.RegisterMessage<ResolveInnerServiceResponse>(200034u, NetworkMessageRole.RpcResponse, new ProtobufMessageParser<ResolveInnerServiceResponse>(ResolveInnerServiceResponse.Parser));
        }
    }
}
