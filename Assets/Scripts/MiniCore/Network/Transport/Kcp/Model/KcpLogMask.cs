using System;

namespace System.Net.Sockets.Kcp
{
    /// <summary>
    /// KCP 诊断日志类别位掩码。
    /// </summary>
    [Flags]
    public enum KcpLogMask
    {
        /// <summary>
        /// 输出数据报文。
        /// </summary>
        IKCP_LOG_OUTPUT = 1 << 0,

        /// <summary>
        /// 输入数据报文。
        /// </summary>
        IKCP_LOG_INPUT = 1 << 1,

        /// <summary>
        /// 业务发送调用。
        /// </summary>
        IKCP_LOG_SEND = 1 << 2,

        /// <summary>
        /// 业务接收调用。
        /// </summary>
        IKCP_LOG_RECV = 1 << 3,

        /// <summary>
        /// 输入的数据分片。
        /// </summary>
        IKCP_LOG_IN_DATA = 1 << 4,

        /// <summary>
        /// 输入的确认分片。
        /// </summary>
        IKCP_LOG_IN_ACK = 1 << 5,

        /// <summary>
        /// 输入的窗口探测请求。
        /// </summary>
        IKCP_LOG_IN_PROBE = 1 << 6,

        /// <summary>
        /// 输入的窗口大小通知。
        /// </summary>
        IKCP_LOG_IN_WINS = 1 << 7,

        /// <summary>
        /// 输出的数据分片。
        /// </summary>
        IKCP_LOG_OUT_DATA = 1 << 8,

        /// <summary>
        /// 输出的确认分片。
        /// </summary>
        IKCP_LOG_OUT_ACK = 1 << 9,

        /// <summary>
        /// 输出的窗口探测请求。
        /// </summary>
        IKCP_LOG_OUT_PROBE = 1 << 10,

        /// <summary>
        /// 输出的窗口大小通知。
        /// </summary>
        IKCP_LOG_OUT_WINS = 1 << 11,

        /// <summary>
        /// 数据分片解析过程。
        /// </summary>
        IKCP_LOG_PARSE_DATA = 1 << 12,

        /// <summary>
        /// 分片进入待发送状态。
        /// </summary>
        IKCP_LOG_NEED_SEND = 1 << 13,

        /// <summary>
        /// 连接达到死链阈值。
        /// </summary>
        IKCP_LOG_DEAD_LINK = 1 << 14
    }
}
