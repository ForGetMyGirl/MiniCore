namespace MiniCore.Server
{
    /// <summary>
    /// 指定 Dedicated Server 业务持久化依赖模式。
    /// </summary>
    public enum ServerPersistenceMode
    {
        /// <summary>
        /// 当前进程不使用 DatabaseServer。
        /// </summary>
        None = 0,

        /// <summary>
        /// 业务启动前必须发现 Ready 的 DatabaseServer。
        /// </summary>
        Database = 1
    }
}
