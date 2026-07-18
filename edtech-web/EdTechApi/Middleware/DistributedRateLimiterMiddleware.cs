using System.Security.Claims;
using EdTechApi.Services;

namespace EdTechApi.Middleware;

public class DistributedRateLimiterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DistributedRateLimiterMiddleware> _logger;
    private static readonly TimeSpan AuthWindow = TimeSpan.FromMinutes(1);
    private const int AuthLimit = 5;
    private static readonly TimeSpan OtpWindow = TimeSpan.FromMinutes(1);
    private const int OtpLimit = 10;
    private static readonly TimeSpan ApiWindow = TimeSpan.FromMinutes(1);
    private const int ApiLimit = 100;

    private static readonly HashSet<string> AuthPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login", "/api/auth/register", "/api/auth/forgot-password",
        "/api/auth/external-session"
    };

    private static readonly HashSet<string> OtpPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/verify-otp", "/api/auth/register-verify-otp", "/api/auth/reset-password"
    };

    public DistributedRateLimiterMiddleware(RequestDelegate next, ILogger<DistributedRateLimiterMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IRedisCacheService cache)
    {
        var path = context.Request.Path.Value ?? "";
        var method = context.Request.Method;

        if (method == "OPTIONS") { await _next(context); return; }

        var policy = GetPolicy(path);
        if (policy == null) { await _next(context); return; }

        var (limit, window) = policy.Value;

        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var identifier = userId != null ? $"user:{userId}" : $"ip:{ip}";

        var key = $"{GetPolicyName(path)}:{identifier}";

        bool allowed;
        if (cache.IsConnected)
        {
            allowed = await cache.CheckRateLimitAsync(key, limit, window);
        }
        else
        {
            allowed = InMemoryFallback(key, limit, window);
        }

        if (!allowed)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = ((int)window.TotalSeconds).ToString();
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                error = "Too many requests. Please try again later.",
                retryAfterSeconds = (int)window.TotalSeconds
            });
            return;
        }

        await _next(context);
    }

    private string GetPolicyName(string path)
    {
        if (AuthPaths.Contains(path)) return "auth";
        if (OtpPaths.Contains(path)) return "otp";
        return "api";
    }

    private (int limit, TimeSpan window)? GetPolicy(string path)
    {
        if (AuthPaths.Contains(path)) return (AuthLimit, AuthWindow);
        if (OtpPaths.Contains(path)) return (OtpLimit, OtpWindow);

        if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/hubs/", StringComparison.OrdinalIgnoreCase))
            return (ApiLimit, ApiWindow);

        return null;
    }

    private static readonly Dictionary<string, (int count, DateTime resetAt)> _memCounters = new();
    private static readonly object _memLock = new();

    private bool InMemoryFallback(string key, int limit, TimeSpan window)
    {
        lock (_memLock)
        {
            var now = DateTime.UtcNow;
            if (_memCounters.TryGetValue(key, out var entry))
            {
                if (now < entry.resetAt)
                {
                    if (entry.count >= limit) return false;
                    _memCounters[key] = (entry.count + 1, entry.resetAt);
                    return true;
                }
            }
            _memCounters[key] = (1, now.Add(window));
            return true;
        }
    }
}

public static class DistributedRateLimiterExtensions
{
    public static IApplicationBuilder UseDistributedRateLimiter(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<DistributedRateLimiterMiddleware>();
    }
}
