namespace AuthenticationServer.Contracts;

/// <summary>
/// 注册账号 HTTP JSON 请求。
/// </summary>
public sealed class RegisterRequest
{
    #region Public 公共成员

    /// <summary>
    /// 获取或设置待注册账号。
    /// </summary>
    public string Account { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置仅通过 HTTPS 传输的密码。
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置初始玩家显示名。
    /// </summary>
    public string PlayerName { get; set; } = string.Empty;

    #endregion
}
