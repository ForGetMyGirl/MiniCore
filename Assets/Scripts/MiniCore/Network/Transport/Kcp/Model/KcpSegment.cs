using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace System.Net.Sockets.Kcp
{

    /// <summary>
    /// 使用非托管内存保存 KCP 包头与负载的分片结构。
    /// </summary>
    public struct KcpSegment : IKcpSegment
    {
        internal readonly unsafe byte* ptr;
        /// <summary>
        /// 使用已分配内存创建 KCP 分片视图。
        /// </summary>
        /// <param name="intPtr">执行该方法所需的 intPtr 参数。</param>
        /// <param name="appendDateSize">执行该方法所需的 appendDateSize 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public unsafe KcpSegment(byte* intPtr, uint appendDateSize)
        {
            this.ptr = intPtr;
            len = appendDateSize;
        }

        /// <summary>
        /// 分配包含 KCP 本地状态、包头和负载的非托管内存。
        /// </summary>
        /// <param name="appendDateSize">执行该方法所需的 appendDateSize 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public static KcpSegment AllocHGlobal(int appendDateSize)
        {
            var total = LocalOffset + HeadOffset + appendDateSize;
            IntPtr intPtr = Marshal.AllocHGlobal(total);
            unsafe
            {

                Span<byte> span = new Span<byte>(intPtr.ToPointer(), total);
                span.Clear();

                return new KcpSegment((byte*)intPtr.ToPointer(), (uint)appendDateSize);
            }
        }

        /// <summary>
        /// 释放由 <see cref="AllocHGlobal"/> 分配的非托管内存。
        /// </summary>
        /// <param name="seg">执行该方法所需的 seg 参数。</param>
        public static void FreeHGlobal(KcpSegment seg)
        {
            unsafe
            {
                Marshal.FreeHGlobal((IntPtr)seg.ptr);
            }
        }

        /// <summary>
        /// 下一次重传的时间戳。
        /// </summary>
        public uint resendts
        {
            get
            {
                unsafe
                {
                    return *(uint*)(ptr + 0);
                }
            }
            set
            {
                unsafe
                {
                    *(uint*)(ptr + 0) = value;
                }
            }
        }

        /// <summary>
        /// 当前分片的重传超时。
        /// </summary>
        public uint rto
        {
            get
            {
                unsafe
                {
                    return *(uint*)(ptr + 4);
                }
            }
            set
            {
                unsafe
                {
                    *(uint*)(ptr + 4) = value;
                }
            }
        }

        /// <summary>
        /// 累计快速确认次数。
        /// </summary>
        public uint fastack
        {
            get
            {
                unsafe
                {
                    return *(uint*)(ptr + 8);
                }
            }
            set
            {
                unsafe
                {
                    *(uint*)(ptr + 8) = value;
                }
            }
        }

        /// <summary>
        /// 该分片已发送次数。
        /// </summary>
        public uint xmit
        {
            get
            {
                unsafe
                {
                    return *(uint*)(ptr + 12);
                }
            }
            set
            {
                unsafe
                {
                    *(uint*)(ptr + 12) = value;
                }
            }
        }

        /// <summary>
        /// 分片本地重传状态区长度。
        /// </summary>
        public const int LocalOffset = 4 * 4;
        /// <summary>
        /// KCP 协议包头长度。
        /// </summary>
        public const int HeadOffset = KcpConst.IKCP_OVERHEAD;

        /// <summary>
        /// KCP 会话标识。
        /// </summary>
        public uint conv
        {
            get
            {
                unsafe
                {
                    return *(uint*)(LocalOffset + 0 + ptr);
                }
            }
            set
            {
                unsafe
                {
                    *(uint*)(LocalOffset + 0 + ptr) = value;
                }
            }
        }

        /// <summary>
        /// KCP 命令类型。
        /// </summary>
        public byte cmd
        {
            get
            {
                unsafe
                {
                    return *(LocalOffset + 4 + ptr);
                }
            }
            set
            {
                unsafe
                {
                    *(LocalOffset + 4 + ptr) = value;
                }
            }
        }

        /// <summary>
        /// 业务消息剩余分片数。
        /// </summary>
        public byte frg
        {
            get
            {
                unsafe
                {
                    return *(LocalOffset + 5 + ptr);
                }
            }
            set
            {
                unsafe
                {
                    *(LocalOffset + 5 + ptr) = value;
                }
            }
        }

        /// <summary>
        /// 发送方通告的可用接收窗口。
        /// </summary>
        public ushort wnd
        {
            get
            {
                unsafe
                {
                    return *(ushort*)(LocalOffset + 6 + ptr);
                }
            }
            set
            {
                unsafe
                {
                    *(ushort*)(LocalOffset + 6 + ptr) = value;
                }
            }
        }

        /// <summary>
        /// 发送该分片时的时间戳。
        /// </summary>
        public uint ts
        {
            get
            {
                unsafe
                {
                    return *(uint*)(LocalOffset + 8 + ptr);
                }
            }
            set
            {
                unsafe
                {
                    *(uint*)(LocalOffset + 8 + ptr) = value;
                }
            }
        }

        /// <summary>
        /// 分片序列号。
        /// </summary>
        public uint sn
        {
            get
            {
                unsafe
                {
                    return *(uint*)(LocalOffset + 12 + ptr);
                }
            }
            set
            {
                unsafe
                {
                    *(uint*)(LocalOffset + 12 + ptr) = value;
                }
            }
        }

        /// <summary>
        /// 接收方期望的下一个序列号。
        /// </summary>
        public uint una
        {
            get
            {
                unsafe
                {
                    return *(uint*)(LocalOffset + 16 + ptr);
                }
            }
            set
            {
                unsafe
                {
                    *(uint*)(LocalOffset + 16 + ptr) = value;
                }
            }
        }

        /// <summary>
        /// 分片负载字节长度。
        /// </summary>
        public uint len
        {
            get
            {
                unsafe
                {
                    return *(uint*)(LocalOffset + 20 + ptr);
                }
            }
            private set
            {
                unsafe
                {
                    *(uint*)(LocalOffset + 20 + ptr) = value;
                }
            }
        }

        /// <summary>
        /// 分片负载的可写内存跨度。
        /// </summary>
        public Span<byte> data
        {
            get
            {
                unsafe
                {
                    return new Span<byte>(LocalOffset + HeadOffset + ptr, (int)len);
                }
            }
        }

        /// <summary>
        /// 将 KCP 包头和负载编码到目标缓冲区。
        /// </summary>
        /// <param name="buffer">执行该方法所需的 buffer 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public int Encode(Span<byte> buffer)
        {
            var datelen = (int)(HeadOffset + len);

            const int offset = 0;

            if (KcpConst.IsLittleEndian)
            {
                if (BitConverter.IsLittleEndian)
                {

                    unsafe
                    {

                        Span<byte> sendDate = new Span<byte>(ptr + LocalOffset, datelen);
                        sendDate.CopyTo(buffer);
                    }
                }
                else
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(offset), conv);
                    buffer[offset + 4] = cmd;
                    buffer[offset + 5] = frg;
                    BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(offset + 6), wnd);

                    BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(offset + 8), ts);
                    BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(offset + 12), sn);
                    BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(offset + 16), una);
                    BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(offset + 20), len);

                    data.CopyTo(buffer.Slice(HeadOffset));
                }
            }
            else
            {
                if (BitConverter.IsLittleEndian)
                {
                    BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset), conv);
                    buffer[offset + 4] = cmd;
                    buffer[offset + 5] = frg;
                    BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset + 6), wnd);

                    BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset + 8), ts);
                    BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset + 12), sn);
                    BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset + 16), una);
                    BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset + 20), len);

                    data.CopyTo(buffer.Slice(HeadOffset));
                }
                else
                {

                    unsafe
                    {

                        Span<byte> sendDate = new Span<byte>(ptr + LocalOffset, datelen);
                        sendDate.CopyTo(buffer);
                    }
                }
            }

            return datelen;
        }
    }
}
