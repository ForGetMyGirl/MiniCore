using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Infrastructure.Build;

/// <summary>
/// 描述当前 Unity 安装可用于 BatchMode 的平台模块。
/// </summary>
public sealed class UnityModuleAvailability
{
    #region Public 公共成员

    /// <summary>
    /// 获取模块扫描结果摘要。
    /// </summary>
    public string Summary { get; init; } = "尚未检测 Unity 平台模块。";

    /// <summary>
    /// 获取 Linux Dedicated Server 模块是否可用。
    /// </summary>
    public bool ServerLinuxX64 { get; init; }

    /// <summary>
    /// 获取 Windows Dedicated Server 模块是否可用。
    /// </summary>
    public bool ServerWindowsX64 { get; init; }

    /// <summary>
    /// 获取 Windows 客户端模块是否可用。
    /// </summary>
    public bool ClientWindowsX64 { get; init; }

    /// <summary>
    /// 获取 macOS 客户端模块是否可用。
    /// </summary>
    public bool ClientMacOS { get; init; }

    /// <summary>
    /// 获取 Android 模块是否可用。
    /// </summary>
    public bool ClientAndroid { get; init; }

    /// <summary>
    /// 获取 WebGL 模块是否可用。
    /// </summary>
    public bool ClientWebGL { get; init; }

    /// <summary>
    /// 判断指定构建目标在当前 Unity 安装中是否可用。
    /// </summary>
    /// <param name="target">构建目标。</param>
    /// <returns>目标可用或无需 Unity 模块时返回 true。</returns>
    public bool IsAvailable(BuildTargetKind target)
    {
        return target switch
        {
            BuildTargetKind.ServerLinuxX64 => ServerLinuxX64,
            BuildTargetKind.ServerWindowsX64 => ServerWindowsX64,
            BuildTargetKind.ClientWindowsX64 => ClientWindowsX64,
            BuildTargetKind.ClientMacOS => ClientMacOS,
            BuildTargetKind.ClientAndroid => ClientAndroid,
            BuildTargetKind.ClientWebGL => ClientWebGL,
            BuildTargetKind.AuthenticationServer or BuildTargetKind.DatabaseServer => true,
            _ => false
        };
    }

    #endregion
}
