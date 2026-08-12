using System;

namespace MiniCore.Model
{
    /// <summary>
    /// 为 UDP 高频数据队列提供多个逻辑业务包的单数据报封装与校验。
    /// 该格式只用于 TrySend 数据队列，可靠发送、RPC 与心跳仍保持一个业务包对应一个数据报。
    /// </summary>
    internal static class UdpBatchDatagramCodec
    {
        #region Private 私有成员

        private const uint Magic = 0x4D435542; // ASCII "MCUB"，保留为 UDP 批量数据报识别码。
        private const byte Version = 1; // 当前 UDP 批量数据报格式版本。
        private const int MagicByteCount = sizeof(uint); // 批量数据报识别码长度。
        private const int PacketCountByteCount = sizeof(byte); // 批量数据报内逻辑包数量字段长度。
        private const int PacketLengthByteCount = sizeof(ushort); // 每个逻辑包正文长度字段长度。

        #endregion

        #region Internal 内部成员

        internal const int HeaderByteCount = MagicByteCount + PacketCountByteCount + PacketCountByteCount;
        internal const int PacketLengthPrefixByteCount = PacketLengthByteCount;

        /// <summary>
        /// 将 UDP 批量数据报固定头写入目标缓冲区。
        /// </summary>
        /// <param name="buffer">承载整个数据报的目标数组。</param>
        /// <param name="offset">固定头的起始写入位置。</param>
        /// <param name="packetCount">当前数据报包含的逻辑业务包数量，必须为 2 到 255。</param>
        internal static void WriteHeader(byte[] buffer, int offset, int packetCount)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (packetCount < 2 || packetCount > byte.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(packetCount));
            }

            WriteUInt32BE(buffer, offset, Magic);
            buffer[offset + MagicByteCount] = Version;
            buffer[offset + MagicByteCount + PacketCountByteCount] = (byte)packetCount;
        }

        /// <summary>
        /// 将一个逻辑业务包的长度前缀写入 UDP 批量数据报。
        /// </summary>
        /// <param name="buffer">承载整个数据报的目标数组。</param>
        /// <param name="offset">长度前缀的起始写入位置。</param>
        /// <param name="packetLength">当前逻辑业务包的有效字节数，必须大于零且不超过 UInt16 上限。</param>
        internal static void WritePacketLength(byte[] buffer, int offset, int packetLength)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (packetLength <= 0 || packetLength > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(packetLength));
            }

            buffer[offset] = (byte)(packetLength >> 8);
            buffer[offset + 1] = (byte)packetLength;
        }

        /// <summary>
        /// 判断数据报是否为有效的 UDP 批量格式，并在不分配对象的前提下校验所有逻辑包边界。
        /// </summary>
        /// <param name="datagram">收到或即将解析的完整 UDP 数据报。</param>
        /// <param name="isBatchDatagram">数据报以批量识别码开头时返回 true；无识别码时调用方应按单包处理。</param>
        /// <param name="packetCount">有效批量数据报中的逻辑业务包数量；非批量或非法数据报为零。</param>
        /// <returns>批量格式有效或当前数据报本来就不是批量格式时返回 true；识别为批量但内容非法时返回 false。</returns>
        internal static bool TryValidateBatchDatagram(
            ReadOnlyMemory<byte> datagram,
            out bool isBatchDatagram,
            out int packetCount)
        {
            isBatchDatagram = false;
            packetCount = 0;
            if (datagram.Length < MagicByteCount || ReadUInt32BE(datagram, 0) != Magic)
            {
                return true;
            }

            isBatchDatagram = true;
            if (datagram.Length < HeaderByteCount || datagram.Span[MagicByteCount] != Version)
            {
                return false;
            }

            packetCount = datagram.Span[MagicByteCount + PacketCountByteCount];
            if (packetCount < 2)
            {
                return false;
            }

            int offset = HeaderByteCount;
            for (int index = 0; index < packetCount; index++)
            {
                if (!TryReadPacket(datagram, ref offset, out _))
                {
                    return false;
                }
            }

            return offset == datagram.Length;
        }

        /// <summary>
        /// 从已验证的 UDP 批量数据报中读取下一个逻辑业务包切片。
        /// </summary>
        /// <param name="datagram">完整 UDP 批量数据报。</param>
        /// <param name="offset">输入时为当前逻辑包长度前缀位置；成功后移动到下一个逻辑包位置。</param>
        /// <param name="packet">成功时返回不复制底层数组的业务包切片。</param>
        /// <returns>当前位置存在完整、非空逻辑业务包时返回 true。</returns>
        internal static bool TryReadPacket(ReadOnlyMemory<byte> datagram, ref int offset, out ReadOnlyMemory<byte> packet)
        {
            packet = default;
            if (offset < 0 || offset > datagram.Length - PacketLengthPrefixByteCount)
            {
                return false;
            }

            int packetLength = (datagram.Span[offset] << 8) | datagram.Span[offset + 1];
            offset += PacketLengthPrefixByteCount;
            if (packetLength <= 0 || packetLength > datagram.Length - offset)
            {
                return false;
            }

            packet = datagram.Slice(offset, packetLength);
            offset += packetLength;
            return true;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 将无符号 32 位数按大端顺序写入指定数组位置。
        /// </summary>
        /// <param name="buffer">目标字节数组。</param>
        /// <param name="offset">写入起始位置。</param>
        /// <param name="value">需要写入的无符号整数。</param>
        private static void WriteUInt32BE(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }

        /// <summary>
        /// 从只读内存按大端顺序读取无符号 32 位数。
        /// </summary>
        /// <param name="data">包含读取目标的只读字节内存。</param>
        /// <param name="offset">读取起始位置。</param>
        /// <returns>按大端顺序还原的无符号整数。</returns>
        private static uint ReadUInt32BE(ReadOnlyMemory<byte> data, int offset)
        {
            return ((uint)data.Span[offset] << 24)
                | ((uint)data.Span[offset + 1] << 16)
                | ((uint)data.Span[offset + 2] << 8)
                | data.Span[offset + 3];
        }

        #endregion
    }
}
