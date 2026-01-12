using System.Buffers;
using BufferOwner = System.Buffers.IMemoryOwner<byte>;

namespace System.Net.Sockets.Kcp
{
    public class Kcp<Segment> : KcpCore<Segment>
        where Segment : IKcpSegment
    {
        
        
        
        
        
        
        
        public Kcp(uint conv_, IKcpCallback callback, IRentable rentable = null)
            : base(conv_)
        {
            callbackHandle = callback;
            this.rentable = rentable;
        }


        

        IRentable rentable;
        
        
        
        
        
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










