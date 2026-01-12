using Cysharp.Threading.Tasks;
using MiniCore.Model;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MiniCore.Core
{
    /// <summary>
    /// Session manager component for creating and disposing network sessions.
    /// </summary>
    public class NetworkSessionComponent : AComponent
    {
        private readonly Dictionary<string, NetworkSession> sessions = new Dictionary<string, NetworkSession>();
        private readonly Dictionary<string, KcpServerTransport> serverTransports = new Dictionary<string, KcpServerTransport>();
        private KcpServer kcpServer;

        public event Action<NetworkSession> OnServerSessionCreated;
        public event Action<string> OnServerSessionClosed;

        public override void Dispose()
        {
            base.Dispose();
            StopKcpServer();
            foreach (var kv in sessions)
            {
                kv.Value.Dispose();
            }
            sessions.Clear();
            serverTransports.Clear();
        }

        public async UniTask<NetworkSession> CreateTcpSessionAsync(string sessionId, string host, int port, CancellationToken token = default)
        {
            if (sessions.ContainsKey(sessionId))
            {
                throw new InvalidOperationException($"Session {sessionId} already exists.");
            }

            var transport = new TcpTransport();
            await transport.ConnectAsync(host, port, token);
            var session = new NetworkSession(sessionId, transport);
            sessions.Add(sessionId, session);
            return session;
        }

        public async UniTask<NetworkSession> CreateKcpSessionAsync(string sessionId, string host, int port, uint conv, KcpTransportConfig config = null, CancellationToken token = default)
        {
            if (sessions.ContainsKey(sessionId))
            {
                throw new InvalidOperationException($"Session {sessionId} already exists.");
            }

            var transport = new KcpTransport(conv, config);
            await transport.ConnectAsync(host, port, token);
            var session = new NetworkSession(sessionId, transport);
            sessions.Add(sessionId, session);
            return session;
        }

        public async UniTask StartKcpServerAsync(string host, int port, KcpServerConfig config = null, CancellationToken token = default)
        {
            if (kcpServer != null)
            {
                throw new InvalidOperationException("KcpServer already started.");
            }

            kcpServer = new KcpServer(config);
            kcpServer.OnSessionCreated += HandleServerSessionCreated;
            kcpServer.OnSessionClosed += HandleServerSessionClosed;
            kcpServer.OnDataReceived += HandleServerDataReceived;
            await kcpServer.StartAsync(host, port, token);
        }

        public void StopKcpServer()
        {
            if (kcpServer == null)
            {
                return;
            }

            kcpServer.OnSessionCreated -= HandleServerSessionCreated;
            kcpServer.OnSessionClosed -= HandleServerSessionClosed;
            kcpServer.OnDataReceived -= HandleServerDataReceived;
            kcpServer.Stop();
            kcpServer = null;

            var serverSessionIds = new List<string>(serverTransports.Keys);
            foreach (var sessionId in serverSessionIds)
            {
                RemoveSession(sessionId);
            }
        }

        public NetworkSession GetSession(string sessionId)
        {
            sessions.TryGetValue(sessionId, out var session);
            return session;
        }

        public void RemoveSession(string sessionId)
        {
            if (sessions.TryGetValue(sessionId, out var session))
            {
                session.Dispose();
                sessions.Remove(sessionId);
            }

            if (serverTransports.ContainsKey(sessionId))
            {
                serverTransports.Remove(sessionId);
            }
        }

        private void HandleServerSessionCreated(KcpServerSession serverSession)
        {
            if (sessions.ContainsKey(serverSession.SessionId))
            {
                return;
            }

            var transport = new KcpServerTransport(serverSession);
            var session = new NetworkSession(serverSession.SessionId, transport);
            sessions.Add(serverSession.SessionId, session);
            serverTransports.Add(serverSession.SessionId, transport);
            OnServerSessionCreated?.Invoke(session);
        }

        private void HandleServerSessionClosed(KcpServerSession serverSession)
        {
            if (serverSession == null)
            {
                return;
            }

            RemoveSession(serverSession.SessionId);
            OnServerSessionClosed?.Invoke(serverSession.SessionId);
        }

        private UniTask HandleServerDataReceived(KcpServerSession serverSession, ReadOnlyMemory<byte> data)
        {
            if (serverSession == null || data.IsEmpty)
            {
                return UniTask.CompletedTask;
            }

            if (serverTransports.TryGetValue(serverSession.SessionId, out var transport))
            {
                return transport.PushReceivedAsync(data);
            }

            return UniTask.CompletedTask;
        }
    }
}
