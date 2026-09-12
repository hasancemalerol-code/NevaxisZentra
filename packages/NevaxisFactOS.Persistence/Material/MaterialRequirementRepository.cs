using System.Text.Json;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Material;

public interface IMaterialRequirementRepository
{
    Task<IReadOnlyList<MaterialRequirementProfileSummaryReadModel>> ListProfilesAsync(
        MaterialOperationContext context,
        MaterialRequirementProfileListFilters filters,
        CancellationToken cancellationToken = default);

    Task<MaterialRequirementProfileDetailReadModel?> GetProfileByIdAsync(
        MaterialOperationContext context,
        Guid profileId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaterialConsumptionObservationReadModel>> ListObservationsAsync(
        MaterialOperationContext context,
        MaterialConsumptionObservationListFilters filters,
        CancellationToken cancellationToken = default);

    Task<MaterialConsumptionObservationReadModel?> GetObservationByIdAsync(
        MaterialOperationContext context,
        Guid observationId,
        CancellationToken cancellationToken = default);

    Task<MaterialRequirementOverviewReadModel?> GetOverviewAsync(
        MaterialOperationContext context,
        Guid materialId,
        CancellationToken cancellationToken = default);

    Task<MaterialRequirementAssessmentReadModel?> GetAssessmentByIdAsync(
        MaterialOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task<MaterialRequirementWriteResult> CreateProfileAsync(
        MaterialOperationContext context,
        CreateMaterialRequirementProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<MaterialRequirementWriteResult> UpdateProfileAsync(
        MaterialOperationContext context,
        UpdateMaterialRequirementProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<MaterialRequirementWriteResult> CreateObservationAsync(
        MaterialOperationContext context,
        CreateMaterialConsumptionObservationCommand command,
        CancellationToken cancellationToken = default);

    Task<MaterialRequirementWriteResult> EvaluateRequirementAsync(
        MaterialOperationContext context,
        EvaluateMaterialRequirementCommand command,
        CancellationToken cancellationToken = default);

    Task<MaterialRequirementWriteResult> EvaluateVarianceAsync(
        MaterialOperationContext context,
        EvaluateMaterialVarianceCommand command,
        CancellationToken cancellationToken = default);

    Task<MaterialRequirementWriteResult> EvaluateRegrindPolicyAsync(
        MaterialOperationContext context,
        EvaluateMaterialRegrindPolicyCommand command,
        CancellationToken cancellationToken = default);

    Task<MaterialRequirementWriteResult> EvaluateAvailabilityRiskAsync(
        MaterialOperationContext context,
        EvaluateMaterialAvailabilityRiskCommand command,
        bool hasErpReadAccess,
        CancellationToken cancellationToken = default);
}

public sealed class MaterialRequirementRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IMaterialRequirementRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<IReadOnlyList<MaterialRequirementProfileSummaryReadModel>> ListProfilesAsync(
        MaterialOperationContext context,
        MaterialRequirementProfileListFilters filters,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(filters.Page, 1);
        var pageSize = Math.Clamp(filters.PageSize, 1, 100);

        return tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var query = dbContext.MaterialRequirementProfiles.AsNoTracking()
                    .Include(p => p.Material)
                    .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId);

                if (filters.MaterialId.HasValue)
                {
                    query = query.Where(p => p.MaterialId == filters.MaterialId.Value);
                }

                if (!string.IsNullOrWhiteSpace(filters.RequirementType))
                {
                    var type = MaterialRequirementValueConversions.RequirementTypeFromStorage(filters.RequirementType.Trim());
                    query = query.Where(p => p.RequirementType == type);
                }

                var profiles = await query.ToListAsync(ct);

                return (IReadOnlyList<MaterialRequirementProfileSummaryReadModel>)profiles
                    .OrderByDescending(p => p.UpdatedAtUtc)
                    .ThenByDescending(p => p.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(MapProfileSummary)
                    .ToList();
            },
            cancellationToken);
    }

    public Task<MaterialRequirementProfileDetailReadModel?> GetProfileByIdAsync(
        MaterialOperationContext context,
        Guid profileId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var profile = await dbContext.MaterialRequirementProfiles.AsNoTracking()
                    .Include(p => p.Material)
                    .FirstOrDefaultAsync(
                        p => p.Id == profileId
                             && p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId,
                        ct);

                return profile is null ? null : MapProfileDetail(profile);
            },
            cancellationToken);

