namespace AuthenticationServer.Contracts;

/// <summary>
/// 登录成功后下发身份、令牌和 Coordinator 动态入口。
/// </summary>
public sealed class LoginResponse
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

    /// <summary>
    /// 获取或设置账号对应的全局标识。
    /// </summary>
    public long AccountId { get; set; }

    /// <summary>
    /// 获取或设置玩家显示名。
    /// </summary>
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置业务会话令牌。
    /// </summary>
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置本次登录动态下发的 Coordinator 外网入口。
    /// </summary>
    public string CoordinatorWebSocketUrl { get; set; } = string.Empty;

    #endregion
}
