namespace DatabaseServer;

/// <summary>
/// DatabaseServer 自身监听、Coordinator 和有界并发配置。
/// </summary>
public sealed class DatabaseServerOptions
{
    #region Public 公共成员

    /// <summary>
    /// 获取或设置集群内唯一实例标识。
    /// </summary>
    public string InstanceId { get; set; } = "Database-01";

    /// <summary>
    /// 获取或设置 Inner TCP 本地监听地址。
    /// </summary>
    public string ListenHost { get; set; } = "0.0.0.0";

    /// <summary>
    /// 获取或设置 Inner TCP 本地监听端口。
    /// </summary>
    public int ListenPort { get; set; } = 7300;

    /// <summary>
    /// 获取或设置向 Coordinator 公布的内网主机。
    /// </summary>
    public string AdvertisedHost { get; set; } = "127.0.0.1";

    /// <summary>
    /// 获取或设置 Coordinator 内网主机。
    /// </summary>
    public string CoordinatorHost { get; set; } = "127.0.0.1";

    /// <summary>
    /// 获取或设置 Coordinator Inner TCP 端口。
    /// </summary>
    public int CoordinatorPort { get; set; } = 7000;

    /// <summary>
    /// 获取或设置数据库 RPC 同时执行上限。
    /// </summary>
    public int MaximumConcurrency { get; set; } = 32;

    #endregion
}
