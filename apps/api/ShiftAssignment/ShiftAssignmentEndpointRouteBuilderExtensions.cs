using System.Globalization;
using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.ShiftAssignment;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.ShiftAssignment;

public static class ShiftAssignmentEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapShiftAssignmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/shift-assignments").RequireAuthorization();

        group.MapGet("/candidates", ListCandidatesAsync);
        group.MapGet("/candidates/{personId:guid}", GetCandidateAsync);
        group.MapPost("/assignments", CreateAssignmentAsync);
        group.MapPost("/assignments/{id:guid}/approve", ApproveAssignmentAsync);
        group.MapPost("/assignments/{id:guid}/cancel", CancelAssignmentAsync);
        group.MapGet("/assignments", ListAssignmentsAsync);
        group.MapGet("/assignments/{id:guid}", GetAssignmentAsync);
        group.MapGet("/assignments/{id:guid}/audit-events", ListAuditEventsAsync);

        return endpoints;
    }

    private static async Task<IResult> ListCandidatesAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? productionOrderReference,
        [FromQuery] Guid? machineAssetId,
        [FromQuery] Guid? productionShiftId,
        [FromQuery] string? shiftDate,
        [FromQuery] string? requiredCompetencyCode,
        [FromQuery] string? machineFamilyScope,
        [FromQuery] string? processScope,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftAssignmentRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftAssignmentEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var (evaluationContext, buildFailure) = await BuildEvaluationContextAsync(
            repository, context, productionOrderReference, machineAssetId, productionShiftId,
            shiftDate, requiredCompetencyCode, machineFamilyScope, processScope, cancellationToken);
        if (buildFailure is not null) return buildFailure;

        var result = await repository.EvaluateCandidatesAsync(context, evaluationContext!, cancellationToken);
        var canManage = await ShiftAssignmentEndpointHelpers.HasManageAccessAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);

        return TypedResults.Ok(new
        {
            assessmentId = result.AssessmentId,
            ruleSetVersion = result.RuleSetVersion,
            evaluatedAtUtc = result.EvaluatedAtUtc,
            summary = new
            {
                eligibleCount = result.Summary.EligibleCount,
                conditionalCount = result.Summary.ConditionalCount,
                ineligibleCount = result.Summary.IneligibleCount,
                insufficientDataCount = result.Summary.InsufficientDataCount,
            },
            candidates = result.Candidates.Select(MapCandidateSummary),
            capabilities = new { canRead = true, canManage },
        });
    }

    private static async Task<IResult> GetCandidateAsync(
        Guid personId,
        [FromQuery] Guid? facilityId,
        [FromQuery] string? productionOrderReference,
        [FromQuery] Guid? machineAssetId,
        [FromQuery] Guid? productionShiftId,
        [FromQuery] string? shiftDate,
        [FromQuery] string? requiredCompetencyCode,
        [FromQuery] string? machineFamilyScope,
        [FromQuery] string? processScope,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftAssignmentRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftAssignmentEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var (evaluationContext, buildFailure) = await BuildEvaluationContextAsync(
            repository, context, productionOrderReference, machineAssetId, productionShiftId,
            shiftDate, requiredCompetencyCode, machineFamilyScope, processScope, cancellationToken);
        if (buildFailure is not null) return buildFailure;

        var candidate = await repository.EvaluateCandidateAsync(
            context, personId, evaluationContext!, cancellationToken);

        return candidate is null
            ? ShiftAssignmentEndpointHelpers.MapFailure(
                ShiftAssignmentFailureCodes.PersonNotFound,
                "Workforce person not found in facility scope.")
            : TypedResults.Ok(MapCandidateDetail(candidate));
    }

    private static async Task<IResult> CreateAssignmentAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateShiftAssignmentRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftAssignmentRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftAssignmentEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        if (!DateOnly.TryParse(body.ShiftDate, out var shiftDate))
        {
            return ShiftAssignmentEndpointHelpers.MapFailure(
                ShiftAssignmentFailureCodes.ValidationFailed,
                "ShiftDate must be a valid date.");
        }

        if (!DateTimeOffset.TryParse(body.PlannedStartUtc, out var plannedStartUtc)
            || !DateTimeOffset.TryParse(body.PlannedEndUtc, out var plannedEndUtc))
        {
            return ShiftAssignmentEndpointHelpers.MapFailure(
                ShiftAssignmentFailureCodes.ValidationFailed,
                "Planned start and end timestamps must be valid.");
        }

        var command = new CreateShiftOperatorAssignmentCommand(
            body.ProductionOrderReference,
            body.MachineAssetId,
            body.ProductionShiftId,
            shiftDate,
            body.WorkforcePersonId,
            plannedStartUtc,
            plannedEndUtc,
            body.EligibilityAssessmentId,
            body.EligibilityResultId,
            body.RequiredCompetencyCode,
            body.MachineFamilyScope,
            body.ProcessScope,
            body.IdempotencyKey);

        var result = await repository.CreateAssignmentAsync(context, command, cancellationToken);
        return ShiftAssignmentEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> ApproveAssignmentAsync(
        Guid id,
        [FromQuery] Guid? facilityId,
        [FromBody] ApproveShiftAssignmentRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftAssignmentRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftAssignmentEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ApproveAssignmentAsync(
            context,
            id,
            new ApproveShiftOperatorAssignmentCommand(body.ExpectedVersion, body.IdempotencyKey),
            cancellationToken);

        return ShiftAssignmentEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> CancelAssignmentAsync(
        Guid id,
        [FromQuery] Guid? facilityId,
        [FromBody] CancelShiftAssignmentRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftAssignmentRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftAssignmentEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.CancelAssignmentAsync(
            context,
            id,
            new CancelShiftOperatorAssignmentCommand(body.ExpectedVersion, body.CancellationReason),
            cancellationToken);

        return ShiftAssignmentEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> ListAssignmentsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? productionOrderReference,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IShiftAssignmentRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await ShiftAssignmentEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ListAssignmentsAsync(
            context,
            new ShiftAssignmentListFilters(productionOrderReference, status, page, pageSize),
            cancellationToken);

        var canManage = await ShiftAssignmentEndpointHelpers.HasManageAccessAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);

        return TypedResults.Ok(new
        {
            items = result.Items.Select(MapAssignmentRecord),
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
            capabilities = new { canRead = true, canManage },
        });
    }

    private static async Task<IResult> GetAssignmentAsync(
        Guid id,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftAssignmentRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftAssignmentEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var assignment = await repository.GetAssignmentByIdAsync(context, id, cancellationToken);
        return assignment is null
            ? ShiftAssignmentEndpointHelpers.MapFailure(ShiftAssignmentFailureCodes.NotFound, "Assignment not found.")
            : TypedResults.Ok(MapAssignmentDetail(assignment));
    }

    private static async Task<IResult> ListAuditEventsAsync(
        Guid id,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftAssignmentRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftAssignmentEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var events = await repository.ListAuditEventsAsync(context, id, cancellationToken);
        return TypedResults.Ok(events.Select(e => new
        {
            id = e.Id,
            eventType = e.EventType,
            actorDisplayName = e.ActorSubjectId,
            occurredAtUtc = e.OccurredAtUtc,
            summary = e.Summary,
        }));
    }

    private static async Task<(ShiftAssignmentEvaluationContext? Context, IResult? Failure)> BuildEvaluationContextAsync(
        IShiftAssignmentRepository repository,
        ShiftAssignmentOperationContext context,
        string? productionOrderReference,
        Guid? machineAssetId,
        Guid? productionShiftId,
        string? shiftDate,
        string? requiredCompetencyCode,
        string? machineFamilyScope,
        string? processScope,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(productionOrderReference)
            || machineAssetId is null || machineAssetId == Guid.Empty
            || productionShiftId is null || productionShiftId == Guid.Empty
            || string.IsNullOrWhiteSpace(shiftDate))
        {
            return (null, ShiftAssignmentEndpointHelpers.MapFailure(
                ShiftAssignmentFailureCodes.ValidationFailed,
                "Production order, machine, shift, and shift date are required."));
        }

        if (!DateOnly.TryParse(shiftDate, out var parsedShiftDate))
        {
            return (null, ShiftAssignmentEndpointHelpers.MapFailure(
                ShiftAssignmentFailureCodes.ValidationFailed,
                "ShiftDate must be a valid date."));
        }

        var buildResult = await repository.BuildEvaluationContextAsync(
            context,
            productionOrderReference,
            machineAssetId.Value,
            productionShiftId.Value,
            parsedShiftDate,
            requiredCompetencyCode,
            machineFamilyScope,
            processScope,
            cancellationToken);

        if (!buildResult.Succeeded || buildResult.Context is null)
        {
            return (null, ShiftAssignmentEndpointHelpers.MapFailure(
                buildResult.FailureCode ?? ShiftAssignmentFailureCodes.ValidationFailed,
                buildResult.FailureReason));
        }

        return (buildResult.Context, null);
    }

    private static object MapCandidateSummary(ShiftAssignmentCandidateSummaryReadModel candidate) => new
    {
        personId = candidate.PersonId,
        eligibilityResultId = candidate.EligibilityResultId,
        employeeCode = candidate.EmployeeCode,
        displayName = candidate.DisplayName,
        department = candidate.Department,
        jobTitle = candidate.JobTitle,
        availabilityState = candidate.AvailabilityState,
        competencySummary = candidate.CompetencySummary,
        freshnessState = candidate.FreshnessState,
        outcome = candidate.Outcome,
        primaryReasonCode = candidate.PrimaryReasonCode,
        hasOverlapWarning = candidate.HasOverlapWarning,
        isPilotData = candidate.IsPilotData,
    };

    private static object MapCandidateDetail(ShiftAssignmentCandidateDetailReadModel candidate) => new
    {
        personId = candidate.PersonId,
        eligibilityResultId = candidate.EligibilityResultId,
        employeeCode = candidate.EmployeeCode,
        displayName = candidate.DisplayName,
        department = candidate.Department,
        jobTitle = candidate.JobTitle,
        availabilityState = candidate.AvailabilityState,
        competencySummary = candidate.CompetencySummary,
        freshnessState = candidate.FreshnessState,
        outcome = candidate.Outcome,
        primaryReasonCode = candidate.PrimaryReasonCode,
        hasOverlapWarning = candidate.HasOverlapWarning,
        isPilotData = candidate.IsPilotData,
        sourceSystem = candidate.SourceSystem,
        sourceUpdatedAtUtc = candidate.SourceUpdatedAtUtc,
        shiftLabel = candidate.ShiftLabel,
        assessmentId = candidate.AssessmentId,
        ruleSetVersion = candidate.RuleSetVersion,
        reasons = candidate.Reasons.Select(r => new
        {
            reasonCode = r.ReasonCode,
            labelTr = r.LabelTr,
            severity = r.Severity,
            requiredValue = r.RequiredValue,
            observedValue = r.ObservedValue,
            evidenceSource = r.EvidenceSource,
            evidenceTimestampUtc = r.EvidenceTimestampUtc,
            freshnessState = r.FreshnessState,
        }),
    };

    private static object MapAssignmentRecord(ShiftAssignmentRecordReadModel item) => new
    {
        id = item.Id,
        productionOrderReference = item.ProductionOrderReference,
        machineAssetLabel = item.MachineAssetLabel,
        shiftLabel = item.ShiftLabel,
        shiftDate = item.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        employeeCode = item.EmployeeCode,
        displayName = item.DisplayName,
        status = item.Status,
        plannedStartUtc = item.PlannedStartUtc,
        plannedEndUtc = item.PlannedEndUtc,
        approvedByDisplayName = item.ApprovedBySubjectId,
        approvedAtUtc = item.ApprovedAtUtc,
        cancelledByDisplayName = item.CancelledBySubjectId,
        cancelledAtUtc = item.CancelledAtUtc,
        ruleSetVersion = item.RuleSetVersion,
        version = item.Version,
        isPilotData = item.IsPilotData,
    };

    private static object MapAssignmentDetail(ShiftAssignmentDetailReadModel item) => new
    {
        id = item.Id,
        productionOrderReference = item.ProductionOrderReference,
        machineAssetId = item.MachineAssetId,
        productionShiftId = item.ProductionShiftId,
        machineAssetLabel = item.MachineAssetLabel,
        shiftLabel = item.ShiftLabel,
        shiftDate = item.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        workforcePersonId = item.WorkforcePersonId,
        employeeCode = item.EmployeeCode,
        displayName = item.DisplayName,
        department = item.Department,
        jobTitle = item.JobTitle,
        status = item.Status,
        plannedStartUtc = item.PlannedStartUtc,
        plannedEndUtc = item.PlannedEndUtc,
        eligibilityAssessmentId = item.EligibilityAssessmentId,
        eligibilityResultId = item.EligibilityResultId,
        requiredCompetencyCode = item.RequiredCompetencyCode,
        machineFamilyScope = item.MachineFamilyScope,
        processScope = item.ProcessScope,
        approvedByDisplayName = item.ApprovedBySubjectId,
        approvedAtUtc = item.ApprovedAtUtc,
        cancelledByDisplayName = item.CancelledBySubjectId,
        cancelledAtUtc = item.CancelledAtUtc,
        ruleSetVersion = item.RuleSetVersion,
        version = item.Version,
        isPilotData = item.IsPilotData,
    };
}

public sealed record CreateShiftAssignmentRequest(
    string ProductionOrderReference,
    Guid MachineAssetId,
    Guid ProductionShiftId,
    string ShiftDate,
    Guid WorkforcePersonId,
    string PlannedStartUtc,
    string PlannedEndUtc,
    Guid EligibilityAssessmentId,
    Guid EligibilityResultId,
    string RequiredCompetencyCode,
    string? MachineFamilyScope,
    string? ProcessScope,
    string IdempotencyKey);

public sealed record ApproveShiftAssignmentRequest(uint ExpectedVersion, string IdempotencyKey);

public sealed record CancelShiftAssignmentRequest(uint ExpectedVersion, string? CancellationReason);

public static class ShiftAssignmentServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSShiftAssignment(this IServiceCollection services)
    {
        services.AddScoped<IShiftAssignmentRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableShiftAssignmentRepository()
                : new ShiftAssignmentRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        return services;
    }
}
