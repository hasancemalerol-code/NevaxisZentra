using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.SaasIdentity;

namespace NevaxisFactOS.Api.SaasIdentity;

public static class SaasIdentityEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapSaasIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/identity");

        group.MapPost("/sign-in", SignInAsync).AllowAnonymous();
        group.MapPost("/mfa/verify", VerifyMfaAsync).AllowAnonymous();
        group.MapPost("/sign-out", SignOutAsync).RequireAuthorization();
        group.MapPost("/sign-out-all", SignOutAllAsync).RequireAuthorization();
        group.MapGet("/me", GetMeAsync).RequireAuthorization();
        group.MapGet("/sessions", ListSessionsAsync).RequireAuthorization();
        group.MapPost("/sessions/{sessionReference}/revoke", RevokeSessionAsync).RequireAuthorization();
        group.MapPost("/password/forgot", ForgotPasswordAsync).AllowAnonymous();
        group.MapPost("/password/reset", ResetPasswordAsync).AllowAnonymous();
        group.MapPost("/email/verify", VerifyEmailAsync).AllowAnonymous();
        group.MapPost("/invitations/accept", AcceptInvitationAsync).AllowAnonymous();
        group.MapPost("/mfa/enroll", EnrollMfaAsync).RequireAuthorization();
        group.MapPost("/mfa/recovery/consume", ConsumeRecoveryCodeAsync).AllowAnonymous();

        return endpoints;
    }

    private static async Task<IResult> SignInAsync(
        SignInRequest request,
        HttpContext httpContext,
        IHostEnvironment environment,
        ISaasIdentityRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OrganizationContext)
            || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Password))
        {
            return SaasIdentityEndpointHelpers.GenericInvalidCredentials();
        }

        SaasIdentitySignInResult result;
        try
        {
            result = await repository.SignInAsync(
                new SaasIdentitySignInCommand(
                    request.OrganizationContext.Trim(),
                    request.Email.Trim(),
                    request.Password,
                    string.IsNullOrWhiteSpace(request.DeviceLabel) ? null : request.DeviceLabel.Trim(),
                    SaasIdentityEndpointHelpers.ResolveCorrelationId(correlationContext)),
                cancellationToken);
        }
        catch
        {
            return SaasIdentityEndpointHelpers.ServiceUnavailable();
        }

        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.SessionToken))
        {
            return SaasIdentityEndpointHelpers.GenericInvalidCredentials();
        }

        SaasIdentityEndpointHelpers.ApplySessionCookies(httpContext, environment, result.SessionToken);

        return TypedResults.Ok(new
        {
            requiresMfa = result.RequiresMfa,
            sessionReference = result.SessionReference,
        });
    }

    private static async Task<IResult> VerifyMfaAsync(
        MfaVerifyRequest request,
        HttpContext httpContext,
        IHostEnvironment environment,
        ISaasIdentityRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var sessionToken = SaasIdentityEndpointHelpers.GetSessionToken(httpContext);
        if (sessionToken is null || string.IsNullOrWhiteSpace(request.TotpCode))
        {
            return SaasIdentityEndpointHelpers.GenericInvalidCredentials();
        }

        SaasIdentityAuthenticationResult result;
        try
        {
            result = await repository.VerifyMfaAsync(
                new SaasIdentityMfaVerifyCommand(
                    sessionToken,
                    request.TotpCode.Trim(),
                    SaasIdentityEndpointHelpers.ResolveCorrelationId(correlationContext)),
                cancellationToken);
        }
        catch
        {
            return SaasIdentityEndpointHelpers.ServiceUnavailable();
        }

        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.SessionToken))
        {
            return TypedResults.Json(
                new { type = "about:blank", status = 401, title = "Multi-factor authentication failed." },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        SaasIdentityEndpointHelpers.ApplySessionCookies(httpContext, environment, result.SessionToken);

        return TypedResults.Ok(new { sessionReference = result.SessionReference });
    }

    private static async Task<IResult> SignOutAsync(
        HttpContext httpContext,
        IHostEnvironment environment,
        ISaasIdentityRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (_, failure) = SaasIdentityEndpointHelpers.RequireAuthenticatedSession(httpContext);
        if (failure is not null)
        {
            return failure;
        }

        var sessionToken = SaasIdentityEndpointHelpers.GetSessionToken(httpContext);
        if (sessionToken is null)
        {
            return SaasIdentityEndpointHelpers.GenericUnauthorized();
        }

        try
        {
            var result = await repository.SignOutAsync(
                new SaasIdentitySignOutCommand(
                    sessionToken,
                    SaasIdentityEndpointHelpers.ResolveCorrelationId(correlationContext)),
                cancellationToken);

            if (!result.Succeeded && result.FailureCode == SaasIdentityFailureCodes.Unavailable)
            {
                return SaasIdentityEndpointHelpers.ServiceUnavailable();
            }
        }
        catch
        {
            return SaasIdentityEndpointHelpers.ServiceUnavailable();
        }

        SaasIdentityEndpointHelpers.ClearSessionCookies(httpContext, environment);
        return TypedResults.Ok(new { signedOut = true });
    }

    private static async Task<IResult> SignOutAllAsync(
        HttpContext httpContext,
        IHostEnvironment environment,
        ISaasIdentityRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (session, failure) = SaasIdentityEndpointHelpers.RequireFullSession(httpContext);
        if (failure is not null)
        {
            return failure;
        }

        try
        {
            var result = await repository.SignOutAllAsync(
                new SaasIdentitySignOutAllCommand(
                    session!.AccountId,
                    session.TenantId,
                    SaasIdentityEndpointHelpers.ResolveCorrelationId(correlationContext)),
                cancellationToken);

            if (!result.Succeeded)
            {
                return SaasIdentityEndpointHelpers.MapWriteFailure(result);
            }
        }
        catch
        {
            return SaasIdentityEndpointHelpers.ServiceUnavailable();
        }

        SaasIdentityEndpointHelpers.ClearSessionCookies(httpContext, environment);
        return TypedResults.Ok(new { signedOutAll = true });
    }

    private static async Task<IResult> GetMeAsync(
        HttpContext httpContext,
        ISaasIdentityRepository repository,
        CancellationToken cancellationToken)
    {
        var (session, failure) = SaasIdentityEndpointHelpers.RequireFullSession(httpContext);
        if (failure is not null)
        {
            return failure;
        }

        SaasIdentityMeReadModel? profile;
        try
        {
            profile = await repository.GetMeAsync(session!, cancellationToken);
        }
        catch
        {
            return SaasIdentityEndpointHelpers.ServiceUnavailable();
        }

        if (profile is null)
        {
            return SaasIdentityEndpointHelpers.GenericUnauthorized();
        }

        return TypedResults.Ok(new
        {
            accountReference = profile.AccountReference,
            displayName = profile.DisplayName,
            emailMasked = MaskEmailForDisplay(profile.Email),
            organizationLabel = profile.TenantDisplayName,
            sessionReference = session!.SessionReference,
            mfaPending = session.PendingMfa,
        });
    }

    private static string MaskEmailForDisplay(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
        {
            return email;
        }

        var localPart = email[..atIndex];
        var domain = email[(atIndex + 1)..];
        var visible = localPart.Length > 0 ? localPart[..1] : string.Empty;
        return $"{visible}***@{domain}";
    }

    private static async Task<IResult> ListSessionsAsync(
        HttpContext httpContext,
        ISaasIdentityRepository repository,
        CancellationToken cancellationToken)
    {
        var (session, failure) = SaasIdentityEndpointHelpers.RequireFullSession(httpContext);
        if (failure is not null)
        {
            return failure;
        }

        IReadOnlyList<SaasIdentitySessionReadModel> sessions;
        try
        {
            sessions = await repository.ListSessionsAsync(session!, cancellationToken);
        }
        catch
        {
            return SaasIdentityEndpointHelpers.ServiceUnavailable();
        }

        return TypedResults.Ok(new { sessions });
    }

    private static async Task<IResult> RevokeSessionAsync(
        string sessionReference,
        HttpContext httpContext,
        ISaasIdentityRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (session, failure) = SaasIdentityEndpointHelpers.RequireFullSession(httpContext);
        if (failure is not null)
        {
            return failure;
        }

        if (string.IsNullOrWhiteSpace(sessionReference))
        {
            return TypedResults.BadRequest(new
            {
                type = "about:blank",
                status = 400,
                title = "Session reference is required.",
            });
        }

        SaasIdentityWriteResult result;
        try
        {
            result = await repository.RevokeSessionAsync(
                new SaasIdentityRevokeSessionCommand(
                    session!.AccountId,
                    sessionReference.Trim(),
                    SaasIdentityEndpointHelpers.ResolveCorrelationId(correlationContext)),
                cancellationToken);
        }
        catch
        {
            return SaasIdentityEndpointHelpers.ServiceUnavailable();
        }

        return result.Succeeded
            ? TypedResults.Ok(new { revoked = true, sessionReference = sessionReference.Trim() })
            : SaasIdentityEndpointHelpers.MapWriteFailure(result);
    }

    private static async Task<IResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        ISaasIdentityRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return SaasIdentityEndpointHelpers.GenericForgotPasswordAccepted();
        }

        try
        {
            await repository.RequestPasswordResetAsync(
                new SaasIdentityForgotPasswordCommand(
                    request.Email.Trim(),
                    SaasIdentityEndpointHelpers.ResolveCorrelationId(correlationContext)),
                cancellationToken);
        }
        catch
        {
            return SaasIdentityEndpointHelpers.ServiceUnavailable();
        }

        return SaasIdentityEndpointHelpers.GenericForgotPasswordAccepted();
    }

    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        ISaasIdentityRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return TypedResults.Json(
                new { type = "about:blank", status = 400, title = "The link is invalid or has expired." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        SaasIdentityWriteResult result;
        try
        {
            result = await repository.ResetPasswordAsync(
                new SaasIdentityResetPasswordCommand(
                    request.Token.Trim(),
                    request.NewPassword,
                    SaasIdentityEndpointHelpers.ResolveCorrelationId(correlationContext)),
                cancellationToken);
        }
        catch
        {
            return SaasIdentityEndpointHelpers.ServiceUnavailable();
        }

        return result.Succeeded
            ? TypedResults.Ok(new { reset = true })
            : SaasIdentityEndpointHelpers.MapWriteFailure(result);
    }

    private static async Task<IResult> VerifyEmailAsync(
        VerifyEmailRequest request,
        ISaasIdentityRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return TypedResults.Json(
                new { type = "about:blank", status = 400, title = "The link is invalid or has expired." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        SaasIdentityWriteResult result;
        try
        {
            result = await repository.VerifyEmailAsync(
                new SaasIdentityVerifyEmailCommand(
                    request.Token.Trim(),
                    SaasIdentityEndpointHelpers.ResolveCorrelationId(correlationContext)),
                cancellationToken);
        }
        catch
        {
            return SaasIdentityEndpointHelpers.ServiceUnavailable();
        }

        return result.Succeeded
            ? TypedResults.Ok(new { verified = true })
            : SaasIdentityEndpointHelpers.MapWriteFailure(result);
    }

    private static async Task<IResult> AcceptInvitationAsync(
        AcceptInvitationRequest request,
        ISaasIdentityRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token)
            || string.IsNullOrWhiteSpace(request.DisplayName)
            || string.IsNullOrWhiteSpace(request.Password))
        {
            return TypedResults.Json(
                new { type = "about:blank", status = 400, title = "The invitation could not be accepted." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        SaasIdentityInvitationAcceptResult result;
        try
        {
            result = await repository.AcceptInvitationAsync(
                new SaasIdentityAcceptInvitationCommand(
                    request.Token.Trim(),
                    request.DisplayName.Trim(),
                    request.Password,
                    SaasIdentityEndpointHelpers.ResolveCorrelationId(correlationContext)),
                cancellationToken);
        }
        catch
        {
            return SaasIdentityEndpointHelpers.ServiceUnavailable();
        }

        return result.Succeeded
            ? TypedResults.Ok(new { accepted = true, accountReference = result.AccountReference })
            : TypedResults.Json(
                new { type = "about:blank", status = 400, title = "The invitation could not be accepted." },
                statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> EnrollMfaAsync(
        MfaEnrollRequest request,
        HttpContext httpContext,
        ISaasIdentityRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (session, failure) = SaasIdentityEndpointHelpers.RequireFullSession(httpContext);
        if (failure is not null)
        {
            return failure;
        }

        if (!string.IsNullOrWhiteSpace(request.TotpCode) && !string.IsNullOrWhiteSpace(request.SharedSecret))
        {
            SaasIdentityWriteResult confirmResult;
            try
            {
                confirmResult = await repository.ConfirmMfaEnrollmentAsync(
                    new SaasIdentityMfaEnrollConfirmCommand(
                        session!.AccountId,
                        request.SharedSecret.Trim(),
                        request.TotpCode.Trim(),
                        request.RequireMfa,
                        SaasIdentityEndpointHelpers.ResolveCorrelationId(correlationContext)),
                    cancellationToken);
            }
            catch
            {
                return SaasIdentityEndpointHelpers.ServiceUnavailable();
            }

            return confirmResult.Succeeded
                ? TypedResults.Ok(new { enrolled = true, requireMfa = request.RequireMfa })
                : SaasIdentityEndpointHelpers.MapWriteFailure(confirmResult);
        }

        SaasIdentityMfaEnrollBeginResult beginResult;
        try
        {
            beginResult = await repository.BeginMfaEnrollmentAsync(
                new SaasIdentityMfaEnrollCommand(
                    session!.AccountId,
                    SaasIdentityEndpointHelpers.ResolveCorrelationId(correlationContext)),
                cancellationToken);
        }
        catch
        {
            return SaasIdentityEndpointHelpers.ServiceUnavailable();
        }

        if (!beginResult.Succeeded)
        {
            return TypedResults.Json(
                new { type = "about:blank", status = 400, title = "Multi-factor enrollment could not be started." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        return TypedResults.Ok(new
        {
            sharedSecret = beginResult.SharedSecret,
            provisioningUri = beginResult.ProvisioningUri,
            recoveryCodes = beginResult.RecoveryCodes,
        });
    }

    private static async Task<IResult> ConsumeRecoveryCodeAsync(
        RecoveryCodeRequest request,
        HttpContext httpContext,
        IHostEnvironment environment,
        ISaasIdentityRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var sessionToken = SaasIdentityEndpointHelpers.GetSessionToken(httpContext);
        if (sessionToken is null || string.IsNullOrWhiteSpace(request.RecoveryCode))
        {
            return TypedResults.Json(
                new { type = "about:blank", status = 401, title = "Recovery code validation failed." },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        SaasIdentityAuthenticationResult result;
        try
        {
            result = await repository.ConsumeRecoveryCodeAsync(
                new SaasIdentityRecoveryCodeCommand(
                    sessionToken,
                    request.RecoveryCode.Trim(),
                    SaasIdentityEndpointHelpers.ResolveCorrelationId(correlationContext)),
                cancellationToken);
        }
        catch
        {
            return SaasIdentityEndpointHelpers.ServiceUnavailable();
        }

        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.SessionToken))
        {
            return TypedResults.Json(
                new { type = "about:blank", status = 401, title = "Recovery code validation failed." },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        SaasIdentityEndpointHelpers.ApplySessionCookies(httpContext, environment, result.SessionToken);

        return TypedResults.Ok(new { sessionReference = result.SessionReference });
    }

    private sealed record SignInRequest(
        string OrganizationContext,
        string Email,
        string Password,
        string? DeviceLabel);

    private sealed record MfaVerifyRequest(string TotpCode);

    private sealed record ForgotPasswordRequest(string Email);

    private sealed record ResetPasswordRequest(string Token, string NewPassword);

    private sealed record VerifyEmailRequest(string Token);

    private sealed record AcceptInvitationRequest(string Token, string DisplayName, string Password);

    private sealed record MfaEnrollRequest(
        string? SharedSecret,
        string? TotpCode,
        bool RequireMfa = true);

    private sealed record RecoveryCodeRequest(string RecoveryCode);
}
