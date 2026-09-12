using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.IotPlc;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisFactOS.Api.IotPlc;

public static class IotPlcIntelligenceEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapIotPlcIntelligenceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/iot-plc/intelligence").RequireAuthorization();

        group.MapGet("/workspace", GetWorkspaceAsync);
        group.MapGet("/quality-profiles", ListProfilesAsync);
        group.MapGet("/quality-profiles/{reference}", GetProfileAsync);
        group.MapPost("/quality-profiles", CreateProfileAsync);
        group.MapPut("/quality-profiles/{reference}", UpdateProfileAsync);
        group.MapGet("/connectivity-assessments", ListConnectivityAssessmentsAsync);
        group.MapGet("/connectivity-assessments/{assessmentId:guid}", GetConnectivityAssessmentAsync);
        group.MapGet("/signal-assessments", ListSignalAssessmentsAsync);
        group.MapGet("/signal-assessments/{assessmentId:guid}", GetSignalAssessmentAsync);
        group.MapPost("/connectivity-assessments/evaluate", EvaluateConnectivityAsync);
        group.MapPost("/signal-assessments/evaluate", EvaluateSignalQualityAsync);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisFactOSIotPlcIntelligence(this IServiceCollection services)
    {
        services.AddScoped<IIotPlcIntelligenceRepository>(sp =>
        {
            var dbContext = sp.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableIotPlcIntelligenceRepository()
                : new IotPlcIntelligenceRepository(dbContext, sp.GetRequiredService<TenantDatabaseSession>());
        });

        return services;
    }

    private static async Task<IResult> GetWorkspaceAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IIotPlcIntelligenceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IotPlcEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var workspace = await repository.GetWorkspaceAsync(context, cancellationToken);
        var canManage = await IotPlcEndpointHelpers.HasManageAccessAsync(facilityId!.Value, facilityAccessResolver, cancellationToken);

        return TypedResults.Ok(new
        {
            approvedProfileCount = workspace.ApprovedProfileCount,
            recentConnectivityAssessmentCount = workspace.RecentConnectivityAssessmentCount,
            recentSignalAssessmentCount = workspace.RecentSignalAssessmentCount,
            ruleSetVersion = workspace.RuleSetVersion,
            provenanceDisclaimer = workspace.ProvenanceDisclaimer,
            hasDevelopmentPilotData = workspace.HasDevelopmentPilotData,
            capabilities = new { canRead = true, canManage },
        });
    }

    private static async Task<IResult> ListProfilesAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IIotPlcIntelligenceRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await IotPlcEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ListProfilesAsync(context, status, page, pageSize, cancellationToken);
        return TypedResults.Ok(new
        {
            items = result.Items.Select(MapProfile),
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
        });
    }

    private static async Task<IResult> GetProfileAsync(
        string reference,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IIotPlcIntelligenceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IotPlcEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var profile = await repository.GetProfileByReferenceAsync(context, reference, cancellationToken);
        if (profile is null)
        {
            return IotPlcEndpointHelpers.MapFailure(IotPlcIntelligenceFailureCodes.ProfileNotFound, "Quality profile not found.");
        }

        return TypedResults.Ok(new { profile = MapProfile(profile) });
    }

    private static async Task<IResult> CreateProfileAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateIotPlcQualityProfileRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IIotPlcIntelligenceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IotPlcEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.CreateProfileAsync(context, new CreateIotPlcQualityProfileCommand(
            body.ProfileCode, body.DisplayName, body.ExpectedObservationIntervalSeconds, body.FreshnessToleranceSeconds,
            body.StaleThresholdSeconds, body.MaximumClockSkewToleranceSeconds, body.MinimumExpectedObservationCount,
            body.MinimumGoodQualityRatio, body.MaximumBadQualityRatio, body.MaximumUncertainQualityRatio,
            body.MaximumSourceConflictRatio, body.SequenceGapEvaluationEnabled, body.Status, body.SourceProvenance),
            cancellationToken);

        return MapIntelligenceWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateProfileAsync(
        string reference,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateIotPlcQualityProfileRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IIotPlcIntelligenceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IotPlcEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.UpdateProfileAsync(context, reference, new UpdateIotPlcQualityProfileCommand(
            body.DisplayName, body.ExpectedObservationIntervalSeconds, body.FreshnessToleranceSeconds,
            body.StaleThresholdSeconds, body.MaximumClockSkewToleranceSeconds, body.MinimumExpectedObservationCount,
            body.MinimumGoodQualityRatio, body.MaximumBadQualityRatio, body.MaximumUncertainQualityRatio,
            body.MaximumSourceConflictRatio, body.SequenceGapEvaluationEnabled, body.Status, body.SourceProvenance,
            body.ExpectedVersion), cancellationToken);

        return MapIntelligenceWriteResult(result);
    }

    private static async Task<IResult> ListConnectivityAssessmentsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? endpointId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IIotPlcIntelligenceRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await IotPlcEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var items = await repository.ListConnectivityAssessmentsAsync(context, endpointId, page, pageSize, cancellationToken);
        return TypedResults.Ok(new { items = items.Select(MapConnectivity), page, pageSize });
    }

    private static async Task<IResult> GetConnectivityAssessmentAsync(
        Guid assessmentId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IIotPlcIntelligenceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IotPlcEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var detail = await repository.GetConnectivityAssessmentAsync(context, assessmentId, cancellationToken);
        if (detail is null)
        {
            return IotPlcEndpointHelpers.MapFailure(IotPlcIntelligenceFailureCodes.AssessmentNotFound, "Assessment not found.");
        }

        return TypedResults.Ok(new
        {
            assessment = MapConnectivity(detail.Assessment),
            drivers = detail.Drivers.Select(MapDriver),
            leadingDriver = detail.Drivers.FirstOrDefault(d => d.IsLeadingDriver),
        });
    }

    private static async Task<IResult> ListSignalAssessmentsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? signalId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IIotPlcIntelligenceRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await IotPlcEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var items = await repository.ListSignalAssessmentsAsync(context, signalId, page, pageSize, cancellationToken);
        return TypedResults.Ok(new { items = items.Select(MapSignalAssessment), page, pageSize });
    }

    private static async Task<IResult> GetSignalAssessmentAsync(
        Guid assessmentId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IIotPlcIntelligenceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IotPlcEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var detail = await repository.GetSignalAssessmentAsync(context, assessmentId, cancellationToken);
        if (detail is null)
        {
            return IotPlcEndpointHelpers.MapFailure(IotPlcIntelligenceFailureCodes.AssessmentNotFound, "Assessment not found.");
        }

        return TypedResults.Ok(new
        {
            assessment = MapSignalAssessment(detail.Assessment),
            drivers = detail.Drivers.Select(MapDriver),
            leadingDriver = detail.Drivers.FirstOrDefault(d => d.IsLeadingDriver),
        });
    }

    private static async Task<IResult> EvaluateConnectivityAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] EvaluateIotPlcConnectivityRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IIotPlcIntelligenceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IotPlcEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.EvaluateConnectivityAsync(context, new EvaluateIotPlcConnectivityCommand(
            body.EndpointReference, body.EvaluationWindowStartUtc, body.EvaluationWindowEndUtc, body.EvaluationTimeUtc,
            body.ProfileReference, body.EvidenceMode, body.SourceProvenance, body.IdempotencyKey), cancellationToken);

        return MapIntelligenceWriteResult(result, isCreate: !result.IdempotentReplay);
    }

    private static async Task<IResult> EvaluateSignalQualityAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] EvaluateIotPlcSignalQualityRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IIotPlcIntelligenceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IotPlcEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.EvaluateSignalQualityAsync(context, new EvaluateIotPlcSignalQualityCommand(
            body.SignalReference, body.EvaluationWindowStartUtc, body.EvaluationWindowEndUtc, body.EvaluationTimeUtc,
            body.ProfileReference, body.EvidenceMode, body.SourceProvenance, body.IdempotencyKey), cancellationToken);

        return MapIntelligenceWriteResult(result, isCreate: !result.IdempotentReplay);
    }

    private static IResult MapIntelligenceWriteResult(IotPlcIntelligenceWriteResult result, bool isCreate = false)
    {
        if (!result.Succeeded)
        {
            return result.FailureCode switch
            {
                IotPlcIntelligenceFailureCodes.ConcurrencyConflict or IotPlcIntelligenceFailureCodes.IdempotencyConflict =>
                    TypedResults.Problem(title: result.FailureReason, statusCode: StatusCodes.Status409Conflict,
                        extensions: new Dictionary<string, object?> { ["failureCode"] = result.FailureCode }),
                IotPlcIntelligenceFailureCodes.ProfileNotFound or IotPlcIntelligenceFailureCodes.AssessmentNotFound or IotPlcIntelligenceFailureCodes.TargetNotFound =>
                    TypedResults.Problem(title: result.FailureReason, statusCode: StatusCodes.Status404NotFound,
                        extensions: new Dictionary<string, object?> { ["failureCode"] = result.FailureCode }),
                _ => TypedResults.Problem(title: result.FailureReason, statusCode: StatusCodes.Status400BadRequest,
                    extensions: new Dictionary<string, object?> { ["failureCode"] = result.FailureCode }),
            };
        }

        var dto = new { id = result.EntityId, version = result.Version, idempotentReplay = result.IdempotentReplay };
        if (result.IdempotentReplay) return TypedResults.Ok(dto);
        return isCreate ? TypedResults.Created((string?)null, dto) : TypedResults.Ok(dto);
    }

    private static object MapProfile(IotPlcQualityProfileReadModel p) => new
    {
        id = p.Id, profileCode = p.ProfileCode, displayName = p.DisplayName,
        expectedObservationIntervalSeconds = p.ExpectedObservationIntervalSeconds,
        freshnessToleranceSeconds = p.FreshnessToleranceSeconds, staleThresholdSeconds = p.StaleThresholdSeconds,
        maximumClockSkewToleranceSeconds = p.MaximumClockSkewToleranceSeconds,
        minimumExpectedObservationCount = p.MinimumExpectedObservationCount,
        minimumGoodQualityRatio = p.MinimumGoodQualityRatio, maximumBadQualityRatio = p.MaximumBadQualityRatio,
        maximumUncertainQualityRatio = p.MaximumUncertainQualityRatio, maximumSourceConflictRatio = p.MaximumSourceConflictRatio,
        sequenceGapEvaluationEnabled = p.SequenceGapEvaluationEnabled, status = p.Status,
        sourceProvenance = p.SourceProvenance, version = p.Version, isPilotData = p.IsPilotData,
    };

    private static object MapConnectivity(IotPlcConnectivityAssessmentReadModel a) => new
    {
        id = a.Id, targetType = a.TargetType, targetId = a.TargetId,
        evaluationWindowStartUtc = a.EvaluationWindowStartUtc, evaluationWindowEndUtc = a.EvaluationWindowEndUtc,
        qualityProfileId = a.QualityProfileId, evidenceMode = a.EvidenceMode, outcome = a.Outcome,
        leadingDriverCode = a.LeadingDriverCode, latestHealthState = a.LatestHealthState,
        healthObservationAvailabilityRatio = a.HealthObservationAvailabilityRatio,
        freshnessSeconds = a.FreshnessSeconds, evaluatedAtUtc = a.EvaluatedAtUtc,
        sourceProvenance = a.SourceProvenance, isPilotData = a.IsPilotData,
    };

    private static object MapSignalAssessment(IotPlcSignalQualityAssessmentReadModel a) => new
    {
        id = a.Id, targetType = a.TargetType, targetId = a.TargetId, signalDefinitionId = a.SignalDefinitionId,
        evaluationWindowStartUtc = a.EvaluationWindowStartUtc, evaluationWindowEndUtc = a.EvaluationWindowEndUtc,
        qualityProfileId = a.QualityProfileId, evidenceMode = a.EvidenceMode, outcome = a.Outcome,
        leadingDriverCode = a.LeadingDriverCode, actualObservationCount = a.ActualObservationCount,
        expectedObservationCount = a.ExpectedObservationCount, completenessRatio = a.CompletenessRatio,
        goodRatio = a.GoodRatio, uncertainRatio = a.UncertainRatio, badRatio = a.BadRatio,
        staleRatio = a.StaleRatio, sourceConflictRatio = a.SourceConflictRatio,
        freshnessSeconds = a.FreshnessSeconds, maxClockSkewSeconds = a.MaxClockSkewSeconds,
        evaluatedAtUtc = a.EvaluatedAtUtc, sourceProvenance = a.SourceProvenance, isPilotData = a.IsPilotData,
    };

    private static object MapDriver(IotPlcIntelligenceDriverReadModel d) => new
    {
        id = d.Id, reasonCode = d.ReasonCode, outcomeSeverity = d.OutcomeSeverity,
        thresholdVariance = d.ThresholdVariance, affectedObservationCount = d.AffectedObservationCount,
        explanation = d.Explanation, isLeadingDriver = d.IsLeadingDriver, sortOrder = d.SortOrder,
    };
}

