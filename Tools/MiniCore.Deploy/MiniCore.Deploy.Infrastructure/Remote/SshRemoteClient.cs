using System.Security.Cryptography;
using MiniCore.Deploy.Core.Models;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace MiniCore.Deploy.Infrastructure.Remote;

/// <summary>
/// 通过固定主机指纹的 SSH/SFTP 连接执行发布动作。
/// </summary>
public sealed class SshRemoteClient
{
    #region Public 公共成员

    /// <summary>
    /// 使用当前认证方式和固定指纹分别建立 SSH 与 SFTP 连接。
    /// </summary>
    /// <param name="host">待验证主机。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>两种连接均完成握手后的任务。</returns>
    public Task TestConnectionAsync(HostDefinition host, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(host);
        return Task.Run(() =>
        {
            using (SshClient sshClient = CreateSshClient(host))
            {
                sshClient.Connect();
                sshClient.Disconnect();
            }

            using (SftpClient sftpClient = CreateSftpClient(host))
            {
                sftpClient.Connect();
                sftpClient.Disconnect();
            }
        }, cancellationToken);
    }

    /// <summary>
    /// 执行一条由发布后端生成的固定远程命令。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="commandText">固定命令文本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>命令结果。</returns>
    public Task<RemoteCommandResult> ExecuteAsync(
        HostDefinition host,
        string commandText,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandText);
        return Task.Run(() =>
        {
            using SshClient client = CreateSshClient(host);
            client.Connect();
            using SshCommand command = client.CreateCommand(commandText);
            string output = command.Execute();
            var result = new RemoteCommandResult(command.ExitStatus ?? -1, output ?? string.Empty, command.Error ?? string.Empty);
            client.Disconnect();
            return result;
        }, cancellationToken);
    }

    /// <summary>
    /// 通过 SFTP 上传一个本地文件并覆盖同名临时文件。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="localPath">本地文件。</param>
    /// <param name="remotePath">远程文件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>上传完成任务。</returns>
    public Task UploadFileAsync(
        HostDefinition host,
        string localPath,
        string remotePath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(host);
        return Task.Run(() =>
        {
            using SftpClient client = CreateSftpClient(host);
            client.Connect();
            EnsureRemoteDirectory(client, GetParentPath(remotePath));
            using FileStream stream = File.OpenRead(localPath);
            client.UploadFile(stream, remotePath, true);
            client.Disconnect();
        }, cancellationToken);
    }

    /// <summary>
    /// 将内存文本写入临时文件后通过 SFTP 上传。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="content">文本内容。</param>
    /// <param name="remotePath">远程文件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>上传完成任务。</returns>
    public async Task UploadTextAsync(
        HostDefinition host,
        string content,
        string remotePath,
        CancellationToken cancellationToken)
    {
        string temporaryPath = Path.Combine(Path.GetTempPath(), "minicore-deploy-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            await File.WriteAllTextAsync(temporaryPath, content, new System.Text.UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
            await UploadFileAsync(host, temporaryPath, remotePath, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 创建并配置主机指纹校验的 SSH 客户端。
    /// </summary>
    /// <param name="host">主机配置。</param>
    /// <returns>未连接客户端。</returns>
    private static SshClient CreateSshClient(HostDefinition host)
    {
        var client = new SshClient(CreateConnectionInfo(host));
        client.HostKeyReceived += (_, eventArgs) => eventArgs.CanTrust = IsExpectedHostKey(host.HostKeyFingerprint, eventArgs);
        return client;
    }

    /// <summary>
    /// 创建并配置主机指纹校验的 SFTP 客户端。
    /// </summary>
    /// <param name="host">主机配置。</param>
    /// <returns>未连接客户端。</returns>
    private static SftpClient CreateSftpClient(HostDefinition host)
    {
        var client = new SftpClient(CreateConnectionInfo(host));
        client.HostKeyReceived += (_, eventArgs) => eventArgs.CanTrust = IsExpectedHostKey(host.HostKeyFingerprint, eventArgs);
        return client;
    }

    /// <summary>
    /// 根据主机选择使用私钥或当前会话密码创建连接信息。
    /// </summary>
    /// <param name="host">主机配置。</param>
    /// <returns>SSH.NET 连接信息。</returns>
    private static ConnectionInfo CreateConnectionInfo(HostDefinition host)
    {
        AuthenticationMethod authentication;
        if (host.AuthenticationType == SshAuthenticationType.PrivateKey)
        {
            if (!File.Exists(host.PrivateKeyPath))
            {
                throw new FileNotFoundException($"主机 {host.HostId} 的 SSH 私钥不存在。", host.PrivateKeyPath);
            }

            var keyFile = new PrivateKeyFile(host.PrivateKeyPath);
            authentication = new PrivateKeyAuthenticationMethod(host.UserName, keyFile);
        }
        else
        {
            if (string.IsNullOrEmpty(host.Password))
            {
                throw new InvalidOperationException($"主机 {host.HostId} 尚未输入当前会话使用的 SSH 密码。");
            }

            authentication = new PasswordAuthenticationMethod(host.UserName, host.Password);
        }

        return new ConnectionInfo(host.Address, host.SshPort, host.UserName, authentication)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    /// <summary>
    /// 同时支持 OpenSSH SHA256 和十六进制指纹格式。
    /// </summary>
    /// <param name="expected">用户确认并保存的指纹。</param>
    /// <param name="eventArgs">SSH.NET 主机密钥事件。</param>
    /// <returns>密钥与固定指纹一致时返回 true。</returns>
    private static bool IsExpectedHostKey(string expected, HostKeyEventArgs eventArgs)
    {
        string normalizedExpected = expected.Trim();
        string sha256 = "SHA256:" + Convert.ToBase64String(SHA256.HashData(eventArgs.HostKey)).TrimEnd('=');
        if (string.Equals(normalizedExpected, sha256, StringComparison.Ordinal))
        {
            return true;
        }

        string hexadecimal = Convert.ToHexString(eventArgs.FingerPrint);
        string normalizedHexadecimal = normalizedExpected.Replace(":", string.Empty, StringComparison.Ordinal);
        return string.Equals(normalizedHexadecimal, hexadecimal, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 递归创建 SFTP 目录，不修改已有目录权限。
    /// </summary>
    /// <param name="client">已连接 SFTP 客户端。</param>
    /// <param name="path">目标目录。</param>
    private static void EnsureRemoteDirectory(SftpClient client, string path)
    {
        if (string.IsNullOrEmpty(path) || path == "/")
        {
            return;
        }

        string normalized = path.Replace('\\', '/');
        string[] parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        string current = normalized.StartsWith("/", StringComparison.Ordinal) ? "/" : string.Empty;
        for (int index = 0; index < parts.Length; index++)
        {
            current = current.Length == 0 || current == "/"
                ? current + parts[index]
                : current + "/" + parts[index];
            if (!client.Exists(current))
            {
                client.CreateDirectory(current);
            }
        }
    }

    /// <summary>
    /// 取得远程文件的父目录。
    /// </summary>
    /// <param name="path">远程文件路径。</param>
    /// <returns>父目录路径。</returns>
    private static string GetParentPath(string path)
    {
        string normalized = path.Replace('\\', '/');
        int separator = normalized.LastIndexOf('/');
        return separator <= 0 ? string.Empty : normalized[..separator];
    }

    #endregion
}
