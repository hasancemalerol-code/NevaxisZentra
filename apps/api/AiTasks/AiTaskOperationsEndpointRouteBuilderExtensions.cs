using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.AiTasks;
using NevaxisZentra.Persistence.Audit;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisZentra.Api.AiTasks;

public static class AiTaskOperationsEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAiTaskOperationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/ai-tasks").RequireAuthorization();

        group.MapPost("/{taskId:guid}/assign", AssignTaskAsync);
        group.MapPost("/{taskId:guid}/acknowledge", AcknowledgeTaskAsync);
        group.MapPost("/{taskId:guid}/priority", ChangePriorityAsync);
        group.MapPost("/{taskId:guid}/due-date", ChangeDueDateAsync);
        group.MapPost("/{taskId:guid}/escalate", EscalateTaskAsync);
        group.MapPost("/escalations/evaluate", EvaluateEscalationsAsync);

        return endpoints;
    }

    private static async Task<IResult> AssignTaskAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        [FromBody] AssignAiTaskRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskOperationsRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.Assign, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.AssignTaskAsync(
            context,
            new AssignAiTaskCommand(
                taskId,
                body.ExpectedVersion,
                body.TargetMembershipId,
                body.TargetTeamChannelId,
                body.Reason,
                body.Source ?? AiTaskAssignmentSources.Manual),
            cancellationToken);

        return AiTaskEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> AcknowledgeTaskAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        [FromBody] AcknowledgeAiTaskRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskOperationsRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.Acknowledge, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.AcknowledgeTaskAsync(
            context,
            new AcknowledgeAiTaskCommand(taskId, body.ExpectedVersion, body.Note),
            cancellationToken);

        return AiTaskEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> ChangePriorityAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        [FromBody] ChangeAiTaskPriorityRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskOperationsRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.Manage, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ChangePriorityAsync(
            context,
            new ChangeAiTaskPriorityCommand(taskId, body.ExpectedVersion, body.Priority, body.Reason),
            cancellationToken);

        return AiTaskEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> ChangeDueDateAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        [FromBody] ChangeAiTaskDueDateRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskOperationsRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.Manage, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ChangeDueDateAsync(
            context,
            new ChangeAiTaskDueDateCommand(taskId, body.ExpectedVersion, body.DueAtUtc, body.Reason),
            cancellationToken);

        return AiTaskEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> EscalateTaskAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        [FromBody] EscalateAiTaskRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskOperationsRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.Escalate, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.EscalateTaskAsync(
            context,
            new EscalateAiTaskCommand(taskId, body.ExpectedVersion, body.BreachType, body.Note),
            cancellationToken);

        return AiTaskEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> EvaluateEscalationsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] int maxTasks = 50,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IAiTaskOperationsRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.Escalate, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.EvaluateEscalationsAsync(context, maxTasks, cancellationToken);
        return TypedResults.Ok(new
        {
            tasksEvaluated = result.TasksEvaluated,
            escalationsCreated = result.EscalationsCreated,
            escalatedTaskIds = result.EscalatedTaskIds,
        });
    }
}

public sealed record AssignAiTaskRequest(
    uint ExpectedVersion,
    Guid? TargetMembershipId,
    Guid? TargetTeamChannelId,
    string Reason,
    string? Source);

public sealed record AcknowledgeAiTaskRequest(uint ExpectedVersion, string? Note);

public sealed record ChangeAiTaskPriorityRequest(uint ExpectedVersion, string Priority, string? Reason);

public sealed record ChangeAiTaskDueDateRequest(uint ExpectedVersion, DateTimeOffset? DueAtUtc, string? Reason);

public sealed record EscalateAiTaskRequest(uint ExpectedVersion, string BreachType, string? Note);
