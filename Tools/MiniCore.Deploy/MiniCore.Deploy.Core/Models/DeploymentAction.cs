namespace MiniCore.Deploy.Core.Models;

/// <summary>
/// 定义确定性发布状态机支持的原子操作。
/// </summary>
public enum DeploymentAction
{
    /// <summary>
    /// 检查本地项目、制品与目标主机。
    /// </summary>
    Preflight,

    /// <summary>
    /// 调用 Unity 与 .NET 构建器。
    /// </summary>
    Build,

    /// <summary>
    /// 上传并校验不可变制品。
    /// </summary>
    StageArtifact,

    /// <summary>
    /// 写入外部实例配置。
    /// </summary>
    WriteConfiguration,

    /// <summary>
    /// 安装或刷新进程服务定义。
    /// </summary>
    InstallService,

    /// <summary>
    /// 请求实例停止接收新工作。
    /// </summary>
    BeginDrain,

    /// <summary>
    /// 等待业务阻塞项清空。
    /// </summary>
    WaitForDrain,

    /// <summary>
    /// 停止目标实例。
    /// </summary>
    StopService,

    /// <summary>
    /// 原子切换实例引用的版本。
    /// </summary>
    ActivateRelease,

    /// <summary>
    /// 启动目标实例。
    /// </summary>
    StartService,

    /// <summary>
    /// 等待服务健康和 Coordinator 注册。
    /// </summary>
    WaitForHealth,

    /// <summary>
    /// 发布静态目录。
    /// </summary>
    PublishStaticContent,

    /// <summary>
    /// 校验并整理无需远程安装的桌面或移动客户端发布制品。
    /// </summary>
    PublishClientArtifact,

    /// <summary>
    /// 移除进程服务定义但保留日志与配置。
    /// </summary>
    UninstallService,

    /// <summary>
    /// 健康失败后恢复上一版本指针、配置和服务状态。
    /// </summary>
    AutomaticRollback,

    /// <summary>
    /// 在计划终态释放环境级远程互斥锁。
    /// </summary>
    ReleaseEnvironmentLock,

    /// <summary>
    /// 保存远程与本地发布状态。
    /// </summary>
    PersistState
}
