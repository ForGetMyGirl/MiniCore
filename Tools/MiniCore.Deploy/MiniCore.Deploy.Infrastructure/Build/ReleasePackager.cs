using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Infrastructure.Build;

/// <summary>
/// 将构建目录压缩为不可变制品并生成 SHA-256 发布清单。
/// </summary>
public sealed class ReleasePackager
{
    #region Private 私有成员

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions(); // 发布清单格式。

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 压缩已知构建目录并写出 ReleaseManifest.json。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="sourceFingerprint">源码指纹。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>完整发布清单。</returns>
    public async Task<ReleaseManifest> CreateManifestAsync(
        DeploymentProfile profile,
        string sourceFingerprint,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        string releaseRoot = Path.GetFullPath(Path.Combine(profile.Project.OutputPath, profile.Environment.ReleaseVersion));
        if (!Directory.Exists(releaseRoot))
        {
            throw new DirectoryNotFoundException($"发布输出目录不存在：{releaseRoot}。");
        }

        string artifactRoot = Path.Combine(releaseRoot, "Artifacts");
        Directory.CreateDirectory(artifactRoot);
        var manifest = new ReleaseManifest
        {
            ReleaseVersion = profile.Environment.ReleaseVersion,
            ControlProtocolVersion = "1",
            SourceFingerprint = sourceFingerprint,
            DatabaseMigrationFingerprint = await ComputeDatabaseMigrationFingerprintAsync(profile.Project.ProjectPath, cancellationToken).ConfigureAwait(false),
            DatabaseMigrationReviewedReleaseVersion = profile.Environment.DatabaseMigrationReviewedReleaseVersion,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        for (int index = 0; index < profile.Project.BuildTargets.Count; index++)
        {
            BuildTargetKind target = profile.Project.BuildTargets[index];
            string sourcePath = GetTargetOutputPath(releaseRoot, target);
            if (!Directory.Exists(sourcePath))
            {
                throw new DirectoryNotFoundException($"构建目标 {target} 未产生输出目录：{sourcePath}。");
            }

            string archivePath = Path.Combine(artifactRoot, target + ".zip");
            if (File.Exists(archivePath))
            {
                File.Delete(archivePath);
            }

            ZipFile.CreateFromDirectory(sourcePath, archivePath, CompressionLevel.SmallestSize, false);
            await using FileStream stream = File.OpenRead(archivePath);
            string hash = Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));
            manifest.Artifacts.Add(new ReleaseArtifact
            {
                Target = target,
                RelativePath = Path.GetRelativePath(releaseRoot, archivePath).Replace('\\', '/'),
                Sha256 = hash,
                Length = stream.Length
            });
        }

        string manifestPath = Path.Combine(releaseRoot, "ReleaseManifest.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest, JsonOptions), cancellationToken).ConfigureAwait(false);
        return manifest;
    }

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 返回构建桥接为目标约定的输出目录。
    /// </summary>
    /// <param name="releaseRoot">发布根目录。</param>
    /// <param name="target">目标。</param>
    /// <returns>待压缩目录。</returns>
    private static string GetTargetOutputPath(string releaseRoot, BuildTargetKind target)
    {
        return target switch
        {
            BuildTargetKind.AuthenticationServer => Path.Combine(releaseRoot, "DotNet", "AuthenticationServer"),
            BuildTargetKind.DatabaseServer => Path.Combine(releaseRoot, "DotNet", "DatabaseServer"),
            _ => Path.Combine(releaseRoot, target.ToString())
        };
    }

    /// <summary>
    /// 计算可选 Auth/DB 迁移源码的稳定指纹，供发布审计和后续版本差异判断。
    /// </summary>
    /// <param name="projectPath">仓库根目录。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>没有迁移文件时为空，否则返回小写 SHA-256。</returns>
    private static async Task<string> ComputeDatabaseMigrationFingerprintAsync(string projectPath, CancellationToken cancellationToken)
    {
        string serverPath = Path.Combine(projectPath, "Server");
        if (!Directory.Exists(serverPath))
        {
            return string.Empty;
        }

        string[] files = Directory.GetFiles(serverPath, "*.cs", SearchOption.AllDirectories)
            .Where(static path => path.Contains(Path.DirectorySeparatorChar + "Migrations" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();
        if (files.Length == 0)
        {
            return string.Empty;
        }

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        for (int index = 0; index < files.Length; index++)
        {
            string relativePath = Path.GetRelativePath(projectPath, files[index]).Replace('\\', '/');
            hash.AppendData(System.Text.Encoding.UTF8.GetBytes(relativePath));
            await using FileStream stream = File.OpenRead(files[index]);
            byte[] buffer = new byte[81920];
            int count;
            while ((count = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                hash.AppendData(buffer.AsSpan(0, count));
            }
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    /// <summary>
    /// 创建可读且稳定的发布清单 JSON 设置。
    /// </summary>
    /// <returns>JSON 设置。</returns>
    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    #endregion
}
