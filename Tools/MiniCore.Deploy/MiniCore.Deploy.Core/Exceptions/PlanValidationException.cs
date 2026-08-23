namespace MiniCore.Deploy.Core.Exceptions;

/// <summary>
/// 表示发布配置无法生成安全且确定的执行计划。
/// </summary>
public sealed class PlanValidationException : Exception
{
    #region Public 公共成员

    /// <summary>
    /// 创建计划校验异常。
    /// </summary>
    /// <param name="message">可直接展示给操作人员的错误原因。</param>
    public PlanValidationException(string message)
        : base(message)
    {
    }

    #endregion
}
