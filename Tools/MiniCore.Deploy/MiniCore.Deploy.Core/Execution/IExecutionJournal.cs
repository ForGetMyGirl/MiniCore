using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Core.Execution;

/// <summary>
/// 定义发布步骤日志与断点状态的持久化边界。
/// </summary>
public interface IExecutionJournal
{
    /// <summary>
    /// 读取指定计划已经完成的步骤结果。
    /// </summary>
    /// <param name="planId">计划标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>按写入顺序排列的结果。</returns>
    Task<IReadOnlyList<StepResult>> LoadAsync(string planId, CancellationToken cancellationToken);

    /// <summary>
    /// 原子追加一个步骤结果。
    /// </summary>
    /// <param name="planId">计划标识。</param>
    /// <param name="result">待记录结果。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>写入完成任务。</returns>
    Task AppendAsync(string planId, StepResult result, CancellationToken cancellationToken);
}
