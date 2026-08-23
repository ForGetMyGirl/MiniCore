using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Core.Execution;

/// <summary>
/// 顺序执行可重入发布计划，并在每一步后立即写入恢复日志。
/// </summary>
public sealed class DeploymentOrchestrator
{
    #region Private 私有成员

    private readonly IDeploymentStepExecutor executor; // 执行具体本地或远程动作。
    private readonly IExecutionJournal journal; // 持久化断点与结果。

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 创建发布状态机。
    /// </summary>
    /// <param name="executor">步骤执行器。</param>
    /// <param name="journal">执行日志。</param>
    public DeploymentOrchestrator(IDeploymentStepExecutor executor, IExecutionJournal journal)
    {
        this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
        this.journal = journal ?? throw new ArgumentNullException(nameof(journal));
    }

    /// <summary>
    /// 从已成功步骤之后继续执行计划。
    /// </summary>
    /// <param name="context">发布上下文。</param>
    /// <param name="approveAsync">风险步骤人工确认回调。</param>
    /// <param name="progress">界面状态回调。</param>
    /// <param name="cancellationToken">用户取消令牌。</param>
    /// <returns>完整或失败前的步骤结果。</returns>
    public async Task<DeploymentExecutionResult> ExecuteAsync(
        DeploymentExecutionContext context,
        Func<DeploymentStep, CancellationToken, Task<bool>> approveAsync,
        IProgress<StepResult>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(approveAsync);

        IReadOnlyList<StepResult> previous = await journal.LoadAsync(context.Plan.PlanId, cancellationToken).ConfigureAwait(false);
        var completedIds = new HashSet<string>(StringComparer.Ordinal);
        var results = new List<StepResult>(previous.Count + context.Plan.Steps.Count);
        for (int index = 0; index < previous.Count; index++)
        {
            StepResult existing = previous[index];
            results.Add(existing);
            if (existing.Status == StepStatus.Succeeded)
            {
                completedIds.Add(existing.StepId);
            }
        }

        for (int index = 0; index < context.Plan.Steps.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeploymentStep step = context.Plan.Steps[index];
            if (completedIds.Contains(step.StepId) && !MustRevalidateOnResume(step.Action))
            {
                continue;
            }

            if (step.RequiresApproval && !await approveAsync(step, cancellationToken).ConfigureAwait(false))
            {
                StepResult cancelled = CreateTerminalResult(step, StepStatus.Cancelled, "APPROVAL_DENIED", "操作人员未批准风险步骤。", 0);
                await journal.AppendAsync(context.Plan.PlanId, cancelled, cancellationToken).ConfigureAwait(false);
                results.Add(cancelled);
                progress?.Report(cancelled);
                return new DeploymentExecutionResult(false, results);
            }

            StepResult? result = null;
            for (int attempt = 1; attempt <= step.MaxAttempts; attempt++)
            {
                var running = new StepResult
                {
                    StepId = step.StepId,
                    DisplayName = step.DisplayName,
                    HostId = step.HostId,
                    Status = StepStatus.Running,
                    Attempt = attempt,
                    StartedAtUtc = DateTimeOffset.UtcNow
                };
                progress?.Report(running);

                result = await executor.ExecuteAsync(step, context, cancellationToken).ConfigureAwait(false);
                result.StepId = step.StepId;
                result.DisplayName = step.DisplayName;
                result.HostId = step.HostId;
                result.Attempt = attempt;
                if (result.StartedAtUtc == default)
                {
                    result.StartedAtUtc = running.StartedAtUtc;
                }

                result.CompletedAtUtc ??= DateTimeOffset.UtcNow;
                await journal.AppendAsync(context.Plan.PlanId, result, cancellationToken).ConfigureAwait(false);
                results.Add(result);
                progress?.Report(result);
                if (result.Status == StepStatus.Succeeded)
                {
                    break;
                }
            }

            if (result == null || result.Status != StepStatus.Succeeded)
            {
                return new DeploymentExecutionResult(false, results);
            }
        }

        return new DeploymentExecutionResult(true, results);
    }

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 创建未进入基础设施执行器的终态结果。
    /// </summary>
    /// <param name="step">目标步骤。</param>
    /// <param name="status">终态。</param>
    /// <param name="errorCode">错误码。</param>
    /// <param name="message">结果说明。</param>
    /// <param name="attempt">执行次数。</param>
    /// <returns>结构化步骤结果。</returns>
    private static StepResult CreateTerminalResult(
        DeploymentStep step,
        StepStatus status,
        string errorCode,
        string message,
        int attempt)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new StepResult
        {
            StepId = step.StepId,
            DisplayName = step.DisplayName,
            HostId = step.HostId,
            Status = status,
            Attempt = attempt,
            StartedAtUtc = now,
            CompletedAtUtc = now,
            ErrorCode = errorCode,
            Message = message
        };
    }

    /// <summary>
    /// 判断中断恢复时必须重新核对的安全步骤。
    /// </summary>
    /// <param name="action">原子发布动作。</param>
    /// <returns>远程状态或制品可能变化、且操作本身幂等时返回 true。</returns>
    private static bool MustRevalidateOnResume(DeploymentAction action)
    {
        return action is DeploymentAction.Preflight or DeploymentAction.StageArtifact;
    }

    #endregion
}
