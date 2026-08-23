using MiniCore.Model;
using MiniCore.Protocol.Generated;

namespace MiniCore.Server
{
    /// <summary>
    /// 在框架服务目录模型与 Protobuf DTO 之间执行无状态转换。
    /// </summary>
    internal static class ServiceDiscoveryProtocolMapper
    {
        #region Internal 内部成员

        /// <summary>
        /// 将框架端点转换为协议 DTO。
        /// </summary>
        internal static ClusterServiceEndpoint ToProtocol(DiscoveredServiceEndpoint endpoint)
        {
            return new ClusterServiceEndpoint
            {
                InstanceId = endpoint.InstanceId ?? string.Empty,
                ServiceId = endpoint.ServiceId.Value,
                InnerHost = endpoint.InnerHost ?? string.Empty,
                InnerPort = endpoint.InnerPort,
                OuterWebSocketUrl = endpoint.OuterWebSocketUrl ?? string.Empty,
                State = (ClusterServiceState)(int)endpoint.State,
                DirectoryRevision = endpoint.DirectoryRevision
            };
        }

        /// <summary>
        /// 将协议 DTO 转换为框架端点。
        /// </summary>
        internal static DiscoveredServiceEndpoint FromProtocol(ClusterServiceEndpoint endpoint)
        {
            return new DiscoveredServiceEndpoint(
                endpoint.InstanceId,
                new ServiceId(endpoint.ServiceId),
                endpoint.InnerHost,
                endpoint.InnerPort,
                endpoint.OuterWebSocketUrl,
                (ServiceLifecycleState)(int)endpoint.State,
                endpoint.DirectoryRevision);
        }

        #endregion
    }
}