internal sealed record CreateIotPlcQualityProfileRequest(
    string ProfileCode, string DisplayName, int ExpectedObservationIntervalSeconds, int FreshnessToleranceSeconds,
    int StaleThresholdSeconds, int MaximumClockSkewToleranceSeconds, int MinimumExpectedObservationCount,
    decimal MinimumGoodQualityRatio, decimal MaximumBadQualityRatio, decimal MaximumUncertainQualityRatio,
    decimal MaximumSourceConflictRatio, bool SequenceGapEvaluationEnabled, string Status, string SourceProvenance);

internal sealed record UpdateIotPlcQualityProfileRequest(
    string DisplayName, int ExpectedObservationIntervalSeconds, int FreshnessToleranceSeconds,
    int StaleThresholdSeconds, int MaximumClockSkewToleranceSeconds, int MinimumExpectedObservationCount,
    decimal MinimumGoodQualityRatio, decimal MaximumBadQualityRatio, decimal MaximumUncertainQualityRatio,
    decimal MaximumSourceConflictRatio, bool SequenceGapEvaluationEnabled, string Status, string SourceProvenance,
    uint ExpectedVersion);

internal sealed record EvaluateIotPlcConnectivityRequest(
    string EndpointReference, DateTimeOffset EvaluationWindowStartUtc, DateTimeOffset EvaluationWindowEndUtc,
    DateTimeOffset EvaluationTimeUtc, string? ProfileReference, string EvidenceMode, string SourceProvenance,
    string? IdempotencyKey);

