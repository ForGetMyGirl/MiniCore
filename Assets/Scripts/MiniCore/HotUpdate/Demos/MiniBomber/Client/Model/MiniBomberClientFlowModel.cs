namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 客户端跨窗口与场景流程的长期业务数据。
    /// </summary>
    public sealed class MiniBomberClientFlowModel
    {
        #region Public 公共成员

        /// <summary>
        /// 获取当前客户端流程状态。
        /// </summary>
        public MiniBomberClientFlowState State { get; internal set; }

        /// <summary>
        /// 获取当前比赛准备参数。
        /// </summary>
        public MiniBomberMatchPrepareModel MatchPrepare { get; internal set; }

        /// <summary>
        /// 获取当前比赛倒计时。
        /// </summary>
        public MiniBomberMatchCountdownModel Countdown { get; internal set; }

        /// <summary>
        /// 获取当前重连尝试序号。
        /// </summary>
        public int ReconnectAttempt { get; internal set; }

        /// <summary>
        /// 获取下一次重连等待毫秒数。
        /// </summary>
        public int NextRetryMilliseconds { get; internal set; }

        /// <summary>
        /// 获取当前结构化流程提示。
        /// </summary>
        public MiniBomberClientFlowNotice Notice { get; internal set; }

        /// <summary>
        /// 获取服务器或传输返回的可选原始说明。
        /// </summary>
        public string Detail { get; internal set; } = string.Empty;

        #endregion
    }
}
