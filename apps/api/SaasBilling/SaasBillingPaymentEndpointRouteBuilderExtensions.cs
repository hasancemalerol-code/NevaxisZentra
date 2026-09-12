using System.Security.Cryptography;
using System.Text;
using NevaxisFactOS.Persistence;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.Identity;
using NevaxisFactOS.Persistence.SaasBilling;
using NevaxisFactOS.Persistence.SaasBilling.Payments;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisFactOS.Api.SaasBilling;

public static class SaasBillingPaymentEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapSaasBillingPaymentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/saas-billing/payments").RequireAuthorization();
        group.MapPost("/checkout/initialize", InitializeCardCheckoutAsync);
        group.MapPost("/checkout/verify-return", VerifyCheckoutReturnAsync);
        endpoints.MapGroup("/api/saas-billing")
            .RequireAuthorization()
            .MapGet("/payment-attempts/{paymentAttemptId:guid}", GetPaymentAttemptDetailAsync);

        endpoints.MapPost("/api/saas-payments/iyzico/webhook", ProcessIyzicoWebhookAsync)
            .AllowAnonymous()
            .DisableAntiforgery();

        return endpoints;
    }

    private static async Task<IResult> InitializeCardCheckoutAsync(
        [FromBody] InitializeCardCheckoutRequest request,
        ISaasBillingRepository repository,
        ISaasPaymentProvider paymentProvider,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await SaasBillingEndpointHelpers.ResolveManageAccessAsync(
            permissionEvaluator,
            identityAccessor,
            tenantContextAccessor,
            authorizationContext,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var providerStatus = paymentProvider.GetStatus();
        if (!providerStatus.CardCheckoutAvailable)
        {
            return TypedResults.Json(
                new { failureCode = SaasPaymentFailureCodes.ProviderNotConfigured, message = "Card checkout is not available." },
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        var command = new InitializeCardCheckoutCommand(
            request.InvoiceId,
            request.InvoiceExpectedVersion,
            request.IdempotencyKey);
        var actorReference = $"membership:{context.MembershipId:N}";
        var result = await repository.InitializeCardCheckoutAsync(
            command,
            paymentProvider,
            actorReference,
            context.CorrelationId,
            cancellationToken);
        return SaasBillingEndpointHelpers.MapWriteResult(result, value => new
        {
            paymentAttemptId = value.PaymentAttemptId,
            checkoutSessionId = value.CheckoutSessionId,
            hostedCheckoutUrl = value.HostedCheckoutUrl,
            paymentAttemptVersion = value.PaymentAttemptVersion,
            idempotentReplay = result.IdempotentReplay,
        });
    }

    private static async Task<IResult> VerifyCheckoutReturnAsync(
        [FromBody] VerifyCheckoutReturnRequest request,
        ISaasBillingRepository repository,
        ISaasPaymentProvider paymentProvider,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await SaasBillingEndpointHelpers.ResolveManageAccessAsync(
            permissionEvaluator,
            identityAccessor,
            tenantContextAccessor,
            authorizationContext,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var command = new VerifyCheckoutReturnCommand(
            request.PaymentAttemptId,
            request.CheckoutSessionToken,
            request.IdempotencyKey);
        var actorReference = $"membership:{context.MembershipId:N}";
        var result = await repository.VerifyCheckoutReturnAsync(
            command,
            paymentProvider,
            actorReference,
            context.CorrelationId,
            cancellationToken);
        return SaasBillingEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> GetPaymentAttemptDetailAsync(
        Guid paymentAttemptId,
        ISaasBillingRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await SaasBillingEndpointHelpers.ResolveReadAccessAsync(
            permissionEvaluator,
            identityAccessor,
            tenantContextAccessor,
            authorizationContext,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var detail = await repository.GetPaymentAttemptDetailAsync(paymentAttemptId, cancellationToken);
        return detail is null
            ? TypedResults.NotFound(new { failureCode = SaasPaymentFailureCodes.NotFound, message = "Payment attempt was not found." })
            : TypedResults.Ok(detail);
    }

    private static async Task<IResult> ProcessIyzicoWebhookAsync(
        HttpRequest httpRequest,
        ISaasBillingRepository repository,
        ISaasPaymentProvider paymentProvider,
        TenantContextAccessor tenantContextAccessor,
        NevaxisFactOSDbContext dbContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(httpRequest.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return TypedResults.BadRequest(new { failureCode = SaasPaymentFailureCodes.WebhookRejected, message = "Webhook body is required." });
        }

        var headers = httpRequest.Headers.ToDictionary(
            item => item.Key,
            item => item.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        var validation = paymentProvider.ValidateWebhookSignature(
            new SaasPaymentWebhookValidationRequest(rawBody, headers));
        if (!validation.IsValid)
        {
            return TypedResults.Json(
                new
                {
                    failureCode = validation.FailureCode ?? SaasPaymentFailureCodes.WebhookSignatureInvalid,
                    message = "Webhook signature is invalid.",
                },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!await SaasBillingWebhookTenantResolver.TryEstablishWebhookTenantContextAsync(
                rawBody,
                tenantContextAccessor,
                dbContext,
                cancellationToken))
        {
            return TypedResults.NotFound(new { failureCode = SaasPaymentFailureCodes.NotFound, message = "Payment reference was not found." });
        }

        var normalized = paymentProvider.NormalizeProviderEvent(rawBody);
        if (normalized is null)
        {
            return TypedResults.BadRequest(new { failureCode = SaasPaymentFailureCodes.WebhookRejected, message = "Webhook payload is invalid." });
        }

        var identityHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(normalized.ProviderEventIdentity))).ToLowerInvariant();

        var command = new ProcessProviderWebhookCommand(
            SaasPaymentProviderKeys.Iyzico,
            identityHash,
            normalized.NormalizedStatus,
            normalized.ProviderPaymentReferenceMasked,
            normalized.CheckoutSessionReferenceMasked,
            normalized.Amount,
            normalized.CurrencyCode,
            normalized.SanitizedEvidenceJson,
            identityHash);

        var result = await repository.ProcessProviderWebhookAsync(
            command,
            "provider:iyzico-webhook",
            correlationContext.CorrelationId,
            cancellationToken);

        return result.Succeeded
            ? TypedResults.Ok(new { acknowledged = true, idempotentReplay = result.IdempotentReplay })
            : SaasBillingEndpointHelpers.MapWriteResult(result);
    }
}

public sealed record InitializeCardCheckoutRequest(
    Guid InvoiceId,
    long InvoiceExpectedVersion,
    string? IdempotencyKey);

public sealed record VerifyCheckoutReturnRequest(
    Guid PaymentAttemptId,
    string CheckoutSessionToken,
    string? IdempotencyKey);
