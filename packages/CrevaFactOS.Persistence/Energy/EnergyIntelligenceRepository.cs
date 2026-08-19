using System.Text.Json;
using CrevaFactOS.Persistence.Edge;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Energy;

public interface IEnergyIntelligenceRepository
{
    Task<EnergyIntelligenceWorkspaceReadModel> GetWorkspaceAsync(
        EnergyOperationContext context,
        CancellationToken cancellationToken = default);

    Task<EnergyBaselineListReadModel> ListBaselinesAsync(
        EnergyOperationContext context,
        EnergyBaselineListFilters filters,
        CancellationToken cancellationToken = default);

    Task<EnergyWriteResult> EvaluateBaselineAsync(
        EnergyOperationContext context,
        EvaluateEnergyBaselineCommand command,
        CancellationToken cancellationToken = default);

    Task<EnergyTargetListReadModel> ListTargetsAsync(
        EnergyOperationContext context,
        EnergyTargetListFilters filters,
        CancellationToken cancellationToken = default);

    Task<EnergyWriteResult> CreateTargetAsync(
        EnergyOperationContext context,
        CreateEnergyTargetCommand command,
        CancellationToken cancellationToken = default);

    Task<EnergyWriteResult> UpdateTargetAsync(
        EnergyOperationContext context,
        UpdateEnergyTargetCommand command,
        CancellationToken cancellationToken = default);

    Task<EnergyIntelligenceAssessmentListReadModel> ListAssessmentsAsync(
        EnergyOperationContext context,
        EnergyIntelligenceAssessmentListFilters filters,
        CancellationToken cancellationToken = default);

    Task<EnergyIntelligenceAssessmentDetailReadModel?> GetAssessmentByReferenceAsync(
        EnergyOperationContext context,
        string assessmentReference,
        CancellationToken cancellationToken = default);

    Task<EnergyWriteResult> EvaluateAssessmentAsync(
        EnergyOperationContext context,
        EvaluateEnergyIntelligenceAssessmentCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class EnergyIntelligenceRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IEnergyIntelligenceRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<EnergyIntelligenceWorkspaceReadModel> GetWorkspaceAsync(
        EnergyOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var baselineCount = await dbContext.EnergyBaselineProfiles.AsNoTracking()
                    .CountAsync(b => b.TenantId == context.TenantId && b.FacilityId == context.FacilityId, ct);

                var activeTargetCount = await dbContext.EnergyTargets.AsNoTracking()
                    .CountAsync(
                        t => t.TenantId == context.TenantId
                             && t.FacilityId == context.FacilityId
                             && t.Status == EnergyTargetStatuses.Approved,
                        ct);

                var recentAssessmentCount = await dbContext.EnergyIntelligenceAssessments.AsNoTracking()
                    .CountAsync(
                        a => a.TenantId == context.TenantId
                             && a.FacilityId == context.FacilityId
                             && a.EvaluatedAtUtc >= DateTimeOffset.UtcNow.AddDays(-30),
                        ct);

                var hasPilot = await dbContext.EnergyIntelligenceAssessments.AsNoTracking()
                    .AnyAsync(
                        a => a.TenantId == context.TenantId
                             && a.FacilityId == context.FacilityId
                             && a.RuleSetVersion == EnergyIntelligenceConstants.RuleSetVersion
                             && a.CreatedBy != string.Empty,
                        ct)
                    || await dbContext.EnergyBaselineProfiles.AsNoTracking()
                        .AnyAsync(
                            b => b.TenantId == context.TenantId
                                 && b.FacilityId == context.FacilityId
                                 && b.Provenance.Contains(EnergyIntelligenceConstants.DevelopmentPilotProvenance),
                            ct);

                return new EnergyIntelligenceWorkspaceReadModel(
                    baselineCount,
                    activeTargetCount,
                    recentAssessmentCount,
                    EnergyIntelligenceConstants.RuleSetVersion,
                    hasPilot ? EnergyIntelligenceConstants.DevelopmentPilotProvenance : string.Empty,
                    hasPilot);
            },
            cancellationToken);

