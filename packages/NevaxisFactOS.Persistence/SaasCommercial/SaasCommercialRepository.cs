using System.Text.Json;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.SaasCommercial;

public interface ISaasCommercialRepository
{
    Task<SaasCommercialCatalogListReadModel> ListAvailablePlansAsync(
        SaasCommercialOperationContext context,
        CancellationToken cancellationToken = default);

    Task<SaasCommercialPlanDetailReadModel?> GetPlanByCodeAsync(
        SaasCommercialOperationContext context,
        string planCode,
        CancellationToken cancellationToken = default);

    Task<SaasCommercialPlanVersionDetailReadModel?> GetPlanVersionAsync(
        SaasCommercialOperationContext context,
        string planCode,
        int versionNumber,
        CancellationToken cancellationToken = default);

    Task<SaasCommercialPlanVersionDetailReadModel?> ResolveVersionByEffectiveDateAndCadenceAsync(
        SaasCommercialOperationContext context,
        string planCode,
        string billingCadence,
        DateTimeOffset effectiveAtUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SaasCommercialEntitlementReadModel>> ResolvePlanEntitlementsAsync(
        SaasCommercialOperationContext context,
        string planCode,
        int? versionNumber,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SaasCommercialPaymentMethodCapabilityReadModel>> ResolvePaymentMethodCapabilitiesAsync(
        SaasCommercialOperationContext context,
        string planCode,
        int? versionNumber,
        CancellationToken cancellationToken = default);

    Task<SaasCommercialWriteResult> CreateOrUpdateDraftPlanAsync(
        CreateSaasCommercialPlanCommand command,
        string actorSubjectId,
        CancellationToken cancellationToken = default);

    Task<SaasCommercialWriteResult> CreateDraftPlanVersionAsync(
        CreateSaasCommercialPlanVersionCommand command,
        string actorSubjectId,
        CancellationToken cancellationToken = default);

    Task<SaasCommercialWriteResult> PublishDraftVersionAsync(
        PublishSaasCommercialPlanVersionCommand command,
        string actorSubjectId,
        CancellationToken cancellationToken = default);

    Task<SaasCommercialWriteResult> RetireVersionAsync(
        RetireSaasCommercialPlanVersionCommand command,
        string actorSubjectId,
        CancellationToken cancellationToken = default);
}

public sealed class SaasCommercialRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : ISaasCommercialRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<SaasCommercialCatalogListReadModel> ListAvailablePlansAsync(
        SaasCommercialOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var plans = await dbContext.SaasCommercialPlans.AsNoTracking()
                    .Include(p => p.Versions)
                    .Where(p => p.AvailabilityState == SaasCommercialAvailabilityStates.Available
                        || p.AvailabilityState == SaasCommercialAvailabilityStates.Draft)
                    .OrderBy(p => p.PlanCodeNormalized)
                    .ToListAsync(ct);

                return new SaasCommercialCatalogListReadModel(plans.Select(MapPlanSummary).ToList());
            },
            cancellationToken);

