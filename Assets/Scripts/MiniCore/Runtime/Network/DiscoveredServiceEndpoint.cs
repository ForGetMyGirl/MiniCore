namespace MiniCore.Model
{
    /// <summary>
    /// 表示服务目录中一个可直连的服务实例快照。
    /// </summary>
    public sealed class DiscoveredServiceEndpoint
    {
        #region Public 公共成员

        /// <summary>
        /// 获取服务实例标识。
        /// </summary>
        public string InstanceId { get; }

        /// <summary>
        /// 获取服务种类。
        /// </summary>
        public ServiceId ServiceId { get; }

        /// <summary>
        /// 获取服务端内网主机。
        /// </summary>
        public string InnerHost { get; }

        /// <summary>
        /// 获取服务端内网端口。
        /// </summary>
        public int InnerPort { get; }

        /// <summary>
        /// 获取客户端可访问的外网 WebSocket 地址。
        /// </summary>
        public string OuterWebSocketUrl { get; }

        /// <summary>
        /// 获取当前生命周期状态。
        /// </summary>
        public ServiceLifecycleState State { get; }

        /// <summary>
        /// 获取产生当前快照的目录修订号。
        /// </summary>
        public long DirectoryRevision { get; }

        /// <summary>
        /// 创建服务端点快照。
        /// </summary>
        /// <param name="instanceId">集群内唯一实例标识。</param>
        /// <param name="serviceId">实例提供的稳定服务标识。</param>
        /// <param name="innerHost">服务间直连主机。</param>
        /// <param name="innerPort">服务间直连端口。</param>
        /// <param name="outerWebSocketUrl">客户端可访问地址。</param>
        /// <param name="state">实例生命周期状态。</param>
        /// <param name="directoryRevision">产生快照的目录修订号。</param>
        public DiscoveredServiceEndpoint(
            string instanceId,
            ServiceId serviceId,
            string innerHost,
            int innerPort,
            string outerWebSocketUrl,
            ServiceLifecycleState state,
            long directoryRevision)
        {
            InstanceId = instanceId;
            ServiceId = serviceId;
            InnerHost = innerHost;
            InnerPort = innerPort;
            OuterWebSocketUrl = outerWebSocketUrl;
            State = state;
            DirectoryRevision = directoryRevision;
        }

        #endregion
    }
}
