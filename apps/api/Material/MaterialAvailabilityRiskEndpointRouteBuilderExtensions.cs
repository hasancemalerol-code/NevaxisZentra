using CrevaFactOS.Api.AiTasks;
using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Erp;
using CrevaFactOS.Persistence.Material;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.Material;

internal static class MaterialAvailabilityRiskEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMaterialAvailabilityRiskEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/materials/availability-risks").RequireAuthorization();

        group.MapGet("/workspace", GetWorkspaceAsync);
        group.MapGet("/{assessmentId:guid}", GetAssessmentAsync);
        group.MapGet("/{assessmentId:guid}/history", GetHistoryAsync);
        group.MapPost("/evaluate", EvaluateAsync);
        group.MapMaterialAvailabilityRiskFollowUpEndpoints();

        return endpoints;
    }

    private static async Task<IResult> GetWorkspaceAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? materialId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IMaterialAvailabilityRiskRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var canManage = await HasManageAccessAsync(facilityId!.Value, facilityAccessResolver, cancellationToken);
        var canCreateTask = await AiTaskEndpointHelpers.HasCreateAccessAsync(
            facilityId.Value, facilityAccessResolver, cancellationToken);
        var workspace = await repository.GetWorkspaceAsync(
            context,
            new MaterialAvailabilityRiskListFilters(materialId, status, page, pageSize),
            canManage,
            canCreateTask,
            cancellationToken);

        return TypedResults.Ok(new
        {
            ruleSetVersion = workspace.RuleSetVersion,
            automationDisclaimer = workspace.AutomationDisclaimer,
            kpis = new
            {
                shortageCount = workspace.ShortageCount,
                sufficientCount = workspace.SufficientCount,
                insufficientDataCount = workspace.InsufficientDataCount,
                sourceConflictCount = workspace.SourceConflictCount,
                requiresReviewCount = workspace.RequiresReviewCount,
                taskLinkedCount = workspace.TaskLinkedCount,
                dataReviewCount = workspace.DataReviewCount,
                latestEvaluatedAtUtc = workspace.LatestEvaluatedAtUtc,
            },
            capabilities = new
            {
                canRead = workspace.CanRead,
                canManage = workspace.CanManage,
                canCreateTask = workspace.CanCreateTask,
            },
            assessments = workspace.Assessments.Select(MaterialAvailabilityRiskApiMapper.MapRow).ToList(),
        });
    }

    private static async Task<IResult> GetAssessmentAsync(
        Guid assessmentId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialAvailabilityRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var followUp = await repository.GetFollowUpDetailAsync(context, assessmentId, cancellationToken);
        return followUp is null
            ? MaterialEndpointHelpers.MapRequirementFailure(MaterialRequirementFailureCodes.NotFound, "Availability risk assessment not found.")
            : TypedResults.Ok(MaterialAvailabilityRiskFollowUpApiMapper.MapDetail(followUp));
    }

    private static async Task<IResult> GetHistoryAsync(
        Guid assessmentId,
        [FromQuery] Guid? facilityId,
        [FromQuery] int limit = 10,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IMaterialAvailabilityRiskRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var current = await repository.GetAssessmentByIdAsync(context, assessmentId, cancellationToken);
        if (current is null)
        {
            return MaterialEndpointHelpers.MapRequirementFailure(
                MaterialRequirementFailureCodes.NotFound,
                "Availability risk assessment not found.");
        }

        var history = await repository.ListAssessmentHistoryAsync(
            context,
            current.MaterialId,
            current.RequirementProfileId,
            limit,
            cancellationToken);

        return TypedResults.Ok(history.Select(MaterialAvailabilityRiskApiMapper.MapSummary).ToList());
    }

    private static async Task<IResult> EvaluateAsync(
        [FromBody] EvaluateMaterialAvailabilityRiskRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialRequirementRepository requirementRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        if (!request.RequirementProfileId.HasValue)
        {
            return MaterialEndpointHelpers.MapRequirementFailure(
                MaterialRequirementFailureCodes.ValidationFailed,
                "Requirement profile id is required.");
        }

        var hasErpRead = await HasErpReadAccessAsync(context.FacilityId, facilityAccessResolver, cancellationToken);
        var result = await requirementRepository.EvaluateAvailabilityRiskAsync(
            context,
            new EvaluateMaterialAvailabilityRiskCommand(
                request.MaterialId,
                request.RequirementProfileId,
                request.IdempotencyKey),
            hasErpRead,
            cancellationToken);

        return MaterialEndpointHelpers.MapRequirementWriteResult(result, isCreate: true);
    }

    private static async Task<bool> HasManageAccessAsync(
        Guid facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId, MaterialPermissionKeys.Manage),
            cancellationToken);
        return accessResult.Succeeded && accessResult.Access is not null;
    }

    private static async Task<bool> HasErpReadAccessAsync(
        Guid facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId, ErpPermissionKeys.Read),
            cancellationToken);
        return accessResult.Succeeded && accessResult.Access is not null;
    }
}

