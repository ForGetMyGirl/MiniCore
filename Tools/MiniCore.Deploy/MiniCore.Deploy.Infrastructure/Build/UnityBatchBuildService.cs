using System.Diagnostics;
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

    private const double EditorInstanceTimestampToleranceSeconds = 2d; // 兼容进程启动时间与文件时间戳的极小精度差。
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
    /// <param name="releaseRoot">本轮隔离构建的版本根目录。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Unity 输出摘要。</returns>
    public async Task<UnityBuildResponse> BuildAsync(
        DeploymentProfile profile,
        string releaseRoot,
        CancellationToken cancellationToken)
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
        string generationBuildTarget = targetNames.Length == 0
            ? string.Empty
            : GetUnityBuildTargetArgument(targetNames[0]);

        await RunGenerationStageAsync(
            profile,
            generationBuildTarget,
            "MiniCore.EditorTools.Deploy.MiniCoreDeployGenerationCommand.GenerateSources",
            Path.Combine(operationDirectory, "01-generate-sources.log"),
            cancellationToken).ConfigureAwait(false);
        await RunGenerationStageAsync(
            profile,
            generationBuildTarget,
            "MiniCore.EditorTools.Deploy.MiniCoreDeployGenerationCommand.GenerateHandlers",
            Path.Combine(operationDirectory, "02-generate-handlers.log"),
            cancellationToken).ConfigureAwait(false);

        if (targetNames.Length == 0)
        {
            return new UnityBuildResponse { Succeeded = true, Message = "代码生成完成；没有选择 Unity Player 构建目标。" };
        }

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
                OutputPath = Path.GetFullPath(releaseRoot),
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
        if (IsRunningEditorInstance(editorInstancePath, out string editorDiagnostic))
        {
            throw new InvalidOperationException(
                $"Unity Editor 正在占用该项目。{editorDiagnostic}；{DescribeLockFile(lockPath)}。请保存并关闭对应 Editor 后重新执行构建。");
        }

        if (File.Exists(lockPath))
        {
            RemoveStaleUnityLockFile(lockPath, editorInstancePath, editorDiagnostic);
        }
    }

    /// <summary>
    /// 判断 EditorInstance 记录的进程是否仍是创建该记录的 Unity Editor。
    /// </summary>
    /// <param name="editorInstancePath">Unity EditorInstance.json 路径。</param>
    /// <param name="diagnostic">可写入构建结果的实例诊断。</param>
    /// <returns>对应 Unity Editor 仍在运行时返回 true。</returns>
    private static bool IsRunningEditorInstance(string editorInstancePath, out string diagnostic)
    {
        if (!File.Exists(editorInstancePath))
        {
            diagnostic = $"EditorInstance 不存在：{editorInstancePath}";
            return false;
        }

        int processId;
        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(editorInstancePath));
            if (!document.RootElement.TryGetProperty("process_id", out JsonElement processIdElement)
                || !processIdElement.TryGetInt32(out processId)
                || processId <= 0)
            {
                diagnostic = $"EditorInstance 未包含有效 process_id：{editorInstancePath}";
                return false;
            }
        }
        catch (InvalidOperationException)
        {
            diagnostic = $"EditorInstance 内容无效：{editorInstancePath}";
            return false;
        }
        catch (JsonException)
        {
            diagnostic = $"EditorInstance JSON 已损坏：{editorInstancePath}";
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            diagnostic = $"当前账户无权读取 EditorInstance：{editorInstancePath}；为避免并发构建按有效占用处理";
            return true;
        }
        catch (IOException)
        {
            diagnostic = $"EditorInstance 读取失败：{editorInstancePath}；为避免并发构建按有效占用处理";
            return true;
        }

        try
        {
            using Process process = Process.GetProcessById(processId);
            if (process.HasExited)
            {
                diagnostic = $"EditorInstance PID {processId} 已退出";
                return false;
            }

            string processName = process.ProcessName;
            if (!string.Equals(processName, "Unity", StringComparison.OrdinalIgnoreCase))
            {
                diagnostic = $"EditorInstance PID {processId} 已被非 Unity 进程复用：{processName}";
                return false;
            }

            DateTime instanceWrittenUtc = File.GetLastWriteTimeUtc(editorInstancePath);
            DateTime processStartedUtc = process.StartTime.ToUniversalTime();
            if (processStartedUtc > instanceWrittenUtc.AddSeconds(EditorInstanceTimestampToleranceSeconds))
            {
                diagnostic =
                    $"EditorInstance PID {processId} 已被后来启动的 Unity 进程复用：进程启动 {processStartedUtc:O}，实例记录 {instanceWrittenUtc:O}";
                return false;
            }

            diagnostic =
                $"EditorInstance PID {processId} 仍在运行，进程 {processName}，启动时间 {processStartedUtc:O}，实例记录时间 {instanceWrittenUtc:O}";
            return true;
        }
        catch (ArgumentException)
        {
            diagnostic = $"EditorInstance PID {processId} 已不存在";
            return false;
        }
        catch (InvalidOperationException)
        {
            diagnostic = $"EditorInstance PID {processId} 在检查期间退出";
            return false;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            diagnostic = $"EditorInstance PID {processId} 存在，但当前账户无法读取完整进程信息；为避免并发构建按有效占用处理";
            return true;
        }
        catch (NotSupportedException)
        {
            diagnostic = $"EditorInstance PID {processId} 存在，但当前平台无法读取完整进程信息；为避免并发构建按有效占用处理";
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            diagnostic = $"EditorInstance PID {processId} 存在，但当前账户无法读取实例文件时间；为避免并发构建按有效占用处理";
            return true;
        }
        catch (IOException)
        {
            diagnostic = $"EditorInstance PID {processId} 存在，但实例文件时间读取失败；为避免并发构建按有效占用处理";
            return true;
        }
    }

    /// <summary>
    /// 在确认没有有效 Unity Editor 且能够独占锁文件后清理陈旧锁。
    /// </summary>
    /// <param name="lockPath">Unity 锁文件路径。</param>
    /// <param name="editorInstancePath">Unity EditorInstance.json 路径。</param>
    /// <param name="initialEditorDiagnostic">第一次 EditorInstance 检查结果。</param>
    private static void RemoveStaleUnityLockFile(
        string lockPath,
        string editorInstancePath,
        string initialEditorDiagnostic)
    {
        string lockDiagnostic = DescribeLockFile(lockPath);
        string? quarantinedPath = null;
        try
        {
            using (var lockStream = new FileStream(
                       lockPath,
                       FileMode.Open,
                       FileAccess.ReadWrite,
                       FileShare.None))
            {
                if (IsRunningEditorInstance(editorInstancePath, out string currentEditorDiagnostic))
                {
                    throw new InvalidOperationException(
                        $"Unity Editor 在项目占用检查期间启动，未清理锁文件。{currentEditorDiagnostic}；{lockDiagnostic}。请关闭对应 Editor 后重试。");
                }

                if (!OperatingSystem.IsWindows())
                {
                    quarantinedPath = lockPath + ".stale-" + Guid.NewGuid().ToString("N");
                    File.Move(lockPath, quarantinedPath);
                }
            }

            if (OperatingSystem.IsWindows())
            {
                if (IsRunningEditorInstance(editorInstancePath, out string currentEditorDiagnostic))
                {
                    throw new InvalidOperationException(
                        $"Unity Editor 在陈旧锁清理前启动，未删除锁文件。{currentEditorDiagnostic}；{lockDiagnostic}。请关闭对应 Editor 后重试。");
                }

                File.Delete(lockPath);
                return;
            }

            if (!string.IsNullOrEmpty(quarantinedPath))
            {
                DeleteQuarantinedLockFile(quarantinedPath);
            }
        }
        catch (FileNotFoundException)
        {
            return;
        }
        catch (DirectoryNotFoundException)
        {
            return;
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new InvalidOperationException(
                $"检测到可能陈旧的 Unity 锁，但当前账户无权安全检查或清理。{initialEditorDiagnostic}；{lockDiagnostic}。请检查项目 Temp 目录权限。",
                exception);
        }
        catch (IOException exception)
        {
            throw new InvalidOperationException(
                $"UnityLockfile 仍被进程持有或无法取得独占锁，未执行清理。{initialEditorDiagnostic}；{lockDiagnostic}。请确认没有 Unity Editor 或构建进程正在使用该项目。",
                exception);
        }
    }

    /// <summary>
    /// 尽力删除已经与正式锁路径隔离的陈旧文件，不让清理残留再次阻止构建。
    /// </summary>
    /// <param name="quarantinedPath">已隔离的陈旧锁路径。</param>
    private static void DeleteQuarantinedLockFile(string quarantinedPath)
    {
        try
        {
            File.Delete(quarantinedPath);
        }
        catch (UnauthorizedAccessException exception)
        {
            Trace.TraceWarning(
                "陈旧 UnityLockfile 已隔离，但当前账户无法删除隔离文件 {0}（{1}）。该文件不会继续阻止构建。",
                quarantinedPath,
                exception.GetType().Name);
        }
        catch (IOException exception)
        {
            Trace.TraceWarning(
                "陈旧 UnityLockfile 已隔离，但删除隔离文件 {0} 失败（{1}）。该文件不会继续阻止构建。",
                quarantinedPath,
                exception.GetType().Name);
        }
    }

    /// <summary>
    /// 生成人工可定位的 Unity 锁文件诊断。
    /// </summary>
    /// <param name="lockPath">Unity 锁文件路径。</param>
    /// <returns>包含路径、大小和最后修改时间的诊断文本。</returns>
    private static string DescribeLockFile(string lockPath)
    {
        try
        {
            var file = new FileInfo(lockPath);
            file.Refresh();
            if (!file.Exists)
            {
                return $"UnityLockfile 不存在：{lockPath}";
            }

            return $"UnityLockfile：{lockPath}，大小 {file.Length} 字节，最后修改 {file.LastWriteTimeUtc:O}";
        }
        catch (UnauthorizedAccessException)
        {
            return $"UnityLockfile：{lockPath}，当前账户无权读取文件信息";
        }
        catch (IOException exception)
        {
            return $"UnityLockfile：{lockPath}，文件信息读取失败（{exception.GetType().Name}）";
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
    /// <param name="buildTarget">可选 Unity 命令行构建目标；仅构建 .NET 服务时为空。</param>
    /// <param name="executeMethod">Unity 静态入口。</param>
    /// <param name="logPath">日志路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>生成完成任务。</returns>
    private async Task RunGenerationStageAsync(
        DeploymentProfile profile,
        string buildTarget,
        string executeMethod,
        string logPath,
        CancellationToken cancellationToken)
    {
        ProcessResult result = await RunUnityAsync(
            profile,
            buildTarget,
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
            profile.Project.ProjectPath
        };
        if (!string.IsNullOrWhiteSpace(buildTarget))
        {
            arguments.Add("-buildTarget");
            arguments.Add(buildTarget);
        }

        arguments.Add("-executeMethod");
        arguments.Add(executeMethod);
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
