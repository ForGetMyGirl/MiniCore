namespace MiniCore.Deploy.RegressionTests;

/// <summary>
/// 提供回归检查使用的最小断言能力。
/// </summary>
internal static class RegressionAssert
{
    #region Public 公共成员

    /// <summary>
    /// 断言条件为真。
    /// </summary>
    /// <param name="condition">待检查条件。</param>
    /// <param name="message">失败说明。</param>
    public static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// 断言两个字符串完全一致。
    /// </summary>
    /// <param name="expected">期望值。</param>
    /// <param name="actual">实际值。</param>
    /// <param name="message">失败说明。</param>
    public static void Equal(string expected, string actual, string message)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{message} 期望：{expected}；实际：{actual}。");
        }
    }

    #endregion
}
