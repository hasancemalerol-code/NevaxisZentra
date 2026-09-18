using System.Text.Json;
using NevaxisZentra.Persistence.Development;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.OperationalRisk;

public sealed class OperationalRiskDetectionRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IEnumerable<IOperationalRiskEvidenceSource> evidenceSources) : IOperationalRiskDetectionRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<IReadOnlyList<OperationalRiskDetectionProfileReadModel>> ListProfilesAsync(
        OperationalRiskOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var profiles = await dbContext.OperationalRiskDetectionProfiles.AsNoTracking()
                    .Include(p => p.Rules)
                    .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId)
                    .OrderBy(p => p.ProfileCode)
                    .ToListAsync(ct);

                return (IReadOnlyList<OperationalRiskDetectionProfileReadModel>)profiles.Select(MapProfile).ToList();
            },
            cancellationToken);

    public Task<OperationalRiskDetectionProfileReadModel?> GetProfileByIdAsync(
        OperationalRiskOperationContext context,
        Guid profileId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var profile = await dbContext.OperationalRiskDetectionProfiles.AsNoTracking()
                    .Include(p => p.Rules)
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId
                             && p.Id == profileId,
                        ct);

                return profile is null ? null : MapProfile(profile);
            },
            cancellationToken);

    public Task<OperationalRiskWriteResult> CreateProfileAsync(
        OperationalRiskOperationContext context,
        CreateOperationalRiskDetectionProfileCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateCreateProfileCommand(command);
                if (validationError is not null)
                {
                    return OperationalRiskWriteResult.Fail(OperationalRiskFailureCodes.ValidationFailed, validationError);
                }

                var fingerprint = OperationalRiskDetectionIdempotencyFingerprint.ComputeProfileFingerprint(context, command);
                var existing = await dbContext.OperationalRiskDetectionProfiles.AsNoTracking()
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId && p.CreateIdempotencyKey == command.IdempotencyKey.Trim(),
                        ct);

                if (existing is not null)
                {
                    return ResolveIdempotentProfileReplay(existing, fingerprint);
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var profile = new OperationalRiskDetectionProfile
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    ProfileCode = command.ProfileCode.Trim(),
                    DisplayName = command.DisplayName.Trim(),
                    Status = OperationalRiskDetectionProfileStatuses.Draft,
                    RuleSetVersion = OperationalRiskDetectionConstants.RuleSetVersion,
                    SourceProvenance = command.SourceProvenance.Trim(),
                    CreateIdempotencyKey = command.IdempotencyKey.Trim(),
                    CreateIdempotencyPayloadFingerprint = fingerprint,
                    Version = 1,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedBy = actor,
                    UpdatedBy = actor,
                };

                foreach (var ruleCommand in command.Rules)
                {
                    profile.Rules.Add(new OperationalRiskDetectionRule
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        ProfileId = profile.Id,
                        RuleCode = ruleCommand.RuleCode.Trim(),
                        SourceModule = ruleCommand.SourceModule.Trim(),
                        SourceOutcome = ruleCommand.SourceOutcome.Trim(),
                        RiskCategory = ruleCommand.RiskCategory.Trim(),
                        DefaultLikelihood = ruleCommand.DefaultLikelihood,
                        DefaultImpact = ruleCommand.DefaultImpact,
                        ThresholdVarianceBand = ruleCommand.ThresholdVarianceBand,
                        DriverReasonCode = ruleCommand.DriverReasonCode.Trim(),
                        PriorityOrder = ruleCommand.PriorityOrder,
                        IsActive = ruleCommand.IsActive,
                        SourceProvenance = command.SourceProvenance.Trim(),
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now,
                    });
                }

                dbContext.OperationalRiskDetectionProfiles.Add(profile);
                AddAuditEvent(
                    context,
                    OperationalRiskDetectionAuditEventTypes.ProfileCreated,
                    "operational-risk-detection-profile",
                    profile.Id,
                    new { profile.ProfileCode },
                    actor,
                    now);

                await dbContext.SaveChangesAsync(ct);
                return OperationalRiskWriteResult.Success(profile.Id, profile.Version);
            },
            cancellationToken);

    public Task<OperationalRiskWriteResult> UpdateProfileAsync(
        OperationalRiskOperationContext context,
        UpdateOperationalRiskDetectionProfileCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                if (string.IsNullOrWhiteSpace(command.DisplayName))
                {
                    return OperationalRiskWriteResult.Fail(OperationalRiskFailureCodes.ValidationFailed, "Display name is required.");
                }

                if (!OperationalRiskDetectionProfileStatuses.All.Contains(command.Status.Trim()))
                {
                    return OperationalRiskWriteResult.Fail(OperationalRiskFailureCodes.ValidationFailed, "Profile status is invalid.");
                }

                var profile = await dbContext.OperationalRiskDetectionProfiles
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId
                             && p.Id == command.ProfileId,
                        ct);

                if (profile is null)
                {
                    return OperationalRiskWriteResult.Fail(OperationalRiskFailureCodes.NotFound, "Detection profile not found.");
                }

                if (profile.Version != command.ExpectedVersion)
                {
                    return OperationalRiskWriteResult.Fail(
                        OperationalRiskFailureCodes.ConcurrencyConflict,
                        "Detection profile version is stale.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                profile.DisplayName = command.DisplayName.Trim();
                profile.Status = command.Status.Trim();
                profile.SourceProvenance = command.SourceProvenance.Trim();
                profile.UpdatedAtUtc = now;
                profile.UpdatedBy = actor;

                AddAuditEvent(
                    context,
                    OperationalRiskDetectionAuditEventTypes.ProfileUpdated,
                    "operational-risk-detection-profile",
                    profile.Id,
                    new { profile.ProfileCode, profile.Status },
                    actor,
                    now);

                await dbContext.SaveChangesAsync(ct);
                return OperationalRiskWriteResult.Success(profile.Id, profile.Version);
            },
            cancellationToken);

    public Task<IReadOnlyList<OperationalRiskDetectionRunSummaryReadModel>> ListRunsAsync(
        OperationalRiskOperationContext context,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                page = page < 1 ? 1 : page;
                pageSize = pageSize is < 1 or > 100 ? 25 : pageSize;

                var query = dbContext.OperationalRiskDetectionRuns.AsNoTracking()
                    .Include(r => r.Profile)
                    .Where(r => r.TenantId == context.TenantId && r.FacilityId == context.FacilityId);

                var runs = await query
                    .OrderByDescending(r => r.CreatedAtUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                return (IReadOnlyList<OperationalRiskDetectionRunSummaryReadModel>)runs.Select(MapRunSummary).ToList();
            },
            cancellationToken);

    public Task<OperationalRiskDetectionRunDetailReadModel?> GetRunByIdAsync(
        OperationalRiskOperationContext context,
        Guid runId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var run = await dbContext.OperationalRiskDetectionRuns.AsNoTracking()
                    .Include(r => r.Profile)
                    .Include(r => r.Results)
                    .ThenInclude(result => result.Drivers)
                    .SingleOrDefaultAsync(
                        r => r.TenantId == context.TenantId
                             && r.FacilityId == context.FacilityId
                             && r.Id == runId,
                        ct);

                return run is null ? null : MapRunDetail(run);
            },
            cancellationToken);

    public Task<OperationalRiskWriteResult> ExecuteRunAsync(
        OperationalRiskOperationContext context,
        ExecuteOperationalRiskDetectionRunCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateRunCommand(command);
                if (validationError is not null)
                {
                    return OperationalRiskWriteResult.Fail(OperationalRiskFailureCodes.ValidationFailed, validationError);
                }

                var fingerprint = OperationalRiskDetectionIdempotencyFingerprint.ComputeRunFingerprint(context, command);
                var existingRun = await dbContext.OperationalRiskDetectionRuns.AsNoTracking()
                    .SingleOrDefaultAsync(
                        r => r.TenantId == context.TenantId && r.IdempotencyKey == command.IdempotencyKey.Trim(),
                        ct);

                if (existingRun is not null)
                {
                    return ResolveIdempotentRunReplay(existingRun, fingerprint);
                }

                var profile = await dbContext.OperationalRiskDetectionProfiles
                    .Include(p => p.Rules)
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId
                             && p.Id == command.ProfileId,
                        ct);

                if (profile is null)
                {
                    return OperationalRiskWriteResult.Fail(OperationalRiskFailureCodes.NotFound, "Detection profile not found.");
                }

                if (!string.Equals(profile.Status, OperationalRiskDetectionProfileStatuses.Approved, StringComparison.Ordinal))
                {
                    return OperationalRiskWriteResult.Fail(
                        OperationalRiskFailureCodes.ValidationFailed,
                        "Only approved detection profiles may execute runs.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var snapshots = await CollectSnapshotsAsync(context, command.WindowStartUtc, command.WindowEndUtc, ct);
                var activeRules = profile.Rules.Where(r => r.IsActive).OrderBy(r => r.PriorityOrder).ToList();

                var run = new OperationalRiskDetectionRun
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    RunReference = await GenerateRunReferenceAsync(context, ct),
                    ProfileId = profile.Id,
                    ProfileVersion = profile.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    WindowStartUtc = command.WindowStartUtc,
                    WindowEndUtc = command.WindowEndUtc,
                    Status = OperationalRiskDetectionRunStatuses.Completed,
                    SourceCount = snapshots.Count,
                    RuleSetVersion = OperationalRiskDetectionConstants.RuleSetVersion,
                    IdempotencyKey = command.IdempotencyKey.Trim(),
                    IdempotencyPayloadFingerprint = fingerprint,
                    SourceProvenance = command.SourceProvenance.Trim(),
                    CreatedAtUtc = now,
                    CompletedAtUtc = now,
                    CreatedBy = actor,
                };

                var runOpenCasesByDetectionKey = new Dictionary<string, OperationalRiskCase>(StringComparer.Ordinal);

                foreach (var snapshot in snapshots.OrderBy(s => s.SourceModule).ThenBy(s => s.SourceReference))
                {
                    var rule = MatchRule(activeRules, snapshot);
                    var evaluation = OperationalRiskDetectionEvaluator.Evaluate(
                        new OperationalRiskDetectionEvaluationInput(snapshot, rule));

                    var ruleCode = rule?.RuleCode ?? "unmapped";
                    var detectionKey = OperationalRiskDetectionKeyBuilder.Build(
                        context.TenantId,
                        context.FacilityId,
                        snapshot.RiskCategory,
                        snapshot.ScopeType,
                        snapshot.ScopeReference,
                        snapshot.SourceModule,
                        snapshot.SourceReference,
                        ruleCode);

                    Guid? linkedCaseId = null;
                    if (string.Equals(evaluation.DetectionOutcome, OperationalRiskDetectionOutcomes.RiskDetected, StringComparison.Ordinal)
                        && rule is not null)
                    {
                        linkedCaseId = await ReconcileRiskCaseAsync(
                            context,
                            snapshot,
                            rule,
                            evaluation,
                            detectionKey,
                            command.SourceProvenance,
                            actor,
                            now,
                            runOpenCasesByDetectionKey,
                            ct);
                    }
                    else if (string.Equals(evaluation.DetectionOutcome, OperationalRiskDetectionOutcomes.SourceConflict, StringComparison.Ordinal))
                    {
                        linkedCaseId = await AppendConflictEvidenceIfKnownAsync(
                            context,
                            snapshot,
                            detectionKey,
                            command.SourceProvenance,
                            actor,
                            now,
                            ct);
                    }

                    var result = new OperationalRiskDetectionResult
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        RunId = run.Id,
                        SourceModule = snapshot.SourceModule,
                        SourceReference = snapshot.SourceReference,
                        RiskCategory = snapshot.RiskCategory,
                        ScopeType = snapshot.ScopeType,
                        ScopeReference = snapshot.ScopeReference,
                        EvidenceState = evaluation.EvidenceState,
                        SourceOutcome = snapshot.SourceOutcome,
                        DetectionOutcome = evaluation.DetectionOutcome,
                        ActualValue = snapshot.ActualValue,
                        ThresholdValue = snapshot.ThresholdValue,
                        Unit = snapshot.Unit,
                        NormalizedVariance = evaluation.NormalizedVariance,
                        LikelihoodLevel = evaluation.LikelihoodLevel,
                        ImpactLevel = evaluation.ImpactLevel,
                        RiskScore = evaluation.RiskScore,
                        SeverityBand = evaluation.SeverityBand,
                        LeadingDriverCode = evaluation.LeadingDriverCode,
                        LinkedCaseId = linkedCaseId,
                        DetectionKey = detectionKey,
                        SourceAssessmentReference = snapshot.SourceAssessmentReference,
                        EvaluatedAtUtc = snapshot.ObservedAtUtc,
                        CreatedAtUtc = now,
                    };

                    var orderedDrivers = OrderDrivers(evaluation.Drivers);
                    for (var index = 0; index < orderedDrivers.Count; index++)
                    {
                        var driver = orderedDrivers[index];
                        result.Drivers.Add(new OperationalRiskDetectionDriver
                        {
                            Id = Guid.NewGuid(),
                            TenantId = context.TenantId,
                            FacilityId = context.FacilityId,
                            ResultId = result.Id,
                            ReasonCode = driver.ReasonCode,
                            Severity = driver.Severity,
                            ActualValue = driver.ActualValue,
                            ThresholdValue = driver.ThresholdValue,
                            Unit = driver.Unit,
                            NormalizedVariance = driver.NormalizedVariance,
                            SupportingText = driver.SupportingText,
                            SourceProvenance = snapshot.SourceProvenance,
                            SortOrder = index + 1,
                            CreatedAtUtc = now,
                        });
                    }

                    run.Results.Add(result);
                }

                run.ResultCount = run.Results.Count;
                dbContext.OperationalRiskDetectionRuns.Add(run);
                AddAuditEvent(
                    context,
                    OperationalRiskDetectionAuditEventTypes.RunCompleted,
                    "operational-risk-detection-run",
                    run.Id,
                    new { run.RunReference, run.ResultCount },
                    actor,
                    now);

                await dbContext.SaveChangesAsync(ct);
                return OperationalRiskWriteResult.Success(run.Id, 0);
            },
            cancellationToken);

    private async Task<IReadOnlyList<OperationalRiskSourceSnapshot>> CollectSnapshotsAsync(
        OperationalRiskOperationContext context,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        CancellationToken cancellationToken)
    {
        var snapshots = new List<OperationalRiskSourceSnapshot>();
        foreach (var source in evidenceSources.OrderBy(s => s.SourceModule, StringComparer.Ordinal))
        {
            var result = await source.CollectSnapshotsAsync(context, windowStartUtc, windowEndUtc, cancellationToken);
            if (result.Snapshots.Count > 0)
            {
                snapshots.AddRange(result.Snapshots);
            }
        }

        return snapshots;
    }

    private static OperationalRiskDetectionRule? MatchRule(
        IReadOnlyList<OperationalRiskDetectionRule> rules,
        OperationalRiskSourceSnapshot snapshot) =>
        rules
            .Where(r => string.Equals(r.SourceModule, snapshot.SourceModule, StringComparison.Ordinal)
                        && string.Equals(r.SourceOutcome, snapshot.SourceOutcome, StringComparison.Ordinal))
            .OrderBy(r => r.PriorityOrder)
            .ThenBy(r => r.RuleCode, StringComparer.Ordinal)
            .FirstOrDefault();

    private async Task<Guid> ReconcileRiskCaseAsync(
        OperationalRiskOperationContext context,
        OperationalRiskSourceSnapshot snapshot,
        OperationalRiskDetectionRule rule,
        OperationalRiskDetectionEvaluationOutput evaluation,
        string detectionKey,
        string sourceProvenance,
        string actor,
        DateTimeOffset now,
        Dictionary<string, OperationalRiskCase> runOpenCasesByDetectionKey,
        CancellationToken cancellationToken)
    {
        if (runOpenCasesByDetectionKey.TryGetValue(detectionKey, out var pendingCase))
        {
            UpdateReconciledCase(pendingCase, snapshot, evaluation, sourceProvenance, actor, now);
            AppendDetectionEvidence(context, pendingCase, snapshot, evaluation, sourceProvenance, actor, now);
            return pendingCase.Id;
        }

        var unresolvedCase = await dbContext.OperationalRiskCases
            .SingleOrDefaultAsync(
                c => c.TenantId == context.TenantId
                     && c.FacilityId == context.FacilityId
                     && c.DetectionKey == detectionKey
                     && c.LifecycleStatus != OperationalRiskLifecycleStatuses.Resolved
                     && c.LifecycleStatus != OperationalRiskLifecycleStatuses.Dismissed,
                cancellationToken);

        if (unresolvedCase is not null)
        {
            UpdateReconciledCase(unresolvedCase, snapshot, evaluation, sourceProvenance, actor, now);
            AppendDetectionEvidence(context, unresolvedCase, snapshot, evaluation, sourceProvenance, actor, now);
            AddAuditEvent(
                context,
                OperationalRiskAuditEventTypes.CaseAssessmentUpdated,
                "operational-risk-case",
                unresolvedCase.Id,
                new { unresolvedCase.CaseReference, Source = "detection-run" },
                actor,
                now);

            runOpenCasesByDetectionKey[detectionKey] = unresolvedCase;
            return unresolvedCase.Id;
        }

        var maxSequence = await dbContext.OperationalRiskCases.AsNoTracking()
            .Where(c => c.TenantId == context.TenantId && c.DetectionKey == detectionKey)
            .MaxAsync(c => (int?)c.OccurrenceSequence, cancellationToken) ?? 0;

        var riskCase = new OperationalRiskCase
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            CaseReference = await GenerateCaseReferenceAsync(context, cancellationToken),
            Category = snapshot.RiskCategory,
            ScopeType = snapshot.ScopeType,
            ScopeReference = snapshot.ScopeReference,
            Title = BuildCaseTitle(snapshot, rule),
            Description = BuildCaseDescription(snapshot, rule),
            EvidenceState = evaluation.EvidenceState,
            LikelihoodLevel = evaluation.LikelihoodLevel,
            ImpactLevel = evaluation.ImpactLevel,
            RiskScore = evaluation.RiskScore,
            SeverityBand = evaluation.SeverityBand,
            AssessmentOutcome = OperationalRiskAssessmentOutcomes.Scored,
            LifecycleStatus = OperationalRiskLifecycleStatuses.Detected,
            RuleSetVersion = OperationalRiskConstants.RuleSetVersion,
            SourceModule = snapshot.SourceModule,
            SourceReference = snapshot.SourceReference,
            SourceProvenance = sourceProvenance.Trim(),
            DetectionKey = detectionKey,
            OccurrenceSequence = maxSequence + 1,
            DetectionRuleCode = rule.RuleCode,
            FirstDetectedAtUtc = snapshot.ObservedAtUtc,
            LastObservedAtUtc = snapshot.ObservedAtUtc,
            Version = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            CreatedBy = actor,
            UpdatedBy = actor,
        };

        dbContext.OperationalRiskCases.Add(riskCase);
        AppendDetectionEvidence(context, riskCase, snapshot, evaluation, sourceProvenance, actor, now);
        AddAuditEvent(
            context,
            OperationalRiskAuditEventTypes.CaseCreated,
            "operational-risk-case",
            riskCase.Id,
            new { riskCase.CaseReference, riskCase.DetectionKey, riskCase.OccurrenceSequence },
            actor,
            now);

        runOpenCasesByDetectionKey[detectionKey] = riskCase;
        return riskCase.Id;
    }

    private static void UpdateReconciledCase(
        OperationalRiskCase riskCase,
        OperationalRiskSourceSnapshot snapshot,
        OperationalRiskDetectionEvaluationOutput evaluation,
        string sourceProvenance,
        string actor,
        DateTimeOffset now)
    {
        riskCase.EvidenceState = evaluation.EvidenceState;
        riskCase.LikelihoodLevel = evaluation.LikelihoodLevel;
        riskCase.ImpactLevel = evaluation.ImpactLevel;
        riskCase.RiskScore = evaluation.RiskScore;
        riskCase.SeverityBand = evaluation.SeverityBand;
        riskCase.AssessmentOutcome = OperationalRiskAssessmentOutcomes.Scored;
        riskCase.LastObservedAtUtc = snapshot.ObservedAtUtc > riskCase.LastObservedAtUtc
            ? snapshot.ObservedAtUtc
            : riskCase.LastObservedAtUtc;
        riskCase.UpdatedAtUtc = now;
        riskCase.UpdatedBy = actor;
        _ = sourceProvenance;
    }

    private async Task<Guid?> AppendConflictEvidenceIfKnownAsync(
        OperationalRiskOperationContext context,
        OperationalRiskSourceSnapshot snapshot,
        string detectionKey,
        string sourceProvenance,
        string actor,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var unresolvedCase = await dbContext.OperationalRiskCases
            .SingleOrDefaultAsync(
                c => c.TenantId == context.TenantId
                     && c.FacilityId == context.FacilityId
                     && c.DetectionKey == detectionKey
                     && c.LifecycleStatus != OperationalRiskLifecycleStatuses.Resolved
                     && c.LifecycleStatus != OperationalRiskLifecycleStatuses.Dismissed,
                cancellationToken);

        if (unresolvedCase is null)
        {
            return null;
        }

        var evidence = new OperationalRiskEvidence
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            CaseId = unresolvedCase.Id,
            EvidenceTypeCode = "source-conflict",
            EvidenceState = OperationalRiskEvidenceStates.SourceConflict,
            ObservedAtUtc = snapshot.ObservedAtUtc,
            SourceModule = snapshot.SourceModule,
            SourceReference = snapshot.SourceReference,
            Summary = "Source conflict observed during detection run.",
            NumericValue = snapshot.ActualValue,
            Unit = snapshot.Unit,
            ComparisonReference = snapshot.SourceAssessmentReference,
            SourceProvenance = sourceProvenance.Trim(),
            CreatedAtUtc = now,
            CreatedBy = actor,
        };

        dbContext.OperationalRiskEvidence.Add(evidence);
        unresolvedCase.LastObservedAtUtc = snapshot.ObservedAtUtc;
        unresolvedCase.UpdatedAtUtc = now;
        unresolvedCase.UpdatedBy = actor;

        AddAuditEvent(
            context,
            OperationalRiskAuditEventTypes.EvidenceAppended,
            "operational-risk-evidence",
            evidence.Id,
            new { unresolvedCase.Id, evidence.EvidenceTypeCode },
            actor,
            now);

        return unresolvedCase.Id;
    }

    private void AppendDetectionEvidence(
        OperationalRiskOperationContext context,
        OperationalRiskCase riskCase,
        OperationalRiskSourceSnapshot snapshot,
        OperationalRiskDetectionEvaluationOutput evaluation,
        string sourceProvenance,
        string actor,
        DateTimeOffset now)
    {
        var evidence = new OperationalRiskEvidence
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            CaseId = riskCase.Id,
            EvidenceTypeCode = "cross-module-detection",
            EvidenceState = evaluation.EvidenceState,
            ObservedAtUtc = snapshot.ObservedAtUtc,
            SourceModule = snapshot.SourceModule,
            SourceReference = snapshot.SourceReference,
            Summary = evaluation.LeadingDriverCode ?? "Detection evidence",
            NumericValue = snapshot.ActualValue,
            Unit = snapshot.Unit,
            ComparisonReference = snapshot.SourceAssessmentReference,
            SourceProvenance = sourceProvenance.Trim(),
            CreatedAtUtc = now,
            CreatedBy = actor,
        };

        dbContext.OperationalRiskEvidence.Add(evidence);

        AddAuditEvent(
            context,
            OperationalRiskAuditEventTypes.EvidenceAppended,
            "operational-risk-evidence",
            evidence.Id,
            new { riskCase.Id, evidence.EvidenceTypeCode },
            actor,
            now);
    }

    private static List<OperationalRiskDetectionDriverDraft> OrderDrivers(
        IReadOnlyList<OperationalRiskDetectionDriverDraft> drivers) =>
        drivers
            .OrderByDescending(d => DriverSeverityRank(d.Severity))
            .ThenByDescending(d => d.NormalizedVariance ?? 0m)
            .ThenBy(d => Array.IndexOf(DriverOrder, d.ReasonCode))
            .ToList();

    private static readonly string[] DriverOrder =
    [
        OperationalRiskDetectionDriverReasonCodes.SourceConflict,
        OperationalRiskDetectionDriverReasonCodes.SourceStale,
        OperationalRiskDetectionDriverReasonCodes.MaintenanceRisk,
        OperationalRiskDetectionDriverReasonCodes.ConnectivityRisk,
        OperationalRiskDetectionDriverReasonCodes.EnergyRisk,
        OperationalRiskDetectionDriverReasonCodes.CostDeviationExceeded,
        OperationalRiskDetectionDriverReasonCodes.QualityRisk,
        OperationalRiskDetectionDriverReasonCodes.ProductionContinuityRisk,
        OperationalRiskDetectionDriverReasonCodes.OrderDeliveryDelay,
        OperationalRiskDetectionDriverReasonCodes.SupplierDelay,
        OperationalRiskDetectionDriverReasonCodes.MaterialShortfall,
        OperationalRiskDetectionDriverReasonCodes.MissingThreshold,
        OperationalRiskDetectionDriverReasonCodes.MissingDenominator,
        OperationalRiskDetectionDriverReasonCodes.InsufficientEvidence,
        OperationalRiskDetectionDriverReasonCodes.WithinThreshold,
    ];

    private static int DriverSeverityRank(string severity) =>
        severity switch
        {
            OperationalRiskSeverityBands.Critical => 4,
            OperationalRiskSeverityBands.High => 3,
            OperationalRiskSeverityBands.Moderate => 2,
            OperationalRiskSeverityBands.Low => 1,
            _ => 0,
        };

    private static string BuildCaseTitle(OperationalRiskSourceSnapshot snapshot, OperationalRiskDetectionRule rule) =>
        $"Cross-module risk detected ({rule.DriverReasonCode})";

    private static string BuildCaseDescription(OperationalRiskSourceSnapshot snapshot, OperationalRiskDetectionRule rule) =>
        $"Deterministic detection from {snapshot.SourceModule} outcome {snapshot.SourceOutcome}. " +
        $"Source assessment {snapshot.SourceAssessmentReference}. No guaranteed loss or savings claim.";

    private async Task<string> GenerateRunReferenceAsync(OperationalRiskOperationContext context, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var reference = $"OPR2-RUN-{suffix}";
            var exists = await dbContext.OperationalRiskDetectionRuns.AsNoTracking()
                .AnyAsync(r => r.TenantId == context.TenantId && r.RunReference == reference, ct);
            if (!exists)
            {
                return reference;
            }
        }

        return $"OPR2-RUN-{Guid.NewGuid():N}".ToUpperInvariant();
    }

    private async Task<string> GenerateCaseReferenceAsync(OperationalRiskOperationContext context, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var reference = $"OR-{suffix}";
            var exists = await dbContext.OperationalRiskCases.AsNoTracking()
                .AnyAsync(c => c.TenantId == context.TenantId && c.CaseReference == reference, ct);
            if (!exists)
            {
                return reference;
            }
        }

        return $"OR-{Guid.NewGuid():N}".ToUpperInvariant();
    }

    private static OperationalRiskWriteResult ResolveIdempotentProfileReplay(
        OperationalRiskDetectionProfile existing,
        string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(existing.CreateIdempotencyPayloadFingerprint))
        {
            return OperationalRiskWriteResult.Fail(
                OperationalRiskFailureCodes.IdempotencyLegacyUnverifiable,
                "Existing profile idempotency fingerprint is unavailable.");
        }

        if (!string.Equals(existing.CreateIdempotencyPayloadFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return OperationalRiskWriteResult.Fail(
                OperationalRiskFailureCodes.IdempotencyConflict,
                "Idempotency key was already used with a different payload.");
        }

        return OperationalRiskWriteResult.Success(existing.Id, existing.Version, idempotentReplay: true);
    }

    private static OperationalRiskWriteResult ResolveIdempotentRunReplay(
        OperationalRiskDetectionRun existing,
        string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(existing.IdempotencyPayloadFingerprint))
        {
            return OperationalRiskWriteResult.Fail(
                OperationalRiskFailureCodes.IdempotencyLegacyUnverifiable,
                "Existing run idempotency fingerprint is unavailable.");
        }

        if (!string.Equals(existing.IdempotencyPayloadFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return OperationalRiskWriteResult.Fail(
                OperationalRiskFailureCodes.IdempotencyConflict,
                "Idempotency key was already used with a different payload.");
        }

        return OperationalRiskWriteResult.Success(existing.Id, 0, idempotentReplay: true);
    }

    private static string? ValidateCreateProfileCommand(CreateOperationalRiskDetectionProfileCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ProfileCode))
        {
            return "Profile code is required.";
        }

        if (string.IsNullOrWhiteSpace(command.DisplayName))
        {
            return "Display name is required.";
        }

        if (string.IsNullOrWhiteSpace(command.SourceProvenance))
        {
            return "Source provenance is required.";
        }

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return "Idempotency key is required.";
        }

        if (command.Rules.Count == 0)
        {
            return "At least one detection rule is required.";
        }

        foreach (var rule in command.Rules)
        {
            if (rule.DefaultLikelihood is < 1 or > 5 || rule.DefaultImpact is < 1 or > 5)
            {
                return "Likelihood and impact must be between 1 and 5.";
            }
        }

        return null;
    }

    private static string? ValidateRunCommand(ExecuteOperationalRiskDetectionRunCommand command)
    {
        if (command.WindowEndUtc <= command.WindowStartUtc)
        {
            return "Window end must be greater than window start.";
        }

        if (string.IsNullOrWhiteSpace(command.SourceProvenance))
        {
            return "Source provenance is required.";
        }

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return "Idempotency key is required.";
        }

        return null;
    }

    private void AddAuditEvent(
        OperationalRiskOperationContext context,
        string eventType,
        string entityType,
        Guid entityId,
        object payload,
        string actor,
        DateTimeOffset occurredAtUtc)
    {
        dbContext.OperationalRiskAuditEvents.Add(new OperationalRiskAuditEvent
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

    private static OperationalRiskDetectionProfileReadModel MapProfile(OperationalRiskDetectionProfile profile) =>
        new(
            profile.Id,
            profile.ProfileCode,
            profile.DisplayName,
            profile.Status,
            profile.RuleSetVersion,
            profile.SourceProvenance,
            profile.Version,
            profile.Rules
                .OrderBy(r => r.PriorityOrder)
                .ThenBy(r => r.RuleCode, StringComparer.Ordinal)
                .Select(r => new OperationalRiskDetectionRuleReadModel(
                    r.Id,
                    r.RuleCode,
                    r.SourceModule,
                    r.SourceOutcome,
                    r.RiskCategory,
                    r.DefaultLikelihood,
                    r.DefaultImpact,
                    r.ThresholdVarianceBand,
                    r.DriverReasonCode,
                    r.PriorityOrder,
                    r.IsActive))
                .ToList(),
            IsPilotProfile(profile.Id));

    private static OperationalRiskDetectionRunSummaryReadModel MapRunSummary(OperationalRiskDetectionRun run) =>
        new(
            run.Id,
            run.RunReference,
            run.ProfileId,
            run.Profile?.ProfileCode ?? string.Empty,
            run.WindowStartUtc,
            run.WindowEndUtc,
            run.Status,
            run.SourceCount,
            run.ResultCount,
            run.RuleSetVersion,
            run.CreatedAtUtc,
            run.CompletedAtUtc,
            IsPilotRun(run.Id));

    private static OperationalRiskDetectionRunDetailReadModel MapRunDetail(OperationalRiskDetectionRun run) =>
        new(
            run.Id,
            run.RunReference,
            run.ProfileId,
            run.Profile?.ProfileCode ?? string.Empty,
            run.WindowStartUtc,
            run.WindowEndUtc,
            run.Status,
            run.SourceCount,
            run.ResultCount,
            run.RuleSetVersion,
            run.SourceProvenance,
            run.CreatedAtUtc,
            run.CompletedAtUtc,
            run.Results
                .OrderBy(r => r.SourceModule)
                .ThenBy(r => r.SourceReference, StringComparer.Ordinal)
                .Select(r => new OperationalRiskDetectionResultReadModel(
                    r.Id,
                    r.SourceModule,
                    r.SourceReference,
                    r.RiskCategory,
                    r.ScopeType,
                    r.ScopeReference,
                    r.EvidenceState,
                    r.SourceOutcome,
                    r.DetectionOutcome,
                    r.ActualValue,
                    r.ThresholdValue,
                    r.Unit,
                    r.NormalizedVariance,
                    r.LikelihoodLevel,
                    r.ImpactLevel,
                    r.RiskScore,
                    r.SeverityBand,
                    r.LeadingDriverCode,
                    r.LinkedCaseId,
                    r.DetectionKey,
                    r.SourceAssessmentReference,
                    r.EvaluatedAtUtc,
                    r.Drivers
                        .OrderBy(d => d.SortOrder)
                        .Select(d => new OperationalRiskDetectionDriverReadModel(
                            d.Id,
                            d.ReasonCode,
                            d.Severity,
                            d.ActualValue,
                            d.ThresholdValue,
                            d.Unit,
                            d.NormalizedVariance,
                            d.SupportingText,
                            d.SortOrder))
                        .ToList()))
                .ToList(),
            IsPilotRun(run.Id));

    private static bool IsPilotProfile(Guid id) =>
        id == DevelopmentPilotIds.OperationalRiskDetectionPilotProfileId;

    private static bool IsPilotRun(Guid id) =>
        id == DevelopmentPilotIds.OperationalRiskDetectionPilotBaselineRunId;
}

public sealed class UnavailableOperationalRiskDetectionRepository : IOperationalRiskDetectionRepository
{
    private static OperationalRiskWriteResult Unavailable() =>
        OperationalRiskWriteResult.Fail("operational-risk.unavailable", "Operational risk detection is unavailable.");

    public Task<IReadOnlyList<OperationalRiskDetectionProfileReadModel>> ListProfilesAsync(
        OperationalRiskOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OperationalRiskDetectionProfileReadModel>>([]);

    public Task<OperationalRiskDetectionProfileReadModel?> GetProfileByIdAsync(
        OperationalRiskOperationContext context,
        Guid profileId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<OperationalRiskDetectionProfileReadModel?>(null);

    public Task<OperationalRiskWriteResult> CreateProfileAsync(
        OperationalRiskOperationContext context,
        CreateOperationalRiskDetectionProfileCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<OperationalRiskWriteResult> UpdateProfileAsync(
        OperationalRiskOperationContext context,
        UpdateOperationalRiskDetectionProfileCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<IReadOnlyList<OperationalRiskDetectionRunSummaryReadModel>> ListRunsAsync(
        OperationalRiskOperationContext context,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OperationalRiskDetectionRunSummaryReadModel>>([]);

    public Task<OperationalRiskDetectionRunDetailReadModel?> GetRunByIdAsync(
        OperationalRiskOperationContext context,
        Guid runId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<OperationalRiskDetectionRunDetailReadModel?>(null);

    public Task<OperationalRiskWriteResult> ExecuteRunAsync(
        OperationalRiskOperationContext context,
        ExecuteOperationalRiskDetectionRunCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}
