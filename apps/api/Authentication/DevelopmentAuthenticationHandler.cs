using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace NevaxisFactOS.Api.Authentication;

public sealed class DevelopmentAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "NevaxisFactOS.Development";
    public const string IssuerHeader = "X-crevafactos-dev-Issuer";
    public const string SubjectHeader = "X-crevafactos-dev-Subject";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var hasIssuer = Request.Headers.TryGetValue(IssuerHeader, out var issuers);
        var hasSubject = Request.Headers.TryGetValue(SubjectHeader, out var subjects);

        if (!hasIssuer && !hasSubject)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!HasOneValidValue(issuers, 200) || !HasOneValidValue(subjects, 200))
        {
            return Task.FromResult(AuthenticateResult.Fail("Development identity headers are malformed."));
        }

        var claims = new[]
        {
            new Claim("iss", issuers[0]!),
            new Claim("sub", subjects[0]!),
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool HasOneValidValue(Microsoft.Extensions.Primitives.StringValues values, int maximumLength) =>
        values.Count == 1
        && !string.IsNullOrWhiteSpace(values[0])
        && values[0]!.Length <= maximumLength;
}
