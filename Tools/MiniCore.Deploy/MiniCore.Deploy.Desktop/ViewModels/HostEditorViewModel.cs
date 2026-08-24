using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Desktop.ViewModels;

/// <summary>
/// 为主机模型补充认证方式切换、会话密码和指纹显示通知。
/// </summary>
public sealed class HostEditorViewModel : ObservableObject
{
    #region Private 私有成员

    private SshAuthenticationOptionViewModel selectedAuthenticationOption; // 当前认证方式选项。
    private string privateKeyPath; // 本机私钥文件路径。
    private string password; // 仅驻留当前应用会话的 SSH 密码。
    private string privateKeyPassphrase; // 仅驻留当前应用会话的私钥解密口令。
    private string hostKeyFingerprint; // 用户确认后固定的主机指纹。
    private string privateAddress; // 环境内服务互访使用的 VPC 地址。
    private string connectionTestStatus = "尚未测试"; // 远程命令与文件上传连接测试状态。
    private bool isConnectionTestRunning; // 当前是否正在测试连接。

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 获取底层主机配置。
    /// </summary>
    public HostDefinition Model { get; }

    /// <summary>
    /// 获取界面可选择的 SSH 认证方式。
    /// </summary>
    public IReadOnlyList<SshAuthenticationOptionViewModel> AuthenticationOptions { get; }

    /// <summary>
    /// 获取或设置当前选择的 SSH 认证方式。
    /// </summary>
    public SshAuthenticationOptionViewModel SelectedAuthenticationOption
    {
        get => selectedAuthenticationOption;
        set
        {
            if (value == null || ReferenceEquals(selectedAuthenticationOption, value))
            {
                return;
            }

            selectedAuthenticationOption = value;
            Model.AuthenticationType = value.Type;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsPrivateKeyAuthentication));
            RaisePropertyChanged(nameof(IsPasswordAuthentication));
        }
    }

    /// <summary>
    /// 获取当前是否使用本机 SSH 私钥文件。
    /// </summary>
    public bool IsPrivateKeyAuthentication => Model.AuthenticationType == SshAuthenticationType.PrivateKey;

    /// <summary>
    /// 获取当前是否使用当前会话密码。
    /// </summary>
    public bool IsPasswordAuthentication => Model.AuthenticationType == SshAuthenticationType.Password;

    /// <summary>
    /// 获取或设置本机 SSH 私钥文件路径。
    /// </summary>
    public string PrivateKeyPath
    {
        get => privateKeyPath;
        set
        {
            if (SetProperty(ref privateKeyPath, value))
            {
                Model.PrivateKeyPath = value;
            }
        }
    }

    /// <summary>
    /// 获取或设置只在当前应用进程中保留的 SSH 密码。
    /// </summary>
    public string Password
    {
        get => password;
        set
        {
            if (SetProperty(ref password, value))
            {
                Model.Password = value;
            }
        }
    }

    /// <summary>
    /// 获取或设置只在当前应用进程中保留的 SSH 私钥口令。
    /// </summary>
    public string PrivateKeyPassphrase
    {
        get => privateKeyPassphrase;
        set
        {
            if (SetProperty(ref privateKeyPassphrase, value))
            {
                Model.PrivateKeyPassphrase = value;
            }
        }
    }

    /// <summary>
    /// 获取或设置已经人工确认并固定保存的 SSH 主机指纹。
    /// </summary>
    public string HostKeyFingerprint
    {
        get => hostKeyFingerprint;
        set
        {
            if (SetProperty(ref hostKeyFingerprint, value))
            {
                Model.HostKeyFingerprint = value;
            }
        }
    }

    /// <summary>
    /// 获取或设置供实例默认继承的 VPC IP 或内网 DNS。
    /// </summary>
    public string PrivateAddress
    {
        get => privateAddress;
        set
        {
            if (SetProperty(ref privateAddress, value))
            {
                Model.PrivateAddress = value;
            }
        }
    }

    /// <summary>
    /// 获取当前主机最近一次远程命令与文件上传测试结果。
    /// </summary>
    public string ConnectionTestStatus
    {
        get => connectionTestStatus;
        private set => SetProperty(ref connectionTestStatus, value);
    }

    /// <summary>
    /// 获取当前是否允许再次发起连接测试。
    /// </summary>
    public bool CanTestConnection => !isConnectionTestRunning;

    /// <summary>
    /// 创建主机编辑模型并恢复持久化认证方式。
    /// </summary>
    /// <param name="model">底层主机配置。</param>
    public HostEditorViewModel(HostDefinition model)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        AuthenticationOptions = new[]
        {
            new SshAuthenticationOptionViewModel(SshAuthenticationType.PrivateKey, "SSH 私钥（推荐）"),
            new SshAuthenticationOptionViewModel(SshAuthenticationType.Password, "账号密码")
        };
        selectedAuthenticationOption = AuthenticationOptions[0].Type == model.AuthenticationType
            ? AuthenticationOptions[0]
            : AuthenticationOptions[1];
        privateKeyPath = model.PrivateKeyPath;
        password = model.Password;
        privateKeyPassphrase = model.PrivateKeyPassphrase;
        hostKeyFingerprint = model.HostKeyFingerprint;
        privateAddress = model.PrivateAddress;
    }

    /// <summary>
    /// 标记连接测试开始并禁用重复点击。
    /// </summary>
    public void BeginConnectionTest()
    {
        isConnectionTestRunning = true;
        ConnectionTestStatus = "正在测试远程命令与文件上传…";
        RaisePropertyChanged(nameof(CanTestConnection));
    }

    /// <summary>
    /// 记录连接测试完成状态并恢复按钮。
    /// </summary>
    /// <param name="succeeded">远程命令与文件上传通道是否都连接成功。</param>
    /// <param name="message">不包含凭证的结果说明。</param>
    public void CompleteConnectionTest(bool succeeded, string message)
    {
        isConnectionTestRunning = false;
        ConnectionTestStatus = succeeded ? "连接成功：远程命令与文件上传均可用" : "连接失败：" + message;
        RaisePropertyChanged(nameof(CanTestConnection));
    }

    #endregion
}
