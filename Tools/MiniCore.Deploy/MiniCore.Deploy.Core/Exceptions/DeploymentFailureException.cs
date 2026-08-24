namespace MiniCore.Deploy.Core.Exceptions;

/// <summary>
/// 表示具有稳定错误码和明确恢复建议的发布执行失败。
/// </summary>
public sealed class DeploymentFailureException : Exception
{
    #region Public 公共成员

    /// <summary>
    /// 获取供执行中心和发布历史定位问题的稳定错误码。
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// 获取由操作人员执行、且不会由工具自动运行的恢复建议。
    /// </summary>
    public string RecoverySuggestion { get; }

    /// <summary>
    /// 创建包含结构化诊断信息的发布执行异常。
    /// </summary>
    /// <param name="errorCode">稳定错误码。</param>
    /// <param name="message">可直接展示给操作人员的失败原因。</param>
    /// <param name="recoverySuggestion">不包含凭据的恢复建议。</param>
    public DeploymentFailureException(
        string errorCode,
        string message,
        string recoverySuggestion)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(recoverySuggestion);
        ErrorCode = errorCode;
        RecoverySuggestion = recoverySuggestion;
    }

    #endregion
}
