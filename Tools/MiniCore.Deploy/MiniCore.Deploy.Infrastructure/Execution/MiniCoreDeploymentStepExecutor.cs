using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
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
    private SourceFingerprint? sourceFingerprint; // 当前执行捕获的源码指纹。

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
    public MiniCoreDeploymentStepExecutor(
        UnityBatchBuildService unityBuildService,
        DotNetComponentPublisher dotNetPublisher,
        GitSourceInspector sourceInspector,
        ReleasePackager releasePackager,
        SshRemoteClient remoteClient,
        ApplicationPaths paths)
    {
        this.unityBuildService = unityBuildService ?? throw new ArgumentNullException(nameof(unityBuildService));
        this.dotNetPublisher = dotNetPublisher ?? throw new ArgumentNullException(nameof(dotNetPublisher));
        this.sourceInspector = sourceInspector ?? throw new ArgumentNullException(nameof(sourceInspector));
        this.releasePackager = releasePackager ?? throw new ArgumentNullException(nameof(releasePackager));
        this.remoteClient = remoteClient ?? throw new ArgumentNullException(nameof(remoteClient));
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
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
            return CreateResult(step, StepStatus.Failed, startedAt, GetErrorCode(exception), exception.Message, GetRecoverySuggestion(step.Action));
        }
    }

    #endregion

    #region Private 私有成员

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
            string command = RemoteCommandBuilder.ForHost(
                host,
                "set -eu; test \"$(uname -s)\" = Linux; case \"$(uname -m)\" in x86_64|amd64) ;; *) echo 'unsupported architecture' >&2; exit 21;; esac; command -v sha256sum >/dev/null; command -v unzip >/dev/null; command -v systemctl >/dev/null; command -v ss >/dev/null; sudo -n true; root=" + RemoteCommandBuilder.QuoteLinux(host.DeploymentRoot) + "; if [ ! -d \"$root\" ]; then sudo -n install -d -o \"$(id -un)\" -g \"$(id -gn)\" \"$root\"; fi; test -w \"$root\"; available=$(df -Pk \"$root\" | awk 'NR==2 {print $4}'); test \"${available:-0}\" -ge 524288; " + portCheckLinux + " echo \"Linux x64, availableKiB=$available\"",
                "$root=" + RemoteCommandBuilder.QuotePowerShell(host.DeploymentRoot) + ";if(-not [Environment]::Is64BitOperatingSystem){throw 'unsupported architecture'};$principal=New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent());if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'administrator privileges required'};if(-not(Test-Path -LiteralPath $root)){New-Item -ItemType Directory -Force -Path $root|Out-Null};$probe=Join-Path $root '.minicore-write-probe';[IO.File]::WriteAllText($probe,'ok');Remove-Item -LiteralPath $probe -Force;$drive=(Get-Item -LiteralPath $root).PSDrive;$available=$drive.Free;if($available -lt 536870912){throw 'insufficient disk space'};Get-Command Get-FileHash,Expand-Archive,sc.exe|Out-Null;" + portCheckWindows + "Write-Output ('Windows x64, availableBytes='+$available)");
            RemoteCommandResult result = await remoteClient.ExecuteAsync(host, command, cancellationToken).ConfigureAwait(false);
            EnsureRemoteSuccess(host, result, "主机预检");
            capacity.Add(host.HostId + ": " + result.StandardOutput.Trim());
        }

        await ValidateRollingProtocolAsync(profile, cancellationToken).ConfigureAwait(false);

        return "源码、输出目录、SSH 指纹、x64 架构、磁盘、权限、端口和主机依赖预检通过。" + (capacity.Count == 0 ? string.Empty : " " + string.Join("；", capacity));
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
        IReadOnlyList<int> ports = GetPreflightPorts(profile, host.HostId);
        if (ports.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder("for port in");
        for (int index = 0; index < ports.Count; index++)
        {
            builder.Append(' ').Append(ports[index]);
        }

        builder.Append("; do if ss -ltnH | awk '{value=$4; sub(/^.*:/,\"\",value); print value}' | grep -Fqx \"$port\"; then echo \"port in use: $port\" >&2; exit 22; fi; done;");
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
        IReadOnlyList<int> ports = GetPreflightPorts(profile, host.HostId);
        if (ports.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder("$ports=@(");
        for (int index = 0; index < ports.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append(ports[index]);
        }

        builder.Append(");foreach($port in $ports){if(Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue){throw ('port in use: '+$port)}};");
        return builder.ToString();
    }

    /// <summary>
    /// 返回当前操作必须保持空闲的目标主机端口。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="hostId">主机标识。</param>
    /// <returns>去重后的端口。</returns>
    private static IReadOnlyList<int> GetPreflightPorts(DeploymentProfile profile, string hostId)
    {
        var ports = new List<int>();
        if (profile.Operation is not (DeploymentOperation.FirstInstall or DeploymentOperation.ScaleOut))
        {
            return ports;
        }

        for (int index = 0; index < profile.Environment.Instances.Count; index++)
        {
            InstanceDefinition instance = profile.Environment.Instances[index];
            if (!instance.Enabled
                || instance.Component == ComponentKind.StaticContent
                || !string.Equals(instance.HostId, hostId, StringComparison.Ordinal)
                || (profile.Operation == DeploymentOperation.ScaleOut
                    && !string.Equals(instance.InstanceId, profile.TargetInstanceId, StringComparison.Ordinal)))
            {
                continue;
            }

            AddUniquePort(ports, instance.InnerPort);
            if (instance.Component is ComponentKind.Coordinator or ComponentKind.DedicatedServer)
            {
                AddUniquePort(ports, instance.OuterPort);
                AddUniquePort(ports, instance.ManagementPort);
            }
        }

        return ports;
    }

    /// <summary>
    /// 在端口有效且未出现时加入列表。
    /// </summary>
    /// <param name="ports">目标列表。</param>
    /// <param name="port">候选端口。</param>
    private static void AddUniquePort(ICollection<int> ports, int port)
    {
        if (port > 0 && !ports.Contains(port))
        {
            ports.Add(port);
        }
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

        UnityBuildResponse unity = await unityBuildService.BuildAsync(context.Profile, cancellationToken).ConfigureAwait(false);
        string postGenerationLogDirectory = Path.Combine(paths.LogsPath, context.Plan.PlanId, "post-generation");
        Directory.CreateDirectory(postGenerationLogDirectory);
        SourceFingerprint postGenerationFingerprint = await sourceInspector.CaptureAsync(context.Profile.Project.ProjectPath, postGenerationLogDirectory, cancellationToken).ConfigureAwait(false);
        if (context.Profile.Environment.RequireCleanGitWorkspace && !postGenerationFingerprint.IsClean)
        {
            throw new InvalidOperationException("生产构建的代码生成结果与仓库不一致。请审查并提交生成文件后重新发布。");
        }

        sourceFingerprint = postGenerationFingerprint;
        await dotNetPublisher.PublishAsync(context.Profile, cancellationToken).ConfigureAwait(false);
        context.ReleaseManifest = await releasePackager.CreateManifestAsync(context.Profile, sourceFingerprint.ToString(), cancellationToken).ConfigureAwait(false);
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
        ReleaseManifest manifest = await EnsureManifestAsync(context, cancellationToken).ConfigureAwait(false);
        string releaseRoot = Path.GetFullPath(Path.Combine(context.Profile.Project.OutputPath, manifest.ReleaseVersion));
        string remoteReleaseRoot = CombineRemote(host, host.DeploymentRoot, "releases", manifest.ReleaseVersion);
        int uploaded = 0;
        for (int index = 0; index < manifest.Artifacts.Count; index++)
        {
            ReleaseArtifact artifact = manifest.Artifacts[index];
            if (!ShouldStageArtifact(host, context.Profile, artifact.Target))
            {
                continue;
            }

            string localPath = Path.Combine(releaseRoot, artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            string remoteArchivePath = CombineRemote(host, remoteReleaseRoot, "archives", Path.GetFileName(artifact.RelativePath));
            await remoteClient.UploadFileAsync(host, localPath, ToSftpPath(host, remoteArchivePath), cancellationToken).ConfigureAwait(false);
            string targetDirectory = CombineRemote(host, remoteReleaseRoot, artifact.Target.ToString());
            string verifyAndExtract = RemoteCommandBuilder.ForHost(
                host,
                "test \"$(sha256sum " + RemoteCommandBuilder.QuoteLinux(remoteArchivePath) + " | cut -d' ' -f1)\" = " + RemoteCommandBuilder.QuoteLinux(artifact.Sha256) + " && mkdir -p " + RemoteCommandBuilder.QuoteLinux(targetDirectory) + " && unzip -oq " + RemoteCommandBuilder.QuoteLinux(remoteArchivePath) + " -d " + RemoteCommandBuilder.QuoteLinux(targetDirectory),
                "$archive=" + RemoteCommandBuilder.QuotePowerShell(remoteArchivePath) + ";$target=" + RemoteCommandBuilder.QuotePowerShell(targetDirectory) + ";$actual=(Get-FileHash -Algorithm SHA256 -LiteralPath $archive).Hash.ToLowerInvariant();if($actual -ne " + RemoteCommandBuilder.QuotePowerShell(artifact.Sha256) + "){throw 'SHA256 mismatch'};New-Item -ItemType Directory -Force -Path $target|Out-Null;Expand-Archive -LiteralPath $archive -DestinationPath $target -Force");
            EnsureRemoteSuccess(host, await remoteClient.ExecuteAsync(host, verifyAndExtract, cancellationToken).ConfigureAwait(false), $"校验并展开 {artifact.Target}");
            uploaded++;
        }

        string manifestPath = Path.Combine(releaseRoot, "ReleaseManifest.json");
        await remoteClient.UploadFileAsync(host, manifestPath, ToSftpPath(host, CombineRemote(host, remoteReleaseRoot, "ReleaseManifest.json")), cancellationToken).ConfigureAwait(false);
        return $"已在 {host.HostId} 暂存并校验 {uploaded} 个制品。";
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

        if (instance.Component is ComponentKind.AuthenticationServer or ComponentKind.DatabaseServer)
        {
            JsonObject serviceDocument = BuildDotNetServiceConfiguration(context.Profile, instance);
            string serviceJson = serviceDocument.ToJsonString(JsonOptions);
            string remoteServiceConfig = CombineRemote(host, configDirectory, "appsettings.json");
            await remoteClient.UploadTextAsync(host, serviceJson, ToSftpPath(host, remoteServiceConfig + ".tmp"), cancellationToken).ConfigureAwait(false);
            string activateServiceConfig = RemoteCommandBuilder.ForHost(
                host,
                "mkdir -p " + RemoteCommandBuilder.QuoteLinux(configDirectory) + " " + RemoteCommandBuilder.QuoteLinux(logDirectory) + " && mv -f " + RemoteCommandBuilder.QuoteLinux(remoteServiceConfig + ".tmp") + " " + RemoteCommandBuilder.QuoteLinux(remoteServiceConfig) + " && chmod 600 " + RemoteCommandBuilder.QuoteLinux(remoteServiceConfig),
                "$configDir=" + RemoteCommandBuilder.QuotePowerShell(configDirectory) + ";$logDir=" + RemoteCommandBuilder.QuotePowerShell(logDirectory) + ";New-Item -ItemType Directory -Force -Path $configDir,$logDir|Out-Null;Move-Item -Force -LiteralPath " + RemoteCommandBuilder.QuotePowerShell(remoteServiceConfig + ".tmp") + " -Destination " + RemoteCommandBuilder.QuotePowerShell(remoteServiceConfig));
            EnsureRemoteSuccess(host, await remoteClient.ExecuteAsync(host, activateServiceConfig, cancellationToken).ConfigureAwait(false), "激活 .NET 服务配置");
            string databaseUsage = instance.Component == ComponentKind.AuthenticationServer ? "账号库" : "游戏库";
            return $"已为 {instance.InstanceId} 写入监听、Coordinator 和{databaseUsage}参数；密码未进入本地方案或日志。";
        }

        string tokenPath = CombineRemote(host, configDirectory, "management.token");
        var document = new JsonObject
        {
            ["environmentId"] = context.Profile.Environment.EnvironmentId,
            ["instanceId"] = instance.InstanceId,
            ["releaseVersion"] = context.Profile.Environment.ReleaseVersion,
            ["controlProtocolVersion"] = "1",
            ["roles"] = new JsonArray(CreateRoleNodes(instance.Roles)),
            ["coordinator"] = new JsonObject
            {
                ["innerHost"] = coordinator?.InnerAdvertisedHost ?? instance.InnerAdvertisedHost,
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
                ["innerHost"] = instance.InnerAdvertisedHost,
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
            ["persistenceMode"] = instance.RequiresDatabase ? "Database" : "None",
            ["configVersion"] = context.Profile.Environment.ReleaseVersion
        };
        string canonical = document.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        document["configSha256"] = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        string json = document.ToJsonString(JsonOptions);
        string remoteConfig = CombineRemote(host, configDirectory, "MiniCoreServerRuntime.json");
        await remoteClient.UploadTextAsync(host, json, ToSftpPath(host, remoteConfig + ".tmp"), cancellationToken).ConfigureAwait(false);

        string finalize = RemoteCommandBuilder.ForHost(
            host,
            "mkdir -p " + RemoteCommandBuilder.QuoteLinux(configDirectory) + " " + RemoteCommandBuilder.QuoteLinux(logDirectory) + " && mv -f " + RemoteCommandBuilder.QuoteLinux(remoteConfig + ".tmp") + " " + RemoteCommandBuilder.QuoteLinux(remoteConfig) + " && if [ ! -f " + RemoteCommandBuilder.QuoteLinux(tokenPath) + " ]; then umask 077; head -c 32 /dev/urandom | base64 > " + RemoteCommandBuilder.QuoteLinux(tokenPath) + "; fi && chmod 600 " + RemoteCommandBuilder.QuoteLinux(tokenPath),
            "$configDir=" + RemoteCommandBuilder.QuotePowerShell(configDirectory) + ";$logDir=" + RemoteCommandBuilder.QuotePowerShell(logDirectory) + ";New-Item -ItemType Directory -Force -Path $configDir,$logDir|Out-Null;Move-Item -Force -LiteralPath " + RemoteCommandBuilder.QuotePowerShell(remoteConfig + ".tmp") + " -Destination " + RemoteCommandBuilder.QuotePowerShell(remoteConfig) + ";$token=" + RemoteCommandBuilder.QuotePowerShell(tokenPath) + ";if(-not(Test-Path -LiteralPath $token)){[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))|Set-Content -NoNewline -Encoding ascii -LiteralPath $token}");
        EnsureRemoteSuccess(host, await remoteClient.ExecuteAsync(host, finalize, cancellationToken).ConfigureAwait(false), "激活实例配置");
        return $"已写入实例 {instance.InstanceId} 的外部配置和 SHA-256。";
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
                    ["CoordinatorWebSocketUrl"] = coordinator.OuterAdvertisedUrl
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
                    ["AdvertisedHost"] = instance.InnerAdvertisedHost,
                    ["CoordinatorHost"] = coordinator.InnerAdvertisedHost,
                    ["CoordinatorPort"] = coordinator.InnerPort,
                    ["MaximumConcurrency"] = instance.MaximumConcurrency
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
            string command = "sudo install -m 0644 " + RemoteCommandBuilder.QuoteLinux(temporaryUnit) + " " + RemoteCommandBuilder.QuoteLinux("/etc/systemd/system/" + serviceName + ".service") + " && sudo systemctl daemon-reload && sudo systemctl enable " + RemoteCommandBuilder.QuoteLinux(serviceName);
            EnsureRemoteSuccess(host, await remoteClient.ExecuteAsync(host, "sh -lc " + RemoteCommandBuilder.QuoteLinux(command), cancellationToken).ConfigureAwait(false), "安装 systemd 服务");
        }
        else
        {
            string descriptorPath = CombineRemote(host, host.DeploymentRoot, "instances", instance.InstanceId, "config", "service-host.json");
            string serverCtlPath = CombineRemote(host, host.DeploymentRoot, "releases", context.Profile.Environment.ReleaseVersion, BuildTargetKind.ServerWindowsX64.ToString(), "Tools", "ServerCtl", "MiniCore.ServerCtl.exe");
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
            await remoteClient.UploadTextAsync(host, descriptor, ToSftpPath(host, descriptorPath), cancellationToken).ConfigureAwait(false);
            string serviceHostPath = CombineRemote(host, host.DeploymentRoot, "releases", context.Profile.Environment.ReleaseVersion, BuildTargetKind.ServerWindowsX64.ToString(), "Tools", "ServiceHost", "MiniCore.Deploy.ServiceHost.exe");
            string script = "$name=" + RemoteCommandBuilder.QuotePowerShell(serviceName) + ";$bin='\"'+" + RemoteCommandBuilder.QuotePowerShell(serviceHostPath) + "+'\" --descriptor \"'+" + RemoteCommandBuilder.QuotePowerShell(descriptorPath) + "+'\"';if(Get-Service -Name $name -ErrorAction SilentlyContinue){sc.exe config $name binPath= $bin start= auto|Out-Null}else{sc.exe create $name binPath= $bin start= auto DisplayName= $name|Out-Null};if($LASTEXITCODE -ne 0){throw 'sc.exe service install failed'}";
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
        string command = RemoteCommandBuilder.ForHost(
            host,
            "mkdir -p " + RemoteCommandBuilder.QuoteLinux(CombineRemote(host, host.DeploymentRoot, "current")) + " && ln -sfn " + RemoteCommandBuilder.QuoteLinux(releasePath) + " " + RemoteCommandBuilder.QuoteLinux(currentPath),
            "$target=" + RemoteCommandBuilder.QuotePowerShell(releasePath) + ";$link=" + RemoteCommandBuilder.QuotePowerShell(currentPath) + ";New-Item -ItemType Directory -Force -Path (Split-Path $link)|Out-Null;if(Test-Path -LiteralPath $link){Remove-Item -Force -Recurse -LiteralPath $link};New-Item -ItemType Junction -Path $link -Target $target|Out-Null");
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
                result = await ExecuteServerCtlAsync(host, instance, context.Profile.Environment.ReleaseVersion, "health", cancellationToken).ConfigureAwait(false);
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
        var builder = new StringBuilder("minicore-");
        for (int index = 0; index < instance.InstanceId.Length; index++)
        {
            char character = char.ToLowerInvariant(instance.InstanceId[index]);
            builder.Append(char.IsLetterOrDigit(character) || character is '-' or '.' ? character : '-');
        }

        return builder.ToString();
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
        throw new InvalidOperationException($"主机 {host.HostId} 的{operation}失败，退出码 {result.ExitCode}：{detail.Trim()}");
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
    /// <returns>恢复建议。</returns>
    private static string GetRecoverySuggestion(DeploymentAction action)
    {
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
    private static StepResult CreateResult(
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
            ErrorCode = errorCode,
            Message = message,
            RecoverySuggestion = recoverySuggestion
        };
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
