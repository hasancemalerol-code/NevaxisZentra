using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CrevaFactOS.Persistence.SaasCommercial;
using CrevaFactOS.Persistence.SaasBilling.Payments;
using CrevaFactOS.Persistence.SaasSubscription;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CrevaFactOS.Persistence.SaasBilling;

public sealed partial class SaasBillingRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : ISaasBillingRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public Task<SaasBillingWorkspaceSummaryReadModel> GetWorkspaceSummaryAsync(
        SaasBillingProviderStatusReadModel providerStatus,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var account = await dbContext.SaasTenantBillingAccounts.AsNoTracking()
                    .SingleOrDefaultAsync(ct);
                var invoices = await dbContext.SaasBillingInvoices.AsNoTracking()
                    .OrderByDescending(item => item.CreatedAtUtc)
                    .Take(50)
                    .ToListAsync(ct);
                var payments = await dbContext.SaasBillingPaymentAttempts.AsNoTracking()
                    .OrderByDescending(item => item.CreatedAtUtc)
                    .Take(20)
                    .ToListAsync(ct);

                return new SaasBillingWorkspaceSummaryReadModel(
                    account is null ? null : MapAccount(account),
                    invoices.Select(MapInvoiceSummary).ToList(),
                    payments.Select(MapPayment).ToList(),
                    providerStatus,
                    invoices.Count(item => item.InvoiceStatus == SaasBillingInvoiceStatuses.Open),
                    invoices.Count(item => item.InvoiceStatus == SaasBillingInvoiceStatuses.Overdue),
                    payments.Count(item =>
                        item.PaymentStatus == SaasBillingPaymentStatuses.AwaitingManualVerification));
            },
            cancellationToken);

    public Task<SaasBillingAccountReadModel?> GetBillingAccountAsync(
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var account = await dbContext.SaasTenantBillingAccounts.AsNoTracking()
                    .SingleOrDefaultAsync(ct);
                return account is null ? null : MapAccount(account);
            },
            cancellationToken);

    public Task<SaasBillingInvoiceReadModel?> GetInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var invoice = await dbContext.SaasBillingInvoices.AsNoTracking()
                    .Include(item => item.Lines)
                    .SingleOrDefaultAsync(item => item.Id == invoiceId, ct);
                return invoice is null ? null : MapInvoice(invoice);
            },
            cancellationToken);

    public Task<IReadOnlyList<SaasBillingInvoiceSummaryReadModel>> ListInvoicesAsync(
        string? statusFilter,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        string? search,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var query = dbContext.SaasBillingInvoices.AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(statusFilter))
                {
                    query = query.Where(item => item.InvoiceStatus == statusFilter);
                }

                if (fromUtc.HasValue)
                {
                    query = query.Where(item => item.PeriodStartUtc >= fromUtc.Value);
                }

                if (toUtc.HasValue)
                {
                    query = query.Where(item => item.PeriodEndUtc <= toUtc.Value);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim();
                    query = query.Where(item => item.InvoiceNumber.Contains(term));
                }

                var invoices = await query
                    .OrderByDescending(item => item.CreatedAtUtc)
                    .Take(200)
                    .ToListAsync(ct);
                return (IReadOnlyList<SaasBillingInvoiceSummaryReadModel>)invoices
                    .Select(MapInvoiceSummary)
                    .ToList();
            },
            cancellationToken);

    public Task<IReadOnlyList<SaasBillingPaymentAttemptReadModel>> ListPaymentAttemptsAsync(
        Guid? invoiceId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var query = dbContext.SaasBillingPaymentAttempts.AsNoTracking().AsQueryable();
                if (invoiceId.HasValue)
                {
                    query = query.Where(item => item.InvoiceId == invoiceId.Value);
                }

                var attempts = await query
                    .OrderByDescending(item => item.CreatedAtUtc)
                    .Take(100)
                    .ToListAsync(ct);
                return (IReadOnlyList<SaasBillingPaymentAttemptReadModel>)attempts
                    .Select(MapPayment)
                    .ToList();
            },
            cancellationToken);

    public Task<SaasBillingWriteResult> UpdateBillingAccountAsync(
        UpdateSaasBillingAccountCommand command,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default) =>
        ExecuteMutationAsync(
            command.IdempotencyKey,
            "billing.account.update",
            command,
            async ct =>
            {
                if (!SaasCommercialValueConversions.IsValidBillingCadence(command.PreferredBillingCadence))
                {
                    return SaasBillingWriteResult.Fail(
                        SaasBillingFailureCodes.ValidationFailed,
                        "Preferred billing cadence is invalid.");
                }

                if (command.PreferredPaymentMethod is not null
                    && !SaasBillingValueConversions.IsValidPaymentMethod(command.PreferredPaymentMethod))
                {
                    return SaasBillingWriteResult.Fail(
                        SaasBillingFailureCodes.ValidationFailed,
                        "Preferred payment method is invalid.");
                }

                var tenantId = await ResolveTenantIdAsync(ct);
                var now = DateTimeOffset.UtcNow;
                var account = await dbContext.SaasTenantBillingAccounts
                    .SingleOrDefaultAsync(ct);
                if (account is null)
                {
                    account = new SaasTenantBillingAccount
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        CreatedAtUtc = now,
                        Version = 1,
                    };
                    dbContext.SaasTenantBillingAccounts.Add(account);
                }
                else if (account.Version != command.ExpectedVersion)
                {
                    return SaasBillingWriteResult.Fail(
                        SaasBillingFailureCodes.ConcurrencyConflict,
                        "Billing account version is stale.");
                }

                account.LegalName = command.LegalName.Trim();
                account.DisplayName = command.DisplayName.Trim();
                account.BillingContactMasked = command.BillingContactMasked.Trim();
                account.BillingAddressSnapshot = command.BillingAddressSnapshot.Trim();
                account.TaxCountryCode = command.TaxCountryCode?.Trim();
                account.TaxNumberMasked = command.TaxNumberMasked?.Trim();
                account.PreferredBillingCadence = command.PreferredBillingCadence;
                account.PreferredPaymentMethod = command.PreferredPaymentMethod;
                account.UpdatedAtUtc = now;
                account.Version++;

                AppendAudit(
                    tenantId,
                    SaasBillingAuditEventTypes.BillingAccountUpdated,
                    SaasBillingAuditEntityTypes.BillingAccount,
                    account.Id.ToString("D"),
                    actorReference,
                    new { account.Id, account.Version },
                    correlationId,
                    now);

                await dbContext.SaveChangesAsync(ct);
                return SaasBillingWriteResult.Ok(account.Id);
            },
            cancellationToken);

    public Task<SaasBillingWriteResult> CreateDraftInvoiceAsync(
        CreateSaasBillingInvoiceCommand command,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default) =>
        ExecuteMutationAsync(
            command.IdempotencyKey,
            "billing.invoice.create",
            command,
            async ct => await CreateDraftInvoiceCoreAsync(command, actorReference, correlationId, ct),
            cancellationToken);

    public Task<SaasBillingWriteResult> TransitionInvoiceAsync(
        TransitionSaasBillingInvoiceCommand command,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default) =>
        ExecuteMutationAsync(
            command.IdempotencyKey,
            "billing.invoice.transition",
            command,
            async ct =>
            {
                var invoice = await dbContext.SaasBillingInvoices
                    .Include(item => item.Lines)
                    .SingleOrDefaultAsync(item => item.Id == command.InvoiceId, ct);
                if (invoice is null)
                {
                    return SaasBillingWriteResult.Fail(
                        SaasBillingFailureCodes.NotFound,
                        "Invoice was not found.");
                }

                if (invoice.Version != command.ExpectedVersion)
                {
                    return SaasBillingWriteResult.Fail(
                        SaasBillingFailureCodes.ConcurrencyConflict,
                        "Invoice version is stale.");
                }

                if (!SaasBillingValueConversions.IsValidInvoiceTransition(
                        invoice.InvoiceStatus,
                        command.TargetStatus))
                {
                    return SaasBillingWriteResult.Fail(
                        SaasBillingFailureCodes.InvalidTransition,
                        "Invoice transition is not allowed.");
                }

                var now = DateTimeOffset.UtcNow;
                var fromStatus = invoice.InvoiceStatus;
                invoice.InvoiceStatus = command.TargetStatus;
                invoice.UpdatedAtUtc = now;
                invoice.Version++;

                switch (command.TargetStatus)
                {
                    case SaasBillingInvoiceStatuses.Open:
                        invoice.IssuedAtUtc = now;
                        invoice.DueAtUtc = now.AddDays(14);
                        AppendAudit(
                            invoice.TenantId,
                            SaasBillingAuditEventTypes.InvoiceOpened,
                            SaasBillingAuditEntityTypes.Invoice,
                            invoice.Id.ToString("D"),
                            actorReference,
                            new { invoice.Id, fromStatus, toStatus = command.TargetStatus },
                            correlationId,
                            now);
                        break;
                    case SaasBillingInvoiceStatuses.Void:
                        invoice.VoidedAtUtc = now;
                        AppendAudit(
                            invoice.TenantId,
                            SaasBillingAuditEventTypes.InvoiceVoided,
                            SaasBillingAuditEntityTypes.Invoice,
                            invoice.Id.ToString("D"),
                            actorReference,
                            new { invoice.Id, fromStatus, toStatus = command.TargetStatus },
                            correlationId,
                            now);
                        break;
                    case SaasBillingInvoiceStatuses.Paid:
                        invoice.PaidAtUtc = now;
                        break;
                    case SaasBillingInvoiceStatuses.Overdue:
                        break;
                }

                await dbContext.SaveChangesAsync(ct);
                return SaasBillingWriteResult.Ok(invoice.Id);
            },
            cancellationToken);

    public Task<SaasBillingWriteResult> CreatePaymentAttemptAsync(
        CreateSaasBillingPaymentAttemptCommand command,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default) =>
        ExecuteMutationAsync(
            command.IdempotencyKey,
            "billing.payment.create",
            command,
            async ct =>
            {
                if (!SaasBillingValueConversions.IsValidPaymentMethod(command.PaymentMethod))
                {
                    return SaasBillingWriteResult.Fail(
                        SaasBillingFailureCodes.ValidationFailed,
                        "Payment method is invalid.");
                }

                var invoice = await dbContext.SaasBillingInvoices.AsNoTracking()
                    .SingleOrDefaultAsync(item => item.Id == command.InvoiceId, ct);
                if (invoice is null)
                {
                    return SaasBillingWriteResult.Fail(
                        SaasBillingFailureCodes.NotFound,
                        "Invoice was not found.");
                }

                if (invoice.InvoiceStatus is not (
                    SaasBillingInvoiceStatuses.Open or SaasBillingInvoiceStatuses.Overdue))
                {
                    return SaasBillingWriteResult.Fail(
                        SaasBillingFailureCodes.ValidationFailed,
                        "Invoice is not payable.");
                }

                if (!string.Equals(invoice.CurrencyCode, command.CurrencyCode, StringComparison.OrdinalIgnoreCase))
                {
                    return SaasBillingWriteResult.Fail(
                        SaasBillingFailureCodes.CurrencyMismatch,
                        "Payment currency does not match invoice currency.");
                }

                var outstanding = await CalculateOutstandingAsync(invoice.Id, invoice.TotalAmount, ct);
                if (command.Amount > outstanding)
                {
                    return SaasBillingWriteResult.Fail(
                        SaasBillingFailureCodes.AmountExceeded,
                        "Payment amount exceeds outstanding balance.");
                }

                var now = DateTimeOffset.UtcNow;
                var tenantId = invoice.TenantId;
                var status = command.PaymentMethod switch
                {
                    SaasBillingPaymentMethods.BankTransfer =>
                        SaasBillingPaymentStatuses.AwaitingManualVerification,
                    SaasBillingPaymentMethods.Card => SaasBillingPaymentStatuses.Failed,
                    SaasBillingPaymentMethods.EnterpriseInvoice => SaasBillingPaymentStatuses.Pending,
                    _ => SaasBillingPaymentStatuses.Pending,
                };
                var failureCode = command.PaymentMethod == SaasBillingPaymentMethods.Card
                    ? SaasBillingFailureCodes.ProviderNotConfigured
                    : null;

                var attempt = new SaasBillingPaymentAttempt
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    InvoiceId = invoice.Id,
                    PaymentMethod = command.PaymentMethod,
                    ProviderKey = command.PaymentMethod == SaasBillingPaymentMethods.Card
                        ? DevelopmentBillingPaymentProvider.ProviderKeyValue
                        : null,
                    ExternalReferenceMasked = command.ExternalReferenceMasked?.Trim(),
                    Amount = command.Amount,
                    CurrencyCode = command.CurrencyCode.ToUpperInvariant(),
                    PaymentStatus = status,
                    FailureCode = failureCode,
                    IdempotencyKey = command.IdempotencyKey,
                    Version = 1,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                };
                dbContext.SaasBillingPaymentAttempts.Add(attempt);

                AppendAudit(
                    tenantId,
                    SaasBillingAuditEventTypes.PaymentAttemptCreated,
                    SaasBillingAuditEntityTypes.PaymentAttempt,
                    attempt.Id.ToString("D"),
                    actorReference,
                    new { attempt.Id, attempt.InvoiceId, attempt.PaymentMethod, attempt.PaymentStatus },
                    correlationId,
                    now);

                await dbContext.SaveChangesAsync(ct);
                return SaasBillingWriteResult.Ok(attempt.Id);
            },
            cancellationToken);

    public Task<SaasBillingWriteResult> VerifyBankTransferAsync(
        VerifySaasBillingBankTransferCommand command,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default) =>
        ExecuteMutationAsync(
            command.IdempotencyKey,
            "billing.payment.verify-bank-transfer",
            command,
            async ct =>
            {
                var attempt = await dbContext.SaasBillingPaymentAttempts
                    .SingleOrDefaultAsync(item => item.Id == command.PaymentAttemptId, ct);
                if (attempt is null)
                {
                    return SaasBillingWriteResult.Fail(
                        SaasBillingFailureCodes.NotFound,
                        "Payment attempt was not found.");
                }

                if (attempt.Version != command.ExpectedVersion)
                {
                    return SaasBillingWriteResult.Fail(
                        SaasBillingFailureCodes.ConcurrencyConflict,
                        "Payment attempt version is stale.");
                }

                if (attempt.PaymentMethod != SaasBillingPaymentMethods.BankTransfer)
                {
                    return SaasBillingWriteResult.Fail(
                        SaasBillingFailureCodes.ValidationFailed,
                        "Only bank-transfer payments can be manually verified.");
                }

                if (attempt.PaymentStatus != SaasBillingPaymentStatuses.AwaitingManualVerification)
                {
                    return SaasBillingWriteResult.Fail(
                        SaasBillingFailureCodes.InvalidTransition,
                        "Payment attempt is not awaiting verification.");
                }

                var invoice = await dbContext.SaasBillingInvoices
                    .SingleAsync(item => item.Id == attempt.InvoiceId, ct);
                var now = DateTimeOffset.UtcNow;
                attempt.PaymentStatus = SaasBillingPaymentStatuses.Succeeded;
                attempt.UpdatedAtUtc = now;
                attempt.Version++;

                var outstanding = await CalculateOutstandingAsync(invoice.Id, invoice.TotalAmount, ct);
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

                await dbContext.SaveChangesAsync(ct);
                return SaasBillingWriteResult.Ok(attempt.Id);
            },
            cancellationToken);

    private async Task<SaasBillingWriteResult> CreateDraftInvoiceCoreAsync(
        CreateSaasBillingInvoiceCommand command,
        string actorReference,
        Guid? correlationId,
        CancellationToken cancellationToken)
    {
        var subscription = await dbContext.SaasTenantSubscriptions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.IsCurrent && item.Id == command.SubscriptionId, cancellationToken);
        if (subscription is null)
        {
            return SaasBillingWriteResult.Fail(
                SaasBillingFailureCodes.NotFound,
                "Subscription was not found.");
        }

        var account = await dbContext.SaasTenantBillingAccounts
            .SingleOrDefaultAsync(cancellationToken);
        if (account is null)
        {
            return SaasBillingWriteResult.Fail(
                SaasBillingFailureCodes.ValidationFailed,
                "Billing account must exist before creating invoices.");
        }

        var planVersion = await dbContext.SaasCommercialPlanVersions.AsNoTracking()
            .Include(item => item.PriceComponents)
            .SingleOrDefaultAsync(item => item.Id == subscription.CommercialPlanVersionId, cancellationToken);
        if (planVersion is null)
        {
            return SaasBillingWriteResult.Fail(
                SaasBillingFailureCodes.NotFound,
                "Commercial plan version was not found.");
        }

        var tenantId = subscription.TenantId;
        var now = DateTimeOffset.UtcNow;
        var sequence = await dbContext.SaasBillingInvoices.CountAsync(cancellationToken) + 1;
        var invoiceId = Guid.NewGuid();
        var lines = new List<SaasBillingInvoiceLine>();
        var sortOrder = 10;
        decimal subtotal = 0;

        foreach (var component in planVersion.PriceComponents.OrderBy(item => item.SortOrder))
        {
            if (component.Amount is null)
            {
                continue;
            }

            var lineSubtotal = component.Amount.Value;
            subtotal += lineSubtotal;
            lines.Add(new SaasBillingInvoiceLine
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                InvoiceId = invoiceId,
                DescriptionSnapshot = component.ComponentType,
                ComponentType = component.ComponentType,
                Quantity = 1,
                UnitAmount = component.Amount.Value,
                LineSubtotalAmount = lineSubtotal,
                SourceProvenance = $"{SaasBillingProvenance.BillingSystem};{planVersion.Id:D}",
                SortOrder = sortOrder,
                CreatedAtUtc = now,
            });
            sortOrder += 10;
        }

        if (lines.Count == 0)
        {
            return SaasBillingWriteResult.Fail(
                SaasBillingFailureCodes.ValidationFailed,
                "Plan price component amount is not configured.");
        }

        var invoice = new SaasBillingInvoice
        {
            Id = invoiceId,
            TenantId = tenantId,
            BillingAccountId = account.Id,
            SubscriptionId = subscription.Id,
            CommercialPlanVersionId = planVersion.Id,
            InvoiceNumber = SaasBillingValueConversions.FormatInvoiceNumber(tenantId, sequence),
            PeriodStartUtc = command.PeriodStartUtc,
            PeriodEndUtc = command.PeriodEndUtc,
            CurrencyCode = planVersion.CurrencyCode,
            SubtotalAmount = subtotal,
            TaxAmount = null,
            TotalAmount = subtotal,
            InvoiceStatus = SaasBillingInvoiceStatuses.Draft,
            SourceProvenance = SaasBillingProvenance.BillingSystem,
            IdempotencyKey = command.IdempotencyKey,
            InvoiceSequence = sequence,
            Version = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Lines = lines,
        };
        dbContext.SaasBillingInvoices.Add(invoice);

        AppendAudit(
            tenantId,
            SaasBillingAuditEventTypes.InvoiceCreated,
            SaasBillingAuditEntityTypes.Invoice,
            invoice.Id.ToString("D"),
            actorReference,
            new { invoice.Id, invoice.InvoiceNumber, invoice.TotalAmount },
            correlationId,
            now);

        await dbContext.SaveChangesAsync(cancellationToken);
        return SaasBillingWriteResult.Ok(invoice.Id);
    }

    private async Task<decimal> CalculateOutstandingAsync(
        Guid invoiceId,
        decimal totalAmount,
        CancellationToken cancellationToken)
    {
        var succeeded = await dbContext.SaasBillingPaymentAttempts.AsNoTracking()
            .Where(item =>
                item.InvoiceId == invoiceId
                && item.PaymentStatus == SaasBillingPaymentStatuses.Succeeded)
            .SumAsync(item => item.Amount, cancellationToken);
        return totalAmount - succeeded;
    }

    private async Task<SaasBillingWriteResult> ExecuteMutationAsync<TCommand>(
        string? idempotencyKey,
        string operation,
        TCommand command,
        Func<CancellationToken, Task<SaasBillingWriteResult>> action,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return SaasBillingWriteResult.Fail(
                SaasBillingFailureCodes.IdempotencyUnverifiable,
                "Idempotency key is required for billing mutations.");
        }

        return await tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                try
                {
                    return await ExecuteMutationCoreAsync(idempotencyKey, operation, command, action, ct);
                }
                catch (DbUpdateException exception) when (IsUniqueViolation(exception))
                {
                    dbContext.ChangeTracker.Clear();
                    if (string.IsNullOrWhiteSpace(idempotencyKey))
                    {
                        throw;
                    }

                    var hash = HashCommand(command);
                    var existing = await dbContext.SaasBillingIdempotencyRecords
                        .SingleOrDefaultAsync(record => record.IdempotencyKey == idempotencyKey, ct);
                    if (existing is null)
                    {
                        throw;
                    }

                    if (string.IsNullOrWhiteSpace(existing.RequestHash))
                    {
                        return SaasBillingWriteResult.Fail(
                            SaasBillingFailureCodes.IdempotencyUnverifiable,
                            "Idempotent retry cannot be verified.");
                    }

                    return existing.RequestHash == hash
                        ? SaasBillingWriteResult.Ok(idempotentReplay: true)
                        : SaasBillingWriteResult.Fail(
                            SaasBillingFailureCodes.IdempotencyConflict,
                            "Idempotent retry conflicts with the original request.");
                }
            },
            cancellationToken);
    }

    private async Task<SaasBillingWriteResult> ExecuteMutationCoreAsync<TCommand>(
        string? idempotencyKey,
        string operation,
        TCommand command,
        Func<CancellationToken, Task<SaasBillingWriteResult>> action,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return SaasBillingWriteResult.Fail(
                SaasBillingFailureCodes.IdempotencyUnverifiable,
                "Idempotency key is required for billing mutations.");
        }

        var hash = HashCommand(command);
        var existing = await dbContext.SaasBillingIdempotencyRecords
            .SingleOrDefaultAsync(record => record.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (string.IsNullOrWhiteSpace(existing.RequestHash))
            {
                return SaasBillingWriteResult.Fail(
                    SaasBillingFailureCodes.IdempotencyUnverifiable,
                    "Idempotent retry cannot be verified.");
            }

            return existing.RequestHash == hash
                ? SaasBillingWriteResult.Ok(idempotentReplay: true)
                : SaasBillingWriteResult.Fail(
                    SaasBillingFailureCodes.IdempotencyConflict,
                    "Idempotent retry conflicts with the original request.");
        }

        dbContext.SaasBillingIdempotencyRecords.Add(new SaasBillingIdempotencyRecord
        {
            Id = Guid.NewGuid(),
            TenantId = await ResolveTenantIdAsync(cancellationToken),
            IdempotencyKey = idempotencyKey,
            RequestHash = hash,
            Operation = operation,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        var result = await action(cancellationToken);
        if (!result.Succeeded)
        {
            dbContext.ChangeTracker.Clear();
        }

        return result;
    }

    private async Task<SaasBillingWriteResult<TValue>> ExecuteMutationAsync<TCommand, TValue>(
        string? idempotencyKey,
        string operation,
        TCommand command,
        Func<CancellationToken, Task<SaasBillingWriteResult<TValue>>> action,
        Func<CancellationToken, Task<SaasBillingWriteResult<TValue>>> replayAction,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return SaasBillingWriteResult<TValue>.Fail(
                SaasBillingFailureCodes.IdempotencyUnverifiable,
                "Idempotency key is required for billing mutations.");
        }

        return await tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                try
                {
                    return await ExecuteMutationCoreAsync(
                        idempotencyKey,
                        operation,
                        command,
                        action,
                        replayAction,
                        ct);
                }
                catch (DbUpdateException exception) when (IsUniqueViolation(exception))
                {
                    dbContext.ChangeTracker.Clear();
                    var hash = HashCommand(command);
                    var existing = await dbContext.SaasBillingIdempotencyRecords
                        .SingleOrDefaultAsync(record => record.IdempotencyKey == idempotencyKey, ct);
                    if (existing is null)
                    {
                        throw;
                    }

                    if (string.IsNullOrWhiteSpace(existing.RequestHash))
                    {
                        return SaasBillingWriteResult<TValue>.Fail(
                            SaasBillingFailureCodes.IdempotencyUnverifiable,
                            "Idempotent retry cannot be verified.");
                    }

                    return existing.RequestHash == hash
                        ? await replayAction(ct)
                        : SaasBillingWriteResult<TValue>.Fail(
                            SaasBillingFailureCodes.IdempotencyConflict,
                            "Idempotent retry conflicts with the original request.");
                }
            },
            cancellationToken);
    }

    private async Task<SaasBillingWriteResult<TValue>> ExecuteMutationCoreAsync<TCommand, TValue>(
        string? idempotencyKey,
        string operation,
        TCommand command,
        Func<CancellationToken, Task<SaasBillingWriteResult<TValue>>> action,
        Func<CancellationToken, Task<SaasBillingWriteResult<TValue>>> replayAction,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return SaasBillingWriteResult<TValue>.Fail(
                SaasBillingFailureCodes.IdempotencyUnverifiable,
                "Idempotency key is required for billing mutations.");
        }

        var hash = HashCommand(command);
        var existing = await dbContext.SaasBillingIdempotencyRecords
            .SingleOrDefaultAsync(record => record.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (string.IsNullOrWhiteSpace(existing.RequestHash))
            {
                return SaasBillingWriteResult<TValue>.Fail(
                    SaasBillingFailureCodes.IdempotencyUnverifiable,
                    "Idempotent retry cannot be verified.");
            }

            return existing.RequestHash == hash
                ? await replayAction(cancellationToken)
                : SaasBillingWriteResult<TValue>.Fail(
                    SaasBillingFailureCodes.IdempotencyConflict,
                    "Idempotent retry conflicts with the original request.");
        }

        dbContext.SaasBillingIdempotencyRecords.Add(new SaasBillingIdempotencyRecord
        {
            Id = Guid.NewGuid(),
            TenantId = await ResolveTenantIdAsync(cancellationToken),
            IdempotencyKey = idempotencyKey,
            RequestHash = hash,
            Operation = operation,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        var result = await action(cancellationToken);
        if (!result.Succeeded)
        {
            dbContext.ChangeTracker.Clear();
        }

        return result;
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgresException
        && postgresException.SqlState == "23505";

    private async Task<Guid> ResolveTenantIdAsync(CancellationToken cancellationToken)
    {
        var tenant = await dbContext.Tenants.AsNoTracking().SingleAsync(cancellationToken);
        return tenant.Id;
    }

    private static string HashCommand<TCommand>(TCommand command)
    {
        var json = JsonSerializer.Serialize(command, JsonOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash);
    }

    private void AppendAudit(
        Guid tenantId,
        string eventType,
        string entityType,
        string entityReference,
        string actorReference,
        object payload,
        Guid? correlationId,
        DateTimeOffset occurredAtUtc)
    {
        dbContext.SaasBillingAuditEvents.Add(new SaasBillingAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EventType = eventType,
            EntityType = entityType,
            EntityReference = entityReference,
            ActorReference = actorReference,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            CorrelationId = correlationId,
            OccurredAtUtc = occurredAtUtc,
        });
    }

    private static SaasBillingAccountReadModel MapAccount(SaasTenantBillingAccount account) =>
        new(
            account.Id,
            account.LegalName,
            account.DisplayName,
            account.BillingContactMasked,
            account.BillingAddressSnapshot,
            account.TaxCountryCode,
            account.TaxNumberMasked,
            account.PreferredBillingCadence,
            account.PreferredPaymentMethod,
            account.Version,
            account.CreatedAtUtc,
            account.UpdatedAtUtc);

    private static SaasBillingInvoiceSummaryReadModel MapInvoiceSummary(SaasBillingInvoice invoice) =>
        new(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.InvoiceStatus,
            invoice.CurrencyCode,
            invoice.TotalAmount,
            invoice.PeriodStartUtc,
            invoice.PeriodEndUtc,
            invoice.DueAtUtc,
            invoice.PaidAtUtc);

    private static SaasBillingInvoiceReadModel MapInvoice(SaasBillingInvoice invoice) =>
        new(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.BillingAccountId,
            invoice.SubscriptionId,
            invoice.CommercialPlanVersionId,
            invoice.PeriodStartUtc,
            invoice.PeriodEndUtc,
            invoice.CurrencyCode,
            invoice.SubtotalAmount,
            invoice.TaxAmount,
            invoice.TotalAmount,
            invoice.InvoiceStatus,
            invoice.IssuedAtUtc,
            invoice.DueAtUtc,
            invoice.PaidAtUtc,
            invoice.VoidedAtUtc,
            invoice.SourceProvenance,
            invoice.Version,
            invoice.Lines.OrderBy(line => line.SortOrder).Select(line => new SaasBillingInvoiceLineReadModel(
                line.Id,
                line.DescriptionSnapshot,
                line.ComponentType,
                line.Quantity,
                line.UnitAmount,
                line.LineSubtotalAmount,
                line.SourceProvenance,
                line.SortOrder)).ToList());

    private static SaasBillingPaymentAttemptReadModel MapPayment(SaasBillingPaymentAttempt attempt) =>
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
            attempt.UpdatedAtUtc);
}

