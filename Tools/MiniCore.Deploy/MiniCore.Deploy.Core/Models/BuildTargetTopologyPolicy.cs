namespace MiniCore.Deploy.Core.Models;

/// <summary>
/// 计算可选服务端制品与当前启用拓扑之间的确定性约束。
/// </summary>
public static class BuildTargetTopologyPolicy
{
    #region Public 公共成员

    /// <summary>
    /// 判断 Auth 或 DB 构建目标是否存在对应的启用实例。
    /// </summary>
    /// <param name="instances">当前期望拓扑。</param>
    /// <param name="target">待检查的可选服务目标。</param>
    /// <returns>拓扑中存在对应启用实例时返回 true。</returns>
    public static bool IsOptionalComponentEnabled(
        IReadOnlyList<InstanceDefinition> instances,
        BuildTargetKind target)
    {
        ArgumentNullException.ThrowIfNull(instances);
        ComponentKind expected = target switch
        {
            BuildTargetKind.AuthenticationServer => ComponentKind.AuthenticationServer,
            BuildTargetKind.DatabaseServer => ComponentKind.DatabaseServer,
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, "只有 Auth/DB 使用可选拓扑策略。")
        };

        for (int index = 0; index < instances.Count; index++)
        {
            InstanceDefinition instance = instances[index];
            if (instance.Enabled && instance.Component == expected)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 从构建与发布选择中移除当前拓扑已经不再启用的 Auth/DB 目标。
    /// </summary>
    /// <param name="project">待同步的项目发布选择。</param>
    /// <param name="instances">当前期望拓扑。</param>
    public static void RemoveUnavailableOptionalTargets(
        ProjectDefinition project,
        IReadOnlyList<InstanceDefinition> instances)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(instances);
        RemoveIfUnavailable(project, instances, BuildTargetKind.AuthenticationServer);
        RemoveIfUnavailable(project, instances, BuildTargetKind.DatabaseServer);
    }

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 在对应拓扑不存在时移除单个可选目标。
    /// </summary>
    /// <param name="project">待同步项目。</param>
    /// <param name="instances">当前期望拓扑。</param>
    /// <param name="target">Auth 或 DB 目标。</param>
    private static void RemoveIfUnavailable(
        ProjectDefinition project,
        IReadOnlyList<InstanceDefinition> instances,
        BuildTargetKind target)
    {
        if (IsOptionalComponentEnabled(instances, target))
        {
            return;
        }

        project.BuildTargets.Remove(target);
        project.PublishTargets.Remove(target);
    }

    #endregion
}
