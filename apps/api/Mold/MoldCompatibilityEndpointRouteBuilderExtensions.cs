using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Mold;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.Mold;

public static class MoldCompatibilityEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMoldCompatibilityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/molds").RequireAuthorization();

        group.MapGet("/machine-compatibility-profiles", ListMachineProfilesAsync);
        group.MapGet("/machine-compatibility-profiles/{machineAssetId:guid}", GetMachineProfileAsync);
        group.MapPut("/machine-compatibility-profiles/{machineAssetId:guid}", UpsertMachineProfileAsync);

        group.MapGet("/{moldId:guid}/compatibility-requirement", GetRequirementAsync);
        group.MapPut("/{moldId:guid}/compatibility-requirement", UpsertRequirementAsync);

        group.MapPost("/{moldId:guid}/compatibility-evaluations/{machineAssetId:guid}", EvaluateAsync);
        group.MapPost("/{moldId:guid}/compatibility-evaluations", EvaluateAllAsync);

        group.MapGet("/{moldId:guid}/compatibility-overview", GetOverviewAsync);
        group.MapGet("/{moldId:guid}/compatibility-assessments/{assessmentId:guid}", GetAssessmentAsync);

        return endpoints;
    }

    private static async Task<IResult> GetRequirementAsync(
        Guid moldId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldCompatibilityRepository compatibilityRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var requirement = await compatibilityRepository.GetRequirementAsync(context, moldId, cancellationToken);
        return requirement is null
            ? MoldEndpointHelpers.MapMoldFailure(MoldFailureCodes.NotFound, "Compatibility requirement not found.")
            : TypedResults.Ok(MoldCompatibilityApiMapper.MapRequirement(requirement));
    }

    private static async Task<IResult> UpsertRequirementAsync(
        Guid moldId,
        [FromBody] UpsertCompatibilityRequirementRequestDto body,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldCompatibilityRepository compatibilityRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var command = new UpsertMoldCompatibilityRequirementCommand(
            moldId,
            body.RequiredClampForceTon,
            body.MoldWidthMm,
            body.MoldHeightMm,
            body.MoldDepthMm,
            body.RequiredTieBarWidthMm,
            body.RequiredTieBarHeightMm,
            body.MinimumShotCapacityGrams,
            body.RequiredInjectionUnit,
            body.SupportedMaterialGroup,
            body.NominalCycleTimeSeconds,
            body.EffectiveCavityCount,
            body.SourceType,
            body.SourceReference,
            body.DataRecordedAtUtc,
            body.ExpectedVersion);

        var result = await compatibilityRepository.UpsertRequirementAsync(context, command, cancellationToken);
        return MoldEndpointHelpers.MapWriteResult(result, isCreate: body.ExpectedVersion is null);
    }

    private static async Task<IResult> ListMachineProfilesAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldCompatibilityRepository compatibilityRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var profiles = await compatibilityRepository.ListMachineProfilesAsync(context, cancellationToken);
        return TypedResults.Ok(profiles.Select(MoldCompatibilityApiMapper.MapMachineProfile).ToList());
    }

    private static async Task<IResult> GetMachineProfileAsync(
        Guid machineAssetId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldCompatibilityRepository compatibilityRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var profile = await compatibilityRepository.GetMachineProfileAsync(context, machineAssetId, cancellationToken);
        return profile is null
            ? MoldEndpointHelpers.MapMoldFailure(MoldFailureCodes.NotFound, "Machine compatibility profile not found.")
            : TypedResults.Ok(MoldCompatibilityApiMapper.MapMachineProfile(profile));
    }

    private static async Task<IResult> UpsertMachineProfileAsync(
        Guid machineAssetId,
        [FromBody] UpsertMachineCompatibilityProfileRequestDto body,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldCompatibilityRepository compatibilityRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var command = new UpsertMachineCompatibilityCapabilityCommand(
            machineAssetId,
            body.MachineCode,
            body.MachineDisplayName,
            body.ClampForceTon,
            body.MaximumMoldWidthMm,
            body.MaximumMoldHeightMm,
            body.MinimumMoldDepthMm,
            body.MaximumMoldDepthMm,
            body.TieBarWidthMm,
            body.TieBarHeightMm,
            body.MaximumShotCapacityGrams,
            body.InjectionUnit,
            body.SupportedMaterialGroups,
            body.SourceType,
            body.SourceReference,
            body.DataRecordedAtUtc,
            body.ExpectedVersion);

        var result = await compatibilityRepository.UpsertMachineProfileAsync(context, command, cancellationToken);
        return MoldEndpointHelpers.MapWriteResult(result, isCreate: body.ExpectedVersion is null);
    }

    private static async Task<IResult> EvaluateAsync(
        Guid moldId,
        Guid machineAssetId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldCompatibilityRepository compatibilityRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await compatibilityRepository.EvaluateAsync(
            context,
            new EvaluateMoldMachineCompatibilityCommand(moldId, machineAssetId),
            cancellationToken);

        return MapEvaluationResult(result);
    }

    private static async Task<IResult> EvaluateAllAsync(
        Guid moldId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldCompatibilityRepository compatibilityRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var overview = await compatibilityRepository.GetOverviewAsync(context, moldId, cancellationToken);
        if (overview is null)
        {
            return MoldEndpointHelpers.MapMoldFailure(MoldFailureCodes.NotFound, "Mold not found.");
        }

        var assessments = await compatibilityRepository.EvaluateAllAsync(
            context,
            new EvaluateAllMoldMachineCompatibilityCommand(moldId),
            cancellationToken);

        return TypedResults.Ok(assessments.Select(MoldCompatibilityApiMapper.MapAssessment).ToList());
    }

    private static async Task<IResult> GetOverviewAsync(
        Guid moldId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldCompatibilityRepository compatibilityRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var overview = await compatibilityRepository.GetOverviewAsync(context, moldId, cancellationToken);
        return overview is null
            ? MoldEndpointHelpers.MapMoldFailure(MoldFailureCodes.NotFound, "Mold not found.")
            : TypedResults.Ok(MoldCompatibilityApiMapper.MapOverview(overview));
    }

    private static async Task<IResult> GetAssessmentAsync(
        Guid moldId,
        Guid assessmentId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldCompatibilityRepository compatibilityRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var assessment = await compatibilityRepository.GetAssessmentAsync(
            context, moldId, assessmentId, cancellationToken);

        return assessment is null
            ? MoldEndpointHelpers.MapMoldFailure(MoldFailureCodes.NotFound, "Compatibility assessment not found.")
            : TypedResults.Ok(MoldCompatibilityApiMapper.MapAssessment(assessment));
    }

    private static IResult MapEvaluationResult(MoldCompatibilityEvaluationResult result)
    {
        if (!result.Succeeded)
        {
            return MoldEndpointHelpers.MapMoldFailure(result.FailureCode!, result.FailureReason);
        }

        var dto = MoldCompatibilityApiMapper.MapAssessment(result.Assessment!);
        return result.IdempotentReplay
            ? TypedResults.Ok(dto)
            : TypedResults.Created((string?)null, dto);
    }
}

