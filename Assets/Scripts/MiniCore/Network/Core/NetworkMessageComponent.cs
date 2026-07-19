using MiniCore.Threading;
using MiniCore.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using MiniCore.Core;
using MiniCore.Serialization;

namespace MiniCore.Service
{
    /// <summary>
    /// 网络消息中枢，负责多会话的发包、收包、RPC、心跳和处理器派发。
    /// </summary>
    [AppService("网络", typeof(INetworkService), Description = "管理多会话的收发包、RPC、心跳和消息处理器派发。")]
    public class NetworkService : AAppService, INetworkService
    {
        #region Private 私有成员

        private INetworkSessionService sessionComponent; // 会话服务实现缓存。
        private INetworkSerializer serializer; // 当前网络序列化器。
        private MDedicatedThreadExecutor networkExecutor; // 网络 I/O 和协议循环使用的独占线程执行器。
        private long rpcIdGenerator = 1; // 单调递增的 RPC 标识生成器。
        private readonly object pendingRpcLock = new object(); // 待完成 RPC 表的同步锁。
        private readonly HashSet<string> boundSessionReceivers = new HashSet<string>(); // 已绑定收包回调的会话标识。

        private readonly Dictionary<long, PendingRpc> pendingRpcs = new Dictionary<long, PendingRpc>(); // 等待响应的 RPC 请求。
        private readonly Dictionary<uint, HandlerInfo> handlers = new Dictionary<uint, HandlerInfo>(); // 普通消息处理器映射。
        private readonly Dictionary<uint, RpcHandlerInfo> rpcHandlers = new Dictionary<uint, RpcHandlerInfo>(); // RPC 处理器映射。
        private readonly Dictionary<string, NetworkHeartbeatState> heartbeatStates = new Dictionary<string, NetworkHeartbeatState>(); // 各会话心跳状态。
        private readonly Dictionary<Type, uint> opcodeCache = new Dictionary<Type, uint>(); // 协议类型到 opcode 的缓存。
        private readonly ConcurrentQueue<NetworkIncomingPacket> incomingPackets = new ConcurrentQueue<NetworkIncomingPacket>(); // 等待主线程处理的收包队列。
        private int processingQueue; // 收包队列处理任务的互斥标志。
        private static readonly TimeSpan DefaultProbeTimeout = TimeSpan.FromSeconds(2); // 连接探测的默认超时。


        private class PendingRpc
        {
            /// <summary>
            /// 网络模块公开成员 SessionId 的说明。
            /// </summary>
            public string SessionId; // RPC 所属会话标识。
            public Type ResponseType; // 期望的 RPC 响应类型。
            public MTaskCompletionSource<object> Tcs; // 等待响应完成的任务源。
        }

        private class HandlerInfo
        {
            /// <summary>
            /// 网络模块公开成员 MessageType 的说明。
            /// </summary>
            public Type MessageType; // 普通消息运行时类型。
            public INetworkMessageHandlerInvoker Invoker; // 无反射普通消息派发器。
        }

        private class RpcHandlerInfo
        {
            /// <summary>
            /// 网络模块公开成员 RequestType 的说明。
            /// </summary>
            public Type RequestType; // RPC 请求运行时类型。
            public Type ResponseType; // RPC 响应运行时类型。
            public INetworkRpcHandlerInvoker Invoker; // 无反射 RPC 派发器。
            public Func<IRpcResponse> ResponseFactory; // 已缓存的 RPC 响应对象工厂。
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 默认客户端会话标识。
        /// </summary>
        public string DefaultSessionId { get; set; } = "default";
        /// <summary>
        /// 心跳请求的 opcode。
        /// </summary>
        public uint PingOpcode { get; set; } = 1;
        /// <summary>
        /// 心跳响应的 opcode。
        /// </summary>
        public uint PongOpcode { get; set; } = 2;
        /// <summary>
        /// 客户端发送心跳或服务端检查心跳的间隔。
        /// </summary>
        public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(5);
        /// <summary>
        /// 判定连接心跳超时的时长。
        /// </summary>
        public TimeSpan HeartbeatTimeout { get; set; } = TimeSpan.FromSeconds(15);
        /// <summary>
        /// 未指定取消令牌时使用的 RPC 超时时长。
        /// </summary>
        public TimeSpan RpcTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// 服务端创建逻辑会话后触发。
        /// </summary>
        public event Action<NetworkSession> OnServerSessionCreated;
        /// <summary>
        /// 服务端逻辑会话关闭后触发。
        /// </summary>
        public event Action<string> OnServerSessionClosed;

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 初始化消息中枢；Handler 由加载后的生成注册表显式添加。
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            networkExecutor = MTaskExecutors.CreateDedicated("MiniCore.Network");
            MTaskExecutors.Network = networkExecutor;
            serializer = null;
        }

        /// <summary>
        /// 在任务域取消前解除网络等待，并在快速退出时无等待关闭网络线程。
        /// </summary>
        protected override void OnDisposing()
        {
            StopNetworkOperations();
            if (MTaskRuntime.IsFastShutdown)
            {
                ReleaseNetworkExecutor();
            }
        }

