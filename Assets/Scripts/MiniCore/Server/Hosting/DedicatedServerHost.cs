using System;
using MiniCore.Core;
using MiniCore.Eventing;
using MiniCore.Model;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Server
{
    /// <summary>
    /// 执行所有游戏共用的 Dedicated Server 配置、网络、控制面和生命周期启动顺序。
    /// </summary>
    public static class DedicatedServerHost
    {
        #region Private 私有成员

        private static IServiceDiscoveryService activeDiscovery; // 当前固定宿主用于优雅摘流量的服务发现实例。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 启动固定 Dedicated Server 宿主，并在控制面进入 Starting 后调用游戏业务入口。
        /// </summary>
        /// <param name="application">当前游戏提供的热更新业务入口。</param>
        /// <returns>业务启动并向 Coordinator 报告 Ready 后完成的任务。</returns>
        public static async MTask StartAsync(IDedicatedServerApplication application)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            DedicatedServerRuntimeBootstrap.Prepare();
            MiniCoreServerRuntimeConfig runtimeConfig = DedicatedServerRuntimeBootstrap.Current;
            DedicatedServerRole activeRoles = DedicatedServerRuntimeContext.ActiveRoles;

            Global.RegisterAppModule<IApplicationEventBus, ApplicationEventBusModule>(string.Empty);
            NetworkService network = Global.RegisterAppService<INetworkService, NetworkService>(null);
            var protocolBuilder = new NetworkProtocolBuilder();
            ServerControlPlaneRegistration.Register(protocolBuilder, activeRoles);
            application.RegisterProtocols(protocolBuilder, activeRoles);
            network.ConfigureProtocol(protocolBuilder.Build());

            Global.RegisterAppService<IResourceService, YooAssetResourceService>(new YooAssetResourceServiceInitArgs());
            Global.RegisterAppService<IStoragePathService, StoragePathService>(new StoragePathServiceInitArgs
            {
                RelativePath = "MiniCoreDedicatedServer"
            });
            Global.RegisterAppService<ITelemetryService, LocalTelemetryFileService>(null);
            Global.RegisterAppService<ITimerService, TimerService>(null);

            ServiceDiscoveryService discovery = Global.RegisterAppService<IServiceDiscoveryService, ServiceDiscoveryService>(null);
            await discovery.InitializeAsync();
            activeDiscovery = discovery;

            var context = new DedicatedServerApplicationContext(runtimeConfig, network, discovery);
            await application.StartAsync(context);
            await discovery.ReportReadyAsync();
        }

        /// <summary>
        /// 在外部停服流程真正关闭进程前，将当前实例从可发现目录中摘除。
        /// </summary>
        /// <returns>未启动时立即完成；已启动时等待 Coordinator 确认 Draining。</returns>
        public static async MTask StopAsync()
        {
            IServiceDiscoveryService discovery = activeDiscovery;
            if (discovery == null)
            {
                return;
            }

            activeDiscovery = null;
            await discovery.ReportDrainingAsync();
        }

        #endregion
    }
}
