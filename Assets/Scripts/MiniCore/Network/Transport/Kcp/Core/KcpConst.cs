namespace System.Net.Sockets.Kcp
{
    /// <summary>
    /// KCP 协议算法使用的默认常量集合。
    /// </summary>
    public abstract class KcpConst
    {
        #region Public 公共成员

        /// <summary>
        /// 无延迟模式下的最小重传超时毫秒数。
        /// </summary>
        public const int IKCP_RTO_NDL = 30;

        /// <summary>
        /// 默认最小重传超时毫秒数。
        /// </summary>
        public const int IKCP_RTO_MIN = 100;

        /// <summary>
        /// 默认重传超时毫秒数。
        /// </summary>
        public const int IKCP_RTO_DEF = 200;

        /// <summary>
        /// 最大重传超时毫秒数。
        /// </summary>
        public const int IKCP_RTO_MAX = 60000;

        /// <summary>
        /// 数据推送命令标识。
        /// </summary>
        public const int IKCP_CMD_PUSH = 81;

        /// <summary>
        /// 数据确认命令标识。
        /// </summary>
        public const int IKCP_CMD_ACK = 82;

        /// <summary>
        /// 远端窗口探测请求命令标识。
        /// </summary>
        public const int IKCP_CMD_WASK = 83;

        /// <summary>
        /// 远端窗口大小通知命令标识。
        /// </summary>
        public const int IKCP_CMD_WINS = 84;

        /// <summary>
        /// 请求发送窗口探测的标记。
        /// </summary>
        public const int IKCP_ASK_SEND = 1;

        /// <summary>
        /// 请求通知本地窗口大小的标记。
        /// </summary>
        public const int IKCP_ASK_TELL = 2;

        /// <summary>
        /// 默认发送窗口分片数。
        /// </summary>
        public const int IKCP_WND_SND = 32;

        /// <summary>
        /// 默认接收窗口分片数。
        /// </summary>
        public const int IKCP_WND_RCV = 128;

        /// <summary>
        /// 默认最大传输单元字节数。
        /// </summary>
        public const int IKCP_MTU_DEF = 1400;

        /// <summary>
        /// 快速确认的默认触发次数。
        /// </summary>
        public const int IKCP_ACK_FAST = 3;

        /// <summary>
        /// 默认协议更新间隔毫秒数。
        /// </summary>
        public const int IKCP_INTERVAL = 100;

        /// <summary>
        /// KCP 协议头字节数。
        /// </summary>
        public const int IKCP_OVERHEAD = 24;

        /// <summary>
        /// 判定死链的最大重传次数。
        /// </summary>
        public const int IKCP_DEADLINK = 20;

        /// <summary>
        /// 拥塞控制初始慢启动阈值。
        /// </summary>
        public const int IKCP_THRESH_INIT = 2;

        /// <summary>
        /// 拥塞控制最小慢启动阈值。
        /// </summary>
        public const int IKCP_THRESH_MIN = 2;

        /// <summary>
        /// 零窗口探测的初始等待毫秒数。
        /// </summary>
        public const int IKCP_PROBE_INIT = 7000;

        /// <summary>
        /// 零窗口探测的最大等待毫秒数。
        /// </summary>
        public const int IKCP_PROBE_LIMIT = 120000;

        /// <summary>
        /// 单个分片允许累计的快速确认上限。
        /// </summary>
        public const int IKCP_FASTACK_LIMIT = 5;

        /// <summary>
        /// 获取或设置协议编解码是否按小端序处理。
        /// </summary>
        public static bool IsLittleEndian = true;

        #endregion
    }
}
