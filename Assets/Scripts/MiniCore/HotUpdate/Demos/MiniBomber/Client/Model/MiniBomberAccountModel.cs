namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 客户端账号与会话的长期业务数据。
    /// </summary>
    public sealed class MiniBomberAccountModel
    {
        #region Public 公共成员

        /// <summary>
        /// 获取当前是否已经连接 Lobby。
        /// </summary>
        public bool IsConnected { get; internal set; }

        /// <summary>
        /// 获取当前是否持有有效认证会话。
        /// </summary>
        public bool IsAuthenticated => PlayerId > 0 && !string.IsNullOrEmpty(SessionToken);

        /// <summary>
        /// 获取当前玩家标识。
        /// </summary>
        public long PlayerId { get; internal set; }

        /// <summary>
        /// 获取当前玩家显示名。
        /// </summary>
        public string PlayerName { get; internal set; } = string.Empty;

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 获取或设置仅供账号组件持有的认证令牌。
        /// </summary>
        internal string SessionToken { get; set; } = string.Empty;

        #endregion
    }
}
