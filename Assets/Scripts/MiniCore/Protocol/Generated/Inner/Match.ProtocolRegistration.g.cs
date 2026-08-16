// Auto-generated from Proto/Business/Inner/Match.proto. Do not edit by hand.
using MiniCore.Model;
using MiniCore.Serialization;

namespace MiniCore.Protocol.Generated
{
    /// <summary>
    /// 注册 Match Proto 中的全部网络消息。
    /// </summary>
    public static class MatchProtocolRegistration
    {
        /// <summary>
        /// 将消息、Opcode、角色和 Parser 写入协议构建器。
        /// </summary>
        /// <param name="builder">目标协议构建器。</param>
        public static void Register(NetworkProtocolBuilder builder)
        {
            builder.RegisterMessage<EnqueueMatchRequest>(200041u, NetworkMessageRole.RpcRequest, new ProtobufMessageParser<EnqueueMatchRequest>(EnqueueMatchRequest.Parser));
            builder.RegisterMessage<EnqueueMatchResponse>(200042u, NetworkMessageRole.RpcResponse, new ProtobufMessageParser<EnqueueMatchResponse>(EnqueueMatchResponse.Parser));
            builder.RegisterMessage<CancelMatchRequest>(200043u, NetworkMessageRole.RpcRequest, new ProtobufMessageParser<CancelMatchRequest>(CancelMatchRequest.Parser));
            builder.RegisterMessage<CancelMatchResponse>(200044u, NetworkMessageRole.RpcResponse, new ProtobufMessageParser<CancelMatchResponse>(CancelMatchResponse.Parser));
            builder.RegisterMessage<TakeMatchRequest>(200045u, NetworkMessageRole.RpcRequest, new ProtobufMessageParser<TakeMatchRequest>(TakeMatchRequest.Parser));
            builder.RegisterMessage<TakeMatchResponse>(200046u, NetworkMessageRole.RpcResponse, new ProtobufMessageParser<TakeMatchResponse>(TakeMatchResponse.Parser));
        }
    }
}
