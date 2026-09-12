using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.Mold;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisFactOS.Api.Mold;

public static class MoldHistoryEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMoldHistoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/molds").RequireAuthorization();

        group.MapGet("/{moldId:guid}/maintenance-history", ListMaintenanceHistoryAsync);
        group.MapGet("/{moldId:guid}/maintenance-history/{maintenanceId:guid}", GetMaintenanceAsync);
        group.MapPost("/{moldId:guid}/maintenance-history", CreateMaintenanceAsync);
        group.MapPut("/{moldId:guid}/maintenance-history/{maintenanceId:guid}", UpdateMaintenanceAsync);
        group.MapPost("/{moldId:guid}/maintenance-history/{maintenanceId:guid}/complete", CompleteMaintenanceAsync);
        group.MapPost("/{moldId:guid}/maintenance-history/{maintenanceId:guid}/cancel", CancelMaintenanceAsync);

        group.MapGet("/{moldId:guid}/revision-history", ListRevisionHistoryAsync);
        group.MapGet("/{moldId:guid}/revision-history/{revisionId:guid}", GetRevisionAsync);
        group.MapPost("/{moldId:guid}/revision-history", AppendRevisionAsync);

        group.MapGet("/{moldId:guid}/trial-history", ListTrialHistoryAsync);
        group.MapGet("/{moldId:guid}/trial-history/{trialId:guid}", GetTrialAsync);
        group.MapPost("/{moldId:guid}/trial-history", AppendTrialAsync);

        group.MapGet("/{moldId:guid}/timeline", ListTimelineAsync);

        return endpoints;
    }

    private static async Task<IResult> ListMaintenanceHistoryAsync(
        Guid moldId,
        [FromQuery] Guid? facilityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IMoldHistoryRepository historyRepository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var items = await historyRepository.ListMaintenanceAsync(
            context, moldId, new MoldHistoryListFilters(page, pageSize), cancellationToken);
        return TypedResults.Ok(items.Select(MoldHistoryApiMapper.MapMaintenance).ToList());
    }

    private static async Task<IResult> GetMaintenanceAsync(
        Guid moldId,
        Guid maintenanceId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldHistoryRepository historyRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var item = await historyRepository.GetMaintenanceAsync(context, moldId, maintenanceId, cancellationToken);
        return item is null
            ? MoldEndpointHelpers.MapMoldFailure(MoldFailureCodes.NotFound, "Maintenance record not found.")
            : TypedResults.Ok(MoldHistoryApiMapper.MapMaintenance(item));
    }

    private static async Task<IResult> CreateMaintenanceAsync(
        Guid moldId,
        [FromBody] CreateMaintenanceRequestDto body,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldHistoryRepository historyRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var command = new CreateMoldMaintenanceCommand(
            moldId,
            body.MaintenanceType,
            body.Status,
            body.OpenedAtUtc,
            body.Summary,
            body.Findings,
            body.ActionsTaken,
            body.PerformedByDisplayName,
            body.ExternalMaintenanceRequestReference,
            body.ExternalWorkOrderReference,
            body.SourceType,
            body.SourceReference,
            body.IdempotencyKey);

        var result = await historyRepository.CreateMaintenanceAsync(context, command, cancellationToken);
        return MoldEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateMaintenanceAsync(
        Guid moldId,
        Guid maintenanceId,
        [FromBody] UpdateMaintenanceRequestDto body,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldHistoryRepository historyRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var command = new UpdateMoldMaintenanceCommand(
            moldId,
            maintenanceId,
            body.ExpectedVersion,
            body.MaintenanceType,
            body.Status,
            body.OpenedAtUtc,
            body.Summary,
            body.Findings,
            body.ActionsTaken,
            body.PerformedByDisplayName,
            body.ExternalMaintenanceRequestReference,
            body.ExternalWorkOrderReference,
            body.SourceType,
            body.SourceReference);

        var result = await historyRepository.UpdateMaintenanceAsync(context, command, cancellationToken);
        return MoldEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> CompleteMaintenanceAsync(
        Guid moldId,
        Guid maintenanceId,
        [FromBody] CompleteMaintenanceRequestDto body,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldHistoryRepository historyRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var command = new CompleteMoldMaintenanceCommand(
            moldId,
            maintenanceId,
            body.ExpectedVersion,
            body.CompletedAtUtc,
            body.Findings,
            body.ActionsTaken,
            body.PerformedByDisplayName);

        var result = await historyRepository.CompleteMaintenanceAsync(context, command, cancellationToken);
        return MoldEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> CancelMaintenanceAsync(
        Guid moldId,
        Guid maintenanceId,
        [FromBody] CancelMaintenanceRequestDto body,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldHistoryRepository historyRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var command = new CancelMoldMaintenanceCommand(moldId, maintenanceId, body.ExpectedVersion, body.Findings);
        var result = await historyRepository.CancelMaintenanceAsync(context, command, cancellationToken);
        return MoldEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> ListRevisionHistoryAsync(
        Guid moldId,
        [FromQuery] Guid? facilityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IMoldHistoryRepository historyRepository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var items = await historyRepository.ListRevisionsAsync(
            context, moldId, new MoldHistoryListFilters(page, pageSize), cancellationToken);
        return TypedResults.Ok(items.Select(MoldHistoryApiMapper.MapRevision).ToList());
    }

    private static async Task<IResult> GetRevisionAsync(
        Guid moldId,
        Guid revisionId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldHistoryRepository historyRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var item = await historyRepository.GetRevisionAsync(context, moldId, revisionId, cancellationToken);
        return item is null
            ? MoldEndpointHelpers.MapMoldFailure(MoldFailureCodes.NotFound, "Revision record not found.")
            : TypedResults.Ok(MoldHistoryApiMapper.MapRevision(item));
    }

    private static async Task<IResult> AppendRevisionAsync(
        Guid moldId,
        [FromBody] AppendRevisionRequestDto body,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldHistoryRepository historyRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var command = new AppendMoldRevisionCommand(
            moldId,
            body.RevisionNumber,
            body.RevisionType,
            body.RevisionDate,
            body.Summary,
            body.Reason,
            body.TechnicalChange,
            body.PreviousCavityCount,
            body.NewCavityCount,
            body.PerformedByDisplayName,
            body.ExternalReference,
            body.SourceType,
            body.SourceReference,
            body.IdempotencyKey);

        var result = await historyRepository.AppendRevisionAsync(context, command, cancellationToken);
        return MoldEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> ListTrialHistoryAsync(
        Guid moldId,
        [FromQuery] Guid? facilityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IMoldHistoryRepository historyRepository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var items = await historyRepository.ListTrialsAsync(
            context, moldId, new MoldHistoryListFilters(page, pageSize), cancellationToken);
        return TypedResults.Ok(items.Select(MoldHistoryApiMapper.MapTrial).ToList());
    }

    private static async Task<IResult> GetTrialAsync(
        Guid moldId,
        Guid trialId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldHistoryRepository historyRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var item = await historyRepository.GetTrialAsync(context, moldId, trialId, cancellationToken);
        return item is null
            ? MoldEndpointHelpers.MapMoldFailure(MoldFailureCodes.NotFound, "Trial record not found.")
            : TypedResults.Ok(MoldHistoryApiMapper.MapTrial(item));
    }

    private static async Task<IResult> AppendTrialAsync(
        Guid moldId,
        [FromBody] AppendTrialRequestDto body,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldHistoryRepository historyRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var command = new AppendMoldTrialCommand(
            moldId,
            body.TrialNumber,
            body.TrialDate,
            body.Result,
            body.MachineReference,
            body.ProductionOrderReference,
            body.ProductReference,
            body.MaterialReference,
            body.CycleTimeSeconds,
            body.ShotCount,
            body.GoodPartCount,
            body.ScrapPartCount,
            body.Findings,
            body.Decision,
            body.PerformedByDisplayName,
            body.SourceType,
            body.SourceReference,
            body.IdempotencyKey);

        var result = await historyRepository.AppendTrialAsync(context, command, cancellationToken);
        return MoldEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> ListTimelineAsync(
        Guid moldId,
        [FromQuery] Guid? facilityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IMoldHistoryRepository historyRepository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var items = await historyRepository.ListTimelineAsync(
            context, moldId, new MoldHistoryListFilters(page, pageSize), cancellationToken);
        return TypedResults.Ok(items.Select(MoldHistoryApiMapper.MapTimelineEntry).ToList());
    }
}

internal static class MoldHistoryApiMapper
{
    internal static object MapMaintenance(MoldMaintenanceReadModel item) => new
    {
        id = item.Id,
        moldId = item.MoldId,
        maintenanceType = item.MaintenanceType,
        status = item.Status,
        openedAtUtc = item.OpenedAtUtc,
        completedAtUtc = item.CompletedAtUtc,
        summary = item.Summary,
        findings = item.Findings,
        actionsTaken = item.ActionsTaken,
        performedByDisplayName = item.PerformedByDisplayName,
        externalMaintenanceRequestReference = item.ExternalMaintenanceRequestReference,
        externalWorkOrderReference = item.ExternalWorkOrderReference,
        sourceType = item.SourceType,
        sourceReference = item.SourceReference,
        createdAtUtc = item.CreatedAtUtc,
        updatedAtUtc = item.UpdatedAtUtc,
        version = item.Version,
    };

    internal static object MapRevision(MoldRevisionReadModel item) => new
    {
        id = item.Id,
        moldId = item.MoldId,
        revisionNumber = item.RevisionNumber,
        revisionType = item.RevisionType,
        revisionDate = item.RevisionDate,
        summary = item.Summary,
        reason = item.Reason,
        technicalChange = item.TechnicalChange,
        previousCavityCount = item.PreviousCavityCount,
        newCavityCount = item.NewCavityCount,
        performedByDisplayName = item.PerformedByDisplayName,
        externalReference = item.ExternalReference,
        sourceType = item.SourceType,
        sourceReference = item.SourceReference,
        createdAtUtc = item.CreatedAtUtc,
    };

    internal static object MapTrial(MoldTrialReadModel item) => new
    {
        id = item.Id,
        moldId = item.MoldId,
        trialNumber = item.TrialNumber,
        trialDate = item.TrialDate,
        result = item.Result,
        machineReference = item.MachineReference,
        productionOrderReference = item.ProductionOrderReference,
        productReference = item.ProductReference,
        materialReference = item.MaterialReference,
        cycleTimeSeconds = item.CycleTimeSeconds,
        shotCount = item.ShotCount,
        goodPartCount = item.GoodPartCount,
        scrapPartCount = item.ScrapPartCount,
        findings = item.Findings,
        decision = item.Decision,
        performedByDisplayName = item.PerformedByDisplayName,
        sourceType = item.SourceType,
        sourceReference = item.SourceReference,
        createdAtUtc = item.CreatedAtUtc,
    };

    internal static object MapTimelineEntry(MoldTimelineEntryReadModel item) => new
    {
        id = item.Id,
        eventKind = item.EventKind,
        eventDateUtc = item.EventDateUtc,
        createdAtUtc = item.CreatedAtUtc,
        title = item.Title,
        summary = item.Summary,
        statusOrResult = item.StatusOrResult,
        reference = item.Reference,
        actorDisplayName = item.ActorDisplayName,
    };
}

internal sealed record CreateMaintenanceRequestDto(
    string MaintenanceType,
    string Status,
    DateTimeOffset OpenedAtUtc,
    string Summary,
    string? Findings,
    string? ActionsTaken,
    string? PerformedByDisplayName,
    string? ExternalMaintenanceRequestReference,
    string? ExternalWorkOrderReference,
    string SourceType,
    string? SourceReference,
    string IdempotencyKey);

internal sealed record UpdateMaintenanceRequestDto(
    uint ExpectedVersion,
    string MaintenanceType,
    string Status,
    DateTimeOffset OpenedAtUtc,
    string Summary,
    string? Findings,
    string? ActionsTaken,
    string? PerformedByDisplayName,
    string? ExternalMaintenanceRequestReference,
    string? ExternalWorkOrderReference,
    string SourceType,
    string? SourceReference);

internal sealed record CompleteMaintenanceRequestDto(
    uint ExpectedVersion,
    DateTimeOffset CompletedAtUtc,
    string? Findings,
    string? ActionsTaken,
    string? PerformedByDisplayName);

internal sealed record CancelMaintenanceRequestDto(uint ExpectedVersion, string? Findings);

internal sealed record AppendRevisionRequestDto(
    string RevisionNumber,
    string RevisionType,
    DateOnly RevisionDate,
    string Summary,
    string? Reason,
    string? TechnicalChange,
    int? PreviousCavityCount,
    int? NewCavityCount,
    string? PerformedByDisplayName,
    string? ExternalReference,
    string SourceType,
    string? SourceReference,
    string IdempotencyKey);

internal sealed record AppendTrialRequestDto(
    string TrialNumber,
    DateOnly TrialDate,
    string Result,
    string? MachineReference,
    string? ProductionOrderReference,
    string? ProductReference,
    string? MaterialReference,
    decimal? CycleTimeSeconds,
    int? ShotCount,
    int? GoodPartCount,
    int? ScrapPartCount,
    string? Findings,
    string? Decision,
    string? PerformedByDisplayName,
    string SourceType,
    string? SourceReference,
    string IdempotencyKey);
