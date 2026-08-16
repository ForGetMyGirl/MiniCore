using AuthenticationServer.Data;
using AuthenticationServer.Endpoints;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
string connectionString = builder.Configuration.GetConnectionString("Authentication")
    ?? throw new InvalidOperationException("缺少 ConnectionStrings:Authentication。");
builder.Services.AddDbContextFactory<AuthenticationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

WebApplication app = builder.Build();
app.UseHttpsRedirection();
app.MapAuthenticationEndpoints();
app.Run();
