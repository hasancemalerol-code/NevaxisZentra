using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Material;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.Material;

public static class MaterialRequirementEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMaterialRequirementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/materials").RequireAuthorization();

        group.MapGet("/requirement-profiles", ListProfilesAsync);
        group.MapPost("/requirement-profiles", CreateProfileAsync);
        group.MapGet("/requirement-profiles/{profileId:guid}", GetProfileAsync);
        group.MapPut("/requirement-profiles/{profileId:guid}", UpdateProfileAsync);
        group.MapGet("/consumption-observations", ListObservationsAsync);
        group.MapPost("/consumption-observations", CreateObservationAsync);
        group.MapGet("/consumption-observations/{observationId:guid}", GetObservationAsync);
        group.MapGet("/{materialId:guid}/requirement-overview", GetOverviewAsync);
        group.MapPost("/{materialId:guid}/requirement-evaluations", EvaluateRequirementAsync);
        group.MapPost("/{materialId:guid}/variance-evaluations", EvaluateVarianceAsync);
        group.MapPost("/{materialId:guid}/regrind-policy-evaluations", EvaluateRegrindPolicyAsync);
        group.MapPost("/{materialId:guid}/availability-risk-evaluations", EvaluateAvailabilityRiskAsync);
        group.MapGet("/assessments/{assessmentId:guid}", GetAssessmentAsync);

        return endpoints;
    }

    private static async Task<IResult> ListProfilesAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? materialId,
        [FromQuery] string? requirementType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IMaterialRequirementRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var profiles = await repository.ListProfilesAsync(
            context, new MaterialRequirementProfileListFilters(materialId, requirementType, page, pageSize), cancellationToken);
        return TypedResults.Ok(profiles.Select(MaterialRequirementApiMapper.MapProfileSummary).ToList());
    }

    private static async Task<IResult> GetProfileAsync(
        Guid profileId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialRequirementRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var profile = await repository.GetProfileByIdAsync(context, profileId, cancellationToken);
        return profile is null
            ? MaterialEndpointHelpers.MapRequirementFailure(MaterialRequirementFailureCodes.NotFound, "Requirement profile not found.")
            : TypedResults.Ok(MaterialRequirementApiMapper.MapProfileDetail(profile));
    }

    private static async Task<IResult> CreateProfileAsync(
        [FromBody] CreateMaterialRequirementProfileRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialRequirementRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.CreateProfileAsync(context, request.ToCommand(), cancellationToken);
        return MaterialEndpointHelpers.MapRequirementWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateProfileAsync(
        Guid profileId,
        [FromBody] UpdateMaterialRequirementProfileRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialRequirementRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.UpdateProfileAsync(context, request.ToCommand(profileId), cancellationToken);
        return MaterialEndpointHelpers.MapRequirementWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> ListObservationsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? materialId,
        [FromQuery] Guid? requirementProfileId,
        [FromQuery] string? observationType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IMaterialRequirementRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var observations = await repository.ListObservationsAsync(
            context, new MaterialConsumptionObservationListFilters(materialId, requirementProfileId, observationType, page, pageSize), cancellationToken);
        return TypedResults.Ok(observations.Select(MaterialRequirementApiMapper.MapObservation).ToList());
    }

    private static async Task<IResult> GetObservationAsync(
        Guid observationId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialRequirementRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var observation = await repository.GetObservationByIdAsync(context, observationId, cancellationToken);
        return observation is null
            ? MaterialEndpointHelpers.MapRequirementFailure(MaterialRequirementFailureCodes.NotFound, "Consumption observation not found.")
            : TypedResults.Ok(MaterialRequirementApiMapper.MapObservation(observation));
    }

    private static async Task<IResult> CreateObservationAsync(
        [FromBody] CreateMaterialConsumptionObservationRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialRequirementRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.CreateObservationAsync(context, request.ToCommand(), cancellationToken);
        return MaterialEndpointHelpers.MapRequirementWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> GetOverviewAsync(
        Guid materialId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialRequirementRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var overview = await repository.GetOverviewAsync(context, materialId, cancellationToken);
        return overview is null
            ? MaterialEndpointHelpers.MapRequirementFailure(MaterialRequirementFailureCodes.NotFound, "Material not found.")
            : TypedResults.Ok(MaterialRequirementApiMapper.MapOverview(overview));
    }

    private static async Task<IResult> GetAssessmentAsync(
        Guid assessmentId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialRequirementRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var assessment = await repository.GetAssessmentByIdAsync(context, assessmentId, cancellationToken);
        return assessment is null
            ? MaterialEndpointHelpers.MapRequirementFailure(MaterialRequirementFailureCodes.NotFound, "Assessment not found.")
            : TypedResults.Ok(MaterialRequirementApiMapper.MapAssessment(assessment));
    }

    private static async Task<IResult> EvaluateRequirementAsync(
        Guid materialId,
        [FromBody] EvaluateMaterialAssessmentRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialRequirementRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        if (!request.RequirementProfileId.HasValue)
        {
            return MaterialEndpointHelpers.MapRequirementFailure(
                MaterialRequirementFailureCodes.ValidationFailed, "Requirement profile id is required.");
        }

        var result = await repository.EvaluateRequirementAsync(
            context, new EvaluateMaterialRequirementCommand(materialId, request.RequirementProfileId.Value, request.IdempotencyKey), cancellationToken);
        return MaterialEndpointHelpers.MapRequirementWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> EvaluateVarianceAsync(
        Guid materialId,
        [FromBody] EvaluateMaterialAssessmentRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialRequirementRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        if (!request.RequirementProfileId.HasValue)
        {
            return MaterialEndpointHelpers.MapRequirementFailure(
                MaterialRequirementFailureCodes.ValidationFailed, "Requirement profile id is required.");
        }

        var result = await repository.EvaluateVarianceAsync(
            context, new EvaluateMaterialVarianceCommand(materialId, request.RequirementProfileId.Value, request.IdempotencyKey), cancellationToken);
        return MaterialEndpointHelpers.MapRequirementWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> EvaluateRegrindPolicyAsync(
        Guid materialId,
        [FromBody] EvaluateMaterialAssessmentRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialRequirementRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        if (!request.RequirementProfileId.HasValue)
        {
            return MaterialEndpointHelpers.MapRequirementFailure(
                MaterialRequirementFailureCodes.ValidationFailed, "Requirement profile id is required.");
        }

        var result = await repository.EvaluateRegrindPolicyAsync(
            context, new EvaluateMaterialRegrindPolicyCommand(materialId, request.RequirementProfileId.Value, request.IdempotencyKey), cancellationToken);
        return MaterialEndpointHelpers.MapRequirementWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> EvaluateAvailabilityRiskAsync(
        Guid materialId,
        [FromBody] EvaluateMaterialAssessmentRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialRequirementRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var hasErpRead = await HasErpReadAccessAsync(context.FacilityId, facilityAccessResolver, cancellationToken);

        var result = await repository.EvaluateAvailabilityRiskAsync(
            context,
            new EvaluateMaterialAvailabilityRiskCommand(materialId, request.RequirementProfileId, request.IdempotencyKey),
            hasErpRead,
            cancellationToken);
        return MaterialEndpointHelpers.MapRequirementWriteResult(result, isCreate: true);
    }

    private static async Task<bool> HasErpReadAccessAsync(
        Guid facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId, ErpPermissionKeys.Read),
            cancellationToken);
        return accessResult.Succeeded && accessResult.Access is not null;
    }
}

internal static class MaterialRequirementApiMapper
{
    internal static object MapProfileSummary(MaterialRequirementProfileSummaryReadModel item) => new
    {
        id = item.Id,
        materialId = item.MaterialId,
        materialCode = item.MaterialCode,
        materialName = item.MaterialName,
        requirementType = item.RequirementType,
        productionOrderReference = item.ProductionOrderReference,
        productReference = item.ProductReference,
        moldReference = item.MoldReference,
        goodPartTargetQuantity = item.GoodPartTargetQuantity,
        plannedRegrindPercent = item.PlannedRegrindPercent,
        sourceType = item.SourceType,
        sourceReference = item.SourceReference,
        dataRecordedAtUtc = item.DataRecordedAtUtc,
        updatedAtUtc = item.UpdatedAtUtc,
        version = item.Version,
    };

    internal static object MapProfileDetail(MaterialRequirementProfileDetailReadModel item) => new
    {
        id = item.Id,
        facilityId = item.FacilityId,
        materialId = item.MaterialId,
        materialCode = item.MaterialCode,
        materialName = item.MaterialName,
        requirementType = item.RequirementType,
        productionOrderReference = item.ProductionOrderReference,
        productReference = item.ProductReference,
        moldReference = item.MoldReference,
        plannedProductionQuantity = item.PlannedProductionQuantity,
        goodPartTargetQuantity = item.GoodPartTargetQuantity,
        partWeightGrams = item.PartWeightGrams,
        runnerWeightGramsPerShot = item.RunnerWeightGramsPerShot,
        effectiveCavityCount = item.EffectiveCavityCount,
        setupAllowanceGrams = item.SetupAllowanceGrams,
        processLossAllowancePercent = item.ProcessLossAllowancePercent,
        plannedVirginPercent = item.PlannedVirginPercent,
        plannedRegrindPercent = item.PlannedRegrindPercent,
        sourceType = item.SourceType,
        sourceReference = item.SourceReference,
        dataRecordedAtUtc = item.DataRecordedAtUtc,
        createdAtUtc = item.CreatedAtUtc,
        updatedAtUtc = item.UpdatedAtUtc,
        version = item.Version,
    };

    internal static object MapObservation(MaterialConsumptionObservationReadModel item) => new
    {
        id = item.Id,
        materialId = item.MaterialId,
        materialCode = item.MaterialCode,
        materialLotId = item.MaterialLotId,
        lotNumber = item.LotNumber,
        requirementProfileId = item.RequirementProfileId,
        observationType = item.ObservationType,
        quantity = item.Quantity,
        unitOfMeasure = item.UnitOfMeasure,
        virginQuantity = item.VirginQuantity,
        regrindQuantity = item.RegrindQuantity,
        goodPartQuantity = item.GoodPartQuantity,
        scrapQuantity = item.ScrapQuantity,
        productionOrderReference = item.ProductionOrderReference,
        productReference = item.ProductReference,
        moldReference = item.MoldReference,
        observedAtUtc = item.ObservedAtUtc,
        sourceType = item.SourceType,
        sourceReference = item.SourceReference,
        dataRecordedAtUtc = item.DataRecordedAtUtc,
        notes = item.Notes,
        createdAtUtc = item.CreatedAtUtc,
        version = item.Version,
        disclaimer = MaterialRequirementConstants.ConsumptionObservationDisclaimer,
    };

    internal static object MapAssessment(MaterialRequirementAssessmentReadModel item) => new
    {
        id = item.Id,
        materialId = item.MaterialId,
        requirementProfileId = item.RequirementProfileId,
        assessmentType = item.AssessmentType,
        status = item.Status,
        ruleSetVersion = item.RuleSetVersion,
        inputFingerprint = item.InputFingerprint,
        drivers = item.Drivers,
        evidence = item.Evidence,
        theoreticalRequirementGrams = item.TheoreticalRequirementGrams,
        observedConsumptionGrams = item.ObservedConsumptionGrams,
        varianceGrams = item.VarianceGrams,
        variancePercent = item.VariancePercent,
        virginRequirementGrams = item.VirginRequirementGrams,
        regrindRequirementGrams = item.RegrindRequirementGrams,
        evaluatedAtUtc = item.EvaluatedAtUtc,
        createdAtUtc = item.CreatedAtUtc,
        version = item.Version,
    };

    internal static object MapOverview(MaterialRequirementOverviewReadModel item) => new
    {
        materialId = item.MaterialId,
        materialCode = item.MaterialCode,
        currentRequirementAssessment = item.CurrentRequirementAssessment is null ? null : MapAssessment(item.CurrentRequirementAssessment),
        currentVarianceAssessment = item.CurrentVarianceAssessment is null ? null : MapAssessment(item.CurrentVarianceAssessment),
        currentRegrindAssessment = item.CurrentRegrindAssessment is null ? null : MapAssessment(item.CurrentRegrindAssessment),
        currentAvailabilityAssessment = item.CurrentAvailabilityAssessment is null ? null : MapAssessment(item.CurrentAvailabilityAssessment),
        profiles = item.Profiles.Select(MapProfileSummary).ToList(),
        recentObservations = item.RecentObservations.Select(MapObservation).ToList(),
        theoreticalRequirementDisclaimer = item.TheoreticalRequirementDisclaimer,
        consumptionObservationDisclaimer = item.ConsumptionObservationDisclaimer,
    };
}

internal sealed record CreateMaterialRequirementProfileRequest(
    Guid MaterialId,
    string RequirementType,
    string? ProductionOrderReference,
    string? ProductReference,
    string? MoldReference,
    decimal? PlannedProductionQuantity,
    decimal? GoodPartTargetQuantity,
    decimal? PartWeightGrams,
    decimal? RunnerWeightGramsPerShot,
    int? EffectiveCavityCount,
    decimal? SetupAllowanceGrams,
    decimal? ProcessLossAllowancePercent,
    decimal? PlannedVirginPercent,
    decimal? PlannedRegrindPercent,
    string SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc,
    string? IdempotencyKey)
{
    internal CreateMaterialRequirementProfileCommand ToCommand() => new(
        MaterialId, RequirementType, ProductionOrderReference, ProductReference, MoldReference,
        PlannedProductionQuantity, GoodPartTargetQuantity, PartWeightGrams, RunnerWeightGramsPerShot,
        EffectiveCavityCount, SetupAllowanceGrams, ProcessLossAllowancePercent,
        PlannedVirginPercent, PlannedRegrindPercent, SourceType, SourceReference,
        DataRecordedAtUtc, IdempotencyKey);
}

internal sealed record UpdateMaterialRequirementProfileRequest(
    uint ExpectedVersion,
    string RequirementType,
    string? ProductionOrderReference,
    string? ProductReference,
    string? MoldReference,
    decimal? PlannedProductionQuantity,
    decimal? GoodPartTargetQuantity,
    decimal? PartWeightGrams,
    decimal? RunnerWeightGramsPerShot,
    int? EffectiveCavityCount,
    decimal? SetupAllowanceGrams,
    decimal? ProcessLossAllowancePercent,
    decimal? PlannedVirginPercent,
    decimal? PlannedRegrindPercent,
    string SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc)
{
    internal UpdateMaterialRequirementProfileCommand ToCommand(Guid profileId) => new(
        profileId, ExpectedVersion, RequirementType, ProductionOrderReference, ProductReference, MoldReference,
        PlannedProductionQuantity, GoodPartTargetQuantity, PartWeightGrams, RunnerWeightGramsPerShot,
        EffectiveCavityCount, SetupAllowanceGrams, ProcessLossAllowancePercent,
        PlannedVirginPercent, PlannedRegrindPercent, SourceType, SourceReference, DataRecordedAtUtc);
}

internal sealed record CreateMaterialConsumptionObservationRequest(
    Guid MaterialId,
    Guid? MaterialLotId,
    Guid? RequirementProfileId,
    string ObservationType,
    decimal Quantity,
    string UnitOfMeasure,
    decimal? VirginQuantity,
    decimal? RegrindQuantity,
    decimal? GoodPartQuantity,
    decimal? ScrapQuantity,
    string? ProductionOrderReference,
    string? ProductReference,
    string? MoldReference,
    DateTimeOffset ObservedAtUtc,
    string SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc,
    string? Notes,
    string IdempotencyKey)
{
    internal CreateMaterialConsumptionObservationCommand ToCommand() => new(
        MaterialId, MaterialLotId, RequirementProfileId, ObservationType, Quantity, UnitOfMeasure,
        VirginQuantity, RegrindQuantity, GoodPartQuantity, ScrapQuantity,
        ProductionOrderReference, ProductReference, MoldReference, ObservedAtUtc,
        SourceType, SourceReference, DataRecordedAtUtc, Notes, IdempotencyKey);
}

internal sealed record EvaluateMaterialAssessmentRequest(
    Guid? RequirementProfileId,
    string? IdempotencyKey);
