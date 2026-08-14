using System.Security.Cryptography;
using System.Text;
using CrevaFactOS.Persistence.SaasIdentity;

namespace CrevaFactOS.Api.SaasIdentity;

public sealed class SaasIdentityCsrfMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> ExemptPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/identity/sign-in",
        "/api/identity/password/forgot",
        "/api/identity/password/reset",
        "/api/identity/email/verify",
        "/api/identity/invitations/accept",
    };

    public async Task InvokeAsync(HttpContext httpContext)
    {
        if (!RequiresCsrfValidation(httpContext))
        {
            await next(httpContext);
            return;
        }

        if (!httpContext.Request.Cookies.TryGetValue(SaasIdentityConstants.SessionCookieName, out var sessionCookie)
            || string.IsNullOrWhiteSpace(sessionCookie))
        {
            await next(httpContext);
            return;
        }

        if (!httpContext.Request.Cookies.TryGetValue(SaasIdentityConstants.CsrfCookieName, out var csrfCookie)
            || string.IsNullOrWhiteSpace(csrfCookie))
        {
            await WriteCsrfFailureAsync(httpContext);
            return;
        }

        if (!httpContext.Request.Headers.TryGetValue(SaasIdentityConstants.CsrfHeaderName, out var csrfHeaderValues)
            || csrfHeaderValues.Count != 1
            || string.IsNullOrWhiteSpace(csrfHeaderValues[0]))
        {
            await WriteCsrfFailureAsync(httpContext);
            return;
        }

        if (csrfHeaderValues[0]!.Length != csrfCookie.Length
            || !FixedTimeEquals(csrfHeaderValues[0]!, csrfCookie))
        {
            await WriteCsrfFailureAsync(httpContext);
            return;
        }

        await next(httpContext);
    }

    private static bool FixedTimeEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(left),
            Encoding.UTF8.GetBytes(right));

    private static bool RequiresCsrfValidation(HttpContext httpContext)
    {
        if (!httpContext.Request.Path.StartsWithSegments("/api/identity", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (httpContext.Request.Method is not ("POST" or "PUT" or "PATCH" or "DELETE"))
        {
            return false;
        }

        return !ExemptPaths.Contains(httpContext.Request.Path.Value ?? string.Empty);
    }

    private static Task WriteCsrfFailureAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
        return httpContext.Response.WriteAsJsonAsync(new
        {
            type = "about:blank",
            status = StatusCodes.Status403Forbidden,
            title = "Cross-site request validation failed.",
        });
    }
}
