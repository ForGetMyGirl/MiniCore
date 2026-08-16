using MiniCore.Threading;
using MiniCore.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using MiniCore.Core;
using MiniCore.Serialization;

namespace MiniCore.Service
{
    /// <summary>
    /// 网络消息中枢，负责多会话的发包、收包、RPC、心跳和处理器派发。
    /// </summary>
    [AppService(
        "网络",
        typeof(INetworkService),
        Description = "管理多会话的收发包、RPC、心跳和消息处理器派发。",
        RuntimeTargets = AppServiceRuntimeTargets.All,
        RequiredInDedicatedServer = true)]
    public class NetworkService : AAppService, INetworkService
    {
        #region Private 私有成员

        private INetworkSessionService sessionComponent; // 会话服务实现缓存。
        private INetworkSerializer serializer; // 当前网络序列化器。
        private NetworkProtocolRegistry protocolRegistry; // 当前服务实例使用的不可变协议映射。
        private IMTaskOwnedExecutor ownedNetworkExecutor; // 当前模块创建并负责释放的网络执行器租约；无多线程环境下为空。
        private IMTaskExecutor networkExecutor; // 网络连接、接收与协议更新循环使用的执行器。
        private long rpcIdGenerator = 1; // 单调递增的 RPC 标识生成器。
        private readonly object pendingRpcLock = new object(); // 待完成 RPC 表的同步锁。
        private readonly HashSet<string> boundSessionReceivers = new HashSet<string>(); // 已绑定收包回调的会话标识。

        private readonly Dictionary<long, PendingRpc> pendingRpcs = new Dictionary<long, PendingRpc>(); // 等待响应的 RPC 请求。
        private readonly Dictionary<string, NetworkHeartbeatState> heartbeatStates = new Dictionary<string, NetworkHeartbeatState>(); // 各会话心跳状态。
        private const int IncomingDataMaximumPacketCount = 4096; // 全局普通收包队列固定槽位数量。
        private const int IncomingDataMaximumByteCount = 4 * 1024 * 1024; // 全局普通收包队列有效字节上限。
        private const int IncomingControlMaximumPacketCount = 256; // Ping/Pong 与已匹配 RPC 响应的保留槽位数量。
        private const int IncomingControlMaximumByteCount = 64 * 1024; // Ping/Pong 与已匹配 RPC 响应的保留字节上限。
        private const int IncomingSessionMaximumPacketCount = 1024; // 单会话全部收包最大数量。
        private const int IncomingSessionMaximumByteCount = 1024 * 1024; // 单会话全部收包最大有效字节数。
        private static readonly long IncomingCongestionDisconnectTicks = Stopwatch.Frequency * 3L; // 单会话持续三秒满载后断开的阈值。
        private readonly object incomingQueueLock = new object(); // 同步全局预算、会话预算和两条固定队列。
        private readonly FixedCapacityPacketQueue<NetworkIncomingPacket> incomingDataPackets = new FixedCapacityPacketQueue<NetworkIncomingPacket>(IncomingDataMaximumPacketCount, IncomingDataMaximumByteCount); // 普通业务收包环形队列。
        private readonly FixedCapacityPacketQueue<NetworkIncomingPacket> incomingControlPackets = new FixedCapacityPacketQueue<NetworkIncomingPacket>(IncomingControlMaximumPacketCount, IncomingControlMaximumByteCount); // 心跳与已匹配 RPC 响应的独立处理队列。
        private readonly Dictionary<string, IncomingSessionBudget> incomingSessionBudgets = new Dictionary<string, IncomingSessionBudget>(); // 各会话当前占用与持续满载时刻。
        private long incomingPacketCount; // 当前等待主线程处理的数据包数量。
        private long incomingPacketBytes; // 当前等待主线程处理的数据总字节数。
        private long peakIncomingPacketCount; // 统计周期内等待处理的数据包数量峰值。
        private long peakIncomingPacketBytes; // 统计周期内等待处理的数据字节数峰值。
        private long processedIncomingPacketCount; // 统计周期内已完成处理的数据包总数。
        private long maxIncomingPacketProcessTicks; // 统计周期内单包处理耗时峰值的 Stopwatch tick。
        private int incomingTimingMetricsEnabled; // 是否记录仅供压测诊断使用的入站队列等待耗时。
        private long incomingQueueWaitSampleCount; // 当前统计周期内完成入站队列等待采样的包数量。
        private long totalIncomingQueueWaitTicks; // 网络线程入队到主线程开始处理的累计 Stopwatch tick。
        private long maxIncomingQueueWaitTicks; // 网络线程入队到主线程开始处理的最大 Stopwatch tick。
        private readonly NetworkTimingHistogram incomingQueueWaitHistogram = new NetworkTimingHistogram(); // 仅压测启用的入站等待百分位时间桶。
        private readonly NetworkTimingHistogram incomingPacketProcessHistogram = new NetworkTimingHistogram(); // 仅压测启用的主线程单包处理百分位时间桶。
        private long incomingControlRejectedPacketCount; // 统计周期内控制保留入站队列或单会话预算拒绝的包数量。
        private long incomingDataRejectedPacketCount; // 统计周期内普通数据入站队列或单会话预算拒绝的包数量。
        private int processingControlQueue; // 心跳与已匹配 RPC 响应处理任务的互斥标志。
        private int processingDataQueue; // 普通消息与 RPC 请求处理任务的互斥标志。
        private static readonly TimeSpan DefaultProbeTimeout = TimeSpan.FromSeconds(2); // 连接探测的默认超时。

