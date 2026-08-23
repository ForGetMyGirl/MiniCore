namespace MiniCore.Deploy.Core.Models;

/// <summary>
/// 描述发布清单中的一个不可变制品。
/// </summary>
public sealed class ReleaseArtifact
{
    #region Public 公共成员

    /// <summary>
    /// 获取或设置构建目标。
    /// </summary>
    public BuildTargetKind Target { get; set; }

    /// <summary>
    /// 获取或设置制品相对于发布目录的路径。
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置小写十六进制 SHA-256。
    /// </summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置制品字节数。
    /// </summary>
    public long Length { get; set; }

    #endregion
}
