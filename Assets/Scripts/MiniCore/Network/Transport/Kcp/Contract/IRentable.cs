using BufferOwner = System.Buffers.IMemoryOwner<byte>;

namespace System.Net.Sockets.Kcp
{
    /// <summary>
    /// 为 KCP Core 提供可回收字节缓冲区的接口。
    /// </summary>
    public interface IRentable
    {
        #region Public 公共成员

        /// <summary>
        /// 租用至少容纳指定字节数的缓冲区。
        /// </summary>
        /// <param name="length">最小容量字节数。</param>
        /// <returns>需要由调用方释放的缓冲区所有者。</returns>
        BufferOwner RentBuffer(int length);

        #endregion
    }
}
