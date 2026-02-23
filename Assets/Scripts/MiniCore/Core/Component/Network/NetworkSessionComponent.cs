using Cysharp.Threading.Tasks;
using MiniCore.Model;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MiniCore.Core
{
    /// <summary>
    /// Session manager component for creating and disposing network sessions.
    /// Supports client sessions and server sessions for TCP/KCP/UDP.
    /// </summary>
    public class NetworkSessionComponent : AComponent
    {
        private readonly object sessionLock = new object();
        private readonly Dictionary<string, ISession> sessions = new Dictionary<string, ISession>();
        private readonly HashSet<string> serverSessionIds = new HashSet<string>();
        private readonly HashSet<string> closedServerSessionIds = new HashSet<string>();
        private readonly HashSet<string> tcpServerSessionIds = new HashSet<string>();
        private readonly Dictionary<string, KcpServerTransport> kcpServerTransports = new Dictionary<string, KcpServerTransport>();
        private readonly Dictionary<string, UdpServerTransport> udpServerTransports = new Dictionary<string, UdpServerTransport>();

        private KcpServer kcpServer;
        private TcpServer tcpServer;
        private UdpServer udpServer;
        private SynchronizationContext unityContext;

        public event Action<NetworkSession> OnServerSessionCreated;
        public event Action<string> OnServerSessionClosed;

        public override void Awake()
        {
            base.Awake();
            unityContext = SynchronizationContext.Current;
        }

        public override void Dispose()
        {
            base.Dispose();
            StopKcpServer();
            StopTcpServer();
            StopUdpServer();

            List<ISession> toDispose;
            lock (sessionLock)
            {
                toDispose = new List<ISession>(sessions.Values);
                sessions.Clear();
                serverSessionIds.Clear();
                closedServerSessionIds.Clear();
                tcpServerSessionIds.Clear();
                kcpServerTransports.Clear();
                udpServerTransports.Clear();
            }

            foreach (var session in toDispose)
            {
                session.Dispose();
            }
        }

        public async UniTask<NetworkSession> CreateTcpSessionAsync(string sessionId, string host, int port, CancellationToken token = default)
        {
            return await CreateClientSessionAsync(
                sessionId,
                async ct =>
                {
                    var transport = new TcpTransport();
                    await transport.ConnectAsync(host, port, ct);
                    return transport;
                },
                token);
        }

        public async UniTask<NetworkSession> CreateKcpSessionAsync(string sessionId, string host, int port, uint conv, KcpTransportConfig config = null, CancellationToken token = default)
        {
            return await CreateClientSessionAsync(
                sessionId,
                async ct =>
                {
                    var transport = new KcpTransport(conv, config);
                    await transport.ConnectAsync(host, port, ct);
                    return transport;
                },
                token);
        }

        public async UniTask<NetworkSession> CreateUdpSessionAsync(string sessionId, string host, int port, CancellationToken token = default)
        {
            return await CreateClientSessionAsync(
                sessionId,
                async ct =>
                {
                    var transport = new UdpTransport();
                    await transport.ConnectAsync(host, port, ct);
                    return transport;
                },
                token);
        }

        public async UniTask StartKcpServerAsync(string host, int port, KcpServerConfig config = null, CancellationToken token = default)
        {
            if (kcpServer != null)
            {
                throw new InvalidOperationException("KcpServer already started.");
            }

            kcpServer = new KcpServer(config);
            kcpServer.OnSessionCreated += HandleKcpServerSessionCreated;
            kcpServer.OnSessionClosed += HandleKcpServerSessionClosed;
            kcpServer.OnDataReceived += HandleKcpServerDataReceived;
            await kcpServer.StartAsync(host, port, token);
        }

        public async UniTask StartTcpServerAsync(string host, int port, CancellationToken token = default)
        {
            if (tcpServer != null)
            {
                throw new InvalidOperationException("TcpServer already started.");
            }

            tcpServer = new TcpServer();
            tcpServer.OnClientAccepted += HandleTcpClientAccepted;
            await tcpServer.StartAsync(host, port, token);
        }

        public async UniTask StartUdpServerAsync(string host, int port, UdpServerConfig config = null, CancellationToken token = default)
        {
            if (udpServer != null)
            {
                throw new InvalidOperationException("UdpServer already started.");
            }

            udpServer = new UdpServer(config);
            udpServer.OnSessionCreated += HandleUdpServerSessionCreated;
            udpServer.OnSessionClosed += HandleUdpServerSessionClosed;
            udpServer.OnDataReceived += HandleUdpServerDataReceived;
            await udpServer.StartAsync(host, port, token);
        }

        public void StopKcpServer()
        {
            List<string> kcpSessionIds;

            if (kcpServer == null)
            {
                return;
            }

            kcpServer.OnSessionCreated -= HandleKcpServerSessionCreated;
            kcpServer.OnSessionClosed -= HandleKcpServerSessionClosed;
            kcpServer.OnDataReceived -= HandleKcpServerDataReceived;
            kcpServer.Stop();
            kcpServer = null;

            lock (sessionLock)
            {
                kcpSessionIds = new List<string>(kcpServerTransports.Keys);
            }

            foreach (var sessionId in kcpSessionIds)
            {
                bool removed = RemoveSessionInternal(sessionId, out var session);
                session?.Dispose();
                if (removed)
                {
                    DispatchServerSessionClosedOnce(sessionId);
                }
            }
        }

        public void StopTcpServer()
        {
            List<string> tcpSessionIds;

            if (tcpServer == null)
            {
                return;
            }

            tcpServer.OnClientAccepted -= HandleTcpClientAccepted;
            tcpServer.Stop();
            tcpServer = null;

            lock (sessionLock)
            {
                tcpSessionIds = new List<string>(tcpServerSessionIds);
            }

            foreach (var sessionId in tcpSessionIds)
            {
                bool removed = RemoveSessionInternal(sessionId, out var session);
                session?.Dispose();
                if (removed)
                {
                    DispatchServerSessionClosedOnce(sessionId);
                }
            }
        }

        public void StopUdpServer()
        {
            List<string> udpSessionIds;

            if (udpServer == null)
            {
                return;
            }

            udpServer.OnSessionCreated -= HandleUdpServerSessionCreated;
            udpServer.OnSessionClosed -= HandleUdpServerSessionClosed;
            udpServer.OnDataReceived -= HandleUdpServerDataReceived;
            udpServer.Stop();
            udpServer = null;

            lock (sessionLock)
            {
                udpSessionIds = new List<string>(udpServerTransports.Keys);
            }

            foreach (var sessionId in udpSessionIds)
            {
                bool removed = RemoveSessionInternal(sessionId, out var session);
                session?.Dispose();
                if (removed)
                {
                    DispatchServerSessionClosedOnce(sessionId);
                }
            }
        }

        public NetworkSession GetSession(string sessionId)
        {
            lock (sessionLock)
            {
                sessions.TryGetValue(sessionId, out var session);
                return session as NetworkSession;
            }
        }

        public List<NetworkSession> GetServerSessionsSnapshot()
        {
            var result = new List<NetworkSession>();
            lock (sessionLock)
            {
                foreach (var sessionId in serverSessionIds)
                {
                    if (sessions.TryGetValue(sessionId, out var session))
                    {
                        if (session is NetworkSession networkSession)
                        {
                            result.Add(networkSession);
                        }
                    }
                }
            }
            return result;
        }

        public void DisconnectSession(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return;
            }

            bool isServerSession;
            bool isKcpServerSession;
            bool isUdpServerSession;
            lock (sessionLock)
            {
                isServerSession = serverSessionIds.Contains(sessionId);
                isKcpServerSession = kcpServerTransports.ContainsKey(sessionId);
                isUdpServerSession = udpServerTransports.ContainsKey(sessionId);
            }

            if (isServerSession && isKcpServerSession && kcpServer != null)
            {
                kcpServer.CloseSession(sessionId);
                return;
            }

            if (isServerSession && isUdpServerSession && udpServer != null)
            {
                udpServer.CloseSession(sessionId);
                return;
            }

            bool removed = RemoveSessionInternal(sessionId, out var session);
            session?.Dispose();

            if (removed && isServerSession)
            {
                DispatchServerSessionClosedOnce(sessionId);
            }
        }

        public void RemoveSession(string sessionId)
        {
            bool removed = RemoveSessionInternal(sessionId, out var session);
            if (removed)
            {
                session.Dispose();
            }
        }

        private void HandleTcpClientAccepted(IServerSession serverSession)
        {
            if (serverSession == null)
            {
                return;
            }

            string sessionId = serverSession.SessionId;
            var transport = new TcpServerTransport(serverSession);
            var session = new NetworkSession(sessionId, transport);

            if (!AddServerSessionInternal(serverSession, session, () => tcpServerSessionIds.Add(sessionId)))
            {
                session.Dispose();
                return;
            }

            transport.OnDisconnected += () =>
            {
                bool removed = RemoveSessionInternal(sessionId, out var removedSession);
                removedSession?.Dispose();
                if (removed)
                {
                    DispatchServerSessionClosedOnce(sessionId);
                }
            };

            DispatchToMainThread(() => OnServerSessionCreated?.Invoke(session));
        }

        private void HandleKcpServerSessionCreated(IServerSession serverSession)
        {
            if (serverSession == null)
            {
                return;
            }

            var transport = new KcpServerTransport(serverSession);
            var session = new NetworkSession(serverSession.SessionId, transport);
            if (!AddServerSessionInternal(serverSession, session, () => kcpServerTransports.Add(serverSession.SessionId, transport)))
            {
                session.Dispose();
                return;
            }

            DispatchToMainThread(() => OnServerSessionCreated?.Invoke(session));
        }

        private void HandleKcpServerSessionClosed(IServerSession serverSession)
        {
            if (serverSession == null)
            {
                return;
            }

            RemoveServerSessionAndDispatch(serverSession);
        }

        private UniTask HandleKcpServerDataReceived(IServerSession serverSession, ReadOnlyMemory<byte> data)
        {
            if (serverSession == null || data.IsEmpty)
            {
                return UniTask.CompletedTask;
            }

            KcpServerTransport transport;
            lock (sessionLock)
            {
                kcpServerTransports.TryGetValue(serverSession.SessionId, out transport);
            }

            if (transport != null)
            {
                return transport.PushReceivedAsync(data);
            }

            return UniTask.CompletedTask;
        }

        private void HandleUdpServerSessionCreated(IServerSession serverSession)
        {
            if (serverSession == null)
            {
                return;
            }

            var transport = new UdpServerTransport(serverSession);
            var session = new NetworkSession(serverSession.SessionId, transport);
            if (!AddServerSessionInternal(serverSession, session, () => udpServerTransports.Add(serverSession.SessionId, transport)))
            {
                session.Dispose();
                return;
            }

            DispatchToMainThread(() => OnServerSessionCreated?.Invoke(session));
        }

        private void HandleUdpServerSessionClosed(IServerSession serverSession)
        {
            if (serverSession == null)
            {
                return;
            }

            RemoveServerSessionAndDispatch(serverSession);
        }

        private UniTask HandleUdpServerDataReceived(IServerSession serverSession, ReadOnlyMemory<byte> data)
        {
            if (serverSession == null || data.IsEmpty)
            {
                return UniTask.CompletedTask;
            }

            UdpServerTransport transport;
            lock (sessionLock)
            {
                udpServerTransports.TryGetValue(serverSession.SessionId, out transport);
            }

            if (transport != null)
            {
                return transport.PushReceivedAsync(data);
            }

            return UniTask.CompletedTask;
        }

        private bool RemoveSessionInternal(string sessionId, out ISession session)
        {
            session = null;
            lock (sessionLock)
            {
                if (!sessions.TryGetValue(sessionId, out session))
                {
                    return false;
                }

                sessions.Remove(sessionId);
                serverSessionIds.Remove(sessionId);
                tcpServerSessionIds.Remove(sessionId);
                kcpServerTransports.Remove(sessionId);
                udpServerTransports.Remove(sessionId);
                return true;
            }
        }

        private async UniTask<NetworkSession> CreateClientSessionAsync(
            string sessionId,
            Func<CancellationToken, UniTask<INetworkTransport>> connectFactory,
            CancellationToken token = default)
        {
            lock (sessionLock)
            {
                if (sessions.ContainsKey(sessionId))
                {
                    throw new InvalidOperationException($"Session {sessionId} already exists.");
                }
            }

            var transport = await connectFactory(token);
            var session = new NetworkSession(sessionId, transport);
            lock (sessionLock)
            {
                if (sessions.ContainsKey(sessionId))
                {
                    session.Dispose();
                    throw new InvalidOperationException($"Session {sessionId} already exists.");
                }

                sessions.Add(sessionId, session);
            }

            return session;
        }

        private bool AddServerSessionInternal(IServerSession serverSession, ISession session, Action onAddedUnderLock = null)
        {
            if (serverSession == null || session == null)
            {
                return false;
            }

            lock (sessionLock)
            {
                if (sessions.ContainsKey(serverSession.SessionId))
                {
                    return false;
                }

                sessions.Add(serverSession.SessionId, session);
                serverSessionIds.Add(serverSession.SessionId);
                closedServerSessionIds.Remove(serverSession.SessionId);
                onAddedUnderLock?.Invoke();
                return true;
            }
        }

        private void RemoveServerSessionAndDispatch(IServerSession serverSession)
        {
            if (serverSession == null)
            {
                return;
            }

            bool removed = RemoveSessionInternal(serverSession.SessionId, out var session);
            session?.Dispose();
            if (removed)
            {
                DispatchServerSessionClosedOnce(serverSession.SessionId);
            }
        }

        private void DispatchServerSessionClosedOnce(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return;
            }

            lock (sessionLock)
            {
                if (!closedServerSessionIds.Add(sessionId))
                {
                    return;
                }
            }

            DispatchToMainThread(() => OnServerSessionClosed?.Invoke(sessionId));
        }

        private void DispatchToMainThread(Action action)
        {
            if (action == null)
            {
                return;
            }

            if (unityContext != null)
            {
                unityContext.Post(_ => action(), null);
                return;
            }

            action();
        }
    }
}

