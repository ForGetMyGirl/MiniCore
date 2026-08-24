namespace MiniCore.Deploy.Core.Models;

/// <summary>
/// 描述一个使用独立配置运行的服务实例。
/// </summary>
public sealed class InstanceDefinition
{
    #region Public 公共成员

    /// <summary>
    /// 获取或设置环境内唯一实例标识。
    /// </summary>
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置承载实例的主机标识。
    /// </summary>
    public string HostId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置组件种类。
    /// </summary>
    public ComponentKind Component { get; set; } = ComponentKind.DedicatedServer;

    /// <summary>
    /// 获取或设置稳定 Role 标识列表。
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// 获取或设置进程在目标主机上的内网监听地址。
    /// </summary>
    public string InnerListenHost { get; set; } = "0.0.0.0";

    /// <summary>
    /// 获取或设置实例级内网公布地址覆盖；留空时继承所选主机的 VPC 地址。
    /// </summary>
    public string InnerAdvertisedHost { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置服务间监听端口。
    /// </summary>
    public int InnerPort { get; set; }

    /// <summary>
    /// 获取或设置 Dedicated Server 的外网监听地址。
    /// </summary>
    public string OuterListenHost { get; set; } = "0.0.0.0";

    /// <summary>
    /// 获取或设置客户端 WebSocket 监听端口。
    /// </summary>
    public int OuterPort { get; set; }

    /// <summary>
    /// 获取或设置 DS WebSocket、Auth HTTP 或静态内容对外公布的完整访问地址。
    /// </summary>
    public string OuterAdvertisedUrl { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Dedicated Server 外网 WebSocket 监听路径。
    /// </summary>
    public string OuterPath { get; set; } = "/minicore";

    /// <summary>
    /// 获取或设置只监听回环地址的管理端口。
    /// </summary>
    public int ManagementPort { get; set; }

    /// <summary>
    /// 获取或设置当前 Dedicated Server 是否要求发现可用 DatabaseServer。
    /// </summary>
    public bool RequiresDatabase { get; set; }

    /// <summary>
    /// 获取或设置 DatabaseServer 同时处理 RPC 的上限。
    /// </summary>
    public int MaximumConcurrency { get; set; } = 32;

    /// <summary>
    /// 获取或设置 StaticContent 在目标主机上的原子版本指针绝对路径。
    /// </summary>
    public string StaticContentPublishPath { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Auth 账号库或 DB 游戏库的连接参数。
    /// </summary>
    public DatabaseConnectionDefinition Database { get; set; } = new();

    /// <summary>
    /// 获取或设置是否在异常退出后自动重新启动。
    /// </summary>
    public bool AutoRestart { get; set; } = true;

    /// <summary>
    /// 获取或设置该实例是否参与当前期望拓扑。
    /// </summary>
    public bool Enabled { get; set; } = true;

    #endregion
}
