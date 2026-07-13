using Cysharp.Threading.Tasks;
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
        UniTask ConnectAsync(string host, int port, CancellationToken token = default);

        /// <summary>
        /// 发送一个完整的业务数据包。
        /// </summary>
        UniTask SendAsync(ArraySegment<byte> data, CancellationToken token = default);

        /// <summary>
        /// 主动断开传输层连接。
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 接收到完整业务数据包时触发。
        /// </summary>
        event Func<ReadOnlyMemory<byte>, UniTask> OnDataReceived;
        /// <summary>
        /// 传输层断开时触发。
        /// </summary>
        event Action OnDisconnected;
    }
}
