using System.Text.Json.Serialization;

namespace MiniCore.Deploy.Core.Models;

/// <summary>
/// 描述一个已经准备好 SSH 服务的目标主机。
/// </summary>
public sealed class HostDefinition
{
    #region Public 公共成员

    /// <summary>
    /// 获取或设置环境内唯一主机标识。
    /// </summary>
    public string HostId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置仅用于界面展示的主机用途说明。
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 SSH 主机名或地址。
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 SSH 端口。
    /// </summary>
    public int SshPort { get; set; } = 22;

    /// <summary>
    /// 获取或设置 SSH 用户名。
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 SSH 身份认证方式。
    /// </summary>
    public SshAuthenticationType AuthenticationType { get; set; } = SshAuthenticationType.PrivateKey;

    /// <summary>
    /// 获取或设置私钥文件路径；私钥内容不会进入配置。
    /// </summary>
    public string PrivateKeyPath { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前应用会话使用的 SSH 密码；不得写入配置文件或日志。
    /// </summary>
    [JsonIgnore]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置首次确认后固定的主机公钥指纹。
    /// </summary>
    public string HostKeyFingerprint { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置目标操作系统。
    /// </summary>
    public HostOperatingSystem OperatingSystem { get; set; }

    /// <summary>
    /// 获取或设置应用部署根目录。
    /// </summary>
    public string DeploymentRoot { get; set; } = "/opt/minicore";

    #endregion
}
