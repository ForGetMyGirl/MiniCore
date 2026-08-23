namespace MiniCore.Deploy.Core.Models;

/// <summary>
/// 聚合步骤执行期间只读使用的发布输入与制品清单。
/// </summary>
public sealed class DeploymentExecutionContext
{
    #region Public 公共成员

    /// <summary>
    /// 获取发布配置。
    /// </summary>
    public DeploymentProfile Profile { get; }

    /// <summary>
    /// 获取当前计划。
    /// </summary>
    public DeploymentPlan Plan { get; }

    /// <summary>
    /// 获取或设置构建完成后的发布清单。
    /// </summary>
    public ReleaseManifest? ReleaseManifest { get; set; }

    /// <summary>
    /// 创建步骤执行上下文。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="plan">当前发布计划。</param>
    public DeploymentExecutionContext(DeploymentProfile profile, DeploymentPlan plan)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
    }

    #endregion
}
