namespace MiniCore.Model
{
    /// <summary>
    /// MiniCore 网络模块提供的传输类型。
    /// </summary>
    public enum NetworkTransportKind
    {
        /// <summary>
        /// 有序可靠字节流传输。
        /// </summary>
        Tcp,

        /// <summary>
        /// 无连接数据报传输。
        /// </summary>
        Udp,

        /// <summary>
        /// 基于 UDP 的可靠低延迟传输。
        /// </summary>
        Kcp,

        /// <summary>
        /// RFC 6455 WebSocket 二进制传输。
        /// </summary>
        WebSocket
    }
}
