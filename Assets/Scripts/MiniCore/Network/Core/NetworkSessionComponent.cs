using Cysharp.Threading.Tasks;
using MiniCore.Model;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MiniCore.Core
{
    /// <summary>
    /// 网络会话管理组件，创建、保存和释放 TCP、KCP、UDP 的客户端与服务端逻辑会话。
    /// </summary>
    public class NetworkSessionComponent : AComponent, INetworkSessionService
    {
        #region Private 私有成员

        private readonly object sessionLock = new object(); // 会话集合的同步锁。
        private readonly Dictionary<string, ISession> sessions = new Dictionary<string, ISession>(); // 全部逻辑会话。
        private readonly HashSet<string> serverSessionIds = new HashSet<string>(); // 服务端创建的会话标识。
        private readonly HashSet<string> closedServerSessionIds = new HashSet<string>(); // 已通知关闭的服务端会话标识。
        private readonly HashSet<string> tcpServerSessionIds = new HashSet<string>(); // TCP 服务端会话标识。
        private readonly Dictionary<string, KcpServerTransport> kcpServerTransports = new Dictionary<string, KcpServerTransport>(); // KCP 服务端传输层。
        private readonly Dictionary<string, UdpServerTransport> udpServerTransports = new Dictionary<string, UdpServerTransport>(); // UDP 服务端传输层。

        private KcpServer kcpServer; // KCP 服务端监听器。
        private TcpServer tcpServer; // TCP 服务端监听器。
        private UdpServer udpServer; // UDP 服务端监听器。
        private SynchronizationContext unityContext; // Unity 主线程同步上下文。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 服务端接受并封装新会话后触发。
        /// </summary>
        public event Action<NetworkSession> OnServerSessionCreated;
        /// <summary>
        /// 服务端会话关闭且完成清理后触发。
        /// </summary>
        public event Action<string> OnServerSessionClosed;

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 缓存 Unity 主线程同步上下文。
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            unityContext = SynchronizationContext.Current;
        }

        /// <summary>
        /// 停止全部服务端并释放已保存的会话。
        /// </summary>
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

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 连接远端并创建 TCP 客户端逻辑会话。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <param name="token">执行该方法所需的 token 参数。</param>
        /// <returns>执行处理后的结果。</returns>
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

        /// <summary>
        /// 连接远端并创建 KCP 客户端逻辑会话。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <param name="conv">执行该方法所需的 conv 参数。</param>
        /// <param name="config">执行该方法所需的 config 参数。</param>
        /// <param name="token">执行该方法所需的 token 参数。</param>
        /// <returns>执行处理后的结果。</returns>
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

        /// <summary>
        /// 连接远端并创建 UDP 客户端逻辑会话。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <param name="token">执行该方法所需的 token 参数。</param>
        /// <returns>执行处理后的结果。</returns>
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

        /// <summary>
        /// 启动 KCP 服务端监听并订阅会话事件。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <param name="config">执行该方法所需的 config 参数。</param>
        /// <param name="token">执行该方法所需的 token 参数。</param>
        /// <returns>执行处理后的结果。</returns>
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

        /// <summary>
        /// 启动 TCP 服务端监听并订阅接入事件。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <param name="token">执行该方法所需的 token 参数。</param>
        /// <returns>执行处理后的结果。</returns>
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

        /// <summary>
        /// 启动 UDP 服务端监听并订阅会话事件。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <param name="config">执行该方法所需的 config 参数。</param>
        /// <param name="token">执行该方法所需的 token 参数。</param>
        /// <returns>执行处理后的结果。</returns>
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

        /// <summary>
        /// 停止 KCP 服务端并清理其逻辑会话。
        /// </summary>
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

        /// <summary>
        /// 停止 TCP 服务端并清理其逻辑会话。
        /// </summary>
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

        /// <summary>
        /// 停止 UDP 服务端并清理其逻辑会话。
        /// </summary>
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

        /// <summary>
        /// 按会话标识获取逻辑会话；未找到时返回空。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public NetworkSession GetSession(string sessionId)
        {
            lock (sessionLock)
            {
                sessions.TryGetValue(sessionId, out var session);
                return session as NetworkSession;
            }
        }

        /// <summary>
        /// 获取当前服务端逻辑会话的独立快照。
        /// </summary>
        /// <returns>执行处理后的结果。</returns>
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

        /// <summary>
        /// 断开指定会话，并在需要时通知服务端会话关闭事件。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
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

        /// <summary>
        /// 从管理器中移除并释放指定会话。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        public void RemoveSession(string sessionId)
        {
            bool removed = RemoveSessionInternal(sessionId, out var session);
            if (removed)
            {
                session.Dispose();
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 执行 HandleTcpClientAccepted 相关处理。
        /// </summary>
        /// <param name="serverSession">执行该方法所需的 serverSession 参数。</param>
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

        /// <summary>
        /// 执行 HandleKcpServerSessionCreated 相关处理。
        /// </summary>
        /// <param name="serverSession">执行该方法所需的 serverSession 参数。</param>
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

        /// <summary>
        /// 执行 HandleKcpServerSessionClosed 相关处理。
        /// </summary>
        /// <param name="serverSession">执行该方法所需的 serverSession 参数。</param>
        private void HandleKcpServerSessionClosed(IServerSession serverSession)
        {
            if (serverSession == null)
            {
                return;
            }

            RemoveServerSessionAndDispatch(serverSession);
        }

        /// <summary>
        /// 执行 HandleKcpServerDataReceived 相关处理。
        /// </summary>
        /// <param name="serverSession">执行该方法所需的 serverSession 参数。</param>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <returns>执行处理后的结果。</returns>
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

        /// <summary>
        /// 执行 HandleUdpServerSessionCreated 相关处理。
        /// </summary>
        /// <param name="serverSession">执行该方法所需的 serverSession 参数。</param>
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

        /// <summary>
        /// 执行 HandleUdpServerSessionClosed 相关处理。
        /// </summary>
        /// <param name="serverSession">执行该方法所需的 serverSession 参数。</param>
        private void HandleUdpServerSessionClosed(IServerSession serverSession)
        {
            if (serverSession == null)
            {
                return;
            }

            RemoveServerSessionAndDispatch(serverSession);
        }

        /// <summary>
        /// 执行 HandleUdpServerDataReceived 相关处理。
        /// </summary>
        /// <param name="serverSession">执行该方法所需的 serverSession 参数。</param>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <returns>执行处理后的结果。</returns>
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

        /// <summary>
        /// 执行 RemoveSessionInternal 相关处理。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="session">执行该方法所需的 session 参数。</param>
        /// <returns>执行处理后的结果。</returns>
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

        /// <summary>
        /// 执行 CreateClientSessionAsync 相关处理。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="connectFactory">执行该方法所需的 connectFactory 参数。</param>
        /// <param name="token">执行该方法所需的 token 参数。</param>
        /// <returns>执行处理后的结果。</returns>
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

        /// <summary>
        /// 执行 AddServerSessionInternal 相关处理。
        /// </summary>
        /// <param name="serverSession">执行该方法所需的 serverSession 参数。</param>
        /// <param name="session">执行该方法所需的 session 参数。</param>
        /// <param name="onAddedUnderLock">执行该方法所需的 onAddedUnderLock 参数。</param>
        /// <returns>执行处理后的结果。</returns>
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

        /// <summary>
        /// 执行 RemoveServerSessionAndDispatch 相关处理。
        /// </summary>
        /// <param name="serverSession">执行该方法所需的 serverSession 参数。</param>
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

        /// <summary>
        /// 执行 DispatchServerSessionClosedOnce 相关处理。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
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

        /// <summary>
        /// 执行 DispatchToMainThread 相关处理。
        /// </summary>
        /// <param name="action">执行该方法所需的 action 参数。</param>
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

        #endregion
    }
}
