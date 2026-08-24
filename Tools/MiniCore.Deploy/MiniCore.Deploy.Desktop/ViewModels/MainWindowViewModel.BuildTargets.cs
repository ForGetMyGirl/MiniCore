using System.Collections.ObjectModel;
using System.Windows.Input;
using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Desktop.ViewModels;

/// <summary>
/// 提供服务端、客户端和资源构建范围的独立构建与发布选择。
/// </summary>
public sealed partial class MainWindowViewModel
{
    #region Public 公共成员

    /// <summary>
    /// 获取服务端制品目标。
    /// </summary>
    public ObservableCollection<BuildTargetSelectionViewModel> ServerTargetSelections { get; } = new();

    /// <summary>
    /// 获取客户端制品目标。
    /// </summary>
    public ObservableCollection<BuildTargetSelectionViewModel> ClientTargetSelections { get; } = new();

    /// <summary>
    /// 获取或设置是否只生成 HotUpdate 与 YooAsset 内容。
    /// </summary>
    public bool ContentOnly
    {
        get => profile.Project.ContentOnly;
        set
        {
            if (profile.Project.ContentOnly == value)
            {
                return;
            }

            profile.Project.ContentOnly = value;
            if (value)
            {
                profile.Operation = DeploymentOperation.BusinessRelease;
                RemoveDotNetTargets(profile.Project.BuildTargets);
                profile.Project.PublishTargets.Clear();
                RaisePropertyChanged(nameof(Operation));
            }

            RefreshBuildTargetSelections();
            RaisePropertyChanged();
            InvalidatePreview();
        }
    }

    /// <summary>
    /// 获取当前构建与发布目标数量摘要。
    /// </summary>
    public string BuildScopeSummary => $"将构建 {profile.Project.BuildTargets.Count} 项 · 将发布 {profile.Project.PublishTargets.Count} 项";

    /// <summary>
    /// 获取顶部主执行按钮随当前选择变化的文本。
    /// </summary>
    public string PrimaryExecutionActionText
    {
        get
        {
            bool builds = profile.Project.BuildTargets.Count > 0;
            bool publishes = profile.Project.PublishTargets.Count > 0;
            if (builds && publishes)
            {
                return "构建并发布";
            }

            if (builds)
            {
                return "仅构建";
            }

            return publishes ? "发布已有制品" : "请选择目标";
        }
    }

    /// <summary>
    /// 获取仅选择服务端目标命令。
    /// </summary>
    public ICommand SelectServerOnlyCommand { get; private set; } = null!;

    /// <summary>
    /// 获取仅选择客户端目标命令。
    /// </summary>
    public ICommand SelectClientOnlyCommand { get; private set; } = null!;

    /// <summary>
    /// 获取仅生成资源与热更新内容命令。
    /// </summary>
    public ICommand SelectContentOnlyCommand { get; private set; } = null!;

    /// <summary>
    /// 获取选择全部可构建目标命令。
    /// </summary>
    public ICommand SelectAllTargetsCommand { get; private set; } = null!;

    /// <summary>
    /// 获取清空构建与发布选择命令。
    /// </summary>
    public ICommand ClearTargetsCommand { get; private set; } = null!;

    /// <summary>
    /// 获取把当前目标设置为构建并发布命令。
    /// </summary>
    public ICommand UseBuildAndPublishCommand { get; private set; } = null!;

    /// <summary>
    /// 获取把当前目标设置为仅构建命令。
    /// </summary>
    public ICommand UseBuildOnlyCommand { get; private set; } = null!;

