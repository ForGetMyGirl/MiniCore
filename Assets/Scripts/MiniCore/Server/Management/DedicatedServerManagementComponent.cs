using System;
using System.Net;
using MiniCore.Model;
using MiniCore.Service;
using MiniCore.Threading;
using Newtonsoft.Json;
using UnityEngine;

namespace MiniCore.Server
{
    /// <summary>
    /// 在 MiniCore 主循环中处理管理请求，避免后台线程直接读取或修改业务状态。
    /// </summary>
    internal sealed class DedicatedServerManagementComponent : AComponent
    {
        #region Private 私有成员

        private const int MaximumRequestsPerUpdate = 16; // 单帧最多处理的管理请求数。
        private DedicatedServerManagementServer server; // 回环 HTTP 服务器。
        private IDedicatedServerApplication application; // 热更新业务入口。
        private IServiceDiscoveryService discovery; // 服务发现状态。
        private MiniCoreServerRuntimeConfig config; // 当前实例配置。
        private bool drainRequested; // 是否已经请求业务停止接收新工作。
        private bool shutdownRequested; // 是否已经请求进程退出。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 绑定当前实例、业务和发现服务并启动回环监听。
        /// </summary>
        /// <param name="runtimeConfig">外部实例配置。</param>
        /// <param name="dedicatedServerApplication">热更新业务入口。</param>
        /// <param name="serviceDiscovery">服务发现状态。</param>
        internal void Initialize(
            MiniCoreServerRuntimeConfig runtimeConfig,
            IDedicatedServerApplication dedicatedServerApplication,
            IServiceDiscoveryService serviceDiscovery)
        {
            config = runtimeConfig ?? throw new ArgumentNullException(nameof(runtimeConfig));
            application = dedicatedServerApplication ?? throw new ArgumentNullException(nameof(dedicatedServerApplication));
            discovery = serviceDiscovery ?? throw new ArgumentNullException(nameof(serviceDiscovery));
            server = new DedicatedServerManagementServer(config.Management);
            server.Start();
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 在主线程处理有界数量的 HTTP 管理请求。
        /// </summary>
        protected override void Update()
        {
            for (int index = 0; index < MaximumRequestsPerUpdate && server != null && server.TryDequeue(out HttpListenerContext context); index++)
            {
                HandleRequest(context);
            }
        }

        /// <summary>
        /// 停止回环管理监听并解除业务引用。
        /// </summary>
        protected override void OnDispose()
        {
            server?.Dispose();
            server = null;
            application = null;
            discovery = null;
            config = null;
            base.OnDispose();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 根据固定路由处理状态、健康、Drain 和安全关闭。
        /// </summary>
        /// <param name="context">已认证 HTTP 请求。</param>
        private void HandleRequest(HttpListenerContext context)
        {
            string method = context.Request.HttpMethod;
            string path = context.Request.Url?.AbsolutePath ?? string.Empty;
            if (method == "GET" && path == "/v1/status")
            {
                RespondStatus(context);
                return;
            }

            if (method == "GET" && path == "/v1/health")
            {
                bool healthy = discovery.IsRegistered && discovery.CurrentState == ServiceLifecycleState.Ready;
                DedicatedServerManagementServer.Respond(context, healthy ? 200 : 503, JsonConvert.SerializeObject(new
                {
                    healthy,
                    registered = discovery.IsRegistered,
                    state = discovery.CurrentState.ToString(),
                    releaseVersion = config.ReleaseVersion
                }));
                return;
            }

            if (method == "POST" && path == "/v1/drain")
            {
                if (!drainRequested)
                {
                    drainRequested = true;
                    application.DrainParticipant?.BeginDrain();
                    discovery.ReportDrainingAsync().Forget();
                }

                DedicatedServerManagementServer.Respond(context, 202, "{\"accepted\":true}");
                return;
            }

            if (method == "GET" && path == "/v1/drain")
            {
                DedicatedServerDrainStatus status = CaptureDrainStatus();
                DedicatedServerManagementServer.Respond(context, 200, JsonConvert.SerializeObject(new
                {
                    drained = drainRequested && status.IsDrained,
                    status.ActiveWorkCount,
                    status.Blockers
                }));
                return;
            }

            if (method == "POST" && path == "/v1/shutdown")
            {
                DedicatedServerDrainStatus status = CaptureDrainStatus();
                if (!drainRequested || !status.IsDrained)
                {
                    DedicatedServerManagementServer.Respond(context, 409, JsonConvert.SerializeObject(new
                    {
                        error = "drain_not_complete",
                        status.ActiveWorkCount,
                        status.Blockers
                    }));
                    return;
                }

                DedicatedServerManagementServer.Respond(context, 202, "{\"accepted\":true}");
                if (!shutdownRequested)
                {
                    shutdownRequested = true;
                    ShutdownAsync().Forget();
                }

                return;
            }

            DedicatedServerManagementServer.Respond(context, 404, "{\"error\":\"not_found\"}");
        }

        /// <summary>
        /// 返回不包含凭据的完整实例状态。
        /// </summary>
        /// <param name="context">HTTP 请求。</param>
        private void RespondStatus(HttpListenerContext context)
        {
            DedicatedServerDrainStatus drainStatus = CaptureDrainStatus();
            DedicatedServerManagementServer.Respond(context, 200, JsonConvert.SerializeObject(new
            {
                environmentId = config.EnvironmentId,
                instanceId = config.InstanceId,
                releaseVersion = config.ReleaseVersion,
                controlProtocolVersion = config.ControlProtocolVersion,
                configVersion = config.ConfigVersion,
                configSha256 = config.ConfigSha256,
                roles = config.Roles,
                registered = discovery.IsRegistered,
                state = discovery.CurrentState.ToString(),
                drainRequested,
                drained = drainRequested && drainStatus.IsDrained,
                drainStatus.ActiveWorkCount,
                drainStatus.Blockers
            }));
        }

        /// <summary>
        /// 安全读取业务 Drain 状态；未提供参与者时视为无阻塞。
        /// </summary>
        /// <returns>业务 Drain 快照。</returns>
        private DedicatedServerDrainStatus CaptureDrainStatus()
        {
            return application.DrainParticipant?.CaptureDrainStatus() ?? DedicatedServerDrainStatus.Drained();
        }

        /// <summary>
        /// 完成框架 Draining 上报后从 Unity 主线程退出进程。
        /// </summary>
        /// <returns>退出流程任务。</returns>
        private async MTask ShutdownAsync()
        {
            await DedicatedServerHost.StopAsync();
            Application.Quit(0);
        }

        #endregion
    }
}
