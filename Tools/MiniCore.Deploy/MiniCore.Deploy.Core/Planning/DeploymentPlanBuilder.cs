using MiniCore.Deploy.Core.Exceptions;
using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Core.Planning;

/// <summary>
/// 将用户期望拓扑转换为必须预览的确定性步骤序列。
/// </summary>
public sealed class DeploymentPlanBuilder
{
    #region Public 公共成员

    /// <summary>
    /// 校验配置并生成发布计划。
    /// </summary>
    /// <param name="profile">桌面应用当前配置。</param>
    /// <returns>可直接展示和执行的发布计划。</returns>
    public DeploymentPlan Build(DeploymentProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        Validate(profile);

        var plan = new DeploymentPlan
        {
            EnvironmentId = profile.Environment.EnvironmentId,
            ReleaseVersion = profile.Environment.ReleaseVersion,
            Operation = profile.Operation
        };

        AddStep(plan, DeploymentAction.Preflight, "本地与目标主机预检");
        if (profile.Project.BuildTargets.Count > 0)
        {
            AddStep(plan, DeploymentAction.Build, profile.Project.ContentOnly ? "构建热更新程序集与 YooAsset 内容" : "构建所选发布制品");
        }

        if (profile.Project.PublishTargets.Count > 0)
        {
            switch (profile.Operation)
            {
                case DeploymentOperation.FirstInstall:
                    AddFirstInstallSteps(profile, plan);
                    break;
                case DeploymentOperation.FullRelease:
                case DeploymentOperation.BusinessRelease:
                case DeploymentOperation.Rollback:
                    AddRollingSteps(profile, plan);
                    break;
                case DeploymentOperation.MaintenanceRelease:
                    AddMaintenanceSteps(profile, plan);
                    break;
                case DeploymentOperation.ScaleOut:
                case DeploymentOperation.Repair:
                    AddTargetInstallSteps(profile, plan);
                    break;
                case DeploymentOperation.ConfigurationUpdate:
                    AddConfigurationSteps(profile, plan);
                    break;
                case DeploymentOperation.RemoveInstance:
                    AddRemoveSteps(profile, plan);
                    break;
                default:
                    throw new PlanValidationException($"不支持的发布操作：{profile.Operation}。");
            }

            AddClientArtifactSteps(profile, plan);
        }

        AddStep(plan, DeploymentAction.PersistState, "保存发布状态与历史");
        return plan;
    }

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 校验路径、版本、主机、实例、Role 和端口唯一性。
    /// </summary>
    /// <param name="profile">待校验配置。</param>
    private static void Validate(DeploymentProfile profile)
    {
        if (profile.Project.BuildTargets.Count == 0 && profile.Project.PublishTargets.Count == 0)
        {
            throw new PlanValidationException("至少需要选择一个构建目标或发布目标。");
        }

        if (string.IsNullOrWhiteSpace(profile.Project.OutputPath))
        {
            throw new PlanValidationException("必须配置发布制品输出路径。");
        }

        if (profile.Project.BuildTargets.Count > 0
            && (string.IsNullOrWhiteSpace(profile.Project.ProjectPath)
                || string.IsNullOrWhiteSpace(profile.Project.UnityExecutablePath)
                || !Directory.Exists(profile.Project.ProjectPath)
                || !File.Exists(profile.Project.UnityExecutablePath)))
        {
            throw new PlanValidationException("Unity 可执行程序或项目目录不存在。");
        }

        if (profile.Project.BuildTargets.Count > 0)
        {
            for (int publishIndex = 0; publishIndex < profile.Project.PublishTargets.Count; publishIndex++)
            {
                if (!profile.Project.BuildTargets.Contains(profile.Project.PublishTargets[publishIndex]))
                {
                    throw new PlanValidationException("同一次构建并发布不能混用新旧制品；发布目标必须包含在构建目标中。若要复用已有制品，请选择“发布已有制品”。");
                }
            }

            if (profile.Project.ContentOnly
                && (profile.Project.BuildTargets.Contains(BuildTargetKind.AuthenticationServer)
                    || profile.Project.BuildTargets.Contains(BuildTargetKind.DatabaseServer)))
            {
                throw new PlanValidationException("仅资源更新不能同时构建 AuthenticationServer 或 DatabaseServer；请取消可选 .NET 服务目标。");
            }
        }
        else
        {
            string manifestPath = Path.Combine(profile.Project.OutputPath, profile.Environment.ReleaseVersion, "ReleaseManifest.json");
            if (!File.Exists(manifestPath))
            {
                throw new PlanValidationException($"发布已有制品需要本地存在 ReleaseManifest：{manifestPath}。");
            }
        }

        EnvironmentDefinition environment = profile.Environment;
        if (string.IsNullOrWhiteSpace(environment.EnvironmentId)
            || string.IsNullOrWhiteSpace(environment.ReleaseVersion)
            || !IsStableIdentifier(environment.EnvironmentId)
            || !IsStableIdentifier(environment.ReleaseVersion))
        {
            throw new PlanValidationException("环境标识和统一发布版本只能包含字母、数字、点、短横线或下划线。");
        }

        if (profile.Project.BuildTargets.Count > 0)
        {
            ValidateBuildTargets(profile);
        }

        if (!HasRemotePublishTarget(profile))
        {
            if (RequiresTarget(profile.Operation) && profile.Project.PublishTargets.Count > 0)
            {
                throw new PlanValidationException($"操作 {profile.Operation} 必须选择一个可远程部署的服务端目标。");
            }

            return;
        }

        var hostIds = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < environment.Hosts.Count; index++)
        {
            HostDefinition host = environment.Hosts[index];
            if (string.IsNullOrWhiteSpace(host.HostId) || !IsStableIdentifier(host.HostId) || !hostIds.Add(host.HostId))
            {
                throw new PlanValidationException($"主机标识为空或重复：{host.HostId}。");
            }

            if (string.IsNullOrWhiteSpace(host.Address)
                || string.IsNullOrWhiteSpace(host.UserName)
                || string.IsNullOrWhiteSpace(host.HostKeyFingerprint)
                || string.IsNullOrWhiteSpace(host.DeploymentRoot)
                || host.SshPort is <= 0 or > 65535)
            {
                throw new PlanValidationException($"主机 {host.HostId} 的 SSH、指纹或部署目录配置不完整。");
            }

            if (host.OperatingSystem == HostOperatingSystem.Linux && !IsStableIdentifier(host.UserName))
            {
                throw new PlanValidationException($"Linux 主机 {host.HostId} 的 SSH 登录用户包含不允许写入 systemd User 指令的字符。");
            }

            ValidateHostAuthentication(host);

            if (!IsValidDeploymentRoot(host))
            {
                throw new PlanValidationException($"主机 {host.HostId} 的部署根目录必须是无空白和换行的绝对路径。");
            }
        }