        private class PendingRpc
        {
            /// <summary>
            /// 网络模块公开成员 SessionId 的说明。
            /// </summary>
            public string SessionId; // RPC 所属会话标识。
            public Type ResponseType; // 期望的 RPC 响应类型。
            public uint ResponseOpcode; // 期望的 RPC 响应 Opcode。
            public MTaskCompletionSource<object> Tcs; // 等待响应完成的任务源。
        }

        /// <summary>
        /// 保存单个逻辑会话在入站固定队列中占用的容量与持续满载状态。
        /// </summary>
        private sealed class IncomingSessionBudget
        {
            /// <summary>
            /// 当前会话等待主线程处理的数据包数量。
            /// </summary>
            public int PacketCount;
            /// <summary>
            /// 当前会话等待主线程处理的有效字节数。
            /// </summary>
            public long ByteCount;
            /// <summary>
            /// 最近一次连续容量拒绝开始的 Stopwatch tick；零表示当前未满载。
            /// </summary>
            public long FullSinceTicks;
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 默认客户端会话标识。
        /// </summary>
        public string DefaultSessionId { get; set; } = "default";
        /// <summary>
        /// 每次主循环最多处理的入站业务包数量；控制包仍优先。
        /// </summary>
        public int IncomingPacketBudgetPerUpdate { get; set; } = 256;
        /// <summary>
        /// 每次主循环用于入站派发的最大时间预算。
        /// </summary>
        public TimeSpan IncomingTimeBudgetPerUpdate { get; set; } = TimeSpan.FromMilliseconds(2);
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
        public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(2);
        /// <summary>
        /// 判定连接心跳超时的时长。
        /// </summary>
        public TimeSpan HeartbeatTimeout { get; set; } = TimeSpan.FromSeconds(10);

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
            if (MTaskExecutors.TryCreateSingleThread("MiniCore.Network", out ownedNetworkExecutor))
            {
                networkExecutor = ownedNetworkExecutor;
            }
            else
            {
                networkExecutor = MTaskExecutors.Unity;
            }

            serializer = null;
            protocolRegistry = null;
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
            if (protocolRegistry != null)
            {
                throw new InvalidOperationException("协议 Registry 已配置，不能再替换网络序列化器。");
            }

            serializer = customSerializer ?? throw new ArgumentNullException(nameof(customSerializer));
        }

