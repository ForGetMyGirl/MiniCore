namespace MiniCore.Deploy.Core.Models;

/// <summary>
/// 定义一个发布步骤的执行状态。
/// </summary>
public enum StepStatus
{
    /// <summary>
    /// 尚未开始。
    /// </summary>
    Pending,

    /// <summary>
    /// 正在执行。
    /// </summary>
    Running,

    /// <summary>
    /// 等待操作人员确认风险。
    /// </summary>
    ApprovalRequired,

    /// <summary>
    /// 执行成功。
    /// </summary>
    Succeeded,

    /// <summary>
    /// 执行失败。
    /// </summary>
    Failed,

    /// <summary>
    /// 因前置失败或不适用而跳过。
    /// </summary>
    Skipped,

    /// <summary>
    /// 被操作人员取消。
    /// </summary>
    Cancelled
}
