using System.Security.Claims;
using System.Text.Encodings.Web;
using CrevaFactOS.Persistence.SaasIdentity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CrevaFactOS.Api.SaasIdentity;

public sealed class SessionCookieAuthenticationHandler(
    ISaasIdentityRepository identityRepository,
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "CrevaFactOS.SessionCookie";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(SaasIdentityConstants.SessionCookieName, out var sessionToken)
            || string.IsNullOrWhiteSpace(sessionToken))
        {
            return AuthenticateResult.NoResult();
        }

        SaasIdentitySessionContext? sessionContext;
        try
        {
            sessionContext = await identityRepository.ResolveSessionAsync(
                sessionToken,
                Context.RequestAborted);
        }
        catch
        {
            return AuthenticateResult.Fail("Session validation is unavailable.");
        }

        if (sessionContext is null)
        {
            return AuthenticateResult.Fail("Session is invalid.");
        }

        Context.Items[SaasIdentityHttpContextKeys.SessionContext] = sessionContext;

        var claims = new[]
        {
            new Claim("iss", SaasIdentityConstants.Issuer),
            new Claim("sub", sessionContext.AccountId.ToString("D")),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
    }
}

public static class SaasIdentityHttpContextKeys
{
    public const string SessionContext = "CrevaFactOS.SaasIdentity.SessionContext";
}
