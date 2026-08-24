using MiniCore.Deploy.Core.Models;
using MiniCore.Deploy.Infrastructure.Persistence;
using MiniCore.Deploy.Infrastructure.Processes;

namespace MiniCore.Deploy.Infrastructure.Build;

/// <summary>
/// 发布可选 Auth/DB、Windows ServiceHost 与跨平台 ServerCtl 制品。
/// </summary>
public sealed class DotNetComponentPublisher
{
    #region Private 私有成员

    private readonly ProcessRunner runner; // dotnet 外部进程执行器。
    private readonly ApplicationPaths paths; // 构建日志目录。

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 创建 .NET 组件发布器。
    /// </summary>
    /// <param name="runner">进程执行器。</param>
    /// <param name="paths">应用路径。</param>
    public DotNetComponentPublisher(ProcessRunner runner, ApplicationPaths paths)
    {
        this.runner = runner ?? throw new ArgumentNullException(nameof(runner));
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    /// <summary>
    /// 按服务器目标和可选组件发布自包含 .NET 可执行程序。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="releaseRoot">本轮隔离构建的版本根目录。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>生成的输出目录。</returns>
    public async Task<IReadOnlyList<string>> PublishAsync(
        DeploymentProfile profile,
        string releaseRoot,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        releaseRoot = Path.GetFullPath(releaseRoot);
        string logRoot = Path.Combine(paths.LogsPath, "dotnet-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(logRoot);
        var outputs = new List<string>();

        bool needsLinuxDedicatedServer = Contains(profile.Project.BuildTargets, BuildTargetKind.ServerLinuxX64);
        bool needsWindowsDedicatedServer = Contains(profile.Project.BuildTargets, BuildTargetKind.ServerWindowsX64);
        if (needsLinuxDedicatedServer)
        {
            await PublishProjectAsync(profile.Project.ProjectPath, "Tools/MiniCore.Deploy/MiniCore.ServerCtl/MiniCore.ServerCtl.csproj", "linux-x64", Path.Combine(releaseRoot, BuildTargetKind.ServerLinuxX64.ToString(), "Tools", "ServerCtl"), logRoot, cancellationToken).ConfigureAwait(false);
            outputs.Add(Path.Combine(releaseRoot, BuildTargetKind.ServerLinuxX64.ToString(), "Tools", "ServerCtl"));
        }

        if (needsWindowsDedicatedServer)
        {
            await PublishProjectAsync(profile.Project.ProjectPath, "Tools/MiniCore.Deploy/MiniCore.ServerCtl/MiniCore.ServerCtl.csproj", "win-x64", Path.Combine(releaseRoot, BuildTargetKind.ServerWindowsX64.ToString(), "Tools", "ServerCtl"), logRoot, cancellationToken).ConfigureAwait(false);
            await PublishProjectAsync(profile.Project.ProjectPath, "Tools/MiniCore.Deploy/MiniCore.Deploy.ServiceHost/MiniCore.Deploy.ServiceHost.csproj", "win-x64", Path.Combine(releaseRoot, BuildTargetKind.ServerWindowsX64.ToString(), "Tools", "ServiceHost"), logRoot, cancellationToken).ConfigureAwait(false);
            outputs.Add(Path.Combine(releaseRoot, BuildTargetKind.ServerWindowsX64.ToString(), "Tools", "ServerCtl"));
            outputs.Add(Path.Combine(releaseRoot, BuildTargetKind.ServerWindowsX64.ToString(), "Tools", "ServiceHost"));
        }

        if (Contains(profile.Project.BuildTargets, BuildTargetKind.AuthenticationServer))
        {
            bool needsLinux = HasComponentOnOperatingSystem(profile, ComponentKind.AuthenticationServer, HostOperatingSystem.Linux);
            bool needsWindows = HasComponentOnOperatingSystem(profile, ComponentKind.AuthenticationServer, HostOperatingSystem.Windows);
            await PublishOptionalServerAsync(profile, "AuthenticationServer", "Server/AuthenticationServer/AuthenticationServer.csproj", releaseRoot, logRoot, needsLinux, needsWindows, cancellationToken).ConfigureAwait(false);
            outputs.Add(Path.Combine(releaseRoot, "DotNet", "AuthenticationServer"));
        }

        if (Contains(profile.Project.BuildTargets, BuildTargetKind.DatabaseServer))
        {
            bool needsLinux = HasComponentOnOperatingSystem(profile, ComponentKind.DatabaseServer, HostOperatingSystem.Linux);
            bool needsWindows = HasComponentOnOperatingSystem(profile, ComponentKind.DatabaseServer, HostOperatingSystem.Windows);
            await PublishOptionalServerAsync(profile, "DatabaseServer", "Server/DatabaseServer/DatabaseServer.csproj", releaseRoot, logRoot, needsLinux, needsWindows, cancellationToken).ConfigureAwait(false);
            outputs.Add(Path.Combine(releaseRoot, "DotNet", "DatabaseServer"));
        }

        return outputs;
    }

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 为已经选择的服务器系统发布一个可选 .NET 服务。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="componentName">组件名称。</param>
    /// <param name="projectRelativePath">项目相对路径。</param>
    /// <param name="releaseRoot">版本输出根目录。</param>
    /// <param name="logRoot">日志目录。</param>
    /// <param name="needsLinux">是否发布 Linux。</param>
    /// <param name="needsWindows">是否发布 Windows。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>发布完成任务。</returns>
    private async Task PublishOptionalServerAsync(
        DeploymentProfile profile,
        string componentName,
        string projectRelativePath,
        string releaseRoot,
        string logRoot,
        bool needsLinux,
        bool needsWindows,
        CancellationToken cancellationToken)
    {
        if (needsLinux)
        {
            await PublishProjectAsync(profile.Project.ProjectPath, projectRelativePath, "linux-x64", Path.Combine(releaseRoot, "DotNet", componentName, "linux-x64"), logRoot, cancellationToken).ConfigureAwait(false);
        }

        if (needsWindows)
        {
            string componentOutput = Path.Combine(releaseRoot, "DotNet", componentName, "win-x64");
            await PublishProjectAsync(profile.Project.ProjectPath, projectRelativePath, "win-x64", componentOutput, logRoot, cancellationToken).ConfigureAwait(false);
            await PublishProjectAsync(
                profile.Project.ProjectPath,
                "Tools/MiniCore.Deploy/MiniCore.Deploy.ServiceHost/MiniCore.Deploy.ServiceHost.csproj",
                "win-x64",
                Path.Combine(componentOutput, "Tools", "ServiceHost"),
                logRoot,
                cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 运行一次不会执行测试的 dotnet publish。
    /// </summary>
    /// <param name="projectRoot">仓库根目录。</param>
    /// <param name="projectRelativePath">项目文件相对路径。</param>
    /// <param name="runtimeIdentifier">目标 RID。</param>
    /// <param name="outputPath">输出目录。</param>
    /// <param name="logRoot">日志目录。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>发布完成任务。</returns>
    private async Task PublishProjectAsync(
        string projectRoot,
        string projectRelativePath,
        string runtimeIdentifier,
        string outputPath,
        string logRoot,
        CancellationToken cancellationToken)
    {
        string projectPath = Path.Combine(projectRoot, projectRelativePath);
        if (Directory.Exists(outputPath))
        {
            Directory.Delete(outputPath, true);
        }

        string logName = Path.GetFileNameWithoutExtension(projectPath) + "-" + runtimeIdentifier + ".log";
        ProcessResult result = await runner.RunAsync(
            "dotnet",
            new[]
            {
                "publish",
                projectPath,
                "--configuration",
                "Release",
                "--runtime",
                runtimeIdentifier,
                "--self-contained",
                "true",
                "--output",
                outputPath,
                "-p:PublishSingleFile=true",
                "-p:DebugType=None",
                "-p:DebugSymbols=false"
            },
            projectRoot,
            Path.Combine(logRoot, logName),
            cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"发布 {projectRelativePath} ({runtimeIdentifier}) 失败，详情见 {result.LogPath}。");
        }
    }

    /// <summary>
    /// 判断目标集合是否包含指定项。
    /// </summary>
    /// <param name="targets">目标集合。</param>
    /// <param name="expected">指定项。</param>
    /// <returns>包含时返回 true。</returns>
    private static bool Contains(IReadOnlyList<BuildTargetKind> targets, BuildTargetKind expected)
    {
        for (int index = 0; index < targets.Count; index++)
        {
            if (targets[index] == expected)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断指定组件是否在目标操作系统上具有启用实例。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="component">组件种类。</param>
    /// <param name="operatingSystem">目标操作系统。</param>
    /// <returns>存在匹配实例时返回 true。</returns>
    private static bool HasComponentOnOperatingSystem(
        DeploymentProfile profile,
        ComponentKind component,
        HostOperatingSystem operatingSystem)
    {
        for (int instanceIndex = 0; instanceIndex < profile.Environment.Instances.Count; instanceIndex++)
        {
            InstanceDefinition instance = profile.Environment.Instances[instanceIndex];
            if (!instance.Enabled || instance.Component != component)
            {
                continue;
            }

            for (int hostIndex = 0; hostIndex < profile.Environment.Hosts.Count; hostIndex++)
            {
                HostDefinition host = profile.Environment.Hosts[hostIndex];
                if (string.Equals(host.HostId, instance.HostId, StringComparison.Ordinal)
                    && host.OperatingSystem == operatingSystem)
                {
                    return true;
                }
            }
        }

        return false;
    }

    #endregion
}
