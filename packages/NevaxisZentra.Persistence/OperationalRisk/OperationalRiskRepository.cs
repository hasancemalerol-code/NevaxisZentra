using System.Text.Json;
using NevaxisZentra.Persistence.Energy;
using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.IotPlc;
using NevaxisZentra.Persistence.Material;
using NevaxisZentra.Persistence.Edge;
using NevaxisZentra.Persistence.Development;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.OperationalRisk;

public interface IOperationalRiskRepository
{
    Task<OperationalRiskCaseListReadModel> ListCasesAsync(
        OperationalRiskOperationContext context,
        OperationalRiskCaseListFilters filters,
        CancellationToken cancellationToken = default);

    Task<OperationalRiskCaseDetailReadModel?> GetCaseByIdAsync(
        OperationalRiskOperationContext context,
        Guid caseId,
        CancellationToken cancellationToken = default);

    Task<OperationalRiskWriteResult> CreateCaseAsync(
        OperationalRiskOperationContext context,
        CreateOperationalRiskCaseCommand command,
        CancellationToken cancellationToken = default);

    Task<OperationalRiskWriteResult> AppendEvidenceAsync(
        OperationalRiskOperationContext context,
        AppendOperationalRiskEvidenceCommand command,
        CancellationToken cancellationToken = default);

    Task<OperationalRiskWriteResult> UpdateAssessmentAsync(
        OperationalRiskOperationContext context,
        UpdateOperationalRiskAssessmentCommand command,
        CancellationToken cancellationToken = default);

    Task<OperationalRiskWriteResult> TransitionCaseAsync(
        OperationalRiskOperationContext context,
        TransitionOperationalRiskCaseCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class OperationalRiskRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IOperationalRiskRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<OperationalRiskCaseListReadModel> ListCasesAsync(
        OperationalRiskOperationContext context,
        OperationalRiskCaseListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var page = filters.Page < 1 ? 1 : filters.Page;
                var pageSize = filters.PageSize is < 1 or > 100 ? 25 : filters.PageSize;

                var query = dbContext.OperationalRiskCases.AsNoTracking()
                    .Where(c => c.TenantId == context.TenantId && c.FacilityId == context.FacilityId);

                if (!string.IsNullOrWhiteSpace(filters.Category))
                {
                    query = query.Where(c => c.Category == filters.Category.Trim());
                }

                if (!string.IsNullOrWhiteSpace(filters.ScopeType))
                {
                    query = query.Where(c => c.ScopeType == filters.ScopeType.Trim());
                }

                if (!string.IsNullOrWhiteSpace(filters.LifecycleStatus))
                {
                    query = query.Where(c => c.LifecycleStatus == filters.LifecycleStatus.Trim());
                }

                if (!string.IsNullOrWhiteSpace(filters.SeverityBand))
                {
                    query = query.Where(c => c.SeverityBand == filters.SeverityBand.Trim());
                }

                if (!string.IsNullOrWhiteSpace(filters.AssessmentOutcome))
                {
                    query = query.Where(c => c.AssessmentOutcome == filters.AssessmentOutcome.Trim());
                }

                var totalCount = await query.CountAsync(ct);
                var items = await query
                    .OrderByDescending(c => c.LastObservedAtUtc)
                    .ThenByDescending(c => c.CreatedAtUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                return new OperationalRiskCaseListReadModel(
                    items.Select(MapSummary).ToList(),
                    totalCount,
                    page,
                    pageSize);
            },
            cancellationToken);

    public Task<OperationalRiskCaseDetailReadModel?> GetCaseByIdAsync(
        OperationalRiskOperationContext context,
        Guid caseId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var riskCase = await dbContext.OperationalRiskCases.AsNoTracking()
                    .Include(c => c.Evidence)
                    .Include(c => c.StatusHistory)
                    .SingleOrDefaultAsync(
                        c => c.TenantId == context.TenantId
                             && c.FacilityId == context.FacilityId
                             && c.Id == caseId,
                        ct);

                return riskCase is null ? null : MapDetail(riskCase);
            },
            cancellationToken);

