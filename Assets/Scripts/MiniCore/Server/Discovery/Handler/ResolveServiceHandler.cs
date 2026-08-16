using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Server
{
    /// <summary>
    /// 处理客户端向 Coordinator 查询 Lobby、Match 或 Game 外网端点。
    /// </summary>
    public sealed class ResolveServiceHandler : ARpcHandler<ResolveServiceRequest, ResolveServiceResponse>
    {
        #region Public 公共成员

        /// <summary>
        /// 轮询选择一个 Ready 实例并只返回客户端直连所需端点。
        /// </summary>
        public override MTask HandleAsync(NetworkSession session, ResolveServiceRequest request, ResolveServiceResponse response)
        {
            ServiceKind kind = (ServiceKind)(int)request.ServiceKind;
            IServiceDiscoveryService discovery = Global.GetService<IServiceDiscoveryService>(this);
            try
            {
                if (kind == ServiceKind.Database)
                {
                    response.Code = 403;
                    response.Msg = "客户端不能发现 DatabaseServer";
                }
                else if (discovery.TryResolve(kind, out DiscoveredServiceEndpoint endpoint) && !string.IsNullOrWhiteSpace(endpoint.OuterWebSocketUrl))
                {
                    response.Code = 0;
                    response.Endpoint = ServiceDiscoveryProtocolMapper.ToProtocol(endpoint);
                }
                else
                {
                    response.Code = 404;
                    response.Msg = "没有可供客户端连接的 Ready 服务";
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
