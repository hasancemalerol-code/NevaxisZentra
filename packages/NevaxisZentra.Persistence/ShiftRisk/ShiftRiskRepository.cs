using System.Globalization;
using System.Text.Json;
using NevaxisZentra.Persistence.Edge;
using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.Production;
using NevaxisZentra.Persistence.ShiftAssignment;
using NevaxisZentra.Persistence.ShiftWorkforce;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.ShiftRisk;

public interface IShiftRiskRepository
{
    Task<ShiftRiskEvaluationContextBuildResult> BuildEvaluationContextAsync(
        ShiftRiskOperationContext context,
        Guid productionShiftId,
        DateOnly shiftDate,
        CancellationToken cancellationToken = default);

    Task<ShiftRiskRequirementListReadModel> ListRequirementsAsync(
        ShiftRiskOperationContext context,
        ShiftRiskRequirementListFilters filters,
        CancellationToken cancellationToken = default);

    Task<ShiftRiskRequirementRecordReadModel?> GetRequirementByIdAsync(
        ShiftRiskOperationContext context,
        Guid requirementId,
        CancellationToken cancellationToken = default);

    Task<ShiftRiskWriteResult> CreateRequirementAsync(
        ShiftRiskOperationContext context,
        CreateShiftCoverageRequirementCommand command,
        CancellationToken cancellationToken = default);

    Task<ShiftRiskWriteResult> UpdateRequirementAsync(
        ShiftRiskOperationContext context,
        UpdateShiftCoverageRequirementCommand command,
        CancellationToken cancellationToken = default);

    Task<ShiftRiskWriteResult> EvaluateRiskAsync(
        ShiftRiskOperationContext context,
        EvaluateShiftRiskCommand command,
        CancellationToken cancellationToken = default);

    Task<ShiftRiskWriteResult> EvaluateWorkforceContinuityAsync(
        ShiftRiskOperationContext context,
        EvaluateWorkforceContinuityCommand command,
        CancellationToken cancellationToken = default);

    Task<ShiftContinuityOverviewReadModel> GetContinuityOverviewAsync(
        ShiftRiskOperationContext context,
        CancellationToken cancellationToken = default);

    Task<ShiftContinuityAssessmentListReadModel> ListContinuityAssessmentsAsync(
        ShiftRiskOperationContext context,
        ShiftContinuityAssessmentListFilters filters,
        CancellationToken cancellationToken = default);

    Task<ShiftContinuityAssessmentDetailReadModel?> GetContinuityAssessmentByIdAsync(
        ShiftRiskOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task<ShiftRiskOverviewReadModel> GetOverviewAsync(
        ShiftRiskOperationContext context,
        CancellationToken cancellationToken = default);

    Task<ShiftRiskAssessmentListReadModel> ListAssessmentsAsync(
        ShiftRiskOperationContext context,
        ShiftRiskAssessmentListFilters filters,
        CancellationToken cancellationToken = default);

    Task<ShiftRiskAssessmentDetailReadModel?> GetAssessmentByIdAsync(
        ShiftRiskOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShiftRiskAuditEventReadModel>> ListAuditEventsAsync(
        ShiftRiskOperationContext context,
        string? entityType,
        Guid? entityId,
        CancellationToken cancellationToken = default);
}

public sealed partial class ShiftRiskRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IShiftRiskRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<ShiftRiskEvaluationContextBuildResult> BuildEvaluationContextAsync(
        ShiftRiskOperationContext context,
        Guid productionShiftId,
        DateOnly shiftDate,
        CancellationToken cancellationToken = default) =>
        BuildEvaluationContextCoreAsync(context, productionShiftId, shiftDate, cancellationToken);

    private async Task<ShiftRiskEvaluationContextBuildResult> BuildEvaluationContextCoreAsync(
        ShiftRiskOperationContext context,
        Guid productionShiftId,
        DateOnly shiftDate,
        CancellationToken cancellationToken)
    {
        var facility = await dbContext.Facilities.AsNoTracking()
            .SingleOrDefaultAsync(
                f => f.TenantId == context.TenantId && f.Id == context.FacilityId,
                cancellationToken);

        if (facility is null)
        {
            return new ShiftRiskEvaluationContextBuildResult(
                false, null, ShiftRiskFailureCodes.InvalidScope, "Facility not found.");
        }

        var shift = await dbContext.ProductionShifts.AsNoTracking()
            .SingleOrDefaultAsync(
                s => s.TenantId == context.TenantId
                     && s.FacilityId == context.FacilityId
                     && s.Id == productionShiftId,
                cancellationToken);

        if (shift is null)
        {
            return new ShiftRiskEvaluationContextBuildResult(
                false, null, ShiftRiskFailureCodes.ShiftNotFound, "Production shift not found.");
        }

        var (startUtc, endUtc) = ShiftAssignmentShiftWindowCalculator.ComputeShiftWindowUtc(
            facility, shift, shiftDate);

        return new ShiftRiskEvaluationContextBuildResult(
            true,
            new ShiftRiskEvaluationContext(
                productionShiftId,
                shiftDate,
                startUtc,
                endUtc,
                null,
                null,
                null,
                null),
            null,
            null);
    }

    public Task<ShiftRiskRequirementListReadModel> ListRequirementsAsync(
        ShiftRiskOperationContext context,
        ShiftRiskRequirementListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var page = filters.Page < 1 ? 1 : filters.Page;
                var pageSize = filters.PageSize is < 1 or > 100 ? 25 : filters.PageSize;

                var query = dbContext.ShiftCoverageRequirements.AsNoTracking()
                    .Where(r => r.TenantId == context.TenantId && r.FacilityId == context.FacilityId);

                if (filters.ProductionShiftId.HasValue)
                {
                    query = query.Where(r => r.ProductionShiftId == filters.ProductionShiftId.Value);
                }

                if (!string.IsNullOrWhiteSpace(filters.ProductionOrderReference))
                {
                    var reference = filters.ProductionOrderReference.Trim();
                    query = query.Where(r => r.ProductionOrderReference == reference);
                }

                if (filters.MachineAssetId.HasValue)
                {
                    query = query.Where(r => r.MachineAssetId == filters.MachineAssetId.Value);
                }

                if (filters.IsActive.HasValue)
                {
                    query = query.Where(r => r.IsActive == filters.IsActive.Value);
                }

                var totalCount = await query.CountAsync(ct);
                var requirements = await query
                    .OrderByDescending(r => r.ShiftDate)
                    .ThenByDescending(r => r.UpdatedAtUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                var shiftLookup = await LoadShiftLookupAsync(context, ct);
                var machineLookup = await LoadMachineLookupAsync(context, ct);
                var competencyLabels = await LoadCompetencyLabelsAsync(context, ct);

                var items = requirements
                    .Select(r => MapRequirementRecord(r, shiftLookup, machineLookup, competencyLabels))
                    .ToList();

                return new ShiftRiskRequirementListReadModel(items, totalCount, page, pageSize);
            },
            cancellationToken);

    public Task<ShiftRiskRequirementRecordReadModel?> GetRequirementByIdAsync(
        ShiftRiskOperationContext context,
        Guid requirementId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var requirement = await dbContext.ShiftCoverageRequirements.AsNoTracking()
                    .FirstOrDefaultAsync(
                        r => r.Id == requirementId
                             && r.TenantId == context.TenantId
                             && r.FacilityId == context.FacilityId,
                        ct);

                if (requirement is null)
                {
                    return null;
                }

                var shiftLookup = await LoadShiftLookupAsync(context, ct);
                var machineLookup = await LoadMachineLookupAsync(context, ct);
                var competencyLabels = await LoadCompetencyLabelsAsync(context, ct);
                return MapRequirementRecord(requirement, shiftLookup, machineLookup, competencyLabels);
            },
            cancellationToken);