    public Task<SaasCommercialPlanDetailReadModel?> GetPlanByCodeAsync(
        SaasCommercialOperationContext context,
        string planCode,
        CancellationToken cancellationToken = default)
    {
        var normalized = SaasCommercialValueConversions.NormalizePlanCode(planCode);
        return tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var plan = await LoadPlanWithVersionsAsync(normalized, ct);
                return plan is null ? null : MapPlanDetail(plan);
            },
            cancellationToken);
    }

    public Task<SaasCommercialPlanVersionDetailReadModel?> GetPlanVersionAsync(
        SaasCommercialOperationContext context,
        string planCode,
        int versionNumber,
        CancellationToken cancellationToken = default)
    {
        var normalized = SaasCommercialValueConversions.NormalizePlanCode(planCode);
        return tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var version = await LoadVersionAsync(normalized, versionNumber, ct);
                return version is null ? null : MapVersionDetail(version);
            },
            cancellationToken);
    }

    public Task<SaasCommercialPlanVersionDetailReadModel?> ResolveVersionByEffectiveDateAndCadenceAsync(
        SaasCommercialOperationContext context,
        string planCode,
        string billingCadence,
        DateTimeOffset effectiveAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (!SaasCommercialValueConversions.IsValidBillingCadence(billingCadence))
        {
            return Task.FromResult<SaasCommercialPlanVersionDetailReadModel?>(null);
        }

        var normalized = SaasCommercialValueConversions.NormalizePlanCode(planCode);
        return tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var versions = await dbContext.SaasCommercialPlanVersions.AsNoTracking()
                    .Include(v => v.Plan)
                    .Include(v => v.PriceComponents)
                    .Where(v => v.Plan!.PlanCodeNormalized == normalized
                        && v.BillingCadence == billingCadence
                        && v.EffectiveFromUtc <= effectiveAtUtc
                        && (v.EffectiveUntilUtc == null || v.EffectiveUntilUtc > effectiveAtUtc))
                    .OrderByDescending(v => v.EffectiveFromUtc)
                    .ThenByDescending(v => v.VersionNumber)
                    .ToListAsync(ct);

                var version = versions.FirstOrDefault();
                return version is null ? null : MapVersionDetail(version);
            },
            cancellationToken);
    }

    public Task<IReadOnlyList<SaasCommercialEntitlementReadModel>> ResolvePlanEntitlementsAsync(
        SaasCommercialOperationContext context,
        string planCode,
        int? versionNumber,
        CancellationToken cancellationToken = default)
    {
        var normalized = SaasCommercialValueConversions.NormalizePlanCode(planCode);
        return tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var version = versionNumber.HasValue
                    ? await LoadVersionAsync(normalized, versionNumber.Value, ct)
                    : await LoadLatestReadableVersionAsync(normalized, ct);

                if (version is null)
                {
                    return (IReadOnlyList<SaasCommercialEntitlementReadModel>)[];
                }

                var grants = await dbContext.SaasCommercialPlanVersionEntitlements.AsNoTracking()
                    .Include(e => e.EntitlementDefinition)
                    .Where(e => e.PlanVersionId == version.Id)
                    .OrderBy(e => e.EntitlementDefinition!.CodeNormalized)
                    .ToListAsync(ct);

                return grants.Select(MapEntitlement).ToList();
            },
            cancellationToken);
    }

    public Task<IReadOnlyList<SaasCommercialPaymentMethodCapabilityReadModel>> ResolvePaymentMethodCapabilitiesAsync(
        SaasCommercialOperationContext context,
        string planCode,
        int? versionNumber,
        CancellationToken cancellationToken = default)
    {
        var normalized = SaasCommercialValueConversions.NormalizePlanCode(planCode);
        return tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var version = versionNumber.HasValue
                    ? await LoadVersionAsync(normalized, versionNumber.Value, ct)
                    : await LoadLatestReadableVersionAsync(normalized, ct);

                if (version is null)
                {
                    return (IReadOnlyList<SaasCommercialPaymentMethodCapabilityReadModel>)[];
                }

                var capabilities = await dbContext.SaasCommercialPaymentMethodCapabilities.AsNoTracking()
                    .Where(c => c.PlanVersionId == version.Id)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.MethodType)
                    .ToListAsync(ct);

                return capabilities.Select(MapPaymentMethod).ToList();
            },
            cancellationToken);
    }

    public Task<SaasCommercialWriteResult> CreateOrUpdateDraftPlanAsync(
        CreateSaasCommercialPlanCommand command,
        string actorSubjectId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                if (string.IsNullOrWhiteSpace(command.PlanCode))
                {
                    return SaasCommercialWriteResult.Fail(
                        SaasCommercialFailureCodes.ValidationFailed,
                        "Plan code is required.");
                }

                var normalized = SaasCommercialValueConversions.NormalizePlanCode(command.PlanCode);
                var now = DateTimeOffset.UtcNow;
                var existing = await dbContext.SaasCommercialPlans
                    .FirstOrDefaultAsync(p => p.PlanCodeNormalized == normalized, ct);

                if (existing is null)
                {
                    var plan = new SaasCommercialPlan
                    {
                        Id = Guid.NewGuid(),
                        PlanCode = command.PlanCode.Trim(),
                        PlanCodeNormalized = normalized,
                        DisplayNameTr = command.DisplayNameTr.Trim(),
                        DescriptionTr = command.DescriptionTr?.Trim(),
                        LifecycleState = command.LifecycleState,
                        AvailabilityState = command.AvailabilityState,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now,
                        Version = 1,
                    };
                    dbContext.SaasCommercialPlans.Add(plan);
                    await AppendAuditAsync(
                        SaasCommercialAuditEventTypes.PlanCreated,
                        SaasCommercialAuditEntityTypes.Plan,
                        plan.Id,
                        new { plan.PlanCode, plan.LifecycleState },
                        actorSubjectId,
                        now,
                        ct);
                }
                else
                {
                    existing.DisplayNameTr = command.DisplayNameTr.Trim();
                    existing.DescriptionTr = command.DescriptionTr?.Trim();
                    existing.LifecycleState = command.LifecycleState;
                    existing.AvailabilityState = command.AvailabilityState;
                    existing.UpdatedAtUtc = now;
                    await AppendAuditAsync(
                        SaasCommercialAuditEventTypes.PlanUpdated,
                        SaasCommercialAuditEntityTypes.Plan,
                        existing.Id,
                        new { existing.PlanCode, existing.LifecycleState },
                        actorSubjectId,
                        now,
                        ct);
                }

                await dbContext.SaveChangesAsync(ct);
                return SaasCommercialWriteResult.Ok();
            },
            cancellationToken);

    public Task<SaasCommercialWriteResult> CreateDraftPlanVersionAsync(
        CreateSaasCommercialPlanVersionCommand command,
        string actorSubjectId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validation = ValidateVersionCommand(command);
                if (validation is not null)
                {
                    return validation;
                }

                var normalized = SaasCommercialValueConversions.NormalizePlanCode(command.PlanCode);
                var plan = await dbContext.SaasCommercialPlans
                    .Include(p => p.Versions)
                    .FirstOrDefaultAsync(p => p.PlanCodeNormalized == normalized, ct);
                if (plan is null)
                {
                    return SaasCommercialWriteResult.Fail(
                        SaasCommercialFailureCodes.NotFound,
                        "Commercial plan was not found.");
                }

                if (plan.Version != command.ExpectedPlanVersion)
                {
                    return SaasCommercialWriteResult.Fail(
                        SaasCommercialFailureCodes.ConcurrencyConflict,
                        "Plan optimistic concurrency conflict.");
                }

                if (plan.Versions.Any(v => v.VersionNumber == command.VersionNumber))
                {
                    return SaasCommercialWriteResult.Fail(
                        SaasCommercialFailureCodes.Conflict,
                        "Plan version number already exists.");
                }

                var overlap = plan.Versions.Any(v =>
                    v.BillingCadence == command.BillingCadence
                    && SaasCommercialValueConversions.PeriodsOverlap(
                        command.EffectiveFromUtc,
                        command.EffectiveUntilUtc,
                        v.EffectiveFromUtc,
                        v.EffectiveUntilUtc));
                if (overlap)
                {
                    return SaasCommercialWriteResult.Fail(
                        SaasCommercialFailureCodes.Conflict,
                        "Effective date range overlaps an existing plan version for the same cadence.");
                }

                var now = DateTimeOffset.UtcNow;
                var version = new SaasCommercialPlanVersion
                {
                    Id = Guid.NewGuid(),
                    PlanId = plan.Id,
                    VersionNumber = command.VersionNumber,
                    EffectiveFromUtc = command.EffectiveFromUtc,
                    EffectiveUntilUtc = command.EffectiveUntilUtc,
                    BillingCadence = command.BillingCadence,
                    TrialDays = command.TrialDays,
                    CurrencyCode = command.CurrencyCode.ToUpperInvariant(),
                    Sellable = command.Sellable,
                    PublicationStatus = SaasCommercialPublicationStatuses.Draft,
                    CommercialNotes = command.CommercialNotes,
                    Provenance = command.Provenance,
                    CreatedAtUtc = now,
                    Version = 1,
                };
                dbContext.SaasCommercialPlanVersions.Add(version);
                plan.UpdatedAtUtc = now;

                await AppendAuditAsync(
                    SaasCommercialAuditEventTypes.VersionCreated,
                    SaasCommercialAuditEntityTypes.PlanVersion,
                    version.Id,
                    new { command.PlanCode, command.VersionNumber, command.BillingCadence },
                    actorSubjectId,
                    now,
                    ct);

                await dbContext.SaveChangesAsync(ct);
                return SaasCommercialWriteResult.Ok();
            },
            cancellationToken);

    public Task<SaasCommercialWriteResult> PublishDraftVersionAsync(
        PublishSaasCommercialPlanVersionCommand command,
        string actorSubjectId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var normalized = SaasCommercialValueConversions.NormalizePlanCode(command.PlanCode);
                var version = await dbContext.SaasCommercialPlanVersions
                    .Include(v => v.Plan)
                    .FirstOrDefaultAsync(
                        v => v.Plan!.PlanCodeNormalized == normalized && v.VersionNumber == command.VersionNumber,
                        ct);

                if (version is null)
                {
                    return SaasCommercialWriteResult.Fail(
                        SaasCommercialFailureCodes.NotFound,
                        "Plan version was not found.");
                }

                if (SaasCommercialValueConversions.IsPublishedImmutable(version.PublicationStatus))
                {
                    return SaasCommercialWriteResult.Fail(
                        SaasCommercialFailureCodes.ImmutablePublished,
                        "Published or retired versions are immutable.");
                }

                if (version.Version != command.ExpectedVersionVersion)
                {
                    return SaasCommercialWriteResult.Fail(
                        SaasCommercialFailureCodes.ConcurrencyConflict,
                        "Plan version optimistic concurrency conflict.");
                }

                var siblings = await dbContext.SaasCommercialPlanVersions
                    .Where(v => v.PlanId == version.PlanId
                        && v.BillingCadence == version.BillingCadence
                        && v.Id != version.Id)
                    .ToListAsync(ct);

                if (siblings.Any(v => SaasCommercialValueConversions.PeriodsOverlap(
                        version.EffectiveFromUtc,
                        version.EffectiveUntilUtc,
                        v.EffectiveFromUtc,
                        v.EffectiveUntilUtc)))
                {
                    return SaasCommercialWriteResult.Fail(
                        SaasCommercialFailureCodes.Conflict,
                        "Effective date range overlaps an existing plan version for the same cadence.");
                }

                var now = DateTimeOffset.UtcNow;
                version.PublicationStatus = SaasCommercialPublicationStatuses.Published;
                if (version.Plan is not null)
                {
                    version.Plan.LifecycleState = SaasCommercialPlanLifecycleStates.Active;
                    version.Plan.UpdatedAtUtc = now;
                }

                await AppendAuditAsync(
                    SaasCommercialAuditEventTypes.VersionPublished,
                    SaasCommercialAuditEntityTypes.PlanVersion,
                    version.Id,
                    new { command.PlanCode, command.VersionNumber },
                    actorSubjectId,
                    now,
                    ct);

                await dbContext.SaveChangesAsync(ct);
                return SaasCommercialWriteResult.Ok();
            },
            cancellationToken);

    public Task<SaasCommercialWriteResult> RetireVersionAsync(
        RetireSaasCommercialPlanVersionCommand command,
        string actorSubjectId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var normalized = SaasCommercialValueConversions.NormalizePlanCode(command.PlanCode);
                var version = await dbContext.SaasCommercialPlanVersions
                    .Include(v => v.Plan)
                    .FirstOrDefaultAsync(
                        v => v.Plan!.PlanCodeNormalized == normalized && v.VersionNumber == command.VersionNumber,
                        ct);

                if (version is null)
                {
                    return SaasCommercialWriteResult.Fail(
                        SaasCommercialFailureCodes.NotFound,
                        "Plan version was not found.");
                }

                if (version.PublicationStatus == SaasCommercialPublicationStatuses.Retired)
                {
                    return SaasCommercialWriteResult.Fail(
                        SaasCommercialFailureCodes.ImmutablePublished,
                        "Retired versions cannot be reactivated.");
                }

                var now = DateTimeOffset.UtcNow;
                version.PublicationStatus = SaasCommercialPublicationStatuses.Retired;
                if (version.Plan is not null)
                {
                    version.Plan.LifecycleState = SaasCommercialPlanLifecycleStates.Retired;
                    version.Plan.UpdatedAtUtc = now;
                }

                await AppendAuditAsync(
                    SaasCommercialAuditEventTypes.VersionRetired,
                    SaasCommercialAuditEntityTypes.PlanVersion,
                    version.Id,
                    new { command.PlanCode, command.VersionNumber },
                    actorSubjectId,
                    now,
                    ct);

                await dbContext.SaveChangesAsync(ct);
                return SaasCommercialWriteResult.Ok();
            },
            cancellationToken);

    private static SaasCommercialWriteResult? ValidateVersionCommand(CreateSaasCommercialPlanVersionCommand command)
    {
        if (!SaasCommercialValueConversions.IsValidBillingCadence(command.BillingCadence))
        {
            return SaasCommercialWriteResult.Fail(
                SaasCommercialFailureCodes.ValidationFailed,
                "Billing cadence must be monthly or annual.");
        }

        if (!SaasCommercialValueConversions.IsValidCurrencyCode(command.CurrencyCode))
        {
            return SaasCommercialWriteResult.Fail(
                SaasCommercialFailureCodes.ValidationFailed,
                "Currency code must be a valid ISO-style token.");
        }

        if (command.EffectiveUntilUtc is not null && command.EffectiveUntilUtc <= command.EffectiveFromUtc)
        {
            return SaasCommercialWriteResult.Fail(
                SaasCommercialFailureCodes.ValidationFailed,
                "Effective until must be after effective from.");
        }

        return null;
    }

    private async Task<SaasCommercialPlan?> LoadPlanWithVersionsAsync(string normalizedPlanCode, CancellationToken ct) =>
        await dbContext.SaasCommercialPlans.AsNoTracking()
            .Include(p => p.Versions)
            .ThenInclude(v => v.PriceComponents)
            .FirstOrDefaultAsync(p => p.PlanCodeNormalized == normalizedPlanCode, ct);

    private async Task<SaasCommercialPlanVersion?> LoadVersionAsync(
        string normalizedPlanCode,
        int versionNumber,
        CancellationToken ct) =>
        await dbContext.SaasCommercialPlanVersions.AsNoTracking()
            .Include(v => v.Plan)
            .Include(v => v.PriceComponents)
            .FirstOrDefaultAsync(
                v => v.Plan!.PlanCodeNormalized == normalizedPlanCode && v.VersionNumber == versionNumber,
                ct);

    private async Task<SaasCommercialPlanVersion?> LoadLatestReadableVersionAsync(
        string normalizedPlanCode,
        CancellationToken ct) =>
        await dbContext.SaasCommercialPlanVersions.AsNoTracking()
            .Include(v => v.Plan)
            .Include(v => v.PriceComponents)
            .Where(v => v.Plan!.PlanCodeNormalized == normalizedPlanCode)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(ct);

    private static SaasCommercialPlanSummaryReadModel MapPlanSummary(SaasCommercialPlan plan) =>
        new(
            plan.PlanCode,
            plan.DisplayNameTr,
            plan.DescriptionTr,
            plan.LifecycleState,
            plan.AvailabilityState,
            plan.Versions
                .OrderBy(v => v.BillingCadence)
                .ThenBy(v => v.VersionNumber)
                .Select(v => new SaasCommercialPlanVersionSummaryReadModel(
                    v.VersionNumber,
                    v.BillingCadence,
                    v.EffectiveFromUtc,
                    v.EffectiveUntilUtc,
                    v.PublicationStatus,
                    v.Sellable,
                    v.Provenance))
                .ToList());

    private static SaasCommercialPlanDetailReadModel MapPlanDetail(SaasCommercialPlan plan) =>
        new(
            plan.PlanCode,
            plan.DisplayNameTr,
            plan.DescriptionTr,
            plan.LifecycleState,
            plan.AvailabilityState,
            plan.Versions
                .OrderBy(v => v.BillingCadence)
                .ThenBy(v => v.VersionNumber)
                .Select(MapVersionDetail)
                .ToList());

    private static SaasCommercialPlanVersionDetailReadModel MapVersionDetail(SaasCommercialPlanVersion version) =>
        new(
            version.VersionNumber,
            version.BillingCadence,
            version.EffectiveFromUtc,
            version.EffectiveUntilUtc,
            version.TrialDays,
            version.CurrencyCode,
            version.Sellable,
            version.PublicationStatus,
            version.CommercialNotes,
            version.Provenance,
            version.PriceComponents
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.ComponentType)
                .Select(c => new SaasCommercialPriceComponentReadModel(
                    c.ComponentType,
                    c.Amount,
                    c.CurrencyCode,
                    c.SortOrder))
                .ToList());

    private static SaasCommercialEntitlementReadModel MapEntitlement(SaasCommercialPlanVersionEntitlement grant) =>
        new(
            grant.EntitlementDefinition!.Code,
            grant.EntitlementDefinition.DisplayNameTr,
            grant.EntitlementDefinition.DescriptionTr,
            grant.EntitlementDefinition.ValueKind,
            grant.Semantics,
            grant.BooleanValue,
            grant.IntegerLimitValue);

    private static SaasCommercialPaymentMethodCapabilityReadModel MapPaymentMethod(
        SaasCommercialPaymentMethodCapability capability) =>
        new(
            capability.MethodType,
            capability.Enabled,
            capability.ManualVerificationRequired,
            capability.RecurringCapable,
            capability.DisplayOrder,
            capability.EffectiveFromUtc,
            capability.EffectiveUntilUtc);

    private async Task AppendAuditAsync(
        string eventType,
        string entityType,
        Guid entityId,
        object payload,
        string actorSubjectId,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        dbContext.SaasCommercialAuditEvents.Add(new SaasCommercialAuditEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            ActorSubjectId = actorSubjectId,
            OccurredAtUtc = occurredAtUtc,
        });
        await Task.CompletedTask;
    }
}

