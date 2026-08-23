namespace MiniCore.Deploy.Core.Models;

/// <summary>
/// 描述发布计划中可持久化和重入的一个步骤。
/// </summary>
public sealed class DeploymentStep
{
    #region Public 公共成员

    /// <summary>
    /// 获取或设置计划内稳定步骤标识。
    /// </summary>
    public string StepId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置用户可读名称。
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置原子操作类型。
    /// </summary>
    public DeploymentAction Action { get; set; }

    /// <summary>
    /// 获取或设置目标主机标识；本地步骤为空。
    /// </summary>
    public string HostId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置目标实例标识；环境级步骤为空。
    /// </summary>
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置该步骤是否必须获得人工确认。
    /// </summary>
    public bool RequiresApproval { get; set; }

    /// <summary>
    /// 获取或设置可安全重复执行的次数上限。
    /// </summary>
    public int MaxAttempts { get; set; } = 1;

    #endregion
}