    public Task<ShiftRiskWriteResult> CreateRequirementAsync(
        ShiftRiskOperationContext context,
        CreateShiftCoverageRequirementCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateCreateRequirement(command);
                if (validationError is not null)
                {
                    return ShiftRiskWriteResult.Fail(ShiftRiskFailureCodes.ValidationFailed, validationError);
                }

                if (!DateOnly.TryParse(command.ShiftDate, out var shiftDate))
                {
                    return ShiftRiskWriteResult.Fail(
                        ShiftRiskFailureCodes.ValidationFailed,
                        "ShiftDate must be a valid date.");
                }

                var fingerprint = ShiftRiskIdempotencyFingerprint.ComputeCreateRequirementFingerprint(context, command);
                var existing = await dbContext.ShiftCoverageRequirements
                    .FirstOrDefaultAsync(
                        r => r.TenantId == context.TenantId && r.CreateIdempotencyKey == command.IdempotencyKey.Trim(),
                        ct);

                if (existing is not null)
                {
                    return ResolveCreateIdempotency(existing, fingerprint);
                }

                var buildResult = await BuildEvaluationContextCoreAsync(
                    context, command.ProductionShiftId, shiftDate, ct);
                if (!buildResult.Succeeded || buildResult.Context is null)
                {
                    return ShiftRiskWriteResult.Fail(
                        buildResult.FailureCode ?? ShiftRiskFailureCodes.ValidationFailed,
                        buildResult.FailureReason ?? "Evaluation context build failed.");
                }

                if (command.MachineAssetId.HasValue && command.MachineAssetId != Guid.Empty)
                {
                    var machineExists = await dbContext.MachineRegistries.AsNoTracking()
                        .AnyAsync(
                            m => m.Id == command.MachineAssetId.Value
                                 && m.TenantId == context.TenantId
                                 && m.FacilityId == context.FacilityId,
                            ct);
                    if (!machineExists)
                    {
                        return ShiftRiskWriteResult.Fail(
                            ShiftRiskFailureCodes.MachineNotFound,
                            "Machine asset not found.");
                    }
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var evalContext = buildResult.Context;

                var requirement = new ShiftCoverageRequirement
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    ProductionShiftId = command.ProductionShiftId,
                    ShiftDate = shiftDate,
                    PlannedStartUtc = evalContext.PlannedStartUtc,
                    PlannedEndUtc = evalContext.PlannedEndUtc,
                    ProductionOrderReference = TrimOrNull(command.ProductionOrderReference),
                    MachineAssetId = command.MachineAssetId is null || command.MachineAssetId == Guid.Empty
                        ? null
                        : command.MachineAssetId,
                    MachineFamilyScope = TrimOrNull(command.MachineFamilyScope),
                    RequiredCompetencyCode = command.RequiredCompetencyCode.Trim(),
                    MinimumApprovedOperatorCount = command.MinimumApprovedOperatorCount,
                    IsCriticalRole = command.IsCriticalRole,
                    SourceReference = command.SourceReference.Trim(),
                    EffectiveFrom = ParseDateOrNull(command.EffectiveFrom),
                    EffectiveUntil = ParseDateOrNull(command.EffectiveUntil),
                    IsActive = command.IsActive,
                    CreateIdempotencyKey = command.IdempotencyKey.Trim(),
                    CreateIdempotencyPayloadFingerprint = fingerprint,
                    Version = 1,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedBy = actor,
                    UpdatedBy = actor,
                };

                dbContext.ShiftCoverageRequirements.Add(requirement);
                AddAuditEvent(
                    context,
                    requirement.Id,
                    ShiftRiskEntityTypes.CoverageRequirement,
                    ShiftRiskAuditEventTypes.RequirementCreated,
                    actor,
                    now,
                    new { requirementId = requirement.Id });

                await dbContext.SaveChangesAsync(ct);
                return ShiftRiskWriteResult.Success(requirement.Id, requirement.Version);
            },
            cancellationToken);

    public Task<ShiftRiskWriteResult> UpdateRequirementAsync(
        ShiftRiskOperationContext context,
        UpdateShiftCoverageRequirementCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var requirement = await dbContext.ShiftCoverageRequirements
                    .FirstOrDefaultAsync(
                        r => r.Id == command.RequirementId
                             && r.TenantId == context.TenantId
                             && r.FacilityId == context.FacilityId,
                        ct);

                if (requirement is null)
                {
                    return ShiftRiskWriteResult.Fail(ShiftRiskFailureCodes.NotFound, "Requirement not found.");
                }

                if (requirement.Version != command.ExpectedVersion)
                {
                    return ShiftRiskWriteResult.Fail(
                        ShiftRiskFailureCodes.ConcurrencyConflict,
                        "Requirement version mismatch.");
                }

                if (command.MinimumApprovedOperatorCount < 1
                    || command.MinimumApprovedOperatorCount > ShiftRiskConstants.MaxMinimumOperatorCount)
                {
                    return ShiftRiskWriteResult.Fail(
                        ShiftRiskFailureCodes.ValidationFailed,
                        "MinimumApprovedOperatorCount must be between 1 and 50.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                requirement.MinimumApprovedOperatorCount = command.MinimumApprovedOperatorCount;
                requirement.IsCriticalRole = command.IsCriticalRole;
                requirement.SourceReference = command.SourceReference.Trim();
                requirement.EffectiveFrom = ParseDateOrNull(command.EffectiveFrom);
                requirement.EffectiveUntil = ParseDateOrNull(command.EffectiveUntil);
                requirement.IsActive = command.IsActive;
                requirement.UpdatedAtUtc = now;
                requirement.UpdatedBy = actor;
                requirement.Version++;

                AddAuditEvent(
                    context,
                    requirement.Id,
                    ShiftRiskEntityTypes.CoverageRequirement,
                    ShiftRiskAuditEventTypes.RequirementUpdated,
                    actor,
                    now,
                    new { requirementId = requirement.Id, version = requirement.Version });

                await dbContext.SaveChangesAsync(ct);
                return ShiftRiskWriteResult.Success(requirement.Id, requirement.Version);
            },
            cancellationToken);

    public Task<ShiftRiskWriteResult> EvaluateRiskAsync(
        ShiftRiskOperationContext context,
        EvaluateShiftRiskCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return ShiftRiskWriteResult.Fail(
                ShiftRiskFailureCodes.ValidationFailed,
                "IdempotencyKey is required.");
        }

        if (!DateOnly.TryParse(command.ShiftDate, out var shiftDate))
        {
            return ShiftRiskWriteResult.Fail(
                ShiftRiskFailureCodes.ValidationFailed,
                "ShiftDate must be a valid date.");
        }

        var commandFingerprint = ShiftRiskIdempotencyFingerprint.ComputeEvaluateCommandFingerprint(context, command);
        var existingByKey = await dbContext.ShiftRiskAssessments
            .FirstOrDefaultAsync(
                a => a.TenantId == context.TenantId
                     && a.EvaluateIdempotencyKey == command.IdempotencyKey.Trim(),
                ct);

        if (existingByKey is not null)
        {
            if (string.IsNullOrWhiteSpace(existingByKey.EvaluateIdempotencyPayloadFingerprint))
            {
                return ShiftRiskWriteResult.Fail(
                    ShiftRiskFailureCodes.IdempotencyLegacyUnverifiable,
                    "Evaluate idempotency key belongs to a legacy record that cannot be verified safely.");
            }

            if (existingByKey.EvaluateIdempotencyPayloadFingerprint == commandFingerprint)
            {
                return ShiftRiskWriteResult.SuccessAssessment(existingByKey.Id, true);
            }

            return ShiftRiskWriteResult.Fail(
                ShiftRiskFailureCodes.IdempotencyConflict,
                "Evaluate idempotency key was already used with a different payload.");
        }

        var buildResult = await BuildEvaluationContextCoreAsync(
            context, command.ProductionShiftId, shiftDate, ct);
        if (!buildResult.Succeeded || buildResult.Context is null)
        {
            return ShiftRiskWriteResult.Fail(
                buildResult.FailureCode ?? ShiftRiskFailureCodes.ValidationFailed,
                buildResult.FailureReason ?? "Evaluation context build failed.");
        }

        var evaluationContext = buildResult.Context with
        {
            ProductionOrderReference = TrimOrNull(command.ProductionOrderReference),
            MachineAssetId = command.MachineAssetId is null || command.MachineAssetId == Guid.Empty
                ? null
                : command.MachineAssetId,
            MachineFamilyScope = TrimOrNull(command.MachineFamilyScope),
            RequiredCompetencyCode = TrimOrNull(command.RequiredCompetencyCode),
        };

        var inputFingerprint = ShiftRiskIdempotencyFingerprint.ComputeEvaluationInputFingerprint(evaluationContext);
        var existingByFingerprint = await dbContext.ShiftRiskAssessments
            .FirstOrDefaultAsync(
                a => a.TenantId == context.TenantId
                     && a.FacilityId == context.FacilityId
                     && a.AssessmentType == ShiftRiskAssessmentTypes.Coverage
                     && a.InputFingerprint == inputFingerprint,
                ct);

        if (existingByFingerprint is not null)
        {
            return ShiftRiskWriteResult.SuccessAssessment(existingByFingerprint.Id, true);
        }

        var requirement = await FindMatchingRequirementAsync(context, evaluationContext, ct);
        var assignments = await LoadAssignmentEvidenceAsync(context, evaluationContext, ct);
        var personIds = assignments.Select(a => a.WorkforcePersonId).Distinct().ToList();
        var persons = await LoadPersonEvidenceAsync(context, personIds, ct);

        var requirementSnapshot = requirement is null
            ? null
            : new ShiftCoverageRiskEvaluator.RequirementSnapshot(
                requirement.Id,
                requirement.ProductionShiftId,
                requirement.ShiftDate,
                requirement.PlannedStartUtc,
                requirement.PlannedEndUtc,
                requirement.ProductionOrderReference,
                requirement.MachineAssetId,
                requirement.MachineFamilyScope,
                requirement.RequiredCompetencyCode,
                requirement.MinimumApprovedOperatorCount,
                requirement.IsCriticalRole);

        var now = DateTimeOffset.UtcNow;
        var evaluation = ShiftCoverageRiskEvaluator.Evaluate(
            requirementSnapshot,
            evaluationContext,
            assignments,
            persons,
            now);

        var actor = context.IdentitySubjectId.ToString();
        var assessment = new ShiftRiskAssessment
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            AssessmentType = ShiftRiskAssessmentTypes.Coverage,
            CoverageRequirementId = requirement?.Id,
            ProductionShiftId = evaluationContext.ProductionShiftId,
            ShiftDate = evaluationContext.ShiftDate,
            PlannedStartUtc = evaluationContext.PlannedStartUtc,
            PlannedEndUtc = evaluationContext.PlannedEndUtc,
            ProductionOrderReference = evaluationContext.ProductionOrderReference,
            MachineAssetId = evaluationContext.MachineAssetId,
            MachineFamilyScope = evaluationContext.MachineFamilyScope,
            RequiredCompetencyCode = requirement?.RequiredCompetencyCode ?? evaluationContext.RequiredCompetencyCode,
            MinimumRequiredCount = requirement?.MinimumApprovedOperatorCount,
            IsCriticalRole = requirement?.IsCriticalRole,
            ApprovedEligibleCount = evaluation.ApprovedEligibleCount,
            CoverageGap = evaluation.CoverageGap,
            RuleSetVersion = ShiftRiskConstants.RuleSetVersion,
            InputFingerprint = inputFingerprint,
            Outcome = evaluation.Outcome,
            LeadingDriverCategory = evaluation.LeadingDriverCategory,
            LeadingReasonCode = evaluation.LeadingReasonCode,
            StructuredEvidenceJson = JsonSerializer.Serialize(
                new
                {
                    countedAssignmentIds = evaluation.CountedAssignments.Select(a => a.AssignmentId),
                    excludedAssignmentIds = evaluation.ExcludedAssignments.Select(a => a.Assignment.AssignmentId),
                },
                JsonOptions),
            SourceFreshnessSnapshotJson = JsonSerializer.Serialize(
                persons.Values.Select(p => new
                {
                    personId = p.PersonId,
                    freshnessState = p.FreshnessState,
                    availabilityState = p.AvailabilityState,
                }),
                JsonOptions),
            EvaluatedAtUtc = now,
            EvaluateIdempotencyKey = command.IdempotencyKey.Trim(),
            EvaluateIdempotencyPayloadFingerprint = commandFingerprint,
            CreatedAtUtc = now,
            CreatedBy = actor,
        };

        foreach (var driverDraft in evaluation.Drivers)
        {
            var driver = new ShiftRiskDriver
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                AssessmentId = assessment.Id,
                ReasonCode = driverDraft.ReasonCode,
                LabelTr = ShiftRiskReasonLabels.GetLabelTr(driverDraft.ReasonCode),
                Category = driverDraft.Category,
                Severity = driverDraft.Severity,
                RequiredValue = driverDraft.RequiredValue,
                ObservedValue = driverDraft.ObservedValue,
                GapValue = driverDraft.GapValue,
                ProductionShiftId = driverDraft.ProductionShiftId,
                ProductionOrderReference = driverDraft.ProductionOrderReference,
                MachineAssetId = driverDraft.MachineAssetId,
                RequiredCompetencyCode = driverDraft.RequiredCompetencyCode,
                EvidenceSource = driverDraft.EvidenceSource,
                EvidenceReference = driverDraft.EvidenceReference,
                EvidenceTimestampUtc = driverDraft.EvidenceTimestampUtc,
                FreshnessState = driverDraft.FreshnessState,
                RuleSetVersion = ShiftRiskConstants.RuleSetVersion,
                IsLeadingRisk = driverDraft.ReasonCode == evaluation.LeadingReasonCode,
            };
            assessment.Drivers.Add(driver);
        }

        var leadingDriver = assessment.Drivers.FirstOrDefault(d => d.IsLeadingRisk);
        assessment.LeadingDriverId = leadingDriver?.Id;

        dbContext.ShiftRiskAssessments.Add(assessment);
        AddAuditEvent(
            context,
            assessment.Id,
            ShiftRiskEntityTypes.Assessment,
            ShiftRiskAuditEventTypes.AssessmentEvaluated,
            actor,
            now,
            new { assessmentId = assessment.Id, outcome = assessment.Outcome });

        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return await ResolveEvaluateIdempotencyRaceAsync(
                context,
                command.IdempotencyKey.Trim(),
                commandFingerprint,
                ct);
        }

        return ShiftRiskWriteResult.SuccessAssessment(assessment.Id);
            },
            cancellationToken);

    public Task<ShiftRiskWriteResult> EvaluateWorkforceContinuityAsync(
        ShiftRiskOperationContext context,
        EvaluateWorkforceContinuityCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
                {
                    return ShiftRiskWriteResult.Fail(
                        ShiftRiskFailureCodes.ValidationFailed,
                        "IdempotencyKey is required.");
                }

                if (command.WorkforcePersonId == Guid.Empty)
                {
                    return ShiftRiskWriteResult.Fail(
                        ShiftRiskFailureCodes.ValidationFailed,
                        "WorkforcePersonId is required.");
                }

                if (!DateOnly.TryParse(command.ShiftDate, out var shiftDate))
                {
                    return ShiftRiskWriteResult.Fail(
                        ShiftRiskFailureCodes.ValidationFailed,
                        "ShiftDate must be a valid date.");
                }

                var commandFingerprint = ShiftRiskIdempotencyFingerprint.ComputeContinuityEvaluateCommandFingerprint(
                    context,
                    command);
                var existingByKey = await dbContext.ShiftRiskAssessments
                    .FirstOrDefaultAsync(
                        a => a.TenantId == context.TenantId
                             && a.EvaluateIdempotencyKey == command.IdempotencyKey.Trim(),
                        ct);

                if (existingByKey is not null)
                {
                    if (string.IsNullOrWhiteSpace(existingByKey.EvaluateIdempotencyPayloadFingerprint))
                    {
                        return ShiftRiskWriteResult.Fail(
                            ShiftRiskFailureCodes.IdempotencyLegacyUnverifiable,
                            "Evaluate idempotency key belongs to a legacy record that cannot be verified safely.");
                    }

                    if (existingByKey.EvaluateIdempotencyPayloadFingerprint == commandFingerprint)
                    {
                        return ShiftRiskWriteResult.SuccessAssessment(existingByKey.Id, true);
                    }

                    return ShiftRiskWriteResult.Fail(
                        ShiftRiskFailureCodes.IdempotencyConflict,
                        "Evaluate idempotency key was already used with a different payload.");
                }

                var buildResult = await BuildEvaluationContextCoreAsync(
                    context, command.ProductionShiftId, shiftDate, ct);
                if (!buildResult.Succeeded || buildResult.Context is null)
                {
                    return ShiftRiskWriteResult.Fail(
                        buildResult.FailureCode ?? ShiftRiskFailureCodes.ValidationFailed,
                        buildResult.FailureReason ?? "Evaluation context build failed.");
                }

                var evaluationContext = buildResult.Context with
                {
                    ProductionOrderReference = TrimOrNull(command.ProductionOrderReference),
                    MachineAssetId = command.MachineAssetId is null || command.MachineAssetId == Guid.Empty
                        ? null
                        : command.MachineAssetId,
                    RequiredCompetencyCode = TrimOrNull(command.RequiredCompetencyCode),
                };

                var inputFingerprint = ShiftRiskIdempotencyFingerprint.ComputeContinuityEvaluationInputFingerprint(
                    command.WorkforcePersonId,
                    evaluationContext);
                var existingByFingerprint = await dbContext.ShiftRiskAssessments
                    .FirstOrDefaultAsync(
                        a => a.TenantId == context.TenantId
                             && a.FacilityId == context.FacilityId
                             && a.AssessmentType == ShiftWorkforceContinuityConstants.AssessmentType
                             && a.InputFingerprint == inputFingerprint,
                        ct);

                if (existingByFingerprint is not null)
                {
                    return ShiftRiskWriteResult.SuccessAssessment(existingByFingerprint.Id, true);
                }

                var person = await dbContext.ShiftWorkforcePersons.AsNoTracking()
                    .FirstOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId
                             && p.Id == command.WorkforcePersonId,
                        ct);

                if (person is null)
                {
                    return ShiftRiskWriteResult.Fail(
                        ShiftRiskFailureCodes.PersonNotFound,
                        "Workforce person not found.");
                }

                var competencies = await LoadContinuityCompetencyEvidenceAsync(
                    context,
                    command.WorkforcePersonId,
                    ct);
                var assignments = await LoadContinuityAssignmentEvidenceAsync(
                    context,
                    command.WorkforcePersonId,
                    evaluationContext,
                    ct);
                var coverageRequirements = await LoadContinuityCoverageRequirementsAsync(
                    context,
                    evaluationContext,
                    ct);

                var personContext = new ShiftWorkforceContinuityRiskEvaluator.PersonContext(
                    person.Id,
                    person.EmployeeCode,
                    person.DisplayName,
                    person.IsActive,
                    person.AvailabilityState,
                    person.FreshnessState,
                    person.SourceUpdatedAtUtc,
                    person.SourceSystem == ShiftWorkforceSourceSystems.LocalDevelopmentPilot);

                var now = DateTimeOffset.UtcNow;
                var evaluation = ShiftWorkforceContinuityRiskEvaluator.Evaluate(
                    personContext,
                    competencies,
                    assignments,
                    coverageRequirements,
                    evaluationContext.ProductionShiftId,
                    evaluationContext.ShiftDate,
                    evaluationContext.PlannedStartUtc,
                    evaluationContext.PlannedEndUtc,
                    evaluationContext.RequiredCompetencyCode,
                    now);

                var actor = context.IdentitySubjectId.ToString();
                var assessment = new ShiftRiskAssessment
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    AssessmentType = ShiftWorkforceContinuityConstants.AssessmentType,
                    WorkforcePersonId = command.WorkforcePersonId,
                    ProductionShiftId = evaluationContext.ProductionShiftId,
                    ShiftDate = evaluationContext.ShiftDate,
                    PlannedStartUtc = evaluationContext.PlannedStartUtc,
                    PlannedEndUtc = evaluationContext.PlannedEndUtc,
                    ProductionOrderReference = evaluationContext.ProductionOrderReference,
                    MachineAssetId = evaluationContext.MachineAssetId,
                    RequiredCompetencyCode = evaluationContext.RequiredCompetencyCode,
                    ApprovedEligibleCount = evaluation.TrustworthyAvailableCount,
                    ApprovedAssignmentCount = evaluation.ApprovedAssignmentCount,
                    PlannedAssignmentMinutes = evaluation.PlannedAssignmentMinutes,
                    CoverageGap = evaluation.CoverageGap,
                    RuleSetVersion = ShiftWorkforceContinuityConstants.RuleSetVersion,
                    InputFingerprint = inputFingerprint,
                    Outcome = evaluation.Outcome,
                    LeadingDriverCategory = evaluation.LeadingDriverCategory,
                    LeadingReasonCode = evaluation.LeadingReasonCode,
                    StructuredEvidenceJson = JsonSerializer.Serialize(
                        new
                        {
                            consideredAssignmentIds = evaluation.ConsideredAssignments.Select(a => a.AssignmentId),
                            competencyAssignmentIds = competencies.Select(c => c.AssignmentId),
                        },
                        JsonOptions),
                    SourceFreshnessSnapshotJson = JsonSerializer.Serialize(
                        new
                        {
                            personId = person.Id,
                            availabilityState = person.AvailabilityState,
                            freshnessState = person.FreshnessState,
                            sourceUpdatedAtUtc = person.SourceUpdatedAtUtc,
                        },
                        JsonOptions),
                    EvaluatedAtUtc = now,
                    EvaluateIdempotencyKey = command.IdempotencyKey.Trim(),
                    EvaluateIdempotencyPayloadFingerprint = commandFingerprint,
                    CreatedAtUtc = now,
                    CreatedBy = actor,
                };

                foreach (var driverDraft in evaluation.Drivers)
                {
                    var driver = new ShiftRiskDriver
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        AssessmentId = assessment.Id,
                        ReasonCode = driverDraft.ReasonCode,
                        LabelTr = ShiftWorkforceContinuityReasonLabels.GetLabelTr(driverDraft.ReasonCode),
                        Category = driverDraft.Category,
                        Severity = driverDraft.Severity,
                        RequiredValue = driverDraft.RequiredValue,
                        ObservedValue = driverDraft.ObservedValue,
                        GapValue = driverDraft.GapValue,
                        WorkforcePersonId = driverDraft.WorkforcePersonId,
                        ProductionShiftId = driverDraft.ProductionShiftId,
                        ProductionOrderReference = driverDraft.ProductionOrderReference,
                        MachineAssetId = driverDraft.MachineAssetId,
                        RequiredCompetencyCode = driverDraft.RequiredCompetencyCode,
                        EvidenceSource = driverDraft.EvidenceSource,
                        EvidenceReference = driverDraft.EvidenceReference,
                        EvidenceTimestampUtc = driverDraft.EvidenceTimestampUtc,
                        FreshnessState = driverDraft.FreshnessState,
                        RuleSetVersion = ShiftWorkforceContinuityConstants.RuleSetVersion,
                        IsLeadingRisk = driverDraft.ReasonCode == evaluation.LeadingReasonCode,
                    };
                    assessment.Drivers.Add(driver);
                }

                assessment.LeadingDriverId = assessment.Drivers.FirstOrDefault(d => d.IsLeadingRisk)?.Id;
                dbContext.ShiftRiskAssessments.Add(assessment);
                AddAuditEvent(
                    context,
                    assessment.Id,
                    ShiftRiskEntityTypes.Assessment,
                    ShiftRiskAuditEventTypes.AssessmentEvaluated,
                    actor,
                    now,
                    new { assessmentId = assessment.Id, outcome = assessment.Outcome, assessmentType = assessment.AssessmentType });

                try
                {
                    await dbContext.SaveChangesAsync(ct);
                }
                catch (DbUpdateException ex) when (IsUniqueViolation(ex))
                {
                    return await ResolveEvaluateIdempotencyRaceAsync(
                        context,
                        command.IdempotencyKey.Trim(),
                        commandFingerprint,
                        ct);
                }

                return ShiftRiskWriteResult.SuccessAssessment(assessment.Id);
            },
            cancellationToken);

    public Task<ShiftContinuityOverviewReadModel> GetContinuityOverviewAsync(
        ShiftRiskOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var assessments = await dbContext.ShiftRiskAssessments.AsNoTracking()
                    .Where(a => a.TenantId == context.TenantId
                                && a.FacilityId == context.FacilityId
                                && a.AssessmentType == ShiftWorkforceContinuityConstants.AssessmentType)
                    .ToListAsync(ct);

                return new ShiftContinuityOverviewReadModel(
                    assessments.Count,
                    assessments.Count(a => a.Outcome == ShiftRiskOutcomes.Clear),
                    assessments.Count(a => a.Outcome == ShiftRiskOutcomes.Attention),
                    assessments.Count(a => a.Outcome == ShiftRiskOutcomes.Critical),
                    assessments.Count(a => a.Outcome == ShiftRiskOutcomes.InsufficientData),
                    assessments.Count(a => a.LeadingDriverCategory == ShiftWorkforceContinuityDriverCategories.AbsenceImpact),
                    assessments.Count(a => a.LeadingDriverCategory == ShiftWorkforceContinuityDriverCategories.CompetencyExpired),
                    assessments.Count(a => a.LeadingDriverCategory == ShiftWorkforceContinuityDriverCategories.CompetencyExpiring),
                    assessments.Count(a => a.LeadingDriverCategory is ShiftWorkforceContinuityDriverCategories.SourceStale
                        or ShiftWorkforceContinuityDriverCategories.AvailabilityUnknown),
                    assessments.Count(a => a.LeadingDriverCategory == ShiftWorkforceContinuityDriverCategories.AssignmentOverlap),
                    assessments.Count(a => a.LeadingDriverCategory == ShiftWorkforceContinuityDriverCategories.CoverageImpact));
            },
            cancellationToken);

    public Task<ShiftContinuityAssessmentListReadModel> ListContinuityAssessmentsAsync(
        ShiftRiskOperationContext context,
        ShiftContinuityAssessmentListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var page = filters.Page < 1 ? 1 : filters.Page;
                var pageSize = filters.PageSize is < 1 or > 100 ? 25 : filters.PageSize;

                var query = dbContext.ShiftRiskAssessments.AsNoTracking()
                    .Where(a => a.TenantId == context.TenantId
                                && a.FacilityId == context.FacilityId
                                && a.AssessmentType == ShiftWorkforceContinuityConstants.AssessmentType);

                if (filters.ProductionShiftId.HasValue)
                {
                    query = query.Where(a => a.ProductionShiftId == filters.ProductionShiftId.Value);
                }

                if (!string.IsNullOrWhiteSpace(filters.ProductionOrderReference))
                {
                    var reference = filters.ProductionOrderReference.Trim();
                    query = query.Where(a => a.ProductionOrderReference == reference);
                }

                if (filters.MachineAssetId.HasValue)
                {
                    query = query.Where(a => a.MachineAssetId == filters.MachineAssetId.Value);
                }

                if (filters.WorkforcePersonId.HasValue)
                {
                    query = query.Where(a => a.WorkforcePersonId == filters.WorkforcePersonId.Value);
                }

                if (!string.IsNullOrWhiteSpace(filters.Outcome))
                {
                    var outcome = filters.Outcome.Trim();
                    query = query.Where(a => a.Outcome == outcome);
                }

                if (!string.IsNullOrWhiteSpace(filters.DriverCategory))
                {
                    var category = filters.DriverCategory.Trim();
                    query = query.Where(a => a.LeadingDriverCategory == category);
                }

                if (!string.IsNullOrWhiteSpace(filters.RequiredCompetencyCode))
                {
                    var competencyCode = filters.RequiredCompetencyCode.Trim();
                    query = query.Where(a => a.RequiredCompetencyCode == competencyCode);
                }

                if (!string.IsNullOrWhiteSpace(filters.AvailabilityState)
                    || !string.IsNullOrWhiteSpace(filters.FreshnessState))
                {
                    var personQuery = dbContext.ShiftWorkforcePersons.AsNoTracking()
                        .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId);
                    if (!string.IsNullOrWhiteSpace(filters.AvailabilityState))
                    {
                        var availability = filters.AvailabilityState.Trim();
                        personQuery = personQuery.Where(p => p.AvailabilityState == availability);
                    }

                    if (!string.IsNullOrWhiteSpace(filters.FreshnessState))
                    {
                        var freshness = filters.FreshnessState.Trim();
                        personQuery = personQuery.Where(p => p.FreshnessState == freshness);
                    }

                    var personIds = await personQuery.Select(p => p.Id).ToListAsync(ct);
                    query = query.Where(a => a.WorkforcePersonId.HasValue && personIds.Contains(a.WorkforcePersonId.Value));
                }

                if (filters.CriticalOnly == true)
                {
                    query = query.Where(a => a.IsCriticalRole == true);
                }

                var totalCount = await query.CountAsync(ct);
                var assessments = await query
                    .OrderByDescending(a => a.EvaluatedAtUtc)
                    .ThenByDescending(a => a.CreatedAtUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                var shiftLookup = await LoadShiftLookupAsync(context, ct);
                var machineLookup = await LoadMachineLookupAsync(context, ct);
                var personIdsOnPage = assessments
                    .Where(a => a.WorkforcePersonId.HasValue)
                    .Select(a => a.WorkforcePersonId!.Value)
                    .Distinct()
                    .ToList();
                var persons = await dbContext.ShiftWorkforcePersons.AsNoTracking()
                    .Where(p => p.TenantId == context.TenantId && personIdsOnPage.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, ct);

                var items = assessments
                    .Select(a => MapContinuityAssessmentSummary(a, persons, shiftLookup, machineLookup))
                    .ToList();

                return new ShiftContinuityAssessmentListReadModel(items, totalCount, page, pageSize);
            },
            cancellationToken);

    public Task<ShiftContinuityAssessmentDetailReadModel?> GetContinuityAssessmentByIdAsync(
        ShiftRiskOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var assessment = await dbContext.ShiftRiskAssessments.AsNoTracking()
                    .Include(a => a.Drivers)
                    .FirstOrDefaultAsync(
                        a => a.Id == assessmentId
                             && a.TenantId == context.TenantId
                             && a.FacilityId == context.FacilityId
                             && a.AssessmentType == ShiftWorkforceContinuityConstants.AssessmentType,
                        ct);

                if (assessment is null || !assessment.WorkforcePersonId.HasValue)
                {
                    return null;
                }

                var person = await dbContext.ShiftWorkforcePersons.AsNoTracking()
                    .FirstOrDefaultAsync(
                        p => p.TenantId == context.TenantId && p.Id == assessment.WorkforcePersonId.Value,
                        ct);

                if (person is null)
                {
                    return null;
                }

                var shiftLookup = await LoadShiftLookupAsync(context, ct);
                var machineLookup = await LoadMachineLookupAsync(context, ct);
                var evaluationContext = new ShiftRiskEvaluationContext(
                    assessment.ProductionShiftId,
                    assessment.ShiftDate,
                    assessment.PlannedStartUtc,
                    assessment.PlannedEndUtc,
                    assessment.ProductionOrderReference,
                    assessment.MachineAssetId,
                    null,
                    assessment.RequiredCompetencyCode);

                var competencies = await LoadContinuityCompetencyEvidenceAsync(
                    context,
                    assessment.WorkforcePersonId.Value,
                    ct);
                var assignments = await LoadContinuityAssignmentEvidenceAsync(
                    context,
                    assessment.WorkforcePersonId.Value,
                    evaluationContext,
                    ct);

                return new ShiftContinuityAssessmentDetailReadModel(
                    assessment.Id,
                    assessment.WorkforcePersonId.Value,
                    person.EmployeeCode,
                    person.DisplayName,
                    person.Department ?? "—",
                    person.JobTitle ?? "—",
                    person.IsActive,
                    person.AvailabilityState,
                    person.FreshnessState,
                    person.SourceUpdatedAtUtc,
                    assessment.ProductionShiftId,
                    shiftLookup.GetValueOrDefault(assessment.ProductionShiftId)?.Name
                        ?? shiftLookup.GetValueOrDefault(assessment.ProductionShiftId)?.Code
                        ?? "—",
                    assessment.ShiftDate,
                    assessment.PlannedStartUtc,
                    assessment.PlannedEndUtc,
                    assessment.ProductionOrderReference,
                    assessment.MachineAssetId,
                    assessment.MachineAssetId.HasValue
                        ? machineLookup.GetValueOrDefault(assessment.MachineAssetId.Value)?.Name ?? "—"
                        : null,
                    assessment.RequiredCompetencyCode,
                    assessment.ApprovedAssignmentCount,
                    assessment.PlannedAssignmentMinutes,
                    assessment.ApprovedEligibleCount,
                    assessment.CoverageGap,
                    assessment.Outcome,
                    assessment.LeadingDriverCategory,
                    assessment.LeadingReasonCode,
                    assessment.LeadingDriverId,
                    assessment.RuleSetVersion,
                    assessment.InputFingerprint,
                    assessment.EvaluatedAtUtc,
                    assessment.StructuredEvidenceJson,
                    assessment.SourceFreshnessSnapshotJson,
                    assessment.Drivers
                        .OrderByDescending(d => d.IsLeadingRisk)
                        .ThenBy(d => d.Category)
                        .Select(MapDriver)
                        .ToList(),
                    competencies.Select(c => new ShiftContinuityCompetencyEvidenceReadModel(
                        c.AssignmentId,
                        c.CompetencyCode,
                        c.CompetencyLabel,
                        c.VerificationStatus,
                        c.ValidFromUtc,
                        c.ValidUntilUtc,
                        c.IsPilotData)).ToList(),
                    assignments.Select(a => MapAssignmentEvidence(
                        new ShiftCoverageRiskEvaluator.AssignmentEvidence(
                            a.AssignmentId,
                            a.WorkforcePersonId,
                            person.EmployeeCode,
                            person.DisplayName,
                            a.Status,
                            a.PlannedStartUtc,
                            a.PlannedEndUtc,
                            a.RequiredCompetencyCode,
                            a.MachineAssetId,
                            a.ProductionOrderReference,
                            Guid.Empty,
                            null,
                            null,
                            a.IsPilotData),
                        null)).ToList(),
                    person.SourceSystem == ShiftWorkforceSourceSystems.LocalDevelopmentPilot
                        || assessment.StructuredEvidenceJson.Contains("pilot", StringComparison.OrdinalIgnoreCase));
            },
            cancellationToken);

    public Task<ShiftRiskOverviewReadModel> GetOverviewAsync(
        ShiftRiskOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var assessments = await dbContext.ShiftRiskAssessments.AsNoTracking()
                    .Where(a => a.TenantId == context.TenantId
                                && a.FacilityId == context.FacilityId
                                && a.AssessmentType == ShiftRiskAssessmentTypes.Coverage)
                    .ToListAsync(ct);

                return new ShiftRiskOverviewReadModel(
                    assessments.Count,
                    assessments.Count(a => a.Outcome == ShiftRiskOutcomes.Clear),
                    assessments.Count(a => a.Outcome == ShiftRiskOutcomes.Attention),
                    assessments.Count(a => a.Outcome == ShiftRiskOutcomes.Critical),
                    assessments.Count(a => a.Outcome == ShiftRiskOutcomes.InsufficientData),
                    assessments.Count(a => a.LeadingDriverCategory == ShiftRiskDriverCategories.CoverageGap),
                    assessments.Count(a => a.LeadingDriverCategory == ShiftRiskDriverCategories.CriticalRoleGap),
                    assessments.Count(a => a.LeadingDriverCategory == ShiftRiskDriverCategories.AssignmentConflict));
            },
            cancellationToken);

    public Task<ShiftRiskAssessmentListReadModel> ListAssessmentsAsync(
        ShiftRiskOperationContext context,
        ShiftRiskAssessmentListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var page = filters.Page < 1 ? 1 : filters.Page;
                var pageSize = filters.PageSize is < 1 or > 100 ? 25 : filters.PageSize;

                var query = dbContext.ShiftRiskAssessments.AsNoTracking()
                    .Where(a => a.TenantId == context.TenantId
                                && a.FacilityId == context.FacilityId
                                && a.AssessmentType == ShiftRiskAssessmentTypes.Coverage);

                if (filters.ProductionShiftId.HasValue)
                {
                    query = query.Where(a => a.ProductionShiftId == filters.ProductionShiftId.Value);
                }

                if (!string.IsNullOrWhiteSpace(filters.ProductionOrderReference))
                {
                    var reference = filters.ProductionOrderReference.Trim();
                    query = query.Where(a => a.ProductionOrderReference == reference);
                }

                if (filters.MachineAssetId.HasValue)
                {
                    query = query.Where(a => a.MachineAssetId == filters.MachineAssetId.Value);
                }

                if (!string.IsNullOrWhiteSpace(filters.Outcome))
                {
                    var outcome = filters.Outcome.Trim();
                    query = query.Where(a => a.Outcome == outcome);
                }

                if (!string.IsNullOrWhiteSpace(filters.DriverCategory))
                {
                    var category = filters.DriverCategory.Trim();
                    query = query.Where(a => a.LeadingDriverCategory == category);
                }

                if (filters.CriticalOnly == true)
                {
                    query = query.Where(a => a.IsCriticalRole == true);
                }

                var totalCount = await query.CountAsync(ct);
                var assessments = await query
                    .OrderByDescending(a => a.EvaluatedAtUtc)
                    .ThenByDescending(a => a.CreatedAtUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                var shiftLookup = await LoadShiftLookupAsync(context, ct);
                var machineLookup = await LoadMachineLookupAsync(context, ct);

                var items = assessments
                    .Select(a => MapAssessmentSummary(a, shiftLookup, machineLookup))
                    .ToList();

                return new ShiftRiskAssessmentListReadModel(items, totalCount, page, pageSize);
            },
            cancellationToken);

    public Task<ShiftRiskAssessmentDetailReadModel?> GetAssessmentByIdAsync(
        ShiftRiskOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var assessment = await dbContext.ShiftRiskAssessments.AsNoTracking()
                    .Include(a => a.Drivers)
                    .FirstOrDefaultAsync(
                        a => a.Id == assessmentId
                             && a.TenantId == context.TenantId
                             && a.FacilityId == context.FacilityId
                             && a.AssessmentType == ShiftRiskAssessmentTypes.Coverage,
                        ct);

                if (assessment is null)
                {
                    return null;
                }

                var shiftLookup = await LoadShiftLookupAsync(context, ct);
                var machineLookup = await LoadMachineLookupAsync(context, ct);
                var evaluationContext = new ShiftRiskEvaluationContext(
                    assessment.ProductionShiftId,
                    assessment.ShiftDate,
                    assessment.PlannedStartUtc,
                    assessment.PlannedEndUtc,
                    assessment.ProductionOrderReference,
                    assessment.MachineAssetId,
                    assessment.MachineFamilyScope,
                    assessment.RequiredCompetencyCode);

                var assignments = await LoadAssignmentEvidenceAsync(context, evaluationContext, ct);
                var requirement = assessment.CoverageRequirementId.HasValue
                    ? await dbContext.ShiftCoverageRequirements.AsNoTracking()
                        .FirstOrDefaultAsync(r => r.Id == assessment.CoverageRequirementId.Value, ct)
                    : null;

                var requirementSnapshot = requirement is null
                    ? null
                    : new ShiftCoverageRiskEvaluator.RequirementSnapshot(
                        requirement.Id,
                        requirement.ProductionShiftId,
                        requirement.ShiftDate,
                        requirement.PlannedStartUtc,
                        requirement.PlannedEndUtc,
                        requirement.ProductionOrderReference,
                        requirement.MachineAssetId,
                        requirement.MachineFamilyScope,
                        requirement.RequiredCompetencyCode,
                        requirement.MinimumApprovedOperatorCount,
                        requirement.IsCriticalRole);

                var persons = await LoadPersonEvidenceAsync(
                    context,
                    assignments.Select(a => a.WorkforcePersonId).Distinct().ToList(),
                    ct);

                var liveEvaluation = ShiftCoverageRiskEvaluator.Evaluate(
                    requirementSnapshot,
                    evaluationContext,
                    assignments,
                    persons,
                    assessment.EvaluatedAtUtc);

                var countedIds = liveEvaluation.CountedAssignments.Select(a => a.AssignmentId).ToHashSet();
                var excludedMap = liveEvaluation.ExcludedAssignments
                    .ToDictionary(x => x.Assignment.AssignmentId, x => x.ExclusionReasonCode);

                return new ShiftRiskAssessmentDetailReadModel(
                    assessment.Id,
                    assessment.CoverageRequirementId,
                    assessment.ProductionShiftId,
                    shiftLookup.GetValueOrDefault(assessment.ProductionShiftId)?.Name
                        ?? shiftLookup.GetValueOrDefault(assessment.ProductionShiftId)?.Code
                        ?? "—",
                    assessment.ShiftDate,
                    assessment.PlannedStartUtc,
                    assessment.PlannedEndUtc,
                    assessment.ProductionOrderReference,
                    assessment.MachineAssetId,
                    assessment.MachineAssetId.HasValue
                        ? machineLookup.GetValueOrDefault(assessment.MachineAssetId.Value)?.Name ?? "—"
                        : null,
                    assessment.MachineFamilyScope,
                    assessment.RequiredCompetencyCode,
                    assessment.MinimumRequiredCount,
                    assessment.IsCriticalRole,
                    assessment.ApprovedEligibleCount,
                    assessment.CoverageGap,
                    assessment.Outcome,
                    assessment.LeadingDriverCategory,
                    assessment.LeadingReasonCode,
                    assessment.LeadingDriverId,
                    assessment.RuleSetVersion,
                    assessment.InputFingerprint,
                    assessment.EvaluatedAtUtc,
                    assessment.StructuredEvidenceJson,
                    assessment.SourceFreshnessSnapshotJson,
                    assessment.Drivers
                        .OrderByDescending(d => d.IsLeadingRisk)
                        .ThenBy(d => d.Category)
                        .Select(MapDriver)
                        .ToList(),
                    assignments
                        .Where(a => countedIds.Contains(a.AssignmentId))
                        .Select(a => MapAssignmentEvidence(a, null))
                        .ToList(),
                    assignments
                        .Where(a => excludedMap.ContainsKey(a.AssignmentId))
                        .Select(a => MapAssignmentEvidence(a, excludedMap[a.AssignmentId]))
                        .ToList(),
                    assignments.Any(a => a.IsPilotData));
            },
            cancellationToken);

    public Task<IReadOnlyList<ShiftRiskAuditEventReadModel>> ListAuditEventsAsync(
        ShiftRiskOperationContext context,
        string? entityType,
        Guid? entityId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var query = dbContext.ShiftRiskAuditEvents.AsNoTracking()
                    .Where(e => e.TenantId == context.TenantId && e.FacilityId == context.FacilityId);

                if (!string.IsNullOrWhiteSpace(entityType))
                {
                    var type = entityType.Trim();
                    query = query.Where(e => e.EntityType == type);
                }

                if (entityId.HasValue)
                {
                    query = query.Where(e => e.EntityId == entityId.Value);
                }

                var events = await query
                    .OrderByDescending(e => e.OccurredAtUtc)
                    .Take(100)
                    .ToListAsync(ct);

                return (IReadOnlyList<ShiftRiskAuditEventReadModel>)events.Select(e => new ShiftRiskAuditEventReadModel(
                    e.Id,
                    e.EventType,
                    e.EntityType,
                    e.EntityId,
                    e.ActorSubjectId,
                    e.OccurredAtUtc,
                    BuildAuditSummary(e.EventType))).ToList();
            },
            cancellationToken);

    private async Task<IReadOnlyList<ShiftWorkforceContinuityRiskEvaluator.CompetencyContext>> LoadContinuityCompetencyEvidenceAsync(
        ShiftRiskOperationContext context,
        Guid workforcePersonId,
        CancellationToken cancellationToken)
    {
        var assignments = await dbContext.ShiftWorkforceCompetencyAssignments.AsNoTracking()
            .Include(a => a.CompetencyDefinition)
            .Where(a => a.TenantId == context.TenantId
                        && a.FacilityId == context.FacilityId
                        && a.WorkforcePersonId == workforcePersonId)
            .ToListAsync(cancellationToken);

        return assignments.Select(a => new ShiftWorkforceContinuityRiskEvaluator.CompetencyContext(
            a.Id,
            a.CompetencyDefinition?.Code ?? string.Empty,
            a.CompetencyDefinition?.DisplayLabelTr ?? string.Empty,
            a.VerificationStatus,
            a.ValidFromUtc,
            a.ValidUntilUtc,
            a.CreateIdempotencyKey?.StartsWith("pilot-shift-continuity", StringComparison.OrdinalIgnoreCase) == true)).ToList();
    }

    private async Task<IReadOnlyList<ShiftWorkforceContinuityRiskEvaluator.AssignmentContext>> LoadContinuityAssignmentEvidenceAsync(
        ShiftRiskOperationContext context,
        Guid workforcePersonId,
        ShiftRiskEvaluationContext evaluationContext,
        CancellationToken cancellationToken)
    {
        var assignments = await dbContext.ShiftOperatorAssignments.AsNoTracking()
            .Where(a => a.TenantId == context.TenantId
                        && a.FacilityId == context.FacilityId
                        && a.WorkforcePersonId == workforcePersonId
                        && a.ProductionShiftId == evaluationContext.ProductionShiftId
                        && a.ShiftDate == evaluationContext.ShiftDate)
            .ToListAsync(cancellationToken);

        return assignments.Select(a => new ShiftWorkforceContinuityRiskEvaluator.AssignmentContext(
            a.Id,
            a.WorkforcePersonId,
            a.Status,
            a.PlannedStartUtc,
            a.PlannedEndUtc,
            a.ProductionOrderReference,
            a.MachineAssetId,
            a.RequiredCompetencyCode,
            a.CreatedBy.StartsWith("pilot", StringComparison.OrdinalIgnoreCase))).ToList();
    }

    private async Task<IReadOnlyList<ShiftWorkforceContinuityRiskEvaluator.CoverageRequirementContext>> LoadContinuityCoverageRequirementsAsync(
        ShiftRiskOperationContext context,
        ShiftRiskEvaluationContext evaluationContext,
        CancellationToken cancellationToken)
    {
        var requirements = await dbContext.ShiftCoverageRequirements.AsNoTracking()
            .Where(r => r.TenantId == context.TenantId
                        && r.FacilityId == context.FacilityId
                        && r.IsActive
                        && r.ProductionShiftId == evaluationContext.ProductionShiftId
                        && r.ShiftDate == evaluationContext.ShiftDate)
            .ToListAsync(cancellationToken);

        return requirements
            .Where(r => MatchesOptionalReference(r.ProductionOrderReference, evaluationContext.ProductionOrderReference))
            .Where(r => evaluationContext.MachineAssetId is null
                        || r.MachineAssetId is null
                        || r.MachineAssetId == evaluationContext.MachineAssetId)
            .Select(r => new ShiftWorkforceContinuityRiskEvaluator.CoverageRequirementContext(
                r.Id,
                r.ProductionOrderReference,
                r.MachineAssetId,
                r.RequiredCompetencyCode,
                r.MinimumApprovedOperatorCount,
                r.IsCriticalRole))
            .ToList();
    }

    private static ShiftContinuityAssessmentSummaryReadModel MapContinuityAssessmentSummary(
        ShiftRiskAssessment assessment,
        IReadOnlyDictionary<Guid, ShiftWorkforcePerson> persons,
        IReadOnlyDictionary<Guid, ProductionShift> shiftLookup,
        IReadOnlyDictionary<Guid, MachineRegistry> machineLookup)
    {
        var person = assessment.WorkforcePersonId.HasValue
            ? persons.GetValueOrDefault(assessment.WorkforcePersonId.Value)
            : null;
        var shift = shiftLookup.GetValueOrDefault(assessment.ProductionShiftId);
        var machineLabel = assessment.MachineAssetId.HasValue
            ? machineLookup.GetValueOrDefault(assessment.MachineAssetId.Value)?.Name
            : null;

        return new ShiftContinuityAssessmentSummaryReadModel(
            assessment.Id,
            assessment.WorkforcePersonId ?? Guid.Empty,
            person?.EmployeeCode ?? "—",
            person?.DisplayName ?? "—",
            assessment.ProductionShiftId,
            shift?.Name ?? shift?.Code ?? "—",
            assessment.ShiftDate,
            assessment.ProductionOrderReference,
            assessment.MachineAssetId,
            machineLabel,
            assessment.RequiredCompetencyCode,
            person?.AvailabilityState ?? ShiftWorkforceAvailabilityStates.Unknown,
            person?.FreshnessState ?? ShiftWorkforceFreshnessStates.Unknown,
            assessment.ApprovedAssignmentCount,
            assessment.PlannedAssignmentMinutes,
            assessment.ApprovedEligibleCount,
            assessment.CoverageGap,
            assessment.Outcome,
            assessment.LeadingDriverCategory,
            assessment.LeadingReasonCode,
            assessment.RuleSetVersion,
            assessment.EvaluatedAtUtc,
            assessment.StructuredEvidenceJson.Contains("pilot", StringComparison.OrdinalIgnoreCase)
                || person?.SourceSystem == ShiftWorkforceSourceSystems.LocalDevelopmentPilot);
    }

    private async Task<ShiftCoverageRequirement?> FindMatchingRequirementAsync(
        ShiftRiskOperationContext context,
        ShiftRiskEvaluationContext evaluationContext,
        CancellationToken cancellationToken)
    {
        var requirements = await dbContext.ShiftCoverageRequirements.AsNoTracking()
            .Where(r => r.TenantId == context.TenantId
                        && r.FacilityId == context.FacilityId
                        && r.IsActive
                        && r.ProductionShiftId == evaluationContext.ProductionShiftId
                        && r.ShiftDate == evaluationContext.ShiftDate)
            .ToListAsync(cancellationToken);

        return requirements
            .Where(r => MatchesOptionalReference(r.ProductionOrderReference, evaluationContext.ProductionOrderReference))
            .Where(r => evaluationContext.MachineAssetId is null
                        || r.MachineAssetId is null
                        || r.MachineAssetId == evaluationContext.MachineAssetId)
            .Where(r => string.IsNullOrWhiteSpace(evaluationContext.RequiredCompetencyCode)
                        || string.Equals(
                            r.RequiredCompetencyCode.Trim(),
                            evaluationContext.RequiredCompetencyCode.Trim(),
                            StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.MachineAssetId.HasValue)
            .ThenByDescending(r => !string.IsNullOrWhiteSpace(r.ProductionOrderReference))
            .FirstOrDefault();
    }

    private async Task<IReadOnlyList<ShiftCoverageRiskEvaluator.AssignmentEvidence>> LoadAssignmentEvidenceAsync(
        ShiftRiskOperationContext context,
        ShiftRiskEvaluationContext evaluationContext,
        CancellationToken cancellationToken)
    {
        var assignments = await dbContext.ShiftOperatorAssignments.AsNoTracking()
            .Where(a => a.TenantId == context.TenantId
                        && a.FacilityId == context.FacilityId
                        && a.ProductionShiftId == evaluationContext.ProductionShiftId
                        && a.ShiftDate == evaluationContext.ShiftDate)
            .ToListAsync(cancellationToken);

        var personIds = assignments.Select(a => a.WorkforcePersonId).Distinct().ToList();
        var persons = await dbContext.ShiftWorkforcePersons.AsNoTracking()
            .Where(p => p.TenantId == context.TenantId && personIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var resultIds = assignments.Select(a => a.EligibilityResultId).Distinct().ToList();
        var eligibilityResults = await dbContext.ShiftOperatorEligibilityResults.AsNoTracking()
            .Where(r => r.TenantId == context.TenantId && resultIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        return assignments.Select(a =>
        {
            var person = persons.GetValueOrDefault(a.WorkforcePersonId);
            var eligibility = eligibilityResults.GetValueOrDefault(a.EligibilityResultId);
            return new ShiftCoverageRiskEvaluator.AssignmentEvidence(
                a.Id,
                a.WorkforcePersonId,
                person?.EmployeeCode ?? "—",
                person?.DisplayName ?? "—",
                a.Status,
                a.PlannedStartUtc,
                a.PlannedEndUtc,
                a.RequiredCompetencyCode,
                a.MachineAssetId,
                a.ProductionOrderReference,
                a.EligibilityResultId,
                eligibility?.Outcome,
                eligibility?.PrimaryReasonCode,
                person?.SourceSystem == ShiftWorkforceSourceSystems.LocalDevelopmentPilot);
        }).ToList();
    }

    private async Task<Dictionary<Guid, ShiftCoverageRiskEvaluator.PersonEvidence>> LoadPersonEvidenceAsync(
        ShiftRiskOperationContext context,
        List<Guid> personIds,
        CancellationToken cancellationToken)
    {
        if (personIds.Count == 0)
        {
            return new Dictionary<Guid, ShiftCoverageRiskEvaluator.PersonEvidence>();
        }

        var persons = await dbContext.ShiftWorkforcePersons.AsNoTracking()
            .Where(p => p.TenantId == context.TenantId && personIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        return persons.ToDictionary(
            p => p.Id,
            p => new ShiftCoverageRiskEvaluator.PersonEvidence(
                p.Id,
                p.EmployeeCode,
                p.DisplayName,
                p.IsActive,
                p.AvailabilityState,
                p.FreshnessState,
                p.SourceUpdatedAtUtc,
                p.SourceSystem == ShiftWorkforceSourceSystems.LocalDevelopmentPilot));
    }

    private async Task<Dictionary<Guid, ProductionShift>> LoadShiftLookupAsync(
        ShiftRiskOperationContext context,
        CancellationToken cancellationToken)
    {
        var shifts = await dbContext.ProductionShifts.AsNoTracking()
            .Where(s => s.TenantId == context.TenantId && s.FacilityId == context.FacilityId)
            .ToListAsync(cancellationToken);

        return shifts.ToDictionary(s => s.Id);
    }

    private async Task<Dictionary<Guid, MachineRegistry>> LoadMachineLookupAsync(
        ShiftRiskOperationContext context,
        CancellationToken cancellationToken)
    {
        var machines = await dbContext.MachineRegistries.AsNoTracking()
            .Where(m => m.TenantId == context.TenantId && m.FacilityId == context.FacilityId)
            .ToListAsync(cancellationToken);

        return machines.ToDictionary(m => m.Id);
    }

    private async Task<Dictionary<string, string>> LoadCompetencyLabelsAsync(
        ShiftRiskOperationContext context,
        CancellationToken cancellationToken)
    {
        var definitions = await dbContext.ShiftWorkforceCompetencyDefinitions.AsNoTracking()
            .Where(d => d.TenantId == context.TenantId)
            .ToListAsync(cancellationToken);

        return definitions.ToDictionary(d => d.Code, d => d.DisplayLabelTr);
    }

    private static ShiftRiskRequirementRecordReadModel MapRequirementRecord(
        ShiftCoverageRequirement requirement,
        IReadOnlyDictionary<Guid, ProductionShift> shiftLookup,
        IReadOnlyDictionary<Guid, MachineRegistry> machineLookup,
        IReadOnlyDictionary<string, string> competencyLabels)
    {
        var shift = shiftLookup.GetValueOrDefault(requirement.ProductionShiftId);
        var machineLabel = requirement.MachineAssetId.HasValue
            ? machineLookup.GetValueOrDefault(requirement.MachineAssetId.Value)?.Name
            : null;

        return new ShiftRiskRequirementRecordReadModel(
            requirement.Id,
            requirement.ProductionShiftId,
            shift?.Name ?? shift?.Code ?? "—",
            requirement.ShiftDate,
            requirement.PlannedStartUtc,
            requirement.PlannedEndUtc,
            requirement.ProductionOrderReference,
            requirement.MachineAssetId,
            machineLabel,
            requirement.MachineFamilyScope,
            requirement.RequiredCompetencyCode,
            competencyLabels.GetValueOrDefault(requirement.RequiredCompetencyCode) ?? requirement.RequiredCompetencyCode,
            requirement.MinimumApprovedOperatorCount,
            requirement.IsCriticalRole,
            requirement.SourceReference,
            requirement.EffectiveFrom,
            requirement.EffectiveUntil,
            requirement.IsActive,
            requirement.Version,
            requirement.SourceReference.StartsWith("pilot-shift-risk", StringComparison.OrdinalIgnoreCase));
    }

    private static ShiftRiskAssessmentSummaryReadModel MapAssessmentSummary(
        ShiftRiskAssessment assessment,
        IReadOnlyDictionary<Guid, ProductionShift> shiftLookup,
        IReadOnlyDictionary<Guid, MachineRegistry> machineLookup)
    {
        var shift = shiftLookup.GetValueOrDefault(assessment.ProductionShiftId);
        var machineLabel = assessment.MachineAssetId.HasValue
            ? machineLookup.GetValueOrDefault(assessment.MachineAssetId.Value)?.Name
            : null;

        return new ShiftRiskAssessmentSummaryReadModel(
            assessment.Id,
            assessment.CoverageRequirementId,
            assessment.ProductionShiftId,
            shift?.Name ?? shift?.Code ?? "—",
            assessment.ShiftDate,
            assessment.ProductionOrderReference,
            assessment.MachineAssetId,
            machineLabel,
            assessment.RequiredCompetencyCode,
            assessment.MinimumRequiredCount,
            assessment.ApprovedEligibleCount,
            assessment.CoverageGap,
            assessment.Outcome,
            assessment.LeadingDriverCategory,
            assessment.LeadingReasonCode,
            assessment.RuleSetVersion,
            assessment.EvaluatedAtUtc,
            assessment.StructuredEvidenceJson.Contains("pilot", StringComparison.OrdinalIgnoreCase));
    }

    private static ShiftRiskDriverReadModel MapDriver(ShiftRiskDriver driver) =>
        new(
            driver.Id,
            driver.ReasonCode,
            driver.LabelTr,
            driver.Category,
            driver.Severity,
            driver.RequiredValue,
            driver.ObservedValue,
            driver.GapValue,
            driver.ProductionShiftId,
            driver.ProductionOrderReference,
            driver.MachineAssetId,
            driver.RequiredCompetencyCode,
            driver.EvidenceSource,
            driver.EvidenceReference,
            driver.EvidenceTimestampUtc,
            driver.FreshnessState,
            driver.RuleSetVersion,
            driver.IsLeadingRisk);

    private static ShiftRiskAssignmentEvidenceReadModel MapAssignmentEvidence(
        ShiftCoverageRiskEvaluator.AssignmentEvidence assignment,
        string? exclusionReasonCode) =>
        new(
            assignment.AssignmentId,
            assignment.WorkforcePersonId,
            assignment.EmployeeCode,
            assignment.DisplayName,
            assignment.Status,
            assignment.PlannedStartUtc,
            assignment.PlannedEndUtc,
            exclusionReasonCode,
            assignment.IsPilotData);

    private static bool MatchesOptionalReference(string? requirementReference, string? contextReference)
    {
        if (string.IsNullOrWhiteSpace(requirementReference))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(contextReference))
        {
            return true;
        }

        return string.Equals(requirementReference.Trim(), contextReference.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static ShiftRiskWriteResult ResolveCreateIdempotency(
        ShiftCoverageRequirement existing,
        string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(existing.CreateIdempotencyPayloadFingerprint))
        {
            return ShiftRiskWriteResult.Fail(
                ShiftRiskFailureCodes.IdempotencyLegacyUnverifiable,
                "Create idempotency key belongs to a legacy record that cannot be verified safely.");
        }

        if (existing.CreateIdempotencyPayloadFingerprint == fingerprint)
        {
            return ShiftRiskWriteResult.Success(existing.Id, existing.Version, true);
        }

        return ShiftRiskWriteResult.Fail(
            ShiftRiskFailureCodes.IdempotencyConflict,
            "Create idempotency key was already used with a different command payload.");
    }

    private async Task<ShiftRiskWriteResult> ResolveEvaluateIdempotencyRaceAsync(
        ShiftRiskOperationContext context,
        string idempotencyKey,
        string commandFingerprint,
        CancellationToken cancellationToken)
    {
        var raced = await dbContext.ShiftRiskAssessments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.TenantId == context.TenantId
                     && a.EvaluateIdempotencyKey == idempotencyKey,
                cancellationToken);

        if (raced is null)
        {
            throw new InvalidOperationException("Evaluate idempotency race could not be resolved.");
        }

        if (string.IsNullOrWhiteSpace(raced.EvaluateIdempotencyPayloadFingerprint))
        {
            return ShiftRiskWriteResult.Fail(
                ShiftRiskFailureCodes.IdempotencyLegacyUnverifiable,
                "Evaluate idempotency key belongs to a legacy record that cannot be verified safely.");
        }

        if (raced.EvaluateIdempotencyPayloadFingerprint != commandFingerprint)
        {
            return ShiftRiskWriteResult.Fail(
                ShiftRiskFailureCodes.IdempotencyConflict,
                "Evaluate idempotency key was already used with a different payload.");
        }

        return ShiftRiskWriteResult.SuccessAssessment(raced.Id, true);
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("23505", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) == true;

    private static string? ValidateCreateRequirement(CreateShiftCoverageRequirementCommand command)
    {
        if (command.ProductionShiftId == Guid.Empty)
        {
            return "ProductionShiftId is required.";
        }

        if (string.IsNullOrWhiteSpace(command.RequiredCompetencyCode))
        {
            return "RequiredCompetencyCode is required.";
        }

        if (command.MinimumApprovedOperatorCount < 1
            || command.MinimumApprovedOperatorCount > ShiftRiskConstants.MaxMinimumOperatorCount)
        {
            return "MinimumApprovedOperatorCount must be between 1 and 50.";
        }

        if (string.IsNullOrWhiteSpace(command.SourceReference))
        {
            return "SourceReference is required.";
        }

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return "IdempotencyKey is required.";
        }

        return null;
    }

    private void AddAuditEvent(
        ShiftRiskOperationContext context,
        Guid entityId,
        string entityType,
        string eventType,
        string actorSubjectId,
        DateTimeOffset occurredAtUtc,
        object payload)
    {
        dbContext.ShiftRiskAuditEvents.Add(new ShiftRiskAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            ActorSubjectId = actorSubjectId,
            OccurredAtUtc = occurredAtUtc,
            CorrelationId = context.CorrelationId,
        });
    }

    private static string BuildAuditSummary(string eventType) =>
        eventType switch
        {
            ShiftRiskAuditEventTypes.RequirementCreated => "Kapsama gereksinimi oluşturuldu.",
            ShiftRiskAuditEventTypes.RequirementUpdated => "Kapsama gereksinimi güncellendi.",
            ShiftRiskAuditEventTypes.AssessmentEvaluated => "Vardiya risk değerlendirmesi yapıldı.",
            _ => eventType,
        };

    private static DateOnly? ParseDateOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : DateOnly.Parse(value, CultureInfo.InvariantCulture);

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class UnavailableShiftRiskRepository : IShiftRiskRepository
{
    private const string Message = "Shift risk persistence is not available.";

    public Task<ShiftRiskEvaluationContextBuildResult> BuildEvaluationContextAsync(
        ShiftRiskOperationContext context,
        Guid productionShiftId,
        DateOnly shiftDate,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ShiftRiskEvaluationContextBuildResult(
            false, null, ShiftRiskFailureCodes.InvalidScope, Message));

    public Task<ShiftRiskRequirementListReadModel> ListRequirementsAsync(
        ShiftRiskOperationContext context,
        ShiftRiskRequirementListFilters filters,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);

    public Task<ShiftRiskRequirementRecordReadModel?> GetRequirementByIdAsync(
        ShiftRiskOperationContext context,
        Guid requirementId,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);

    public Task<ShiftRiskWriteResult> CreateRequirementAsync(
        ShiftRiskOperationContext context,
        CreateShiftCoverageRequirementCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ShiftRiskWriteResult.Fail(ShiftRiskFailureCodes.InvalidScope, Message));

    public Task<ShiftRiskWriteResult> UpdateRequirementAsync(
        ShiftRiskOperationContext context,
        UpdateShiftCoverageRequirementCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ShiftRiskWriteResult.Fail(ShiftRiskFailureCodes.InvalidScope, Message));

    public Task<ShiftRiskWriteResult> EvaluateRiskAsync(
        ShiftRiskOperationContext context,
        EvaluateShiftRiskCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ShiftRiskWriteResult.Fail(ShiftRiskFailureCodes.InvalidScope, Message));

    public Task<ShiftRiskWriteResult> EvaluateWorkforceContinuityAsync(
        ShiftRiskOperationContext context,
        EvaluateWorkforceContinuityCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ShiftRiskWriteResult.Fail(ShiftRiskFailureCodes.InvalidScope, Message));

    public Task<ShiftContinuityOverviewReadModel> GetContinuityOverviewAsync(
        ShiftRiskOperationContext context,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);

    public Task<ShiftContinuityAssessmentListReadModel> ListContinuityAssessmentsAsync(
        ShiftRiskOperationContext context,
        ShiftContinuityAssessmentListFilters filters,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);

    public Task<ShiftContinuityAssessmentDetailReadModel?> GetContinuityAssessmentByIdAsync(
        ShiftRiskOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);

    public Task<ShiftRiskOverviewReadModel> GetOverviewAsync(
        ShiftRiskOperationContext context,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);

    public Task<ShiftRiskAssessmentListReadModel> ListAssessmentsAsync(
        ShiftRiskOperationContext context,
        ShiftRiskAssessmentListFilters filters,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);

    public Task<ShiftRiskAssessmentDetailReadModel?> GetAssessmentByIdAsync(
        ShiftRiskOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);

    public Task<IReadOnlyList<ShiftRiskAuditEventReadModel>> ListAuditEventsAsync(
        ShiftRiskOperationContext context,
        string? entityType,
        Guid? entityId,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);
}
