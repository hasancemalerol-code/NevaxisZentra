using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.DigitalWorkforce;
using NevaxisZentra.Persistence.FactoryDecisionCenter;
using NevaxisZentra.Persistence.FactoryOutcomeExecution;
using NevaxisZentra.Persistence.FactoryRecoveryIntelligence;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using NevaxisZentra.Persistence;

namespace NevaxisZentra.Api.FactoryDecisionCenter;

public static class FactoryDecisionCenterEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapFactoryDecisionCenterEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/factory-decision-center").RequireAuthorization();

        group.MapGet("/workspace", GetWorkspaceAsync);
        group.MapGet("/cases/{caseId:guid}", GetCaseDetailAsync);
        group.MapPost("/cases/{caseId:guid}/approvals/{approvalId:guid}/decision", DecideApprovalAsync);
        group.MapPost("/cases/{caseId:guid}/execute", ExecuteRecoveryAsync);
        group.MapPost("/cases/{caseId:guid}/verify", VerifyOutcomeAsync);
        group.MapPost("/reset", ResetScenarioAsync);
        group.MapGet("/learning/{learningRecordId:guid}", GetHistoricalLearningCaseAsync);
        group.MapGet("/learning/search", SearchHistoricalLearningCasesAsync);
        group.MapPost("/learning/dev-fixtures/seed", SeedDevelopmentLearningFixturesAsync);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisZentraFactoryDecisionCenter(this IServiceCollection services)
    {
        services.AddScoped<IFactoryDecisionCenterRepository, FactoryDecisionCenterRepository>();
        services.AddScoped<IFactoryDecisionCenterAiTaskSyncService, FactoryDecisionCenterAiTaskSyncService>();
        services.AddScoped<IFactoryDecisionCenterService, FactoryDecisionCenterService>();
        services.AddScoped<IFactoryOutcomeExecutionRepository, FactoryOutcomeExecutionRepository>();
        services.AddScoped<IFactoryOutcomeExecutionService, FactoryOutcomeExecutionService>();
        services.AddScoped<IFactoryOutcomeAiTaskSyncService, FactoryOutcomeAiTaskSyncService>();
        services.AddScoped<IFactoryOutcomeLearningService, FactoryOutcomeLearningService>();
        services.AddScoped<IFactoryRecoveryLearningRepository, FactoryRecoveryLearningRepository>();
        services.AddScoped<IFactoryRecoveryIntelligenceService, FactoryRecoveryIntelligenceService>();
        return services;
    }

    private static async Task<IResult> GetWorkspaceAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryDecisionCenterService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryDecisionCenterEndpointHelpers.ResolveAccessAsync(
            facilityId, FactoryOrchestrationPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var workspace = await service.GetWorkspaceAsync(access.FacilityId, cancellationToken);
        return TypedResults.Ok(FactoryDecisionCenterEndpointHelpers.MapWorkspace(workspace));
    }

    private static async Task<IResult> GetCaseDetailAsync(
        Guid caseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryDecisionCenterService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryDecisionCenterEndpointHelpers.ResolveAccessAsync(
            facilityId, FactoryOrchestrationPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var detail = await service.GetCaseDetailAsync(caseId, cancellationToken);
            return TypedResults.Ok(FactoryDecisionCenterEndpointHelpers.MapCaseDetail(detail));
        }
        catch (FactoryDecisionCenterException ex)
        {
            return FactoryDecisionCenterEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> DecideApprovalAsync(
        Guid caseId,
        Guid approvalId,
        [FromQuery] Guid? facilityId,
        [FromBody] FactoryDecisionCenterApprovalRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IFactoryDecisionCenterService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryDecisionCenterEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.ApprovalDecide, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";

        try
        {
            var detail = await service.DecideApprovalAsync(caseId, approvalId, request, subjectId, cancellationToken);
            return TypedResults.Ok(FactoryDecisionCenterEndpointHelpers.MapCaseDetail(detail));
        }
        catch (FactoryDecisionCenterException ex)
        {
            return FactoryDecisionCenterEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> ExecuteRecoveryAsync(
        Guid caseId,
        [FromQuery] Guid? facilityId,
        [FromBody] FactoryDecisionCenterExecuteRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IFactoryDecisionCenterService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryDecisionCenterEndpointHelpers.ResolveAccessAsync(
            facilityId, FactoryOrchestrationPermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";

        try
        {
            var detail = await service.AdvanceExecutionAsync(caseId, request.ConcurrencyToken, subjectId, cancellationToken);
            return TypedResults.Ok(FactoryDecisionCenterEndpointHelpers.MapCaseDetail(detail));
        }
        catch (FactoryDecisionCenterException ex)
        {
            return FactoryDecisionCenterEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> VerifyOutcomeAsync(
        Guid caseId,
        [FromQuery] Guid? facilityId,
        [FromBody] FactoryDecisionCenterVerifyRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IFactoryDecisionCenterService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryDecisionCenterEndpointHelpers.ResolveAccessAsync(
            facilityId, FactoryOrchestrationPermissionKeys.Verify, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";

        try
        {
            var detail = await service.VerifyOutcomeAsync(caseId, request, subjectId, cancellationToken);
            return TypedResults.Ok(FactoryDecisionCenterEndpointHelpers.MapCaseDetail(detail));
        }
        catch (FactoryDecisionCenterException ex)
        {
            return FactoryDecisionCenterEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> GetHistoricalLearningCaseAsync(
        Guid learningRecordId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryDecisionCenterService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryDecisionCenterEndpointHelpers.ResolveAccessAsync(
            facilityId, FactoryOrchestrationPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var detail = await service.GetHistoricalLearningCaseAsync(learningRecordId, cancellationToken);
            return TypedResults.Ok(FactoryDecisionCenterEndpointHelpers.MapHistoricalLearningCase(detail));
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.NotFound(new { code = "not-found", message = ex.Message });
        }
    }

    private static async Task<IResult> SearchHistoricalLearningCasesAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? outcomeType,
        [FromQuery] string? machineCode,
        [FromQuery] string? orderReference,
        [FromQuery] string? eventCategory,
        [FromQuery] string? strategyKey,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryDecisionCenterService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryDecisionCenterEndpointHelpers.ResolveAccessAsync(
            facilityId, FactoryOrchestrationPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var results = await service.SearchHistoricalLearningCasesAsync(
            access.FacilityId,
            new FactoryRecoveryLearningSearchFilters(
                outcomeType, machineCode, orderReference, null, eventCategory, strategyKey, null, null, 20),
            cancellationToken);
        return TypedResults.Ok(results.Select(FactoryDecisionCenterEndpointHelpers.MapHistoricalLearningCase).ToList());
    }

    private static async Task<IResult> SeedDevelopmentLearningFixturesAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        NevaxisZentraDbContext dbContext,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return TypedResults.Forbid();
        }

        var (access, failure) = await FactoryDecisionCenterEndpointHelpers.ResolveAccessAsync(
            facilityId, FactoryOrchestrationPermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        await DevelopmentFactoryRecoveryLearningFixtureSeeder.SeedAsync(
            dbContext, access.TenantId, access.FacilityId, cancellationToken);
        return TypedResults.Ok(new { seeded = true });
    }

    private static async Task<IResult> ResetScenarioAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IFactoryDecisionCenterService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await FactoryDecisionCenterEndpointHelpers.ResolveAccessAsync(
            facilityId, FactoryOrchestrationPermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";

        try
        {
            await service.ResetScenarioAsync(access.FacilityId, subjectId, cancellationToken);
            var workspace = await service.GetWorkspaceAsync(access.FacilityId, cancellationToken);
            return TypedResults.Ok(FactoryDecisionCenterEndpointHelpers.MapWorkspace(workspace));
        }
        catch (FactoryDecisionCenterException ex)
        {
            return FactoryDecisionCenterEndpointHelpers.MapException(ex);
        }
    }
}

public sealed record FactoryDecisionCenterExecuteRequest(long ConcurrencyToken);
