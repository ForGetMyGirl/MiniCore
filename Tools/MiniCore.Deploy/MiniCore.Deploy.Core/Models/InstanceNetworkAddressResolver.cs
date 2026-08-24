namespace MiniCore.Deploy.Core.Models;

/// <summary>
/// 解析实例覆盖值与主机级 VPC 地址形成的有效运行时地址。
/// </summary>
public static class InstanceNetworkAddressResolver
{
    #region Public 公共成员

    /// <summary>
    /// 解析实例最终向环境内其他服务公布的内网地址。
    /// </summary>
    /// <param name="hosts">当前环境登记的主机。</param>
    /// <param name="instance">待解析的实例。</param>
    /// <returns>实例覆盖值；未覆盖时返回所选主机的 VPC 地址；无法解析时返回空字符串。</returns>
    public static string ResolveInnerAdvertisedHost(
        IReadOnlyList<HostDefinition> hosts,
        InstanceDefinition instance)
    {
        ArgumentNullException.ThrowIfNull(hosts);
        ArgumentNullException.ThrowIfNull(instance);
        if (!string.IsNullOrWhiteSpace(instance.InnerAdvertisedHost))
        {
            return instance.InnerAdvertisedHost.Trim();
        }

        for (int index = 0; index < hosts.Count; index++)
        {
            HostDefinition host = hosts[index];
            if (string.Equals(host.HostId, instance.HostId, StringComparison.Ordinal))
            {
                return host.PrivateAddress.Trim();
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// 判断实例当前是否跟随所选主机的 VPC 地址。
    /// </summary>
    /// <param name="instance">待检查的实例。</param>
    /// <returns>实例没有显式覆盖内网公布地址时返回 true。</returns>
    public static bool UsesHostPrivateAddress(InstanceDefinition instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        return string.IsNullOrWhiteSpace(instance.InnerAdvertisedHost);
    }

    #endregion
}
