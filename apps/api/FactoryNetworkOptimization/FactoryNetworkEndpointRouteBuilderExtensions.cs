using Microsoft.AspNetCore.Mvc;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.FactoryNetworkOptimization;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Api.FactoryNetworkOptimization;

public static class FactoryNetworkEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapFactoryNetworkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/factory-network").RequireAuthorization();

        group.MapGet("/workspace", GetWorkspaceAsync);
        group.MapPost("/simulate", SimulateAsync);
        group.MapPost("/evaluate-allocation", EvaluateAllocationAsync);
        group.MapPost("/execute", ExecuteAllocationAsync);
        group.MapPost("/approve", ApproveAllocationAsync);
        group.MapPost("/verify-outcome", VerifyOutcomeAsync);
        group.MapPut("/policy", UpdatePolicyAsync);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisZentraFactoryNetwork(this IServiceCollection services)
    {
        services.AddScoped<IFactoryNetworkRepository, FactoryNetworkRepository>();
        services.AddScoped<IFactoryNetworkService, FactoryNetworkService>();
        return services;
    }

    private static async Task<IResult> GetWorkspaceAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? networkId,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryNetworkRepository repository,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IFactoryNetworkService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryNetworkEndpointHelpers.ResolveNetworkAccessAsync(
            facilityId,
            networkId,
            FactoryNetworkPermissionKeys.Read,
            facilityAccessResolver,
            repository,
            tenantDatabaseSession,
            cancellationToken);
        if (access is null) return failure!;

        try
        {
            var workspace = await service.GetWorkspaceAsync(
                access.NetworkId,
                access.PermittedFacilityIds,
                cancellationToken);
            return TypedResults.Ok(FactoryNetworkEndpointHelpers.MapWorkspace(workspace));
        }
        catch (FactoryNetworkException ex)
        {
            return FactoryNetworkEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> SimulateAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] SimulateNetworkRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryNetworkRepository repository,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IFactoryNetworkService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryNetworkEndpointHelpers.ResolveNetworkAccessAsync(
            facilityId,
            request.NetworkId,
            FactoryNetworkPermissionKeys.Simulate,
            facilityAccessResolver,
            repository,
            tenantDatabaseSession,
            cancellationToken);
        if (access is null) return failure!;

        var scopeFailure = FactoryNetworkEndpointHelpers.ValidateFacilityScope(
            access.PermittedFacilityIds,
            request.SourceFacilityId,
            request.DestinationFacilityId);
        if (scopeFailure is not null) return scopeFailure;

        try
        {
            var subject = httpContext.User.Identity?.Name ?? "system";
            var results = await service.SimulateAsync(request, subject, cancellationToken);
            return TypedResults.Ok(results.Select(FactoryNetworkEndpointHelpers.MapSimulationResult));
        }
        catch (FactoryNetworkException ex)
        {
            return FactoryNetworkEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> EvaluateAllocationAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] EvaluateNetworkAllocationRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryNetworkRepository repository,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IFactoryNetworkService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryNetworkEndpointHelpers.ResolveNetworkAccessAsync(
            facilityId,
            request.NetworkId,
            FactoryNetworkPermissionKeys.Read,
            facilityAccessResolver,
            repository,
            tenantDatabaseSession,
            cancellationToken);
        if (access is null) return failure!;

        try
        {
            var evaluation = await service.EvaluateAllocationAsync(request, cancellationToken);
            return TypedResults.Ok(FactoryNetworkEndpointHelpers.MapAllocationEvaluation(evaluation));
        }
        catch (FactoryNetworkException ex)
        {
            return FactoryNetworkEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> ExecuteAllocationAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] ExecuteNetworkAllocationRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryNetworkRepository repository,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IFactoryNetworkService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryNetworkEndpointHelpers.ResolveNetworkAccessAsync(
            facilityId,
            null,
            FactoryNetworkPermissionKeys.Execute,
            facilityAccessResolver,
            repository,
            tenantDatabaseSession,
            cancellationToken);
        if (access is null) return failure!;

        var allocation = await tenantDatabaseSession.ExecuteReadAsync(
            ct => repository.GetAllocationAsync(request.AllocationId, ct),
            cancellationToken);
        if (allocation is null)
        {
            return TypedResults.NotFound(new
            {
                code = FactoryNetworkFailureCodes.NotFound,
                message = "Tahsis bulunamadı.",
            });
        }

        var scopeFailure = FactoryNetworkEndpointHelpers.ValidateFacilityScope(
            access.PermittedFacilityIds,
            allocation.SourceFacilityId,
            allocation.DestinationFacilityId);
        if (scopeFailure is not null) return scopeFailure;

        try
        {
            var subject = httpContext.User.Identity?.Name ?? "system";
            var result = await service.ExecuteAllocationAsync(request, subject, cancellationToken);
            if (result.ApprovalRequired)
            {
                return TypedResults.UnprocessableEntity(new
                {
                    code = FactoryNetworkFailureCodes.ApprovalRequired,
                    message = "Tahsis onay gerektiriyor.",
                    allocation = FactoryNetworkEndpointHelpers.MapAllocation(result.Allocation),
                });
            }

            return TypedResults.Ok(FactoryNetworkEndpointHelpers.MapAllocationResult(result));
        }
        catch (FactoryNetworkException ex)
        {
            return FactoryNetworkEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> ApproveAllocationAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] ApproveNetworkAllocationRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryNetworkRepository repository,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IFactoryNetworkService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryNetworkEndpointHelpers.ResolveNetworkAccessAsync(
            facilityId,
            null,
            FactoryNetworkPermissionKeys.Approve,
            facilityAccessResolver,
            repository,
            tenantDatabaseSession,
            cancellationToken);
        if (access is null) return failure!;

        var allocation = await tenantDatabaseSession.ExecuteReadAsync(
            ct => repository.GetAllocationAsync(request.AllocationId, ct),
            cancellationToken);
        if (allocation is null)
        {
            return TypedResults.NotFound(new
            {
                code = FactoryNetworkFailureCodes.NotFound,
                message = "Tahsis bulunamadı.",
            });
        }

        var scopeFailure = FactoryNetworkEndpointHelpers.ValidateFacilityScope(
            access.PermittedFacilityIds,
            allocation.SourceFacilityId,
            allocation.DestinationFacilityId);
        if (scopeFailure is not null) return scopeFailure;

        try
        {
            var subject = httpContext.User.Identity?.Name ?? "system";
            var result = await service.ApproveAllocationAsync(request, subject, cancellationToken);
            return TypedResults.Ok(FactoryNetworkEndpointHelpers.MapAllocationResult(result));
        }
        catch (FactoryNetworkException ex)
        {
            return FactoryNetworkEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> VerifyOutcomeAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid allocationId,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryNetworkRepository repository,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IFactoryNetworkService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryNetworkEndpointHelpers.ResolveNetworkAccessAsync(
            facilityId,
            null,
            FactoryNetworkPermissionKeys.Manage,
            facilityAccessResolver,
            repository,
            tenantDatabaseSession,
            cancellationToken);
        if (access is null) return failure!;

        var allocation = await tenantDatabaseSession.ExecuteReadAsync(
            ct => repository.GetAllocationAsync(allocationId, ct),
            cancellationToken);
        if (allocation is null)
        {
            return TypedResults.NotFound(new
            {
                code = FactoryNetworkFailureCodes.NotFound,
                message = "Tahsis bulunamadı.",
            });
        }

        var scopeFailure = FactoryNetworkEndpointHelpers.ValidateFacilityScope(
            access.PermittedFacilityIds,
            allocation.SourceFacilityId,
            allocation.DestinationFacilityId);
        if (scopeFailure is not null) return scopeFailure;

        try
        {
            var result = await service.VerifyOutcomeAsync(allocationId, cancellationToken);
            return TypedResults.Ok(new
            {
                allocationId = result.AllocationId,
                outcomeStatus = result.OutcomeStatus,
                expectedImpactJson = result.ExpectedImpactJson,
                actualImpactJson = result.ActualImpactJson,
                quantityConservationPercent = result.QuantityConservationPercent,
                learningMemoryId = result.LearningMemoryId,
            });
        }
        catch (FactoryNetworkException ex)
        {
            return FactoryNetworkEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> UpdatePolicyAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? networkId,
        [FromBody] UpdateFactoryNetworkPolicyRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryNetworkRepository repository,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IFactoryNetworkService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryNetworkEndpointHelpers.ResolveNetworkAccessAsync(
            facilityId,
            networkId,
            FactoryNetworkPermissionKeys.PolicyManage,
            facilityAccessResolver,
            repository,
            tenantDatabaseSession,
            cancellationToken);
        if (access is null) return failure!;

        try
        {
            var policy = await service.UpdatePolicyAsync(access.NetworkId, request, cancellationToken);
            return TypedResults.Ok(FactoryNetworkEndpointHelpers.MapPolicy(policy));
        }
        catch (FactoryNetworkException ex)
        {
            return FactoryNetworkEndpointHelpers.MapException(ex);
        }
    }
}
