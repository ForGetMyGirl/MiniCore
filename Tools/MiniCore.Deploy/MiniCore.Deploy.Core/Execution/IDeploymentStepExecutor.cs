using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Core.Execution;

/// <summary>
/// 定义原子发布步骤的基础设施执行边界。
/// </summary>
public interface IDeploymentStepExecutor
{
    /// <summary>
    /// 在任何计划步骤前取得环境级执行租约。
    /// </summary>
    /// <param name="context">本轮发布上下文。</param>
    /// <param name="cancellationToken">用户取消令牌。</param>
    /// <returns>租约取得完成任务。</returns>
    Task BeginExecutionAsync(
        DeploymentExecutionContext context,
        CancellationToken cancellationToken);

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

    /// <summary>
    /// 在启动或最终健康检查失败后尝试恢复该实例的上一已知版本和配置。
    /// </summary>
    /// <param name="failedStep">已经耗尽重试次数的失败步骤。</param>
    /// <param name="context">本轮发布上下文。</param>
    /// <param name="cancellationToken">补偿动作使用的独立令牌。</param>
    /// <returns>不适用时为空，否则返回补偿结果。</returns>
    Task<StepResult?> CompensateAsync(
        DeploymentStep failedStep,
        DeploymentExecutionContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// 在成功、失败或取消后释放环境级执行租约。
    /// </summary>
    /// <param name="context">本轮发布上下文。</param>
    /// <param name="cancellationToken">只用于租约清理的令牌。</param>
    /// <returns>租约释放完成任务。</returns>
    Task EndExecutionAsync(
        DeploymentExecutionContext context,
        CancellationToken cancellationToken);
}
