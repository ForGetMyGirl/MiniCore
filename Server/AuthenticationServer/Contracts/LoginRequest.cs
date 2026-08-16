namespace AuthenticationServer.Contracts;

/// <summary>
/// 登录账号 HTTP JSON 请求。
/// </summary>
public sealed class LoginRequest
{
    #region Public 公共成员

    /// <summary>
    /// 获取或设置登录账号。
    /// </summary>
    public string Account { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置仅通过 HTTPS 传输的密码。
    /// </summary>
    public string Password { get; set; } = string.Empty;

    #endregion
}
