namespace MiniCore.Deploy.Core.Models;

/// <summary>
/// 描述执行前必须展示给操作人员的完整发布计划。
/// </summary>
public sealed class DeploymentPlan
{
    #region Public 公共成员

    /// <summary>
    /// 获取或设置唯一计划标识。
    /// </summary>
    public string PlanId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 获取或设置目标环境标识。
    /// </summary>
    public string EnvironmentId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置目标统一版本。
    /// </summary>
    public string ReleaseVersion { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置发布操作。
    /// </summary>
    public DeploymentOperation Operation { get; set; }

    /// <summary>
    /// 获取或设置按执行顺序排列的步骤。
    /// </summary>
    public List<DeploymentStep> Steps { get; set; } = new();

    #endregion
}
