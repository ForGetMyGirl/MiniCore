using System;

namespace System.Net.Sockets.Kcp
{
    /// <summary>
    /// 包含本地重传状态和负载的 KCP 分片接口。
    /// </summary>
    public interface IKcpSegment : IKcpHeader
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置下次重传时间戳。
        /// </summary>
        uint resendts { get; set; }

        /// <summary>
        /// 获取或设置当前重传超时时长。
        /// </summary>
        uint rto { get; set; }

        /// <summary>
        /// 获取或设置快速确认累计次数。
        /// </summary>
        uint fastack { get; set; }

        /// <summary>
        /// 获取或设置已发送次数。
        /// </summary>
        uint xmit { get; set; }

        /// <summary>
        /// 获取分片负载的可写字节区域。
        /// </summary>
        Span<byte> data { get; }

        /// <summary>
        /// 将分片包头和负载编码到目标缓冲区。
        /// </summary>
        /// <param name="buffer">容纳完整分片的目标缓冲区。</param>
        /// <returns>实际写入的字节数。</returns>
        int Encode(Span<byte> buffer);

        #endregion
    }
}
