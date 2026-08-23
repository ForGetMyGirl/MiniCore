using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MiniCore.Deploy.Core.Execution;
using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Infrastructure.Persistence;

/// <summary>
/// 使用每计划一份 JSONL 文件保存可恢复步骤结果。
/// </summary>
public sealed class JsonExecutionJournal : IExecutionJournal
{
    #region Private 私有成员

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions(); // 稳定日志格式。
    private readonly ApplicationPaths paths; // 历史目录。
    private readonly SemaphoreSlim writeLock = new(1, 1); // 保证同一进程追加完整行。

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 创建 JSONL 执行日志。
    /// </summary>
    /// <param name="paths">应用路径。</param>
    public JsonExecutionJournal(ApplicationPaths paths)
    {
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StepResult>> LoadAsync(string planId, CancellationToken cancellationToken)
    {
        string path = GetPath(planId);
        var results = new List<StepResult>();
        if (!File.Exists(path))
        {
            return results;
        }

        using var reader = new StreamReader(path, Encoding.UTF8);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line == null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            StepResult? result = JsonSerializer.Deserialize<StepResult>(line, JsonOptions);
            if (result != null)
            {
                results.Add(result);
            }
        }

        return results;
    }

    /// <inheritdoc />
    public async Task AppendAsync(string planId, StepResult result, CancellationToken cancellationToken)
    {
        string path = GetPath(planId);
        string line = JsonSerializer.Serialize(result, JsonOptions);
        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await File.AppendAllTextAsync(path, line + Environment.NewLine, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 校验计划标识并返回日志路径。
    /// </summary>
    /// <param name="planId">计划标识。</param>
    /// <returns>JSONL 文件路径。</returns>
    private string GetPath(string planId)
    {
        if (string.IsNullOrWhiteSpace(planId) || planId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("计划标识包含无效字符。", nameof(planId));
        }

        return Path.Combine(paths.HistoryPath, planId + ".jsonl");
    }

    /// <summary>
    /// 创建执行历史 JSON 设置。
    /// </summary>
    /// <returns>JSON 设置。</returns>
    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    #endregion
}
