namespace MiniCore.Deploy.RegressionTests;

/// <summary>
/// 提供不依赖第三方测试框架的自动化回归检查入口。
/// </summary>
internal static class Program
{
    #region Private 私有成员

    /// <summary>
    /// 执行全部 MiniCore Deploy 回归检查。
    /// </summary>
    /// <returns>全部通过时返回零。</returns>
    private static int Main()
    {
        RegressionTestSuite.RunAll();
        return 0;
    }

    #endregion
}
