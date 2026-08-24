namespace MiniCore.Deploy.Desktop.ViewModels;

/// <summary>
/// 汇总一个发布计划的可筛选、可直接阅读历史信息。
/// </summary>
public sealed class DeploymentHistoryEntryViewModel
{
    #region Public 公共成员

    /// <summary>
    /// 获取或设置计划标识。
    /// </summary>
    public string PlanId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置环境标识。
    /// </summary>
    public string EnvironmentId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置操作名称。
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置执行操作人员。
    /// </summary>
    public string Operator { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置执行开始时间文本。
    /// </summary>
    public string StartedAt { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置执行完成时间文本。
    /// </summary>
    public string CompletedAt { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置上一程序版本。
    /// </summary>
    public string PreviousReleaseVersion { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置目标程序版本。
    /// </summary>
    public string ReleaseVersion { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置版本变更摘要。
    /// </summary>
    public string VersionTransition { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置涉及实例摘要。
    /// </summary>
    public string Instances { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置最终结果。
    /// </summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置最终失败步骤。
    /// </summary>
    public string FailedStep { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置脱敏失败原因。
    /// </summary>
    public string FailureReason { get; set; } = string.Empty;

    /// <summary>
    /// 获取当前历史是否包含最终失败步骤。
    /// </summary>
    public bool HasFailure => !string.IsNullOrWhiteSpace(FailedStep);

    /// <summary>
    /// 获取或设置额外重试次数。
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// 获取或设置自动回滚摘要。
    /// </summary>
    public string RollbackResult { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置本计划结构化日志目录。
    /// </summary>
    public string LogDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 判断当前历史记录是否满足全部文本筛选条件。
    /// </summary>
    /// <param name="environment">环境筛选。</param>
    /// <param name="version">版本筛选。</param>
    /// <param name="instance">实例筛选。</param>
    /// <param name="operation">操作筛选。</param>
    /// <param name="result">结果筛选。</param>
    /// <returns>所有非空条件均匹配时返回 true。</returns>
    public bool Matches(
        string environment,
        string version,
        string instance,
        string operation,
        string result)
    {
        return Contains(EnvironmentId, environment)
            && (Contains(ReleaseVersion, version) || Contains(PreviousReleaseVersion, version))
            && Contains(Instances, instance)
            && Contains(Operation, operation)
            && Contains(Result, result);
    }

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 以忽略大小写方式执行空条件友好的文本包含判断。
    /// </summary>
    /// <param name="value">候选文本。</param>
    /// <param name="filter">筛选文本。</param>
    /// <returns>筛选为空或候选包含筛选时返回 true。</returns>
    private static bool Contains(string value, string filter)
    {
        return string.IsNullOrWhiteSpace(filter)
            || value.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
