namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 认证服务器账号注册结果。
    /// </summary>
    public sealed class AuthenticationRegisterResponse
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

        #endregion
    }
}
