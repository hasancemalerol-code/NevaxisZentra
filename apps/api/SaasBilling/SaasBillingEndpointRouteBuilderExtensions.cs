using CrevaFactOS.Api.SaasBilling.Payments;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Identity;
using CrevaFactOS.Persistence.SaasBilling;
using CrevaFactOS.Persistence.SaasBilling.Payments;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.SaasBilling;

public static class SaasBillingEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapSaasBillingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/saas-billing").RequireAuthorization();

        group.MapGet("/workspace", GetWorkspaceSummaryAsync);
        group.MapGet("/account", GetBillingAccountAsync);
        group.MapPut("/account", UpdateBillingAccountAsync);
        group.MapGet("/invoices", ListInvoicesAsync);
        group.MapGet("/invoices/{invoiceId:guid}", GetInvoiceAsync);
        group.MapPost("/invoices", CreateDraftInvoiceAsync);
        group.MapPost("/invoices/transitions", TransitionInvoiceAsync);
        group.MapGet("/payment-attempts", ListPaymentAttemptsAsync);
        group.MapPost("/payment-attempts", CreatePaymentAttemptAsync);
        group.MapPost("/payment-attempts/verify-bank-transfer", VerifyBankTransferAsync);
        group.MapGet("/provider-status", GetProviderStatusAsync);

        return endpoints;
    }

    private static async Task<IResult> GetWorkspaceSummaryAsync(
        ISaasBillingRepository repository,
        ISaasPaymentProvider paymentProvider,
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

        var providerStatus = BuildProviderStatus(paymentProvider);
        var summary = await repository.GetWorkspaceSummaryAsync(providerStatus, cancellationToken);
        return TypedResults.Ok(summary);
    }

    private static SaasBillingProviderStatusReadModel BuildProviderStatus(ISaasPaymentProvider paymentProvider)
    {
        var status = paymentProvider.GetStatus();
        var methods = new List<string>
        {
            SaasBillingPaymentMethods.BankTransfer,
            SaasBillingPaymentMethods.EnterpriseInvoice,
        };
        if (status.CardCheckoutAvailable)
        {
            methods.Insert(0, SaasBillingPaymentMethods.Card);
        }

        return new SaasBillingProviderStatusReadModel(
            status.ProviderKey,
            status.Mode,
            status.IsProduction,
            status.CardCheckoutAvailable,
            status.CardCheckoutStatus,
            methods);
    }

    private static async Task<IResult> GetBillingAccountAsync(
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

        var account = await repository.GetBillingAccountAsync(cancellationToken);
        return account is null ? TypedResults.NotFound() : TypedResults.Ok(account);
    }

    private static async Task<IResult> UpdateBillingAccountAsync(
        [FromBody] UpdateBillingAccountRequest request,
        ISaasBillingRepository repository,
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

        var command = new UpdateSaasBillingAccountCommand(
            request.LegalName,
            request.DisplayName,
            request.BillingContactMasked,
            request.BillingAddressSnapshot,
            request.TaxCountryCode,
            request.TaxNumberMasked,
            request.PreferredBillingCadence,
            request.PreferredPaymentMethod,
            request.ExpectedVersion,
            request.IdempotencyKey);

        var actorReference = $"membership:{context.MembershipId:N}";
        var result = await repository.UpdateBillingAccountAsync(
            command,
            actorReference,
            context.CorrelationId,
            cancellationToken);
        return SaasBillingEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> ListInvoicesAsync(
        [FromQuery] string? status,
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        [FromQuery] string? search,
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

        var invoices = await repository.ListInvoicesAsync(status, fromUtc, toUtc, search, cancellationToken);
        return TypedResults.Ok(invoices);
    }

    private static async Task<IResult> GetInvoiceAsync(
        Guid invoiceId,
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

        var invoice = await repository.GetInvoiceAsync(invoiceId, cancellationToken);
        return invoice is null ? TypedResults.NotFound() : TypedResults.Ok(invoice);
    }

    private static async Task<IResult> CreateDraftInvoiceAsync(
        [FromBody] CreateDraftInvoiceRequest request,
        ISaasBillingRepository repository,
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

        var command = new CreateSaasBillingInvoiceCommand(
            request.SubscriptionId,
            request.PeriodStartUtc,
            request.PeriodEndUtc,
            request.IdempotencyKey);

        var actorReference = $"membership:{context.MembershipId:N}";
        var result = await repository.CreateDraftInvoiceAsync(
            command,
            actorReference,
            context.CorrelationId,
            cancellationToken);
        return SaasBillingEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> TransitionInvoiceAsync(
        [FromBody] TransitionInvoiceRequest request,
        ISaasBillingRepository repository,
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

        var command = new TransitionSaasBillingInvoiceCommand(
            request.InvoiceId,
            request.TargetStatus,
            request.ExpectedVersion,
            request.IdempotencyKey);

        var actorReference = $"membership:{context.MembershipId:N}";
        var result = await repository.TransitionInvoiceAsync(
            command,
            actorReference,
            context.CorrelationId,
            cancellationToken);
        return SaasBillingEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> ListPaymentAttemptsAsync(
        [FromQuery] Guid? invoiceId,
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

        var attempts = await repository.ListPaymentAttemptsAsync(invoiceId, cancellationToken);
        return TypedResults.Ok(attempts);
    }

    private static async Task<IResult> CreatePaymentAttemptAsync(
        [FromBody] CreatePaymentAttemptRequest request,
        ISaasBillingRepository repository,
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

        var command = new CreateSaasBillingPaymentAttemptCommand(
            request.InvoiceId,
            request.PaymentMethod,
            request.Amount,
            request.CurrencyCode,
            request.ExternalReferenceMasked,
            request.IdempotencyKey);

        var actorReference = $"membership:{context.MembershipId:N}";
        var result = await repository.CreatePaymentAttemptAsync(
            command,
            actorReference,
            context.CorrelationId,
            cancellationToken);
        return SaasBillingEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> VerifyBankTransferAsync(
        [FromBody] VerifyBankTransferRequest request,
        ISaasBillingRepository repository,
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

        var command = new VerifySaasBillingBankTransferCommand(
            request.PaymentAttemptId,
            request.ExpectedVersion,
            request.IdempotencyKey);

        var actorReference = $"membership:{context.MembershipId:N}";
        var result = await repository.VerifyBankTransferAsync(
            command,
            actorReference,
            context.CorrelationId,
            cancellationToken);
        return SaasBillingEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> GetProviderStatusAsync(
        ISaasPaymentProvider paymentProvider,
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

        var providerStatus = BuildProviderStatus(paymentProvider);
        var summary = await repository.GetWorkspaceSummaryAsync(providerStatus, cancellationToken);
        return TypedResults.Ok(new
        {
            provider = summary.ProviderStatus,
            providerKey = paymentProvider.ProviderKey,
            isProduction = providerStatus.IsProduction,
            providerMode = providerStatus.ProviderMode,
        });
    }
}

public static class SaasBillingApiServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSSaasBillingApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCrevaFactOSSaasBilling();
        services.AddCrevaFactOSSaasPayments(configuration);
        return services;
    }
}

public sealed record UpdateBillingAccountRequest(
    string LegalName,
    string DisplayName,
    string BillingContactMasked,
    string BillingAddressSnapshot,
    string? TaxCountryCode,
    string? TaxNumberMasked,
    string PreferredBillingCadence,
    string? PreferredPaymentMethod,
    long ExpectedVersion,
    string? IdempotencyKey);

public sealed record CreateDraftInvoiceRequest(
    Guid SubscriptionId,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    string? IdempotencyKey);

public sealed record TransitionInvoiceRequest(
    Guid InvoiceId,
    string TargetStatus,
    long ExpectedVersion,
    string? IdempotencyKey);

public sealed record CreatePaymentAttemptRequest(
    Guid InvoiceId,
    string PaymentMethod,
    decimal Amount,
    string CurrencyCode,
    string? ExternalReferenceMasked,
    string? IdempotencyKey);

public sealed record VerifyBankTransferRequest(
    Guid PaymentAttemptId,
    long ExpectedVersion,
    string? IdempotencyKey);
