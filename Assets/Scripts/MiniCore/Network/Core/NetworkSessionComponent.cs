using MiniCore.Threading;
using MiniCore.Model;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MiniCore.Core
{
    /// <summary>
    /// 网络会话管理组件，创建、保存和释放 TCP、KCP、UDP 的客户端与服务端逻辑会话。
    /// </summary>
    public class NetworkSessionComponent : AComponent, INetworkSessionService, INetworkExecutorConfigurable
    {
        #region Private 私有成员

        private readonly object sessionLock = new object(); // 会话集合的同步锁。
        private readonly Dictionary<string, ISession> sessions = new Dictionary<string, ISession>(); // 全部逻辑会话。
        private readonly HashSet<string> serverSessionIds = new HashSet<string>(); // 服务端创建的会话标识。
        private readonly HashSet<string> closedServerSessionIds = new HashSet<string>(); // 已通知关闭的服务端会话标识。
        private readonly HashSet<string> tcpServerSessionIds = new HashSet<string>(); // TCP 服务端会话标识。
        private readonly Dictionary<string, KcpServerTransport> kcpServerTransports = new Dictionary<string, KcpServerTransport>(); // KCP 服务端传输层。
        private readonly Dictionary<string, UdpServerTransport> udpServerTransports = new Dictionary<string, UdpServerTransport>(); // UDP 服务端传输层。
        private readonly Dictionary<string, WebSocketServerTransport> webSocketServerTransports = new Dictionary<string, WebSocketServerTransport>(); // WebSocket 服务端传输层。

        private KcpServer kcpServer; // KCP 服务端监听器。
        private TcpServer tcpServer; // TCP 服务端监听器。
        private UdpServer udpServer; // UDP 服务端监听器。
        private NativeWebSocketServer webSocketServer; // 原生 WebSocket 服务端监听器。
        private SynchronizationContext unityContext; // Unity 主线程同步上下文。
        private IMTaskExecutor networkExecutor; // 网络中枢注入的异步执行器；独立使用时按运行环境选择默认执行器。

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
            networkExecutor = NetworkExecutorResolver.Resolve(null);
        }

        /// <summary>
        /// 在任务域取消前停止全部服务端并释放会话，从而同步解除 Socket I/O 等待。
        /// </summary>
        protected override void OnDisposing()
        {
            StopKcpServer();
            StopTcpServer();
            StopUdpServer();
            StopWebSocketServer();

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
                webSocketServerTransports.Clear();
            }

            foreach (var session in toDispose)
            {
                session.Dispose();
            }
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 配置后续创建的监听器、传输和发送队列所使用的网络执行器。
        /// </summary>
        /// <param name="executor">由网络中枢持有并负责释放的执行器。</param>
        void INetworkExecutorConfigurable.SetNetworkExecutor(IMTaskExecutor executor)
        {
            networkExecutor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        /// <summary>
        /// 连接远端并创建 TCP 客户端逻辑会话。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public async MTask<NetworkSession> CreateTcpSessionAsync(string sessionId, string host, int port)
        {
            EnsureConnectSupported(NetworkTransportKind.Tcp);
            return await CreateClientSessionAsync(
                sessionId,
                async () =>
                {
                    var transport = new TcpTransport(networkExecutor);
                    try
                    {
                        await transport.ConnectAsync(host, port);
                        return transport;
                    }
                    catch
                    {
                        transport.Dispose();
                        throw;
                    }
                });
        }

        /// <summary>
        /// 连接远端并创建 KCP 客户端逻辑会话。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <param name="conv">执行该方法所需的 conv 参数。</param>
        /// <param name="config">执行该方法所需的 config 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public async MTask<NetworkSession> CreateKcpSessionAsync(string sessionId, string host, int port, uint conv, KcpTransportConfig config = null)
        {
            EnsureConnectSupported(NetworkTransportKind.Kcp);
            return await CreateClientSessionAsync(
                sessionId,
                async () =>
                {
                    var transport = new KcpTransport(conv, config, networkExecutor);
                    try
                    {
                        await transport.ConnectAsync(host, port);
                        return transport;
                    }
                    catch
                    {
                        transport.Dispose();
                        throw;
                    }
                });
        }

        /// <summary>
        /// 连接远端并创建 UDP 客户端逻辑会话。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public async MTask<NetworkSession> CreateUdpSessionAsync(string sessionId, string host, int port)
        {
            EnsureConnectSupported(NetworkTransportKind.Udp);
            return await CreateClientSessionAsync(
                sessionId,
                async () =>
                {
                    var transport = new UdpTransport(networkExecutor);
                    try
                    {
                        await transport.ConnectAsync(host, port);
                        return transport;
                    }
                    catch
                    {
                        transport.Dispose();
                        throw;
                    }
                });
        }

        /// <summary>
        /// 连接完整 WS/WSS 地址并创建客户端逻辑会话。
        /// </summary>
        /// <param name="sessionId">逻辑会话标识。</param>
        /// <param name="url">包含路径的完整 WS/WSS 地址。</param>
        /// <returns>已经完成握手并加入会话表的逻辑会话。</returns>
        public async MTask<NetworkSession> CreateWebSocketSessionAsync(string sessionId, string url)
        {
            EnsureConnectSupported(NetworkTransportKind.WebSocket);
            return await CreateClientSessionAsync(
                sessionId,
                async () =>
                {
                    var transport = new WebSocketTransport();
                    try
                    {
                        await transport.ConnectAsync(url);
                        return transport;
                    }
                    catch
                    {
                        transport.Dispose();
                        throw;
                    }
                });
        }

        /// <summary>
        /// 启动 KCP 服务端监听并订阅会话事件。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <param name="config">执行该方法所需的 config 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public async MTask StartKcpServerAsync(string host, int port, KcpServerConfig config = null)
        {
            EnsureListenSupported(NetworkTransportKind.Kcp);
            if (kcpServer != null)
            {
                throw new InvalidOperationException("KcpServer already started.");
            }

            kcpServer = new KcpServer(config, networkExecutor);
            kcpServer.OnSessionCreated += HandleKcpServerSessionCreated;
            kcpServer.OnSessionClosed += HandleKcpServerSessionClosed;
            kcpServer.OnDataReceived += HandleKcpServerDataReceived;
            await kcpServer.StartAsync(host, port);
        }

        /// <summary>
        /// 启动 TCP 服务端监听并订阅接入事件。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public async MTask StartTcpServerAsync(string host, int port)
        {
            EnsureListenSupported(NetworkTransportKind.Tcp);
            if (tcpServer != null)
            {
                throw new InvalidOperationException("TcpServer already started.");
            }

            tcpServer = new TcpServer(networkExecutor);
            tcpServer.OnClientAccepted += HandleTcpClientAccepted;
            await tcpServer.StartAsync(host, port);
        }

        /// <summary>
        /// 启动 UDP 服务端监听并订阅会话事件。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <param name="config">执行该方法所需的 config 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public async MTask StartUdpServerAsync(string host, int port, UdpServerConfig config = null)
        {
            EnsureListenSupported(NetworkTransportKind.Udp);
            if (udpServer != null)
            {
                throw new InvalidOperationException("UdpServer already started.");
            }

            udpServer = new UdpServer(config, networkExecutor);
            udpServer.OnSessionCreated += HandleUdpServerSessionCreated;
            udpServer.OnSessionClosed += HandleUdpServerSessionClosed;
            udpServer.OnDataReceived += HandleUdpServerDataReceived;
            await udpServer.StartAsync(host, port);
        }

        /// <summary>
        /// 启动原生 WS/WSS 监听并订阅会话事件。
        /// </summary>
        /// <param name="host">监听地址。</param>
        /// <param name="port">监听端口。</param>
        /// <param name="config">路径、消息大小、握手和 TLS 配置。</param>
        /// <returns>监听成功后完成的任务。</returns>
        public async MTask StartWebSocketServerAsync(string host, int port, WebSocketServerConfig config = null)
        {
            EnsureListenSupported(NetworkTransportKind.WebSocket);
            if (webSocketServer != null)
            {
                throw new InvalidOperationException("WebSocketServer already started.");
            }

            webSocketServer = new NativeWebSocketServer();
            webSocketServer.OnSessionCreated += HandleWebSocketServerSessionCreated;
            webSocketServer.OnSessionClosed += HandleWebSocketServerSessionClosed;
            webSocketServer.OnDataReceived += HandleWebSocketServerDataReceived;
            await webSocketServer.StartAsync(host, port, config);
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
        /// 停止 WebSocket 服务端并清理其逻辑会话。
        /// </summary>
        public void StopWebSocketServer()
        {
            if (webSocketServer == null)
            {
                return;
            }

            webSocketServer.OnSessionCreated -= HandleWebSocketServerSessionCreated;
            webSocketServer.OnSessionClosed -= HandleWebSocketServerSessionClosed;
            webSocketServer.OnDataReceived -= HandleWebSocketServerDataReceived;
            webSocketServer.Stop();
            webSocketServer = null;

            List<string> sessionIds;
            lock (sessionLock)
            {
                sessionIds = new List<string>(webSocketServerTransports.Keys);
            }

            foreach (string sessionId in sessionIds)
            {
                bool removed = RemoveSessionInternal(sessionId, out ISession session);
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
        /// 在创建连接前校验当前运行环境具备目标传输能力。
        /// </summary>
        /// <param name="kind">目标传输类型。</param>
        private static void EnsureConnectSupported(NetworkTransportKind kind)
        {
            if (!NetworkCapabilities.SupportsConnect(kind))
            {
                throw new PlatformNotSupportedException($"当前运行环境不支持 {kind} 主动连接。");
            }
        }

        /// <summary>
        /// 在创建监听器前校验当前运行环境具备目标传输能力。
        /// </summary>
        /// <param name="kind">目标传输类型。</param>
        private static void EnsureListenSupported(NetworkTransportKind kind)
        {
            if (!NetworkCapabilities.SupportsListen(kind))
            {
                throw new PlatformNotSupportedException($"当前运行环境不支持 {kind} 监听器。");
            }
        }

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
            var transport = new TcpServerTransport(serverSession, networkExecutor);
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
        private MTask HandleKcpServerDataReceived(IServerSession serverSession, ReadOnlyMemory<byte> data)
        {
            if (serverSession == null || data.IsEmpty)
            {
                return MTask.CompletedTask;
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

            return MTask.CompletedTask;
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
        private MTask HandleUdpServerDataReceived(IServerSession serverSession, ReadOnlyMemory<byte> data)
        {
            if (serverSession == null || data.IsEmpty)
            {
                return MTask.CompletedTask;
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

            return MTask.CompletedTask;
        }

        /// <summary>
        /// 为新 WebSocket 服务端会话创建统一传输和逻辑会话。
        /// </summary>
        /// <param name="serverSession">已经完成握手的服务端会话。</param>
        private void HandleWebSocketServerSessionCreated(IServerSession serverSession)
        {
            if (serverSession == null)
            {
                return;
            }

            var transport = new WebSocketServerTransport(serverSession);
            var session = new NetworkSession(serverSession.SessionId, transport);
            if (!AddServerSessionInternal(
                    serverSession,
                    session,
                    () => webSocketServerTransports.Add(serverSession.SessionId, transport)))
            {
                session.Dispose();
                return;
            }

            DispatchToMainThread(() => OnServerSessionCreated?.Invoke(session));
        }

        /// <summary>
        /// 清理已经关闭的 WebSocket 服务端逻辑会话。
        /// </summary>
        /// <param name="serverSession">已经关闭的服务端会话。</param>
        private void HandleWebSocketServerSessionClosed(IServerSession serverSession)
        {
            if (serverSession != null)
            {
                RemoveServerSessionAndDispatch(serverSession);
            }
        }

        /// <summary>
        /// 将监听器拆出的 WebSocket 业务包推送给统一传输。
        /// </summary>
        /// <param name="serverSession">消息所属服务端会话。</param>
        /// <param name="data">完整业务包正文。</param>
        /// <returns>传输回调完成任务。</returns>
        private MTask HandleWebSocketServerDataReceived(IServerSession serverSession, ReadOnlyMemory<byte> data)
        {
            if (serverSession == null || data.IsEmpty)
            {
                return MTask.CompletedTask;
            }

            WebSocketServerTransport transport;
            lock (sessionLock)
            {
                webSocketServerTransports.TryGetValue(serverSession.SessionId, out transport);
            }

            return transport != null ? transport.PushReceivedAsync(data) : MTask.CompletedTask;
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
                webSocketServerTransports.Remove(sessionId);
                return true;
            }
        }

        /// <summary>
        /// 执行 CreateClientSessionAsync 相关处理。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="connectFactory">执行该方法所需的 connectFactory 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private async MTask<NetworkSession> CreateClientSessionAsync(
            string sessionId,
            Func<MTask<INetworkTransport>> connectFactory)
        {
            lock (sessionLock)
            {
                if (sessions.ContainsKey(sessionId))
                {
                    throw new InvalidOperationException($"Session {sessionId} already exists.");
                }
            }

            var transport = await connectFactory();
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
