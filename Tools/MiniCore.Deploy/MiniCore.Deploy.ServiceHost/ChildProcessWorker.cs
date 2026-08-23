using System.Diagnostics;
using System.Text;

namespace MiniCore.Deploy.ServiceHost;

/// <summary>
/// 监督一个服务子进程，并在 SCM 停止时先调用本地优雅关闭命令。
/// </summary>
public sealed class ChildProcessWorker : BackgroundService
{
    #region Private 私有成员

    private static readonly TimeSpan[] RestartDelays =
    {
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(15)
    }; // 有界重启退避。
    private readonly ServiceHostOptions options; // 子进程描述。
    private readonly ILogger<ChildProcessWorker> logger; // Windows 事件日志与控制台日志。
    private readonly object processLock = new(); // 保护当前子进程引用。
    private Process? activeProcess; // 当前监督的子进程。

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 创建子进程监督器。
    /// </summary>
    /// <param name="options">服务描述。</param>
    /// <param name="logger">宿主日志。</param>
    public ChildProcessWorker(ServiceHostOptions options, ILogger<ChildProcessWorker> logger)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 请求子进程优雅退出，超时后终止进程树。
    /// </summary>
    /// <param name="cancellationToken">SCM 停止令牌。</param>
    /// <returns>停止完成任务。</returns>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Process? process;
        lock (processLock)
        {
            process = activeProcess;
        }

        if (process != null && !process.HasExited)
        {
            await RequestGracefulShutdownAsync(cancellationToken).ConfigureAwait(false);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(options.GracefulShutdownSeconds));
            try
            {
                await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("子进程未在 {Seconds} 秒内退出，将终止进程树。", options.GracefulShutdownSeconds);
                process.Kill(true);
            }
        }

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Override 重写实现

    /// <summary>
    /// 启动子进程并在意外退出时按有界退避重启。
    /// </summary>
    /// <param name="stoppingToken">宿主停止令牌。</param>
    /// <returns>宿主生命周期任务。</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int retryIndex = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            int exitCode = await RunChildOnceAsync(stoppingToken).ConfigureAwait(false);
            if (stoppingToken.IsCancellationRequested || !options.RestartOnUnexpectedExit)
            {
                return;
            }

            TimeSpan delay = RestartDelays[Math.Min(retryIndex, RestartDelays.Length - 1)];
            retryIndex = Math.Min(retryIndex + 1, RestartDelays.Length - 1);
            logger.LogWarning("子进程意外退出，退出码 {ExitCode}，将在 {Delay} 后重启。", exitCode, delay);
            await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
        }
    }

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 启动一次子进程并将标准输出和错误输出写入实例日志。
    /// </summary>
    /// <param name="cancellationToken">宿主停止令牌。</param>
    /// <returns>子进程退出码。</returns>
    private async Task<int> RunChildOnceAsync(CancellationToken cancellationToken)
    {
        string logPath = Path.Combine(options.LogDirectory, "service-host-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd") + ".log");
        var startInfo = new ProcessStartInfo
        {
            FileName = options.ExecutablePath,
            WorkingDirectory = options.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        for (int index = 0; index < options.Arguments.Length; index++)
        {
            startInfo.ArgumentList.Add(options.Arguments[index]);
        }

        ApplyEnvironmentVariables(startInfo, options);

        using var process = new Process { StartInfo = startInfo };
        await using var writer = new StreamWriter(logPath, true, new UTF8Encoding(false));
        var writeLock = new SemaphoreSlim(1, 1);
        process.OutputDataReceived += (_, eventArgs) => WriteLogLineAsync(writer, writeLock, eventArgs.Data).GetAwaiter().GetResult();
        process.ErrorDataReceived += (_, eventArgs) => WriteLogLineAsync(writer, writeLock, eventArgs.Data).GetAwaiter().GetResult();
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        lock (processLock)
        {
            activeProcess = process;
        }

        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return process.ExitCode;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        finally
        {
            lock (processLock)
            {
                activeProcess = null;
            }
        }
    }

    /// <summary>
    /// 运行描述文件中的本地优雅关闭命令。
    /// </summary>
    /// <param name="cancellationToken">停止令牌。</param>
    /// <returns>关闭命令完成任务。</returns>
    private async Task RequestGracefulShutdownAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.ShutdownExecutablePath) || !File.Exists(options.ShutdownExecutablePath))
        {
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = options.ShutdownExecutablePath,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        for (int index = 0; index < options.ShutdownArguments.Length; index++)
        {
            startInfo.ArgumentList.Add(options.ShutdownArguments[index]);
        }

        using var process = Process.Start(startInfo);
        if (process != null)
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 串行追加子进程日志。
    /// </summary>
    /// <param name="writer">日志写入器。</param>
    /// <param name="writeLock">写入锁。</param>
    /// <param name="line">日志行。</param>
    /// <returns>写入完成任务。</returns>
    private static async Task WriteLogLineAsync(StreamWriter writer, SemaphoreSlim writeLock, string? line)
    {
        if (line == null)
        {
            return;
        }

        await writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await writer.WriteLineAsync(line).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    /// <summary>
    /// 将服务描述中明确声明的环境变量注入子进程。
    /// </summary>
    /// <param name="startInfo">子进程启动信息。</param>
    /// <param name="options">服务描述配置。</param>
    private static void ApplyEnvironmentVariables(ProcessStartInfo startInfo, ServiceHostOptions options)
    {
        foreach (KeyValuePair<string, string> pair in options.EnvironmentVariables)
        {
            startInfo.Environment[pair.Key] = pair.Value;
        }
    }

    #endregion
}
