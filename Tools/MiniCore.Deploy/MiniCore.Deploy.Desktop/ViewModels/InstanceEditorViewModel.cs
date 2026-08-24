using System.Collections.ObjectModel;
using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Desktop.ViewModels;

/// <summary>
/// 为实例模型补充由项目 Role Catalog 驱动的多选项。
/// </summary>
public sealed class InstanceEditorViewModel : ObservableObject
{
    #region Private 私有成员

    private string roleText; // 界面编辑中的 Role 文本。
    private readonly IReadOnlyList<HostDefinition> hosts; // 当前方案可选择的主机。

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 获取底层实例模型。
    /// </summary>
    public InstanceDefinition Model { get; }

    /// <summary>
    /// 获取当前项目可选择的 Role。
    /// </summary>
    public ObservableCollection<RoleOptionViewModel> RoleOptions { get; } = new();

    /// <summary>
    /// 获取 MySQL 常用 SSL 模式选项。
    /// </summary>
    public IReadOnlyList<string> DatabaseSslModes { get; } = new[]
    {
        "Required",
        "Preferred",
        "VerifyCA",
        "VerifyFull",
        "Disabled"
    };

    /// <summary>
    /// 获取或设置承载实例的主机标识；切换主机不会覆盖显式实例地址。
    /// </summary>
    public string HostId
    {
        get => Model.HostId;
        set
        {
            if (string.Equals(Model.HostId, value, StringComparison.Ordinal))
            {
                return;
            }

            Model.HostId = value;
            RaisePropertyChanged();
            RefreshHostDerivedValues();
        }
    }

    /// <summary>
    /// 获取或设置实例级内网公布地址覆盖；留空表示跟随所选主机。
    /// </summary>
    public string InnerAdvertisedHostOverride
    {
        get => Model.InnerAdvertisedHost;
        set
        {
            if (string.Equals(Model.InnerAdvertisedHost, value, StringComparison.Ordinal))
            {
                return;
            }

            Model.InnerAdvertisedHost = value;
            RaisePropertyChanged();
            RefreshHostDerivedValues();
        }
    }

    /// <summary>
    /// 获取实例覆盖或主机 VPC 继承后实际写入运行配置的内网公布地址。
    /// </summary>
    public string EffectiveInnerAdvertisedHost =>
        InstanceNetworkAddressResolver.ResolveInnerAdvertisedHost(hosts, Model);

    /// <summary>
    /// 获取当前内网公布地址的继承状态说明。
    /// </summary>
    public string InnerAdvertisedHostSourceText =>
        InstanceNetworkAddressResolver.UsesHostPrivateAddress(Model)
            ? string.IsNullOrWhiteSpace(EffectiveInnerAdvertisedHost)
                ? "正在跟随所选主机；请先在主机管理填写 VPC 地址。"
                : "跟随所选主机 VPC 地址，当前有效值：" + EffectiveInnerAdvertisedHost
            : "使用实例级覆盖；切换主机时会保留此值。";

    /// <summary>
    /// 获取或设置实例是否参与当前期望拓扑。
    /// </summary>
    public bool Enabled
    {
        get => Model.Enabled;
        set
        {
            if (Model.Enabled == value)
            {
                return;
            }

            Model.Enabled = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// 获取或设置当前实例的组件类型，并同步组件专属界面状态。
    /// </summary>
    public ComponentKind Component
    {
        get => Model.Component;
        set
        {
            if (Model.Component == value)
            {
                return;
            }

            Model.Component = value;
            NormalizeRolesForComponent();
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(ShowsRoles));
            RaisePropertyChanged(nameof(ShowsDedicatedServerSettings));
            RaisePropertyChanged(nameof(ShowsAuthenticationSettings));
            RaisePropertyChanged(nameof(ShowsDatabaseSettings));
            RaisePropertyChanged(nameof(ShowsStaticContentSettings));
            RaisePropertyChanged(nameof(ShowsProcessSettings));
            RaisePropertyChanged(nameof(ComponentExplanation));
        }
    }

    /// <summary>
    /// 获取当前组件是否允许选择框架或业务 Role。
    /// </summary>
    public bool ShowsRoles => Component is ComponentKind.Coordinator or ComponentKind.DedicatedServer;

    /// <summary>
    /// 获取当前组件是否使用 Dedicated Server 内外网与管理配置。
    /// </summary>
    public bool ShowsDedicatedServerSettings => ShowsRoles;

    /// <summary>
    /// 获取当前组件是否使用 AuthenticationServer HTTP 配置。
    /// </summary>
    public bool ShowsAuthenticationSettings => Component == ComponentKind.AuthenticationServer;

    /// <summary>
    /// 获取当前组件是否使用 DatabaseServer 内网 RPC 配置。
    /// </summary>
    public bool ShowsDatabaseSettings => Component == ComponentKind.DatabaseServer;

    /// <summary>
    /// 获取当前组件是否表示无进程的静态内容版本指针。
    /// </summary>
    public bool ShowsStaticContentSettings => Component == ComponentKind.StaticContent;

    /// <summary>
    /// 获取当前组件是否由 systemd 或 Windows 服务管理器托管进程。
    /// </summary>
    public bool ShowsProcessSettings => !ShowsStaticContentSettings;

    /// <summary>
    /// 获取当前组件类型与 Role、端口关系的界面说明。
    /// </summary>
    public string ComponentExplanation => Component switch
    {
        ComponentKind.Coordinator => "Coordinator 是框架保留控制面；也可以在同一进程中额外勾选业务 Role。",
        ComponentKind.DedicatedServer => "普通 DS 可同时承载多个项目业务 Role，但不能选择 Coordinator。",
        ComponentKind.AuthenticationServer => "Auth 是独立 HTTP 服务，不属于 DS Role；它向客户端返回 Coordinator 的外网地址。",
        ComponentKind.DatabaseServer => "DB 使用框架固定的 Database 服务标识自动注册，不属于项目业务 Role。",
        ComponentKind.StaticContent => "静态内容不是服务器进程，只把 WebGL/YooAsset 目录原子切换到指定远程路径。",
        _ => string.Empty
    };

    /// <summary>
    /// 获取或设置逗号分隔的稳定 Role 标识。
    /// </summary>
    public string RoleText
    {
        get => roleText;
        set => SetProperty(ref roleText, value);
    }

    /// <summary>
    /// 创建实例编辑模型。
    /// </summary>
    /// <param name="model">底层实例模型。</param>
    /// <param name="hosts">当前方案可选择的主机集合。</param>
    public InstanceEditorViewModel(InstanceDefinition model, IReadOnlyList<HostDefinition> hosts)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        this.hosts = hosts ?? throw new ArgumentNullException(nameof(hosts));
        roleText = string.Join(",", model.Roles);
    }

