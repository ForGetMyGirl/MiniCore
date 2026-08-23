using MiniCore.Deploy.ServiceHost;

/// <summary>
/// 创建可由 Windows SCM 托管的 MiniCore 子进程监督器。
/// </summary>
/// <param name="args">必须包含 --descriptor 与描述文件路径。</param>
/// <returns>进程退出码。</returns>
static async Task<int> MainAsync(string[] args)
{
    string descriptorPath = ServiceHostOptions.FindDescriptorPath(args);
    ServiceHostOptions options = await ServiceHostOptions.LoadAsync(descriptorPath, CancellationToken.None).ConfigureAwait(false);
    HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddWindowsService(serviceOptions => serviceOptions.ServiceName = "MiniCore Deploy Service Host");
    builder.Services.AddSingleton(options);
    builder.Services.AddHostedService<ChildProcessWorker>();
    using IHost host = builder.Build();
    await host.RunAsync().ConfigureAwait(false);
    return 0;
}

return await MainAsync(args).ConfigureAwait(false);
