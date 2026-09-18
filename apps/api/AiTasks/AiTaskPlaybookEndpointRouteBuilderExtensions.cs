using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.AiTasks;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisZentra.Api.AiTasks;

public static class AiTaskPlaybookEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAiTaskPlaybookEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var playbooks = endpoints.MapGroup("/api/ai-tasks/playbooks").RequireAuthorization();
        playbooks.MapGet("/", ListPlaybooksAsync);
        playbooks.MapPost("/", CreatePlaybookAsync);
        playbooks.MapGet("/{id:guid}", GetPlaybookAsync);
        playbooks.MapPut("/{id:guid}", UpdatePlaybookAsync);
        playbooks.MapPost("/{id:guid}/enable", EnablePlaybookAsync);
        playbooks.MapPost("/{id:guid}/disable", DisablePlaybookAsync);
        playbooks.MapPost("/validate-graph", ValidateGraphAsync);
        playbooks.MapPost("/{id:guid}/runs", StartRunAsync);

        var runs = endpoints.MapGroup("/api/ai-tasks/playbook-runs").RequireAuthorization();
        runs.MapGet("/", ListRunsAsync);
        runs.MapGet("/{runId:guid}", GetRunAsync);
        runs.MapPost("/{runId:guid}/cancel", CancelRunAsync);
        runs.MapPost("/{runId:guid}/steps/{stepId:guid}/block", BlockRunStepAsync);
        runs.MapPost("/{runId:guid}/steps/{stepId:guid}/unblock", UnblockRunStepAsync);
        runs.MapPost("/{runId:guid}/steps/{stepId:guid}/skip", SkipRunStepAsync);

        return endpoints;
    }

    private static async Task<IResult> ListPlaybooksAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskPlaybookRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.PlaybookRead, cancellationToken);
        if (context is null) return failResult!;

        var items = await repository.ListPlaybooksAsync(context, cancellationToken);
        return TypedResults.Ok(new { items = items.Select(MapSummary) });
    }

    private static async Task<IResult> GetPlaybookAsync(
        Guid id,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskPlaybookRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.PlaybookRead, cancellationToken);
        if (context is null) return failResult!;

        var playbook = await repository.GetPlaybookAsync(context, id, cancellationToken);
        return playbook is null ? Results.NotFound() : TypedResults.Ok(MapDetail(playbook));
    }

    private static async Task<IResult> CreatePlaybookAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateAiTaskPlaybookRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskPlaybookRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.PlaybookManage, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.CreatePlaybookAsync(
            context,
            new CreateAiTaskPlaybookCommand(
                request.PlaybookKey,
                request.Name,
                request.Description,
                request.Category,
                request.TriggerContext,
                request.FacilityId ?? facilityId,
                request.Enabled,
                request.EffectiveFromUtc,
                request.EffectiveUntilUtc,
                request.Steps.Select(MapStep).ToList(),
                request.Dependencies.Select(MapDependency).ToList()),
            cancellationToken);

        return MapWriteResult(result);
    }

    private static async Task<IResult> UpdatePlaybookAsync(
        Guid id,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateAiTaskPlaybookRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskPlaybookRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.PlaybookManage, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.UpdatePlaybookAsync(
            context,
            new UpdateAiTaskPlaybookCommand(
                id,
                request.ExpectedVersion,
                request.Name,
                request.Description,
                request.Category,
                request.TriggerContext,
                request.Enabled,
                request.EffectiveFromUtc,
                request.EffectiveUntilUtc,
                request.Steps.Select(MapStep).ToList(),
                request.Dependencies.Select(MapDependency).ToList(),
                request.ChangeReason),
            cancellationToken);

        return MapWriteResult(result);
    }

    private static async Task<IResult> EnablePlaybookAsync(
        Guid id,
        [FromQuery] Guid? facilityId,
        [FromBody] PlaybookToggleRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskPlaybookRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.PlaybookManage, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.SetPlaybookEnabledAsync(context, id, request.ExpectedVersion, true, cancellationToken);
        return MapWriteResult(result);
    }

    private static async Task<IResult> DisablePlaybookAsync(
        Guid id,
        [FromQuery] Guid? facilityId,
        [FromBody] PlaybookToggleRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskPlaybookRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.PlaybookManage, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.SetPlaybookEnabledAsync(context, id, request.ExpectedVersion, false, cancellationToken);
        return MapWriteResult(result);
    }

    private static async Task<IResult> ValidateGraphAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] ValidateAiTaskPlaybookGraphRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        IAiTaskPlaybookRepository repository,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.PlaybookManage, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ValidateGraphAsync(
            request.Steps.Select(MapStep).ToList(),
            request.Dependencies.Select(MapDependency).ToList());

        return TypedResults.Ok(new { isValid = result.IsValid, errors = result.Errors });
    }

    private static async Task<IResult> ListRunsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? playbookId,
        [FromQuery] string? status,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskPlaybookRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.PlaybookRead, cancellationToken);
        if (context is null) return failResult!;

        var items = await repository.ListRunsAsync(context, playbookId, status, cancellationToken);
        return TypedResults.Ok(new { items = items.Select(MapRunSummary) });
    }

    private static async Task<IResult> StartRunAsync(
        Guid id,
        [FromQuery] Guid? facilityId,
        [FromBody] StartAiTaskPlaybookRunRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskPlaybookRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.PlaybookRun, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.StartRunAsync(
            context,
            new StartAiTaskPlaybookRunCommand(
                id,
                request.IdempotencyKey ?? $"{id:N}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
                request.TriggerSource ?? AiTaskPlaybookTriggerSources.Manual,
                request.SourceTaskId,
                request.SourceEntityJson,
                request.CorrelationId ?? correlationContext.CorrelationId,
                request.ContextValues),
            cancellationToken);

        return MapWriteResult(result);
    }

    private static async Task<IResult> GetRunAsync(
        Guid runId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskPlaybookRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.PlaybookRead, cancellationToken);
        if (context is null) return failResult!;

        var run = await repository.GetRunAsync(context, runId, cancellationToken);
        return run is null ? Results.NotFound() : TypedResults.Ok(MapRunDetail(run));
    }

    private static async Task<IResult> CancelRunAsync(
        Guid runId,
        [FromQuery] Guid? facilityId,
        [FromBody] CancelAiTaskPlaybookRunRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskPlaybookRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.PlaybookOverride, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.CancelRunAsync(
            context,
            new CancelAiTaskPlaybookRunCommand(runId, request.Reason),
            cancellationToken);

        return MapWriteResult(result);
    }

    private static async Task<IResult> BlockRunStepAsync(
        Guid runId,
        Guid stepId,
        [FromQuery] Guid? facilityId,
        [FromBody] PlaybookRunStepActionRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskPlaybookRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.PlaybookOverride, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.BlockRunStepAsync(
            context,
            new BlockAiTaskPlaybookRunStepCommand(runId, stepId, request.Reason),
            cancellationToken);

        return MapWriteResult(result);
    }

    private static async Task<IResult> UnblockRunStepAsync(
        Guid runId,
        Guid stepId,
        [FromQuery] Guid? facilityId,
        [FromBody] PlaybookRunStepActionRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskPlaybookRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.PlaybookOverride, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.UnblockRunStepAsync(
            context,
            new UnblockAiTaskPlaybookRunStepCommand(runId, stepId, request.Reason),
            cancellationToken);

        return MapWriteResult(result);
    }

    private static async Task<IResult> SkipRunStepAsync(
        Guid runId,
        Guid stepId,
        [FromQuery] Guid? facilityId,
        [FromBody] PlaybookRunStepActionRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskPlaybookRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.PlaybookOverride, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.SkipRunStepAsync(
            context,
            new SkipAiTaskPlaybookRunStepCommand(runId, stepId, request.Reason),
            cancellationToken);

        return MapWriteResult(result);
    }

    private static IResult MapWriteResult(AiTaskPlaybookWriteResult result) =>
        result.Success
            ? TypedResults.Ok(new { id = result.EntityId, version = result.Version, runId = result.RunId })
            : AiTaskEndpointHelpers.MapFailure(result.FailureCode ?? AiTaskFailureCodes.Unavailable, result.Message ?? "Operation failed.");

    private static object MapSummary(AiTaskPlaybookSummaryReadModel playbook) => new
    {
        playbook.Id,
        playbook.PlaybookKey,
        playbook.Name,
        playbook.Category,
        playbook.FacilityId,
        playbook.Version,
        playbook.Enabled,
        playbook.StepCount,
        playbook.UpdatedAtUtc,
        playbook.LastRunAtUtc,
    };

    private static object MapDetail(AiTaskPlaybookDetailReadModel playbook) => new
    {
        playbook.Id,
        playbook.PlaybookKey,
        playbook.Name,
        playbook.Description,
        playbook.Category,
        playbook.TriggerContext,
        playbook.FacilityId,
        playbook.Version,
        playbook.Enabled,
        playbook.EffectiveFromUtc,
        playbook.EffectiveUntilUtc,
        steps = playbook.Steps,
        dependencies = playbook.Dependencies,
        playbook.CreatedAtUtc,
        playbook.UpdatedAtUtc,
    };

    private static object MapRunSummary(AiTaskPlaybookRunSummaryReadModel run) => new
    {
        run.Id,
        run.PlaybookId,
        run.PlaybookName,
        run.Status,
        run.FacilityId,
        run.TriggerSource,
        run.SourceTaskId,
        run.RequiredStepsCompleted,
        run.RequiredStepsTotal,
        run.OptionalStepsCompleted,
        run.OptionalStepsTotal,
        run.BlockedStepsCount,
        run.ApprovalsPending,
        run.AtRisk,
        run.StartedAtUtc,
        run.CompletedAtUtc,
    };

    private static object MapRunDetail(AiTaskPlaybookRunDetailReadModel run) => new
    {
        run.Id,
        run.PlaybookId,
        run.PlaybookName,
        run.PlaybookVersion,
        run.Status,
        run.FacilityId,
        run.TriggerSource,
        run.SourceTaskId,
        run.SourceEntityJson,
        run.ParentTaskId,
        run.RequiredStepsCompleted,
        run.RequiredStepsTotal,
        run.OptionalStepsCompleted,
        run.OptionalStepsTotal,
        run.BlockedStepsCount,
        run.ApprovalsPending,
        run.AtRisk,
        run.BlockReason,
        run.CriticalPathJson,
        steps = run.Steps,
        run.StartedAtUtc,
        run.CompletedAtUtc,
        run.CancelledAtUtc,
        run.CancellationReason,
    };

    private static AiTaskPlaybookStepDefinition MapStep(PlaybookStepRequest request) =>
        new(
            request.StepKey,
            request.Name,
            request.Description,
            request.SortOrder,
            request.StepType,
            request.Required,
            request.DefaultPriority,
            request.AssignmentPolicyJson,
            request.SlaPolicyJson,
            request.EvidencePolicyJson,
            request.ClosurePolicyJson,
            request.Condition is null
                ? null
                : new AiTaskPlaybookConditionDefinition(
                    request.Condition.Field,
                    request.Condition.Operator,
                    request.Condition.Value),
            request.TargetDurationMinutes);

    private static AiTaskPlaybookDependencyDefinition MapDependency(PlaybookDependencyRequest request) =>
        new(request.StepKey, request.DependsOnStepKey, request.DependencyType);
}

