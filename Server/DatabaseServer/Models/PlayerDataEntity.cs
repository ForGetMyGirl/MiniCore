namespace DatabaseServer.Models;

/// <summary>
/// 表示 DatabaseServer 管理的一条玩家业务数据。
/// </summary>
public sealed class PlayerDataEntity
{
    #region Public 公共成员

    /// <summary>
    /// 获取或设置玩家主键。
    /// </summary>
    public long PlayerId { get; set; }

    /// <summary>
    /// 获取或设置玩家显示名。
    /// </summary>
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置乐观并发修订号。
    /// </summary>
    public long Revision { get; set; }

    /// <summary>
    /// 获取或设置由具体游戏解释的 Protobuf 业务载荷。
    /// </summary>
    public byte[] Payload { get; set; } = [];

    #endregion
}
