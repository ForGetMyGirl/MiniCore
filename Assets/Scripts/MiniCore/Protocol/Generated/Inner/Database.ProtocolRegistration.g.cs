// Auto-generated from Proto/Business/Inner/Database.proto. Do not edit by hand.
using MiniCore.Model;
using MiniCore.Serialization;

namespace MiniCore.Protocol.Generated
{
    /// <summary>
    /// 注册 Database Proto 中的全部网络消息。
    /// </summary>
    public static class DatabaseProtocolRegistration
    {
        /// <summary>
        /// 将消息、Opcode、角色和 Parser 写入协议构建器。
        /// </summary>
        /// <param name="builder">目标协议构建器。</param>
        public static void Register(NetworkProtocolBuilder builder)
        {
            builder.RegisterMessage<LoadPlayerDataRequest>(200035u, NetworkMessageRole.RpcRequest, new ProtobufMessageParser<LoadPlayerDataRequest>(LoadPlayerDataRequest.Parser));
            builder.RegisterMessage<LoadPlayerDataResponse>(200036u, NetworkMessageRole.RpcResponse, new ProtobufMessageParser<LoadPlayerDataResponse>(LoadPlayerDataResponse.Parser));
            builder.RegisterMessage<SavePlayerDataRequest>(200037u, NetworkMessageRole.RpcRequest, new ProtobufMessageParser<SavePlayerDataRequest>(SavePlayerDataRequest.Parser));
            builder.RegisterMessage<SavePlayerDataResponse>(200038u, NetworkMessageRole.RpcResponse, new ProtobufMessageParser<SavePlayerDataResponse>(SavePlayerDataResponse.Parser));
        }
    }
}
