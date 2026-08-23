using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Infrastructure.Persistence;

/// <summary>
/// 保存已向操作人员展示的计划及其完整配置指纹。
/// </summary>
public sealed class DeploymentPlanSnapshot
{
    #region Public 公共成员

    /// <summary>
    /// 获取或设置计划生成时的配置 SHA-256。
    /// </summary>
    public string ProfileFingerprint { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置已经展示的完整发布计划。
    /// </summary>
    public DeploymentPlan Plan { get; set; } = new();

    /// <summary>
    /// 获取或设置快照 UTC 保存时间。
    /// </summary>
    public DateTimeOffset SavedAtUtc { get; set; }

    #endregion
}
