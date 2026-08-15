using CrevaFactOS.Persistence.SaasCommercial;
using CrevaFactOS.Persistence.SaasSubscription;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Development;

internal static class DevelopmentPilotSaasSubscriptionSeed
{
    private static readonly DateTimeOffset DevPeriodStartUtc =
        new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset DevPeriodEndUtc =
        new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    internal static async Task EnsureDevelopmentSubscriptionAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.SaasTenantSubscriptions.AsNoTracking()
                .AnyAsync(subscription => subscription.Id == DevelopmentPilotIds.DevPilotTenantSubscriptionId, cancellationToken))
        {
            return;
        }

        dbContext.SaasTenantSubscriptions.Add(new SaasTenantSubscription
        {
            Id = DevelopmentPilotIds.DevPilotTenantSubscriptionId,
            TenantId = tenantId,
            CommercialPlanVersionId = DevelopmentPilotIds.SaasCommercialDevMonthlyVersionId,
            SubscriptionStatus = SaasSubscriptionStatuses.Active,
            BillingCadence = SaasCommercialBillingCadences.Monthly,
            StartsAtUtc = DevPeriodStartUtc,
            CurrentPeriodStartUtc = DevPeriodStartUtc,
            CurrentPeriodEndUtc = DevPeriodEndUtc,
            TrialEndsAtUtc = null,
            GraceUntilUtc = null,
            SuspendedAtUtc = null,
            CancelledAtUtc = null,
            CancelAtPeriodEnd = false,
            SourceProvenance = $"{SaasSubscriptionProvenance.LocalDevelopmentPilot};{SaasSubscriptionProvenance.NonProduction}",
            IsCurrent = true,
            Version = 1,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
        });

        dbContext.SaasTenantSubscriptionTransitionHistory.Add(new SaasTenantSubscriptionTransitionHistory
        {
            Id = DevelopmentPilotIds.DevPilotTenantSubscriptionHistoryId,
            TenantId = tenantId,
            SubscriptionId = DevelopmentPilotIds.DevPilotTenantSubscriptionId,
            FromStatus = SaasSubscriptionStatuses.Expired,
            ToStatus = SaasSubscriptionStatuses.Active,
            TransitionReason = SaasSubscriptionTransitionReasons.DevelopmentSeed,
            ActorReference = actorSubjectId,
            OccurredAtUtc = seedTimestampUtc,
            EvidenceJson = """{"commercialPlanVersionId":"bb000001-00e2-4000-8000-0000000000e2","provenance":"local-development-pilot"}""",
        });

        dbContext.SaasSubscriptionAuditEvents.Add(new SaasSubscriptionAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EventType = SaasSubscriptionAuditEventTypes.SubscriptionAssigned,
            EntityType = SaasSubscriptionAuditEntityTypes.TenantSubscription,
            EntityReference = DevelopmentPilotIds.DevPilotTenantSubscriptionId.ToString("D"),
            ActorReference = actorSubjectId,
            PayloadJson = """{"source":"development-pilot-seed","status":"active"}""",
            OccurredAtUtc = seedTimestampUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
