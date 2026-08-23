namespace MiniCore.Deploy.Core.Models;

/// <summary>
/// 聚合桌面应用持久化的项目、环境和当前发布选择。
/// </summary>
public sealed class DeploymentProfile
{
    #region Public 公共成员

    /// <summary>
    /// 获取或设置配置文件格式版本。
    /// </summary>
    public int SchemaVersion { get; set; } = 4;

    /// <summary>
    /// 获取或设置配置方案的稳定标识，用于独立文件保存和切换。
    /// </summary>
    public string ProfileId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 获取或设置用户可见的配置方案名称。
    /// </summary>
    public string Name { get; set; } = "本地开发";

    /// <summary>
    /// 获取或设置配置方案的用途说明。
    /// </summary>
    public string Purpose { get; set; } = "用于本机开发构建。";

    /// <summary>
    /// 获取或设置项目构建信息。
    /// </summary>
    public ProjectDefinition Project { get; set; } = new();

    /// <summary>
    /// 获取或设置目标环境。
    /// </summary>
    public EnvironmentDefinition Environment { get; set; } = new();

    /// <summary>
    /// 获取或设置当前选择的发布操作。
    /// </summary>
    public DeploymentOperation Operation { get; set; } = DeploymentOperation.FirstInstall;

    /// <summary>
    /// 获取或设置扩容、修复或下线操作的目标实例标识。
    /// </summary>
    public string TargetInstanceId { get; set; } = string.Empty;

    #endregion
}
