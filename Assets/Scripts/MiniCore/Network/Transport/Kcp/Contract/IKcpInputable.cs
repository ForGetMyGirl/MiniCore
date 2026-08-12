using System;
using System.Buffers;

namespace System.Net.Sockets.Kcp
{
    /// <summary>
    /// KCP 输入数据报接口。
    /// </summary>
    public interface IKcpInputable
    {
        #region Public 公共成员

        /// <summary>
        /// 输入一段连续 KCP 数据报。
        /// </summary>
        /// <param name="span">已接收的数据报字节。</param>
        /// <returns>零表示成功，负数表示协议错误。</returns>
        int Input(ReadOnlySpan<byte> span);

        /// <summary>
        /// 输入一段可分段表示的 KCP 数据报。
        /// </summary>
        /// <param name="span">已接收的数据报字节序列。</param>
        /// <returns>零表示成功，负数表示协议错误。</returns>
        int Input(ReadOnlySequence<byte> span);

        #endregion
    }
}