    /// <summary>
    /// 通知界面重新显示由主机选择或 VPC 地址派生的有效地址。
    /// </summary>
    public void RefreshHostDerivedValues()
    {
        RaisePropertyChanged(nameof(EffectiveInnerAdvertisedHost));
        RaisePropertyChanged(nameof(InnerAdvertisedHostSourceText));
    }

    /// <summary>
    /// 使用当前项目 Role Catalog 重建多选项，并保留目录中暂时未知的旧 Role。
    /// </summary>
    /// <param name="catalog">项目 Role Catalog。</param>
    public void SetRoleCatalog(IReadOnlyList<RoleCatalogItem> catalog)
    {
        RoleOptions.Clear();
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < catalog.Count; index++)
        {
            RoleCatalogItem item = catalog[index];
            known.Add(item.Key);
            RoleOptions.Add(new RoleOptionViewModel(item.Key, item.DisplayName, ContainsRole(item.Key)));
        }

        for (int index = 0; index < Model.Roles.Count; index++)
        {
            string role = Model.Roles[index];
            if (!known.Contains(role))
            {
                RoleOptions.Add(new RoleOptionViewModel(role, "目录缺失", true));
            }
        }
    }

    /// <summary>
    /// 将 Role 文本去空白、去重后同步到底层模型。
    /// </summary>
    public void CommitRoles()
    {
        Model.Roles.Clear();
        if (!ShowsRoles)
        {
            roleText = string.Empty;
            return;
        }

        if (RoleOptions.Count > 0)
        {
            for (int index = 0; index < RoleOptions.Count; index++)
            {
                RoleOptionViewModel option = RoleOptions[index];
                if (option.IsSelected)
                {
                    Model.Roles.Add(option.Key);
                }
            }

            roleText = string.Join(",", Model.Roles);
            return;
        }

        string[] values = roleText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var unique = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < values.Length; index++)
        {
            if (unique.Add(values[index]))
            {
                Model.Roles.Add(values[index]);
            }
        }
    }

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 判断底层模型是否已经包含指定 Role。
    /// </summary>
    /// <param name="role">稳定 Role 键。</param>
    /// <returns>已包含时返回 true。</returns>
    private bool ContainsRole(string role)
    {
        for (int index = 0; index < Model.Roles.Count; index++)
        {
            if (string.Equals(Model.Roles[index], role, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 在组件类型变化后移除不适用 Role，并维护 Coordinator 保留位约束。
    /// </summary>
    private void NormalizeRolesForComponent()
    {
        if (!ShowsRoles)
        {
            Model.Roles.Clear();
            for (int index = 0; index < RoleOptions.Count; index++)
            {
                RoleOptions[index].IsSelected = false;
            }

            roleText = string.Empty;
            return;
        }

        for (int index = 0; index < RoleOptions.Count; index++)
        {
            RoleOptionViewModel option = RoleOptions[index];
            if (string.Equals(option.Key, "Coordinator", StringComparison.OrdinalIgnoreCase))
            {
                option.IsSelected = Component == ComponentKind.Coordinator;
            }
        }
    }

    #endregion
}
