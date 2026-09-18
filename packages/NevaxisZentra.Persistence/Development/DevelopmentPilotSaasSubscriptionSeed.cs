using NevaxisZentra.Persistence.SaasCommercial;
using NevaxisZentra.Persistence.SaasSubscription;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Development;

internal static class DevelopmentPilotSaasSubscriptionSeed
{
    internal static async Task EnsureDevelopmentSubscriptionAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var periodStartUtc = seedTimestampUtc.AddMonths(-1);
        var periodEndUtc = seedTimestampUtc.AddYears(1);

        var existing = await dbContext.SaasTenantSubscriptions
            .SingleOrDefaultAsync(
                subscription => subscription.Id == DevelopmentPilotIds.DevPilotTenantSubscriptionId,
                cancellationToken);

        if (existing is not null)
        {
            if (existing.CurrentPeriodEndUtc < seedTimestampUtc
                || !string.Equals(existing.SubscriptionStatus, SaasSubscriptionStatuses.Active, StringComparison.Ordinal))
            {
                existing.SubscriptionStatus = SaasSubscriptionStatuses.Active;
                existing.CurrentPeriodStartUtc = periodStartUtc;
                existing.CurrentPeriodEndUtc = periodEndUtc;
                existing.GraceUntilUtc = null;
                existing.SuspendedAtUtc = null;
                existing.CancelledAtUtc = null;
                existing.IsCurrent = true;
                existing.UpdatedAtUtc = seedTimestampUtc;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        dbContext.SaasTenantSubscriptions.Add(new SaasTenantSubscription
        {
            Id = DevelopmentPilotIds.DevPilotTenantSubscriptionId,
            TenantId = tenantId,
            CommercialPlanVersionId = DevelopmentPilotIds.SaasCommercialDevMonthlyVersionId,
            SubscriptionStatus = SaasSubscriptionStatuses.Active,
            BillingCadence = SaasCommercialBillingCadences.Monthly,
            StartsAtUtc = periodStartUtc,
            CurrentPeriodStartUtc = periodStartUtc,
            CurrentPeriodEndUtc = periodEndUtc,
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