        var instanceIds = new HashSet<string>(StringComparer.Ordinal);
        var hostPorts = new HashSet<string>(StringComparer.Ordinal);
        int coordinatorCount = 0;
        bool hasDedicatedServer = false;
        bool hasAuthenticationServer = false;
        bool hasDatabaseServer = false;
        bool requiresDatabaseServer = false;
        InstanceDefinition? coordinatorInstance = null;
        for (int index = 0; index < environment.Instances.Count; index++)
        {
            InstanceDefinition instance = environment.Instances[index];
            if (!instance.Enabled || !ShouldPublishInstance(profile, instance))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(instance.InstanceId) || !IsStableIdentifier(instance.InstanceId) || !instanceIds.Add(instance.InstanceId))
            {
                throw new PlanValidationException($"实例标识为空或重复：{instance.InstanceId}。");
            }

            if (!hostIds.Contains(instance.HostId))
            {
                throw new PlanValidationException($"实例 {instance.InstanceId} 引用了不存在的主机 {instance.HostId}。");
            }

            if (instance.Component == ComponentKind.StaticContent)
            {
                HostDefinition staticHost = FindHost(profile, instance.HostId);
                if (!IsValidRemoteAbsolutePath(staticHost, instance.StaticContentPublishPath))
                {
                    throw new PlanValidationException($"静态内容 {instance.InstanceId} 必须配置目标服务器上的发布路径绝对地址。");
                }

                if (!string.IsNullOrWhiteSpace(instance.OuterAdvertisedUrl)
                    && !IsValidAbsoluteUrl(instance.OuterAdvertisedUrl, "http", "https"))
                {
                    throw new PlanValidationException($"静态内容 {instance.InstanceId} 的外部访问地址必须是 HTTP/HTTPS 绝对地址。");
                }

                continue;
            }

            ValidatePort(instance.InstanceId, instance.HostId, instance.InnerPort, "Inner", hostPorts);

