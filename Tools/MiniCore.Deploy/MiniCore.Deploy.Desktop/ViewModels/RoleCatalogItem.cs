namespace MiniCore.Deploy.Desktop.ViewModels;

/// <summary>
/// 保存从项目 ServerRoleCatalog.json 读取的一个可选 Role。
/// </summary>
public sealed class RoleCatalogItem
{
    #region Public 公共成员

    /// <summary>
    /// 获取稳定 Role 键。
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// 获取用户可读名称。
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// 获取该 Role 是否为框架保留项。
    /// </summary>
    public bool FrameworkReserved { get; init; }

    #endregion
}
