using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Infrastructure.Build;

/// <summary>
/// 定义桌面应用传递给 Unity BatchMode 的完整构建请求。
/// </summary>
public sealed class UnityBuildRequest
{
    #region Public 公共成员

    /// <summary>
    /// 获取或设置发布版本。
    /// </summary>
    public string ReleaseVersion { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置完整 AOT 构建或业务构建操作。
    /// </summary>
    public DeploymentOperation Operation { get; set; }

    /// <summary>
    /// 获取或设置构建输出目录。
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置客户端启动场景。
    /// </summary>
    public string ClientScenePath { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Dedicated Server 启动场景。
    /// </summary>
    public string ServerScenePath { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置目标名称数组。
    /// </summary>
    public string[] Targets { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 获取或设置 Android 是否生成 AAB；false 时生成 APK。
    /// </summary>
    public bool AndroidAppBundle { get; set; } = true;

    /// <summary>
    /// 获取或设置是否只输出 HotUpdate 与 YooAsset 内容而不构建 Player。
    /// </summary>
    public bool ContentOnly { get; set; }

    #endregion
}
