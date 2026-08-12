using MiniCore.Threading;
using System;
using System.Threading;

namespace MiniCore.Model
{

    /// <summary>
    /// 允许发送器直接写入已包含长度前缀的连续 TCP 帧字节。
    /// 仅供同会话、已保持顺序的出站发送器批量合并普通数据包使用；调用方不得混入未加长度前缀的业务正文。
    /// </summary>
    internal interface IFramedBatchNetworkTransport
    {
        /// <summary>
        /// 将一个或多个已完成长度前缀封装的连续 TCP 帧写入底层传输。
        /// </summary>
        /// <param name="frames">按发送顺序连续排列的完整长度帧字节。</param>
        /// <returns>全部帧字节写入完成或发生异常时完成的任务。</returns>
        MTask SendFramedBatchAsync(ArraySegment<byte> frames);
    }
}
