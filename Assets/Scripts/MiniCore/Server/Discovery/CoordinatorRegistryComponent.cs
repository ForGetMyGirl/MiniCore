using System;
using System.Collections.Generic;
using MiniCore.Model;
using MiniCore.Protocol.Generated;

namespace MiniCore.Server
{
    /// <summary>
    /// 保存 Coordinator 当前服务目录、租约和轮询游标。
    /// </summary>
    public sealed class CoordinatorRegistryComponent : AComponent
    {
        #region Private 私有成员

        private const long LeaseMilliseconds = 15000; // 默认服务租约长度。
        private readonly object syncRoot = new object(); // 保护目录和轮询游标。
        private readonly Dictionary<string, RegisteredInstance> instances = new Dictionary<string, RegisteredInstance>(StringComparer.Ordinal); // 实例标识到注册记录。
        private readonly Dictionary<ServiceKind, int> roundRobinIndices = new Dictionary<ServiceKind, int>(); // 各服务种类的轮询游标。
        private long directoryRevision; // 单调递增目录修订号。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 注册或替换一个服务实例，并将其初始状态设为 Starting。
        /// </summary>
        /// <param name="request">服务实例注册请求。</param>
        /// <returns>包含当前目录快照的注册响应。</returns>
        public RegisterServerResponse Register(RegisterServerRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.InstanceId))
            {
                return new RegisterServerResponse { Code = 400, Msg = "instanceId 不能为空" };
            }

            List<ServiceKind> kinds = ResolveKinds(request);
            if (kinds.Count == 0)
            {
                return new RegisterServerResponse { Code = 400, Msg = "注册请求没有有效服务类型" };
            }

