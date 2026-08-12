using BufferOwner = System.Buffers.IMemoryOwner<byte>;

namespace System.Net.Sockets.Kcp
{
    /// <summary>
    /// 接收 KCP 输出分片的回调接口。
    /// </summary>
    public interface IKcpCallback
    {
        #region Public 公共成员

        /// <summary>
        /// 接收一段已编码的 KCP 输出数据。
        /// </summary>
        /// <param name="buffer">持有输出字节的可释放缓冲区。</param>
        /// <param name="avalidLength">缓冲区内的有效字节数。</param>
        void Output(BufferOwner buffer, int avalidLength);

        #endregion
    }
}
