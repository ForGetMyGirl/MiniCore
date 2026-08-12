using YooAsset;

/// <summary>
/// 为 YooAsset 提供主、备用资源服务器地址。
/// </summary>
internal sealed class RemoteServices : IRemoteServices
{
    #region Private 私有成员

    private readonly string resourcesServerUrl; // 主资源服务器地址。
    private readonly string fallbackServerUrl; // 备用资源服务器地址。

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 使用主、备用服务器地址创建 YooAsset 远端服务。
    /// </summary>
    /// <param name="resourcesServerUrl">主资源服务器地址。</param>
    /// <param name="fallbackServerUrl">备用资源服务器地址。</param>
    public RemoteServices(string resourcesServerUrl, string fallbackServerUrl)
    {
        this.resourcesServerUrl = resourcesServerUrl;
        this.fallbackServerUrl = fallbackServerUrl;
    }

    #endregion

    #region Interface 接口实现

    /// <summary>
    /// 获取指定资源文件的备用下载地址。
    /// </summary>
    /// <param name="fileName">资源文件名。</param>
    /// <returns>备用服务器中的资源完整地址。</returns>
    string IRemoteServices.GetRemoteFallbackURL(string fileName)
    {
        return $"{fallbackServerUrl}/{fileName}";
    }

    /// <summary>
    /// 获取指定资源文件的主下载地址。
    /// </summary>
    /// <param name="fileName">资源文件名。</param>
    /// <returns>主服务器中的资源完整地址。</returns>
    string IRemoteServices.GetRemoteMainURL(string fileName)
    {
        return $"{resourcesServerUrl}/{fileName}";
    }

    #endregion
}
