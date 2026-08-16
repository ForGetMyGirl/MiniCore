namespace MiniCore.Server.Rpc;

/// <summary>
/// 表示一个已经从 MiniCore TCP 帧中解析出的业务包。
/// </summary>
public sealed class MiniCoreRpcFrame
{
    #region Public 公共成员

    /// <summary>
    /// 获取业务消息 Opcode。
    /// </summary>
    public uint Opcode { get; }

    /// <summary>
    /// 获取 RPC 关联标识；普通消息为零。
    /// </summary>
    public long RpcId { get; }

    /// <summary>
    /// 获取 Protobuf 正文。
    /// </summary>
    public byte[] Payload { get; }

    /// <summary>
    /// 创建已解析业务帧。
    /// </summary>
    /// <param name="opcode">业务消息 Opcode。</param>
    /// <param name="rpcId">RPC 关联标识。</param>
    /// <param name="payload">Protobuf 正文。</param>
    public MiniCoreRpcFrame(uint opcode, long rpcId, byte[] payload)
    {
        Opcode = opcode;
        RpcId = rpcId;
        Payload = payload;
    }

    #endregion
}
