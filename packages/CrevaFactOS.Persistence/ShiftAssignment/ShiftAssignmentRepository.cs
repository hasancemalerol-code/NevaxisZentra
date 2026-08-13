using System.Text.Json;
using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Edge;
using CrevaFactOS.Persistence.Production;
using CrevaFactOS.Persistence.ShiftWorkforce;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.ShiftAssignment;

public interface IShiftAssignmentRepository
{
    Task<ShiftAssignmentCandidatesReadModel> EvaluateCandidatesAsync(
        ShiftAssignmentOperationContext context,
        ShiftAssignmentEvaluationContext evaluationContext,
        CancellationToken cancellationToken = default);

    Task<ShiftAssignmentCandidateDetailReadModel?> EvaluateCandidateAsync(
        ShiftAssignmentOperationContext context,
        Guid personId,
        ShiftAssignmentEvaluationContext evaluationContext,
        CancellationToken cancellationToken = default);

    Task<ShiftAssignmentWriteResult> CreateAssignmentAsync(
        ShiftAssignmentOperationContext context,
        CreateShiftOperatorAssignmentCommand command,
        CancellationToken cancellationToken = default);

    Task<ShiftAssignmentWriteResult> ApproveAssignmentAsync(
        ShiftAssignmentOperationContext context,
        Guid assignmentId,
        ApproveShiftOperatorAssignmentCommand command,
        CancellationToken cancellationToken = default);

    Task<ShiftAssignmentWriteResult> CancelAssignmentAsync(
        ShiftAssignmentOperationContext context,
        Guid assignmentId,
        CancelShiftOperatorAssignmentCommand command,
        CancellationToken cancellationToken = default);

    Task<ShiftAssignmentListReadModel> ListAssignmentsAsync(
        ShiftAssignmentOperationContext context,
        ShiftAssignmentListFilters filters,
        CancellationToken cancellationToken = default);

