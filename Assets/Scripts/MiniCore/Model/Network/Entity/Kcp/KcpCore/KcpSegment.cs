using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace System.Net.Sockets.Kcp
{
    
    
    
    
    
    
    
    public struct KcpSegment : IKcpSegment
    {
        internal readonly unsafe byte* ptr;
        public unsafe KcpSegment(byte* intPtr, uint appendDateSize)
        {
            this.ptr = intPtr;
            len = appendDateSize;
        }

        
        
        
        
        
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

        
        
        
        
        public static void FreeHGlobal(KcpSegment seg)
        {
            unsafe
            {
                Marshal.FreeHGlobal((IntPtr)seg.ptr);
            }
        }

        
        
        
        
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

        
        public const int LocalOffset = 4 * 4;
        public const int HeadOffset = KcpConst.IKCP_OVERHEAD;

        
        
        
        
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