internal static class MoldCompatibilityApiMapper
{
    internal static object MapRequirement(MoldCompatibilityRequirementReadModel item) => new
    {
        id = item.Id,
        moldId = item.MoldId,
        requiredClampForceTon = item.RequiredClampForceTon,
        moldWidthMm = item.MoldWidthMm,
        moldHeightMm = item.MoldHeightMm,
        moldDepthMm = item.MoldDepthMm,
        requiredTieBarWidthMm = item.RequiredTieBarWidthMm,
        requiredTieBarHeightMm = item.RequiredTieBarHeightMm,
        minimumShotCapacityGrams = item.MinimumShotCapacityGrams,
        requiredInjectionUnit = item.RequiredInjectionUnit,
        supportedMaterialGroup = item.SupportedMaterialGroup,
        nominalCycleTimeSeconds = item.NominalCycleTimeSeconds,
        effectiveCavityCount = item.EffectiveCavityCount,
        sourceType = item.SourceType,
        sourceReference = item.SourceReference,
        dataRecordedAtUtc = item.DataRecordedAtUtc,
        updatedAtUtc = item.UpdatedAtUtc,
        version = item.Version,
    };

    internal static object MapMachineProfile(MachineCompatibilityCapabilityReadModel item) => new
    {
        id = item.Id,
        machineAssetId = item.MachineAssetId,
        machineCode = item.MachineCode,
        machineDisplayName = item.MachineDisplayName,
        clampForceTon = item.ClampForceTon,
        maximumMoldWidthMm = item.MaximumMoldWidthMm,
        maximumMoldHeightMm = item.MaximumMoldHeightMm,
        minimumMoldDepthMm = item.MinimumMoldDepthMm,
        maximumMoldDepthMm = item.MaximumMoldDepthMm,
        tieBarWidthMm = item.TieBarWidthMm,
        tieBarHeightMm = item.TieBarHeightMm,
        maximumShotCapacityGrams = item.MaximumShotCapacityGrams,
        injectionUnit = item.InjectionUnit,
        supportedMaterialGroups = item.SupportedMaterialGroups,
        sourceType = item.SourceType,
        sourceReference = item.SourceReference,
        dataRecordedAtUtc = item.DataRecordedAtUtc,
        updatedAtUtc = item.UpdatedAtUtc,
        version = item.Version,
    };

