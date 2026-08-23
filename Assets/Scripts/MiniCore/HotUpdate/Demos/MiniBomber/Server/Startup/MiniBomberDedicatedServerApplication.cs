using System;
using MiniCore.Core;
using MiniCore.HotUpdate.Server;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Server;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 向固定 Dedicated Server 宿主提供 MiniBomber 协议、Handler 与 Role 业务启动逻辑。
    /// </summary>
    public sealed class MiniBomberDedicatedServerApplication : IDedicatedServerApplication, IDedicatedServerDrainParticipant
    {
        #region Private 私有成员

        private MiniBomberServerStartupComponent startup; // 当前业务启动和 Drain 状态入口。

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 获取 MiniBomber 业务安全停服参与者。
        /// </summary>
        public IDedicatedServerDrainParticipant DrainParticipant => this;

        /// <summary>
        /// 登记当前项目全部业务协议和 Role-aware Handler。
        /// </summary>
        /// <param name="builder">固定控制面已经开始装配的协议构建器。</param>
        /// <param name="activeRoles">当前部署副本启用的 Role。</param>
        public void RegisterProtocols(NetworkProtocolBuilder builder, ServerRoleMask activeRoles)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            BusinessClientProtocolRegistration.Register(builder);
            BusinessServerProtocolRegistration.Register(builder);
            ServerHotUpdateHandlerRegistration.Register(builder, activeRoles);
        }

        /// <summary>
        /// 根据固定宿主提供的 Role 和持久化配置启动 MiniBomber 服务端业务组件。
        /// </summary>
        /// <param name="context">固定宿主运行上下文。</param>
        /// <returns>MiniBomber 业务启动完成任务。</returns>
        public async MTask StartAsync(DedicatedServerApplicationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            startup = Global.Pin<MiniBomberServerStartupComponent>();
            await startup.InitializeAsync(context);
        }

        /// <summary>
        /// 停止 MiniBomber 各业务 Role 接收新的房间、比赛和匹配请求。
        /// </summary>
        public void BeginDrain()
        {
            startup?.BeginDrain();
        }

        /// <summary>
        /// 汇总当前玩家、房间、比赛和匹配队列阻塞项。
        /// </summary>
        /// <returns>MiniBomber Drain 快照。</returns>
        public DedicatedServerDrainStatus CaptureDrainStatus()
        {
            return startup?.CaptureDrainStatus() ?? DedicatedServerDrainStatus.Drained();
        }

        #endregion
    }
}
