using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Maintenance;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.Maintenance;

public static class MaintenanceAiEndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapMaintenanceAiEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/assets/{assetReference}/ai-drafts/generate", GenerateDraftAsync);
        group.MapGet("/assets/{assetReference}/ai-drafts/current", GetCurrentDraftAsync);
        group.MapGet("/assets/{assetReference}/ai-drafts", ListDraftsAsync);
        group.MapGet("/ai-drafts/{draftId:guid}", GetDraftByIdAsync);
        group.MapPut("/ai-drafts/{draftId:guid}", UpdateDraftAsync);
        group.MapPost("/ai-drafts/{draftId:guid}/approve", ApproveDraftAsync);
        group.MapPost("/ai-drafts/{draftId:guid}/reject", RejectDraftAsync);
        return group;
    }

    private static async Task<IResult> GenerateDraftAsync(
        string assetReference,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceAiDraftRepository draftRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            MaintenancePermissionKeys.Manage,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await draftRepository.GenerateDraftAsync(context, assetReference, cancellationToken);
        return MapDraftWriteResult(result);
    }

    private static async Task<IResult> GetCurrentDraftAsync(
        string assetReference,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceAiDraftRepository draftRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var draft = await draftRepository.GetCurrentDraftAsync(context, assetReference, cancellationToken);
        return draft is null
            ? TypedResults.NotFound(new { code = MaintenanceApiFailureCodes.NotFound, message = "AI draft not found." })
            : TypedResults.Ok(MaintenanceApiMapper.MapMaintenanceAiDraftDetail(draft));
    }

    private static async Task<IResult> ListDraftsAsync(
        string assetReference,
        [FromQuery] Guid? facilityId,
        [FromQuery] int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceAiDraftRepository draftRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var drafts = await draftRepository.ListDraftsAsync(context, assetReference, limit, cancellationToken);
        return TypedResults.Ok(drafts.Select(MaintenanceApiMapper.MapMaintenanceAiDraftSummary).ToList());
    }

    private static async Task<IResult> GetDraftByIdAsync(
        Guid draftId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceAiDraftRepository draftRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var draft = await draftRepository.GetDraftByIdAsync(context, draftId, cancellationToken);
        return draft is null
            ? TypedResults.NotFound(new { code = MaintenanceApiFailureCodes.NotFound, message = "AI draft not found." })
            : TypedResults.Ok(MaintenanceApiMapper.MapMaintenanceAiDraftDetail(draft));
    }

    private static async Task<IResult> UpdateDraftAsync(
        Guid draftId,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateMaintenanceAiDraftApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceAiDraftRepository draftRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            MaintenancePermissionKeys.Manage,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await draftRepository.UpdateDraftAsync(
            context,
            new UpdateMaintenanceAiDraftCommand(
                draftId,
                body.ExpectedVersion,
                body.Title,
                body.Description,
                body.Priority,
                body.RequestType),
            cancellationToken);

        return MapDraftWriteResult(result);
    }

    private static async Task<IResult> ApproveDraftAsync(
        Guid draftId,
        [FromQuery] Guid? facilityId,
        [FromBody] ApproveMaintenanceAiDraftApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceAiDraftRepository draftRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            MaintenancePermissionKeys.Manage,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await draftRepository.ApproveDraftAsync(
            context,
            new ApproveMaintenanceAiDraftCommand(draftId, body.ExpectedVersion),
            cancellationToken);

        return MapDraftWriteResult(result);
    }

    private static async Task<IResult> RejectDraftAsync(
        Guid draftId,
        [FromQuery] Guid? facilityId,
        [FromBody] RejectMaintenanceAiDraftApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceAiDraftRepository draftRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            MaintenancePermissionKeys.Manage,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await draftRepository.RejectDraftAsync(
            context,
            new RejectMaintenanceAiDraftCommand(draftId, body.ExpectedVersion, body.RejectionReason),
            cancellationToken);

        return MapDraftWriteResult(result);
    }

    private static IResult MapDraftWriteResult(MaintenanceAiDraftWriteResult result)
    {
        if (!result.Succeeded)
        {
            var status = result.FailureCode switch
            {
                MaintenanceAiFailureCodes.NotFound => StatusCodes.Status404NotFound,
                MaintenanceAiFailureCodes.ConcurrencyConflict => StatusCodes.Status409Conflict,
                MaintenanceAiFailureCodes.StaleDraft => StatusCodes.Status409Conflict,
                MaintenanceAiFailureCodes.ValidationFailed or MaintenanceAiFailureCodes.InvalidOutput => StatusCodes.Status422UnprocessableEntity,
                _ => StatusCodes.Status400BadRequest,
            };

            return TypedResults.Problem(
                title: "Maintenance AI draft operation failed.",
                statusCode: status,
                extensions: MaintenanceEndpointHelpers.FailureExt(result.FailureCode ?? MaintenanceApiFailureCodes.ValidationFailed));
        }

        return TypedResults.Ok(new
        {
            draftId = result.DraftId,
            version = result.Version,
            maintenanceRequestId = result.MaintenanceRequestId,
            requestNumber = result.RequestNumber,
        });
    }

    public static IServiceCollection AddCrevaFactOSMaintenanceAi(this IServiceCollection services)
    {
        services.AddScoped<IMaintenanceAiProvider, DeterministicMaintenanceAiProvider>();
        services.AddScoped<IMaintenanceAiDraftRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableMaintenanceAiDraftRepository()
                : new MaintenanceAiDraftRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                    serviceProvider.GetRequiredService<IMaintenanceAiProvider>());
        });

        return services;
    }
}

