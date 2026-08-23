using MiniCore.Deploy.Infrastructure.Processes;

namespace MiniCore.Deploy.Infrastructure.Build;

/// <summary>
/// 读取 Git 提交和工作区差异，用于生产阻断与开发指纹。
/// </summary>
public sealed class GitSourceInspector
{
    #region Private 私有成员

    private readonly ProcessRunner runner; // 无 Shell 进程执行器。

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 创建 Git 状态读取器。
    /// </summary>
    /// <param name="runner">进程执行器。</param>
    public GitSourceInspector(ProcessRunner runner)
    {
        this.runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    /// <summary>
    /// 读取当前提交和脏工作区摘要。
    /// </summary>
    /// <param name="projectPath">项目路径。</param>
    /// <param name="logDirectory">日志目录。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>提交号、干净状态和差异 SHA-256。</returns>
    public async Task<SourceFingerprint> CaptureAsync(
        string projectPath,
        string logDirectory,
        CancellationToken cancellationToken)
    {
        string statusLog = Path.Combine(logDirectory, "git-status.log");
        ProcessResult status = await runner.RunAsync(
            "git",
            new[] { "status", "--porcelain=v1", "--untracked-files=all" },
            projectPath,
            statusLog,
            cancellationToken).ConfigureAwait(false);
        if (status.ExitCode != 0)
        {
            throw new InvalidOperationException($"无法读取 Git 工作区状态，详情见 {status.LogPath}。");
        }

        string commitLog = Path.Combine(logDirectory, "git-commit.log");
        ProcessResult commit = await runner.RunAsync(
            "git",
            new[] { "rev-parse", "HEAD" },
            projectPath,
            commitLog,
            cancellationToken).ConfigureAwait(false);
        if (commit.ExitCode != 0)
        {
            throw new InvalidOperationException($"无法读取 Git 提交号，详情见 {commit.LogPath}。");
        }

        string statusText = await File.ReadAllTextAsync(statusLog, cancellationToken).ConfigureAwait(false);
        string commitText = (await File.ReadAllTextAsync(commitLog, cancellationToken).ConfigureAwait(false)).Trim();
        byte[] differenceHash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(statusText));
        return new SourceFingerprint(commitText, string.IsNullOrWhiteSpace(statusText), Convert.ToHexStringLower(differenceHash));
    }

    #endregion
}
