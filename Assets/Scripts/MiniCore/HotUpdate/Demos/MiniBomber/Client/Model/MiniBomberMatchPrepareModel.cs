namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 客户端加载战斗场景所需的协议无关参数。
    /// </summary>
    public sealed class MiniBomberMatchPrepareModel
    {
        #region Public 公共成员

        /// <summary>
        /// 获取房间标识。
        /// </summary>
        public long RoomId { get; internal set; }

        /// <summary>
        /// 获取比赛标识。
        /// </summary>
        public long MatchId { get; internal set; }

        /// <summary>
        /// 获取战斗场景资源地址。
        /// </summary>
        public string BattleSceneAddress { get; internal set; } = string.Empty;

        /// <summary>
        /// 获取地图资源地址。
        /// </summary>
        public string MapAddress { get; internal set; } = string.Empty;

        /// <summary>
        /// 获取单局时长秒数。
        /// </summary>
        public int DurationSeconds { get; internal set; }

        /// <summary>
        /// 获取服务器随机种子。
        /// </summary>
        public int RandomSeed { get; internal set; }

        /// <summary>
        /// 获取加载超时毫秒数。
        /// </summary>
        public int LoadingTimeoutMilliseconds { get; internal set; }

        #endregion
    }
}
