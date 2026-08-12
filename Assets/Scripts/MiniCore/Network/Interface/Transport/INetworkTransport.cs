using MiniCore.Threading;
using System;
using System.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// 传输层接口，负责底层传输实现（如 TCP/WebSocket 等）。
    /// </summary>
    public interface INetworkTransport : IDisposable
    {
        /// <summary>
        /// 传输层是否保持连接或可用状态。
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 连接到指定远端地址。
        /// </summary>
        MTask ConnectAsync(string host, int port);

        /// <summary>
        /// 发送一个完整的业务数据包。
        /// </summary>
        MTask SendAsync(ArraySegment<byte> data);

        /// <summary>
        /// 主动断开传输层连接。
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 接收到完整业务数据包时触发。
        /// </summary>
        event Func<ReadOnlyMemory<byte>, MTask> OnDataReceived;
        /// <summary>
        /// 传输层断开时触发。
        /// </summary>
        event Action OnDisconnected;
    }
}
