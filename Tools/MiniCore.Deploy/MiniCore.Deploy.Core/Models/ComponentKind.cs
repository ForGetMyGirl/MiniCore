namespace MiniCore.Deploy.Core.Models;

/// <summary>
/// 定义可由发布系统管理的组件种类。
/// </summary>
public enum ComponentKind
{
    /// <summary>
    /// Coordinator 控制面。
    /// </summary>
    Coordinator,

    /// <summary>
    /// 游戏 Dedicated Server。
    /// </summary>
    DedicatedServer,

    /// <summary>
    /// 可选认证服务。
    /// </summary>
    AuthenticationServer,

    /// <summary>
    /// 可选数据库服务。
    /// </summary>
    DatabaseServer,

    /// <summary>
    /// WebGL 或 YooAsset 静态内容。
    /// </summary>
    StaticContent
}
