using System;

namespace MiniCore.Model
{
    public static class NetBinaryCodec
    {
        public static int ReadInt32BE(byte[] buffer, int offset)
        {
            int b0 = buffer[offset];
            int b1 = buffer[offset + 1];
            int b2 = buffer[offset + 2];
            int b3 = buffer[offset + 3];
            return (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
        }

        public static void WriteInt32BE(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)((value >> 24) & 0xFF);
            buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 3] = (byte)(value & 0xFF);
        }

        public static uint ReadUInt32BE(ReadOnlySpan<byte> buffer, int offset)
        {
            uint value = 0;
            for (int i = 0; i < 4; i++)
            {
                value = (value << 8) | buffer[offset + i];
            }
            return value;
        }

        public static long ReadInt64BE(ReadOnlySpan<byte> buffer, int offset)
        {
            long value = 0;
            for (int i = 0; i < 8; i++)
            {
                value = (value << 8) | buffer[offset + i];
            }
            return value;
        }

        public static void WriteUInt32BE(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)((value >> 24) & 0xFF);
            buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 3] = (byte)(value & 0xFF);
        }

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
