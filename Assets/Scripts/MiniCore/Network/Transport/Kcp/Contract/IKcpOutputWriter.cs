using System.Buffers;

namespace System.Net.Sockets.Kcp
{
    /// <summary>
    /// 支持查询未刷新字节数和主动刷新的 KCP 输出写入器。
    /// </summary>
    public interface IKcpOutputWriter : IBufferWriter<byte>
    {
        #region Public 公共成员

        /// <summary>
        /// 获取当前尚未刷新的字节数。
        /// </summary>
        int UnflushedBytes { get; }

        /// <summary>
        /// 将已写入数据刷新到底层输出目标。
        /// </summary>
        void Flush();

        #endregion
    }
}
