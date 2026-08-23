namespace MiniCore.Deploy.Core.Models;

/// <summary>
/// 汇总一轮发布状态机的全部步骤结果。
/// </summary>
public sealed class DeploymentExecutionResult
{
    #region Public 公共成员

    /// <summary>
    /// 获取整体是否成功。
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// 获取执行或恢复后产生的步骤结果。
    /// </summary>
    public IReadOnlyList<StepResult> Steps { get; }

    /// <summary>
    /// 创建执行结果。
    /// </summary>
    /// <param name="succeeded">整体成功状态。</param>
    /// <param name="steps">步骤结果。</param>
    public DeploymentExecutionResult(bool succeeded, IReadOnlyList<StepResult> steps)
    {
        Succeeded = succeeded;
        Steps = steps ?? throw new ArgumentNullException(nameof(steps));
    }

    #endregion
}
