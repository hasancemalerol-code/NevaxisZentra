using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.SaasIdentity;

namespace NevaxisFactOS.Api.SaasIdentity;

internal static class SaasIdentityEndpointHelpers
{
    internal static SaasIdentitySessionContext? GetSessionContext(HttpContext httpContext) =>
        httpContext.Items.TryGetValue(SaasIdentityHttpContextKeys.SessionContext, out var value)
        && value is SaasIdentitySessionContext sessionContext
            ? sessionContext
            : null;

    internal static string? GetSessionToken(HttpContext httpContext) =>
        httpContext.Request.Cookies.TryGetValue(SaasIdentityConstants.SessionCookieName, out var token)
        && !string.IsNullOrWhiteSpace(token)
            ? token
            : null;

    internal static (SaasIdentitySessionContext? Session, IResult? Failure) RequireAuthenticatedSession(
        HttpContext httpContext)
    {
        var session = GetSessionContext(httpContext);
        if (session is null || httpContext.User.Identity?.IsAuthenticated != true)
        {
            return (null, GenericUnauthorized());
        }

        return (session, null);
    }

    internal static (SaasIdentitySessionContext? Session, IResult? Failure) RequireFullSession(
        HttpContext httpContext)
    {
        var (session, failure) = RequireAuthenticatedSession(httpContext);
        if (failure is not null)
        {
            return (null, failure);
        }

        if (session!.PendingMfa)
        {
            return (null, TypedResults.Json(
                new { type = "about:blank", status = 403, title = "Multi-factor authentication is required." },
                statusCode: StatusCodes.Status403Forbidden));
        }

        return (session, null);
    }

    internal static void ApplySessionCookies(
        HttpContext httpContext,
        IHostEnvironment environment,
        string sessionToken)
    {
        var csrfToken = SaasIdentityTokenUtilities.GenerateOpaqueToken();
        var secure = !environment.IsDevelopment();

        httpContext.Response.Cookies.Append(
            SaasIdentityConstants.SessionCookieName,
            sessionToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                IsEssential = true,
            });

        httpContext.Response.Cookies.Append(
            SaasIdentityConstants.CsrfCookieName,
            csrfToken,
            new CookieOptions
            {
                HttpOnly = false,
                Secure = secure,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                IsEssential = true,
            });

        ApplyNoCacheHeaders(httpContext);
    }

    internal static void ClearSessionCookies(HttpContext httpContext, IHostEnvironment environment)
    {
        var secure = !environment.IsDevelopment();

        httpContext.Response.Cookies.Delete(
            SaasIdentityConstants.SessionCookieName,
            new CookieOptions { Path = "/", Secure = secure, SameSite = SameSiteMode.Lax });

        httpContext.Response.Cookies.Delete(
            SaasIdentityConstants.CsrfCookieName,
            new CookieOptions { Path = "/", Secure = secure, SameSite = SameSiteMode.Lax });

        ApplyNoCacheHeaders(httpContext);
    }

    internal static void ApplyNoCacheHeaders(HttpContext httpContext)
    {
        httpContext.Response.Headers.CacheControl = "no-store, no-cache";
        httpContext.Response.Headers.Pragma = "no-cache";
    }

    internal static IResult GenericUnauthorized() =>
        TypedResults.Json(
            new { type = "about:blank", status = 401, title = "Authentication is required." },
            statusCode: StatusCodes.Status401Unauthorized);

    internal static IResult GenericInvalidCredentials() =>
        TypedResults.Json(
            new { type = "about:blank", status = 401, title = "Invalid credentials." },
            statusCode: StatusCodes.Status401Unauthorized);

    internal static IResult GenericForgotPasswordAccepted() =>
        TypedResults.Ok(new { accepted = true });

    internal static IResult ServiceUnavailable() =>
        TypedResults.Json(
            new { type = "about:blank", status = 503, title = "Identity services are unavailable." },
            statusCode: StatusCodes.Status503ServiceUnavailable);

    internal static IResult MapWriteFailure(SaasIdentityWriteResult result) =>
        result.FailureCode switch
        {
            SaasIdentityFailureCodes.ValidationFailed => TypedResults.BadRequest(new
            {
                type = "about:blank",
                status = 400,
                title = result.FailureMessage ?? "Validation failed.",
            }),
            SaasIdentityFailureCodes.TokenInvalid or SaasIdentityFailureCodes.TokenExpired
                or SaasIdentityFailureCodes.TokenConsumed => TypedResults.Json(
                    new { type = "about:blank", status = 400, title = "The link is invalid or has expired." },
                    statusCode: StatusCodes.Status400BadRequest),
            SaasIdentityFailureCodes.NotFound => TypedResults.NotFound(),
            SaasIdentityFailureCodes.Unavailable => ServiceUnavailable(),
            _ => TypedResults.Json(
                new { type = "about:blank", status = 400, title = "The request could not be completed." },
                statusCode: StatusCodes.Status400BadRequest),
        };

    internal static Guid? ResolveCorrelationId(CorrelationContext correlationContext) =>
        correlationContext.IsAvailable ? correlationContext.CorrelationId : null;
}
