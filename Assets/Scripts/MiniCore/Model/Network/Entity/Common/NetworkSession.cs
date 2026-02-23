using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// Represents a logical client session wrapping a transport.
    /// </summary>
    public class NetworkSession : IClientSession
    {
        private int disposed;

        public string SessionId { get; }
        public INetworkTransport Transport { get; }
        public bool IsConnected => Transport.IsConnected;

        public NetworkSession(string sessionId, INetworkTransport transport)
        {
            SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            Transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public UniTask SendAsync(ArraySegment<byte> data, CancellationToken token = default)
        {
            return Transport.SendAsync(data, token);
        }

        public void Close()
        {
            Transport.Disconnect();
        }

        public event Action OnDisconnected
        {
            add => Transport.OnDisconnected += value;
            remove => Transport.OnDisconnected -= value;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
            {
                return;
            }

            Transport.Dispose();
        }
    }
}
