using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EdTechApi.Middleware;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireAuthAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (HasAllowAnonymous(context)) return;

        var userId = context.HttpContext.Items["UserId"];
        if (userId == null)
        {
            context.Result = new JsonResult(new { success = false, error = "Authentication required" })
            {
                StatusCode = 401
            };
        }
    }

    protected static bool HasAllowAnonymous(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor descriptor)
        {
            if (descriptor.MethodInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), false).Length > 0)
                return true;
            if (descriptor.ControllerTypeInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), false).Length > 0)
                return true;
        }
        return false;
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute : RequireAuthAttribute
{
    private readonly string[] _roles;

    public RequireRoleAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public new void OnAuthorization(AuthorizationFilterContext context)
    {
        base.OnAuthorization(context);
        if (context.Result != null) return;

        var role = context.HttpContext.Items["UserRole"] as string;
        if (string.IsNullOrEmpty(role) || !_roles.Contains(role))
        {
            context.Result = new JsonResult(new { success = false, error = $"Access denied. Required role: {string.Join(" or ", _roles)}" })
            {
                StatusCode = 403
            };
        }
    }
}
