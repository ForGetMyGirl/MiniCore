namespace MiniCore.Deploy.Desktop.ViewModels;

/// <summary>
/// 表示实例 Role 多选列表中的一个稳定选项。
/// </summary>
public sealed class RoleOptionViewModel : ObservableObject
{
    #region Private 私有成员

    private bool isSelected; // 当前实例是否启用该 Role。

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 获取稳定 Role 键。
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// 获取界面显示文本。
    /// </summary>
    public string DisplayText { get; }

    /// <summary>
    /// 获取或设置当前实例是否选择该 Role。
    /// </summary>
    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }

    /// <summary>
    /// 创建一个 Role 选择项。
    /// </summary>
    /// <param name="key">稳定 Role 键。</param>
    /// <param name="displayName">显示名称。</param>
    /// <param name="selected">初始选择状态。</param>
    public RoleOptionViewModel(string key, string displayName, bool selected)
    {
        Key = key;
        DisplayText = string.IsNullOrWhiteSpace(displayName) || string.Equals(key, displayName, StringComparison.Ordinal)
            ? key
            : $"{displayName} ({key})";
        isSelected = selected;
    }

    #endregion
}
