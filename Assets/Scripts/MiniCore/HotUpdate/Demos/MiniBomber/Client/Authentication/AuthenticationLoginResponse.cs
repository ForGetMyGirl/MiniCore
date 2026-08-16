namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 认证服务器登录成功后返回的业务身份和 Coordinator 入口。
    /// </summary>
    public sealed class AuthenticationLoginResponse
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置业务错误码。
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// 获取或设置可展示消息。
        /// </summary>
        public string Msg { get; set; }

        /// <summary>
        /// 获取或设置账号对应的玩家标识。
        /// </summary>
        public long AccountId { get; set; }

        /// <summary>
        /// 获取或设置玩家显示名。
        /// </summary>
        public string PlayerName { get; set; }

        /// <summary>
        /// 获取或设置后续服务端 RPC 使用的会话令牌。
        /// </summary>
        public string SessionToken { get; set; }

        /// <summary>
        /// 获取或设置认证服务器动态下发的 Coordinator WebSocket 地址。
        /// </summary>
        public string CoordinatorWebSocketUrl { get; set; }

        #endregion
    }
}