        /// <summary>
        /// 在全部网络任务退场后回收网络专用线程。
        /// </summary>
        protected override void OnDispose()
        {
            StopNetworkOperations();
            ReleaseNetworkExecutor();
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 设置本消息中枢使用的协议序列化器。
        /// </summary>
        /// <param name="customSerializer">执行该方法所需的 customSerializer 参数。</param>
        public void SetSerializer(INetworkSerializer customSerializer)
        {
            serializer = customSerializer;
        }

        /// <summary>
        /// 创建并绑定默认 TCP 客户端会话。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public async MTask InitializeDefaultSessionAsync(string host, int port)
        {
            await InitializeSessionAsync(DefaultSessionId, host, port);
        }
        /*
                /// <summary>
                /// 执行 InitializeDefaultKcpSessionAsync 相关处理。
                /// </summary>
                /// <param name="host">执行该方法所需的 host 参数。</param>
                /// <param name="port">执行该方法所需的 port 参数。</param>
                /// <param name="conv">执行该方法所需的 conv 参数。</param>
                /// <param name="config">执行该方法所需的 config 参数。</param>
                /// <returns>执行处理后的结果。</returns>
                public async MTask InitializeDefaultKcpSessionAsync(string host, int port, uint conv, KcpTransportConfig config = null)
                {
                    await InitializeKcpSessionAsync(DefaultSessionId, host, port, conv, config);
                }*/

        /// <summary>
        /// 连接默认 KCP 会话，并通过心跳探测确认可用性。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <param name="conv">执行该方法所需的 conv 参数。</param>
        /// <param name="probeTimeout">执行该方法所需的 probeTimeout 参数。</param>
        /// <param name="config">执行该方法所需的 config 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public MTask<bool> ConnectDefaultKcpSessionAsync(string host, int port, uint conv, TimeSpan probeTimeout = default, KcpTransportConfig config = null)
        {
            return ConnectKcpSessionAsync(DefaultSessionId, host, port, conv, probeTimeout, config);
        }

        /// <summary>
        /// 连接默认 TCP 会话，并通过心跳探测确认可用性。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <param name="probeTimeout">执行该方法所需的 probeTimeout 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public MTask<bool> ConnectDefaultTcpSessionAsync(string host, int port, TimeSpan probeTimeout = default)
        {
            return ConnectTcpSessionAsync(DefaultSessionId, host, port, probeTimeout);
        }

        /// <summary>
        /// 连接默认 UDP 会话，并通过心跳探测确认可用性。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <param name="probeTimeout">执行该方法所需的 probeTimeout 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public MTask<bool> ConnectDefaultUdpSessionAsync(string host, int port, TimeSpan probeTimeout = default)
        {
            return ConnectUdpSessionAsync(DefaultSessionId, host, port, probeTimeout);
        }

        /// <summary>
        /// 重建指定 TCP 会话，并探测其是否能够收发心跳。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <param name="probeTimeout">执行该方法所需的 probeTimeout 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public async MTask<bool> ConnectTcpSessionAsync(string sessionId, string host, int port, TimeSpan probeTimeout = default)
        {
            PrepareSessionForReconnect(sessionId);

            try
            {
                await InitializeSessionAsync(sessionId, host, port);
            }
            catch (Exception ex)
            {
                LogSwitch.Warning($"Tcp session init failed: {ex.Message}");
                return false;
            }

            if (probeTimeout <= TimeSpan.Zero)
            {
                probeTimeout = DefaultProbeTimeout;
            }

            bool ok = await ProbeSessionAsync(sessionId, probeTimeout);
            if (!ok)
            {
                if (TryEnsureSessionService(out var service))
                {
                    service.RemoveSession(sessionId);
                }
            }

            return ok;
        }

        /// <summary>
        /// 重建指定 KCP 会话，并探测其是否能够收发心跳。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <param name="conv">执行该方法所需的 conv 参数。</param>
        /// <param name="probeTimeout">执行该方法所需的 probeTimeout 参数。</param>
        /// <param name="config">执行该方法所需的 config 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public async MTask<bool> ConnectKcpSessionAsync(string sessionId, string host, int port, uint conv, TimeSpan probeTimeout = default, KcpTransportConfig config = null)
        {
            PrepareSessionForReconnect(sessionId);

            try
            {
                await InitializeKcpSessionAsync(sessionId, host, port, conv, config);
            }
            catch (Exception ex)
            {
                LogSwitch.Warning($"Kcp session init failed: {ex.Message}");
                return false;
            }

            if (probeTimeout <= TimeSpan.Zero)
            {
                probeTimeout = DefaultProbeTimeout;
            }

            bool ok = await ProbeSessionAsync(sessionId, probeTimeout);
            if (!ok)
            {
                if (TryEnsureSessionService(out var service))
                {
                    service.RemoveSession(sessionId);
                }
            }
            return ok;
        }

        /// <summary>
        /// 重建指定 UDP 会话，并探测其是否能够收发心跳。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <param name="probeTimeout">执行该方法所需的 probeTimeout 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public async MTask<bool> ConnectUdpSessionAsync(string sessionId, string host, int port, TimeSpan probeTimeout = default)
        {
            PrepareSessionForReconnect(sessionId);

            try
            {
                await InitializeUdpSessionAsync(sessionId, host, port);
            }
            catch (Exception ex)
            {
                LogSwitch.Warning($"Udp session init failed: {ex.Message}");
                return false;
            }

            if (probeTimeout <= TimeSpan.Zero)
            {
                probeTimeout = DefaultProbeTimeout;
            }

            bool ok = await ProbeSessionAsync(sessionId, probeTimeout);
            if (!ok)
            {
                if (TryEnsureSessionService(out var service))
                {
                    service.RemoveSession(sessionId);
                }
            }

            return ok;
        }

        /// <summary>
        /// 创建 TCP 客户端会话并绑定其收包回调。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public async MTask InitializeSessionAsync(string sessionId, string host, int port)
        {
            await EnsureSessionService().CreateTcpSessionAsync(sessionId, host, port);
            BindSessionReceiver(sessionId);
        }

        /// <summary>
        /// 创建 KCP 客户端会话并绑定其收包回调。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <param name="conv">执行该方法所需的 conv 参数。</param>
        /// <param name="config">执行该方法所需的 config 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public async MTask InitializeKcpSessionAsync(string sessionId, string host, int port, uint conv, KcpTransportConfig config = null)
        {
            await EnsureSessionService().CreateKcpSessionAsync(sessionId, host, port, conv, config);
            BindSessionReceiver(sessionId);
        }

        /// <summary>
        /// 创建 UDP 客户端会话并绑定其收包回调。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public async MTask InitializeUdpSessionAsync(string sessionId, string host, int port)
        {
            await EnsureSessionService().CreateUdpSessionAsync(sessionId, host, port);
            BindSessionReceiver(sessionId);
        }

        /// <summary>
        /// 以客户端心跳模式绑定指定会话的收包回调。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        public void BindSessionReceiver(string sessionId)
        {
            BindSessionReceiverInternal(sessionId, NetworkHeartbeatMode.Client);
        }

        /// <summary>
        /// 以服务端心跳模式绑定指定会话的收包回调。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        public void BindServerSessionReceiver(string sessionId)
        {
            BindSessionReceiverInternal(sessionId, NetworkHeartbeatMode.Server);
        }

        /// <summary>
        /// 启动 KCP 服务端监听。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <param name="config">执行该方法所需的 config 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public MTask StartKcpServerAsync(string host, int port, KcpServerConfig config = null)
        {
            return EnsureSessionService().StartKcpServerAsync(host, port, config);
        }

        /// <summary>
        /// 启动 TCP 服务端监听。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public MTask StartTcpServerAsync(string host, int port)
        {
            return EnsureSessionService().StartTcpServerAsync(host, port);
        }

        /// <summary>
        /// 启动 UDP 服务端监听。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <param name="config">执行该方法所需的 config 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public MTask StartUdpServerAsync(string host, int port, UdpServerConfig config = null)
        {
            return EnsureSessionService().StartUdpServerAsync(host, port, config);
        }

        /// <summary>
        /// 停止 KCP 服务端及其关联会话。
        /// </summary>
        public void StopKcpServer()
        {
            if (!TryEnsureSessionService(out var service))
            {
                return;
            }
            service.StopKcpServer();
        }

        /// <summary>
        /// 停止 TCP 服务端及其关联会话。
        /// </summary>
        public void StopTcpServer()
        {
            if (!TryEnsureSessionService(out var service))
            {
                return;
            }
            service.StopTcpServer();
        }

        /// <summary>
        /// 停止 UDP 服务端及其关联会话。
        /// </summary>
        public void StopUdpServer()
        {
            if (!TryEnsureSessionService(out var service))
            {
                return;
            }
            service.StopUdpServer();
        }

        /// <summary>
        /// 获取当前服务端逻辑会话的快照。
        /// </summary>
        /// <returns>执行处理后的结果。</returns>
        public List<NetworkSession> GetServerSessionsSnapshot()
        {
            if (!TryEnsureSessionService(out var service))
            {
                return new List<NetworkSession>();
            }
            return service.GetServerSessionsSnapshot();
        }

        /// <summary>
        /// 按标识获取逻辑会话；不存在时返回空。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public NetworkSession GetSession(string sessionId)
        {
            if (!TryEnsureSessionService(out var service))
            {
                return null;
            }
            return service.GetSession(sessionId);
        }

        /// <summary>
        /// 断开并移除指定逻辑会话。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        public void DisconnectSession(string sessionId)
        {
            if (!TryEnsureSessionService(out var service))
            {
                return;
            }
            service.DisconnectSession(sessionId);
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 执行 Update 相关处理。
        /// </summary>
        protected override void Update()
        {
            if (incomingPackets.IsEmpty)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref processingQueue, 1, 0) != 0)
            {
                return;
            }

            ProcessQueueAsync().Forget();
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 在限定时间内发送心跳，验证指定会话是否可达。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="timeout">执行该方法所需的 timeout 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public async MTask<bool> ProbeSessionAsync(string sessionId, TimeSpan timeout)
        {
            if (!heartbeatStates.TryGetValue(sessionId, out var state))
            {
                LogSwitch.Warning($"Probe failed: heartbeat state missing. session:{sessionId}");
                return false;
            }

            if (!TryGetSession(sessionId, out var session))
            {
                LogSwitch.Warning($"Probe failed: session missing. session:{sessionId}");
                return false;
            }
            long lastPong = state.LastPongTicks;
            var start = DateTimeOffset.UtcNow;
            var nextPing = start;
            while (DateTimeOffset.UtcNow - start < timeout)
            {
                MTask.ThrowIfCancellationRequested();
                if (DateTimeOffset.UtcNow >= nextPing)
                {
                    try
                    {
                        await SendPingAsync(session);
                    }
                    catch (Exception ex)
                    {
                        LogSwitch.Warning($"Probe ping send failed. session:{sessionId} err:{ex.Message}");
                        return false;
                    }
                    nextPing = DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(200);
                }
                if (heartbeatStates.TryGetValue(sessionId, out var updated) && updated.LastPongTicks != lastPong)
                {
                    return true;
                }
                await MTask.Delay(50);
            }

            LogSwitch.Warning($"Probe timeout. session:{sessionId} timeoutMs:{(int)timeout.TotalMilliseconds}");
            return false;
        }

        /// <summary>
        /// 通过默认会话发送 RPC 请求并等待对应响应。
        /// </summary>
        /// <param name="request">执行该方法所需的 request 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public MTask<TResponse> CallAsync<TRequest, TResponse>(TRequest request)
            where TRequest : IRpcRequest
            where TResponse : IRpcResponse
        {
            return CallAsync<TRequest, TResponse>(DefaultSessionId, request);
        }

        /// <summary>
        /// 通过指定会话发送 RPC 请求并等待对应响应。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="request">执行该方法所需的 request 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public async MTask<TResponse> CallAsync<TRequest, TResponse>(string sessionId, TRequest request)
            where TRequest : IRpcRequest
            where TResponse : IRpcResponse
        {
            if (!TryGetSession(sessionId, out var session))
            {
                return CreateLocalErrorResponse<TResponse>($"Session {sessionId} not found.");
            }
            if (!session.IsConnected)
            {
                return CreateLocalErrorResponse<TResponse>($"Session {sessionId} not connected.");
            }

            long rpcId = Interlocked.Increment(ref rpcIdGenerator);
            request.RpcId = rpcId;

            var tcs = new MTaskCompletionSource<object>();
            lock (pendingRpcLock)
            {
                pendingRpcs[rpcId] = new PendingRpc { SessionId = sessionId, ResponseType = typeof(TResponse), Tcs = tcs };
            }

            using var linkedCts = new CancellationTokenSource();
            if (RpcTimeout > TimeSpan.Zero)
            {
                linkedCts.CancelAfter(RpcTimeout);
            }
            using var registration = linkedCts.Token.Register(() =>
            {
                if (TryRemovePendingRpc(rpcId, out var pending))
                {
                    Exception ex = new TimeoutException($"RPC timeout. session:{sessionId} rpcId:{rpcId}");
                    pending.Tcs.TrySetException(ex);
                }
            });

            uint opcode = ResolveOpcode(request.GetType());
            bool isLogEnabled = LogSwitch.EnableLog;
            string sendTime = null;
            if (isLogEnabled)
            {
                sendTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                LogSwitch.Info($"[{sendTime}] [{GetLogSide(session.SessionId)}] 发送RPC opcode:{opcode} rpcId:{rpcId} type:{request.GetType().FullName}");
            }
            byte[] payload = GetSerializer().Serialize(request);
            if (isLogEnabled && LogSwitch.EnablePayloadLog)
            {
                string payloadText = Encoding.UTF8.GetString(payload);
                LogSwitch.Info($"[{sendTime}] 发送RPC内容: {payloadText}");
            }
            byte[] body = BuildPacket(opcode, rpcId, payload);
            try
            {
                await session.SendAsync(new ArraySegment<byte>(body));
            }
            catch (Exception ex)
            {
                TryFailPendingRpc(rpcId, ex);
                throw;
            }

            object result = await tcs.Task;
            return (TResponse)result;
        }

        /// <summary>
        /// 通过默认会话发送普通协议消息。
        /// </summary>
        /// <param name="message">执行该方法所需的 message 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public MTask SendAsync<TMessage>(TMessage message) where TMessage : INormalMessage
        {
            return SendAsync(DefaultSessionId, message);
        }

        /// <summary>
        /// 通过指定会话发送普通协议消息。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="message">执行该方法所需的 message 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public async MTask SendAsync<TMessage>(string sessionId, TMessage message) where TMessage : INormalMessage
        {
            if (!TryGetSession(sessionId, out var session))
            {
                LogSwitch.Warning($"Session {sessionId} not found, send skipped.");
                return;
            }
            if (!session.IsConnected)
            {
                LogSwitch.Warning($"Session {sessionId} not connected, send skipped.");
                return;
            }
            uint opcode = ResolveOpcode(message.GetType());
            bool isLogEnabled = LogSwitch.EnableLog;
            string sendTime = null;
            if (isLogEnabled)
            {
                sendTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                LogSwitch.Info($"[{sendTime}] [{GetLogSide(session.SessionId)}] 发送普通消息 opcode:{opcode} rpcId:0 type:{message.GetType().FullName}");
            }
            byte[] payload = GetSerializer().Serialize(message);
            if (isLogEnabled && LogSwitch.EnablePayloadLog)
            {
                string payloadText = Encoding.UTF8.GetString(payload);
                LogSwitch.Info($"[{sendTime}] 发送普通消息内容: {payloadText}");
            }
            byte[] body = BuildPacket(opcode, 0, payload);
            await session.SendAsync(new ArraySegment<byte>(body));
        }

        /// <summary>
        /// 注册一个由生成表创建的普通消息处理器。
        /// </summary>
        /// <param name="invoker">普通消息处理器。</param>
        public void RegisterHandler(INetworkMessageHandlerInvoker invoker)
        {
            if (invoker == null)
            {
                throw new ArgumentNullException(nameof(invoker));
            }

            uint opcode = ResolveOpcode(invoker.MessageType);
            if (handlers.ContainsKey(opcode))
            {
                throw new InvalidOperationException($"普通消息 opcode 冲突：{opcode}。");
            }

            handlers.Add(opcode, new HandlerInfo
            {
                MessageType = invoker.MessageType,
                Invoker = invoker
            });
        }

        /// <summary>
        /// 注册一个由生成表创建的 RPC 处理器。
        /// </summary>
        /// <param name="invoker">RPC 处理器。</param>
        public void RegisterHandler(INetworkRpcHandlerInvoker invoker)
        {
            if (invoker == null)
            {
                throw new ArgumentNullException(nameof(invoker));
            }

            uint opcode = ResolveOpcode(invoker.RequestType);
            if (rpcHandlers.ContainsKey(opcode))
            {
                throw new InvalidOperationException($"RPC opcode 冲突：{opcode}。");
            }

            rpcHandlers.Add(opcode, new RpcHandlerInfo
            {
                RequestType = invoker.RequestType,
                ResponseType = invoker.ResponseType,
                Invoker = invoker,
                ResponseFactory = invoker.CreateResponse
            });
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 解除事件、终止 RPC 和心跳，使任务域取消后不再等待网络业务回调。
        /// </summary>
        private void StopNetworkOperations()
        {
            UnbindSessionServiceEvents();

            lock (pendingRpcLock)
            {
                foreach (var kv in pendingRpcs)
                {
                    kv.Value.Tcs.TrySetException(new ObjectDisposedException(nameof(NetworkService)));
                }
                pendingRpcs.Clear();
            }

            foreach (var sessionId in new List<string>(heartbeatStates.Keys))
            {
                StopHeartbeat(sessionId);
            }

            boundSessionReceivers.Clear();
        }

        /// <summary>
        /// 解除全局网络执行器引用并停止当前网络专用线程。
        /// </summary>
        private void ReleaseNetworkExecutor()
        {
            if (ReferenceEquals(MTaskExecutors.Network, networkExecutor))
            {
                MTaskExecutors.Network = MTaskExecutors.Unity;
            }

            networkExecutor?.Dispose();
            networkExecutor = null;
        }

        /// <summary>
        /// 执行 BuildPacket 相关处理。
        /// </summary>
        /// <param name="opcode">执行该方法所需的 opcode 参数。</param>
        /// <param name="rpcId">执行该方法所需的 rpcId 参数。</param>
        /// <param name="payload">执行该方法所需的 payload 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private byte[] BuildPacket(uint opcode, long rpcId, byte[] payload)
        {
            // header: opcode(4 bytes, big-endian) + rpcId(8 bytes, big-endian)
            int length = 4 + 8 + (payload?.Length ?? 0);
            byte[] buffer = new byte[length];

            NetBinaryCodec.WriteUInt32BE(buffer, 0, opcode);
            NetBinaryCodec.WriteInt64BE(buffer, 4, rpcId);

            if (payload != null && payload.Length > 0)
            {
                Buffer.BlockCopy(payload, 0, buffer, 12, payload.Length);
            }
            return buffer;
        }

        /// <summary>
        /// 执行 HandleIncoming 相关处理。
        /// </summary>
        /// <param name="session">执行该方法所需的 session 参数。</param>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private async MTask HandleIncoming(NetworkSession session, ReadOnlyMemory<byte> data)
        {
            if (data.Length < 12)
            {
                LogSwitch.Warning("包长度无效，头部不足。");
                return;
            }

            uint opcode = NetBinaryCodec.ReadUInt32BE(data.Span, 0);
            long rpcId = NetBinaryCodec.ReadInt64BE(data.Span, 4);

            int payloadLength = data.Length - 12;
            ReadOnlyMemory<byte> payload = payloadLength > 0 ? data.Slice(12, payloadLength) : ReadOnlyMemory<byte>.Empty;

            if (LogSwitch.EnableLog)
            {
                string recvTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                LogSwitch.Info($"[{recvTime}] [{GetLogSide(session.SessionId)}] 收到消息 opcode:{opcode} rpcId:{rpcId} len:{payloadLength}");
            }

            // Heartbeat handling
            if (opcode == PingOpcode)
            {
                // Always respond pong to ping to avoid race during server-session bootstrap.
                TouchPing(session.SessionId);
                await SendPongAsync(session, rpcId);
                return;
            }
            if (opcode == PongOpcode)
            {
                if (TryGetHeartbeatMode(session.SessionId, out var mode) && mode == NetworkHeartbeatMode.Client)
                {
                    TouchPong(session.SessionId);
                }
                return;
            }

            if (rpcId != 0 && TryRemovePendingRpc(session.SessionId, rpcId, out var pending))
            {
                try
                {
                    object resp = GetSerializer().Deserialize(pending.ResponseType, payload);
                    if (resp is IRpcResponse rpcResponse)
                    {
                        rpcResponse.RpcId = rpcId;
                    }
                    pending.Tcs.TrySetResult(resp);
                }
                catch (Exception ex)
                {
                    LogSwitch.Error($"反序列化响应失败 opcode:{opcode} rpcId:{rpcId} err:{ex.Message}");
                    pending.Tcs.TrySetException(ex);
                }
                return;
            }

            if (rpcId != 0 && rpcHandlers.TryGetValue(opcode, out RpcHandlerInfo rpcInfo))
            {
                if (!(GetSerializer().Deserialize(rpcInfo.RequestType, payload) is IRpcRequest req))
                {
                    LogSwitch.Error($"RPC请求反序列化失败，类型:{rpcInfo.RequestType?.FullName}");
                    return;
                }

                req.RpcId = rpcId;

                IRpcResponse response = rpcInfo.ResponseFactory();
                if (response == null)
                {
                    LogSwitch.Error($"RPC响应实例创建失败，类型:{rpcInfo.ResponseType?.FullName}");
                    return;
                }

                response.RpcId = rpcId;
                try
                {
                    await rpcInfo.Invoker.HandleAsync(session, req, response);
                }
                catch (Exception ex)
                {
                    LogSwitch.Error($"RPC处理器执行异常，opcode:{opcode} 会话:{session.SessionId} 错误:{ex}");
                    return;
                }

                uint respOpcode = ResolveOpcode(response.GetType());
                bool isLogEnabled = LogSwitch.EnableLog;
                string sendTime = null;
                if (isLogEnabled)
                {
                    sendTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    LogSwitch.Info($"[{sendTime}] [{GetLogSide(session.SessionId)}] 发送RPC响应 opcode:{respOpcode} rpcId:{rpcId} type:{response.GetType().FullName}");
                }
                try
                {
                    byte[] respPayload = GetSerializer().Serialize(response);
                    if (isLogEnabled && LogSwitch.EnablePayloadLog)
                    {
                        string payloadText = Encoding.UTF8.GetString(respPayload);
                        LogSwitch.Info($"[{sendTime}] 发送RPC响应内容: {payloadText}");
                    }
                    byte[] packet = BuildPacket(respOpcode, rpcId, respPayload);
                    await session.SendAsync(new ArraySegment<byte>(packet));
                }
                catch (Exception ex)
                {
                    LogSwitch.Error($"RPC响应发送异常，opcode:{respOpcode} 会话:{session.SessionId} 错误:{ex}");
                }
                return;
            }

            if (handlers.TryGetValue(opcode, out HandlerInfo info))
            {
                if (!(GetSerializer().Deserialize(info.MessageType, payload) is INormalMessage msg))
                {
                    LogSwitch.Error($"普通消息反序列化失败，类型:{info.MessageType?.FullName}");
                    return;
                }

                await info.Invoker.HandleAsync(session, msg);
            }
            else
            {
                LogSwitch.Warning($"未找到 opcode:{opcode} 的处理器");
            }
        }

        /// <summary>
        /// 执行 StartHeartbeat 相关处理。
        /// </summary>
        /// <param name="session">执行该方法所需的 session 参数。</param>
        /// <param name="mode">执行该方法所需的 mode 参数。</param>
        private void StartHeartbeat(NetworkSession session, NetworkHeartbeatMode mode)
        {
            StopHeartbeat(session.SessionId);
            var state = new NetworkHeartbeatState
            {
                LastPongTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                LastPingTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Mode = mode
            };
            heartbeatStates[session.SessionId] = state;
            if (mode == NetworkHeartbeatMode.Client)
            {
                HeartbeatLoopClient(session, state).Forget();
            }
            else
            {
                HeartbeatLoopServer(session, state).Forget();
            }
        }

        /// <summary>
        /// 执行 StopHeartbeat 相关处理。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        private void StopHeartbeat(string sessionId)
        {
            if (heartbeatStates.TryGetValue(sessionId, out var state))
            {
                Volatile.Write(ref state.Stopped, 1);
                heartbeatStates.Remove(sessionId);
            }
        }

        /// <summary>
        /// 执行 HeartbeatLoopClient 相关处理。
        /// </summary>
        /// <param name="session">执行该方法所需的 session 参数。</param>
        /// <param name="state">当前会话的心跳状态。</param>
        /// <returns>执行处理后的结果。</returns>
        private async MTask HeartbeatLoopClient(NetworkSession session, NetworkHeartbeatState state)
        {
            try
            {
                while (Volatile.Read(ref state.Stopped) == 0 && session.IsConnected)
                {
                    await MTask.Delay(HeartbeatInterval);
                    if (Volatile.Read(ref state.Stopped) != 0)
                    {
                        break;
                    }

                    await SendPingAsync(session);
                    if (IsHeartbeatTimeout(session.SessionId))
                    {
                        string side = GetLogSide(session.SessionId);
                        string text = $"{side}心跳超时，主动断开，会话:{session.SessionId}";
                        LogSwitch.Warning(text);
                        session.Transport?.Disconnect();
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                LogSwitch.Warning($"心跳循环异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 HeartbeatLoopServer 相关处理。
        /// </summary>
        /// <param name="session">执行该方法所需的 session 参数。</param>
        /// <param name="state">当前会话的心跳状态。</param>
        /// <returns>执行处理后的结果。</returns>
        private async MTask HeartbeatLoopServer(NetworkSession session, NetworkHeartbeatState state)
        {
            try
            {
                while (Volatile.Read(ref state.Stopped) == 0 && session.IsConnected)
                {
                    await MTask.Delay(HeartbeatInterval);
                    if (Volatile.Read(ref state.Stopped) != 0)
                    {
                        break;
                    }

                    if (IsPingTimeout(session.SessionId))
                    {
                        string side = GetLogSide(session.SessionId);
                        string text = $"{side}心跳超时，踢出连接，会话:{session.SessionId}";
                        LogSwitch.Warning(text);
                        session.Transport?.Disconnect();
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                LogSwitch.Warning($"服务端心跳循环异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 SendPingAsync 相关处理。
        /// </summary>
        /// <param name="session">执行该方法所需的 session 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private async MTask SendPingAsync(NetworkSession session)
        {
            if (heartbeatStates.TryGetValue(session.SessionId, out var state))
            {
                state.LastPingSentTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
            byte[] body = BuildPacket(PingOpcode, 0, null);
            await session.SendAsync(new ArraySegment<byte>(body));
        }

        /// <summary>
        /// 执行 SendPongAsync 相关处理。
        /// </summary>
        /// <param name="session">执行该方法所需的 session 参数。</param>
        /// <param name="rpcId">执行该方法所需的 rpcId 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private async MTask SendPongAsync(NetworkSession session, long rpcId)
        {
            byte[] body = BuildPacket(PongOpcode, rpcId, null);
            await session.SendAsync(new ArraySegment<byte>(body));
        }

        /// <summary>
        /// 执行 TouchPong 相关处理。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        private void TouchPong(string sessionId)
        {
            if (heartbeatStates.TryGetValue(sessionId, out var state))
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                state.LastPongTicks = now;
                if (state.LastPingSentTicks > 0)
                {
                    int rtt = (int)Math.Max(0, now - state.LastPingSentTicks);
                    state.LastRttMs = rtt;
                    if (state.MinRttWindowStartTicks == 0)
                    {
                        state.MinRttWindowStartTicks = now;
                        state.MinRttMs = rtt;
                    }
                    else if (now - state.MinRttWindowStartTicks > 10000)
                    {
                        state.MinRttWindowStartTicks = now;
                        state.MinRttMs = rtt;
                    }
                    else if (state.MinRttMs == 0 || rtt < state.MinRttMs)
                    {
                        state.MinRttMs = rtt;
                    }
                }
            }
        }

        /// <summary>
        /// 执行 TouchPing 相关处理。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        private void TouchPing(string sessionId)
        {
            if (heartbeatStates.TryGetValue(sessionId, out var state))
            {
                state.LastPingTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取最近一次心跳往返耗时。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="pingMs">执行该方法所需的 pingMs 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public bool TryGetLastPingMs(string sessionId, out int pingMs)
        {
            pingMs = 0;
            if (!heartbeatStates.TryGetValue(sessionId, out var state))
            {
                return false;
            }
            pingMs = state.LastRttMs;
            return pingMs > 0;
        }

        /// <summary>
        /// 获取当前统计窗口内的最小心跳往返耗时。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="pingMs">执行该方法所需的 pingMs 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public bool TryGetMinPingMs(string sessionId, out int pingMs)
        {
            pingMs = 0;
            if (!heartbeatStates.TryGetValue(sessionId, out var state))
            {
                return false;
            }
            pingMs = state.MinRttMs;
            return pingMs > 0;
        }

        /// <summary>
        /// 获取底层 KCP 传输层的平滑 RTT。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="rttMs">执行该方法所需的 rttMs 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public bool TryGetTransportRttMs(string sessionId, out int rttMs)
        {
            rttMs = 0;
            if (!TryEnsureSessionService(out var service))
            {
                return false;
            }

            var session = service.GetSession(sessionId);
            if (session?.Transport is KcpTransport kcpTransport)
            {
                return kcpTransport.TryGetSmoothedRttMs(out rttMs);
            }

            return false;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 执行 IsHeartbeatTimeout 相关处理。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private bool IsHeartbeatTimeout(string sessionId)
        {
            if (!heartbeatStates.TryGetValue(sessionId, out var state))
            {
                return false;
            }
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return now - state.LastPongTicks > HeartbeatTimeout.TotalMilliseconds;
        }

        /// <summary>
        /// 执行 IsPingTimeout 相关处理。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private bool IsPingTimeout(string sessionId)
        {
            if (!heartbeatStates.TryGetValue(sessionId, out var state))
            {
                return false;
            }
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return now - state.LastPingTicks > HeartbeatTimeout.TotalMilliseconds;
        }

        /// <summary>
        /// 执行 TryGetSession 相关处理。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="session">执行该方法所需的 session 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private bool TryGetSession(string sessionId, out NetworkSession session)
        {
            session = null;
            if (!TryEnsureSessionService(out var service))
            {
                LogSwitch.Warning($"Session service missing. session:{sessionId}");
                return false;
            }

            session = service.GetSession(sessionId);
            if (session != null)
            {
                return true;
            }

            LogSwitch.Warning($"Session {sessionId} not found.");
            return false;
        }

        /// <summary>
        /// 执行 PrepareSessionForReconnect 相关处理。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        private void PrepareSessionForReconnect(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId) || !TryEnsureSessionService(out var service))
            {
                return;
            }

            var existing = service.GetSession(sessionId);
            if (existing == null)
            {
                return;
            }

            try
            {
                existing.Close();
            }
            catch
            {
            }

            service.RemoveSession(sessionId);
            boundSessionReceivers.Remove(sessionId);
            StopHeartbeat(sessionId);
            FailPendingRpcsBySession(sessionId, new InvalidOperationException($"Session reconnect reset: {sessionId}"));
        }

        /// <summary>
        /// 执行 TryRemovePendingRpc 相关处理。
        /// </summary>
        /// <param name="rpcId">执行该方法所需的 rpcId 参数。</param>
        /// <param name="pending">执行该方法所需的 pending 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private bool TryRemovePendingRpc(long rpcId, out PendingRpc pending)
        {
            lock (pendingRpcLock)
            {
                if (pendingRpcs.TryGetValue(rpcId, out pending))
                {
                    pendingRpcs.Remove(rpcId);
                    return true;
                }
            }

            pending = null;
            return false;
        }

        /// <summary>
        /// 仅在入站包所属会话与待处理请求一致时移除 RPC，避免同一组件承载客户端和服务端时将请求误识别为响应。
        /// </summary>
        /// <param name="sessionId">收到入站包的逻辑会话标识。</param>
        /// <param name="rpcId">入站包头携带的 RPC 标识。</param>
        /// <param name="pending">成功匹配时返回的待处理 RPC 信息。</param>
        /// <returns>找到且会话归属一致时返回 true。</returns>
        private bool TryRemovePendingRpc(string sessionId, long rpcId, out PendingRpc pending)
        {
            lock (pendingRpcLock)
            {
                if (pendingRpcs.TryGetValue(rpcId, out pending) && string.Equals(pending.SessionId, sessionId, StringComparison.Ordinal))
                {
                    pendingRpcs.Remove(rpcId);
                    return true;
                }
            }

            pending = null;
            return false;
        }

        /// <summary>
        /// 执行 TryFailPendingRpc 相关处理。
        /// </summary>
        /// <param name="rpcId">执行该方法所需的 rpcId 参数。</param>
        /// <param name="ex">执行该方法所需的 ex 参数。</param>
        private void TryFailPendingRpc(long rpcId, Exception ex)
        {
            if (TryRemovePendingRpc(rpcId, out var pending))
            {
                pending.Tcs.TrySetException(ex);
            }
        }

        /// <summary>
        /// 执行 FailPendingRpcsBySession 相关处理。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="ex">执行该方法所需的 ex 参数。</param>
        private void FailPendingRpcsBySession(string sessionId, Exception ex)
        {
            List<PendingRpc> toFail = null;
            List<long> toRemove = null;
            lock (pendingRpcLock)
            {
                foreach (var kv in pendingRpcs)
                {
                    if (!string.Equals(kv.Value.SessionId, sessionId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (toFail == null)
                    {
                        toFail = new List<PendingRpc>();
                    }
                    if (toRemove == null)
                    {
                        toRemove = new List<long>();
                    }

                    toFail.Add(kv.Value);
                    toRemove.Add(kv.Key);
                }

                if (toRemove != null)
                {
                    foreach (var rpcId in toRemove)
                    {
                        pendingRpcs.Remove(rpcId);
                    }
                }
            }

            if (toFail == null)
            {
                return;
            }

            foreach (var pending in toFail)
            {
                pending.Tcs.TrySetException(ex);
            }
        }

        /// <summary>
        /// 执行 CreateLocalErrorResponse<TResponse> 相关处理。
        /// </summary>
        /// <param name="message">执行该方法所需的 message 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private TResponse CreateLocalErrorResponse<TResponse>(string message) where TResponse : IRpcResponse
        {
            if (Activator.CreateInstance(typeof(TResponse)) is TResponse response)
            {
                response.Code = -1;
                response.Msg = message;
                return response;
            }

            throw new InvalidOperationException($"Response type {typeof(TResponse).FullName} cannot be created.");
        }

        /// <summary>
        /// 执行 GetSerializer 相关处理。
        /// </summary>
        /// <returns>执行处理后的结果。</returns>
        private INetworkSerializer GetSerializer()
        {
            if (serializer == null)
            {
                serializer = new ProtobufSerializer();
            }
            return serializer;
        }

        /// <summary>
        /// 执行 ResolveOpcode 相关处理。
        /// </summary>
        /// <param name="msgType">执行该方法所需的 msgType 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private uint ResolveOpcode(Type msgType)
        {
            if (opcodeCache.TryGetValue(msgType, out uint cached))
            {
                return cached;
            }
            if (OpcodeRegistry.TryGetOpcodeByMessage(msgType, out uint mapped))
            {
                opcodeCache[msgType] = mapped;
                return mapped;
            }
            throw new InvalidOperationException($"Opcode mapping missing for {msgType.FullName}, please regenerate mapping.");
        }

        /// <summary>
        /// 执行 TryGetHeartbeatMode 相关处理。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="mode">执行该方法所需的 mode 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private bool TryGetHeartbeatMode(string sessionId, out NetworkHeartbeatMode mode)
        {
            if (heartbeatStates.TryGetValue(sessionId, out var state))
            {
                mode = state.Mode;
                return true;
            }
            mode = NetworkHeartbeatMode.Client;
            return false;
        }

        /// <summary>
        /// 执行 BindSessionReceiverInternal 相关处理。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="mode">执行该方法所需的 mode 参数。</param>
        private void BindSessionReceiverInternal(string sessionId, NetworkHeartbeatMode mode)
        {
            var sessionService = EnsureSessionService();
            var session = sessionService.GetSession(sessionId);
            if (session?.Transport == null)
            {
                throw new InvalidOperationException($"Session {sessionId} not found or not connected.");
            }

            if (!boundSessionReceivers.Add(sessionId))
            {
                return;
            }

            session.Transport.OnDataReceived += data => EnqueueIncoming(session, data);
            session.Transport.OnDisconnected += () =>
            {
                boundSessionReceivers.Remove(session.SessionId);
                StopHeartbeat(session.SessionId);
                FailPendingRpcsBySession(session.SessionId, new InvalidOperationException($"Session disconnected: {session.SessionId}"));
                string side = GetLogSide(session.SessionId);
                string text = $"{side}连接已断开，会话:{session.SessionId}";
                LogSwitch.Warning(text);
            };
            StartHeartbeat(session, mode);
        }

        /// <summary>
        /// 执行 EnqueueIncoming 相关处理。
        /// </summary>
        /// <param name="session">执行该方法所需的 session 参数。</param>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private MTask EnqueueIncoming(NetworkSession session, ReadOnlyMemory<byte> data)
        {
            if (data.IsEmpty)
            {
                return MTask.CompletedTask;
            }

            int length = data.Length;
            byte[] buffer = ByteBufferPool.Shared.Rent(length);
            data.Span.CopyTo(buffer);
            incomingPackets.Enqueue(new NetworkIncomingPacket
            {
                Session = session,
                Buffer = buffer,
                Length = length
            });
            return MTask.CompletedTask;
        }

        /// <summary>
        /// 执行 ProcessQueueAsync 相关处理。
        /// </summary>
        /// <returns>执行处理后的结果。</returns>
        private async MTask ProcessQueueAsync()
        {
            try
            {
                while (incomingPackets.TryDequeue(out var packet))
                {
                    try
                    {
                        await HandleIncoming(packet.Session, new ReadOnlyMemory<byte>(packet.Buffer, 0, packet.Length));
                    }
                    finally
                    {
                        ByteBufferPool.Shared.Return(packet.Buffer);
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref processingQueue, 0);
            }
        }

        /// <summary>
        /// 执行 HandleServerSessionCreated 相关处理。
        /// </summary>
        /// <param name="session">执行该方法所需的 session 参数。</param>
        private void HandleServerSessionCreated(NetworkSession session)
        {
            if (session == null)
            {
                return;
            }

            BindServerSessionReceiver(session.SessionId);
            OnServerSessionCreated?.Invoke(session);
        }

        /// <summary>
        /// 执行 HandleServerSessionClosed 相关处理。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        private void HandleServerSessionClosed(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return;
            }

            boundSessionReceivers.Remove(sessionId);
            StopHeartbeat(sessionId);
            FailPendingRpcsBySession(sessionId, new InvalidOperationException($"Session closed: {sessionId}"));
            OnServerSessionClosed?.Invoke(sessionId);
        }

        /// <summary>
        /// 执行 EnsureSessionService 相关处理。
        /// </summary>
        /// <returns>执行处理后的结果。</returns>
        private INetworkSessionService EnsureSessionService()
        {
            if (sessionComponent != null)
            {
                return sessionComponent;
            }

            if (NetworkSessionServiceRegistry.TryResolve(out var resolved) && resolved != null)
            {
                sessionComponent = resolved;
                BindSessionServiceEvents();
                return sessionComponent;
            }

            sessionComponent = Global.GetOrAdd<NetworkSessionComponent>(this);

            BindSessionServiceEvents();
            return sessionComponent;
        }

        /// <summary>
        /// 执行 TryEnsureSessionService 相关处理。
        /// </summary>
        /// <param name="service">执行该方法所需的 service 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private bool TryEnsureSessionService(out INetworkSessionService service)
        {
            if (sessionComponent != null)
            {
                service = sessionComponent;
                return true;
            }

            if (NetworkSessionServiceRegistry.TryResolve(out var resolved) && resolved != null)
            {
                sessionComponent = resolved;
                BindSessionServiceEvents();
                service = sessionComponent;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// 执行 BindSessionServiceEvents 相关处理。
        /// </summary>
        private void BindSessionServiceEvents()
        {
            if (sessionComponent == null)
            {
                return;
            }

            sessionComponent.OnServerSessionCreated -= HandleServerSessionCreated;
            sessionComponent.OnServerSessionClosed -= HandleServerSessionClosed;
            sessionComponent.OnServerSessionCreated += HandleServerSessionCreated;
            sessionComponent.OnServerSessionClosed += HandleServerSessionClosed;
        }

        /// <summary>
        /// 执行 UnbindSessionServiceEvents 相关处理。
        /// </summary>
        private void UnbindSessionServiceEvents()
        {
            if (sessionComponent == null)
            {
                return;
            }

            sessionComponent.OnServerSessionCreated -= HandleServerSessionCreated;
            sessionComponent.OnServerSessionClosed -= HandleServerSessionClosed;
        }

        /// <summary>
        /// 执行 GetLogSide 相关处理。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private string GetLogSide(string sessionId)
        {
            if (TryGetHeartbeatMode(sessionId, out var mode))
            {
                return mode == NetworkHeartbeatMode.Server ? "服务端" : "客户端";
            }

            return "未知端";
        }

        #endregion
    }
}