public sealed record CreateAiTaskPlaybookRequest(
    string PlaybookKey,
    string Name,
    string? Description,
    string Category,
    string? TriggerContext,
    Guid? FacilityId,
    bool Enabled,
    DateTimeOffset? EffectiveFromUtc,
    DateTimeOffset? EffectiveUntilUtc,
    IReadOnlyList<PlaybookStepRequest> Steps,
    IReadOnlyList<PlaybookDependencyRequest> Dependencies);

public sealed record UpdateAiTaskPlaybookRequest(
    uint ExpectedVersion,
    string Name,
    string? Description,
    string Category,
    string? TriggerContext,
    bool Enabled,
    DateTimeOffset? EffectiveFromUtc,
    DateTimeOffset? EffectiveUntilUtc,
    IReadOnlyList<PlaybookStepRequest> Steps,
    IReadOnlyList<PlaybookDependencyRequest> Dependencies,
    string ChangeReason);

public sealed record PlaybookToggleRequest(uint ExpectedVersion);

public sealed record ValidateAiTaskPlaybookGraphRequest(
    IReadOnlyList<PlaybookStepRequest> Steps,
    IReadOnlyList<PlaybookDependencyRequest> Dependencies);

public sealed record StartAiTaskPlaybookRunRequest(
    string? IdempotencyKey,
    string? TriggerSource,
    Guid? SourceTaskId,
    string? SourceEntityJson,
    Guid? CorrelationId,
    IReadOnlyDictionary<string, string>? ContextValues);

public sealed record CancelAiTaskPlaybookRunRequest(string Reason);

public sealed record PlaybookRunStepActionRequest(string Reason);

public sealed record PlaybookStepRequest(
    string StepKey,
    string Name,
    string? Description,
    int SortOrder,
    string StepType,
    bool Required,
    string DefaultPriority,
    string? AssignmentPolicyJson,
    string? SlaPolicyJson,
    string? EvidencePolicyJson,
    string? ClosurePolicyJson,
    PlaybookConditionRequest? Condition,
    int? TargetDurationMinutes);

public sealed record PlaybookConditionRequest(
    string Field,
    string Operator,
    string Value);

public sealed record PlaybookDependencyRequest(
    string StepKey,
    string DependsOnStepKey,
    string DependencyType);