    internal static object MapAssessment(MoldCompatibilityAssessmentReadModel item) => new
    {
        id = item.Id,
        moldId = item.MoldId,
        machineAssetId = item.MachineAssetId,
        machineCode = item.MachineCode,
        machineDisplayName = item.MachineDisplayName,
        status = item.Status,
        ruleSetVersion = item.RuleSetVersion,
        requirementVersion = item.RequirementVersion,
        machineProfileVersion = item.MachineProfileVersion,
        evaluatedAtUtc = item.EvaluatedAtUtc,
        reasons = item.Reasons.Select(MapRuleResult).ToList(),
        evidence = item.Evidence,
        theoreticalPartsPerHour = item.TheoreticalPartsPerHour,
        requirementStale = item.RequirementStale,
        machineProfileStale = item.MachineProfileStale,
        missingFields = item.MissingFields,
        idempotentReplay = item.IdempotentReplay,
    };

    internal static object MapRuleResult(MoldCompatibilityRuleResultReadModel item) => new
    {
        ruleCode = item.RuleCode,
        labelTr = item.LabelTr,
        status = item.Status,
        requiredValue = item.RequiredValue,
        availableValue = item.AvailableValue,
        source = item.Source,
        dataTimestampUtc = item.DataTimestampUtc,
        isCritical = item.IsCritical,
    };

    internal static object MapOverview(MoldCompatibilityOverviewReadModel item) => new
    {
        moldId = item.MoldId,
        requirement = item.Requirement is null ? null : MapRequirement(item.Requirement),
        compatibleCount = item.CompatibleCount,
        conditionalCount = item.ConditionalCount,
        incompatibleCount = item.IncompatibleCount,
        insufficientDataCount = item.InsufficientDataCount,
        machines = item.Machines.Select(MapAssessmentSummary).ToList(),
    };

    internal static object MapAssessmentSummary(MoldCompatibilityAssessmentSummaryReadModel item) => new
    {
        assessmentId = item.AssessmentId,
        machineAssetId = item.MachineAssetId,
        machineCode = item.MachineCode,
        machineDisplayName = item.MachineDisplayName,
        status = item.Status,
        criticalConstraintLabel = item.CriticalConstraintLabel,
        requirementStale = item.RequirementStale,
        machineProfileStale = item.MachineProfileStale,
        theoreticalPartsPerHour = item.TheoreticalPartsPerHour,
        evaluatedAtUtc = item.EvaluatedAtUtc,
    };
}

internal sealed record UpsertCompatibilityRequirementRequestDto(
    decimal? RequiredClampForceTon,
    decimal? MoldWidthMm,
    decimal? MoldHeightMm,
    decimal? MoldDepthMm,
    decimal? RequiredTieBarWidthMm,
    decimal? RequiredTieBarHeightMm,
    decimal? MinimumShotCapacityGrams,
    string? RequiredInjectionUnit,
    string? SupportedMaterialGroup,
    decimal? NominalCycleTimeSeconds,
    int? EffectiveCavityCount,
    string SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc,
    uint? ExpectedVersion);

internal sealed record UpsertMachineCompatibilityProfileRequestDto(
    string MachineCode,
    string MachineDisplayName,
    decimal? ClampForceTon,
    decimal? MaximumMoldWidthMm,
    decimal? MaximumMoldHeightMm,
    decimal? MinimumMoldDepthMm,
    decimal? MaximumMoldDepthMm,
    decimal? TieBarWidthMm,
    decimal? TieBarHeightMm,
    decimal? MaximumShotCapacityGrams,
    string? InjectionUnit,
    IReadOnlyList<string>? SupportedMaterialGroups,
    string SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc,
    uint? ExpectedVersion);
