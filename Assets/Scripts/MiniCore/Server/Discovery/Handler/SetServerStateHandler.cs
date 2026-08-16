using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Server
{
    /// <summary>
    /// 处理服务实例 Starting、Ready 和 Draining 状态变更。
    /// </summary>
    public sealed class SetServerStateHandler : ARpcHandler<SetServerStateRequest, SetServerStateResponse>
    {
        #region Public 公共成员

        /// <summary>
        /// 更新目录状态并返回新的目录修订号。
        /// </summary>
        public override MTask HandleAsync(NetworkSession session, SetServerStateRequest request, SetServerStateResponse response)
        {
            ServiceDiscoveryService discovery = Global.GetService<IServiceDiscoveryService>(this) as ServiceDiscoveryService;
            try
            {
                response.MergeFrom(discovery.SetState(request));
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
