using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.AiTasks;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisZentra.Api.AiTasks;

public static class AiTaskAutomationEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAiTaskAutomationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/ai-tasks/automation").RequireAuthorization();

        group.MapGet("/rules", ListRulesAsync);
        group.MapGet("/rules/{ruleId:guid}", GetRuleAsync);
        group.MapPost("/rules", CreateRuleAsync);
        group.MapPut("/rules/{ruleId:guid}", UpdateRuleAsync);
        group.MapPost("/rules/{ruleId:guid}/enable", EnableRuleAsync);
        group.MapPost("/rules/{ruleId:guid}/disable", DisableRuleAsync);
        group.MapGet("/rules/{ruleId:guid}/history", ListRuleHistoryAsync);
        group.MapPost("/simulate", SimulateAsync);
        group.MapGet("/executions", ListExecutionsAsync);

        return endpoints;
    }

    private static async Task<IResult> ListRulesAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskAutomationRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.AutomationRead, cancellationToken);
        if (context is null) return failResult!;

        var items = await repository.ListRulesAsync(context, cancellationToken);
        return TypedResults.Ok(new { items = items.Select(MapSummary) });
    }

    private static async Task<IResult> GetRuleAsync(
        Guid ruleId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskAutomationRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.AutomationRead, cancellationToken);
        if (context is null) return failResult!;

        var rule = await repository.GetRuleAsync(context, ruleId, cancellationToken);
        return rule is null ? Results.NotFound() : TypedResults.Ok(MapDetail(rule));
    }

    private static async Task<IResult> CreateRuleAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateAiTaskAutomationRuleRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskAutomationRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.AutomationManage, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.CreateRuleAsync(
            context,
            new CreateAiTaskAutomationRuleCommand(
                request.RuleKey,
                request.Name,
                request.Description,
                request.FacilityId ?? facilityId,
                request.TriggerType,
                request.SortOrder,
                request.Enabled,
                request.Conditions.Select(MapCondition).ToList(),
                request.Actions.Select(MapAction).ToList(),
                request.EffectiveFromUtc,
                request.EffectiveUntilUtc),
            cancellationToken);

        return MapWriteResult(result);
    }

    private static async Task<IResult> UpdateRuleAsync(
        Guid ruleId,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateAiTaskAutomationRuleRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskAutomationRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.AutomationManage, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.UpdateRuleAsync(
            context,
            new UpdateAiTaskAutomationRuleCommand(
                ruleId,
                request.ExpectedVersion,
                request.Name,
                request.Description,
                request.TriggerType,
                request.SortOrder,
                request.Enabled,
                request.Conditions.Select(MapCondition).ToList(),
                request.Actions.Select(MapAction).ToList(),
                request.EffectiveFromUtc,
                request.EffectiveUntilUtc,
                request.ChangeReason),
            cancellationToken);

        return MapWriteResult(result);
    }

    private static async Task<IResult> EnableRuleAsync(
        Guid ruleId,
        [FromQuery] Guid? facilityId,
        [FromBody] AutomationRuleToggleRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskAutomationRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.AutomationManage, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.SetRuleEnabledAsync(context, ruleId, request.ExpectedVersion, true, cancellationToken);
        return MapWriteResult(result);
    }

    private static async Task<IResult> DisableRuleAsync(
        Guid ruleId,
        [FromQuery] Guid? facilityId,
        [FromBody] AutomationRuleToggleRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskAutomationRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.AutomationManage, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.SetRuleEnabledAsync(context, ruleId, request.ExpectedVersion, false, cancellationToken);
        return MapWriteResult(result);
    }

    private static async Task<IResult> ListRuleHistoryAsync(
        Guid ruleId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskAutomationRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.AutomationRead, cancellationToken);
        if (context is null) return failResult!;

        var items = await repository.ListRuleHistoryAsync(context, ruleId, cancellationToken);
        return TypedResults.Ok(new { items });
    }

    private static async Task<IResult> SimulateAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] SimulateAiTaskAutomationRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskAutomationRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.AutomationSimulate, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.SimulateAsync(
            context,
            new SimulateAiTaskAutomationCommand(request.RuleId, request.TaskId, request.TriggerType),
            cancellationToken);

        return TypedResults.Ok(MapEvaluation(result));
    }

    private static async Task<IResult> ListExecutionsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? taskId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IAiTaskAutomationRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.AutomationRead, cancellationToken);
        if (context is null) return failResult!;

        var items = await repository.ListExecutionsAsync(context, taskId, page, pageSize, cancellationToken);
        return TypedResults.Ok(new { items, page, pageSize });
    }

    private static IResult MapWriteResult(AiTaskAutomationWriteResult result) =>
        result.Succeeded
            ? TypedResults.Ok(new { ruleId = result.RuleId, version = result.Version })
            : AiTaskEndpointHelpers.MapFailure(result.FailureCode ?? AiTaskFailureCodes.Unavailable, result.Message ?? "Operation failed.");

    private static object MapSummary(AiTaskAutomationRuleSummaryReadModel rule) => new
    {
        rule.Id,
        rule.RuleKey,
        rule.Name,
        rule.Description,
        rule.FacilityId,
        rule.TriggerType,
        rule.SortOrder,
        rule.Enabled,
        rule.Version,
        rule.UpdatedAtUtc,
        rule.LastMatchedAtUtc,
        actionSummaries = rule.ActionSummaries,
    };

    private static object MapDetail(AiTaskAutomationRuleDetailReadModel rule) => new
    {
        rule.Id,
        rule.RuleKey,
        rule.Name,
        rule.Description,
        rule.FacilityId,
        rule.TriggerType,
        rule.SortOrder,
        rule.Enabled,
        rule.Version,
        conditions = rule.Conditions,
        actions = rule.Actions,
        rule.EffectiveFromUtc,
        rule.EffectiveUntilUtc,
        rule.CreatedAtUtc,
        rule.UpdatedAtUtc,
        rule.LastMatchedAtUtc,
        rule.CreatedBy,
        rule.UpdatedBy,
    };

    private static object MapEvaluation(AiTaskAutomationEvaluationResult result) => new
    {
        result.TriggerType,
        matchedRules = result.MatchedRules,
        skippedRules = result.SkippedRules,
        proposedChanges = result.ProposedChanges,
        conflicts = result.Conflicts,
        actionsApplied = result.ActionsApplied,
        result.Simulated,
        result.OutcomeSummary,
    };

    private static AiTaskAutomationCondition MapCondition(AutomationConditionRequest request) =>
        new(request.Field, request.Operator, request.Value, request.Values);

    private static AiTaskAutomationAction MapAction(AutomationActionRequest request) =>
        new(request.Type, request.Value, request.GuidValue, request.IntValue, request.BoolValue, request.Values);
}

