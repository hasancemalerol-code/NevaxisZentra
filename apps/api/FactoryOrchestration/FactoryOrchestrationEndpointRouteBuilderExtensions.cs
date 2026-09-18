using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.FactoryOrchestration;
using NevaxisZentra.Persistence.FactoryOutcomeExecution;
using NevaxisZentra.Persistence.FactoryRecoveryIntelligence;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisZentra.Api.FactoryOrchestration;

public static class FactoryOrchestrationEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapFactoryOrchestrationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/factory-orchestration").RequireAuthorization();

        group.MapGet("/cases", ListActiveCasesAsync);
        group.MapGet("/cases/{caseId:guid}", GetCaseDetailAsync);
        group.MapPost("/events/route", RouteEventAsync);
        group.MapPost("/cases/{caseId:guid}/verify", VerifyOutcomeAsync);
        group.MapPost("/cases/{caseId:guid}/reopen", ReopenCaseAsync);
        group.MapPost("/cases/{caseId:guid}/escalate", EscalateCaseAsync);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisZentraFactoryOrchestration(this IServiceCollection services)
    {
        services.AddScoped<IFactoryOrchestrationRepository, FactoryOrchestrationRepository>();
        services.AddScoped<IFactoryEventRouter, FactoryEventRouter>();
        services.AddScoped<IFactoryOrchestrationCoordinator, FactoryOrchestrationCoordinator>();
        services.AddScoped<IFactoryOrchestrationService, FactoryOrchestrationService>();
        services.AddScoped<IFactoryOutcomeExecutionRepository, FactoryOutcomeExecutionRepository>();
        services.AddScoped<IFactoryOutcomeExecutionService, FactoryOutcomeExecutionService>();
        services.AddScoped<IFactoryOutcomeAiTaskSyncService, FactoryOutcomeAiTaskSyncService>();
        services.AddScoped<IFactoryOutcomeLearningService, FactoryOutcomeLearningService>();
        services.AddScoped<IFactoryRecoveryLearningRepository, FactoryRecoveryLearningRepository>();
        services.AddScoped<IFactoryRecoveryIntelligenceService, FactoryRecoveryIntelligenceService>();
        return services;
    }

    private static async Task<IResult> ListActiveCasesAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryOrchestrationRepository repository,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryOrchestrationEndpointHelpers.ResolveAccessAsync(
            facilityId, FactoryOrchestrationPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var cases = await repository.ListActiveRootCasesAsync(access.FacilityId, limit ?? 50, cancellationToken);
        return TypedResults.Ok(new { items = cases.Select(FactoryOrchestrationEndpointHelpers.MapCaseSummary).ToList() });
    }

    private static async Task<IResult> GetCaseDetailAsync(
        Guid caseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryOrchestrationService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryOrchestrationEndpointHelpers.ResolveAccessAsync(
            facilityId, FactoryOrchestrationPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var detail = await service.GetCaseDetailAsync(caseId, cancellationToken);
            if (detail.RootCase.FacilityId != access.FacilityId)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(FactoryOrchestrationEndpointHelpers.MapCaseDetail(detail));
        }
        catch (FactoryOrchestrationException ex)
        {
            return FactoryOrchestrationEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> RouteEventAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] RouteFactoryEventRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IFactoryOrchestrationService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryOrchestrationEndpointHelpers.ResolveAccessAsync(
            facilityId, FactoryOrchestrationPermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";

        try
        {
            var eventData = new FactoryDomainEvent(
                request.EventType,
                request.Title,
                request.Description,
                request.Severity,
                request.SourceClassification,
                access.FacilityId,
                request.SourceType,
                request.SourceId,
                request.MachineId,
                request.OrderId,
                request.MoldId,
                request.MaterialId,
                request.MetadataJson);

            var result = await service.RouteEventAsync(eventData, subjectId, cancellationToken);
            return TypedResults.Ok(new
            {
                createdNew = result.CreatedNew,
                enrichedExisting = result.EnrichedExisting,
                correlationKey = result.CorrelationKey,
                participatingDomains = result.ParticipatingDomains,
                caseRow = FactoryOrchestrationEndpointHelpers.MapCaseSummary(result.Case),
            });
        }
        catch (FactoryOrchestrationException ex)
        {
            return FactoryOrchestrationEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> VerifyOutcomeAsync(
        Guid caseId,
        [FromQuery] Guid? facilityId,
        [FromBody] VerifyFactoryOutcomeRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IFactoryOrchestrationService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryOrchestrationEndpointHelpers.ResolveAccessAsync(
            facilityId, FactoryOrchestrationPermissionKeys.Verify, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";

        try
        {
            var caseRow = await service.VerifyOutcomeAsync(caseId, request, subjectId, cancellationToken);
            if (caseRow.FacilityId != access.FacilityId)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(FactoryOrchestrationEndpointHelpers.MapCaseSummary(caseRow));
        }
        catch (FactoryOrchestrationException ex)
        {
            return FactoryOrchestrationEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> ReopenCaseAsync(
        Guid caseId,
        [FromQuery] Guid? facilityId,
        [FromBody] ReopenFactoryCaseRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IFactoryOrchestrationService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryOrchestrationEndpointHelpers.ResolveAccessAsync(
            facilityId, FactoryOrchestrationPermissionKeys.Reopen, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";

        try
        {
            var caseRow = await service.ReopenCaseAsync(caseId, request, subjectId, cancellationToken);
            if (caseRow.FacilityId != access.FacilityId)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(FactoryOrchestrationEndpointHelpers.MapCaseSummary(caseRow));
        }
        catch (FactoryOrchestrationException ex)
        {
            return FactoryOrchestrationEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> EscalateCaseAsync(
        Guid caseId,
        [FromQuery] Guid? facilityId,
        [FromBody] EscalateFactoryCaseRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IFactoryOrchestrationService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryOrchestrationEndpointHelpers.ResolveAccessAsync(
            facilityId, FactoryOrchestrationPermissionKeys.Escalate, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";

        try
        {
            var caseRow = await service.EscalateCaseAsync(caseId, request.Reason, subjectId, cancellationToken);
            if (caseRow.FacilityId != access.FacilityId)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(FactoryOrchestrationEndpointHelpers.MapCaseSummary(caseRow));
        }
        catch (FactoryOrchestrationException ex)
        {
            return FactoryOrchestrationEndpointHelpers.MapException(ex);
        }
    }
}

public sealed record RouteFactoryEventRequest(
    string EventType,
    string Title,
    string Description,
    string Severity,
    string SourceClassification,
    string SourceType,
    string SourceId,
    string? MachineId,
    string? OrderId,
    string? MoldId,
    string? MaterialId,
    string? MetadataJson);

public sealed record EscalateFactoryCaseRequest(string Reason);
