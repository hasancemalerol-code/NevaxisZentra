using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.DigitalWorkforce;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisZentra.Api.DigitalWorkforce;

public static class DigitalWorkforceEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapDigitalWorkforceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/digital-workforce").RequireAuthorization();

        group.MapGet("/workers", ListWorkersAsync);
        group.MapGet("/workers/{workerId:guid}", GetWorkerAsync);
        group.MapPut("/workers/{workerId:guid}/autonomy", UpdateAutonomyAsync);
        group.MapPut("/workers/{workerId:guid}/policy", UpdatePolicyAsync);
        group.MapPost("/workers/{workerId:guid}/pause", PauseWorkerAsync);
        group.MapPost("/workers/{workerId:guid}/resume", ResumeWorkerAsync);
        group.MapPost("/workers/{workerId:guid}/kill", KillWorkerAsync);
        group.MapPost("/control/global-pause", GlobalPauseAsync);
        group.MapPost("/control/global-resume", GlobalResumeAsync);
        group.MapPost("/control/global-kill", GlobalKillAsync);
        group.MapPost("/control/global-suggest-mode", GlobalSuggestModeAsync);
        group.MapPost("/control/apply-policies", ApplyPoliciesAsync);
        group.MapPost("/events/trigger", TriggerDomainEventAsync);
        group.MapGet("/workers/{workerId:guid}/detail", GetWorkerDetailAsync);
        group.MapPost("/cases/{caseId:guid}/resume-human-control", ResumeHumanControlAsync);
        group.MapGet("/control-center", GetControlCenterAsync);
        group.MapGet("/cases", ListCasesAsync);
        group.MapPost("/cases", TriggerCaseAsync);
        group.MapPost("/cases/{caseId:guid}/advance", AdvanceCaseAsync);
        group.MapPost("/cases/{caseId:guid}/takeover", TakeoverCaseAsync);
        group.MapPost("/cases/{caseId:guid}/handoff", HandoffAsync);
        group.MapPost("/cases/{caseId:guid}/verification", RecordVerificationAsync);
        group.MapGet("/approvals", ListApprovalsAsync);
        group.MapPost("/approvals/{approvalId:guid}/decision", DecideApprovalAsync);
        group.MapGet("/activity", ListActivityAsync);
        group.MapGet("/metrics", GetMetricsAsync);
        group.MapPost("/cost", RecordCostAsync);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisZentraDigitalWorkforce(this IServiceCollection services)
    {
        services.AddScoped<IDigitalWorkforceRepository, DigitalWorkforceRepository>();
        services.AddScoped<IDigitalWorkforceRuntimeService, DigitalWorkforceRuntimeService>();
        services.AddScoped<IDigitalWorkforceTaskCenterBridge, DigitalWorkforceTaskCenterBridge>();
        services.AddScoped<IDigitalWorkforceWorkerFlowEngine, DigitalWorkforceWorkerFlowEngine>();
        services.AddScoped<IDigitalWorkforceEventDispatcher, DigitalWorkforceEventDispatcher>();
        services.AddScoped<IDigitalWorkforceWorksService, DigitalWorkforceWorksService>();
        services.AddScoped<DigitalWorkforceModelRouterUsage>();
        services.AddNevaxisZentraDigitalWorkforceExecutionRuntime();
        return services;
    }

    private static async Task<IResult> GlobalSuggestModeAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.AutonomyManage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            await runtime.GlobalSuggestModeAsync(authorizationContext.Current!.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.NoContent();
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> ApplyPoliciesAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.PolicyManage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            await runtime.ApplyDefaultPoliciesAsync(authorizationContext.Current!.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.NoContent();
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> TriggerDomainEventAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] TriggerDomainEventRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IDigitalWorkforceEventDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var workCase = await dispatcher.DispatchAsync(
                new DigitalWorkforceDomainEvent(
                    request.EventType,
                    request.Title,
                    request.Description,
                    request.Severity,
                    request.SourceClassification,
                    request.FacilityId ?? facilityId,
                    request.MetadataJson),
                authorizationContext.Current!.IdentitySubjectId.ToString(),
                cancellationToken);
            return TypedResults.Created(
                $"/api/digital-workforce/cases/{workCase.Id}",
                DigitalWorkforceEndpointHelpers.ToCaseResponse(workCase));
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> GetWorkerDetailAsync(
        Guid workerId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var detail = await runtime.GetWorkerDetailAsync(workerId, cancellationToken);
            return TypedResults.Ok(detail);
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> ResumeHumanControlAsync(
        Guid caseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            await runtime.ResumeHumanControlAsync(
                caseId, authorizationContext.Current!.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.NoContent();
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> GetControlCenterAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var snapshot = await runtime.GetControlCenterAsync(cancellationToken);
            return TypedResults.Ok(snapshot);
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> ListWorkersAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IDigitalWorkforceRepository repository,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var workers = await repository.ListWorkersAsync(cancellationToken);
        return TypedResults.Ok(workers.Select(w => new
        {
            w.Id, w.Code, w.Name, w.Description, w.Mission, w.Status, w.AutonomyMode,
            w.Enabled, w.IsPaused, w.IsKilled, w.IsHumanControlled, w.SourceClassification,
            concurrencyToken = w.ConcurrencyToken,
        }));
    }

    private static async Task<IResult> GetWorkerAsync(
        Guid workerId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var worker = await runtime.GetWorkerAsync(workerId, cancellationToken);
        return worker is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(worker);
    }

    private static async Task<IResult> UpdateAutonomyAsync(
        Guid workerId,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateAutonomyRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.AutonomyManage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            await runtime.UpdateAutonomyAsync(
                workerId, request, authorizationContext.Current!.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.NoContent();
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> UpdatePolicyAsync(
        Guid workerId,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdatePolicyRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.PolicyManage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            await runtime.UpdatePolicyAsync(
                workerId, request, authorizationContext.Current!.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.NoContent();
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> PauseWorkerAsync(
        Guid workerId,
        [FromQuery] Guid? facilityId,
        [FromQuery] long concurrencyToken,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            await runtime.PauseWorkerAsync(
                workerId, concurrencyToken, authorizationContext.Current!.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.NoContent();
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> ResumeWorkerAsync(
        Guid workerId,
        [FromQuery] Guid? facilityId,
        [FromQuery] long concurrencyToken,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            await runtime.ResumeWorkerAsync(
                workerId, concurrencyToken, authorizationContext.Current!.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.NoContent();
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> KillWorkerAsync(
        Guid workerId,
        [FromQuery] Guid? facilityId,
        [FromQuery] long concurrencyToken,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Kill, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            await runtime.KillWorkerAsync(
                workerId, concurrencyToken, authorizationContext.Current!.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.NoContent();
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> GlobalPauseAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            await runtime.GlobalPauseAsync(authorizationContext.Current!.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.NoContent();
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> GlobalResumeAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            await runtime.GlobalResumeAsync(authorizationContext.Current!.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.NoContent();
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> GlobalKillAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Kill, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            await runtime.GlobalKillAsync(authorizationContext.Current!.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.NoContent();
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> ListCasesAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? status,
        [FromQuery] Guid? workerId,
        IFacilityAccessResolver facilityAccessResolver,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var cases = await runtime.ListCasesAsync(status, workerId, cancellationToken);
        return TypedResults.Ok(cases);
    }

    private static async Task<IResult> TriggerCaseAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] TriggerWorkCaseRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var workCase = await runtime.TriggerCaseAsync(
                request with { FacilityId = request.FacilityId ?? facilityId },
                authorizationContext.Current!.IdentitySubjectId.ToString(),
                cancellationToken);
            return TypedResults.Created(
                $"/api/digital-workforce/cases/{workCase.Id}",
                DigitalWorkforceEndpointHelpers.ToCaseResponse(workCase));
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> AdvanceCaseAsync(
        Guid caseId,
        [FromQuery] Guid? facilityId,
        [FromBody] AdvanceWorkCaseRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var workCase = await runtime.AdvanceCaseAsync(
                caseId, request, authorizationContext.Current!.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.Ok(workCase);
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> TakeoverCaseAsync(
        Guid caseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var workCase = await runtime.TakeoverCaseAsync(
                caseId, authorizationContext.Current!.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.Ok(workCase);
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> HandoffAsync(
        Guid caseId,
        [FromQuery] Guid? facilityId,
        [FromBody] HandoffRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var handoff = await runtime.HandoffAsync(
                caseId, request, authorizationContext.Current!.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.Ok(DigitalWorkforceEndpointHelpers.ToHandoffResponse(handoff));
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> RecordVerificationAsync(
        Guid caseId,
        [FromQuery] Guid? facilityId,
        [FromBody] RecordVerificationRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            await runtime.RecordVerificationAsync(
                caseId, request, authorizationContext.Current!.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.NoContent();
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> ListApprovalsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? status,
        IFacilityAccessResolver facilityAccessResolver,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var approvals = await runtime.ListApprovalsAsync(status, cancellationToken);
        return TypedResults.Ok(approvals);
    }

    private static async Task<IResult> DecideApprovalAsync(
        Guid approvalId,
        [FromQuery] Guid? facilityId,
        [FromBody] ApprovalDecisionRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.ApprovalDecide, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var approval = await runtime.DecideApprovalAsync(
                approvalId, request, authorizationContext.Current!.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.Ok(approval);
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> ListActivityAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? workerId,
        [FromQuery] Guid? caseId,
        IFacilityAccessResolver facilityAccessResolver,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var activity = await runtime.ListActivityAsync(workerId, caseId, cancellationToken);
        return TypedResults.Ok(activity);
    }

    private static async Task<IResult> GetMetricsAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var metrics = await runtime.GetMetricsAsync(cancellationToken);
        return TypedResults.Ok(metrics);
    }

    private static async Task<IResult> RecordCostAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] RecordAiUsageRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IDigitalWorkforceRuntimeService runtime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.CostRead, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            await runtime.RecordAiUsageAsync(request, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }
}
