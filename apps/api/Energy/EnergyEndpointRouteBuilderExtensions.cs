using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.Energy;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisFactOS.Api.Energy;

public static class EnergyEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapEnergyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/energy").RequireAuthorization();

        group.MapGet("/workspace", GetWorkspaceAsync);
        group.MapGet("/measurement-points", ListMeasurementPointsAsync);
        group.MapGet("/measurement-points/{reference}", GetMeasurementPointAsync);
        group.MapGet("/consumption", ListConsumptionAsync);
        group.MapGet("/allocations", ListAllocationsAsync);
        group.MapPost("/readings", IngestReadingAsync);
        group.MapPost("/consumption/evaluate", EvaluateConsumptionAsync);

        return endpoints;
    }

    private static async Task<IResult> GetWorkspaceAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IEnergyRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EnergyEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var workspace = await repository.GetWorkspaceAsync(context, cancellationToken);
        var canManage = await EnergyEndpointHelpers.HasManageAccessAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);

        return TypedResults.Ok(new
        {
            activeMeasurementPointCount = workspace.ActiveMeasurementPointCount,
            recentWindowCount = workspace.RecentWindowCount,
            ruleSetVersion = workspace.RuleSetVersion,
            provenanceDisclaimer = workspace.ProvenanceDisclaimer,
            hasDevelopmentPilotData = workspace.HasDevelopmentPilotData,
            capabilities = new { canRead = true, canManage },
        });
    }

    private static async Task<IResult> ListMeasurementPointsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? measurementScope,
        [FromQuery] string? equipmentCategory,
        [FromQuery] Guid? machineAssetId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IEnergyRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await EnergyEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ListMeasurementPointsAsync(
            context,
            new EnergyMeasurementPointListFilters(measurementScope, equipmentCategory, machineAssetId, isActive, page, pageSize),
            cancellationToken);

        var canManage = await EnergyEndpointHelpers.HasManageAccessAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);

        return TypedResults.Ok(new
        {
            items = result.Items.Select(MapPoint),
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
            capabilities = new { canRead = true, canManage },
        });
    }

    private static async Task<IResult> GetMeasurementPointAsync(
        string reference,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IEnergyRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EnergyEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var point = await repository.GetMeasurementPointByReferenceAsync(context, reference, cancellationToken);
        if (point is null)
        {
            return EnergyEndpointHelpers.MapFailure(
                EnergyFailureCodes.MeasurementPointNotFound,
                "Measurement point not found.");
        }

        var canManage = await EnergyEndpointHelpers.HasManageAccessAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);

        return TypedResults.Ok(new
        {
            measurementPoint = MapPoint(point),
            capabilities = new { canRead = true, canManage },
        });
    }

    private static async Task<IResult> ListConsumptionAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? measurementPointReference,
        [FromQuery] DateTimeOffset? windowStartUtc,
        [FromQuery] DateTimeOffset? windowEndUtc,
        [FromQuery] string? evidenceStatus,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IEnergyRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await EnergyEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ListConsumptionWindowsAsync(
            context,
            new EnergyConsumptionListFilters(
                measurementPointReference,
                windowStartUtc,
                windowEndUtc,
                evidenceStatus,
                page,
                pageSize),
            cancellationToken);

        return TypedResults.Ok(new
        {
            items = result.Items.Select(MapConsumption),
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
        });
    }

    private static async Task<IResult> ListAllocationsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? consumptionWindowId,
        [FromQuery] string? targetType,
        [FromQuery] string? targetReference,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IEnergyRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await EnergyEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ListAllocationsAsync(
            context,
            new EnergyAllocationListFilters(consumptionWindowId, targetType, targetReference, page, pageSize),
            cancellationToken);

        return TypedResults.Ok(new
        {
            items = result.Items.Select(MapAllocation),
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
        });
    }

    private static async Task<IResult> IngestReadingAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] IngestEnergyReadingRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IEnergyRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EnergyEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var command = new IngestEnergyReadingCommand(
            body.MeasurementPointReference,
            body.OccurredAtUtc,
            body.CumulativeEnergyKwh,
            body.IntervalEnergyKwh,
            body.ActivePowerKw,
            body.MaximumDemandKw,
            body.SourceType,
            body.SourceReference,
            body.FreshnessState,
            body.EvidenceState,
            body.IdempotencyKey);

        var result = await repository.IngestReadingAsync(context, command, cancellationToken);
        return EnergyEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> EvaluateConsumptionAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] EvaluateEnergyConsumptionRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IEnergyRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EnergyEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var allocations = body.Allocations?
            .Select(a => new EnergyAllocationInput(
                a.TargetType,
                a.TargetReference,
                a.AllocatedKwh,
                a.AllocationMethod,
                a.EvidenceSource,
                a.ConfidenceStatus))
            .ToList();

        var command = new EvaluateEnergyConsumptionCommand(
            body.MeasurementPointReference,
            body.WindowStartUtc,
            body.WindowEndUtc,
            body.TariffSnapshotId,
            allocations,
            body.IdempotencyKey);

        var result = await repository.EvaluateConsumptionAsync(context, command, cancellationToken);
        return EnergyEndpointHelpers.MapWriteResult(result);
    }

    private static object MapPoint(EnergyMeasurementPointReadModel point) => new
    {
        id = point.Id,
        pointCode = point.PointCode,
        displayName = point.DisplayName,
        measurementScope = point.MeasurementScope,
        machineAssetId = point.MachineAssetId,
        machineAssetLabel = point.MachineAssetLabel,
        edgeAssetReference = point.EdgeAssetReference,
        equipmentCategory = point.EquipmentCategory,
        measurementUnit = point.MeasurementUnit,
        status = point.Status,
        sourceProvenance = point.SourceProvenance,
        version = point.Version,
        isPilotData = point.IsPilotData,
    };

    private static object MapConsumption(EnergyConsumptionWindowReadModel window) => new
    {
        id = window.Id,
        measurementPointId = window.MeasurementPointId,
        measurementPointCode = window.MeasurementPointCode,
        windowStartUtc = window.WindowStartUtc,
        windowEndUtc = window.WindowEndUtc,
        consumedKwh = window.ConsumedKwh,
        averagePowerKw = window.AveragePowerKw,
        maximumDemandKw = window.MaximumDemandKw,
        calculationMethod = window.CalculationMethod,
        evidenceStatus = window.EvidenceStatus,
        readingCount = window.ReadingCount,
        freshnessState = window.FreshnessState,
        ruleSetVersion = window.RuleSetVersion,
        costAmount = window.CostAmount,
        costCurrencyCode = window.CostCurrencyCode,
        missingDataExplanation = window.MissingDataExplanation,
        calculatedAtUtc = window.CalculatedAtUtc,
        isPilotData = window.IsPilotData,
    };

    private static object MapAllocation(EnergyAllocationReadModel allocation) => new
    {
        id = allocation.Id,
        consumptionWindowId = allocation.ConsumptionWindowId,
        targetType = allocation.TargetType,
        targetReference = allocation.TargetReference,
        allocatedKwh = allocation.AllocatedKwh,
        allocationMethod = allocation.AllocationMethod,
        evidenceSource = allocation.EvidenceSource,
        confidenceStatus = allocation.ConfidenceStatus,
        ruleSetVersion = allocation.RuleSetVersion,
        calculatedAtUtc = allocation.CalculatedAtUtc,
    };

    public static IServiceCollection AddNevaxisFactOSEnergy(this IServiceCollection services)
    {
        services.AddScoped<IEnergyRepository>(sp =>
        {
            var dbContext = sp.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableEnergyRepository()
                : new EnergyRepository(dbContext, sp.GetRequiredService<TenantDatabaseSession>());
        });

        return services;
    }
}

internal sealed record IngestEnergyReadingRequest(
    string MeasurementPointReference,
    DateTimeOffset OccurredAtUtc,
    decimal? CumulativeEnergyKwh,
    decimal? IntervalEnergyKwh,
    decimal? ActivePowerKw,
    decimal? MaximumDemandKw,
    string SourceType,
    string? SourceReference,
    string? FreshnessState,
    string? EvidenceState,
    string? IdempotencyKey);

internal sealed record EvaluateEnergyConsumptionRequest(
    string MeasurementPointReference,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    Guid? TariffSnapshotId,
    IReadOnlyList<EnergyAllocationRequest>? Allocations,
    string? IdempotencyKey);

internal sealed record EnergyAllocationRequest(
    string TargetType,
    string? TargetReference,
    decimal AllocatedKwh,
    string AllocationMethod,
    string EvidenceSource,
    string ConfidenceStatus);
