namespace MiniCore.Model
{
    /// <summary>
    /// 表示非等待普通消息发送尝试的入队结果。
    /// </summary>
    public enum NetworkSendResult
    {
        /// <summary>
        /// 消息已进入当前会话的出站队列。
        /// </summary>
        Accepted = 0,
        /// <summary>
        /// 指定会话不存在。
        /// </summary>
        SessionNotFound = 1,
        /// <summary>
        /// 指定会话已经断开。
        /// </summary>
        Disconnected = 2,
        /// <summary>
        /// 对应优先级队列达到消息数或字节数上限。
        /// </summary>
        QueueFull = 3
    }
}
