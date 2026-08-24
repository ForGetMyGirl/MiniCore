using AuthenticationServer.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationServer.Endpoints;

/// <summary>
/// 提供同时验证账号数据库和 Coordinator 配置的认证服务就绪端点。
/// </summary>
public static class AuthenticationReadinessEndpointRouteBuilderExtensions
{
    #region Public 公共成员

    /// <summary>
    /// 注册仅在账号数据库可连接且 Coordinator 地址有效时返回成功的就绪端点。
    /// </summary>
    /// <param name="endpoints">ASP.NET Core 端点路由器。</param>
    /// <returns>原端点路由器。</returns>
    public static IEndpointRouteBuilder MapAuthenticationReadiness(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/ready",
            async (
                IDbContextFactory<AuthenticationDbContext> dbContextFactory,
                IConfiguration configuration,
                CancellationToken cancellationToken) =>
            {
                string coordinatorUrl = configuration["Authentication:CoordinatorWebSocketUrl"] ?? string.Empty;
                if (!Uri.TryCreate(coordinatorUrl, UriKind.Absolute, out Uri? uri)
                    || (!string.Equals(uri.Scheme, "ws", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(uri.Scheme, "wss", StringComparison.OrdinalIgnoreCase)))
                {
                    return Results.Json(
                        new { status = "not-ready", database = false, coordinatorConfigured = false },
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }

                try
                {
                    await using AuthenticationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
                    bool databaseReady = await dbContext.Database.CanConnectAsync(cancellationToken);
                    return databaseReady
                        ? Results.Ok(new { status = "ready", database = true, coordinatorConfigured = true })
                        : Results.Json(
                            new { status = "not-ready", database = false, coordinatorConfigured = true },
                            statusCode: StatusCodes.Status503ServiceUnavailable);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    return Results.Json(
                        new { status = "not-ready", database = false, coordinatorConfigured = true },
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }
            });
        return endpoints;
    }

    #endregion
}
