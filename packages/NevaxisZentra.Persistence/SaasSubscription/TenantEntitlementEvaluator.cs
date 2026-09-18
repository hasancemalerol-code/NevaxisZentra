using NevaxisZentra.Persistence.Edge;
using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.Identity;
using NevaxisZentra.Persistence.SaasCommercial;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.SaasSubscription;

public sealed class TenantEntitlementEvaluator(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : ITenantEntitlementEvaluator
{
    public Task<TenantEntitlementEvaluationReadModel> EvaluatePaidApplicationAccessAsync(
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                try
                {
                    var context = await LoadSubscriptionContextAsync(ct);
                    if (context is null)
                    {
                        return TenantEntitlementEvaluationReadModel.Deny(
                            SaasSubscriptionEntitlementReasonCodes.SubscriptionNotFound,
                            SaasSubscriptionEntitlementExplanationKeys.SubscriptionNotFound);
                    }

                    var now = DateTimeOffset.UtcNow;
                    if (!SaasSubscriptionValueConversions.ResolvePaidApplicationAccess(
                            context.Subscription.SubscriptionStatus,
                            now,
                            context.Subscription.TrialEndsAtUtc,
                            context.Subscription.GraceUntilUtc,
                            context.Subscription.CurrentPeriodEndUtc))
                    {
                        return TenantEntitlementEvaluationReadModel.Deny(
                            SaasSubscriptionEntitlementReasonCodes.PaidApplicationDenied,
                            SaasSubscriptionEntitlementExplanationKeys.PaidApplicationDenied,
                            context.Subscription.SubscriptionStatus,
                            context.PlanVersion,
                            graceState: BuildGraceState(context.Subscription, now));
                    }

                    return TenantEntitlementEvaluationReadModel.Allow(
                        context.Subscription.SubscriptionStatus,
                        context.PlanVersion,
                        graceState: BuildGraceState(context.Subscription, now));
                }
                catch
                {
                    return TenantEntitlementEvaluationReadModel.Deny(
                        SaasSubscriptionEntitlementReasonCodes.EvaluationUnavailable,
                        SaasSubscriptionEntitlementExplanationKeys.EvaluationUnavailable);
                }
            },
            cancellationToken);

    public Task<TenantEntitlementEvaluationReadModel> EvaluateModuleAccessAsync(
        string routePath,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                try
                {
                    if (SaasSubscriptionModuleEntitlements.IsRecoveryExemptRoute(routePath))
                    {
                        return TenantEntitlementEvaluationReadModel.Allow(
                            subscriptionStatus: null,
                            planVersion: null,
                            entitlementCode: null,
                            graceState: null);
                    }

                    if (!SaasSubscriptionModuleEntitlements.TryResolveEntitlementCode(routePath, out var entitlementCode))
                    {
                        return TenantEntitlementEvaluationReadModel.Deny(
                            SaasSubscriptionEntitlementReasonCodes.EntitlementUnknownConfig,
                            SaasSubscriptionEntitlementExplanationKeys.EntitlementUnknownConfig);
                    }

                    return await EvaluateEntitlementCoreAsync(entitlementCode, ct);
                }
                catch
                {
                    return TenantEntitlementEvaluationReadModel.Deny(
                        SaasSubscriptionEntitlementReasonCodes.EvaluationUnavailable,
                        SaasSubscriptionEntitlementExplanationKeys.EvaluationUnavailable);
                }
            },
            cancellationToken);

    public Task<TenantEntitlementEvaluationReadModel> EvaluateFeatureAccessAsync(
        string featureCode,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                try
                {
                    return await EvaluateEntitlementCoreAsync(featureCode, ct);
                }
                catch
                {
                    return TenantEntitlementEvaluationReadModel.Deny(
                        SaasSubscriptionEntitlementReasonCodes.EvaluationUnavailable,
                        SaasSubscriptionEntitlementExplanationKeys.EvaluationUnavailable);
                }
            },
            cancellationToken);

    public Task<TenantEntitlementEvaluationReadModel> EvaluateLimitAsync(
        string limitEntitlementCode,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                try
                {
                    return await EvaluateEntitlementCoreAsync(limitEntitlementCode, ct);
                }
                catch
                {
                    return TenantEntitlementEvaluationReadModel.Deny(
                        SaasSubscriptionEntitlementReasonCodes.EvaluationUnavailable,
                        SaasSubscriptionEntitlementExplanationKeys.EvaluationUnavailable);
                }
            },
            cancellationToken);

    public Task<TenantEntitlementEvaluationReadModel> EvaluateEntitlementAsync(
        string entitlementCode,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                try
                {
                    return await EvaluateEntitlementCoreAsync(entitlementCode, ct);
                }
                catch
                {
                    return TenantEntitlementEvaluationReadModel.Deny(
                        SaasSubscriptionEntitlementReasonCodes.EvaluationUnavailable,
                        SaasSubscriptionEntitlementExplanationKeys.EvaluationUnavailable);
                }
            },
            cancellationToken);

    public Task<TenantEntitlementEvaluationReadModel> EvaluateUserLimitForAdditionalSeatsAsync(
        int additionalSeats,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                try
                {
                    if (additionalSeats < 1)
                    {
                        return TenantEntitlementEvaluationReadModel.Deny(
                            SaasSubscriptionEntitlementReasonCodes.EvaluationUnavailable,
                            SaasSubscriptionEntitlementExplanationKeys.EvaluationUnavailable);
                    }

                    var context = await LoadSubscriptionContextAsync(ct);
                    if (context is null)
                    {
                        return TenantEntitlementEvaluationReadModel.Deny(
                            SaasSubscriptionEntitlementReasonCodes.SubscriptionNotFound,
                            SaasSubscriptionEntitlementExplanationKeys.SubscriptionNotFound,
                            entitlementCode: "user-limit");
                    }

                    var grant = context.EntitlementGrants
                        .SingleOrDefault(item => string.Equals(
                            item.DefinitionCode,
                            "user-limit",
                            StringComparison.OrdinalIgnoreCase));
                    if (grant is null)
                    {
                        return TenantEntitlementEvaluationReadModel.Deny(
                            SaasSubscriptionEntitlementReasonCodes.EntitlementUnknownConfig,
                            SaasSubscriptionEntitlementExplanationKeys.EntitlementUnknownConfig,
                            context.Subscription.SubscriptionStatus,
                            context.PlanVersion,
                            "user-limit",
                            graceState: BuildGraceState(context.Subscription));
                    }

                    var currentUsage = await ResolveUsageAsync("user-limit", ct);
                    var projectedUsage = (currentUsage ?? 0) + additionalSeats;
                    var limitEvidence = new SaasSubscriptionLimitEvidenceReadModel(
                        "user-limit",
                        grant.Semantics,
                        grant.IntegerLimitValue,
                        projectedUsage,
                        grant.IntegerLimitValue.HasValue);

                    return EvaluateProjectedLimit(grant, context, "user-limit", projectedUsage, limitEvidence);
                }
                catch
                {
                    return TenantEntitlementEvaluationReadModel.Deny(
                        SaasSubscriptionEntitlementReasonCodes.EvaluationUnavailable,
                        SaasSubscriptionEntitlementExplanationKeys.EvaluationUnavailable);
                }
            },
            cancellationToken);

    private async Task<TenantEntitlementEvaluationReadModel> EvaluateEntitlementCoreAsync(
        string entitlementCode,
        CancellationToken cancellationToken)
    {
        var context = await LoadSubscriptionContextAsync(cancellationToken);
        if (context is null)
        {
            return TenantEntitlementEvaluationReadModel.Deny(
                SaasSubscriptionEntitlementReasonCodes.SubscriptionNotFound,
                SaasSubscriptionEntitlementExplanationKeys.SubscriptionNotFound,
                entitlementCode: entitlementCode);
        }

        var now = DateTimeOffset.UtcNow;
        if (!SaasSubscriptionValueConversions.ResolvePaidApplicationAccess(
                context.Subscription.SubscriptionStatus,
                now,
                context.Subscription.TrialEndsAtUtc,
                context.Subscription.GraceUntilUtc,
                context.Subscription.CurrentPeriodEndUtc))
        {
            return TenantEntitlementEvaluationReadModel.Deny(
                SaasSubscriptionEntitlementReasonCodes.SubscriptionInactive,
                SaasSubscriptionEntitlementExplanationKeys.SubscriptionInactive,
                context.Subscription.SubscriptionStatus,
                context.PlanVersion,
                entitlementCode,
                graceState: BuildGraceState(context.Subscription, now));
        }

        var grant = context.EntitlementGrants
            .SingleOrDefault(item => string.Equals(
                item.DefinitionCode,
                entitlementCode,
                StringComparison.OrdinalIgnoreCase));
        if (grant is null)
        {
            var definitionExists = await dbContext.SaasCommercialEntitlementDefinitions.AsNoTracking()
                .AnyAsync(
                    definition => definition.CodeNormalized
                        == SaasCommercialValueConversions.NormalizeEntitlementCode(entitlementCode),
                    cancellationToken);
            if (!definitionExists)
            {
                return TenantEntitlementEvaluationReadModel.Allow(
                    context.Subscription.SubscriptionStatus,
                    context.PlanVersion,
                    entitlementCode,
                    graceState: BuildGraceState(context.Subscription, now));
            }

            return TenantEntitlementEvaluationReadModel.Deny(
                SaasSubscriptionEntitlementReasonCodes.EntitlementUnknownConfig,
                SaasSubscriptionEntitlementExplanationKeys.EntitlementUnknownConfig,
                context.Subscription.SubscriptionStatus,
                context.PlanVersion,
                entitlementCode,
                graceState: BuildGraceState(context.Subscription, now));
        }

        return grant.ValueKind switch
        {
            SaasCommercialEntitlementValueKinds.ModuleAccess
                or SaasCommercialEntitlementValueKinds.FeatureAccess
                or SaasCommercialEntitlementValueKinds.BooleanCapability =>
                EvaluateBooleanEntitlement(context, grant, entitlementCode),
            SaasCommercialEntitlementValueKinds.FacilityLimit
                or SaasCommercialEntitlementValueKinds.ConnectedMachineLimit
                or SaasCommercialEntitlementValueKinds.UserLimit
                or SaasCommercialEntitlementValueKinds.IntegerLimit =>
                await EvaluateNumericLimitEntitlementAsync(context, grant, entitlementCode, cancellationToken),
            _ => TenantEntitlementEvaluationReadModel.Deny(
                SaasSubscriptionEntitlementReasonCodes.EntitlementUnknownConfig,
                SaasSubscriptionEntitlementExplanationKeys.EntitlementUnknownConfig,
                context.Subscription.SubscriptionStatus,
                context.PlanVersion,
                entitlementCode,
                graceState: BuildGraceState(context.Subscription)),
        };
    }

    private static TenantEntitlementEvaluationReadModel EvaluateBooleanEntitlement(
        SubscriptionEvaluationContext context,
        EntitlementGrantSnapshot grant,
        string entitlementCode)
    {
        return grant.Semantics switch
        {
            SaasCommercialEntitlementSemantics.Unavailable => TenantEntitlementEvaluationReadModel.Deny(
                SaasSubscriptionEntitlementReasonCodes.EntitlementUnavailable,
                SaasSubscriptionEntitlementExplanationKeys.EntitlementUnavailable,
                context.Subscription.SubscriptionStatus,
                context.PlanVersion,
                entitlementCode,
                graceState: BuildGraceState(context.Subscription)),
            SaasCommercialEntitlementSemantics.Unlimited
                or SaasCommercialEntitlementSemantics.Limited when grant.BooleanValue == true =>
                TenantEntitlementEvaluationReadModel.Allow(
                    context.Subscription.SubscriptionStatus,
                    context.PlanVersion,
                    entitlementCode,
                    graceState: BuildGraceState(context.Subscription)),
            SaasCommercialEntitlementSemantics.Limited when grant.BooleanValue == false =>
                TenantEntitlementEvaluationReadModel.Deny(
                    SaasSubscriptionEntitlementReasonCodes.EntitlementUnavailable,
                    SaasSubscriptionEntitlementExplanationKeys.EntitlementUnavailable,
                    context.Subscription.SubscriptionStatus,
                    context.PlanVersion,
                    entitlementCode,
                    graceState: BuildGraceState(context.Subscription)),
            SaasCommercialEntitlementSemantics.Unknown => TenantEntitlementEvaluationReadModel.Deny(
                SaasSubscriptionEntitlementReasonCodes.EntitlementUnknownConfig,
                SaasSubscriptionEntitlementExplanationKeys.EntitlementUnknownConfig,
                context.Subscription.SubscriptionStatus,
                context.PlanVersion,
                entitlementCode,
                graceState: BuildGraceState(context.Subscription)),
            _ => TenantEntitlementEvaluationReadModel.Deny(
                SaasSubscriptionEntitlementReasonCodes.EntitlementUnknownConfig,
                SaasSubscriptionEntitlementExplanationKeys.EntitlementUnknownConfig,
                context.Subscription.SubscriptionStatus,
                context.PlanVersion,
                entitlementCode,
                graceState: BuildGraceState(context.Subscription)),
        };
    }

    private async Task<TenantEntitlementEvaluationReadModel> EvaluateNumericLimitEntitlementAsync(
        SubscriptionEvaluationContext context,
        EntitlementGrantSnapshot grant,
        string entitlementCode,
        CancellationToken cancellationToken)
    {
        var usage = await ResolveUsageAsync(entitlementCode, cancellationToken);
        var limitEvidence = new SaasSubscriptionLimitEvidenceReadModel(
            entitlementCode,
            grant.Semantics,
            grant.IntegerLimitValue,
            usage,
            grant.IntegerLimitValue.HasValue);

        return grant.Semantics switch
        {
            SaasCommercialEntitlementSemantics.Unavailable => TenantEntitlementEvaluationReadModel.Deny(
                SaasSubscriptionEntitlementReasonCodes.EntitlementUnavailable,
                SaasSubscriptionEntitlementExplanationKeys.EntitlementUnavailable,
                context.Subscription.SubscriptionStatus,
                context.PlanVersion,
                entitlementCode,
                limitEvidence,
                BuildGraceState(context.Subscription)),
            SaasCommercialEntitlementSemantics.Unlimited => TenantEntitlementEvaluationReadModel.Allow(
                context.Subscription.SubscriptionStatus,
                context.PlanVersion,
                entitlementCode,
                limitEvidence,
                BuildGraceState(context.Subscription)),
            SaasCommercialEntitlementSemantics.Limited when grant.IntegerLimitValue is null =>
                TenantEntitlementEvaluationReadModel.Deny(
                    SaasSubscriptionEntitlementReasonCodes.EntitlementUnknownConfig,
                    SaasSubscriptionEntitlementExplanationKeys.EntitlementUnknownConfig,
                    context.Subscription.SubscriptionStatus,
                    context.PlanVersion,
                    entitlementCode,
                    limitEvidence,
                    BuildGraceState(context.Subscription)),
            SaasCommercialEntitlementSemantics.Limited when usage is null =>
                TenantEntitlementEvaluationReadModel.Deny(
                    SaasSubscriptionEntitlementReasonCodes.EntitlementUnknownConfig,
                    SaasSubscriptionEntitlementExplanationKeys.EntitlementUnknownConfig,
                    context.Subscription.SubscriptionStatus,
                    context.PlanVersion,
                    entitlementCode,
                    limitEvidence,
                    BuildGraceState(context.Subscription)),
            SaasCommercialEntitlementSemantics.Limited when usage > grant.IntegerLimitValue =>
                TenantEntitlementEvaluationReadModel.Deny(
                    SaasSubscriptionEntitlementReasonCodes.EntitlementLimitExceeded,
                    SaasSubscriptionEntitlementExplanationKeys.EntitlementLimitExceeded,
                    context.Subscription.SubscriptionStatus,
                    context.PlanVersion,
                    entitlementCode,
                    limitEvidence,
                    BuildGraceState(context.Subscription)),
            SaasCommercialEntitlementSemantics.Limited => TenantEntitlementEvaluationReadModel.Allow(
                context.Subscription.SubscriptionStatus,
                context.PlanVersion,
                entitlementCode,
                limitEvidence,
                BuildGraceState(context.Subscription)),
            SaasCommercialEntitlementSemantics.Unknown => TenantEntitlementEvaluationReadModel.Deny(
                SaasSubscriptionEntitlementReasonCodes.EntitlementUnknownConfig,
                SaasSubscriptionEntitlementExplanationKeys.EntitlementUnknownConfig,
                context.Subscription.SubscriptionStatus,
                context.PlanVersion,
                entitlementCode,
                limitEvidence,
                BuildGraceState(context.Subscription)),
            _ => TenantEntitlementEvaluationReadModel.Deny(
                SaasSubscriptionEntitlementReasonCodes.EntitlementUnknownConfig,
                SaasSubscriptionEntitlementExplanationKeys.EntitlementUnknownConfig,
                context.Subscription.SubscriptionStatus,
                context.PlanVersion,
                entitlementCode,
                limitEvidence,
                BuildGraceState(context.Subscription)),
        };
    }

    private async Task<int?> ResolveUsageAsync(string entitlementCode, CancellationToken cancellationToken)
    {
        if (string.Equals(entitlementCode, "facility-limit", StringComparison.OrdinalIgnoreCase))
        {
            // Authoritative facility usage is the count of tenant facilities with Status=Active.
            return await dbContext.Facilities.AsNoTracking()
                .CountAsync(f => f.Status == FacilityStatus.Active, cancellationToken);
        }

        if (string.Equals(entitlementCode, "connected-machine-limit", StringComparison.OrdinalIgnoreCase))
        {
            return await dbContext.MachineGatewayAssignments.AsNoTracking()
                .CountAsync(
                    assignment => assignment.Status == MachineAssignmentStatuses.Active,
                    cancellationToken);
        }

        if (string.Equals(entitlementCode, "user-limit", StringComparison.OrdinalIgnoreCase))
        {
            var now = DateTimeOffset.UtcNow;
            // Suspended memberships still consume a seat; only rejected/expired/revoked/removed invitations are excluded.
            var activeMembers = await dbContext.TenantMemberships.AsNoTracking()
                .CountAsync(m => m.Status == TenantMembershipStatus.Active, cancellationToken);
            var suspendedMembers = await dbContext.TenantMemberships.AsNoTracking()
                .CountAsync(m => m.Status == TenantMembershipStatus.Inactive, cancellationToken);
            var pendingInvitations = await dbContext.SaasIdentityInvitations.AsNoTracking()
                .CountAsync(
                    invitation => invitation.ConsumedAtUtc == null && invitation.ExpiresAtUtc > now,
                    cancellationToken);
            return activeMembers + suspendedMembers + pendingInvitations;
        }

        return null;
    }

    private async Task<SubscriptionEvaluationContext?> LoadSubscriptionContextAsync(CancellationToken cancellationToken)
    {
        var subscription = await dbContext.SaasTenantSubscriptions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.IsCurrent, cancellationToken);
        if (subscription is null)
        {
            return null;
        }

        var planVersion = await (
            from version in dbContext.SaasCommercialPlanVersions.AsNoTracking()
            join plan in dbContext.SaasCommercialPlans.AsNoTracking() on version.PlanId equals plan.Id
            where version.Id == subscription.CommercialPlanVersionId
            select new SaasSubscriptionPlanVersionReferenceReadModel(
                version.Id,
                plan.PlanCode,
                version.VersionNumber)).SingleOrDefaultAsync(cancellationToken);

        var grants = await (
            from grant in dbContext.SaasCommercialPlanVersionEntitlements.AsNoTracking()
            join definition in dbContext.SaasCommercialEntitlementDefinitions.AsNoTracking()
                on grant.EntitlementDefinitionId equals definition.Id
            where grant.PlanVersionId == subscription.CommercialPlanVersionId
            select new EntitlementGrantSnapshot(
                definition.Code,
                definition.ValueKind,
                grant.Semantics,
                grant.BooleanValue,
                grant.IntegerLimitValue)).ToListAsync(cancellationToken);

        return new SubscriptionEvaluationContext(
            subscription,
            planVersion,
            grants);
    }

    private static TenantEntitlementEvaluationReadModel EvaluateProjectedLimit(
        EntitlementGrantSnapshot grant,
        SubscriptionEvaluationContext context,
        string entitlementCode,
        int projectedUsage,
        SaasSubscriptionLimitEvidenceReadModel limitEvidence)
    {
        return grant.Semantics switch
        {
            SaasCommercialEntitlementSemantics.Unavailable => TenantEntitlementEvaluationReadModel.Deny(
                SaasSubscriptionEntitlementReasonCodes.EntitlementUnavailable,
                SaasSubscriptionEntitlementExplanationKeys.EntitlementUnavailable,
                context.Subscription.SubscriptionStatus,
                context.PlanVersion,
                entitlementCode,
                limitEvidence,
                BuildGraceState(context.Subscription)),
            SaasCommercialEntitlementSemantics.Unlimited => TenantEntitlementEvaluationReadModel.Allow(
                context.Subscription.SubscriptionStatus,
                context.PlanVersion,
                entitlementCode,
                limitEvidence,
                BuildGraceState(context.Subscription)),
            SaasCommercialEntitlementSemantics.Limited when grant.IntegerLimitValue is null =>
                TenantEntitlementEvaluationReadModel.Deny(
                    SaasSubscriptionEntitlementReasonCodes.EntitlementUnknownConfig,
                    SaasSubscriptionEntitlementExplanationKeys.EntitlementUnknownConfig,
                    context.Subscription.SubscriptionStatus,
                    context.PlanVersion,
                    entitlementCode,
                    limitEvidence,
                    BuildGraceState(context.Subscription)),
            SaasCommercialEntitlementSemantics.Limited when projectedUsage > grant.IntegerLimitValue =>
                TenantEntitlementEvaluationReadModel.Deny(
                    SaasSubscriptionEntitlementReasonCodes.EntitlementLimitExceeded,
                    SaasSubscriptionEntitlementExplanationKeys.EntitlementLimitExceeded,
                    context.Subscription.SubscriptionStatus,
                    context.PlanVersion,
                    entitlementCode,
                    limitEvidence,
                    BuildGraceState(context.Subscription)),
            SaasCommercialEntitlementSemantics.Limited => TenantEntitlementEvaluationReadModel.Allow(
                context.Subscription.SubscriptionStatus,
                context.PlanVersion,
                entitlementCode,
                limitEvidence,
                BuildGraceState(context.Subscription)),
            SaasCommercialEntitlementSemantics.Unknown => TenantEntitlementEvaluationReadModel.Deny(
                SaasSubscriptionEntitlementReasonCodes.EntitlementUnknownConfig,
                SaasSubscriptionEntitlementExplanationKeys.EntitlementUnknownConfig,
                context.Subscription.SubscriptionStatus,
                context.PlanVersion,
                entitlementCode,
                limitEvidence,
                BuildGraceState(context.Subscription)),
            _ => TenantEntitlementEvaluationReadModel.Deny(
                SaasSubscriptionEntitlementReasonCodes.EntitlementUnknownConfig,
                SaasSubscriptionEntitlementExplanationKeys.EntitlementUnknownConfig,
                context.Subscription.SubscriptionStatus,
                context.PlanVersion,
                entitlementCode,
                limitEvidence,
                BuildGraceState(context.Subscription)),
        };
    }

    private static SaasSubscriptionGraceStateReadModel BuildGraceState(
        SaasTenantSubscription subscription,
        DateTimeOffset? nowUtc = null)
    {
        var now = nowUtc ?? DateTimeOffset.UtcNow;
        var inGrace = subscription.SubscriptionStatus == SaasSubscriptionStatuses.GracePeriod
            && (subscription.GraceUntilUtc is null || now <= subscription.GraceUntilUtc);
        return new SaasSubscriptionGraceStateReadModel(inGrace, subscription.GraceUntilUtc);
    }

    private sealed record SubscriptionEvaluationContext(
        SaasTenantSubscription Subscription,
        SaasSubscriptionPlanVersionReferenceReadModel? PlanVersion,
        IReadOnlyList<EntitlementGrantSnapshot> EntitlementGrants);

    private sealed record EntitlementGrantSnapshot(
        string DefinitionCode,
        string ValueKind,
        string Semantics,
        bool? BooleanValue,
        int? IntegerLimitValue);
}
