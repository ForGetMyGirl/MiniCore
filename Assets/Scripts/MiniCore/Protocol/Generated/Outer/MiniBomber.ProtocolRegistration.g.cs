// Auto-generated from Proto/Business/Outer/MiniBomber.proto. Do not edit by hand.
using MiniCore.Model;
using MiniCore.Serialization;

namespace MiniCore.Protocol.Generated
{
    /// <summary>
    /// 注册 MiniBomber Proto 中的全部网络消息。
    /// </summary>
    public static class MiniBomberProtocolRegistration
    {
        /// <summary>
        /// 将消息、Opcode、角色和 Parser 写入协议构建器。
        /// </summary>
        /// <param name="builder">目标协议构建器。</param>
        public static void Register(NetworkProtocolBuilder builder)
        {
            builder.RegisterMessage<MiniBomberResumeSessionRequest>(200007u, NetworkMessageRole.RpcRequest, new ProtobufMessageParser<MiniBomberResumeSessionRequest>(MiniBomberResumeSessionRequest.Parser));
            builder.RegisterMessage<MiniBomberResumeSessionResponse>(200008u, NetworkMessageRole.RpcResponse, new ProtobufMessageParser<MiniBomberResumeSessionResponse>(MiniBomberResumeSessionResponse.Parser));
            builder.RegisterMessage<MiniBomberLobbySnapshotRequest>(200009u, NetworkMessageRole.RpcRequest, new ProtobufMessageParser<MiniBomberLobbySnapshotRequest>(MiniBomberLobbySnapshotRequest.Parser));
            builder.RegisterMessage<MiniBomberLobbySnapshotResponse>(200010u, NetworkMessageRole.RpcResponse, new ProtobufMessageParser<MiniBomberLobbySnapshotResponse>(MiniBomberLobbySnapshotResponse.Parser));
            builder.RegisterMessage<MiniBomberCreateRoomRequest>(200011u, NetworkMessageRole.RpcRequest, new ProtobufMessageParser<MiniBomberCreateRoomRequest>(MiniBomberCreateRoomRequest.Parser));
            builder.RegisterMessage<MiniBomberCreateRoomResponse>(200012u, NetworkMessageRole.RpcResponse, new ProtobufMessageParser<MiniBomberCreateRoomResponse>(MiniBomberCreateRoomResponse.Parser));
            builder.RegisterMessage<MiniBomberJoinRoomRequest>(200013u, NetworkMessageRole.RpcRequest, new ProtobufMessageParser<MiniBomberJoinRoomRequest>(MiniBomberJoinRoomRequest.Parser));
            builder.RegisterMessage<MiniBomberJoinRoomResponse>(200014u, NetworkMessageRole.RpcResponse, new ProtobufMessageParser<MiniBomberJoinRoomResponse>(MiniBomberJoinRoomResponse.Parser));
            builder.RegisterMessage<MiniBomberLeaveRoomRequest>(200015u, NetworkMessageRole.RpcRequest, new ProtobufMessageParser<MiniBomberLeaveRoomRequest>(MiniBomberLeaveRoomRequest.Parser));
            builder.RegisterMessage<MiniBomberLeaveRoomResponse>(200016u, NetworkMessageRole.RpcResponse, new ProtobufMessageParser<MiniBomberLeaveRoomResponse>(MiniBomberLeaveRoomResponse.Parser));
            builder.RegisterMessage<MiniBomberUpdateRoomRequest>(200017u, NetworkMessageRole.RpcRequest, new ProtobufMessageParser<MiniBomberUpdateRoomRequest>(MiniBomberUpdateRoomRequest.Parser));
            builder.RegisterMessage<MiniBomberUpdateRoomResponse>(200018u, NetworkMessageRole.RpcResponse, new ProtobufMessageParser<MiniBomberUpdateRoomResponse>(MiniBomberUpdateRoomResponse.Parser));
            builder.RegisterMessage<MiniBomberSetReadyRequest>(200019u, NetworkMessageRole.RpcRequest, new ProtobufMessageParser<MiniBomberSetReadyRequest>(MiniBomberSetReadyRequest.Parser));
            builder.RegisterMessage<MiniBomberSetReadyResponse>(200020u, NetworkMessageRole.RpcResponse, new ProtobufMessageParser<MiniBomberSetReadyResponse>(MiniBomberSetReadyResponse.Parser));
            builder.RegisterMessage<MiniBomberStartMatchRequest>(200021u, NetworkMessageRole.RpcRequest, new ProtobufMessageParser<MiniBomberStartMatchRequest>(MiniBomberStartMatchRequest.Parser));
            builder.RegisterMessage<MiniBomberStartMatchResponse>(200022u, NetworkMessageRole.RpcResponse, new ProtobufMessageParser<MiniBomberStartMatchResponse>(MiniBomberStartMatchResponse.Parser));
            builder.RegisterMessage<MiniBomberSceneReadyRequest>(200023u, NetworkMessageRole.RpcRequest, new ProtobufMessageParser<MiniBomberSceneReadyRequest>(MiniBomberSceneReadyRequest.Parser));
            builder.RegisterMessage<MiniBomberSceneReadyResponse>(200024u, NetworkMessageRole.RpcResponse, new ProtobufMessageParser<MiniBomberSceneReadyResponse>(MiniBomberSceneReadyResponse.Parser));
            builder.RegisterMessage<MiniBomberLobbyChangedNotice>(100004u, NetworkMessageRole.Normal, new ProtobufMessageParser<MiniBomberLobbyChangedNotice>(MiniBomberLobbyChangedNotice.Parser));
            builder.RegisterMessage<MiniBomberRoomSnapshotNotice>(100005u, NetworkMessageRole.Normal, new ProtobufMessageParser<MiniBomberRoomSnapshotNotice>(MiniBomberRoomSnapshotNotice.Parser));
            builder.RegisterMessage<MiniBomberMatchPrepareNotice>(100006u, NetworkMessageRole.Normal, new ProtobufMessageParser<MiniBomberMatchPrepareNotice>(MiniBomberMatchPrepareNotice.Parser));
            builder.RegisterMessage<MiniBomberMatchCountdownNotice>(100007u, NetworkMessageRole.Normal, new ProtobufMessageParser<MiniBomberMatchCountdownNotice>(MiniBomberMatchCountdownNotice.Parser));
            builder.RegisterMessage<MiniBomberBattleInputBatch>(100012u, NetworkMessageRole.Normal, new ProtobufMessageParser<MiniBomberBattleInputBatch>(MiniBomberBattleInputBatch.Parser));
            builder.RegisterMessage<MiniBomberBattleSnapshot>(100008u, NetworkMessageRole.Normal, new ProtobufMessageParser<MiniBomberBattleSnapshot>(MiniBomberBattleSnapshot.Parser));
            builder.RegisterMessage<MiniBomberBattleDelta>(100013u, NetworkMessageRole.Normal, new ProtobufMessageParser<MiniBomberBattleDelta>(MiniBomberBattleDelta.Parser));
            builder.RegisterMessage<MiniBomberBattleEventBatch>(100009u, NetworkMessageRole.Normal, new ProtobufMessageParser<MiniBomberBattleEventBatch>(MiniBomberBattleEventBatch.Parser));
            builder.RegisterMessage<MiniBomberBattleResyncRequest>(200025u, NetworkMessageRole.RpcRequest, new ProtobufMessageParser<MiniBomberBattleResyncRequest>(MiniBomberBattleResyncRequest.Parser));
            builder.RegisterMessage<MiniBomberBattleResyncResponse>(200026u, NetworkMessageRole.RpcResponse, new ProtobufMessageParser<MiniBomberBattleResyncResponse>(MiniBomberBattleResyncResponse.Parser));
            builder.RegisterMessage<MiniBomberMatchResultNotice>(100010u, NetworkMessageRole.Normal, new ProtobufMessageParser<MiniBomberMatchResultNotice>(MiniBomberMatchResultNotice.Parser));
            builder.RegisterMessage<MiniBomberDisconnectNotice>(100011u, NetworkMessageRole.Normal, new ProtobufMessageParser<MiniBomberDisconnectNotice>(MiniBomberDisconnectNotice.Parser));
        }
    }
}
