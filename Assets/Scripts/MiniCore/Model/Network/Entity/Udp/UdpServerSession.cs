using Cysharp.Threading.Tasks;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MiniCore.Model
{
    public sealed class UdpServerSession : IServerSession
    {
        private readonly Socket socket;
        private bool closed;

        public EndPoint RemoteEndPoint { get; }
        public string SessionId => $"udp:{RemoteEndPoint}";
        public bool IsConnected => !closed;

        public event Action OnDisconnected;

        public UdpServerSession(EndPoint remoteEndPoint, Socket socket)
        {
            RemoteEndPoint = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));
            this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
        }

        public UniTask SendAsync(ArraySegment<byte> data, CancellationToken token = default)
        {
            if (closed)
            {
                throw new InvalidOperationException("UdpServerSession is closed.");
            }

            if (data.Array == null)
            {
                throw new ArgumentException("ArraySegment has no backing array.", nameof(data));
            }

            socket.SendTo(data.Array, data.Offset, data.Count, SocketFlags.None, RemoteEndPoint);
            return UniTask.CompletedTask;
        }

        public void Close()
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
            Close();
        }
    }
}
