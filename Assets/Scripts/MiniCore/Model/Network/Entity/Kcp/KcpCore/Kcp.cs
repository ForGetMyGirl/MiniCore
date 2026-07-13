using System.Buffers;
using BufferOwner = System.Buffers.IMemoryOwner<byte>;

namespace System.Net.Sockets.Kcp
{
    /// <summary>
    /// 为 KCP Core 补充接收业务包能力的泛型实现。
    /// </summary>
    public class Kcp<Segment> : KcpCore<Segment>
        where Segment : IKcpSegment
    {
        
        
        
        
        
        
        
        /// <summary>
        /// 使用 conv、输出回调和可选缓冲池创建 KCP 实例。
        /// </summary>
        /// <param name="conv_">执行该方法所需的 conv_ 参数。</param>
        /// <param name="callback">执行该方法所需的 callback 参数。</param>
        /// <param name="rentable">执行该方法所需的 rentable 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public Kcp(uint conv_, IKcpCallback callback, IRentable rentable = null)
            : base(conv_)
        {
            callbackHandle = callback;
            this.rentable = rentable;
        }


        

        IRentable rentable;
        
        
        
        
        
        /// <summary>
        /// 执行 CreateBuffer 相关处理。
        /// </summary>
        /// <param name="needSize">执行该方法所需的 needSize 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        internal protected override BufferOwner CreateBuffer(int needSize)
        {
            var res = rentable?.RentBuffer(needSize);
            if (res == null)
            {
                return base.CreateBuffer(needSize);
            }
            else
            {
                if (res.Memory.Length < needSize)
                {
                    throw new ArgumentException($"{nameof(rentable.RentBuffer)} returned a buffer smaller than {nameof(needSize)}.");
                }
            }

            return res;
        }

        
        
        
        
        
        
        /// <summary>
        /// 尝试以可回收缓冲区形式取出已重组的业务包。
        /// </summary>
        /// <param name="buffer">执行该方法所需的 buffer 参数。</param>
        /// <param name="TryRecv">执行该方法所需的 TryRecv 参数。</param>
        public (BufferOwner buffer, int avalidLength) TryRecv()
        {
            var peekSize = -1;
            lock (rcv_queueLock)
            {
                if (rcv_queue.Count == 0)
                {
                    
                    return (null, -1);
                }

                var seq = rcv_queue[0];

                if (seq.frg == 0)
                {
                    peekSize = (int)seq.len;
                }

                if (rcv_queue.Count < seq.frg + 1)
                {
                    
                    return (null, -1);
                }

                uint length = 0;

                foreach (var item in rcv_queue)
                {
                    length += item.len;
                    if (item.frg == 0)
                    {
                        break;
                    }
                }

                peekSize = (int)length;

                if (peekSize < 0)
                {
                    return (null, -2);
                }
            }

            var buffer = CreateBuffer(peekSize);
            var recvlength = UncheckRecv(buffer.Memory.Span);
            return (buffer, recvlength);
        }

        
        
        
        
        
        
        
        /// <summary>
        /// 尝试将已重组业务包写入指定缓冲区写入器。
        /// </summary>
        /// <param name="writer">执行该方法所需的 writer 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public int TryRecv(IBufferWriter<byte> writer)
        {
            var peekSize = -1;
            lock (rcv_queueLock)
            {
                if (rcv_queue.Count == 0)
                {
                    
                    return -1;
                }

                var seq = rcv_queue[0];

                if (seq.frg == 0)
                {
                    peekSize = (int)seq.len;
                }

                if (rcv_queue.Count < seq.frg + 1)
                {
                    
                    return -1;
                }

                uint length = 0;

                foreach (var item in rcv_queue)
                {
                    length += item.len;
                    if (item.frg == 0)
                    {
                        break;
                    }
                }

                peekSize = (int)length;

                if (peekSize < 0)
                {
                    return -2;
                }
            }

            return UncheckRecv(writer);
        }

        
        
        
        
        
        /// <summary>
        /// 将下一条已重组业务包复制到指定跨度。
        /// </summary>
        /// <param name="buffer">执行该方法所需的 buffer 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public int Recv(Span<byte> buffer)
        {
            if (0 == rcv_queue.Count)
            {
                return -1;
            }

            var peekSize = PeekSize();
            if (peekSize < 0)
            {
                return -2;
            }

            if (peekSize > buffer.Length)
            {
                return -3;
            }

            
            var recvLength = UncheckRecv(buffer);

            return recvLength;
        }

        
        
        
        
        
        /// <summary>
        /// 将下一条已重组业务包写入指定缓冲区写入器。
        /// </summary>
        /// <param name="writer">执行该方法所需的 writer 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public int Recv(IBufferWriter<byte> writer)
        {
            if (0 == rcv_queue.Count)
            {
                return -1;
            }

            var peekSize = PeekSize();
            if (peekSize < 0)
            {
                return -2;
            }

            
            
            
            

            
            var recvLength = UncheckRecv(writer);

            return recvLength;
        }

        
        
        
        
        
        int UncheckRecv(Span<byte> buffer)
        {
            var recover = false;
            if (rcv_queue.Count >= rcv_wnd)
            {
                recover = true;
            }

            #region merge fragment.
            

            var recvLength = 0;
            lock (rcv_queueLock)
            {
                var count = 0;
                foreach (var seg in rcv_queue)
                {
                    seg.data.CopyTo(buffer.Slice(recvLength));
                    recvLength += (int)seg.len;

                    count++;
                    int frg = seg.frg;

                    SegmentManager.Free(seg);
                    if (frg == 0)
                    {
                        break;
                    }
                }

                if (count > 0)
                {
                    rcv_queue.RemoveRange(0, count);
                }
            }

            #endregion

            Move_Rcv_buf_2_Rcv_queue();

            #region fast recover
            
            if (rcv_queue.Count < rcv_wnd && recover)
            {
                
                
                probe |= IKCP_ASK_TELL;
            }
            #endregion
            return recvLength;
        }

        
        
        
        
        
        int UncheckRecv(IBufferWriter<byte> writer)
        {
            var recover = false;
            if (rcv_queue.Count >= rcv_wnd)
            {
                recover = true;
            }

            #region merge fragment.
            

            var recvLength = 0;
            lock (rcv_queueLock)
            {
                var count = 0;
                foreach (var seg in rcv_queue)
                {
                    var len = (int)seg.len;
                    var destination = writer.GetSpan(len);

                    seg.data.CopyTo(destination);
                    writer.Advance(len);

                    recvLength += len;

                    count++;
                    int frg = seg.frg;

                    SegmentManager.Free(seg);
                    if (frg == 0)
                    {
                        break;
                    }
                }

                if (count > 0)
                {
                    rcv_queue.RemoveRange(0, count);
                }
            }

            #endregion

            Move_Rcv_buf_2_Rcv_queue();

            #region fast recover
            
            if (rcv_queue.Count < rcv_wnd && recover)
            {
                
                
                probe |= IKCP_ASK_TELL;
            }
            #endregion
            return recvLength;
        }

        
        
        
        
        /// <summary>
        /// 获取下一条已重组业务包所需的字节数。
        /// </summary>
        /// <returns>执行处理后的结果。</returns>
        public int PeekSize()
        {
            lock (rcv_queueLock)
            {
                if (rcv_queue.Count == 0)
                {
                    
                    return -1;
                }

                var seq = rcv_queue[0];

                if (seq.frg == 0)
                {
                    return (int)seq.len;
                }

                if (rcv_queue.Count < seq.frg + 1)
                {
                    
                    return -1;
                }

                uint length = 0;

                foreach (var seg in rcv_queue)
                {
                    length += seg.len;
                    if (seg.frg == 0)
                    {
                        break;
                    }
                }

                return (int)length;
            }
        }
    }
}









