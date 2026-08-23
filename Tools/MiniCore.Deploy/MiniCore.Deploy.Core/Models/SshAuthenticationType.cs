namespace MiniCore.Deploy.Core.Models;

/// <summary>
/// 定义部署工具连接目标主机时使用的 SSH 身份认证方式。
/// </summary>
public enum SshAuthenticationType
{
    /// <summary>
    /// 使用本机私钥文件完成认证。
    /// </summary>
    PrivateKey,

    /// <summary>
    /// 使用仅保存在当前应用进程内的密码完成认证。
    /// </summary>
    Password
}
