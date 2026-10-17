using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.ProductionManager;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisZentra.Api.ProductionManager;

public static class ProductionManagerEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapProductionManagerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/production-manager").RequireAuthorization();

        group.MapGet("/workspace", GetWorkspaceAsync);
        group.MapGet("/decisions/{decisionId:guid}", GetDecisionAsync);
        group.MapPost("/decisions", CreateDecisionAsync);
        group.MapPost("/simulate", SimulateAsync);
        group.MapPost("/decisions/{decisionId:guid}/approve", ApproveDecisionAsync);
        group.MapPost("/decisions/{decisionId:guid}/execute", ExecuteDecisionAsync);
        group.MapPost("/decisions/{decisionId:guid}/verify", VerifyDecisionAsync);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisZentraProductionManager(this IServiceCollection services, bool scenarioEnginesEnabled)
    {
        services.AddSingleton(new ProductionManagerScenarioGate(scenarioEnginesEnabled));
        services.AddScoped<IProductionManagerRepository, ProductionManagerRepository>();
        services.AddScoped<IProductionManagerService, ProductionManagerService>();
        return services;
    }

    private static async Task<IResult> GetWorkspaceAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionManagerService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ProductionManagerEndpointHelpers.ResolveAccessAsync(
            facilityId, ProductionManagerPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var workspace = await service.GetWorkspaceAsync(access.FacilityId, cancellationToken);
            return TypedResults.Ok(ProductionManagerEndpointHelpers.MapWorkspace(workspace));
        }
        catch (ProductionManagerException ex)
        {
            return ProductionManagerEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> GetDecisionAsync(
        Guid decisionId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionManagerService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ProductionManagerEndpointHelpers.ResolveAccessAsync(
            facilityId, ProductionManagerPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var decision = await service.GetDecisionDetailAsync(decisionId, cancellationToken);
        if (decision is null || decision.FacilityId != access.FacilityId)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(ProductionManagerEndpointHelpers.MapDecision(decision));
    }

    private static async Task<IResult> CreateDecisionAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateDecisionRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IProductionManagerService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ProductionManagerEndpointHelpers.ResolveAccessAsync(
            facilityId, ProductionManagerPermissionKeys.Decide, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";

        try
        {
            var decision = await service.CreateDecisionAsync(
                access.FacilityId, request.DecisionType, request.CorrelationKey, subjectId, cancellationToken);
            return TypedResults.Ok(ProductionManagerEndpointHelpers.MapDecision(decision));
        }
        catch (ProductionManagerException ex)
        {
            return ProductionManagerEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> SimulateAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] SimulationRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IProductionManagerService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ProductionManagerEndpointHelpers.ResolveAccessAsync(
            facilityId, ProductionManagerPermissionKeys.Simulate, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";

        try
        {
            var result = await service.SimulateAsync(access.FacilityId, request, subjectId, cancellationToken);
            return TypedResults.Ok(new
            {
                scenarioType = result.ScenarioType,
                output = result.OutputJson,
                comparison = result.ComparisonJson,
                rows = result.ComparisonRows,
                dataClassification = ProductionManagerDataClassifications.Simulated,
            });
        }
        catch (ProductionManagerException ex)
        {
            return ProductionManagerEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> ApproveDecisionAsync(
        Guid decisionId,
        [FromQuery] Guid? facilityId,
        [FromBody] ApproveDecisionRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IProductionManagerService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ProductionManagerEndpointHelpers.ResolveAccessAsync(
            facilityId, ProductionManagerPermissionKeys.Approve, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";

        try
        {
            var decision = await service.ApproveDecisionAsync(decisionId, request, subjectId, cancellationToken);
            if (decision.FacilityId != access.FacilityId) return TypedResults.NotFound();
            return TypedResults.Ok(ProductionManagerEndpointHelpers.MapDecision(decision));
        }
        catch (ProductionManagerException ex)
        {
            return ProductionManagerEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> ExecuteDecisionAsync(
        Guid decisionId,
        [FromQuery] Guid? facilityId,
        [FromBody] ExecuteDecisionRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IProductionManagerService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ProductionManagerEndpointHelpers.ResolveAccessAsync(
            facilityId, ProductionManagerPermissionKeys.Decide, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";

        try
        {
            var decision = await service.ExecuteDecisionAsync(decisionId, request, subjectId, cancellationToken);
            if (decision.FacilityId != access.FacilityId) return TypedResults.NotFound();
            return TypedResults.Ok(ProductionManagerEndpointHelpers.MapDecision(decision));
        }
        catch (ProductionManagerException ex)
        {
            return ProductionManagerEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> VerifyDecisionAsync(
        Guid decisionId,
        [FromQuery] Guid? facilityId,
        [FromBody] VerifyDecisionOutcomeRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IProductionManagerService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ProductionManagerEndpointHelpers.ResolveAccessAsync(
            facilityId, ProductionManagerPermissionKeys.Decide, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";

        try
        {
            var decision = await service.VerifyDecisionAsync(decisionId, request, subjectId, cancellationToken);
            if (decision.FacilityId != access.FacilityId) return TypedResults.NotFound();
            return TypedResults.Ok(ProductionManagerEndpointHelpers.MapDecision(decision));
        }
        catch (ProductionManagerException ex)
        {
            return ProductionManagerEndpointHelpers.MapException(ex);
        }
    }
}

public sealed record CreateDecisionRequest(string DecisionType, string? CorrelationKey);
