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
        bool IsConnected { get; }

        UniTask ConnectAsync(string host, int port, CancellationToken token = default);

        UniTask SendAsync(ArraySegment<byte> data, CancellationToken token = default);

        void Disconnect();

        event Func<ReadOnlyMemory<byte>, UniTask> OnDataReceived;
        event Action OnDisconnected;
    }
}