    /// <summary>
    /// 获取把当前目标设置为发布已有制品命令。
    /// </summary>
    public ICommand UseExistingArtifactsCommand { get; private set; } = null!;

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 创建目标列表和范围快捷命令。
    /// </summary>
    private void InitializeBuildTargetCommands()
    {
        ServerTargetSelections.Add(CreateTarget(BuildTargetKind.ServerLinuxX64, "Linux（Dedicated Server）", "Coordinator 与多 Role Dedicated Server"));
        ServerTargetSelections.Add(CreateTarget(BuildTargetKind.ServerWindowsX64, "Windows（Dedicated Server）", "Coordinator 与多 Role Dedicated Server"));
        ServerTargetSelections.Add(CreateTarget(BuildTargetKind.AuthenticationServer, "认证服务（可选）", "仅在当前拓扑启用 Auth 时构建"));
        ServerTargetSelections.Add(CreateTarget(BuildTargetKind.DatabaseServer, "数据库服务（可选）", "仅在当前拓扑启用 DB 时构建"));
        ClientTargetSelections.Add(CreateTarget(BuildTargetKind.ClientWindowsX64, "Windows 客户端", "Windows x64 Player"));
        ClientTargetSelections.Add(CreateTarget(BuildTargetKind.ClientMacOS, "macOS 客户端", "macOS Player 应用"));
        ClientTargetSelections.Add(CreateTarget(BuildTargetKind.ClientAndroid, "Android 客户端", "输出 APK 或 AAB"));
        ClientTargetSelections.Add(CreateTarget(BuildTargetKind.ClientWebGL, "WebGL 客户端", "浏览器 Player 与静态目录"));

        SelectServerOnlyCommand = new RelayCommand(SelectServerOnly);
        SelectClientOnlyCommand = new RelayCommand(SelectClientOnly);
        SelectContentOnlyCommand = new RelayCommand(SelectContentOnly);
        SelectAllTargetsCommand = new RelayCommand(SelectAllTargets);
        ClearTargetsCommand = new RelayCommand(ClearTargets);
        UseBuildAndPublishCommand = new RelayCommand(UseBuildAndPublish);
        UseBuildOnlyCommand = new RelayCommand(UseBuildOnly);
        UseExistingArtifactsCommand = new RelayCommand(UseExistingArtifacts);
        RefreshBuildTargetSelections();
    }

    /// <summary>
    /// 创建一个绑定到当前活动方案的制品目标选择项。
    /// </summary>
    /// <param name="target">制品目标。</param>
    /// <param name="title">显示名称。</param>
    /// <param name="description">用途说明。</param>
    /// <returns>目标选择视图模型。</returns>
    private BuildTargetSelectionViewModel CreateTarget(BuildTargetKind target, string title, string description)
    {
        return new BuildTargetSelectionViewModel(target, title, description, SetBuildTarget, SetPublishTarget);
    }

    /// <summary>
    /// 更新一个目标是否参与构建。
    /// </summary>
    /// <param name="target">制品目标。</param>
    /// <param name="selected">是否参与构建。</param>
    private void SetBuildTarget(BuildTargetKind target, bool selected)
    {
        SetListTarget(profile.Project.BuildTargets, target, selected);
        InvalidatePreview();
    }

    /// <summary>
    /// 更新一个目标是否参与发布。
    /// </summary>
    /// <param name="target">制品目标。</param>
    /// <param name="selected">是否参与发布。</param>
    private void SetPublishTarget(BuildTargetKind target, bool selected)
    {
        SetListTarget(profile.Project.PublishTargets, target, selected);
        InvalidatePreview();
    }

    /// <summary>
    /// 仅选择当前 Unity 安装可用的服务端目标。
    /// </summary>
    private void SelectServerOnly()
    {
        RefreshBuildTargetSelections();
        SelectGroup(ServerTargetSelections);
        profile.Project.ContentOnly = false;
        CompleteScopeChange();
    }

    /// <summary>
    /// 仅选择当前 Unity 安装可用的客户端目标。
    /// </summary>
    private void SelectClientOnly()
    {
        SelectGroup(ClientTargetSelections);
        profile.Project.ContentOnly = false;
        CompleteScopeChange();
    }

    /// <summary>
    /// 保留当前平台目标并切换为不构建 Player 的业务内容发布。
    /// </summary>
    private void SelectContentOnly()
    {
        RemoveDotNetTargets(profile.Project.BuildTargets);
        if (!ContainsUnityTarget(profile.Project.BuildTargets))
        {
            for (int index = 0; index < profile.Project.PublishTargets.Count; index++)
            {
                BuildTargetKind target = profile.Project.PublishTargets[index];
                if (target is not (BuildTargetKind.AuthenticationServer or BuildTargetKind.DatabaseServer))
                {
                    SetListTarget(profile.Project.BuildTargets, target, true);
                }
            }

            if (!ContainsUnityTarget(profile.Project.BuildTargets))
            {
                SetListTarget(profile.Project.BuildTargets, GetFirstAvailableUnityTarget(), true);
            }
        }

        profile.Project.PublishTargets.Clear();
        profile.Project.ContentOnly = true;
        profile.Operation = DeploymentOperation.BusinessRelease;
        CompleteScopeChange();
        RaisePropertyChanged(nameof(Operation));
    }

    /// <summary>
    /// 选择全部可用服务端和客户端目标参与构建与发布。
    /// </summary>
    private void SelectAllTargets()
    {
        profile.Project.BuildTargets.Clear();
        profile.Project.PublishTargets.Clear();
        AddAvailableTargets(ServerTargetSelections);
        AddAvailableTargets(ClientTargetSelections);
        profile.Project.ContentOnly = false;
        CompleteScopeChange();
    }

