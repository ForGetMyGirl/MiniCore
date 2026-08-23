using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Infrastructure.Persistence;

/// <summary>
/// 在仓库外保存和恢复已经预览的发布计划。
/// </summary>
public sealed class DeploymentPlanStore
{
    #region Private 私有成员

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions(); // 稳定计划格式。
    private readonly ApplicationPaths paths; // 仓库外应用路径。

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 创建发布计划快照存储。
    /// </summary>
    /// <param name="paths">仓库外应用路径。</param>
    public DeploymentPlanStore(ApplicationPaths paths)
    {
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    /// <summary>
    /// 保存已经展示的计划及其配置指纹。
    /// </summary>
    /// <param name="plan">发布计划。</param>
    /// <param name="profileFingerprint">完整配置指纹。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>保存完成任务。</returns>
    public Task SaveAsync(DeploymentPlan plan, string profileFingerprint, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var snapshot = new DeploymentPlanSnapshot
        {
            Plan = plan,
            ProfileFingerprint = profileFingerprint,
            SavedAtUtc = DateTimeOffset.UtcNow
        };
        string path = Path.Combine(paths.PlansPath, plan.PlanId + ".json");
        string json = JsonSerializer.Serialize(snapshot, JsonOptions);
        return File.WriteAllTextAsync(path, json, new UTF8Encoding(false), cancellationToken);
    }

    /// <summary>
    /// 查找与当前配置指纹相同的最近计划，以便应用重启后继续。
    /// </summary>
    /// <param name="profileFingerprint">当前完整配置指纹。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>没有匹配快照时返回 null。</returns>
    public async Task<DeploymentPlan?> LoadLatestMatchingAsync(string profileFingerprint, CancellationToken cancellationToken)
    {
        string[] files = Directory.GetFiles(paths.PlansPath, "*.json", SearchOption.TopDirectoryOnly);
        Array.Sort(files, static (left, right) => File.GetLastWriteTimeUtc(right).CompareTo(File.GetLastWriteTimeUtc(left)));
        for (int index = 0; index < files.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using FileStream stream = File.OpenRead(files[index]);
            DeploymentPlanSnapshot? snapshot = await JsonSerializer.DeserializeAsync<DeploymentPlanSnapshot>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
            if (snapshot != null && string.Equals(snapshot.ProfileFingerprint, profileFingerprint, StringComparison.Ordinal))
            {
                return snapshot.Plan;
            }
        }

        return null;
    }

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 创建可读且支持枚举名称的计划 JSON 设置。
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
