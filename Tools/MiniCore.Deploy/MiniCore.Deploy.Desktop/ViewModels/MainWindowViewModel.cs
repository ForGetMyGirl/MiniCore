using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;
using MiniCore.Deploy.Core.Execution;
using MiniCore.Deploy.Core.Exceptions;
using MiniCore.Deploy.Core.Models;
using MiniCore.Deploy.Core.Planning;
using MiniCore.Deploy.Infrastructure.Build;
using MiniCore.Deploy.Infrastructure.Execution;
using MiniCore.Deploy.Infrastructure.Persistence;
using MiniCore.Deploy.Infrastructure.Processes;
using MiniCore.Deploy.Infrastructure.Remote;

namespace MiniCore.Deploy.Desktop.ViewModels;

/// <summary>
/// 连接可编辑发布配置、计划预览和确定性执行状态机。
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    #region Private 私有成员

    private static readonly JsonSerializerOptions HistoryJsonOptions = CreateHistoryJsonOptions(); // 兼容字符串枚举的历史 JSON 设置。
    private readonly ApplicationPaths paths; // 仓库外应用数据目录。
    private readonly ProfileStore profileStore; // 配置存储。
    private readonly DeploymentPlanStore planStore; // 已展示计划快照存储。
    private readonly DeploymentPlanBuilder planBuilder = new(); // 计划生成器。
    private readonly DeploymentOrchestrator orchestrator; // 发布状态机。
    private readonly List<RoleCatalogItem> roleCatalog = new(); // 当前项目 Role Catalog。
    private DeploymentProfile profile = new(); // 当前编辑配置。
    private DeploymentPlan? previewedPlan; // 已展示且尚未失效的计划。
    private UnityModuleAvailability moduleAvailability = new(); // 当前 Unity 安装的平台模块。
    private string previewFingerprint = string.Empty; // 预览时配置摘要。
    private string roleCatalogFingerprint = string.Empty; // 当前项目 Role Catalog 摘要。
    private string previewRoleCatalogFingerprint = string.Empty; // 计划预览锁定的 Role Catalog 摘要。
    private string statusMessage = "正在加载配置…"; // 底部状态摘要。
    private string roleCatalogStatus = "尚未读取项目 Role Catalog。"; // Role 目录状态。
    private string selectedTargetInstanceId = string.Empty; // 单实例操作目标。
    private CancellationTokenSource? executionCancellation; // 当前执行取消源。
    private string saveStateText = "正在加载"; // 当前方案保存状态。
    private bool isExecuting; // 当前是否正在执行已预览计划。
    private readonly List<DeploymentHistoryEntryViewModel> allHistoryEntries = new(); // 未筛选的发布历史。
    private readonly List<InstanceDefinition> buildTargetInstanceBuffer = new(); // 构建目标拓扑同步复用缓冲。
    private string historyEnvironmentFilter = string.Empty; // 环境筛选文本。
    private string historyVersionFilter = string.Empty; // 版本筛选文本。
    private string historyInstanceFilter = string.Empty; // 实例筛选文本。
    private string historyOperationFilter = string.Empty; // 操作筛选文本。
    private string historyResultFilter = string.Empty; // 结果筛选文本。

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 风险步骤需要主窗口显示确认对话框时触发。
    /// </summary>
    public event EventHandler<ApprovalRequestEventArgs>? ApprovalRequested;

    /// <summary>
    /// 删除配置方案前需要主窗口显示确认对话框时触发。
    /// </summary>
    public event EventHandler<ProfileDeletionRequestEventArgs>? ProfileDeletionRequested;

    /// <summary>
    /// 获取可绑定的主机集合。
    /// </summary>
    public ObservableCollection<HostDefinition> Hosts { get; } = new();

    /// <summary>
    /// 获取带认证状态和安全输入通知的主机编辑集合。
    /// </summary>
    public ObservableCollection<HostEditorViewModel> HostEditors { get; } = new();

    /// <summary>
    /// 获取可绑定的实例编辑集合。
    /// </summary>
    public ObservableCollection<InstanceEditorViewModel> Instances { get; } = new();

    /// <summary>
    /// 获取当前计划步骤。
    /// </summary>
    public ObservableCollection<DeploymentStep> PlanSteps { get; } = new();

    /// <summary>
    /// 获取执行中心的步骤结果。
    /// </summary>
    public ObservableCollection<StepResult> ExecutionResults { get; } = new();

    /// <summary>
    /// 获取已经解析并应用筛选条件的发布历史。
    /// </summary>
    public ObservableCollection<DeploymentHistoryEntryViewModel> HistoryEntries { get; } = new();

    /// <summary>
    /// 获取当前方案是否尚未添加主机。
    /// </summary>
    public bool HasNoHosts => Hosts.Count == 0;

    /// <summary>
    /// 获取当前方案是否尚未添加服务实例。
    /// </summary>
    public bool HasNoInstances => Instances.Count == 0;

    /// <summary>
    /// 获取当前是否尚未生成计划步骤。
    /// </summary>
    public bool HasNoPlanSteps => PlanSteps.Count == 0;

    /// <summary>
    /// 获取执行中心是否尚无步骤结果。
    /// </summary>
    public bool HasNoExecutionResults => ExecutionResults.Count == 0;

    /// <summary>
    /// 获取是否尚无本地发布历史。
    /// </summary>
    public bool HasNoHistory => HistoryEntries.Count == 0;

    /// <summary>
    /// 获取或设置历史环境筛选文本。
    /// </summary>
    public string HistoryEnvironmentFilter
    {
        get => historyEnvironmentFilter;
        set
        {
            if (SetProperty(ref historyEnvironmentFilter, value))
            {
                ApplyHistoryFilters();
            }
        }
    }

    /// <summary>
    /// 获取或设置历史版本筛选文本。
    /// </summary>
    public string HistoryVersionFilter
    {
        get => historyVersionFilter;
        set
        {
            if (SetProperty(ref historyVersionFilter, value))
            {
                ApplyHistoryFilters();
            }
        }
    }

    /// <summary>
    /// 获取或设置历史实例筛选文本。
    /// </summary>
    public string HistoryInstanceFilter
    {
        get => historyInstanceFilter;
        set
        {
            if (SetProperty(ref historyInstanceFilter, value))
            {
                ApplyHistoryFilters();
            }
        }
    }

    /// <summary>
    /// 获取或设置历史操作筛选文本。
    /// </summary>
    public string HistoryOperationFilter
    {
        get => historyOperationFilter;
        set
        {
            if (SetProperty(ref historyOperationFilter, value))
            {
                ApplyHistoryFilters();
            }
        }
    }

    /// <summary>
    /// 获取或设置历史结果筛选文本。
    /// </summary>
    public string HistoryResultFilter
    {
        get => historyResultFilter;
        set
        {
            if (SetProperty(ref historyResultFilter, value))
            {
                ApplyHistoryFilters();
            }
        }
    }

    /// <summary>
    /// 获取当前配置是否已经成功生成可执行计划预览。
    /// </summary>
    public bool HasPreviewedPlan => previewedPlan != null;

    /// <summary>
    /// 获取当前配置是否尚未生成计划预览。
    /// </summary>
    public bool HasNoPreviewedPlan => previewedPlan == null;

    /// <summary>
    /// 获取当前是否有正在执行的发布计划。
    /// </summary>
    public bool HasActiveExecution => isExecuting;

    /// <summary>
    /// 获取主机系统选项。
    /// </summary>
    public Array HostOperatingSystems { get; } = Enum.GetValues(typeof(HostOperatingSystem));

    /// <summary>
    /// 获取组件选项。
    /// </summary>
    public Array ComponentKinds { get; } = Enum.GetValues(typeof(ComponentKind));

    /// <summary>
    /// 获取或设置 Unity 路径。
    /// </summary>
    public string UnityExecutablePath
    {
        get => profile.Project.UnityExecutablePath;
        set
        {
            profile.Project.UnityExecutablePath = value;
            RefreshModuleAvailability();
            InvalidatePreview();
        }
    }

    /// <summary>
    /// 获取当前 Unity 项目的用户可见名称。
    /// </summary>
    public string ProjectDisplayName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(profile.Project.ProjectPath))
            {
                return "未选择项目";
            }

            string trimmedPath = Path.TrimEndingDirectorySeparator(profile.Project.ProjectPath);
            string name = Path.GetFileName(trimmedPath);
            return string.IsNullOrWhiteSpace(name) ? "Unity 项目" : name;
        }
    }

    /// <summary>
    /// 获取 Unity 平台模块检测摘要。
    /// </summary>
    public string UnityModuleSummary => moduleAvailability.Summary;

    /// <summary>
    /// 获取 Linux Dedicated Server 目标是否可选。
    /// </summary>
    public bool CanBuildServerLinux => moduleAvailability.ServerLinuxX64;

    /// <summary>
    /// 获取 Windows Dedicated Server 目标是否可选。
    /// </summary>
    public bool CanBuildServerWindows => moduleAvailability.ServerWindowsX64;

    /// <summary>
    /// 获取 Windows 客户端目标是否可选。
    /// </summary>
    public bool CanBuildClientWindows => moduleAvailability.ClientWindowsX64;

    /// <summary>
    /// 获取 macOS 客户端目标是否可选。
    /// </summary>
    public bool CanBuildClientMacOS => moduleAvailability.ClientMacOS;

    /// <summary>
    /// 获取 Android 客户端目标是否可选。
    /// </summary>
    public bool CanBuildClientAndroid => moduleAvailability.ClientAndroid;

    /// <summary>
    /// 获取 WebGL 客户端目标是否可选。
    /// </summary>
    public bool CanBuildClientWebGL => moduleAvailability.ClientWebGL;

    /// <summary>
    /// 获取或设置 Unity 项目路径。
    /// </summary>
    public string ProjectPath
    {
        get => profile.Project.ProjectPath;
        set
        {
            profile.Project.ProjectPath = value;
            LoadRoleCatalog();
            RaisePropertyChanged(nameof(ProjectDisplayName));
            InvalidatePreview();
        }
    }

    /// <summary>
    /// 获取项目 Role Catalog 的读取状态。
    /// </summary>
    public string RoleCatalogStatus
    {
        get => roleCatalogStatus;
        private set => SetProperty(ref roleCatalogStatus, value);
    }

    /// <summary>
    /// 获取或设置制品输出路径。
    /// </summary>
    public string OutputPath
    {
        get => profile.Project.OutputPath;
        set
        {
            profile.Project.OutputPath = value;
            InvalidatePreview();
        }
    }

    /// <summary>
    /// 获取或设置客户端启动场景。
    /// </summary>
    public string ClientScenePath
    {
        get => profile.Project.ClientScenePath;
        set
        {
            profile.Project.ClientScenePath = value;
            InvalidatePreview();
        }
    }

    /// <summary>
    /// 获取或设置服务端启动场景。
    /// </summary>
    public string ServerScenePath
    {
        get => profile.Project.ServerScenePath;
        set
        {
            profile.Project.ServerScenePath = value;
            InvalidatePreview();
        }
    }

    /// <summary>
    /// 获取或设置环境标识。
    /// </summary>
    public string EnvironmentId
    {
        get => profile.Environment.EnvironmentId;
        set
        {
            profile.Environment.EnvironmentId = value;
            InvalidatePreview();
        }
    }

    /// <summary>
    /// 获取或设置环境显示名称。
    /// </summary>
    public string EnvironmentDisplayName
    {
        get => profile.Environment.DisplayName;
        set => profile.Environment.DisplayName = value;
    }

    /// <summary>
    /// 获取或设置构建发布前是否强制要求 Git 工作区干净。
    /// </summary>
    public bool RequireCleanGitWorkspace
    {
        get => profile.Environment.RequireCleanGitWorkspace;
        set
        {
            if (profile.Environment.RequireCleanGitWorkspace == value)
            {
                return;
            }

            profile.Environment.RequireCleanGitWorkspace = value;
            InvalidatePreview();
        }
    }

    /// <summary>
    /// 获取或设置是否按生产规则强制校验客户端公网 HTTPS/WSS 地址。
    /// </summary>
    public bool EnforcePublicEndpointSafety
    {
        get => profile.Environment.EnforcePublicEndpointSafety;
        set
        {
            if (profile.Environment.EnforcePublicEndpointSafety == value)
            {
                return;
            }

            profile.Environment.EnforcePublicEndpointSafety = value;
            InvalidatePreview();
        }
    }

    /// <summary>
    /// 获取或设置环境统一版本。
    /// </summary>
    public string ReleaseVersion
    {
        get => profile.Environment.ReleaseVersion;
        set
        {
            profile.Environment.ReleaseVersion = value;
            RaisePropertyChanged(nameof(DatabaseMigrationReviewed));
            InvalidatePreview();
        }
    }

    /// <summary>
    /// 获取或设置当前发布版本是否已由开发运维人员完成数据库迁移评审。
    /// </summary>
    public bool DatabaseMigrationReviewed
    {
        get => string.Equals(
            profile.Environment.DatabaseMigrationReviewedReleaseVersion,
            profile.Environment.ReleaseVersion,
            StringComparison.Ordinal);
        set
        {
            profile.Environment.DatabaseMigrationReviewedReleaseVersion = value
                ? profile.Environment.ReleaseVersion
                : string.Empty;
            RaisePropertyChanged();
            InvalidatePreview();
        }
    }

    /// <summary>
    /// 获取或设置发布操作。
    /// </summary>
    public DeploymentOperation Operation
    {
        get => profile.Operation;
        set
        {
            profile.Operation = value;
            RaisePropertyChanged(nameof(SelectedDeploymentOperationOption));
            RaisePropertyChanged(nameof(SelectedOperationDescription));
            RaisePropertyChanged(nameof(SelectedOperationImpact));
            InvalidatePreview();
        }
    }

    /// <summary>
    /// 获取或设置单实例操作目标。
    /// </summary>
    public string SelectedTargetInstanceId
    {
        get => selectedTargetInstanceId;
        set
        {
            if (SetProperty(ref selectedTargetInstanceId, value))
            {
                profile.TargetInstanceId = value;
                InvalidatePreview();
            }
        }
    }

    /// <summary>
    /// 获取或设置 Linux DS 构建目标。
    /// </summary>
    public bool BuildServerLinux
    {
        get => HasTarget(BuildTargetKind.ServerLinuxX64);
        set => SetTarget(BuildTargetKind.ServerLinuxX64, value);
    }

    /// <summary>
    /// 获取或设置 Windows DS 构建目标。
    /// </summary>
    public bool BuildServerWindows
    {
        get => HasTarget(BuildTargetKind.ServerWindowsX64);
        set => SetTarget(BuildTargetKind.ServerWindowsX64, value);
    }

    /// <summary>
    /// 获取或设置 Windows 客户端构建目标。
    /// </summary>
    public bool BuildClientWindows
    {
        get => HasTarget(BuildTargetKind.ClientWindowsX64);
        set => SetTarget(BuildTargetKind.ClientWindowsX64, value);
    }

    /// <summary>
    /// 获取或设置 macOS 客户端构建目标。
    /// </summary>
    public bool BuildClientMacOS
    {
        get => HasTarget(BuildTargetKind.ClientMacOS);
        set => SetTarget(BuildTargetKind.ClientMacOS, value);
    }

    /// <summary>
    /// 获取或设置 Android 客户端构建目标。
    /// </summary>
    public bool BuildClientAndroid
    {
        get => HasTarget(BuildTargetKind.ClientAndroid);
        set => SetTarget(BuildTargetKind.ClientAndroid, value);
    }

    /// <summary>
    /// 获取或设置 Android 是否输出 AAB；关闭时输出 APK。
    /// </summary>
    public bool AndroidAppBundle
    {
        get => profile.Project.AndroidAppBundle;
        set
        {
            profile.Project.AndroidAppBundle = value;
            InvalidatePreview();
        }
    }

    /// <summary>
    /// 获取或设置 WebGL 客户端构建目标。
    /// </summary>
    public bool BuildClientWebGL
    {
        get => HasTarget(BuildTargetKind.ClientWebGL);
        set => SetTarget(BuildTargetKind.ClientWebGL, value);
    }

    /// <summary>
    /// 获取或设置可选 Auth 构建目标。
    /// </summary>
    public bool BuildAuthenticationServer
    {
        get => HasTarget(BuildTargetKind.AuthenticationServer);
        set => SetTarget(BuildTargetKind.AuthenticationServer, value);
    }

    /// <summary>
    /// 获取或设置可选 DB 构建目标。
    /// </summary>
    public bool BuildDatabaseServer
    {
        get => HasTarget(BuildTargetKind.DatabaseServer);
        set => SetTarget(BuildTargetKind.DatabaseServer, value);
    }

    /// <summary>
    /// 获取界面底部状态摘要。
    /// </summary>
    public string StatusMessage
    {
        get => statusMessage;
        private set => SetProperty(ref statusMessage, value);
    }

    /// <summary>
    /// 获取顶部显示的当前方案保存状态。
    /// </summary>
    public string SaveStateText
    {
        get => saveStateText;
        private set => SetProperty(ref saveStateText, value);
    }

    /// <summary>
    /// 获取保存配置命令。
    /// </summary>
    public ICommand SaveCommand { get; }

    /// <summary>
    /// 获取新增主机命令。
    /// </summary>
    public ICommand AddHostCommand { get; }

    /// <summary>
    /// 获取删除指定主机命令。
    /// </summary>
    public ParameterRelayCommand<HostDefinition> RemoveHostCommand { get; }

    /// <summary>
    /// 获取新增实例命令。
    /// </summary>
    public ICommand AddInstanceCommand { get; }

    /// <summary>
    /// 获取删除指定实例命令。
    /// </summary>
    public ParameterRelayCommand<InstanceEditorViewModel> RemoveInstanceCommand { get; }

    /// <summary>
    /// 获取重新检测 Unity 模块命令。
    /// </summary>
    public ICommand RefreshUnityModulesCommand { get; }

    /// <summary>
    /// 获取重新读取项目 Role Catalog 的命令。
    /// </summary>
    public ICommand RefreshRoleCatalogCommand { get; }

    /// <summary>
    /// 获取生产拓扑预设命令。
    /// </summary>
    public ICommand ProductionPresetCommand { get; }

    /// <summary>
    /// 获取单机一体化预设命令。
    /// </summary>
    public ICommand AllInOnePresetCommand { get; }

    /// <summary>
    /// 获取生成计划命令。
    /// </summary>
    public ICommand PreviewPlanCommand { get; }

    /// <summary>
    /// 获取一键构建并发布命令。
    /// </summary>
    public ICommand ExecuteCommand { get; }

    /// <summary>
    /// 获取取消当前执行命令。
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// 创建主窗口视图模型和全部基础设施服务。
    /// </summary>
    public MainWindowViewModel()
    {
        paths = new ApplicationPaths();
        profileStore = new ProfileStore(paths);
        planStore = new DeploymentPlanStore(paths);
        var runner = new ProcessRunner();
        var executor = new MiniCoreDeploymentStepExecutor(
            new UnityBatchBuildService(runner, paths),
            new DotNetComponentPublisher(runner, paths),
            new GitSourceInspector(runner),
            new ReleasePackager(),
            new SshRemoteClient(),
            paths,
            profileStore);
        orchestrator = new DeploymentOrchestrator(executor, new JsonExecutionJournal(paths));
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        AddHostCommand = new RelayCommand(AddHost);
        RemoveHostCommand = new ParameterRelayCommand<HostDefinition>(RemoveHost);
        AddInstanceCommand = new RelayCommand(AddInstance);
        RemoveInstanceCommand = new ParameterRelayCommand<InstanceEditorViewModel>(RemoveInstance);
        RefreshUnityModulesCommand = new RelayCommand(RefreshUnityModules);
        RefreshRoleCatalogCommand = new RelayCommand(RefreshRoleCatalog);
        ProductionPresetCommand = new RelayCommand(ApplyProductionPreset);
        AllInOnePresetCommand = new RelayCommand(ApplyAllInOnePreset);
        PreviewPlanCommand = new AsyncRelayCommand(PreviewPlanAsync);
        ExecuteCommand = new AsyncRelayCommand(ExecuteAsync);
        CancelCommand = new RelayCommand(CancelExecution);
        InitializeProfileCommands();
        InitializeBuildTargetCommands();
    }

    /// <summary>
    /// 从仓库外应用数据目录加载上次配置和历史列表。
    /// </summary>
    /// <returns>加载完成任务。</returns>
    public async Task InitializeAsync()
    {
        ProfileStoreSnapshot snapshot = await profileStore.LoadAllAsync(CancellationToken.None).ConfigureAwait(true);
        LoadProfileSnapshot(snapshot);
        selectedTargetInstanceId = profile.TargetInstanceId;
        RefreshModuleAvailability();
        LoadRoleCatalog();
        SynchronizeCollectionsFromProfile();
        RaiseAllConfigurationProperties();
        await RestoreMatchingPlanAsync().ConfigureAwait(true);
        RefreshHistory();
        if (previewedPlan == null)
        {
            StatusMessage = "配置已加载。请完成主机指纹和拓扑配置，然后先生成计划预览。";
        }

        SaveStateText = "已保存";
    }

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 保存当前可编辑配置。
    /// </summary>
    /// <returns>保存完成任务。</returns>
    private async Task SaveAsync()
    {
        CommitCollections();
        await profileStore.SaveAsync(profile, CancellationToken.None).ConfigureAwait(true);
        await profileStore.SetActiveAsync(profile.ProfileId, CancellationToken.None).ConfigureAwait(true);
        RefreshSelectedProfileItem();
        SaveStateText = "已保存";
        StatusMessage = $"“{profile.Name}”已独立保存到 {profileStore.GetProfilePath(profile.ProfileId)}。";
    }

    /// <summary>
    /// 新增一个待填写 SSH 信息的主机。
    /// </summary>
    private void AddHost()
    {
        var host = new HostDefinition
        {
            HostId = "host-" + (Hosts.Count + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
            OperatingSystem = HostOperatingSystem.Linux,
            DeploymentRoot = "/opt/minicore"
        };
        Hosts.Add(host);
        var editor = new HostEditorViewModel(host);
        AttachHostEditor(editor);
        HostEditors.Add(editor);
        RaiseEmptyStateProperties();
        InvalidatePreview();
    }

    /// <summary>
    /// 删除一台尚未被任何实例引用的主机。
    /// </summary>
    /// <param name="host">待删除主机。</param>
    private void RemoveHost(HostDefinition host)
    {
        for (int index = 0; index < Instances.Count; index++)
        {
            if (string.Equals(Instances[index].Model.HostId, host.HostId, StringComparison.Ordinal))
            {
                StatusMessage = $"主机 {host.HostId} 仍被实例 {Instances[index].Model.InstanceId} 使用，请先迁移或删除该实例。";
                return;
            }
        }

        Hosts.Remove(host);
        for (int index = HostEditors.Count - 1; index >= 0; index--)
        {
            if (ReferenceEquals(HostEditors[index].Model, host))
            {
                DetachHostEditor(HostEditors[index]);
                HostEditors.RemoveAt(index);
            }
        }

        RaiseEmptyStateProperties();
        StatusMessage = $"已从当前方案移除主机 {host.HostId}；尚未执行任何远程操作。";
        InvalidatePreview();
    }

    /// <summary>
    /// 新增一个可编辑 Dedicated Server 实例。
    /// </summary>
    private void AddInstance()
    {
        var instance = new InstanceDefinition
        {
            InstanceId = "ds-" + (Instances.Count + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
            HostId = Hosts.Count > 0 ? Hosts[0].HostId : string.Empty,
            Component = ComponentKind.DedicatedServer,
            InnerPort = 7100 + Instances.Count * 10,
            OuterPort = 7101 + Instances.Count * 10,
            ManagementPort = 7199 + Instances.Count * 10
        };
        for (int roleIndex = 0; roleIndex < roleCatalog.Count; roleIndex++)
        {
            if (!roleCatalog[roleIndex].FrameworkReserved)
            {
                instance.Roles.Add(roleCatalog[roleIndex].Key);
                break;
            }
        }
        var editor = new InstanceEditorViewModel(instance, Hosts);
        editor.SetRoleCatalog(roleCatalog);
        AttachInstanceEditor(editor);
        Instances.Add(editor);
        RaiseEmptyStateProperties();
        if (string.IsNullOrEmpty(SelectedTargetInstanceId))
        {
            SelectedTargetInstanceId = instance.InstanceId;
        }

        RefreshBuildTargetSelections();
        InvalidatePreview();
    }

    /// <summary>
    /// 从当前期望拓扑中删除指定实例编辑项。
    /// </summary>
    /// <param name="instance">待删除实例编辑项。</param>
    private void RemoveInstance(InstanceEditorViewModel instance)
    {
        string instanceId = instance.Model.InstanceId;
        DetachInstanceEditor(instance);
        Instances.Remove(instance);
        RefreshBuildTargetSelections();
        RaiseEmptyStateProperties();
        if (string.Equals(SelectedTargetInstanceId, instanceId, StringComparison.Ordinal))
        {
            SelectedTargetInstanceId = Instances.Count > 0 ? Instances[0].Model.InstanceId : string.Empty;
        }

        StatusMessage = $"已从当前方案移除实例 {instanceId}；如需安全下线线上实例，请使用“下线实例”发布方式。";
        InvalidatePreview();
    }

    /// <summary>
    /// 应用独立 Coordinator 与业务 DS 的生产默认拓扑。
    /// </summary>
    private void ApplyProductionPreset()
    {
        RefreshRoleCatalog();
        if (roleCatalog.Count == 0)
        {
            StatusMessage = "未读取到项目 Role Catalog，无法生成生产拓扑预设。";
            return;
        }

        if (Hosts.Count == 0)
        {
            AddHost();
        }

        ClearInstanceEditors();
        AddPresetInstance("coordinator-01", ComponentKind.Coordinator, 7000, 7001, 7099, "Coordinator");
        string firstBusinessInstanceId = string.Empty;
        int businessIndex = 0;
        for (int roleIndex = 0; roleIndex < roleCatalog.Count; roleIndex++)
        {
            RoleCatalogItem role = roleCatalog[roleIndex];
            if (role.FrameworkReserved)
            {
                continue;
            }

            string instanceId = BuildPresetInstanceId(role.Key, businessIndex);
            int innerPort = 7100 + businessIndex * 100;
            AddPresetInstance(instanceId, ComponentKind.DedicatedServer, innerPort, innerPort + 1, innerPort + 99, role.Key);
            if (firstBusinessInstanceId.Length == 0)
            {
                firstBusinessInstanceId = instanceId;
            }

            businessIndex++;
        }

        SelectedTargetInstanceId = firstBusinessInstanceId.Length > 0 ? firstBusinessInstanceId : "coordinator-01";
        RefreshBuildTargetSelections();
        StatusMessage = $"已按当前 Role Catalog 生成生产拓扑，共 {businessIndex} 个业务 Role；Auth 和 DB 仍保持可选。";
        InvalidatePreview();
    }

    /// <summary>
    /// 应用 Coordinator 与全部业务 Role 同进程的小项目预设。
    /// </summary>
    private void ApplyAllInOnePreset()
    {
        RefreshRoleCatalog();
        if (roleCatalog.Count == 0)
        {
            StatusMessage = "未读取到项目 Role Catalog，无法生成单机一体化预设。";
            return;
        }

        if (Hosts.Count == 0)
        {
            AddHost();
        }

        ClearInstanceEditors();
        var roles = new StringBuilder();
        for (int roleIndex = 0; roleIndex < roleCatalog.Count; roleIndex++)
        {
            if (roles.Length > 0)
            {
                roles.Append(',');
            }

            roles.Append(roleCatalog[roleIndex].Key);
        }

        AddPresetInstance("all-in-one-01", ComponentKind.Coordinator, 7000, 7001, 7099, roles.ToString());
        SelectedTargetInstanceId = "all-in-one-01";
        RefreshBuildTargetSelections();
        StatusMessage = $"已按当前 Role Catalog 生成单机一体化拓扑，共承载 {roleCatalog.Count} 个 Role。";
        InvalidatePreview();
    }

    /// <summary>
    /// 添加一个预设实例。
    /// </summary>
    /// <param name="instanceId">实例标识。</param>
    /// <param name="component">组件种类。</param>
    /// <param name="innerPort">内网端口。</param>
    /// <param name="outerPort">外网端口。</param>
    /// <param name="managementPort">管理端口。</param>
    /// <param name="roles">逗号分隔 Role。</param>
    private void AddPresetInstance(string instanceId, ComponentKind component, int innerPort, int outerPort, int managementPort, string roles)
    {
        var instance = new InstanceDefinition
        {
            InstanceId = instanceId,
            HostId = Hosts[0].HostId,
            Component = component,
            InnerPort = innerPort,
            OuterPort = outerPort,
            ManagementPort = managementPort
        };
        var editor = new InstanceEditorViewModel(instance, Hosts) { RoleText = roles };
        editor.CommitRoles();
        editor.SetRoleCatalog(roleCatalog);
        AttachInstanceEditor(editor);
        Instances.Add(editor);
    }

    /// <summary>
    /// 校验配置并展示执行前的完整步骤列表。
    /// </summary>
    /// <returns>预览完成任务。</returns>
    private async Task PreviewPlanAsync()
    {
        try
        {
            CommitCollections();
            LoadRoleCatalog();
            CommitCollections();
            ValidateSelectedRolesAgainstCatalog();
            previewedPlan = planBuilder.Build(profile);
            previewFingerprint = ComputeProfileFingerprint(profile);
            previewRoleCatalogFingerprint = roleCatalogFingerprint;
            PlanSteps.Clear();
            for (int index = 0; index < previewedPlan.Steps.Count; index++)
            {
                PlanSteps.Add(previewedPlan.Steps[index]);
            }

            RaiseEmptyStateProperties();
            RaisePreviewStateProperties();

            await profileStore.SaveAsync(profile, CancellationToken.None).ConfigureAwait(true);
            await profileStore.SetActiveAsync(profile.ProfileId, CancellationToken.None).ConfigureAwait(true);
            RefreshSelectedProfileItem();
            SaveStateText = "已保存";
            await planStore.SaveAsync(previewedPlan, previewFingerprint, CancellationToken.None).ConfigureAwait(true);
            StatusMessage = $"计划 {previewedPlan.PlanId} 已生成，共 {previewedPlan.Steps.Count} 步；请检查风险确认项。";
        }
        catch (PlanValidationException exception)
        {
            StatusMessage = "计划无法生成：" + exception.Message;
        }
        catch (Exception exception)
        {
            StatusMessage = "计划预览失败：" + exception.Message;
        }
    }

    /// <summary>
    /// 仅在当前配置与已展示计划一致时启动构建和发布。
    /// </summary>
    /// <returns>执行完成任务。</returns>
    private async Task ExecuteAsync()
    {
        CommitCollections();
        LoadRoleCatalog();
        CommitCollections();
        try
        {
            ValidateSelectedRolesAgainstCatalog();
        }
        catch (PlanValidationException exception)
        {
            StatusMessage = "执行已拦截：" + exception.Message;
            return;
        }

        if (previewedPlan == null
            || !string.Equals(previewFingerprint, ComputeProfileFingerprint(profile), StringComparison.Ordinal)
            || !string.Equals(previewRoleCatalogFingerprint, roleCatalogFingerprint, StringComparison.Ordinal))
        {
            StatusMessage = "配置或项目 Role Catalog 已变化。请先重新生成计划，确认后再执行。";
            return;
        }

        executionCancellation?.Dispose();
        executionCancellation = new CancellationTokenSource();
        ExecutionResults.Clear();
        isExecuting = true;
        RaiseEmptyStateProperties();
        RaisePropertyChanged(nameof(HasActiveExecution));
        StatusMessage = "正在执行计划；已成功步骤会写入历史并支持继续。";
        var progress = new Progress<StepResult>(UpdateExecutionResult);
        var context = new DeploymentExecutionContext(profile, previewedPlan);
        try
        {
            DeploymentExecutionResult result = await orchestrator.ExecuteAsync(
                context,
                RequestApprovalAsync,
                progress,
                executionCancellation.Token).ConfigureAwait(true);
            StatusMessage = result.Succeeded
                ? $"发布成功：环境已收敛到 {profile.Environment.ReleaseVersion}。"
                : "发布未完成；请查看执行中心的失败原因和恢复建议。";
            RefreshHistory();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "执行已取消；已完成的安全步骤仍保留在发布历史中。";
            RefreshHistory();
        }
        catch (Exception exception)
        {
            StatusMessage = "执行基础设施失败：" + SensitiveDataRedactor.Redact(exception.Message);
            RefreshHistory();
        }
        finally
        {
            isExecuting = false;
            RaisePropertyChanged(nameof(HasActiveExecution));
        }
    }

    /// <summary>
    /// 取消尚未完成的安全步骤。
    /// </summary>
    private void CancelExecution()
    {
        executionCancellation?.Cancel();
        StatusMessage = "正在取消，等待当前安全步骤完成；已经完成的步骤不会回退。";
    }

    /// <summary>
    /// 将风险确认请求交给主窗口模态对话框。
    /// </summary>
    /// <param name="step">风险步骤。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>操作人员是否批准。</returns>
    private async Task<bool> RequestApprovalAsync(DeploymentStep step, CancellationToken cancellationToken)
    {
        var request = new ApprovalRequestEventArgs(step);
        ApprovalRequested?.Invoke(this, request);
        using CancellationTokenRegistration registration = cancellationToken.Register(() => request.Completion.TrySetCanceled(cancellationToken));
        return await request.Completion.Task.ConfigureAwait(true);
    }

    /// <summary>
    /// 更新执行中心中同一步骤的最新状态。
    /// </summary>
    /// <param name="result">步骤状态。</param>
    private void UpdateExecutionResult(StepResult result)
    {
        for (int index = 0; index < ExecutionResults.Count; index++)
        {
            if (string.Equals(ExecutionResults[index].StepId, result.StepId, StringComparison.Ordinal))
            {
                ExecutionResults[index] = result;
                RaiseEmptyStateProperties();
                return;
            }
        }

        ExecutionResults.Add(result);
        RaiseEmptyStateProperties();
    }

    /// <summary>
    /// 把界面集合和 Role 编辑文本同步到持久化模型。
    /// </summary>
    private void CommitCollections()
    {
        profile.Environment.Hosts.Clear();
        for (int index = 0; index < Hosts.Count; index++)
        {
            profile.Environment.Hosts.Add(Hosts[index]);
        }

        profile.Environment.Instances.Clear();
        for (int index = 0; index < Instances.Count; index++)
        {
            Instances[index].CommitRoles();
            profile.Environment.Instances.Add(Instances[index].Model);
        }

        profile.TargetInstanceId = selectedTargetInstanceId;
    }

    /// <summary>
    /// 从持久化模型重建可绑定集合。
    /// </summary>
    private void SynchronizeCollectionsFromProfile()
    {
        Hosts.Clear();
        for (int editorIndex = 0; editorIndex < HostEditors.Count; editorIndex++)
        {
            DetachHostEditor(HostEditors[editorIndex]);
        }

        HostEditors.Clear();
        for (int index = 0; index < profile.Environment.Hosts.Count; index++)
        {
            HostDefinition host = profile.Environment.Hosts[index];
            Hosts.Add(host);
            var hostEditor = new HostEditorViewModel(host);
            AttachHostEditor(hostEditor);
            HostEditors.Add(hostEditor);
        }

        ClearInstanceEditors();
        for (int index = 0; index < profile.Environment.Instances.Count; index++)
        {
            var editor = new InstanceEditorViewModel(profile.Environment.Instances[index], Hosts);
            editor.SetRoleCatalog(roleCatalog);
            AttachInstanceEditor(editor);
            Instances.Add(editor);
        }

        RefreshBuildTargetSelections();
        RaiseEmptyStateProperties();
    }

    /// <summary>
    /// 订阅主机编辑器的 VPC 地址变化，使继承地址立即刷新。
    /// </summary>
    /// <param name="editor">待订阅主机编辑器。</param>
    private void AttachHostEditor(HostEditorViewModel editor)
    {
        editor.PropertyChanged += OnHostEditorPropertyChanged;
    }

    /// <summary>
    /// 解除主机编辑器属性变化订阅。
    /// </summary>
    /// <param name="editor">待解除订阅主机编辑器。</param>
    private void DetachHostEditor(HostEditorViewModel editor)
    {
        editor.PropertyChanged -= OnHostEditorPropertyChanged;
    }

    /// <summary>
    /// 在主机 VPC 地址变化时刷新全部实例的有效公布地址。
    /// </summary>
    /// <param name="sender">变化来源。</param>
    /// <param name="eventArgs">属性变化参数。</param>
    private void OnHostEditorPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (!string.Equals(eventArgs.PropertyName, nameof(HostEditorViewModel.PrivateAddress), StringComparison.Ordinal))
        {
            return;
        }

        for (int index = 0; index < Instances.Count; index++)
        {
            Instances[index].RefreshHostDerivedValues();
        }

        InvalidatePreview();
    }

    /// <summary>
    /// 订阅实例组件、启用状态和地址变化。
    /// </summary>
    /// <param name="editor">待订阅实例编辑器。</param>
    private void AttachInstanceEditor(InstanceEditorViewModel editor)
    {
        editor.PropertyChanged += OnInstanceEditorPropertyChanged;
    }

    /// <summary>
    /// 解除实例编辑器属性变化订阅。
    /// </summary>
    /// <param name="editor">待解除订阅实例编辑器。</param>
    private void DetachInstanceEditor(InstanceEditorViewModel editor)
    {
        editor.PropertyChanged -= OnInstanceEditorPropertyChanged;
    }

    /// <summary>
    /// 在拓扑可用性变化时立即刷新 Auth/DB 构建目标并清理失效选择。
    /// </summary>
    /// <param name="sender">变化来源。</param>
    /// <param name="eventArgs">属性变化参数。</param>
    private void OnInstanceEditorPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (string.Equals(eventArgs.PropertyName, nameof(InstanceEditorViewModel.Component), StringComparison.Ordinal)
            || string.Equals(eventArgs.PropertyName, nameof(InstanceEditorViewModel.Enabled), StringComparison.Ordinal))
        {
            RefreshBuildTargetSelections();
        }

        InvalidatePreview();
    }

    /// <summary>
    /// 解除现有实例订阅并清空实例编辑集合。
    /// </summary>
    private void ClearInstanceEditors()
    {
        for (int index = 0; index < Instances.Count; index++)
        {
            DetachInstanceEditor(Instances[index]);
        }

        Instances.Clear();
    }

    /// <summary>
    /// 刷新发布历史文件列表。
    /// </summary>
    private void RefreshHistory()
    {
        allHistoryEntries.Clear();
        string[] files = Directory.GetFiles(paths.HistoryPath, "*.jsonl", SearchOption.TopDirectoryOnly);
        Array.Sort(
            files,
            static (left, right) => File.GetLastWriteTimeUtc(right).CompareTo(File.GetLastWriteTimeUtc(left)));
        for (int index = 0; index < files.Length; index++)
        {
            DeploymentHistoryEntryViewModel? entry = TryLoadHistoryEntry(files[index]);
            if (entry != null)
            {
                allHistoryEntries.Add(entry);
            }
        }

        ApplyHistoryFilters();
    }

    /// <summary>
    /// 读取一份 JSONL 执行日志并汇总为用户可读的计划历史。
    /// </summary>
    /// <param name="path">计划执行日志路径。</param>
    /// <returns>有效历史摘要；文件没有有效步骤时返回 null。</returns>
    private DeploymentHistoryEntryViewModel? TryLoadHistoryEntry(string path)
    {
        var results = new List<StepResult>();
        try
        {
            string[] lines = File.ReadAllLines(path);
            for (int index = 0; index < lines.Length; index++)
            {
                if (string.IsNullOrWhiteSpace(lines[index]))
                {
                    continue;
                }

                StepResult? result = JsonSerializer.Deserialize<StepResult>(lines[index], HistoryJsonOptions);
                if (result != null)
                {
                    results.Add(result);
                }
            }
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }

        if (results.Count == 0)
        {
            return null;
        }

        StepResult first = results[0];
        StepResult last = results[^1];
        StepResult? rollback = results.LastOrDefault(static result => result.Action == DeploymentAction.AutomaticRollback);
        bool lockReleaseFailed = results.Any(static result => result.Action == DeploymentAction.ReleaseEnvironmentLock && result.Status == StepStatus.Failed);
        bool completed = !lockReleaseFailed
            && results.Any(static result => result.Action == DeploymentAction.PersistState && result.Status == StepStatus.Succeeded);
        StepResult? failed = completed
            ? null
            : results.LastOrDefault(static result => result.Status == StepStatus.Failed && result.Action != DeploymentAction.AutomaticRollback);
        string[] instances = results
            .Select(static result => result.InstanceId)
            .Where(static instanceId => !string.IsNullOrWhiteSpace(instanceId))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static instanceId => instanceId, StringComparer.Ordinal)
            .ToArray();
        int retryCount = results
            .Where(static result => result.Attempt > 1)
            .GroupBy(static result => result.StepId, StringComparer.Ordinal)
            .Sum(static group => group.Max(static result => result.Attempt) - 1);
        string previousVersion = results
            .Select(static result => result.PreviousReleaseVersion)
            .FirstOrDefault(static version => !string.IsNullOrWhiteSpace(version)) ?? string.Empty;
        string releaseVersion = first.ReleaseVersion;
        string resultText = completed
            ? "成功"
            : rollback?.RollbackSucceeded == true
                ? "失败，已自动回滚"
                : rollback != null
                    ? "失败，自动回滚失败"
                    : last.Status == StepStatus.Cancelled
                        ? "已取消"
                        : "失败";
        return new DeploymentHistoryEntryViewModel
        {
            PlanId = string.IsNullOrWhiteSpace(first.PlanId) ? Path.GetFileNameWithoutExtension(path) : first.PlanId,
            EnvironmentId = first.EnvironmentId,
            Operation = first.Operation.ToString(),
            Operator = first.Operator,
            StartedAt = results.Min(static result => result.StartedAtUtc).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
            CompletedAt = results.Max(static result => result.CompletedAtUtc ?? result.StartedAtUtc).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
            PreviousReleaseVersion = previousVersion,
            ReleaseVersion = releaseVersion,
            VersionTransition = (string.IsNullOrWhiteSpace(previousVersion) ? "首次/未知" : previousVersion) + " → " + releaseVersion,
            Instances = instances.Length == 0 ? "环境级" : string.Join("、", instances),
            Result = resultText,
            FailedStep = failed?.DisplayName ?? string.Empty,
            FailureReason = failed?.Message ?? string.Empty,
            RetryCount = retryCount,
            RollbackResult = rollback == null
                ? "未触发"
                : rollback.RollbackSucceeded == true ? "成功" : "失败：" + rollback.Message,
            LogDirectory = Path.Combine(paths.LogsPath, string.IsNullOrWhiteSpace(first.PlanId) ? Path.GetFileNameWithoutExtension(path) : first.PlanId)
        };
    }

    /// <summary>
    /// 使用环境、版本、实例、操作和结果条件刷新发布历史列表。
    /// </summary>
    private void ApplyHistoryFilters()
    {
        HistoryEntries.Clear();
        for (int index = 0; index < allHistoryEntries.Count; index++)
        {
            DeploymentHistoryEntryViewModel entry = allHistoryEntries[index];
            if (entry.Matches(
                historyEnvironmentFilter,
                historyVersionFilter,
                historyInstanceFilter,
                historyOperationFilter,
                historyResultFilter))
            {
                HistoryEntries.Add(entry);
            }
        }

        RaiseEmptyStateProperties();
    }

    /// <summary>
    /// 创建能够读取字符串枚举的历史反序列化设置。
    /// </summary>
    /// <returns>只用于本地历史的 JSON 设置。</returns>
    private static JsonSerializerOptions CreateHistoryJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    /// <summary>
    /// 在当前配置未变化时恢复最近一次已展示计划，支持应用重启后继续执行。
    /// </summary>
    /// <returns>恢复完成任务。</returns>
    private async Task RestoreMatchingPlanAsync()
    {
        previewFingerprint = ComputeProfileFingerprint(profile);
        previewedPlan = await planStore.LoadLatestMatchingAsync(previewFingerprint, CancellationToken.None).ConfigureAwait(true);
        if (previewedPlan == null)
        {
            previewFingerprint = string.Empty;
            RaisePreviewStateProperties();
            RaiseEmptyStateProperties();
            return;
        }

        PlanSteps.Clear();
        for (int index = 0; index < previewedPlan.Steps.Count; index++)
        {
            PlanSteps.Add(previewedPlan.Steps[index]);
        }

        RaisePreviewStateProperties();
        RaiseEmptyStateProperties();

        StatusMessage = $"已恢复计划 {previewedPlan.PlanId}；执行时会重新预检并重新校验目标主机上的制品。";
    }

    /// <summary>
    /// 判断当前构建目标是否已选择。
    /// </summary>
    /// <param name="target">目标。</param>
    /// <returns>已选择时返回 true。</returns>
    private bool HasTarget(BuildTargetKind target)
    {
        return profile.Project.BuildTargets.Contains(target);
    }

    /// <summary>
    /// 添加或移除一个构建目标，并使旧计划失效。
    /// </summary>
    /// <param name="target">目标。</param>
    /// <param name="enabled">是否选择。</param>
    private void SetTarget(BuildTargetKind target, bool enabled)
    {
        bool exists = HasTarget(target);
        if (enabled && !exists)
        {
            profile.Project.BuildTargets.Add(target);
        }
        else if (!enabled && exists)
        {
            profile.Project.BuildTargets.Remove(target);
        }

        InvalidatePreview();
    }

    /// <summary>
    /// 使已预览计划失效，防止执行未展示的变更。
    /// </summary>
    private void InvalidatePreview()
    {
        previewedPlan = null;
        previewFingerprint = string.Empty;
        previewRoleCatalogFingerprint = string.Empty;
        PlanSteps.Clear();
        RaisePreviewStateProperties();
        RaiseEmptyStateProperties();
        SaveStateText = "未保存";
        RaisePropertyChanged(nameof(PrimaryExecutionActionText));
        RaisePropertyChanged(nameof(BuildScopeSummary));
        RaisePropertyChanged(nameof(ProfileSummary));
    }

    /// <summary>
    /// 计算当前可持久化配置的 SHA-256，用于锁定计划预览。
    /// </summary>
    /// <param name="value">发布配置。</param>
    /// <returns>配置摘要。</returns>
    private static string ComputeProfileFingerprint(DeploymentProfile value)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(bytes);
        for (int index = 0; index < value.Environment.Instances.Count; index++)
        {
            string password = value.Environment.Instances[index].Database.Password;
            if (password.Length > 0)
            {
                hash.AppendData(Encoding.UTF8.GetBytes(password));
            }

            hash.AppendData(new byte[] { 0 });
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    /// <summary>
    /// 配置加载后通知所有包装属性刷新。
    /// </summary>
    private void RaiseAllConfigurationProperties()
    {
        RaisePropertyChanged(nameof(UnityExecutablePath));
        RaisePropertyChanged(nameof(ProjectDisplayName));
        RaisePropertyChanged(nameof(ProjectPath));
        RaisePropertyChanged(nameof(RoleCatalogStatus));
        RaisePropertyChanged(nameof(OutputPath));
        RaisePropertyChanged(nameof(ClientScenePath));
        RaisePropertyChanged(nameof(ServerScenePath));
        RaisePropertyChanged(nameof(EnvironmentId));
        RaisePropertyChanged(nameof(EnvironmentDisplayName));
        RaisePropertyChanged(nameof(RequireCleanGitWorkspace));
        RaisePropertyChanged(nameof(EnforcePublicEndpointSafety));
        RaisePropertyChanged(nameof(ReleaseVersion));
        RaisePropertyChanged(nameof(DatabaseMigrationReviewed));
        RaisePropertyChanged(nameof(Operation));
        RaisePropertyChanged(nameof(SelectedDeploymentOperationOption));
        RaisePropertyChanged(nameof(SelectedOperationDescription));
        RaisePropertyChanged(nameof(SelectedOperationImpact));
        RaisePropertyChanged(nameof(SelectedTargetInstanceId));
        RaisePropertyChanged(nameof(BuildServerLinux));
        RaisePropertyChanged(nameof(BuildServerWindows));
        RaisePropertyChanged(nameof(BuildClientWindows));
        RaisePropertyChanged(nameof(BuildClientMacOS));
        RaisePropertyChanged(nameof(BuildClientAndroid));
        RaisePropertyChanged(nameof(AndroidAppBundle));
        RaisePropertyChanged(nameof(BuildClientWebGL));
        RaisePropertyChanged(nameof(BuildAuthenticationServer));
        RaisePropertyChanged(nameof(BuildDatabaseServer));
        RaisePropertyChanged(nameof(ProfileName));
        RaisePropertyChanged(nameof(ProfilePurpose));
        RaisePropertyChanged(nameof(ProfileFilePath));
        RaisePropertyChanged(nameof(ProfileSummary));
        RaisePropertyChanged(nameof(ContentOnly));
        RaisePropertyChanged(nameof(PrimaryExecutionActionText));
        RaisePropertyChanged(nameof(BuildScopeSummary));
        RefreshBuildTargetSelections();
        RaisePreviewStateProperties();
        RaiseEmptyStateProperties();
    }

    /// <summary>
    /// 刷新 Unity 平台模块状态并通知构建目标控件。
    /// </summary>
    private void RefreshModuleAvailability()
    {
        moduleAvailability = UnityModuleDetector.Detect(profile.Project.UnityExecutablePath);
        RaisePropertyChanged(nameof(UnityModuleSummary));
        RaisePropertyChanged(nameof(CanBuildServerLinux));
        RaisePropertyChanged(nameof(CanBuildServerWindows));
        RaisePropertyChanged(nameof(CanBuildClientWindows));
        RaisePropertyChanged(nameof(CanBuildClientMacOS));
        RaisePropertyChanged(nameof(CanBuildClientAndroid));
        RaisePropertyChanged(nameof(CanBuildClientWebGL));
        RefreshBuildTargetSelections();
    }

    /// <summary>
    /// 重新扫描当前 Unity 安装的全部 PlaybackEngines 模块并更新界面。
    /// </summary>
    private void RefreshUnityModules()
    {
        RefreshModuleAvailability();
        StatusMessage = moduleAvailability.Summary;
    }

    /// <summary>
    /// 通知界面刷新计划预览与执行按钮的真实可用状态。
    /// </summary>
    private void RaisePreviewStateProperties()
    {
        RaisePropertyChanged(nameof(HasPreviewedPlan));
        RaisePropertyChanged(nameof(HasNoPreviewedPlan));
    }

    /// <summary>
    /// 通知界面刷新主机、实例、计划、执行和历史的空状态。
    /// </summary>
    private void RaiseEmptyStateProperties()
    {
        RaisePropertyChanged(nameof(HasNoHosts));
        RaisePropertyChanged(nameof(HasNoInstances));
        RaisePropertyChanged(nameof(HasNoPlanSteps));
        RaisePropertyChanged(nameof(HasNoExecutionResults));
        RaisePropertyChanged(nameof(HasNoHistory));
    }

    /// <summary>
    /// 从项目仓库读取 Role Catalog，并刷新全部实例的可见多选项。
    /// </summary>
    private void LoadRoleCatalog()
    {
        roleCatalog.Clear();
        roleCatalogFingerprint = string.Empty;
        try
        {
            string path = Path.Combine(profile.Project.ProjectPath, "Server", "DedicatedServer", "Config", "ServerRoleCatalog.json");
            if (!File.Exists(path))
            {
                RoleCatalogStatus = "未找到 ServerRoleCatalog.json；请先运行 MiniCore Deploy 代码生成。";
                RefreshInstanceRoleOptions();
                return;
            }

            byte[] catalogBytes = File.ReadAllBytes(path);
            roleCatalogFingerprint = Convert.ToHexStringLower(SHA256.HashData(catalogBytes));
            using JsonDocument document = JsonDocument.Parse(catalogBytes);
            if (!document.RootElement.TryGetProperty("roles", out JsonElement rolesElement)
                || rolesElement.ValueKind != JsonValueKind.Array)
            {
                RoleCatalogStatus = "ServerRoleCatalog.json 缺少 roles 数组。";
                RefreshInstanceRoleOptions();
                return;
            }

            foreach (JsonElement roleElement in rolesElement.EnumerateArray())
            {
                if (!roleElement.TryGetProperty("key", out JsonElement keyElement)
                    || string.IsNullOrWhiteSpace(keyElement.GetString()))
                {
                    continue;
                }

                string key = keyElement.GetString()!;
                string displayName = roleElement.TryGetProperty("displayName", out JsonElement displayElement)
                    ? displayElement.GetString() ?? key
                    : key;
                bool frameworkReserved = roleElement.TryGetProperty("frameworkReserved", out JsonElement reservedElement)
                    && reservedElement.ValueKind is JsonValueKind.True or JsonValueKind.False
                    && reservedElement.GetBoolean();
                roleCatalog.Add(new RoleCatalogItem
                {
                    Key = key,
                    DisplayName = displayName,
                    FrameworkReserved = frameworkReserved
                });
            }

            RoleCatalogStatus = $"已同步 {roleCatalog.Count} 个 Role；来源为 Unity 生成的 ServerRoleCatalog.json，构建前会再次核对摘要。";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            RoleCatalogStatus = "Role Catalog 读取失败：" + exception.Message;
        }

        RefreshInstanceRoleOptions();
    }

    /// <summary>
    /// 保存当前 Role 勾选并重新读取项目生成目录。
    /// </summary>
    private void RefreshRoleCatalog()
    {
        for (int index = 0; index < Instances.Count; index++)
        {
            Instances[index].CommitRoles();
        }

        LoadRoleCatalog();
        InvalidatePreview();
        StatusMessage = RoleCatalogStatus;
    }

    /// <summary>
    /// 确认所有已选 Role 都仍存在于项目生成目录中。
    /// </summary>
    private void ValidateSelectedRolesAgainstCatalog()
    {
        var knownRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int roleIndex = 0; roleIndex < roleCatalog.Count; roleIndex++)
        {
            knownRoles.Add(roleCatalog[roleIndex].Key);
        }

        for (int instanceIndex = 0; instanceIndex < Instances.Count; instanceIndex++)
        {
            InstanceDefinition instance = Instances[instanceIndex].Model;
            if (!instance.Enabled
                || instance.Component is not (ComponentKind.Coordinator or ComponentKind.DedicatedServer))
            {
                continue;
            }

            for (int roleIndex = 0; roleIndex < instance.Roles.Count; roleIndex++)
            {
                string role = instance.Roles[roleIndex];
                if (!knownRoles.Contains(role))
                {
                    throw new PlanValidationException($"实例 {instance.InstanceId} 仍选择了目录中不存在的 Role：{role}。请刷新 Role 后重新选择。");
                }
            }
        }
    }

    /// <summary>
    /// 从稳定 Role 键生成可读且可用于服务名的预设实例标识。
    /// </summary>
    /// <param name="roleKey">稳定 Role 键。</param>
    /// <param name="roleIndex">业务 Role 顺序。</param>
    /// <returns>实例标识。</returns>
    private static string BuildPresetInstanceId(string roleKey, int roleIndex)
    {
        int segmentStart = roleKey.LastIndexOf('.') + 1;
        var builder = new StringBuilder(roleKey.Length + 3);
        for (int index = segmentStart; index < roleKey.Length; index++)
        {
            char value = char.ToLowerInvariant(roleKey[index]);
            builder.Append(char.IsLetterOrDigit(value) ? value : '-');
        }

        string segment = builder.ToString().Trim('-');
        if (segment.Length == 0)
        {
            segment = "role-" + (roleIndex + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return segment + "-01";
    }

    /// <summary>
    /// 使用当前 Role Catalog 刷新所有实例编辑项。
    /// </summary>
    private void RefreshInstanceRoleOptions()
    {
        for (int index = 0; index < Instances.Count; index++)
        {
            Instances[index].SetRoleCatalog(roleCatalog);
        }
    }

    #endregion
}
