using System.Diagnostics;
using System.Text;

namespace MiniCore.Deploy.Infrastructure.Processes;

/// <summary>
/// 以参数列表启动外部进程，避免通过 Shell 拼接用户输入。
/// </summary>
public sealed class ProcessRunner
{
    #region Public 公共成员

    /// <summary>
    /// 运行外部命令并将输出写入指定日志。
    /// </summary>
    /// <param name="fileName">可执行程序路径。</param>
    /// <param name="arguments">逐项传递的参数。</param>
    /// <param name="workingDirectory">工作目录。</param>
    /// <param name="logPath">日志文件路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>退出码与日志位置。</returns>
    public async Task<ProcessResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        string logPath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(logPath);

        string? logDirectory = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrEmpty(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        for (int index = 0; index < arguments.Count; index++)
        {
            startInfo.ArgumentList.Add(arguments[index]);
        }

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        await using var writer = new StreamWriter(logPath, false, new UTF8Encoding(false));
        var writeLock = new SemaphoreSlim(1, 1);
        process.OutputDataReceived += (_, eventArgs) => WriteLineAsync(writer, writeLock, eventArgs.Data).GetAwaiter().GetResult();
        process.ErrorDataReceived += (_, eventArgs) => WriteLineAsync(writer, writeLock, eventArgs.Data).GetAwaiter().GetResult();

        if (!process.Start())
        {
            throw new InvalidOperationException($"无法启动外部进程：{fileName}。");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(true);
            }

            throw;
        }

        await writeLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            await writer.FlushAsync(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }

        return new ProcessResult(process.ExitCode, logPath);
    }

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 串行写入一行进程输出，忽略流关闭时的空回调。
    /// </summary>
    /// <param name="writer">日志写入器。</param>
    /// <param name="writeLock">保护写入器的锁。</param>
    /// <param name="line">待写入内容。</param>
    /// <returns>写入完成任务。</returns>
    private static async Task WriteLineAsync(StreamWriter writer, SemaphoreSlim writeLock, string? line)
    {
        if (line == null)
        {
            return;
        }

        await writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await writer.WriteLineAsync(line).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    #endregion
}
