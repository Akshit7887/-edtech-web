namespace EdTechApi.Middleware;

public class RoleGuardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string[] _allowedRoles;

    public RoleGuardMiddleware(RequestDelegate next, string[] allowedRoles)
    {
        _next = next;
        _allowedRoles = allowedRoles;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var userRole = context.Items["UserRole"] as string;
        if (string.IsNullOrEmpty(userRole))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { success = false, error = "Unauthorized", message = "No user found in request" });
            return;
        }

        if (!_allowedRoles.Contains(userRole))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { success = false, error = "Forbidden", message = $"Access denied. Required role: {string.Join(" or ", _allowedRoles)}" });
            return;
        }

        await _next(context);
    }
}

public static class RoleGuardExtensions
{
    public static IApplicationBuilder UseRoleGuard(this IApplicationBuilder builder, params string[] roles)
    {
        return builder.UseWhen(context => true, app =>
        {
            app.UseMiddleware<RoleGuardMiddleware>(roles);
        });
    }
}