    public Task<EnergyBaselineListReadModel> ListBaselinesAsync(
        EnergyOperationContext context,
        EnergyBaselineListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var page = filters.Page < 1 ? 1 : filters.Page;
                var pageSize = filters.PageSize is < 1 or > 100 ? 25 : filters.PageSize;

                var query = dbContext.EnergyBaselineProfiles.AsNoTracking()
                    .Where(b => b.TenantId == context.TenantId && b.FacilityId == context.FacilityId);

                if (!string.IsNullOrWhiteSpace(filters.ComparisonDimension))
                {
                    query = query.Where(b => b.ComparisonDimension == filters.ComparisonDimension.Trim());
                }

                if (!string.IsNullOrWhiteSpace(filters.MeasurementPointReference))
                {
                    var pointCode = filters.MeasurementPointReference.Trim();
                    var pointIds = await dbContext.EnergyMeasurementPoints.AsNoTracking()
                        .Where(p => p.TenantId == context.TenantId
                                    && p.FacilityId == context.FacilityId
                                    && p.PointCode == pointCode)
                        .Select(p => p.Id)
                        .ToListAsync(ct);
                    query = query.Where(b => b.MeasurementPointId.HasValue && pointIds.Contains(b.MeasurementPointId.Value));
                }

                var totalCount = await query.CountAsync(ct);
                var baselines = await query
                    .OrderBy(b => b.ComparisonDimension)
                    .ThenBy(b => b.BaselineReference)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                var pointLookup = await LoadPointLookupAsync(context, ct);
                var items = baselines.Select(b => MapBaseline(b, pointLookup)).ToList();
                return new EnergyBaselineListReadModel(items, totalCount, page, pageSize);
            },
            cancellationToken);

    public Task<EnergyWriteResult> EvaluateBaselineAsync(
        EnergyOperationContext context,
        EvaluateEnergyBaselineCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateBaselineCommand(command);
                if (validationError is not null)
                {
                    return EnergyWriteResult.Failure(EnergyFailureCodes.ValidationFailed, validationError);
                }

                var point = await FindPointByReferenceAsync(context, command.MeasurementPointReference.Trim(), ct);
                if (point is null)
                {
                    return EnergyWriteResult.Failure(
                        EnergyFailureCodes.MeasurementPointNotFound,
                        "Measurement point not found.");
                }

                var fingerprint = EnergyIntelligenceIdempotencyFingerprint.ComputeBaselineFingerprint(context, command);
                var baselineReference = BuildBaselineReference(command, point.PointCode);
                if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
                {
                    var existing = await dbContext.EnergyBaselineProfiles
                        .FirstOrDefaultAsync(
                            b => b.TenantId == context.TenantId
                                 && b.EvaluateIdempotencyKey == command.IdempotencyKey!.Trim(),
                            ct);

                    if (existing is not null)
                    {
                        return ResolveIdempotentReplay(existing.EvaluateIdempotencyPayloadFingerprint, fingerprint, existing.Id);
                    }
                }

                var existingReference = await dbContext.EnergyBaselineProfiles
                    .FirstOrDefaultAsync(
                        b => b.TenantId == context.TenantId
                             && b.FacilityId == context.FacilityId
                             && b.BaselineReference == baselineReference,
                        ct);

                if (existingReference is not null && string.IsNullOrWhiteSpace(command.IdempotencyKey))
                {
                    return EnergyWriteResult.Failure(
                        EnergyFailureCodes.IdempotencyConflict,
                        "Baseline reference already exists for this scope.");
                }

                var samples = await LoadComparableWindowSamplesAsync(
                    context,
                    point,
                    command.ComparisonDimension.Trim(),
                    command.ReportingWindowDurationSeconds,
                    TrimOrNull(command.ShiftReference),
                    TrimOrNull(command.ProductionContextReference),
                    ct);

                var allowSimulated = point.SourceProvenance.Contains("Development", StringComparison.Ordinal);
                var tolerance = command.RelativeTolerance ?? EnergyIntelligenceConstants.DefaultRelativeTolerance;
                var calculation = EnergyIntelligenceCalculator.CalculateBaseline(samples, tolerance, allowSimulated);

                if (!calculation.Succeeded)
                {
                    return EnergyWriteResult.Failure(
                        EnergyFailureCodes.ValidationFailed,
                        calculation.FailureReason ?? "Baseline calculation failed.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var profile = new EnergyBaselineProfile
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    BaselineReference = baselineReference,
                    ComparisonDimension = command.ComparisonDimension.Trim(),
                    MeasurementPointId = point.Id,
                    MachineAssetId = point.MachineAssetId,
                    ShiftReference = TrimOrNull(command.ShiftReference),
                    ProductionContextReference = TrimOrNull(command.ProductionContextReference),
                    ReportingWindowDurationSeconds = command.ReportingWindowDurationSeconds,
                    SampleCount = calculation.SampleCount,
                    BaselineConsumedKwh = calculation.BaselineConsumedKwh,
                    BaselineMaximumDemandKw = calculation.BaselineMaximumDemandKw,
                    BaselineKwhPerKg = calculation.BaselineKwhPerKg,
                    BaselineKwhPer1000Parts = calculation.BaselineKwhPer1000Parts,
                    BaselineKwhPer1000GoodParts = calculation.BaselineKwhPer1000GoodParts,
                    MedianCenter = calculation.MedianCenter,
                    MadDispersion = calculation.MadDispersion,
                    NormalizedDispersion = calculation.NormalizedDispersion,
                    RelativeTolerance = calculation.RelativeTolerance,
                    UpperThresholdConsumedKwh = calculation.UpperThresholdConsumedKwh,
                    UpperThresholdDemandKw = calculation.UpperThresholdDemandKw,
                    CalculationMethod = calculation.CalculationMethod,
                    EvidenceStatus = calculation.EvidenceStatus,
                    SourceWindowReferencesJson = JsonSerializer.Serialize(calculation.SourceWindowReferences, JsonOptions),
                    ValidFromUtc = now,
                    ValidToUtc = now.AddYears(1),
                    RuleSetVersion = EnergyIntelligenceConstants.RuleSetVersion,
                    Provenance = allowSimulated
                        ? EnergyIntelligenceConstants.DevelopmentPilotProvenance
                        : "calculated-baseline",
                    EvaluateIdempotencyKey = TrimOrNull(command.IdempotencyKey),
                    EvaluateIdempotencyPayloadFingerprint = fingerprint,
                    Version = 1,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedBy = actor,
                    UpdatedBy = actor,
                };

                dbContext.EnergyBaselineProfiles.Add(profile);
                AddAuditEvent(
                    context,
                    profile.Id,
                    "energy-baseline-profile",
                    EnergyIntelligenceAuditEventTypes.BaselineEvaluated,
                    actor,
                    now,
                    new { baselineReference = profile.BaselineReference, sampleCount = profile.SampleCount });

                await dbContext.SaveChangesAsync(ct);
                return EnergyWriteResult.Success(profile.Id);
            },
            cancellationToken);

    public Task<EnergyTargetListReadModel> ListTargetsAsync(
        EnergyOperationContext context,
        EnergyTargetListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var page = filters.Page < 1 ? 1 : filters.Page;
                var pageSize = filters.PageSize is < 1 or > 100 ? 25 : filters.PageSize;

                var query = dbContext.EnergyTargets.AsNoTracking()
                    .Where(t => t.TenantId == context.TenantId && t.FacilityId == context.FacilityId);

                if (!string.IsNullOrWhiteSpace(filters.TargetScope))
                {
                    query = query.Where(t => t.TargetScope == filters.TargetScope.Trim());
                }

                if (!string.IsNullOrWhiteSpace(filters.TargetMetric))
                {
                    query = query.Where(t => t.TargetMetric == filters.TargetMetric.Trim());
                }

                if (!string.IsNullOrWhiteSpace(filters.Status))
                {
                    query = query.Where(t => t.Status == filters.Status.Trim());
                }

                var totalCount = await query.CountAsync(ct);
                var targets = await query
                    .OrderBy(t => t.TargetScope)
                    .ThenBy(t => t.TargetReference)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                var items = targets.Select(MapTarget).ToList();
                return new EnergyTargetListReadModel(items, totalCount, page, pageSize);
            },
            cancellationToken);

    public Task<EnergyWriteResult> CreateTargetAsync(
        EnergyOperationContext context,
        CreateEnergyTargetCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateCreateTarget(command);
                if (validationError is not null)
                {
                    return EnergyWriteResult.Failure(EnergyFailureCodes.ValidationFailed, validationError);
                }

                var fingerprint = EnergyIntelligenceIdempotencyFingerprint.ComputeCreateTargetFingerprint(context, command);
                if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
                {
                    var existing = await dbContext.EnergyTargets
                        .FirstOrDefaultAsync(
                            t => t.TenantId == context.TenantId
                                 && t.CreateIdempotencyKey == command.IdempotencyKey!.Trim(),
                            ct);

                    if (existing is not null)
                    {
                        return ResolveIdempotentReplay(existing.CreateIdempotencyPayloadFingerprint, fingerprint, existing.Id, existing.Version);
                    }
                }

                var reference = command.TargetReference.Trim();
                var duplicate = await dbContext.EnergyTargets
                    .AnyAsync(
                        t => t.TenantId == context.TenantId
                             && t.FacilityId == context.FacilityId
                             && t.TargetReference == reference,
                        ct);

                if (duplicate)
                {
                    return EnergyWriteResult.Failure(
                        EnergyFailureCodes.IdempotencyConflict,
                        "Target reference already exists.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var target = new EnergyTarget
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    TargetReference = reference,
                    TargetScope = command.TargetScope.Trim(),
                    ScopeReference = command.ScopeReference.Trim(),
                    TargetMetric = command.TargetMetric.Trim(),
                    TargetValue = command.TargetValue,
                    Unit = command.Unit.Trim(),
                    Tolerance = command.Tolerance,
                    EffectiveFromUtc = command.EffectiveFromUtc,
                    EffectiveToUtc = command.EffectiveToUtc,
                    SourceProvenance = command.SourceProvenance.Trim(),
                    Status = EnergyTargetStatuses.Approved,
                    CreateIdempotencyKey = TrimOrNull(command.IdempotencyKey),
                    CreateIdempotencyPayloadFingerprint = fingerprint,
                    Version = 1,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedBy = actor,
                    UpdatedBy = actor,
                };

                dbContext.EnergyTargets.Add(target);
                AddAuditEvent(
                    context,
                    target.Id,
                    "energy-target",
                    EnergyIntelligenceAuditEventTypes.TargetCreated,
                    actor,
                    now,
                    new { targetReference = target.TargetReference, metric = target.TargetMetric });

                await dbContext.SaveChangesAsync(ct);
                return EnergyWriteResult.Success(target.Id);
            },
            cancellationToken);

    public Task<EnergyWriteResult> UpdateTargetAsync(
        EnergyOperationContext context,
        UpdateEnergyTargetCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var reference = command.TargetReference.Trim();
                var target = await dbContext.EnergyTargets
                    .FirstOrDefaultAsync(
                        t => t.TenantId == context.TenantId
                             && t.FacilityId == context.FacilityId
                             && t.TargetReference == reference,
                        ct);

                if (target is null)
                {
                    return EnergyWriteResult.Failure(
                        EnergyIntelligenceFailureCodes.TargetNotFound,
                        "Energy target not found.");
                }

                if (target.Version != command.ExpectedVersion)
                {
                    return EnergyWriteResult.Failure(
                        EnergyFailureCodes.ConcurrencyConflict,
                        "Target version is stale.");
                }

                if (command.EffectiveToUtc <= command.EffectiveFromUtc)
                {
                    return EnergyWriteResult.Failure(
                        EnergyFailureCodes.ValidationFailed,
                        "EffectiveToUtc must be after EffectiveFromUtc.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                target.TargetValue = command.TargetValue;
                target.Tolerance = command.Tolerance;
                target.EffectiveFromUtc = command.EffectiveFromUtc;
                target.EffectiveToUtc = command.EffectiveToUtc;
                target.SourceProvenance = command.SourceProvenance.Trim();
                target.Status = command.Status.Trim();
                target.UpdatedAtUtc = now;
                target.UpdatedBy = actor;
                target.Version += 1;

                AddAuditEvent(
                    context,
                    target.Id,
                    "energy-target",
                    EnergyIntelligenceAuditEventTypes.TargetUpdated,
                    actor,
                    now,
                    new { targetReference = target.TargetReference, version = target.Version });

                await dbContext.SaveChangesAsync(ct);
                return EnergyWriteResult.Success(target.Id, target.Version);
            },
            cancellationToken);

    public Task<EnergyIntelligenceAssessmentListReadModel> ListAssessmentsAsync(
        EnergyOperationContext context,
        EnergyIntelligenceAssessmentListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var page = filters.Page < 1 ? 1 : filters.Page;
                var pageSize = filters.PageSize is < 1 or > 100 ? 25 : filters.PageSize;

                var query = dbContext.EnergyIntelligenceAssessments.AsNoTracking()
                    .Where(a => a.TenantId == context.TenantId && a.FacilityId == context.FacilityId);

                if (!string.IsNullOrWhiteSpace(filters.ComparisonDimension))
                {
                    query = query.Where(a => a.ComparisonDimension == filters.ComparisonDimension.Trim());
                }

                if (!string.IsNullOrWhiteSpace(filters.Outcome))
                {
                    query = query.Where(a => a.Outcome == filters.Outcome.Trim());
                }

                if (!string.IsNullOrWhiteSpace(filters.MeasurementPointReference))
                {
                    var pointCode = filters.MeasurementPointReference.Trim();
                    var pointIds = await dbContext.EnergyMeasurementPoints.AsNoTracking()
                        .Where(p => p.TenantId == context.TenantId
                                    && p.FacilityId == context.FacilityId
                                    && p.PointCode == pointCode)
                        .Select(p => p.Id)
                        .ToListAsync(ct);
                    query = query.Where(a => a.MeasurementPointId.HasValue && pointIds.Contains(a.MeasurementPointId.Value));
                }

                var totalCount = await query.CountAsync(ct);
                var assessments = await query
                    .OrderByDescending(a => a.EvaluatedAtUtc)
                    .ThenBy(a => a.AssessmentReference)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                var pointLookup = await LoadPointLookupAsync(context, ct);
                var items = assessments.Select(a => MapAssessmentSummary(a, pointLookup)).ToList();
                return new EnergyIntelligenceAssessmentListReadModel(items, totalCount, page, pageSize);
            },
            cancellationToken);

    public Task<EnergyIntelligenceAssessmentDetailReadModel?> GetAssessmentByReferenceAsync(
        EnergyOperationContext context,
        string assessmentReference,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var reference = assessmentReference.Trim();
                var assessment = await dbContext.EnergyIntelligenceAssessments.AsNoTracking()
                    .Include(a => a.Drivers)
                    .FirstOrDefaultAsync(
                        a => a.TenantId == context.TenantId
                             && a.FacilityId == context.FacilityId
                             && a.AssessmentReference == reference,
                        ct);

                if (assessment is null)
                {
                    return null;
                }

                var pointLookup = await LoadPointLookupAsync(context, ct);
                var baselineRef = assessment.BaselineProfileId.HasValue
                    ? await dbContext.EnergyBaselineProfiles.AsNoTracking()
                        .Where(b => b.Id == assessment.BaselineProfileId.Value)
                        .Select(b => b.BaselineReference)
                        .FirstOrDefaultAsync(ct)
                    : null;

                var targetRef = assessment.TargetId.HasValue
                    ? await dbContext.EnergyTargets.AsNoTracking()
                        .Where(t => t.Id == assessment.TargetId.Value)
                        .Select(t => t.TargetReference)
                        .FirstOrDefaultAsync(ct)
                    : null;

                var summary = MapAssessmentSummary(assessment, pointLookup);
                var drivers = assessment.Drivers
                    .OrderByDescending(d => d.IsLeadingDriver)
                    .ThenBy(d => d.ReasonCode)
                    .Select(MapDriver)
                    .ToList();

                return new EnergyIntelligenceAssessmentDetailReadModel(summary, baselineRef, targetRef, drivers);
            },
            cancellationToken);

    public Task<EnergyWriteResult> EvaluateAssessmentAsync(
        EnergyOperationContext context,
        EvaluateEnergyIntelligenceAssessmentCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateAssessmentCommand(command);
                if (validationError is not null)
                {
                    return EnergyWriteResult.Failure(EnergyFailureCodes.ValidationFailed, validationError);
                }

                var point = await FindPointByReferenceAsync(context, command.MeasurementPointReference.Trim(), ct);
                if (point is null)
                {
                    return EnergyWriteResult.Failure(
                        EnergyFailureCodes.MeasurementPointNotFound,
                        "Measurement point not found.");
                }

                var fingerprint = EnergyIntelligenceIdempotencyFingerprint.ComputeAssessmentFingerprint(context, command);
                if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
                {
                    var existing = await dbContext.EnergyIntelligenceAssessments
                        .Include(a => a.Drivers)
                        .FirstOrDefaultAsync(
                            a => a.TenantId == context.TenantId
                                 && a.EvaluateIdempotencyKey == command.IdempotencyKey!.Trim(),
                            ct);

                    if (existing is not null)
                    {
                        return ResolveIdempotentReplay(existing.EvaluateIdempotencyPayloadFingerprint, fingerprint, existing.Id);
                    }
                }

                var window = await dbContext.EnergyConsumptionWindows.AsNoTracking()
                    .FirstOrDefaultAsync(
                        w => w.TenantId == context.TenantId
                             && w.FacilityId == context.FacilityId
                             && w.MeasurementPointId == point.Id
                             && w.WindowStartUtc == command.EvaluationWindowStartUtc
                             && w.WindowEndUtc == command.EvaluationWindowEndUtc,
                        ct);

                if (window is null)
                {
                    return EnergyWriteResult.Failure(
                        EnergyFailureCodes.NotFound,
                        "Consumption window not found for evaluation boundaries.");
                }

                var durationSeconds = (int)(command.EvaluationWindowEndUtc - command.EvaluationWindowStartUtc).TotalSeconds;
                var comparisonKey = BuildComparisonKey(
                    command.ComparisonDimension.Trim(),
                    point.Id,
                    point.MachineAssetId,
                    null,
                    TrimOrNull(command.ProductionContextReference),
                    durationSeconds);

                var actualSample = new EnergyComparableWindowSample(
                    window.Id.ToString(),
                    comparisonKey,
                    durationSeconds,
                    window.WindowStartUtc,
                    window.WindowEndUtc,
                    window.ConsumedKwh,
                    window.MaximumDemandKw,
                    window.EvidenceStatus,
                    window.FreshnessState,
                    TrimOrNull(command.ProductionContextReference));

                var baseline = await ResolveBaselineAsync(
                    context,
                    TrimOrNull(command.BaselineReference),
                    command.ComparisonDimension.Trim(),
                    point.Id,
                    durationSeconds,
                    ct);

                var consumptionTarget = await ResolveTargetAsync(context, TrimOrNull(command.ConsumptionTargetReference), ct);
                var demandTarget = await ResolveTargetAsync(context, TrimOrNull(command.DemandTargetReference), ct);
                var idleTarget = await ResolveTargetAsync(context, TrimOrNull(command.IdleTargetReference), ct);
                var intensityTarget = await ResolveTargetAsync(context, TrimOrNull(command.IntensityTargetReference), ct);

                EnergyTariffSnapshot? tariff = null;
                if (window.TariffSnapshotId.HasValue)
                {
                    tariff = await dbContext.EnergyTariffSnapshots.AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == window.TariffSnapshotId.Value, ct);
                }

                var productionState = await LoadProductionStateEvidenceAsync(
                    context,
                    point.MachineAssetId,
                    window.WindowStartUtc,
                    window.WindowEndUtc,
                    TrimOrNull(command.ProductionContextReference),
                    ct);

                var allowSimulated = point.SourceProvenance.Contains("Development", StringComparison.Ordinal);
                var evaluation = EnergyIntelligenceCalculator.EvaluateAssessment(
                    actualSample,
                    baseline,
                    consumptionTarget,
                    demandTarget,
                    idleTarget,
                    intensityTarget,
                    productionState,
                    tariff,
                    allowSimulated);

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var assessmentReference = command.AssessmentReference.Trim();

                var assessment = new EnergyIntelligenceAssessment
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    AssessmentReference = assessmentReference,
                    ComparisonDimension = command.ComparisonDimension.Trim(),
                    MeasurementPointId = point.Id,
                    ConsumptionWindowId = window.Id,
                    BaselineProfileId = baseline?.Id,
                    TargetId = consumptionTarget?.Id,
                    EvaluationWindowStartUtc = command.EvaluationWindowStartUtc,
                    EvaluationWindowEndUtc = command.EvaluationWindowEndUtc,
                    ActualConsumedKwh = evaluation.ActualConsumedKwh,
                    ActualMaximumDemandKw = evaluation.ActualMaximumDemandKw,
                    ActualKwhPer1000GoodParts = productionState?.GoodParts is > 0
                        ? EnergyCalculator.RoundEnergy(evaluation.ActualConsumedKwh / productionState!.GoodParts!.Value * 1000m)
                        : null,
                    VarianceConsumedKwh = evaluation.VarianceConsumedKwh,
                    VariancePercent = evaluation.VariancePercent,
                    PotentialReducibleKwh = evaluation.PotentialReducibleKwh,
                    EstimatedCostImpact = evaluation.EstimatedCostImpact,
                    CostCurrencyCode = evaluation.CostCurrencyCode,
                    OpportunityLabel = evaluation.OpportunityLabel,
                    Outcome = evaluation.Outcome,
                    RuleSetVersion = EnergyIntelligenceConstants.RuleSetVersion,
                    CalculationMethod = "deterministic-intelligence",
                    EvidenceStatus = evaluation.EvidenceStatus,
                    FreshnessState = evaluation.FreshnessState,
                    EvaluateIdempotencyKey = TrimOrNull(command.IdempotencyKey),
                    EvaluateIdempotencyPayloadFingerprint = fingerprint,
                    Version = 1,
                    EvaluatedAtUtc = now,
                    CreatedAtUtc = now,
                    CreatedBy = actor,
                };

                var leadingDraft = EnergyIntelligenceCalculator.SelectLeadingDriver(
                    evaluation.Drivers,
                    evaluation.Outcome);

                foreach (var driverDraft in evaluation.Drivers)
                {
                    var driverId = Guid.NewGuid();
                    var isLeading = leadingDraft is not null
                                    && driverDraft.ReasonCode == leadingDraft.ReasonCode
                                    && driverDraft.Severity == leadingDraft.Severity;

                    var driver = new EnergyIntelligenceDriver
                    {
                        Id = driverId,
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        AssessmentId = assessment.Id,
                        ReasonCode = driverDraft.ReasonCode,
                        LabelTr = driverDraft.LabelTr,
                        Severity = driverDraft.Severity,
                        Category = driverDraft.Category,
                        ActualValue = driverDraft.ActualValue,
                        ActualUnit = driverDraft.ActualUnit,
                        ComparisonValue = driverDraft.ComparisonValue,
                        ComparisonUnit = driverDraft.ComparisonUnit,
                        NormalizedVariance = driverDraft.NormalizedVariance,
                        StructuredEvidenceJson = JsonSerializer.Serialize(new { source = driverDraft.EvidenceSource }, JsonOptions),
                        EvidenceSource = driverDraft.EvidenceSource,
                        FreshnessState = evaluation.FreshnessState,
                        Explanation = driverDraft.Explanation,
                        RuleSetVersion = EnergyIntelligenceConstants.RuleSetVersion,
                        IsLeadingDriver = isLeading,
                    };

                    if (isLeading)
                    {
                        assessment.LeadingDriverCode = driver.ReasonCode;
                        assessment.LeadingDriverId = driverId;
                    }

                    assessment.Drivers.Add(driver);
                }

                dbContext.EnergyIntelligenceAssessments.Add(assessment);
                AddAuditEvent(
                    context,
                    assessment.Id,
                    "energy-intelligence-assessment",
                    EnergyIntelligenceAuditEventTypes.AssessmentEvaluated,
                    actor,
                    now,
                    new { assessmentReference, outcome = assessment.Outcome });

                await dbContext.SaveChangesAsync(ct);
                return EnergyWriteResult.Success(assessment.Id);
            },
            cancellationToken);

    private async Task<List<EnergyComparableWindowSample>> LoadComparableWindowSamplesAsync(
        EnergyOperationContext context,
        EnergyMeasurementPoint point,
        string comparisonDimension,
        int durationSeconds,
        string? shiftReference,
        string? productionContextReference,
        CancellationToken cancellationToken)
    {
        var windows = await dbContext.EnergyConsumptionWindows.AsNoTracking()
            .Where(w => w.TenantId == context.TenantId
                        && w.FacilityId == context.FacilityId
                        && w.MeasurementPointId == point.Id)
            .ToListAsync(cancellationToken);

        var samples = new List<EnergyComparableWindowSample>();
        foreach (var window in windows)
        {
            var windowDuration = (int)(window.WindowEndUtc - window.WindowStartUtc).TotalSeconds;
            if (windowDuration != durationSeconds)
            {
                continue;
            }

            var key = BuildComparisonKey(
                comparisonDimension,
                point.Id,
                point.MachineAssetId,
                shiftReference,
                productionContextReference,
                durationSeconds);

            samples.Add(new EnergyComparableWindowSample(
                window.Id.ToString(),
                key,
                windowDuration,
                window.WindowStartUtc,
                window.WindowEndUtc,
                window.ConsumedKwh,
                window.MaximumDemandKw,
                window.EvidenceStatus,
                window.FreshnessState,
                productionContextReference));
        }

        return samples
            .OrderBy(s => s.WindowStartUtc)
            .ThenBy(s => s.WindowReference)
            .ToList();
    }

    private async Task<EnergyBaselineProfile?> ResolveBaselineAsync(
        EnergyOperationContext context,
        string? baselineReference,
        string comparisonDimension,
        Guid measurementPointId,
        int durationSeconds,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(baselineReference))
        {
            return await dbContext.EnergyBaselineProfiles.AsNoTracking()
                .FirstOrDefaultAsync(
                    b => b.TenantId == context.TenantId
                         && b.FacilityId == context.FacilityId
                         && b.BaselineReference == baselineReference,
                    cancellationToken);
        }

        return await dbContext.EnergyBaselineProfiles.AsNoTracking()
            .Where(b => b.TenantId == context.TenantId
                        && b.FacilityId == context.FacilityId
                        && b.ComparisonDimension == comparisonDimension
                        && b.MeasurementPointId == measurementPointId
                        && b.ReportingWindowDurationSeconds == durationSeconds)
            .OrderByDescending(b => b.ValidFromUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<EnergyTarget?> ResolveTargetAsync(
        EnergyOperationContext context,
        string? targetReference,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetReference))
        {
            return null;
        }

        return await dbContext.EnergyTargets.AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.TenantId == context.TenantId
                     && t.FacilityId == context.FacilityId
                     && t.TargetReference == targetReference,
                cancellationToken);
    }

    private async Task<EnergyProductionStateEvidence?> LoadProductionStateEvidenceAsync(
        EnergyOperationContext context,
        Guid? machineAssetId,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        string? contextReference,
        CancellationToken cancellationToken)
    {
        if (machineAssetId is null)
        {
            return new EnergyProductionStateEvidence(
                windowStartUtc,
                windowEndUtc,
                false,
                0,
                0,
                contextReference);
        }

        var cycles = await dbContext.MachineObservedCycles.AsNoTracking()
            .Where(c => c.TenantId == context.TenantId
                        && c.FacilityId == context.FacilityId
                        && c.MachineId == machineAssetId.Value
                        && c.SourceTimestampUtc >= windowStartUtc
                        && c.SourceTimestampUtc <= windowEndUtc
                        && !c.IsDuplicate)
            .ToListAsync(cancellationToken);

        if (cycles.Count == 0)
        {
            return new EnergyProductionStateEvidence(
                windowStartUtc,
                windowEndUtc,
                false,
                0,
                0,
                contextReference);
        }

        var goodParts = cycles.Count(c =>
            string.Equals(c.Quality, TelemetryQualities.Good, StringComparison.OrdinalIgnoreCase));

        return new EnergyProductionStateEvidence(
            windowStartUtc,
            windowEndUtc,
            true,
            cycles.Count,
            goodParts,
            contextReference);
    }

    private static string BuildComparisonKey(
        string comparisonDimension,
        Guid measurementPointId,
        Guid? machineAssetId,
        string? shiftReference,
        string? productionContextReference,
        int durationSeconds) =>
        $"{comparisonDimension}|{measurementPointId:N}|{machineAssetId?.ToString("N") ?? "-"}|{shiftReference ?? "-"}|{productionContextReference ?? "-"}|{durationSeconds}";

    private static string BuildBaselineReference(EvaluateEnergyBaselineCommand command, string pointCode)
    {
        var shift = command.ShiftReference?.Trim() ?? "none";
        var context = command.ProductionContextReference?.Trim() ?? "none";
        return $"BL-{command.ComparisonDimension.Trim()}-{pointCode}-{command.ReportingWindowDurationSeconds}-{shift}-{context}";
    }

    private async Task<EnergyMeasurementPoint?> FindPointByReferenceAsync(
        EnergyOperationContext context,
        string reference,
        CancellationToken cancellationToken) =>
        await dbContext.EnergyMeasurementPoints
            .FirstOrDefaultAsync(
                p => p.TenantId == context.TenantId
                     && p.FacilityId == context.FacilityId
                     && p.PointCode == reference,
                cancellationToken);

    private async Task<Dictionary<Guid, string>> LoadPointLookupAsync(
        EnergyOperationContext context,
        CancellationToken cancellationToken)
    {
        var points = await dbContext.EnergyMeasurementPoints.AsNoTracking()
            .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId)
            .Select(p => new { p.Id, p.PointCode })
            .ToListAsync(cancellationToken);

        return points.ToDictionary(p => p.Id, p => p.PointCode);
    }

    private static EnergyWriteResult ResolveIdempotentReplay(string? storedFingerprint, string fingerprint, Guid entityId, uint? version = null)
    {
        if (string.IsNullOrEmpty(storedFingerprint))
        {
            return EnergyWriteResult.Failure(
                EnergyFailureCodes.IdempotencyLegacyUnverifiable,
                "Legacy record cannot be verified for idempotent replay.");
        }

        if (!string.Equals(storedFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return EnergyWriteResult.Failure(
                EnergyFailureCodes.IdempotencyConflict,
                "Idempotency key was already used with a different payload.");
        }

        return EnergyWriteResult.Success(entityId, version, idempotentReplay: true);
    }

    private void AddAuditEvent(
        EnergyOperationContext context,
        Guid entityId,
        string entityType,
        string eventType,
        string actor,
        DateTimeOffset occurredAtUtc,
        object payload)
    {
        dbContext.EnergyAuditEvents.Add(new EnergyAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            ActorSubjectId = actor,
            OccurredAtUtc = occurredAtUtc,
            CorrelationId = context.CorrelationId,
        });
    }

    private static EnergyBaselineProfileReadModel MapBaseline(
        EnergyBaselineProfile baseline,
        Dictionary<Guid, string> pointLookup) =>
        new(
            baseline.Id,
            baseline.BaselineReference,
            baseline.ComparisonDimension,
            baseline.MeasurementPointId.HasValue
                ? pointLookup.GetValueOrDefault(baseline.MeasurementPointId.Value)
                : null,
            baseline.MachineAssetId,
            baseline.ShiftReference,
            baseline.ProductionContextReference,
            baseline.ReportingWindowDurationSeconds,
            baseline.SampleCount,
            baseline.BaselineConsumedKwh,
            baseline.BaselineMaximumDemandKw,
            baseline.MedianCenter,
            baseline.MadDispersion,
            baseline.NormalizedDispersion,
            baseline.RelativeTolerance,
            baseline.UpperThresholdConsumedKwh,
            baseline.UpperThresholdDemandKw,
            baseline.CalculationMethod,
            baseline.EvidenceStatus,
            baseline.RuleSetVersion,
            baseline.Provenance,
            baseline.Version,
            baseline.Provenance.Contains(EnergyIntelligenceConstants.DevelopmentPilotProvenance, StringComparison.Ordinal));

    private static EnergyTargetReadModel MapTarget(EnergyTarget target) =>
        new(
            target.Id,
            target.TargetReference,
            target.TargetScope,
            target.ScopeReference,
            target.TargetMetric,
            target.TargetValue,
            target.Unit,
            target.Tolerance,
            target.EffectiveFromUtc,
            target.EffectiveToUtc,
            target.SourceProvenance,
            target.Status,
            target.Version,
            target.SourceProvenance.Contains("Development", StringComparison.Ordinal));

    private static EnergyIntelligenceAssessmentSummaryReadModel MapAssessmentSummary(
        EnergyIntelligenceAssessment assessment,
        Dictionary<Guid, string> pointLookup) =>
        new(
            assessment.Id,
            assessment.AssessmentReference,
            assessment.ComparisonDimension,
            assessment.MeasurementPointId.HasValue
                ? pointLookup.GetValueOrDefault(assessment.MeasurementPointId.Value)
                : null,
            assessment.EvaluationWindowStartUtc,
            assessment.EvaluationWindowEndUtc,
            assessment.ActualConsumedKwh,
            assessment.ActualMaximumDemandKw,
            assessment.VarianceConsumedKwh,
            assessment.VariancePercent,
            assessment.Outcome,
            assessment.LeadingDriverCode,
            assessment.PotentialReducibleKwh,
            assessment.EstimatedCostImpact,
            assessment.CostCurrencyCode,
            assessment.OpportunityLabel,
            assessment.EvidenceStatus,
            assessment.EvaluatedAtUtc,
            assessment.RuleSetVersion.Contains("ENERGY-2", StringComparison.Ordinal));

    private static EnergyIntelligenceDriverReadModel MapDriver(EnergyIntelligenceDriver driver) =>
        new(
            driver.Id,
            driver.ReasonCode,
            driver.LabelTr,
            driver.Severity,
            driver.Category,
            driver.ActualValue,
            driver.ActualUnit,
            driver.ComparisonValue,
            driver.ComparisonUnit,
            driver.NormalizedVariance,
            driver.Explanation,
            driver.EvidenceSource,
            driver.FreshnessState,
            driver.IsLeadingDriver);

    private static string? ValidateBaselineCommand(EvaluateEnergyBaselineCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ComparisonDimension))
        {
            return "ComparisonDimension is required.";
        }

        if (string.IsNullOrWhiteSpace(command.MeasurementPointReference))
        {
            return "MeasurementPointReference is required.";
        }

        if (command.ReportingWindowDurationSeconds <= 0)
        {
            return "ReportingWindowDurationSeconds must be positive.";
        }

        return null;
    }

    private static string? ValidateCreateTarget(CreateEnergyTargetCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.TargetReference))
        {
            return "TargetReference is required.";
        }

        if (string.IsNullOrWhiteSpace(command.TargetScope))
        {
            return "TargetScope is required.";
        }

        if (string.IsNullOrWhiteSpace(command.ScopeReference))
        {
            return "ScopeReference is required.";
        }

        if (string.IsNullOrWhiteSpace(command.TargetMetric))
        {
            return "TargetMetric is required.";
        }

        if (string.IsNullOrWhiteSpace(command.Unit))
        {
            return "Unit is required.";
        }

        if (command.EffectiveToUtc <= command.EffectiveFromUtc)
        {
            return "EffectiveToUtc must be after EffectiveFromUtc.";
        }

        return null;
    }

    private static string? ValidateAssessmentCommand(EvaluateEnergyIntelligenceAssessmentCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.AssessmentReference))
        {
            return "AssessmentReference is required.";
        }

        if (string.IsNullOrWhiteSpace(command.ComparisonDimension))
        {
            return "ComparisonDimension is required.";
        }

        if (string.IsNullOrWhiteSpace(command.MeasurementPointReference))
        {
            return "MeasurementPointReference is required.";
        }

        if (command.EvaluationWindowEndUtc <= command.EvaluationWindowStartUtc)
        {
            return "EvaluationWindowEndUtc must be after EvaluationWindowStartUtc.";
        }

        return null;
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class UnavailableEnergyIntelligenceRepository : IEnergyIntelligenceRepository
{
    public Task<EnergyIntelligenceWorkspaceReadModel> GetWorkspaceAsync(
        EnergyOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new EnergyIntelligenceWorkspaceReadModel(0, 0, 0, EnergyIntelligenceConstants.RuleSetVersion, string.Empty, false));

    public Task<EnergyBaselineListReadModel> ListBaselinesAsync(
        EnergyOperationContext context,
        EnergyBaselineListFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new EnergyBaselineListReadModel([], 0, 1, 25));

    public Task<EnergyWriteResult> EvaluateBaselineAsync(
        EnergyOperationContext context,
        EvaluateEnergyBaselineCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(EnergyWriteResult.Failure(EnergyFailureCodes.ValidationFailed, "Persistence unavailable."));

    public Task<EnergyTargetListReadModel> ListTargetsAsync(
        EnergyOperationContext context,
        EnergyTargetListFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new EnergyTargetListReadModel([], 0, 1, 25));

    public Task<EnergyWriteResult> CreateTargetAsync(
        EnergyOperationContext context,
        CreateEnergyTargetCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(EnergyWriteResult.Failure(EnergyFailureCodes.ValidationFailed, "Persistence unavailable."));

    public Task<EnergyWriteResult> UpdateTargetAsync(
        EnergyOperationContext context,
        UpdateEnergyTargetCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(EnergyWriteResult.Failure(EnergyFailureCodes.ValidationFailed, "Persistence unavailable."));

    public Task<EnergyIntelligenceAssessmentListReadModel> ListAssessmentsAsync(
        EnergyOperationContext context,
        EnergyIntelligenceAssessmentListFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new EnergyIntelligenceAssessmentListReadModel([], 0, 1, 25));

    public Task<EnergyIntelligenceAssessmentDetailReadModel?> GetAssessmentByReferenceAsync(
        EnergyOperationContext context,
        string assessmentReference,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<EnergyIntelligenceAssessmentDetailReadModel?>(null);

    public Task<EnergyWriteResult> EvaluateAssessmentAsync(
        EnergyOperationContext context,
        EvaluateEnergyIntelligenceAssessmentCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(EnergyWriteResult.Failure(EnergyFailureCodes.ValidationFailed, "Persistence unavailable."));
}