        /// <summary>
        /// 为当前服务实例提交已经完整校验的不可变协议 Registry。
        /// </summary>
        /// <param name="registry">包含消息、Opcode、解析器与 Handler 的协议 Registry。</param>
        public void ConfigureProtocol(NetworkProtocolRegistry registry)
        {
            if (protocolRegistry != null)
            {
                throw new InvalidOperationException("当前网络服务已经配置协议 Registry。");
            }

            protocolRegistry = registry ?? throw new ArgumentNullException(nameof(registry));
            if (serializer == null)
            {
                serializer = new ProtobufSerializer(protocolRegistry);
            }
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
        /// 连接默认 WebSocket 会话，并通过心跳探测确认可用性。
        /// </summary>
        /// <param name="url">包含路径的完整 WS/WSS 地址。</param>
        /// <param name="probeTimeout">连接后心跳探测超时。</param>
        /// <returns>连接和心跳探测均成功时返回 true。</returns>
        public MTask<bool> ConnectDefaultWebSocketSessionAsync(string url, TimeSpan probeTimeout = default)
        {
            return ConnectWebSocketSessionAsync(DefaultSessionId, url, probeTimeout);
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
        /// 重建指定 WebSocket 会话，并探测其是否能够收发心跳。
        /// </summary>
        /// <param name="sessionId">逻辑会话标识。</param>
        /// <param name="url">包含路径的完整 WS/WSS 地址。</param>
        /// <param name="probeTimeout">连接后心跳探测超时。</param>
        /// <returns>连接和心跳探测均成功时返回 true。</returns>
        public async MTask<bool> ConnectWebSocketSessionAsync(string sessionId, string url, TimeSpan probeTimeout = default)
        {
            PrepareSessionForReconnect(sessionId);
            try
            {
                await InitializeWebSocketSessionAsync(sessionId, url);
            }
            catch (Exception exception)
            {
                LogSwitch.Warning($"WebSocket session init failed: {exception.Message}");
                return false;
            }

            if (probeTimeout <= TimeSpan.Zero)
            {
                probeTimeout = DefaultProbeTimeout;
            }

            bool connected = await ProbeSessionAsync(sessionId, probeTimeout);
            if (!connected && TryEnsureSessionService(out INetworkSessionService service))
            {
                service.RemoveSession(sessionId);
            }

            return connected;
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
        /// 创建 WebSocket 客户端会话并绑定其收包回调。
        /// </summary>
        /// <param name="sessionId">逻辑会话标识。</param>
        /// <param name="url">包含路径的完整 WS/WSS 地址。</param>
        /// <returns>会话完成握手并绑定后的任务。</returns>
        public async MTask InitializeWebSocketSessionAsync(string sessionId, string url)
        {
            await EnsureSessionService().CreateWebSocketSessionAsync(sessionId, url);
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
        /// 启动原生 WS/WSS 服务端监听。
        /// </summary>
        /// <param name="host">监听地址。</param>
        /// <param name="port">监听端口。</param>
        /// <param name="config">路径、消息大小、握手和 TLS 配置。</param>
        /// <returns>监听启动任务。</returns>
        public MTask StartWebSocketServerAsync(string host, int port, WebSocketServerConfig config = null)
        {
            return EnsureSessionService().StartWebSocketServerAsync(host, port, config);
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
        /// 停止原生 WebSocket 服务端及其关联会话。
        /// </summary>
        public void StopWebSocketServer()
        {
            if (TryEnsureSessionService(out INetworkSessionService service))
            {
                service.StopWebSocketServer();
            }
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
        /// 获取当前收包队列的积压量、峰值与处理耗时诊断快照。
        /// </summary>
        /// <returns>不改变队列状态的当前诊断快照。</returns>
        public NetworkIncomingQueueSnapshot GetIncomingQueueSnapshot()
        {
            long maxProcessTicks = Interlocked.Read(ref maxIncomingPacketProcessTicks);
            long queueWaitSamples = Interlocked.Read(ref incomingQueueWaitSampleCount);
            NetworkTimingPercentileSummary queueWait = incomingQueueWaitHistogram.GetSummary();
            NetworkTimingPercentileSummary packetProcess = incomingPacketProcessHistogram.GetSummary();
            return new NetworkIncomingQueueSnapshot(
                Interlocked.Read(ref incomingPacketCount),
                Interlocked.Read(ref incomingPacketBytes),
                Interlocked.Read(ref peakIncomingPacketCount),
                Interlocked.Read(ref peakIncomingPacketBytes),
                Interlocked.Read(ref processedIncomingPacketCount),
                maxProcessTicks * 1000d / Stopwatch.Frequency,
                packetProcess.P50Milliseconds,
                packetProcess.P95Milliseconds,
                packetProcess.P99Milliseconds,
                queueWaitSamples,
                ToAverageMilliseconds(Interlocked.Read(ref totalIncomingQueueWaitTicks), queueWaitSamples),
                ToMilliseconds(Interlocked.Read(ref maxIncomingQueueWaitTicks)),
                queueWait.P50Milliseconds,
                queueWait.P95Milliseconds,
                queueWait.P99Milliseconds,
                Interlocked.Read(ref incomingControlRejectedPacketCount),
                Interlocked.Read(ref incomingDataRejectedPacketCount));
        }

        /// <summary>
        /// 启用或关闭入站队列等待耗时诊断，并清空上一周期诊断数据。
        /// </summary>
        /// <param name="enabled">为 true 时记录网络线程入队到主线程开始处理的等待时间；仅建议由压测启用。</param>
        public void SetIncomingQueueTimingMetricsEnabled(bool enabled)
        {
            Interlocked.Exchange(ref incomingTimingMetricsEnabled, enabled ? 1 : 0);
            ResetIncomingQueueTimingMetrics();
        }

        /// <summary>
        /// 重置收包队列的峰值、累计处理数量和单包最大处理耗时；当前积压不会被清除。
        /// </summary>
        public void ResetIncomingQueueMetrics()
        {
            incomingControlPackets.ResetMetrics();
            incomingDataPackets.ResetMetrics();
            Interlocked.Exchange(ref peakIncomingPacketCount, Interlocked.Read(ref incomingPacketCount));
            Interlocked.Exchange(ref peakIncomingPacketBytes, Interlocked.Read(ref incomingPacketBytes));
            Interlocked.Exchange(ref processedIncomingPacketCount, 0);
            Interlocked.Exchange(ref maxIncomingPacketProcessTicks, 0);
            Interlocked.Exchange(ref incomingControlRejectedPacketCount, 0);
            Interlocked.Exchange(ref incomingDataRejectedPacketCount, 0);
            ResetIncomingQueueTimingMetrics();
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
            if (HasIncomingPackets(incomingControlPackets)
                && Interlocked.CompareExchange(ref processingControlQueue, 1, 0) == 0)
            {
                ProcessControlQueueAsync().Forget();
            }

            if (HasIncomingPackets(incomingDataPackets)
                && Interlocked.CompareExchange(ref processingDataQueue, 1, 0) == 0)
            {
                ProcessDataQueueAsync().Forget();
            }
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
        /// <typeparam name="TRequest">RPC 请求类型。</typeparam>
        /// <typeparam name="TResponse">RPC 响应类型。</typeparam>
        /// <param name="request">需要发送的 RPC 请求。</param>
        /// <param name="timeoutSeconds">当前调用等待响应的秒数。</param>
        /// <returns>执行处理后的结果。</returns>
        public MTask<TResponse> CallAsync<TRequest, TResponse>(TRequest request, int timeoutSeconds = 10)
            where TRequest : IRpcRequest
            where TResponse : IRpcResponse
        {
            return CallAsync<TRequest, TResponse>(DefaultSessionId, request, timeoutSeconds);
        }

        /// <summary>
        /// 通过指定会话发送 RPC 请求并等待对应响应。
        /// </summary>
        /// <typeparam name="TRequest">RPC 请求类型。</typeparam>
        /// <typeparam name="TResponse">RPC 响应类型。</typeparam>
        /// <param name="sessionId">目标逻辑会话标识。</param>
        /// <param name="request">需要发送的 RPC 请求。</param>
        /// <param name="timeoutSeconds">当前调用等待响应的秒数。</param>
        /// <returns>执行处理后的结果。</returns>
        public async MTask<TResponse> CallAsync<TRequest, TResponse>(string sessionId, TRequest request, int timeoutSeconds = 10)
            where TRequest : IRpcRequest
            where TResponse : IRpcResponse
        {
            if (timeoutSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), timeoutSeconds, "RPC 超时秒数必须大于零。");
            }

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
            uint opcode = ResolveOpcode(request.GetType());
            uint responseOpcode = ResolveOpcode(typeof(TResponse));

            var tcs = new MTaskCompletionSource<object>();
            lock (pendingRpcLock)
            {
                pendingRpcs[rpcId] = new PendingRpc
                {
                    SessionId = sessionId,
                    ResponseType = typeof(TResponse),
                    ResponseOpcode = responseOpcode,
                    Tcs = tcs
                };
            }

            using var linkedCts = new CancellationTokenSource();
            linkedCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            using var registration = linkedCts.Token.Register(() =>
            {
                if (TryRemovePendingRpc(rpcId, out var pending))
                {
                    Exception ex = new TimeoutException($"RPC timeout. session:{sessionId} rpcId:{rpcId}");
                    pending.Tcs.TrySetException(ex);
                }
            });

            bool isLogEnabled = LogSwitch.EnableLog;
            string sendTime = null;
            if (isLogEnabled)
            {
                sendTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                LogSwitch.Info($"[{sendTime}] [{GetLogSide(session.SessionId)}] 发送RPC opcode:{opcode} rpcId:{rpcId} type:{request.GetType().FullName}");
            }
            if (isLogEnabled && LogSwitch.EnablePayloadLog)
            {
                byte[] payload = GetSerializer().Serialize(request);
                string payloadText = Encoding.UTF8.GetString(payload);
                LogSwitch.Info($"[{sendTime}] 发送RPC内容: {payloadText}");
            }
            byte[] body = BuildPacket(opcode, rpcId, request, out int bodyLength);
            try
            {
                await session.SendOwnedAsync(body, bodyLength);
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
            if (isLogEnabled && LogSwitch.EnablePayloadLog)
            {
                byte[] payload = GetSerializer().Serialize(message);
                string payloadText = Encoding.UTF8.GetString(payload);
                LogSwitch.Info($"[{sendTime}] 发送普通消息内容: {payloadText}");
            }
            byte[] body = BuildPacket(opcode, 0, message, out int bodyLength);
            await session.SendOwnedAsync(body, bodyLength);
        }

        /// <summary>
        /// 尝试将默认会话的高频普通消息放入非等待出站队列。
        /// </summary>
        /// <typeparam name="TMessage">需要发送的普通消息类型。</typeparam>
        /// <param name="message">需要发送的高频普通消息。</param>
        /// <returns>当前连接与队列接受或拒绝该消息的原因。</returns>
        public NetworkSendResult TrySend<TMessage>(TMessage message) where TMessage : INormalMessage
        {
            return TrySend(DefaultSessionId, message);
        }

        /// <summary>
        /// 尝试将指定会话的高频普通消息放入非等待出站队列。
        /// </summary>
        /// <typeparam name="TMessage">需要发送的普通消息类型。</typeparam>
        /// <param name="sessionId">目标逻辑会话标识。</param>
        /// <param name="message">需要发送的高频普通消息。</param>
        /// <returns>当前连接与队列接受或拒绝该消息的原因。</returns>
        public NetworkSendResult TrySend<TMessage>(string sessionId, TMessage message) where TMessage : INormalMessage
        {
            if (!TryGetSession(sessionId, out var session))
            {
                return NetworkSendResult.SessionNotFound;
            }

            if (!session.IsConnected)
            {
                return NetworkSendResult.Disconnected;
            }

            uint opcode = ResolveOpcode(message.GetType());
            byte[] body = BuildPacket(opcode, 0, message, out int bodyLength);
            return session.TrySendOwned(body, bodyLength);
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
            DrainIncomingPackets();
        }

        /// <summary>
        /// 在网络服务停止时归还两条入站固定队列中尚未交给主线程处理的缓冲区。
        /// </summary>
        private void DrainIncomingPackets()
        {
            lock (incomingQueueLock)
            {
                while (incomingControlPackets.TryDequeue(out NetworkIncomingPacket controlPacket, out _))
                {
                    ByteBufferPool.Shared.Return(controlPacket.Buffer);
                }

                while (incomingDataPackets.TryDequeue(out NetworkIncomingPacket dataPacket, out _))
                {
                    ByteBufferPool.Shared.Return(dataPacket.Buffer);
                }

                incomingSessionBudgets.Clear();
                Interlocked.Exchange(ref incomingPacketCount, 0);
                Interlocked.Exchange(ref incomingPacketBytes, 0);
            }
        }

        /// <summary>
        /// 释放由当前网络模块持有的独占执行器；主循环执行器由宿主管理，不在此释放。
        /// </summary>
        private void ReleaseNetworkExecutor()
        {
            ownedNetworkExecutor?.Dispose();
            ownedNetworkExecutor = null;
            networkExecutor = null;
        }

        /// <summary>
        /// 将正式消息直接封装到租用数组中；Protobuf 路径不创建独立正文数组。
        /// </summary>
        /// <typeparam name="TMessage">需要封装的协议消息类型。</typeparam>
        /// <param name="opcode">消息对应的稳定 opcode。</param>
        /// <param name="rpcId">普通消息为零的 RPC 关联标识。</param>
        /// <param name="message">需要写入包体的协议消息。</param>
        /// <param name="length">返回完整业务包有效长度。</param>
        /// <returns>由调用方转交会话发送器归还的完整业务包数组。</returns>
        private byte[] BuildPacket<TMessage>(uint opcode, long rpcId, TMessage message, out int length)
        {
            INetworkSerializer currentSerializer = GetSerializer();
            if (currentSerializer is ProtobufSerializer protobufSerializer)
            {
                int payloadLength = protobufSerializer.GetSerializedSize(message);
                length = 12 + payloadLength;
                byte[] buffer = ByteBufferPool.Shared.Rent(length);
                try
                {
                    protobufSerializer.SerializeInto(message, buffer, 12, payloadLength);
                    WritePacketHeader(buffer, opcode, rpcId);
                    return buffer;
                }
                catch
                {
                    ByteBufferPool.Shared.Return(buffer);
                    throw;
                }
            }

            byte[] payload = currentSerializer.Serialize(message);
            length = 12 + (payload?.Length ?? 0);
            byte[] fallbackBuffer = ByteBufferPool.Shared.Rent(length);
            try
            {
                WritePacketHeader(fallbackBuffer, opcode, rpcId);
                if (payload != null && payload.Length > 0)
                {
                    Buffer.BlockCopy(payload, 0, fallbackBuffer, 12, payload.Length);
                }

                return fallbackBuffer;
            }
            catch
            {
                ByteBufferPool.Shared.Return(fallbackBuffer);
                throw;
            }
        }

        /// <summary>
        /// 创建没有业务正文的控制包。
        /// </summary>
        /// <param name="opcode">控制包 opcode。</param>
        /// <param name="rpcId">控制包关联的 RPC 标识。</param>
        /// <returns>由调用方转交会话发送器归还的十二字节业务包数组。</returns>
        private byte[] BuildControlPacket(uint opcode, long rpcId)
        {
            byte[] buffer = ByteBufferPool.Shared.Rent(12);
            WritePacketHeader(buffer, opcode, rpcId);
            return buffer;
        }

        /// <summary>
        /// 将固定十二字节业务包头写入目标数组。
        /// </summary>
        /// <param name="buffer">至少包含十二字节容量的目标数组。</param>
        /// <param name="opcode">需要写入的 opcode。</param>
        /// <param name="rpcId">需要写入的 RPC 标识。</param>
        private static void WritePacketHeader(byte[] buffer, uint opcode, long rpcId)
        {
            NetBinaryCodec.WriteUInt32BE(buffer, 0, opcode);
            NetBinaryCodec.WriteInt64BE(buffer, 4, rpcId);
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
                    TouchPong(session.SessionId, rpcId);
                }
                return;
            }

            if (rpcId != 0 && TryRemovePendingRpc(session.SessionId, rpcId, out var pending))
            {
                try
                {
                    if (pending.ResponseOpcode != opcode)
                    {
                        throw new InvalidOperationException($"RPC 响应 Opcode 不匹配，期望:{pending.ResponseOpcode} 实际:{opcode}。");
                    }

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

            NetworkProtocolRegistry registry = GetProtocolRegistry();
            if (rpcId != 0 && registry.TryGetRpcHandler(opcode, out NetworkProtocolRegistry.RpcHandlerBinding rpcInfo))
            {
                if (!(GetSerializer().Deserialize(rpcInfo.Request.MessageType, payload) is IRpcRequest req))
                {
                    LogSwitch.Error($"RPC请求反序列化失败，类型:{rpcInfo.Request.MessageType.FullName}");
                    return;
                }

                req.RpcId = rpcId;

                IRpcResponse response = rpcInfo.Invoker.CreateResponse();
                if (response == null)
                {
                    LogSwitch.Error($"RPC响应实例创建失败，类型:{rpcInfo.Response.MessageType.FullName}");
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
                    if (isLogEnabled && LogSwitch.EnablePayloadLog)
                    {
                        byte[] respPayload = GetSerializer().Serialize(response);
                        string payloadText = Encoding.UTF8.GetString(respPayload);
                        LogSwitch.Info($"[{sendTime}] 发送RPC响应内容: {payloadText}");
                    }
                    byte[] packet = BuildPacket(respOpcode, rpcId, response, out int packetLength);
                    NetworkSendResult sendResult = session.TrySendReliableOwned(packet, packetLength);
                    if (sendResult != NetworkSendResult.Accepted)
                    {
                        LogSwitch.Error($"RPC响应未能进入可靠出站队列，opcode:{respOpcode} 会话:{session.SessionId} 原因:{sendResult}。");
                        session.Close();
                    }
                }
                catch (Exception ex)
                {
                    LogSwitch.Error($"RPC响应发送异常，opcode:{respOpcode} 会话:{session.SessionId} 错误:{ex}");
                }
                return;
            }

            if (registry.TryGetMessageHandler(opcode, out NetworkProtocolRegistry.MessageHandlerBinding info))
            {
                if (!(GetSerializer().Deserialize(info.Message.MessageType, payload) is INormalMessage msg))
                {
                    LogSwitch.Error($"普通消息反序列化失败，类型:{info.Message.MessageType.FullName}");
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
            long pingTimestamp = Stopwatch.GetTimestamp();
            byte[] body = BuildControlPacket(PingOpcode, pingTimestamp);
            await session.SendOwnedAsync(body, 12);
        }

        /// <summary>
        /// 执行 SendPongAsync 相关处理。
        /// </summary>
        /// <param name="session">执行该方法所需的 session 参数。</param>
        /// <param name="rpcId">执行该方法所需的 rpcId 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private async MTask SendPongAsync(NetworkSession session, long rpcId)
        {
            byte[] body = BuildControlPacket(PongOpcode, rpcId);
            await session.SendOwnedAsync(body, 12);
        }

        /// <summary>
        /// 执行 TouchPong 相关处理。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="pingTimestamp">服务端原样返回的客户端 Ping 单调时钟时间戳。</param>
        private void TouchPong(string sessionId, long pingTimestamp)
        {
            if (heartbeatStates.TryGetValue(sessionId, out var state))
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                state.LastPongTicks = now;
                long elapsedTicks = Stopwatch.GetTimestamp() - pingTimestamp;
                if (pingTimestamp > 0 && elapsedTicks >= 0)
                {
                    long elapsedMilliseconds = elapsedTicks * 1000L / Stopwatch.Frequency;
                    int rtt = elapsedMilliseconds > int.MaxValue ? int.MaxValue : (int)elapsedMilliseconds;
                    Volatile.Write(ref state.LastRttMs, rtt);
                    if (state.MinRttWindowStartTicks == 0)
                    {
                        state.MinRttWindowStartTicks = now;
                        Volatile.Write(ref state.MinRttMs, rtt);
                    }
                    else if (now - state.MinRttWindowStartTicks > 10000)
                    {
                        state.MinRttWindowStartTicks = now;
                        Volatile.Write(ref state.MinRttMs, rtt);
                    }
                    else if (rtt < Volatile.Read(ref state.MinRttMs))
                    {
                        Volatile.Write(ref state.MinRttMs, rtt);
                    }

                    Volatile.Write(ref state.HasRtt, 1);
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
        /// 获取适用于所有传输协议的最近一次应用层心跳往返耗时。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="pingMs">执行该方法所需的 pingMs 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public bool TryGetLastPingMs(string sessionId, out int pingMs)
        {
            pingMs = 0;
            if (!heartbeatStates.TryGetValue(sessionId, out var state) || Volatile.Read(ref state.HasRtt) == 0)
            {
                return false;
            }
            pingMs = Volatile.Read(ref state.LastRttMs);
            return true;
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
            if (!heartbeatStates.TryGetValue(sessionId, out var state) || Volatile.Read(ref state.HasRtt) == 0)
            {
                return false;
            }
            pingMs = Volatile.Read(ref state.MinRttMs);
            return true;
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
                serializer = new ProtobufSerializer(GetProtocolRegistry());
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
            if (GetProtocolRegistry().TryGetOpcode(msgType, out uint opcode))
            {
                return opcode;
            }

            throw new InvalidOperationException($"消息 {msgType?.FullName ?? "<null>"} 未注册稳定 Opcode，请重新生成项目协议。");
        }

        /// <summary>
        /// 获取当前服务实例已经提交的不可变协议 Registry。
        /// </summary>
        /// <returns>当前网络服务使用的协议 Registry。</returns>
        private NetworkProtocolRegistry GetProtocolRegistry()
        {
            if (protocolRegistry == null)
            {
                throw new InvalidOperationException("网络服务尚未配置协议 Registry。");
            }

            return protocolRegistry;
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
            bool disconnectForCongestion = false;
            bool isControlPacket = IsIncomingControlPacket(session, data);
            lock (incomingQueueLock)
            {
                IncomingSessionBudget budget = GetOrCreateIncomingBudget(session.SessionId);
                if (!CanAcceptIncomingPacket(budget, length))
                {
                    disconnectForCongestion = MarkIncomingPacketRejected(budget, isControlPacket);
                }
                else
                {
                    FixedCapacityPacketQueue<NetworkIncomingPacket> queue = isControlPacket ? incomingControlPackets : incomingDataPackets;
                    if (CanAcceptGlobalIncomingPacket(queue, length))
                    {
                        byte[] buffer = ByteBufferPool.Shared.Rent(length);
                        try
                        {
                            data.Span.CopyTo(buffer);
                            var packet = new NetworkIncomingPacket
                            {
                                Session = session,
                                Buffer = buffer,
                                Length = length,
                                EnqueuedTicks = Volatile.Read(ref incomingTimingMetricsEnabled) != 0 ? Stopwatch.GetTimestamp() : 0
                            };
                            if (!queue.TryEnqueue(packet, length))
                            {
                                ByteBufferPool.Shared.Return(buffer);
                                disconnectForCongestion = MarkIncomingPacketRejected(budget, isControlPacket);
                            }
                            else
                            {
                                budget.PacketCount++;
                                budget.ByteCount += length;
                                budget.FullSinceTicks = 0;
                                long pendingCount = Interlocked.Increment(ref incomingPacketCount);
                                long pendingBytes = Interlocked.Add(ref incomingPacketBytes, length);
                                UpdateMaximum(ref peakIncomingPacketCount, pendingCount);
                                UpdateMaximum(ref peakIncomingPacketBytes, pendingBytes);
                            }
                        }
                        catch
                        {
                            ByteBufferPool.Shared.Return(buffer);
                            throw;
                        }
                    }
                    else
                    {
                        disconnectForCongestion = MarkIncomingPacketRejected(budget, isControlPacket);
                    }
                }
            }

            if (disconnectForCongestion)
            {
                session.Transport.Disconnect();
            }

            return MTask.CompletedTask;
        }

        /// <summary>
        /// 处理心跳与已匹配 RPC 响应；该通道不等待普通业务处理器结束。
        /// </summary>
        /// <returns>当前控制响应批次处理任务。</returns>
        private async MTask ProcessControlQueueAsync()
        {
            try
            {
                await ProcessQueueAsync(incomingControlPackets);
            }
            finally
            {
                Interlocked.Exchange(ref processingControlQueue, 0);
            }
        }

        /// <summary>
        /// 串行处理普通消息与 RPC 请求，保持现有业务状态修改顺序。
        /// </summary>
        /// <returns>当前普通业务批次处理任务。</returns>
        private async MTask ProcessDataQueueAsync()
        {
            try
            {
                await ProcessQueueAsync(incomingDataPackets);
            }
            finally
            {
                Interlocked.Exchange(ref processingDataQueue, 0);
            }
        }

        /// <summary>
        /// 在单次主循环预算内处理指定入站队列。
        /// </summary>
        /// <param name="queue">当前独占消费的入站队列。</param>
        /// <returns>当前队列批次处理任务。</returns>
        private async MTask ProcessQueueAsync(FixedCapacityPacketQueue<NetworkIncomingPacket> queue)
        {
            long updateStartedTicks = Stopwatch.GetTimestamp();
            int processedThisUpdate = 0;
            while (TryDequeueIncomingPacket(queue, out NetworkIncomingPacket packet))
            {
                RecordIncomingQueueWait(packet.EnqueuedTicks);
                long startedTicks = Stopwatch.GetTimestamp();
                try
                {
                    await HandleIncoming(packet.Session, new ReadOnlyMemory<byte>(packet.Buffer, 0, packet.Length));
                }
                finally
                {
                    long elapsedTicks = Stopwatch.GetTimestamp() - startedTicks;
                    Interlocked.Increment(ref processedIncomingPacketCount);
                    UpdateMaximum(ref maxIncomingPacketProcessTicks, elapsedTicks);
                    if (Volatile.Read(ref incomingTimingMetricsEnabled) != 0)
                    {
                        incomingPacketProcessHistogram.Record(elapsedTicks);
                    }
                    ByteBufferPool.Shared.Return(packet.Buffer);
                }

                processedThisUpdate++;
                if (ShouldYieldIncomingProcessing(processedThisUpdate, updateStartedTicks))
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 判断本次主循环入站派发是否已经达到包数量或时间预算。
        /// </summary>
        /// <param name="processedCount">本次主循环已经处理的包数量。</param>
        /// <param name="startedTicks">本次派发开始的 Stopwatch tick。</param>
        /// <returns>应将剩余积压留到下一次主循环时返回 true。</returns>
        private bool ShouldYieldIncomingProcessing(int processedCount, long startedTicks)
        {
            if (IncomingPacketBudgetPerUpdate > 0 && processedCount >= IncomingPacketBudgetPerUpdate)
            {
                return true;
            }

            if (IncomingTimeBudgetPerUpdate <= TimeSpan.Zero)
            {
                return false;
            }

            double elapsedSeconds = (Stopwatch.GetTimestamp() - startedTicks) / (double)Stopwatch.Frequency;
            return elapsedSeconds >= IncomingTimeBudgetPerUpdate.TotalSeconds;
        }

        /// <summary>
        /// 以无锁比较交换更新指定的最大值计数器。
        /// </summary>
        /// <param name="location">需要更新的最大值计数器。</param>
        /// <param name="candidate">本次观察到的候选值。</param>
        private static void UpdateMaximum(ref long location, long candidate)
        {
            long current = Interlocked.Read(ref location);
            while (candidate > current)
            {
                long observed = Interlocked.CompareExchange(ref location, candidate, current);
                if (observed == current)
                {
                    return;
                }

                current = observed;
            }
        }

        /// <summary>
        /// 记录一条已从入站队列取出的数据包等待时间。
        /// </summary>
        /// <param name="enqueuedTicks">该数据包进入队列时的 Stopwatch tick；零表示未启用采样。</param>
        private void RecordIncomingQueueWait(long enqueuedTicks)
        {
            if (enqueuedTicks == 0)
            {
                return;
            }

            long elapsedTicks = Stopwatch.GetTimestamp() - enqueuedTicks;
            Interlocked.Increment(ref incomingQueueWaitSampleCount);
            Interlocked.Add(ref totalIncomingQueueWaitTicks, elapsedTicks);
            UpdateMaximum(ref maxIncomingQueueWaitTicks, elapsedTicks);
            incomingQueueWaitHistogram.Record(elapsedTicks);
        }

        /// <summary>
        /// 清空入站队列等待耗时诊断，不影响当前已排队数据与拒绝计数。
        /// </summary>
        private void ResetIncomingQueueTimingMetrics()
        {
            Interlocked.Exchange(ref incomingQueueWaitSampleCount, 0);
            Interlocked.Exchange(ref totalIncomingQueueWaitTicks, 0);
            Interlocked.Exchange(ref maxIncomingQueueWaitTicks, 0);
            incomingQueueWaitHistogram.Reset();
            incomingPacketProcessHistogram.Reset();
        }

        /// <summary>
        /// 将 Stopwatch tick 换算为毫秒。
        /// </summary>
        /// <param name="ticks">需要换算的 Stopwatch tick 数。</param>
        /// <returns>对应的毫秒数。</returns>
        private static double ToMilliseconds(long ticks)
        {
            return ticks * 1000d / Stopwatch.Frequency;
        }

        /// <summary>
        /// 将累计 Stopwatch tick 换算为平均毫秒。
        /// </summary>
        /// <param name="totalTicks">累计 Stopwatch tick。</param>
        /// <param name="sampleCount">参与累计的样本数量。</param>
        /// <returns>没有样本时为零，否则返回平均毫秒。</returns>
        private static double ToAverageMilliseconds(long totalTicks, long sampleCount)
        {
            return sampleCount <= 0 ? 0d : ToMilliseconds(totalTicks) / sampleCount;
        }

        /// <summary>
        /// 判断指定入站固定队列中是否仍有待主线程处理的数据包。
        /// </summary>
        /// <param name="queue">需要检查的入站队列。</param>
        /// <returns>存在待处理数据包时返回 true。</returns>
        private bool HasIncomingPackets(FixedCapacityPacketQueue<NetworkIncomingPacket> queue)
        {
            lock (incomingQueueLock)
            {
                return queue.TryPeek(out _, out _);
            }
        }

        /// <summary>
        /// 从指定入站队列取包，并同步回收全局与会话预算。
        /// </summary>
        /// <param name="queue">当前独占消费的入站队列。</param>
        /// <param name="packet">成功时返回需要在主线程处理的数据包。</param>
        /// <returns>存在可处理数据包时返回 true。</returns>
        private bool TryDequeueIncomingPacket(
            FixedCapacityPacketQueue<NetworkIncomingPacket> queue,
            out NetworkIncomingPacket packet)
        {
            lock (incomingQueueLock)
            {
                if (!queue.TryDequeue(out packet, out _))
                {
                    packet = default;
                    return false;
                }

                Interlocked.Decrement(ref incomingPacketCount);
                Interlocked.Add(ref incomingPacketBytes, -packet.Length);
                if (packet.Session != null && incomingSessionBudgets.TryGetValue(packet.Session.SessionId, out IncomingSessionBudget budget))
                {
                    budget.PacketCount--;
                    budget.ByteCount -= packet.Length;
                    if (budget.PacketCount == 0)
                    {
                        budget.FullSinceTicks = 0;
                        incomingSessionBudgets.Remove(packet.Session.SessionId);
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// 判断当前入站包是否应进入不会被业务处理器阻塞的控制响应队列。
        /// </summary>
        /// <param name="session">收到当前包的逻辑会话。</param>
        /// <param name="data">完整业务包数据。</param>
        /// <returns>Ping、Pong 或已登记待完成 RPC 的匹配响应时返回 true。</returns>
        private bool IsIncomingControlPacket(NetworkSession session, ReadOnlyMemory<byte> data)
        {
            if (data.Length < 12)
            {
                return false;
            }

            uint opcode = NetBinaryCodec.ReadUInt32BE(data.Span, 0);
            if (opcode == PingOpcode || opcode == PongOpcode)
            {
                return true;
            }

            long rpcId = NetBinaryCodec.ReadInt64BE(data.Span, 4);
            if (rpcId == 0 || session == null)
            {
                return false;
            }

            lock (pendingRpcLock)
            {
                return pendingRpcs.TryGetValue(rpcId, out PendingRpc pending)
                    && string.Equals(pending.SessionId, session.SessionId, StringComparison.Ordinal)
                    && pending.ResponseOpcode == opcode;
            }
        }

        /// <summary>
        /// 获取指定会话的入站预算记录；首次收包时才创建一次记录。
        /// </summary>
        /// <param name="sessionId">逻辑会话标识。</param>
        /// <returns>当前会话的可变预算记录。</returns>
        private IncomingSessionBudget GetOrCreateIncomingBudget(string sessionId)
        {
            if (!incomingSessionBudgets.TryGetValue(sessionId, out IncomingSessionBudget budget))
            {
                budget = new IncomingSessionBudget();
                incomingSessionBudgets.Add(sessionId, budget);
            }

            return budget;
        }

        /// <summary>
        /// 判断单会话预算是否还允许接收指定长度的数据包。
        /// </summary>
        /// <param name="budget">需要检查的会话预算。</param>
        /// <param name="length">即将接收的数据包有效字节数。</param>
        /// <returns>会话包数量与字节数均未达到上限时返回 true。</returns>
        private static bool CanAcceptIncomingPacket(IncomingSessionBudget budget, int length)
        {
            return budget.PacketCount < IncomingSessionMaximumPacketCount && length <= IncomingSessionMaximumByteCount - budget.ByteCount;
        }

        /// <summary>
        /// 判断指定优先级全局队列是否仍有可用固定槽位与字节预算。
        /// </summary>
        /// <param name="queue">需要检查的固定容量队列。</param>
        /// <param name="length">即将进入队列的数据包有效字节数。</param>
        /// <returns>队列允许该数据包进入时返回 true。</returns>
        private static bool CanAcceptGlobalIncomingPacket(FixedCapacityPacketQueue<NetworkIncomingPacket> queue, int length)
        {
            return queue.CanAccept(length);
        }

        /// <summary>
        /// 记录一次容量拒绝，并在同一会话持续满载三秒时请求主动断开。
        /// </summary>
        /// <param name="budget">需要更新的会话预算。</param>
        /// <param name="isControlPacket">本次拒绝是否属于 Ping、Pong 或 RPC 保留队列。</param>
        /// <returns>持续满载达到断开阈值时返回 true。</returns>
        private bool MarkIncomingPacketRejected(IncomingSessionBudget budget, bool isControlPacket)
        {
            if (isControlPacket)
            {
                Interlocked.Increment(ref incomingControlRejectedPacketCount);
            }
            else
            {
                Interlocked.Increment(ref incomingDataRejectedPacketCount);
            }

            long now = Stopwatch.GetTimestamp();
            if (budget.FullSinceTicks == 0)
            {
                budget.FullSinceTicks = now;
                return false;
            }

            return now - budget.FullSinceTicks >= IncomingCongestionDisconnectTicks;
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
            GetProtocolRegistry();
            if (sessionComponent != null)
            {
                return sessionComponent;
            }

            if (NetworkSessionServiceRegistry.TryResolve(out var resolved) && resolved != null)
            {
                sessionComponent = resolved;
                ConfigureSessionExecutor(sessionComponent);
                BindSessionServiceEvents();
                return sessionComponent;
            }

            sessionComponent = Global.GetOrAdd<NetworkSessionComponent>(this);
            ConfigureSessionExecutor(sessionComponent);

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
                ConfigureSessionExecutor(sessionComponent);
                BindSessionServiceEvents();
                service = sessionComponent;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// 将网络模块持有的执行器注入支持该能力的会话服务。
        /// </summary>
        /// <param name="service">需要创建传输、监听器和发送队列的会话服务。</param>
        private void ConfigureSessionExecutor(INetworkSessionService service)
        {
            if (service is INetworkExecutorConfigurable configurable)
            {
                configurable.SetNetworkExecutor(networkExecutor ?? MTaskExecutors.Unity);
            }
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
