using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Api.Tenancy;

public sealed class TenantResolutionMiddleware(
    RequestDelegate next,
    ILogger<TenantResolutionMiddleware> logger)
{
    private static readonly Action<ILogger, Exception?> TenantResolutionFailed =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(1001, nameof(TenantResolutionFailed)),
            "Tenant resolution failed without exposing tenant or connection details.");

    public const string HeaderName = "X-CrevaFactOS-Tenant";

    public async Task InvokeAsync(
        HttpContext httpContext,
        TenantContextAccessor tenantContextAccessor)
    {
        if (!httpContext.Request.Headers.TryGetValue(HeaderName, out var values))
        {
            await next(httpContext);
            return;
        }

        if (values.Count != 1
            || string.IsNullOrWhiteSpace(values[0])
            || values[0]!.Length > 100)
        {
            await WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Invalid tenant context header.");
            return;
        }

        var resolver = httpContext.RequestServices.GetService<ITenantResolver>();
        if (resolver is null)
        {
            await WriteProblemAsync(
                httpContext,
                StatusCodes.Status503ServiceUnavailable,
                "Tenant resolution is unavailable.");
            return;
        }

        TenantContext? context;
        try
        {
            context = await resolver.ResolveActiveTenantAsync(
                values[0]!,
                httpContext.RequestAborted);
        }
        catch
        {
            TenantResolutionFailed(logger, null);
            await WriteProblemAsync(
                httpContext,
                StatusCodes.Status503ServiceUnavailable,
                "Tenant resolution is unavailable.");
            return;
        }

        if (context is null)
        {
            await WriteProblemAsync(
                httpContext,
                StatusCodes.Status404NotFound,
                "No active tenant matches the supplied context.");
            return;
        }

        tenantContextAccessor.Set(context);
        await next(httpContext);
    }

    private static Task WriteProblemAsync(HttpContext context, int statusCode, string title)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(new
        {
            type = "about:blank",
            status = statusCode,
            title,
        });
    }
}
