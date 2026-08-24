using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Infrastructure.Persistence;

/// <summary>
/// 使用独立 JSON 文件原子保存多份不含密钥内容的配置方案。
/// </summary>
public sealed class ProfileStore
{
    #region Private 私有成员

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions(); // 统一稳定 JSON 格式。
    private static readonly UTF8Encoding Utf8WithoutBom = new(false); // 配置文件统一使用无 BOM UTF-8。
    private readonly ApplicationPaths paths; // 仓库外应用数据路径。

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 创建配置存储。
    /// </summary>
    /// <param name="paths">应用路径。</param>
    public ProfileStore(ApplicationPaths paths)
    {
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    /// <summary>
    /// 读取全部配置方案；首次启动时创建一份本地开发方案。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>全部方案和当前活动方案。</returns>
    public async Task<ProfileStoreSnapshot> LoadAllAsync(CancellationToken cancellationToken)
    {
        string[] files = Directory.GetFiles(paths.ProfilesPath, "*.deploy.json", SearchOption.TopDirectoryOnly);
        Array.Sort(files, StringComparer.Ordinal);
        var profiles = new List<DeploymentProfile>(Math.Max(1, files.Length));
        for (int index = 0; index < files.Length; index++)
        {
            await using FileStream stream = File.OpenRead(files[index]);
            DeploymentProfile? loaded = await JsonSerializer.DeserializeAsync<DeploymentProfile>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
            if (loaded == null || string.IsNullOrWhiteSpace(loaded.ProfileId))
            {
                continue;
            }

            profiles.Add(loaded);
        }

        if (profiles.Count == 0)
        {
            DeploymentProfile initial = CreateDefaultProfile();
            profiles.Add(initial);
            await SaveAsync(initial, cancellationToken).ConfigureAwait(false);
            await SetActiveAsync(initial.ProfileId, cancellationToken).ConfigureAwait(false);
            return new ProfileStoreSnapshot(profiles, initial.ProfileId);
        }

        string activeProfileId = File.Exists(paths.ActiveProfilePath)
            ? (await File.ReadAllTextAsync(paths.ActiveProfilePath, cancellationToken).ConfigureAwait(false)).Trim()
            : string.Empty;
        if (!ContainsProfile(profiles, activeProfileId))
        {
            activeProfileId = profiles[0].ProfileId;
            await SetActiveAsync(activeProfileId, cancellationToken).ConfigureAwait(false);
        }

        return new ProfileStoreSnapshot(profiles, activeProfileId);
    }

    /// <summary>
    /// 将指定配置方案原子写入自己的独立文件。
    /// </summary>
    /// <param name="profile">待保存配置方案。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>保存完成任务。</returns>
    public async Task SaveAsync(DeploymentProfile profile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        profile.SchemaVersion = 5;
        ValidateProfileId(profile.ProfileId);
        string targetPath = GetProfilePath(profile.ProfileId);
        string temporaryPath = targetPath + ".tmp";
        await using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, true))
        {
            await JsonSerializer.SerializeAsync(stream, profile, JsonOptions, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        File.Move(temporaryPath, targetPath, true);
    }

    /// <summary>
    /// 原子记录下次启动应恢复的活动配置方案。
    /// </summary>
    /// <param name="profileId">活动方案标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>写入完成任务。</returns>
    public async Task SetActiveAsync(string profileId, CancellationToken cancellationToken)
    {
        ValidateProfileId(profileId);
        string temporaryPath = paths.ActiveProfilePath + ".tmp";
        await File.WriteAllTextAsync(temporaryPath, profileId, Utf8WithoutBom, cancellationToken).ConfigureAwait(false);
        File.Move(temporaryPath, paths.ActiveProfilePath, true);
    }

    /// <summary>
    /// 立即记录活动配置方案，供桌面选择器的轻量切换事件使用。
    /// </summary>
    /// <param name="profileId">活动方案标识。</param>
    public void SetActive(string profileId)
    {
        ValidateProfileId(profileId);
        string temporaryPath = paths.ActiveProfilePath + ".tmp";
        File.WriteAllText(temporaryPath, profileId, Utf8WithoutBom);
        File.Move(temporaryPath, paths.ActiveProfilePath, true);
    }

    /// <summary>
    /// 删除指定配置方案文件，不触碰远程服务、制品、日志或发布历史。
    /// </summary>
    /// <param name="profileId">待删除方案标识。</param>
    /// <returns>删除完成任务。</returns>
    public Task DeleteAsync(string profileId)
    {
        ValidateProfileId(profileId);
        string path = GetProfilePath(profileId);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 创建适配当前仓库布局的本地开发配置方案。
    /// </summary>
    /// <returns>带项目路径和常用服务端目标的默认方案。</returns>
    public static DeploymentProfile CreateDefaultProfile()
    {
        string projectPath = FindUnityProjectPath();
        string unityPath = OperatingSystem.IsMacOS()
            ? "/Applications/Unity/Hub/Editor/2021.3.45f2/Unity.app/Contents/MacOS/Unity"
            : string.Empty;
        var profile = new DeploymentProfile
        {
            Name = "本地开发",
            Purpose = "用于本机开发构建，不连接生产服务器。",
            Project = new ProjectDefinition
            {
                ProjectPath = projectPath,
                UnityExecutablePath = unityPath,
                OutputPath = string.IsNullOrEmpty(projectPath) ? string.Empty : Path.Combine(projectPath, "Builds", "Releases")
            },
            Environment = new EnvironmentDefinition
            {
                EnvironmentId = "local-development",
                DisplayName = "本地开发",
                RequireCleanGitWorkspace = false,
                ReleaseVersion = "0.1.0"
            }
        };
        profile.Project.BuildTargets.Add(BuildTargetKind.ServerLinuxX64);
        profile.Project.PublishTargets.Add(BuildTargetKind.ServerLinuxX64);
        return profile;
    }

    /// <summary>
    /// 返回指定方案的独立配置文件路径，供界面展示和导出。
    /// </summary>
    /// <param name="profileId">配置方案标识。</param>
    /// <returns>绝对文件路径。</returns>
    public string GetProfilePath(string profileId)
    {
        ValidateProfileId(profileId);
        return Path.Combine(paths.ProfilesPath, profileId + ".deploy.json");
    }

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 创建支持字符串枚举和驼峰字段的 JSON 设置。
    /// </summary>
    /// <returns>共享 JSON 设置。</returns>
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

    /// <summary>
    /// 判断已加载方案是否包含指定标识。
    /// </summary>
    /// <param name="profiles">已加载方案。</param>
    /// <param name="profileId">候选标识。</param>
    /// <returns>存在匹配方案时返回 true。</returns>
    private static bool ContainsProfile(IReadOnlyList<DeploymentProfile> profiles, string profileId)
    {
        for (int index = 0; index < profiles.Count; index++)
        {
            if (string.Equals(profiles[index].ProfileId, profileId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 校验方案标识只能用作当前配置目录中的普通文件名。
    /// </summary>
    /// <param name="profileId">待校验方案标识。</param>
    private static void ValidateProfileId(string profileId)
    {
        if (!Guid.TryParseExact(profileId, "N", out _))
        {
            throw new ArgumentException("配置方案标识必须是无分隔符 GUID。", nameof(profileId));
        }
    }

    /// <summary>
    /// 从当前目录和应用目录向上查找 Unity 项目，避免 Finder 启动时默认得到根目录。
    /// </summary>
    /// <returns>找到的 Unity 项目路径；未找到时返回空字符串。</returns>
    private static string FindUnityProjectPath()
    {
        string[] candidates = { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
        for (int candidateIndex = 0; candidateIndex < candidates.Length; candidateIndex++)
        {
            DirectoryInfo? directory = new(Path.GetFullPath(candidates[candidateIndex]));
            while (directory != null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "Assets"))
                    && Directory.Exists(Path.Combine(directory.FullName, "ProjectSettings")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        return string.Empty;
    }

    #endregion
}
