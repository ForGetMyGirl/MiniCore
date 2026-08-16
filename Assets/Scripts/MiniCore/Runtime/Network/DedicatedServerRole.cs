using System;

namespace MiniCore.Model
{
    /// <summary>
    /// 描述同一 Dedicated Server 进程当前承载的业务角色。
    /// </summary>
    [Flags]
    public enum DedicatedServerRole
    {
        /// <summary>
        /// 不承载任何服务端角色。
        /// </summary>
        None = 0,

        /// <summary>
        /// 服务注册、发现与客户端目标服务查询。
        /// </summary>
        Coordinator = 1,

        /// <summary>
        /// 大厅与房间业务。
        /// </summary>
        Lobby = 2,

        /// <summary>
        /// 匹配业务。
        /// </summary>
        Match = 4,

        /// <summary>
        /// 权威游戏逻辑。
        /// </summary>
        Game = 8,

        /// <summary>
        /// 当前进程承载全部内置角色。
        /// </summary>
        All = Coordinator | Lobby | Match | Game
    }
}
