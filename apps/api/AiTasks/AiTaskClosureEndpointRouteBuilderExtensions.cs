using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.AiTasks;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisZentra.Api.AiTasks;

public static class AiTaskClosureEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAiTaskClosureEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/ai-tasks").RequireAuthorization();

        group.MapGet("/{taskId:guid}/evidence", ListEvidenceAsync);
        group.MapPost("/{taskId:guid}/evidence", SubmitEvidenceAsync);
        group.MapGet("/{taskId:guid}/closure-validation", GetClosureValidationAsync);
        group.MapPost("/{taskId:guid}/resolution", UpdateResolutionAsync);
        group.MapPost("/{taskId:guid}/closure-request", RequestClosureAsync);
        group.MapPost("/{taskId:guid}/approve-closure", ApproveClosureAsync);
        group.MapPost("/{taskId:guid}/reject-closure", RejectClosureAsync);
        group.MapPost("/{taskId:guid}/reopen", ReopenAsync);

        return endpoints;
    }

    private static async Task<IResult> ListEvidenceAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskClosureRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var items = await repository.ListEvidenceAsync(context, taskId, cancellationToken);
        return TypedResults.Ok(new { items = items.Select(MapEvidence) });
    }

    private static async Task<IResult> GetClosureValidationAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskClosureRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var validation = await repository.GetClosureValidationAsync(context, taskId, cancellationToken);
        return TypedResults.Ok(MapValidation(validation));
    }

    private static async Task<IResult> SubmitEvidenceAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        [FromBody] SubmitAiTaskEvidenceRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskClosureRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.EvidenceManage, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.SubmitEvidenceAsync(
            context,
            new SubmitAiTaskEvidenceCommand(
                taskId,
                request.ExpectedVersion,
                request.EvidenceType,
                request.Title,
                request.Description,
                request.ReferenceMetadataJson,
                request.SourceModule,
                request.SourceEntityId),
            cancellationToken);

        return MapWriteResult(result);
    }

    private static async Task<IResult> UpdateResolutionAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateAiTaskResolutionRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskClosureRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.ClosureRequest, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.UpdateResolutionAsync(
            context,
            new UpdateAiTaskResolutionCommand(
                taskId,
                request.ExpectedVersion,
                request.ResolutionSummary,
                request.ResolutionCode,
                request.ClosureReason),
            cancellationToken);

        return MapWriteResult(result);
    }

    private static async Task<IResult> RequestClosureAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        [FromBody] RequestAiTaskClosureRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskClosureRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.ClosureRequest, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.RequestClosureAsync(
            context,
            new RequestAiTaskClosureCommand(taskId, request.ExpectedVersion, request.Note),
            cancellationToken);

        return MapWriteResult(result);
    }

    private static async Task<IResult> ApproveClosureAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        [FromBody] ApproveAiTaskClosureRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskClosureRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.ClosureApprove, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ApproveClosureAsync(
            context,
            new ApproveAiTaskClosureCommand(taskId, request.ExpectedVersion, request.Note),
            cancellationToken);

        return MapWriteResult(result);
    }

    private static async Task<IResult> RejectClosureAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        [FromBody] RejectAiTaskClosureRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskClosureRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.ClosureApprove, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.RejectClosureAsync(
            context,
            new RejectAiTaskClosureCommand(taskId, request.ExpectedVersion, request.RejectionReason),
            cancellationToken);

        return MapWriteResult(result);
    }

    private static async Task<IResult> ReopenAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        [FromBody] ReopenAiTaskRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskClosureRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.Reopen, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ReopenAsync(
            context,
            new ReopenAiTaskCommand(taskId, request.ExpectedVersion, request.ReopenReason),
            cancellationToken);

        return MapWriteResult(result);
    }

    private static IResult MapWriteResult(AiTaskWriteResult result) =>
        result.Succeeded
            ? TypedResults.Ok(new { id = result.EntityId, version = result.Version, idempotentReplay = result.IdempotentReplay })
            : AiTaskEndpointHelpers.MapFailure(result.FailureCode ?? AiTaskFailureCodes.Unavailable, result.FailureReason ?? "Operation failed.");

    private static object MapEvidence(AiTaskEvidenceReadModel item) => new
    {
        id = item.Id,
        taskId = item.TaskId,
        evidenceType = item.EvidenceType,
        title = item.Title,
        description = item.Description,
        referenceMetadataJson = item.ReferenceMetadataJson,
        sourceModule = item.SourceModule,
        sourceEntityId = item.SourceEntityId,
        submittedByMembershipId = item.SubmittedByMembershipId,
        submittedByLabel = item.SubmittedByLabel,
        submittedAtUtc = item.SubmittedAtUtc,
    };

    private static object MapValidation(AiTaskClosureValidationResult validation) => new
    {
        isValid = validation.IsValid,
        issues = validation.Issues.Select(issue => new { code = issue.Code, message = issue.Message }),
    };
}

public sealed record SubmitAiTaskEvidenceRequest(
    uint ExpectedVersion,
    string EvidenceType,
    string Title,
    string? Description,
    string? ReferenceMetadataJson,
    string? SourceModule,
    string? SourceEntityId);

public sealed record UpdateAiTaskResolutionRequest(
    uint ExpectedVersion,
    string ResolutionSummary,
    string ResolutionCode,
    string? ClosureReason);

public sealed record RequestAiTaskClosureRequest(uint ExpectedVersion, string? Note);

public sealed record ApproveAiTaskClosureRequest(uint ExpectedVersion, string? Note);

public sealed record RejectAiTaskClosureRequest(uint ExpectedVersion, string RejectionReason);

public sealed record ReopenAiTaskRequest(uint ExpectedVersion, string ReopenReason);
