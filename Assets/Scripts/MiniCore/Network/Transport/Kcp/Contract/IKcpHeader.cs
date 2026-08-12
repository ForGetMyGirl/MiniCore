namespace System.Net.Sockets.Kcp
{
    /// <summary>
    /// KCP 固定包头字段接口。
    /// </summary>
    public interface IKcpHeader
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置 KCP 会话标识。
        /// </summary>
        uint conv { get; set; }

        /// <summary>
        /// 获取或设置协议命令。
        /// </summary>
        byte cmd { get; set; }

        /// <summary>
        /// 获取或设置分片剩余数量。
        /// </summary>
        byte frg { get; set; }

        /// <summary>
        /// 获取或设置接收窗口大小。
        /// </summary>
        ushort wnd { get; set; }

        /// <summary>
        /// 获取或设置发送时间戳。
        /// </summary>
        uint ts { get; set; }

        /// <summary>
        /// 获取或设置分片序号。
        /// </summary>
        uint sn { get; set; }

        /// <summary>
        /// 获取或设置对端尚未确认的起始序号。
        /// </summary>
        uint una { get; set; }

        /// <summary>
        /// 获取分片负载字节数。
        /// </summary>
        uint len { get; }

        #endregion
    }
}
