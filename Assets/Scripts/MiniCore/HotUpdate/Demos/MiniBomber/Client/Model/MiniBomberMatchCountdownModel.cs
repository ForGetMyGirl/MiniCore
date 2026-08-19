namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 客户端统一比赛倒计时数据。
    /// </summary>
    public sealed class MiniBomberMatchCountdownModel
    {
        #region Public 公共成员

        /// <summary>
        /// 获取比赛标识。
        /// </summary>
        public long MatchId { get; internal set; }

        /// <summary>
        /// 获取服务器开始时间戳毫秒数。
        /// </summary>
        public long ServerStartTimestampMilliseconds { get; internal set; }

        /// <summary>
        /// 获取倒计时毫秒数。
        /// </summary>
        public int CountdownMilliseconds { get; internal set; }

        #endregion
    }
}
