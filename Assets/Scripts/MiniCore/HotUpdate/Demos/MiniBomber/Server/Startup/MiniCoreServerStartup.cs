using MiniCore.Demo.MiniBomber;
using MiniCore.Server;
using MiniCore.Threading;

namespace MiniCore.HotUpdate.Server
{
    /// <summary>
    /// Bootstrap 反射调用的 Dedicated Server 热更新薄入口。
    /// </summary>
    public static class MiniCoreServerStartup
    {
        #region Public 公共成员

        /// <summary>
        /// 将当前项目的 MiniBomber 业务入口交给固定 AOT 宿主。
        /// </summary>
        /// <returns>Dedicated Server 完整启动任务。</returns>
        public static MTask StartAsync()
        {
            return DedicatedServerHost.StartAsync(new MiniBomberDedicatedServerApplication());
        }

        /// <summary>
        /// 在部署系统计划停止 Dedicated Server 时先报告 Draining。
        /// </summary>
        /// <returns>Coordinator 已确认实例摘流量时完成。</returns>
        public static MTask StopAsync()
        {
            return DedicatedServerHost.StopAsync();
        }

        #endregion
    }
}