internal sealed record UpdateMaintenanceAiDraftApiRequest(
    uint ExpectedVersion,
    string? Title,
    string? Description,
    string? Priority,
    string? RequestType);

internal sealed record ApproveMaintenanceAiDraftApiRequest(uint ExpectedVersion);

internal sealed record RejectMaintenanceAiDraftApiRequest(
    uint ExpectedVersion,
    string RejectionReason);

public sealed class UnavailableMaintenanceAiDraftRepository : IMaintenanceAiDraftRepository
{
    public Task<MaintenanceAiDraftDetailReadModel?> GetCurrentDraftAsync(
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<MaintenanceAiDraftDetailReadModel?>(null);

    public Task<IReadOnlyList<MaintenanceAiDraftSummaryReadModel>> ListDraftsAsync(
        MaintenanceOperationContext context,
        string assetReference,
        int? limit,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MaintenanceAiDraftSummaryReadModel>>([]);

    public Task<MaintenanceAiDraftDetailReadModel?> GetDraftByIdAsync(
        MaintenanceOperationContext context,
        Guid draftId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<MaintenanceAiDraftDetailReadModel?>(null);

    public Task<MaintenanceAiDraftWriteResult> GenerateDraftAsync(
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(MaintenanceAiDraftWriteResult.Fail(
            MaintenanceFailureCodes.SourceUnavailable,
            "Maintenance AI service is unavailable."));

    public Task<MaintenanceAiDraftWriteResult> UpdateDraftAsync(
        MaintenanceOperationContext context,
        UpdateMaintenanceAiDraftCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(MaintenanceAiDraftWriteResult.Fail(
            MaintenanceFailureCodes.SourceUnavailable,
            "Maintenance AI service is unavailable."));

    public Task<MaintenanceAiDraftWriteResult> ApproveDraftAsync(
        MaintenanceOperationContext context,
        ApproveMaintenanceAiDraftCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(MaintenanceAiDraftWriteResult.Fail(
            MaintenanceFailureCodes.SourceUnavailable,
            "Maintenance AI service is unavailable."));

    public Task<MaintenanceAiDraftWriteResult> RejectDraftAsync(
        MaintenanceOperationContext context,
        RejectMaintenanceAiDraftCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(MaintenanceAiDraftWriteResult.Fail(
            MaintenanceFailureCodes.SourceUnavailable,
            "Maintenance AI service is unavailable."));
}
