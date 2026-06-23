namespace EdTechApi.Middleware;

public class RequestIdMiddleware
{
    private readonly RequestDelegate _next;

    public RequestIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Request.Headers["X-Request-Id"].FirstOrDefault();
        if (string.IsNullOrEmpty(requestId))
        {
            requestId = Guid.NewGuid().ToString("N")[..8];
        }

        context.Items["RequestId"] = requestId;
        context.Response.Headers["X-Request-Id"] = requestId;

        await _next(context);
    }
}

public static class RequestIdExtensions
{
    public static IApplicationBuilder UseRequestId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestIdMiddleware>();
    }
}
