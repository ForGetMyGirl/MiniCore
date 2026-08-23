namespace MiniCore.Deploy.Infrastructure.Remote;

/// <summary>
/// 保存远程命令的退出码和已脱敏输出。
/// </summary>
public sealed class RemoteCommandResult
{
    #region Public 公共成员

    /// <summary>
    /// 获取退出码。
    /// </summary>
    public int ExitCode { get; }

    /// <summary>
    /// 获取标准输出。
    /// </summary>
    public string StandardOutput { get; }

    /// <summary>
    /// 获取标准错误。
    /// </summary>
    public string StandardError { get; }

    /// <summary>
    /// 创建远程命令结果。
    /// </summary>
    /// <param name="exitCode">退出码。</param>
    /// <param name="standardOutput">标准输出。</param>
    /// <param name="standardError">标准错误。</param>
    public RemoteCommandResult(int exitCode, string standardOutput, string standardError)
    {
        ExitCode = exitCode;
        StandardOutput = standardOutput;
        StandardError = standardError;
    }

    #endregion
}
