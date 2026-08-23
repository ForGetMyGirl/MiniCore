using System.Security.Cryptography;
using MiniCore.Deploy.Core.Models;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace MiniCore.Deploy.Infrastructure.Remote;

/// <summary>
/// 在正式认证前读取目标 SSH 服务展示的主机公钥指纹。
/// </summary>
public sealed class SshHostKeyProbe
{
    #region Public 公共成员

    /// <summary>
    /// 获取目标主机当前展示的 OpenSSH SHA-256 指纹。
    /// </summary>
    /// <param name="host">包含地址、端口和可选用户名的主机配置。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>形如 SHA256:xxxx 的主机密钥指纹。</returns>
    public Task<string> GetFingerprintAsync(HostDefinition host, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(host);
        if (string.IsNullOrWhiteSpace(host.Address) || host.SshPort is <= 0 or > 65535)
        {
            throw new ArgumentException("获取主机指纹前必须填写有效的 SSH 地址和端口。", nameof(host));
        }

        return Task.Run(() => Probe(host), cancellationToken);
    }

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 建立只用于密钥交换的连接并捕获主机指纹。
    /// </summary>
    /// <param name="host">目标主机配置。</param>
    /// <returns>目标主机展示的 SHA-256 指纹。</returns>
    private static string Probe(HostDefinition host)
    {
        string userName = string.IsNullOrWhiteSpace(host.UserName) ? "root" : host.UserName;
        var connection = new ConnectionInfo(
            host.Address,
            host.SshPort,
            userName,
            new NoneAuthenticationMethod(userName))
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        using var client = new SshClient(connection);
        string fingerprint = string.Empty;
        client.HostKeyReceived += (_, eventArgs) =>
        {
            fingerprint = BuildSha256Fingerprint(eventArgs);
            eventArgs.CanTrust = true;
        };

        try
        {
            client.Connect();
        }
        catch (SshAuthenticationException) when (!string.IsNullOrEmpty(fingerprint))
        {
            // 主机密钥交换发生在用户认证之前；无认证方法失败不影响已取得的指纹。
        }
        finally
        {
            if (client.IsConnected)
            {
                client.Disconnect();
            }
        }

        if (string.IsNullOrEmpty(fingerprint))
        {
            throw new InvalidOperationException("目标 SSH 服务没有返回可确认的主机密钥指纹。");
        }

        return fingerprint;
    }

    /// <summary>
    /// 将 SSH.NET 主机公钥转换为 OpenSSH SHA-256 指纹格式。
    /// </summary>
    /// <param name="eventArgs">主机密钥事件。</param>
    /// <returns>OpenSSH SHA-256 指纹。</returns>
    private static string BuildSha256Fingerprint(HostKeyEventArgs eventArgs)
    {
        return "SHA256:" + Convert.ToBase64String(SHA256.HashData(eventArgs.HostKey)).TrimEnd('=');
    }

    #endregion
}
