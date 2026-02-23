using Cysharp.Threading.Tasks;
using System;
using System.Net.Sockets;
using System.Threading;

namespace MiniCore.Model
{
    public sealed class TcpServerTransport : LengthPrefixedTcpTransportBase
    {
        private readonly IServerSession session;

        public TcpServerTransport(IServerSession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            if (!(session is TcpServerSession tcpSession))
            {
                throw new ArgumentException("TcpServerTransport requires TcpServerSession.", nameof(session));
            }

            if (tcpSession.RawSocket == null)
            {
                throw new ArgumentNullException(nameof(tcpSession.RawSocket));
            }

            AttachConnectedSocket(tcpSession.RawSocket);
        }

        public override UniTask ConnectAsync(string host, int port, CancellationToken token = default)
        {
            throw new NotSupportedException("TcpServerTransport does not support ConnectAsync.");
        }

        public override void Disconnect()
        {
            base.Disconnect();
            session.Close();
        }
    }
}
