using System.Globalization;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.ShiftRisk;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisZentra.Api.ShiftRisk;

public static class ShiftRiskEndpointRouteBuilderExtensions
{
    private const string WorkforceContinuityEvaluationsRoute = "/workforce-continuity/evaluations";
    private const string WorkforceContinuityOverviewRoute = "/workforce-continuity/overview";
    private const string WorkforceContinuityAssessmentsRoute = "/workforce-continuity/assessments";

    public static IEndpointRouteBuilder MapShiftRiskEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/shift-risks").RequireAuthorization();

        group.MapGet("/requirements", ListRequirementsAsync);
        group.MapPost("/requirements", CreateRequirementAsync);
        group.MapPut("/requirements/{id:guid}", UpdateRequirementAsync);
        group.MapPost("/evaluations", EvaluateRiskAsync);
        group.MapGet("/overview", GetOverviewAsync);
        group.MapGet("/assessments", ListAssessmentsAsync);
        group.MapGet("/assessments/{id:guid}", GetAssessmentAsync);
        group.MapGet("/audit-events", ListAuditEventsAsync);

        var continuity = group.MapGroup("/workforce-continuity");
        continuity.MapPost("/evaluations", EvaluateWorkforceContinuityAsync);
        continuity.MapGet("/overview", GetContinuityOverviewAsync);
        continuity.MapGet("/assessments", ListContinuityAssessmentsAsync);
        continuity.MapGet("/assessments/{id:guid}", GetContinuityAssessmentAsync);

