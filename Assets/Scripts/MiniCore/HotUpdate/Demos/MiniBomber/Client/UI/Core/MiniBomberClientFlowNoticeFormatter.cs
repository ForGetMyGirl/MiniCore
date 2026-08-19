namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 将结构化客户端流程提示转换为界面文案。
    /// </summary>
    internal static class MiniBomberClientFlowNoticeFormatter
    {
        #region Internal 内部成员

        /// <summary>
        /// 格式化当前流程提示及其参数。
        /// </summary>
        /// <param name="notice">结构化提示类型。</param>
        /// <param name="attempt">重连尝试序号。</param>
        /// <param name="retryMilliseconds">下次重试等待毫秒数。</param>
        /// <param name="detail">服务器或传输补充说明。</param>
        /// <returns>用于界面展示的中文文案。</returns>
        internal static string Format(MiniBomberClientFlowNotice notice, int attempt, int retryMilliseconds, string detail)
        {
            switch (notice)
            {
                case MiniBomberClientFlowNotice.RestoringBattle:
                    return "正在恢复战斗...";
                case MiniBomberClientFlowNotice.EnteringRoom:
                    return "正在进入房间...";
                case MiniBomberClientFlowNotice.EnteringLobby:
                    return "正在进入大厅...";
                case MiniBomberClientFlowNotice.ReturningLogin:
                    return "正在返回登录界面...";
                case MiniBomberClientFlowNotice.LoadingBattle:
                    return "正在加载战斗场景...";
                case MiniBomberClientFlowNotice.SceneReadyFailed:
                    return string.IsNullOrWhiteSpace(detail) ? "战斗场景准备失败" : detail;
                case MiniBomberClientFlowNotice.Disconnected:
                    return "网络连接已断开";
                case MiniBomberClientFlowNotice.ReconnectWaiting:
                    return $"第 {attempt} 次重连失败，{retryMilliseconds / 1000f:0.0} 秒后重试";
                case MiniBomberClientFlowNotice.ReconnectTimedOut:
                    return "重连超时，正在返回登录界面";
                case MiniBomberClientFlowNotice.ReconnectFailed:
                    return string.IsNullOrWhiteSpace(detail) ? "重连失败" : detail;
                case MiniBomberClientFlowNotice.BattleLoadingTimedOut:
                    return "战斗加载超时，正在恢复流程";
                default:
                    return detail ?? string.Empty;
            }
        }

        #endregion
    }
}
