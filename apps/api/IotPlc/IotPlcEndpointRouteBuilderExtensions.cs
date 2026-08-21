using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.IotPlc;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.IotPlc;

public static class IotPlcEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapIotPlcEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/iot-plc").RequireAuthorization();

        group.MapGet("/workspace", GetWorkspaceAsync);
        group.MapGet("/endpoints", ListEndpointsAsync);
        group.MapGet("/endpoints/{reference}", GetEndpointAsync);
        group.MapGet("/signals", ListSignalsAsync);
        group.MapGet("/signals/{reference}/observations", ListSignalObservationsAsync);
        group.MapGet("/endpoints/{reference}/connection-health", ListConnectionHealthAsync);
        group.MapPost("/endpoints", CreateEndpointAsync);
        group.MapPut("/endpoints/{reference}", UpdateEndpointAsync);
        group.MapPost("/signals", CreateSignalAsync);
        group.MapPut("/signals/{reference}", UpdateSignalAsync);
        group.MapPost("/ingest/observations", IngestObservationAsync);
        group.MapPost("/ingest/connection-health", IngestConnectionHealthAsync);

        var devGroup = endpoints.MapGroup("/api/iot-plc/dev").RequireAuthorization();
        devGroup.MapPost("/ingest/observations", DevIngestObservationAsync);
        devGroup.MapPost("/ingest/connection-health", DevIngestConnectionHealthAsync);

        return endpoints;
    }

    private static async Task<IResult> GetWorkspaceAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IIotPlcRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IotPlcEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var workspace = await repository.GetWorkspaceAsync(context, cancellationToken);
        var canManage = await IotPlcEndpointHelpers.HasManageAccessAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);

        return TypedResults.Ok(new
        {
            activeEndpointCount = workspace.ActiveEndpointCount,
            activeSignalCount = workspace.ActiveSignalCount,
            recentObservationCount = workspace.RecentObservationCount,
            provenanceDisclaimer = workspace.ProvenanceDisclaimer,
            hasDevelopmentPilotData = workspace.HasDevelopmentPilotData,
            capabilities = new { canRead = true, canManage },
        });
    }

    private static async Task<IResult> ListEndpointsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? protocolType,
        [FromQuery] string? endpointStatus,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IIotPlcRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await IotPlcEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ListEndpointsAsync(
            context,
            new IotPlcEndpointListFilters(protocolType, endpointStatus, page, pageSize),
            cancellationToken);

        var canManage = await IotPlcEndpointHelpers.HasManageAccessAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);

        return TypedResults.Ok(new
        {
            items = result.Items.Select(MapEndpoint),
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
            capabilities = new { canRead = true, canManage },
        });
    }

    private static async Task<IResult> GetEndpointAsync(
        string reference,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IIotPlcRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IotPlcEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var detail = await repository.GetEndpointByReferenceAsync(context, reference, cancellationToken);
        if (detail is null)
        {
            return IotPlcEndpointHelpers.MapFailure(
                IotPlcFailureCodes.EndpointNotFound,
                "Endpoint not found.");
        }

        var canManage = await IotPlcEndpointHelpers.HasManageAccessAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);

        return TypedResults.Ok(new
        {
            endpoint = MapEndpoint(detail.Endpoint),
            connectionProfileJson = detail.ConnectionProfileJson,
            signals = detail.Signals.Select(MapSignal),
            capabilities = new { canRead = true, canManage },
        });
    }

    private static async Task<IResult> ListSignalsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? endpointReference,
        [FromQuery] string? mappedAssetType,
        [FromQuery] Guid? mappedCanonicalAssetId,
        [FromQuery] bool? isEnabled,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IIotPlcRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await IotPlcEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ListSignalsAsync(
            context,
            new IotPlcSignalListFilters(endpointReference, mappedAssetType, mappedCanonicalAssetId, isEnabled, page, pageSize),
            cancellationToken);

        return TypedResults.Ok(new
        {
            items = result.Items.Select(MapSignal),
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
        });
    }

    private static async Task<IResult> ListSignalObservationsAsync(
        string reference,
        [FromQuery] Guid? facilityId,
        [FromQuery] DateTimeOffset? observedFromUtc,
        [FromQuery] DateTimeOffset? observedToUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IIotPlcRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await IotPlcEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ListSignalObservationsAsync(
            context,
            reference,
            new IotPlcSignalObservationListFilters(observedFromUtc, observedToUtc, page, pageSize),
            cancellationToken);

        return TypedResults.Ok(new
        {
            items = result.Items.Select(MapObservation),
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
        });
    }

    private static async Task<IResult> ListConnectionHealthAsync(
        string reference,
        [FromQuery] Guid? facilityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IIotPlcRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await IotPlcEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ListConnectionHealthAsync(
            context, reference, page, pageSize, cancellationToken);

        return TypedResults.Ok(new
        {
            items = result.Items.Select(MapHealth),
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
        });
    }

    private static async Task<IResult> CreateEndpointAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateIotPlcEndpointRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IIotPlcRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IotPlcEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.CreateEndpointAsync(
            context,
            new CreateIotPlcEndpointCommand(
                body.EndpointCode,
                body.DisplayName,
                body.ProtocolType,
                body.ConnectionDirection,
                body.EndpointStatus,
                body.ConfigurationProvenance,
                body.ExternalSecretReference,
                body.ConnectionProfileJson),
            cancellationToken);

        return IotPlcEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateEndpointAsync(
        string reference,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateIotPlcEndpointRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IIotPlcRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IotPlcEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.UpdateEndpointAsync(
            context,
            reference,
            new UpdateIotPlcEndpointCommand(
                body.EndpointCode,
                body.DisplayName,
                body.ProtocolType,
                body.ConnectionDirection,
                body.EndpointStatus,
                body.ConfigurationProvenance,
                body.ExternalSecretReference,
                body.ConnectionProfileJson,
                body.ExpectedVersion),
            cancellationToken);

        return IotPlcEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> CreateSignalAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateIotPlcSignalRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IIotPlcRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IotPlcEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.CreateSignalAsync(
            context,
            new CreateIotPlcSignalCommand(
                body.EndpointReference,
                body.SignalCode,
                body.DisplayName,
                body.SourceAddress,
                body.DataType,
                body.EngineeringUnit,
                body.SemanticType,
                body.MappedAssetType,
                body.MappedCanonicalAssetId,
                body.IsEnabled,
                body.SamplingExpectation,
                body.SourceProvenance),
            cancellationToken);

        return IotPlcEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateSignalAsync(
        string reference,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateIotPlcSignalRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IIotPlcRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IotPlcEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.UpdateSignalAsync(
            context,
            reference,
            new UpdateIotPlcSignalCommand(
                body.DisplayName,
                body.SourceAddress,
                body.DataType,
                body.EngineeringUnit,
                body.SemanticType,
                body.MappedAssetType,
                body.MappedCanonicalAssetId,
                body.IsEnabled,
                body.SamplingExpectation,
                body.SourceProvenance,
                body.ExpectedVersion),
            cancellationToken);

        return IotPlcEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> IngestObservationAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] IngestIotPlcObservationRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IIotPlcRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IotPlcEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        return await IngestObservationCoreAsync(context, body, repository, cancellationToken);
    }

    private static async Task<IResult> IngestConnectionHealthAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] IngestIotPlcConnectionHealthRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IIotPlcRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IotPlcEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        return await IngestConnectionHealthCoreAsync(context, body, repository, cancellationToken);
    }

    private static async Task<IResult> DevIngestObservationAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] IngestIotPlcObservationRequest body,
        IWebHostEnvironment environment,
        IFacilityAccessResolver facilityAccessResolver,
        IIotPlcRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return TypedResults.Problem(
                title: "Development ingestion endpoint is not available outside Development environment.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: new Dictionary<string, object?> { ["failureCode"] = "iot-plc.dev-ingest-unavailable" });
        }

        var (context, failResult) = await IotPlcEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        return await IngestObservationCoreAsync(context, body, repository, cancellationToken);
    }

    private static async Task<IResult> DevIngestConnectionHealthAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] IngestIotPlcConnectionHealthRequest body,
        IWebHostEnvironment environment,
        IFacilityAccessResolver facilityAccessResolver,
        IIotPlcRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return TypedResults.Problem(
                title: "Development ingestion endpoint is not available outside Development environment.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: new Dictionary<string, object?> { ["failureCode"] = "iot-plc.dev-ingest-unavailable" });
        }

        var (context, failResult) = await IotPlcEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        return await IngestConnectionHealthCoreAsync(context, body, repository, cancellationToken);
    }

    private static async Task<IResult> IngestObservationCoreAsync(
        IotPlcOperationContext context,
        IngestIotPlcObservationRequest body,
        IIotPlcRepository repository,
        CancellationToken cancellationToken)
    {
        var result = await repository.IngestObservationAsync(
            context,
            new IngestIotPlcObservationCommand(
                body.SignalReference,
                body.ObservedAtUtc,
                body.BooleanValue,
                body.IntegerValue,
                body.DecimalValue,
                body.TextValue,
                body.TimestampValue,
                body.QualityState,
                body.EvidenceState,
                body.SourceSequenceReference,
                body.SourceProvenance,
                body.IdempotencyKey),
            cancellationToken);

        return IotPlcEndpointHelpers.MapWriteResult(result, isCreate: !result.IdempotentReplay);
    }

    private static async Task<IResult> IngestConnectionHealthCoreAsync(
        IotPlcOperationContext context,
        IngestIotPlcConnectionHealthRequest body,
        IIotPlcRepository repository,
        CancellationToken cancellationToken)
    {
        var result = await repository.IngestConnectionHealthAsync(
            context,
            new IngestIotPlcConnectionHealthCommand(
                body.EndpointReference,
                body.ObservedAtUtc,
                body.HealthState,
                body.Source,
                body.EvidenceState,
                body.ReasonCode,
                body.LatencyMs,
                body.LastSuccessfulObservationAtUtc,
                body.IdempotencyKey),
            cancellationToken);

        return IotPlcEndpointHelpers.MapWriteResult(result, isCreate: !result.IdempotentReplay);
    }

    private static object MapEndpoint(IotPlcEndpointReadModel endpoint) => new
    {
        id = endpoint.Id,
        endpointCode = endpoint.EndpointCode,
        displayName = endpoint.DisplayName,
        protocolType = endpoint.ProtocolType,
        connectionDirection = endpoint.ConnectionDirection,
        endpointStatus = endpoint.EndpointStatus,
        configurationProvenance = endpoint.ConfigurationProvenance,
        lastObservedConnectionState = endpoint.LastObservedConnectionState,
        lastObservedAtUtc = endpoint.LastObservedAtUtc,
        version = endpoint.Version,
        signalCount = endpoint.SignalCount,
        isPilotData = endpoint.IsPilotData,
    };

    private static object MapSignal(IotPlcSignalReadModel signal) => new
    {
        id = signal.Id,
        endpointId = signal.EndpointId,
        endpointCode = signal.EndpointCode,
        signalCode = signal.SignalCode,
        displayName = signal.DisplayName,
        sourceAddress = signal.SourceAddress,
        dataType = signal.DataType,
        engineeringUnit = signal.EngineeringUnit,
        semanticType = signal.SemanticType,
        mappedAssetType = signal.MappedAssetType,
        mappedCanonicalAssetId = signal.MappedCanonicalAssetId,
        isEnabled = signal.IsEnabled,
        samplingExpectation = signal.SamplingExpectation,
        sourceProvenance = signal.SourceProvenance,
        version = signal.Version,
        isPilotData = signal.IsPilotData,
    };

    private static object MapObservation(IotPlcSignalObservationReadModel observation) => new
    {
        id = observation.Id,
        signalDefinitionId = observation.SignalDefinitionId,
        signalCode = observation.SignalCode,
        mappedAssetType = observation.MappedAssetType,
        mappedCanonicalAssetId = observation.MappedCanonicalAssetId,
        observedAtUtc = observation.ObservedAtUtc,
        receivedAtUtc = observation.ReceivedAtUtc,
        value = observation.Value,
        qualityState = observation.QualityState,
        evidenceState = observation.EvidenceState,
        sourceSequenceReference = observation.SourceSequenceReference,
        sourceProvenance = observation.SourceProvenance,
    };

    private static object MapHealth(IotPlcConnectionHealthReadModel health) => new
    {
        id = health.Id,
        endpointId = health.EndpointId,
        observedAtUtc = health.ObservedAtUtc,
        receivedAtUtc = health.ReceivedAtUtc,
        healthState = health.HealthState,
        source = health.Source,
        evidenceState = health.EvidenceState,
        reasonCode = health.ReasonCode,
        latencyMs = health.LatencyMs,
        lastSuccessfulObservationAtUtc = health.LastSuccessfulObservationAtUtc,
    };

    public static IServiceCollection AddCrevaFactOSIotPlc(this IServiceCollection services)
    {
        services.AddScoped<IIotPlcRepository>(sp =>
        {
            var dbContext = sp.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableIotPlcRepository()
                : new IotPlcRepository(dbContext, sp.GetRequiredService<TenantDatabaseSession>());
        });

        return services;
    }
}

