using MiniCore.Model;
using MiniCore.Threading;

namespace MiniCore.Service
{
    /// <summary>
    /// 提供 Dedicated Server 自动注册、心跳和服务目录查询能力。
    /// </summary>
    public interface IServiceDiscoveryService : IAppService, IAsyncAppService
    {
        /// <summary>
        /// 获取当前 Dedicated Server 的活动 Role。
        /// </summary>
        DedicatedServerRole ActiveRoles { get; }

        /// <summary>
        /// 在业务启动完成后把当前服务状态切换为 Ready。
        /// </summary>
        /// <returns>Coordinator 已确认状态变化时完成。</returns>
        MTask ReportReadyAsync();

        /// <summary>
        /// 在计划停服、摘流量或进程退出前把当前服务状态切换为 Draining。
        /// </summary>
        /// <returns>Coordinator 已确认状态变化时完成。</returns>
        MTask ReportDrainingAsync();

        /// <summary>
        /// 尝试从本地目录快照取得一个 Ready 服务实例。
        /// </summary>
        /// <param name="kind">目标服务种类。</param>
        /// <param name="endpoint">成功时返回可直连端点。</param>
        /// <returns>存在 Ready 实例时返回 true。</returns>
        bool TryResolve(ServiceKind kind, out DiscoveredServiceEndpoint endpoint);
    }
}