            if (instance.Component == ComponentKind.Coordinator)
            {
                ValidateDedicatedServerNetwork(instance, hostPorts);
                coordinatorCount++;
                coordinatorInstance = instance;
                requiresDatabaseServer |= instance.RequiresDatabase;
                if (!ContainsRole(instance, "Coordinator"))
                {
                    throw new PlanValidationException($"Coordinator 实例 {instance.InstanceId} 必须选择框架保留 Role：Coordinator。");
                }
            }
            else if (instance.Component == ComponentKind.DedicatedServer)
            {
                ValidateDedicatedServerNetwork(instance, hostPorts);
                hasDedicatedServer = true;
                requiresDatabaseServer |= instance.RequiresDatabase;
                if (ContainsRole(instance, "Coordinator"))
                {
                    throw new PlanValidationException($"普通 DS {instance.InstanceId} 不能选择 Coordinator；一体化进程应使用 Coordinator 组件。");
                }
            }
            else if (instance.Component == ComponentKind.AuthenticationServer)
            {
                hasDedicatedServer = true;
                hasAuthenticationServer = true;
                if (string.IsNullOrWhiteSpace(instance.InnerListenHost)
                    || string.IsNullOrWhiteSpace(instance.InnerAdvertisedHost)
                    || !IsValidAbsoluteUrl(instance.OuterAdvertisedUrl, "http", "https"))
                {
                    throw new PlanValidationException($"AuthenticationServer {instance.InstanceId} 必须配置 HTTP 监听、内网公布地址和客户端 HTTP/HTTPS 绝对地址。");
                }

                ValidateDatabaseConnection(instance, "账号库");
            }
            else if (instance.Component == ComponentKind.DatabaseServer)
            {
                hasDedicatedServer = true;
                hasDatabaseServer = true;
                if (string.IsNullOrWhiteSpace(instance.InnerListenHost)
                    || string.IsNullOrWhiteSpace(instance.InnerAdvertisedHost)
                    || instance.MaximumConcurrency <= 0)
                {
                    throw new PlanValidationException($"DatabaseServer {instance.InstanceId} 必须配置内网监听、公布地址和正整数并发上限。");
                }

                ValidateDatabaseConnection(instance, "游戏库");
            }
        }

        if (coordinatorCount > 1)
        {
            throw new PlanValidationException("首版环境只允许一个启用的 Coordinator 实例。");
        }

        if (hasDedicatedServer && coordinatorCount != 1)
        {
            throw new PlanValidationException("启用普通 Dedicated Server、AuthenticationServer 或 DatabaseServer 时必须配置且只能配置一个 Coordinator。");
        }

        if (hasAuthenticationServer
            && (coordinatorInstance == null
                || !IsValidAbsoluteUrl(coordinatorInstance.OuterAdvertisedUrl, "ws", "wss")))
        {
            throw new PlanValidationException("AuthenticationServer 需要 Coordinator 配置客户端可访问的 ws/wss 外网地址。");
        }

        if (requiresDatabaseServer && !hasDatabaseServer)
        {
            throw new PlanValidationException("已有 Dedicated Server 勾选依赖 DatabaseServer，但当前发布拓扑未启用 DatabaseServer。");
        }

        if (RequiresTarget(profile.Operation)
            && (string.IsNullOrWhiteSpace(profile.TargetInstanceId) || !instanceIds.Contains(profile.TargetInstanceId)))
        {
            throw new PlanValidationException($"操作 {profile.Operation} 必须选择一个已启用的目标实例。");
        }

        if (profile.Operation == DeploymentOperation.RemoveInstance
            && FindTargetInstance(profile).Component == ComponentKind.StaticContent)
        {
            throw new PlanValidationException("StaticContent 使用版本指针发布，不支持按服务实例下线。");
        }
    }

    /// <summary>
    /// 校验主机所选 SSH 认证方式需要的会话凭证。
    /// </summary>
    /// <param name="host">目标主机。</param>
    private static void ValidateHostAuthentication(HostDefinition host)
    {
        if (host.AuthenticationType == SshAuthenticationType.PrivateKey)
        {
            if (string.IsNullOrWhiteSpace(host.PrivateKeyPath) || !File.Exists(host.PrivateKeyPath))
            {
                throw new PlanValidationException($"主机 {host.HostId} 选择了 SSH 私钥认证，但本机私钥文件不存在。");
            }

            return;
        }

        if (string.IsNullOrEmpty(host.Password))
        {
            throw new PlanValidationException($"主机 {host.HostId} 选择了密码认证，请输入本次应用会话使用的 SSH 密码；密码不会保存。");
        }
    }

    /// <summary>
    /// 校验 Auth 账号库或 DB 游戏库连接参数以及当前会话密码。
    /// </summary>
    /// <param name="instance">使用数据库的服务实例。</param>
    /// <param name="displayName">用户可见数据库用途。</param>
    private static void ValidateDatabaseConnection(InstanceDefinition instance, string displayName)
    {
        DatabaseConnectionDefinition database = instance.Database;
        if (string.IsNullOrWhiteSpace(database.Host)
            || database.Port is <= 0 or > 65535
            || string.IsNullOrWhiteSpace(database.DatabaseName)
            || string.IsNullOrWhiteSpace(database.UserName)
            || string.IsNullOrWhiteSpace(database.SslMode))
        {
            throw new PlanValidationException($"实例 {instance.InstanceId} 的{displayName}必须配置地址、1-65535 端口、数据库名称、账号和 SSL 模式。");
        }

        if (string.IsNullOrEmpty(database.Password))
        {
            throw new PlanValidationException($"实例 {instance.InstanceId} 尚未输入{displayName}密码；密码只保留当前应用会话，不会写入配置方案或日志。");
        }
    }

    /// <summary>
    /// 校验 Coordinator 或 Dedicated Server 的完整监听与公布配置。
    /// </summary>
    /// <param name="instance">目标实例。</param>
    /// <param name="hostPorts">当前主机已经占用的端口。</param>
    private static void ValidateDedicatedServerNetwork(InstanceDefinition instance, HashSet<string> hostPorts)
    {
        if (instance.Roles.Count == 0)
        {
            throw new PlanValidationException($"实例 {instance.InstanceId} 必须至少配置一个 Role。");
        }

        if (string.IsNullOrWhiteSpace(instance.InnerListenHost)
            || string.IsNullOrWhiteSpace(instance.InnerAdvertisedHost)
            || string.IsNullOrWhiteSpace(instance.OuterListenHost)
            || string.IsNullOrWhiteSpace(instance.OuterPath)
            || instance.OuterPath[0] != '/')
        {
            throw new PlanValidationException($"实例 {instance.InstanceId} 必须配置内外网监听地址、内网公布地址和以 / 开头的 WebSocket 路径。");
        }

        ValidatePort(instance.InstanceId, instance.HostId, instance.OuterPort, "Outer", hostPorts);
        ValidatePort(instance.InstanceId, instance.HostId, instance.ManagementPort, "Management", hostPorts);
        if (!string.IsNullOrWhiteSpace(instance.OuterAdvertisedUrl)
            && !IsValidAbsoluteUrl(instance.OuterAdvertisedUrl, "ws", "wss"))
        {
            throw new PlanValidationException($"实例 {instance.InstanceId} 的外网公布地址必须是 ws/wss 绝对地址，或为空表示不向客户端公布。");
        }
    }

    /// <summary>
    /// 判断文本是否为指定协议之一的绝对 URL。
    /// </summary>
    /// <param name="value">待检查地址。</param>
    /// <param name="schemes">允许的协议名称。</param>
    /// <returns>地址有效且协议匹配时返回 true。</returns>
    private static bool IsValidAbsoluteUrl(string value, params string[] schemes)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        for (int index = 0; index < schemes.Length; index++)
        {
            if (string.Equals(uri.Scheme, schemes[index], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断标识是否可安全用于本地文件名、远程目录和服务名。
    /// </summary>
    /// <param name="value">待校验文本。</param>
    /// <returns>只包含稳定安全字符时返回 true。</returns>
    private static bool IsStableIdentifier(string value)
    {
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (!char.IsLetterOrDigit(character) && character is not ('.' or '-' or '_'))
            {
                return false;
            }
        }

        return value.Length > 0 && !value.Contains("..", StringComparison.Ordinal);
    }

    /// <summary>
    /// 校验同一主机上的监听端口不重复。
    /// </summary>
    /// <param name="instanceId">实例标识。</param>
    /// <param name="hostId">主机标识。</param>
    /// <param name="port">端口。</param>
    /// <param name="portKind">端口用途。</param>
    /// <param name="hostPorts">已占用端口集合。</param>
    private static void ValidatePort(string instanceId, string hostId, int port, string portKind, HashSet<string> hostPorts)
    {
        if (port is <= 0 or > 65535)
        {
            throw new PlanValidationException($"实例 {instanceId} 的 {portKind} 端口无效：{port}。");
        }

        string key = hostId + ":" + port.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!hostPorts.Add(key))
        {
            throw new PlanValidationException($"主机 {hostId} 上的端口 {port} 被多个实例占用。");
        }
    }

    /// <summary>
    /// 判断当前操作是否必须选择单个实例。
    /// </summary>
    /// <param name="operation">发布操作。</param>
    /// <returns>必须选择实例时返回 true。</returns>
    private static bool RequiresTarget(DeploymentOperation operation)
    {
        return operation is DeploymentOperation.ScaleOut
            or DeploymentOperation.Repair
            or DeploymentOperation.RemoveInstance;
    }

    /// <summary>
    /// 为首次安装按 Coordinator、DB、Auth、业务实例顺序添加步骤。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="plan">目标计划。</param>
    private static void AddFirstInstallSteps(DeploymentProfile profile, DeploymentPlan plan)
    {
        AddHostStagingSteps(profile, plan);
        AddInstancesByKind(profile, plan, ComponentKind.Coordinator, false, false);
        AddInstancesByKind(profile, plan, ComponentKind.DatabaseServer, false, false);
        AddInstancesByKind(profile, plan, ComponentKind.AuthenticationServer, false, false);
        AddInstancesByKind(profile, plan, ComponentKind.DedicatedServer, false, false);
        AddStaticSteps(profile, plan);
    }

    /// <summary>
    /// 为滚动更新添加制品暂存和逐实例切换步骤，Coordinator 固定最后处理。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="plan">目标计划。</param>
    private static void AddRollingSteps(DeploymentProfile profile, DeploymentPlan plan)
    {
        AddHostStagingSteps(profile, plan);
        AddInstancesByKind(profile, plan, ComponentKind.DatabaseServer, true, true);
        AddInstancesByKind(profile, plan, ComponentKind.AuthenticationServer, true, false);
        AddInstancesByKind(profile, plan, ComponentKind.DedicatedServer, true, false);
        AddInstancesByKind(profile, plan, ComponentKind.Coordinator, true, true);
        AddStaticSteps(profile, plan);
    }

    /// <summary>
    /// 为不兼容控制协议或无冗余环境生成全停、统一切换和按依赖启动的维护窗口计划。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="plan">目标计划。</param>
    private static void AddMaintenanceSteps(DeploymentProfile profile, DeploymentPlan plan)
    {
        AddHostStagingSteps(profile, plan);
        AddMaintenanceStopSteps(profile, plan, ComponentKind.DedicatedServer);
        AddMaintenanceStopSteps(profile, plan, ComponentKind.AuthenticationServer);
        AddMaintenanceStopSteps(profile, plan, ComponentKind.DatabaseServer);
        AddMaintenanceStopSteps(profile, plan, ComponentKind.Coordinator);
        AddInstancesByKind(profile, plan, ComponentKind.Coordinator, false, true);
        AddInstancesByKind(profile, plan, ComponentKind.DatabaseServer, false, true);
        AddInstancesByKind(profile, plan, ComponentKind.AuthenticationServer, false, false);
        AddInstancesByKind(profile, plan, ComponentKind.DedicatedServer, false, true);
        AddStaticSteps(profile, plan);
    }

    /// <summary>
    /// 在维护窗口中安全停止一种组件的全部实例。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="plan">目标计划。</param>
    /// <param name="kind">组件种类。</param>
    private static void AddMaintenanceStopSteps(DeploymentProfile profile, DeploymentPlan plan, ComponentKind kind)
    {
        for (int index = 0; index < profile.Environment.Instances.Count; index++)
        {
            InstanceDefinition instance = profile.Environment.Instances[index];
            if (!instance.Enabled || instance.Component != kind || !ShouldPublishInstance(profile, instance))
            {
                continue;
            }

            if (instance.Component is ComponentKind.Coordinator or ComponentKind.DedicatedServer)
            {
                AddStep(plan, DeploymentAction.BeginDrain, $"维护窗口摘除 {instance.InstanceId} 流量", instance.HostId, instance.InstanceId, true);
                AddStep(plan, DeploymentAction.WaitForDrain, $"维护窗口等待 {instance.InstanceId} 排空", instance.HostId, instance.InstanceId, true);
            }

            AddStep(plan, DeploymentAction.StopService, $"维护窗口停止 {instance.InstanceId}", instance.HostId, instance.InstanceId, true);
        }
    }

    /// <summary>
    /// 为扩容或修复单个实例添加安装链路。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="plan">目标计划。</param>
    private static void AddTargetInstallSteps(DeploymentProfile profile, DeploymentPlan plan)
    {
        InstanceDefinition instance = FindTargetInstance(profile);
        AddStep(plan, DeploymentAction.StageArtifact, $"暂存 {instance.HostId} 的目标版本", instance.HostId);
        AddInstallSteps(plan, instance, false, instance.Component == ComponentKind.Coordinator);
    }

    /// <summary>
    /// 为配置更新添加受影响实例的安全重启步骤。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="plan">目标计划。</param>
    private static void AddConfigurationSteps(DeploymentProfile profile, DeploymentPlan plan)
    {
        for (int index = 0; index < profile.Environment.Instances.Count; index++)
        {
            InstanceDefinition instance = profile.Environment.Instances[index];
            if (!instance.Enabled || !ShouldPublishInstance(profile, instance))
            {
                continue;
            }

            AddStep(plan, DeploymentAction.WriteConfiguration, $"写入 {instance.InstanceId} 配置", instance.HostId, instance.InstanceId);
            AddRestartSteps(plan, instance, instance.Component == ComponentKind.Coordinator);
        }
    }

    /// <summary>
    /// 为安全下线添加摘流量、停止和卸载服务步骤。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="plan">目标计划。</param>
    private static void AddRemoveSteps(DeploymentProfile profile, DeploymentPlan plan)
    {
        InstanceDefinition instance = FindTargetInstance(profile);
        AddStep(plan, DeploymentAction.BeginDrain, $"摘除 {instance.InstanceId} 流量", instance.HostId, instance.InstanceId, true);
        AddStep(plan, DeploymentAction.WaitForDrain, $"等待 {instance.InstanceId} 排空", instance.HostId, instance.InstanceId, true);
        AddStep(plan, DeploymentAction.StopService, $"停止 {instance.InstanceId}", instance.HostId, instance.InstanceId, true);
        AddStep(plan, DeploymentAction.UninstallService, $"注销 {instance.InstanceId} 服务", instance.HostId, instance.InstanceId, true);
    }

    /// <summary>
    /// 为所有被实例引用的主机添加一次制品暂存步骤。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="plan">目标计划。</param>
    private static void AddHostStagingSteps(DeploymentProfile profile, DeploymentPlan plan)
    {
        var stagedHosts = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < profile.Environment.Instances.Count; index++)
        {
            InstanceDefinition instance = profile.Environment.Instances[index];
            if (instance.Enabled && ShouldPublishInstance(profile, instance) && stagedHosts.Add(instance.HostId))
            {
                AddStep(plan, DeploymentAction.StageArtifact, $"暂存 {instance.HostId} 的目标版本", instance.HostId, maxAttempts: 3);
            }
        }
    }

    /// <summary>
    /// 按组件种类为所有启用实例添加安装或滚动更新步骤。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="plan">目标计划。</param>
    /// <param name="kind">组件种类。</param>
    /// <param name="restart">是否执行滚动切换。</param>
    /// <param name="forceApproval">是否强制人工确认。</param>
    private static void AddInstancesByKind(
        DeploymentProfile profile,
        DeploymentPlan plan,
        ComponentKind kind,
        bool restart,
        bool forceApproval)
    {
        for (int index = 0; index < profile.Environment.Instances.Count; index++)
        {
            InstanceDefinition instance = profile.Environment.Instances[index];
            if (!instance.Enabled || instance.Component != kind || !ShouldPublishInstance(profile, instance))
            {
                continue;
            }

            bool requiresApproval = forceApproval
                || (restart && kind == ComponentKind.DedicatedServer && IsLastInstanceForAnyRole(profile, instance));
            AddInstallSteps(plan, instance, restart, requiresApproval);
        }
    }

    /// <summary>
    /// 判断实例承载的任一业务 Role 是否没有其他启用实例提供。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="target">目标实例。</param>
    /// <returns>更新会触及某 Role 最后一个实例时返回 true。</returns>
    private static bool IsLastInstanceForAnyRole(DeploymentProfile profile, InstanceDefinition target)
    {
        for (int roleIndex = 0; roleIndex < target.Roles.Count; roleIndex++)
        {
            string role = target.Roles[roleIndex];
            bool hasOther = false;
            for (int instanceIndex = 0; instanceIndex < profile.Environment.Instances.Count; instanceIndex++)
            {
                InstanceDefinition candidate = profile.Environment.Instances[instanceIndex];
                if (!candidate.Enabled
                    || ReferenceEquals(candidate, target)
                    || candidate.Component is not (ComponentKind.Coordinator or ComponentKind.DedicatedServer)
                    || !ContainsRole(candidate, role))
                {
                    continue;
                }

                hasOther = true;
                break;
            }

            if (!hasOther)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 校验新版本包含当前拓扑每种启用组件所需的制品目标。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    private static void ValidateBuildTargets(DeploymentProfile profile)
    {
        if (profile.Project.BuildTargets.Count == 0)
        {
            throw new PlanValidationException("构建型发布至少需要选择一个构建目标。");
        }

        if (profile.Project.BuildTargets.Contains(BuildTargetKind.DatabaseServer)
            && !string.Equals(
                profile.Environment.DatabaseMigrationReviewedReleaseVersion,
                profile.Environment.ReleaseVersion,
                StringComparison.Ordinal))
        {
            throw new PlanValidationException("DatabaseServer 参与本次构建，但当前 ReleaseVersion 尚未完成数据库迁移评审。工具不会自动执行 Migration，请确认外部迁移方案后勾选评审项。");
        }

        bool hasServerTarget = profile.Project.BuildTargets.Contains(BuildTargetKind.ServerLinuxX64)
            || profile.Project.BuildTargets.Contains(BuildTargetKind.ServerWindowsX64);
        bool hasClientTarget = profile.Project.BuildTargets.Contains(BuildTargetKind.ClientWindowsX64)
            || profile.Project.BuildTargets.Contains(BuildTargetKind.ClientMacOS)
            || profile.Project.BuildTargets.Contains(BuildTargetKind.ClientAndroid)
            || profile.Project.BuildTargets.Contains(BuildTargetKind.ClientWebGL);
        if (!profile.Project.ContentOnly
            && hasServerTarget
            && !File.Exists(ResolveProjectPath(profile.Project.ProjectPath, profile.Project.ServerScenePath)))
        {
            throw new PlanValidationException($"Dedicated Server 启动场景不存在：{profile.Project.ServerScenePath}。");
        }

        if (!profile.Project.ContentOnly
            && hasClientTarget
            && !File.Exists(ResolveProjectPath(profile.Project.ProjectPath, profile.Project.ClientScenePath)))
        {
            throw new PlanValidationException($"客户端启动场景不存在：{profile.Project.ClientScenePath}。");
        }

        if (!HasRemotePublishTarget(profile))
        {
            return;
        }

        for (int index = 0; index < profile.Environment.Instances.Count; index++)
        {
            InstanceDefinition instance = profile.Environment.Instances[index];
            if (!instance.Enabled)
            {
                continue;
            }

            HostDefinition host = FindHost(profile, instance.HostId);
            BuildTargetKind? requiredTarget = instance.Component switch
            {
                ComponentKind.Coordinator or ComponentKind.DedicatedServer => host.OperatingSystem == HostOperatingSystem.Linux
                    ? BuildTargetKind.ServerLinuxX64
                    : BuildTargetKind.ServerWindowsX64,
                ComponentKind.AuthenticationServer => BuildTargetKind.AuthenticationServer,
                ComponentKind.DatabaseServer => BuildTargetKind.DatabaseServer,
                ComponentKind.StaticContent => BuildTargetKind.ClientWebGL,
                _ => null
            };
            if (requiredTarget.HasValue
                && ShouldPublishInstance(profile, instance)
                && profile.Project.BuildTargets.Count > 0
                && !profile.Project.BuildTargets.Contains(requiredTarget.Value))
            {
                throw new PlanValidationException($"实例 {instance.InstanceId} 缺少构建目标 {requiredTarget.Value}。");
            }
        }
    }

    /// <summary>
    /// 将 Unity 项目相对路径转换为本地绝对路径。
    /// </summary>
    /// <param name="projectPath">Unity 项目根目录。</param>
    /// <param name="path">绝对或项目相对路径。</param>
    /// <returns>规范化绝对路径。</returns>
    private static string ResolveProjectPath(string projectPath, string path)
    {
        return Path.IsPathRooted(path) ? Path.GetFullPath(path) : Path.GetFullPath(Path.Combine(projectPath, path));
    }

    /// <summary>
    /// 判断部署根目录能否安全用于 systemd、PowerShell 和版本目录拼接。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <returns>路径为对应系统的无空白绝对路径时返回 true。</returns>
    private static bool IsValidDeploymentRoot(HostDefinition host)
    {
        string root = host.DeploymentRoot;
        for (int index = 0; index < root.Length; index++)
        {
            if (char.IsWhiteSpace(root[index]))
            {
                return false;
            }
        }

        return host.OperatingSystem == HostOperatingSystem.Linux
            ? root.Length > 0 && root[0] == '/'
            : root.Length >= 3 && char.IsLetter(root[0]) && root[1] == ':' && (root[2] == '\\' || root[2] == '/');
    }

    /// <summary>
    /// 判断目标服务器路径是否为可安全写入服务定义的无空白绝对路径。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="path">目标服务器路径。</param>
    /// <returns>路径满足目标系统绝对路径规则时返回 true。</returns>
    private static bool IsValidRemoteAbsolutePath(HostDefinition host, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        for (int index = 0; index < path.Length; index++)
        {
            if (char.IsWhiteSpace(path[index]))
            {
                return false;
            }
        }

        return host.OperatingSystem == HostOperatingSystem.Linux
            ? path[0] == '/'
            : path.Length >= 3 && char.IsLetter(path[0]) && path[1] == ':' && (path[2] == '\\' || path[2] == '/');
    }

    /// <summary>
    /// 判断实例 Role 列表是否包含指定稳定键。
    /// </summary>
    /// <param name="instance">实例。</param>
    /// <param name="role">稳定 Role 键。</param>
    /// <returns>包含时返回 true。</returns>
    private static bool ContainsRole(InstanceDefinition instance, string role)
    {
        for (int index = 0; index < instance.Roles.Count; index++)
        {
            if (string.Equals(instance.Roles[index], role, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 查找实例引用的主机。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="hostId">主机标识。</param>
    /// <returns>主机。</returns>
    private static HostDefinition FindHost(DeploymentProfile profile, string hostId)
    {
        for (int index = 0; index < profile.Environment.Hosts.Count; index++)
        {
            HostDefinition host = profile.Environment.Hosts[index];
            if (string.Equals(host.HostId, hostId, StringComparison.Ordinal))
            {
                return host;
            }
        }

        throw new PlanValidationException($"找不到主机：{hostId}。");
    }

    /// <summary>
    /// 添加实例配置、服务定义和启动链路。
    /// </summary>
    /// <param name="plan">目标计划。</param>
    /// <param name="instance">目标实例。</param>
    /// <param name="restart">是否先安全停止旧实例。</param>
    /// <param name="forceApproval">是否需要人工确认。</param>
    private static void AddInstallSteps(DeploymentPlan plan, InstanceDefinition instance, bool restart, bool forceApproval)
    {
        AddStep(plan, DeploymentAction.WriteConfiguration, $"写入 {instance.InstanceId} 配置", instance.HostId, instance.InstanceId);
        AddStep(plan, DeploymentAction.InstallService, $"安装 {instance.InstanceId} 服务", instance.HostId, instance.InstanceId);
        if (restart)
        {
            AddRestartSteps(plan, instance, forceApproval);
            return;
        }

        AddStep(plan, DeploymentAction.ActivateRelease, $"激活 {instance.InstanceId} 版本", instance.HostId, instance.InstanceId);
        AddStep(plan, DeploymentAction.StartService, $"启动 {instance.InstanceId}", instance.HostId, instance.InstanceId);
        AddStep(plan, DeploymentAction.WaitForHealth, $"确认 {instance.InstanceId} 健康", instance.HostId, instance.InstanceId, forceApproval, 3);
    }

    /// <summary>
    /// 添加一个实例的 Drain、停止、版本切换和健康等待步骤。
    /// </summary>
    /// <param name="plan">目标计划。</param>
    /// <param name="instance">目标实例。</param>
    /// <param name="forceApproval">是否强制人工确认。</param>
    private static void AddRestartSteps(DeploymentPlan plan, InstanceDefinition instance, bool forceApproval)
    {
        AddStep(plan, DeploymentAction.BeginDrain, $"摘除 {instance.InstanceId} 流量", instance.HostId, instance.InstanceId, forceApproval);
        AddStep(plan, DeploymentAction.WaitForDrain, $"等待 {instance.InstanceId} 排空", instance.HostId, instance.InstanceId, forceApproval);
        AddStep(plan, DeploymentAction.StopService, $"停止 {instance.InstanceId}", instance.HostId, instance.InstanceId, forceApproval);
        AddStep(plan, DeploymentAction.ActivateRelease, $"切换 {instance.InstanceId} 版本", instance.HostId, instance.InstanceId);
        AddStep(plan, DeploymentAction.StartService, $"启动 {instance.InstanceId}", instance.HostId, instance.InstanceId);
        AddStep(plan, DeploymentAction.WaitForHealth, $"确认 {instance.InstanceId} 健康", instance.HostId, instance.InstanceId, forceApproval, 3);
    }

    /// <summary>
    /// 为静态组件添加版本目录发布步骤。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="plan">目标计划。</param>
    private static void AddStaticSteps(DeploymentProfile profile, DeploymentPlan plan)
    {
        for (int index = 0; index < profile.Environment.Instances.Count; index++)
        {
            InstanceDefinition instance = profile.Environment.Instances[index];
            if (instance.Enabled
                && instance.Component == ComponentKind.StaticContent
                && ShouldPublishInstance(profile, instance))
            {
                AddStep(plan, DeploymentAction.PublishStaticContent, $"发布 {instance.InstanceId} 静态内容", instance.HostId, instance.InstanceId);
            }
        }
    }

    /// <summary>
    /// 为不依赖远程主机的桌面和移动客户端制品添加发布确认步骤。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="plan">目标计划。</param>
    private static void AddClientArtifactSteps(DeploymentProfile profile, DeploymentPlan plan)
    {
        for (int index = 0; index < profile.Project.PublishTargets.Count; index++)
        {
            BuildTargetKind target = profile.Project.PublishTargets[index];
            if (target is BuildTargetKind.ClientWindowsX64 or BuildTargetKind.ClientMacOS or BuildTargetKind.ClientAndroid or BuildTargetKind.ClientWebGL)
            {
                AddStep(plan, DeploymentAction.PublishClientArtifact, $"整理 {target} 客户端发布制品", instanceId: target.ToString());
            }
        }
    }

    /// <summary>
    /// 判断当前发布范围是否包含需要通过 SSH 操作的制品目标。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <returns>存在远程目标时返回 true。</returns>
    private static bool HasRemotePublishTarget(DeploymentProfile profile)
    {
        for (int index = 0; index < profile.Project.PublishTargets.Count; index++)
        {
            if (profile.Project.PublishTargets[index] is BuildTargetKind.ServerLinuxX64
                or BuildTargetKind.ServerWindowsX64
                or BuildTargetKind.AuthenticationServer
                or BuildTargetKind.DatabaseServer
                or BuildTargetKind.ClientWebGL)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断一个拓扑实例是否属于当前发布目标范围。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="instance">候选实例。</param>
    /// <returns>应参与本次发布时返回 true。</returns>
    private static bool ShouldPublishInstance(DeploymentProfile profile, InstanceDefinition instance)
    {
        if (instance.Component is ComponentKind.Coordinator or ComponentKind.DedicatedServer)
        {
            if (!profile.Project.PublishTargets.Contains(BuildTargetKind.ServerLinuxX64)
                && !profile.Project.PublishTargets.Contains(BuildTargetKind.ServerWindowsX64))
            {
                return false;
            }

            HostDefinition host = FindHost(profile, instance.HostId);
            BuildTargetKind serverTarget = host.OperatingSystem == HostOperatingSystem.Linux
                ? BuildTargetKind.ServerLinuxX64
                : BuildTargetKind.ServerWindowsX64;
            return profile.Project.PublishTargets.Contains(serverTarget);
        }

        BuildTargetKind target = instance.Component switch
        {
            ComponentKind.AuthenticationServer => BuildTargetKind.AuthenticationServer,
            ComponentKind.DatabaseServer => BuildTargetKind.DatabaseServer,
            ComponentKind.StaticContent => BuildTargetKind.ClientWebGL,
            _ => throw new ArgumentOutOfRangeException(nameof(instance), instance.Component, "未知部署组件。")
        };
        return profile.Project.PublishTargets.Contains(target);
    }

    /// <summary>
    /// 查找用户选择的单实例操作目标。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <returns>目标实例。</returns>
    private static InstanceDefinition FindTargetInstance(DeploymentProfile profile)
    {
        for (int index = 0; index < profile.Environment.Instances.Count; index++)
        {
            InstanceDefinition instance = profile.Environment.Instances[index];
            if (instance.Enabled && string.Equals(instance.InstanceId, profile.TargetInstanceId, StringComparison.Ordinal))
            {
                return instance;
            }
        }

        throw new PlanValidationException($"找不到目标实例：{profile.TargetInstanceId}。");
    }

    /// <summary>
    /// 向计划追加具有稳定顺序标识的步骤。
    /// </summary>
    /// <param name="plan">目标计划。</param>
    /// <param name="action">原子操作。</param>
    /// <param name="displayName">显示名称。</param>
    /// <param name="hostId">目标主机。</param>
    /// <param name="instanceId">目标实例。</param>
    /// <param name="requiresApproval">是否需要人工确认。</param>
    /// <param name="maxAttempts">最大执行次数。</param>
    private static void AddStep(
        DeploymentPlan plan,
        DeploymentAction action,
        string displayName,
        string hostId = "",
        string instanceId = "",
        bool requiresApproval = false,
        int maxAttempts = 1)
    {
        int ordinal = plan.Steps.Count + 1;
        plan.Steps.Add(new DeploymentStep
        {
            StepId = ordinal.ToString("D4", System.Globalization.CultureInfo.InvariantCulture) + "-" + action,
            DisplayName = displayName,
            Action = action,
            HostId = hostId,
            InstanceId = instanceId,
            RequiresApproval = requiresApproval,
            MaxAttempts = Math.Max(1, maxAttempts)
        });
    }

    #endregion
}