internal sealed record CreateIotPlcEndpointRequest(
    string EndpointCode,
    string DisplayName,
    string ProtocolType,
    string ConnectionDirection,
    string EndpointStatus,
    string ConfigurationProvenance,
    string? ExternalSecretReference,
    string ConnectionProfileJson);

internal sealed record UpdateIotPlcEndpointRequest(
    string EndpointCode,
    string DisplayName,
    string ProtocolType,
    string ConnectionDirection,
    string EndpointStatus,
    string ConfigurationProvenance,
    string? ExternalSecretReference,
    string ConnectionProfileJson,
    uint ExpectedVersion);

internal sealed record CreateIotPlcSignalRequest(
    string EndpointReference,
    string SignalCode,
    string DisplayName,
    string SourceAddress,
    string DataType,
    string? EngineeringUnit,
    string SemanticType,
    string MappedAssetType,
    Guid? MappedCanonicalAssetId,
    bool IsEnabled,
    string? SamplingExpectation,
    string SourceProvenance);

internal sealed record UpdateIotPlcSignalRequest(
    string DisplayName,
    string SourceAddress,
    string DataType,
    string? EngineeringUnit,
    string SemanticType,
    string MappedAssetType,
    Guid? MappedCanonicalAssetId,
    bool IsEnabled,
    string? SamplingExpectation,
    string SourceProvenance,
    uint ExpectedVersion);

