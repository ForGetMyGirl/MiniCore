using System.Text.Json;

namespace MiniCore.Deploy.ServiceHost;

/// <summary>
/// 描述 Windows 服务包装器需要监督的子进程和优雅退出命令。
/// </summary>
public sealed class ServiceHostOptions
{
    #region Public 公共成员

    /// <summary>
    /// 获取或设置子进程可执行程序。
    /// </summary>
    public string ExecutablePath { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置逐项传递给子进程的参数。
    /// </summary>
    public string[] Arguments { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 获取或设置子进程工作目录。
    /// </summary>
    public string WorkingDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置子进程日志目录。
    /// </summary>
    public string LogDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置显式子进程环境变量。
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// 获取或设置意外退出时是否按有界退避重启。
    /// </summary>
    public bool RestartOnUnexpectedExit { get; set; } = true;

    /// <summary>
    /// 获取或设置 SCM 停止时等待优雅退出的秒数。
    /// </summary>
    public int GracefulShutdownSeconds { get; set; } = 90;

    /// <summary>
    /// 获取或设置停止时调用的本地控制程序。
    /// </summary>
    public string ShutdownExecutablePath { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置停止命令参数。
    /// </summary>
    public string[] ShutdownArguments { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 从命令行中读取描述文件路径。
    /// </summary>
    /// <param name="args">进程参数。</param>
    /// <returns>描述文件完整路径。</returns>
    public static string FindDescriptorPath(IReadOnlyList<string> args)
    {
        for (int index = 0; index < args.Count - 1; index++)
        {
            if (string.Equals(args[index], "--descriptor", StringComparison.Ordinal))
            {
                return Path.GetFullPath(args[index + 1]);
            }
        }

        throw new ArgumentException("MiniCore.Deploy.ServiceHost 必须提供 --descriptor <path>。");
    }

    /// <summary>
    /// 加载并校验服务描述文件。
    /// </summary>
    /// <param name="path">描述文件路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>有效配置。</returns>
    public static async Task<ServiceHostOptions> LoadAsync(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = File.OpenRead(path);
        ServiceHostOptions? options = await JsonSerializer.DeserializeAsync<ServiceHostOptions>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken).ConfigureAwait(false);
        if (options == null || string.IsNullOrWhiteSpace(options.ExecutablePath) || string.IsNullOrWhiteSpace(options.WorkingDirectory))
        {
            throw new InvalidDataException("ServiceHost 描述文件缺少 executablePath 或 workingDirectory。");
        }

        if (!File.Exists(options.ExecutablePath))
        {
            throw new FileNotFoundException("ServiceHost 找不到子进程可执行文件。", options.ExecutablePath);
        }

        options.GracefulShutdownSeconds = Math.Clamp(options.GracefulShutdownSeconds, 5, 300);
        Directory.CreateDirectory(options.LogDirectory);
        return options;
    }

    #endregion
}
