using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Core.Execution;

/// <summary>
/// 定义原子发布步骤的基础设施执行边界。
/// </summary>
public interface IDeploymentStepExecutor
{
    /// <summary>
    /// 执行一个已经过计划校验的步骤。
    /// </summary>
    /// <param name="step">待执行步骤。</param>
    /// <param name="context">本轮发布上下文。</param>
    /// <param name="cancellationToken">用户取消令牌。</param>
    /// <returns>包含结构化错误信息的步骤结果。</returns>
    Task<StepResult> ExecuteAsync(
        DeploymentStep step,
        DeploymentExecutionContext context,
        CancellationToken cancellationToken);
}
