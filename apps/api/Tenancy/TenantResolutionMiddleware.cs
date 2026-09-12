using NevaxisFactOS.Api.SaasIdentity;
using NevaxisFactOS.Persistence.SaasIdentity;
using NevaxisFactOS.Persistence.Tenancy;
using NevaxisFactOS.Persistence.Identity;

namespace NevaxisFactOS.Api.Tenancy;

public sealed class TenantResolutionMiddleware(
    RequestDelegate next,
    ILogger<TenantResolutionMiddleware> logger)
{
    private static readonly Action<ILogger, Exception?> TenantResolutionFailed =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(1001, nameof(TenantResolutionFailed)),
            "Tenant resolution failed without exposing tenant or connection details.");

    public const string HeaderName = "X-NevaxisFactOS-Tenant";
    public const string LegacyHeaderName = "X-CrevaFactOS-Tenant";

    public async Task InvokeAsync(
        HttpContext httpContext,
        TenantContextAccessor tenantContextAccessor,
        ICurrentIdentityAccessor currentIdentityAccessor)
    {
        if (httpContext.Request.Path.StartsWithSegments("/api/integration/webhooks", StringComparison.OrdinalIgnoreCase))
        {
            await next(httpContext);
            return;
        }

        var session = SaasIdentityEndpointHelpers.GetSessionContext(httpContext);
        if (session is not null)
        {
            if (TryGetTenantHeader(httpContext.Request.Headers, out var sessionHeaderValues)
                && !HeaderMatchesTrustedSession(sessionHeaderValues, session))
            {
                await WriteProblemAsync(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Tenant access denied.");
                return;
            }

            tenantContextAccessor.Set(
                new TenantContext(session.TenantId, session.TenantKey, session.TenantMembershipId));
            await next(httpContext);
            return;
        }

        if (!TryGetTenantHeader(httpContext.Request.Headers, out var values))
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

        if (!currentIdentityAccessor.IsResolved)
        {
            await WriteProblemAsync(
                httpContext,
                StatusCodes.Status401Unauthorized,
                "Authentication is required for tenant selection.");
            return;
        }

        var resolver = httpContext.RequestServices.GetService<ITenantMembershipResolver>();
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
            context = await resolver.ResolveActiveMembershipAsync(
                currentIdentityAccessor.RequireIdentitySubjectId(),
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
                StatusCodes.Status403Forbidden,
                "Tenant access denied.");
            return;
        }

        tenantContextAccessor.Set(context);
        await next(httpContext);
    }

    private static bool TryGetTenantHeader(
        IHeaderDictionary headers,
        out Microsoft.Extensions.Primitives.StringValues values)
    {
        if (headers.TryGetValue(HeaderName, out values))
        {
            return true;
        }

        return headers.TryGetValue(LegacyHeaderName, out values);
    }

    private static bool HeaderMatchesTrustedSession(
        Microsoft.Extensions.Primitives.StringValues values,
        SaasIdentitySessionContext session)
    {
        if (values.Count == 0)
        {
            return true;
        }

        if (values.Count != 1 || string.IsNullOrWhiteSpace(values[0]) || values[0]!.Length > 100)
        {
            return false;
        }

        var requested = values[0]!.Trim();
        return string.Equals(requested, session.TenantKey, StringComparison.OrdinalIgnoreCase)
            || string.Equals(requested, session.TenantId.ToString("D"), StringComparison.OrdinalIgnoreCase);
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
