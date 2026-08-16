namespace MiniCore.Model
{
    /// <summary>
    /// 框架服务目录中可被发现的服务种类。
    /// </summary>
    public enum ServiceKind
    {
        /// <summary>
        /// 未指定服务。
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// Coordinator 控制面服务。
        /// </summary>
        Coordinator = 1,

        /// <summary>
        /// 大厅服务。
        /// </summary>
        Lobby = 2,

        /// <summary>
        /// 匹配服务。
        /// </summary>
        Match = 3,

        /// <summary>
        /// 游戏服务。
        /// </summary>
        Game = 4,

        /// <summary>
        /// 独立数据库服务。
        /// </summary>
        Database = 5
    }
}
