namespace MiniCore.Model
{
    /// <summary>
    /// 定义框架保留的控制面和可选基础设施服务标识。
    /// </summary>
    public static class FrameworkServiceIds
    {
        #region Public 公共成员

        /// <summary>
        /// Coordinator 控制面服务，与框架保留 Role 位一致。
        /// </summary>
        public const ulong Coordinator = ServerRoleMask.CoordinatorValue;

        /// <summary>
        /// 可选独立 DatabaseServer 服务，使用业务 Role 区域之外的最高位。
        /// </summary>
        public const ulong Database = 1UL << 63;

        #endregion
    }
}
