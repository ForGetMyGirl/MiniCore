namespace AuthenticationServer.Models;

/// <summary>
/// 表示 AuthenticationServer 自己管理的账号记录。
/// </summary>
public sealed class AccountEntity
{
    #region Public 公共成员

    /// <summary>
    /// 获取或设置账号主键，同时作为游戏 AccountId。
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 获取或设置规范化账号。
    /// </summary>
    public string Account { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置玩家显示名。
    /// </summary>
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Base64 PBKDF2 盐。
    /// </summary>
    public string PasswordSalt { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Base64 PBKDF2 摘要。
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    #endregion
}
