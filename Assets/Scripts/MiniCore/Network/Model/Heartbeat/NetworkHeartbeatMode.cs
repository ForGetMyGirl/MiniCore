namespace MiniCore.Core
{
    /// <summary>
    /// 会话使用的心跳角色。
    /// </summary>
    public enum NetworkHeartbeatMode
    {
        /// <summary>
        /// 主动发送 Ping 并等待 Pong 的客户端模式。
        /// </summary>
        Client,
        /// <summary>
        /// 等待 Ping 并检查超时的服务端模式。
        /// </summary>
        Server
    }
}
