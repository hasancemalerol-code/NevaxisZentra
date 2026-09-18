using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.Quality;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisZentra.Api.Quality;

public static class QualityEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapQualityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/quality").RequireAuthorization();

        group.MapGet("/control-plans", ListControlPlansAsync);
        group.MapPost("/control-plans", CreateControlPlanAsync);
        group.MapGet("/control-plans/{planId:guid}", GetControlPlanAsync);
        group.MapPut("/control-plans/{planId:guid}", UpdateControlPlanAsync);

        group.MapGet("/inspections", ListInspectionsAsync);
        group.MapPost("/inspections", CreateInspectionAsync);
        group.MapGet("/inspections/{inspectionId:guid}", GetInspectionAsync);
        group.MapPost("/inspections/{inspectionId:guid}/start", StartInspectionAsync);
        group.MapPost("/inspections/{inspectionId:guid}/results", RecordInspectionResultAsync);
        group.MapPost("/inspections/{inspectionId:guid}/complete", CompleteInspectionAsync);
        group.MapPost("/inspections/{inspectionId:guid}/release", ReleaseInspectionAsync);
        group.MapPost("/inspections/{inspectionId:guid}/reject", RejectInspectionAsync);

        group.MapGet("/workspace-summary", GetWorkspaceSummaryAsync);

        endpoints.MapQualityNcrEndpoints();
        endpoints.MapQualityReportingEndpoints();

        return endpoints;
    }

    // ─────────────────────────── Control Plans ───────────────────────────

    private static async Task<IResult> ListControlPlansAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityControlPlanRepository planRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var plans = await planRepository.ListAsync(context, cancellationToken);
        return TypedResults.Ok(plans.Select(QualityApiMapper.MapPlanSummary).ToList());
    }

    private static async Task<IResult> GetControlPlanAsync(
        Guid planId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityControlPlanRepository planRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var plan = await planRepository.GetByIdAsync(context, planId, cancellationToken);
        if (plan is null)
        {
            return TypedResults.Problem(
                title: "Quality control plan not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: QualityEndpointHelpers.FailureExt(QualityApiFailureCodes.NotFound));
        }

        return TypedResults.Ok(QualityApiMapper.MapPlanDetail(plan));
    }

    private static async Task<IResult> CreateControlPlanAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateControlPlanApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityControlPlanRepository planRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            QualityPermissionKeys.Manage,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await planRepository.CreateAsync(
            context,
            new CreateQualityControlPlanCommand(
                body.Code,
                body.Name,
                body.Description,
                body.ProductReference,
                body.ProductGroup,
                body.OperationReference,
                body.Status,
                body.Revision,
                MapDefinitionInputs(body.InspectionDefinitions)),
            cancellationToken);

        return QualityEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateControlPlanAsync(
        Guid planId,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateControlPlanApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityControlPlanRepository planRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            QualityPermissionKeys.Manage,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await planRepository.UpdateAsync(
            context,
            new UpdateQualityControlPlanCommand(
                planId,
                body.ExpectedVersion,
                body.Code,
                body.Name,
                body.Description,
                body.ProductReference,
                body.ProductGroup,
                body.OperationReference,
                body.Status,
                body.Revision,
                MapDefinitionInputs(body.InspectionDefinitions)),
            cancellationToken);

        return QualityEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    // ─────────────────────────── Inspections ───────────────────────────

    private static async Task<IResult> ListInspectionsAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityInspectionRepository inspectionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var inspections = await inspectionRepository.ListAsync(context, cancellationToken);
        return TypedResults.Ok(inspections.Select(QualityApiMapper.MapInspectionSummary).ToList());
    }

    private static async Task<IResult> GetInspectionAsync(
        Guid inspectionId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityInspectionRepository inspectionRepository,
        IQualityNonconformanceRepository ncrRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var inspection = await inspectionRepository.GetByIdAsync(context, inspectionId, cancellationToken);
        if (inspection is null)
        {
            return TypedResults.Problem(
                title: "Quality inspection not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: QualityEndpointHelpers.FailureExt(QualityApiFailureCodes.NotFound));
        }

        var linked = new List<LinkedNonconformanceReadModel>();
        foreach (var result in inspection.Results.Where(r => r.Id != Guid.Empty))
        {
            var ncr = await ncrRepository.FindByInspectionResultAsync(context, result.Id, cancellationToken);
            if (ncr is not null)
            {
                linked.Add(ncr);
            }
        }

        var enriched = inspection with { LinkedNonconformances = linked };
        return TypedResults.Ok(QualityApiMapper.MapInspectionDetail(enriched));
    }

    private static async Task<IResult> CreateInspectionAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateInspectionApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityInspectionRepository inspectionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            QualityPermissionKeys.Inspect,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await inspectionRepository.CreateAsync(
            context,
            new CreateQualityInspectionCommand(
                body.ProductionOrderId,
                body.QualityControlPlanId,
                body.Notes),
            cancellationToken);

        return QualityEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> StartInspectionAsync(
        Guid inspectionId,
        [FromQuery] Guid? facilityId,
        [FromBody] InspectionLifecycleApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityInspectionRepository inspectionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            QualityPermissionKeys.Inspect,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await inspectionRepository.StartAsync(
            context,
            new QualityInspectionLifecycleCommand(inspectionId, body.ExpectedVersion, body.Note),
            cancellationToken);

        return QualityEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> RecordInspectionResultAsync(
        Guid inspectionId,
        [FromQuery] Guid? facilityId,
        [FromBody] RecordInspectionResultApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityInspectionRepository inspectionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            QualityPermissionKeys.Inspect,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        if (body.ExpectedVersion == 0)
        {
            return TypedResults.Problem(
                title: "ExpectedVersion is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: QualityEndpointHelpers.FailureExt(QualityApiFailureCodes.InvalidScope));
        }

        var result = await inspectionRepository.RecordResultAsync(
            context,
            new RecordQualityInspectionResultCommand(
                inspectionId,
                body.ExpectedVersion,
                body.InspectionDefinitionId,
                body.MeasuredNumericValue,
                body.MeasuredTextValue,
                body.BooleanResult,
                body.Comment),
            cancellationToken);

        return QualityEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> CompleteInspectionAsync(
        Guid inspectionId,
        [FromQuery] Guid? facilityId,
        [FromBody] InspectionLifecycleApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityInspectionRepository inspectionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            QualityPermissionKeys.Inspect,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await inspectionRepository.CompleteAsync(
            context,
            new QualityInspectionLifecycleCommand(inspectionId, body.ExpectedVersion, body.Note),
            cancellationToken);

        return QualityEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> ReleaseInspectionAsync(
        Guid inspectionId,
        [FromQuery] Guid? facilityId,
        [FromBody] InspectionLifecycleApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityInspectionRepository inspectionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            QualityPermissionKeys.Release,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await inspectionRepository.ReleaseAsync(
            context,
            new QualityInspectionLifecycleCommand(inspectionId, body.ExpectedVersion, body.Note),
            cancellationToken);

        return QualityEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> RejectInspectionAsync(
        Guid inspectionId,
        [FromQuery] Guid? facilityId,
        [FromBody] InspectionLifecycleApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityInspectionRepository inspectionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            QualityPermissionKeys.Release,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await inspectionRepository.RejectAsync(
            context,
            new QualityInspectionLifecycleCommand(inspectionId, body.ExpectedVersion, body.Note),
            cancellationToken);

        return QualityEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    // ─────────────────────────── Workspace ───────────────────────────

    private static async Task<IResult> GetWorkspaceSummaryAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityInspectionRepository inspectionRepository,
        IQualityNonconformanceRepository ncrRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var summary = await inspectionRepository.GetWorkspaceSummaryAsync(context, cancellationToken);
        var extension = await ncrRepository.GetWorkspaceExtensionAsync(context, cancellationToken);
        return TypedResults.Ok(QualityApiMapper.MapWorkspaceSummary(summary, extension));
    }

    // ─────────────────────────── Helpers ───────────────────────────

    private static List<QualityInspectionDefinitionInput> MapDefinitionInputs(
        IReadOnlyList<InspectionDefinitionApiInput>? definitions) =>
        (definitions ?? [])
            .Select(d => new QualityInspectionDefinitionInput(
                d.Code,
                d.Name,
                d.Description,
                d.InspectionType,
                d.Sequence,
                d.Unit,
                d.NominalValue,
                d.LowerLimit,
                d.UpperLimit,
                d.Required,
                d.Critical,
                d.Instruction))
            .ToList();
}

internal static class QualityApiFailureCodes
{
    public const string InvalidRequest = "quality.invalid-request";
    public const string InvalidScope = "quality.invalid-scope";
    public const string NotFound = "quality.not-found";
    public const string ProductionOrderNotFound = "quality.production-order-not-found";
    public const string ConcurrencyConflict = "quality.concurrency-conflict";
    public const string InvalidTransition = "quality.invalid-transition";
    public const string PlanNotActive = "quality.plan-not-active";
    public const string ReleaseNotAllowed = "quality.release-not-allowed";
    public const string ValidationFailed = "quality.validation-failed";
    public const string SourceUnavailable = "quality.source-unavailable";
    public const string Unauthenticated = "quality.unauthenticated";
    public const string MembershipMissing = "quality.membership-missing";
    public const string FacilityAccessDenied = "quality.facility-access-denied";
    public const string PermissionDenied = "quality.permission-denied";
    public const string UnexpectedError = "quality.unexpected-error";
    public const string BlockingQualityHold = "quality.blocking-quality-hold";
    public const string DuplicateNonconformance = "quality.duplicate-nonconformance";
    public const string CloseNotAllowed = "quality.close-not-allowed";
}

internal static class QualityApiMapper
{
    public static object MapPlanSummary(QualityControlPlanSummaryReadModel model) => new
    {
        id = model.Id,
        code = model.Code,
        name = model.Name,
        status = model.Status,
        revision = model.Revision,
        productReference = model.ProductReference,
        operationReference = model.OperationReference,
        definitionCount = model.DefinitionCount,
        version = model.Version,
        updatedAtUtc = model.UpdatedAtUtc,
    };

    public static object MapPlanDetail(QualityControlPlanDetailReadModel model) => new
    {
        id = model.Id,
        code = model.Code,
        name = model.Name,
        description = model.Description,
        status = model.Status,
        revision = model.Revision,
        productReference = model.ProductReference,
        productGroup = model.ProductGroup,
        operationReference = model.OperationReference,
        effectiveFromUtc = model.EffectiveFromUtc,
        effectiveToUtc = model.EffectiveToUtc,
        version = model.Version,
        createdAtUtc = model.CreatedAtUtc,
        updatedAtUtc = model.UpdatedAtUtc,
        inspectionDefinitions = model.InspectionDefinitions.Select(d => new
        {
            id = d.Id,
            code = d.Code,
            name = d.Name,
            description = d.Description,
            inspectionType = d.InspectionType,
            sequence = d.Sequence,
            unit = d.Unit,
            nominalValue = d.NominalValue,
            lowerLimit = d.LowerLimit,
            upperLimit = d.UpperLimit,
            required = d.Required,
            critical = d.Critical,
            instruction = d.Instruction,
            version = d.Version,
        }).ToList(),
    };

    public static object MapInspectionSummary(QualityInspectionSummaryReadModel model) => new
    {
        id = model.Id,
        inspectionNumber = model.InspectionNumber,
        status = model.Status,
        overallResult = model.OverallResult,
        releaseStatus = model.ReleaseStatus,
        productionOrderId = model.ProductionOrderId,
        productionOrderReference = model.ProductionOrderReference,
        productReference = model.ProductReference,
        qualityControlPlanId = model.QualityControlPlanId,
        planCode = model.PlanCode,
        inspectorSubjectId = model.InspectorSubjectId,
        startedAtUtc = model.StartedAtUtc,
        completedAtUtc = model.CompletedAtUtc,
        version = model.Version,
        updatedAtUtc = model.UpdatedAtUtc,
    };

    public static object MapInspectionDetail(QualityInspectionDetailReadModel model) => new
    {
        id = model.Id,
        inspectionNumber = model.InspectionNumber,
        status = model.Status,
        overallResult = model.OverallResult,
        releaseStatus = model.ReleaseStatus,
        productionOrderId = model.ProductionOrderId,
        productionContext = new
        {
            productionOrderId = model.ProductionContext.ProductionOrderId,
            referenceCode = model.ProductionContext.ReferenceCode,
            productReference = model.ProductionContext.ProductReference,
            status = model.ProductionContext.Status,
            plannedQuantity = model.ProductionContext.PlannedQuantity,
            goodQuantity = model.ProductionContext.GoodQuantity,
            scrapQuantity = model.ProductionContext.ScrapQuantity,
            available = model.ProductionContext.Available,
        },
        qualityControlPlanId = model.QualityControlPlanId,
        planCode = model.PlanCode,
        planName = model.PlanName,
        planRevision = model.PlanRevision,
        inspectorSubjectId = model.InspectorSubjectId,
        notes = model.Notes,
        releaseNote = model.ReleaseNote,
        startedAtUtc = model.StartedAtUtc,
        completedAtUtc = model.CompletedAtUtc,
        releasedAtUtc = model.ReleasedAtUtc,
        version = model.Version,
        results = model.Results.Select(r => new
        {
            id = r.Id,
            inspectionDefinitionId = r.InspectionDefinitionId,
            definitionCode = r.DefinitionCode,
            definitionName = r.DefinitionName,
            inspectionType = r.InspectionType,
            unit = r.Unit,
            nominalValue = r.NominalValue,
            lowerLimit = r.LowerLimit,
            upperLimit = r.UpperLimit,
            required = r.Required,
            critical = r.Critical,
            measuredNumericValue = r.MeasuredNumericValue,
            measuredTextValue = r.MeasuredTextValue,
            booleanResult = r.BooleanResult,
            result = r.Result,
            comment = r.Comment,
            recordedAtUtc = r.RecordedAtUtc,
            recordedBySubjectId = r.RecordedBySubjectId,
        }).ToList(),
        linkedNonconformances = model.LinkedNonconformances.Select(n => new
        {
            id = n.Id,
            ncrNumber = n.NcrNumber,
            status = n.Status,
        }).ToList(),
    };

    public static object MapWorkspaceSummary(
        QualityWorkspaceSummaryReadModel model,
        QualityNcrWorkspaceExtensionReadModel extension) => new
    {
        openInspectionCount = model.OpenInspectionCount,
        pendingReviewCount = model.PendingReviewCount,
        failedInspectionCount = model.FailedInspectionCount,
        releasedInspectionCount = model.ReleasedInspectionCount,
        openNcrCount = extension.OpenNcrCount,
        activeHoldCount = extension.ActiveHoldCount,
        actionPendingCount = extension.ActionPendingCount,
    };
}

internal sealed record CreateControlPlanApiRequest(
    string Code,
    string Name,
    string? Description,
    string? ProductReference,
    string? ProductGroup,
    string? OperationReference,
    string Status,
    string Revision,
    IReadOnlyList<InspectionDefinitionApiInput> InspectionDefinitions);

internal sealed record UpdateControlPlanApiRequest(
    uint ExpectedVersion,
    string Code,
    string Name,
    string? Description,
    string? ProductReference,
    string? ProductGroup,
    string? OperationReference,
    string Status,
    string Revision,
    IReadOnlyList<InspectionDefinitionApiInput> InspectionDefinitions);

internal sealed record InspectionDefinitionApiInput(
    string Code,
    string Name,
    string? Description,
    string InspectionType,
    int Sequence,
    string? Unit,
    decimal? NominalValue,
    decimal? LowerLimit,
    decimal? UpperLimit,
    bool Required,
    bool Critical,
    string? Instruction);

internal sealed record CreateInspectionApiRequest(
    Guid ProductionOrderId,
    Guid QualityControlPlanId,
    string? Notes);

internal sealed record InspectionLifecycleApiRequest(
    uint ExpectedVersion,
    string? Note);

internal sealed class RecordInspectionResultApiRequest
{
    public uint ExpectedVersion { get; set; }
    public Guid InspectionDefinitionId { get; set; }
    public decimal? MeasuredNumericValue { get; set; }
    public string? MeasuredTextValue { get; set; }
    public bool? BooleanResult { get; set; }
    public string? Comment { get; set; }
}

internal sealed record QualityWriteSuccessDto(Guid Id, uint Version);

public static class QualityServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraQuality(this IServiceCollection services)
    {
        services.AddScoped<IProductionOrderContextReader>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisZentraDbContext>();
            return dbContext is null
                ? new UnavailableProductionOrderContextReader()
                : new ProductionOrderContextReader(dbContext);
        });

        services.AddScoped<IQualityControlPlanRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisZentraDbContext>();
            return dbContext is null
                ? new UnavailableQualityControlPlanRepository()
                : new QualityControlPlanRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IQualityReleaseGuard>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisZentraDbContext>();
            return dbContext is null
                ? new UnavailableQualityReleaseGuard()
                : new QualityReleaseGuard(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IProductionQualitySignalReader>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisZentraDbContext>();
            return dbContext is null
                ? new UnavailableProductionQualitySignalReader()
                : new ProductionQualitySignalReader(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IQualityNonconformanceRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisZentraDbContext>();
            return dbContext is null
                ? new UnavailableQualityNonconformanceRepository()
                : new QualityNonconformanceRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                    serviceProvider.GetRequiredService<IProductionOrderContextReader>());
        });

        services.AddScoped<IQualityInspectionRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisZentraDbContext>();
            return dbContext is null
                ? new UnavailableQualityInspectionRepository()
                : new QualityInspectionRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                    serviceProvider.GetRequiredService<IProductionOrderContextReader>(),
                    serviceProvider.GetRequiredService<IQualityReleaseGuard>());
        });

        services.AddScoped<IQualityReportingRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisZentraDbContext>();
            return dbContext is null
                ? new UnavailableQualityReportingRepository()
                : new QualityReportingRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IQualityTraceabilityRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisZentraDbContext>();
            return dbContext is null
                ? new UnavailableQualityTraceabilityRepository()
                : new QualityTraceabilityRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                    serviceProvider.GetRequiredService<IQualityReportingRepository>());
        });

        services.AddScoped<IQualityReleaseIntelligenceReader>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisZentraDbContext>();
            return dbContext is null
                ? new UnavailableQualityReleaseIntelligenceReader()
                : new QualityReleaseIntelligenceReader(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        return services;
    }
}
