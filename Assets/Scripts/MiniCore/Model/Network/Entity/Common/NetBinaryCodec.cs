using System;

namespace MiniCore.Model
{
    /// <summary>
    /// 网络包头使用的无分配大端整数编解码工具。
    /// </summary>
    public static class NetBinaryCodec
    {
        /// <summary>
        /// 从字节数组读取 32 位大端有符号整数。
        /// </summary>
        /// <param name="buffer">执行该方法所需的 buffer 参数。</param>
        /// <param name="offset">执行该方法所需的 offset 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public static int ReadInt32BE(byte[] buffer, int offset)
        {
            int b0 = buffer[offset];
            int b1 = buffer[offset + 1];
            int b2 = buffer[offset + 2];
            int b3 = buffer[offset + 3];
            return (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
        }

        /// <summary>
        /// 向字节数组写入 32 位大端有符号整数。
        /// </summary>
        /// <param name="buffer">执行该方法所需的 buffer 参数。</param>
        /// <param name="offset">执行该方法所需的 offset 参数。</param>
        /// <param name="value">执行该方法所需的 value 参数。</param>
        public static void WriteInt32BE(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)((value >> 24) & 0xFF);
            buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 3] = (byte)(value & 0xFF);
        }

        /// <summary>
        /// 从只读字节跨度读取 32 位大端无符号整数。
        /// </summary>
        /// <param name="buffer">执行该方法所需的 buffer 参数。</param>
        /// <param name="offset">执行该方法所需的 offset 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public static uint ReadUInt32BE(ReadOnlySpan<byte> buffer, int offset)
        {
            uint value = 0;
            for (int i = 0; i < 4; i++)
            {
                value = (value << 8) | buffer[offset + i];
            }
            return value;
        }

        /// <summary>
        /// 从只读字节跨度读取 64 位大端有符号整数。
        /// </summary>
        /// <param name="buffer">执行该方法所需的 buffer 参数。</param>
        /// <param name="offset">执行该方法所需的 offset 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public static long ReadInt64BE(ReadOnlySpan<byte> buffer, int offset)
        {
            long value = 0;
            for (int i = 0; i < 8; i++)
            {
                value = (value << 8) | buffer[offset + i];
            }
            return value;
        }

        /// <summary>
        /// 向字节数组写入 32 位大端无符号整数。
        /// </summary>
        /// <param name="buffer">执行该方法所需的 buffer 参数。</param>
        /// <param name="offset">执行该方法所需的 offset 参数。</param>
        /// <param name="value">执行该方法所需的 value 参数。</param>
        public static void WriteUInt32BE(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)((value >> 24) & 0xFF);
            buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 3] = (byte)(value & 0xFF);
        }

        /// <summary>
        /// 向字节数组写入 64 位大端有符号整数。
        /// </summary>
        /// <param name="buffer">执行该方法所需的 buffer 参数。</param>
        /// <param name="offset">执行该方法所需的 offset 参数。</param>
        /// <param name="value">执行该方法所需的 value 参数。</param>
        public static void WriteInt64BE(byte[] buffer, int offset, long value)
        {
            buffer[offset] = (byte)((value >> 56) & 0xFF);
            buffer[offset + 1] = (byte)((value >> 48) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 40) & 0xFF);
            buffer[offset + 3] = (byte)((value >> 32) & 0xFF);
            buffer[offset + 4] = (byte)((value >> 24) & 0xFF);
            buffer[offset + 5] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 6] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 7] = (byte)(value & 0xFF);
        }
    }
}
