using System.Text.Json.Serialization;

namespace MiniCore.Deploy.Core.Models;

/// <summary>
/// 描述 AuthenticationServer 或 DatabaseServer 使用的一组 MySQL 连接参数。
/// </summary>
public sealed class DatabaseConnectionDefinition
{
    #region Public 公共成员

    /// <summary>
    /// 获取或设置数据库主机名、内网地址或 RDS 地址。
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置数据库 TCP 端口。
    /// </summary>
    public int Port { get; set; } = 3306;

    /// <summary>
    /// 获取或设置数据库名称。
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置数据库登录账号。
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置仅保留在当前应用会话中的数据库密码。
    /// </summary>
    [JsonIgnore]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 MySQL 连接使用的 SSL 模式。
    /// </summary>
    public string SslMode { get; set; } = "Required";

    #endregion
}
