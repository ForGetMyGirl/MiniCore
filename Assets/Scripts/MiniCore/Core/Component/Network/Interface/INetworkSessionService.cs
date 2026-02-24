using Cysharp.Threading.Tasks;
using MiniCore.Model;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MiniCore.Core
{
    public interface INetworkSessionService
    {
        event Action<NetworkSession> OnServerSessionCreated;
        event Action<string> OnServerSessionClosed;

        UniTask<NetworkSession> CreateTcpSessionAsync(string sessionId, string host, int port, CancellationToken token = default);
        UniTask<NetworkSession> CreateKcpSessionAsync(string sessionId, string host, int port, uint conv, KcpTransportConfig config = null, CancellationToken token = default);
        UniTask<NetworkSession> CreateUdpSessionAsync(string sessionId, string host, int port, CancellationToken token = default);

        UniTask StartKcpServerAsync(string host, int port, KcpServerConfig config = null, CancellationToken token = default);
        UniTask StartTcpServerAsync(string host, int port, CancellationToken token = default);
        UniTask StartUdpServerAsync(string host, int port, UdpServerConfig config = null, CancellationToken token = default);

        void StopKcpServer();
        void StopTcpServer();
        void StopUdpServer();

        NetworkSession GetSession(string sessionId);
        List<NetworkSession> GetServerSessionsSnapshot();
        void DisconnectSession(string sessionId);
        void RemoveSession(string sessionId);
    }
}
