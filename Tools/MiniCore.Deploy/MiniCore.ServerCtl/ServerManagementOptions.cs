namespace MiniCore.ServerCtl;

/// <summary>
/// 描述 Dedicated Server 本地管理监听和 Token 文件。
/// </summary>
public sealed class ServerManagementOptions
{
    #region Public 公共成员

    /// <summary>
    /// 获取或设置管理监听主机；只允许回环地址。
    /// </summary>
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>
    /// 获取或设置管理端口。
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// 获取或设置服务器本地 Token 文件。
    /// </summary>
    public string TokenFile { get; set; } = string.Empty;

    #endregion
}
