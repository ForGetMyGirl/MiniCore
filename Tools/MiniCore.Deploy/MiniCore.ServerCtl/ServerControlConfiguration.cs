using System.Text.Json;

namespace MiniCore.ServerCtl;

/// <summary>
/// 保存 ServerCtl 从 Dedicated Server 外部配置读取的本地管理端信息。
/// </summary>
public sealed class ServerControlConfiguration
{
    #region Public 公共成员

    /// <summary>
    /// 获取或设置实例标识。
    /// </summary>
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置管理监听设置。
    /// </summary>
    public ServerManagementOptions Management { get; set; } = new();

    /// <summary>
    /// 加载并校验 Dedicated Server 外部配置。
    /// </summary>
    /// <param name="path">配置路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>管理端配置。</returns>
    public static async Task<ServerControlConfiguration> LoadAsync(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = File.OpenRead(path);
        ServerControlConfiguration? configuration = await JsonSerializer.DeserializeAsync<ServerControlConfiguration>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken).ConfigureAwait(false);
        if (configuration == null
            || configuration.Management.Port is <= 0 or > 65535
            || string.IsNullOrWhiteSpace(configuration.Management.TokenFile))
        {
            throw new InvalidDataException("Dedicated Server 外部配置缺少有效 management.port 或 management.tokenFile。");
        }

        return configuration;
    }

    #endregion
}
