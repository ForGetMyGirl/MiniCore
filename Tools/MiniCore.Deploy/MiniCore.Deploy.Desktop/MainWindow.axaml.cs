using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MiniCore.Deploy.Desktop.ViewModels;
using MiniCore.Deploy.Infrastructure.Remote;

namespace MiniCore.Deploy.Desktop;

/// <summary>
/// 承载 MiniCore Deploy 配置界面和风险确认对话框。
/// </summary>
public sealed partial class MainWindow : Window
{
    #region Private 私有成员

    private readonly MainWindowViewModel viewModel; // 主窗口发布视图模型。
    private readonly SshHostKeyProbe hostKeyProbe = new(); // SSH 主机指纹探测器。
    private readonly SshRemoteClient sshRemoteClient = new(); // SSH/SFTP 连接测试器。

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 创建主窗口并在打开时加载仓库外配置。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);
        viewModel = new MainWindowViewModel();
        viewModel.ApprovalRequested += OnApprovalRequested;
        viewModel.ProfileDeletionRequested += OnProfileDeletionRequested;
        DataContext = viewModel;
        Opened += OnOpened;
    }

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 加载主窗口 XAML。
    /// </summary>
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 窗口首次显示后异步加载用户配置。
    /// </summary>
    /// <param name="sender">窗口事件源。</param>
    /// <param name="eventArgs">事件参数。</param>
    private async void OnOpened(object? sender, EventArgs eventArgs)
    {
        Opened -= OnOpened;
        await viewModel.InitializeAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// 根据左侧编号导航切换中间工作页面。
    /// </summary>
    /// <param name="sender">带页面索引 Tag 的导航项。</param>
    /// <param name="eventArgs">点击事件参数。</param>
    private void OnNavigationClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (sender is RadioButton navigation
            && int.TryParse(navigation.Tag?.ToString(), out int pageIndex))
        {
            viewModel.SelectedPageIndex = pageIndex;
        }
    }

    /// <summary>
    /// 从顶部下拉菜单切换为构建并发布模式。
    /// </summary>
    /// <param name="sender">菜单项。</param>
    /// <param name="eventArgs">点击事件参数。</param>
    private void OnBuildAndPublishModeClick(object? sender, RoutedEventArgs eventArgs)
    {
        viewModel.UseBuildAndPublishCommand.Execute(null);
        CloseExecutionModePopup();
    }

    /// <summary>
    /// 显式打开顶部执行模式菜单，避免平台默认下拉行为不一致。
    /// </summary>
    /// <param name="sender">下拉按钮。</param>
    /// <param name="eventArgs">点击事件参数。</param>
    private void OnExecutionMenuClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (sender is Control control)
        {
            Popup? popup = this.FindControl<Popup>("ExecutionModePopup");
            if (popup != null)
            {
                popup.PlacementTarget = control;
                popup.IsOpen = !popup.IsOpen;
            }
        }
    }

    /// <summary>
    /// 从顶部下拉菜单切换为只构建模式。
    /// </summary>
    /// <param name="sender">菜单项。</param>
    /// <param name="eventArgs">点击事件参数。</param>
    private void OnBuildOnlyModeClick(object? sender, RoutedEventArgs eventArgs)
    {
        viewModel.UseBuildOnlyCommand.Execute(null);
        CloseExecutionModePopup();
    }

    /// <summary>
    /// 从顶部下拉菜单切换为发布已有制品模式。
    /// </summary>
    /// <param name="sender">菜单项。</param>
    /// <param name="eventArgs">点击事件参数。</param>
    private void OnExistingArtifactsModeClick(object? sender, RoutedEventArgs eventArgs)
    {
        viewModel.UseExistingArtifactsCommand.Execute(null);
        CloseExecutionModePopup();
    }

    /// <summary>
    /// 关闭顶部执行模式弹层。
    /// </summary>
    private void CloseExecutionModePopup()
    {
        Popup? popup = this.FindControl<Popup>("ExecutionModePopup");
        if (popup != null)
        {
            popup.IsOpen = false;
        }
    }

    /// <summary>
    /// 选择 Unity 可执行程序。
    /// </summary>
    /// <param name="sender">路径选择按钮。</param>
    /// <param name="eventArgs">点击事件参数。</param>
    private async void OnPickUnityExecutableClick(object? sender, RoutedEventArgs eventArgs)
    {
        string? path = await PickFileAsync("选择 Unity 可执行程序").ConfigureAwait(true);
        if (!string.IsNullOrEmpty(path))
        {
            viewModel.UnityExecutablePath = path;
        }
    }

    /// <summary>
    /// 选择 Unity 项目根目录。
    /// </summary>
    /// <param name="sender">路径选择按钮。</param>
    /// <param name="eventArgs">点击事件参数。</param>
    private async void OnPickProjectDirectoryClick(object? sender, RoutedEventArgs eventArgs)
    {
        string? path = await PickFolderAsync("选择 Unity 项目根目录").ConfigureAwait(true);
        if (!string.IsNullOrEmpty(path))
        {
            viewModel.ProjectPath = path;
        }
    }

    /// <summary>
    /// 选择本地发布制品输出目录。
    /// </summary>
    /// <param name="sender">路径选择按钮。</param>
    /// <param name="eventArgs">点击事件参数。</param>
    private async void OnPickOutputDirectoryClick(object? sender, RoutedEventArgs eventArgs)
    {
        string? path = await PickFolderAsync("选择发布制品输出目录").ConfigureAwait(true);
        if (!string.IsNullOrEmpty(path))
        {
            viewModel.OutputPath = path;
        }
    }

    /// <summary>
    /// 选择客户端启动场景。
    /// </summary>
    /// <param name="sender">路径选择按钮。</param>
    /// <param name="eventArgs">点击事件参数。</param>
    private async void OnPickClientSceneClick(object? sender, RoutedEventArgs eventArgs)
    {
        string? path = await PickUnitySceneAsync("选择客户端启动场景").ConfigureAwait(true);
        if (!string.IsNullOrEmpty(path))
        {
            viewModel.ClientScenePath = path;
        }
    }

    /// <summary>
    /// 选择 Dedicated Server 启动场景。
    /// </summary>
    /// <param name="sender">路径选择按钮。</param>
    /// <param name="eventArgs">点击事件参数。</param>
    private async void OnPickServerSceneClick(object? sender, RoutedEventArgs eventArgs)
    {
        string? path = await PickUnitySceneAsync("选择 Dedicated Server 启动场景").ConfigureAwait(true);
        if (!string.IsNullOrEmpty(path))
        {
            viewModel.ServerScenePath = path;
        }
    }

    /// <summary>
    /// 选择当前主机使用的本地 SSH 私钥文件。
    /// </summary>
    /// <param name="sender">路径选择按钮。</param>
    /// <param name="eventArgs">点击事件参数。</param>
    private async void OnPickHostPrivateKeyClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (sender is not Control { DataContext: HostEditorViewModel hostEditor })
        {
            return;
        }

        string? path = await PickFileAsync("选择 SSH 私钥文件").ConfigureAwait(true);
        if (!string.IsNullOrEmpty(path))
        {
            hostEditor.PrivateKeyPath = path;
        }
    }

    /// <summary>
    /// 从 SSH 密钥交换读取主机指纹，并在用户核对后固定保存。
    /// </summary>
    /// <param name="sender">当前主机的指纹按钮。</param>
    /// <param name="eventArgs">点击事件参数。</param>
    private async void OnProbeHostFingerprintClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (sender is not Control { DataContext: HostEditorViewModel hostEditor })
        {
            return;
        }

        try
        {
            string fingerprint = await hostKeyProbe
                .GetFingerprintAsync(hostEditor.Model, CancellationToken.None)
                .ConfigureAwait(true);
            bool confirmed = await ShowFingerprintConfirmationAsync(hostEditor, fingerprint).ConfigureAwait(true);
            if (confirmed)
            {
                hostEditor.HostKeyFingerprint = fingerprint;
            }
        }
        catch (Exception exception)
        {
            await ShowErrorAsync("无法获取主机指纹", exception.Message).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// 使用当前地址、登录用户、认证方式和固定指纹验证远程命令与文件上传通道。
    /// </summary>
    /// <param name="sender">当前主机的测试按钮。</param>
    /// <param name="eventArgs">点击事件参数。</param>
    private async void OnTestHostConnectionClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (sender is not Control { DataContext: HostEditorViewModel hostEditor }
            || !hostEditor.CanTestConnection)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(hostEditor.HostKeyFingerprint))
        {
            hostEditor.CompleteConnectionTest(false, "请先获取并确认 SSH 主机指纹");
            return;
        }

        hostEditor.BeginConnectionTest();
        try
        {
            await sshRemoteClient
                .TestConnectionAsync(hostEditor.Model, CancellationToken.None)
                .ConfigureAwait(true);
            hostEditor.CompleteConnectionTest(true, string.Empty);
        }
        catch (Exception exception)
        {
            hostEditor.CompleteConnectionTest(false, exception.Message);
        }
    }

    /// <summary>
    /// 打开单文件选择器。
    /// </summary>
    /// <param name="title">选择器标题。</param>
    /// <param name="fileTypes">可选文件类型过滤器。</param>
    /// <returns>用户选择的本机绝对路径；取消时返回 null。</returns>
    private async Task<string?> PickFileAsync(string title, IReadOnlyList<FilePickerFileType>? fileTypes = null)
    {
        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = fileTypes
        }).ConfigureAwait(true);
        return files.Count == 0 ? null : files[0].Path.LocalPath;
    }

    /// <summary>
    /// 打开单目录选择器。
    /// </summary>
    /// <param name="title">选择器标题。</param>
    /// <returns>用户选择的本机绝对路径；取消时返回 null。</returns>
    private async Task<string?> PickFolderAsync(string title)
    {
        IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        }).ConfigureAwait(true);
        return folders.Count == 0 ? null : folders[0].Path.LocalPath;
    }

    /// <summary>
    /// 打开仅显示 Unity 场景文件的选择器。
    /// </summary>
    /// <param name="title">选择器标题。</param>
    /// <returns>用户选择的场景绝对路径；取消时返回 null。</returns>
    private Task<string?> PickUnitySceneAsync(string title)
    {
        var sceneType = new FilePickerFileType("Unity 场景")
        {
            Patterns = new[] { "*.unity" }
        };
        return PickFileAsync(title, new[] { sceneType });
    }

    /// <summary>
    /// 显示首次主机指纹核对窗口。
    /// </summary>
    /// <param name="hostEditor">当前主机编辑模型。</param>
    /// <param name="fingerprint">服务器在 SSH 握手中展示的指纹。</param>
    /// <returns>用户确认该指纹属于目标服务器时返回 true。</returns>
    private async Task<bool> ShowFingerprintConfirmationAsync(
        HostEditorViewModel hostEditor,
        string fingerprint)
    {
        var confirmButton = new Button { Content = "确认并固定", MinWidth = 110 };
        var cancelButton = new Button { Content = "取消", MinWidth = 90 };
        var dialog = new Window
        {
            Title = "确认 SSH 主机指纹",
            Width = 600,
            Height = 330,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(24),
                Spacing = 14,
                Children =
                {
                    new TextBlock { Text = $"确认 {hostEditor.Model.Address}:{hostEditor.Model.SshPort}", FontSize = 20, FontWeight = FontWeight.SemiBold },
                    new TextBlock { Text = "程序已经从 SSH 密钥交换读取到下面的指纹。请与阿里云控制台、服务器初始化记录或其他可信渠道显示的指纹核对；不要只因为连接成功就直接确认。", Foreground = Brush.Parse("#475569"), TextWrapping = TextWrapping.Wrap },
                    new Border
                    {
                        Padding = new Avalonia.Thickness(13),
                        CornerRadius = new Avalonia.CornerRadius(6),
                        Background = Brush.Parse("#F1F5F9"),
                        Child = new TextBlock { Text = fingerprint, FontFamily = FontFamily.Parse("Menlo,Consolas"), TextWrapping = TextWrapping.Wrap }
                    },
                    new TextBlock { Text = "确认后它会随当前方案保存。后续服务器返回不同指纹时，发布工具会拒绝 SSH 连接，以防地址被冒充。", Foreground = Brush.Parse("#175CD3"), TextWrapping = TextWrapping.Wrap },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 10,
                        Children = { cancelButton, confirmButton }
                    }
                }
            }
        };
        confirmButton.Click += (_, _) => dialog.Close(true);
        cancelButton.Click += (_, _) => dialog.Close(false);
        return await dialog.ShowDialog<bool>(this).ConfigureAwait(true);
    }

    /// <summary>
    /// 显示不包含敏感信息的错误提示。
    /// </summary>
    /// <param name="title">错误标题。</param>
    /// <param name="message">错误说明。</param>
    /// <returns>对话框关闭任务。</returns>
    private async Task ShowErrorAsync(string title, string message)
    {
        var closeButton = new Button { Content = "知道了", MinWidth = 90 };
        var dialog = new Window
        {
            Title = title,
            Width = 520,
            Height = 230,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(24),
                Spacing = 14,
                Children =
                {
                    new TextBlock { Text = title, FontSize = 20, FontWeight = FontWeight.SemiBold },
                    new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Children = { closeButton }
                    }
                }
            }
        };
        closeButton.Click += (_, _) => dialog.Close();
        await dialog.ShowDialog(this).ConfigureAwait(true);
    }

    /// <summary>
    /// 对 Coordinator、最后实例和停服风险显示明确的模态确认。
    /// </summary>
    /// <param name="sender">视图模型。</param>
    /// <param name="eventArgs">风险步骤和结果任务。</param>
    private async void OnApprovalRequested(object? sender, ApprovalRequestEventArgs eventArgs)
    {
        var approveButton = new Button { Content = "确认继续", MinWidth = 110 };
        var cancelButton = new Button { Content = "取消发布", MinWidth = 110 };
        var dialog = new Window
        {
            Title = "需要人工确认",
            Width = 520,
            Height = 260,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(24),
                Spacing = 14,
                Children =
                {
                    new TextBlock { Text = "该步骤可能影响在线服务", FontSize = 21, FontWeight = Avalonia.Media.FontWeight.SemiBold },
                    new TextBlock { Text = eventArgs.Step.DisplayName, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    new TextBlock { Text = $"操作：{eventArgs.Step.Action}    主机：{eventArgs.Step.HostId}    实例：{eventArgs.Step.InstanceId}", Foreground = Avalonia.Media.Brushes.Gray, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 10,
                        Children = { cancelButton, approveButton }
                    }
                }
            }
        };
        approveButton.Click += (_, _) => dialog.Close(true);
        cancelButton.Click += (_, _) => dialog.Close(false);
        bool approved = await dialog.ShowDialog<bool>(this).ConfigureAwait(true);
        eventArgs.Completion.TrySetResult(approved);
    }

    /// <summary>
    /// 删除配置方案前说明实际影响范围并要求用户明确确认。
    /// </summary>
    /// <param name="sender">视图模型。</param>
    /// <param name="eventArgs">待删除方案和结果任务。</param>
    private async void OnProfileDeletionRequested(object? sender, ProfileDeletionRequestEventArgs eventArgs)
    {
        var deleteButton = new Button
        {
            Content = "删除本地方案",
            MinWidth = 120,
            Background = Avalonia.Media.Brush.Parse("#DC2626"),
            Foreground = Avalonia.Media.Brushes.White
        };
        var cancelButton = new Button { Content = "取消", MinWidth = 90 };
        var dialog = new Window
        {
            Title = "删除配置方案",
            Width = 520,
            Height = 280,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Avalonia.Media.Brush.Parse("#F8FAFC"),
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(24),
                Spacing = 14,
                Children =
                {
                    new TextBlock { Text = $"删除“{eventArgs.Profile.Name}”？", FontSize = 20, FontWeight = Avalonia.Media.FontWeight.SemiBold, Foreground = Avalonia.Media.Brush.Parse("#0F172A") },
                    new TextBlock { Text = "只会删除这份本地配置文件，不会停止远程服务器，也不会删除制品、日志或发布历史。", Foreground = Avalonia.Media.Brush.Parse("#475569"), TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    new Border
                    {
                        Padding = new Avalonia.Thickness(12),
                        CornerRadius = new Avalonia.CornerRadius(6),
                        Background = Avalonia.Media.Brush.Parse("#FFF7ED"),
                        Child = new TextBlock { Text = "删除后无法从应用内恢复；如需保留，请先复制方案。", Foreground = Avalonia.Media.Brush.Parse("#9A3412"), TextWrapping = Avalonia.Media.TextWrapping.Wrap }
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 10,
                        Children = { cancelButton, deleteButton }
                    }
                }
            }
        };
        deleteButton.Click += (_, _) => dialog.Close(true);
        cancelButton.Click += (_, _) => dialog.Close(false);
        bool confirmed = await dialog.ShowDialog<bool>(this).ConfigureAwait(true);
        eventArgs.Completion.TrySetResult(confirmed);
    }

    #endregion
}
