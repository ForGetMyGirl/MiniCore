namespace MiniCore.Model
{
    /// <summary>
    /// 描述一个服务实例是否可以接收新业务流量。
    /// </summary>
    public enum ServiceLifecycleState
    {
        /// <summary>
        /// 尚未指定状态。
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// 已注册但业务尚未完成启动。
        /// </summary>
        Starting = 1,

        /// <summary>
        /// 可以接收新流量。
        /// </summary>
        Ready = 2,

        /// <summary>
        /// 正在退出，只处理存量流量。
        /// </summary>
        Draining = 3
    }
}