internal sealed record IngestIotPlcObservationRequest(
    string SignalReference,
    DateTimeOffset ObservedAtUtc,
    bool? BooleanValue,
    long? IntegerValue,
    decimal? DecimalValue,
    string? TextValue,
    DateTimeOffset? TimestampValue,
    string QualityState,
    string EvidenceState,
    string? SourceSequenceReference,
    string SourceProvenance,
    string? IdempotencyKey);

internal sealed record IngestIotPlcConnectionHealthRequest(
    string EndpointReference,
    DateTimeOffset ObservedAtUtc,
    string HealthState,
    string Source,
    string EvidenceState,
    string ReasonCode,
    int? LatencyMs,
    DateTimeOffset? LastSuccessfulObservationAtUtc,
    string? IdempotencyKey);

internal sealed class UnavailableIotPlcRepository : IIotPlcRepository
{
    private static IotPlcWriteResult Unavailable() =>
        IotPlcWriteResult.Failure("iot-plc.source-unavailable", "Service unavailable.");

    public Task<IotPlcWorkspaceReadModel> GetWorkspaceAsync(IotPlcOperationContext context, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("IoT PLC repository is unavailable.");

    public Task<IotPlcEndpointListReadModel> ListEndpointsAsync(IotPlcOperationContext context, IotPlcEndpointListFilters filters, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("IoT PLC repository is unavailable.");

    public Task<IotPlcEndpointDetailReadModel?> GetEndpointByReferenceAsync(IotPlcOperationContext context, string reference, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("IoT PLC repository is unavailable.");

    public Task<IotPlcSignalListReadModel> ListSignalsAsync(IotPlcOperationContext context, IotPlcSignalListFilters filters, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("IoT PLC repository is unavailable.");

    public Task<IotPlcSignalObservationListReadModel> ListSignalObservationsAsync(IotPlcOperationContext context, string signalReference, IotPlcSignalObservationListFilters filters, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("IoT PLC repository is unavailable.");

    public Task<IotPlcConnectionHealthListReadModel> ListConnectionHealthAsync(IotPlcOperationContext context, string endpointReference, int page, int pageSize, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("IoT PLC repository is unavailable.");

    public Task<IotPlcWriteResult> CreateEndpointAsync(IotPlcOperationContext context, CreateIotPlcEndpointCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<IotPlcWriteResult> UpdateEndpointAsync(IotPlcOperationContext context, string reference, UpdateIotPlcEndpointCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<IotPlcWriteResult> CreateSignalAsync(IotPlcOperationContext context, CreateIotPlcSignalCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<IotPlcWriteResult> UpdateSignalAsync(IotPlcOperationContext context, string reference, UpdateIotPlcSignalCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<IotPlcWriteResult> IngestObservationAsync(IotPlcOperationContext context, IngestIotPlcObservationCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<IotPlcWriteResult> IngestConnectionHealthAsync(IotPlcOperationContext context, IngestIotPlcConnectionHealthCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}
