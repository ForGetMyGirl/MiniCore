using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace MiniCore.Model
{
    public sealed class UdpServerTransport : INetworkTransport
    {
        private readonly IServerSession session;
        private bool closed;

        public UdpServerTransport(IServerSession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            session.OnDisconnected += HandleSessionDisconnected;
        }

        public bool IsConnected => !closed;

        public event Func<ReadOnlyMemory<byte>, UniTask> OnDataReceived;
        public event Action OnDisconnected;

        public UniTask ConnectAsync(string host, int port, CancellationToken token = default)
        {
            throw new InvalidOperationException("Server-side transport does not support ConnectAsync.");
        }

        public UniTask SendAsync(ArraySegment<byte> data, CancellationToken token = default)
        {
            return session.SendAsync(data, token);
        }

        public void Disconnect()
        {
            if (closed)
            {
                return;
            }

            closed = true;
            OnDisconnected?.Invoke();
        }

        public void Dispose()
        {
            Disconnect();
        }

        public UniTask PushReceivedAsync(ReadOnlyMemory<byte> data)
        {
            return TransportEventDispatcher.DispatchAsync(OnDataReceived, data);
        }

        private void HandleSessionDisconnected()
        {
            if (closed)
            {
                return;
            }

            closed = true;
            OnDisconnected?.Invoke();
        }
    }
}