internal sealed record EvaluateIotPlcSignalQualityRequest(
    string SignalReference, DateTimeOffset EvaluationWindowStartUtc, DateTimeOffset EvaluationWindowEndUtc,
    DateTimeOffset EvaluationTimeUtc, string? ProfileReference, string EvidenceMode, string SourceProvenance,
    string? IdempotencyKey);

internal sealed class UnavailableIotPlcIntelligenceRepository : IIotPlcIntelligenceRepository
{
    public Task<IotPlcIntelligenceWorkspaceReadModel> GetWorkspaceAsync(IotPlcOperationContext context, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("IoT PLC intelligence repository is unavailable.");
    public Task<IotPlcQualityProfileListReadModel> ListProfilesAsync(IotPlcOperationContext context, string? status, int page, int pageSize, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("IoT PLC intelligence repository is unavailable.");
    public Task<IotPlcQualityProfileReadModel?> GetProfileByReferenceAsync(IotPlcOperationContext context, string reference, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("IoT PLC intelligence repository is unavailable.");
    public Task<IotPlcIntelligenceWriteResult> CreateProfileAsync(IotPlcOperationContext context, CreateIotPlcQualityProfileCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(IotPlcIntelligenceWriteResult.Failure("iot-plc.intelligence.source-unavailable", "Service unavailable."));
    public Task<IotPlcIntelligenceWriteResult> UpdateProfileAsync(IotPlcOperationContext context, string reference, UpdateIotPlcQualityProfileCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(IotPlcIntelligenceWriteResult.Failure("iot-plc.intelligence.source-unavailable", "Service unavailable."));
    public Task<IotPlcAssessmentDetailReadModel<IotPlcConnectivityAssessmentReadModel>?> GetConnectivityAssessmentAsync(IotPlcOperationContext context, Guid assessmentId, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("IoT PLC intelligence repository is unavailable.");
    public Task<IReadOnlyList<IotPlcConnectivityAssessmentReadModel>> ListConnectivityAssessmentsAsync(IotPlcOperationContext context, Guid? endpointId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("IoT PLC intelligence repository is unavailable.");
    public Task<IotPlcAssessmentDetailReadModel<IotPlcSignalQualityAssessmentReadModel>?> GetSignalAssessmentAsync(IotPlcOperationContext context, Guid assessmentId, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("IoT PLC intelligence repository is unavailable.");
    public Task<IReadOnlyList<IotPlcSignalQualityAssessmentReadModel>> ListSignalAssessmentsAsync(IotPlcOperationContext context, Guid? signalId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("IoT PLC intelligence repository is unavailable.");
    public Task<IotPlcIntelligenceWriteResult> EvaluateConnectivityAsync(IotPlcOperationContext context, EvaluateIotPlcConnectivityCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(IotPlcIntelligenceWriteResult.Failure("iot-plc.intelligence.source-unavailable", "Service unavailable."));
    public Task<IotPlcIntelligenceWriteResult> EvaluateSignalQualityAsync(IotPlcOperationContext context, EvaluateIotPlcSignalQualityCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(IotPlcIntelligenceWriteResult.Failure("iot-plc.intelligence.source-unavailable", "Service unavailable."));
}
