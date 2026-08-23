using System;
using MiniCore.Model;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 定义 MiniBomber 业务服务端角色；位值发布后不得复用或改变含义。
    /// </summary>
    [Flags]
    public enum MiniBomberServerRole : ulong
    {
        /// <summary>
        /// 不包含任何 MiniBomber 业务角色。
        /// </summary>
        None = 0UL,

        /// <summary>
        /// 大厅、房间与玩家会话业务。
        /// </summary>
        [ServerRoleDefinition("minibomber.lobby", "MiniBomber Lobby", ClientDiscoverable = true, PublicName = "Lobby")]
        Lobby = 1UL << 1,

        /// <summary>
        /// 全局或分片匹配队列业务。
        /// </summary>
        [ServerRoleDefinition("minibomber.match", "MiniBomber Match")]
        Match = 1UL << 2,

        /// <summary>
        /// 权威战斗模拟业务。
        /// </summary>
        [ServerRoleDefinition("minibomber.game", "MiniBomber Game")]
        Game = 1UL << 3,

        /// <summary>
        /// MiniBomber 当前全部业务角色。
        /// </summary>
        All = Lobby | Match | Game
    }
}
