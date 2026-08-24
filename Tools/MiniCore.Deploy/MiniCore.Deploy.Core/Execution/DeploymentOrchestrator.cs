using MiniCore.Deploy.Core.Exceptions;
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

        try
        {
            await executor.BeginExecutionAsync(context, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            DeploymentStep leaseStep = context.Plan.Steps.FirstOrDefault(static step => step.Action == DeploymentAction.Preflight)
                ?? new DeploymentStep
                {
                    StepId = "execution-lock",
                    DisplayName = "取得环境发布锁",
                    Action = DeploymentAction.Preflight
                };
            StepStatus status = exception is OperationCanceledException ? StepStatus.Cancelled : StepStatus.Failed;
            DeploymentFailureException? deploymentFailure = exception as DeploymentFailureException;
            string errorCode = exception is OperationCanceledException
                ? "CANCELLED"
                : deploymentFailure?.ErrorCode ?? "EXECUTION_LOCK_FAILED";
            StepResult leaseFailure = CreateTerminalResult(leaseStep, status, errorCode, exception.Message, 1);
            leaseFailure.RecoverySuggestion = deploymentFailure?.RecoverySuggestion
                ?? "检查 SSH、目标目录权限和环境发布锁后重新执行。";
            EnrichResult(leaseFailure, leaseStep, context);
            await journal.AppendAsync(context.Plan.PlanId, leaseFailure, CancellationToken.None).ConfigureAwait(false);
            progress?.Report(leaseFailure);
            return new DeploymentExecutionResult(false, new[] { leaseFailure });
        }

        List<StepResult>? results = null;
        try
        {
            IReadOnlyList<StepResult> previous = await journal.LoadAsync(context.Plan.PlanId, cancellationToken).ConfigureAwait(false);
            var completedIds = new HashSet<string>(StringComparer.Ordinal);
            var rollbackCutoffs = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);
            results = new List<StepResult>(previous.Count + context.Plan.Steps.Count);
            for (int index = 0; index < previous.Count; index++)
            {
                StepResult existing = previous[index];
                if (existing.Action != DeploymentAction.AutomaticRollback
                    || existing.RollbackSucceeded != true
                    || string.IsNullOrWhiteSpace(existing.InstanceId))
                {
                    continue;
                }

                DateTimeOffset completedAt = existing.CompletedAtUtc ?? existing.StartedAtUtc;
                if (!rollbackCutoffs.TryGetValue(existing.InstanceId, out DateTimeOffset currentCutoff)
                    || completedAt > currentCutoff)
                {
                    rollbackCutoffs[existing.InstanceId] = completedAt;
                }
            }

            for (int index = 0; index < previous.Count; index++)
            {
                StepResult existing = previous[index];
                results.Add(existing);
                if (existing.Status == StepStatus.Succeeded
                    && !WasInvalidatedByAutomaticRollback(existing, rollbackCutoffs))
                {
                    completedIds.Add(existing.StepId);
                }
            }

            for (int index = 0; index < context.Plan.Steps.Count; index++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return new DeploymentExecutionResult(false, results);
                }

                DeploymentStep step = context.Plan.Steps[index];
                if (completedIds.Contains(step.StepId) && !MustRevalidateOnResume(step.Action))
                {
                    continue;
                }

                bool approved = true;
                if (step.RequiresApproval)
                {
                    try
                    {
                        approved = await approveAsync(step, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return new DeploymentExecutionResult(false, results);
                    }
                }

                if (!approved)
                {
                    StepResult cancelled = CreateTerminalResult(step, StepStatus.Cancelled, "APPROVAL_DENIED", "操作人员未批准风险步骤。", 0);
                    EnrichResult(cancelled, step, context);
                    await journal.AppendAsync(context.Plan.PlanId, cancelled, CancellationToken.None).ConfigureAwait(false);
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

                    CancellationToken stepCancellationToken = IsAtomicStep(step.Action)
                        ? CancellationToken.None
                        : cancellationToken;
                    result = await executor.ExecuteAsync(step, context, stepCancellationToken).ConfigureAwait(false);
                    result.StepId = step.StepId;
                    result.DisplayName = step.DisplayName;
                    result.HostId = step.HostId;
                    result.Attempt = attempt;
                    if (result.StartedAtUtc == default)
                    {
                        result.StartedAtUtc = running.StartedAtUtc;
                    }

                    result.CompletedAtUtc ??= DateTimeOffset.UtcNow;
                    EnrichResult(result, step, context);
                    await journal.AppendAsync(context.Plan.PlanId, result, CancellationToken.None).ConfigureAwait(false);
                    results.Add(result);
                    progress?.Report(result);
                    if (result.Status == StepStatus.Succeeded)
                    {
                        break;
                    }
                }

                if (result == null || result.Status != StepStatus.Succeeded)
                {
                    if (result != null && result.Status == StepStatus.Failed)
                    {
                        StepResult? compensation = await executor.CompensateAsync(
                            step,
                            context,
                            CancellationToken.None).ConfigureAwait(false);
                        if (compensation != null)
                        {
                            var compensationStep = new DeploymentStep
                            {
                                StepId = compensation.StepId,
                                DisplayName = compensation.DisplayName,
                                Action = DeploymentAction.AutomaticRollback,
                                HostId = step.HostId,
                                InstanceId = step.InstanceId
                            };
                            compensation.RollbackSucceeded = compensation.Status == StepStatus.Succeeded;
                            EnrichResult(compensation, compensationStep, context);
                            await journal.AppendAsync(context.Plan.PlanId, compensation, CancellationToken.None).ConfigureAwait(false);
                            results.Add(compensation);
                            progress?.Report(compensation);
                        }
                    }

                    return new DeploymentExecutionResult(false, results);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return new DeploymentExecutionResult(false, results);
                }
            }

            return new DeploymentExecutionResult(true, results);
        }
        finally
        {
            try
            {
                await executor.EndExecutionAsync(context, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                var releaseStep = new DeploymentStep
                {
                    StepId = "release-environment-lock",
                    DisplayName = "释放环境发布锁",
                    Action = DeploymentAction.ReleaseEnvironmentLock
                };
                StepResult releaseFailure = CreateTerminalResult(
                    releaseStep,
                    StepStatus.Failed,
                    "ENVIRONMENT_LOCK_RELEASE_FAILED",
                    exception.Message,
                    1);
                releaseFailure.RecoverySuggestion = "确认没有发布进程仍在运行后，按占用错误提示核对并人工清理远程环境锁目录。";
                EnrichResult(releaseFailure, releaseStep, context);
                try
                {
                    await journal.AppendAsync(context.Plan.PlanId, releaseFailure, CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                    // 锁释放异常优先返回给操作人员，日志写入异常不覆盖原始恢复信息。
                }

                results?.Add(releaseFailure);
                progress?.Report(releaseFailure);
                throw new InvalidOperationException("发布流程已结束，但环境锁释放失败；请根据执行中心日志人工检查远程锁。", exception);
            }
        }
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
    /// 为基础设施结果补齐计划、环境、实例、操作人员和耗时等审计维度。
    /// </summary>
    /// <param name="result">待补齐结果。</param>
    /// <param name="step">当前计划步骤。</param>
    /// <param name="context">当前发布上下文。</param>
    private static void EnrichResult(
        StepResult result,
        DeploymentStep step,
        DeploymentExecutionContext context)
    {
        result.PlanId = context.Plan.PlanId;
        result.EnvironmentId = context.Plan.EnvironmentId;
        result.InstanceId = step.InstanceId;
        result.Operation = context.Plan.Operation;
        result.Action = step.Action;
        result.Operator = Environment.UserName + "@" + Environment.MachineName;
        result.ReleaseVersion = context.Plan.ReleaseVersion;
        if (result.CompletedAtUtc.HasValue && result.StartedAtUtc != default)
        {
            result.DurationMilliseconds = Math.Max(
                0L,
                (long)(result.CompletedAtUtc.Value - result.StartedAtUtc).TotalMilliseconds);
        }

        if (string.IsNullOrWhiteSpace(result.StandardOutputSummary)
            && result.Status == StepStatus.Succeeded)
        {
            result.StandardOutputSummary = result.Message;
        }

        if (string.IsNullOrWhiteSpace(result.StandardErrorSummary)
            && result.Status is StepStatus.Failed or StepStatus.Cancelled)
        {
            result.StandardErrorSummary = result.Message;
        }
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

    /// <summary>
    /// 判断一个已成功步骤是否已经被同实例之后的自动回滚撤销。
    /// </summary>
    /// <param name="result">历史步骤结果。</param>
    /// <param name="rollbackCutoffs">每个实例最后一次成功自动回滚的完成时间。</param>
    /// <returns>配置、服务定义、版本或启动状态已被回滚撤销时返回 true。</returns>
    private static bool WasInvalidatedByAutomaticRollback(
        StepResult result,
        IReadOnlyDictionary<string, DateTimeOffset> rollbackCutoffs)
    {
        if (string.IsNullOrWhiteSpace(result.InstanceId)
            || !rollbackCutoffs.TryGetValue(result.InstanceId, out DateTimeOffset cutoff))
        {
            return false;
        }

        DateTimeOffset completedAt = result.CompletedAtUtc ?? result.StartedAtUtc;
        if (completedAt > cutoff)
        {
            return false;
        }

        return result.Action is DeploymentAction.WriteConfiguration
            or DeploymentAction.InstallService
            or DeploymentAction.ActivateRelease
            or DeploymentAction.StartService
            or DeploymentAction.WaitForHealth;
    }

    /// <summary>
    /// 判断动作是否属于必须完整完成后才能响应取消的远程原子段。
    /// </summary>
    /// <param name="action">计划动作。</param>
    /// <returns>服务定义或运行状态切换动作返回 true。</returns>
    private static bool IsAtomicStep(DeploymentAction action)
    {
        return action is DeploymentAction.InstallService
            or DeploymentAction.StopService
            or DeploymentAction.ActivateRelease
            or DeploymentAction.StartService
            or DeploymentAction.UninstallService;
    }

    #endregion
}
