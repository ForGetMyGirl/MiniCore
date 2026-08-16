namespace AuthenticationServer.Contracts;

/// <summary>
/// 注册账号 HTTP JSON 响应。
/// </summary>
public sealed class RegisterResponse
{
    #region Public 公共成员

    /// <summary>
    /// 获取或设置业务结果码。
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// 获取或设置可读结果消息。
    /// </summary>
    public string Msg { get; set; } = string.Empty;

    #endregion
}
