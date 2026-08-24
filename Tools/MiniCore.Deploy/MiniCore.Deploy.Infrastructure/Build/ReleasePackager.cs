using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
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
    /// <param name="releaseRoot">本轮隔离构建使用的暂存根目录。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>完整发布清单。</returns>
    public async Task<ReleaseManifest> CreateManifestAsync(
        DeploymentProfile profile,
        string sourceFingerprint,
        string releaseRoot,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        releaseRoot = Path.GetFullPath(releaseRoot);
        if (!Directory.Exists(releaseRoot))
        {
            throw new DirectoryNotFoundException($"发布输出目录不存在：{releaseRoot}。");
        }

        string artifactRoot = Path.Combine(releaseRoot, "Artifacts");
        Directory.CreateDirectory(artifactRoot);
        var manifest = new ReleaseManifest
        {
            ReleaseVersion = profile.Environment.ReleaseVersion,
            IsCompleteRelease = !profile.Project.ContentOnly,
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
                Length = stream.Length,
                UncompressedLength = GetDirectoryLength(sourcePath)
            });
        }

        manifest.ReleaseContentSha256 = ComputeReleaseContentSha256(manifest);
        string manifestPath = Path.Combine(releaseRoot, "ReleaseManifest.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest, JsonOptions), cancellationToken).ConfigureAwait(false);
        return manifest;
    }

    /// <summary>
    /// 校验暂存版本后以目录原子改名提交；已有同内容版本只复用，异内容版本直接拒绝。
    /// </summary>
    /// <param name="profile">发布配置。</param>
    /// <param name="stagingRoot">本轮隔离构建目录。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>最终不可变版本清单。</returns>
    public async Task<ReleaseManifest> CommitAsync(
        DeploymentProfile profile,
        string stagingRoot,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        stagingRoot = Path.GetFullPath(stagingRoot);
        string finalRoot = Path.GetFullPath(Path.Combine(profile.Project.OutputPath, profile.Environment.ReleaseVersion));
        ReleaseManifest stagedManifest = await LoadAndValidateAsync(stagingRoot, false, cancellationToken).ConfigureAwait(false);
        if (Directory.Exists(finalRoot))
        {
            ReleaseManifest existingManifest;
            try
            {
                existingManifest = await LoadAndValidateAsync(finalRoot, false, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or JsonException)
            {
                throw new InvalidOperationException($"版本目录 {finalRoot} 已存在但不是有效不可变 Release，禁止覆盖。请更换 ReleaseVersion 或人工审查旧目录。", exception);
            }

            if (!string.Equals(existingManifest.ReleaseContentSha256, stagedManifest.ReleaseContentSha256, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"ReleaseVersion {stagedManifest.ReleaseVersion} 已存在但内容 SHA-256 不同，禁止用同版本覆盖已有制品。");
            }

            Directory.Delete(stagingRoot, true);
            return existingManifest;
        }

        Directory.Move(stagingRoot, finalRoot);
        return stagedManifest;
    }

    /// <summary>
    /// 从指定版本目录加载清单，并重新校验所有本地制品的大小、压缩内容大小和 SHA-256。
    /// </summary>
    /// <param name="releaseRoot">本地版本目录。</param>
    /// <param name="requireCompleteRelease">是否要求清单可直接激活。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>验证通过的清单。</returns>
    public async Task<ReleaseManifest> LoadAndValidateAsync(
        string releaseRoot,
        bool requireCompleteRelease,
        CancellationToken cancellationToken)
    {
        releaseRoot = Path.GetFullPath(releaseRoot);
        string manifestPath = Path.Combine(releaseRoot, "ReleaseManifest.json");
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("发布目录缺少 ReleaseManifest.json。", manifestPath);
        }

        await using FileStream manifestStream = File.OpenRead(manifestPath);
        ReleaseManifest manifest = await JsonSerializer.DeserializeAsync<ReleaseManifest>(manifestStream, JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("ReleaseManifest 不是有效 JSON 对象。");
        ValidateManifestStructure(manifest);
        if (requireCompleteRelease && !manifest.IsCompleteRelease)
        {
            throw new InvalidDataException("当前清单只包含内容增量，不是可直接激活的完整 Release。");
        }

        if (string.IsNullOrWhiteSpace(manifest.ReleaseContentSha256)
            || !string.Equals(manifest.ReleaseContentSha256, ComputeReleaseContentSha256(manifest), StringComparison.Ordinal))
        {
            throw new InvalidDataException("ReleaseManifest 的确定性内容摘要缺失或不匹配。");
        }

        string rootPrefix = releaseRoot.EndsWith(Path.DirectorySeparatorChar)
            ? releaseRoot
            : releaseRoot + Path.DirectorySeparatorChar;
        for (int index = 0; index < manifest.Artifacts.Count; index++)
        {
            ReleaseArtifact artifact = manifest.Artifacts[index];
            string artifactPath = Path.GetFullPath(Path.Combine(releaseRoot, artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!artifactPath.StartsWith(rootPrefix, StringComparison.Ordinal) || !File.Exists(artifactPath))
            {
                throw new InvalidDataException($"制品 {artifact.Target} 的相对路径越界或文件不存在。");
            }

            var fileInfo = new FileInfo(artifactPath);
            if (fileInfo.Length != artifact.Length)
            {
                throw new InvalidDataException($"制品 {artifact.Target} 的文件大小已变化，禁止上传。");
            }

            await using FileStream artifactStream = File.OpenRead(artifactPath);
            string actualSha256 = Convert.ToHexStringLower(await SHA256.HashDataAsync(artifactStream, cancellationToken).ConfigureAwait(false));
            if (!string.Equals(actualSha256, artifact.Sha256, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"制品 {artifact.Target} 的 SHA-256 已变化，禁止上传。");
            }

            long actualUncompressedLength = GetArchiveUncompressedLength(artifactPath);
            if (actualUncompressedLength != artifact.UncompressedLength)
            {
                throw new InvalidDataException($"制品 {artifact.Target} 的解压大小与清单不一致，禁止上传。");
            }
        }

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
    /// 计算目录内全部文件的未压缩总字节数。
    /// </summary>
    /// <param name="directoryPath">目标目录。</param>
    /// <returns>文件总字节数。</returns>
    private static long GetDirectoryLength(string directoryPath)
    {
        long length = 0;
        string[] files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
        for (int index = 0; index < files.Length; index++)
        {
            length = checked(length + new FileInfo(files[index]).Length);
        }

        return length;
    }

    /// <summary>
    /// 读取 ZIP 中所有文件项的未压缩总字节数。
    /// </summary>
    /// <param name="archivePath">ZIP 路径。</param>
    /// <returns>文件项总字节数。</returns>
    private static long GetArchiveUncompressedLength(string archivePath)
    {
        long length = 0;
        var entryPaths = new HashSet<string>(StringComparer.Ordinal);
        using ZipArchive archive = ZipFile.OpenRead(archivePath);
        for (int index = 0; index < archive.Entries.Count; index++)
        {
            ZipArchiveEntry entry = archive.Entries[index];
            string normalizedPath = entry.FullName.Replace('\\', '/');
            string[] segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (normalizedPath.StartsWith("/", StringComparison.Ordinal)
                || (normalizedPath.Length >= 2 && normalizedPath[1] == ':')
                || segments.Any(static segment => string.Equals(segment, "..", StringComparison.Ordinal))
                || !entryPaths.Add(normalizedPath))
            {
                throw new InvalidDataException($"制品压缩包包含越界或重复路径：{entry.FullName}。");
            }

            length = checked(length + entry.Length);
        }

        return length;
    }

    /// <summary>
    /// 校验清单基础字段、制品标识和相对路径唯一性。
    /// </summary>
    /// <param name="manifest">待校验发布清单。</param>
    private static void ValidateManifestStructure(ReleaseManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest.ReleaseVersion) || manifest.Artifacts.Count == 0)
        {
            throw new InvalidDataException("ReleaseManifest 缺少发布版本或制品列表为空。");
        }

        var targets = new HashSet<BuildTargetKind>();
        var relativePaths = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < manifest.Artifacts.Count; index++)
        {
            ReleaseArtifact artifact = manifest.Artifacts[index];
            string normalizedPath = artifact.RelativePath.Replace('\\', '/');
            string[] segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            bool invalidPath = string.IsNullOrWhiteSpace(normalizedPath)
                || normalizedPath.StartsWith("/", StringComparison.Ordinal)
                || (normalizedPath.Length >= 2 && normalizedPath[1] == ':')
                || segments.Any(static segment => string.Equals(segment, "..", StringComparison.Ordinal));
            if (invalidPath
                || !targets.Add(artifact.Target)
                || !relativePaths.Add(normalizedPath)
                || artifact.Length <= 0
                || artifact.UncompressedLength <= 0
                || artifact.Sha256.Length != 64
                || !artifact.Sha256.All(static character => Uri.IsHexDigit(character)))
            {
                throw new InvalidDataException($"ReleaseManifest 中制品 {artifact.Target} 的标识、路径、大小或 SHA-256 无效或重复。");
            }
        }
    }

    /// <summary>
    /// 只使用兼容字段和排序后的制品元数据计算稳定发布内容摘要。
    /// </summary>
    /// <param name="manifest">待摘要清单。</param>
    /// <returns>小写 SHA-256。</returns>
    private static string ComputeReleaseContentSha256(ReleaseManifest manifest)
    {
        var builder = new StringBuilder(512);
        builder.Append(manifest.ReleaseVersion).Append('\n')
            .Append(manifest.IsCompleteRelease ? '1' : '0').Append('\n')
            .Append(manifest.ControlProtocolVersion).Append('\n')
            .Append(manifest.DatabaseMigrationFingerprint).Append('\n');
        ReleaseArtifact[] artifacts = manifest.Artifacts
            .OrderBy(static artifact => artifact.Target)
            .ThenBy(static artifact => artifact.RelativePath, StringComparer.Ordinal)
            .ToArray();
        for (int index = 0; index < artifacts.Length; index++)
        {
            ReleaseArtifact artifact = artifacts[index];
            builder.Append(artifact.Target).Append('|')
                .Append(artifact.RelativePath).Append('|')
                .Append(artifact.Sha256).Append('|')
                .Append(artifact.Length).Append('|')
                .Append(artifact.UncompressedLength).Append('\n');
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
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
