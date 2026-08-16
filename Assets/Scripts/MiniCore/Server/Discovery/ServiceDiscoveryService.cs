using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Server
{
    /// <summary>
    /// 自动启动 DS 监听，并按 Role 选择 Coordinator 目录或普通服务注册行为。
    /// </summary>
    [AppService(
        "Dedicated Server 服务发现",
        typeof(IServiceDiscoveryService),
        Description = "负责 DS 注册、心跳、服务目录和 Ready 状态；Coordinator 只维护控制面。",
        RequiresServices = new[] { typeof(INetworkService) },
        RuntimeTargets = AppServiceRuntimeTargets.DedicatedServer,
        RequiredInDedicatedServer = true)]
    public sealed class ServiceDiscoveryService : AAppService, IServiceDiscoveryService
    {
        #region Private 私有成员

        private const string CoordinatorSessionId = "coordinator-control"; // 普通 DS 到 Coordinator 的固定会话标识。
        private const int CoordinatorRpcTimeoutSeconds = 3; // 控制面单次 RPC 超时秒数。
        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5); // Coordinator 连接探测超时。
        private static readonly TimeSpan DatabaseDiscoveryTimeout = TimeSpan.FromSeconds(30); // 必选数据库发现超时。
        private static readonly int[] ReconnectDelaySeconds = { 1, 2, 4, 8, 15 }; // Coordinator 断线重连退避秒数。
        private readonly object directoryLock = new object(); // 保护本地目录快照。
        private readonly object reconnectLock = new object(); // 保证同时只有一个 Coordinator 重连任务。
        private readonly Dictionary<ServiceKind, List<DiscoveredServiceEndpoint>> directory = new Dictionary<ServiceKind, List<DiscoveredServiceEndpoint>>(); // 服务种类到实例快照。
        private readonly Dictionary<ServiceKind, int> resolveCursors = new Dictionary<ServiceKind, int>(); // 本地轮询游标。
        private INetworkService network; // 当前进程网络服务。
        private MiniCoreServerRuntimeConfig config; // 当前 DS 部署配置。
        private CoordinatorRegistryComponent coordinatorRegistry; // Coordinator Role 专用目录组件。
        private MSharedTask<bool> reconnectTask; // 多个控制面调用共享的当前重连任务。
        private long directoryRevision; // 当前已应用目录修订号。
        private ServiceLifecycleState desiredState = ServiceLifecycleState.Starting; // 重连后需要恢复的服务状态。
        private bool initialized; // 是否完成监听与注册。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取当前 Dedicated Server 启用的 Role。
        /// </summary>
        public DedicatedServerRole ActiveRoles => DedicatedServerRuntimeContext.ActiveRoles;

        /// <summary>
        /// 取得网络依赖和已经在 AppService 前加载的 DS 配置。
        /// </summary>
        public override void Awake()
        {
            network = Global.GetService<INetworkService>(this);
            config = DedicatedServerRuntimeBootstrap.Current ?? throw new InvalidOperationException("服务发现启动前尚未加载 Dedicated Server 配置。");
        }

        /// <summary>
        /// 启动内外网监听，并创建 Coordinator 目录或向 Coordinator 注册 Starting。
        /// </summary>
        /// <returns>监听、注册和可选数据库发现完成任务。</returns>
        public async MTask InitializeAsync()
        {
            await network.StartTcpServerAsync(config.Listeners.InnerHost, config.Listeners.InnerPort);
            await network.StartWebSocketServerAsync(config.Listeners.OuterHost, config.Listeners.OuterPort, new WebSocketServerConfig
            {
                Path = config.Listeners.OuterPath
            });

            if ((ActiveRoles & DedicatedServerRole.Coordinator) != 0)
            {
                coordinatorRegistry = Global.GetOrAdd<CoordinatorRegistryComponent>(this);
                RegisterLocalCoordinatorInstance();
            }
            else
            {
                await EnsureRemoteRegistrationAsync();
                HeartbeatLoopAsync().Forget();
            }

            if (config.ParsePersistenceMode() == ServerPersistenceMode.Database)
            {
                await WaitForReadyDatabaseAsync();
            }

            initialized = true;
        }

        /// <summary>
        /// 在业务 GameStartup 完成后向 Coordinator 报告 Ready。
        /// </summary>
        /// <returns>状态已更新任务。</returns>
        public async MTask ReportReadyAsync()
        {
            await ReportStateAsync(ServiceLifecycleState.Ready, "报告 Ready");
        }

        /// <summary>
        /// 在计划停服前向 Coordinator 报告 Draining，使新发现请求不再选择当前实例。
        /// </summary>
        /// <returns>状态已更新任务。</returns>
        public async MTask ReportDrainingAsync()
        {
            await ReportStateAsync(ServiceLifecycleState.Draining, "报告 Draining");
        }

        /// <summary>
        /// 从本地快照轮询选择一个 Ready 服务实例。
        /// </summary>
        /// <param name="kind">目标服务种类。</param>
        /// <param name="endpoint">成功时返回可直连端点。</param>
        /// <returns>存在 Ready 实例时返回 true。</returns>
        public bool TryResolve(ServiceKind kind, out DiscoveredServiceEndpoint endpoint)
        {
            if (coordinatorRegistry != null)
            {
                return coordinatorRegistry.TryResolve(kind, out endpoint);
            }

            lock (directoryLock)
            {
                if (!directory.TryGetValue(kind, out List<DiscoveredServiceEndpoint> candidates) || candidates.Count == 0)
                {
                    endpoint = null;
                    return false;
                }

                resolveCursors.TryGetValue(kind, out int cursor);
                endpoint = candidates[cursor % candidates.Count];
                resolveCursors[kind] = (cursor + 1) % candidates.Count;
                return true;
            }
        }

        /// <summary>
        /// 由 Coordinator Handler 注册一个远程服务实例。
        /// </summary>
        internal RegisterServerResponse Register(RegisterServerRequest request)
        {
            EnsureCoordinator();
            return coordinatorRegistry.Register(request);
        }

        /// <summary>
        /// 由 Coordinator Handler 续约远程服务实例。
        /// </summary>
        internal ServerHeartbeatResponse Heartbeat(ServerHeartbeatRequest request)
        {
            EnsureCoordinator();
            return coordinatorRegistry.Heartbeat(request);
        }

        /// <summary>
        /// 由 Coordinator Handler 更新远程服务状态。
        /// </summary>
        internal SetServerStateResponse SetState(SetServerStateRequest request)
        {
            EnsureCoordinator();
            return coordinatorRegistry.SetState(request);
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 停止服务发现拥有的监听和 Coordinator 会话。
        /// </summary>
        protected override void OnDispose()
        {
            network?.DisconnectSession(CoordinatorSessionId);
            network?.StopWebSocketServer();
            network?.StopTcpServer();
            coordinatorRegistry = null;
            reconnectTask = null;
            network = null;
            config = null;
            lock (directoryLock)
            {
                directory.Clear();
                resolveCursors.Clear();
            }

            Global.ReleaseAll(this);
            base.OnDispose();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 报告一个受支持的服务生命周期状态并同步最新目录修订。
        /// </summary>
        /// <param name="state">要报告的生命周期状态。</param>
        /// <param name="operationName">失败信息中的操作名称。</param>
        /// <returns>Coordinator 已确认状态变化时完成。</returns>
        private async MTask ReportStateAsync(ServiceLifecycleState state, string operationName)
        {
            if (!initialized)
            {
                throw new InvalidOperationException($"服务发现尚未完成初始化，不能{operationName}。");
            }

            var request = new SetServerStateRequest
            {
                InstanceId = config.InstanceId,
                State = (ClusterServiceState)(int)state
            };
            desiredState = state;
            if (coordinatorRegistry == null)
            {
                try
                {
                    SetServerStateResponse remoteResponse = await network.CallAsync<SetServerStateRequest, SetServerStateResponse>(
                        CoordinatorSessionId,
                        request,
                        CoordinatorRpcTimeoutSeconds);
                    if (remoteResponse.Code == -1 || remoteResponse.Code == 404)
                    {
                        MarkRemoteDirectoryUnavailable();
                        await EnsureRemoteRegistrationAsync();
                        return;
                    }

                    EnsureSuccess(remoteResponse.Code, remoteResponse.Msg, operationName);
                    directoryRevision = remoteResponse.DirectoryRevision;
                    return;
                }
                catch (Exception exception) when (IsRecoverableCoordinatorException(exception))
                {
                    MarkRemoteDirectoryUnavailable();
                    await EnsureRemoteRegistrationAsync();
                    return;
                }
            }

            SetServerStateResponse response = coordinatorRegistry.SetState(request);
            EnsureSuccess(response.Code, response.Msg, operationName);
            directoryRevision = response.DirectoryRevision;
            ApplyDirectory(coordinatorRegistry.GetSnapshot());
        }

        /// <summary>
        /// 将包含 Coordinator 的当前进程直接登记到本地目录。
        /// </summary>
        private void RegisterLocalCoordinatorInstance()
        {
            RegisterServerResponse response = coordinatorRegistry.Register(CreateRegistrationRequest());
            EnsureSuccess(response.Code, response.Msg, "注册 Coordinator 本机实例");
            ApplyDirectory(response.Services);
            directoryRevision = response.DirectoryRevision;
        }

        /// <summary>
        /// 连接 Coordinator 内网端口，重新注册当前服务并恢复期望状态。
        /// </summary>
        /// <returns>注册与状态恢复完成任务。</returns>
        private async MTask ConnectAndRegisterAsync()
        {
            network.DisconnectSession(CoordinatorSessionId);
            bool connected = await network.ConnectTcpSessionAsync(
                CoordinatorSessionId,
                config.Coordinator.InnerHost,
                config.Coordinator.InnerPort,
                ConnectTimeout);
            if (!connected)
            {
                throw new CoordinatorConnectionException($"无法连接 Coordinator：{config.Coordinator.InnerHost}:{config.Coordinator.InnerPort}。");
            }

            RegisterServerResponse response;
            try
            {
                response = await network.CallAsync<RegisterServerRequest, RegisterServerResponse>(
                    CoordinatorSessionId,
                    CreateRegistrationRequest(),
                    CoordinatorRpcTimeoutSeconds);
            }
            catch (Exception exception) when (IsRecoverableCoordinatorException(exception))
            {
                throw new CoordinatorConnectionException("连接 Coordinator 后注册当前 Dedicated Server 失败。", exception);
            }

            if (response.Code == -1)
            {
                throw new CoordinatorConnectionException(response.Msg);
            }

            EnsureSuccess(response.Code, response.Msg, "注册 Dedicated Server");
            directoryRevision = response.DirectoryRevision;
            ApplyDirectory(response.Services);

            if (desiredState == ServiceLifecycleState.Starting)
            {
                return;
            }

            SetServerStateResponse stateResponse;
            try
            {
                stateResponse = await network.CallAsync<SetServerStateRequest, SetServerStateResponse>(
                    CoordinatorSessionId,
                    new SetServerStateRequest
                    {
                        InstanceId = config.InstanceId,
                        State = (ClusterServiceState)(int)desiredState
                    },
                    CoordinatorRpcTimeoutSeconds);
            }
            catch (Exception exception) when (IsRecoverableCoordinatorException(exception))
            {
                throw new CoordinatorConnectionException("重新注册后恢复 Dedicated Server 状态失败。", exception);
            }

            if (stateResponse.Code == -1 || stateResponse.Code == 404)
            {
                throw new CoordinatorConnectionException(stateResponse.Msg);
            }

            EnsureSuccess(stateResponse.Code, stateResponse.Msg, $"恢复 {desiredState}");
            directoryRevision = stateResponse.DirectoryRevision;
        }

        /// <summary>
        /// 获取或创建当前唯一的 Coordinator 重连任务。
        /// </summary>
        /// <returns>成功重新注册时返回 true。</returns>
        private async MTask<bool> EnsureRemoteRegistrationAsync()
        {
            MSharedTask<bool> currentTask;
            lock (reconnectLock)
            {
                currentTask = reconnectTask;
                if (currentTask == null)
                {
                    currentTask = ReconnectCoordinatorLoopAsync().Share();
                    reconnectTask = currentTask;
                }
            }

            try
            {
                return await currentTask;
            }
            finally
            {
                lock (reconnectLock)
                {
                    if (ReferenceEquals(reconnectTask, currentTask))
                    {
                        reconnectTask = null;
                    }
                }
            }
        }

        /// <summary>
        /// 使用一、二、四、八、十五秒退避持续恢复 Coordinator 注册。
        /// </summary>
        /// <returns>成功重新注册时返回 true。</returns>
        private async MTask<bool> ReconnectCoordinatorLoopAsync()
        {
            int retryIndex = 0;
            while (!IsDisposing && !IsDisposed)
            {
                try
                {
                    await ConnectAndRegisterAsync();
                    LogSwitch.Info($"Dedicated Server {config.InstanceId} 已连接 Coordinator 并恢复 {desiredState} 状态。");
                    return true;
                }
                catch (CoordinatorConnectionException exception)
                {
                    MarkRemoteDirectoryUnavailable();
                    int delaySeconds = ReconnectDelaySeconds[Math.Min(retryIndex, ReconnectDelaySeconds.Length - 1)];
                    retryIndex = Math.Min(retryIndex + 1, ReconnectDelaySeconds.Length - 1);
                    LogSwitch.Warning($"Dedicated Server {config.InstanceId} 的 Coordinator 连接失败，{delaySeconds} 秒后重试：{exception.Message}");
                    await MTask.Delay(delaySeconds * 1000);
                }
            }

            return false;
        }

        /// <summary>
        /// 创建当前 DS 的注册请求。
        /// </summary>
        private RegisterServerRequest CreateRegistrationRequest()
        {
            return new RegisterServerRequest
            {
                InstanceId = config.InstanceId,
                Roles = (uint)ActiveRoles,
                InnerHost = config.Advertised.InnerHost,
                InnerPort = config.Advertised.InnerPort,
                OuterWebSocketUrl = config.Advertised.OuterWebSocketUrl ?? string.Empty,
                ServiceKind = (ClusterServiceKind)(int)ServiceKind.Unspecified,
                ProtocolVersion = "1"
            };
        }

        /// <summary>
        /// 定期向 Coordinator 续约，并应用目录变更。
        /// </summary>
        private async MTask HeartbeatLoopAsync()
        {
            while (!IsDisposing && !IsDisposed)
            {
                await MTask.Delay(5000);
                if (IsDisposing || IsDisposed)
                {
                    return;
                }

                ServerHeartbeatResponse response;
                try
                {
                    response = await network.CallAsync<ServerHeartbeatRequest, ServerHeartbeatResponse>(CoordinatorSessionId, new ServerHeartbeatRequest
                    {
                        InstanceId = config.InstanceId,
                        KnownDirectoryRevision = directoryRevision
                    }, CoordinatorRpcTimeoutSeconds);
                }
                catch (Exception exception) when (IsRecoverableCoordinatorException(exception))
                {
                    MarkRemoteDirectoryUnavailable();
                    await EnsureRemoteRegistrationAsync();
                    continue;
                }

                if (response.Code == -1 || response.Code == 404)
                {
                    MarkRemoteDirectoryUnavailable();
                    await EnsureRemoteRegistrationAsync();
                    continue;
                }

                EnsureSuccess(response.Code, response.Msg, "续约 Dedicated Server");
                directoryRevision = response.DirectoryRevision;
                if (response.ChangedServices.Count > 0)
                {
                    ApplyDirectory(response.ChangedServices);
                }
            }
        }

        /// <summary>
        /// 在 Database 持久化模式下等待服务目录出现 Ready DatabaseServer。
        /// </summary>
        private async MTask WaitForReadyDatabaseAsync()
        {
            DateTime deadline = DateTime.UtcNow + DatabaseDiscoveryTimeout;
            while (DateTime.UtcNow < deadline)
            {
                if (TryResolve(ServiceKind.Database, out _))
                {
                    return;
                }

                if (coordinatorRegistry == null)
                {
                    ResolveInnerServiceResponse response;
                    try
                    {
                        response = await network.CallAsync<ResolveInnerServiceRequest, ResolveInnerServiceResponse>(CoordinatorSessionId, new ResolveInnerServiceRequest
                        {
                            ServiceKind = (ClusterServiceKind)(int)ServiceKind.Database
                        }, CoordinatorRpcTimeoutSeconds);
                    }
                    catch (Exception exception) when (IsRecoverableCoordinatorException(exception))
                    {
                        MarkRemoteDirectoryUnavailable();
                        await EnsureRemoteRegistrationAsync();
                        continue;
                    }

                    if (response.Code == -1)
                    {
                        MarkRemoteDirectoryUnavailable();
                        await EnsureRemoteRegistrationAsync();
                        continue;
                    }

                    if (response.Code == 0 && response.Endpoint != null)
                    {
                        CacheResolvedEndpoint(response.Endpoint);
                        return;
                    }
                }

                await MTask.Delay(1000);
            }

            throw new TimeoutException("persistenceMode=Database，但在 30 秒内没有发现 Ready DatabaseServer。");
        }

        /// <summary>
        /// 使用协议 DTO 替换本地服务目录快照。
        /// </summary>
        private void ApplyDirectory(IEnumerable<ClusterServiceEndpoint> services)
        {
            var endpoints = new List<DiscoveredServiceEndpoint>();
            foreach (ClusterServiceEndpoint service in services)
            {
                endpoints.Add(ServiceDiscoveryProtocolMapper.FromProtocol(service));
            }

            ApplyDirectory(endpoints);
        }

        /// <summary>
        /// 使用框架端点替换本地服务目录快照。
        /// </summary>
        private void ApplyDirectory(IEnumerable<DiscoveredServiceEndpoint> services)
        {
            lock (directoryLock)
            {
                directory.Clear();
                resolveCursors.Clear();
                foreach (DiscoveredServiceEndpoint endpoint in services)
                {
                    if (endpoint.State != ServiceLifecycleState.Ready)
                    {
                        continue;
                    }

                    if (!directory.TryGetValue(endpoint.Kind, out List<DiscoveredServiceEndpoint> list))
                    {
                        list = new List<DiscoveredServiceEndpoint>();
                        directory.Add(endpoint.Kind, list);
                    }

                    list.Add(endpoint);
                }
            }
        }

        /// <summary>
        /// 缓存一次按类型查询得到的端点，同时保留本地已有的其他服务种类。
        /// </summary>
        /// <param name="service">Coordinator 返回的单个 Ready 服务端点。</param>
        private void CacheResolvedEndpoint(ClusterServiceEndpoint service)
        {
            DiscoveredServiceEndpoint endpoint = ServiceDiscoveryProtocolMapper.FromProtocol(service);
            if (endpoint.State != ServiceLifecycleState.Ready)
            {
                return;
            }

            lock (directoryLock)
            {
                if (!directory.TryGetValue(endpoint.Kind, out List<DiscoveredServiceEndpoint> list))
                {
                    list = new List<DiscoveredServiceEndpoint>();
                    directory.Add(endpoint.Kind, list);
                }

                list.RemoveAll(candidate => string.Equals(candidate.InstanceId, endpoint.InstanceId, StringComparison.Ordinal));
                list.Add(endpoint);
                resolveCursors[endpoint.Kind] = 0;
            }
        }

        /// <summary>
        /// 断开失效控制会话并清空不可继续用于新连接的过期目录。
        /// </summary>
        private void MarkRemoteDirectoryUnavailable()
        {
            network?.DisconnectSession(CoordinatorSessionId);
            lock (directoryLock)
            {
                directory.Clear();
                resolveCursors.Clear();
            }
        }

        /// <summary>
        /// 判断异常是否属于可通过重新连接恢复的控制面故障。
        /// </summary>
        /// <param name="exception">待判断异常。</param>
        /// <returns>可以重新连接恢复时返回 true。</returns>
        private static bool IsRecoverableCoordinatorException(Exception exception)
        {
            if (exception is TimeoutException
                or IOException
                or SocketException
                or ObjectDisposedException
                or CoordinatorConnectionException)
            {
                return true;
            }

            if (exception is not InvalidOperationException)
            {
                return false;
            }

            string message = exception.Message ?? string.Empty;
            return message.IndexOf("not connected", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("尚未连接", StringComparison.Ordinal) >= 0
                || message.IndexOf("未连接", StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// 确认当前服务运行在 Coordinator Role。
        /// </summary>
        private void EnsureCoordinator()
        {
            if (coordinatorRegistry == null)
            {
                throw new InvalidOperationException("当前 Dedicated Server 不包含 Coordinator Role。");
            }
        }

        /// <summary>
        /// 将非零控制面错误码转换为启动或运行异常。
        /// </summary>
        private static void EnsureSuccess(int code, string message, string operation)
        {
            if (code != 0)
            {
                throw new InvalidOperationException($"{operation}失败：{message}（{code}）。");
            }
        }

        /// <summary>
        /// 表示可通过重新连接恢复的 Coordinator 控制连接异常。
        /// </summary>
        private sealed class CoordinatorConnectionException : Exception
        {
            /// <summary>
            /// 创建控制连接异常。
            /// </summary>
            /// <param name="message">异常说明。</param>
            public CoordinatorConnectionException(string message)
                : base(message)
            {
            }

            /// <summary>
            /// 创建包含底层原因的控制连接异常。
            /// </summary>
            /// <param name="message">异常说明。</param>
            /// <param name="innerException">底层网络异常。</param>
            public CoordinatorConnectionException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }

        #endregion
    }
}
