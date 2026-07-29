using MiniCore.Model;

namespace MiniCore.Core
{
    /// <summary>
    /// 从传输线程复制后等待主线程处理的完整业务包。
    /// </summary>
    public struct NetworkIncomingPacket
    {
        /// <summary>
        /// 接收该数据包的逻辑会话。
        /// </summary>
        public NetworkSession Session;
        /// <summary>
        /// 从缓冲池租用的数据缓冲区。
        /// </summary>
        public byte[] Buffer;
        /// <summary>
        /// 缓冲区中有效数据的字节长度。
        /// </summary>
        public int Length;
        /// <summary>
        /// 诊断启用时记录进入入站队列的 Stopwatch tick；零表示本包不采样。
        /// </summary>
        public long EnqueuedTicks;
    }
}
