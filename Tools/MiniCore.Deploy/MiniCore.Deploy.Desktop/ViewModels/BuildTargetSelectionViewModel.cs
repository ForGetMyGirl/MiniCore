using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Desktop.ViewModels;

/// <summary>
/// 表示一个制品目标在当前方案中独立的构建和发布选择。
/// </summary>
public sealed class BuildTargetSelectionViewModel : ObservableObject
{
    #region Private 私有成员

    private readonly Action<BuildTargetKind, bool> setBuildSelection; // 更新构建选择。
    private readonly Action<BuildTargetKind, bool> setPublishSelection; // 更新发布选择。
    private bool isBuildSelected; // 当前是否构建。
    private bool isPublishSelected; // 当前是否发布。
    private bool isBuildAvailable = true; // Unity 模块是否允许构建。
    private string availabilityText = "可用"; // 模块或拓扑状态摘要。

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 获取制品目标枚举。
    /// </summary>
    public BuildTargetKind Target { get; }

    /// <summary>
    /// 获取用户可见名称。
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// 获取目标用途说明。
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 获取当前 Unity 安装是否能够构建该目标。
    /// </summary>
    public bool IsBuildAvailable
    {
        get => isBuildAvailable;
        private set => SetProperty(ref isBuildAvailable, value);
    }

    /// <summary>
    /// 获取模块或拓扑状态摘要。
    /// </summary>
    public string AvailabilityText
    {
        get => availabilityText;
        private set => SetProperty(ref availabilityText, value);
    }

    /// <summary>
    /// 获取或设置是否生成新制品。
    /// </summary>
    public bool IsBuildSelected
    {
        get => isBuildSelected;
        set
        {
            if (!IsBuildAvailable && value)
            {
                return;
            }

            if (SetProperty(ref isBuildSelected, value))
            {
                setBuildSelection(Target, value);
            }
        }
    }

    /// <summary>
    /// 获取或设置是否发布该目标制品。
    /// </summary>
    public bool IsPublishSelected
    {
        get => isPublishSelected;
        set
        {
            if (SetProperty(ref isPublishSelected, value))
            {
                setPublishSelection(Target, value);
            }
        }
    }

    /// <summary>
    /// 创建目标选择项。
    /// </summary>
    /// <param name="target">制品目标。</param>
    /// <param name="title">用户可见名称。</param>
    /// <param name="description">用途说明。</param>
    /// <param name="setBuildSelection">构建选择回调。</param>
    /// <param name="setPublishSelection">发布选择回调。</param>
    public BuildTargetSelectionViewModel(
        BuildTargetKind target,
        string title,
        string description,
        Action<BuildTargetKind, bool> setBuildSelection,
        Action<BuildTargetKind, bool> setPublishSelection)
    {
        Target = target;
        Title = title;
        Description = description;
        this.setBuildSelection = setBuildSelection ?? throw new ArgumentNullException(nameof(setBuildSelection));
        this.setPublishSelection = setPublishSelection ?? throw new ArgumentNullException(nameof(setPublishSelection));
    }

    /// <summary>
    /// 从活动配置方案刷新选择和模块状态，不触发回写回调。
    /// </summary>
    /// <param name="buildSelected">是否构建。</param>
    /// <param name="publishSelected">是否发布。</param>
    /// <param name="buildAvailable">构建模块是否可用。</param>
    /// <param name="statusText">状态说明。</param>
    public void Refresh(bool buildSelected, bool publishSelected, bool buildAvailable, string statusText)
    {
        isBuildSelected = buildSelected;
        isPublishSelected = publishSelected;
        RaisePropertyChanged(nameof(IsBuildSelected));
        RaisePropertyChanged(nameof(IsPublishSelected));
        IsBuildAvailable = buildAvailable;
        AvailabilityText = statusText;
    }

    #endregion
}
