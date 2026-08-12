using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Material;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.Material;

public static class MaterialEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMaterialEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/materials").RequireAuthorization();

        group.MapGet("/", ListMaterialsAsync);
        group.MapGet("/workspace-summary", GetWorkspaceSummaryAsync);
        group.MapPost("/", CreateMaterialAsync);
        group.MapGet("/{materialId:guid}", GetMaterialAsync);
        group.MapPut("/{materialId:guid}", UpdateMaterialAsync);
        group.MapGet("/{materialId:guid}/technical-specification", GetTechnicalSpecificationAsync);
        group.MapPut("/{materialId:guid}/technical-specification", UpdateTechnicalSpecificationAsync);
        group.MapGet("/{materialId:guid}/lifecycle-history", ListLifecycleHistoryAsync);
        group.MapPost("/{materialId:guid}/lifecycle-transitions", TransitionLifecycleAsync);

        endpoints.MapMaterialLotEndpoints();

        return endpoints;
    }

    private static async Task<IResult> ListMaterialsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? search,
        [FromQuery] string? lifecycleStatus,
        [FromQuery] string? materialCategory,
        [FromQuery] string? polymerFamily,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IMaterialRepository materialRepository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var materials = await materialRepository.ListAsync(
            context,
            new MaterialListFilters(search, lifecycleStatus, materialCategory, polymerFamily, isActive, page, pageSize),
            cancellationToken);

        return TypedResults.Ok(materials.Select(MaterialApiMapper.MapSummary).ToList());
    }

    private static async Task<IResult> GetWorkspaceSummaryAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialRepository materialRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var summary = await materialRepository.GetWorkspaceSummaryAsync(context, cancellationToken);
        var canManage = await MaterialEndpointHelpers.HasManageAccessAsync(
            facilityId!.Value,
            facilityAccessResolver,
            cancellationToken);

        return TypedResults.Ok(MaterialApiMapper.MapWorkspaceSummary(summary, canManage));
    }

    private static async Task<IResult> GetMaterialAsync(
        Guid materialId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialRepository materialRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var material = await materialRepository.GetByIdAsync(context, materialId, cancellationToken);
        if (material is null)
        {
            return TypedResults.Problem(
                title: "Material not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: MaterialEndpointHelpers.FailureExt(MaterialApiFailureCodes.NotFound));
        }

        return TypedResults.Ok(MaterialApiMapper.MapDetail(material));
    }

    private static async Task<IResult> CreateMaterialAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateMaterialApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialRepository materialRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await materialRepository.CreateAsync(
            context,
            new CreateMaterialCommand(
                body.Code,
                body.Name,
                body.Description,
                body.MaterialCategory,
                body.PolymerFamily,
                body.Manufacturer,
                body.TradeNameOrGrade,
                body.ColorName,
                body.ColorCode,
                body.PhysicalForm,
                body.UnitOfMeasure,
                body.IsActive,
                body.SourceType,
                body.SourceSystem,
                body.SourceReference,
                body.ExternalMaterialCode,
                body.DataRecordedAtUtc,
                body.IdempotencyKey),
            cancellationToken);

        return MaterialEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateMaterialAsync(
        Guid materialId,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateMaterialApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialRepository materialRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await materialRepository.UpdateAsync(
            context,
            new UpdateMaterialCommand(
                materialId,
                body.ExpectedVersion,
                body.Name,
                body.Description,
                body.MaterialCategory,
                body.PolymerFamily,
                body.Manufacturer,
                body.TradeNameOrGrade,
                body.ColorName,
                body.ColorCode,
                body.PhysicalForm,
                body.UnitOfMeasure,
                body.IsActive,
                body.SourceSystem,
                body.SourceReference,
                body.ExternalMaterialCode,
                body.DataRecordedAtUtc),
            cancellationToken);

        return MaterialEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> GetTechnicalSpecificationAsync(
        Guid materialId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialRepository materialRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var material = await materialRepository.GetByIdAsync(context, materialId, cancellationToken);
        if (material is null)
        {
            return TypedResults.Problem(
                title: "Material not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: MaterialEndpointHelpers.FailureExt(MaterialApiFailureCodes.NotFound));
        }

        var spec = await materialRepository.GetTechnicalSpecificationAsync(context, materialId, cancellationToken);
        return spec is null
            ? TypedResults.Ok((object?)null)
            : TypedResults.Ok(MaterialApiMapper.MapTechnicalSpecification(spec));
    }

    private static async Task<IResult> UpdateTechnicalSpecificationAsync(
        Guid materialId,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateMaterialTechnicalSpecificationApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialRepository materialRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await materialRepository.UpdateTechnicalSpecificationAsync(
            context,
            new UpdateMaterialTechnicalSpecificationCommand(
                materialId,
                body.ExpectedVersion,
                body.DensityGPerCm3,
                body.MeltFlowRate,
                body.MeltFlowUnit,
                body.MeltFlowTestTemperatureC,
                body.MeltFlowTestLoadKg,
                body.MoistureLimitPercent,
                body.DryingRequired,
                body.DryingTemperatureMinC,
                body.DryingTemperatureMaxC,
                body.DryingTimeMinHours,
                body.DryingTimeMaxHours,
                body.MeltTemperatureMinC,
                body.MeltTemperatureMaxC,
                body.MoldTemperatureMinC,
                body.MoldTemperatureMaxC,
                body.StorageTemperatureMinC,
                body.StorageTemperatureMaxC,
                body.MaximumRegrindPercent,
                body.TechnicalNotes,
                body.SafetyDataSheetReference,
                body.TechnicalDataSheetReference,
                body.SpecificationSource,
                body.SpecificationSourceReference,
                body.SpecificationRecordedAtUtc),
            cancellationToken);

        return MaterialEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> ListLifecycleHistoryAsync(
        Guid materialId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialRepository materialRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var material = await materialRepository.GetByIdAsync(context, materialId, cancellationToken);
        if (material is null)
        {
            return TypedResults.Problem(
                title: "Material not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: MaterialEndpointHelpers.FailureExt(MaterialApiFailureCodes.NotFound));
        }

        var events = await materialRepository.ListLifecycleHistoryAsync(context, materialId, cancellationToken);
        return TypedResults.Ok(events.Select(MaterialApiMapper.MapLifecycleEvent).ToList());
    }

    private static async Task<IResult> TransitionLifecycleAsync(
        Guid materialId,
        [FromQuery] Guid? facilityId,
        [FromBody] TransitionMaterialLifecycleApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialRepository materialRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await materialRepository.TransitionLifecycleAsync(
            context,
            new TransitionMaterialLifecycleCommand(
                materialId,
                body.ExpectedVersion,
                body.TargetStatus,
                body.Reason,
                body.SourceReference,
                body.IdempotencyKey),
            cancellationToken);

        return MaterialEndpointHelpers.MapWriteResult(result, isCreate: false);
    }
}

internal sealed record CreateMaterialApiRequest(
    string Code,
    string Name,
    string? Description,
    string MaterialCategory,
    string PolymerFamily,
    string? Manufacturer,
    string? TradeNameOrGrade,
    string? ColorName,
    string? ColorCode,
    string PhysicalForm,
    string UnitOfMeasure,
    bool IsActive,
    string SourceType,
    string? SourceSystem,
    string? SourceReference,
    string? ExternalMaterialCode,
    DateTimeOffset? DataRecordedAtUtc,
    string? IdempotencyKey);

internal sealed record UpdateMaterialApiRequest(
    uint ExpectedVersion,
    string Name,
    string? Description,
    string MaterialCategory,
    string PolymerFamily,
    string? Manufacturer,
    string? TradeNameOrGrade,
    string? ColorName,
    string? ColorCode,
    string PhysicalForm,
    string UnitOfMeasure,
    bool IsActive,
    string? SourceSystem,
    string? SourceReference,
    string? ExternalMaterialCode,
    DateTimeOffset? DataRecordedAtUtc);

internal sealed record UpdateMaterialTechnicalSpecificationApiRequest(
    uint ExpectedVersion,
    decimal? DensityGPerCm3,
    decimal? MeltFlowRate,
    string? MeltFlowUnit,
    decimal? MeltFlowTestTemperatureC,
    decimal? MeltFlowTestLoadKg,
    decimal? MoistureLimitPercent,
    bool? DryingRequired,
    decimal? DryingTemperatureMinC,
    decimal? DryingTemperatureMaxC,
    decimal? DryingTimeMinHours,
    decimal? DryingTimeMaxHours,
    decimal? MeltTemperatureMinC,
    decimal? MeltTemperatureMaxC,
    decimal? MoldTemperatureMinC,
    decimal? MoldTemperatureMaxC,
    decimal? StorageTemperatureMinC,
    decimal? StorageTemperatureMaxC,
    decimal? MaximumRegrindPercent,
    string? TechnicalNotes,
    string? SafetyDataSheetReference,
    string? TechnicalDataSheetReference,
    string SpecificationSource,
    string? SpecificationSourceReference,
    DateTimeOffset? SpecificationRecordedAtUtc);

internal sealed record TransitionMaterialLifecycleApiRequest(
    uint ExpectedVersion,
    string TargetStatus,
    string Reason,
    string? SourceReference,
    string IdempotencyKey);

internal static class MaterialApiMapper
{
    internal static object MapSummary(MaterialSummaryReadModel item) => new
    {
        id = item.Id,
        code = item.Code,
        name = item.Name,
        materialCategory = item.MaterialCategory,
        polymerFamily = item.PolymerFamily,
        manufacturer = item.Manufacturer,
        tradeNameOrGrade = item.TradeNameOrGrade,
        colorName = item.ColorName,
        lifecycleStatus = item.LifecycleStatus,
        isActive = item.IsActive,
        hasTechnicalData = item.HasTechnicalData,
        facilityId = item.FacilityId,
        updatedAtUtc = item.UpdatedAtUtc,
        version = item.Version,
    };

    internal static object MapDetail(MaterialDetailReadModel item) => new
    {
        id = item.Id,
        facilityId = item.FacilityId,
        code = item.Code,
        name = item.Name,
        description = item.Description,
        materialCategory = item.MaterialCategory,
        polymerFamily = item.PolymerFamily,
        manufacturer = item.Manufacturer,
        tradeNameOrGrade = item.TradeNameOrGrade,
        colorName = item.ColorName,
        colorCode = item.ColorCode,
        physicalForm = item.PhysicalForm,
        lifecycleStatus = item.LifecycleStatus,
        unitOfMeasure = item.UnitOfMeasure,
        isActive = item.IsActive,
        sourceType = item.SourceType,
        sourceSystem = item.SourceSystem,
        sourceReference = item.SourceReference,
        externalMaterialCode = item.ExternalMaterialCode,
        dataRecordedAtUtc = item.DataRecordedAtUtc,
        technicalSpecification = item.TechnicalSpecification is null
            ? null
            : MapTechnicalSpecification(item.TechnicalSpecification),
        createdAtUtc = item.CreatedAtUtc,
        updatedAtUtc = item.UpdatedAtUtc,
        version = item.Version,
    };

    internal static object MapTechnicalSpecification(MaterialTechnicalSpecificationReadModel item) => new
    {
        densityGPerCm3 = item.DensityGPerCm3,
        meltFlowRate = item.MeltFlowRate,
        meltFlowUnit = item.MeltFlowUnit,
        meltFlowTestTemperatureC = item.MeltFlowTestTemperatureC,
        meltFlowTestLoadKg = item.MeltFlowTestLoadKg,
        moistureLimitPercent = item.MoistureLimitPercent,
        dryingRequired = item.DryingRequired,
        dryingTemperatureMinC = item.DryingTemperatureMinC,
        dryingTemperatureMaxC = item.DryingTemperatureMaxC,
        dryingTimeMinHours = item.DryingTimeMinHours,
        dryingTimeMaxHours = item.DryingTimeMaxHours,
        meltTemperatureMinC = item.MeltTemperatureMinC,
        meltTemperatureMaxC = item.MeltTemperatureMaxC,
        moldTemperatureMinC = item.MoldTemperatureMinC,
        moldTemperatureMaxC = item.MoldTemperatureMaxC,
        storageTemperatureMinC = item.StorageTemperatureMinC,
        storageTemperatureMaxC = item.StorageTemperatureMaxC,
        maximumRegrindPercent = item.MaximumRegrindPercent,
        technicalNotes = item.TechnicalNotes,
        safetyDataSheetReference = item.SafetyDataSheetReference,
        technicalDataSheetReference = item.TechnicalDataSheetReference,
        specificationSource = item.SpecificationSource,
        specificationSourceReference = item.SpecificationSourceReference,
        specificationRecordedAtUtc = item.SpecificationRecordedAtUtc,
        hasTechnicalData = item.HasTechnicalData,
        version = item.Version,
    };

    internal static object MapLifecycleEvent(MaterialLifecycleEventReadModel item) => new
    {
        id = item.Id,
        previousStatus = item.PreviousStatus,
        newStatus = item.NewStatus,
        reason = item.Reason,
        actorDisplayName = item.ActorDisplayName,
        occurredAtUtc = item.OccurredAtUtc,
        sourceReference = item.SourceReference,
        version = item.Version,
    };

    internal static object MapWorkspaceSummary(MaterialWorkspaceSummaryReadModel summary, bool canManage) => new
    {
        totalCount = summary.TotalCount,
        activeCount = summary.ActiveCount,
        blockedCount = summary.BlockedCount,
        obsoleteCount = summary.ObsoleteCount,
        missingTechnicalDataCount = summary.MissingTechnicalDataCount,
        capabilities = new { canRead = true, canManage },
    };
}

public static class MaterialServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSMaterial(this IServiceCollection services)
    {
        services.AddScoped<IMaterialRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableMaterialRepository()
                : new MaterialRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IMaterialLotRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableMaterialLotRepository()
                : new MaterialLotRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        return services;
    }
}
