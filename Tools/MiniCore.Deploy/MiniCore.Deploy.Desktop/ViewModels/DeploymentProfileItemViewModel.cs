using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Desktop.ViewModels;

/// <summary>
/// 为配置方案列表提供可刷新且不泄露存储细节的显示信息。
/// </summary>
public sealed class DeploymentProfileItemViewModel : ObservableObject
{
    #region Public 公共成员

    /// <summary>
    /// 获取配置方案模型。
    /// </summary>
    public DeploymentProfile Model { get; }

    /// <summary>
    /// 获取方案标识。
    /// </summary>
    public string ProfileId => Model.ProfileId;

    /// <summary>
    /// 获取方案名称。
    /// </summary>
    public string Name => Model.Name;

    /// <summary>
    /// 获取用途说明。
    /// </summary>
    public string Purpose => Model.Purpose;

    /// <summary>
    /// 创建配置方案列表项。
    /// </summary>
    /// <param name="model">配置方案模型。</param>
    public DeploymentProfileItemViewModel(DeploymentProfile model)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <summary>
    /// 通知界面重新读取已经修改的方案名称和用途。
    /// </summary>
    public void Refresh()
    {
        RaisePropertyChanged(nameof(Name));
        RaisePropertyChanged(nameof(Purpose));
    }

    #endregion
}
