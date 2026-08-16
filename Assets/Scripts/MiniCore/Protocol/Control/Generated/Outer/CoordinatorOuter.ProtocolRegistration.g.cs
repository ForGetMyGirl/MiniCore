// Auto-generated from Proto/Control/Outer/CoordinatorOuter.proto. Do not edit by hand.
using MiniCore.Model;
using MiniCore.Serialization;

namespace MiniCore.Protocol.Generated
{
    /// <summary>
    /// 注册 CoordinatorOuter Proto 中的全部网络消息。
    /// </summary>
    public static class CoordinatorOuterProtocolRegistration
    {
        /// <summary>
        /// 将消息、Opcode、角色和 Parser 写入协议构建器。
        /// </summary>
        /// <param name="builder">目标协议构建器。</param>
        public static void Register(NetworkProtocolBuilder builder)
        {
            builder.RegisterMessage<ResolveServiceRequest>(200039u, NetworkMessageRole.RpcRequest, new ProtobufMessageParser<ResolveServiceRequest>(ResolveServiceRequest.Parser));
            builder.RegisterMessage<ResolveServiceResponse>(200040u, NetworkMessageRole.RpcResponse, new ProtobufMessageParser<ResolveServiceResponse>(ResolveServiceResponse.Parser));
        }
    }
}
