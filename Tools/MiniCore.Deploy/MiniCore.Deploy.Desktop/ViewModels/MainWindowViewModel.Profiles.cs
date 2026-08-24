using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using MiniCore.Deploy.Core.Models;
using MiniCore.Deploy.Infrastructure.Persistence;

namespace MiniCore.Deploy.Desktop.ViewModels;

/// <summary>
/// 提供多配置方案切换、编辑和独立持久化能力。
/// </summary>
public sealed partial class MainWindowViewModel
{
    #region Private 私有成员

    private DeploymentProfileItemViewModel? selectedProfileItem; // 当前活动方案列表项。
    private int selectedPageIndex; // 当前工作流页面索引。

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 获取全部可切换的配置方案。
    /// </summary>
    public ObservableCollection<DeploymentProfileItemViewModel> Profiles { get; } = new();

    /// <summary>
    /// 获取或设置当前活动配置方案。
    /// </summary>
    public DeploymentProfileItemViewModel? SelectedProfileItem
    {
        get => selectedProfileItem;
        set
        {
            if (value == null || ReferenceEquals(selectedProfileItem, value))
            {
                return;
            }

            if (selectedProfileItem != null)
            {
                CommitCollections();
            }

            selectedProfileItem = value;
            profile = value.Model;
            selectedTargetInstanceId = profile.TargetInstanceId;
            profileStore.SetActive(profile.ProfileId);
            RefreshModuleAvailability();
            LoadRoleCatalog();
            SynchronizeCollectionsFromProfile();
            InvalidatePreview();
            RaiseAllConfigurationProperties();
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(CurrentProfileName));
            SaveStateText = "已保存";
            StatusMessage = $"已切换到配置方案“{profile.Name}”。";
        }
    }

    /// <summary>
    /// 获取顶部显示的当前方案名称。
    /// </summary>
    public string CurrentProfileName => profile.Name;

    /// <summary>
    /// 获取或设置当前方案名称。
    /// </summary>
    public string ProfileName
    {
        get => profile.Name;
        set
        {
            if (string.Equals(profile.Name, value, StringComparison.Ordinal))
            {
                return;
            }

            profile.Name = value;
            RefreshSelectedProfileItem();
            RaisePropertyChanged(nameof(CurrentProfileName));
            InvalidatePreview();
        }
    }

    /// <summary>
    /// 获取或设置当前方案用途说明。
    /// </summary>
    public string ProfilePurpose
    {
        get => profile.Purpose;
        set
        {
            if (string.Equals(profile.Purpose, value, StringComparison.Ordinal))
            {
                return;
            }

            profile.Purpose = value;
            RefreshSelectedProfileItem();
            InvalidatePreview();
        }
    }

    /// <summary>
    /// 获取当前方案的独立配置文件路径。
    /// </summary>
    public string ProfileFilePath => profileStore.GetProfilePath(profile.ProfileId);

    /// <summary>
    /// 获取当前方案包含的主机、实例和构建目标摘要。
    /// </summary>
    public string ProfileSummary => $"{Hosts.Count} 台主机 · {Instances.Count} 个实例 · {profile.Project.BuildTargets.Count} 个构建目标 · {profile.Project.PublishTargets.Count} 个发布目标";

    /// <summary>
    /// 获取或设置当前工作流页面索引。
    /// </summary>
    public int SelectedPageIndex
    {
        get => selectedPageIndex;
        set
        {
            if (!SetProperty(ref selectedPageIndex, value))
            {
                return;
            }

            if (value == 4)
            {
                RefreshBuildTargetSelections();
            }

            RaisePropertyChanged(nameof(CurrentHelpTitle));
            RaisePropertyChanged(nameof(CurrentHelpText));
            RaisePropertyChanged(nameof(CurrentHelpTip));
        }
    }

    /// <summary>
    /// 获取右侧上下文帮助标题。
    /// </summary>
    public string CurrentHelpTitle => SelectedPageIndex switch
    {
        0 => "主机管理",
        1 => "项目与 Unity",
        2 => "配置方案",
        3 => "服务拓扑",
        4 => "构建目标",
        5 => "发布方式",
        6 => "计划预览",
        7 => "执行中心",
        8 => "发布历史",
        _ => "帮助与文档"
    };

    /// <summary>
    /// 获取右侧上下文帮助正文。
    /// </summary>
    public string CurrentHelpText => SelectedPageIndex switch
    {
        0 => "先登记允许通过 SSH 管理的目标机器，并单独填写供服务互访的 VPC 地址。SSH 地址用于部署连接，VPC 地址由实例默认继承。",
        1 => "设置 Unity 可执行程序、项目根目录、制品目录和显式启动场景。构建时会启动独立 BatchMode。",
        2 => "每份方案都由开发运维人员自由命名，并独立保存自己的主机、拓扑、构建目标和发布策略。",
        3 => "ListenHost 只控制本机绑定；内网公布地址留空时跟随主机 VPC，也可实例覆盖；OuterAdvertisedUrl 是客户端实际访问的 HTTPS/WSS 地址。",
        4 => "构建会生成新制品，发布会使用所选制品。两列可以独立选择，服务端、客户端和资源互不强制。",
        5 => "选择首次安装、滚动更新、扩容、修复、回滚或下线等确定性流程。",
        6 => "执行前必须先看到完整计划。未预览的配置变化不会被直接执行。",
        7 => "每一步显示状态、失败原因、日志位置和恢复建议；风险步骤会等待人工确认。",
        8 => "历史保存在操作系统应用数据目录，不会把生产地址或密钥写入仓库。",
        _ => "这里集中说明配置、安全边界、构建与发布的基本概念。"
    };

    /// <summary>
    /// 获取右侧上下文帮助中的推荐设置。
    /// </summary>
    public string CurrentHelpTip => SelectedPageIndex switch
    {
        0 => "阿里云 Linux 通常使用 root 或 ecs-user；优先选择 ecs-user 与 SSH 私钥。VPC 地址不要填写 127.0.0.1，也不会自动替代监听地址。",
        2 => "建议按用途自由命名，例如本地联调、国服滚动更新或海外灰度，不再强制套用方案分类。",
        3 => "小型项目可让 Lobby、Room、Match、Game 共用一个 DS；Auth/DB 没有业务 Role，StaticContent 也不启动服务进程。",
        4 => "“仅服务端”会选择当前拓扑已启用的 DS、Auth 和 DB；禁用或删除最后一个 Auth/DB 实例会立即清除对应失效目标。",
        5 => "Coordinator、某 Role 最后实例和可能造成停服的步骤必须保留人工确认。",
        _ => "先填写必需字段，再生成计划预览；不要直接在远程机器上临时改动运行文件。"
    };

    /// <summary>
    /// 获取新建方案命令。
    /// </summary>
    public ICommand NewProfileCommand { get; private set; } = null!;

    /// <summary>
    /// 获取复制当前方案命令。
    /// </summary>
    public ICommand DuplicateProfileCommand { get; private set; } = null!;

    /// <summary>
    /// 获取删除当前方案命令。
    /// </summary>
    public ICommand DeleteProfileCommand { get; private set; } = null!;

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 初始化配置方案相关命令。
    /// </summary>
    private void InitializeProfileCommands()
    {
        NewProfileCommand = new AsyncRelayCommand(CreateProfileAsync);
        DuplicateProfileCommand = new AsyncRelayCommand(DuplicateProfileAsync);
        DeleteProfileCommand = new AsyncRelayCommand(DeleteProfileAsync);
    }

    /// <summary>
    /// 把存储快照装载为可绑定方案列表并选择活动方案。
    /// </summary>
    /// <param name="snapshot">配置存储快照。</param>
    private void LoadProfileSnapshot(ProfileStoreSnapshot snapshot)
    {
        Profiles.Clear();
        DeploymentProfileItemViewModel? active = null;
        for (int index = 0; index < snapshot.Profiles.Count; index++)
        {
            var item = new DeploymentProfileItemViewModel(snapshot.Profiles[index]);
            Profiles.Add(item);
            if (string.Equals(item.ProfileId, snapshot.ActiveProfileId, StringComparison.Ordinal))
            {
                active = item;
            }
        }

        selectedProfileItem = active ?? Profiles[0];
        profile = selectedProfileItem.Model;
        RaisePropertyChanged(nameof(SelectedProfileItem));
        RaisePropertyChanged(nameof(CurrentProfileName));
    }

    /// <summary>
    /// 新建一份复用当前项目路径但不复制远程拓扑的配置方案。
    /// </summary>
    /// <returns>创建完成任务。</returns>
    private async Task CreateProfileAsync()
    {
        CommitCollections();
        await profileStore.SaveAsync(profile, CancellationToken.None).ConfigureAwait(true);
        DeploymentProfile created = ProfileStore.CreateDefaultProfile();
        created.Name = CreateUniqueProfileName("新配置方案");
        created.Purpose = "请填写这份配置方案的用途。";
        created.Project.UnityExecutablePath = profile.Project.UnityExecutablePath;
        created.Project.ProjectPath = profile.Project.ProjectPath;
        created.Project.OutputPath = profile.Project.OutputPath;
        created.Project.ClientScenePath = profile.Project.ClientScenePath;
        created.Project.ServerScenePath = profile.Project.ServerScenePath;
        await profileStore.SaveAsync(created, CancellationToken.None).ConfigureAwait(true);
        var item = new DeploymentProfileItemViewModel(created);
        Profiles.Add(item);
        SelectedProfileItem = item;
        SaveStateText = "已保存";
        StatusMessage = $"已创建“{created.Name}”，它不会覆盖其他配置方案。";
    }

    /// <summary>
    /// 复制当前完整方案并为副本生成新的稳定标识。
    /// </summary>
    /// <returns>复制完成任务。</returns>
    private async Task DuplicateProfileAsync()
    {
        CommitCollections();
        string json = JsonSerializer.Serialize(profile);
        DeploymentProfile copy = JsonSerializer.Deserialize<DeploymentProfile>(json)
            ?? throw new InvalidDataException("当前配置方案无法复制。");
        copy.ProfileId = Guid.NewGuid().ToString("N");
        copy.Name = CreateUniqueProfileName(profile.Name + " 副本");
        await profileStore.SaveAsync(copy, CancellationToken.None).ConfigureAwait(true);
        var item = new DeploymentProfileItemViewModel(copy);
        Profiles.Add(item);
        SelectedProfileItem = item;
        SaveStateText = "已保存";
        StatusMessage = $"已复制为“{copy.Name}”。";
    }

    /// <summary>
    /// 经用户确认后删除当前本地配置方案，并切换到剩余方案。
    /// </summary>
    /// <returns>删除完成任务。</returns>
    private async Task DeleteProfileAsync()
    {
        if (Profiles.Count <= 1)
        {
            StatusMessage = "至少需要保留一份配置方案。";
            return;
        }

        var request = new ProfileDeletionRequestEventArgs(profile);
        ProfileDeletionRequested?.Invoke(this, request);
        if (ProfileDeletionRequested == null || !await request.Completion.Task.ConfigureAwait(true))
        {
            StatusMessage = "已取消删除配置方案。";
            return;
        }

        DeploymentProfileItemViewModel deleting = selectedProfileItem!;
        int deletingIndex = Profiles.IndexOf(deleting);
        DeploymentProfileItemViewModel next = Profiles[deletingIndex == 0 ? 1 : deletingIndex - 1];
        Profiles.Remove(deleting);
        await profileStore.DeleteAsync(deleting.ProfileId).ConfigureAwait(true);
        SelectedProfileItem = next;
        await profileStore.SetActiveAsync(next.ProfileId, CancellationToken.None).ConfigureAwait(true);
        StatusMessage = $"已删除本地配置方案“{deleting.Name}”；远程服务、制品和日志均未改动。";
    }

    /// <summary>
    /// 生成不与现有方案重名的用户可见名称。
    /// </summary>
    /// <param name="baseName">基础名称。</param>
    /// <returns>唯一名称。</returns>
    private string CreateUniqueProfileName(string baseName)
    {
        string candidate = baseName;
        int suffix = 2;
        while (ContainsProfileName(candidate))
        {
            candidate = baseName + " " + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
            suffix++;
        }

        return candidate;
    }

    /// <summary>
    /// 判断方案列表中是否已经存在指定名称。
    /// </summary>
    /// <param name="name">候选名称。</param>
    /// <returns>存在同名方案时返回 true。</returns>
    private bool ContainsProfileName(string name)
    {
        for (int index = 0; index < Profiles.Count; index++)
        {
            if (string.Equals(Profiles[index].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 刷新当前方案在顶部选择器和方案列表中的显示信息。
    /// </summary>
    private void RefreshSelectedProfileItem()
    {
        selectedProfileItem?.Refresh();
        RaisePropertyChanged(nameof(CurrentProfileName));
        RaisePropertyChanged(nameof(ProfileFilePath));
        RaisePropertyChanged(nameof(ProfileSummary));
    }

    #endregion
}
