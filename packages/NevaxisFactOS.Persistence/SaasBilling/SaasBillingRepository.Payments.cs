using NevaxisFactOS.Persistence.SaasBilling.Payments;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.SaasBilling;

public sealed partial class SaasBillingRepository
{
    public Task<SaasBillingWriteResult<InitializeCardCheckoutResult>> InitializeCardCheckoutAsync(
        InitializeCardCheckoutCommand command,
        ISaasPaymentProvider paymentProvider,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default) =>
        ExecuteMutationAsync(
            command.IdempotencyKey,
            "billing.checkout.initialize",
            command,
            async ct => await InitializeCardCheckoutCoreAsync(
                command,
                paymentProvider,
                actorReference,
                correlationId,
                ct),
            async ct => await LoadInitializeCardCheckoutReplayAsync(command.IdempotencyKey!, ct),
            cancellationToken);

    public Task<SaasBillingWriteResult> VerifyCheckoutReturnAsync(
        VerifyCheckoutReturnCommand command,
        ISaasPaymentProvider paymentProvider,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default) =>
        ExecuteMutationAsync(
            command.IdempotencyKey,
            "billing.checkout.verify-return",
            command,
            async ct => await VerifyCheckoutReturnWithProviderAsync(
                command,
                paymentProvider,
                actorReference,
                correlationId,
                ct),
            cancellationToken);

    public Task<SaasBillingWriteResult> ProcessProviderWebhookAsync(
        ProcessProviderWebhookCommand command,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default) =>
        ExecuteMutationAsync(
            command.IdempotencyKey,
            "billing.provider-webhook.process",
            command,
            async ct => await ProcessProviderWebhookCoreAsync(
                command,
                actorReference,
                correlationId,
                ct),
            cancellationToken);

