using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace CrevaFactOS.Api.Authorization;

public sealed class UniformAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "about:blank",
                status = StatusCodes.Status403Forbidden,
                title = "Authorization denied.",
            });
            return;
        }

        await defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
