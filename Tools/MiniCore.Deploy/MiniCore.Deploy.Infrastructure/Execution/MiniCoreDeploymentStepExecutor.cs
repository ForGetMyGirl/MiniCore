using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using MiniCore.Deploy.Core.Exceptions;
using MiniCore.Deploy.Core.Execution;
using MiniCore.Deploy.Core.Models;
using MiniCore.Deploy.Infrastructure.Build;
using MiniCore.Deploy.Infrastructure.Persistence;
using MiniCore.Deploy.Infrastructure.Remote;

namespace MiniCore.Deploy.Infrastructure.Execution;

/// <summary>
/// 执行 MiniCore Deploy v1 的本地构建、SSH 上传和服务管理步骤。
/// </summary>
public sealed class MiniCoreDeploymentStepExecutor : IDeploymentStepExecutor
{
    #region Private 私有成员

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions(); // 配置和远程状态 JSON 格式。
    private readonly UnityBatchBuildService unityBuildService; // Unity Player 与 YooAsset 构建器。
    private readonly DotNetComponentPublisher dotNetPublisher; // .NET 服务与工具发布器。
    private readonly GitSourceInspector sourceInspector; // 源码干净状态和指纹读取器。
    private readonly ReleasePackager releasePackager; // 制品压缩与清单生成器。
    private readonly SshRemoteClient remoteClient; // SSH/SFTP 执行器。
    private readonly ApplicationPaths paths; // 仓库外日志与历史目录。
    private readonly ProfileStore profileStore; // 下线成功后持久更新期望拓扑。
    private SourceFingerprint? sourceFingerprint; // 当前执行捕获的源码指纹。
    private HostDefinition? environmentLockHost; // 当前远程环境锁所在主机。
    private string environmentLockDirectory = string.Empty; // 当前远程环境锁目录。
    private string environmentLockPlanId = string.Empty; // 当前远程环境锁所有者计划。
    private string previousReleaseVersion = string.Empty; // 执行前最近一次本地确认的稳定版本。

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 创建完整基础设施步骤执行器。
    /// </summary>
    /// <param name="unityBuildService">Unity 构建服务。</param>
    /// <param name="dotNetPublisher">.NET 发布器。</param>
    /// <param name="sourceInspector">源码检查器。</param>
    /// <param name="releasePackager">制品打包器。</param>
    /// <param name="remoteClient">远程执行器。</param>
    /// <param name="paths">应用路径。</param>
    /// <param name="profileStore">配置方案存储。</param>
    public MiniCoreDeploymentStepExecutor(
        UnityBatchBuildService unityBuildService,
        DotNetComponentPublisher dotNetPublisher,
        GitSourceInspector sourceInspector,
        ReleasePackager releasePackager,
        SshRemoteClient remoteClient,
        ApplicationPaths paths,
        ProfileStore profileStore)
    {
        this.unityBuildService = unityBuildService ?? throw new ArgumentNullException(nameof(unityBuildService));
        this.dotNetPublisher = dotNetPublisher ?? throw new ArgumentNullException(nameof(dotNetPublisher));
        this.sourceInspector = sourceInspector ?? throw new ArgumentNullException(nameof(sourceInspector));
        this.releasePackager = releasePackager ?? throw new ArgumentNullException(nameof(releasePackager));
        this.remoteClient = remoteClient ?? throw new ArgumentNullException(nameof(remoteClient));
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
        this.profileStore = profileStore ?? throw new ArgumentNullException(nameof(profileStore));
    }

    /// <summary>
    /// 在远程预检前通过原子目录创建取得环境级发布互斥锁。
    /// </summary>
    /// <param name="context">本轮发布上下文。</param>
    /// <param name="cancellationToken">用户取消令牌。</param>
    /// <returns>锁取得完成任务。</returns>
    public async Task BeginExecutionAsync(
        DeploymentExecutionContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        previousReleaseVersion = LoadPreviousReleaseVersion(context.Profile.Environment.EnvironmentId);
        if (environmentLockHost != null)
        {
            throw new InvalidOperationException("当前执行器已经持有一个环境发布锁，不能并行执行另一个计划。");
        }

        HostDefinition? host = FindEnvironmentLockHost(context.Profile);
        if (host == null)
        {
            return;
        }

        if (host.OperatingSystem == HostOperatingSystem.Linux)
        {
            await ValidateLinuxSudoAsync(host, cancellationToken).ConfigureAwait(false);
        }

        string lockDirectory = CombineRemote(
            host,
            host.DeploymentRoot,
            "state",
            "environment-" + context.Profile.Environment.EnvironmentId + ".deploy.lock");
        string stateDirectory = GetParentRemotePath(
            lockDirectory,
            host.OperatingSystem == HostOperatingSystem.Linux ? '/' : '\\');
        string ownerJson = JsonSerializer.Serialize(
            new
            {
                planId = context.Plan.PlanId,
                environmentId = context.Profile.Environment.EnvironmentId,
                @operator = Environment.UserName + "@" + Environment.MachineName,
                startedAtUtc = DateTimeOffset.UtcNow
            },
            JsonOptions);
        string ownerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(ownerJson));
        string command = RemoteCommandBuilder.ForHost(
            host,
            "set -eu; root=" + RemoteCommandBuilder.QuoteLinux(host.DeploymentRoot)
                + "; state=" + RemoteCommandBuilder.QuoteLinux(stateDirectory)
                + "; lock=" + RemoteCommandBuilder.QuoteLinux(lockDirectory)
                + "; if [ ! -d \"$root\" ]; then sudo -n install -d -o \"$(id -un)\" -g \"$(id -gn)\" \"$root\"; fi; mkdir -p \"$state\"; if mkdir \"$lock\" 2>/dev/null; then printf %s "
                + RemoteCommandBuilder.QuoteLinux(ownerBase64)
                + " | base64 -d > \"$lock/owner.json\"; printf %s "
                + RemoteCommandBuilder.QuoteLinux(context.Plan.PlanId)
                + " > \"$lock/plan-id\"; exit 0; fi; printf %s "
                + RemoteCommandBuilder.QuoteLinux("__MINICORE_LOCK_OCCUPIED__")
                + "; if [ -f \"$lock/owner.json\" ]; then cat \"$lock/owner.json\"; fi; exit 73",
            "$root=" + RemoteCommandBuilder.QuotePowerShell(host.DeploymentRoot)
                + ";$state=" + RemoteCommandBuilder.QuotePowerShell(stateDirectory)
                + ";$lock=" + RemoteCommandBuilder.QuotePowerShell(lockDirectory)
                + ";New-Item -ItemType Directory -Force -Path $root,$state|Out-Null;if(Test-Path -LiteralPath $lock){Write-Output '__MINICORE_LOCK_OCCUPIED__';$owner=Join-Path $lock 'owner.json';if(Test-Path -LiteralPath $owner){Get-Content -Raw -LiteralPath $owner};exit 73};New-Item -ItemType Directory -Path $lock|Out-Null;[IO.File]::WriteAllBytes((Join-Path $lock 'owner.json'),[Convert]::FromBase64String("
                + RemoteCommandBuilder.QuotePowerShell(ownerBase64)
                + "));[IO.File]::WriteAllText((Join-Path $lock 'plan-id'),"
                + RemoteCommandBuilder.QuotePowerShell(context.Plan.PlanId)
                + ")");
        RemoteCommandResult result = await remoteClient.ExecuteAsync(host, command, CancellationToken.None).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            if (result.ExitCode == 73 || result.StandardOutput.Contains("__MINICORE_LOCK_OCCUPIED__", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(BuildOccupiedLockMessage(host, result.StandardOutput));
            }

            EnsureRemoteSuccess(host, result, "取得环境发布锁");
        }

