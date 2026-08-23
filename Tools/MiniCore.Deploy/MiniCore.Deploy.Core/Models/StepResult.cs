namespace MiniCore.Deploy.Core.Models;

/// <summary>
/// 保存一个步骤的审计结果和可定位失败信息。
/// </summary>
public sealed class StepResult
{
    #region Public 公共成员

    /// <summary>
    /// 获取或设置步骤标识。
    /// </summary>
    public string StepId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置显示名称。
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置目标主机。
    /// </summary>
    public string HostId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前状态。
    /// </summary>
    public StepStatus Status { get; set; } = StepStatus.Pending;

    /// <summary>
    /// 获取或设置执行次数。
    /// </summary>
    public int Attempt { get; set; }

    /// <summary>
    /// 获取或设置步骤开始时间。
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; set; }

    /// <summary>
    /// 获取或设置步骤完成时间。
    /// </summary>
    public DateTimeOffset? CompletedAtUtc { get; set; }

    /// <summary>
    /// 获取或设置稳定错误码。
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置用户可读结果或错误原因。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置已经脱敏的日志文件路径。
    /// </summary>
    public string LogPath { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置操作人员可执行的恢复建议。
    /// </summary>
    public string RecoverySuggestion { get; set; } = string.Empty;

    #endregion
}
