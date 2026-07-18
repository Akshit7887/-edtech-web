using System.Security.Claims;
using Dapper;
using EdTechApi.Data;
using EdTechApi.Models;
using EdTechApi.Services;

namespace EdTechApi.Middleware;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly TimeSpan UserCacheTtl = TimeSpan.FromMinutes(5);

    public JwtMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IJwtService jwtService, IDbConnectionFactory dbFactory, IRedisCacheService cache)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        var token = !string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ")
            ? authHeader["Bearer ".Length..].Trim()
            : context.Request.Query["access_token"].FirstOrDefault();

        if (!string.IsNullOrEmpty(token))
        {
            var principal = jwtService.VerifyToken(token);
            if (principal != null)
            {
                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value;
                var tokenVersionClaim = principal.FindFirst("tokenVersion")?.Value;

                if (userIdClaim != null && int.TryParse(userIdClaim, out var userId))
                {
                    var user = await GetUserWithCacheAsync(dbFactory, cache, userId, tokenVersionClaim);

                    if (user != null)
                    {
                        context.Items["User"] = user;
                        context.Items["UserRole"] = user.Role;
                        context.Items["UserId"] = user.Id;
                    }
                }
            }
        }

        await _next(context);
    }

    private static async Task<User?> GetUserWithCacheAsync(IDbConnectionFactory dbFactory, IRedisCacheService cache, int userId, string? tokenVersionClaim)
    {
        if (cache.IsConnected)
        {
            var cached = await cache.GetAsync<User>($"user:{userId}");
            if (cached != null)
            {
                if (tokenVersionClaim != null && int.TryParse(tokenVersionClaim, out var tv) && tv != cached.TokenVersion)
                    return null;
                return cached;
            }
        }

        using var conn = dbFactory.CreateConnection();
        var user = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM \"Users\" WHERE \"id\" = @Id", new { Id = userId });

        if (user != null)
        {
            if (tokenVersionClaim != null && int.TryParse(tokenVersionClaim, out var tv) && tv != user.TokenVersion)
                return null;

            if (cache.IsConnected)
                await cache.SetAsync($"user:{userId}", user, UserCacheTtl);
        }

        return user;
    }
}

public static class JwtMiddlewareExtensions
{
    public static IApplicationBuilder UseJwtMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtMiddleware>();
    }
}
