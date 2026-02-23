using Cysharp.Threading.Tasks;
using System;
using System.Net.Sockets;
using System.Threading;

namespace MiniCore.Model
{
    public sealed class TcpServerSession : IServerSession
    {
        private readonly Socket socket;
        private bool closed;

        public string SessionId { get; }
        public bool IsConnected => !closed && socket != null && socket.Connected;
        internal Socket RawSocket => socket;

        public event Action OnDisconnected;

        public TcpServerSession(string sessionId, Socket socket)
        {
            SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
        }

        public UniTask SendAsync(ArraySegment<byte> data, CancellationToken token = default)
        {
            if (closed)
            {
                throw new InvalidOperationException("TcpServerSession is closed.");
            }

            if (data.Array == null)
            {
                throw new ArgumentException("ArraySegment has no backing array.", nameof(data));
            }

            socket.Send(data.Array, data.Offset, data.Count, SocketFlags.None);
            return UniTask.CompletedTask;
        }

        public void Close()
        {
            if (closed)
            {
                return;
            }

            closed = true;
            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
            }

            try
            {
                socket.Close();
            }
            catch
            {
            }

            OnDisconnected?.Invoke();
        }

        public void Dispose()
        {
            Close();
        }
    }
}
