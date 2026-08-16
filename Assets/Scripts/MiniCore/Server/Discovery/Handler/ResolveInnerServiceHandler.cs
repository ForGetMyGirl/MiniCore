using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Server
{
    /// <summary>
    /// 处理服务端通过 Coordinator 查询另一个服务的内网端点。
    /// </summary>
    public sealed class ResolveInnerServiceHandler : ARpcHandler<ResolveInnerServiceRequest, ResolveInnerServiceResponse>
    {
        #region Public 公共成员

        /// <summary>
        /// 轮询选择一个 Ready 实例并返回其完整端点。
        /// </summary>
        public override MTask HandleAsync(NetworkSession session, ResolveInnerServiceRequest request, ResolveInnerServiceResponse response)
        {
            IServiceDiscoveryService discovery = Global.GetService<IServiceDiscoveryService>(this);
            try
            {
                if (discovery.TryResolve((ServiceKind)(int)request.ServiceKind, out DiscoveredServiceEndpoint endpoint))
                {
                    response.Code = 0;
                    response.Endpoint = ServiceDiscoveryProtocolMapper.ToProtocol(endpoint);
                }
                else
                {
                    response.Code = 404;
                    response.Msg = "没有 Ready 的目标服务";
                }

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
