using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Desktop.ViewModels;

/// <summary>
/// 将风险步骤和等待结果传递给主窗口确认对话框。
/// </summary>
public sealed class ApprovalRequestEventArgs : EventArgs
{
    #region Public 公共成员

    /// <summary>
    /// 获取需要确认的步骤。
    /// </summary>
    public DeploymentStep Step { get; }

    /// <summary>
    /// 获取用于返回用户选择的任务源。
    /// </summary>
    public TaskCompletionSource<bool> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// 创建风险确认请求。
    /// </summary>
    /// <param name="step">风险步骤。</param>
    public ApprovalRequestEventArgs(DeploymentStep step)
    {
        Step = step ?? throw new ArgumentNullException(nameof(step));
    }

    #endregion
}