        environmentLockHost = host;
        environmentLockDirectory = lockDirectory;
        environmentLockPlanId = context.Plan.PlanId;
    }

    /// <summary>
    /// 仅在远程锁仍属于当前计划时释放环境级发布锁。
    /// </summary>
    /// <param name="context">本轮发布上下文。</param>
    /// <param name="cancellationToken">锁清理令牌。</param>
    /// <returns>锁释放完成任务。</returns>
    public async Task EndExecutionAsync(
        DeploymentExecutionContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        HostDefinition? host = environmentLockHost;
        if (host == null)
        {
            return;
        }

        string lockDirectory = environmentLockDirectory;
        string planId = environmentLockPlanId;
        environmentLockHost = null;
        environmentLockDirectory = string.Empty;
        environmentLockPlanId = string.Empty;
        string command = RemoteCommandBuilder.ForHost(
            host,
            "lock=" + RemoteCommandBuilder.QuoteLinux(lockDirectory)
                + "; if [ -d \"$lock\" ] && [ -f \"$lock/plan-id\" ] && [ \"$(cat \"$lock/plan-id\")\" = "
                + RemoteCommandBuilder.QuoteLinux(planId)
                + " ]; then rm -f \"$lock/owner.json\" \"$lock/plan-id\"; rmdir \"$lock\"; fi",
            "$lock=" + RemoteCommandBuilder.QuotePowerShell(lockDirectory)
                + ";$plan=Join-Path $lock 'plan-id';if((Test-Path -LiteralPath $plan) -and ([IO.File]::ReadAllText($plan) -eq "
                + RemoteCommandBuilder.QuotePowerShell(planId)
                + ")){Remove-Item -LiteralPath $lock -Recurse -Force}");
        RemoteCommandResult result = await remoteClient.ExecuteAsync(host, command, cancellationToken).ConfigureAwait(false);
        EnsureRemoteSuccess(host, result, "释放环境发布锁");
    }

    /// <summary>
    /// 将计划原子步骤分派到对应的本地或远程实现。
    /// </summary>
    /// <param name="step">目标步骤。</param>
    /// <param name="context">发布上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结构化步骤结果。</returns>
    public async Task<StepResult> ExecuteAsync(
        DeploymentStep step,
        DeploymentExecutionContext context,
        CancellationToken cancellationToken)
    {
        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        try
        {
            string message = step.Action switch
            {
                DeploymentAction.Preflight => await PreflightAsync(context, cancellationToken).ConfigureAwait(false),
                DeploymentAction.Build => await BuildAsync(context, cancellationToken).ConfigureAwait(false),
                DeploymentAction.StageArtifact => await StageArtifactAsync(step, context, cancellationToken).ConfigureAwait(false),
                DeploymentAction.WriteConfiguration => await WriteConfigurationAsync(step, context, cancellationToken).ConfigureAwait(false),
                DeploymentAction.InstallService => await InstallServiceAsync(step, context, cancellationToken).ConfigureAwait(false),
                DeploymentAction.BeginDrain => await ExecuteLifecycleAsync(step, context, "drain", cancellationToken).ConfigureAwait(false),
                DeploymentAction.WaitForDrain => await WaitForDrainAsync(step, context, cancellationToken).ConfigureAwait(false),
                DeploymentAction.StopService => await StopServiceAsync(step, context, cancellationToken).ConfigureAwait(false),
                DeploymentAction.ActivateRelease => await ActivateReleaseAsync(step, context, cancellationToken).ConfigureAwait(false),
                DeploymentAction.StartService => await StartServiceAsync(step, context, cancellationToken).ConfigureAwait(false),
                DeploymentAction.WaitForHealth => await WaitForHealthAsync(step, context, cancellationToken).ConfigureAwait(false),
                DeploymentAction.PublishStaticContent => await PublishStaticAsync(step, context, cancellationToken).ConfigureAwait(false),
                DeploymentAction.PublishClientArtifact => await PublishClientArtifactAsync(step, context, cancellationToken).ConfigureAwait(false),
                DeploymentAction.UninstallService => await UninstallServiceAsync(step, context, cancellationToken).ConfigureAwait(false),
                DeploymentAction.PersistState => await PersistStateAsync(context, cancellationToken).ConfigureAwait(false),
                _ => throw new NotSupportedException($"尚未实现发布步骤：{step.Action}。")
            };
            return CreateResult(step, StepStatus.Succeeded, startedAt, string.Empty, message, string.Empty);
        }
        catch (OperationCanceledException)
        {
            return CreateResult(step, StepStatus.Cancelled, startedAt, "CANCELLED", "步骤已取消。", "可以从发布历史继续安全步骤。");
        }
        catch (Exception exception)
        {
            return CreateResult(
                step,
                StepStatus.Failed,
                startedAt,
                GetErrorCode(exception),
                exception.Message,
                GetRecoverySuggestion(step.Action, exception));
        }
    }

    /// <summary>
    /// 在健康检查耗尽重试后恢复实例的上一版本指针、旧配置并重新验证。
    /// </summary>
    /// <param name="failedStep">最终失败步骤。</param>
    /// <param name="context">本轮发布上下文。</param>
    /// <param name="cancellationToken">补偿动作令牌。</param>
    /// <returns>仅健康失败返回补偿结果，其他步骤返回空。</returns>
    public async Task<StepResult?> CompensateAsync(
        DeploymentStep failedStep,
        DeploymentExecutionContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(failedStep);
        ArgumentNullException.ThrowIfNull(context);
        if (failedStep.Action is not (DeploymentAction.StartService or DeploymentAction.WaitForHealth)
            || string.IsNullOrWhiteSpace(failedStep.InstanceId))
        {
            return null;
        }

        var compensationStep = new DeploymentStep
        {
            StepId = failedStep.StepId + "-automatic-rollback",
            DisplayName = "自动恢复 " + failedStep.InstanceId + " 的上一版本",
            Action = DeploymentAction.AutomaticRollback,
            HostId = failedStep.HostId,
            InstanceId = failedStep.InstanceId
        };
        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        try
        {
            string message = await CompensateInstanceAsync(compensationStep, context, cancellationToken).ConfigureAwait(false);
            return CreateResult(compensationStep, StepStatus.Succeeded, startedAt, string.Empty, message, string.Empty);
        }
        catch (Exception exception)
        {
            InstanceDefinition instance = FindInstance(context.Profile, failedStep.InstanceId);
            HostDefinition host = FindHost(context.Profile, instance.HostId);
            string serviceName = GetServiceName(instance);
            string currentPath = CombineRemote(host, host.DeploymentRoot, "current", instance.InstanceId);
            string rollbackDirectory = GetRollbackDirectory(host, context, instance);
            string recovery = host.OperatingSystem == HostOperatingSystem.Linux
                ? $"人工恢复：sudo systemctl stop {serviceName}；读取 {CombineRemote(host, rollbackDirectory, "previous-target.txt")}；将 {currentPath} 重新指向该目录；恢复 {CombineRemote(host, rollbackDirectory, "previous-config.json")}；再执行 sudo systemctl start {serviceName} 并检查日志。"
                : $"人工恢复：停止 Windows 服务 {serviceName}；读取 {CombineRemote(host, rollbackDirectory, "previous-target.txt")}；重建 Junction {currentPath}；恢复 {CombineRemote(host, rollbackDirectory, "previous-config.json")}；再启动服务并检查日志。";
            return CreateResult(
                compensationStep,
                StepStatus.Failed,
                startedAt,
                "AUTOMATIC_ROLLBACK_FAILED",
                "新版本健康失败，自动恢复上一版本也失败：" + exception.Message,
                recovery);
        }
    }

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 停止失败实例、恢复上一指针与配置、重新启动并执行深度健康检查。
    /// </summary>
    /// <param name="step">自动补偿步骤。</param>
    /// <param name="context">发布上下文。</param>
    /// <param name="cancellationToken">补偿动作令牌。</param>
    /// <returns>恢复摘要。</returns>
    private async Task<string> CompensateInstanceAsync(
        DeploymentStep step,
        DeploymentExecutionContext context,
        CancellationToken cancellationToken)
    {
        InstanceDefinition instance = FindInstance(context.Profile, step.InstanceId);
        HostDefinition host = FindHost(context.Profile, instance.HostId);
        await StopServiceAsync(step, context, CancellationToken.None).ConfigureAwait(false);
        string rollbackDirectory = GetRollbackDirectory(host, context, instance);
        string previousTargetPath = CombineRemote(host, rollbackDirectory, "previous-target.txt");
        string previousConfigPath = CombineRemote(host, rollbackDirectory, "previous-config.json");
        string previousServiceDefinitionPath = CombineRemote(host, rollbackDirectory, "previous-service-definition");
        string currentPath = CombineRemote(host, host.DeploymentRoot, "current", instance.InstanceId);
        string configPath = GetInstanceConfigurationPath(host, instance);
        string serviceName = GetServiceName(instance);
        string descriptorPath = CombineRemote(host, host.DeploymentRoot, "instances", instance.InstanceId, "config", "service-host.json");
        string command = RemoteCommandBuilder.ForHost(
            host,
            "previous_file=" + RemoteCommandBuilder.QuoteLinux(previousTargetPath)
                + "; backup_config=" + RemoteCommandBuilder.QuoteLinux(previousConfigPath)
                + "; current=" + RemoteCommandBuilder.QuoteLinux(currentPath)
                + "; config=" + RemoteCommandBuilder.QuoteLinux(configPath)
                + "; backup_unit=" + RemoteCommandBuilder.QuoteLinux(previousServiceDefinitionPath)
                + "; installed_unit=" + RemoteCommandBuilder.QuoteLinux("/etc/systemd/system/" + serviceName + ".service")
                + "; test -s \"$previous_file\"; previous=$(cat \"$previous_file\"); test -d \"$previous\"; ln -sfn \"$previous\" \"$current\"; if [ -f \"$backup_config\" ]; then cp -p \"$backup_config\" \"$config\"; chmod 600 \"$config\"; fi; if [ -f \"$backup_unit\" ]; then sudo install -m 0644 \"$backup_unit\" \"$installed_unit\"; sudo systemctl daemon-reload; fi",
            "$previousFile=" + RemoteCommandBuilder.QuotePowerShell(previousTargetPath)
                + ";$backupConfig=" + RemoteCommandBuilder.QuotePowerShell(previousConfigPath)
                + ";$backupDescriptor=" + RemoteCommandBuilder.QuotePowerShell(previousServiceDefinitionPath)
                + ";$descriptor=" + RemoteCommandBuilder.QuotePowerShell(descriptorPath)
                + ";$current=" + RemoteCommandBuilder.QuotePowerShell(currentPath)
                + ";$config=" + RemoteCommandBuilder.QuotePowerShell(configPath)
                + ";if(-not(Test-Path -LiteralPath $previousFile)){throw 'previous target metadata missing'};$previous=[IO.File]::ReadAllText($previousFile).Trim();if([string]::IsNullOrWhiteSpace($previous) -or (-not(Test-Path -PathType Container -LiteralPath $previous))){throw 'previous release target missing'};if(Test-Path -LiteralPath $current){Remove-Item -Force -Recurse -LiteralPath $current};New-Item -ItemType Junction -Path $current -Target $previous|Out-Null;if(Test-Path -LiteralPath $backupConfig){Copy-Item -Force -LiteralPath $backupConfig -Destination $config};if(Test-Path -LiteralPath $backupDescriptor){Copy-Item -Force -LiteralPath $backupDescriptor -Destination $descriptor}");
        EnsureRemoteSuccess(
            host,
            await remoteClient.ExecuteAsync(host, command, CancellationToken.None).ConfigureAwait(false),
            "恢复上一版本指针和配置");
        await StartServiceAsync(step, context, CancellationToken.None).ConfigureAwait(false);
        await WaitForHealthAsync(step, context, cancellationToken).ConfigureAwait(false);
        return $"实例 {instance.InstanceId} 已恢复上一版本和配置，并通过深度健康检查。";
    }

    /// <summary>
    /// 为所有会修改同一环境远程状态的计划选择稳定的锁主机。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <returns>无需远程发布时为空，否则返回锁主机。</returns>
    private static HostDefinition? FindEnvironmentLockHost(DeploymentProfile profile)
    {
        bool hasRemotePublish = false;
        for (int hostIndex = 0; hostIndex < profile.Environment.Hosts.Count; hostIndex++)
        {
            if (HasSelectedArtifactForHost(profile.Environment.Hosts[hostIndex], profile))
            {
                hasRemotePublish = true;
                break;
            }
        }

        if (!hasRemotePublish)
        {
            return null;
        }

        for (int instanceIndex = 0; instanceIndex < profile.Environment.Instances.Count; instanceIndex++)
        {
            InstanceDefinition instance = profile.Environment.Instances[instanceIndex];
            if (!instance.Enabled || instance.Component != ComponentKind.Coordinator)
            {
                continue;
            }

            for (int hostIndex = 0; hostIndex < profile.Environment.Hosts.Count; hostIndex++)
            {
                HostDefinition host = profile.Environment.Hosts[hostIndex];
                if (string.Equals(host.HostId, instance.HostId, StringComparison.Ordinal))
                {
                    return host;
                }
            }
        }

        return profile.Environment.Hosts
            .OrderBy(static host => host.HostId, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    /// <summary>
    /// 将远程锁所有者记录转换为包含陈旧锁提示的用户错误。
    /// </summary>
    /// <param name="host">锁主机。</param>
    /// <param name="standardOutput">远程锁命令输出。</param>
    /// <returns>可直接显示的错误文本。</returns>
    private static string BuildOccupiedLockMessage(HostDefinition host, string standardOutput)
    {
        const string marker = "__MINICORE_LOCK_OCCUPIED__";
        string ownerJson = standardOutput.Replace(marker, string.Empty, StringComparison.Ordinal).Trim();
        if (string.IsNullOrWhiteSpace(ownerJson))
        {
            return $"环境发布锁已被占用（锁主机 {host.HostId}），但所有者信息缺失。请确认没有其他发布正在运行后人工检查 state 目录中的锁文件。";
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(ownerJson);
            string planId = document.RootElement.TryGetProperty("planId", out JsonElement planElement)
                ? planElement.GetString() ?? "未知计划"
                : "未知计划";
            string operatorName = document.RootElement.TryGetProperty("operator", out JsonElement operatorElement)
                ? operatorElement.GetString() ?? "未知操作者"
                : "未知操作者";
            DateTimeOffset startedAtUtc = document.RootElement.TryGetProperty("startedAtUtc", out JsonElement timeElement)
                && timeElement.TryGetDateTimeOffset(out DateTimeOffset parsed)
                    ? parsed
                    : DateTimeOffset.MinValue;
            bool looksStale = startedAtUtc != DateTimeOffset.MinValue
                && DateTimeOffset.UtcNow - startedAtUtc > TimeSpan.FromHours(12);
            string staleText = looksStale
                ? "该锁已超过 12 小时，疑似陈旧锁；确认对应计划已停止后再人工移除，工具不会自动抢锁。"
                : "请等待该计划完成，不能同时切换同一环境。";
            return $"环境发布锁已被占用：主机 {host.HostId}，计划 {planId}，操作者 {operatorName}，开始时间 {startedAtUtc:O}。{staleText}";
        }
        catch (JsonException)
        {
            return $"环境发布锁已被占用（锁主机 {host.HostId}），所有者记录无法解析：{ownerJson}";
        }
    }

    /// <summary>
    /// 检查源码策略、构建路径、目标模块和所有主机的基础命令。
    /// </summary>
    /// <param name="context">发布上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>预检摘要。</returns>
    private async Task<string> PreflightAsync(DeploymentExecutionContext context, CancellationToken cancellationToken)
    {
        DeploymentProfile profile = context.Profile;
        string logDirectory = Path.Combine(paths.LogsPath, context.Plan.PlanId);
        Directory.CreateDirectory(logDirectory);
        if (profile.Project.BuildTargets.Count > 0)
        {
            sourceFingerprint = await sourceInspector.CaptureAsync(profile.Project.ProjectPath, logDirectory, cancellationToken).ConfigureAwait(false);
            if (profile.Environment.RequireCleanGitWorkspace && !sourceFingerprint.IsClean)
            {
                throw new InvalidOperationException("生产发布要求 Git 工作区和生成结果保持干净；当前存在未提交改动。");
            }
        }
        else
        {
            await EnsureManifestAsync(context, cancellationToken).ConfigureAwait(false);
        }

        if (!Directory.Exists(profile.Project.OutputPath))
        {
            Directory.CreateDirectory(profile.Project.OutputPath);
        }

        var capacity = new List<string>(profile.Environment.Hosts.Count);
        for (int index = 0; index < profile.Environment.Hosts.Count; index++)
        {
            HostDefinition host = profile.Environment.Hosts[index];
            if (!HasSelectedArtifactForHost(host, profile))
            {
                continue;
            }

            string portCheckLinux = BuildLinuxPortCheck(profile, host);
            string portCheckWindows = BuildWindowsPortCheck(profile, host);
            if (host.OperatingSystem == HostOperatingSystem.Linux)
            {
                await ValidateLinuxDependenciesAsync(
                    host,
                    HasHostedComponent(host, profile, ComponentKind.AuthenticationServer),
                    cancellationToken).ConfigureAwait(false);
            }

            string command = RemoteCommandBuilder.ForHost(
                host,
                "set -eu; test \"$(uname -s)\" = Linux; case \"$(uname -m)\" in x86_64|amd64) ;; *) echo 'unsupported architecture' >&2; exit 21;; esac; root=" + RemoteCommandBuilder.QuoteLinux(host.DeploymentRoot) + "; test -d \"$root\"; test -w \"$root\"; available=$(df -Pk \"$root\" | awk 'NR==2 {print $4}'); " + portCheckLinux + " echo \"Linux x64, availableKiB=$available\"",
                "$root=" + RemoteCommandBuilder.QuotePowerShell(host.DeploymentRoot) + ";if(-not [Environment]::Is64BitOperatingSystem){throw 'unsupported architecture'};$principal=New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent());if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'administrator privileges required'};if(-not(Test-Path -LiteralPath $root)){New-Item -ItemType Directory -Force -Path $root|Out-Null};$probe=Join-Path $root '.minicore-write-probe';[IO.File]::WriteAllText($probe,'ok');Remove-Item -LiteralPath $probe -Force;$drive=(Get-Item -LiteralPath $root).PSDrive;$available=$drive.Free;Get-Command Get-FileHash,Expand-Archive,sc.exe|Out-Null;" + portCheckWindows + "Write-Output ('Windows x64, availableBytes='+$available)");
            RemoteCommandResult result = await remoteClient.ExecuteAsync(host, command, cancellationToken).ConfigureAwait(false);
            EnsureRemoteSuccess(host, result, "主机预检");
            capacity.Add(host.HostId + ": " + result.StandardOutput.Trim());
        }

        await ValidateRollingProtocolAsync(profile, cancellationToken).ConfigureAwait(false);

        return "源码、输出目录、SSH 指纹、x64 架构、权限、端口和主机依赖预检通过；精确磁盘预算将在本地清单完成校验后执行。" + (capacity.Count == 0 ? string.Empty : " " + string.Join("；", capacity));
    }

    /// <summary>
    /// 逐项检查 Linux 发布依赖，并把全部缺失命令转换为可定位的中文诊断。
    /// </summary>
    /// <param name="host">目标 Linux 主机。</param>
    /// <param name="requiresCurl">当前主机是否承载 AuthenticationServer。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>依赖检查完成任务。</returns>
    private async Task ValidateLinuxDependenciesAsync(
        HostDefinition host,
        bool requiresCurl,
        CancellationToken cancellationToken)
    {
        var command = new StringBuilder("for minicore_dependency in sha256sum unzip systemctl ss");
        if (requiresCurl)
        {
            command.Append(" curl");
        }

        command.Append(" sudo; do command -v \"$minicore_dependency\" >/dev/null 2>&1 || printf '%s\\n' \"$minicore_dependency\"; done");
        RemoteCommandResult result = await remoteClient.ExecuteAsync(
            host,
            RemoteCommandBuilder.ForHost(host, command.ToString(), string.Empty),
            cancellationToken).ConfigureAwait(false);
        EnsureRemoteSuccess(host, result, "逐项检查 Linux 发布依赖");

        string[] reportedCommands = result.StandardOutput.Split(
            new[] { '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var missingCommands = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < reportedCommands.Length; index++)
        {
            if (IsExpectedLinuxDependency(reportedCommands[index], requiresCurl))
            {
                missingCommands.Add(reportedCommands[index]);
            }
        }

        if (missingCommands.Count > 0)
        {
            throw CreateMissingLinuxDependenciesException(host, missingCommands, requiresCurl);
        }

        await ValidateLinuxSudoAsync(host, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 判断远程依赖检查输出是否属于本次允许识别的固定命令名。
    /// </summary>
    /// <param name="command">远程命令输出的一行。</param>
    /// <param name="requiresCurl">当前主机是否需要 curl。</param>
    /// <returns>属于固定依赖清单时返回 true。</returns>
    private static bool IsExpectedLinuxDependency(string command, bool requiresCurl)
    {
        return command is "sha256sum" or "unzip" or "systemctl" or "ss" or "sudo"
            || (requiresCurl && string.Equals(command, "curl", StringComparison.Ordinal));
    }

    /// <summary>
    /// 检查 sudo 命令及免交互权限，避免首次发布在创建环境锁时只返回空白退出码。
    /// </summary>
    /// <param name="host">目标 Linux 主机。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>sudo 检查完成任务。</returns>
    private async Task ValidateLinuxSudoAsync(HostDefinition host, CancellationToken cancellationToken)
    {
        RemoteCommandResult commandResult = await remoteClient.ExecuteAsync(
            host,
            RemoteCommandBuilder.ForHost(host, "command -v sudo >/dev/null 2>&1", string.Empty),
            cancellationToken).ConfigureAwait(false);
        if (commandResult.ExitCode != 0)
        {
            throw CreateMissingLinuxDependenciesException(
                host,
                new HashSet<string>(StringComparer.Ordinal) { "sudo" },
                false);
        }

        RemoteCommandResult permissionResult = await remoteClient.ExecuteAsync(
            host,
            RemoteCommandBuilder.ForHost(host, "sudo -n true >/dev/null 2>&1", string.Empty),
            cancellationToken).ConfigureAwait(false);
        if (permissionResult.ExitCode != 0)
        {
            throw new DeploymentFailureException(
                "LINUX_SUDO_NON_INTERACTIVE_DENIED",
                $"主机 {host.HostId} 无法通过“sudo -n”取得免交互权限。MiniCore Deploy 不会提示、保存或记录远程 sudo 密码。",
                "请由运维人员通过 visudo 为当前 SSH 用户配置发布所需命令的最小免交互权限，或改用已具备该权限的 SSH 账户；确认 sudo -n true 返回 0 后重新预检。");
        }
    }

    /// <summary>
    /// 根据缺失命令生成不含远程输出和凭据的结构化发布失败。
    /// </summary>
    /// <param name="host">目标 Linux 主机。</param>
    /// <param name="missingCommands">远程检查确认缺失的命令。</param>
    /// <param name="requiresCurl">当前主机是否需要 curl。</param>
    /// <returns>包含逐项原因、错误码和安装方向的异常。</returns>
    private static DeploymentFailureException CreateMissingLinuxDependenciesException(
        HostDefinition host,
        IReadOnlySet<string> missingCommands,
        bool requiresCurl)
    {
        string[] expectedCommands = requiresCurl
            ? new[] { "sha256sum", "unzip", "systemctl", "ss", "curl", "sudo" }
            : new[] { "sha256sum", "unzip", "systemctl", "ss", "sudo" };
        var orderedMissing = new List<string>(missingCommands.Count);
        for (int index = 0; index < expectedCommands.Length; index++)
        {
            if (missingCommands.Contains(expectedCommands[index]))
            {
                orderedMissing.Add(expectedCommands[index]);
            }
        }

        var errorCode = new StringBuilder(orderedMissing.Count == 1
            ? "LINUX_DEPENDENCY_"
            : "LINUX_DEPENDENCIES_MISSING_");
        if (orderedMissing.Count == 1)
        {
            errorCode.Append(orderedMissing[0].ToUpperInvariant()).Append("_MISSING");
        }
        else
        {
            for (int index = 0; index < orderedMissing.Count; index++)
            {
                if (index > 0)
                {
                    errorCode.Append('_');
                }

                errorCode.Append(orderedMissing[index].ToUpperInvariant());
            }
        }

        var message = new StringBuilder("主机 ").Append(host.HostId).Append(" 缺少 Linux 发布依赖：");
        var recovery = new StringBuilder("请由运维人员安装或修正以下主机依赖后重新预检；MiniCore Deploy 只检查，不会自动安装系统包：");
        for (int index = 0; index < orderedMissing.Count; index++)
        {
            string command = orderedMissing[index];
            message.Append("\n- ").Append(command).Append("：").Append(GetLinuxDependencyPurpose(command));
            recovery.Append("\n- ").Append(command).Append("：").Append(GetLinuxDependencyRecovery(command));
        }

        return new DeploymentFailureException(errorCode.ToString(), message.ToString(), recovery.ToString());
    }

    /// <summary>
    /// 返回 Linux 命令在发布流程中的用途说明。
    /// </summary>
    /// <param name="command">固定依赖命令名。</param>
    /// <returns>中文用途。</returns>
    private static string GetLinuxDependencyPurpose(string command)
    {
        return command switch
        {
            "sha256sum" => "校验上传制品和 Release 清单哈希。",
            "unzip" => "解压不可变发布制品。",
            "systemctl" => "安装、启动、停止和查询 systemd 服务。",
            "ss" => "只读检查监听端口及其进程归属。",
            "curl" => "检查 AuthenticationServer 的 HTTP 就绪状态。",
            "sudo" => "以免交互方式执行受控的服务和目录管理命令。",
            _ => "执行 Linux 发布预检。"
        };
    }

    /// <summary>
    /// 返回 Linux 缺失命令对应的安装或主机整改方向。
    /// </summary>
    /// <param name="command">固定依赖命令名。</param>
    /// <returns>不会由工具自动执行的中文恢复建议。</returns>
    private static string GetLinuxDependencyRecovery(string command)
    {
        return command switch
        {
            "sha256sum" => "安装 coreutils（Debian/Ubuntu：apt install coreutils；RHEL/CentOS/Alibaba Cloud Linux：dnf 或 yum install coreutils）。",
            "unzip" => "安装 unzip（Debian/Ubuntu：apt install unzip；RHEL/CentOS/Alibaba Cloud Linux：dnf 或 yum install unzip）。",
            "systemctl" => "确认主机使用 systemd 并安装 systemd；不使用 systemd 的发行版暂不支持当前 Linux 服务后端。",
            "ss" => "安装 iproute2（Debian/Ubuntu：apt install iproute2；RHEL/CentOS/Alibaba Cloud Linux：dnf 或 yum install iproute）。",
            "curl" => "安装 curl（Debian/Ubuntu：apt install curl；RHEL/CentOS/Alibaba Cloud Linux：dnf 或 yum install curl）。",
            "sudo" => "安装 sudo，并通过 visudo 为当前 SSH 用户配置发布所需命令的最小免交互权限；不要把密码写入方案或命令。",
            _ => "按照目标发行版的系统包管理说明安装该命令。"
        };
    }

    /// <summary>
    /// 在滚动计划执行前比较远程稳定状态与目标控制协议版本。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>校验完成任务。</returns>
    private async Task ValidateRollingProtocolAsync(DeploymentProfile profile, CancellationToken cancellationToken)
    {
        if (profile.Operation is not (DeploymentOperation.FullRelease or DeploymentOperation.BusinessRelease or DeploymentOperation.Rollback))
        {
            return;
        }

        string targetProtocolVersion = "1";
        if (profile.Operation == DeploymentOperation.Rollback)
        {
            string manifestPath = Path.Combine(profile.Project.OutputPath, profile.Environment.ReleaseVersion, "ReleaseManifest.json");
            if (!File.Exists(manifestPath))
            {
                throw new FileNotFoundException("回滚目标缺少 ReleaseManifest。", manifestPath);
            }

            await using FileStream stream = File.OpenRead(manifestPath);
            ReleaseManifest manifest = await JsonSerializer.DeserializeAsync<ReleaseManifest>(stream, JsonOptions, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidDataException("回滚 ReleaseManifest 无效。");
            targetProtocolVersion = manifest.ControlProtocolVersion;
        }

        for (int index = 0; index < profile.Environment.Hosts.Count; index++)
        {
            HostDefinition host = profile.Environment.Hosts[index];
            string statePath = CombineRemote(host, host.DeploymentRoot, "state", "deployment-state.json");
            string command = RemoteCommandBuilder.ForHost(
                host,
                "if [ -f " + RemoteCommandBuilder.QuoteLinux(statePath) + " ]; then cat " + RemoteCommandBuilder.QuoteLinux(statePath) + "; fi",
                "$path=" + RemoteCommandBuilder.QuotePowerShell(statePath) + ";if(Test-Path -LiteralPath $path){Get-Content -Raw -LiteralPath $path}");
            RemoteCommandResult result = await remoteClient.ExecuteAsync(host, command, cancellationToken).ConfigureAwait(false);
            EnsureRemoteSuccess(host, result, "读取远程发布状态");
            if (string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                continue;
            }

            using JsonDocument document = JsonDocument.Parse(result.StandardOutput);
            if (!document.RootElement.TryGetProperty("controlProtocolVersion", out JsonElement protocolElement))
            {
                continue;
            }

            string currentProtocolVersion = protocolElement.GetString() ?? string.Empty;
            if (!string.Equals(currentProtocolVersion, targetProtocolVersion, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"主机 {host.HostId} 的控制协议 {currentProtocolVersion} 与目标 {targetProtocolVersion} 不兼容。请选择 MaintenanceRelease 维护窗口全停更新。");
            }
        }
    }

    /// <summary>
    /// 生成首次安装或扩容使用的 Linux 监听端口冲突检查。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="host">目标主机。</param>
    /// <returns>安全拼接的固定 Shell 片段。</returns>
    private static string BuildLinuxPortCheck(DeploymentProfile profile, HostDefinition host)
    {
        var builder = new StringBuilder();
        for (int index = 0; index < profile.Environment.Instances.Count; index++)
        {
            InstanceDefinition instance = profile.Environment.Instances[index];
            if (!ShouldCheckPreflightPorts(profile, host.HostId, instance))
            {
                continue;
            }

            string serviceName = ServiceNameFormatter.Format(instance.InstanceId);
            AppendLinuxPortOwnershipCheck(builder, instance.InnerPort, serviceName);
            if (instance.Component is ComponentKind.Coordinator or ComponentKind.DedicatedServer)
            {
                AppendLinuxPortOwnershipCheck(builder, instance.OuterPort, serviceName);
                AppendLinuxPortOwnershipCheck(builder, instance.ManagementPort, serviceName);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// 生成首次安装或扩容使用的 Windows 监听端口冲突检查。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="host">目标主机。</param>
    /// <returns>固定 PowerShell 片段。</returns>
    private static string BuildWindowsPortCheck(DeploymentProfile profile, HostDefinition host)
    {
        var builder = new StringBuilder();
        for (int index = 0; index < profile.Environment.Instances.Count; index++)
        {
            InstanceDefinition instance = profile.Environment.Instances[index];
            if (!ShouldCheckPreflightPorts(profile, host.HostId, instance))
            {
                continue;
            }

            string serviceName = ServiceNameFormatter.Format(instance.InstanceId);
            AppendWindowsPortOwnershipCheck(builder, instance.InnerPort, serviceName);
            if (instance.Component is ComponentKind.Coordinator or ComponentKind.DedicatedServer)
            {
                AppendWindowsPortOwnershipCheck(builder, instance.OuterPort, serviceName);
                AppendWindowsPortOwnershipCheck(builder, instance.ManagementPort, serviceName);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// 判断一个实例是否属于首次安装或扩容续跑时需要核对所有权的目标。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="hostId">主机标识。</param>
    /// <param name="instance">候选实例。</param>
    /// <returns>需要校验时返回 true。</returns>
    private static bool ShouldCheckPreflightPorts(
        DeploymentProfile profile,
        string hostId,
        InstanceDefinition instance)
    {
        if (profile.Operation is not (DeploymentOperation.FirstInstall or DeploymentOperation.ScaleOut))
        {
            return false;
        }

        return instance.Enabled
            && instance.Component != ComponentKind.StaticContent
            && string.Equals(instance.HostId, hostId, StringComparison.Ordinal)
            && (profile.Operation != DeploymentOperation.ScaleOut
                || string.Equals(instance.InstanceId, profile.TargetInstanceId, StringComparison.Ordinal));
    }

    /// <summary>
    /// 追加一个允许由目标 systemd 服务自身占用端口的 Linux 预检片段。
    /// </summary>
    /// <param name="builder">命令构建器。</param>
    /// <param name="port">监听端口。</param>
    /// <param name="serviceName">期望服务名。</param>
    private static void AppendLinuxPortOwnershipCheck(StringBuilder builder, int port, string serviceName)
    {
        if (port <= 0)
        {
            return;
        }

        builder.Append("port=").Append(port)
            .Append("; service=").Append(RemoteCommandBuilder.QuoteLinux(serviceName))
            .Append("; if ss -ltnH | awk '{value=$4; sub(/^.*:/,\"\",value); print value}' | grep -Fqx \"$port\"; then pid=$(sudo systemctl show -p MainPID --value \"$service\" 2>/dev/null || true); if [ \"${pid:-0}\" -le 0 ] || ! sudo ss -ltnpH \"sport = :$port\" | grep -Fq \"pid=$pid,\"; then echo \"port in use by external process: $port\" >&2; exit 22; fi; fi;");
    }

    /// <summary>
    /// 追加一个允许由目标 Windows 服务或其直接子进程占用端口的预检片段。
    /// </summary>
    /// <param name="builder">脚本构建器。</param>
    /// <param name="port">监听端口。</param>
    /// <param name="serviceName">期望服务名。</param>
    private static void AppendWindowsPortOwnershipCheck(StringBuilder builder, int port, string serviceName)
    {
        if (port <= 0)
        {
            return;
        }

        builder.Append("$port=").Append(port)
            .Append(";$listener=Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue|Select-Object -First 1;if($listener){$service=Get-CimInstance Win32_Service -Filter \"Name='")
            .Append(serviceName)
            .Append("'\" -ErrorAction SilentlyContinue;$owner=Get-CimInstance Win32_Process -Filter ('ProcessId='+$listener.OwningProcess) -ErrorAction SilentlyContinue;$owned=$service -and (($listener.OwningProcess -eq $service.ProcessId) -or ($owner -and $owner.ParentProcessId -eq $service.ProcessId));if(-not $owned){throw ('port in use by external process: '+$port)}};");
    }

    /// <summary>
    /// 调用 Unity、.NET 发布器并生成统一 ReleaseManifest。
    /// </summary>
    /// <param name="context">发布上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>构建摘要。</returns>
    private async Task<string> BuildAsync(DeploymentExecutionContext context, CancellationToken cancellationToken)
    {
        if (sourceFingerprint == null)
        {
            string logDirectory = Path.Combine(paths.LogsPath, context.Plan.PlanId);
            Directory.CreateDirectory(logDirectory);
            sourceFingerprint = await sourceInspector.CaptureAsync(context.Profile.Project.ProjectPath, logDirectory, cancellationToken).ConfigureAwait(false);
        }

        string stagingParent = Path.Combine(context.Profile.Project.OutputPath, ".staging");
        string stagingRoot = Path.Combine(
            stagingParent,
            context.Profile.Environment.ReleaseVersion + "-" + context.Plan.PlanId);
        Directory.CreateDirectory(stagingParent);
        if (Directory.Exists(stagingRoot))
        {
            Directory.Delete(stagingRoot, true);
        }

        Directory.CreateDirectory(stagingRoot);
        UnityBuildResponse unity = await unityBuildService.BuildAsync(context.Profile, stagingRoot, cancellationToken).ConfigureAwait(false);
        string postGenerationLogDirectory = Path.Combine(paths.LogsPath, context.Plan.PlanId, "post-generation");
        Directory.CreateDirectory(postGenerationLogDirectory);
        SourceFingerprint postGenerationFingerprint = await sourceInspector.CaptureAsync(context.Profile.Project.ProjectPath, postGenerationLogDirectory, cancellationToken).ConfigureAwait(false);
        if (context.Profile.Environment.RequireCleanGitWorkspace && !postGenerationFingerprint.IsClean)
        {
            throw new InvalidOperationException("生产构建的代码生成结果与仓库不一致。请审查并提交生成文件后重新发布。");
        }

        sourceFingerprint = postGenerationFingerprint;
        await dotNetPublisher.PublishAsync(context.Profile, stagingRoot, cancellationToken).ConfigureAwait(false);
        await releasePackager.CreateManifestAsync(context.Profile, sourceFingerprint.ToString(), stagingRoot, cancellationToken).ConfigureAwait(false);
        context.ReleaseManifest = await releasePackager.CommitAsync(context.Profile, stagingRoot, cancellationToken).ConfigureAwait(false);
        return $"构建完成：Unity 输出 {unity.Outputs.Length} 项，发布清单包含 {context.ReleaseManifest.Artifacts.Count} 个不可变制品。";
    }

    /// <summary>
    /// 上传目标主机所需制品、校验哈希并解压到版本目录。
    /// </summary>
    /// <param name="step">目标主机步骤。</param>
    /// <param name="context">发布上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>上传摘要。</returns>
    private async Task<string> StageArtifactAsync(DeploymentStep step, DeploymentExecutionContext context, CancellationToken cancellationToken)
    {
        HostDefinition host = FindHost(context.Profile, step.HostId);
        ReleaseManifest requestedManifest = await EnsureManifestAsync(context, cancellationToken).ConfigureAwait(false);
        string releaseRoot = Path.GetFullPath(Path.Combine(context.Profile.Project.OutputPath, requestedManifest.ReleaseVersion));
        ReleaseManifest manifest = await releasePackager.LoadAndValidateAsync(releaseRoot, true, cancellationToken).ConfigureAwait(false);
        context.ReleaseManifest = manifest;
        string remoteReleaseRoot = CombineRemote(host, host.DeploymentRoot, "releases", manifest.ReleaseVersion);
        var selectedArtifacts = new List<ReleaseArtifact>();
        long compressedBytes = 0;
        long uncompressedBytes = 0;
        for (int index = 0; index < manifest.Artifacts.Count; index++)
        {
            ReleaseArtifact artifact = manifest.Artifacts[index];
            if (!ShouldStageArtifact(host, context.Profile, artifact.Target))
            {
                continue;
            }

            selectedArtifacts.Add(artifact);
            compressedBytes = checked(compressedBytes + artifact.Length);
            uncompressedBytes = checked(uncompressedBytes + artifact.UncompressedLength);
        }

        ReleaseManifest? existingManifest = await ReadRemoteReleaseManifestAsync(host, remoteReleaseRoot, cancellationToken).ConfigureAwait(false);
        if (existingManifest != null)
        {
            if (!existingManifest.IsCompleteRelease
                || !string.Equals(existingManifest.ReleaseContentSha256, manifest.ReleaseContentSha256, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"主机 {host.HostId} 已存在 ReleaseVersion {manifest.ReleaseVersion}，但内容摘要不同或不是完整 Release，禁止覆盖。");
            }

            await EnsureRemoteReleaseTargetsAsync(host, remoteReleaseRoot, selectedArtifacts, cancellationToken).ConfigureAwait(false);
            return $"主机 {host.HostId} 已存在相同内容的不可变 Release，已安全复用。";
        }

        long workingBytes = checked(compressedBytes + checked(uncompressedBytes * 2));
        long safetyBytes = Math.Max(64L * 1024 * 1024, workingBytes / 5);
        long requiredBytes = checked(workingBytes + safetyBytes);
        await EnsureRemoteDiskCapacityAsync(host, requiredBytes, cancellationToken).ConfigureAwait(false);

        string remoteStagingRoot = CombineRemote(
            host,
            host.DeploymentRoot,
            "releases",
            "." + manifest.ReleaseVersion + "." + context.Plan.PlanId + ".staging");
        string prepareStaging = RemoteCommandBuilder.ForHost(
            host,
            "staging=" + RemoteCommandBuilder.QuoteLinux(remoteStagingRoot) + "; rm -rf \"$staging\"; mkdir -p \"$staging/archives\"",
            "$staging=" + RemoteCommandBuilder.QuotePowerShell(remoteStagingRoot) + ";if(Test-Path -LiteralPath $staging){Remove-Item -LiteralPath $staging -Recurse -Force};New-Item -ItemType Directory -Force -Path (Join-Path $staging 'archives')|Out-Null");
        EnsureRemoteSuccess(
            host,
            await remoteClient.ExecuteAsync(host, prepareStaging, cancellationToken).ConfigureAwait(false),
            "准备隔离发布目录");

        try
        {
            int uploaded = 0;
            for (int index = 0; index < selectedArtifacts.Count; index++)
            {
                ReleaseArtifact artifact = selectedArtifacts[index];
                string localPath = Path.Combine(releaseRoot, artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                string remoteArchivePath = CombineRemote(host, remoteStagingRoot, "archives", Path.GetFileName(artifact.RelativePath));
                await remoteClient.UploadFileAsync(host, localPath, ToSftpPath(host, remoteArchivePath), cancellationToken).ConfigureAwait(false);
                string targetDirectory = CombineRemote(host, remoteStagingRoot, artifact.Target.ToString());
                string temporaryTargetDirectory = targetDirectory + ".extracting";
                string verifyAndExtract = RemoteCommandBuilder.ForHost(
                    host,
                    "archive=" + RemoteCommandBuilder.QuoteLinux(remoteArchivePath)
                        + "; target=" + RemoteCommandBuilder.QuoteLinux(targetDirectory)
                        + "; temporary=" + RemoteCommandBuilder.QuoteLinux(temporaryTargetDirectory)
                        + "; test \"$(sha256sum \"$archive\" | cut -d' ' -f1)\" = " + RemoteCommandBuilder.QuoteLinux(artifact.Sha256)
                        + " && test ! -e \"$target\" && rm -rf \"$temporary\" && mkdir -p \"$temporary\" && unzip -q \"$archive\" -d \"$temporary\" && mv \"$temporary\" \"$target\"",
                    "$archive=" + RemoteCommandBuilder.QuotePowerShell(remoteArchivePath)
                        + ";$target=" + RemoteCommandBuilder.QuotePowerShell(targetDirectory)
                        + ";$temporary=" + RemoteCommandBuilder.QuotePowerShell(temporaryTargetDirectory)
                        + ";$actual=(Get-FileHash -Algorithm SHA256 -LiteralPath $archive).Hash.ToLowerInvariant();if($actual -ne " + RemoteCommandBuilder.QuotePowerShell(artifact.Sha256)
                        + "){throw 'SHA256 mismatch'};if(Test-Path -LiteralPath $target){throw 'immutable target already exists'};if(Test-Path -LiteralPath $temporary){Remove-Item -LiteralPath $temporary -Recurse -Force};New-Item -ItemType Directory -Path $temporary|Out-Null;Expand-Archive -LiteralPath $archive -DestinationPath $temporary;Move-Item -LiteralPath $temporary -Destination $target");
                EnsureRemoteSuccess(host, await remoteClient.ExecuteAsync(host, verifyAndExtract, cancellationToken).ConfigureAwait(false), $"校验并展开 {artifact.Target}");
                uploaded++;
            }

            string manifestPath = Path.Combine(releaseRoot, "ReleaseManifest.json");
            await remoteClient.UploadFileAsync(
                host,
                manifestPath,
                ToSftpPath(host, CombineRemote(host, remoteStagingRoot, "ReleaseManifest.json")),
                cancellationToken).ConfigureAwait(false);
            string commit = RemoteCommandBuilder.ForHost(
                host,
                "staging=" + RemoteCommandBuilder.QuoteLinux(remoteStagingRoot)
                    + "; final=" + RemoteCommandBuilder.QuoteLinux(remoteReleaseRoot)
                    + "; test -f \"$staging/ReleaseManifest.json\"; if [ -e \"$final\" ]; then echo 'release appeared during commit' >&2; exit 42; fi; mv \"$staging\" \"$final\"",
                "$staging=" + RemoteCommandBuilder.QuotePowerShell(remoteStagingRoot)
                    + ";$final=" + RemoteCommandBuilder.QuotePowerShell(remoteReleaseRoot)
                    + ";if(-not(Test-Path -LiteralPath (Join-Path $staging 'ReleaseManifest.json'))){throw 'staged manifest missing'};if(Test-Path -LiteralPath $final){throw 'release appeared during commit'};Move-Item -LiteralPath $staging -Destination $final");
            EnsureRemoteSuccess(host, await remoteClient.ExecuteAsync(host, commit, CancellationToken.None).ConfigureAwait(false), "原子提交不可变 Release");
            return $"已在 {host.HostId} 校验并原子提交 {uploaded} 个制品；动态空间预算 {requiredBytes} 字节。";
        }
        catch
        {
            await DeleteRemoteDirectoryIfExistsAsync(host, remoteStagingRoot).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// 生成实例外部配置、配置哈希和管理 Token 文件。
    /// </summary>
    /// <param name="step">实例步骤。</param>
    /// <param name="context">发布上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>配置摘要。</returns>
    private async Task<string> WriteConfigurationAsync(DeploymentStep step, DeploymentExecutionContext context, CancellationToken cancellationToken)
    {
        InstanceDefinition instance = FindInstance(context.Profile, step.InstanceId);
        HostDefinition host = FindHost(context.Profile, instance.HostId);
        InstanceDefinition? coordinator = FindCoordinator(context.Profile);
        string configDirectory = CombineRemote(host, host.DeploymentRoot, "instances", instance.InstanceId, "config");
        string logDirectory = CombineRemote(host, host.DeploymentRoot, "instances", instance.InstanceId, "logs");
        if (instance.Component == ComponentKind.StaticContent)
        {
            string prepareDirectories = RemoteCommandBuilder.ForHost(
                host,
                "mkdir -p " + RemoteCommandBuilder.QuoteLinux(configDirectory) + " " + RemoteCommandBuilder.QuoteLinux(logDirectory),
                "$configDir=" + RemoteCommandBuilder.QuotePowerShell(configDirectory) + ";$logDir=" + RemoteCommandBuilder.QuotePowerShell(logDirectory) + ";New-Item -ItemType Directory -Force -Path $configDir,$logDir|Out-Null");
            EnsureRemoteSuccess(host, await remoteClient.ExecuteAsync(host, prepareDirectories, cancellationToken).ConfigureAwait(false), "准备实例目录");
            return $"已准备静态内容 {instance.InstanceId} 的发布目录；该组件不启动服务进程。";
        }

        string instanceInnerAdvertisedHost = InstanceNetworkAddressResolver.ResolveInnerAdvertisedHost(
            context.Profile.Environment.Hosts,
            instance);
        string coordinatorInnerAdvertisedHost = coordinator == null
            ? instanceInnerAdvertisedHost
            : InstanceNetworkAddressResolver.ResolveInnerAdvertisedHost(context.Profile.Environment.Hosts, coordinator);

        string secureDirectories = RemoteCommandBuilder.ForHost(
            host,
            "mkdir -p " + RemoteCommandBuilder.QuoteLinux(logDirectory)
                + " && install -d -m 0700 " + RemoteCommandBuilder.QuoteLinux(configDirectory),
            "$configDir=" + RemoteCommandBuilder.QuotePowerShell(configDirectory)
                + ";$logDir=" + RemoteCommandBuilder.QuotePowerShell(logDirectory)
                + ";New-Item -ItemType Directory -Force -Path $configDir,$logDir|Out-Null;icacls.exe $configDir /inheritance:r /grant:r \"$($env:USERNAME):(OI)(CI)F\" \"*S-1-5-18:(OI)(CI)F\" \"*S-1-5-32-544:(OI)(CI)F\"|Out-Null;if($LASTEXITCODE -ne 0){throw 'secure config directory ACL failed'}");
        EnsureRemoteSuccess(
            host,
            await remoteClient.ExecuteAsync(host, secureDirectories, cancellationToken).ConfigureAwait(false),
            "准备受限实例配置目录");

        if (instance.Component is ComponentKind.AuthenticationServer or ComponentKind.DatabaseServer)
        {
            JsonObject serviceDocument = BuildDotNetServiceConfiguration(context.Profile, instance);
            string serviceConfigVersion = ApplyDotNetServiceConfigIdentity(
                serviceDocument,
                context.Profile.Environment.ReleaseVersion,
                instance.InstanceId);
            string serviceJson = serviceDocument.ToJsonString(JsonOptions);
            string remoteServiceConfig = CombineRemote(host, configDirectory, "appsettings.json");
            string temporaryServiceConfig = remoteServiceConfig + ".tmp";
            string serviceRollbackConfig = CombineRemote(host, GetRollbackDirectory(host, context, instance), "previous-config.json");
            try
            {
                await remoteClient.UploadSensitiveTextAsync(host, serviceJson, ToSftpPath(host, temporaryServiceConfig), cancellationToken).ConfigureAwait(false);
                string activateServiceConfig = RemoteCommandBuilder.ForHost(
                    host,
                    "rollback=" + RemoteCommandBuilder.QuoteLinux(serviceRollbackConfig) + "; mkdir -p \"$(dirname \"$rollback\")\"; if [ -f " + RemoteCommandBuilder.QuoteLinux(remoteServiceConfig) + " ] && [ ! -f \"$rollback\" ]; then cp -p " + RemoteCommandBuilder.QuoteLinux(remoteServiceConfig) + " \"$rollback\"; fi; mv -f " + RemoteCommandBuilder.QuoteLinux(temporaryServiceConfig) + " " + RemoteCommandBuilder.QuoteLinux(remoteServiceConfig) + " && chmod 600 " + RemoteCommandBuilder.QuoteLinux(remoteServiceConfig),
                    "$rollback=" + RemoteCommandBuilder.QuotePowerShell(serviceRollbackConfig) + ";New-Item -ItemType Directory -Force -Path (Split-Path $rollback)|Out-Null;$current=" + RemoteCommandBuilder.QuotePowerShell(remoteServiceConfig) + ";if((Test-Path -LiteralPath $current) -and (-not(Test-Path -LiteralPath $rollback))){Copy-Item -LiteralPath $current -Destination $rollback};Move-Item -Force -LiteralPath " + RemoteCommandBuilder.QuotePowerShell(temporaryServiceConfig) + " -Destination $current");
                EnsureRemoteSuccess(host, await remoteClient.ExecuteAsync(host, activateServiceConfig, cancellationToken).ConfigureAwait(false), "激活 .NET 服务配置");
            }
            catch
            {
                await DeleteRemoteFileIfExistsAsync(host, temporaryServiceConfig).ConfigureAwait(false);
                throw;
            }

            string databaseUsage = instance.Component == ComponentKind.AuthenticationServer ? "账号库" : "游戏库";
            return $"已为 {instance.InstanceId} 写入配置版本 {serviceConfigVersion}、监听、Coordinator 和{databaseUsage}参数；密码未进入本地方案或日志。";
        }

        string tokenPath = CombineRemote(host, configDirectory, "management.token");
        string persistenceMode = instance.RequiresDatabase ? "Database" : "None";
        ComputeDedicatedServerConfigIdentity(
            context.Profile,
            instance,
            coordinator,
            tokenPath,
            logDirectory,
            persistenceMode,
            instanceInnerAdvertisedHost,
            coordinatorInnerAdvertisedHost,
            out string configVersion,
            out string configSha256);
        var document = new JsonObject
        {
            ["environmentId"] = context.Profile.Environment.EnvironmentId,
            ["instanceId"] = instance.InstanceId,
            ["releaseVersion"] = context.Profile.Environment.ReleaseVersion,
            ["controlProtocolVersion"] = "1",
            ["roles"] = new JsonArray(CreateRoleNodes(instance.Roles)),
            ["coordinator"] = new JsonObject
            {
                ["innerHost"] = coordinatorInnerAdvertisedHost,
                ["innerPort"] = coordinator?.InnerPort ?? instance.InnerPort
            },
            ["listeners"] = new JsonObject
            {
                ["innerHost"] = instance.InnerListenHost,
                ["innerPort"] = instance.InnerPort,
                ["outerHost"] = instance.OuterListenHost,
                ["outerPort"] = instance.OuterPort,
                ["outerPath"] = instance.OuterPath
            },
            ["advertised"] = new JsonObject
            {
                ["innerHost"] = instanceInnerAdvertisedHost,
                ["innerPort"] = instance.InnerPort,
                ["outerWebSocketUrl"] = instance.OuterAdvertisedUrl
            },
            ["management"] = new JsonObject
            {
                ["host"] = "127.0.0.1",
                ["port"] = instance.ManagementPort,
                ["tokenFile"] = tokenPath
            },
            ["logPath"] = logDirectory,
            ["persistenceMode"] = persistenceMode,
            ["configVersion"] = configVersion,
            ["configSha256"] = configSha256
        };
        string json = document.ToJsonString(JsonOptions);
        string remoteConfig = CombineRemote(host, configDirectory, "MiniCoreServerRuntime.json");
        string temporaryConfig = remoteConfig + ".tmp";
        string rollbackConfig = CombineRemote(host, GetRollbackDirectory(host, context, instance), "previous-config.json");
        try
        {
            await remoteClient.UploadSensitiveTextAsync(host, json, ToSftpPath(host, temporaryConfig), cancellationToken).ConfigureAwait(false);
            string finalize = RemoteCommandBuilder.ForHost(
                host,
                "rollback=" + RemoteCommandBuilder.QuoteLinux(rollbackConfig) + "; mkdir -p \"$(dirname \"$rollback\")\"; if [ -f " + RemoteCommandBuilder.QuoteLinux(remoteConfig) + " ] && [ ! -f \"$rollback\" ]; then cp -p " + RemoteCommandBuilder.QuoteLinux(remoteConfig) + " \"$rollback\"; fi; mv -f " + RemoteCommandBuilder.QuoteLinux(temporaryConfig) + " " + RemoteCommandBuilder.QuoteLinux(remoteConfig) + " && if [ ! -f " + RemoteCommandBuilder.QuoteLinux(tokenPath) + " ]; then umask 077; head -c 32 /dev/urandom | base64 > " + RemoteCommandBuilder.QuoteLinux(tokenPath) + "; fi && chmod 600 " + RemoteCommandBuilder.QuoteLinux(tokenPath),
                "$rollback=" + RemoteCommandBuilder.QuotePowerShell(rollbackConfig) + ";New-Item -ItemType Directory -Force -Path (Split-Path $rollback)|Out-Null;$current=" + RemoteCommandBuilder.QuotePowerShell(remoteConfig) + ";if((Test-Path -LiteralPath $current) -and (-not(Test-Path -LiteralPath $rollback))){Copy-Item -LiteralPath $current -Destination $rollback};Move-Item -Force -LiteralPath " + RemoteCommandBuilder.QuotePowerShell(temporaryConfig) + " -Destination $current;$token=" + RemoteCommandBuilder.QuotePowerShell(tokenPath) + ";if(-not(Test-Path -LiteralPath $token)){[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))|Set-Content -NoNewline -Encoding ascii -LiteralPath $token}");
            EnsureRemoteSuccess(host, await remoteClient.ExecuteAsync(host, finalize, cancellationToken).ConfigureAwait(false), "激活实例配置");
        }
        catch
        {
            await DeleteRemoteFileIfExistsAsync(host, temporaryConfig).ConfigureAwait(false);
            throw;
        }

        return $"已写入实例 {instance.InstanceId} 的外部配置，独立配置版本为 {configVersion}。";
    }

    /// <summary>
    /// 按跨 System.Text.Json/Newtonsoft.Json 的固定字段规范计算独立配置版本和配置哈希。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="instance">目标 DS 实例。</param>
    /// <param name="coordinator">Coordinator 实例；一体化模式允许为空。</param>
    /// <param name="tokenPath">服务器本地管理 Token 路径。</param>
    /// <param name="logDirectory">实例日志目录。</param>
    /// <param name="persistenceMode">持久化模式名称。</param>
    /// <param name="instanceInnerAdvertisedHost">解析继承后的实例内网公布地址。</param>
    /// <param name="coordinatorInnerAdvertisedHost">解析继承后的 Coordinator 内网公布地址。</param>
    /// <param name="configVersion">输出的哈希派生配置版本。</param>
    /// <param name="configSha256">输出的完整规范化配置哈希。</param>
    private static void ComputeDedicatedServerConfigIdentity(
        DeploymentProfile profile,
        InstanceDefinition instance,
        InstanceDefinition? coordinator,
        string tokenPath,
        string logDirectory,
        string persistenceMode,
        string instanceInnerAdvertisedHost,
        string coordinatorInnerAdvertisedHost,
        out string configVersion,
        out string configSha256)
    {
        var builder = new StringBuilder(768);
        AppendCanonicalString(builder, "schema", "1");
        AppendCanonicalString(builder, "environmentId", profile.Environment.EnvironmentId);
        AppendCanonicalString(builder, "instanceId", instance.InstanceId);
        AppendCanonicalString(builder, "releaseVersion", profile.Environment.ReleaseVersion);
        AppendCanonicalString(builder, "controlProtocolVersion", "1");
        string[] roles = instance.Roles.OrderBy(static role => role, StringComparer.Ordinal).ToArray();
        for (int index = 0; index < roles.Length; index++)
        {
            AppendCanonicalString(builder, "role", roles[index]);
        }

        AppendCanonicalString(builder, "coordinator.innerHost", coordinatorInnerAdvertisedHost);
        AppendCanonicalInteger(builder, "coordinator.innerPort", coordinator?.InnerPort ?? instance.InnerPort);
        AppendCanonicalString(builder, "listeners.innerHost", instance.InnerListenHost);
        AppendCanonicalInteger(builder, "listeners.innerPort", instance.InnerPort);
        AppendCanonicalString(builder, "listeners.outerHost", instance.OuterListenHost);
        AppendCanonicalInteger(builder, "listeners.outerPort", instance.OuterPort);
        AppendCanonicalString(builder, "listeners.outerPath", instance.OuterPath);
        AppendCanonicalString(builder, "advertised.innerHost", instanceInnerAdvertisedHost);
        AppendCanonicalInteger(builder, "advertised.innerPort", instance.InnerPort);
        AppendCanonicalString(builder, "advertised.outerWebSocketUrl", instance.OuterAdvertisedUrl);
        AppendCanonicalString(builder, "management.host", "127.0.0.1");
        AppendCanonicalInteger(builder, "management.port", instance.ManagementPort);
        AppendCanonicalString(builder, "management.tokenFile", tokenPath);
        AppendCanonicalString(builder, "logPath", logDirectory);
        AppendCanonicalString(builder, "persistenceMode", persistenceMode);
        string payloadSha256 = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
        configVersion = "cfg-" + payloadSha256[..16];
        AppendCanonicalString(builder, "configVersion", configVersion);
        configSha256 = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    /// <summary>
    /// 以 UTF-8 Base64 编码追加一个不会受 JSON 转义方式影响的规范字符串字段。
    /// </summary>
    /// <param name="builder">规范字节文本构建器。</param>
    /// <param name="key">固定字段键。</param>
    /// <param name="value">字段文本值。</param>
    private static void AppendCanonicalString(StringBuilder builder, string key, string? value)
    {
        builder.Append(key)
            .Append('=')
            .Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty)))
            .Append('\n');
    }

    /// <summary>
    /// 以十进制不变格式追加一个规范整数。
    /// </summary>
    /// <param name="builder">规范字节文本构建器。</param>
    /// <param name="key">固定字段键。</param>
    /// <param name="value">整数值。</param>
    private static void AppendCanonicalInteger(StringBuilder builder, string key, int value)
    {
        builder.Append(key)
            .Append("=#")
            .Append(value.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Append('\n');
    }

    /// <summary>
    /// 为 Auth/DB appsettings 增加独立于程序版本的确定性配置版本和审计哈希。
    /// </summary>
    /// <param name="document">尚未添加部署元数据的 appsettings。</param>
    /// <param name="releaseVersion">程序发布版本。</param>
    /// <param name="instanceId">实例标识。</param>
    /// <returns>哈希派生的独立配置版本。</returns>
    private static string ApplyDotNetServiceConfigIdentity(
        JsonObject document,
        string releaseVersion,
        string instanceId)
    {
        string payload = document.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        string payloadSha256 = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        string configVersion = "cfg-" + payloadSha256[..16];
        var identity = new StringBuilder(payload.Length + 128)
            .Append(payloadSha256)
            .Append('\n')
            .Append(releaseVersion)
            .Append('\n')
            .Append(instanceId)
            .Append('\n')
            .Append(configVersion)
            .Append('\n');
        string configSha256 = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity.ToString())));
        document["MiniCoreDeployment"] = new JsonObject
        {
            ["ReleaseVersion"] = releaseVersion,
            ["ConfigVersion"] = configVersion,
            ["ConfigSha256"] = configSha256
        };
        return configVersion;
    }

    /// <summary>
    /// 根据组件生成 AuthenticationServer 或 DatabaseServer 实际读取的 appsettings 配置。
    /// </summary>
    /// <param name="profile">完整发布配置。</param>
    /// <param name="instance">Auth 或 DB 实例。</param>
    /// <returns>包含运行参数和数据库连接字符串的 JSON 文档。</returns>
    private static JsonObject BuildDotNetServiceConfiguration(DeploymentProfile profile, InstanceDefinition instance)
    {
        InstanceDefinition coordinator = FindCoordinator(profile)
            ?? throw new InvalidOperationException("Auth/DB 配置生成时找不到 Coordinator 实例。");
        HostDefinition instanceHost = FindHost(profile, instance.HostId);
        string instanceInnerAdvertisedHost = InstanceNetworkAddressResolver.ResolveInnerAdvertisedHost(
            profile.Environment.Hosts,
            instance);
        string coordinatorInnerAdvertisedHost = InstanceNetworkAddressResolver.ResolveInnerAdvertisedHost(
            profile.Environment.Hosts,
            coordinator);
        string connectionString = BuildDatabaseConnectionString(instance.Database);
        if (instance.Component == ComponentKind.AuthenticationServer)
        {
            return new JsonObject
            {
                ["Urls"] = "http://" + instance.InnerListenHost + ":" + instance.InnerPort.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["ConnectionStrings"] = new JsonObject
                {
                    ["Authentication"] = connectionString
                },
                ["Authentication"] = new JsonObject
                {
                    ["CoordinatorWebSocketUrl"] = coordinator.OuterAdvertisedUrl,
                    ["AdvertisedHost"] = instanceInnerAdvertisedHost
                },
                ["Logging"] = new JsonObject
                {
                    ["LogLevel"] = new JsonObject
                    {
                        ["Default"] = "Information",
                        ["Microsoft.AspNetCore"] = "Warning"
                    }
                },
                ["AllowedHosts"] = "*"
            };
        }

        if (instance.Component == ComponentKind.DatabaseServer)
        {
            return new JsonObject
            {
                ["ConnectionStrings"] = new JsonObject
                {
                    ["GameDatabase"] = connectionString
                },
                ["DatabaseServer"] = new JsonObject
                {
                    ["InstanceId"] = instance.InstanceId,
                    ["ListenHost"] = instance.InnerListenHost,
                    ["ListenPort"] = instance.InnerPort,
                    ["AdvertisedHost"] = instanceInnerAdvertisedHost,
                    ["CoordinatorHost"] = coordinatorInnerAdvertisedHost,
                    ["CoordinatorPort"] = coordinator.InnerPort,
                    ["MaximumConcurrency"] = instance.MaximumConcurrency,
                    ["ReadinessFilePath"] = CombineRemote(
                        instanceHost,
                        instanceHost.DeploymentRoot,
                        "instances",
                        instance.InstanceId,
                        "config",
                        "database.ready.json")
                },
                ["Logging"] = new JsonObject
                {
                    ["LogLevel"] = new JsonObject
                    {
                        ["Default"] = "Information",
                        ["Microsoft.EntityFrameworkCore.Database.Command"] = "Warning"
                    }
                }
            };
        }

        throw new ArgumentOutOfRangeException(nameof(instance), instance.Component, "只有 Auth/DB 使用 appsettings 配置生成器。");
    }

    /// <summary>
    /// 使用框架连接字符串构建器安全转义数据库参数。
    /// </summary>
    /// <param name="database">结构化数据库连接参数。</param>
    /// <returns>可供 MySqlConnector 读取的连接字符串。</returns>
    private static string BuildDatabaseConnectionString(DatabaseConnectionDefinition database)
    {
        var builder = new DbConnectionStringBuilder
        {
            ["Server"] = database.Host,
            ["Port"] = database.Port,
            ["Database"] = database.DatabaseName,
            ["User"] = database.UserName,
            ["Password"] = database.Password,
            ["SslMode"] = database.SslMode
        };
        return builder.ConnectionString;
    }

    /// <summary>
    /// 安装 Linux systemd unit 或 Windows ServiceHost 服务定义。
    /// </summary>
    /// <param name="step">实例步骤。</param>
    /// <param name="context">发布上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>安装摘要。</returns>
    private async Task<string> InstallServiceAsync(DeploymentStep step, DeploymentExecutionContext context, CancellationToken cancellationToken)
    {
        InstanceDefinition instance = FindInstance(context.Profile, step.InstanceId);
        HostDefinition host = FindHost(context.Profile, instance.HostId);
        string serviceName = GetServiceName(instance);
        string executable = GetCurrentExecutablePath(host, instance);
        string configPath = GetInstanceConfigurationPath(host, instance);
        if (host.OperatingSystem == HostOperatingSystem.Linux)
        {
            string unit = BuildSystemdUnit(host, instance, executable, configPath);
            string temporaryUnit = CombineRemote(host, host.DeploymentRoot, "state", serviceName + ".service");
            await remoteClient.UploadTextAsync(host, unit, ToSftpPath(host, temporaryUnit), cancellationToken).ConfigureAwait(false);
            string installedUnit = "/etc/systemd/system/" + serviceName + ".service";
            string rollbackUnit = CombineRemote(host, GetRollbackDirectory(host, context, instance), "previous-service-definition");
            string command = "mkdir -p " + RemoteCommandBuilder.QuoteLinux(GetParentRemotePath(rollbackUnit, '/'))
                + "; if sudo test -f " + RemoteCommandBuilder.QuoteLinux(installedUnit) + " && [ ! -f " + RemoteCommandBuilder.QuoteLinux(rollbackUnit) + " ]; then sudo cat " + RemoteCommandBuilder.QuoteLinux(installedUnit) + " > " + RemoteCommandBuilder.QuoteLinux(rollbackUnit) + "; fi; if sudo test -f " + RemoteCommandBuilder.QuoteLinux(installedUnit)
                + " && sudo cmp -s " + RemoteCommandBuilder.QuoteLinux(temporaryUnit) + " " + RemoteCommandBuilder.QuoteLinux(installedUnit)
                + "; then echo 'service definition unchanged'; else sudo install -m 0644 " + RemoteCommandBuilder.QuoteLinux(temporaryUnit) + " " + RemoteCommandBuilder.QuoteLinux(installedUnit)
                + " && sudo systemctl daemon-reload; fi; sudo systemctl enable " + RemoteCommandBuilder.QuoteLinux(serviceName);
            EnsureRemoteSuccess(host, await remoteClient.ExecuteAsync(host, "sh -lc " + RemoteCommandBuilder.QuoteLinux(command), cancellationToken).ConfigureAwait(false), "安装 systemd 服务");
        }
        else
        {
            string descriptorPath = CombineRemote(host, host.DeploymentRoot, "instances", instance.InstanceId, "config", "service-host.json");
            string candidateDescriptorPath = descriptorPath + ".candidate";
            string rollbackDescriptorPath = CombineRemote(host, GetRollbackDirectory(host, context, instance), "previous-service-definition");
            string currentComponentRoot = CombineRemote(host, host.DeploymentRoot, "current", instance.InstanceId);
            string serverCtlPath = CombineRemote(host, currentComponentRoot, "Tools", "ServerCtl", "MiniCore.ServerCtl.exe");
            string descriptor = JsonSerializer.Serialize(new
            {
                executablePath = executable,
                arguments = BuildServiceArguments(instance, configPath),
                workingDirectory = GetParentRemotePath(executable, '\\'),
                logDirectory = CombineRemote(host, host.DeploymentRoot, "instances", instance.InstanceId, "logs"),
                environmentVariables = new Dictionary<string, string>(StringComparer.Ordinal),
                restartOnUnexpectedExit = instance.AutoRestart,
                gracefulShutdownSeconds = 90,
                shutdownExecutablePath = IsDedicatedServer(instance) ? serverCtlPath : string.Empty,
                shutdownArguments = IsDedicatedServer(instance) ? new[] { "--config", configPath, "shutdown" } : Array.Empty<string>()
            }, JsonOptions);
            await remoteClient.UploadTextAsync(host, descriptor, ToSftpPath(host, candidateDescriptorPath), cancellationToken).ConfigureAwait(false);
            string serviceHostPath = CombineRemote(host, currentComponentRoot, "Tools", "ServiceHost", "MiniCore.Deploy.ServiceHost.exe");
            string script = "$name=" + RemoteCommandBuilder.QuotePowerShell(serviceName)
                + ";$descriptor=" + RemoteCommandBuilder.QuotePowerShell(descriptorPath)
                + ";$candidate=" + RemoteCommandBuilder.QuotePowerShell(candidateDescriptorPath)
                + ";$backup=" + RemoteCommandBuilder.QuotePowerShell(rollbackDescriptorPath)
                + ";New-Item -ItemType Directory -Force -Path (Split-Path $backup)|Out-Null;if((Test-Path -LiteralPath $descriptor) -and (-not(Test-Path -LiteralPath $backup))){Copy-Item -LiteralPath $descriptor -Destination $backup};$bin='\"'+" + RemoteCommandBuilder.QuotePowerShell(serviceHostPath) + "+'\" --descriptor \"'+$descriptor+'\"';$service=Get-Service -Name $name -ErrorAction SilentlyContinue;$changed=(-not (Test-Path -LiteralPath $descriptor)) -or (-not ((Get-FileHash -Algorithm SHA256 -LiteralPath $descriptor).Hash -eq (Get-FileHash -Algorithm SHA256 -LiteralPath $candidate).Hash));if($changed){Move-Item -Force -LiteralPath $candidate -Destination $descriptor}else{Remove-Item -Force -LiteralPath $candidate};if(-not $service){sc.exe create $name binPath= $bin start= auto DisplayName= $name|Out-Null;if($LASTEXITCODE -ne 0){throw 'sc.exe service create failed'}}elseif($changed -or "
                + (context.Profile.Operation == DeploymentOperation.ConfigurationUpdate ? "$false" : "$true")
                + "){sc.exe config $name binPath= $bin start= auto|Out-Null;if($LASTEXITCODE -ne 0){throw 'sc.exe service config failed'}}";
            EnsureRemoteSuccess(host, await remoteClient.ExecuteAsync(host, RemoteCommandBuilder.EncodePowerShell(script), cancellationToken).ConfigureAwait(false), "安装 Windows 服务");
        }

        return $"已安装服务定义 {serviceName}。";
    }

    /// <summary>
    /// 对 Dedicated Server 调用本地管理命令；其他进程由服务管理器直接控制。
    /// </summary>
    /// <param name="step">实例步骤。</param>
    /// <param name="context">发布上下文。</param>
    /// <param name="operation">ServerCtl 操作。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>生命周期摘要。</returns>
    private async Task<string> ExecuteLifecycleAsync(DeploymentStep step, DeploymentExecutionContext context, string operation, CancellationToken cancellationToken)
    {
        InstanceDefinition instance = FindInstance(context.Profile, step.InstanceId);
        if (!IsDedicatedServer(instance))
        {
            return $"组件 {instance.Component} 不提供业务 Drain，继续使用进程级生命周期。";
        }

        HostDefinition host = FindHost(context.Profile, instance.HostId);
        RemoteCommandResult result = await ExecuteServerCtlAsync(host, instance, context.Profile.Environment.ReleaseVersion, operation, cancellationToken).ConfigureAwait(false);
        EnsureRemoteSuccess(host, result, "Dedicated Server " + operation);
        return string.IsNullOrWhiteSpace(result.StandardOutput) ? $"{instance.InstanceId} 已接受 {operation}。" : result.StandardOutput.Trim();
    }

    /// <summary>
    /// 轮询本地管理端直至业务 Drain 完成或超时。
    /// </summary>
    /// <param name="step">实例步骤。</param>
    /// <param name="context">发布上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Drain 摘要。</returns>
    private async Task<string> WaitForDrainAsync(DeploymentStep step, DeploymentExecutionContext context, CancellationToken cancellationToken)
    {
        InstanceDefinition instance = FindInstance(context.Profile, step.InstanceId);
        if (!IsDedicatedServer(instance))
        {
            return $"组件 {instance.Component} 无业务 Drain 阻塞项。";
        }

        HostDefinition host = FindHost(context.Profile, instance.HostId);
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(90);
        while (DateTimeOffset.UtcNow < deadline)
        {
            RemoteCommandResult result = await ExecuteServerCtlAsync(host, instance, context.Profile.Environment.ReleaseVersion, "drain-status", cancellationToken).ConfigureAwait(false);
            if (result.ExitCode == 0)
            {
                return string.IsNullOrWhiteSpace(result.StandardOutput) ? $"{instance.InstanceId} 已排空。" : result.StandardOutput.Trim();
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException($"实例 {instance.InstanceId} 在 90 秒内未完成 Drain，需要人工确认业务阻塞项。");
    }

    /// <summary>
    /// 停止目标 systemd 或 Windows 服务。
    /// </summary>
    /// <param name="step">实例步骤。</param>
    /// <param name="context">发布上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>停止摘要。</returns>
    private async Task<string> StopServiceAsync(DeploymentStep step, DeploymentExecutionContext context, CancellationToken cancellationToken)
    {
        InstanceDefinition instance = FindInstance(context.Profile, step.InstanceId);
        HostDefinition host = FindHost(context.Profile, instance.HostId);
        string serviceName = GetServiceName(instance);
        string command = RemoteCommandBuilder.ForHost(
            host,
            "sudo systemctl stop " + RemoteCommandBuilder.QuoteLinux(serviceName),
            "Stop-Service -Name " + RemoteCommandBuilder.QuotePowerShell(serviceName) + " -Force -ErrorAction Stop");
        EnsureRemoteSuccess(host, await remoteClient.ExecuteAsync(host, command, cancellationToken).ConfigureAwait(false), "停止服务");
        return $"已停止 {serviceName}。";
    }

    /// <summary>
    /// 原子更新实例的 current 版本指针。
    /// </summary>
    /// <param name="step">实例步骤。</param>
    /// <param name="context">发布上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>切换摘要。</returns>
    private async Task<string> ActivateReleaseAsync(DeploymentStep step, DeploymentExecutionContext context, CancellationToken cancellationToken)
    {
        InstanceDefinition instance = FindInstance(context.Profile, step.InstanceId);
        HostDefinition host = FindHost(context.Profile, instance.HostId);
        string releasePath = GetReleaseComponentPath(host, instance, context.Profile.Environment.ReleaseVersion);
        string currentPath = CombineRemote(host, host.DeploymentRoot, "current", instance.InstanceId);
        string rollbackDirectory = GetRollbackDirectory(host, context, instance);
        string previousTargetPath = CombineRemote(host, rollbackDirectory, "previous-target.txt");
        string command = RemoteCommandBuilder.ForHost(
            host,
            "current=" + RemoteCommandBuilder.QuoteLinux(currentPath)
                + "; rollback=" + RemoteCommandBuilder.QuoteLinux(rollbackDirectory)
                + "; previous=" + RemoteCommandBuilder.QuoteLinux(previousTargetPath)
                + "; mkdir -p " + RemoteCommandBuilder.QuoteLinux(CombineRemote(host, host.DeploymentRoot, "current")) + " \"$rollback\"; if [ ! -f \"$previous\" ]; then if [ -L \"$current\" ]; then readlink -f \"$current\" > \"$previous\"; else : > \"$previous\"; fi; fi; ln -sfn " + RemoteCommandBuilder.QuoteLinux(releasePath) + " \"$current\"",
            "$target=" + RemoteCommandBuilder.QuotePowerShell(releasePath)
                + ";$link=" + RemoteCommandBuilder.QuotePowerShell(currentPath)
                + ";$rollback=" + RemoteCommandBuilder.QuotePowerShell(rollbackDirectory)
                + ";$previous=" + RemoteCommandBuilder.QuotePowerShell(previousTargetPath)
                + ";New-Item -ItemType Directory -Force -Path (Split-Path $link),$rollback|Out-Null;if(-not(Test-Path -LiteralPath $previous)){if(Test-Path -LiteralPath $link){[IO.File]::WriteAllText($previous,[string](Get-Item -LiteralPath $link).Target)}else{[IO.File]::WriteAllText($previous,'')}};if(Test-Path -LiteralPath $link){Remove-Item -Force -Recurse -LiteralPath $link};New-Item -ItemType Junction -Path $link -Target $target|Out-Null");
        EnsureRemoteSuccess(host, await remoteClient.ExecuteAsync(host, command, cancellationToken).ConfigureAwait(false), "切换版本指针");
        return $"实例 {instance.InstanceId} 已切换到 {context.Profile.Environment.ReleaseVersion}。";
    }

    /// <summary>
    /// 启动目标 systemd 或 Windows 服务。
    /// </summary>
    /// <param name="step">实例步骤。</param>
    /// <param name="context">发布上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>启动摘要。</returns>
    private async Task<string> StartServiceAsync(DeploymentStep step, DeploymentExecutionContext context, CancellationToken cancellationToken)
    {
        InstanceDefinition instance = FindInstance(context.Profile, step.InstanceId);
        HostDefinition host = FindHost(context.Profile, instance.HostId);
        string serviceName = GetServiceName(instance);
        string command = RemoteCommandBuilder.ForHost(
            host,
            "sudo systemctl start " + RemoteCommandBuilder.QuoteLinux(serviceName),
            "Start-Service -Name " + RemoteCommandBuilder.QuotePowerShell(serviceName) + " -ErrorAction Stop");
        EnsureRemoteSuccess(host, await remoteClient.ExecuteAsync(host, command, cancellationToken).ConfigureAwait(false), "启动服务");
        return $"已启动 {serviceName}。";
    }

    /// <summary>
    /// 等待 DS 管理端或系统服务管理器确认目标实例健康。
    /// </summary>
    /// <param name="step">实例步骤。</param>
    /// <param name="context">发布上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>健康摘要。</returns>
    private async Task<string> WaitForHealthAsync(DeploymentStep step, DeploymentExecutionContext context, CancellationToken cancellationToken)
    {
        InstanceDefinition instance = FindInstance(context.Profile, step.InstanceId);
        HostDefinition host = FindHost(context.Profile, instance.HostId);
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(60);
        while (DateTimeOffset.UtcNow < deadline)
        {
            RemoteCommandResult result;
            if (IsDedicatedServer(instance))
            {
                result = await ExecuteCurrentServerCtlAsync(host, instance, "health", cancellationToken).ConfigureAwait(false);
            }
            else if (instance.Component == ComponentKind.AuthenticationServer)
            {
                string serviceName = GetServiceName(instance);
                string readyUrl = "http://127.0.0.1:" + instance.InnerPort.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/ready";
                string command = RemoteCommandBuilder.ForHost(
                    host,
                    "sudo systemctl is-active --quiet " + RemoteCommandBuilder.QuoteLinux(serviceName) + " && curl --fail --silent --show-error --max-time 5 " + RemoteCommandBuilder.QuoteLinux(readyUrl),
                    "$service=Get-Service -Name " + RemoteCommandBuilder.QuotePowerShell(serviceName) + " -ErrorAction Stop;if($service.Status -ne 'Running'){exit 1};(Invoke-WebRequest -UseBasicParsing -TimeoutSec 5 -Uri " + RemoteCommandBuilder.QuotePowerShell(readyUrl) + ").Content");
                result = await remoteClient.ExecuteAsync(host, command, cancellationToken).ConfigureAwait(false);
            }
            else if (instance.Component == ComponentKind.DatabaseServer)
            {
                string serviceName = GetServiceName(instance);
                string readinessPath = CombineRemote(host, host.DeploymentRoot, "instances", instance.InstanceId, "config", "database.ready.json");
                string command = RemoteCommandBuilder.ForHost(
                    host,
                    "sudo systemctl is-active --quiet " + RemoteCommandBuilder.QuoteLinux(serviceName) + " && test -f " + RemoteCommandBuilder.QuoteLinux(readinessPath) + " && cat " + RemoteCommandBuilder.QuoteLinux(readinessPath),
                    "$service=Get-Service -Name " + RemoteCommandBuilder.QuotePowerShell(serviceName) + " -ErrorAction Stop;if($service.Status -ne 'Running'){exit 1};Get-Content -Raw -LiteralPath " + RemoteCommandBuilder.QuotePowerShell(readinessPath));
                result = await remoteClient.ExecuteAsync(host, command, cancellationToken).ConfigureAwait(false);
                if (result.ExitCode == 0 && !IsDatabaseReadinessCurrent(result.StandardOutput, instance.InstanceId))
                {
                    result = new RemoteCommandResult(1, result.StandardOutput, "DatabaseServer 深度就绪状态缺失、陈旧或不完整。");
                }
            }
            else
            {
                string serviceName = GetServiceName(instance);
                string command = RemoteCommandBuilder.ForHost(
                    host,
                    "sudo systemctl is-active --quiet " + RemoteCommandBuilder.QuoteLinux(serviceName),
                    "$service=Get-Service -Name " + RemoteCommandBuilder.QuotePowerShell(serviceName) + " -ErrorAction Stop;if($service.Status -ne 'Running'){exit 1}");
                result = await remoteClient.ExecuteAsync(host, command, cancellationToken).ConfigureAwait(false);
            }

            if (result.ExitCode == 0)
            {
                return string.IsNullOrWhiteSpace(result.StandardOutput) ? $"实例 {instance.InstanceId} 健康。" : result.StandardOutput.Trim();
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException($"实例 {instance.InstanceId} 在 60 秒内未达到健康状态。");
    }

    /// <summary>
    /// 校验 DatabaseServer 就绪文件同时确认数据库、Coordinator、业务 RPC 和新鲜时间戳。
    /// </summary>
    /// <param name="json">远程就绪文件内容。</param>
    /// <param name="instanceId">期望实例标识。</param>
    /// <returns>全部深度条件满足时返回 true。</returns>
    private static bool IsDatabaseReadinessCurrent(string json, string instanceId)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            return root.TryGetProperty("instanceId", out JsonElement instanceElement)
                && string.Equals(instanceElement.GetString(), instanceId, StringComparison.Ordinal)
                && root.TryGetProperty("databaseReady", out JsonElement databaseElement)
                && databaseElement.GetBoolean()
                && root.TryGetProperty("coordinatorRegistered", out JsonElement coordinatorElement)
                && coordinatorElement.GetBoolean()
                && root.TryGetProperty("rpcReady", out JsonElement rpcElement)
                && rpcElement.GetBoolean()
                && root.TryGetProperty("updatedAtUtc", out JsonElement updatedElement)
                && updatedElement.TryGetDateTimeOffset(out DateTimeOffset updatedAtUtc)
                && DateTimeOffset.UtcNow - updatedAtUtc <= TimeSpan.FromSeconds(20);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// 原子切换 WebGL 静态目录版本指针。
    /// </summary>
    /// <param name="step">静态组件步骤。</param>
    /// <param name="context">发布上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>发布摘要。</returns>
    private async Task<string> PublishStaticAsync(DeploymentStep step, DeploymentExecutionContext context, CancellationToken cancellationToken)
    {
        InstanceDefinition instance = FindInstance(context.Profile, step.InstanceId);
        HostDefinition host = FindHost(context.Profile, instance.HostId);
        string source = CombineRemote(host, host.DeploymentRoot, "releases", context.Profile.Environment.ReleaseVersion, BuildTargetKind.ClientWebGL.ToString());
        string current = instance.StaticContentPublishPath;
        string command = RemoteCommandBuilder.ForHost(
            host,
            "mkdir -p " + RemoteCommandBuilder.QuoteLinux(GetParentRemotePath(current, '/')) + " && ln -sfn " + RemoteCommandBuilder.QuoteLinux(source) + " " + RemoteCommandBuilder.QuoteLinux(current),
            "$target=" + RemoteCommandBuilder.QuotePowerShell(source) + ";$link=" + RemoteCommandBuilder.QuotePowerShell(current) + ";New-Item -ItemType Directory -Force -Path (Split-Path $link)|Out-Null;if(Test-Path -LiteralPath $link){Remove-Item -Force -Recurse -LiteralPath $link};New-Item -ItemType Junction -Path $link -Target $target|Out-Null");
        EnsureRemoteSuccess(host, await remoteClient.ExecuteAsync(host, command, cancellationToken).ConfigureAwait(false), "发布静态目录");
        return $"静态目录 {current} 已切换到 {context.Profile.Environment.ReleaseVersion}。";
    }

    /// <summary>
    /// 注销进程服务定义，同时保留实例配置与日志。
    /// </summary>
    /// <param name="step">实例步骤。</param>
    /// <param name="context">发布上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>注销摘要。</returns>
    private async Task<string> UninstallServiceAsync(DeploymentStep step, DeploymentExecutionContext context, CancellationToken cancellationToken)
    {
        InstanceDefinition instance = FindInstance(context.Profile, step.InstanceId);
        HostDefinition host = FindHost(context.Profile, instance.HostId);
        string serviceName = GetServiceName(instance);
        string command = RemoteCommandBuilder.ForHost(
            host,
            "sudo systemctl disable " + RemoteCommandBuilder.QuoteLinux(serviceName) + " || true; sudo rm -f " + RemoteCommandBuilder.QuoteLinux("/etc/systemd/system/" + serviceName + ".service") + "; sudo systemctl daemon-reload",
            "if(Get-Service -Name " + RemoteCommandBuilder.QuotePowerShell(serviceName) + " -ErrorAction SilentlyContinue){sc.exe delete " + RemoteCommandBuilder.QuotePowerShell(serviceName) + "|Out-Null;if($LASTEXITCODE -ne 0){throw 'sc.exe delete failed'}}");
        EnsureRemoteSuccess(host, await remoteClient.ExecuteAsync(host, command, cancellationToken).ConfigureAwait(false), "注销服务");
        return $"已注销 {serviceName}，实例配置和日志仍保留。";
    }

    /// <summary>
    /// 将统一版本和计划标识保存到本地及每台远程主机。
    /// </summary>
    /// <param name="context">发布上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>保存摘要。</returns>
    private async Task<string> PersistStateAsync(DeploymentExecutionContext context, CancellationToken cancellationToken)
    {
        if (context.Profile.Operation == DeploymentOperation.RemoveInstance)
        {
            InstanceDefinition removedInstance = FindInstance(context.Profile, context.Profile.TargetInstanceId);
            removedInstance.Enabled = false;
            await profileStore.SaveAsync(context.Profile, cancellationToken).ConfigureAwait(false);
        }

        string state = JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            environmentId = context.Profile.Environment.EnvironmentId,
            releaseVersion = context.Profile.Environment.ReleaseVersion,
            planId = context.Plan.PlanId,
            operation = context.Plan.Operation,
            controlProtocolVersion = context.ReleaseManifest?.ControlProtocolVersion ?? "1",
            databaseMigrationFingerprint = context.ReleaseManifest?.DatabaseMigrationFingerprint ?? string.Empty,
            databaseMigrationReviewedReleaseVersion = context.Profile.Environment.DatabaseMigrationReviewedReleaseVersion,
            previousReleaseVersion,
            expectedInstanceIds = context.Profile.Environment.Instances
                .Where(static instance => instance.Enabled)
                .Select(static instance => instance.InstanceId)
                .OrderBy(static instanceId => instanceId, StringComparer.Ordinal)
                .ToArray(),
            completedAtUtc = DateTimeOffset.UtcNow
        }, JsonOptions);
        string localPath = Path.Combine(paths.HistoryPath, context.Plan.PlanId + "-state.json");
        await File.WriteAllTextAsync(localPath, state, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
        for (int index = 0; index < context.Profile.Environment.Hosts.Count; index++)
        {
            HostDefinition host = context.Profile.Environment.Hosts[index];
            if (!HasSelectedArtifactForHost(host, context.Profile))
            {
                continue;
            }

            string remotePath = CombineRemote(host, host.DeploymentRoot, "state", "deployment-state.json");
            await remoteClient.UploadTextAsync(host, state, ToSftpPath(host, remotePath), cancellationToken).ConfigureAwait(false);
        }

        return "本地历史和远程 deployment-state.json 已更新。";
    }

    /// <summary>
    /// 调用目标实例当前版本中的 ServerCtl 工具。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="instance">目标实例。</param>
    /// <param name="releaseVersion">发布版本。</param>
    /// <param name="operation">管理操作。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>远程命令结果。</returns>
    private Task<RemoteCommandResult> ExecuteServerCtlAsync(
        HostDefinition host,
        InstanceDefinition instance,
        string releaseVersion,
        string operation,
        CancellationToken cancellationToken)
    {
        string target = host.OperatingSystem == HostOperatingSystem.Linux
            ? BuildTargetKind.ServerLinuxX64.ToString()
            : BuildTargetKind.ServerWindowsX64.ToString();
        string executableName = host.OperatingSystem == HostOperatingSystem.Linux ? "MiniCore.ServerCtl" : "MiniCore.ServerCtl.exe";
        string executable = CombineRemote(host, host.DeploymentRoot, "releases", releaseVersion, target, "Tools", "ServerCtl", executableName);
        string config = CombineRemote(host, host.DeploymentRoot, "instances", instance.InstanceId, "config", "MiniCoreServerRuntime.json");
        string command = RemoteCommandBuilder.ForHost(
            host,
            "chmod +x " + RemoteCommandBuilder.QuoteLinux(executable) + " && " + RemoteCommandBuilder.QuoteLinux(executable) + " --config " + RemoteCommandBuilder.QuoteLinux(config) + " " + RemoteCommandBuilder.QuoteLinux(operation),
            "& " + RemoteCommandBuilder.QuotePowerShell(executable) + " --config " + RemoteCommandBuilder.QuotePowerShell(config) + " " + RemoteCommandBuilder.QuotePowerShell(operation) + ";exit $LASTEXITCODE");
        return remoteClient.ExecuteAsync(host, command, cancellationToken);
    }

    /// <summary>
    /// 从实例 current 指针调用 ServerCtl，确保补偿回切后的健康检查使用实际活动版本。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="instance">目标实例。</param>
    /// <param name="operation">管理操作。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>远程命令结果。</returns>
    private Task<RemoteCommandResult> ExecuteCurrentServerCtlAsync(
        HostDefinition host,
        InstanceDefinition instance,
        string operation,
        CancellationToken cancellationToken)
    {
        string executableName = host.OperatingSystem == HostOperatingSystem.Linux ? "MiniCore.ServerCtl" : "MiniCore.ServerCtl.exe";
        string executable = CombineRemote(
            host,
            host.DeploymentRoot,
            "current",
            instance.InstanceId,
            "Tools",
            "ServerCtl",
            executableName);
        string config = CombineRemote(host, host.DeploymentRoot, "instances", instance.InstanceId, "config", "MiniCoreServerRuntime.json");
        string command = RemoteCommandBuilder.ForHost(
            host,
            "chmod +x " + RemoteCommandBuilder.QuoteLinux(executable) + " && " + RemoteCommandBuilder.QuoteLinux(executable) + " --config " + RemoteCommandBuilder.QuoteLinux(config) + " " + RemoteCommandBuilder.QuoteLinux(operation),
            "& " + RemoteCommandBuilder.QuotePowerShell(executable) + " --config " + RemoteCommandBuilder.QuotePowerShell(config) + " " + RemoteCommandBuilder.QuotePowerShell(operation) + ";exit $LASTEXITCODE");
        return remoteClient.ExecuteAsync(host, command, cancellationToken);
    }

    /// <summary>
    /// 加载已有发布清单，以支持扩容、修复和回滚不重新构建。
    /// </summary>
    /// <param name="context">发布上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>发布清单。</returns>
    private static async Task<ReleaseManifest> EnsureManifestAsync(DeploymentExecutionContext context, CancellationToken cancellationToken)
    {
        if (context.ReleaseManifest != null)
        {
            return context.ReleaseManifest;
        }

        string path = Path.Combine(context.Profile.Project.OutputPath, context.Profile.Environment.ReleaseVersion, "ReleaseManifest.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("当前操作需要已有 ReleaseManifest，但本地缓存不存在。", path);
        }

        await using FileStream stream = File.OpenRead(path);
        context.ReleaseManifest = await JsonSerializer.DeserializeAsync<ReleaseManifest>(stream, JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("ReleaseManifest 不是有效 JSON 对象。");
        return context.ReleaseManifest;
    }

    /// <summary>
    /// 在异常或取消后尽力清理一个确定的远程临时文件。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="remotePath">远程临时文件绝对路径。</param>
    /// <returns>清理完成任务。</returns>
    private async Task DeleteRemoteFileIfExistsAsync(HostDefinition host, string remotePath)
    {
        string command = RemoteCommandBuilder.ForHost(
            host,
            "rm -f " + RemoteCommandBuilder.QuoteLinux(remotePath),
            "$path=" + RemoteCommandBuilder.QuotePowerShell(remotePath) + ";if(Test-Path -LiteralPath $path){Remove-Item -Force -LiteralPath $path}");
        try
        {
            await remoteClient.ExecuteAsync(host, command, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // 原始上传或激活异常更重要；同路径重试会再次清理临时文件。
        }
    }

    /// <summary>
    /// 在暂存失败或取消后尽力删除本计划唯一命名的远程隔离目录。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="remotePath">本计划暂存目录绝对路径。</param>
    /// <returns>清理完成任务。</returns>
    private async Task DeleteRemoteDirectoryIfExistsAsync(HostDefinition host, string remotePath)
    {
        string command = RemoteCommandBuilder.ForHost(
            host,
            "if [ -d " + RemoteCommandBuilder.QuoteLinux(remotePath) + " ]; then rm -rf " + RemoteCommandBuilder.QuoteLinux(remotePath) + "; fi",
            "$path=" + RemoteCommandBuilder.QuotePowerShell(remotePath) + ";if(Test-Path -PathType Container -LiteralPath $path){Remove-Item -Recurse -Force -LiteralPath $path}");
        try
        {
            await remoteClient.ExecuteAsync(host, command, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // 原始暂存错误更重要；下一次同 planId 执行会先清理该隔离目录。
        }
    }

    /// <summary>
    /// 读取远程不可变版本清单；目录不存在时返回空，半成品目录则立即拒绝。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="remoteReleaseRoot">远程版本根目录。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已有清单或空。</returns>
    private async Task<ReleaseManifest?> ReadRemoteReleaseManifestAsync(
        HostDefinition host,
        string remoteReleaseRoot,
        CancellationToken cancellationToken)
    {
        string remoteManifestPath = CombineRemote(host, remoteReleaseRoot, "ReleaseManifest.json");
        string command = RemoteCommandBuilder.ForHost(
            host,
            "root=" + RemoteCommandBuilder.QuoteLinux(remoteReleaseRoot)
                + "; manifest=" + RemoteCommandBuilder.QuoteLinux(remoteManifestPath)
                + "; if [ ! -e \"$root\" ]; then printf %s '__MINICORE_RELEASE_ABSENT__'; elif [ ! -f \"$manifest\" ]; then printf %s '__MINICORE_RELEASE_INCOMPLETE__'; else cat \"$manifest\"; fi",
            "$root=" + RemoteCommandBuilder.QuotePowerShell(remoteReleaseRoot)
                + ";$manifest=" + RemoteCommandBuilder.QuotePowerShell(remoteManifestPath)
                + ";if(-not(Test-Path -LiteralPath $root)){Write-Output '__MINICORE_RELEASE_ABSENT__'}elseif(-not(Test-Path -LiteralPath $manifest)){Write-Output '__MINICORE_RELEASE_INCOMPLETE__'}else{Get-Content -Raw -LiteralPath $manifest}");
        RemoteCommandResult result = await remoteClient.ExecuteAsync(host, command, cancellationToken).ConfigureAwait(false);
        EnsureRemoteSuccess(host, result, "读取远程不可变版本清单");
        string output = result.StandardOutput.Trim();
        if (string.Equals(output, "__MINICORE_RELEASE_ABSENT__", StringComparison.Ordinal))
        {
            return null;
        }

        if (string.Equals(output, "__MINICORE_RELEASE_INCOMPLETE__", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"主机 {host.HostId} 的版本目录 {remoteReleaseRoot} 已存在但缺少 ReleaseManifest，禁止覆盖半包。");
        }

        try
        {
            return JsonSerializer.Deserialize<ReleaseManifest>(output, JsonOptions)
                ?? throw new InvalidDataException("远程 ReleaseManifest 不是有效 JSON 对象。");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"主机 {host.HostId} 的远程 ReleaseManifest 无法解析，禁止覆盖。", exception);
        }
    }

    /// <summary>
    /// 按清单压缩大小、解压工作区、旧版保留量和安全余量校验远程可用空间。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="requiredBytes">本轮动态预算字节数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>空间校验完成任务。</returns>
    private async Task EnsureRemoteDiskCapacityAsync(
        HostDefinition host,
        long requiredBytes,
        CancellationToken cancellationToken)
    {
        string requiredText = requiredBytes.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string command = RemoteCommandBuilder.ForHost(
            host,
            "root=" + RemoteCommandBuilder.QuoteLinux(host.DeploymentRoot)
                + "; available_kib=$(df -Pk \"$root\" | awk 'NR==2 {print $4}'); available_bytes=$((available_kib * 1024)); if [ \"$available_bytes\" -lt "
                + requiredText
                + " ]; then echo \"requiredBytes=" + requiredText + ", availableBytes=$available_bytes\" >&2; exit 28; fi; echo \"availableBytes=$available_bytes\"",
            "$root=" + RemoteCommandBuilder.QuotePowerShell(host.DeploymentRoot)
                + ";$available=(Get-Item -LiteralPath $root).PSDrive.Free;$required=[Int64]"
                + requiredText
                + ";if($available -lt $required){throw ('requiredBytes='+$required+', availableBytes='+$available)};Write-Output ('availableBytes='+$available)");
        RemoteCommandResult result = await remoteClient.ExecuteAsync(host, command, cancellationToken).ConfigureAwait(false);
        EnsureRemoteSuccess(host, result, $"动态磁盘预检（需要 {requiredBytes} 字节）");
    }

    /// <summary>
    /// 复用已有远程版本前确认当前主机需要的目标目录都已随原子提交存在。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="remoteReleaseRoot">远程不可变版本目录。</param>
    /// <param name="artifacts">当前主机需要的制品。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>目录校验完成任务。</returns>
    private async Task EnsureRemoteReleaseTargetsAsync(
        HostDefinition host,
        string remoteReleaseRoot,
        IReadOnlyList<ReleaseArtifact> artifacts,
        CancellationToken cancellationToken)
    {
        var linux = new StringBuilder("set -eu;");
        var windows = new StringBuilder();
        for (int index = 0; index < artifacts.Count; index++)
        {
            string targetPath = CombineRemote(host, remoteReleaseRoot, artifacts[index].Target.ToString());
            linux.Append(" test -d ").Append(RemoteCommandBuilder.QuoteLinux(targetPath)).Append(';');
            windows.Append("if(-not(Test-Path -PathType Container -LiteralPath ")
                .Append(RemoteCommandBuilder.QuotePowerShell(targetPath))
                .Append(")){throw 'immutable release target missing'};");
        }

        RemoteCommandResult result = await remoteClient.ExecuteAsync(
            host,
            RemoteCommandBuilder.ForHost(host, linux.ToString(), windows.ToString()),
            cancellationToken).ConfigureAwait(false);
        EnsureRemoteSuccess(host, result, "校验已有不可变 Release 的目标完整性");
    }

    /// <summary>
    /// 判断一个制品是否需要发送到目标主机。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="profile">发布配置。</param>
    /// <param name="target">制品目标。</param>
    /// <returns>目标主机需要该制品时返回 true。</returns>
    private static bool ShouldStageArtifact(HostDefinition host, DeploymentProfile profile, BuildTargetKind target)
    {
        if (!profile.Project.PublishTargets.Contains(target))
        {
            return false;
        }

        if (target == BuildTargetKind.AuthenticationServer)
        {
            return HasHostedComponent(host, profile, ComponentKind.AuthenticationServer);
        }

        if (target == BuildTargetKind.DatabaseServer)
        {
            return HasHostedComponent(host, profile, ComponentKind.DatabaseServer);
        }

        if (target == BuildTargetKind.ClientWebGL)
        {
            return HasHostedComponent(host, profile, ComponentKind.StaticContent);
        }

        bool hostsDedicatedServer = HasHostedComponent(host, profile, ComponentKind.Coordinator)
            || HasHostedComponent(host, profile, ComponentKind.DedicatedServer);
        return hostsDedicatedServer
            && (host.OperatingSystem == HostOperatingSystem.Linux
                ? target == BuildTargetKind.ServerLinuxX64
                : target == BuildTargetKind.ServerWindowsX64);
    }

    /// <summary>
    /// 校验客户端发布目标存在于完整发布清单，并返回其可交付路径。
    /// </summary>
    /// <param name="step">包含客户端目标名称的计划步骤。</param>
    /// <param name="context">发布上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>客户端制品路径摘要。</returns>
    private static async Task<string> PublishClientArtifactAsync(
        DeploymentStep step,
        DeploymentExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse(step.InstanceId, out BuildTargetKind target))
        {
            throw new InvalidDataException($"客户端发布步骤缺少有效目标：{step.InstanceId}。");
        }

        ReleaseManifest manifest = await EnsureManifestAsync(context, cancellationToken).ConfigureAwait(false);
        for (int index = 0; index < manifest.Artifacts.Count; index++)
        {
            ReleaseArtifact artifact = manifest.Artifacts[index];
            if (artifact.Target != target)
            {
                continue;
            }

            string path = Path.GetFullPath(Path.Combine(
                context.Profile.Project.OutputPath,
                manifest.ReleaseVersion,
                artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"客户端制品文件不存在：{target}。", path);
            }

            return $"{target} 客户端制品已就绪：{path}";
        }

        throw new InvalidDataException($"ReleaseManifest 不包含客户端目标 {target}。");
    }

    /// <summary>
    /// 判断目标主机是否需要接收当前发布范围内的任一制品。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="profile">发布配置。</param>
    /// <returns>主机参与当前发布时返回 true。</returns>
    private static bool HasSelectedArtifactForHost(HostDefinition host, DeploymentProfile profile)
    {
        for (int index = 0; index < profile.Project.PublishTargets.Count; index++)
        {
            if (ShouldStageArtifact(host, profile, profile.Project.PublishTargets[index]))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断主机是否承载指定启用组件。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="profile">发布配置。</param>
    /// <param name="component">组件种类。</param>
    /// <returns>存在匹配实例时返回 true。</returns>
    private static bool HasHostedComponent(HostDefinition host, DeploymentProfile profile, ComponentKind component)
    {
        for (int index = 0; index < profile.Environment.Instances.Count; index++)
        {
            InstanceDefinition instance = profile.Environment.Instances[index];
            if (instance.Enabled
                && instance.Component == component
                && string.Equals(instance.HostId, host.HostId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 返回目标实例在不可变版本目录中的组件路径。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="instance">目标实例。</param>
    /// <param name="releaseVersion">发布版本。</param>
    /// <returns>组件目录。</returns>
    private static string GetReleaseComponentPath(HostDefinition host, InstanceDefinition instance, string releaseVersion)
    {
        string target = instance.Component switch
        {
            ComponentKind.AuthenticationServer => BuildTargetKind.AuthenticationServer.ToString(),
            ComponentKind.DatabaseServer => BuildTargetKind.DatabaseServer.ToString(),
            ComponentKind.StaticContent => BuildTargetKind.ClientWebGL.ToString(),
            _ => host.OperatingSystem == HostOperatingSystem.Linux
                ? BuildTargetKind.ServerLinuxX64.ToString()
                : BuildTargetKind.ServerWindowsX64.ToString()
        };
        string result = CombineRemote(host, host.DeploymentRoot, "releases", releaseVersion, target);
        if (instance.Component is ComponentKind.AuthenticationServer or ComponentKind.DatabaseServer)
        {
            result = CombineRemote(host, result, host.OperatingSystem == HostOperatingSystem.Linux ? "linux-x64" : "win-x64");
        }

        return result;
    }

    /// <summary>
    /// 返回本计划为单个实例保存补偿元数据和旧配置的远程目录。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="context">发布上下文。</param>
    /// <param name="instance">目标实例。</param>
    /// <returns>计划级回滚目录。</returns>
    private static string GetRollbackDirectory(
        HostDefinition host,
        DeploymentExecutionContext context,
        InstanceDefinition instance)
    {
        return CombineRemote(
            host,
            host.DeploymentRoot,
            "state",
            "rollback",
            context.Plan.PlanId,
            instance.InstanceId);
    }

    /// <summary>
    /// 返回服务定义使用的当前版本可执行程序路径。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="instance">目标实例。</param>
    /// <returns>可执行程序路径。</returns>
    private static string GetCurrentExecutablePath(HostDefinition host, InstanceDefinition instance)
    {
        string root = CombineRemote(host, host.DeploymentRoot, "current", instance.InstanceId);
        bool windows = host.OperatingSystem == HostOperatingSystem.Windows;
        string fileName = instance.Component switch
        {
            ComponentKind.AuthenticationServer => windows ? "AuthenticationServer.exe" : "AuthenticationServer",
            ComponentKind.DatabaseServer => windows ? "DatabaseServer.exe" : "DatabaseServer",
            _ => windows ? "MiniCoreServer.exe" : "MiniCoreServer.x86_64"
        };
        return CombineRemote(host, root, fileName);
    }

    /// <summary>
    /// 创建 systemd 服务定义，实例配置始终通过明确启动参数传入。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="instance">目标实例。</param>
    /// <param name="executable">当前版本可执行程序。</param>
    /// <param name="configPath">外部实例配置。</param>
    /// <returns>unit 文本。</returns>
    private static string BuildSystemdUnit(
        HostDefinition host,
        InstanceDefinition instance,
        string executable,
        string configPath)
    {
        string[] arguments = BuildServiceArguments(instance, configPath);
        var argumentBuilder = new StringBuilder();
        for (int index = 0; index < arguments.Length; index++)
        {
            argumentBuilder.Append(' ').Append(RemoteCommandBuilder.QuoteLinux(arguments[index]));
        }

        string restart = instance.AutoRestart ? "on-failure" : "no";
        return "[Unit]\nDescription=MiniCore " + instance.InstanceId + "\nAfter=network-online.target\nWants=network-online.target\n\n[Service]\nType=simple\nUser=" + host.UserName + "\nExecStart=" + executable + argumentBuilder + "\nWorkingDirectory=" + GetParentRemotePath(executable, '/') + "\nRestart=" + restart + "\nRestartSec=3\nTimeoutStopSec=100\nKillSignal=SIGTERM\n\n[Install]\nWantedBy=multi-user.target\n";
    }

    /// <summary>
    /// 返回不同组件的逐项进程参数。
    /// </summary>
    /// <param name="instance">实例配置。</param>
    /// <param name="configPath">实例外部配置路径。</param>
    /// <returns>无需 Shell 二次解析的参数数组。</returns>
    private static string[] BuildServiceArguments(InstanceDefinition instance, string configPath)
    {
        if (IsDedicatedServer(instance))
        {
            return new[] { "--minicore-config", configPath };
        }

        if (instance.Component is ComponentKind.AuthenticationServer or ComponentKind.DatabaseServer)
        {
            char separator = configPath.IndexOf('\\') >= 0 ? '\\' : '/';
            return new[] { "--contentRoot", GetParentRemotePath(configPath, separator) };
        }

        return Array.Empty<string>();
    }

    /// <summary>
    /// 返回当前组件使用的外部配置文件路径。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="instance">目标实例。</param>
    /// <returns>DS 配置或 .NET appsettings 的服务器绝对路径。</returns>
    private static string GetInstanceConfigurationPath(HostDefinition host, InstanceDefinition instance)
    {
        string fileName = instance.Component is ComponentKind.AuthenticationServer or ComponentKind.DatabaseServer
            ? "appsettings.json"
            : "MiniCoreServerRuntime.json";
        return CombineRemote(host, host.DeploymentRoot, "instances", instance.InstanceId, "config", fileName);
    }

    /// <summary>
    /// 创建只包含字母、数字、短横线和点的服务名。
    /// </summary>
    /// <param name="instance">实例配置。</param>
    /// <returns>服务名。</returns>
    private static string GetServiceName(InstanceDefinition instance)
    {
        return ServiceNameFormatter.Format(instance.InstanceId);
    }

    /// <summary>
    /// 判断组件是否由 Unity Dedicated Server 宿主管理。
    /// </summary>
    /// <param name="instance">实例。</param>
    /// <returns>Coordinator 或普通 DS 时返回 true。</returns>
    private static bool IsDedicatedServer(InstanceDefinition instance)
    {
        return instance.Component is ComponentKind.Coordinator or ComponentKind.DedicatedServer;
    }

    /// <summary>
    /// 判断环境是否启用了指定可选组件。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="kind">组件种类。</param>
    /// <returns>至少一个启用实例存在时返回 true。</returns>
    private static bool HasComponent(DeploymentProfile profile, ComponentKind kind)
    {
        for (int index = 0; index < profile.Environment.Instances.Count; index++)
        {
            InstanceDefinition instance = profile.Environment.Instances[index];
            if (instance.Enabled && instance.Component == kind)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 查找 Coordinator 实例。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <returns>找到的 Coordinator；不存在时返回 null。</returns>
    private static InstanceDefinition? FindCoordinator(DeploymentProfile profile)
    {
        for (int index = 0; index < profile.Environment.Instances.Count; index++)
        {
            InstanceDefinition instance = profile.Environment.Instances[index];
            if (instance.Enabled && instance.Component == ComponentKind.Coordinator)
            {
                return instance;
            }
        }

        return null;
    }

    /// <summary>
    /// 按标识查找主机。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="hostId">主机标识。</param>
    /// <returns>主机配置。</returns>
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

        throw new InvalidOperationException($"找不到目标主机：{hostId}。");
    }

    /// <summary>
    /// 按标识查找实例。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="instanceId">实例标识。</param>
    /// <returns>实例配置。</returns>
    private static InstanceDefinition FindInstance(DeploymentProfile profile, string instanceId)
    {
        for (int index = 0; index < profile.Environment.Instances.Count; index++)
        {
            InstanceDefinition instance = profile.Environment.Instances[index];
            if (string.Equals(instance.InstanceId, instanceId, StringComparison.Ordinal))
            {
                return instance;
            }
        }

        throw new InvalidOperationException($"找不到目标实例：{instanceId}。");
    }

    /// <summary>
    /// 将 Role 字符串复制为 JsonNode 数组。
    /// </summary>
    /// <param name="roles">Role 列表。</param>
    /// <returns>JSON 节点数组。</returns>
    private static JsonNode?[] CreateRoleNodes(IReadOnlyList<string> roles)
    {
        var result = new JsonNode?[roles.Count];
        for (int index = 0; index < roles.Count; index++)
        {
            result[index] = JsonValue.Create(roles[index]);
        }

        return result;
    }

    /// <summary>
    /// 组合目标系统使用的远程路径。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="parts">路径片段。</param>
    /// <returns>远程路径。</returns>
    private static string CombineRemote(HostDefinition host, params string[] parts)
    {
        char separator = host.OperatingSystem == HostOperatingSystem.Linux ? '/' : '\\';
        string result = parts[0].TrimEnd('/', '\\');
        for (int index = 1; index < parts.Length; index++)
        {
            result += separator + parts[index].Trim('/', '\\');
        }

        return result;
    }

    /// <summary>
    /// 将 Windows 本机样式路径转换为 OpenSSH SFTP 路径。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="path">远程系统路径。</param>
    /// <returns>SFTP 路径。</returns>
    private static string ToSftpPath(HostDefinition host, string path)
    {
        string normalized = path.Replace('\\', '/');
        if (host.OperatingSystem == HostOperatingSystem.Windows && normalized.Length >= 2 && normalized[1] == ':')
        {
            return "/" + normalized;
        }

        return normalized;
    }

    /// <summary>
    /// 取得远程路径的父目录。
    /// </summary>
    /// <param name="path">远程路径。</param>
    /// <param name="separator">目标系统分隔符。</param>
    /// <returns>父目录。</returns>
    private static string GetParentRemotePath(string path, char separator)
    {
        int index = path.LastIndexOf(separator);
        return index <= 0 ? path : path[..index];
    }

    /// <summary>
    /// 检查远程命令退出码并抛出不包含凭据的异常。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="result">命令结果。</param>
    /// <param name="operation">操作名称。</param>
    private static void EnsureRemoteSuccess(HostDefinition host, RemoteCommandResult result, string operation)
    {
        if (result.ExitCode == 0)
        {
            return;
        }

        string detail = string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError;
        string safeDetail = SensitiveDataRedactor.Redact(detail).Trim();
        if (safeDetail.Length == 0)
        {
            safeDetail = "远程命令未返回标准输出或错误输出。";
        }

        throw new InvalidOperationException($"主机 {host.HostId} 的{operation}失败，退出码 {result.ExitCode}：{safeDetail}");
    }

    /// <summary>
    /// 根据异常类型生成稳定错误码。
    /// </summary>
    /// <param name="exception">异常。</param>
    /// <returns>错误码。</returns>
    private static string GetErrorCode(Exception exception)
    {
        return exception switch
        {
            DeploymentFailureException deploymentFailure => deploymentFailure.ErrorCode,
            FileNotFoundException => "FILE_NOT_FOUND",
            DirectoryNotFoundException => "DIRECTORY_NOT_FOUND",
            TimeoutException => "TIMEOUT",
            UnauthorizedAccessException => "ACCESS_DENIED",
            _ => "DEPLOYMENT_STEP_FAILED"
        };
    }

    /// <summary>
    /// 根据失败步骤给出不自动执行破坏性操作的恢复建议。
    /// </summary>
    /// <param name="action">失败动作。</param>
    /// <param name="exception">导致步骤失败的异常。</param>
    /// <returns>恢复建议。</returns>
    private static string GetRecoverySuggestion(DeploymentAction action, Exception exception)
    {
        if (exception is DeploymentFailureException deploymentFailure)
        {
            return deploymentFailure.RecoverySuggestion;
        }

        return action switch
        {
            DeploymentAction.Preflight => "修正路径、源码状态、SSH 指纹、权限或主机依赖后重新生成计划。",
            DeploymentAction.Build => "查看构建日志，修复编译错误后从构建步骤继续。",
            DeploymentAction.StageArtifact => "检查磁盘、SSH/SFTP 和哈希后安全重试暂存步骤。",
            DeploymentAction.WaitForDrain => "查看业务阻塞项并由运维人员决定继续等待或取消。",
            DeploymentAction.ActivateRelease => "检查远程版本目录和 current 指针，不要手工覆盖运行中文件。",
            DeploymentAction.WaitForHealth => "查看实例日志、外部配置和 Coordinator 注册状态后恢复。",
            _ => "修正报告中的原因后从发布历史继续，已成功步骤不会重复执行。"
        };
    }

    /// <summary>
    /// 创建统一步骤结果。
    /// </summary>
    /// <param name="step">步骤。</param>
    /// <param name="status">状态。</param>
    /// <param name="startedAt">开始时间。</param>
    /// <param name="errorCode">错误码。</param>
    /// <param name="message">结果说明。</param>
    /// <param name="recoverySuggestion">恢复建议。</param>
    /// <returns>结构化结果。</returns>
    private StepResult CreateResult(
        DeploymentStep step,
        StepStatus status,
        DateTimeOffset startedAt,
        string errorCode,
        string message,
        string recoverySuggestion)
    {
        return new StepResult
        {
            StepId = step.StepId,
            DisplayName = step.DisplayName,
            HostId = step.HostId,
            Status = status,
            StartedAtUtc = startedAt,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            ErrorCode = SensitiveDataRedactor.Redact(errorCode),
            Message = SensitiveDataRedactor.Redact(message),
            RecoverySuggestion = SensitiveDataRedactor.Redact(recoverySuggestion),
            PreviousReleaseVersion = previousReleaseVersion
        };
    }

    /// <summary>
    /// 从本地已完成发布状态中读取指定环境最近一次稳定程序版本。
    /// </summary>
    /// <param name="environmentId">目标环境标识。</param>
    /// <returns>最近稳定版本；没有历史时返回空文本。</returns>
    private string LoadPreviousReleaseVersion(string environmentId)
    {
        string[] stateFiles = Directory.GetFiles(paths.HistoryPath, "*-state.json", SearchOption.TopDirectoryOnly);
        Array.Sort(
            stateFiles,
            static (left, right) => File.GetLastWriteTimeUtc(right).CompareTo(File.GetLastWriteTimeUtc(left)));
        for (int index = 0; index < stateFiles.Length; index++)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(stateFiles[index]));
                JsonElement root = document.RootElement;
                string storedEnvironmentId = root.TryGetProperty("environmentId", out JsonElement environmentElement)
                    ? environmentElement.GetString() ?? string.Empty
                    : string.Empty;
                if (!string.Equals(storedEnvironmentId, environmentId, StringComparison.Ordinal))
                {
                    continue;
                }

                return root.TryGetProperty("releaseVersion", out JsonElement releaseElement)
                    ? releaseElement.GetString() ?? string.Empty
                    : string.Empty;
            }
            catch (JsonException)
            {
                // 忽略不完整的旧历史文件，继续寻找上一份完整状态。
            }
            catch (IOException)
            {
                // 历史可能被另一个只读查看进程短暂占用，继续尝试更早记录。
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// 创建字符串枚举 JSON 设置。
    /// </summary>
    /// <returns>JSON 设置。</returns>
    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    #endregion
}
