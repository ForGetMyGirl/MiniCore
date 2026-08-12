using MiniCore.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// 表示已完成封包、等待会话发送器写入底层传输的出站数据包。
    /// </summary>
    internal struct NetworkOutgoingPacket
    {
        #region Internal 内部成员

        internal byte[] Buffer; // 队列拥有并在发送或丢弃后归还的数组。
        internal int Length; // 数组中有效协议包长度。
        internal bool ReturnToPool; // 指示发送器完成后是否归还共享缓冲池。
        internal MTaskCompletionSource<bool> CompletionSource; // 等待实际写入完成的 SendAsync 调用者；TrySend 为 null。
        internal long EnqueuedTicks; // 诊断启用时记录进入出站队列的 Stopwatch tick；零表示本包不采样。

        #endregion
    }
}
