using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Desktop.ViewModels;

/// <summary>
/// 承载删除配置方案前的用户确认请求。
/// </summary>
public sealed class ProfileDeletionRequestEventArgs : EventArgs
{
    #region Public 公共成员

    /// <summary>
    /// 获取待删除配置方案。
    /// </summary>
    public DeploymentProfile Profile { get; }

    /// <summary>
    /// 获取由主窗口完成的确认结果任务。
    /// </summary>
    public TaskCompletionSource<bool> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// 创建删除确认请求。
    /// </summary>
    /// <param name="profile">待删除配置方案。</param>
    public ProfileDeletionRequestEventArgs(DeploymentProfile profile)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    #endregion
}
