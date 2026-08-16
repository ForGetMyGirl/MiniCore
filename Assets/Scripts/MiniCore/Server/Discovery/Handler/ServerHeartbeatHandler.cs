using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Server
{
    /// <summary>
    /// 处理服务实例向 Coordinator 发起的租约续期。
    /// </summary>
    public sealed class ServerHeartbeatHandler : ARpcHandler<ServerHeartbeatRequest, ServerHeartbeatResponse>
    {
        #region Public 公共成员

        /// <summary>
        /// 延长实例租约并返回目录增量快照。
        /// </summary>
        public override MTask HandleAsync(NetworkSession session, ServerHeartbeatRequest request, ServerHeartbeatResponse response)
        {
            ServiceDiscoveryService discovery = Global.GetService<IServiceDiscoveryService>(this) as ServiceDiscoveryService;
            try
            {
                response.MergeFrom(discovery.Heartbeat(request));
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }

        #endregion
    }
}