            lock (syncRoot)
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                instances[request.InstanceId] = new RegisteredInstance(
                    request.InstanceId,
                    kinds,
                    request.InnerHost,
                    request.InnerPort,
                    request.OuterWebSocketUrl,
                    ServiceLifecycleState.Starting,
                    now + LeaseMilliseconds);
                directoryRevision++;
                return CreateRegisterResponse();
            }
        }

        /// <summary>
        /// 续约一个已注册实例，并在目录有变化时返回新快照。
        /// </summary>
        /// <param name="request">心跳请求。</param>
        /// <returns>续约和目录变化响应。</returns>
        public ServerHeartbeatResponse Heartbeat(ServerHeartbeatRequest request)
        {
            lock (syncRoot)
            {
                PruneExpired(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                if (request == null || !instances.TryGetValue(request.InstanceId ?? string.Empty, out RegisteredInstance instance))
                {
                    return new ServerHeartbeatResponse { Code = 404, Msg = "服务实例尚未注册" };
                }

                instance.LeaseExpiresAtMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + LeaseMilliseconds;
                var response = new ServerHeartbeatResponse
                {
                    Code = 0,
                    LeaseMilliseconds = LeaseMilliseconds,
                    DirectoryRevision = directoryRevision
                };
                if (request.KnownDirectoryRevision != directoryRevision)
                {
                    response.ChangedServices.Add(CreateDirectorySnapshot());
                }

                return response;
            }
        }

        /// <summary>
        /// 更新一个实例的生命周期状态。
        /// </summary>
        /// <param name="request">状态更新请求。</param>
        /// <returns>状态更新结果。</returns>
        public SetServerStateResponse SetState(SetServerStateRequest request)
        {
            lock (syncRoot)
            {
                if (request == null || !instances.TryGetValue(request.InstanceId ?? string.Empty, out RegisteredInstance instance))
                {
                    return new SetServerStateResponse { Code = 404, Msg = "服务实例尚未注册" };
                }

                ServiceLifecycleState state = (ServiceLifecycleState)(int)request.State;
                if (state == ServiceLifecycleState.Unspecified)
                {
                    return new SetServerStateResponse { Code = 400, Msg = "服务状态无效" };
                }

                instance.State = state;
                instance.LeaseExpiresAtMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + LeaseMilliseconds;
                directoryRevision++;
                return new SetServerStateResponse { Code = 0, DirectoryRevision = directoryRevision };
            }
        }

        /// <summary>
        /// 使用每种服务独立的轮询游标选择一个 Ready 实例。
        /// </summary>
        /// <param name="kind">目标服务种类。</param>
        /// <param name="endpoint">成功时返回服务端点。</param>
        /// <returns>存在 Ready 实例时返回 true。</returns>
        public bool TryResolve(ServiceKind kind, out DiscoveredServiceEndpoint endpoint)
        {
            lock (syncRoot)
            {
                PruneExpired(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                var candidates = new List<RegisteredInstance>();
                foreach (RegisteredInstance instance in instances.Values)
                {
                    if (instance.State == ServiceLifecycleState.Ready && instance.Kinds.Contains(kind))
                    {
                        candidates.Add(instance);
                    }
                }

                if (candidates.Count == 0)
                {
                    endpoint = null;
                    return false;
                }

                candidates.Sort((left, right) => string.CompareOrdinal(left.InstanceId, right.InstanceId));
                roundRobinIndices.TryGetValue(kind, out int cursor);
                RegisteredInstance selected = candidates[cursor % candidates.Count];
                roundRobinIndices[kind] = (cursor + 1) % candidates.Count;
                endpoint = selected.ToEndpoint(kind, directoryRevision);
                return true;
            }
        }

        /// <summary>
        /// 取得当前目录的完整不可变快照。
        /// </summary>
        /// <returns>所有已注册服务端点。</returns>
        public List<DiscoveredServiceEndpoint> GetSnapshot()
        {
            lock (syncRoot)
            {
                PruneExpired(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                var result = new List<DiscoveredServiceEndpoint>();
                foreach (RegisteredInstance instance in instances.Values)
                {
                    for (int index = 0; index < instance.Kinds.Count; index++)
                    {
                        result.Add(instance.ToEndpoint(instance.Kinds[index], directoryRevision));
                    }
                }

                return result;
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 根据专用 Database 类型或 Unity DS Role 标志展开服务种类。
        /// </summary>
        /// <param name="request">注册请求。</param>
        /// <returns>当前实例提供的服务种类。</returns>
        private static List<ServiceKind> ResolveKinds(RegisterServerRequest request)
        {
            var result = new List<ServiceKind>(4);
            ServiceKind explicitKind = (ServiceKind)(int)request.ServiceKind;
            if (explicitKind == ServiceKind.Database)
            {
                result.Add(ServiceKind.Database);
                return result;
            }

            DedicatedServerRole roles = (DedicatedServerRole)request.Roles;
            AddRoleKind(roles, DedicatedServerRole.Coordinator, ServiceKind.Coordinator, result);
            AddRoleKind(roles, DedicatedServerRole.Lobby, ServiceKind.Lobby, result);
            AddRoleKind(roles, DedicatedServerRole.Match, ServiceKind.Match, result);
            AddRoleKind(roles, DedicatedServerRole.Game, ServiceKind.Game, result);
            return result;
        }

        /// <summary>
        /// 当 Role 标志存在时向结果加入对应服务种类。
        /// </summary>
        private static void AddRoleKind(DedicatedServerRole roles, DedicatedServerRole role, ServiceKind kind, List<ServiceKind> result)
        {
            if ((roles & role) != 0)
            {
                result.Add(kind);
            }
        }

        /// <summary>
        /// 创建注册成功响应。
        /// </summary>
        private RegisterServerResponse CreateRegisterResponse()
        {
            var response = new RegisterServerResponse
            {
                Code = 0,
                LeaseMilliseconds = LeaseMilliseconds,
                DirectoryRevision = directoryRevision
            };
            response.Services.Add(CreateDirectorySnapshot());
            return response;
        }

        /// <summary>
        /// 创建 Protobuf 服务目录快照。
        /// </summary>
        private List<ClusterServiceEndpoint> CreateDirectorySnapshot()
        {
            List<DiscoveredServiceEndpoint> endpoints = GetSnapshotWithoutLock();
            var result = new List<ClusterServiceEndpoint>(endpoints.Count);
            for (int index = 0; index < endpoints.Count; index++)
            {
                result.Add(ServiceDiscoveryProtocolMapper.ToProtocol(endpoints[index]));
            }

            return result;
        }

        /// <summary>
        /// 在调用方已经持有同步锁时创建目录快照。
        /// </summary>
        private List<DiscoveredServiceEndpoint> GetSnapshotWithoutLock()
        {
            var result = new List<DiscoveredServiceEndpoint>();
            foreach (RegisteredInstance instance in instances.Values)
            {
                for (int index = 0; index < instance.Kinds.Count; index++)
                {
                    result.Add(instance.ToEndpoint(instance.Kinds[index], directoryRevision));
                }
            }

            return result;
        }

        /// <summary>
        /// 移除已经超过心跳租约的服务实例。
        /// </summary>
        /// <param name="nowMilliseconds">当前 UTC 毫秒时间戳。</param>
        private void PruneExpired(long nowMilliseconds)
        {
            var expired = new List<string>();
            foreach (KeyValuePair<string, RegisteredInstance> pair in instances)
            {
                if (pair.Value.LeaseExpiresAtMilliseconds <= nowMilliseconds)
                {
                    expired.Add(pair.Key);
                }
            }

            for (int index = 0; index < expired.Count; index++)
            {
                instances.Remove(expired[index]);
                directoryRevision++;
            }
        }

        /// <summary>
        /// 保存一个进程注册出的一个或多个服务种类。
        /// </summary>
        private sealed class RegisteredInstance
        {
            /// <summary>
            /// 创建服务实例注册记录。
            /// </summary>
            public RegisteredInstance(string instanceId, List<ServiceKind> kinds, string innerHost, int innerPort, string outerWebSocketUrl, ServiceLifecycleState state, long leaseExpiresAtMilliseconds)
            {
                InstanceId = instanceId;
                Kinds = kinds;
                InnerHost = innerHost;
                InnerPort = innerPort;
                OuterWebSocketUrl = outerWebSocketUrl;
                State = state;
                LeaseExpiresAtMilliseconds = leaseExpiresAtMilliseconds;
            }

            public string InstanceId { get; }
            public List<ServiceKind> Kinds { get; }
            public string InnerHost { get; }
            public int InnerPort { get; }
            public string OuterWebSocketUrl { get; }
            public ServiceLifecycleState State { get; set; }
            public long LeaseExpiresAtMilliseconds { get; set; }

            /// <summary>
            /// 将注册记录转换为指定服务种类的目录端点。
            /// </summary>
            public DiscoveredServiceEndpoint ToEndpoint(ServiceKind kind, long revision)
            {
                return new DiscoveredServiceEndpoint(InstanceId, kind, InnerHost, InnerPort, OuterWebSocketUrl, State, revision);
            }
        }

        #endregion
    }
}
