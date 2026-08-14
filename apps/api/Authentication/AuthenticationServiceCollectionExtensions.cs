using CrevaFactOS.Persistence.SaasIdentity;
using CrevaFactOS.Api.SaasIdentity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace CrevaFactOS.Api.Authentication;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var options = configuration
            .GetSection(AuthenticationOptions.SectionName)
            .Get<AuthenticationOptions>() ?? new AuthenticationOptions();
        ApplyEnvironmentOverrides(options);

        services.AddOptions<AuthenticationOptions>()
            .Bind(configuration.GetSection(AuthenticationOptions.SectionName))
            .PostConfigure(ApplyEnvironmentOverrides);

        switch (options.Mode)
        {
            case AuthenticationMode.Disabled:
                services.AddAuthentication(authenticationOptions =>
                    {
                        authenticationOptions.DefaultAuthenticateScheme = DisabledAuthenticationHandler.SchemeName;
                        authenticationOptions.DefaultChallengeScheme = DisabledAuthenticationHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, DisabledAuthenticationHandler>(
                        DisabledAuthenticationHandler.SchemeName,
                        _ => { });
                break;

            case AuthenticationMode.Development:
                if (!environment.IsDevelopment())
                {
                    throw new InvalidOperationException(
                        "Development authentication is available only in the Development environment.");
                }

                services.AddAuthentication(authenticationOptions =>
                    {
                        authenticationOptions.DefaultAuthenticateScheme = DevelopmentAuthenticationHandler.SchemeName;
                        authenticationOptions.DefaultChallengeScheme = DevelopmentAuthenticationHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
                        DevelopmentAuthenticationHandler.SchemeName,
                        _ => { });
                break;

            case AuthenticationMode.JwtBearer:
                if (string.IsNullOrWhiteSpace(options.Authority)
                    || string.IsNullOrWhiteSpace(options.Audience))
                {
                    throw new InvalidOperationException(
                        "Authentication Authority and Audience are required for JWT bearer mode.");
                }

                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(jwtOptions =>
                    {
                        jwtOptions.Authority = options.Authority;
                        jwtOptions.Audience = options.Audience;
                        jwtOptions.RequireHttpsMetadata = options.RequireHttpsMetadata;
                        jwtOptions.MapInboundClaims = false;
                        jwtOptions.TokenValidationParameters = new TokenValidationParameters
                        {
                            NameClaimType = "sub",
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                        };
                    });
                break;

            case AuthenticationMode.SessionCookie:
                if (environment.IsDevelopment())
                {
                    const string policyScheme = "CrevaFactOS.SessionOrDevelopment";
                    services.AddAuthentication(authenticationOptions =>
                        {
                            authenticationOptions.DefaultAuthenticateScheme = policyScheme;
                            authenticationOptions.DefaultChallengeScheme = policyScheme;
                        })
                        .AddPolicyScheme(
                            policyScheme,
                            "Session cookie with development header fallback",
                            policyOptions =>
                            {
                                policyOptions.ForwardDefaultSelector = context =>
                                    context.Request.Cookies.ContainsKey(SaasIdentityConstants.SessionCookieName)
                                        ? SessionCookieAuthenticationHandler.SchemeName
                                        : DevelopmentAuthenticationHandler.SchemeName;
                            })
                        .AddScheme<AuthenticationSchemeOptions, SessionCookieAuthenticationHandler>(
                            SessionCookieAuthenticationHandler.SchemeName,
                            _ => { })
                        .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
                            DevelopmentAuthenticationHandler.SchemeName,
                            _ => { });
                }
                else
                {
                    services.AddAuthentication(authenticationOptions =>
                        {
                            authenticationOptions.DefaultAuthenticateScheme =
                                SessionCookieAuthenticationHandler.SchemeName;
                            authenticationOptions.DefaultChallengeScheme =
                                SessionCookieAuthenticationHandler.SchemeName;
                        })
                        .AddScheme<AuthenticationSchemeOptions, SessionCookieAuthenticationHandler>(
                            SessionCookieAuthenticationHandler.SchemeName,
                            _ => { });
                }

                break;

            default:
                throw new InvalidOperationException("Unsupported authentication mode.");
        }

        return services;
    }

    private static void ApplyEnvironmentOverrides(AuthenticationOptions options)
    {
        var mode = Environment.GetEnvironmentVariable("CREVAFACTOS__AUTHENTICATION__MODE");
        if (!string.IsNullOrWhiteSpace(mode))
        {
            options.Mode = Enum.Parse<AuthenticationMode>(mode, ignoreCase: true);
        }

        options.Authority = GetEnvironmentValue("AUTHORITY", options.Authority);
        options.Audience = GetEnvironmentValue("AUDIENCE", options.Audience);

        var requireHttps = Environment.GetEnvironmentVariable(
            "CREVAFACTOS__AUTHENTICATION__REQUIRE_HTTPS_METADATA");
        if (!string.IsNullOrWhiteSpace(requireHttps))
        {
            options.RequireHttpsMetadata = bool.Parse(requireHttps);
        }
    }

    private static string GetEnvironmentValue(string suffix, string fallback) =>
        Environment.GetEnvironmentVariable($"CREVAFACTOS__AUTHENTICATION__{suffix}") ?? fallback;
}
