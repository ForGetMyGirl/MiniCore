using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net.Sockets.Kcp
{
    /// <summary>
    /// KCP Core 的可选诊断日志能力。
    /// </summary>
    public partial class KcpCore<Segment>
    {
        /// <summary>
        /// 允许输出的 KCP 日志类型掩码。
        /// </summary>
        public KcpLogMask LogMask { get; set; } = KcpLogMask.IKCP_LOG_PARSE_DATA | KcpLogMask.IKCP_LOG_NEED_SEND | KcpLogMask.IKCP_LOG_DEAD_LINK;

        /// <summary>
        /// 判断指定日志类型当前是否可输出。
        /// </summary>
        /// <param name="mask">执行该方法所需的 mask 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public virtual bool CanLog(KcpLogMask mask)
        {
            if ((mask & LogMask) == 0)
            {
                return false;
            }

#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER
            if (TraceListener != null)
            {
                return true;
            }
#endif
            return false;
        }

#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER
        /// <summary>
        /// 接收 KCP 诊断信息的 Trace 监听器。
        /// </summary>
        public System.Diagnostics.TraceListener TraceListener { get; set; }
#endif

        /// <summary>
        /// 输出 KCP 失败诊断信息。
        /// </summary>
        /// <param name="message">执行该方法所需的 message 参数。</param>
        public virtual void LogFail(string message)
        {
#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER
            TraceListener?.Fail(message);
#endif
        }

        /// <summary>
        /// 按分类输出 KCP 诊断信息。
        /// </summary>
        /// <param name="message">执行该方法所需的 message 参数。</param>
        /// <param name="category">执行该方法所需的 category 参数。</param>
        public virtual void LogWriteLine(string message, string category)
        {
#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER
            TraceListener?.WriteLine(message, category);
#endif
        }

        [Obsolete("Call CanLog first to avoid building log strings without a TraceListener.", true)]
        /// <summary>
        /// 按日志掩码输出 KCP 诊断信息。
        /// </summary>
        /// <param name="message">执行该方法所需的 message 参数。</param>
        /// <param name="mask">执行该方法所需的 mask 参数。</param>
        public virtual void LogWriteLine(string message, KcpLogMask mask)
        {
#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER
            if (CanLog(mask))
            {
                LogWriteLine(message, mask.ToString());
            }
#endif
        }
    }

    [Flags]
    /// <summary>
    /// KCP 诊断日志类别位掩码。
    /// </summary>
    public enum KcpLogMask
    {
        IKCP_LOG_OUTPUT = 1 << 0,
        IKCP_LOG_INPUT = 1 << 1,
        IKCP_LOG_SEND = 1 << 2,
        IKCP_LOG_RECV = 1 << 3,
        IKCP_LOG_IN_DATA = 1 << 4,
        IKCP_LOG_IN_ACK = 1 << 5,
        IKCP_LOG_IN_PROBE = 1 << 6,
        IKCP_LOG_IN_WINS = 1 << 7,
        IKCP_LOG_OUT_DATA = 1 << 8,
        IKCP_LOG_OUT_ACK = 1 << 9,
        IKCP_LOG_OUT_PROBE = 1 << 10,
        IKCP_LOG_OUT_WINS = 1 << 11,

        IKCP_LOG_PARSE_DATA = 1 << 12,
        IKCP_LOG_NEED_SEND = 1 << 13,
        IKCP_LOG_DEAD_LINK = 1 << 14,
    }
}

