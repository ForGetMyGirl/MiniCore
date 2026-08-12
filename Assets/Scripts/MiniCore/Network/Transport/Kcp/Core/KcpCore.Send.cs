using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static System.Math;
using BufferOwner = System.Buffers.IMemoryOwner<byte>;

namespace System.Net.Sockets.Kcp
{
    /// <summary>
    /// KCP Core 的发送能力分部实现。
    /// </summary>
    public partial class KcpCore<Segment> : IKcpSendable
    {

        /// <summary>
        /// 将连续字节跨度分片后加入 KCP 发送队列。
        /// </summary>
        /// <param name="span">执行该方法所需的 span 参数。</param>
        /// <param name="options">执行该方法所需的 options 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public int Send(ReadOnlySpan<byte> span, object options = null)
        {
            if (CheckDispose())
            {

                return -4;
            }

            if (mss <= 0)
            {
                throw new InvalidOperationException($" mss <= 0 ");
            }

            if (span.Length < 0)
            {
                return -1;
            }

            var offset = 0;
            int count;

            #region append to previous segment in streaming mode (if possible)

            #endregion

            #region fragment

            if (span.Length <= mss)
            {
                count = 1;
            }
            else
            {
                count = (int)(span.Length + mss - 1) / (int)mss;
            }

            if (count > IKCP_WND_RCV)
            {
                return -2;
            }

            if (count == 0)
            {
                count = 1;
            }

            lock (snd_queueLock)
            {
                for (var i = 0; i < count; i++)
                {
                    int size;
                    if (span.Length - offset > mss)
                    {
                        size = (int)mss;
                    }
                    else
                    {
                        size = (int)span.Length - offset;
                    }

                    var seg = SegmentManager.Alloc(size);
                    span.Slice(offset, size).CopyTo(seg.data);
                    offset += size;
                    seg.frg = stream == 0 ? (byte)(count - i - 1) : (byte)0;
                    snd_queue.Enqueue(seg);
                }
            }

            #endregion

            return offset;
        }

        /// <summary>
        /// 将只读字节序列分片后加入 KCP 发送队列。
        /// </summary>
        /// <param name="span">执行该方法所需的 span 参数。</param>
        /// <param name="options">执行该方法所需的 options 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public int Send(ReadOnlySequence<byte> span, object options = null)
        {
            if (CheckDispose())
            {

                return -4;
            }

            if (mss <= 0)
            {
                throw new InvalidOperationException($" mss <= 0 ");
            }

            if (span.Length < 0)
            {
                return -1;
            }

            var offset = 0;
            int count;

            #region append to previous segment in streaming mode (if possible)

            #endregion

            #region fragment

            if (span.Length <= mss)
            {
                count = 1;
            }
            else
            {
                count = (int)(span.Length + mss - 1) / (int)mss;
            }

            if (count > IKCP_WND_RCV)
            {
                return -2;
            }

            if (count == 0)
            {
                count = 1;
            }

            lock (snd_queueLock)
            {
                for (var i = 0; i < count; i++)
                {
                    int size;
                    if (span.Length - offset > mss)
                    {
                        size = (int)mss;
                    }
                    else
                    {
                        size = (int)span.Length - offset;
                    }

                    var seg = SegmentManager.Alloc(size);
                    span.Slice(offset, size).CopyTo(seg.data);
                    offset += size;
                    seg.frg = stream == 0 ? (byte)(count - i - 1) : (byte)0;
                    snd_queue.Enqueue(seg);
                }
            }

            #endregion

            return offset;
        }
    }
}
