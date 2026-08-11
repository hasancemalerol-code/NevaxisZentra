using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Mold;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.Mold;

public static class MoldEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMoldEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/molds").RequireAuthorization();

        group.MapGet("/", ListMoldsAsync);
        group.MapGet("/workspace-summary", GetWorkspaceSummaryAsync);
        group.MapPost("/", CreateMoldAsync);
        group.MapGet("/{moldId:guid}", GetMoldAsync);
        group.MapPut("/{moldId:guid}", UpdateMoldAsync);
        group.MapPost("/{moldId:guid}/counter-snapshots", RecordCounterSnapshotAsync);
        group.MapGet("/{moldId:guid}/lifecycle-history", ListLifecycleHistoryAsync);
        group.MapPost("/{moldId:guid}/lifecycle-transitions", TransitionLifecycleAsync);

        return endpoints;
    }

    private static async Task<IResult> ListMoldsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? search,
        [FromQuery] string? lifecycleState,
        [FromQuery] string? ownershipType,
        [FromQuery] string? locationType,
        [FromQuery] string? sourceType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IMoldRepository moldRepository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var molds = await moldRepository.ListAsync(
            context,
            new MoldListFilters(search, lifecycleState, ownershipType, locationType, sourceType, page, pageSize),
            cancellationToken);

        return TypedResults.Ok(molds.Select(MoldApiMapper.MapSummary).ToList());
    }

    private static async Task<IResult> GetWorkspaceSummaryAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldRepository moldRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var summary = await moldRepository.GetWorkspaceSummaryAsync(context, cancellationToken);
        var canManage = await MoldEndpointHelpers.HasManageAccessAsync(
            facilityId!.Value,
            facilityAccessResolver,
            cancellationToken);

        return TypedResults.Ok(MoldApiMapper.MapWorkspaceSummary(summary, canManage));
    }

    private static async Task<IResult> GetMoldAsync(
        Guid moldId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldRepository moldRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var mold = await moldRepository.GetByIdAsync(context, moldId, cancellationToken);
        if (mold is null)
        {
            return TypedResults.Problem(
                title: "Mold not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: MoldEndpointHelpers.FailureExt(MoldApiFailureCodes.NotFound));
        }

        return TypedResults.Ok(MoldApiMapper.MapDetail(mold));
    }

    private static async Task<IResult> CreateMoldAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateMoldApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldRepository moldRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await moldRepository.CreateAsync(
            context,
            new CreateMoldCommand(
                body.Code,
                body.Name,
                body.Description,
                body.SerialNumber,
                body.Manufacturer,
                body.ManufactureYear,
                body.ManufactureDate,
                body.CommissioningDate,
                body.SourceType,
                body.ExternalSourceReference,
                body.OwnershipType,
                body.OwnerReference,
                body.LocationType,
                body.LocationLabel,
                body.MoldType,
                body.CavityCount,
                body.NominalShotLife,
                body.CurrentShotCount,
                body.PrimaryProductReference,
                body.MaterialFamilyReference,
                body.RunnerType,
                body.HotRunnerPresent,
                body.MoldWeightKg,
                body.DimensionLengthMm,
                body.DimensionWidthMm,
                body.DimensionHeightMm,
                body.Notes,
                body.IdempotencyKey),
            cancellationToken);

        return MoldEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateMoldAsync(
        Guid moldId,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateMoldApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldRepository moldRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await moldRepository.UpdateAsync(
            context,
            new UpdateMoldCommand(
                moldId,
                body.ExpectedVersion,
                body.Name,
                body.Description,
                body.SerialNumber,
                body.Manufacturer,
                body.ManufactureYear,
                body.ManufactureDate,
                body.CommissioningDate,
                body.ExternalSourceReference,
                body.OwnershipType,
                body.OwnerReference,
                body.LocationType,
                body.LocationLabel,
                body.MoldType,
                body.CavityCount,
                body.NominalShotLife,
                body.PrimaryProductReference,
                body.MaterialFamilyReference,
                body.RunnerType,
                body.HotRunnerPresent,
                body.MoldWeightKg,
                body.DimensionLengthMm,
                body.DimensionWidthMm,
                body.DimensionHeightMm,
                body.Notes),
            cancellationToken);

        return MoldEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> RecordCounterSnapshotAsync(
        Guid moldId,
        [FromQuery] Guid? facilityId,
        [FromBody] RecordMoldCounterSnapshotApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldRepository moldRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await moldRepository.RecordCounterSnapshotAsync(
            context,
            new RecordMoldCounterSnapshotCommand(
                moldId,
                body.ExpectedVersion,
                body.ShotCount,
                body.SourceType,
                body.SourceReference,
                body.Note,
                body.IdempotencyKey),
            cancellationToken);

        return MoldEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> ListLifecycleHistoryAsync(
        Guid moldId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldRepository moldRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var mold = await moldRepository.GetByIdAsync(context, moldId, cancellationToken);
        if (mold is null)
        {
            return TypedResults.Problem(
                title: "Mold not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: MoldEndpointHelpers.FailureExt(MoldApiFailureCodes.NotFound));
        }

        var events = await moldRepository.ListLifecycleHistoryAsync(context, moldId, cancellationToken);
        return TypedResults.Ok(events.Select(MoldApiMapper.MapLifecycleEvent).ToList());
    }

    private static async Task<IResult> TransitionLifecycleAsync(
        Guid moldId,
        [FromQuery] Guid? facilityId,
        [FromBody] TransitionMoldLifecycleApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldRepository moldRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await moldRepository.TransitionLifecycleAsync(
            context,
            new TransitionMoldLifecycleCommand(
                moldId,
                body.ExpectedVersion,
                body.TargetState,
                body.Reason,
                body.SourceReference,
                body.IdempotencyKey),
            cancellationToken);

        return MoldEndpointHelpers.MapWriteResult(result, isCreate: false);
    }
}

internal sealed record CreateMoldApiRequest(
    string Code,
    string Name,
    string? Description,
    string? SerialNumber,
    string? Manufacturer,
    int? ManufactureYear,
    DateOnly? ManufactureDate,
    DateOnly? CommissioningDate,
    string SourceType,
    string? ExternalSourceReference,
    string OwnershipType,
    string? OwnerReference,
    string LocationType,
    string? LocationLabel,
    string? MoldType,
    int? CavityCount,
    long? NominalShotLife,
    long? CurrentShotCount,
    string? PrimaryProductReference,
    string? MaterialFamilyReference,
    string? RunnerType,
    bool? HotRunnerPresent,
    decimal? MoldWeightKg,
    decimal? DimensionLengthMm,
    decimal? DimensionWidthMm,
    decimal? DimensionHeightMm,
    string? Notes,
    string? IdempotencyKey);

internal sealed record UpdateMoldApiRequest(
    uint ExpectedVersion,
    string Name,
    string? Description,
    string? SerialNumber,
    string? Manufacturer,
    int? ManufactureYear,
    DateOnly? ManufactureDate,
    DateOnly? CommissioningDate,
    string? ExternalSourceReference,
    string OwnershipType,
    string? OwnerReference,
    string LocationType,
    string? LocationLabel,
    string? MoldType,
    int? CavityCount,
    long? NominalShotLife,
    string? PrimaryProductReference,
    string? MaterialFamilyReference,
    string? RunnerType,
    bool? HotRunnerPresent,
    decimal? MoldWeightKg,
    decimal? DimensionLengthMm,
    decimal? DimensionWidthMm,
    decimal? DimensionHeightMm,
    string? Notes);

internal sealed record RecordMoldCounterSnapshotApiRequest(
    uint ExpectedVersion,
    long ShotCount,
    string SourceType,
    string? SourceReference,
    string? Note,
    string IdempotencyKey);

internal sealed record TransitionMoldLifecycleApiRequest(
    uint ExpectedVersion,
    string TargetState,
    string Reason,
    string? SourceReference,
    string IdempotencyKey);

internal static class MoldApiMapper
{
    internal static object MapSummary(MoldSummaryReadModel mold) => new
    {
        id = mold.Id,
        code = mold.Code,
        name = mold.Name,
        lifecycleState = mold.LifecycleState,
        ownershipType = mold.OwnershipType,
        locationType = mold.LocationType,
        locationLabel = mold.LocationLabel,
        facilityId = mold.FacilityId,
        cavityCount = mold.CavityCount,
        currentShotCount = mold.CurrentShotCount,
        counterRecordedAtUtc = mold.CounterRecordedAtUtc,
        sourceType = mold.SourceType,
        updatedAtUtc = mold.UpdatedAtUtc,
        version = mold.Version,
    };

    internal static object MapDetail(MoldDetailReadModel mold) => new
    {
        id = mold.Id,
        facilityId = mold.FacilityId,
        code = mold.Code,
        name = mold.Name,
        description = mold.Description,
        serialNumber = mold.SerialNumber,
        manufacturer = mold.Manufacturer,
        manufactureYear = mold.ManufactureYear,
        manufactureDate = mold.ManufactureDate,
        commissioningDate = mold.CommissioningDate,
        sourceType = mold.SourceType,
        externalSourceReference = mold.ExternalSourceReference,
        ownershipType = mold.OwnershipType,
        ownerReference = mold.OwnerReference,
        locationType = mold.LocationType,
        locationLabel = mold.LocationLabel,
        locationChangedAtUtc = mold.LocationChangedAtUtc,
        lifecycleState = mold.LifecycleState,
        moldType = mold.MoldType,
        cavityCount = mold.CavityCount,
        nominalShotLife = mold.NominalShotLife,
        currentShotCount = mold.CurrentShotCount,
        counterRecordedAtUtc = mold.CounterRecordedAtUtc,
        counterSource = mold.CounterSource,
        counterSourceReference = mold.CounterSourceReference,
        counterNote = mold.CounterNote,
        primaryProductReference = mold.PrimaryProductReference,
        materialFamilyReference = mold.MaterialFamilyReference,
        runnerType = mold.RunnerType,
        hotRunnerPresent = mold.HotRunnerPresent,
        moldWeightKg = mold.MoldWeightKg,
        dimensionLengthMm = mold.DimensionLengthMm,
        dimensionWidthMm = mold.DimensionWidthMm,
        dimensionHeightMm = mold.DimensionHeightMm,
        notes = mold.Notes,
        createdAtUtc = mold.CreatedAtUtc,
        updatedAtUtc = mold.UpdatedAtUtc,
        version = mold.Version,
    };

    internal static object MapWorkspaceSummary(MoldWorkspaceSummaryReadModel summary, bool canManage) => new
    {
        totalCount = summary.TotalCount,
        draftCount = summary.DraftCount,
        activeCount = summary.ActiveCount,
        maintenanceCount = summary.MaintenanceCount,
        trialCount = summary.TrialCount,
        quarantinedCount = summary.QuarantinedCount,
        retiredCount = summary.RetiredCount,
        scrappedCount = summary.ScrappedCount,
        capabilities = new
        {
            canRead = true,
            canManage,
        },
    };

    internal static object MapLifecycleEvent(MoldLifecycleEventReadModel item) => new
    {
        id = item.Id,
        previousState = item.PreviousState,
        newState = item.NewState,
        reason = item.Reason,
        actorDisplayName = item.ActorDisplayName,
        occurredAtUtc = item.OccurredAtUtc,
        sourceReference = item.SourceReference,
        version = item.Version,
    };
}

public static class MoldServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSMold(this IServiceCollection services)
    {
        services.AddScoped<IMoldRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableMoldRepository()
                : new MoldRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        return services;
    }
}
