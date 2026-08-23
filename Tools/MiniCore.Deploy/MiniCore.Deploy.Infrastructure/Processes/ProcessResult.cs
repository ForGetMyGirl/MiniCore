namespace MiniCore.Deploy.Infrastructure.Processes;

/// <summary>
/// 保存外部构建工具的退出状态和脱敏日志位置。
/// </summary>
public sealed class ProcessResult
{
    #region Public 公共成员

    /// <summary>
    /// 获取进程退出码。
    /// </summary>
    public int ExitCode { get; }

    /// <summary>
    /// 获取标准输出与错误输出合并后的日志路径。
    /// </summary>
    public string LogPath { get; }

    /// <summary>
    /// 创建外部进程结果。
    /// </summary>
    /// <param name="exitCode">退出码。</param>
    /// <param name="logPath">日志路径。</param>
    public ProcessResult(int exitCode, string logPath)
    {
        ExitCode = exitCode;
        LogPath = logPath;
    }

    #endregion
}