    public Task<SaasBillingPaymentAttemptDetailReadModel?> GetPaymentAttemptDetailAsync(
        Guid paymentAttemptId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var attempt = await dbContext.SaasBillingPaymentAttempts.AsNoTracking()
                    .SingleOrDefaultAsync(item => item.Id == paymentAttemptId, ct);
                if (attempt is null)
                {
                    return null;
                }

                var session = await dbContext.SaasBillingCheckoutSessions.AsNoTracking()
                    .SingleOrDefaultAsync(item => item.PaymentAttemptId == attempt.Id, ct);
                return MapPaymentDetail(attempt, session);
            },
            cancellationToken);

    public Task<SaasBillingCheckoutSession?> FindCheckoutSessionByReferenceAsync(
        string checkoutSessionReferenceMasked,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct => await dbContext.SaasBillingCheckoutSessions.AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.CheckoutSessionReferenceMasked == checkoutSessionReferenceMasked.Trim(),
                    ct),
            cancellationToken);

    private async Task<SaasBillingWriteResult> VerifyCheckoutReturnWithProviderAsync(
        VerifyCheckoutReturnCommand command,
        ISaasPaymentProvider paymentProvider,
        string actorReference,
        Guid? correlationId,
        CancellationToken cancellationToken)
    {
        var attempt = await dbContext.SaasBillingPaymentAttempts
            .SingleOrDefaultAsync(item => item.Id == command.PaymentAttemptId, cancellationToken);
        if (attempt is null)
        {
            return SaasBillingWriteResult.Fail(
                SaasBillingFailureCodes.NotFound,
                "Payment attempt was not found.");
        }

        var maskedReference = MaskCheckoutSessionToken(command.CheckoutSessionToken);
        var session = await dbContext.SaasBillingCheckoutSessions
            .SingleOrDefaultAsync(
                item => item.PaymentAttemptId == attempt.Id
                    && item.CheckoutSessionReferenceMasked == maskedReference,
                cancellationToken);
        if (session is null)
        {
            return SaasBillingWriteResult.Fail(
                SaasBillingFailureCodes.NotFound,
                "Checkout session was not found.");
        }

        var providerResult = await paymentProvider.RetrieveCheckoutResultAsync(
            new SaasPaymentCheckoutVerificationRequest(
                command.CheckoutSessionToken.Trim(),
                session.ConversationId),
            cancellationToken);

        return await VerifyCheckoutReturnCoreAsync(
            command,
            providerResult,
            actorReference,
            correlationId,
            cancellationToken);
    }

    private async Task<SaasBillingWriteResult<InitializeCardCheckoutResult>> InitializeCardCheckoutCoreAsync(
        InitializeCardCheckoutCommand command,
        ISaasPaymentProvider paymentProvider,
        string actorReference,
        Guid? correlationId,
        CancellationToken cancellationToken)
    {
        var invoice = await dbContext.SaasBillingInvoices
            .SingleOrDefaultAsync(item => item.Id == command.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return SaasBillingWriteResult<InitializeCardCheckoutResult>.Fail(
                SaasBillingFailureCodes.NotFound,
                "Invoice was not found.");
        }

        if (invoice.Version != command.InvoiceExpectedVersion)
        {
            return SaasBillingWriteResult<InitializeCardCheckoutResult>.Fail(
                SaasBillingFailureCodes.ConcurrencyConflict,
                "Invoice version is stale.");
        }

        if (invoice.InvoiceStatus is not (
            SaasBillingInvoiceStatuses.Open or SaasBillingInvoiceStatuses.Overdue))
        {
            return SaasBillingWriteResult<InitializeCardCheckoutResult>.Fail(
                SaasBillingFailureCodes.ValidationFailed,
                "Invoice is not payable.");
        }

        var outstanding = await CalculateOutstandingAsync(invoice.Id, invoice.TotalAmount, cancellationToken);
        if (outstanding <= 0)
        {
            return SaasBillingWriteResult<InitializeCardCheckoutResult>.Fail(
                SaasBillingFailureCodes.ValidationFailed,
                "Invoice has no outstanding balance.");
        }

        var providerStatus = paymentProvider.GetStatus();
        if (!providerStatus.IsConfigured || !providerStatus.CardCheckoutAvailable)
        {
            return SaasBillingWriteResult<InitializeCardCheckoutResult>.Fail(
                SaasPaymentFailureCodes.ProviderNotConfigured,
                "Card checkout provider is not configured.");
        }

        var account = await dbContext.SaasTenantBillingAccounts.AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var tenantId = invoice.TenantId;
        var paymentAttemptId = Guid.NewGuid();
        var conversationId = $"{tenantId:N}-{paymentAttemptId:N}";
        var buyerReference = account?.DisplayName?.Trim() ?? "tenant";

        var providerResult = await paymentProvider.InitializeHostedCheckoutAsync(
            new SaasPaymentCheckoutInitializationRequest(
                tenantId,
                invoice.Id,
                paymentAttemptId,
                conversationId,
                outstanding,
                invoice.CurrencyCode.ToUpperInvariant(),
                buyerReference,
                string.Empty),
            cancellationToken);

        if (!providerResult.Succeeded)
        {
            return SaasBillingWriteResult<InitializeCardCheckoutResult>.Fail(
                providerResult.FailureCode ?? SaasPaymentFailureCodes.ProviderUnavailable,
                "Card checkout could not be initialized.");
        }

        if (string.IsNullOrWhiteSpace(providerResult.CheckoutSessionReferenceMasked)
            || string.IsNullOrWhiteSpace(providerResult.HostedCheckoutUrl)
            || !providerResult.ExpiresAtUtc.HasValue)
        {
            return SaasBillingWriteResult<InitializeCardCheckoutResult>.Fail(
                SaasPaymentFailureCodes.ValidationFailed,
                "Provider checkout response is incomplete.");
        }

        var attempt = new SaasBillingPaymentAttempt
        {
            Id = paymentAttemptId,
            TenantId = tenantId,
            InvoiceId = invoice.Id,
            PaymentMethod = SaasBillingPaymentMethods.Card,
            ProviderKey = SaasPaymentProviderKeys.Iyzico,
            Amount = outstanding,
            CurrencyCode = invoice.CurrencyCode.ToUpperInvariant(),
            PaymentStatus = SaasBillingPaymentStatuses.Pending,
            IdempotencyKey = command.IdempotencyKey,
            Version = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        dbContext.SaasBillingPaymentAttempts.Add(attempt);

        var checkoutSession = new SaasBillingCheckoutSession
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PaymentAttemptId = attempt.Id,
            InvoiceId = invoice.Id,
            ProviderKey = SaasPaymentProviderKeys.Iyzico,
            ConversationId = conversationId,
            CheckoutSessionReferenceMasked = providerResult.CheckoutSessionReferenceMasked.Trim(),
            HostedCheckoutUrl = providerResult.HostedCheckoutUrl.Trim(),
            SessionStatus = SaasBillingCheckoutSessionStatuses.Pending,
            ExpiresAtUtc = providerResult.ExpiresAtUtc.Value,
            Version = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        dbContext.SaasBillingCheckoutSessions.Add(checkoutSession);

        AppendAudit(
            tenantId,
            SaasBillingAuditEventTypes.CheckoutInitialized,
            SaasBillingAuditEntityTypes.PaymentAttempt,
            attempt.Id.ToString("D"),
            actorReference,
            new
            {
                PaymentAttemptId = attempt.Id,
                CheckoutSessionId = checkoutSession.Id,
                attempt.InvoiceId,
                checkoutSession.CheckoutSessionReferenceMasked,
            },
            correlationId,
            now);

        await dbContext.SaveChangesAsync(cancellationToken);
        return SaasBillingWriteResult<InitializeCardCheckoutResult>.Ok(
            new InitializeCardCheckoutResult(
                attempt.Id,
                checkoutSession.Id,
                checkoutSession.HostedCheckoutUrl!,
                attempt.Version));
    }

    private async Task<SaasBillingWriteResult<InitializeCardCheckoutResult>> LoadInitializeCardCheckoutReplayAsync(
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var attempt = await dbContext.SaasBillingPaymentAttempts.AsNoTracking()
            .SingleOrDefaultAsync(item => item.IdempotencyKey == idempotencyKey, cancellationToken);
        if (attempt is null)
        {
            return SaasBillingWriteResult<InitializeCardCheckoutResult>.Fail(
                SaasBillingFailureCodes.IdempotencyUnverifiable,
                "Idempotent retry cannot be verified.");
        }

        var session = await dbContext.SaasBillingCheckoutSessions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.PaymentAttemptId == attempt.Id, cancellationToken);
        if (session is null || string.IsNullOrWhiteSpace(session.HostedCheckoutUrl))
        {
            return SaasBillingWriteResult<InitializeCardCheckoutResult>.Fail(
                SaasBillingFailureCodes.IdempotencyUnverifiable,
                "Idempotent retry cannot be verified.");
        }

        return SaasBillingWriteResult<InitializeCardCheckoutResult>.Ok(
            new InitializeCardCheckoutResult(
                attempt.Id,
                session.Id,
                session.HostedCheckoutUrl,
                attempt.Version),
            idempotentReplay: true);
    }

    private async Task<SaasBillingWriteResult> VerifyCheckoutReturnCoreAsync(
        VerifyCheckoutReturnCommand command,
        SaasPaymentCheckoutVerificationResult providerResult,
        string actorReference,
        Guid? correlationId,
        CancellationToken cancellationToken)
    {
        var attempt = await dbContext.SaasBillingPaymentAttempts
            .SingleOrDefaultAsync(item => item.Id == command.PaymentAttemptId, cancellationToken);
        if (attempt is null)
        {
            return SaasBillingWriteResult.Fail(
                SaasBillingFailureCodes.NotFound,
                "Payment attempt was not found.");
        }

        if (attempt.PaymentMethod != SaasBillingPaymentMethods.Card)
        {
            return SaasBillingWriteResult.Fail(
                SaasBillingFailureCodes.ValidationFailed,
                "Only card payments can be verified through checkout return.");
        }

        var maskedReference = MaskCheckoutSessionToken(command.CheckoutSessionToken);
        var session = await dbContext.SaasBillingCheckoutSessions
            .SingleOrDefaultAsync(
                item => item.PaymentAttemptId == attempt.Id
                    && item.CheckoutSessionReferenceMasked == maskedReference,
                cancellationToken);
        if (session is null)
        {
            return SaasBillingWriteResult.Fail(
                SaasBillingFailureCodes.NotFound,
                "Checkout session was not found.");
        }

        if (IsTerminalPaymentStatus(attempt.PaymentStatus))
        {
            return SaasBillingWriteResult.Ok(attempt.Id, idempotentReplay: true);
        }

        if (!providerResult.Succeeded)
        {
            return SaasBillingWriteResult.Fail(
                providerResult.FailureCode ?? SaasPaymentFailureCodes.CheckoutVerificationFailed,
                "Checkout return could not be verified.");
        }

        var invoice = await dbContext.SaasBillingInvoices
            .SingleAsync(item => item.Id == attempt.InvoiceId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(providerResult.ProviderPaymentReferenceMasked))
        {
            attempt.ExternalReferenceMasked = providerResult.ProviderPaymentReferenceMasked.Trim();
        }

        var reconcileResult = await ReconcilePaymentAttemptAsync(
            attempt,
            invoice,
            providerResult.NormalizedStatus,
            providerResult.PaidAmount,
            providerResult.CurrencyCode,
            actorReference,
            correlationId,
            now,
            cancellationToken);
        if (!reconcileResult.Succeeded)
        {
            return reconcileResult;
        }

        UpdateCheckoutSessionStatus(session, providerResult.NormalizedStatus, now);

        AppendAudit(
            attempt.TenantId,
            SaasBillingAuditEventTypes.CheckoutVerified,
            SaasBillingAuditEntityTypes.PaymentAttempt,
            attempt.Id.ToString("D"),
            actorReference,
            new
            {
                PaymentAttemptId = attempt.Id,
                CheckoutSessionId = session.Id,
                providerResult.NormalizedStatus,
                providerResult.PaidAmount,
            },
            correlationId,
            now);

        await dbContext.SaveChangesAsync(cancellationToken);
        return SaasBillingWriteResult.Ok(attempt.Id);
    }

    private async Task<SaasBillingWriteResult> ProcessProviderWebhookCoreAsync(
        ProcessProviderWebhookCommand command,
        string actorReference,
        Guid? correlationId,
        CancellationToken cancellationToken)
    {
        var existingEvent = await dbContext.SaasBillingProviderEvents.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.ProviderEventIdentityHash == command.ProviderEventIdentityHash,
                cancellationToken);
        if (existingEvent is not null)
        {
            return SaasBillingWriteResult.Ok(existingEvent.PaymentAttemptId, idempotentReplay: true);
        }

        SaasBillingPaymentAttempt? attempt = null;
        SaasBillingCheckoutSession? session = null;
        if (!string.IsNullOrWhiteSpace(command.CheckoutSessionReferenceMasked))
        {
            session = await dbContext.SaasBillingCheckoutSessions
                .SingleOrDefaultAsync(
                    item => item.CheckoutSessionReferenceMasked == command.CheckoutSessionReferenceMasked.Trim(),
                    cancellationToken);
            if (session is not null)
            {
                attempt = await dbContext.SaasBillingPaymentAttempts
                    .SingleOrDefaultAsync(item => item.Id == session.PaymentAttemptId, cancellationToken);
            }
        }

        if (attempt is null && !string.IsNullOrWhiteSpace(command.ProviderPaymentReferenceMasked))
        {
            attempt = await dbContext.SaasBillingPaymentAttempts
                .SingleOrDefaultAsync(
                    item => item.ExternalReferenceMasked == command.ProviderPaymentReferenceMasked.Trim(),
                    cancellationToken);
        }

        var tenantId = attempt?.TenantId ?? await ResolveTenantIdAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        dbContext.SaasBillingProviderEvents.Add(new SaasBillingProviderEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProviderKey = command.ProviderKey,
            ProviderEventIdentityHash = command.ProviderEventIdentityHash,
            PaymentAttemptId = attempt?.Id,
            InvoiceId = attempt?.InvoiceId ?? session?.InvoiceId,
            NormalizedStatus = command.NormalizedStatus,
            Amount = command.Amount,
            CurrencyCode = command.CurrencyCode,
            SanitizedEvidenceJson = command.SanitizedEvidenceJson,
            IdempotencyKey = command.IdempotencyKey,
            ProcessedAtUtc = now,
        });

        if (attempt is not null)
        {
            if (IsTerminalPaymentStatus(attempt.PaymentStatus))
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return SaasBillingWriteResult.Ok(attempt.Id);
            }

            var invoice = await dbContext.SaasBillingInvoices
                .SingleAsync(item => item.Id == attempt.InvoiceId, cancellationToken);

            if (!string.IsNullOrWhiteSpace(command.ProviderPaymentReferenceMasked))
            {
                attempt.ExternalReferenceMasked = command.ProviderPaymentReferenceMasked.Trim();
            }

            var reconcileResult = await ReconcilePaymentAttemptAsync(
                attempt,
                invoice,
                command.NormalizedStatus,
                command.Amount,
                command.CurrencyCode,
                actorReference,
                correlationId,
                now,
                cancellationToken);
            if (!reconcileResult.Succeeded)
            {
                return reconcileResult;
            }

            if (session is not null)
            {
                UpdateCheckoutSessionStatus(session, command.NormalizedStatus, now);
            }
        }

        AppendAudit(
            tenantId,
            SaasBillingAuditEventTypes.ProviderWebhookProcessed,
            SaasBillingAuditEntityTypes.PaymentAttempt,
            attempt?.Id.ToString("D") ?? command.ProviderEventIdentityHash,
            actorReference,
            new
            {
                command.ProviderKey,
                command.ProviderEventIdentityHash,
                command.NormalizedStatus,
                PaymentAttemptId = attempt?.Id,
            },
            correlationId,
            now);

        await dbContext.SaveChangesAsync(cancellationToken);
        return SaasBillingWriteResult.Ok(attempt?.Id);
    }

    private async Task<SaasBillingWriteResult> ReconcilePaymentAttemptAsync(
        SaasBillingPaymentAttempt attempt,
        SaasBillingInvoice invoice,
        string normalizedStatus,
        decimal? paidAmount,
        string? currencyCode,
        string actorReference,
        Guid? correlationId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (IsTerminalPaymentStatus(attempt.PaymentStatus))
        {
            return SaasBillingWriteResult.Ok(attempt.Id, idempotentReplay: true);
        }

        switch (normalizedStatus)
        {
            case SaasPaymentNormalizedStatuses.Succeeded:
                if (!string.IsNullOrWhiteSpace(currencyCode)
                    && !string.Equals(
                        attempt.CurrencyCode,
                        currencyCode,
                        StringComparison.OrdinalIgnoreCase))
                {
                    attempt.PaymentStatus = SaasBillingPaymentStatuses.Failed;
                    attempt.FailureCode = SaasPaymentFailureCodes.CurrencyMismatch;
                    attempt.UpdatedAtUtc = now;
                    attempt.Version++;
                    return SaasBillingWriteResult.Fail(
                        SaasPaymentFailureCodes.CurrencyMismatch,
                        "Payment currency does not match invoice currency.");
                }

                if (!paidAmount.HasValue || paidAmount.Value != attempt.Amount)
                {
                    attempt.PaymentStatus = SaasBillingPaymentStatuses.Failed;
                    attempt.FailureCode = SaasPaymentFailureCodes.ReconciliationReviewRequired;
                    attempt.UpdatedAtUtc = now;
                    attempt.Version++;
                    AppendAudit(
                        attempt.TenantId,
                        SaasBillingAuditEventTypes.PaymentAttemptFailed,
                        SaasBillingAuditEntityTypes.PaymentAttempt,
                        attempt.Id.ToString("D"),
                        actorReference,
                        new { attempt.Id, attempt.InvoiceId, paidAmount, attempt.Amount },
                        correlationId,
                        now);
                    return SaasBillingWriteResult.Fail(
                        SaasPaymentFailureCodes.ReconciliationReviewRequired,
                        "Payment amount requires manual reconciliation review.");
                }

                attempt.PaymentStatus = SaasBillingPaymentStatuses.Succeeded;
                attempt.FailureCode = null;
                attempt.UpdatedAtUtc = now;
                attempt.Version++;

                var outstanding = await CalculateOutstandingAsync(invoice.Id, invoice.TotalAmount, cancellationToken);
                if (attempt.Amount >= outstanding)
                {
                    if (!SaasBillingValueConversions.IsValidInvoiceTransition(
                            invoice.InvoiceStatus,
                            SaasBillingInvoiceStatuses.Paid))
                    {
                        return SaasBillingWriteResult.Fail(
                            SaasBillingFailureCodes.ReconciliationFailed,
                            "Invoice cannot be marked paid.");
                    }

                    invoice.InvoiceStatus = SaasBillingInvoiceStatuses.Paid;
                    invoice.PaidAtUtc = now;
                    invoice.UpdatedAtUtc = now;
                    invoice.Version++;
                }

                AppendAudit(
                    attempt.TenantId,
                    SaasBillingAuditEventTypes.PaymentAttemptVerified,
                    SaasBillingAuditEntityTypes.PaymentAttempt,
                    attempt.Id.ToString("D"),
                    actorReference,
                    new { attempt.Id, attempt.InvoiceId, attempt.Amount },
                    correlationId,
                    now);
                return SaasBillingWriteResult.Ok(attempt.Id);

            case SaasPaymentNormalizedStatuses.Failed:
                attempt.PaymentStatus = SaasBillingPaymentStatuses.Failed;
                attempt.FailureCode = SaasPaymentFailureCodes.CheckoutVerificationFailed;
                attempt.UpdatedAtUtc = now;
                attempt.Version++;
                AppendAudit(
                    attempt.TenantId,
                    SaasBillingAuditEventTypes.PaymentAttemptFailed,
                    SaasBillingAuditEntityTypes.PaymentAttempt,
                    attempt.Id.ToString("D"),
                    actorReference,
                    new { attempt.Id, attempt.InvoiceId },
                    correlationId,
                    now);
                return SaasBillingWriteResult.Ok(attempt.Id);

            case SaasPaymentNormalizedStatuses.Cancelled:
                attempt.PaymentStatus = SaasBillingPaymentStatuses.Cancelled;
                attempt.UpdatedAtUtc = now;
                attempt.Version++;
                return SaasBillingWriteResult.Ok(attempt.Id);

            case SaasPaymentNormalizedStatuses.Refunded:
                attempt.PaymentStatus = SaasBillingPaymentStatuses.Refunded;
                attempt.UpdatedAtUtc = now;
                attempt.Version++;
                return SaasBillingWriteResult.Ok(attempt.Id);

            default:
                return SaasBillingWriteResult.Ok(attempt.Id);
        }
    }

    private static void UpdateCheckoutSessionStatus(
        SaasBillingCheckoutSession session,
        string normalizedStatus,
        DateTimeOffset now)
    {
        session.SessionStatus = normalizedStatus switch
        {
            SaasPaymentNormalizedStatuses.Succeeded => SaasBillingCheckoutSessionStatuses.Completed,
            SaasPaymentNormalizedStatuses.Cancelled => SaasBillingCheckoutSessionStatuses.Cancelled,
            SaasPaymentNormalizedStatuses.Failed when session.ExpiresAtUtc <= now =>
                SaasBillingCheckoutSessionStatuses.Expired,
            SaasPaymentNormalizedStatuses.Failed => SaasBillingCheckoutSessionStatuses.Cancelled,
            _ => session.SessionStatus,
        };
        session.UpdatedAtUtc = now;
        session.Version++;
    }

    private static string MaskCheckoutSessionToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return "tok_***";
        }

        var trimmed = token.Trim();
        if (trimmed.Length <= 4)
        {
            return "tok_***";
        }

        return $"tok_***{trimmed[^4..]}";
    }

    private static bool IsTerminalPaymentStatus(string paymentStatus) =>
        paymentStatus is SaasBillingPaymentStatuses.Succeeded
            or SaasBillingPaymentStatuses.Failed
            or SaasBillingPaymentStatuses.Cancelled
            or SaasBillingPaymentStatuses.Refunded;

    private static SaasBillingPaymentAttemptDetailReadModel MapPaymentDetail(
        SaasBillingPaymentAttempt attempt,
        SaasBillingCheckoutSession? session) =>
        new(
            attempt.Id,
            attempt.InvoiceId,
            attempt.PaymentMethod,
            attempt.ProviderKey,
            attempt.ExternalReferenceMasked,
            attempt.Amount,
            attempt.CurrencyCode,
            attempt.PaymentStatus,
            attempt.FailureCode,
            attempt.Version,
            attempt.CreatedAtUtc,
            attempt.UpdatedAtUtc,
            session?.Id,
            session?.SessionStatus);
}