internal sealed record EvaluateMaterialAvailabilityRiskRequest(
    Guid MaterialId,
    Guid? RequirementProfileId,
    string? IdempotencyKey);

internal static class MaterialAvailabilityRiskApiMapper
{
    internal static object MapRow(MaterialAvailabilityRiskRowReadModel row) => new
    {
        id = row.Id,
        materialId = row.MaterialId,
        materialCode = row.MaterialCode,
        materialName = row.MaterialName,
        requirementProfileId = row.RequirementProfileId,
        productionOrderReference = row.ProductionOrderReference,
        status = row.Status,
        requiredQuantityGrams = row.RequiredQuantityGrams,
        erpAvailableQuantity = row.ErpAvailableQuantity,
        erpReservedQuantity = row.ErpReservedQuantity,
        erpUnit = row.ErpUnit,
        availableAfterReservationQuantity = row.AvailableAfterReservationQuantity,
        shortageQuantity = row.ShortageQuantity,
        sourceFreshness = row.SourceFreshness,
        evaluatedAtUtc = row.EvaluatedAtUtc,
        version = row.Version,
        followUpStatus = row.FollowUpStatus,
        linkedAiTaskId = row.LinkedAiTaskId,
        canCreateTaskDraft = row.CanCreateTaskDraft,
        taskDraftPurpose = row.TaskDraftPurpose,
    };

    internal static object MapDetail(MaterialAvailabilityRiskDetailReadModel detail) => new
    {
        id = detail.Id,
        facilityId = detail.FacilityId,
        materialId = detail.MaterialId,
        materialCode = detail.MaterialCode,
        materialName = detail.MaterialName,
        requirementProfileId = detail.RequirementProfileId,
        productionOrderReference = detail.ProductionOrderReference,
        productReference = detail.ProductReference,
        moldReference = detail.MoldReference,
        status = detail.Status,
        ruleSetVersion = detail.RuleSetVersion,
        inputFingerprint = detail.InputFingerprint,
        requiredQuantityGrams = detail.RequiredQuantityGrams,
        erpAvailableQuantity = detail.ErpAvailableQuantity,
        erpReservedQuantity = detail.ErpReservedQuantity,
        erpUnit = detail.ErpUnit,
        availableAfterReservationQuantity = detail.AvailableAfterReservationQuantity,
        shortageQuantity = detail.ShortageQuantity,
        erpSourceUpdatedAtUtc = detail.ErpSourceUpdatedAtUtc,
        sourceFreshness = detail.SourceFreshness,
        drivers = detail.Drivers.Select(d => new { code = d.Code, label = d.Label, value = d.Value, state = d.State }).ToList(),
        evidence = detail.Evidence.Select(e => new
        {
            field = e.Field,
            value = e.Value,
            source = e.Source,
            recordedAtUtc = e.RecordedAtUtc,
            isMissing = e.IsMissing,
        }).ToList(),
        evaluatedAtUtc = detail.EvaluatedAtUtc,
        createdAtUtc = detail.CreatedAtUtc,
        version = detail.Version,
    };

    internal static object MapSummary(MaterialAvailabilityRiskAssessmentSummaryReadModel summary) => new
    {
        id = summary.Id,
        status = summary.Status,
        evaluatedAtUtc = summary.EvaluatedAtUtc,
        ruleSetVersion = summary.RuleSetVersion,
    };
}
