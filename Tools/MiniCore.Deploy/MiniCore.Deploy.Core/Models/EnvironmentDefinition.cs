namespace MiniCore.Deploy.Core.Models;

/// <summary>
/// 描述一个最终必须收敛到单一版本的部署环境。
/// </summary>
public sealed class EnvironmentDefinition
{
    #region Public 公共成员

    /// <summary>
    /// 获取或设置稳定的环境标识。
    /// </summary>
    public string EnvironmentId { get; set; } = "development";

    /// <summary>
    /// 获取或设置环境显示名称。
    /// </summary>
    public string DisplayName { get; set; } = "Development";

    /// <summary>
    /// 获取或设置构建发布前是否强制要求 Git 工作区干净。
    /// </summary>
    public bool RequireCleanGitWorkspace { get; set; }

    /// <summary>
    /// 获取或设置本次操作结束后必须统一使用的发布版本。
    /// </summary>
    public string ReleaseVersion { get; set; } = "0.1.0";

    /// <summary>
    /// 获取或设置已由开发运维人员完成数据库迁移评审的发布版本；工具本身不执行迁移。
    /// </summary>
    public string DatabaseMigrationReviewedReleaseVersion { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前环境已登记的主机。
    /// </summary>
    public List<HostDefinition> Hosts { get; set; } = new();

    /// <summary>
    /// 获取或设置当前环境期望运行的实例。
    /// </summary>
    public List<InstanceDefinition> Instances { get; set; } = new();

    #endregion
}