        return endpoints;
    }

    private static async Task<IResult> ListRequirementsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? productionShiftId,
        [FromQuery] string? productionOrderReference,
        [FromQuery] Guid? machineAssetId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IShiftRiskRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await ShiftRiskEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ListRequirementsAsync(
            context,
            new ShiftRiskRequirementListFilters(
                productionShiftId,
                productionOrderReference,
                machineAssetId,
                isActive,
                page,
                pageSize),
            cancellationToken);

        var canManage = await ShiftRiskEndpointHelpers.HasManageAccessAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);

        return TypedResults.Ok(new
        {
            items = result.Items.Select(MapRequirement),
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
            capabilities = new { canRead = true, canManage },
        });
    }

    private static async Task<IResult> CreateRequirementAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateShiftRiskRequirementRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftRiskEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var command = new CreateShiftCoverageRequirementCommand(
            body.ProductionShiftId,
            body.ShiftDate,
            body.ProductionOrderReference,
            body.MachineAssetId,
            body.MachineFamilyScope,
            body.RequiredCompetencyCode,
            body.MinimumApprovedOperatorCount,
            body.IsCriticalRole,
            body.SourceReference,
            body.EffectiveFrom,
            body.EffectiveUntil,
            body.IsActive,
            body.IdempotencyKey);

        var result = await repository.CreateRequirementAsync(context, command, cancellationToken);
        return ShiftRiskEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateRequirementAsync(
        Guid id,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateShiftRiskRequirementRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftRiskEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.UpdateRequirementAsync(
            context,
            new UpdateShiftCoverageRequirementCommand(
                id,
                body.ExpectedVersion,
                body.MinimumApprovedOperatorCount,
                body.IsCriticalRole,
                body.SourceReference,
                body.EffectiveFrom,
                body.EffectiveUntil,
                body.IsActive),
            cancellationToken);

        return ShiftRiskEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> EvaluateRiskAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] EvaluateShiftRiskRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftRiskEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.EvaluateRiskAsync(
            context,
            new EvaluateShiftRiskCommand(
                body.ProductionShiftId,
                body.ShiftDate,
                body.ProductionOrderReference,
                body.MachineAssetId,
                body.MachineFamilyScope,
                body.RequiredCompetencyCode,
                body.IdempotencyKey),
            cancellationToken);

        return ShiftRiskEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> EvaluateWorkforceContinuityAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] EvaluateWorkforceContinuityRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftRiskEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.EvaluateWorkforceContinuityAsync(
            context,
            new EvaluateWorkforceContinuityCommand(
                body.ProductionShiftId,
                body.ShiftDate,
                body.WorkforcePersonId,
                body.ProductionOrderReference,
                body.MachineAssetId,
                body.RequiredCompetencyCode,
                body.IdempotencyKey),
            cancellationToken);

        return ShiftRiskEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> GetContinuityOverviewAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftRiskEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var overview = await repository.GetContinuityOverviewAsync(context, cancellationToken);
        var canManage = await ShiftRiskEndpointHelpers.HasManageAccessAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);

        return TypedResults.Ok(new
        {
            evaluatedContextCount = overview.EvaluatedContextCount,
            clearCount = overview.ClearCount,
            attentionCount = overview.AttentionCount,
            criticalCount = overview.CriticalCount,
            insufficientDataCount = overview.InsufficientDataCount,
            absenceImpactCount = overview.AbsenceImpactCount,
            competencyExpiredCount = overview.CompetencyExpiredCount,
            competencyExpiringCount = overview.CompetencyExpiringCount,
            staleOrUnknownCount = overview.StaleOrUnknownCount,
            assignmentOverlapCount = overview.AssignmentOverlapCount,
            coverageImpactCount = overview.CoverageImpactCount,
            capabilities = new { canRead = true, canManage },
        });
    }

    private static async Task<IResult> ListContinuityAssessmentsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? productionShiftId,
        [FromQuery] string? productionOrderReference,
        [FromQuery] Guid? machineAssetId,
        [FromQuery] Guid? workforcePersonId,
        [FromQuery] string? outcome,
        [FromQuery] string? driverCategory,
        [FromQuery] string? availabilityState,
        [FromQuery] string? freshnessState,
        [FromQuery] string? requiredCompetencyCode,
        [FromQuery] bool? criticalOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IShiftRiskRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await ShiftRiskEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ListContinuityAssessmentsAsync(
            context,
            new ShiftContinuityAssessmentListFilters(
                productionShiftId,
                productionOrderReference,
                machineAssetId,
                workforcePersonId,
                outcome,
                driverCategory,
                availabilityState,
                freshnessState,
                requiredCompetencyCode,
                criticalOnly,
                page,
                pageSize),
            cancellationToken);

        var canManage = await ShiftRiskEndpointHelpers.HasManageAccessAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);

        return TypedResults.Ok(new
        {
            items = result.Items.Select(MapContinuityAssessmentSummary),
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
            capabilities = new { canRead = true, canManage },
        });
    }

    private static async Task<IResult> GetContinuityAssessmentAsync(
        Guid id,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftRiskEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var assessment = await repository.GetContinuityAssessmentByIdAsync(context, id, cancellationToken);
        return assessment is null
            ? ShiftRiskEndpointHelpers.MapFailure(ShiftRiskFailureCodes.NotFound, "Assessment not found.")
            : TypedResults.Ok(MapContinuityAssessmentDetail(assessment));
    }

    private static async Task<IResult> GetOverviewAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftRiskEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var overview = await repository.GetOverviewAsync(context, cancellationToken);
        var canManage = await ShiftRiskEndpointHelpers.HasManageAccessAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);

        return TypedResults.Ok(new
        {
            evaluatedContextCount = overview.EvaluatedContextCount,
            clearCount = overview.ClearCount,
            attentionCount = overview.AttentionCount,
            criticalCount = overview.CriticalCount,
            insufficientDataCount = overview.InsufficientDataCount,
            coverageGapCount = overview.CoverageGapCount,
            criticalRoleGapCount = overview.CriticalRoleGapCount,
            conflictCount = overview.ConflictCount,
            capabilities = new { canRead = true, canManage },
        });
    }

    private static async Task<IResult> ListAssessmentsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? productionShiftId,
        [FromQuery] string? productionOrderReference,
        [FromQuery] Guid? machineAssetId,
        [FromQuery] string? outcome,
        [FromQuery] string? driverCategory,
        [FromQuery] bool? criticalOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IShiftRiskRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await ShiftRiskEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ListAssessmentsAsync(
            context,
            new ShiftRiskAssessmentListFilters(
                productionShiftId,
                productionOrderReference,
                machineAssetId,
                outcome,
                driverCategory,
                criticalOnly,
                page,
                pageSize),
            cancellationToken);

        var canManage = await ShiftRiskEndpointHelpers.HasManageAccessAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);

        return TypedResults.Ok(new
        {
            items = result.Items.Select(MapAssessmentSummary),
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
            capabilities = new { canRead = true, canManage },
        });
    }

    private static async Task<IResult> GetAssessmentAsync(
        Guid id,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftRiskEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var assessment = await repository.GetAssessmentByIdAsync(context, id, cancellationToken);
        return assessment is null
            ? ShiftRiskEndpointHelpers.MapFailure(ShiftRiskFailureCodes.NotFound, "Assessment not found.")
            : TypedResults.Ok(MapAssessmentDetail(assessment));
    }

    private static async Task<IResult> ListAuditEventsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftRiskEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var events = await repository.ListAuditEventsAsync(context, entityType, entityId, cancellationToken);
        return TypedResults.Ok(events.Select(e => new
        {
            id = e.Id,
            eventType = e.EventType,
            entityType = e.EntityType,
            entityId = e.EntityId,
            actorDisplayName = e.ActorSubjectId,
            occurredAtUtc = e.OccurredAtUtc,
            summary = e.Summary,
        }));
    }

    private static object MapRequirement(ShiftRiskRequirementRecordReadModel item) => new
    {
        id = item.Id,
        productionShiftId = item.ProductionShiftId,
        shiftLabel = item.ShiftLabel,
        shiftDate = item.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        plannedStartUtc = item.PlannedStartUtc,
        plannedEndUtc = item.PlannedEndUtc,
        productionOrderReference = item.ProductionOrderReference,
        machineAssetId = item.MachineAssetId,
        machineAssetLabel = item.MachineAssetLabel,
        machineFamilyScope = item.MachineFamilyScope,
        requiredCompetencyCode = item.RequiredCompetencyCode,
        requiredCompetencyLabel = item.RequiredCompetencyLabel,
        minimumApprovedOperatorCount = item.MinimumApprovedOperatorCount,
        isCriticalRole = item.IsCriticalRole,
        sourceReference = item.SourceReference,
        effectiveFrom = item.EffectiveFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        effectiveUntil = item.EffectiveUntil?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        isActive = item.IsActive,
        version = item.Version,
        isPilotData = item.IsPilotData,
    };

    private static object MapAssessmentSummary(ShiftRiskAssessmentSummaryReadModel item) => new
    {
        id = item.Id,
        coverageRequirementId = item.CoverageRequirementId,
        productionShiftId = item.ProductionShiftId,
        shiftLabel = item.ShiftLabel,
        shiftDate = item.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        productionOrderReference = item.ProductionOrderReference,
        machineAssetId = item.MachineAssetId,
        machineAssetLabel = item.MachineAssetLabel,
        requiredCompetencyCode = item.RequiredCompetencyCode,
        minimumRequiredCount = item.MinimumRequiredCount,
        approvedEligibleCount = item.ApprovedEligibleCount,
        coverageGap = item.CoverageGap,
        outcome = item.Outcome,
        leadingDriverCategory = item.LeadingDriverCategory,
        leadingReasonCode = item.LeadingReasonCode,
        ruleSetVersion = item.RuleSetVersion,
        evaluatedAtUtc = item.EvaluatedAtUtc,
        isPilotData = item.IsPilotData,
    };

    private static object MapAssessmentDetail(ShiftRiskAssessmentDetailReadModel item) => new
    {
        id = item.Id,
        coverageRequirementId = item.CoverageRequirementId,
        productionShiftId = item.ProductionShiftId,
        shiftLabel = item.ShiftLabel,
        shiftDate = item.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        plannedStartUtc = item.PlannedStartUtc,
        plannedEndUtc = item.PlannedEndUtc,
        productionOrderReference = item.ProductionOrderReference,
        machineAssetId = item.MachineAssetId,
        machineAssetLabel = item.MachineAssetLabel,
        machineFamilyScope = item.MachineFamilyScope,
        requiredCompetencyCode = item.RequiredCompetencyCode,
        minimumRequiredCount = item.MinimumRequiredCount,
        isCriticalRole = item.IsCriticalRole,
        approvedEligibleCount = item.ApprovedEligibleCount,
        coverageGap = item.CoverageGap,
        outcome = item.Outcome,
        leadingDriverCategory = item.LeadingDriverCategory,
        leadingReasonCode = item.LeadingReasonCode,
        leadingDriverId = item.LeadingDriverId,
        ruleSetVersion = item.RuleSetVersion,
        inputFingerprint = item.InputFingerprint,
        evaluatedAtUtc = item.EvaluatedAtUtc,
        structuredEvidenceJson = item.StructuredEvidenceJson,
        sourceFreshnessSnapshotJson = item.SourceFreshnessSnapshotJson,
        drivers = item.Drivers.Select(d => new
        {
            id = d.Id,
            reasonCode = d.ReasonCode,
            labelTr = d.LabelTr,
            category = d.Category,
            severity = d.Severity,
            requiredValue = d.RequiredValue,
            observedValue = d.ObservedValue,
            gapValue = d.GapValue,
            productionShiftId = d.ProductionShiftId,
            productionOrderReference = d.ProductionOrderReference,
            machineAssetId = d.MachineAssetId,
            requiredCompetencyCode = d.RequiredCompetencyCode,
            evidenceSource = d.EvidenceSource,
            evidenceReference = d.EvidenceReference,
            evidenceTimestampUtc = d.EvidenceTimestampUtc,
            freshnessState = d.FreshnessState,
            ruleSetVersion = d.RuleSetVersion,
            isLeadingRisk = d.IsLeadingRisk,
        }),
        countedAssignments = item.CountedAssignments.Select(a => new
        {
            assignmentId = a.AssignmentId,
            workforcePersonId = a.WorkforcePersonId,
            employeeCode = a.EmployeeCode,
            displayName = a.DisplayName,
            status = a.Status,
            plannedStartUtc = a.PlannedStartUtc,
            plannedEndUtc = a.PlannedEndUtc,
            isPilotData = a.IsPilotData,
        }),
        excludedAssignments = item.ExcludedAssignments.Select(a => new
        {
            assignmentId = a.AssignmentId,
            workforcePersonId = a.WorkforcePersonId,
            employeeCode = a.EmployeeCode,
            displayName = a.DisplayName,
            status = a.Status,
            plannedStartUtc = a.PlannedStartUtc,
            plannedEndUtc = a.PlannedEndUtc,
            exclusionReasonCode = a.ExclusionReasonCode,
            isPilotData = a.IsPilotData,
        }),
        isPilotData = item.IsPilotData,
    };

    private static object MapContinuityAssessmentSummary(ShiftContinuityAssessmentSummaryReadModel item) => new
    {
        id = item.Id,
        workforcePersonId = item.WorkforcePersonId,
        employeeCode = item.EmployeeCode,
        displayName = item.DisplayName,
        productionShiftId = item.ProductionShiftId,
        shiftLabel = item.ShiftLabel,
        shiftDate = item.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        productionOrderReference = item.ProductionOrderReference,
        machineAssetId = item.MachineAssetId,
        machineAssetLabel = item.MachineAssetLabel,
        requiredCompetencyCode = item.RequiredCompetencyCode,
        availabilityState = item.AvailabilityState,
        freshnessState = item.FreshnessState,
        approvedAssignmentCount = item.ApprovedAssignmentCount,
        plannedAssignmentMinutes = item.PlannedAssignmentMinutes,
        trustworthyAvailableCount = item.TrustworthyAvailableCount,
        coverageGap = item.CoverageGap,
        outcome = item.Outcome,
        leadingDriverCategory = item.LeadingDriverCategory,
        leadingReasonCode = item.LeadingReasonCode,
        ruleSetVersion = item.RuleSetVersion,
        evaluatedAtUtc = item.EvaluatedAtUtc,
        isPilotData = item.IsPilotData,
    };

    private static object MapContinuityAssessmentDetail(ShiftContinuityAssessmentDetailReadModel item) => new
    {
        id = item.Id,
        workforcePersonId = item.WorkforcePersonId,
        employeeCode = item.EmployeeCode,
        displayName = item.DisplayName,
        department = item.Department,
        jobTitle = item.JobTitle,
        isActive = item.IsActive,
        availabilityState = item.AvailabilityState,
        freshnessState = item.FreshnessState,
        sourceUpdatedAtUtc = item.SourceUpdatedAtUtc,
        productionShiftId = item.ProductionShiftId,
        shiftLabel = item.ShiftLabel,
        shiftDate = item.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        plannedStartUtc = item.PlannedStartUtc,
        plannedEndUtc = item.PlannedEndUtc,
        productionOrderReference = item.ProductionOrderReference,
        machineAssetId = item.MachineAssetId,
        machineAssetLabel = item.MachineAssetLabel,
        requiredCompetencyCode = item.RequiredCompetencyCode,
        approvedAssignmentCount = item.ApprovedAssignmentCount,
        plannedAssignmentMinutes = item.PlannedAssignmentMinutes,
        trustworthyAvailableCount = item.TrustworthyAvailableCount,
        coverageGap = item.CoverageGap,
        outcome = item.Outcome,
        leadingDriverCategory = item.LeadingDriverCategory,
        leadingReasonCode = item.LeadingReasonCode,
        leadingDriverId = item.LeadingDriverId,
        ruleSetVersion = item.RuleSetVersion,
        inputFingerprint = item.InputFingerprint,
        evaluatedAtUtc = item.EvaluatedAtUtc,
        structuredEvidenceJson = item.StructuredEvidenceJson,
        sourceFreshnessSnapshotJson = item.SourceFreshnessSnapshotJson,
        drivers = item.Drivers.Select(d => new
        {
            id = d.Id,
            reasonCode = d.ReasonCode,
            labelTr = d.LabelTr,
            category = d.Category,
            severity = d.Severity,
            requiredValue = d.RequiredValue,
            observedValue = d.ObservedValue,
            gapValue = d.GapValue,
            productionShiftId = d.ProductionShiftId,
            productionOrderReference = d.ProductionOrderReference,
            machineAssetId = d.MachineAssetId,
            requiredCompetencyCode = d.RequiredCompetencyCode,
            evidenceSource = d.EvidenceSource,
            evidenceReference = d.EvidenceReference,
            evidenceTimestampUtc = d.EvidenceTimestampUtc,
            freshnessState = d.FreshnessState,
            ruleSetVersion = d.RuleSetVersion,
            isLeadingRisk = d.IsLeadingRisk,
        }),
        competencies = item.Competencies.Select(c => new
        {
            assignmentId = c.AssignmentId,
            competencyCode = c.CompetencyCode,
            competencyLabel = c.CompetencyLabel,
            verificationStatus = c.VerificationStatus,
            validFromUtc = c.ValidFromUtc,
            validUntilUtc = c.ValidUntilUtc,
            isPilotData = c.IsPilotData,
        }),
        consideredAssignments = item.ConsideredAssignments.Select(a => new
        {
            assignmentId = a.AssignmentId,
            workforcePersonId = a.WorkforcePersonId,
            employeeCode = a.EmployeeCode,
            displayName = a.DisplayName,
            status = a.Status,
            plannedStartUtc = a.PlannedStartUtc,
            plannedEndUtc = a.PlannedEndUtc,
            isPilotData = a.IsPilotData,
        }),
        isPilotData = item.IsPilotData,
    };
}

