using Avalonia;

namespace MiniCore.Deploy.Desktop;

/// <summary>
/// MiniCore Deploy 桌面应用入口。
/// </summary>
internal static class Program
{
    #region Private 私有成员

    /// <summary>
    /// 启动 Avalonia 桌面生命周期。
    /// </summary>
    /// <param name="args">进程参数。</param>
    [STAThread]
    private static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// 创建不依赖浏览器或 Unity Editor 的 Avalonia 应用。
    /// </summary>
    /// <returns>应用构建器。</returns>
    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
    }

    #endregion
}
