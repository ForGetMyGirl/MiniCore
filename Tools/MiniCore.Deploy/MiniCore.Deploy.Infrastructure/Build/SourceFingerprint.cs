namespace MiniCore.Deploy.Infrastructure.Build;

/// <summary>
/// 保存源码提交与工作区差异指纹。
/// </summary>
public sealed class SourceFingerprint
{
    #region Public 公共成员

    /// <summary>
    /// 获取 Git 提交号。
    /// </summary>
    public string Commit { get; }

    /// <summary>
    /// 获取工作区是否干净。
    /// </summary>
    public bool IsClean { get; }

    /// <summary>
    /// 获取工作区状态文本的 SHA-256。
    /// </summary>
    public string DifferenceHash { get; }

    /// <summary>
    /// 创建源码指纹。
    /// </summary>
    /// <param name="commit">提交号。</param>
    /// <param name="isClean">工作区是否干净。</param>
    /// <param name="differenceHash">差异摘要。</param>
    public SourceFingerprint(string commit, bool isClean, string differenceHash)
    {
        Commit = commit;
        IsClean = isClean;
        DifferenceHash = differenceHash;
    }

    /// <summary>
    /// 生成写入发布清单的稳定文本。
    /// </summary>
    /// <returns>提交和差异组合指纹。</returns>
    public override string ToString()
    {
        return IsClean ? Commit : Commit + "+dirty." + DifferenceHash;
    }

    #endregion
}