    /// <summary>
    /// 清空全部构建和发布目标。
    /// </summary>
    private void ClearTargets()
    {
        profile.Project.BuildTargets.Clear();
        profile.Project.PublishTargets.Clear();
        profile.Project.ContentOnly = false;
        CompleteScopeChange();
    }

    /// <summary>
    /// 将构建与发布目标合并，使所有选中目标先构建再发布。
    /// </summary>
    private void UseBuildAndPublish()
    {
        var union = new List<BuildTargetKind>(profile.Project.BuildTargets);
        for (int index = 0; index < profile.Project.PublishTargets.Count; index++)
        {
            SetListTarget(union, profile.Project.PublishTargets[index], true);
        }

        CopyTargets(union, profile.Project.BuildTargets);
        CopyTargets(union, profile.Project.PublishTargets);
        CompleteScopeChange();
    }

    /// <summary>
    /// 保留构建目标并清空发布目标。
    /// </summary>
    private void UseBuildOnly()
    {
        if (profile.Project.BuildTargets.Count == 0)
        {
            CopyTargets(profile.Project.PublishTargets, profile.Project.BuildTargets);
        }

        profile.Project.PublishTargets.Clear();
        CompleteScopeChange();
    }

    /// <summary>
    /// 保留发布目标并清空构建目标，以复用目标版本已有制品。
    /// </summary>
    private void UseExistingArtifacts()
    {
        if (profile.Project.PublishTargets.Count == 0)
        {
            CopyTargets(profile.Project.BuildTargets, profile.Project.PublishTargets);
        }

        profile.Project.BuildTargets.Clear();
        profile.Project.ContentOnly = false;
        CompleteScopeChange();
    }

    /// <summary>
    /// 清空现有范围并选择一个分组内全部可构建目标。
    /// </summary>
    /// <param name="group">目标分组。</param>
    private void SelectGroup(IReadOnlyList<BuildTargetSelectionViewModel> group)
    {
        profile.Project.BuildTargets.Clear();
        profile.Project.PublishTargets.Clear();
        AddAvailableTargets(group);
    }

    /// <summary>
    /// 把分组内可构建目标同时加入构建与发布范围。
    /// </summary>
    /// <param name="group">目标分组。</param>
    private void AddAvailableTargets(IReadOnlyList<BuildTargetSelectionViewModel> group)
    {
        for (int index = 0; index < group.Count; index++)
        {
            if (!group[index].IsBuildAvailable)
            {
                continue;
            }

            SetListTarget(profile.Project.BuildTargets, group[index].Target, true);
            SetListTarget(profile.Project.PublishTargets, group[index].Target, true);
        }
    }

    /// <summary>
    /// 完成批量范围修改并统一刷新界面和计划状态。
    /// </summary>
    private void CompleteScopeChange()
    {
        RefreshBuildTargetSelections();
        RaisePropertyChanged(nameof(ContentOnly));
        InvalidatePreview();
    }

    /// <summary>
    /// 从活动方案和 Unity 模块检测结果刷新全部目标行。
    /// </summary>
    private void RefreshBuildTargetSelections()
    {
        buildTargetInstanceBuffer.Clear();
        for (int index = 0; index < Instances.Count; index++)
        {
            buildTargetInstanceBuffer.Add(Instances[index].Model);
        }

        BuildTargetTopologyPolicy.RemoveUnavailableOptionalTargets(profile.Project, buildTargetInstanceBuffer);
        RefreshTargetGroup(ServerTargetSelections);
        RefreshTargetGroup(ClientTargetSelections);
    }

    /// <summary>
    /// 刷新一个目标分组的选择和模块状态。
    /// </summary>
    /// <param name="group">目标分组。</param>
    private void RefreshTargetGroup(IReadOnlyList<BuildTargetSelectionViewModel> group)
    {
        for (int index = 0; index < group.Count; index++)
        {
            BuildTargetSelectionViewModel item = group[index];
            bool moduleAvailable = moduleAvailability.IsAvailable(item.Target);
            bool optionalComponent = item.Target is BuildTargetKind.AuthenticationServer or BuildTargetKind.DatabaseServer;
            bool topologyAvailable = !optionalComponent || HasEnabledComponent(item.Target);
            bool buildAvailable = moduleAvailable && topologyAvailable;
            bool publishAvailable = topologyAvailable && !profile.Project.ContentOnly;
            string status = GetTargetAvailabilityText(item.Target, moduleAvailable);
            if (optionalComponent)
            {
                status = topologyAvailable ? "拓扑已启用" : "不可用 · 当前拓扑未启用";
                if (!topologyAvailable)
                {
                    profile.Project.BuildTargets.Remove(item.Target);
                    profile.Project.PublishTargets.Remove(item.Target);
                }
            }

            if (profile.Project.ContentOnly)
            {
                profile.Project.PublishTargets.Remove(item.Target);
            }

            item.Refresh(
                profile.Project.BuildTargets.Contains(item.Target),
                profile.Project.PublishTargets.Contains(item.Target),
                buildAvailable,
                publishAvailable,
                status);
        }
    }

