using System.Globalization;
using System.Net;
using NevaxisZentra.Persistence.Bootstrap;

namespace NevaxisZentra.Api.SaasIdentity;

/// <summary>
/// Localhost-only, one-time first-account enrollment (PRODUCTION installs start with no tenant and no
/// user). Never reachable remotely, never after enrollment completes.
/// </summary>
public static class BootstrapEnrollmentEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/bootstrap/status", StatusAsync).AllowAnonymous();
        group.MapPost("/bootstrap/enroll", EnrollAsync).AllowAnonymous();
    }

    /// <summary>
    /// True only for a loopback peer addressing a loopback host, with no non-loopback forwarding or
    /// cross-site origin. Blocks remote clients, DNS-rebinding and cross-site form posts.
    /// </summary>
    public static bool IsLocalRequest(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var remote = httpContext.Connection.RemoteIpAddress;
        if (remote is null || !IPAddress.IsLoopback(remote.IsIPv4MappedToIPv6 ? remote.MapToIPv4() : remote))
        {
            return false;
        }

        if (!IsLoopbackHost(httpContext.Request.Host.Host))
        {
            return false;
        }

        foreach (var forwarded in httpContext.Request.Headers["X-Forwarded-For"]
                     .SelectMany(value => (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
        {
            if (!IPAddress.TryParse(forwarded, out var address) || !IPAddress.IsLoopback(address))
            {
                return false;
            }
        }

        var origin = httpContext.Request.Headers.Origin.ToString();
        if (!string.IsNullOrEmpty(origin)
            && (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri) || !IsLoopbackHost(originUri.Host)))
        {
            return false;
        }

        return true;
    }

    private static bool IsLoopbackHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || (IPAddress.TryParse(host.Trim('[', ']'), out var address) && IPAddress.IsLoopback(address));

    private static IResult StatusAsync(HttpContext httpContext, IFirstAccountEnrollmentService service)
    {
        if (!IsLocalRequest(httpContext))
        {
            return Results.NotFound();
        }

        var availability = service.GetAvailability();
        return Results.Ok(new
        {
            available = availability.Available,
            expiresAtUtc = availability.Available
                ? availability.ExpiresAtUtc?.UtcDateTime.ToString("o", CultureInfo.InvariantCulture)
                : null,
        });
    }

    private static async Task<IResult> EnrollAsync(
        EnrollRequest request,
        HttpContext httpContext,
        IFirstAccountEnrollmentService service,
        BootstrapAttemptThrottle throttle,
        CancellationToken cancellationToken)
    {
        if (!IsLocalRequest(httpContext))
        {
            return Results.NotFound();
        }

        if (throttle.IsBlocked())
        {
            return Results.Json(
                new { type = "about:blank", status = 429, title = "Too many attempts. Try again later." },
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        var result = await service.EnrollAsync(
            new FirstAccountEnrollmentRequest(
                request.Token,
                request.OrganizationCode,
                request.OrganizationName,
                request.OwnerEmail,
                request.OwnerDisplayName,
                request.Password),
            cancellationToken);

        switch (result.Status)
        {
            case FirstAccountEnrollmentStatus.Enrolled:
                throttle.Reset();
                return Results.Json(new { status = "enrolled", tenantKey = result.TenantKey }, statusCode: StatusCodes.Status201Created);
            case FirstAccountEnrollmentStatus.InvalidRequest:
                return Results.Json(
                    new { type = "about:blank", status = 400, title = "Invalid enrollment details.", errors = result.Errors },
                    statusCode: StatusCodes.Status400BadRequest);
            case FirstAccountEnrollmentStatus.InvalidToken:
            case FirstAccountEnrollmentStatus.Expired:
                throttle.RecordFailure();
                return Results.Json(
                    new { type = "about:blank", status = 403, title = "Invalid or expired enrollment token." },
                    statusCode: StatusCodes.Status403Forbidden);
            case FirstAccountEnrollmentStatus.InProgress:
            case FirstAccountEnrollmentStatus.AlreadyEnrolled:
                return Results.Json(
                    new { type = "about:blank", status = 409, title = "Enrollment is not available." },
                    statusCode: StatusCodes.Status409Conflict);
            case FirstAccountEnrollmentStatus.Unavailable:
                return Results.NotFound();
            default:
                return Results.Json(
                    new { type = "about:blank", status = 503, title = "Enrollment could not be completed." },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    private sealed record EnrollRequest(
        string? Token,
        string? OrganizationCode,
        string? OrganizationName,
        string? OwnerEmail,
        string? OwnerDisplayName,
        string? Password);
}
