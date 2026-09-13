using Microsoft.AspNetCore.Mvc;
using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.FactoryLearning;

namespace NevaxisFactOS.Api.FactoryLearning;

public static class FactoryLearningEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapFactoryLearningEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/factory-learning").RequireAuthorization();

        group.MapGet("/workspace", GetWorkspaceAsync);
        group.MapPost("/refresh", RefreshAsync);
        group.MapGet("/opportunities/{opportunityId:guid}", GetOpportunityAsync);
        group.MapGet("/experiments/{experimentId:guid}", GetExperimentAsync);
        group.MapGet("/verified/{verifiedImprovementId:guid}", GetVerifiedAsync);
        group.MapPost("/root-cause", CreateRootCauseAsync);
        group.MapPost("/experiments", CreateExperimentAsync);
        group.MapPost("/experiments/{experimentId:guid}/approve", ApproveExperimentAsync);
        group.MapPost("/experiments/{experimentId:guid}/start", StartExperimentAsync);
        group.MapPost("/experiments/{experimentId:guid}/stop", StopExperimentAsync);
        group.MapPost("/experiments/{experimentId:guid}/complete", CompleteExperimentAsync);
        group.MapPost("/experiments/{experimentId:guid}/verify", VerifyExperimentAsync);
        group.MapPost("/verified/{verifiedImprovementId:guid}/standardize", StandardizeAsync);
        group.MapPost("/simulate", SimulateAsync);
        group.MapPost("/sustainability/check", CheckSustainabilityAsync);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisFactOSFactoryLearning(this IServiceCollection services)
    {
        services.AddScoped<IFactoryLearningRepository, FactoryLearningRepository>();
        services.AddScoped<IFactoryLearningService, FactoryLearningService>();
        return services;
    }

    private static async Task<IResult> GetWorkspaceAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryLearningService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryLearningEndpointHelpers.ResolveAccessAsync(
            facilityId, ContinuousImprovementPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var workspace = await service.GetWorkspaceAsync(access.FacilityId, cancellationToken);
            return TypedResults.Ok(FactoryLearningEndpointHelpers.MapWorkspace(workspace));
        }
        catch (FactoryLearningException ex)
        {
            return FactoryLearningEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> RefreshAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryLearningService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryLearningEndpointHelpers.ResolveAccessAsync(
            facilityId, ContinuousImprovementPermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var workspace = await service.RefreshLossesAsync(access.FacilityId, cancellationToken);
            return TypedResults.Ok(FactoryLearningEndpointHelpers.MapWorkspace(workspace));
        }
        catch (FactoryLearningException ex)
        {
            return FactoryLearningEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> GetOpportunityAsync(
        Guid opportunityId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryLearningService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryLearningEndpointHelpers.ResolveAccessAsync(
            facilityId, ContinuousImprovementPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var opportunity = await service.GetOpportunityAsync(opportunityId, cancellationToken);
        if (opportunity is null || opportunity.FacilityId != access.FacilityId) return TypedResults.NotFound();
        return TypedResults.Ok(FactoryLearningEndpointHelpers.MapOpportunity(opportunity));
    }

    private static async Task<IResult> GetExperimentAsync(
        Guid experimentId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryLearningService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryLearningEndpointHelpers.ResolveAccessAsync(
            facilityId, ContinuousImprovementPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var experiment = await service.GetExperimentAsync(experimentId, cancellationToken);
        if (experiment is null || experiment.FacilityId != access.FacilityId) return TypedResults.NotFound();
        return TypedResults.Ok(FactoryLearningEndpointHelpers.MapExperiment(experiment));
    }

    private static async Task<IResult> GetVerifiedAsync(
        Guid verifiedImprovementId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryLearningService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryLearningEndpointHelpers.ResolveAccessAsync(
            facilityId, ContinuousImprovementPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var verified = await service.GetVerifiedImprovementAsync(verifiedImprovementId, cancellationToken);
        if (verified is null || verified.FacilityId != access.FacilityId) return TypedResults.NotFound();
        return TypedResults.Ok(FactoryLearningEndpointHelpers.MapVerifiedImprovement(verified));
    }

    private static async Task<IResult> CreateRootCauseAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] RootCauseDraftRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryLearningService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryLearningEndpointHelpers.ResolveAccessAsync(
            facilityId, ContinuousImprovementPermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var analysis = await service.CreateRootCauseAsync(access.FacilityId, request, cancellationToken);
            return TypedResults.Ok(FactoryLearningEndpointHelpers.MapRootCause(analysis));
        }
        catch (FactoryLearningException ex)
        {
            return FactoryLearningEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> CreateExperimentAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateExperimentRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IFactoryLearningService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryLearningEndpointHelpers.ResolveAccessAsync(
            facilityId, ContinuousImprovementPermissionKeys.ExperimentCreate, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";
        try
        {
            var experiment = await service.CreateExperimentAsync(access.FacilityId, request, subjectId, cancellationToken);
            return TypedResults.Ok(FactoryLearningEndpointHelpers.MapExperiment(experiment));
        }
        catch (FactoryLearningException ex)
        {
            return FactoryLearningEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> ApproveExperimentAsync(
        Guid experimentId,
        [FromQuery] Guid? facilityId,
        [FromBody] ApproveExperimentRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IFactoryLearningService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryLearningEndpointHelpers.ResolveAccessAsync(
            facilityId, ContinuousImprovementPermissionKeys.ExperimentApprove, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";
        try
        {
            var experiment = await service.ApproveExperimentAsync(experimentId, request, subjectId, cancellationToken);
            if (experiment.FacilityId != access.FacilityId) return TypedResults.NotFound();
            return TypedResults.Ok(FactoryLearningEndpointHelpers.MapExperiment(experiment));
        }
        catch (FactoryLearningException ex)
        {
            return FactoryLearningEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> StartExperimentAsync(
        Guid experimentId,
        [FromQuery] Guid? facilityId,
        [FromBody] StartExperimentRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IFactoryLearningService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryLearningEndpointHelpers.ResolveAccessAsync(
            facilityId, ContinuousImprovementPermissionKeys.ExperimentCreate, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";
        try
        {
            var experiment = await service.StartExperimentAsync(experimentId, request, subjectId, cancellationToken);
            if (experiment.FacilityId != access.FacilityId) return TypedResults.NotFound();
            return TypedResults.Ok(FactoryLearningEndpointHelpers.MapExperiment(experiment));
        }
        catch (FactoryLearningException ex)
        {
            return FactoryLearningEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> StopExperimentAsync(
        Guid experimentId,
        [FromQuery] Guid? facilityId,
        [FromBody] StopExperimentRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IFactoryLearningService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryLearningEndpointHelpers.ResolveAccessAsync(
            facilityId, ContinuousImprovementPermissionKeys.ExperimentStop, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";
        try
        {
            var experiment = await service.StopExperimentAsync(experimentId, request, subjectId, cancellationToken);
            if (experiment.FacilityId != access.FacilityId) return TypedResults.NotFound();
            return TypedResults.Ok(FactoryLearningEndpointHelpers.MapExperiment(experiment));
        }
        catch (FactoryLearningException ex)
        {
            return FactoryLearningEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> CompleteExperimentAsync(
        Guid experimentId,
        [FromQuery] Guid? facilityId,
        [FromBody] CompleteExperimentRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IFactoryLearningService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryLearningEndpointHelpers.ResolveAccessAsync(
            facilityId, ContinuousImprovementPermissionKeys.ExperimentCreate, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";
        try
        {
            var experiment = await service.CompleteExperimentAsync(experimentId, request, subjectId, cancellationToken);
            if (experiment.FacilityId != access.FacilityId) return TypedResults.NotFound();
            return TypedResults.Ok(FactoryLearningEndpointHelpers.MapExperiment(experiment));
        }
        catch (FactoryLearningException ex)
        {
            return FactoryLearningEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> VerifyExperimentAsync(
        Guid experimentId,
        [FromQuery] Guid? facilityId,
        [FromBody] VerifyExperimentRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IFactoryLearningService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryLearningEndpointHelpers.ResolveAccessAsync(
            facilityId, ContinuousImprovementPermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";
        try
        {
            var result = await service.VerifyExperimentAsync(experimentId, request, subjectId, cancellationToken);
            if (result.Experiment.FacilityId != access.FacilityId) return TypedResults.NotFound();
            return TypedResults.Ok(new
            {
                experiment = FactoryLearningEndpointHelpers.MapExperiment(result.Experiment),
                verifiedImprovement = result.VerifiedImprovement is null
                    ? null
                    : FactoryLearningEndpointHelpers.MapVerifiedImprovement(result.VerifiedImprovement),
            });
        }
        catch (FactoryLearningException ex)
        {
            return FactoryLearningEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> StandardizeAsync(
        Guid verifiedImprovementId,
        [FromQuery] Guid? facilityId,
        [FromBody] StandardizeImprovementRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IFactoryLearningService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryLearningEndpointHelpers.ResolveAccessAsync(
            facilityId, ContinuousImprovementPermissionKeys.ImprovementStandardize, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";
        try
        {
            var verified = await service.StandardizeImprovementAsync(verifiedImprovementId, request, subjectId, cancellationToken);
            if (verified.FacilityId != access.FacilityId) return TypedResults.NotFound();
            return TypedResults.Ok(FactoryLearningEndpointHelpers.MapVerifiedImprovement(verified));
        }
        catch (FactoryLearningException ex)
        {
            return FactoryLearningEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> SimulateAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] SimulateImprovementRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryLearningService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryLearningEndpointHelpers.ResolveAccessAsync(
            facilityId, ContinuousImprovementPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var result = await service.SimulateImprovementAsync(access.FacilityId, request, cancellationToken);
            return TypedResults.Ok(new
            {
                scenarioType = result.ScenarioType,
                output = result.OutputJson,
                comparison = result.ComparisonJson,
                dataClassification = result.DataClassification,
            });
        }
        catch (FactoryLearningException ex)
        {
            return FactoryLearningEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> CheckSustainabilityAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryLearningService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryLearningEndpointHelpers.ResolveAccessAsync(
            facilityId, ContinuousImprovementPermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        await service.CheckSustainabilityAsync(access.FacilityId, cancellationToken);
        var workspace = await service.GetWorkspaceAsync(access.FacilityId, cancellationToken);
        return TypedResults.Ok(FactoryLearningEndpointHelpers.MapWorkspace(workspace));
    }
}
