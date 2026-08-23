namespace MiniCore.Deploy.Core.Models;

/// <summary>
/// 定义 MiniCore Deploy 支持的构建目标。
/// </summary>
public enum BuildTargetKind
{
    /// <summary>
    /// Linux x64 Dedicated Server。
    /// </summary>
    ServerLinuxX64,

    /// <summary>
    /// Windows x64 Dedicated Server。
    /// </summary>
    ServerWindowsX64,

    /// <summary>
    /// Windows x64 客户端。
    /// </summary>
    ClientWindowsX64,

    /// <summary>
    /// macOS 通用客户端。
    /// </summary>
    ClientMacOS,

    /// <summary>
    /// Android 客户端。
    /// </summary>
    ClientAndroid,

    /// <summary>
    /// WebGL 客户端。
    /// </summary>
    ClientWebGL,

    /// <summary>
    /// 可选认证服务。
    /// </summary>
    AuthenticationServer,

    /// <summary>
    /// 可选数据库服务。
    /// </summary>
    DatabaseServer
}
