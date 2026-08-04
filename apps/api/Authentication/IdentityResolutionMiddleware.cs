using CrevaFactOS.Persistence.Identity;

namespace CrevaFactOS.Api.Authentication;

public sealed class IdentityResolutionMiddleware(
    RequestDelegate next,
    ILogger<IdentityResolutionMiddleware> logger)
{
    private static readonly Action<ILogger, Exception?> IdentityResolutionFailed =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(1002, nameof(IdentityResolutionFailed)),
            "Identity resolution failed without exposing claims or connection details.");

    public async Task InvokeAsync(
        HttpContext httpContext,
        CurrentIdentityAccessor currentIdentityAccessor)
    {
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            await next(httpContext);
            return;
        }

        var issuers = httpContext.User.FindAll("iss").Select(claim => claim.Value).ToArray();
        var subjects = httpContext.User.FindAll("sub").Select(claim => claim.Value).ToArray();
        if (!HasOneValidValue(issuers) || !HasOneValidValue(subjects))
        {
            await WriteProblemAsync(httpContext, "Authentication could not be resolved.");
            return;
        }

        var resolver = httpContext.RequestServices.GetService<IIdentitySubjectResolver>();
        if (resolver is null)
        {
            httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                type = "about:blank",
                status = StatusCodes.Status503ServiceUnavailable,
                title = "Identity resolution is unavailable.",
            });
            return;
        }

        AuthenticatedIdentity? identity;
        try
        {
            identity = await resolver.ResolveActiveIdentityAsync(
                issuers[0],
                subjects[0],
                httpContext.RequestAborted);
        }
        catch
        {
            IdentityResolutionFailed(logger, null);
            httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                type = "about:blank",
                status = StatusCodes.Status503ServiceUnavailable,
                title = "Identity resolution is unavailable.",
            });
            return;
        }

        if (identity is null)
        {
            await WriteProblemAsync(httpContext, "Authentication could not be resolved.");
            return;
        }

        currentIdentityAccessor.Set(identity);
        await next(httpContext);
    }

    private static bool HasOneValidValue(string[] values) =>
        values.Length == 1
        && !string.IsNullOrWhiteSpace(values[0])
        && values[0].Length <= 200;

    private static Task WriteProblemAsync(HttpContext context, string title)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return context.Response.WriteAsJsonAsync(new
        {
            type = "about:blank",
            status = StatusCodes.Status401Unauthorized,
            title,
        });
    }
}
