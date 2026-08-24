namespace MiniCore.Deploy.Core.Models;

/// <summary>
/// 描述一次可审计、可回滚的完整发布版本。
/// </summary>
public sealed class ReleaseManifest
{
    #region Public 公共成员

    /// <summary>
    /// 获取或设置环境最终统一使用的版本。
    /// </summary>
    public string ReleaseVersion { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前清单是否包含可直接激活的完整制品。
    /// </summary>
    public bool IsCompleteRelease { get; set; }

    /// <summary>
    /// 获取或设置由全部制品内容和兼容字段计算的确定性发布摘要。
    /// </summary>
    public string ReleaseContentSha256 { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置控制面协议兼容版本。
    /// </summary>
    public string ControlProtocolVersion { get; set; } = "1";

    /// <summary>
    /// 获取或设置源码提交和差异指纹。
    /// </summary>
    public string SourceFingerprint { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 AuthenticationServer 与 DatabaseServer 迁移源码的稳定 SHA-256。
    /// </summary>
    public string DatabaseMigrationFingerprint { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置构建时已人工评审数据库迁移的目标发布版本。
    /// </summary>
    public string DatabaseMigrationReviewedReleaseVersion { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 UTC 构建时间。
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// 获取或设置本次构建得到的全部制品。
    /// </summary>
    public List<ReleaseArtifact> Artifacts { get; set; } = new();

    #endregion
}
