namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 发送给可替换认证服务器的登录 JSON。
    /// </summary>
    public sealed class AuthenticationLoginRequest
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置账号。
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// 获取或设置明文密码；生产环境仅允许通过 HTTPS 发送。
        /// </summary>
        public string Password { get; set; }

        #endregion
    }
}
