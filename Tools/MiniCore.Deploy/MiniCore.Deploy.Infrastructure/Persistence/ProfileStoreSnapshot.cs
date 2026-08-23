using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Infrastructure.Persistence;

/// <summary>
/// 表示配置存储加载出的全部方案和当前活动方案。
/// </summary>
public sealed class ProfileStoreSnapshot
{
    #region Public 公共成员

    /// <summary>
    /// 获取加载出的配置方案。
    /// </summary>
    public IReadOnlyList<DeploymentProfile> Profiles { get; }

    /// <summary>
    /// 获取当前活动配置方案标识。
    /// </summary>
    public string ActiveProfileId { get; }

    /// <summary>
    /// 创建配置存储快照。
    /// </summary>
    /// <param name="profiles">全部配置方案。</param>
    /// <param name="activeProfileId">活动配置方案标识。</param>
    public ProfileStoreSnapshot(IReadOnlyList<DeploymentProfile> profiles, string activeProfileId)
    {
        Profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        ActiveProfileId = activeProfileId ?? string.Empty;
    }

    #endregion
}
