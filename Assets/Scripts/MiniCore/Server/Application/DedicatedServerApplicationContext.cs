using MiniCore.Model;
using MiniCore.Service;

namespace MiniCore.Server
{
    /// <summary>
    /// 向 Dedicated Server 游戏业务暴露固定宿主已经准备好的运行状态。
    /// </summary>
    public sealed class DedicatedServerApplicationContext
    {
        #region Public 公共成员

        /// <summary>
        /// 获取当前部署副本启用的 Role。
        /// </summary>
        public DedicatedServerRole ActiveRoles { get; }

        /// <summary>
        /// 获取经过固定宿主校验的部署配置。
        /// </summary>
        public MiniCoreServerRuntimeConfig RuntimeConfig { get; }

        /// <summary>
        /// 获取已经开始监听的网络服务。
        /// </summary>
        public INetworkService Network { get; }

        /// <summary>
        /// 获取已经完成 Starting 注册的服务发现能力。
        /// </summary>
        public IServiceDiscoveryService ServiceDiscovery { get; }

        /// <summary>
        /// 创建不可变的业务启动上下文。
        /// </summary>
        /// <param name="runtimeConfig">经过校验的部署配置。</param>
        /// <param name="network">已配置协议并开始监听的网络服务。</param>
        /// <param name="serviceDiscovery">已完成 Starting 注册的服务发现能力。</param>
        internal DedicatedServerApplicationContext(
            MiniCoreServerRuntimeConfig runtimeConfig,
            INetworkService network,
            IServiceDiscoveryService serviceDiscovery)
        {
            RuntimeConfig = runtimeConfig ?? throw new System.ArgumentNullException(nameof(runtimeConfig));
            Network = network ?? throw new System.ArgumentNullException(nameof(network));
            ServiceDiscovery = serviceDiscovery ?? throw new System.ArgumentNullException(nameof(serviceDiscovery));
            ActiveRoles = runtimeConfig.ParseRoles();
        }

        #endregion
    }
}
