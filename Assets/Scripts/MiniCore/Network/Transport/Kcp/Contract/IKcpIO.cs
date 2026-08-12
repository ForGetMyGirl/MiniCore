using System;
using System.Buffers;
using System.Threading.Tasks;

namespace System.Net.Sockets.Kcp
{
    /// <summary>
    /// 组合 KCP 收发能力的异步 I/O 接口。
    /// </summary>
    public interface IKcpIO : IKcpSendable, IKcpInputable
    {
        #region Public 公共成员

        /// <summary>
        /// 将一条完整 KCP 消息写入目标缓冲写入器。
        /// </summary>
        /// <param name="writer">接收消息字节的缓冲写入器。</param>
        /// <param name="options">可选的调用方上下文。</param>
        /// <returns>读取完成任务。</returns>
        ValueTask RecvAsync(IBufferWriter<byte> writer, object options = null);

        /// <summary>
        /// 将一条完整 KCP 消息写入指定数组片段。
        /// </summary>
        /// <param name="buffer">接收消息字节的数组片段。</param>
        /// <param name="options">可选的调用方上下文。</param>
        /// <returns>实际写入的字节数。</returns>
        ValueTask<int> RecvAsync(ArraySegment<byte> buffer, object options = null);

        /// <summary>
        /// 将 KCP 待输出数据写入目标缓冲写入器。
        /// </summary>
        /// <param name="writer">接收编码后数据的缓冲写入器。</param>
        /// <param name="options">可选的调用方上下文。</param>
        /// <returns>输出完成任务。</returns>
        ValueTask OutputAsync(IBufferWriter<byte> writer, object options = null);

        #endregion
    }
}
