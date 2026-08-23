using System.Text.Json;
using System.Text.Json.Serialization;
using MiniCore.Deploy.Core.Models;
using MiniCore.Deploy.Infrastructure.Persistence;
using MiniCore.Deploy.Infrastructure.Processes;

namespace MiniCore.Deploy.Infrastructure.Build;

/// <summary>
/// 负责检查 Unity 项目占用并调用仓库内 BatchMode 构建桥接。
/// </summary>
public sealed class UnityBatchBuildService
{
    #region Private 私有成员

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions(); // 与 Unity 请求约定一致的 JSON 设置。
    private readonly ProcessRunner runner; // Unity 外部进程执行器。
    private readonly ApplicationPaths paths; // 仓库外日志目录。

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 创建 Unity BatchMode 构建服务。
    /// </summary>
    /// <param name="runner">进程执行器。</param>
    /// <param name="paths">应用路径。</param>
    public UnityBatchBuildService(ProcessRunner runner, ApplicationPaths paths)
    {
        this.runner = runner ?? throw new ArgumentNullException(nameof(runner));
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    /// <summary>
    /// 执行用户选择的 Unity Player 与 YooAsset 构建。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Unity 输出摘要。</returns>
    public async Task<UnityBuildResponse> BuildAsync(DeploymentProfile profile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        EnsureProjectAvailable(profile.Project.ProjectPath);
        if (!File.Exists(profile.Project.UnityExecutablePath))
        {
            throw new FileNotFoundException("找不到配置的 Unity 可执行程序。", profile.Project.UnityExecutablePath);
        }

        UnityModuleDetector.EnsureTargetsAvailable(profile.Project.UnityExecutablePath, profile.Project.BuildTargets);

        string operationDirectory = Path.Combine(paths.LogsPath, DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(operationDirectory);
        string[] targetNames = GetUnityTargetNames(profile.Project.BuildTargets);
        if (targetNames.Length == 0)
        {
            return new UnityBuildResponse { Succeeded = true, Message = "没有选择 Unity 构建目标。" };
        }

        await RunGenerationStageAsync(
            profile,
            targetNames[0],
            "MiniCore.EditorTools.Deploy.MiniCoreDeployGenerationCommand.GenerateSources",
            Path.Combine(operationDirectory, "01-generate-sources.log"),
            cancellationToken).ConfigureAwait(false);
        await RunGenerationStageAsync(
            profile,
            targetNames[0],
            "MiniCore.EditorTools.Deploy.MiniCoreDeployGenerationCommand.GenerateHandlers",
            Path.Combine(operationDirectory, "02-generate-handlers.log"),
            cancellationToken).ConfigureAwait(false);

        var outputs = new List<string>(targetNames.Length);
        var errors = new List<string>();
        for (int targetIndex = 0; targetIndex < targetNames.Length; targetIndex++)
        {
            string targetName = targetNames[targetIndex];
            string requestPath = Path.Combine(operationDirectory, $"build-{targetIndex:D2}-{targetName}-request.json");
            string resultPath = Path.Combine(operationDirectory, $"build-{targetIndex:D2}-{targetName}-result.json");
            string logPath = Path.Combine(operationDirectory, $"build-{targetIndex:D2}-{targetName}.log");
            var request = new UnityBuildRequest
            {
                ReleaseVersion = profile.Environment.ReleaseVersion,
                Operation = profile.Operation,
                OutputPath = Path.GetFullPath(Path.Combine(profile.Project.OutputPath, profile.Environment.ReleaseVersion)),
                ClientScenePath = profile.Project.ClientScenePath,
                ServerScenePath = profile.Project.ServerScenePath,
                Targets = new[] { targetName },
                AndroidAppBundle = profile.Project.AndroidAppBundle,
                ContentOnly = profile.Project.ContentOnly
            };
            await File.WriteAllTextAsync(requestPath, JsonSerializer.Serialize(request, JsonOptions), cancellationToken).ConfigureAwait(false);
            ProcessResult process = await RunUnityAsync(
                profile,
                GetUnityBuildTargetArgument(targetName),
                "MiniCore.EditorTools.Deploy.MiniCoreDeployBuildCommand.Execute",
                new[] { "-minicoreDeployRequest", requestPath, "-minicoreDeployResult", resultPath },
                logPath,
                cancellationToken).ConfigureAwait(false);

            UnityBuildResponse? response = null;
            if (File.Exists(resultPath))
            {
                await using FileStream resultStream = File.OpenRead(resultPath);
                response = await JsonSerializer.DeserializeAsync<UnityBuildResponse>(resultStream, JsonOptions, cancellationToken).ConfigureAwait(false);
            }

            if (process.ExitCode != 0 || response == null || !response.Succeeded)
            {
                string reason = response?.Message ?? $"Unity 未写出 {targetName} 的有效构建结果。";
                errors.Add($"{targetName}: {reason}，日志：{process.LogPath}");
                continue;
            }

            outputs.AddRange(response.Outputs);
        }

        var aggregate = new UnityBuildResponse
        {
            Succeeded = errors.Count == 0,
            Message = errors.Count == 0
                ? $"全部 {outputs.Count} 个 Unity 目标构建成功。"
                : $"{outputs.Count} 个目标成功，{errors.Count} 个目标失败。",
            Outputs = outputs.ToArray(),
            Errors = errors.ToArray()
        };
        if (!aggregate.Succeeded)
        {
            throw new InvalidOperationException(aggregate.Message + Environment.NewLine + string.Join(Environment.NewLine, aggregate.Errors));
        }

        return aggregate;
    }

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 检查 Unity Editor 是否仍占用目标项目。
    /// </summary>
    /// <param name="projectPath">Unity 项目路径。</param>
    private static void EnsureProjectAvailable(string projectPath)
    {
        if (!Directory.Exists(projectPath))
        {
            throw new DirectoryNotFoundException($"Unity 项目目录不存在：{projectPath}。");
        }

        string lockPath = Path.Combine(projectPath, "Temp", "UnityLockfile");
        string editorInstancePath = Path.Combine(projectPath, "Library", "EditorInstance.json");
        if (File.Exists(lockPath) || IsRunningEditorInstance(editorInstancePath))
        {
            throw new InvalidOperationException("Unity Editor 正在占用该项目。请保存并关闭项目后重新执行构建。");
        }
    }

    /// <summary>
    /// 判断 EditorInstance 记录的进程是否仍在运行。
    /// </summary>
    /// <param name="editorInstancePath">Unity EditorInstance.json 路径。</param>
    /// <returns>对应进程仍存在时返回 true。</returns>
    private static bool IsRunningEditorInstance(string editorInstancePath)
    {
        if (!File.Exists(editorInstancePath))
        {
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(editorInstancePath));
            if (!document.RootElement.TryGetProperty("process_id", out JsonElement processIdElement))
            {
                return false;
            }

            int processId = processIdElement.GetInt32();
            using System.Diagnostics.Process process = System.Diagnostics.Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// 仅返回由 Unity 构建的目标，.NET 服务由独立发布器处理。
    /// </summary>
    /// <param name="targets">用户选择目标。</param>
    /// <returns>Unity 构建目标名称。</returns>
    private static string[] GetUnityTargetNames(IReadOnlyList<BuildTargetKind> targets)
    {
        var names = new List<string>(targets.Count);
        for (int index = 0; index < targets.Count; index++)
        {
            BuildTargetKind target = targets[index];
            if (target is BuildTargetKind.AuthenticationServer or BuildTargetKind.DatabaseServer)
            {
                continue;
            }

            names.Add(target.ToString());
        }

        return names.ToArray();
    }

    /// <summary>
    /// 在独立 Unity 进程中运行一个生成阶段，确保下一阶段看到完成编译的新脚本域。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="firstTargetName">第一个构建目标。</param>
    /// <param name="executeMethod">Unity 静态入口。</param>
    /// <param name="logPath">日志路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>生成完成任务。</returns>
    private async Task RunGenerationStageAsync(
        DeploymentProfile profile,
        string firstTargetName,
        string executeMethod,
        string logPath,
        CancellationToken cancellationToken)
    {
        ProcessResult result = await RunUnityAsync(
            profile,
            GetUnityBuildTargetArgument(firstTargetName),
            executeMethod,
            Array.Empty<string>(),
            logPath,
            cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Unity 生成阶段 {executeMethod} 失败，详情见 {result.LogPath}。");
        }
    }

    /// <summary>
    /// 以统一参数运行一次 Unity BatchMode。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="buildTarget">Unity 命令行构建目标。</param>
    /// <param name="executeMethod">静态入口。</param>
    /// <param name="additionalArguments">额外参数。</param>
    /// <param name="logPath">日志路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>外部进程结果。</returns>
    private Task<ProcessResult> RunUnityAsync(
        DeploymentProfile profile,
        string buildTarget,
        string executeMethod,
        IReadOnlyList<string> additionalArguments,
        string logPath,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string>(12 + additionalArguments.Count)
        {
            "-batchmode",
            "-quit",
            "-nographics",
            "-projectPath",
            profile.Project.ProjectPath,
            "-buildTarget",
            buildTarget,
            "-executeMethod",
            executeMethod
        };
        for (int index = 0; index < additionalArguments.Count; index++)
        {
            arguments.Add(additionalArguments[index]);
        }

        arguments.Add("-logFile");
        arguments.Add("-");
        return runner.RunAsync(
            profile.Project.UnityExecutablePath,
            arguments,
            profile.Project.ProjectPath,
            logPath,
            cancellationToken);
    }

    /// <summary>
    /// 将 Deploy 目标转换为 Unity 命令行 -buildTarget 值。
    /// </summary>
    /// <param name="targetName">Deploy 目标名称。</param>
    /// <returns>Unity 命令行平台名。</returns>
    private static string GetUnityBuildTargetArgument(string targetName)
    {
        return targetName switch
        {
            nameof(BuildTargetKind.ServerLinuxX64) => "StandaloneLinux64",
            nameof(BuildTargetKind.ServerWindowsX64) => "StandaloneWindows64",
            nameof(BuildTargetKind.ClientWindowsX64) => "StandaloneWindows64",
            nameof(BuildTargetKind.ClientMacOS) => "StandaloneOSX",
            nameof(BuildTargetKind.ClientAndroid) => "Android",
            nameof(BuildTargetKind.ClientWebGL) => "WebGL",
            _ => throw new ArgumentOutOfRangeException(nameof(targetName), targetName, "未知 Unity 构建目标。")
        };
    }

    /// <summary>
    /// 创建字符串枚举 JSON 设置。
    /// </summary>
    /// <returns>请求和响应 JSON 设置。</returns>
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
