using System;

namespace MiniCore.Service
{
    /// <summary>
    /// 指定 AppService 可以参与装配的运行目标。
    /// </summary>
    [Flags]
    public enum AppServiceRuntimeTargets
    {
        /// <summary>
        /// 不在任何运行目标中装配。
        /// </summary>
        None = 0,

        /// <summary>
        /// 普通客户端 Player。
        /// </summary>
        Client = 1,

        /// <summary>
        /// Unity Dedicated Server Player。
        /// </summary>
        DedicatedServer = 2,

        /// <summary>
        /// 普通客户端与 Dedicated Server。
        /// </summary>
        All = Client | DedicatedServer
    }
}
