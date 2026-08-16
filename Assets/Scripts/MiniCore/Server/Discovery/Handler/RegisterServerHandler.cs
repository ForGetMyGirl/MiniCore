using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Server
{
    /// <summary>
    /// 处理服务实例向 Coordinator 发起的注册请求。
    /// </summary>
    public sealed class RegisterServerHandler : ARpcHandler<RegisterServerRequest, RegisterServerResponse>
    {
        #region Public 公共成员

        /// <summary>
        /// 将服务实例登记为 Starting 并返回当前目录。
        /// </summary>
        public override MTask HandleAsync(NetworkSession session, RegisterServerRequest request, RegisterServerResponse response)
        {
            ServiceDiscoveryService discovery = GetCoordinatorDiscovery();
            try
            {
                response.MergeFrom(discovery.Register(request));
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 取得当前 Coordinator 服务发现实现。
        /// </summary>
        private ServiceDiscoveryService GetCoordinatorDiscovery()
        {
            return Global.GetService<IServiceDiscoveryService>(this) as ServiceDiscoveryService;
        }

        #endregion
    }
}
