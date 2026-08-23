using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace MiniCore.Deploy.Desktop;

/// <summary>
/// 配置 MiniCore Deploy 桌面应用生命周期。
/// </summary>
public sealed partial class App : Application
{
    #region Override 重写实现

    /// <summary>
    /// 加载应用 XAML 资源。
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 创建主窗口并绑定发布视图模型。
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    #endregion
}
