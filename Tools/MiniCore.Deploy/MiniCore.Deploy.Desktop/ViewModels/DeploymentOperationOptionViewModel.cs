using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Desktop.ViewModels;

/// <summary>
/// 为发布操作提供中文名称和面向开发运维人员的用途说明。
/// </summary>
public sealed class DeploymentOperationOptionViewModel
{
    #region Public 公共成员

    /// <summary>
    /// 获取发布操作枚举。
    /// </summary>
    public DeploymentOperation Operation { get; }

    /// <summary>
    /// 获取中文名称。
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// 获取操作用途说明。
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 获取操作影响范围说明。
    /// </summary>
    public string Impact { get; }

    /// <summary>
    /// 创建发布操作显示项。
    /// </summary>
    /// <param name="operation">发布操作。</param>
    /// <param name="title">中文名称。</param>
    /// <param name="description">用途说明。</param>
    /// <param name="impact">影响范围说明。</param>
    public DeploymentOperationOptionViewModel(
        DeploymentOperation operation,
        string title,
        string description,
        string impact)
    {
        Operation = operation;
        Title = title;
        Description = description;
        Impact = impact;
    }

    #endregion
}
