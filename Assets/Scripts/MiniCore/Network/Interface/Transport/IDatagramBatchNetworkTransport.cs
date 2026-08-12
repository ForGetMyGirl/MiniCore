using MiniCore.Threading;
using System;
using System.Threading;

namespace MiniCore.Model
{

    /// <summary>
    /// 允许会话发送器写入一个已封装多个逻辑业务包的 UDP 数据报。
    /// 仅供 TrySend 数据队列使用；调用方必须确保该数据报不超过当前路径的安全 MTU 预算。
    /// </summary>
    internal interface IDatagramBatchNetworkTransport
    {
        /// <summary>
        /// 将一个完整 UDP 批量数据报写入底层传输。
        /// </summary>
        /// <param name="datagram">已包含批量协议头和多个业务包的完整 UDP 数据报。</param>
        /// <returns>数据报被底层 Socket 接受或发生异常时完成的任务。</returns>
        MTask SendDatagramBatchAsync(ArraySegment<byte> datagram);
    }
}
