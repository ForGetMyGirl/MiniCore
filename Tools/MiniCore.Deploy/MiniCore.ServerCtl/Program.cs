using System.Net.Http.Headers;
using MiniCore.ServerCtl;

/// <summary>
/// 解析 ServerCtl 命令行并调用只监听回环地址的 Dedicated Server 管理端。
/// </summary>
/// <param name="args">--config、配置路径和操作名称。</param>
/// <returns>成功为零，未完成 Drain 为三，其他失败为一。</returns>
static async Task<int> MainAsync(string[] args)
{
    try
    {
        (string configPath, string operation) = ParseArguments(args);
        ServerControlConfiguration configuration = await ServerControlConfiguration.LoadAsync(configPath, CancellationToken.None).ConfigureAwait(false);
        string token = (await File.ReadAllTextAsync(configuration.Management.TokenFile).ConfigureAwait(false)).Trim();
        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidDataException("management.tokenFile 内容为空。");
        }

        using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{configuration.Management.Port}/"), Timeout = TimeSpan.FromSeconds(5) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        HttpResponseMessage response = operation switch
        {
            "status" => await client.GetAsync("v1/status").ConfigureAwait(false),
            "health" => await client.GetAsync("v1/health").ConfigureAwait(false),
            "drain" => await client.PostAsync("v1/drain", null).ConfigureAwait(false),
            "drain-status" => await client.GetAsync("v1/drain").ConfigureAwait(false),
            "shutdown" => await client.PostAsync("v1/shutdown", null).ConfigureAwait(false),
            _ => throw new ArgumentException($"未知 ServerCtl 操作：{operation}。")
        };
        string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        Console.WriteLine(content);
        if (!response.IsSuccessStatusCode)
        {
            return 1;
        }

        if (operation == "drain-status" && content.IndexOf("\"drained\":true", StringComparison.OrdinalIgnoreCase) < 0)
        {
            return 3;
        }

        return 0;
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine(exception.Message);
        return 1;
    }
}

/// <summary>
/// 读取必需的 --config 参数和最终操作名。
/// </summary>
/// <param name="args">命令行参数。</param>
/// <returns>配置路径和操作。</returns>
static (string ConfigPath, string Operation) ParseArguments(IReadOnlyList<string> args)
{
    string configPath = string.Empty;
    string operation = string.Empty;
    for (int index = 0; index < args.Count; index++)
    {
        if (string.Equals(args[index], "--config", StringComparison.Ordinal) && index + 1 < args.Count)
        {
            configPath = args[++index];
            continue;
        }

        operation = args[index];
    }

    if (string.IsNullOrWhiteSpace(configPath) || string.IsNullOrWhiteSpace(operation))
    {
        throw new ArgumentException("用法：MiniCore.ServerCtl --config <path> status|health|drain|drain-status|shutdown");
    }

    return (Path.GetFullPath(configPath), operation.ToLowerInvariant());
}

return await MainAsync(args).ConfigureAwait(false);
