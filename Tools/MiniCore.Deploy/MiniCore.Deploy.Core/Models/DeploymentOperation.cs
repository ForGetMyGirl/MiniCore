namespace MiniCore.Deploy.Core.Models;

/// <summary>
/// 定义用户可选择的发布操作。
/// </summary>
public enum DeploymentOperation
{
    /// <summary>
    /// 完成构建、安装和首次启动。
    /// </summary>
    FirstInstall,

    /// <summary>
    /// 构建并滚动更新整个环境。
    /// </summary>
    FullRelease,

    /// <summary>
    /// 仅更新业务热更新内容和相应服务端制品。
    /// </summary>
    BusinessRelease,

    /// <summary>
    /// 在控制协议不兼容或环境无冗余时执行人工确认的维护窗口全停更新。
    /// </summary>
    MaintenanceRelease,

    /// <summary>
    /// 使用当前版本新增服务实例。
    /// </summary>
    ScaleOut,

    /// <summary>
    /// 更新实例配置并滚动重启。
    /// </summary>
    ConfigurationUpdate,

    /// <summary>
    /// 对照期望状态修复单个实例。
    /// </summary>
    Repair,

    /// <summary>
    /// 切换到历史版本。
    /// </summary>
    Rollback,

    /// <summary>
    /// 摘流量并下线实例。
    /// </summary>
    RemoveInstance
}
