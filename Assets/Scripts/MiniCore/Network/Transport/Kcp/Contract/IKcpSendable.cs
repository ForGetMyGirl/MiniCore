using System;
using System.Buffers;

namespace System.Net.Sockets.Kcp
{
    /// <summary>
    /// KCP 发送数据接口。
    /// </summary>
    public interface IKcpSendable
    {
        #region Public 公共成员

        /// <summary>
        /// 提交一段连续业务数据供 KCP 分片发送。
        /// </summary>
        /// <param name="span">待发送的连续字节。</param>
        /// <param name="options">可选的调用方上下文。</param>
        /// <returns>零表示成功，负数表示提交失败。</returns>
        int Send(ReadOnlySpan<byte> span, object options = null);

        /// <summary>
        /// 提交一段可分段表示的业务数据供 KCP 分片发送。
        /// </summary>
        /// <param name="span">待发送的字节序列。</param>
        /// <param name="options">可选的调用方上下文。</param>
        /// <returns>零表示成功，负数表示提交失败。</returns>
        int Send(ReadOnlySequence<byte> span, object options = null);

        #endregion
    }
}
