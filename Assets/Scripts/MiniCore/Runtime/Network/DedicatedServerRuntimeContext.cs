using System;

namespace MiniCore.Model
{
    /// <summary>
    /// 保存 Dedicated Server 在框架启动前完成解析的不可变运行上下文。
    /// 客户端始终保持未配置状态和空 Role。
    /// </summary>
    public static class DedicatedServerRuntimeContext
    {
        #region Public 公共成员

        /// <summary>
        /// 获取当前进程是否已经配置为 Dedicated Server。
        /// </summary>
        public static bool IsDedicatedServer { get; private set; }

        /// <summary>
        /// 获取当前 Dedicated Server 启用的 Role。
        /// </summary>
        public static DedicatedServerRole ActiveRoles { get; private set; }

        /// <summary>
        /// 在任何 AppService 和业务入口启动前设置 Dedicated Server Role。
        /// </summary>
        /// <param name="roles">配置文件解析出的 Role。</param>
        public static void Configure(DedicatedServerRole roles)
        {
            if (roles == DedicatedServerRole.None || (roles & ~DedicatedServerRole.All) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(roles), "Dedicated Server 必须启用至少一个已定义 Role。");
            }

            IsDedicatedServer = true;
            ActiveRoles = roles;
        }

        /// <summary>
        /// 清除运行上下文，仅供退出或编辑器重新进入运行模式时调用。
        /// </summary>
        public static void Reset()
        {
            IsDedicatedServer = false;
            ActiveRoles = DedicatedServerRole.None;
        }

        #endregion
    }
}
