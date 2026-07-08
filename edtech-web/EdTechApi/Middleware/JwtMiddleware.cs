using System.Security.Claims;
using Dapper;
using EdTechApi.Data;
using EdTechApi.Models;
using EdTechApi.Services;

namespace EdTechApi.Middleware;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;

    public JwtMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IJwtService jwtService, IDbConnectionFactory dbFactory)
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
                    using var conn = dbFactory.CreateConnection();
                    var user = await conn.QueryFirstOrDefaultAsync<User>(
                        "SELECT * FROM \"Users\" WHERE \"id\" = @Id", new { Id = userId });

                    if (user != null)
                    {
                        if (tokenVersionClaim != null && int.TryParse(tokenVersionClaim, out var tokenVersion))
                        {
                            if (tokenVersion != user.TokenVersion)
                            {
                                context.Response.StatusCode = 401;
                                await context.Response.WriteAsJsonAsync(new { success = false, error = "Session expired. Please log in again." });
                                return;
                            }
                        }

                        context.Items["User"] = user;
                        context.Items["UserRole"] = user.Role;
                        context.Items["UserId"] = user.Id;
                    }
                }
            }
        }

        await _next(context);
    }
}

public static class JwtMiddlewareExtensions
{
    public static IApplicationBuilder UseJwtMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtMiddleware>();
    }
}