    /// <summary>
    /// 生成与 Unity Hub 模块命名一致的构建目标状态说明。
    /// </summary>
    /// <param name="target">构建目标。</param>
    /// <param name="available">对应模块是否可用。</param>
    /// <returns>用户可见状态说明。</returns>
    private static string GetTargetAvailabilityText(BuildTargetKind target, bool available)
    {
        string moduleName = target switch
        {
            BuildTargetKind.ServerLinuxX64 => "Linux Build Support（Dedicated Server）",
            BuildTargetKind.ServerWindowsX64 => "Windows Build Support（Dedicated Server）",
            BuildTargetKind.ClientWindowsX64 => "Windows Build Support",
            BuildTargetKind.ClientMacOS => "Mac Build Support",
            BuildTargetKind.ClientAndroid => "Android Build Support",
            BuildTargetKind.ClientWebGL => "WebGL Build Support",
            _ => "无需 Unity 模块"
        };
        return available ? "已安装 · " + moduleName : "未检测到 · " + moduleName;
    }

    /// <summary>
    /// 判断可选 .NET 目标是否已经出现在当前拓扑中。
    /// </summary>
    /// <param name="target">Auth 或 DB 构建目标。</param>
    /// <returns>拓扑中存在启用实例时返回 true。</returns>
    private bool HasEnabledComponent(BuildTargetKind target)
    {
        return BuildTargetTopologyPolicy.IsOptionalComponentEnabled(buildTargetInstanceBuffer, target);
    }

    /// <summary>
    /// 在列表中增加或移除目标并保持唯一。
    /// </summary>
    /// <param name="targets">目标列表。</param>
    /// <param name="target">待修改目标。</param>
    /// <param name="selected">是否保留。</param>
    private static void SetListTarget(ICollection<BuildTargetKind> targets, BuildTargetKind target, bool selected)
    {
        bool exists = targets.Contains(target);
        if (selected && !exists)
        {
            targets.Add(target);
        }
        else if (!selected && exists)
        {
            targets.Remove(target);
        }
    }

    /// <summary>
    /// 用源列表完整替换目标列表并去重。
    /// </summary>
    /// <param name="source">源目标。</param>
    /// <param name="destination">目标集合。</param>
    private static void CopyTargets(IReadOnlyList<BuildTargetKind> source, ICollection<BuildTargetKind> destination)
    {
        destination.Clear();
        for (int index = 0; index < source.Count; index++)
        {
            SetListTarget(destination, source[index], true);
        }
    }

    /// <summary>
    /// 从列表移除不属于 Unity Player 或资源平台的 .NET 服务目标。
    /// </summary>
    /// <param name="targets">待修改列表。</param>
    private static void RemoveDotNetTargets(ICollection<BuildTargetKind> targets)
    {
        targets.Remove(BuildTargetKind.AuthenticationServer);
        targets.Remove(BuildTargetKind.DatabaseServer);
    }

    /// <summary>
    /// 判断目标列表是否包含至少一个 Unity 平台。
    /// </summary>
    /// <param name="targets">目标列表。</param>
    /// <returns>包含 Unity 平台时返回 true。</returns>
    private static bool ContainsUnityTarget(IReadOnlyList<BuildTargetKind> targets)
    {
        for (int index = 0; index < targets.Count; index++)
        {
            if (targets[index] is not (BuildTargetKind.AuthenticationServer or BuildTargetKind.DatabaseServer))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 返回当前 Unity 安装可构建的第一个平台目标。
    /// </summary>
    /// <returns>可用平台目标。</returns>
    private BuildTargetKind GetFirstAvailableUnityTarget()
    {
        for (int index = 0; index < ServerTargetSelections.Count; index++)
        {
            BuildTargetSelectionViewModel item = ServerTargetSelections[index];
            if (item.IsBuildAvailable && item.Target is not (BuildTargetKind.AuthenticationServer or BuildTargetKind.DatabaseServer))
            {
                return item.Target;
            }
        }

        for (int index = 0; index < ClientTargetSelections.Count; index++)
        {
            if (ClientTargetSelections[index].IsBuildAvailable)
            {
                return ClientTargetSelections[index].Target;
            }
        }

        throw new InvalidOperationException("当前 Unity 安装没有可用于资源构建的平台模块。");
    }

    #endregion
}