public sealed class UnavailableSaasBillingRepository : ISaasBillingRepository
{
    private static SaasBillingWriteResult Unavailable() =>
        SaasBillingWriteResult.Fail(
            SaasBillingFailureCodes.Unavailable,
            "SaaS billing persistence is unavailable.");

    public Task<SaasBillingWorkspaceSummaryReadModel> GetWorkspaceSummaryAsync(
        SaasBillingProviderStatusReadModel providerStatus,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new SaasBillingWorkspaceSummaryReadModel(
            null, [], [],
            new SaasBillingProviderStatusReadModel(
                "unavailable",
                Payments.SaasPaymentProviderModes.NotConfigured,
                false,
                false,
                "unavailable",
                []),
            0, 0, 0));

    public Task<SaasBillingAccountReadModel?> GetBillingAccountAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<SaasBillingAccountReadModel?>(null);

    public Task<SaasBillingInvoiceReadModel?> GetInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<SaasBillingInvoiceReadModel?>(null);

    public Task<IReadOnlyList<SaasBillingInvoiceSummaryReadModel>> ListInvoicesAsync(
        string? statusFilter,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        string? search,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SaasBillingInvoiceSummaryReadModel>>([]);

    public Task<IReadOnlyList<SaasBillingPaymentAttemptReadModel>> ListPaymentAttemptsAsync(
        Guid? invoiceId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SaasBillingPaymentAttemptReadModel>>([]);

    public Task<SaasBillingWriteResult> UpdateBillingAccountAsync(
        UpdateSaasBillingAccountCommand command,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<SaasBillingWriteResult> CreateDraftInvoiceAsync(
        CreateSaasBillingInvoiceCommand command,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<SaasBillingWriteResult> TransitionInvoiceAsync(
        TransitionSaasBillingInvoiceCommand command,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<SaasBillingWriteResult> CreatePaymentAttemptAsync(
        CreateSaasBillingPaymentAttemptCommand command,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<SaasBillingWriteResult> VerifyBankTransferAsync(
        VerifySaasBillingBankTransferCommand command,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<SaasBillingWriteResult<InitializeCardCheckoutResult>> InitializeCardCheckoutAsync(
        InitializeCardCheckoutCommand command,
        ISaasPaymentProvider paymentProvider,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(SaasBillingWriteResult<InitializeCardCheckoutResult>.Fail(
            SaasBillingFailureCodes.Unavailable,
            "SaaS billing persistence is unavailable."));

    public Task<SaasBillingWriteResult> VerifyCheckoutReturnAsync(
        VerifyCheckoutReturnCommand command,
        ISaasPaymentProvider paymentProvider,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<SaasBillingWriteResult> ProcessProviderWebhookAsync(
        ProcessProviderWebhookCommand command,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<SaasBillingPaymentAttemptDetailReadModel?> GetPaymentAttemptDetailAsync(
        Guid paymentAttemptId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<SaasBillingPaymentAttemptDetailReadModel?>(null);

    public Task<SaasBillingCheckoutSession?> FindCheckoutSessionByReferenceAsync(
        string checkoutSessionReferenceMasked,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<SaasBillingCheckoutSession?>(null);
}