    public Task<IReadOnlyList<MaterialConsumptionObservationReadModel>> ListObservationsAsync(
        MaterialOperationContext context,
        MaterialConsumptionObservationListFilters filters,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(filters.Page, 1);
        var pageSize = Math.Clamp(filters.PageSize, 1, 100);

        return tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var query = dbContext.MaterialConsumptionObservations.AsNoTracking()
                    .Include(o => o.Material)
                    .Include(o => o.MaterialLot)
                    .Where(o => o.TenantId == context.TenantId && o.FacilityId == context.FacilityId);

                if (filters.MaterialId.HasValue)
                {
                    query = query.Where(o => o.MaterialId == filters.MaterialId.Value);
                }

                if (filters.RequirementProfileId.HasValue)
                {
                    query = query.Where(o => o.RequirementProfileId == filters.RequirementProfileId.Value);
                }

                if (!string.IsNullOrWhiteSpace(filters.ObservationType))
                {
                    var type = MaterialRequirementValueConversions.ObservationTypeFromStorage(filters.ObservationType.Trim());
                    query = query.Where(o => o.ObservationType == type);
                }

                var observations = await query.ToListAsync(ct);

                return (IReadOnlyList<MaterialConsumptionObservationReadModel>)observations
                    .OrderByDescending(o => o.ObservedAtUtc)
                    .ThenByDescending(o => o.CreatedAtUtc)
                    .ThenByDescending(o => o.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(MapObservation)
                    .ToList();
            },
            cancellationToken);
    }

    public Task<MaterialConsumptionObservationReadModel?> GetObservationByIdAsync(
        MaterialOperationContext context,
        Guid observationId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var observation = await dbContext.MaterialConsumptionObservations.AsNoTracking()
                    .Include(o => o.Material)
                    .Include(o => o.MaterialLot)
                    .FirstOrDefaultAsync(
                        o => o.Id == observationId
                             && o.TenantId == context.TenantId
                             && o.FacilityId == context.FacilityId,
                        ct);

                return observation is null ? null : MapObservation(observation);
            },
            cancellationToken);

    public Task<MaterialRequirementOverviewReadModel?> GetOverviewAsync(
        MaterialOperationContext context,
        Guid materialId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var material = await dbContext.Materials.AsNoTracking()
                    .FirstOrDefaultAsync(
                        m => m.Id == materialId
                             && m.TenantId == context.TenantId
                             && m.FacilityId == context.FacilityId,
                        ct);

                if (material is null)
                {
                    return null;
                }

                var profiles = await dbContext.MaterialRequirementProfiles.AsNoTracking()
                    .Include(p => p.Material)
                    .Where(p => p.MaterialId == materialId
                                && p.TenantId == context.TenantId
                                && p.FacilityId == context.FacilityId)
                    .OrderByDescending(p => p.UpdatedAtUtc)
                    .Take(10)
                    .ToListAsync(ct);

                var observations = await dbContext.MaterialConsumptionObservations.AsNoTracking()
                    .Include(o => o.Material)
                    .Include(o => o.MaterialLot)
                    .Where(o => o.MaterialId == materialId
                                && o.TenantId == context.TenantId
                                && o.FacilityId == context.FacilityId)
                    .OrderByDescending(o => o.ObservedAtUtc)
                    .ThenByDescending(o => o.CreatedAtUtc)
                    .Take(10)
                    .ToListAsync(ct);

                var assessments = await dbContext.MaterialRequirementAssessments.AsNoTracking()
                    .Where(a => a.MaterialId == materialId
                                && a.TenantId == context.TenantId
                                && a.FacilityId == context.FacilityId)
                    .OrderByDescending(a => a.EvaluatedAtUtc)
                    .ThenByDescending(a => a.CreatedAtUtc)
                    .ToListAsync(ct);

                return new MaterialRequirementOverviewReadModel(
                    materialId,
                    material.Code,
                    FindLatestAssessment(assessments, MaterialRequirementAssessmentType.Requirement),
                    FindLatestAssessment(assessments, MaterialRequirementAssessmentType.ConsumptionVariance),
                    FindLatestAssessment(assessments, MaterialRequirementAssessmentType.RegrindPolicy),
                    FindLatestAssessment(assessments, MaterialRequirementAssessmentType.AvailabilityRisk),
                    profiles.Select(MapProfileSummary).ToList(),
                    observations.Select(MapObservation).ToList(),
                    MaterialRequirementConstants.TheoreticalRequirementDisclaimer,
                    MaterialRequirementConstants.ConsumptionObservationDisclaimer);
            },
            cancellationToken);

    public Task<MaterialRequirementAssessmentReadModel?> GetAssessmentByIdAsync(
        MaterialOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var assessment = await dbContext.MaterialRequirementAssessments.AsNoTracking()
                    .FirstOrDefaultAsync(
                        a => a.Id == assessmentId
                             && a.TenantId == context.TenantId
                             && a.FacilityId == context.FacilityId,
                        ct);

                return assessment is null ? null : MapAssessment(assessment);
            },
            cancellationToken);

    public Task<MaterialRequirementWriteResult> CreateProfileAsync(
        MaterialOperationContext context,
        CreateMaterialRequirementProfileCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationErrors = MaterialRequirementValidator.ValidateProfile(command);
                if (validationErrors.Count > 0)
                {
                    return MaterialRequirementWriteResult.Fail(
                        MaterialRequirementFailureCodes.ValidationFailed,
                        string.Join(" ", validationErrors));
                }

                var material = await dbContext.Materials
                    .FirstOrDefaultAsync(
                        m => m.Id == command.MaterialId
                             && m.TenantId == context.TenantId
                             && m.FacilityId == context.FacilityId,
                        ct);

                if (material is null)
                {
                    return MaterialRequirementWriteResult.Fail(
                        MaterialRequirementFailureCodes.NotFound,
                        "Material not found.");
                }

                string? fingerprint = null;
                if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
                {
                    fingerprint = MaterialRequirementIdempotencyFingerprint.ComputeProfileCreateFingerprint(context, command);
                    var existing = await dbContext.MaterialRequirementProfiles
                        .FirstOrDefaultAsync(
                            p => p.TenantId == context.TenantId
                                 && p.CreateIdempotencyKey == command.IdempotencyKey.Trim(),
                            ct);

                    if (existing is not null)
                    {
                        return ResolveProfileCreateIdempotency(existing, fingerprint);
                    }
                }

                var actor = context.IdentitySubjectId.ToString();
                var now = DateTimeOffset.UtcNow;
                var profile = new MaterialRequirementProfile
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    MaterialId = command.MaterialId,
                    ProductionOrderReference = TrimOrNull(command.ProductionOrderReference),
                    ProductReference = TrimOrNull(command.ProductReference),
                    MoldReference = TrimOrNull(command.MoldReference),
                    RequirementType = MaterialRequirementValueConversions.RequirementTypeFromStorage(command.RequirementType),
                    PlannedProductionQuantity = command.PlannedProductionQuantity,
                    GoodPartTargetQuantity = command.GoodPartTargetQuantity,
                    PartWeightGrams = command.PartWeightGrams,
                    RunnerWeightGramsPerShot = command.RunnerWeightGramsPerShot,
                    EffectiveCavityCount = command.EffectiveCavityCount,
                    SetupAllowanceGrams = command.SetupAllowanceGrams,
                    ProcessLossAllowancePercent = command.ProcessLossAllowancePercent,
                    PlannedVirginPercent = command.PlannedVirginPercent,
                    PlannedRegrindPercent = command.PlannedRegrindPercent,
                    SourceType = MaterialValueConversions.SourceFromStorage(command.SourceType),
                    SourceReference = TrimOrNull(command.SourceReference),
                    DataRecordedAtUtc = command.DataRecordedAtUtc,
                    CreateIdempotencyKey = TrimOrNull(command.IdempotencyKey),
                    CreateIdempotencyPayloadFingerprint = fingerprint,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedBySubjectId = actor,
                    UpdatedBySubjectId = actor,
                    Version = 1,
                };

                dbContext.MaterialRequirementProfiles.Add(profile);
                await dbContext.SaveChangesAsync(ct);

                return MaterialRequirementWriteResult.Success(profile.Id, profile.Version);
            },
            cancellationToken);

    public Task<MaterialRequirementWriteResult> UpdateProfileAsync(
        MaterialOperationContext context,
        UpdateMaterialRequirementProfileCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationErrors = MaterialRequirementValidator.ValidateProfileUpdate(command);
                if (validationErrors.Count > 0)
                {
                    return MaterialRequirementWriteResult.Fail(
                        MaterialRequirementFailureCodes.ValidationFailed,
                        string.Join(" ", validationErrors));
                }

                var profile = await dbContext.MaterialRequirementProfiles
                    .FirstOrDefaultAsync(
                        p => p.Id == command.ProfileId
                             && p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId,
                        ct);

                if (profile is null)
                {
                    return MaterialRequirementWriteResult.Fail(
                        MaterialRequirementFailureCodes.NotFound,
                        "Requirement profile not found.");
                }

                if (profile.Version != command.ExpectedVersion)
                {
                    return MaterialRequirementWriteResult.Fail(
                        MaterialRequirementFailureCodes.ConcurrencyConflict,
                        "The profile was modified by another user.");
                }

                profile.RequirementType = MaterialRequirementValueConversions.RequirementTypeFromStorage(command.RequirementType);
                profile.ProductionOrderReference = TrimOrNull(command.ProductionOrderReference);
                profile.ProductReference = TrimOrNull(command.ProductReference);
                profile.MoldReference = TrimOrNull(command.MoldReference);
                profile.PlannedProductionQuantity = command.PlannedProductionQuantity;
                profile.GoodPartTargetQuantity = command.GoodPartTargetQuantity;
                profile.PartWeightGrams = command.PartWeightGrams;
                profile.RunnerWeightGramsPerShot = command.RunnerWeightGramsPerShot;
                profile.EffectiveCavityCount = command.EffectiveCavityCount;
                profile.SetupAllowanceGrams = command.SetupAllowanceGrams;
                profile.ProcessLossAllowancePercent = command.ProcessLossAllowancePercent;
                profile.PlannedVirginPercent = command.PlannedVirginPercent;
                profile.PlannedRegrindPercent = command.PlannedRegrindPercent;
                profile.SourceType = MaterialValueConversions.SourceFromStorage(command.SourceType);
                profile.SourceReference = TrimOrNull(command.SourceReference);
                profile.DataRecordedAtUtc = command.DataRecordedAtUtc;
                profile.UpdatedAtUtc = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                profile.UpdatedBySubjectId = actor;
                profile.Version++;

                await dbContext.SaveChangesAsync(ct);
                return MaterialRequirementWriteResult.Success(profile.Id, profile.Version);
            },
            cancellationToken);

    public Task<MaterialRequirementWriteResult> CreateObservationAsync(
        MaterialOperationContext context,
        CreateMaterialConsumptionObservationCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationErrors = MaterialRequirementValidator.ValidateObservation(command);
                if (validationErrors.Count > 0)
                {
                    return MaterialRequirementWriteResult.Fail(
                        MaterialRequirementFailureCodes.ValidationFailed,
                        string.Join(" ", validationErrors));
                }

                var material = await dbContext.Materials
                    .FirstOrDefaultAsync(
                        m => m.Id == command.MaterialId
                             && m.TenantId == context.TenantId
                             && m.FacilityId == context.FacilityId,
                        ct);

                if (material is null)
                {
                    return MaterialRequirementWriteResult.Fail(
                        MaterialRequirementFailureCodes.NotFound,
                        "Material not found.");
                }

                if (command.MaterialLotId.HasValue)
                {
                    var lot = await dbContext.MaterialLots
                        .FirstOrDefaultAsync(
                            l => l.Id == command.MaterialLotId.Value
                                 && l.TenantId == context.TenantId
                                 && l.FacilityId == context.FacilityId,
                            ct);

                    if (lot is null || lot.MaterialId != command.MaterialId)
                    {
                        return MaterialRequirementWriteResult.Fail(
                            MaterialRequirementFailureCodes.InvalidLot,
                            "Lot does not belong to the specified material, tenant, or facility.");
                    }
                }

                var fingerprint = MaterialRequirementIdempotencyFingerprint.ComputeObservationFingerprint(context, command);
                var existing = await dbContext.MaterialConsumptionObservations
                    .FirstOrDefaultAsync(
                        o => o.TenantId == context.TenantId
                             && o.MaterialId == command.MaterialId
                             && o.IdempotencyKey == command.IdempotencyKey.Trim(),
                        ct);

                if (existing is not null)
                {
                    if (existing.PayloadFingerprint is null)
                    {
                        return MaterialRequirementWriteResult.Fail(
                            MaterialRequirementFailureCodes.IdempotencyLegacyUnverifiable,
                            "The idempotency key belongs to a legacy observation that cannot be verified safely.");
                    }

                    if (!MaterialRequirementIdempotencyFingerprint.ObservationPayloadMatches(existing, command, fingerprint))
                    {
                        return MaterialRequirementWriteResult.Fail(
                            MaterialRequirementFailureCodes.IdempotencyConflict,
                            "The idempotency key was already used with a different command payload.");
                    }

                    return MaterialRequirementWriteResult.Success(existing.Id, existing.Version, idempotentReplay: true);
                }

                var actor = context.IdentitySubjectId.ToString();
                var now = DateTimeOffset.UtcNow;
                var observation = new MaterialConsumptionObservation
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    MaterialId = command.MaterialId,
                    MaterialLotId = command.MaterialLotId,
                    RequirementProfileId = command.RequirementProfileId,
                    ProductionOrderReference = TrimOrNull(command.ProductionOrderReference),
                    ProductReference = TrimOrNull(command.ProductReference),
                    MoldReference = TrimOrNull(command.MoldReference),
                    ObservationType = MaterialRequirementValueConversions.ObservationTypeFromStorage(command.ObservationType),
                    Quantity = command.Quantity,
                    UnitOfMeasure = command.UnitOfMeasure.Trim(),
                    VirginQuantity = command.VirginQuantity,
                    RegrindQuantity = command.RegrindQuantity,
                    GoodPartQuantity = command.GoodPartQuantity,
                    ScrapQuantity = command.ScrapQuantity,
                    ObservedAtUtc = command.ObservedAtUtc,
                    SourceType = MaterialValueConversions.SourceFromStorage(command.SourceType),
                    SourceReference = TrimOrNull(command.SourceReference),
                    DataRecordedAtUtc = command.DataRecordedAtUtc,
                    Notes = TrimOrNull(command.Notes),
                    IdempotencyKey = command.IdempotencyKey.Trim(),
                    PayloadFingerprint = fingerprint,
                    CreatedAtUtc = now,
                    CreatedBySubjectId = actor,
                    Version = 1,
                };

                dbContext.MaterialConsumptionObservations.Add(observation);
                await dbContext.SaveChangesAsync(ct);

                return MaterialRequirementWriteResult.Success(observation.Id, observation.Version);
            },
            cancellationToken);

    public Task<MaterialRequirementWriteResult> EvaluateRequirementAsync(
        MaterialOperationContext context,
        EvaluateMaterialRequirementCommand command,
        CancellationToken cancellationToken = default) =>
        EvaluateAndPersistAsync(
            context,
            command.MaterialId,
            command.RequirementProfileId,
            MaterialRequirementAssessmentType.Requirement,
            command.IdempotencyKey,
            async ct =>
            {
                var profile = await GetProfileEntityAsync(context, command.RequirementProfileId, ct);
                if (profile is null)
                {
                    return (null, MaterialRequirementWriteResult.Fail(MaterialRequirementFailureCodes.NotFound, "Requirement profile not found."));
                }

                var detail = MapProfileDetail(profile);
                var calculation = MaterialRequirementCalculator.CalculateRequirement(detail);
                var inputJson = JsonSerializer.Serialize(calculation, JsonOptions);
                return (BuildRequirementAssessment(context, profile, calculation, inputJson), null);
            },
            cancellationToken);

    public Task<MaterialRequirementWriteResult> EvaluateVarianceAsync(
        MaterialOperationContext context,
        EvaluateMaterialVarianceCommand command,
        CancellationToken cancellationToken = default) =>
        EvaluateAndPersistAsync(
            context,
            command.MaterialId,
            command.RequirementProfileId,
            MaterialRequirementAssessmentType.ConsumptionVariance,
            command.IdempotencyKey,
            async ct =>
            {
                var profile = await GetProfileEntityAsync(context, command.RequirementProfileId, ct);
                if (profile is null)
                {
                    return (null, MaterialRequirementWriteResult.Fail(MaterialRequirementFailureCodes.NotFound, "Requirement profile not found."));
                }

                var detail = MapProfileDetail(profile);
                var requirement = MaterialRequirementCalculator.CalculateRequirement(detail);

                var observations = await dbContext.MaterialConsumptionObservations.AsNoTracking()
                    .Where(o => o.MaterialId == command.MaterialId
                                && o.TenantId == context.TenantId
                                && o.FacilityId == context.FacilityId
                                && (o.RequirementProfileId == command.RequirementProfileId || o.RequirementProfileId == null))
                    .ToListAsync(ct);

                if (observations.Count == 0)
                {
                    var noData = new MaterialVarianceAssessmentResult(
                        "insufficient-data", requirement.TheoreticalRequirementGrams, null, null, null,
                        MaterialRequirementConstants.DefaultVarianceTolerancePercent, [], []);
                    var inputJson = JsonSerializer.Serialize(noData, JsonOptions);
                    return (BuildVarianceAssessment(context, profile, noData, inputJson), null);
                }

                var observedGrams = observations.Sum(o =>
                    MaterialRequirementCalculator.ConvertQuantityToGrams(o.Quantity, o.UnitOfMeasure));

                var variance = MaterialRequirementCalculator.AssessVariance(requirement, observedGrams);
                var varianceInputJson = JsonSerializer.Serialize(variance, JsonOptions);
                return (BuildVarianceAssessment(context, profile, variance, varianceInputJson), null);
            },
            cancellationToken);

    public Task<MaterialRequirementWriteResult> EvaluateRegrindPolicyAsync(
        MaterialOperationContext context,
        EvaluateMaterialRegrindPolicyCommand command,
        CancellationToken cancellationToken = default) =>
        EvaluateAndPersistAsync(
            context,
            command.MaterialId,
            command.RequirementProfileId,
            MaterialRequirementAssessmentType.RegrindPolicy,
            command.IdempotencyKey,
            async ct =>
            {
                var profile = await GetProfileEntityAsync(context, command.RequirementProfileId, ct);
                if (profile is null)
                {
                    return (null, MaterialRequirementWriteResult.Fail(MaterialRequirementFailureCodes.NotFound, "Requirement profile not found."));
                }

                var material = await dbContext.Materials.AsNoTracking()
                    .Include(m => m.TechnicalSpecification)
                    .FirstOrDefaultAsync(m => m.Id == command.MaterialId, ct);

                var detail = MapProfileDetail(profile);
                var spec = material?.TechnicalSpecification;
                var regrind = MaterialRequirementCalculator.AssessRegrindPolicy(
                    detail,
                    spec?.MaximumRegrindPercent,
                    spec?.SpecificationRecordedAtUtc,
                    spec is null ? null : MaterialValueConversions.ToStorage(spec.SpecificationSource));

                var inputJson = JsonSerializer.Serialize(regrind, JsonOptions);
                return (BuildRegrindAssessment(context, profile, regrind, inputJson), null);
            },
            cancellationToken);

    public Task<MaterialRequirementWriteResult> EvaluateAvailabilityRiskAsync(
        MaterialOperationContext context,
        EvaluateMaterialAvailabilityRiskCommand command,
        bool hasErpReadAccess,
        CancellationToken cancellationToken = default) =>
        EvaluateAndPersistAsync(
            context,
            command.MaterialId,
            command.RequirementProfileId,
            MaterialRequirementAssessmentType.AvailabilityRisk,
            command.IdempotencyKey,
            async ct =>
            {
                var material = await dbContext.Materials.AsNoTracking()
                    .FirstOrDefaultAsync(
                        m => m.Id == command.MaterialId
                             && m.TenantId == context.TenantId
                             && m.FacilityId == context.FacilityId,
                        ct);

                if (material is null)
                {
                    return (null, MaterialRequirementWriteResult.Fail(MaterialRequirementFailureCodes.NotFound, "Material not found."));
                }

                MaterialRequirementProfile? profileEntity = null;
                MaterialRequirementProfileDetailReadModel? profileDetail = null;
                MaterialRequirementCalculationResult? requirement = null;

                if (command.RequirementProfileId.HasValue)
                {
                    profileEntity = await GetProfileEntityAsync(context, command.RequirementProfileId.Value, ct);
                    if (profileEntity is null)
                    {
                        return (null, MaterialRequirementWriteResult.Fail(MaterialRequirementFailureCodes.NotFound, "Requirement profile not found."));
                    }

                    profileDetail = MapProfileDetail(profileEntity);
                    requirement = MaterialRequirementCalculator.CalculateRequirement(profileDetail);
                }

                var erpBalances = hasErpReadAccess
                    ? await dbContext.CanonicalErpInventoryBalances.AsNoTracking()
                        .Where(b => b.TenantId == context.TenantId && b.FacilityId == context.FacilityId)
                        .ToListAsync(ct)
                    : [];

                var availability = MaterialAvailabilityRiskCalculator.AssessAvailabilityRisk(
                    material,
                    profileDetail,
                    requirement,
                    erpBalances,
                    DateTimeOffset.UtcNow,
                    hasErpReadAccess);

                var inputJson = JsonSerializer.Serialize(availability, JsonOptions);
                return (BuildAvailabilityAssessment(context, material.Id, profileEntity?.Id, availability, inputJson), null);
            },
            cancellationToken);

    private async Task<MaterialRequirementWriteResult> EvaluateAndPersistAsync(
        MaterialOperationContext context,
        Guid materialId,
        Guid? requirementProfileId,
        MaterialRequirementAssessmentType assessmentType,
        string? idempotencyKey,
        Func<CancellationToken, Task<(MaterialRequirementAssessment? Assessment, MaterialRequirementWriteResult? Failure)>> buildAssessment,
        CancellationToken cancellationToken)
    {
        return await tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var (assessment, failure) = await buildAssessment(ct);
                if (failure is not null)
                {
                    return failure;
                }

                if (assessment is null)
                {
                    return MaterialRequirementWriteResult.Fail(
                        MaterialRequirementFailureCodes.ValidationFailed,
                        "Assessment could not be built.");
                }

                if (!string.IsNullOrWhiteSpace(idempotencyKey))
                {
                    var trimmedKey = idempotencyKey.Trim();
                    var existingByKey = await dbContext.MaterialRequirementAssessments
                        .FirstOrDefaultAsync(
                            a => a.TenantId == context.TenantId
                                 && a.MaterialId == materialId
                                 && a.AssessmentType == assessmentType
                                 && a.IdempotencyKey == trimmedKey,
                            ct);

                    if (existingByKey is not null)
                    {
                        if (existingByKey.InputFingerprint != assessment.InputFingerprint)
                        {
                            return MaterialRequirementWriteResult.Fail(
                                MaterialRequirementFailureCodes.AssessmentConcurrencyConflict,
                                "The idempotency key was already used with a different evaluation input.");
                        }

                        return MaterialRequirementWriteResult.Success(
                            existingByKey.Id, existingByKey.Version, idempotentReplay: true);
                    }
                }

                var existingByFingerprint = await dbContext.MaterialRequirementAssessments
                    .FirstOrDefaultAsync(
                        a => a.TenantId == context.TenantId
                             && a.MaterialId == materialId
                             && a.AssessmentType == assessmentType
                             && a.InputFingerprint == assessment.InputFingerprint,
                        ct);

                if (existingByFingerprint is not null)
                {
                    if (!string.IsNullOrWhiteSpace(idempotencyKey))
                    {
                        var trimmedKey = idempotencyKey.Trim();
                        if (!string.IsNullOrWhiteSpace(existingByFingerprint.IdempotencyKey)
                            && !string.Equals(existingByFingerprint.IdempotencyKey, trimmedKey, StringComparison.Ordinal))
                        {
                            return MaterialRequirementWriteResult.Fail(
                                MaterialRequirementFailureCodes.AssessmentConcurrencyConflict,
                                "The idempotency key was already used with a different evaluation input.");
                        }
                    }

                    return MaterialRequirementWriteResult.Success(
                        existingByFingerprint.Id, existingByFingerprint.Version, idempotentReplay: true);
                }

                if (!string.IsNullOrWhiteSpace(idempotencyKey))
                {
                    assessment.IdempotencyKey = idempotencyKey.Trim();
                }

                dbContext.MaterialRequirementAssessments.Add(assessment);

                try
                {
                    await dbContext.SaveChangesAsync(ct);
                }
                catch (DbUpdateException)
                {
                    var replay = await dbContext.MaterialRequirementAssessments
                        .FirstOrDefaultAsync(
                            a => a.TenantId == context.TenantId
                                 && a.MaterialId == materialId
                                 && a.AssessmentType == assessmentType
                                 && a.InputFingerprint == assessment.InputFingerprint,
                            ct);

                    if (replay is not null)
                    {
                        return MaterialRequirementWriteResult.Success(replay.Id, replay.Version, idempotentReplay: true);
                    }

                    return MaterialRequirementWriteResult.Fail(
                        MaterialRequirementFailureCodes.AssessmentConcurrencyConflict,
                        "Concurrent evaluation conflict.");
                }

                return MaterialRequirementWriteResult.Success(assessment.Id, assessment.Version);
            },
            cancellationToken);
    }

    private async Task<MaterialRequirementProfile?> GetProfileEntityAsync(
        MaterialOperationContext context,
        Guid profileId,
        CancellationToken ct) =>
        await dbContext.MaterialRequirementProfiles
            .Include(p => p.Material)
            .FirstOrDefaultAsync(
                p => p.Id == profileId
                     && p.TenantId == context.TenantId
                     && p.FacilityId == context.FacilityId,
                ct);

    private static MaterialRequirementWriteResult ResolveProfileCreateIdempotency(
        MaterialRequirementProfile existing,
        string fingerprint)
    {
        if (existing.CreateIdempotencyPayloadFingerprint is null)
        {
            return MaterialRequirementWriteResult.Fail(
                MaterialRequirementFailureCodes.IdempotencyLegacyUnverifiable,
                "The idempotency key belongs to a legacy create that cannot be verified safely.");
        }

        if (existing.CreateIdempotencyPayloadFingerprint != fingerprint)
        {
            return MaterialRequirementWriteResult.Fail(
                MaterialRequirementFailureCodes.IdempotencyConflict,
                "The idempotency key was already used with a different command payload.");
        }

        return MaterialRequirementWriteResult.Success(existing.Id, existing.Version, idempotentReplay: true);
    }

    private static MaterialRequirementAssessment BuildRequirementAssessment(
        MaterialOperationContext context,
        MaterialRequirementProfile profile,
        MaterialRequirementCalculationResult calculation,
        string inputJson)
    {
        var actor = context.IdentitySubjectId.ToString();
        var fingerprint = MaterialRequirementIdempotencyFingerprint.ComputeAssessmentInputFingerprint(
            MaterialRequirementAssessmentType.Requirement,
            profile.MaterialId,
            profile.Id,
            MaterialRequirementConstants.RuleSetVersion,
            inputJson);

        return new MaterialRequirementAssessment
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            MaterialId = profile.MaterialId,
            RequirementProfileId = profile.Id,
            AssessmentType = MaterialRequirementAssessmentType.Requirement,
            Status = calculation.Status,
            RuleSetVersion = MaterialRequirementConstants.RuleSetVersion,
            InputFingerprint = fingerprint,
            StructuredDrivers = JsonSerializer.Serialize(calculation.MissingInputs, JsonOptions),
            StructuredEvidence = JsonSerializer.Serialize(calculation.Evidence, JsonOptions),
            TheoreticalRequirementGrams = calculation.TheoreticalRequirementGrams,
            VirginRequirementGrams = calculation.VirginRequirementGrams,
            RegrindRequirementGrams = calculation.RegrindRequirementGrams,
            EvaluatedAtUtc = DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBySubjectId = actor,
            Version = 1,
        };
    }

    private static MaterialRequirementAssessment BuildVarianceAssessment(
        MaterialOperationContext context,
        MaterialRequirementProfile profile,
        MaterialVarianceAssessmentResult variance,
        string inputJson)
    {
        var actor = context.IdentitySubjectId.ToString();
        var fingerprint = MaterialRequirementIdempotencyFingerprint.ComputeAssessmentInputFingerprint(
            MaterialRequirementAssessmentType.ConsumptionVariance,
            profile.MaterialId,
            profile.Id,
            MaterialRequirementConstants.RuleSetVersion,
            inputJson);

        return new MaterialRequirementAssessment
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            MaterialId = profile.MaterialId,
            RequirementProfileId = profile.Id,
            AssessmentType = MaterialRequirementAssessmentType.ConsumptionVariance,
            Status = variance.Status,
            RuleSetVersion = MaterialRequirementConstants.RuleSetVersion,
            InputFingerprint = fingerprint,
            StructuredDrivers = JsonSerializer.Serialize(variance.Drivers, JsonOptions),
            StructuredEvidence = JsonSerializer.Serialize(variance.Evidence, JsonOptions),
            TheoreticalRequirementGrams = variance.TheoreticalRequirementGrams,
            ObservedConsumptionGrams = variance.ObservedConsumptionGrams,
            VarianceGrams = variance.VarianceGrams,
            VariancePercent = variance.VariancePercent,
            EvaluatedAtUtc = DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBySubjectId = actor,
            Version = 1,
        };
    }

    private static MaterialRequirementAssessment BuildRegrindAssessment(
        MaterialOperationContext context,
        MaterialRequirementProfile profile,
        MaterialRegrindPolicyAssessmentResult regrind,
        string inputJson)
    {
        var actor = context.IdentitySubjectId.ToString();
        var fingerprint = MaterialRequirementIdempotencyFingerprint.ComputeAssessmentInputFingerprint(
            MaterialRequirementAssessmentType.RegrindPolicy,
            profile.MaterialId,
            profile.Id,
            MaterialRequirementConstants.RuleSetVersion,
            inputJson);

        return new MaterialRequirementAssessment
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            MaterialId = profile.MaterialId,
            RequirementProfileId = profile.Id,
            AssessmentType = MaterialRequirementAssessmentType.RegrindPolicy,
            Status = regrind.Status,
            RuleSetVersion = MaterialRequirementConstants.RuleSetVersion,
            InputFingerprint = fingerprint,
            StructuredDrivers = JsonSerializer.Serialize(regrind.Drivers, JsonOptions),
            StructuredEvidence = JsonSerializer.Serialize(regrind.Evidence, JsonOptions),
            EvaluatedAtUtc = DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBySubjectId = actor,
            Version = 1,
        };
    }

    private static MaterialRequirementAssessment BuildAvailabilityAssessment(
        MaterialOperationContext context,
        Guid materialId,
        Guid? profileId,
        MaterialAvailabilityRiskAssessmentResult availability,
        string inputJson)
    {
        var actor = context.IdentitySubjectId.ToString();
        var fingerprint = MaterialRequirementIdempotencyFingerprint.ComputeAssessmentInputFingerprint(
            MaterialRequirementAssessmentType.AvailabilityRisk,
            materialId,
            profileId,
            MaterialAvailabilityRiskConstants.RuleSetVersion,
            inputJson);

        return new MaterialRequirementAssessment
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            MaterialId = materialId,
            RequirementProfileId = profileId,
            AssessmentType = MaterialRequirementAssessmentType.AvailabilityRisk,
            Status = availability.Status,
            RuleSetVersion = MaterialAvailabilityRiskConstants.RuleSetVersion,
            InputFingerprint = fingerprint,
            StructuredDrivers = JsonSerializer.Serialize(availability.Drivers, JsonOptions),
            StructuredEvidence = JsonSerializer.Serialize(availability.Evidence, JsonOptions),
            TheoreticalRequirementGrams = availability.RequiredQuantityGrams,
            ErpAvailableQuantity = availability.ErpAvailableQuantity,
            ErpReservedQuantity = availability.ErpReservedQuantity,
            ErpUnit = availability.ErpUnit,
            AvailableAfterReservationQuantity = availability.AvailableAfterReservationQuantity,
            ShortageQuantity = availability.ShortageQuantity,
            ErpSourceUpdatedAtUtc = availability.ErpSourceUpdatedAtUtc,
            SourceFreshness = availability.SourceFreshness,
            EvaluatedAtUtc = DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBySubjectId = actor,
            FollowUpStatus = MaterialAvailabilityRiskFollowUpStatuses.RequiresReview,
            Version = 1,
        };
    }

    private static MaterialRequirementAssessmentReadModel? FindLatestAssessment(
        IReadOnlyList<MaterialRequirementAssessment> assessments,
        MaterialRequirementAssessmentType type)
    {
        var latest = assessments
            .Where(a => a.AssessmentType == type)
            .OrderByDescending(a => a.EvaluatedAtUtc)
            .ThenByDescending(a => a.CreatedAtUtc)
            .ThenByDescending(a => a.Id)
            .FirstOrDefault();

        return latest is null ? null : MapAssessment(latest);
    }

    private static MaterialRequirementProfileSummaryReadModel MapProfileSummary(MaterialRequirementProfile p) =>
        new(
            p.Id,
            p.MaterialId,
            p.Material?.Code ?? string.Empty,
            p.Material?.Name ?? string.Empty,
            MaterialRequirementValueConversions.ToStorage(p.RequirementType),
            p.ProductionOrderReference,
            p.ProductReference,
            p.MoldReference,
            p.GoodPartTargetQuantity,
            p.PlannedRegrindPercent,
            MaterialValueConversions.ToStorage(p.SourceType),
            p.SourceReference,
            p.DataRecordedAtUtc,
            p.UpdatedAtUtc,
            p.Version);

    private static MaterialRequirementProfileDetailReadModel MapProfileDetail(MaterialRequirementProfile p) =>
        new(
            p.Id,
            p.FacilityId,
            p.MaterialId,
            p.Material?.Code ?? string.Empty,
            p.Material?.Name ?? string.Empty,
            MaterialRequirementValueConversions.ToStorage(p.RequirementType),
            p.ProductionOrderReference,
            p.ProductReference,
            p.MoldReference,
            p.PlannedProductionQuantity,
            p.GoodPartTargetQuantity,
            p.PartWeightGrams,
            p.RunnerWeightGramsPerShot,
            p.EffectiveCavityCount,
            p.SetupAllowanceGrams,
            p.ProcessLossAllowancePercent,
            p.PlannedVirginPercent,
            p.PlannedRegrindPercent,
            MaterialValueConversions.ToStorage(p.SourceType),
            p.SourceReference,
            p.DataRecordedAtUtc,
            p.CreatedAtUtc,
            p.UpdatedAtUtc,
            p.Version);

    private static MaterialConsumptionObservationReadModel MapObservation(MaterialConsumptionObservation o) =>
        new(
            o.Id,
            o.MaterialId,
            o.Material?.Code ?? string.Empty,
            o.MaterialLotId,
            o.MaterialLot?.LotNumber,
            o.RequirementProfileId,
            MaterialRequirementValueConversions.ToStorage(o.ObservationType),
            o.Quantity,
            o.UnitOfMeasure,
            o.VirginQuantity,
            o.RegrindQuantity,
            o.GoodPartQuantity,
            o.ScrapQuantity,
            o.ProductionOrderReference,
            o.ProductReference,
            o.MoldReference,
            o.ObservedAtUtc,
            MaterialValueConversions.ToStorage(o.SourceType),
            o.SourceReference,
            o.DataRecordedAtUtc,
            o.Notes,
            o.CreatedAtUtc,
            o.Version);

    private static MaterialRequirementAssessmentReadModel MapAssessment(MaterialRequirementAssessment a)
    {
        IReadOnlyList<MaterialAssessmentDriverReadModel> drivers;
        IReadOnlyList<MaterialAssessmentEvidenceReadModel> evidence;

        try
        {
            drivers = JsonSerializer.Deserialize<List<MaterialAssessmentDriverReadModel>>(a.StructuredDrivers, JsonOptions) ?? [];
            evidence = JsonSerializer.Deserialize<List<MaterialAssessmentEvidenceReadModel>>(a.StructuredEvidence, JsonOptions) ?? [];
        }
        catch
        {
            drivers = [];
            evidence = [];
        }

        return new MaterialRequirementAssessmentReadModel(
            a.Id,
            a.MaterialId,
            a.RequirementProfileId,
            MaterialRequirementValueConversions.ToStorage(a.AssessmentType),
            a.Status,
            a.RuleSetVersion,
            a.InputFingerprint,
            drivers,
            evidence,
            a.TheoreticalRequirementGrams,
            a.ObservedConsumptionGrams,
            a.VarianceGrams,
            a.VariancePercent,
            a.VirginRequirementGrams,
            a.RegrindRequirementGrams,
            a.EvaluatedAtUtc,
            a.CreatedAtUtc,
            a.Version);
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class UnavailableMaterialRequirementRepository : IMaterialRequirementRepository
{
    private static MaterialRequirementWriteResult Unavailable() =>
        MaterialRequirementWriteResult.Fail(MaterialRequirementFailureCodes.SourceUnavailable, "Material requirement service is unavailable.");

    public Task<IReadOnlyList<MaterialRequirementProfileSummaryReadModel>> ListProfilesAsync(
        MaterialOperationContext context, MaterialRequirementProfileListFilters filters, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MaterialRequirementProfileSummaryReadModel>>([]);

    public Task<MaterialRequirementProfileDetailReadModel?> GetProfileByIdAsync(
        MaterialOperationContext context, Guid profileId, CancellationToken cancellationToken = default) =>
        Task.FromResult<MaterialRequirementProfileDetailReadModel?>(null);

    public Task<IReadOnlyList<MaterialConsumptionObservationReadModel>> ListObservationsAsync(
        MaterialOperationContext context, MaterialConsumptionObservationListFilters filters, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MaterialConsumptionObservationReadModel>>([]);

    public Task<MaterialConsumptionObservationReadModel?> GetObservationByIdAsync(
        MaterialOperationContext context, Guid observationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<MaterialConsumptionObservationReadModel?>(null);

    public Task<MaterialRequirementOverviewReadModel?> GetOverviewAsync(
        MaterialOperationContext context, Guid materialId, CancellationToken cancellationToken = default) =>
        Task.FromResult<MaterialRequirementOverviewReadModel?>(null);

    public Task<MaterialRequirementAssessmentReadModel?> GetAssessmentByIdAsync(
        MaterialOperationContext context, Guid assessmentId, CancellationToken cancellationToken = default) =>
        Task.FromResult<MaterialRequirementAssessmentReadModel?>(null);

    public Task<MaterialRequirementWriteResult> CreateProfileAsync(
        MaterialOperationContext context, CreateMaterialRequirementProfileCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaterialRequirementWriteResult> UpdateProfileAsync(
        MaterialOperationContext context, UpdateMaterialRequirementProfileCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaterialRequirementWriteResult> CreateObservationAsync(
        MaterialOperationContext context, CreateMaterialConsumptionObservationCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaterialRequirementWriteResult> EvaluateRequirementAsync(
        MaterialOperationContext context, EvaluateMaterialRequirementCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaterialRequirementWriteResult> EvaluateVarianceAsync(
        MaterialOperationContext context, EvaluateMaterialVarianceCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaterialRequirementWriteResult> EvaluateRegrindPolicyAsync(
        MaterialOperationContext context, EvaluateMaterialRegrindPolicyCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaterialRequirementWriteResult> EvaluateAvailabilityRiskAsync(
        MaterialOperationContext context, EvaluateMaterialAvailabilityRiskCommand command, bool hasErpReadAccess, CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}
