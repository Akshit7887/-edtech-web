using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EdTechApi.Middleware;

public class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    kv => JsonNamingPolicy.SnakeCaseLower.ConvertName(kv.Key),
                    kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            context.Result = new ObjectResult(new
            {
                success = false,
                error = "Validation failed",
                details = errors
            })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
