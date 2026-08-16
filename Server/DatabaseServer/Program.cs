using DatabaseServer;
using DatabaseServer.Data;
using Microsoft.EntityFrameworkCore;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
string connectionString = builder.Configuration.GetConnectionString("GameDatabase")
    ?? throw new InvalidOperationException("缺少 ConnectionStrings:GameDatabase。");
builder.Services.Configure<DatabaseServerOptions>(builder.Configuration.GetSection("DatabaseServer"));
builder.Services.AddDbContextFactory<GameDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));
builder.Services.AddHostedService<DatabaseRpcWorker>();

IHost host = builder.Build();
await host.RunAsync();
