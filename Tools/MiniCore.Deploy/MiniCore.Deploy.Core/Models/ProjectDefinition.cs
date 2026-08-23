namespace MiniCore.Deploy.Core.Models;

/// <summary>
/// 保存可复用且不包含密钥的 MiniCore 项目构建设置。
/// </summary>
public sealed class ProjectDefinition
{
    #region Public 公共成员

    /// <summary>
    /// 获取或设置 Unity 可执行程序路径。
    /// </summary>
    public string UnityExecutablePath { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Unity 项目根目录。
    /// </summary>
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置本次发布制品输出目录。
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置客户端启动场景。
    /// </summary>
    public string ClientScenePath { get; set; } = "Assets/Scenes/HotUpdateScene.unity";

    /// <summary>
    /// 获取或设置 Dedicated Server 启动场景。
    /// </summary>
    public string ServerScenePath { get; set; } = "Assets/Scenes/Demos/MiniBomber/ServerBootstrapScene.unity";

    /// <summary>
    /// 获取或设置选中的构建目标。
    /// </summary>
    public List<BuildTargetKind> BuildTargets { get; set; } = new();

    /// <summary>
    /// 获取或设置本次需要发布的制品目标；为空时只构建不发布。
    /// </summary>
    public List<BuildTargetKind> PublishTargets { get; set; } = new();

    /// <summary>
    /// 获取或设置是否只生成 HotUpdate 与 YooAsset 内容而不重新构建 Player。
    /// </summary>
    public bool ContentOnly { get; set; }

    /// <summary>
    /// 获取或设置 Android 是否生成 AAB；false 时生成 APK。
    /// </summary>
    public bool AndroidAppBundle { get; set; } = true;

    #endregion
}
