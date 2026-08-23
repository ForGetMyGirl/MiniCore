using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Infrastructure.Build;

/// <summary>
/// 通过 Unity PlaybackEngines 目录在启动构建前检测已安装平台模块。
/// </summary>
public static class UnityModuleDetector
{
    #region Public 公共成员

    /// <summary>
    /// 检测指定 Unity 可执行程序旁的平台模块。
    /// </summary>
    /// <param name="unityExecutablePath">Unity 可执行程序路径。</param>
    /// <returns>平台模块可用性。</returns>
    public static UnityModuleAvailability Detect(string unityExecutablePath)
    {
        IReadOnlyList<string> playbackEngines;
        try
        {
            playbackEngines = ResolvePlaybackEnginesPaths(unityExecutablePath);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return new UnityModuleAvailability { Summary = "Unity 路径格式无效，请重新选择。" };
        }

        if (playbackEngines.Count == 0)
        {
            return new UnityModuleAvailability { Summary = "未找到 Unity PlaybackEngines 目录，请检查 Unity 路径。" };
        }

        bool linux = HasServerModule(playbackEngines, "LinuxStandaloneSupport");
        bool windows = HasServerModule(playbackEngines, "WindowsStandaloneSupport");
        bool macOS = HasModule(playbackEngines, "MacStandaloneSupport");
        bool android = HasModule(playbackEngines, "AndroidPlayer");
        bool webGL = HasModule(playbackEngines, "WebGLSupport");
        var available = new List<string>(5);
        AddAvailable(available, linux, "Linux Dedicated Server");
        AddAvailable(available, windows, "Windows Dedicated Server");
        AddAvailable(available, macOS, "macOS");
        AddAvailable(available, android, "Android");
        AddAvailable(available, webGL, "WebGL");
        return new UnityModuleAvailability
        {
            Summary = available.Count == 0 ? "当前 Unity 未检测到可用平台模块。" : "已安装模块：" + string.Join("、", available),
            ServerLinuxX64 = linux,
            ServerWindowsX64 = windows,
            ClientWindowsX64 = windows,
            ClientMacOS = macOS,
            ClientAndroid = android,
            ClientWebGL = webGL
        };
    }

    /// <summary>
    /// 校验用户选择的全部 Unity 构建目标均已安装对应模块。
    /// </summary>
    /// <param name="unityExecutablePath">Unity 可执行程序路径。</param>
    /// <param name="targets">用户选择目标。</param>
    public static void EnsureTargetsAvailable(string unityExecutablePath, IReadOnlyList<BuildTargetKind> targets)
    {
        UnityModuleAvailability availability = Detect(unityExecutablePath);
        var unavailable = new List<string>();
        for (int index = 0; index < targets.Count; index++)
        {
            if (!availability.IsAvailable(targets[index]))
            {
                unavailable.Add(targets[index].ToString());
            }
        }

        if (unavailable.Count > 0)
        {
            throw new InvalidOperationException($"当前 Unity 缺少平台模块：{string.Join("、", unavailable)}。{availability.Summary}");
        }
    }

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 从 macOS、Windows 和 Linux Unity 安装布局中寻找 PlaybackEngines。
    /// </summary>
    /// <param name="unityExecutablePath">Unity 可执行程序路径。</param>
    /// <returns>全部存在的模块根目录；未找到时返回空集合。</returns>
    private static IReadOnlyList<string> ResolvePlaybackEnginesPaths(string unityExecutablePath)
    {
        if (string.IsNullOrWhiteSpace(unityExecutablePath))
        {
            return Array.Empty<string>();
        }

        string fullPath = Path.GetFullPath(unityExecutablePath);
        string executableDirectory = Directory.Exists(fullPath)
            ? fullPath
            : Path.GetDirectoryName(fullPath) ?? string.Empty;
        var candidates = new List<string>(8);
        AddCandidateAndAncestors(candidates, executableDirectory);
        var result = new List<string>(candidates.Count);
        var unique = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < candidates.Count; index++)
        {
            string candidate = Path.GetFullPath(candidates[index]);
            if (Directory.Exists(candidate) && unique.Add(candidate))
            {
                result.Add(candidate);
            }
        }

        return result;
    }

    /// <summary>
    /// 从 Unity 可执行文件目录向上收集应用内与 Hub 外置模块目录候选。
    /// </summary>
    /// <param name="candidates">候选目录集合。</param>
    /// <param name="startDirectory">开始向上查找的目录。</param>
    private static void AddCandidateAndAncestors(ICollection<string> candidates, string startDirectory)
    {
        DirectoryInfo? directory = new(startDirectory);
        int remainingDepth = 6;
        while (directory != null && remainingDepth-- > 0)
        {
            candidates.Add(Path.Combine(directory.FullName, "PlaybackEngines"));
            candidates.Add(Path.Combine(directory.FullName, "Data", "PlaybackEngines"));
            directory = directory.Parent;
        }
    }

    /// <summary>
    /// 判断任一模块根目录是否包含指定平台模块。
    /// </summary>
    /// <param name="playbackEngines">全部模块根目录。</param>
    /// <param name="moduleName">模块目录名。</param>
    /// <returns>模块存在时返回 true。</returns>
    private static bool HasModule(IReadOnlyList<string> playbackEngines, string moduleName)
    {
        for (int index = 0; index < playbackEngines.Count; index++)
        {
            if (Directory.Exists(Path.Combine(playbackEngines[index], moduleName)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断任一平台模块是否实际包含 Dedicated Server Player 变体。
    /// </summary>
    /// <param name="playbackEngines">全部模块根目录。</param>
    /// <param name="moduleName">平台模块目录名。</param>
    /// <returns>存在服务器构建变体时返回 true。</returns>
    private static bool HasServerModule(IReadOnlyList<string> playbackEngines, string moduleName)
    {
        for (int index = 0; index < playbackEngines.Count; index++)
        {
            string modulePath = Path.Combine(playbackEngines[index], moduleName);
            string variationsPath = Path.Combine(modulePath, "Variations");
            if (!Directory.Exists(variationsPath))
            {
                continue;
            }

            string[] variations = Directory.GetDirectories(variationsPath, "*server*", SearchOption.TopDirectoryOnly);
            if (variations.Length > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 在模块可用时追加显示名称。
    /// </summary>
    /// <param name="available">可用模块列表。</param>
    /// <param name="enabled">模块是否可用。</param>
    /// <param name="displayName">显示名称。</param>
    private static void AddAvailable(ICollection<string> available, bool enabled, string displayName)
    {
        if (enabled)
        {
            available.Add(displayName);
        }
    }

    #endregion
}