public sealed class UnavailableSaasCommercialRepository : ISaasCommercialRepository
{
    private static SaasCommercialWriteResult Unavailable() =>
        SaasCommercialWriteResult.Fail(SaasCommercialFailureCodes.Unavailable, "SaaS commercial persistence is unavailable.");

    public Task<SaasCommercialCatalogListReadModel> ListAvailablePlansAsync(
        SaasCommercialOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new SaasCommercialCatalogListReadModel([]));

    public Task<SaasCommercialPlanDetailReadModel?> GetPlanByCodeAsync(
        SaasCommercialOperationContext context,
        string planCode,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<SaasCommercialPlanDetailReadModel?>(null);

    public Task<SaasCommercialPlanVersionDetailReadModel?> GetPlanVersionAsync(
        SaasCommercialOperationContext context,
        string planCode,
        int versionNumber,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<SaasCommercialPlanVersionDetailReadModel?>(null);

    public Task<SaasCommercialPlanVersionDetailReadModel?> ResolveVersionByEffectiveDateAndCadenceAsync(
        SaasCommercialOperationContext context,
        string planCode,
        string billingCadence,
        DateTimeOffset effectiveAtUtc,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<SaasCommercialPlanVersionDetailReadModel?>(null);

    public Task<IReadOnlyList<SaasCommercialEntitlementReadModel>> ResolvePlanEntitlementsAsync(
        SaasCommercialOperationContext context,
        string planCode,
        int? versionNumber,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SaasCommercialEntitlementReadModel>>([]);

    public Task<IReadOnlyList<SaasCommercialPaymentMethodCapabilityReadModel>> ResolvePaymentMethodCapabilitiesAsync(
        SaasCommercialOperationContext context,
        string planCode,
        int? versionNumber,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SaasCommercialPaymentMethodCapabilityReadModel>>([]);

    public Task<SaasCommercialWriteResult> CreateOrUpdateDraftPlanAsync(
        CreateSaasCommercialPlanCommand command,
        string actorSubjectId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<SaasCommercialWriteResult> CreateDraftPlanVersionAsync(
        CreateSaasCommercialPlanVersionCommand command,
        string actorSubjectId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<SaasCommercialWriteResult> PublishDraftVersionAsync(
        PublishSaasCommercialPlanVersionCommand command,
        string actorSubjectId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<SaasCommercialWriteResult> RetireVersionAsync(
        RetireSaasCommercialPlanVersionCommand command,
        string actorSubjectId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}