    public Task<OperationalRiskWriteResult> CreateCaseAsync(
        OperationalRiskOperationContext context,
        CreateOperationalRiskCaseCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateCreateCommand(command);
                if (validationError is not null)
                {
                    return OperationalRiskWriteResult.Fail(OperationalRiskFailureCodes.ValidationFailed, validationError);
                }

                var fingerprint = OperationalRiskIdempotencyFingerprint.ComputeCaseFingerprint(context, command);
                var existing = await dbContext.OperationalRiskCases.AsNoTracking()
                    .SingleOrDefaultAsync(
                        c => c.TenantId == context.TenantId && c.IdempotencyKey == command.IdempotencyKey.Trim(),
                        ct);

                if (existing is not null)
                {
                    return ResolveIdempotentCaseReplay(existing, fingerprint);
                }

                var scopeError = await ValidateScopeAsync(context, command.ScopeType, command.ScopeReference, ct);
                if (scopeError is not null)
                {
                    return OperationalRiskWriteResult.Fail(OperationalRiskFailureCodes.InvalidScope, scopeError);
                }

                var facilityExists = await dbContext.Facilities.AsNoTracking()
                    .AnyAsync(
                        f => f.TenantId == context.TenantId && f.Id == context.FacilityId,
                        ct);
                if (!facilityExists)
                {
                    return OperationalRiskWriteResult.Fail(
                        OperationalRiskFailureCodes.FacilityScopeDenied,
                        "Facility not found.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var assessment = OperationalRiskAssessor.Assess(
                    command.EvidenceState.Trim(),
                    command.LikelihoodLevel,
                    command.ImpactLevel);

                var riskCase = new OperationalRiskCase
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    CaseReference = await GenerateCaseReferenceAsync(context, ct),
                    Category = command.Category.Trim(),
                    ScopeType = command.ScopeType.Trim(),
                    ScopeReference = TrimOrNull(command.ScopeReference),
                    Title = command.Title.Trim(),
                    Description = command.Description.Trim(),
                    EvidenceState = command.EvidenceState.Trim(),
                    LikelihoodLevel = assessment.LikelihoodLevel,
                    ImpactLevel = assessment.ImpactLevel,
                    RiskScore = assessment.RiskScore,
                    SeverityBand = assessment.SeverityBand,
                    AssessmentOutcome = assessment.AssessmentOutcome,
                    LifecycleStatus = OperationalRiskLifecycleStatuses.Detected,
                    RuleSetVersion = OperationalRiskConstants.RuleSetVersion,
                    SourceModule = TrimOrNull(command.SourceModule),
                    SourceReference = TrimOrNull(command.SourceReference),
                    SourceProvenance = command.SourceProvenance.Trim(),
                    FirstDetectedAtUtc = command.FirstDetectedAtUtc,
                    LastObservedAtUtc = command.LastObservedAtUtc,
                    IdempotencyKey = command.IdempotencyKey.Trim(),
                    IdempotencyPayloadFingerprint = fingerprint,
                    Version = 1,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedBy = actor,
                    UpdatedBy = actor,
                };

                dbContext.OperationalRiskCases.Add(riskCase);
                AddAuditEvent(
                    context,
                    OperationalRiskAuditEventTypes.CaseCreated,
                    "operational-risk-case",
                    riskCase.Id,
                    new { riskCase.CaseReference, riskCase.Category, riskCase.ScopeType },
                    actor,
                    now);

                await dbContext.SaveChangesAsync(ct);
                return OperationalRiskWriteResult.Success(riskCase.Id, riskCase.Version);
            },
            cancellationToken);

    public Task<OperationalRiskWriteResult> AppendEvidenceAsync(
        OperationalRiskOperationContext context,
        AppendOperationalRiskEvidenceCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateEvidenceCommand(command);
                if (validationError is not null)
                {
                    return OperationalRiskWriteResult.Fail(OperationalRiskFailureCodes.ValidationFailed, validationError);
                }

                var fingerprint = OperationalRiskIdempotencyFingerprint.ComputeEvidenceFingerprint(context, command);
                var existing = await dbContext.OperationalRiskEvidence.AsNoTracking()
                    .SingleOrDefaultAsync(
                        e => e.TenantId == context.TenantId && e.IdempotencyKey == command.IdempotencyKey.Trim(),
                        ct);

                if (existing is not null)
                {
                    return ResolveIdempotentEvidenceReplay(existing, fingerprint);
                }

                var riskCase = await dbContext.OperationalRiskCases
                    .SingleOrDefaultAsync(
                        c => c.TenantId == context.TenantId
                             && c.FacilityId == context.FacilityId
                             && c.Id == command.CaseId,
                        ct);

                if (riskCase is null)
                {
                    return OperationalRiskWriteResult.Fail(OperationalRiskFailureCodes.NotFound, "Risk case not found.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var evidence = new OperationalRiskEvidence
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    CaseId = riskCase.Id,
                    EvidenceTypeCode = command.EvidenceTypeCode.Trim(),
                    EvidenceState = command.EvidenceState.Trim(),
                    ObservedAtUtc = command.ObservedAtUtc,
                    SourceModule = TrimOrNull(command.SourceModule),
                    SourceReference = TrimOrNull(command.SourceReference),
                    Summary = command.Summary.Trim(),
                    NumericValue = command.NumericValue,
                    Unit = TrimOrNull(command.Unit),
                    ComparisonReference = TrimOrNull(command.ComparisonReference),
                    SourceProvenance = command.SourceProvenance.Trim(),
                    IdempotencyKey = command.IdempotencyKey.Trim(),
                    IdempotencyPayloadFingerprint = fingerprint,
                    CreatedAtUtc = now,
                    CreatedBy = actor,
                };

                dbContext.OperationalRiskEvidence.Add(evidence);
                riskCase.LastObservedAtUtc = command.ObservedAtUtc > riskCase.LastObservedAtUtc
                    ? command.ObservedAtUtc
                    : riskCase.LastObservedAtUtc;
                riskCase.UpdatedAtUtc = now;
                riskCase.UpdatedBy = actor;

                AddAuditEvent(
                    context,
                    OperationalRiskAuditEventTypes.EvidenceAppended,
                    "operational-risk-evidence",
                    evidence.Id,
                    new { riskCase.Id, evidence.EvidenceTypeCode, evidence.EvidenceState },
                    actor,
                    now);

                await dbContext.SaveChangesAsync(ct);
                return OperationalRiskWriteResult.Success(evidence.Id, riskCase.Version);
            },
            cancellationToken);

    public Task<OperationalRiskWriteResult> UpdateAssessmentAsync(
        OperationalRiskOperationContext context,
        UpdateOperationalRiskAssessmentCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateAssessmentCommand(command);
                if (validationError is not null)
                {
                    return OperationalRiskWriteResult.Fail(OperationalRiskFailureCodes.ValidationFailed, validationError);
                }

                var riskCase = await dbContext.OperationalRiskCases
                    .SingleOrDefaultAsync(
                        c => c.TenantId == context.TenantId
                             && c.FacilityId == context.FacilityId
                             && c.Id == command.CaseId,
                        ct);

                if (riskCase is null)
                {
                    return OperationalRiskWriteResult.Fail(OperationalRiskFailureCodes.NotFound, "Risk case not found.");
                }

                if (riskCase.Version != command.ExpectedVersion)
                {
                    return OperationalRiskWriteResult.Fail(
                        OperationalRiskFailureCodes.ConcurrencyConflict,
                        "Risk case was modified by another request.");
                }

                if (OperationalRiskLifecycle.IsTerminal(riskCase.LifecycleStatus))
                {
                    return OperationalRiskWriteResult.Fail(
                        OperationalRiskFailureCodes.TerminalState,
                        "Terminal risk cases cannot be updated.");
                }

                var assessment = OperationalRiskAssessor.Assess(
                    command.EvidenceState.Trim(),
                    command.LikelihoodLevel,
                    command.ImpactLevel);

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                riskCase.EvidenceState = command.EvidenceState.Trim();
                riskCase.LikelihoodLevel = assessment.LikelihoodLevel;
                riskCase.ImpactLevel = assessment.ImpactLevel;
                riskCase.RiskScore = assessment.RiskScore;
                riskCase.SeverityBand = assessment.SeverityBand;
                riskCase.AssessmentOutcome = assessment.AssessmentOutcome;
                riskCase.RuleSetVersion = OperationalRiskConstants.RuleSetVersion;
                if (command.LastObservedAtUtc.HasValue)
                {
                    riskCase.LastObservedAtUtc = command.LastObservedAtUtc.Value;
                }

                riskCase.UpdatedAtUtc = now;
                riskCase.UpdatedBy = actor;
                riskCase.Version++;

                AddAuditEvent(
                    context,
                    OperationalRiskAuditEventTypes.CaseAssessmentUpdated,
                    "operational-risk-case",
                    riskCase.Id,
                    new
                    {
                        riskCase.AssessmentOutcome,
                        riskCase.RiskScore,
                        riskCase.SeverityBand,
                        riskCase.EvidenceState,
                    },
                    actor,
                    now);

                await dbContext.SaveChangesAsync(ct);
                return OperationalRiskWriteResult.Success(riskCase.Id, riskCase.Version);
            },
            cancellationToken);

    public Task<OperationalRiskWriteResult> TransitionCaseAsync(
        OperationalRiskOperationContext context,
        TransitionOperationalRiskCaseCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                if (string.IsNullOrWhiteSpace(command.TargetStatus)
                    || !OperationalRiskLifecycleStatuses.All.Contains(command.TargetStatus.Trim()))
                {
                    return OperationalRiskWriteResult.Fail(
                        OperationalRiskFailureCodes.ValidationFailed,
                        "A valid target status is required.");
                }

                var targetStatus = command.TargetStatus.Trim();
                var noteError = OperationalRiskLifecycle.ValidateTransitionNote(targetStatus, command.Note);
                if (noteError is not null)
                {
                    return OperationalRiskWriteResult.Fail(OperationalRiskFailureCodes.ValidationFailed, noteError);
                }

                var riskCase = await dbContext.OperationalRiskCases
                    .SingleOrDefaultAsync(
                        c => c.TenantId == context.TenantId
                             && c.FacilityId == context.FacilityId
                             && c.Id == command.CaseId,
                        ct);

                if (riskCase is null)
                {
                    return OperationalRiskWriteResult.Fail(OperationalRiskFailureCodes.NotFound, "Risk case not found.");
                }

                if (riskCase.Version != command.ExpectedVersion)
                {
                    return OperationalRiskWriteResult.Fail(
                        OperationalRiskFailureCodes.ConcurrencyConflict,
                        "Risk case was modified by another request.");
                }

                if (!OperationalRiskLifecycle.CanTransition(riskCase.LifecycleStatus, targetStatus))
                {
                    return OperationalRiskWriteResult.Fail(
                        OperationalRiskFailureCodes.InvalidTransition,
                        $"Transition from '{riskCase.LifecycleStatus}' to '{targetStatus}' is not allowed.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var previousStatus = riskCase.LifecycleStatus;
                riskCase.LifecycleStatus = targetStatus;
                riskCase.UpdatedAtUtc = now;
                riskCase.UpdatedBy = actor;
                riskCase.Version++;

                var history = new OperationalRiskStatusHistory
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    CaseId = riskCase.Id,
                    PreviousStatus = previousStatus,
                    NewStatus = targetStatus,
                    ReasonNote = TrimOrNull(command.Note),
                    ActorSubjectId = actor,
                    OccurredAtUtc = now,
                    ExpectedCaseVersion = command.ExpectedVersion,
                    SourceProvenance = command.SourceProvenance.Trim(),
                };

                dbContext.OperationalRiskStatusHistory.Add(history);
                AddAuditEvent(
                    context,
                    OperationalRiskAuditEventTypes.CaseTransitioned,
                    "operational-risk-case",
                    riskCase.Id,
                    new { previousStatus, newStatus = targetStatus, command.Note },
                    actor,
                    now);

                await dbContext.SaveChangesAsync(ct);
                return OperationalRiskWriteResult.Success(riskCase.Id, riskCase.Version);
            },
            cancellationToken);

    private static OperationalRiskWriteResult ResolveIdempotentCaseReplay(OperationalRiskCase existing, string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(existing.IdempotencyPayloadFingerprint))
        {
            return OperationalRiskWriteResult.Fail(
                OperationalRiskFailureCodes.IdempotencyLegacyUnverifiable,
                "Existing case cannot be verified for idempotent replay.");
        }

        if (!string.Equals(existing.IdempotencyPayloadFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return OperationalRiskWriteResult.Fail(
                OperationalRiskFailureCodes.IdempotencyConflict,
                "Idempotency key was already used with a different payload.");
        }

        return OperationalRiskWriteResult.Success(existing.Id, existing.Version, idempotentReplay: true);
    }

    private static OperationalRiskWriteResult ResolveIdempotentEvidenceReplay(OperationalRiskEvidence existing, string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(existing.IdempotencyPayloadFingerprint))
        {
            return OperationalRiskWriteResult.Fail(
                OperationalRiskFailureCodes.IdempotencyLegacyUnverifiable,
                "Existing evidence cannot be verified for idempotent replay.");
        }

        if (!string.Equals(existing.IdempotencyPayloadFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return OperationalRiskWriteResult.Fail(
                OperationalRiskFailureCodes.IdempotencyConflict,
                "Idempotency key was already used with a different payload.");
        }

        return OperationalRiskWriteResult.Success(existing.Id, 0, idempotentReplay: true);
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

    private async Task<string?> ValidateScopeAsync(
        OperationalRiskOperationContext context,
        string scopeType,
        string? scopeReference,
        CancellationToken ct)
    {
        var normalizedScope = scopeType.Trim();
        if (!OperationalRiskScopeTypes.All.Contains(normalizedScope))
        {
            return "Scope type is not supported.";
        }

        if (OperationalRiskScopeTypes.RequiresScopeReference(normalizedScope)
            && string.IsNullOrWhiteSpace(scopeReference))
        {
            return "A canonical scope reference is required for the selected scope.";
        }

        if (string.Equals(normalizedScope, OperationalRiskScopeTypes.Facility, StringComparison.Ordinal))
        {
            return null;
        }

        var reference = scopeReference!.Trim();

        return normalizedScope switch
        {
            OperationalRiskScopeTypes.Material => await ValidateMaterialScopeAsync(context, reference, ct),
            OperationalRiskScopeTypes.Machine => await ValidateMachineScopeAsync(context, reference, ct),
            OperationalRiskScopeTypes.EnergyMeasurementPoint => await ValidateEnergyPointScopeAsync(context, reference, ct),
            OperationalRiskScopeTypes.IotEndpoint => await ValidateIotEndpointScopeAsync(context, reference, ct),
            _ => null,
        };
    }

    private async Task<string?> ValidateMaterialScopeAsync(
        OperationalRiskOperationContext context,
        string reference,
        CancellationToken ct)
    {
        if (!Guid.TryParse(reference, out var materialId))
        {
            return "Material scope reference must be a canonical material identifier.";
        }

        var exists = await dbContext.Materials.AsNoTracking()
            .AnyAsync(
                m => m.TenantId == context.TenantId
                     && m.FacilityId == context.FacilityId
                     && m.Id == materialId,
                ct);

        return exists ? null : "Scope reference is not available in this facility.";
    }

    private async Task<string?> ValidateMachineScopeAsync(
        OperationalRiskOperationContext context,
        string reference,
        CancellationToken ct)
    {
        if (!Guid.TryParse(reference, out var machineId))
        {
            return "Machine scope reference must be a canonical machine identifier.";
        }

        var exists = await dbContext.MachineRegistries.AsNoTracking()
            .AnyAsync(
                m => m.TenantId == context.TenantId
                     && m.FacilityId == context.FacilityId
                     && m.Id == machineId,
                ct);

        return exists ? null : "Scope reference is not available in this facility.";
    }

    private async Task<string?> ValidateEnergyPointScopeAsync(
        OperationalRiskOperationContext context,
        string reference,
        CancellationToken ct)
    {
        var exists = await dbContext.EnergyMeasurementPoints.AsNoTracking()
            .AnyAsync(
                p => p.TenantId == context.TenantId
                     && p.FacilityId == context.FacilityId
                     && (p.PointCode == reference || p.Id.ToString() == reference),
                ct);

        return exists ? null : "Scope reference is not available in this facility.";
    }

    private async Task<string?> ValidateIotEndpointScopeAsync(
        OperationalRiskOperationContext context,
        string reference,
        CancellationToken ct)
    {
        var exists = await dbContext.IotPlcEndpoints.AsNoTracking()
            .AnyAsync(
                e => e.TenantId == context.TenantId
                     && e.FacilityId == context.FacilityId
                     && (e.EndpointCode == reference || e.Id.ToString() == reference),
                ct);

        return exists ? null : "Scope reference is not available in this facility.";
    }

    private static string? ValidateCreateCommand(CreateOperationalRiskCaseCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return "Idempotency key is required.";
        }

        if (string.IsNullOrWhiteSpace(command.Category) || !OperationalRiskCategories.All.Contains(command.Category.Trim()))
        {
            return "A valid category is required.";
        }

        if (string.IsNullOrWhiteSpace(command.ScopeType))
        {
            return "Scope type is required.";
        }

        if (string.IsNullOrWhiteSpace(command.Title))
        {
            return "Title is required.";
        }

        if (string.IsNullOrWhiteSpace(command.Description))
        {
            return "Description is required.";
        }

        if (string.IsNullOrWhiteSpace(command.EvidenceState)
            || !OperationalRiskEvidenceStates.All.Contains(command.EvidenceState.Trim()))
        {
            return "A valid evidence state is required.";
        }

        if (string.IsNullOrWhiteSpace(command.SourceProvenance))
        {
            return "Source provenance is required.";
        }

        if (command.LastObservedAtUtc < command.FirstDetectedAtUtc)
        {
            return "Last observed timestamp must be on or after first detected timestamp.";
        }

        return null;
    }

    private static string? ValidateEvidenceCommand(AppendOperationalRiskEvidenceCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return "Idempotency key is required.";
        }

        if (string.IsNullOrWhiteSpace(command.EvidenceTypeCode))
        {
            return "Evidence type code is required.";
        }

        if (string.IsNullOrWhiteSpace(command.EvidenceState)
            || !OperationalRiskEvidenceStates.All.Contains(command.EvidenceState.Trim()))
        {
            return "A valid evidence state is required.";
        }

        if (string.IsNullOrWhiteSpace(command.Summary))
        {
            return "Evidence summary is required.";
        }

        if (string.IsNullOrWhiteSpace(command.SourceProvenance))
        {
            return "Source provenance is required.";
        }

        if (command.ObservedAtUtc == default)
        {
            return "Observed UTC timestamp is required.";
        }

        return null;
    }

    private static string? ValidateAssessmentCommand(UpdateOperationalRiskAssessmentCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.EvidenceState)
            || !OperationalRiskEvidenceStates.All.Contains(command.EvidenceState.Trim()))
        {
            return "A valid evidence state is required.";
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

    private static OperationalRiskCaseSummaryReadModel MapSummary(OperationalRiskCase riskCase) =>
        new(
            riskCase.Id,
            riskCase.CaseReference,
            riskCase.Category,
            riskCase.ScopeType,
            riskCase.ScopeReference,
            riskCase.Title,
            riskCase.EvidenceState,
            riskCase.LikelihoodLevel,
            riskCase.ImpactLevel,
            riskCase.RiskScore,
            riskCase.SeverityBand,
            riskCase.AssessmentOutcome,
            riskCase.LifecycleStatus,
            riskCase.RuleSetVersion,
            riskCase.FirstDetectedAtUtc,
            riskCase.LastObservedAtUtc,
            riskCase.Version,
            IsPilotData(riskCase.Id));

    private static OperationalRiskCaseDetailReadModel MapDetail(OperationalRiskCase riskCase) =>
        new(
            riskCase.Id,
            riskCase.CaseReference,
            riskCase.Category,
            riskCase.ScopeType,
            riskCase.ScopeReference,
            riskCase.Title,
            riskCase.Description,
            riskCase.EvidenceState,
            riskCase.LikelihoodLevel,
            riskCase.ImpactLevel,
            riskCase.RiskScore,
            riskCase.SeverityBand,
            riskCase.AssessmentOutcome,
            riskCase.LifecycleStatus,
            riskCase.RuleSetVersion,
            riskCase.SourceModule,
            riskCase.SourceReference,
            riskCase.SourceProvenance,
            riskCase.FirstDetectedAtUtc,
            riskCase.LastObservedAtUtc,
            riskCase.Version,
            riskCase.Evidence
                .OrderByDescending(e => e.ObservedAtUtc)
                .Select(e => new OperationalRiskEvidenceReadModel(
                    e.Id,
                    e.EvidenceTypeCode,
                    e.EvidenceState,
                    e.ObservedAtUtc,
                    e.SourceModule,
                    e.SourceReference,
                    e.Summary,
                    e.NumericValue,
                    e.Unit,
                    e.ComparisonReference,
                    e.SourceProvenance,
                    e.CreatedAtUtc,
                    IsPilotData(e.Id)))
                .ToList(),
            riskCase.StatusHistory
                .OrderBy(h => h.OccurredAtUtc)
                .Select(h => new OperationalRiskStatusHistoryReadModel(
                    h.Id,
                    h.PreviousStatus,
                    h.NewStatus,
                    h.ReasonNote,
                    h.ActorSubjectId,
                    h.OccurredAtUtc,
                    h.ExpectedCaseVersion,
                    h.SourceProvenance))
                .ToList(),
            IsPilotData(riskCase.Id));

    private static bool IsPilotData(Guid id) =>
        id == DevelopmentPilotIds.OperationalRiskPilotMaterialCaseId
        || id == DevelopmentPilotIds.OperationalRiskPilotSupplierCaseId
        || id == DevelopmentPilotIds.OperationalRiskPilotOrderDeliveryCaseId
        || id == DevelopmentPilotIds.OperationalRiskPilotCostDeviationCaseId
        || id == DevelopmentPilotIds.OperationalRiskPilotInsufficientDataCaseId
        || id == DevelopmentPilotIds.OperationalRiskPilotSourceConflictCaseId
        || id == DevelopmentPilotIds.OperationalRiskPilotResolvedCaseId;

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class UnavailableOperationalRiskRepository : IOperationalRiskRepository
{
    private static OperationalRiskWriteResult Unavailable() =>
        OperationalRiskWriteResult.Fail("operational-risk.unavailable", "Operational risk persistence is unavailable.");

    public Task<OperationalRiskCaseListReadModel> ListCasesAsync(
        OperationalRiskOperationContext context,
        OperationalRiskCaseListFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new OperationalRiskCaseListReadModel([], 0, 1, 25));

    public Task<OperationalRiskCaseDetailReadModel?> GetCaseByIdAsync(
        OperationalRiskOperationContext context,
        Guid caseId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<OperationalRiskCaseDetailReadModel?>(null);

    public Task<OperationalRiskWriteResult> CreateCaseAsync(
        OperationalRiskOperationContext context,
        CreateOperationalRiskCaseCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<OperationalRiskWriteResult> AppendEvidenceAsync(
        OperationalRiskOperationContext context,
        AppendOperationalRiskEvidenceCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<OperationalRiskWriteResult> UpdateAssessmentAsync(
        OperationalRiskOperationContext context,
        UpdateOperationalRiskAssessmentCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<OperationalRiskWriteResult> TransitionCaseAsync(
        OperationalRiskOperationContext context,
        TransitionOperationalRiskCaseCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}
