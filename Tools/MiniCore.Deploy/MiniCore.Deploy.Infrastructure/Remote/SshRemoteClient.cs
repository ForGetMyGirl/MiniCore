using System.Security.Cryptography;
using System.Text;
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
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() =>
        {
            try
            {
                using (SshClient sshClient = CreateSshClient(host))
                using (cancellationToken.Register(sshClient.Dispose))
                {
                    sshClient.Connect();
                    sshClient.Disconnect();
                }

                using SftpClient sftpClient = CreateSftpClient(host);
                using CancellationTokenRegistration registration = cancellationToken.Register(sftpClient.Dispose);
                sftpClient.Connect();
                sftpClient.Disconnect();
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }, CancellationToken.None);
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
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() =>
        {
            try
            {
                using SshClient client = CreateSshClient(host);
                using CancellationTokenRegistration registration = cancellationToken.Register(client.Dispose);
                client.Connect();
                using SshCommand command = client.CreateCommand(commandText);
                command.CommandTimeout = TimeSpan.FromMinutes(10);
                string output = command.Execute();
                var result = new RemoteCommandResult(command.ExitStatus ?? -1, output ?? string.Empty, command.Error ?? string.Empty);
                client.Disconnect();
                return result;
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }, CancellationToken.None);
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
        cancellationToken.ThrowIfCancellationRequested();
        return UploadLocalFileAtomicAsync(host, localPath, remotePath, cancellationToken);
    }

    /// <summary>
    /// 直接从内存通过 SFTP 原子上传普通文本，不在本机生成明文临时文件。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="content">文本内容。</param>
    /// <param name="remotePath">远程文件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>上传完成任务。</returns>
    public Task UploadTextAsync(
        HostDefinition host,
        string content,
        string remotePath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(content);
        byte[] bytes = new UTF8Encoding(false).GetBytes(content);
        return UploadBytesAtomicAsync(host, bytes, remotePath, false, cancellationToken);
    }

    /// <summary>
    /// 直接从内存上传敏感文本，Linux 临时文件在写入内容前即限制为 0600。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="content">敏感文本内容。</param>
    /// <param name="remotePath">远程目标文件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>上传和原子改名完成任务。</returns>
    public Task UploadSensitiveTextAsync(
        HostDefinition host,
        string content,
        string remotePath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(content);
        byte[] bytes = new UTF8Encoding(false).GetBytes(content);
        return UploadBytesAtomicAsync(host, bytes, remotePath, true, cancellationToken);
    }

    #endregion

    #region Private 私有成员

    private const short SshNetSensitiveFilePermissionMode = 600; // SSH.NET 接收八进制数字的十进制写法，不是 POSIX 位掩码。

    /// <summary>
    /// 打开本地文件并通过可取消的 .part 文件上传，成功后原子改名。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="localPath">本地文件。</param>
    /// <param name="remotePath">远程目标文件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>上传完成任务。</returns>
    private static Task UploadLocalFileAtomicAsync(
        HostDefinition host,
        string localPath,
        string remotePath,
        CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            using FileStream stream = File.OpenRead(localPath);
            UploadStreamAtomic(host, stream, remotePath, false, cancellationToken);
        }, CancellationToken.None);
    }

    /// <summary>
    /// 使用内存字节流执行可取消原子上传。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="bytes">待上传字节。</param>
    /// <param name="remotePath">远程目标文件。</param>
    /// <param name="sensitive">是否在写入前设置 Linux 0600 权限。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>上传完成任务。</returns>
    private static Task UploadBytesAtomicAsync(
        HostDefinition host,
        byte[] bytes,
        string remotePath,
        bool sensitive,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() =>
        {
            using var stream = new MemoryStream(bytes, false);
            UploadStreamAtomic(host, stream, remotePath, sensitive, cancellationToken);
        }, CancellationToken.None);
    }

    /// <summary>
    /// 在单个 SFTP 会话中上传 .part、响应取消、清理半包并原子替换目标。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="stream">已打开的数据流。</param>
    /// <param name="remotePath">远程目标文件。</param>
    /// <param name="sensitive">是否启用敏感文件权限。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    private static void UploadStreamAtomic(
        HostDefinition host,
        Stream stream,
        string remotePath,
        bool sensitive,
        CancellationToken cancellationToken)
    {
        string partPath = remotePath + ".part";
        bool sensitiveTargetNeedsCleanup = false;
        using SftpClient client = CreateSftpClient(host);
        try
        {
            using CancellationTokenRegistration registration = cancellationToken.Register(client.Dispose);
            client.Connect();
            EnsureRemoteDirectory(client, GetParentPath(remotePath));
            if (client.Exists(partPath))
            {
                client.DeleteFile(partPath);
            }

            if (sensitive && host.OperatingSystem == HostOperatingSystem.Linux)
            {
                using (Stream empty = client.Create(partPath))
                {
                }

                SetAndVerifySensitiveFilePermissions(client, partPath);
            }

            client.UploadFile(
                stream,
                partPath,
                true,
                _ => cancellationToken.ThrowIfCancellationRequested());
            if (sensitive && host.OperatingSystem == HostOperatingSystem.Linux)
            {
                SetAndVerifySensitiveFilePermissions(client, partPath);
            }

            client.RenameFile(partPath, remotePath, true);
            if (sensitive && host.OperatingSystem == HostOperatingSystem.Linux)
            {
                sensitiveTargetNeedsCleanup = true;
                SetAndVerifySensitiveFilePermissions(client, remotePath);
                sensitiveTargetNeedsCleanup = false;
            }

            client.Disconnect();
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            TryDeleteRemoteFile(client, host, partPath);
            if (sensitiveTargetNeedsCleanup)
            {
                TryDeleteRemoteFile(client, host, remotePath);
            }

            throw new OperationCanceledException(cancellationToken);
        }
        catch
        {
            TryDeleteRemoteFile(client, host, partPath);
            if (sensitiveTargetNeedsCleanup)
            {
                TryDeleteRemoteFile(client, host, remotePath);
            }

            throw;
        }
    }

    /// <summary>
    /// 使用 SSH.NET 规定的八进制数字写法设置 0600，并核验服务器返回的最终权限。
    /// </summary>
    /// <param name="client">已连接的 SFTP 客户端。</param>
    /// <param name="remotePath">敏感文件远程路径。</param>
    private static void SetAndVerifySensitiveFilePermissions(SftpClient client, string remotePath)
    {
        client.ChangePermissions(remotePath, SshNetSensitiveFilePermissionMode);
        var attributes = client.GetAttributes(remotePath);
        bool isOwnerReadWriteOnly = attributes.IsRegularFile
            && attributes.OwnerCanRead
            && attributes.OwnerCanWrite
            && !attributes.OwnerCanExecute
            && !attributes.GroupCanRead
            && !attributes.GroupCanWrite
            && !attributes.GroupCanExecute
            && !attributes.OthersCanRead
            && !attributes.OthersCanWrite
            && !attributes.OthersCanExecute
            && !attributes.IsUIDBitSet
            && !attributes.IsGroupIDBitSet
            && !attributes.IsStickyBitSet;
        if (!isOwnerReadWriteOnly)
        {
            throw new InvalidOperationException("远程敏感配置文件权限未收敛到 0600，已拒绝继续上传。");
        }
    }

    /// <summary>
    /// 尽力删除上传失败留下的确定路径文件，清理失败不掩盖原始异常。
    /// </summary>
    /// <param name="client">当前 SFTP 客户端。</param>
    /// <param name="host">目标主机。</param>
    /// <param name="remotePath">需要清理的远程文件路径。</param>
    private static void TryDeleteRemoteFile(SftpClient client, HostDefinition host, string remotePath)
    {
        try
        {
            if (client.IsConnected && client.Exists(remotePath))
            {
                client.DeleteFile(remotePath);
                return;
            }

            using SftpClient cleanupClient = CreateSftpClient(host);
            cleanupClient.Connect();
            if (cleanupClient.Exists(remotePath))
            {
                cleanupClient.DeleteFile(remotePath);
            }

            cleanupClient.Disconnect();
        }
        catch
        {
            // 清理失败由后续修复或同路径重试覆盖，不掩盖取消或上传异常。
        }
    }

    /// <summary>
    /// 创建并配置主机指纹校验的 SSH 客户端。
    /// </summary>
    /// <param name="host">主机配置。</param>
    /// <returns>未连接客户端。</returns>
    private static SshClient CreateSshClient(HostDefinition host)
    {
        var client = new SshClient(CreateConnectionInfo(host));
        client.KeepAliveInterval = TimeSpan.FromSeconds(15);
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
        client.KeepAliveInterval = TimeSpan.FromSeconds(15);
        client.OperationTimeout = TimeSpan.FromMinutes(5);
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

            var keyFile = string.IsNullOrEmpty(host.PrivateKeyPassphrase)
                ? new PrivateKeyFile(host.PrivateKeyPath)
                : new PrivateKeyFile(host.PrivateKeyPath, host.PrivateKeyPassphrase);
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
