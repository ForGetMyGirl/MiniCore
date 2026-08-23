using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Desktop.ViewModels;

/// <summary>
/// 表示一项可在主机编辑界面选择的 SSH 认证方式。
/// </summary>
public sealed class SshAuthenticationOptionViewModel
{
    #region Public 公共成员

    /// <summary>
    /// 获取持久化到主机配置的认证类型。
    /// </summary>
    public SshAuthenticationType Type { get; }

    /// <summary>
    /// 获取面向开发运维人员的中文名称。
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// 创建一项 SSH 认证方式。
    /// </summary>
    /// <param name="type">认证类型。</param>
    /// <param name="displayName">中文显示名称。</param>
    public SshAuthenticationOptionViewModel(SshAuthenticationType type, string displayName)
    {
        Type = type;
        DisplayName = displayName;
    }

    #endregion
}