public sealed record CreateShiftRiskRequirementRequest(
    Guid ProductionShiftId,
    string ShiftDate,
    string? ProductionOrderReference,
    Guid? MachineAssetId,
    string? MachineFamilyScope,
    string RequiredCompetencyCode,
    int MinimumApprovedOperatorCount,
    bool IsCriticalRole,
    string SourceReference,
    string? EffectiveFrom,
    string? EffectiveUntil,
    bool IsActive,
    string IdempotencyKey);

public sealed record UpdateShiftRiskRequirementRequest(
    uint ExpectedVersion,
    int MinimumApprovedOperatorCount,
    bool IsCriticalRole,
    string SourceReference,
    string? EffectiveFrom,
    string? EffectiveUntil,
    bool IsActive);

public sealed record EvaluateShiftRiskRequest(
    Guid ProductionShiftId,
    string ShiftDate,
    string? ProductionOrderReference,
    Guid? MachineAssetId,
    string? MachineFamilyScope,
    string? RequiredCompetencyCode,
    string IdempotencyKey);

public sealed record EvaluateWorkforceContinuityRequest(
    Guid ProductionShiftId,
    string ShiftDate,
    Guid WorkforcePersonId,
    string? ProductionOrderReference,
    Guid? MachineAssetId,
    string? RequiredCompetencyCode,
    string IdempotencyKey);

public static class ShiftRiskServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraShiftRisk(this IServiceCollection services)
    {
        services.AddScoped<IShiftRiskRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisZentraDbContext>();
            return dbContext is null
                ? new UnavailableShiftRiskRepository()
                : new ShiftRiskRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        return services;
    }
}
