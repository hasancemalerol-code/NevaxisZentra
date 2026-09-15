using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using NevaxisFactOS.Api.FactoryOrchestration;
using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.FactoryOrchestration;
using NevaxisFactOS.Persistence.FactoryOutcomeExecution;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Api.FactoryOutcomeExecution;

public static class FactoryOutcomeExecutionEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapFactoryOutcomeExecutionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/factory-outcome-execution").RequireAuthorization();
        group.MapPost("/dev-simulator/order-recovery-2/emit", EmitOrderRecovery2ScenarioAsync);
        return endpoints;
    }

    private static async Task<IResult> EmitOrderRecovery2ScenarioAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IFactoryOrchestrationService orchestrationService,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return TypedResults.Forbid();
        }

        var (access, failure) = await FactoryOrchestrationEndpointHelpers.ResolveAccessAsync(
            facilityId,
            FactoryOrchestrationPermissionKeys.Manage,
            facilityAccessResolver,
            cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "development-simulator";
        var machineEvent = DevelopmentFactoryEventSimulator.CreateInj004CycleDegradationEvent(access.FacilityId);
        var machineResult = await orchestrationService.RouteEventAsync(machineEvent, subjectId, cancellationToken);
        var materialEvent = DevelopmentFactoryEventSimulator.CreateMaterialShortageEvent(access.FacilityId);
        var materialResult = await orchestrationService.RouteEventAsync(materialEvent, subjectId, cancellationToken);

        return TypedResults.Ok(new
        {
            rootCaseId = machineResult.Case.Id,
            correlationKey = machineResult.CorrelationKey,
            createdNew = machineResult.CreatedNew,
            enrichedExisting = materialResult.EnrichedExisting,
            machineEvent = machineEvent.SourceId,
            materialEvent = materialEvent.SourceId,
        });
    }
}
