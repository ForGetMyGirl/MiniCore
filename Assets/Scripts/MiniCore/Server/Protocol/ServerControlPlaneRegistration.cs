using System;
using MiniCore.Model;
using MiniCore.Protocol.Generated;

namespace MiniCore.Server
{
    /// <summary>
    /// 装配 Dedicated Server 固定 Coordinator 协议和控制面 Handler。
    /// </summary>
    internal static class ServerControlPlaneRegistration
    {
        #region Internal 内部成员

        /// <summary>
        /// 注册所有 DS 发起控制面调用所需的 Inner 协议，并为 Coordinator Role 增加外网查询与控制 Handler。
        /// </summary>
        /// <param name="builder">目标协议构建器。</param>
        /// <param name="activeRoles">当前进程启用的 Role。</param>
        internal static void Register(NetworkProtocolBuilder builder, DedicatedServerRole activeRoles)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            CoordinatorInnerProtocolRegistration.Register(builder);
            if ((activeRoles & DedicatedServerRole.Coordinator) == 0)
            {
                return;
            }

            CoordinatorOuterProtocolRegistration.Register(builder);
            builder.RegisterHandler(new RegisterServerHandler());
            builder.RegisterHandler(new ResolveInnerServiceHandler());
            builder.RegisterHandler(new ResolveServiceHandler());
            builder.RegisterHandler(new ServerHeartbeatHandler());
            builder.RegisterHandler(new SetServerStateHandler());
        }

        #endregion
    }
}