    Task<ShiftAssignmentDetailReadModel?> GetAssignmentByIdAsync(
        ShiftAssignmentOperationContext context,
        Guid assignmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShiftAssignmentAuditEventReadModel>> ListAuditEventsAsync(
        ShiftAssignmentOperationContext context,
        Guid assignmentId,
        CancellationToken cancellationToken = default);

    Task<ShiftAssignmentEvaluationContextBuildResult> BuildEvaluationContextAsync(
        ShiftAssignmentOperationContext context,
        string productionOrderReference,
        Guid machineAssetId,
        Guid productionShiftId,
        DateOnly shiftDate,
        string? requiredCompetencyCode,
        string? machineFamilyScope,
        string? processScope,
        CancellationToken cancellationToken = default);
}

public sealed class ShiftAssignmentRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IShiftAssignmentRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<ShiftAssignmentEvaluationContextBuildResult> BuildEvaluationContextAsync(
        ShiftAssignmentOperationContext context,
        string productionOrderReference,
        Guid machineAssetId,
        Guid productionShiftId,
        DateOnly shiftDate,
        string? requiredCompetencyCode,
        string? machineFamilyScope,
        string? processScope,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var facility = await dbContext.Facilities.AsNoTracking()
                    .SingleOrDefaultAsync(
                        f => f.TenantId == context.TenantId && f.Id == context.FacilityId,
                        ct);

                if (facility is null)
                {
                    return new ShiftAssignmentEvaluationContextBuildResult(
                        false, null,
                        ShiftAssignmentFailureCodes.InvalidScope,
                        "Facility not found.");
                }

                var shift = await dbContext.ProductionShifts.AsNoTracking()
                    .SingleOrDefaultAsync(
                        s => s.TenantId == context.TenantId
                             && s.FacilityId == context.FacilityId
                             && s.Id == productionShiftId,
                        ct);

                if (shift is null)
                {
                    return new ShiftAssignmentEvaluationContextBuildResult(
                        false, null,
                        ShiftAssignmentFailureCodes.ShiftNotFound,
                        "Production shift not found.");
                }

                var machineExists = await dbContext.MachineRegistries.AsNoTracking()
                    .AnyAsync(
                        m => m.Id == machineAssetId
                             && m.TenantId == context.TenantId
                             && m.FacilityId == context.FacilityId,
                        ct);

                if (!machineExists)
                {
                    return new ShiftAssignmentEvaluationContextBuildResult(
                        false, null,
                        ShiftAssignmentFailureCodes.MachineNotFound,
                        "Machine asset not found.");
                }

                var (startUtc, endUtc) = ShiftAssignmentShiftWindowCalculator.ComputeShiftWindowUtc(
                    facility, shift, shiftDate);

                return new ShiftAssignmentEvaluationContextBuildResult(
                    true,
                    new ShiftAssignmentEvaluationContext(
                        productionOrderReference.Trim(),
                        machineAssetId,
                        productionShiftId,
                        shiftDate,
                        startUtc,
                        endUtc,
                        requiredCompetencyCode?.Trim() ?? string.Empty,
                        machineFamilyScope,
                        processScope,
                        shift.Code),
                    null,
                    null);
            },
            cancellationToken);

    public Task<ShiftAssignmentCandidatesReadModel> EvaluateCandidatesAsync(
        ShiftAssignmentOperationContext context,
        ShiftAssignmentEvaluationContext evaluationContext,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                if (string.IsNullOrWhiteSpace(evaluationContext.RequiredCompetencyCode))
                {
                    return await BuildEphemeralMissingRequiredCompetencyCandidatesAsync(
                        context,
                        evaluationContext,
                        ct);
                }

                var assessment = await CreateOrLoadAssessmentAsync(context, evaluationContext, ct);
                var candidates = await BuildCandidateSummariesAsync(context, assessment, evaluationContext, ct);
                var summary = BuildSummary(candidates);
                return new ShiftAssignmentCandidatesReadModel(
                    assessment.Id,
                    assessment.RuleSetVersion,
                    assessment.EvaluatedAtUtc,
                    summary,
                    candidates);
            },
            cancellationToken);

    public Task<ShiftAssignmentCandidateDetailReadModel?> EvaluateCandidateAsync(
        ShiftAssignmentOperationContext context,
        Guid personId,
        ShiftAssignmentEvaluationContext evaluationContext,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var person = await dbContext.ShiftWorkforcePersons.AsNoTracking()
                    .FirstOrDefaultAsync(
                        p => p.Id == personId
                             && p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId,
                        ct);

                if (person is null)
                {
                    return null;
                }

                var assessment = await LoadLatestAssessmentAsync(context, evaluationContext, ct);
                if (assessment is null)
                {
                    assessment = await PersistAssessmentAsync(
                        context,
                        evaluationContext,
                        [person],
                        ct);
                }

                var result = assessment.Results.FirstOrDefault(r => r.WorkforcePersonId == personId);
                if (result is null)
                {
                    return null;
                }

                return await MapCandidateDetailAsync(context, person, result, assessment, evaluationContext, ct);
            },
            cancellationToken);

    public Task<ShiftAssignmentWriteResult> CreateAssignmentAsync(
        ShiftAssignmentOperationContext context,
        CreateShiftOperatorAssignmentCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateCreateCommand(command);
                if (validationError is not null)
                {
                    return ShiftAssignmentWriteResult.Fail(
                        ShiftAssignmentFailureCodes.ValidationFailed,
                        validationError);
                }

                var fingerprint = ShiftAssignmentIdempotencyFingerprint.ComputeCreateAssignmentFingerprint(context, command);
                var existing = await dbContext.ShiftOperatorAssignments
                    .FirstOrDefaultAsync(
                        a => a.TenantId == context.TenantId
                             && a.CreateIdempotencyKey == command.IdempotencyKey.Trim(),
                        ct);

                if (existing is not null)
                {
                    return ResolveCreateIdempotency(existing, fingerprint);
                }

                var person = await dbContext.ShiftWorkforcePersons
                    .FirstOrDefaultAsync(
                        p => p.Id == command.WorkforcePersonId
                             && p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId,
                        ct);

                if (person is null)
                {
                    return ShiftAssignmentWriteResult.Fail(
                        ShiftAssignmentFailureCodes.PersonNotFound,
                        "Workforce person not found in facility scope.");
                }

                var assessment = await dbContext.ShiftOperatorEligibilityAssessments
                    .Include(a => a.Results)
                    .ThenInclude(r => r.Reasons)
                    .FirstOrDefaultAsync(
                        a => a.Id == command.EligibilityAssessmentId
                             && a.TenantId == context.TenantId
                             && a.FacilityId == context.FacilityId,
                        ct);

                if (assessment is null)
                {
                    return ShiftAssignmentWriteResult.Fail(
                        ShiftAssignmentFailureCodes.NotFound,
                        "Eligibility assessment not found.");
                }

                var eligibilityResult = assessment.Results
                    .FirstOrDefault(r => r.Id == command.EligibilityResultId && r.WorkforcePersonId == command.WorkforcePersonId);

                if (eligibilityResult is null)
                {
                    return ShiftAssignmentWriteResult.Fail(
                        ShiftAssignmentFailureCodes.AssessmentMismatch,
                        "Eligibility result does not match workforce person and assessment.");
                }

                if (!AssessmentMatchesCommand(assessment, command))
                {
                    return ShiftAssignmentWriteResult.Fail(
                        ShiftAssignmentFailureCodes.AssessmentMismatch,
                        "Eligibility assessment context does not match assignment command.");
                }

                if (eligibilityResult.Outcome is ShiftAssignmentEligibilityOutcomes.Ineligible
                    or ShiftAssignmentEligibilityOutcomes.InsufficientData)
                {
                    return ShiftAssignmentWriteResult.Fail(
                        ShiftAssignmentFailureCodes.EligibilityBlocked,
                        "Operator eligibility outcome blocks draft assignment creation.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var assignment = new ShiftOperatorAssignment
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    ProductionOrderReference = command.ProductionOrderReference.Trim(),
                    MachineAssetId = command.MachineAssetId,
                    ProductionShiftId = command.ProductionShiftId,
                    ShiftDate = command.ShiftDate,
                    WorkforcePersonId = command.WorkforcePersonId,
                    PlannedStartUtc = command.PlannedStartUtc,
                    PlannedEndUtc = command.PlannedEndUtc,
                    Status = ShiftAssignmentStatuses.Draft,
                    EligibilityAssessmentId = command.EligibilityAssessmentId,
                    EligibilityResultId = command.EligibilityResultId,
                    RequiredCompetencyCode = command.RequiredCompetencyCode.Trim(),
                    MachineFamilyScope = TrimOrNull(command.MachineFamilyScope),
                    ProcessScope = TrimOrNull(command.ProcessScope),
                    RuleSetVersion = assessment.RuleSetVersion,
                    CreateIdempotencyKey = command.IdempotencyKey.Trim(),
                    CreateIdempotencyPayloadFingerprint = fingerprint,
                    Version = 1,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedBy = actor,
                    UpdatedBy = actor,
                };

                dbContext.ShiftOperatorAssignments.Add(assignment);
                AddAuditEvent(
                    context,
                    assignment.Id,
                    ShiftAssignmentAuditEventTypes.AssignmentCreated,
                    actor,
                    now,
                    new { assignmentId = assignment.Id, status = assignment.Status });

                await dbContext.SaveChangesAsync(ct);
                return ShiftAssignmentWriteResult.Success(assignment.Id, assignment.Version);
            },
            cancellationToken);

    public Task<ShiftAssignmentWriteResult> ApproveAssignmentAsync(
        ShiftAssignmentOperationContext context,
        Guid assignmentId,
        ApproveShiftOperatorAssignmentCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
                {
                    return ShiftAssignmentWriteResult.Fail(
                        ShiftAssignmentFailureCodes.ValidationFailed,
                        "IdempotencyKey is required.");
                }

                var assignment = await dbContext.ShiftOperatorAssignments
                    .FirstOrDefaultAsync(
                        a => a.Id == assignmentId
                             && a.TenantId == context.TenantId
                             && a.FacilityId == context.FacilityId,
                        ct);

                if (assignment is null)
                {
                    return ShiftAssignmentWriteResult.Fail(
                        ShiftAssignmentFailureCodes.NotFound,
                        "Assignment not found.");
                }

                if (assignment.Status == ShiftAssignmentStatuses.Approved
                    && assignment.ApproveIdempotencyKey == command.IdempotencyKey.Trim())
                {
                    var replayApproveFingerprint = ShiftAssignmentIdempotencyFingerprint.ComputeApproveAssignmentFingerprint(
                        context,
                        assignmentId,
                        command.ExpectedVersion);
                    if (assignment.ApproveIdempotencyPayloadFingerprint == replayApproveFingerprint)
                    {
                        return ShiftAssignmentWriteResult.Success(assignment.Id, assignment.Version, true);
                    }

                    return ShiftAssignmentWriteResult.Fail(
                        ShiftAssignmentFailureCodes.IdempotencyConflict,
                        "Approve idempotency key was already used with a different payload.");
                }

                if (assignment.Version != command.ExpectedVersion)
                {
                    return ShiftAssignmentWriteResult.Fail(
                        ShiftAssignmentFailureCodes.ConcurrencyConflict,
                        "Assignment version mismatch.");
                }

                if (assignment.Status != ShiftAssignmentStatuses.Draft)
                {
                    return ShiftAssignmentWriteResult.Fail(
                        ShiftAssignmentFailureCodes.InvalidStatus,
                        "Only draft assignments can be approved.");
                }

                var eligibilityResult = await dbContext.ShiftOperatorEligibilityResults.AsNoTracking()
                    .FirstOrDefaultAsync(
                        r => r.Id == assignment.EligibilityResultId
                             && r.TenantId == context.TenantId,
                        ct);

                if (eligibilityResult is null)
                {
                    return ShiftAssignmentWriteResult.Fail(
                        ShiftAssignmentFailureCodes.NotFound,
                        "Linked eligibility result not found.");
                }

                if (eligibilityResult.Outcome is ShiftAssignmentEligibilityOutcomes.Ineligible
                    or ShiftAssignmentEligibilityOutcomes.InsufficientData)
                {
                    return ShiftAssignmentWriteResult.Fail(
                        ShiftAssignmentFailureCodes.EligibilityBlocked,
                        "Operator eligibility outcome blocks approval.");
                }

                if (await HasApprovedOverlapAsync(
                        context,
                        assignment.WorkforcePersonId,
                        assignment.PlannedStartUtc,
                        assignment.PlannedEndUtc,
                        assignment.Id,
                        ct))
                {
                    return ShiftAssignmentWriteResult.Fail(
                        ShiftAssignmentFailureCodes.OverlapConflict,
                        "Approved assignment overlap detected for operator.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var approveFingerprint = ShiftAssignmentIdempotencyFingerprint.ComputeApproveAssignmentFingerprint(
                    context,
                    assignmentId,
                    command.ExpectedVersion);

                assignment.Status = ShiftAssignmentStatuses.Approved;
                assignment.ApprovedBySubjectId = actor;
                assignment.ApprovedAtUtc = now;
                assignment.ApproveIdempotencyKey = command.IdempotencyKey.Trim();
                assignment.ApproveIdempotencyPayloadFingerprint = approveFingerprint;
                assignment.UpdatedAtUtc = now;
                assignment.UpdatedBy = actor;
                assignment.Version++;

                AddAuditEvent(
                    context,
                    assignment.Id,
                    ShiftAssignmentAuditEventTypes.AssignmentApproved,
                    actor,
                    now,
                    new { assignmentId = assignment.Id, status = assignment.Status });

                try
                {
                    await dbContext.SaveChangesAsync(ct);
                }
                catch (DbUpdateException)
                {
                    dbContext.ChangeTracker.Clear();
                    var raced = await dbContext.ShiftOperatorAssignments
                        .FirstOrDefaultAsync(
                            a => a.Id == assignmentId
                                 && a.TenantId == context.TenantId
                                 && a.FacilityId == context.FacilityId,
                            ct);

                    if (raced is null)
                    {
                        throw;
                    }

                    if (raced.Status == ShiftAssignmentStatuses.Approved
                        && raced.ApproveIdempotencyKey == command.IdempotencyKey.Trim())
                    {
                        var racedFingerprint = ShiftAssignmentIdempotencyFingerprint.ComputeApproveAssignmentFingerprint(
                            context,
                            assignmentId,
                            command.ExpectedVersion);
                        if (raced.ApproveIdempotencyPayloadFingerprint == racedFingerprint)
                        {
                            return ShiftAssignmentWriteResult.Success(raced.Id, raced.Version, true);
                        }

                        return ShiftAssignmentWriteResult.Fail(
                            ShiftAssignmentFailureCodes.IdempotencyConflict,
                            "Approve idempotency key was already used with a different payload.");
                    }

                    if (raced.Status == ShiftAssignmentStatuses.Approved)
                    {
                        return ShiftAssignmentWriteResult.Fail(
                            ShiftAssignmentFailureCodes.ConcurrencyConflict,
                            "Assignment was already approved by another request.");
                    }

                    return ShiftAssignmentWriteResult.Fail(
                        ShiftAssignmentFailureCodes.ConcurrencyConflict,
                        "Assignment version mismatch.");
                }

                return ShiftAssignmentWriteResult.Success(assignment.Id, assignment.Version);
            },
            cancellationToken);

    public Task<ShiftAssignmentWriteResult> CancelAssignmentAsync(
        ShiftAssignmentOperationContext context,
        Guid assignmentId,
        CancelShiftOperatorAssignmentCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var assignment = await dbContext.ShiftOperatorAssignments
                    .FirstOrDefaultAsync(
                        a => a.Id == assignmentId
                             && a.TenantId == context.TenantId
                             && a.FacilityId == context.FacilityId,
                        ct);

                if (assignment is null)
                {
                    return ShiftAssignmentWriteResult.Fail(
                        ShiftAssignmentFailureCodes.NotFound,
                        "Assignment not found.");
                }

                if (assignment.Status == ShiftAssignmentStatuses.Cancelled)
                {
                    return ShiftAssignmentWriteResult.Success(assignment.Id, assignment.Version, true);
                }

                if (assignment.Version != command.ExpectedVersion)
                {
                    return ShiftAssignmentWriteResult.Fail(
                        ShiftAssignmentFailureCodes.ConcurrencyConflict,
                        "Assignment version mismatch.");
                }

                if (assignment.Status != ShiftAssignmentStatuses.Draft
                    && assignment.Status != ShiftAssignmentStatuses.Approved)
                {
                    return ShiftAssignmentWriteResult.Fail(
                        ShiftAssignmentFailureCodes.InvalidStatus,
                        "Assignment cannot be cancelled in its current status.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                assignment.Status = ShiftAssignmentStatuses.Cancelled;
                assignment.CancelledBySubjectId = actor;
                assignment.CancelledAtUtc = now;
                assignment.CancellationReason = TrimOrNull(command.CancellationReason);
                assignment.UpdatedAtUtc = now;
                assignment.UpdatedBy = actor;
                assignment.Version++;

                AddAuditEvent(
                    context,
                    assignment.Id,
                    ShiftAssignmentAuditEventTypes.AssignmentCancelled,
                    actor,
                    now,
                    new
                    {
                        assignmentId = assignment.Id,
                        status = assignment.Status,
                        cancellationReason = assignment.CancellationReason,
                    });

                await dbContext.SaveChangesAsync(ct);
                return ShiftAssignmentWriteResult.Success(assignment.Id, assignment.Version);
            },
            cancellationToken);

    public Task<ShiftAssignmentListReadModel> ListAssignmentsAsync(
        ShiftAssignmentOperationContext context,
        ShiftAssignmentListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var page = filters.Page < 1 ? 1 : filters.Page;
                var pageSize = filters.PageSize is < 1 or > 100 ? 25 : filters.PageSize;

                var query = dbContext.ShiftOperatorAssignments.AsNoTracking()
                    .Where(a => a.TenantId == context.TenantId && a.FacilityId == context.FacilityId);

                if (!string.IsNullOrWhiteSpace(filters.ProductionOrderReference))
                {
                    var reference = filters.ProductionOrderReference.Trim();
                    query = query.Where(a => a.ProductionOrderReference == reference);
                }

                if (!string.IsNullOrWhiteSpace(filters.Status))
                {
                    var status = filters.Status.Trim();
                    query = query.Where(a => a.Status == status);
                }

                var totalCount = await query.CountAsync(ct);
                var assignments = await query
                    .OrderByDescending(a => a.PlannedStartUtc)
                    .ThenBy(a => a.CreatedAtUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                var machineLookup = await LoadMachineLookupAsync(context, ct);
                var shiftLookup = await LoadShiftLookupAsync(context, ct);
                var personLookup = await LoadPersonLookupAsync(context, assignments.Select(a => a.WorkforcePersonId), ct);

                var items = assignments.Select(a => MapRecord(
                    a,
                    machineLookup,
                    shiftLookup,
                    personLookup)).ToList();

                return new ShiftAssignmentListReadModel(items, totalCount, page, pageSize);
            },
            cancellationToken);

    public Task<ShiftAssignmentDetailReadModel?> GetAssignmentByIdAsync(
        ShiftAssignmentOperationContext context,
        Guid assignmentId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var assignment = await dbContext.ShiftOperatorAssignments.AsNoTracking()
                    .FirstOrDefaultAsync(
                        a => a.Id == assignmentId
                             && a.TenantId == context.TenantId
                             && a.FacilityId == context.FacilityId,
                        ct);

                if (assignment is null)
                {
                    return null;
                }

                var machineLookup = await LoadMachineLookupAsync(context, ct);
                var shiftLookup = await LoadShiftLookupAsync(context, ct);
                var person = await dbContext.ShiftWorkforcePersons.AsNoTracking()
                    .FirstOrDefaultAsync(
                        p => p.Id == assignment.WorkforcePersonId
                             && p.TenantId == context.TenantId,
                        ct);

                var machine = machineLookup.GetValueOrDefault(assignment.MachineAssetId);
                var shift = shiftLookup.GetValueOrDefault(assignment.ProductionShiftId);

                return new ShiftAssignmentDetailReadModel(
                    assignment.Id,
                    assignment.ProductionOrderReference,
                    assignment.MachineAssetId,
                    assignment.ProductionShiftId,
                    machine?.Name ?? "—",
                    shift?.Name ?? shift?.Code ?? "—",
                    assignment.ShiftDate,
                    assignment.WorkforcePersonId,
                    person?.EmployeeCode ?? "—",
                    person?.DisplayName ?? "—",
                    person?.Department,
                    person?.JobTitle,
                    assignment.Status,
                    assignment.PlannedStartUtc,
                    assignment.PlannedEndUtc,
                    assignment.EligibilityAssessmentId,
                    assignment.EligibilityResultId,
                    assignment.RequiredCompetencyCode,
                    assignment.MachineFamilyScope,
                    assignment.ProcessScope,
                    assignment.ApprovedBySubjectId,
                    assignment.ApprovedAtUtc,
                    assignment.CancelledBySubjectId,
                    assignment.CancelledAtUtc,
                    assignment.RuleSetVersion,
                    assignment.Version,
                    person?.SourceSystem == ShiftWorkforceSourceSystems.LocalDevelopmentPilot);
            },
            cancellationToken);

    public Task<IReadOnlyList<ShiftAssignmentAuditEventReadModel>> ListAuditEventsAsync(
        ShiftAssignmentOperationContext context,
        Guid assignmentId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var exists = await dbContext.ShiftOperatorAssignments.AsNoTracking()
                    .AnyAsync(
                        a => a.Id == assignmentId
                             && a.TenantId == context.TenantId
                             && a.FacilityId == context.FacilityId,
                        ct);

                if (!exists)
                {
                    return (IReadOnlyList<ShiftAssignmentAuditEventReadModel>)[];
                }

                var events = await dbContext.ShiftOperatorAssignmentAuditEvents.AsNoTracking()
                    .Where(e => e.TenantId == context.TenantId
                                && e.FacilityId == context.FacilityId
                                && e.EntityId == assignmentId)
                    .OrderByDescending(e => e.OccurredAtUtc)
                    .ToListAsync(ct);

                return events.Select(e => new ShiftAssignmentAuditEventReadModel(
                    e.Id,
                    e.EventType,
                    e.ActorSubjectId,
                    e.OccurredAtUtc,
                    BuildAuditSummary(e.EventType, e.PayloadJson))).ToList();
            },
            cancellationToken);

    private async Task<ShiftOperatorEligibilityAssessment> CreateOrLoadAssessmentAsync(
        ShiftAssignmentOperationContext context,
        ShiftAssignmentEvaluationContext evaluationContext,
        CancellationToken cancellationToken)
    {
        var existing = await LoadLatestAssessmentAsync(context, evaluationContext, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var persons = await dbContext.ShiftWorkforcePersons
            .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId)
            .ToListAsync(cancellationToken);

        return await PersistAssessmentAsync(context, evaluationContext, persons, cancellationToken);
    }

    private async Task<ShiftOperatorEligibilityAssessment?> LoadLatestAssessmentAsync(
        ShiftAssignmentOperationContext context,
        ShiftAssignmentEvaluationContext evaluationContext,
        CancellationToken cancellationToken)
    {
        var fingerprint = ShiftAssignmentIdempotencyFingerprint.ComputeAssessmentInputFingerprint(evaluationContext);
        var assessment = await dbContext.ShiftOperatorEligibilityAssessments
            .Include(a => a.Results)
            .ThenInclude(r => r.Reasons)
            .Where(a => a.TenantId == context.TenantId
                        && a.FacilityId == context.FacilityId
                        && a.InputFingerprint == fingerprint)
            .OrderByDescending(a => a.EvaluatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return assessment;
    }

    private async Task<ShiftOperatorEligibilityAssessment> PersistAssessmentAsync(
        ShiftAssignmentOperationContext context,
        ShiftAssignmentEvaluationContext evaluationContext,
        IReadOnlyList<ShiftWorkforcePerson> persons,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var actor = context.IdentitySubjectId.ToString();
        var fingerprint = ShiftAssignmentIdempotencyFingerprint.ComputeAssessmentInputFingerprint(evaluationContext);
        var missingRequiredCode = string.IsNullOrWhiteSpace(evaluationContext.RequiredCompetencyCode);

        var competencyDefinitions = await dbContext.ShiftWorkforceCompetencyDefinitions.AsNoTracking()
            .Where(d => d.TenantId == context.TenantId)
            .ToListAsync(cancellationToken);

        var competencyCodeLabel = missingRequiredCode
            ? "—"
            : competencyDefinitions
                .FirstOrDefault(d => d.Code == evaluationContext.RequiredCompetencyCode.Trim())?.DisplayLabelTr
                ?? evaluationContext.RequiredCompetencyCode.Trim();

        var assignments = await dbContext.ShiftWorkforceCompetencyAssignments.AsNoTracking()
            .Include(a => a.CompetencyDefinition)
            .Where(a => a.TenantId == context.TenantId && a.FacilityId == context.FacilityId)
            .ToListAsync(cancellationToken);

        var approvedAssignments = await dbContext.ShiftOperatorAssignments.AsNoTracking()
            .Where(a => a.TenantId == context.TenantId
                        && a.FacilityId == context.FacilityId
                        && a.Status == ShiftAssignmentStatuses.Approved)
            .ToListAsync(cancellationToken);

        var assessment = new ShiftOperatorEligibilityAssessment
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            ProductionOrderReference = evaluationContext.ProductionOrderReference.Trim(),
            MachineAssetId = evaluationContext.MachineAssetId,
            ProductionShiftId = evaluationContext.ProductionShiftId,
            ShiftDate = evaluationContext.ShiftDate,
            PlannedStartUtc = evaluationContext.PlannedStartUtc,
            PlannedEndUtc = evaluationContext.PlannedEndUtc,
            RequiredCompetencyCode = evaluationContext.RequiredCompetencyCode.Trim(),
            MachineFamilyScope = TrimOrNull(evaluationContext.MachineFamilyScope),
            ProcessScope = TrimOrNull(evaluationContext.ProcessScope),
            RuleSetVersion = ShiftAssignmentConstants.RuleSetVersion,
            InputFingerprint = fingerprint,
            EvaluatedAtUtc = now,
            CreatedBy = actor,
        };

        foreach (var person in persons)
        {
            var matchingCompetency = FindMatchingCompetency(
                assignments,
                person.Id,
                evaluationContext.RequiredCompetencyCode,
                evaluationContext.MachineFamilyScope,
                evaluationContext.ProcessScope);

            var hasOverlap = approvedAssignments.Any(a =>
                a.WorkforcePersonId == person.Id
                && ShiftOperatorEligibilityEvaluator.WindowsOverlap(
                    a.PlannedStartUtc,
                    a.PlannedEndUtc,
                    evaluationContext.PlannedStartUtc,
                    evaluationContext.PlannedEndUtc));

            var evaluation = ShiftOperatorEligibilityEvaluator.EvaluatePerson(
                new ShiftOperatorEligibilityEvaluator.PersonEvaluationInput(
                    person,
                    matchingCompetency,
                    competencyCodeLabel,
                    evaluationContext.ShiftCode,
                    now,
                    hasOverlap),
                missingRequiredCode);

            var result = new ShiftOperatorEligibilityResult
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                AssessmentId = assessment.Id,
                WorkforcePersonId = person.Id,
                Outcome = evaluation.Outcome,
                PrimaryReasonCode = evaluation.PrimaryReasonCode,
                EmployeeCodeSnapshot = person.EmployeeCode,
                DisplayNameSnapshot = person.DisplayName,
            };

            foreach (var reason in evaluation.Reasons)
            {
                result.Reasons.Add(new ShiftOperatorEligibilityReason
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    ResultId = result.Id,
                    ReasonCode = reason.ReasonCode,
                    LabelTr = ShiftAssignmentReasonLabels.GetLabelTr(reason.ReasonCode),
                    Severity = reason.Severity,
                    RequiredValue = reason.RequiredValue,
                    ObservedValue = reason.ObservedValue,
                    EvidenceSource = reason.EvidenceSource,
                    EvidenceTimestampUtc = reason.EvidenceTimestampUtc,
                    FreshnessState = reason.FreshnessState,
                });
            }

            assessment.Results.Add(result);
        }

        dbContext.ShiftOperatorEligibilityAssessments.Add(assessment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return assessment;
    }

    private async Task<ShiftAssignmentCandidatesReadModel> BuildEphemeralMissingRequiredCompetencyCandidatesAsync(
        ShiftAssignmentOperationContext context,
        ShiftAssignmentEvaluationContext evaluationContext,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var persons = await dbContext.ShiftWorkforcePersons.AsNoTracking()
            .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId)
            .ToListAsync(cancellationToken);

        var competencyAssignments = await dbContext.ShiftWorkforceCompetencyAssignments.AsNoTracking()
            .Include(a => a.CompetencyDefinition)
            .Where(a => a.TenantId == context.TenantId && a.FacilityId == context.FacilityId)
            .ToListAsync(cancellationToken);

        var candidates = new List<ShiftAssignmentCandidateSummaryReadModel>();
        foreach (var person in persons.OrderBy(p => p.DisplayName))
        {
            var evaluation = ShiftOperatorEligibilityEvaluator.EvaluatePerson(
                new ShiftOperatorEligibilityEvaluator.PersonEvaluationInput(
                    person,
                    null,
                    "—",
                    evaluationContext.ShiftCode,
                    now,
                    false),
                missingRequiredCompetencyCode: true);

            var ephemeralResult = new ShiftOperatorEligibilityResult
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                AssessmentId = Guid.Empty,
                WorkforcePersonId = person.Id,
                Outcome = evaluation.Outcome,
                PrimaryReasonCode = evaluation.PrimaryReasonCode,
                EmployeeCodeSnapshot = person.EmployeeCode,
                DisplayNameSnapshot = person.DisplayName,
            };

            candidates.Add(MapCandidateSummary(person, ephemeralResult, competencyAssignments, evaluationContext));
        }

        var summary = BuildSummary(candidates);
        return new ShiftAssignmentCandidatesReadModel(
            Guid.Empty,
            ShiftAssignmentConstants.RuleSetVersion,
            now,
            summary,
            candidates);
    }

    private async Task<IReadOnlyList<ShiftAssignmentCandidateSummaryReadModel>> BuildCandidateSummariesAsync(
        ShiftAssignmentOperationContext context,
        ShiftOperatorEligibilityAssessment assessment,
        ShiftAssignmentEvaluationContext evaluationContext,
        CancellationToken cancellationToken)
    {
        var personIds = assessment.Results.Select(r => r.WorkforcePersonId).ToList();
        var persons = await dbContext.ShiftWorkforcePersons.AsNoTracking()
            .Where(p => p.TenantId == context.TenantId && personIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        var competencyAssignments = await dbContext.ShiftWorkforceCompetencyAssignments.AsNoTracking()
            .Include(a => a.CompetencyDefinition)
            .Where(a => a.TenantId == context.TenantId && personIds.Contains(a.WorkforcePersonId))
            .ToListAsync(cancellationToken);

        var summaries = new List<ShiftAssignmentCandidateSummaryReadModel>();
        foreach (var result in assessment.Results.OrderBy(r => r.DisplayNameSnapshot))
        {
            var person = persons.FirstOrDefault(p => p.Id == result.WorkforcePersonId);
            if (person is null)
            {
                continue;
            }

            summaries.Add(MapCandidateSummary(person, result, competencyAssignments, evaluationContext));
        }

        return summaries;
    }

    private async Task<ShiftAssignmentCandidateDetailReadModel> MapCandidateDetailAsync(
        ShiftAssignmentOperationContext context,
        ShiftWorkforcePerson person,
        ShiftOperatorEligibilityResult result,
        ShiftOperatorEligibilityAssessment assessment,
        ShiftAssignmentEvaluationContext evaluationContext,
        CancellationToken cancellationToken)
    {
        var competencyAssignments = await dbContext.ShiftWorkforceCompetencyAssignments.AsNoTracking()
            .Include(a => a.CompetencyDefinition)
            .Where(a => a.TenantId == context.TenantId && a.WorkforcePersonId == person.Id)
            .ToListAsync(cancellationToken);

        var summary = MapCandidateSummary(person, result, competencyAssignments, evaluationContext);
        var reasons = result.Reasons
            .OrderBy(r => r.Severity)
            .ThenBy(r => r.ReasonCode)
            .Select(r => new ShiftAssignmentEligibilityReasonReadModel(
                r.ReasonCode,
                r.LabelTr,
                r.Severity,
                r.RequiredValue,
                r.ObservedValue,
                r.EvidenceSource,
                r.EvidenceTimestampUtc,
                r.FreshnessState))
            .ToList();

        return new ShiftAssignmentCandidateDetailReadModel(
            summary.PersonId,
            summary.EligibilityResultId,
            summary.EmployeeCode,
            summary.DisplayName,
            summary.Department,
            summary.JobTitle,
            summary.AvailabilityState,
            summary.CompetencySummary,
            summary.FreshnessState,
            summary.Outcome,
            summary.PrimaryReasonCode,
            summary.HasOverlapWarning,
            summary.IsPilotData,
            person.SourceSystem,
            person.SourceUpdatedAtUtc,
            person.SourceShiftReference,
            assessment.Id,
            assessment.RuleSetVersion,
            reasons);
    }

    private static ShiftAssignmentCandidateSummaryReadModel MapCandidateSummary(
        ShiftWorkforcePerson person,
        ShiftOperatorEligibilityResult result,
        IReadOnlyList<ShiftWorkforceCompetencyAssignment> competencyAssignments,
        ShiftAssignmentEvaluationContext evaluationContext)
    {
        var personCompetencies = competencyAssignments
            .Where(a => a.WorkforcePersonId == person.Id)
            .ToList();

        var competencySummary = BuildCompetencySummary(personCompetencies);
        var hasOverlapWarning = result.Reasons.Any(r => r.ReasonCode == ShiftAssignmentReasonCodes.AssignmentOverlap);

        return new ShiftAssignmentCandidateSummaryReadModel(
            person.Id,
            result.Id,
            person.EmployeeCode,
            person.DisplayName,
            person.Department,
            person.JobTitle,
            person.AvailabilityState,
            competencySummary,
            person.FreshnessState,
            result.Outcome,
            result.PrimaryReasonCode,
            hasOverlapWarning,
            person.SourceSystem == ShiftWorkforceSourceSystems.LocalDevelopmentPilot);
    }

    private static ShiftAssignmentEligibilitySummaryReadModel BuildSummary(
        IReadOnlyList<ShiftAssignmentCandidateSummaryReadModel> candidates) =>
        new(
            candidates.Count(c => c.Outcome == ShiftAssignmentEligibilityOutcomes.Eligible),
            candidates.Count(c => c.Outcome == ShiftAssignmentEligibilityOutcomes.Conditional),
            candidates.Count(c => c.Outcome == ShiftAssignmentEligibilityOutcomes.Ineligible),
            candidates.Count(c => c.Outcome == ShiftAssignmentEligibilityOutcomes.InsufficientData));

    private static ShiftAssignmentRecordReadModel MapRecord(
        ShiftOperatorAssignment assignment,
        IReadOnlyDictionary<Guid, MachineRegistry> machineLookup,
        IReadOnlyDictionary<Guid, ProductionShift> shiftLookup,
        IReadOnlyDictionary<Guid, ShiftWorkforcePerson> personLookup)
    {
        var machine = machineLookup.GetValueOrDefault(assignment.MachineAssetId);
        var shift = shiftLookup.GetValueOrDefault(assignment.ProductionShiftId);
        var person = personLookup.GetValueOrDefault(assignment.WorkforcePersonId);

        return new ShiftAssignmentRecordReadModel(
            assignment.Id,
            assignment.ProductionOrderReference,
            machine?.Name ?? "—",
            shift?.Name ?? shift?.Code ?? "—",
            assignment.ShiftDate,
            person?.EmployeeCode ?? "—",
            person?.DisplayName ?? "—",
            assignment.Status,
            assignment.PlannedStartUtc,
            assignment.PlannedEndUtc,
            assignment.ApprovedBySubjectId,
            assignment.ApprovedAtUtc,
            assignment.CancelledBySubjectId,
            assignment.CancelledAtUtc,
            assignment.RuleSetVersion,
            assignment.Version,
            person?.SourceSystem == ShiftWorkforceSourceSystems.LocalDevelopmentPilot);
    }

    private async Task<Dictionary<Guid, MachineRegistry>> LoadMachineLookupAsync(
        ShiftAssignmentOperationContext context,
        CancellationToken cancellationToken)
    {
        var machines = await dbContext.MachineRegistries.AsNoTracking()
            .Where(m => m.TenantId == context.TenantId && m.FacilityId == context.FacilityId)
            .ToListAsync(cancellationToken);

        return machines.ToDictionary(m => m.Id);
    }

    private async Task<Dictionary<Guid, ProductionShift>> LoadShiftLookupAsync(
        ShiftAssignmentOperationContext context,
        CancellationToken cancellationToken)
    {
        var shifts = await dbContext.ProductionShifts.AsNoTracking()
            .Where(s => s.TenantId == context.TenantId && s.FacilityId == context.FacilityId)
            .ToListAsync(cancellationToken);

        return shifts.ToDictionary(s => s.Id);
    }

    private async Task<Dictionary<Guid, ShiftWorkforcePerson>> LoadPersonLookupAsync(
        ShiftAssignmentOperationContext context,
        IEnumerable<Guid> personIds,
        CancellationToken cancellationToken)
    {
        var ids = personIds.Distinct().ToList();
        var persons = await dbContext.ShiftWorkforcePersons.AsNoTracking()
            .Where(p => p.TenantId == context.TenantId && ids.Contains(p.Id))
            .ToListAsync(cancellationToken);

        return persons.ToDictionary(p => p.Id);
    }

    private async Task<bool> HasApprovedOverlapAsync(
        ShiftAssignmentOperationContext context,
        Guid workforcePersonId,
        DateTimeOffset plannedStartUtc,
        DateTimeOffset plannedEndUtc,
        Guid excludeAssignmentId,
        CancellationToken cancellationToken)
    {
        var approved = await dbContext.ShiftOperatorAssignments.AsNoTracking()
            .Where(a => a.TenantId == context.TenantId
                        && a.FacilityId == context.FacilityId
                        && a.WorkforcePersonId == workforcePersonId
                        && a.Status == ShiftAssignmentStatuses.Approved
                        && a.Id != excludeAssignmentId)
            .ToListAsync(cancellationToken);

        return approved.Any(a =>
            ShiftOperatorEligibilityEvaluator.WindowsOverlap(
                a.PlannedStartUtc,
                a.PlannedEndUtc,
                plannedStartUtc,
                plannedEndUtc));
    }

    private static ShiftWorkforceCompetencyAssignment? FindMatchingCompetency(
        IReadOnlyList<ShiftWorkforceCompetencyAssignment> assignments,
        Guid personId,
        string requiredCompetencyCode,
        string? machineFamilyScope,
        string? processScope)
    {
        if (string.IsNullOrWhiteSpace(requiredCompetencyCode))
        {
            return null;
        }

        var code = requiredCompetencyCode.Trim();
        return assignments.FirstOrDefault(a =>
            a.WorkforcePersonId == personId
            && a.CompetencyDefinition?.Code == code
            && ScopeMatches(a.MachineFamilyScope, machineFamilyScope)
            && ScopeMatches(a.ProcessScope, processScope));
    }

    private static bool ScopeMatches(string? assignmentScope, string? requestedScope)
    {
        if (string.IsNullOrWhiteSpace(requestedScope))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(assignmentScope))
        {
            return true;
        }

        return string.Equals(assignmentScope.Trim(), requestedScope.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool AssessmentMatchesCommand(
        ShiftOperatorEligibilityAssessment assessment,
        CreateShiftOperatorAssignmentCommand command) =>
        assessment.ProductionOrderReference == command.ProductionOrderReference.Trim()
        && assessment.MachineAssetId == command.MachineAssetId
        && assessment.ProductionShiftId == command.ProductionShiftId
        && assessment.ShiftDate == command.ShiftDate
        && assessment.PlannedStartUtc == command.PlannedStartUtc
        && assessment.PlannedEndUtc == command.PlannedEndUtc
        && assessment.RequiredCompetencyCode == command.RequiredCompetencyCode.Trim()
        && string.Equals(
            TrimOrNull(assessment.MachineFamilyScope),
            TrimOrNull(command.MachineFamilyScope),
            StringComparison.OrdinalIgnoreCase)
        && string.Equals(
            TrimOrNull(assessment.ProcessScope),
            TrimOrNull(command.ProcessScope),
            StringComparison.OrdinalIgnoreCase);

    private static ShiftAssignmentWriteResult ResolveCreateIdempotency(
        ShiftOperatorAssignment existing,
        string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(existing.CreateIdempotencyPayloadFingerprint))
        {
            return ShiftAssignmentWriteResult.Fail(
                ShiftAssignmentFailureCodes.IdempotencyLegacyUnverifiable,
                "Create idempotency key belongs to a legacy record that cannot be verified safely.");
        }

        if (existing.CreateIdempotencyPayloadFingerprint == fingerprint)
        {
            return ShiftAssignmentWriteResult.Success(existing.Id, existing.Version, true);
        }

        return ShiftAssignmentWriteResult.Fail(
            ShiftAssignmentFailureCodes.IdempotencyConflict,
            "Create idempotency key was already used with a different command payload.");
    }

    private static string? ValidateCreateCommand(CreateShiftOperatorAssignmentCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ProductionOrderReference))
        {
            return "ProductionOrderReference is required.";
        }

        if (command.MachineAssetId == Guid.Empty)
        {
            return "MachineAssetId is required.";
        }

        if (command.ProductionShiftId == Guid.Empty)
        {
            return "ProductionShiftId is required.";
        }

        if (command.WorkforcePersonId == Guid.Empty)
        {
            return "WorkforcePersonId is required.";
        }

        if (string.IsNullOrWhiteSpace(command.RequiredCompetencyCode))
        {
            return "RequiredCompetencyCode is required.";
        }

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return "IdempotencyKey is required.";
        }

        if (command.PlannedEndUtc <= command.PlannedStartUtc)
        {
            return "PlannedEndUtc must be after PlannedStartUtc.";
        }

        if (command.EligibilityAssessmentId == Guid.Empty || command.EligibilityResultId == Guid.Empty)
        {
            return "Eligibility assessment and result identifiers are required.";
        }

        return null;
    }

    private void AddAuditEvent(
        ShiftAssignmentOperationContext context,
        Guid entityId,
        string eventType,
        string actorSubjectId,
        DateTimeOffset occurredAtUtc,
        object payload)
    {
        dbContext.ShiftOperatorAssignmentAuditEvents.Add(new ShiftOperatorAssignmentAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            EventType = eventType,
            EntityType = ShiftAssignmentEntityTypes.Assignment,
            EntityId = entityId,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            ActorSubjectId = actorSubjectId,
            OccurredAtUtc = occurredAtUtc,
            CorrelationId = context.CorrelationId,
        });
    }

    private static string BuildAuditSummary(string eventType, string payloadJson)
    {
        return eventType switch
        {
            ShiftAssignmentAuditEventTypes.AssignmentCreated => "Operatör atama taslağı oluşturuldu.",
            ShiftAssignmentAuditEventTypes.AssignmentApproved => "Operatör atama onaylandı.",
            ShiftAssignmentAuditEventTypes.AssignmentCancelled => "Operatör atama iptal edildi.",
            _ => payloadJson,
        };
    }

    private static string BuildCompetencySummary(List<ShiftWorkforceCompetencyAssignment> competencies)
    {
        if (competencies.Count == 0)
        {
            return "Yetkinlik kaydı yok";
        }

        var verifiedCount = competencies.Count(c =>
            c.VerificationStatus == ShiftWorkforceVerificationStatuses.Verified);
        var attentionCount = competencies.Count(c =>
            ShiftWorkforceValueConversions.RequiresCompetencyAttention(c, DateTimeOffset.UtcNow));

        if (attentionCount > 0)
        {
            return $"{verifiedCount} doğrulandı, {attentionCount} dikkat gerektiriyor";
        }

        return $"{verifiedCount} doğrulandı";
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class UnavailableShiftAssignmentRepository : IShiftAssignmentRepository
{
    private const string Message = "Shift assignment persistence is not available.";

    public Task<ShiftAssignmentEvaluationContextBuildResult> BuildEvaluationContextAsync(
        ShiftAssignmentOperationContext context,
        string productionOrderReference,
        Guid machineAssetId,
        Guid productionShiftId,
        DateOnly shiftDate,
        string? requiredCompetencyCode,
        string? machineFamilyScope,
        string? processScope,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ShiftAssignmentEvaluationContextBuildResult(
            false, null, ShiftAssignmentFailureCodes.InvalidScope, Message));

    public Task<ShiftAssignmentCandidatesReadModel> EvaluateCandidatesAsync(
        ShiftAssignmentOperationContext context,
        ShiftAssignmentEvaluationContext evaluationContext,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);

    public Task<ShiftAssignmentCandidateDetailReadModel?> EvaluateCandidateAsync(
        ShiftAssignmentOperationContext context,
        Guid personId,
        ShiftAssignmentEvaluationContext evaluationContext,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);

    public Task<ShiftAssignmentWriteResult> CreateAssignmentAsync(
        ShiftAssignmentOperationContext context,
        CreateShiftOperatorAssignmentCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ShiftAssignmentWriteResult.Fail(ShiftAssignmentFailureCodes.InvalidScope, Message));

    public Task<ShiftAssignmentWriteResult> ApproveAssignmentAsync(
        ShiftAssignmentOperationContext context,
        Guid assignmentId,
        ApproveShiftOperatorAssignmentCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ShiftAssignmentWriteResult.Fail(ShiftAssignmentFailureCodes.InvalidScope, Message));

    public Task<ShiftAssignmentWriteResult> CancelAssignmentAsync(
        ShiftAssignmentOperationContext context,
        Guid assignmentId,
        CancelShiftOperatorAssignmentCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ShiftAssignmentWriteResult.Fail(ShiftAssignmentFailureCodes.InvalidScope, Message));

    public Task<ShiftAssignmentListReadModel> ListAssignmentsAsync(
        ShiftAssignmentOperationContext context,
        ShiftAssignmentListFilters filters,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);

    public Task<ShiftAssignmentDetailReadModel?> GetAssignmentByIdAsync(
        ShiftAssignmentOperationContext context,
        Guid assignmentId,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);

    public Task<IReadOnlyList<ShiftAssignmentAuditEventReadModel>> ListAuditEventsAsync(
        ShiftAssignmentOperationContext context,
        Guid assignmentId,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);
}

