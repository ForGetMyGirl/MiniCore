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
    /// KCP 可靠传输算法核心，负责窗口、确认、重传与拥塞控制。
    /// </summary>
    public partial class KcpCore<Segment> : KcpConst, IKcpSetting, IKcpUpdate, IDisposable
        where Segment : IKcpSegment
    {
        #region kcp members

        /// <summary>
        /// KCP 会话标识。
        /// </summary>
        public uint conv { get; protected set; }

        protected uint mtu;

        protected int BufferNeedSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (int)((mtu) );
            }
        }

        protected uint mss;

        protected int state;

        protected uint snd_una;

        protected uint snd_nxt;

        protected uint rcv_nxt;
        protected uint ts_recent;
        protected uint ts_lastack;

        protected uint ssthresh;

        protected uint rx_rttval;

        protected uint rx_srtt;

        protected uint rx_rto;

        protected uint rx_minrto;

        protected uint snd_wnd;

        protected uint rcv_wnd;

        protected uint rmt_wnd;

        protected uint cwnd;

        protected uint probe;
        protected uint current;

        protected uint interval;

        protected uint ts_flush;
        protected uint xmit;

        protected uint nodelay;

        protected uint updated;

        protected uint ts_probe;

        protected uint probe_wait;

        protected uint dead_link;

        protected uint incr;

        /// <summary>
        /// 快速重传阈值。
        /// </summary>
        public int fastresend;
        /// <summary>
        /// 快速确认次数上限。
        /// </summary>
        public int fastlimit;

        protected int nocwnd;
        protected int logmask;

        /// <summary>
        /// 是否启用 KCP 流模式。
        /// </summary>
        public int stream;
        protected BufferOwner buffer;

        #endregion

        #region LocksAndQueues

        protected readonly object snd_queueLock = new object();
        protected readonly object snd_bufLock = new object();
        protected readonly object rcv_bufLock = new object();
        protected readonly object rcv_queueLock = new object();

        protected ConcurrentQueue<(uint sn, uint ts)> acklist = new ConcurrentQueue<(uint sn, uint ts)>();

        internal ConcurrentQueue<Segment> snd_queue = new ConcurrentQueue<Segment>();

        internal LinkedList<Segment> snd_buf = new LinkedList<Segment>();

        internal List<Segment> rcv_queue = new List<Segment>();

        internal LinkedList<Segment> rcv_buf = new LinkedList<Segment>();

        /// <summary>
        /// 等待发送或确认的分片数量。
        /// </summary>
        public int WaitSnd => snd_buf.Count + snd_queue.Count;

        #endregion

        /// <summary>
        /// 负责分片分配和回收的管理器。
        /// </summary>
        public ISegmentManager<Segment> SegmentManager { get; set; }
        /// <summary>
        /// 使用指定 conv 创建 KCP 算法核心。
        /// </summary>
        /// <param name="conv_">执行该方法所需的 conv_ 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public KcpCore(uint conv_)
        {
            conv = conv_;

            snd_wnd = IKCP_WND_SND;
            rcv_wnd = IKCP_WND_RCV;
            rmt_wnd = IKCP_WND_RCV;
            mtu = IKCP_MTU_DEF;
            mss = mtu - IKCP_OVERHEAD;
            buffer = CreateBuffer(BufferNeedSize);

            rx_rto = IKCP_RTO_DEF;
            rx_minrto = IKCP_RTO_MIN;
            interval = IKCP_INTERVAL;
            ts_flush = IKCP_INTERVAL;
            ssthresh = IKCP_THRESH_INIT;
            fastlimit = IKCP_FASTACK_LIMIT;
            dead_link = IKCP_DEADLINK;
        }

        #region IDisposable Support
        private bool disposedValue = false;

        private bool m_disposing = false;

        /// <summary>
        /// 执行 CheckDispose 相关处理。
        /// </summary>
        /// <returns>执行处理后的结果。</returns>
        protected bool CheckDispose()
        {
            if (m_disposing)
            {
                return true;
            }

            if (disposedValue)
            {
                throw new ObjectDisposedException(
                    $"{nameof(Kcp)} [conv:{conv}]");
            }

            return false;
        }

        /// <summary>
        /// 执行 Dispose 相关处理。
        /// </summary>
        /// <param name="disposing">执行该方法所需的 disposing 参数。</param>
        protected virtual void Dispose(bool disposing)
        {
            try
            {
                m_disposing = true;
                if (!disposedValue)
                {
                    if (disposing)
                    {

                        callbackHandle = null;
                        acklist = null;
                        buffer = null;
                    }

                    void FreeCollection(IEnumerable<Segment> collection)
                    {
                        if (collection == null)
                        {
                            return;
                        }
                        foreach (var item in collection)
                        {
                            try
                            {
                                SegmentManager.Free(item);
                            }
                            catch (Exception)
                            {

                                LogFail("Unexpected exception during Dispose.");
                            }
                        }
                    }

                    lock (snd_queueLock)
                    {
                        while (snd_queue != null &&
                        (snd_queue.TryDequeue(out var segment)
                        || !snd_queue.IsEmpty)
                        )
                        {
                            try
                            {
                                SegmentManager.Free(segment);
                            }
                            catch (Exception)
                            {

                            }
                        }
                        snd_queue = null;
                    }

                    lock (snd_bufLock)
                    {
                        FreeCollection(snd_buf);
                        snd_buf?.Clear();
                        snd_buf = null;
                    }

                    lock (rcv_bufLock)
                    {
                        FreeCollection(rcv_buf);
                        rcv_buf?.Clear();
                        rcv_buf = null;
                    }

                    lock (rcv_queueLock)
                    {
                        FreeCollection(rcv_queue);
                        rcv_queue?.Clear();
                        rcv_queue = null;
                    }

                    disposedValue = true;
                }
            }
            finally
            {
                m_disposing = false;
            }

        }

        ~KcpCore()
        {

            Dispose(false);
        }

        /// <summary>
        /// 释放 KCP 缓冲区和分片资源。
        /// </summary>
        public void Dispose()
        {

            Dispose(true);

            GC.SuppressFinalize(this);
        }

        #endregion

        internal protected IKcpCallback callbackHandle;
        internal protected IKcpOutputWriter OutputWriter;

        /// <summary>
        /// 执行 Ibound 相关处理。
        /// </summary>
        /// <param name="lower">执行该方法所需的 lower 参数。</param>
        /// <param name="middle">执行该方法所需的 middle 参数。</param>
        /// <param name="upper">执行该方法所需的 upper 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        protected static uint Ibound(uint lower, uint middle, uint upper)
        {
            return Min(Max(lower, middle), upper);
        }

        /// <summary>
        /// 执行 Itimediff 相关处理。
        /// </summary>
        /// <param name="later">执行该方法所需的 later 参数。</param>
        /// <param name="earlier">执行该方法所需的 earlier 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        protected static int Itimediff(uint later, uint earlier)
        {
            return ((int)(later - earlier));
        }

        /// <summary>
        /// 执行 CreateBuffer 相关处理。
        /// </summary>
        /// <param name="needSize">执行该方法所需的 needSize 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        internal protected virtual BufferOwner CreateBuffer(int needSize)
        {
            return new KcpInnerBuffer(needSize);
        }

        internal protected class KcpInnerBuffer : BufferOwner
        {
            private readonly Memory<byte> _memory;

            public Memory<byte> Memory
            {
                get
                {
                    if (alreadyDisposed)
                    {
                        throw new ObjectDisposedException(nameof(KcpInnerBuffer));
                    }
                    return _memory;
                }
            }

            /// <summary>
            /// 执行 KcpInnerBuffer 相关处理。
            /// </summary>
            /// <param name="size">执行该方法所需的 size 参数。</param>
            /// <returns>执行处理后的结果。</returns>
            public KcpInnerBuffer(int size)
            {
                _memory = new Memory<byte>(new byte[size]);
            }

            bool alreadyDisposed = false;
            /// <summary>
            /// 执行 Dispose 相关处理。
            /// </summary>
            public void Dispose()
            {
                alreadyDisposed = true;
            }
        }

        #region CoreLogic

        /// <summary>
        /// 计算下一次需要调用 Update 的时间。
        /// </summary>
        /// <param name="time">执行该方法所需的 time 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public DateTimeOffset Check(in DateTimeOffset time)
        {
            if (CheckDispose())
            {

                return default;
            }

            if (updated == 0)
            {
                return time;
            }

            var current_ = time.ConvertTime();

            var ts_flush_ = ts_flush;
            var tm_flush_ = 0x7fffffff;
            var tm_packet = 0x7fffffff;
            var minimal = 0;

            if (Itimediff(current_, ts_flush_) >= 10000 || Itimediff(current_, ts_flush_) < -10000)
            {
                ts_flush_ = current_;
            }

            if (Itimediff(current_, ts_flush_) >= 0)
            {
                return time;
            }

            tm_flush_ = Itimediff(ts_flush_, current_);

            lock (snd_bufLock)
            {
                foreach (var seg in snd_buf)
                {
                    var diff = Itimediff(seg.resendts, current_);
                    if (diff <= 0)
                    {
                        return time;
                    }

                    if (diff < tm_packet)
                    {
                        tm_packet = diff;
                    }
                }
            }

            minimal = tm_packet < tm_flush_ ? tm_packet : tm_flush_;
            if (minimal >= interval) minimal = (int)interval;

            return time + TimeSpan.FromMilliseconds(minimal);
        }

        /// <summary>
        /// 执行 Move_Rcv_buf_2_Rcv_queue 相关处理。
        /// </summary>
        protected void Move_Rcv_buf_2_Rcv_queue()
        {
            lock (rcv_bufLock)
            {
                while (rcv_buf.Count > 0)
                {
                    var seg = rcv_buf.First.Value;
                    if (seg.sn == rcv_nxt && rcv_queue.Count < rcv_wnd)
                    {
                        rcv_buf.RemoveFirst();
                        lock (rcv_queueLock)
                        {
                            rcv_queue.Add(seg);
                        }

                        rcv_nxt++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 执行 Update_ack 相关处理。
        /// </summary>
        /// <param name="rtt">执行该方法所需的 rtt 参数。</param>
        protected void Update_ack(int rtt)
        {
            if (rx_srtt == 0)
            {
                rx_srtt = (uint)rtt;
                rx_rttval = (uint)rtt / 2;
            }
            else
            {
                int delta = (int)((uint)rtt - rx_srtt);

                if (delta < 0)
                {
                    delta = -delta;
                }

                rx_rttval = (3 * rx_rttval + (uint)delta) / 4;
                rx_srtt = (uint)((7 * rx_srtt + rtt) / 8);

                if (rx_srtt < 1)
                {
                    rx_srtt = 1;
                }
            }

            var rto = rx_srtt + Max(interval, 4 * rx_rttval);

            rx_rto = Ibound(rx_minrto, rto, IKCP_RTO_MAX);
        }

        /// <summary>
        /// 执行 Shrink_buf 相关处理。
        /// </summary>
        protected void Shrink_buf()
        {
            lock (snd_bufLock)
            {
                snd_una = snd_buf.Count > 0 ? snd_buf.First.Value.sn : snd_nxt;
            }
        }

        /// <summary>
        /// 执行 Parse_ack 相关处理。
        /// </summary>
        /// <param name="sn">执行该方法所需的 sn 参数。</param>
        protected void Parse_ack(uint sn)
        {
            if (Itimediff(sn, snd_una) < 0 || Itimediff(sn, snd_nxt) >= 0)
            {
                return;
            }

            lock (snd_bufLock)
            {
                for (var p = snd_buf.First; p != null; p = p.Next)
                {
                    var seg = p.Value;
                    if (sn == seg.sn)
                    {
                        snd_buf.Remove(p);
                        SegmentManager.Free(seg);
                        break;
                    }

                    if (Itimediff(sn, seg.sn) < 0)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 执行 Parse_una 相关处理。
        /// </summary>
        /// <param name="una">执行该方法所需的 una 参数。</param>
        protected void Parse_una(uint una)
        {

            lock (snd_bufLock)
            {
                while (snd_buf.First != null)
                {
                    var seg = snd_buf.First.Value;
                    if (Itimediff(una, seg.sn) > 0)
                    {
                        snd_buf.RemoveFirst();
                        SegmentManager.Free(seg);
                    }
                    else
                    {
                        break;
                    }
                }
            }

        }

        /// <summary>
        /// 执行 Parse_fastack 相关处理。
        /// </summary>
        /// <param name="sn">执行该方法所需的 sn 参数。</param>
        /// <param name="ts">执行该方法所需的 ts 参数。</param>
        protected void Parse_fastack(uint sn, uint ts)
        {
            if (Itimediff(sn, snd_una) < 0 || Itimediff(sn, snd_nxt) >= 0)
            {
                return;
            }

            lock (snd_bufLock)
            {
                foreach (var item in snd_buf)
                {
                    var seg = item;
                    if (Itimediff(sn, seg.sn) < 0)
                    {
                        break;
                    }
                    else if (sn != seg.sn)
                    {
#if !IKCP_FASTACK_CONSERVE
                        seg.fastack++;
#else
                        if (Itimediff(ts, seg.ts) >= 0)
                        {
                            seg.fastack++;
                        }
#endif
                    }
                }
            }
        }

        /// <summary>
        /// 执行 Parse_data 相关处理。
        /// </summary>
        /// <param name="newseg">执行该方法所需的 newseg 参数。</param>
        internal virtual void Parse_data(Segment newseg)
        {
            var sn = newseg.sn;

            lock (rcv_bufLock)
            {
                if (Itimediff(sn, rcv_nxt + rcv_wnd) >= 0 || Itimediff(sn, rcv_nxt) < 0)
                {

                    SegmentManager.Free(newseg);
                    return;
                }

                var repeat = false;

                LinkedListNode<Segment> p;
                for (p = rcv_buf.Last; p != null; p = p.Previous)
                {
                    var seg = p.Value;
                    if (seg.sn == sn)
                    {
                        repeat = true;
                        break;
                    }

                    if (Itimediff(sn, seg.sn) > 0)
                    {
                        break;
                    }
                }

                if (!repeat)
                {
                    if (CanLog(KcpLogMask.IKCP_LOG_PARSE_DATA))
                    {
                        LogWriteLine($"{newseg.ToLogString()}", KcpLogMask.IKCP_LOG_PARSE_DATA.ToString());
                    }

                    if (p == null)
                    {
                        rcv_buf.AddFirst(newseg);
                        if (newseg.frg + 1 > rcv_wnd)
                        {

                            throw new NotSupportedException($"Fragment count exceeds receive window. frgCount:{newseg.frg + 1} rcv_wnd:{rcv_wnd}");
                        }
                    }
                    else
                    {
                        rcv_buf.AddAfter(p, newseg);
                    }

                }
                else
                {
                    SegmentManager.Free(newseg);
                }
            }

            Move_Rcv_buf_2_Rcv_queue();
        }

        /// <summary>
        /// 执行 Wnd_unused 相关处理。
        /// </summary>
        /// <returns>执行处理后的结果。</returns>
        protected ushort Wnd_unused()
        {

            int waitCount = rcv_queue.Count;

            if (waitCount < rcv_wnd)
            {

                var count = rcv_wnd - waitCount;
                return (ushort)Min(count, ushort.MaxValue);
            }

            return 0;
        }

        /// <summary>
        /// 执行 Flush 相关处理。
        /// </summary>
        protected void Flush()
        {
            var current_ = current;
            var buffer_ = buffer;
            var change = 0;
            var lost = 0;
            var offset = 0;

            if (updated == 0)
            {
                return;
            }

            ushort wnd_ = Wnd_unused();

            unsafe
            {

                const int len = KcpSegment.LocalOffset + KcpSegment.HeadOffset;
                var ptr = stackalloc byte[len];
                KcpSegment seg = new KcpSegment(ptr, 0);

                seg.conv = conv;
                seg.cmd = IKCP_CMD_ACK;

                seg.wnd = wnd_;
                seg.una = rcv_nxt;

                #region flush acknowledges

                if (CheckDispose())
                {

                    return;
                }

                while (acklist.TryDequeue(out var temp))
                {
                    if (offset + IKCP_OVERHEAD > mtu)
                    {
                        callbackHandle.Output(buffer, offset);
                        offset = 0;
                        buffer = CreateBuffer(BufferNeedSize);

                    }

                    seg.sn = temp.sn;
                    seg.ts = temp.ts;
                    offset += seg.Encode(buffer.Memory.Span.Slice(offset));
                }

                #endregion

                #region probe window size (if remote window size equals zero)

                if (rmt_wnd == 0)
                {
                    if (probe_wait == 0)
                    {
                        probe_wait = IKCP_PROBE_INIT;
                        ts_probe = current + probe_wait;
                    }
                    else
                    {
                        if (Itimediff(current, ts_probe) >= 0)
                        {
                            if (probe_wait < IKCP_PROBE_INIT)
                            {
                                probe_wait = IKCP_PROBE_INIT;
                            }

                            probe_wait += probe_wait / 2;

                            if (probe_wait > IKCP_PROBE_LIMIT)
                            {
                                probe_wait = IKCP_PROBE_LIMIT;
                            }

                            ts_probe = current + probe_wait;
                            probe |= IKCP_ASK_SEND;
                        }
                    }
                }
                else
                {
                    ts_probe = 0;
                    probe_wait = 0;
                }
                #endregion

                #region flush window probing commands

                if ((probe & IKCP_ASK_SEND) != 0)
                {
                    seg.cmd = IKCP_CMD_WASK;
                    if (offset + IKCP_OVERHEAD > (int)mtu)
                    {
                        callbackHandle.Output(buffer, offset);
                        offset = 0;
                        buffer = CreateBuffer(BufferNeedSize);
                    }
                    offset += seg.Encode(buffer.Memory.Span.Slice(offset));
                }

                if ((probe & IKCP_ASK_TELL) != 0)
                {
                    seg.cmd = IKCP_CMD_WINS;
                    if (offset + IKCP_OVERHEAD > (int)mtu)
                    {
                        callbackHandle.Output(buffer, offset);
                        offset = 0;
                        buffer = CreateBuffer(BufferNeedSize);
                    }
                    offset += seg.Encode(buffer.Memory.Span.Slice(offset));
                }

                probe = 0;
                #endregion
            }

            #region FlushMoveToSend

            var cwnd_ = Min(snd_wnd, rmt_wnd);
            if (nocwnd == 0)
            {
                cwnd_ = Min(cwnd, cwnd_);
            }

            while (Itimediff(snd_nxt, snd_una + cwnd_) < 0)
            {
                if (snd_queue.TryDequeue(out var newseg))
                {
                    newseg.conv = conv;
                    newseg.cmd = IKCP_CMD_PUSH;
                    newseg.wnd = wnd_;
                    newseg.ts = current_;
                    newseg.sn = snd_nxt;
                    snd_nxt++;
                    newseg.una = rcv_nxt;
                    newseg.resendts = current_;
                    newseg.rto = rx_rto;
                    newseg.fastack = 0;
                    newseg.xmit = 0;
                    lock (snd_bufLock)
                    {
                        snd_buf.AddLast(newseg);
                    }
                }
                else
                {
                    break;
                }
            }

            #endregion

            #region FlushSendList

            var resent = fastresend > 0 ? (uint)fastresend : 0xffffffff;
            var rtomin = nodelay == 0 ? (rx_rto >> 3) : 0;

            lock (snd_bufLock)
            {

                foreach (var item in snd_buf)
                {
                    var segment = item;
                    var needsend = false;
                    var debug = Itimediff(current_, segment.resendts);
                    if (segment.xmit == 0)
                    {

                        needsend = true;
                        segment.xmit++;
                        segment.rto = rx_rto;
                        segment.resendts = current_ + rx_rto + rtomin;
                    }
                    else if (Itimediff(current_, segment.resendts) >= 0)
                    {

                        needsend = true;
                        segment.xmit++;
                        this.xmit++;
                        if (nodelay == 0)
                        {
                            segment.rto += Math.Max(segment.rto, rx_rto);
                        }
                        else
                        {
                            var step = nodelay < 2 ? segment.rto : rx_rto;
                            segment.rto += step / 2;
                        }

                        segment.resendts = current_ + segment.rto;
                        lost = 1;
                    }
                    else if (segment.fastack >= resent)
                    {

                        if (segment.xmit <= fastlimit
                            || fastlimit <= 0)
                        {
                            needsend = true;
                            segment.xmit++;
                            segment.fastack = 0;
                            segment.resendts = current_ + segment.rto;
                            change++;
                        }
                    }

                    if (needsend)
                    {
                        segment.ts = current_;
                        segment.wnd = wnd_;
                        segment.una = rcv_nxt;

                        var need = IKCP_OVERHEAD + segment.len;
                        if (offset + need > mtu)
                        {
                            callbackHandle.Output(buffer, offset);
                            offset = 0;
                            buffer = CreateBuffer(BufferNeedSize);
                        }

                        offset += segment.Encode(buffer.Memory.Span.Slice(offset));

                        if (CanLog(KcpLogMask.IKCP_LOG_NEED_SEND))
                        {
                            LogWriteLine($"{segment.ToLogString(true)}", KcpLogMask.IKCP_LOG_NEED_SEND.ToString());
                        }

                        if (segment.xmit >= dead_link)
                        {
                            state = -1;

                            if (CanLog(KcpLogMask.IKCP_LOG_DEAD_LINK))
                            {
                                LogWriteLine($"state = -1; xmit:{segment.xmit} >= dead_link:{dead_link}", KcpLogMask.IKCP_LOG_DEAD_LINK.ToString());
                            }
                        }
                    }
                }
            }

            if (offset > 0)
            {
                callbackHandle.Output(buffer, offset);
                offset = 0;
                buffer = CreateBuffer(BufferNeedSize);
            }

            #endregion

            #region update ssthresh

            if (change != 0)
            {
                var inflight = snd_nxt - snd_una;
                ssthresh = inflight / 2;
                if (ssthresh < IKCP_THRESH_MIN)
                {
                    ssthresh = IKCP_THRESH_MIN;
                }

                cwnd = ssthresh + resent;
                incr = cwnd * mss;
            }

            if (lost != 0)
            {
                ssthresh = cwnd / 2;
                if (ssthresh < IKCP_THRESH_MIN)
                {
                    ssthresh = IKCP_THRESH_MIN;
                }

                cwnd = 1;
                incr = mss;
            }

            if (cwnd < 1)
            {
                cwnd = 1;
                incr = mss;
            }
            #endregion

            if (state == -1)
            {
                OnDeadlink();
            }
        }

        /// <summary>
        /// 执行 OnDeadlink 相关处理。
        /// </summary>
        protected virtual void OnDeadlink()
        {

        }

        /// <summary>
        /// 执行 Flush2 相关处理。
        /// </summary>
        protected void Flush2()
        {
            var current_ = current;
            var change = 0;
            var lost = 0;

            if (updated == 0)
            {
                return;
            }

            ushort wnd_ = Wnd_unused();

            unsafe
            {

                const int len = KcpSegment.LocalOffset + KcpSegment.HeadOffset;
                var ptr = stackalloc byte[len];
                KcpSegment seg = new KcpSegment(ptr, 0);

                seg.conv = conv;
                seg.cmd = IKCP_CMD_ACK;

                seg.wnd = wnd_;
                seg.una = rcv_nxt;

                #region flush acknowledges

                if (CheckDispose())
                {

                    return;
                }

                while (acklist.TryDequeue(out var temp))
                {
                    if (OutputWriter.UnflushedBytes + IKCP_OVERHEAD > mtu)
                    {
                        OutputWriter.Flush();
                    }

                    seg.sn = temp.sn;
                    seg.ts = temp.ts;
                    seg.Encode(OutputWriter);
                }

                #endregion

                #region probe window size (if remote window size equals zero)

                if (rmt_wnd == 0)
                {
                    if (probe_wait == 0)
                    {
                        probe_wait = IKCP_PROBE_INIT;
                        ts_probe = current + probe_wait;
                    }
                    else
                    {
                        if (Itimediff(current, ts_probe) >= 0)
                        {
                            if (probe_wait < IKCP_PROBE_INIT)
                            {
                                probe_wait = IKCP_PROBE_INIT;
                            }

                            probe_wait += probe_wait / 2;

                            if (probe_wait > IKCP_PROBE_LIMIT)
                            {
                                probe_wait = IKCP_PROBE_LIMIT;
                            }

                            ts_probe = current + probe_wait;
                            probe |= IKCP_ASK_SEND;
                        }
                    }
                }
                else
                {
                    ts_probe = 0;
                    probe_wait = 0;
                }
                #endregion

                #region flush window probing commands

                if ((probe & IKCP_ASK_SEND) != 0)
                {
                    seg.cmd = IKCP_CMD_WASK;
                    if (OutputWriter.UnflushedBytes + IKCP_OVERHEAD > (int)mtu)
                    {
                        OutputWriter.Flush();
                    }
                    seg.Encode(OutputWriter);
                }

                if ((probe & IKCP_ASK_TELL) != 0)
                {
                    seg.cmd = IKCP_CMD_WINS;
                    if (OutputWriter.UnflushedBytes + IKCP_OVERHEAD > (int)mtu)
                    {
                        OutputWriter.Flush();
                    }
                    seg.Encode(OutputWriter);
                }

                probe = 0;
                #endregion
            }

            #region FlushMoveToSend

            var cwnd_ = Min(snd_wnd, rmt_wnd);
            if (nocwnd == 0)
            {
                cwnd_ = Min(cwnd, cwnd_);
            }

            while (Itimediff(snd_nxt, snd_una + cwnd_) < 0)
            {
                if (snd_queue.TryDequeue(out var newseg))
                {
                    newseg.conv = conv;
                    newseg.cmd = IKCP_CMD_PUSH;
                    newseg.wnd = wnd_;
                    newseg.ts = current_;
                    newseg.sn = snd_nxt;
                    snd_nxt++;
                    newseg.una = rcv_nxt;
                    newseg.resendts = current_;
                    newseg.rto = rx_rto;
                    newseg.fastack = 0;
                    newseg.xmit = 0;
                    lock (snd_bufLock)
                    {
                        snd_buf.AddLast(newseg);
                    }
                }
                else
                {
                    break;
                }
            }

            #endregion

            #region FlushSendList

            var resent = fastresend > 0 ? (uint)fastresend : 0xffffffff;
            var rtomin = nodelay == 0 ? (rx_rto >> 3) : 0;

            lock (snd_bufLock)
            {

                foreach (var item in snd_buf)
                {
                    var segment = item;
                    var needsend = false;
                    var debug = Itimediff(current_, segment.resendts);
                    if (segment.xmit == 0)
                    {

                        needsend = true;
                        segment.xmit++;
                        segment.rto = rx_rto;
                        segment.resendts = current_ + rx_rto + rtomin;
                    }
                    else if (Itimediff(current_, segment.resendts) >= 0)
                    {

                        needsend = true;
                        segment.xmit++;
                        this.xmit++;
                        if (nodelay == 0)
                        {
                            segment.rto += Math.Max(segment.rto, rx_rto);
                        }
                        else
                        {
                            var step = nodelay < 2 ? segment.rto : rx_rto;
                            segment.rto += step / 2;
                        }

                        segment.resendts = current_ + segment.rto;
                        lost = 1;
                    }
                    else if (segment.fastack >= resent)
                    {

                        if (segment.xmit <= fastlimit
                            || fastlimit <= 0)
                        {
                            needsend = true;
                            segment.xmit++;
                            segment.fastack = 0;
                            segment.resendts = current_ + segment.rto;
                            change++;
                        }
                    }

                    if (needsend)
                    {
                        segment.ts = current_;
                        segment.wnd = wnd_;
                        segment.una = rcv_nxt;

                        var need = IKCP_OVERHEAD + segment.len;
                        if (OutputWriter.UnflushedBytes + need > mtu)
                        {
                            OutputWriter.Flush();
                        }

                        segment.Encode(OutputWriter);

                        if (CanLog(KcpLogMask.IKCP_LOG_NEED_SEND))
                        {
                            LogWriteLine($"{segment.ToLogString(true)}", KcpLogMask.IKCP_LOG_NEED_SEND.ToString());
                        }

                        if (segment.xmit >= dead_link)
                        {
                            state = -1;

                            if (CanLog(KcpLogMask.IKCP_LOG_DEAD_LINK))
                            {
                                LogWriteLine($"state = -1; xmit:{segment.xmit} >= dead_link:{dead_link}", KcpLogMask.IKCP_LOG_DEAD_LINK.ToString());
                            }
                        }
                    }
                }
            }

            if (OutputWriter.UnflushedBytes > 0)
            {
                OutputWriter.Flush();
            }

            #endregion

            #region update ssthresh

            if (change != 0)
            {
                var inflight = snd_nxt - snd_una;
                ssthresh = inflight / 2;
                if (ssthresh < IKCP_THRESH_MIN)
                {
                    ssthresh = IKCP_THRESH_MIN;
                }

                cwnd = ssthresh + resent;
                incr = cwnd * mss;
            }

            if (lost != 0)
            {
                ssthresh = cwnd / 2;
                if (ssthresh < IKCP_THRESH_MIN)
                {
                    ssthresh = IKCP_THRESH_MIN;
                }

                cwnd = 1;
                incr = mss;
            }

            if (cwnd < 1)
            {
                cwnd = 1;
                incr = mss;
            }
            #endregion

            if (state == -1)
            {
                OnDeadlink();
            }
        }

        /// <summary>
        /// 推进 KCP 时钟并执行刷新、重传和确认处理。
        /// </summary>
        /// <param name="time">执行该方法所需的 time 参数。</param>
        public void Update(in DateTimeOffset time)
        {
            if (CheckDispose())
            {

                return;
            }

            current = time.ConvertTime();

            if (updated == 0)
            {
                updated = 1;
                ts_flush = current;
            }

            var slap = Itimediff(current, ts_flush);

            if (slap >= 10000 || slap < -10000)
            {
                ts_flush = current;
                slap = 0;
            }

            if (slap >= 0)
            {
                ts_flush += interval;
                if (Itimediff(current, ts_flush) >= 0)
                {
                    ts_flush = current + interval;
                }

                Flush();
            }
        }

        #endregion

        #region Settings

        /// <summary>
        /// 设置 KCP 最大传输单元并重建输出缓冲。
        /// </summary>
        /// <param name="mtu">执行该方法所需的 mtu 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public int SetMtu(int mtu = IKCP_MTU_DEF)
        {
            if (mtu < 50 || mtu < IKCP_OVERHEAD)
            {
                return -1;
            }

            var buffer_ = CreateBuffer(BufferNeedSize);
            if (null == buffer_)
            {
                return -2;
            }

            this.mtu = (uint)mtu;
            mss = this.mtu - IKCP_OVERHEAD;
            buffer.Dispose();
            buffer = buffer_;
            return 0;
        }

        /// <summary>
        /// 设置 KCP 刷新间隔。
        /// </summary>
        /// <param name="interval_">执行该方法所需的 interval_ 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public int Interval(int interval_)
        {
            if (interval_ > 5000)
            {
                interval_ = 5000;
            }
            else if (interval_ < 0)
            {

                interval_ = 0;
            }
            interval = (uint)interval_;
            return 0;
        }

        /// <summary>
        /// 配置无延迟、刷新间隔、快速重传和拥塞控制。
        /// </summary>
        /// <param name="nodelay_">执行该方法所需的 nodelay_ 参数。</param>
        /// <param name="interval_">执行该方法所需的 interval_ 参数。</param>
        /// <param name="resend_">执行该方法所需的 resend_ 参数。</param>
        /// <param name="nc_">执行该方法所需的 nc_ 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public int NoDelay(int nodelay_, int interval_, int resend_, int nc_)
        {

            if (nodelay_ > 0)
            {
                nodelay = (uint)nodelay_;
                if (nodelay_ != 0)
                {
                    rx_minrto = IKCP_RTO_NDL;
                }
                else
                {
                    rx_minrto = IKCP_RTO_MIN;
                }
            }

            if (resend_ >= 0)
            {
                fastresend = resend_;
            }

            if (nc_ >= 0)
            {
                nocwnd = nc_;
            }

            return Interval(interval_);
        }

        /// <summary>
        /// 设置发送与接收窗口大小。
        /// </summary>
        /// <param name="sndwnd">执行该方法所需的 sndwnd 参数。</param>
        /// <param name="rcvwnd">执行该方法所需的 rcvwnd 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public int WndSize(int sndwnd = IKCP_WND_SND, int rcvwnd = IKCP_WND_RCV)
        {
            if (sndwnd > 0)
            {
                snd_wnd = (uint)sndwnd;
            }

            if (rcvwnd > 0)
            {
                rcv_wnd = (uint)rcvwnd;
            }

            return 0;
        }

        #endregion

    }
}