public sealed record CreateAiTaskAutomationRuleRequest(
    string RuleKey,
    string Name,
    string? Description,
    Guid? FacilityId,
    string TriggerType,
    int SortOrder,
    bool Enabled,
    IReadOnlyList<AutomationConditionRequest> Conditions,
    IReadOnlyList<AutomationActionRequest> Actions,
    DateTimeOffset? EffectiveFromUtc,
    DateTimeOffset? EffectiveUntilUtc);

public sealed record UpdateAiTaskAutomationRuleRequest(
    uint ExpectedVersion,
    string Name,
    string? Description,
    string TriggerType,
    int SortOrder,
    bool Enabled,
    IReadOnlyList<AutomationConditionRequest> Conditions,
    IReadOnlyList<AutomationActionRequest> Actions,
    DateTimeOffset? EffectiveFromUtc,
    DateTimeOffset? EffectiveUntilUtc,
    string ChangeReason);

public sealed record AutomationRuleToggleRequest(uint ExpectedVersion);

public sealed record SimulateAiTaskAutomationRequest(
    Guid? RuleId,
    Guid TaskId,
    string TriggerType);

public sealed record AutomationConditionRequest(
    string Field,
    string Operator,
    string? Value,
    IReadOnlyList<string>? Values);

public sealed record AutomationActionRequest(
    string Type,
    string? Value,
    Guid? GuidValue,
    int? IntValue,
    bool? BoolValue,
    IReadOnlyList<string>? Values);
