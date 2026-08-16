using System.Security.Cryptography;
using AuthenticationServer.Contracts;
using AuthenticationServer.Data;
using AuthenticationServer.Models;
using AuthenticationServer.Security;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationServer.Endpoints;

/// <summary>
/// 注册 AuthenticationServer 的最小 HTTP API。
/// </summary>
public static class AuthenticationEndpointRouteBuilderExtensions
{
    #region Public 公共成员

    /// <summary>
    /// 映射账号注册和登录端点。
    /// </summary>
    /// <param name="endpoints">ASP.NET Core 端点路由构建器。</param>
    /// <returns>便于继续链式配置的原构建器。</returns>
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/register", RegisterAsync);
        endpoints.MapPost("/api/auth/login", LoginAsync);
        return endpoints;
    }

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 校验唯一账号和玩家名后写入独立认证数据库。
    /// </summary>
    /// <param name="request">账号注册数据。</param>
    /// <param name="factory">短生命周期 DbContext 工厂。</param>
    /// <param name="cancellationToken">HTTP 请求取消令牌。</param>
    /// <returns>标准注册 JSON 结果。</returns>
    private static async Task<IResult> RegisterAsync(RegisterRequest request, IDbContextFactory<AuthenticationDbContext> factory, CancellationToken cancellationToken)
    {
        string account = (request.Account ?? string.Empty).Trim();
        string password = request.Password ?? string.Empty;
        string playerName = (request.PlayerName ?? string.Empty).Trim();
        if (account.Length is < 3 or > 64 || password.Length is < 8 or > 128 || playerName.Length is < 1 or > 32)
        {
            return Results.BadRequest(new RegisterResponse { Code = 400, Msg = "账号、密码或玩家名格式不合法" });
        }

        await using AuthenticationDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        if (await db.Accounts.AnyAsync(item => item.Account == account || item.PlayerName == playerName, cancellationToken))
        {
            return Results.Conflict(new RegisterResponse { Code = 409, Msg = "账号或玩家名已经存在" });
        }

        (string salt, string hash) = PasswordHashing.Create(password);
        db.Accounts.Add(new AccountEntity
        {
            Account = account,
            PlayerName = playerName,
            PasswordSalt = salt,
            PasswordHash = hash
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new RegisterResponse { Code = 0, Msg = "注册成功" });
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new RegisterResponse { Code = 409, Msg = "账号或玩家名已经存在" });
        }
    }

    /// <summary>
    /// 验证账号密码并动态下发身份、会话令牌和 Coordinator 地址。
    /// </summary>
    /// <param name="request">账号登录数据。</param>
    /// <param name="factory">短生命周期 DbContext 工厂。</param>
    /// <param name="configuration">AuthenticationServer 项目内配置。</param>
    /// <param name="cancellationToken">HTTP 请求取消令牌。</param>
    /// <returns>标准登录 JSON 结果。</returns>
    private static async Task<IResult> LoginAsync(LoginRequest request, IDbContextFactory<AuthenticationDbContext> factory, IConfiguration configuration, CancellationToken cancellationToken)
    {
        string account = (request.Account ?? string.Empty).Trim();
        string password = request.Password ?? string.Empty;
        if (account.Length is < 3 or > 64 || password.Length is < 8 or > 128)
        {
            return Results.BadRequest(new LoginResponse { Code = 400, Msg = "账号或密码格式不合法" });
        }

        await using AuthenticationDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        AccountEntity? entity = await db.Accounts.AsNoTracking().SingleOrDefaultAsync(item => item.Account == account, cancellationToken);
        if (entity == null || !PasswordHashing.Verify(password, entity.PasswordSalt, entity.PasswordHash))
        {
            return Results.Json(new LoginResponse { Code = 401, Msg = "账号或密码错误" }, statusCode: StatusCodes.Status401Unauthorized);
        }

        string coordinatorUrl = configuration["Authentication:CoordinatorWebSocketUrl"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(coordinatorUrl))
        {
            return Results.Json(new LoginResponse { Code = 503, Msg = "认证服务尚未配置 Coordinator" }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Ok(new LoginResponse
        {
            Code = 0,
            Msg = "登录成功",
            AccountId = entity.Id,
            PlayerName = entity.PlayerName,
            SessionToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            CoordinatorWebSocketUrl = coordinatorUrl
        });
    }

    #endregion
}
