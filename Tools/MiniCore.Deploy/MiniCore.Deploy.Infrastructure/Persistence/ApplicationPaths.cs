namespace MiniCore.Deploy.Infrastructure.Persistence;

/// <summary>
/// 集中定义不会写入项目仓库的用户配置、日志和历史目录。
/// </summary>
public sealed class ApplicationPaths
{
    #region Public 公共成员

    /// <summary>
    /// 获取应用数据根目录。
    /// </summary>
    public string RootPath { get; }

    /// <summary>
    /// 获取所有独立配置方案文件的目录。
    /// </summary>
    public string ProfilesPath => Path.Combine(RootPath, "profiles");

    /// <summary>
    /// 获取记录当前活动配置方案标识的文件路径。
    /// </summary>
    public string ActiveProfilePath => Path.Combine(ProfilesPath, "active-profile.txt");

    /// <summary>
    /// 获取执行历史目录。
    /// </summary>
    public string HistoryPath => Path.Combine(RootPath, "history");

    /// <summary>
    /// 获取可在应用重启后恢复的计划快照目录。
    /// </summary>
    public string PlansPath => Path.Combine(RootPath, "plans");

    /// <summary>
    /// 获取构建与发布日志目录。
    /// </summary>
    public string LogsPath => Path.Combine(RootPath, "logs");

    /// <summary>
    /// 创建当前操作系统的 MiniCore Deploy 数据路径。
    /// </summary>
    public ApplicationPaths()
    {
        string basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        RootPath = Path.Combine(basePath, "MiniCore", "Deploy");
        Directory.CreateDirectory(RootPath);
        Directory.CreateDirectory(ProfilesPath);
        Directory.CreateDirectory(HistoryPath);
        Directory.CreateDirectory(PlansPath);
        Directory.CreateDirectory(LogsPath);
    }

    #endregion
}
