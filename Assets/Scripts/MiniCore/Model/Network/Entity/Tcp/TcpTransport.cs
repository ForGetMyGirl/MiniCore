using Cysharp.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;

namespace MiniCore.Model
{
    public class TcpTransport : LengthPrefixedTcpTransportBase
    {
        public override async UniTask ConnectAsync(string host, int port, CancellationToken token = default)
        {
            Disconnect();
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };
            await socket.ConnectAsync(host, port);
            AttachConnectedSocket(socket, token);
        }
    }
}
