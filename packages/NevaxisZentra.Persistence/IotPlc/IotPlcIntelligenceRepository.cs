using NevaxisZentra.Persistence.Development;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.IotPlc;

public sealed class IotPlcIntelligenceRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IIotPlcIntelligenceRepository
{
    public Task<IotPlcIntelligenceWorkspaceReadModel> GetWorkspaceAsync(
        IotPlcOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var profileCount = await dbContext.IotPlcQualityProfiles
                    .CountAsync(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId && p.Status == IotPlcQualityProfileStatuses.Approved, ct);
                var recentCutoff = DateTimeOffset.UtcNow.AddDays(-7);
                var connectivityCount = await dbContext.IotPlcConnectivityAssessments
                    .CountAsync(a => a.TenantId == context.TenantId && a.FacilityId == context.FacilityId && a.EvaluatedAtUtc >= recentCutoff, ct);
                var signalCount = await dbContext.IotPlcSignalQualityAssessments
                    .CountAsync(a => a.TenantId == context.TenantId && a.FacilityId == context.FacilityId && a.EvaluatedAtUtc >= recentCutoff, ct);
                var hasPilot = await dbContext.IotPlcQualityProfiles
                    .AnyAsync(p => p.Id == DevelopmentPilotIds.IotPlc2PilotProfileStandardId, ct);

                return new IotPlcIntelligenceWorkspaceReadModel(
                    profileCount,
                    connectivityCount,
                    signalCount,
                    IotPlcIntelligenceConstants.RuleSetVersion,
                    IotPlcIntelligenceConstants.DevelopmentPilotProvenance,
                    hasPilot);
            },
            cancellationToken);

    public Task<IotPlcQualityProfileListReadModel> ListProfilesAsync(
        IotPlcOperationContext context,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var normalizedPage = Math.Max(1, page);
                var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
                var query = dbContext.IotPlcQualityProfiles.AsNoTracking()
                    .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId);
                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(p => p.Status == status.Trim());
                }

                var total = await query.CountAsync(ct);
                var items = await query.OrderBy(p => p.ProfileCode)
                    .Skip((normalizedPage - 1) * normalizedPageSize)
                    .Take(normalizedPageSize)
                    .ToListAsync(ct);

                return new IotPlcQualityProfileListReadModel(
                    items.Select(MapProfile).ToList(),
                    total,
                    normalizedPage,
                    normalizedPageSize);
            },
            cancellationToken);

    public Task<IotPlcQualityProfileReadModel?> GetProfileByReferenceAsync(
        IotPlcOperationContext context,
        string reference,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var profile = await FindProfileAsync(context, reference, ct);
                return profile is null ? null : MapProfile(profile);
            },
            cancellationToken);

    public Task<IotPlcIntelligenceWriteResult> CreateProfileAsync(
        IotPlcOperationContext context,
        CreateIotPlcQualityProfileCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                if (string.IsNullOrWhiteSpace(command.ProfileCode) || string.IsNullOrWhiteSpace(command.DisplayName))
                {
                    return IotPlcIntelligenceWriteResult.Failure(IotPlcIntelligenceFailureCodes.ValidationFailed, "Profile code and display name are required.");
                }

                var exists = await dbContext.IotPlcQualityProfiles.AnyAsync(
                    p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId && p.ProfileCode == command.ProfileCode.Trim(), ct);
                if (exists)
                {
                    return IotPlcIntelligenceWriteResult.Failure(IotPlcIntelligenceFailureCodes.ValidationFailed, "Profile code already exists.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var profile = new IotPlcQualityProfile
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    ProfileCode = command.ProfileCode.Trim(),
                    DisplayName = command.DisplayName.Trim(),
                    ExpectedObservationIntervalSeconds = command.ExpectedObservationIntervalSeconds,
                    FreshnessToleranceSeconds = command.FreshnessToleranceSeconds,
                    StaleThresholdSeconds = command.StaleThresholdSeconds,
                    MaximumClockSkewToleranceSeconds = command.MaximumClockSkewToleranceSeconds,
                    MinimumExpectedObservationCount = command.MinimumExpectedObservationCount,
                    MinimumGoodQualityRatio = command.MinimumGoodQualityRatio,
                    MaximumBadQualityRatio = command.MaximumBadQualityRatio,
                    MaximumUncertainQualityRatio = command.MaximumUncertainQualityRatio,
                    MaximumSourceConflictRatio = command.MaximumSourceConflictRatio,
                    SequenceGapEvaluationEnabled = command.SequenceGapEvaluationEnabled,
                    Status = command.Status.Trim(),
                    SourceProvenance = command.SourceProvenance.Trim(),
                    Version = 1,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedBy = actor,
                    UpdatedBy = actor,
                };

                dbContext.IotPlcQualityProfiles.Add(profile);
                await dbContext.SaveChangesAsync(ct);
                return IotPlcIntelligenceWriteResult.Success(profile.Id, profile.Version);
            },
            cancellationToken);

    public Task<IotPlcIntelligenceWriteResult> UpdateProfileAsync(
        IotPlcOperationContext context,
        string reference,
        UpdateIotPlcQualityProfileCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var profile = await FindProfileAsync(context, reference, ct, tracked: true);
                if (profile is null)
                {
                    return IotPlcIntelligenceWriteResult.Failure(IotPlcIntelligenceFailureCodes.ProfileNotFound, "Quality profile not found.");
                }

                if (profile.Version != command.ExpectedVersion)
                {
                    return IotPlcIntelligenceWriteResult.Failure(IotPlcIntelligenceFailureCodes.ConcurrencyConflict, "Quality profile was modified by another request.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                profile.DisplayName = command.DisplayName.Trim();
                profile.ExpectedObservationIntervalSeconds = command.ExpectedObservationIntervalSeconds;
                profile.FreshnessToleranceSeconds = command.FreshnessToleranceSeconds;
                profile.StaleThresholdSeconds = command.StaleThresholdSeconds;
                profile.MaximumClockSkewToleranceSeconds = command.MaximumClockSkewToleranceSeconds;
                profile.MinimumExpectedObservationCount = command.MinimumExpectedObservationCount;
                profile.MinimumGoodQualityRatio = command.MinimumGoodQualityRatio;
                profile.MaximumBadQualityRatio = command.MaximumBadQualityRatio;
                profile.MaximumUncertainQualityRatio = command.MaximumUncertainQualityRatio;
                profile.MaximumSourceConflictRatio = command.MaximumSourceConflictRatio;
                profile.SequenceGapEvaluationEnabled = command.SequenceGapEvaluationEnabled;
                profile.Status = command.Status.Trim();
                profile.SourceProvenance = command.SourceProvenance.Trim();
                profile.UpdatedAtUtc = now;
                profile.UpdatedBy = actor;
                profile.Version++;

                await dbContext.SaveChangesAsync(ct);
                return IotPlcIntelligenceWriteResult.Success(profile.Id, profile.Version);
            },
            cancellationToken);

    public Task<IotPlcAssessmentDetailReadModel<IotPlcConnectivityAssessmentReadModel>?> GetConnectivityAssessmentAsync(
        IotPlcOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct => await LoadConnectivityDetailAsync(context, assessmentId, ct),
            cancellationToken);

    public Task<IReadOnlyList<IotPlcConnectivityAssessmentReadModel>> ListConnectivityAssessmentsAsync(
        IotPlcOperationContext context,
        Guid? endpointId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var normalizedPage = Math.Max(1, page);
                var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
                var query = dbContext.IotPlcConnectivityAssessments.AsNoTracking()
                    .Where(a => a.TenantId == context.TenantId && a.FacilityId == context.FacilityId);
                if (endpointId is not null)
                {
                    query = query.Where(a => a.TargetId == endpointId);
                }

                var items = await query.OrderByDescending(a => a.EvaluatedAtUtc)
                    .Skip((normalizedPage - 1) * normalizedPageSize)
                    .Take(normalizedPageSize)
                    .ToListAsync(ct);

                return (IReadOnlyList<IotPlcConnectivityAssessmentReadModel>)items.Select(MapConnectivity).ToList();
            },
            cancellationToken);

    public Task<IotPlcAssessmentDetailReadModel<IotPlcSignalQualityAssessmentReadModel>?> GetSignalAssessmentAsync(
        IotPlcOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct => await LoadSignalDetailAsync(context, assessmentId, ct),
            cancellationToken);

    public Task<IReadOnlyList<IotPlcSignalQualityAssessmentReadModel>> ListSignalAssessmentsAsync(
        IotPlcOperationContext context,
        Guid? signalId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var normalizedPage = Math.Max(1, page);
                var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
                var query = dbContext.IotPlcSignalQualityAssessments.AsNoTracking()
                    .Where(a => a.TenantId == context.TenantId && a.FacilityId == context.FacilityId);
                if (signalId is not null)
                {
                    query = query.Where(a => a.SignalDefinitionId == signalId);
                }

                var items = await query.OrderByDescending(a => a.EvaluatedAtUtc)
                    .Skip((normalizedPage - 1) * normalizedPageSize)
                    .Take(normalizedPageSize)
                    .ToListAsync(ct);

                return (IReadOnlyList<IotPlcSignalQualityAssessmentReadModel>)items.Select(MapSignalAssessment).ToList();
            },
            cancellationToken);

    public Task<IotPlcIntelligenceWriteResult> EvaluateConnectivityAsync(
        IotPlcOperationContext context,
        EvaluateIotPlcConnectivityCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validation = ValidateEvaluateWindow(command.EvaluationWindowStartUtc, command.EvaluationWindowEndUtc, command.EvaluationTimeUtc);
                if (validation is not null)
                {
                    return IotPlcIntelligenceWriteResult.Failure(IotPlcIntelligenceFailureCodes.ValidationFailed, validation);
                }

                var endpoint = await FindEndpointAsync(context, command.EndpointReference, ct);
                if (endpoint is null)
                {
                    return IotPlcIntelligenceWriteResult.Failure(IotPlcIntelligenceFailureCodes.TargetNotFound, "Endpoint not found.");
                }

                var fingerprint = IotPlcIntelligenceIdempotencyFingerprint.ComputeConnectivityEvaluateFingerprint(context, command);
                if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
                {
                    var existing = await dbContext.IotPlcConnectivityAssessments.FirstOrDefaultAsync(
                        a => a.TenantId == context.TenantId && a.EvaluateIdempotencyKey == command.IdempotencyKey!.Trim(), ct);
                    if (existing is not null)
                    {
                        if (string.IsNullOrEmpty(existing.EvaluateIdempotencyPayloadFingerprint))
                        {
                            return IotPlcIntelligenceWriteResult.Failure(IotPlcIntelligenceFailureCodes.IdempotencyLegacyUnverifiable, "Legacy assessment cannot be verified for replay.");
                        }

                        if (!string.Equals(existing.EvaluateIdempotencyPayloadFingerprint, fingerprint, StringComparison.Ordinal))
                        {
                            return IotPlcIntelligenceWriteResult.Failure(IotPlcIntelligenceFailureCodes.IdempotencyConflict, "Idempotency key used with different payload.");
                        }

                        return IotPlcIntelligenceWriteResult.Success(existing.Id, idempotentReplay: true);
                    }
                }

                var profile = await ResolveProfileAsync(context, command.ProfileReference, ct);
                var health = await dbContext.IotPlcConnectionHealthObservations.AsNoTracking()
                    .Where(h => h.EndpointId == endpoint.Id && h.TenantId == context.TenantId && h.FacilityId == context.FacilityId)
                    .ToListAsync(ct);
                var endpointObs = await dbContext.IotPlcSignalObservations.AsNoTracking()
                    .Where(o => o.EndpointId == endpoint.Id && o.TenantId == context.TenantId && o.FacilityId == context.FacilityId)
                    .ToListAsync(ct);

                var evaluation = IotPlcIntelligenceCalculator.EvaluateConnectivity(new IotPlcIntelligenceCalculator.ConnectivityEvaluationInput(
                    profile,
                    command.EvidenceMode.Trim(),
                    command.EvaluationTimeUtc,
                    command.EvaluationWindowStartUtc,
                    command.EvaluationWindowEndUtc,
                    health,
                    endpointObs));

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var assessmentId = Guid.NewGuid();
                var leading = IotPlcIntelligenceCalculator.SelectLeadingDriver(evaluation.Drivers);
                var driverEntities = PersistDrivers(context, IotPlcIntelligenceAssessmentKinds.Connectivity, assessmentId, evaluation.Drivers, leading, actor, now);
                var leadingEntity = driverEntities.First(d => d.IsLeadingDriver);

                var assessment = new IotPlcConnectivityAssessment
                {
                    Id = assessmentId,
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    TargetType = IotPlcIntelligenceTargetTypes.Endpoint,
                    TargetId = endpoint.Id,
                    EvaluationWindowStartUtc = command.EvaluationWindowStartUtc,
                    EvaluationWindowEndUtc = command.EvaluationWindowEndUtc,
                    QualityProfileId = profile?.Id,
                    EvidenceMode = command.EvidenceMode.Trim(),
                    Outcome = evaluation.Outcome,
                    LeadingDriverCode = leadingEntity.ReasonCode,
                    LeadingDriverId = leadingEntity.Id,
                    LatestHealthState = evaluation.LatestHealthState,
                    HealthObservationAvailabilityRatio = evaluation.HealthObservationAvailabilityRatio,
                    FreshnessSeconds = evaluation.FreshnessSeconds,
                    SourceProvenance = command.SourceProvenance.Trim(),
                    EvaluateIdempotencyKey = TrimOrNull(command.IdempotencyKey),
                    EvaluateIdempotencyPayloadFingerprint = fingerprint,
                    EvaluatedAtUtc = command.EvaluationTimeUtc,
                    CreatedAtUtc = now,
                    CreatedBy = actor,
                };

                dbContext.IotPlcConnectivityAssessments.Add(assessment);
                dbContext.IotPlcIntelligenceDrivers.AddRange(driverEntities);
                await dbContext.SaveChangesAsync(ct);
                return IotPlcIntelligenceWriteResult.Success(assessment.Id);
            },
            cancellationToken);

    public Task<IotPlcIntelligenceWriteResult> EvaluateSignalQualityAsync(
        IotPlcOperationContext context,
        EvaluateIotPlcSignalQualityCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validation = ValidateEvaluateWindow(command.EvaluationWindowStartUtc, command.EvaluationWindowEndUtc, command.EvaluationTimeUtc);
                if (validation is not null)
                {
                    return IotPlcIntelligenceWriteResult.Failure(IotPlcIntelligenceFailureCodes.ValidationFailed, validation);
                }

                var signal = await FindSignalAsync(context, command.SignalReference, ct);
                if (signal is null)
                {
                    return IotPlcIntelligenceWriteResult.Failure(IotPlcIntelligenceFailureCodes.TargetNotFound, "Signal not found.");
                }

                var fingerprint = IotPlcIntelligenceIdempotencyFingerprint.ComputeSignalEvaluateFingerprint(context, command);
                if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
                {
                    var existing = await dbContext.IotPlcSignalQualityAssessments.FirstOrDefaultAsync(
                        a => a.TenantId == context.TenantId && a.EvaluateIdempotencyKey == command.IdempotencyKey!.Trim(), ct);
                    if (existing is not null)
                    {
                        if (string.IsNullOrEmpty(existing.EvaluateIdempotencyPayloadFingerprint))
                        {
                            return IotPlcIntelligenceWriteResult.Failure(IotPlcIntelligenceFailureCodes.IdempotencyLegacyUnverifiable, "Legacy assessment cannot be verified for replay.");
                        }

                        if (!string.Equals(existing.EvaluateIdempotencyPayloadFingerprint, fingerprint, StringComparison.Ordinal))
                        {
                            return IotPlcIntelligenceWriteResult.Failure(IotPlcIntelligenceFailureCodes.IdempotencyConflict, "Idempotency key used with different payload.");
                        }

                        return IotPlcIntelligenceWriteResult.Success(existing.Id, idempotentReplay: true);
                    }
                }

                var profile = await ResolveProfileAsync(context, command.ProfileReference, ct);
                var observations = await dbContext.IotPlcSignalObservations.AsNoTracking()
                    .Where(o => o.SignalDefinitionId == signal.Id && o.TenantId == context.TenantId && o.FacilityId == context.FacilityId)
                    .ToListAsync(ct);

                var evaluation = IotPlcIntelligenceCalculator.EvaluateSignalQuality(new IotPlcIntelligenceCalculator.SignalEvaluationInput(
                    profile,
                    command.EvidenceMode.Trim(),
                    command.EvaluationTimeUtc,
                    command.EvaluationWindowStartUtc,
                    command.EvaluationWindowEndUtc,
                    observations));

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var assessmentId = Guid.NewGuid();
                var leading = IotPlcIntelligenceCalculator.SelectLeadingDriver(evaluation.Drivers);
                var driverEntities = PersistDrivers(context, IotPlcIntelligenceAssessmentKinds.SignalQuality, assessmentId, evaluation.Drivers, leading, actor, now);
                var leadingEntity = driverEntities.First(d => d.IsLeadingDriver);

                var assessment = new IotPlcSignalQualityAssessment
                {
                    Id = assessmentId,
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    TargetType = IotPlcIntelligenceTargetTypes.Signal,
                    TargetId = signal.Id,
                    SignalDefinitionId = signal.Id,
                    EndpointId = signal.EndpointId,
                    EvaluationWindowStartUtc = command.EvaluationWindowStartUtc,
                    EvaluationWindowEndUtc = command.EvaluationWindowEndUtc,
                    QualityProfileId = profile?.Id,
                    EvidenceMode = command.EvidenceMode.Trim(),
                    Outcome = evaluation.Outcome,
                    LeadingDriverCode = leadingEntity.ReasonCode,
                    LeadingDriverId = leadingEntity.Id,
                    ActualObservationCount = evaluation.ActualObservationCount,
                    ExpectedObservationCount = evaluation.ExpectedObservationCount,
                    CompletenessRatio = evaluation.CompletenessRatio,
                    GoodCount = evaluation.GoodCount,
                    UncertainCount = evaluation.UncertainCount,
                    BadCount = evaluation.BadCount,
                    StaleCount = evaluation.StaleCount,
                    SourceConflictCount = evaluation.SourceConflictCount,
                    GoodRatio = evaluation.GoodRatio,
                    UncertainRatio = evaluation.UncertainRatio,
                    BadRatio = evaluation.BadRatio,
                    StaleRatio = evaluation.StaleRatio,
                    SourceConflictRatio = evaluation.SourceConflictRatio,
                    LatestValidObservedAtUtc = evaluation.LatestValidObservedAtUtc,
                    FreshnessSeconds = evaluation.FreshnessSeconds,
                    MaxClockSkewSeconds = evaluation.MaxClockSkewSeconds,
                    SourceProvenance = command.SourceProvenance.Trim(),
                    EvaluateIdempotencyKey = TrimOrNull(command.IdempotencyKey),
                    EvaluateIdempotencyPayloadFingerprint = fingerprint,
                    EvaluatedAtUtc = command.EvaluationTimeUtc,
                    CreatedAtUtc = now,
                    CreatedBy = actor,
                };

                dbContext.IotPlcSignalQualityAssessments.Add(assessment);
                dbContext.IotPlcIntelligenceDrivers.AddRange(driverEntities);
                await dbContext.SaveChangesAsync(ct);
                return IotPlcIntelligenceWriteResult.Success(assessment.Id);
            },
            cancellationToken);

    private static List<IotPlcIntelligenceDriver> PersistDrivers(
        IotPlcOperationContext context,
        string assessmentKind,
        Guid assessmentId,
        IReadOnlyList<IotPlcIntelligenceCalculator.DriverCandidate> drivers,
        IotPlcIntelligenceCalculator.DriverCandidate leading,
        string actor,
        DateTimeOffset now)
    {
        var entities = new List<IotPlcIntelligenceDriver>();
        var sort = 0;
        foreach (var driver in drivers.OrderBy(d => d.ReasonCode, StringComparer.Ordinal))
        {
            entities.Add(new IotPlcIntelligenceDriver
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                AssessmentKind = assessmentKind,
                AssessmentId = assessmentId,
                ReasonCode = driver.ReasonCode,
                OutcomeSeverity = driver.OutcomeSeverity,
                ThresholdVariance = driver.ThresholdVariance,
                AffectedObservationCount = driver.AffectedObservationCount,
                StructuredEvidenceJson = driver.StructuredEvidenceJson,
                Explanation = driver.Explanation,
                IsLeadingDriver = driver.ReasonCode == leading.ReasonCode && driver.Explanation == leading.Explanation,
                SortOrder = sort++,
                CreatedAtUtc = now,
                CreatedBy = actor,
            });
        }

        if (!entities.Any(d => d.IsLeadingDriver) && entities.Count > 0)
        {
            var match = entities.First(d => d.ReasonCode == leading.ReasonCode);
            match.IsLeadingDriver = true;
        }

        return entities;
    }

    private async Task<IotPlcAssessmentDetailReadModel<IotPlcConnectivityAssessmentReadModel>?> LoadConnectivityDetailAsync(
        IotPlcOperationContext context,
        Guid assessmentId,
        CancellationToken ct)
    {
        var assessment = await dbContext.IotPlcConnectivityAssessments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == assessmentId && a.TenantId == context.TenantId && a.FacilityId == context.FacilityId, ct);
        if (assessment is null) return null;
        var drivers = await LoadDriversAsync(IotPlcIntelligenceAssessmentKinds.Connectivity, assessmentId, ct);
        return new IotPlcAssessmentDetailReadModel<IotPlcConnectivityAssessmentReadModel>(MapConnectivity(assessment), drivers);
    }

    private async Task<IotPlcAssessmentDetailReadModel<IotPlcSignalQualityAssessmentReadModel>?> LoadSignalDetailAsync(
        IotPlcOperationContext context,
        Guid assessmentId,
        CancellationToken ct)
    {
        var assessment = await dbContext.IotPlcSignalQualityAssessments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == assessmentId && a.TenantId == context.TenantId && a.FacilityId == context.FacilityId, ct);
        if (assessment is null) return null;
        var drivers = await LoadDriversAsync(IotPlcIntelligenceAssessmentKinds.SignalQuality, assessmentId, ct);
        return new IotPlcAssessmentDetailReadModel<IotPlcSignalQualityAssessmentReadModel>(MapSignalAssessment(assessment), drivers);
    }

    private async Task<IReadOnlyList<IotPlcIntelligenceDriverReadModel>> LoadDriversAsync(string kind, Guid assessmentId, CancellationToken ct)
    {
        var drivers = await dbContext.IotPlcIntelligenceDrivers.AsNoTracking()
            .Where(d => d.AssessmentKind == kind && d.AssessmentId == assessmentId)
            .OrderBy(d => d.SortOrder)
            .ToListAsync(ct);

        return drivers.Select(d => new IotPlcIntelligenceDriverReadModel(
            d.Id, d.ReasonCode, d.OutcomeSeverity, d.ThresholdVariance, d.AffectedObservationCount, d.Explanation, d.IsLeadingDriver, d.SortOrder)).ToList();
    }

    private async Task<IotPlcQualityProfile?> ResolveProfileAsync(IotPlcOperationContext context, string? profileReference, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(profileReference))
        {
            return await dbContext.IotPlcQualityProfiles.AsNoTracking()
                .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId && p.Status == IotPlcQualityProfileStatuses.Approved)
                .OrderBy(p => p.ProfileCode)
                .FirstOrDefaultAsync(ct);
        }

        return await FindProfileAsync(context, profileReference, ct);
    }

    private async Task<IotPlcQualityProfile?> FindProfileAsync(IotPlcOperationContext context, string reference, CancellationToken ct, bool tracked = false)
    {
        var query = tracked ? dbContext.IotPlcQualityProfiles.AsQueryable() : dbContext.IotPlcQualityProfiles.AsNoTracking();
        if (Guid.TryParse(reference, out var id))
        {
            return await query.FirstOrDefaultAsync(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId && p.Id == id, ct);
        }

        return await query.FirstOrDefaultAsync(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId && p.ProfileCode == reference.Trim(), ct);
    }

    private async Task<IotPlcEndpoint?> FindEndpointAsync(IotPlcOperationContext context, string reference, CancellationToken ct)
    {
        if (Guid.TryParse(reference, out var id))
        {
            return await dbContext.IotPlcEndpoints.AsNoTracking()
                .FirstOrDefaultAsync(e => e.TenantId == context.TenantId && e.FacilityId == context.FacilityId && e.Id == id, ct);
        }

        return await dbContext.IotPlcEndpoints.AsNoTracking()
            .FirstOrDefaultAsync(e => e.TenantId == context.TenantId && e.FacilityId == context.FacilityId && e.EndpointCode == reference.Trim(), ct);
    }

    private async Task<IotPlcSignal?> FindSignalAsync(IotPlcOperationContext context, string reference, CancellationToken ct)
    {
        if (Guid.TryParse(reference, out var id))
        {
            return await dbContext.IotPlcSignals.AsNoTracking()
                .FirstOrDefaultAsync(s => s.TenantId == context.TenantId && s.FacilityId == context.FacilityId && s.Id == id, ct);
        }

        return await dbContext.IotPlcSignals.AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == context.TenantId && s.FacilityId == context.FacilityId && s.SignalCode == reference.Trim(), ct);
    }

    private static string? ValidateEvaluateWindow(DateTimeOffset start, DateTimeOffset end, DateTimeOffset evaluationTime)
    {
        if (start.Offset != TimeSpan.Zero || end.Offset != TimeSpan.Zero || evaluationTime.Offset != TimeSpan.Zero)
        {
            return "All timestamps must be UTC.";
        }

        if (end <= start)
        {
            return "Evaluation window end must be after start.";
        }

        return null;
    }

    private static IotPlcQualityProfileReadModel MapProfile(IotPlcQualityProfile profile) =>
        new(profile.Id, profile.ProfileCode, profile.DisplayName, profile.ExpectedObservationIntervalSeconds, profile.FreshnessToleranceSeconds,
            profile.StaleThresholdSeconds, profile.MaximumClockSkewToleranceSeconds, profile.MinimumExpectedObservationCount,
            profile.MinimumGoodQualityRatio, profile.MaximumBadQualityRatio, profile.MaximumUncertainQualityRatio, profile.MaximumSourceConflictRatio,
            profile.SequenceGapEvaluationEnabled, profile.Status, profile.SourceProvenance, profile.Version,
            profile.SourceProvenance.Contains("Deterministic local Development", StringComparison.Ordinal));

    private static IotPlcConnectivityAssessmentReadModel MapConnectivity(IotPlcConnectivityAssessment assessment) =>
        new(assessment.Id, assessment.TargetType, assessment.TargetId, assessment.EvaluationWindowStartUtc, assessment.EvaluationWindowEndUtc,
            assessment.QualityProfileId, assessment.EvidenceMode, assessment.Outcome, assessment.LeadingDriverCode, assessment.LatestHealthState,
            assessment.HealthObservationAvailabilityRatio, assessment.FreshnessSeconds, assessment.EvaluatedAtUtc, assessment.SourceProvenance,
            assessment.SourceProvenance.Contains("Deterministic local Development", StringComparison.Ordinal));

    private static IotPlcSignalQualityAssessmentReadModel MapSignalAssessment(IotPlcSignalQualityAssessment assessment) =>
        new(assessment.Id, assessment.TargetType, assessment.TargetId, assessment.SignalDefinitionId, assessment.EvaluationWindowStartUtc,
            assessment.EvaluationWindowEndUtc, assessment.QualityProfileId, assessment.EvidenceMode, assessment.Outcome, assessment.LeadingDriverCode,
            assessment.ActualObservationCount, assessment.ExpectedObservationCount, assessment.CompletenessRatio, assessment.GoodRatio,
            assessment.UncertainRatio, assessment.BadRatio, assessment.StaleRatio, assessment.SourceConflictRatio, assessment.FreshnessSeconds,
            assessment.MaxClockSkewSeconds, assessment.EvaluatedAtUtc, assessment.SourceProvenance,
            assessment.SourceProvenance.Contains("Deterministic local Development", StringComparison.Ordinal));

    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
