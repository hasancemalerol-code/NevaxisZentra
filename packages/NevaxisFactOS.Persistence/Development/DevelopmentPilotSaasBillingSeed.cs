using NevaxisFactOS.Persistence.SaasBilling;
using NevaxisFactOS.Persistence.SaasCommercial;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Development;

internal static class DevelopmentPilotSaasBillingSeed
{
    private const decimal DevLineAmount = 100.0000m;

    private static readonly DateTimeOffset DevPeriodStartUtc =
        new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset DevPeriodEndUtc =
        new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset DevIssuedUtc =
        new(2026, 8, 5, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset DevDueFutureUtc =
        new(2026, 8, 25, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset DevDuePastUtc =
        new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset DevPaidUtc =
        new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);

    internal static async Task EnsureDevelopmentBillingAsync(
        NevaxisFactOSDbContext dbContext,
        Guid tenantId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        await EnsureDevelopmentCommercialPriceAmountsAsync(dbContext, cancellationToken);

        if (await dbContext.SaasTenantBillingAccounts.AsNoTracking()
                .AnyAsync(account => account.Id == DevelopmentPilotIds.DevPilotBillingAccountId, cancellationToken))
        {
            return;
        }

        var provenance = $"{SaasBillingProvenance.LocalDevelopmentPilot};{SaasBillingProvenance.NonProduction}";

        dbContext.SaasTenantBillingAccounts.Add(new SaasTenantBillingAccount
        {
            Id = DevelopmentPilotIds.DevPilotBillingAccountId,
            TenantId = tenantId,
            LegalName = "NevaxisFactOS Geliştirme Pilotu A.Ş.",
            DisplayName = "Geliştirme Pilotu",
            BillingContactMasked = "f***@dev.local",
            BillingAddressSnapshot = "Geliştirme Ortamı, İstanbul, TR",
            TaxCountryCode = "TR",
            TaxNumberMasked = "****0001",
            PreferredBillingCadence = SaasCommercialBillingCadences.Monthly,
            PreferredPaymentMethod = SaasBillingPaymentMethods.BankTransfer,
            Version = 1,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
        });

        AddInvoice(
            dbContext,
            tenantId,
            DevelopmentPilotIds.DevPilotBillingInvoiceDraftId,
            1,
            SaasBillingInvoiceStatuses.Draft,
            issuedAtUtc: null,
            dueAtUtc: null,
            paidAtUtc: null,
            seedTimestampUtc,
            provenance);
        AddInvoice(
            dbContext,
            tenantId,
            DevelopmentPilotIds.DevPilotBillingInvoiceOpenId,
            2,
            SaasBillingInvoiceStatuses.Open,
            issuedAtUtc: DevIssuedUtc,
            dueAtUtc: DevDueFutureUtc,
            paidAtUtc: null,
            seedTimestampUtc,
            provenance);
        AddInvoice(
            dbContext,
            tenantId,
            DevelopmentPilotIds.DevPilotBillingInvoicePaidId,
            3,
            SaasBillingInvoiceStatuses.Paid,
            issuedAtUtc: DevIssuedUtc,
            dueAtUtc: DevDueFutureUtc,
            paidAtUtc: DevPaidUtc,
            seedTimestampUtc,
            provenance);
        AddInvoice(
            dbContext,
            tenantId,
            DevelopmentPilotIds.DevPilotBillingInvoiceOverdueId,
            4,
            SaasBillingInvoiceStatuses.Overdue,
            issuedAtUtc: DevIssuedUtc,
            dueAtUtc: DevDuePastUtc,
            paidAtUtc: null,
            seedTimestampUtc,
            provenance);

        dbContext.SaasBillingPaymentAttempts.Add(new SaasBillingPaymentAttempt
        {
            Id = DevelopmentPilotIds.DevPilotBillingPaymentAwaitingId,
            TenantId = tenantId,
            InvoiceId = DevelopmentPilotIds.DevPilotBillingInvoiceOpenId,
            PaymentMethod = SaasBillingPaymentMethods.BankTransfer,
            ProviderKey = null,
            ExternalReferenceMasked = "DEV-REF-***01",
            Amount = DevLineAmount,
            CurrencyCode = "TRY",
            PaymentStatus = SaasBillingPaymentStatuses.AwaitingManualVerification,
            FailureCode = null,
            IdempotencyKey = null,
            Version = 1,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
        });

        dbContext.SaasBillingPaymentAttempts.Add(new SaasBillingPaymentAttempt
        {
            Id = DevelopmentPilotIds.DevPilotBillingPaymentSucceededId,
            TenantId = tenantId,
            InvoiceId = DevelopmentPilotIds.DevPilotBillingInvoicePaidId,
            PaymentMethod = SaasBillingPaymentMethods.BankTransfer,
            ProviderKey = null,
            ExternalReferenceMasked = "DEV-REF-***02",
            Amount = DevLineAmount,
            CurrencyCode = "TRY",
            PaymentStatus = SaasBillingPaymentStatuses.Succeeded,
            FailureCode = null,
            IdempotencyKey = null,
            Version = 1,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
        });

        dbContext.SaasBillingAuditEvents.Add(new SaasBillingAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EventType = SaasBillingAuditEventTypes.BillingAccountUpdated,
            EntityType = SaasBillingAuditEntityTypes.BillingAccount,
            EntityReference = DevelopmentPilotIds.DevPilotBillingAccountId.ToString("D"),
            ActorReference = actorSubjectId,
            PayloadJson = """{"source":"development-pilot-seed","provenance":"local-development-pilot"}""",
            OccurredAtUtc = seedTimestampUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureDevelopmentCommercialPriceAmountsAsync(
        NevaxisFactOSDbContext dbContext,
        CancellationToken cancellationToken)
    {
        const decimal devNonProductionAmount = 100.0000m;
        var devVersionIds = new[]
        {
            DevelopmentPilotIds.SaasCommercialDevMonthlyVersionId,
            DevelopmentPilotIds.SaasCommercialDevAnnualVersionId,
        };

        foreach (var versionId in devVersionIds)
        {
            await dbContext.SaasCommercialPriceComponents
                .Where(item =>
                    item.PlanVersionId == versionId
                    && item.ComponentType == SaasCommercialPriceComponentTypes.TenantBase
                    && item.Amount == null)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(item => item.Amount, devNonProductionAmount),
                    cancellationToken);
        }
    }

    private static void AddInvoice(
        NevaxisFactOSDbContext dbContext,
        Guid tenantId,
        Guid invoiceId,
        int sequence,
        string status,
        DateTimeOffset? issuedAtUtc,
        DateTimeOffset? dueAtUtc,
        DateTimeOffset? paidAtUtc,
        DateTimeOffset seedTimestampUtc,
        string provenance)
    {
        var lineId = Guid.NewGuid();
        dbContext.SaasBillingInvoices.Add(new SaasBillingInvoice
        {
            Id = invoiceId,
            TenantId = tenantId,
            BillingAccountId = DevelopmentPilotIds.DevPilotBillingAccountId,
            SubscriptionId = DevelopmentPilotIds.DevPilotTenantSubscriptionId,
            CommercialPlanVersionId = DevelopmentPilotIds.SaasCommercialDevMonthlyVersionId,
            InvoiceNumber = SaasBillingValueConversions.FormatInvoiceNumber(tenantId, sequence),
            PeriodStartUtc = DevPeriodStartUtc,
            PeriodEndUtc = DevPeriodEndUtc,
            CurrencyCode = "TRY",
            SubtotalAmount = DevLineAmount,
            TaxAmount = null,
            TotalAmount = DevLineAmount,
            InvoiceStatus = status,
            IssuedAtUtc = issuedAtUtc,
            DueAtUtc = dueAtUtc,
            PaidAtUtc = paidAtUtc,
            VoidedAtUtc = null,
            SourceProvenance = provenance,
            IdempotencyKey = null,
            InvoiceSequence = sequence,
            Version = 1,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            Lines =
            [
                new SaasBillingInvoiceLine
                {
                    Id = lineId,
                    TenantId = tenantId,
                    InvoiceId = invoiceId,
                    DescriptionSnapshot = "Geliştirme ortamı abonelik kalemi (üretim fiyatı değildir)",
                    ComponentType = SaasCommercialPriceComponentTypes.TenantBase,
                    Quantity = 1,
                    UnitAmount = DevLineAmount,
                    LineSubtotalAmount = DevLineAmount,
                    SourceProvenance = $"{provenance};dev-seed",
                    SortOrder = 10,
                    CreatedAtUtc = seedTimestampUtc,
                },
            ],
        });
    }
}
