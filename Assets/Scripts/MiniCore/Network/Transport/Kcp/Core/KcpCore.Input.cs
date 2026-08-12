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
    /// KCP Core 的收包解析能力分部实现。
    /// </summary>
    public partial class KcpCore<Segment> : IKcpInputable
    {

        /// <summary>
        /// 解析连续 KCP 数据报并更新接收、确认与重传状态。
        /// </summary>
        /// <param name="span">执行该方法所需的 span 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public int Input(ReadOnlySpan<byte> span)
        {
            if (CheckDispose())
            {

                return -4;
            }

            if (CanLog(KcpLogMask.IKCP_LOG_INPUT))
            {
                LogWriteLine($"[RI] {span.Length} bytes", KcpLogMask.IKCP_LOG_INPUT.ToString());
            }

            if (span.Length < IKCP_OVERHEAD)
            {
                return -1;
            }

            uint prev_una = snd_una;
            var offset = 0;
            int flag = 0;
            uint maxack = 0;
            uint latest_ts = 0;
            while (true)
            {
                uint ts = 0;
                uint sn = 0;
                uint length = 0;
                uint una = 0;
                uint conv_ = 0;
                ushort wnd = 0;
                byte cmd = 0;
                byte frg = 0;

                if (span.Length - offset < IKCP_OVERHEAD)
                {
                    break;
                }

                Span<byte> header = stackalloc byte[24];
                span.Slice(offset, 24).CopyTo(header);
                offset += ReadHeader(header,
                                     ref conv_,
                                     ref cmd,
                                     ref frg,
                                     ref wnd,
                                     ref ts,
                                     ref sn,
                                     ref una,
                                     ref length);

                if (conv != conv_)
                {
                    return -1;
                }

                if (span.Length - offset < length || (int)length < 0)
                {
                    return -2;
                }

                switch (cmd)
                {
                    case IKCP_CMD_PUSH:
                    case IKCP_CMD_ACK:
                    case IKCP_CMD_WASK:
                    case IKCP_CMD_WINS:
                        break;
                    default:
                        return -3;
                }

                rmt_wnd = wnd;
                Parse_una(una);
                Shrink_buf();

                if (IKCP_CMD_ACK == cmd)
                {
                    if (Itimediff(current, ts) >= 0)
                    {
                        Update_ack(Itimediff(current, ts));
                    }
                    Parse_ack(sn);
                    Shrink_buf();

                    if (flag == 0)
                    {
                        flag = 1;
                        maxack = sn;
                        latest_ts = ts;
                    }
                    else if (Itimediff(sn, maxack) > 0)
                    {
#if !IKCP_FASTACK_CONSERVE
                        maxack = sn;
                        latest_ts = ts;
#else
                        if (Itimediff(ts, latest_ts) > 0)
                        {
                            maxack = sn;
                            latest_ts = ts;
                        }
#endif
                    }

                    if (CanLog(KcpLogMask.IKCP_LOG_IN_ACK))
                    {
                        LogWriteLine($"input ack: sn={sn} rtt={Itimediff(current, ts)} rto={rx_rto}", KcpLogMask.IKCP_LOG_IN_ACK.ToString());
                    }
                }
                else if (IKCP_CMD_PUSH == cmd)
                {
                    if (CanLog(KcpLogMask.IKCP_LOG_IN_DATA))
                    {
                        LogWriteLine($"input psh: sn={sn} ts={ts}", KcpLogMask.IKCP_LOG_IN_DATA.ToString());
                    }

                    if (Itimediff(sn, rcv_nxt + rcv_wnd) < 0)
                    {

                        acklist.Enqueue((sn, ts));

                        if (Itimediff(sn, rcv_nxt) >= 0)
                        {
                            var seg = SegmentManager.Alloc((int)length);
                            seg.conv = conv_;
                            seg.cmd = cmd;
                            seg.frg = frg;
                            seg.wnd = wnd;
                            seg.ts = ts;
                            seg.sn = sn;
                            seg.una = una;

                            if (length > 0)
                            {
                                span.Slice(offset, (int)length).CopyTo(seg.data);
                            }

                            Parse_data(seg);
                        }
                    }
                }
                else if (IKCP_CMD_WASK == cmd)
                {

                    probe |= IKCP_ASK_TELL;

                    if (CanLog(KcpLogMask.IKCP_LOG_IN_PROBE))
                    {
                        LogWriteLine($"input probe", KcpLogMask.IKCP_LOG_IN_PROBE.ToString());
                    }
                }
                else if (IKCP_CMD_WINS == cmd)
                {

                    if (CanLog(KcpLogMask.IKCP_LOG_IN_WINS))
                    {
                        LogWriteLine($"input wins: {wnd}", KcpLogMask.IKCP_LOG_IN_WINS.ToString());
                    }
                }
                else
                {
                    return -3;
                }

                offset += (int)length;
            }

            if (flag != 0)
            {
                Parse_fastack(maxack, latest_ts);
            }

            if (Itimediff(this.snd_una, prev_una) > 0)
            {
                if (cwnd < rmt_wnd)
                {
                    if (cwnd < ssthresh)
                    {
                        cwnd++;
                        incr += mss;
                    }
                    else
                    {
                        if (incr < mss)
                        {
                            incr = mss;
                        }
                        incr += (mss * mss) / incr + (mss / 16);
                        if ((cwnd + 1) * mss <= incr)
                        {
#if true
                            cwnd = (incr + mss - 1) / ((mss > 0) ? mss : 1);
#else
                            cwnd++;
#endif
                        }
                    }
                    if (cwnd > rmt_wnd)
                    {
                        cwnd = rmt_wnd;
                        incr = rmt_wnd * mss;
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// 解析分段 KCP 数据报并更新接收、确认与重传状态。
        /// </summary>
        /// <param name="span">执行该方法所需的 span 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public int Input(ReadOnlySequence<byte> span)
        {
            if (CheckDispose())
            {

                return -4;
            }

            if (CanLog(KcpLogMask.IKCP_LOG_INPUT))
            {
                LogWriteLine($"[RI] {span.Length} bytes", KcpLogMask.IKCP_LOG_INPUT.ToString());
            }

            if (span.Length < IKCP_OVERHEAD)
            {
                return -1;
            }

            uint prev_una = snd_una;
            var offset = 0;
            int flag = 0;
            uint maxack = 0;
            uint latest_ts = 0;
            while (true)
            {
                uint ts = 0;
                uint sn = 0;
                uint length = 0;
                uint una = 0;
                uint conv_ = 0;
                ushort wnd = 0;
                byte cmd = 0;
                byte frg = 0;

                if (span.Length - offset < IKCP_OVERHEAD)
                {
                    break;
                }

                Span<byte> header = stackalloc byte[24];
                span.Slice(offset, 24).CopyTo(header);
                offset += ReadHeader(header,
                                     ref conv_,
                                     ref cmd,
                                     ref frg,
                                     ref wnd,
                                     ref ts,
                                     ref sn,
                                     ref una,
                                     ref length);

                if (conv != conv_)
                {
                    return -1;
                }

                if (span.Length - offset < length || (int)length < 0)
                {
                    return -2;
                }

                switch (cmd)
                {
                    case IKCP_CMD_PUSH:
                    case IKCP_CMD_ACK:
                    case IKCP_CMD_WASK:
                    case IKCP_CMD_WINS:
                        break;
                    default:
                        return -3;
                }

                rmt_wnd = wnd;
                Parse_una(una);
                Shrink_buf();

                if (IKCP_CMD_ACK == cmd)
                {
                    if (Itimediff(current, ts) >= 0)
                    {
                        Update_ack(Itimediff(current, ts));
                    }
                    Parse_ack(sn);
                    Shrink_buf();

                    if (flag == 0)
                    {
                        flag = 1;
                        maxack = sn;
                        latest_ts = ts;
                    }
                    else if (Itimediff(sn, maxack) > 0)
                    {
#if !IKCP_FASTACK_CONSERVE
                        maxack = sn;
                        latest_ts = ts;
#else
                        if (Itimediff(ts, latest_ts) > 0)
                        {
                            maxack = sn;
                            latest_ts = ts;
                        }
#endif
                    }

                    if (CanLog(KcpLogMask.IKCP_LOG_IN_ACK))
                    {
                        LogWriteLine($"input ack: sn={sn} rtt={Itimediff(current, ts)} rto={rx_rto}", KcpLogMask.IKCP_LOG_IN_ACK.ToString());
                    }
                }
                else if (IKCP_CMD_PUSH == cmd)
                {
                    if (CanLog(KcpLogMask.IKCP_LOG_IN_DATA))
                    {
                        LogWriteLine($"input psh: sn={sn} ts={ts}", KcpLogMask.IKCP_LOG_IN_DATA.ToString());
                    }

                    if (Itimediff(sn, rcv_nxt + rcv_wnd) < 0)
                    {

                        acklist.Enqueue((sn, ts));

                        if (Itimediff(sn, rcv_nxt) >= 0)
                        {
                            var seg = SegmentManager.Alloc((int)length);
                            seg.conv = conv_;
                            seg.cmd = cmd;
                            seg.frg = frg;
                            seg.wnd = wnd;
                            seg.ts = ts;
                            seg.sn = sn;
                            seg.una = una;

                            if (length > 0)
                            {
                                span.Slice(offset, (int)length).CopyTo(seg.data);
                            }

                            Parse_data(seg);
                        }
                    }
                }
                else if (IKCP_CMD_WASK == cmd)
                {

                    probe |= IKCP_ASK_TELL;

                    if (CanLog(KcpLogMask.IKCP_LOG_IN_PROBE))
                    {
                        LogWriteLine($"input probe", KcpLogMask.IKCP_LOG_IN_PROBE.ToString());
                    }
                }
                else if (IKCP_CMD_WINS == cmd)
                {

                    if (CanLog(KcpLogMask.IKCP_LOG_IN_WINS))
                    {
                        LogWriteLine($"input wins: {wnd}", KcpLogMask.IKCP_LOG_IN_WINS.ToString());
                    }
                }
                else
                {
                    return -3;
                }

                offset += (int)length;
            }

            if (flag != 0)
            {
                Parse_fastack(maxack, latest_ts);
            }

            if (Itimediff(this.snd_una, prev_una) > 0)
            {
                if (cwnd < rmt_wnd)
                {
                    if (cwnd < ssthresh)
                    {
                        cwnd++;
                        incr += mss;
                    }
                    else
                    {
                        if (incr < mss)
                        {
                            incr = mss;
                        }
                        incr += (mss * mss) / incr + (mss / 16);
                        if ((cwnd + 1) * mss <= incr)
                        {
#if true
                            cwnd = (incr + mss - 1) / ((mss > 0) ? mss : 1);
#else
                            cwnd++;
#endif
                        }
                    }
                    if (cwnd > rmt_wnd)
                    {
                        cwnd = rmt_wnd;
                        incr = rmt_wnd * mss;
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// 从 KCP 包头读取全部协议字段。
        /// </summary>
        /// <param name="header">执行该方法所需的 header 参数。</param>
        /// <param name="conv_">执行该方法所需的 conv_ 参数。</param>
        /// <param name="cmd">执行该方法所需的 cmd 参数。</param>
        /// <param name="frg">执行该方法所需的 frg 参数。</param>
        /// <param name="wnd">执行该方法所需的 wnd 参数。</param>
        /// <param name="ts">执行该方法所需的 ts 参数。</param>
        /// <param name="sn">执行该方法所需的 sn 参数。</param>
        /// <param name="una">执行该方法所需的 una 参数。</param>
        /// <param name="length">执行该方法所需的 length 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public static int ReadHeader(ReadOnlySpan<byte> header,
                              ref uint conv_,
                              ref byte cmd,
                              ref byte frg,
                              ref ushort wnd,
                              ref uint ts,
                              ref uint sn,
                              ref uint una,
                              ref uint length)
        {
            var offset = 0;
            if (IsLittleEndian)
            {
                conv_ = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(offset));
                offset += 4;

                cmd = header[offset];
                offset += 1;
                frg = header[offset];
                offset += 1;
                wnd = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(offset));
                offset += 2;

                ts = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(offset));
                offset += 4;
                sn = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(offset));
                offset += 4;
                una = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(offset));
                offset += 4;
                length = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(offset));
                offset += 4;
            }
            else
            {
                conv_ = BinaryPrimitives.ReadUInt32BigEndian(header.Slice(offset));
                offset += 4;
                cmd = header[offset];
                offset += 1;
                frg = header[offset];
                offset += 1;
                wnd = BinaryPrimitives.ReadUInt16BigEndian(header.Slice(offset));
                offset += 2;

                ts = BinaryPrimitives.ReadUInt32BigEndian(header.Slice(offset));
                offset += 4;
                sn = BinaryPrimitives.ReadUInt32BigEndian(header.Slice(offset));
                offset += 4;
                una = BinaryPrimitives.ReadUInt32BigEndian(header.Slice(offset));
                offset += 4;
                length = BinaryPrimitives.ReadUInt32BigEndian(header.Slice(offset));
                offset += 4;
            }

            return offset;
        }
    }
}
