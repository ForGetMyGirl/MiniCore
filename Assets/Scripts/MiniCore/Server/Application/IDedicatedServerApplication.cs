using MiniCore.Model;
using MiniCore.Threading;

namespace MiniCore.Server
{
    /// <summary>
    /// 定义由固定 Dedicated Server 宿主调用的游戏业务入口。
    /// </summary>
    public interface IDedicatedServerApplication
    {
        /// <summary>
        /// 获取业务安全停服参与者；无长寿命业务时可以返回 null。
        /// </summary>
        IDedicatedServerDrainParticipant DrainParticipant { get; }

        /// <summary>
        /// 将当前游戏的协议与 Role-aware Handler 登记到框架构建器。
        /// </summary>
        /// <param name="builder">固定控制面已经开始装配的协议构建器。</param>
        /// <param name="activeRoles">当前部署副本启用的 Role。</param>
        void RegisterProtocols(NetworkProtocolBuilder builder, ServerRoleMask activeRoles);

        /// <summary>
        /// 在监听、Starting 注册和可选数据库发现完成后启动游戏业务。
        /// </summary>
        /// <param name="context">固定宿主提供的只读运行上下文。</param>
        /// <returns>游戏业务启动完成任务。</returns>
        MTask StartAsync(DedicatedServerApplicationContext context);
    }
}
