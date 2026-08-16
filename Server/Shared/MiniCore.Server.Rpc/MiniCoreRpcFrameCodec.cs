using System.Buffers;
using System.Buffers.Binary;

namespace MiniCore.Server.Rpc;

/// <summary>
/// 读写 MiniCore TCP 的四字节帧长和十二字节业务头。
/// </summary>
public static class MiniCoreRpcFrameCodec
{
    #region Private 私有成员

    private const int BusinessHeaderLength = 12;
    private const int MaximumFrameLength = 4 * 1024 * 1024;

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 从 TCP 流读取一个完整 MiniCore 业务帧。
    /// </summary>
    /// <param name="stream">连续 TCP 字节流。</param>
    /// <param name="cancellationToken">读取取消令牌。</param>
    /// <returns>完整帧；在帧开始前到达 EOF 时返回 null。</returns>
    public static async ValueTask<MiniCoreRpcFrame?> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        byte[] headerBuffer = ArrayPool<byte>.Shared.Rent(4 + BusinessHeaderLength);
        try
        {
            if (!await ReadExactOrEofAsync(stream, headerBuffer.AsMemory(0, 4), cancellationToken))
            {
                return null;
            }

            int length = BinaryPrimitives.ReadInt32BigEndian(headerBuffer.AsSpan(0, 4));
            if (length < BusinessHeaderLength || length > MaximumFrameLength)
            {
                throw new InvalidDataException($"MiniCore TCP 帧长度无效：{length}。");
            }

            if (!await ReadExactOrEofAsync(stream, headerBuffer.AsMemory(4, BusinessHeaderLength), cancellationToken))
            {
                throw new EndOfStreamException("MiniCore TCP 帧在业务头完成前断开。");
            }

            uint opcode = BinaryPrimitives.ReadUInt32BigEndian(headerBuffer.AsSpan(4, 4));
            long rpcId = BinaryPrimitives.ReadInt64BigEndian(headerBuffer.AsSpan(8, 8));
            int payloadLength = length - BusinessHeaderLength;
            byte[] payload = payloadLength == 0 ? Array.Empty<byte>() : new byte[payloadLength];
            if (payloadLength > 0 && !await ReadExactOrEofAsync(stream, payload, cancellationToken))
            {
                throw new EndOfStreamException("MiniCore TCP 帧在 Protobuf 正文完成前断开。");
            }

            return new MiniCoreRpcFrame(opcode, rpcId, payload);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(headerBuffer);
        }
    }

    /// <summary>
    /// 向 TCP 流写入一个完整 MiniCore 业务帧。
    /// </summary>
    /// <param name="stream">连续 TCP 字节流。</param>
    /// <param name="opcode">业务消息 Opcode。</param>
    /// <param name="rpcId">RPC 关联标识。</param>
    /// <param name="payload">Protobuf 正文。</param>
    /// <param name="cancellationToken">写入取消令牌。</param>
    /// <returns>帧完整写入任务。</returns>
    public static async ValueTask WriteAsync(Stream stream, uint opcode, long rpcId, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        int frameLength = BusinessHeaderLength + payload.Length;
        int totalLength = 4 + frameLength;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(totalLength);
        try
        {
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(0, 4), frameLength);
            BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(4, 4), opcode);
            BinaryPrimitives.WriteInt64BigEndian(buffer.AsSpan(8, 8), rpcId);
            payload.Span.CopyTo(buffer.AsSpan(16, payload.Length));
            await stream.WriteAsync(buffer.AsMemory(0, totalLength), cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 填满缓冲区；在读取任何字节前到达 EOF 时返回 false。
    /// </summary>
    /// <param name="stream">连续 TCP 字节流。</param>
    /// <param name="destination">必须填满的目标缓冲。</param>
    /// <param name="cancellationToken">读取取消令牌。</param>
    /// <returns>完整读取时返回 true，帧开始前 EOF 返回 false。</returns>
    private static async ValueTask<bool> ReadExactOrEofAsync(Stream stream, Memory<byte> destination, CancellationToken cancellationToken)
    {
        int offset = 0;
        while (offset < destination.Length)
        {
            int read = await stream.ReadAsync(destination[offset..], cancellationToken);
            if (read == 0)
            {
                return offset == 0 ? false : throw new EndOfStreamException("MiniCore TCP 帧头不完整。");
            }

            offset += read;
        }

        return true;
    }

    #endregion
}
