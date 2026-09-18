using Microsoft.AspNetCore.Mvc;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.FactoryNetworkOptimization;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Api.FactoryNetworkOptimization;

internal sealed record FactoryNetworkAccessContext(
    FacilityAccessContext RequestingAccess,
    Guid NetworkId,
    IReadOnlyList<Guid> PermittedFacilityIds);

internal static class FactoryNetworkEndpointHelpers
{
    internal static async Task<(FacilityAccessContext? Access, IResult? Failure)> ResolveAccessAsync(
        Guid? facilityId,
        string permissionKey,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, TypedResults.BadRequest(new { code = "facility-missing", message = "Tesis tanımlayıcısı zorunludur." }));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, permissionKey),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (accessResult.Access, null);
    }

    internal static async Task<(FactoryNetworkAccessContext? Access, IResult? Failure)> ResolveNetworkAccessAsync(
        Guid? facilityId,
        Guid? networkId,
        string permissionKey,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryNetworkRepository repository,
        TenantDatabaseSession tenantDatabaseSession,
        CancellationToken cancellationToken)
    {
        var (requestingAccess, failure) = await ResolveAccessAsync(
            facilityId, permissionKey, facilityAccessResolver, cancellationToken);
        if (requestingAccess is null)
        {
            return (null, failure);
        }

        return await tenantDatabaseSession.ExecuteReadAsync<(FactoryNetworkAccessContext?, IResult?)>(async ct =>
        {
            var resolvedNetworkId = await repository.ResolveNetworkIdAsync(
                networkId, requestingAccess.FacilityId, ct);
            if (resolvedNetworkId is null || resolvedNetworkId == Guid.Empty)
            {
                return ((FactoryNetworkAccessContext?)null, TypedResults.NotFound(new
                {
                    code = FactoryNetworkFailureCodes.NotFound,
                    message = "Fabrika ağı bulunamadı.",
                }));
            }

            var networkFacilityIds = await repository.GetNetworkFacilityIdsAsync(
                resolvedNetworkId.Value, ct);
            if (networkFacilityIds.Count == 0)
            {
                return ((FactoryNetworkAccessContext?)null, TypedResults.NotFound(new
                {
                    code = FactoryNetworkFailureCodes.NotFound,
                    message = "Fabrika ağı tesisleri bulunamadı.",
                }));
            }

            if (!networkFacilityIds.Contains(requestingAccess.FacilityId))
            {
                return ((FactoryNetworkAccessContext?)null, TypedResults.Problem(
                    title: "Tesis fabrika ağı kapsamında değil.",
                    statusCode: StatusCodes.Status403Forbidden,
                    extensions: FailureExtensions(FactoryNetworkFailureCodes.FacilityScopeDenied)));
            }

            var permittedFacilityIds = new List<Guid>();
            foreach (var networkFacilityId in networkFacilityIds)
            {
                var facilityAccess = await facilityAccessResolver.ResolveAsync(
                    new FacilityAccessRequest(networkFacilityId, permissionKey),
                    ct);
                if (facilityAccess.Succeeded && facilityAccess.Access is not null)
                {
                    permittedFacilityIds.Add(networkFacilityId);
                }
            }

            if (permittedFacilityIds.Count == 0)
            {
                return ((FactoryNetworkAccessContext?)null, TypedResults.Problem(
                    title: "Fabrika ağı için yetkili tesis bulunamadı.",
                    statusCode: StatusCodes.Status403Forbidden,
                    extensions: FailureExtensions(FactoryNetworkFailureCodes.FacilityScopeDenied)));
            }

            return ((FactoryNetworkAccessContext?)new FactoryNetworkAccessContext(
                requestingAccess,
                resolvedNetworkId.Value,
                permittedFacilityIds), null);
        }, cancellationToken);
    }

    internal static IResult? ValidateFacilityScope(
        IReadOnlyList<Guid> permittedFacilityIds,
        params Guid[] facilityIds)
    {
        foreach (var facilityId in facilityIds)
        {
            if (facilityId != Guid.Empty && !permittedFacilityIds.Contains(facilityId))
            {
                return TypedResults.Problem(
                    title: "İstenen tesis fabrika ağı yetki kapsamında değil.",
                    statusCode: StatusCodes.Status403Forbidden,
                    extensions: FailureExtensions(FactoryNetworkFailureCodes.FacilityScopeDenied));
            }
        }

        return null;
    }

    internal static IResult MapException(FactoryNetworkException ex) =>
        ex.FailureCode switch
        {
            FactoryNetworkFailureCodes.NotFound => TypedResults.NotFound(new { code = ex.FailureCode, message = ex.Message }),
            FactoryNetworkFailureCodes.ConcurrencyConflict => TypedResults.Conflict(new { code = ex.FailureCode, message = ex.Message }),
            FactoryNetworkFailureCodes.DuplicateExecution => TypedResults.Conflict(new { code = ex.FailureCode, message = ex.Message }),
            FactoryNetworkFailureCodes.Conflict => TypedResults.Conflict(new { code = ex.FailureCode, message = ex.Message }),
            FactoryNetworkFailureCodes.Forbidden => TypedResults.Forbid(),
            FactoryNetworkFailureCodes.FacilityScopeDenied => TypedResults.Problem(
                title: ex.Message,
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExtensions(ex.FailureCode)),
            FactoryNetworkFailureCodes.Infeasible => TypedResults.UnprocessableEntity(new { code = ex.FailureCode, message = ex.Message }),
            FactoryNetworkFailureCodes.StaleData => TypedResults.UnprocessableEntity(new { code = ex.FailureCode, message = ex.Message }),
            FactoryNetworkFailureCodes.ApprovalRequired => TypedResults.UnprocessableEntity(new { code = ex.FailureCode, message = ex.Message }),
            FactoryNetworkFailureCodes.PolicyViolation => TypedResults.UnprocessableEntity(new { code = ex.FailureCode, message = ex.Message }),
            FactoryNetworkFailureCodes.QuantityViolation => TypedResults.UnprocessableEntity(new { code = ex.FailureCode, message = ex.Message }),
            FactoryNetworkFailureCodes.ValidationFailed => TypedResults.BadRequest(new { code = ex.FailureCode, message = ex.Message }),
            _ => TypedResults.UnprocessableEntity(new { code = ex.FailureCode, message = ex.Message }),
        };

    internal static object MapWorkspace(FactoryNetworkWorkspaceSnapshot workspace) => new
    {
        network = MapNetwork(workspace.Network),
        facilities = workspace.Facilities.Select(MapFacility).ToList(),
        capabilityProfiles = workspace.CapabilityProfiles.Select(MapCapabilityProfile).ToList(),
        facilityStates = workspace.FacilityStates.Select(MapFacilityState).ToList(),
        capacityMap = workspace.CapacityMap.Select(MapCapacityRow).ToList(),
        demand = workspace.Demand.Select(MapDemandRow).ToList(),
        opportunities = workspace.Opportunities.Select(MapOpportunity).ToList(),
        scenarios = workspace.Scenarios.Select(MapScenario).ToList(),
        activeAllocations = workspace.ActiveAllocations.Select(MapAllocation).ToList(),
        activityFeed = workspace.ActivityFeed.Select(MapActivity).ToList(),
        policy = MapPolicy(workspace.Policy),
        metrics = workspace.Metrics,
        resilience = workspace.Resilience,
        dataFreshnessUtc = workspace.DataFreshnessUtc,
    };

    internal static object MapSimulationResult(NetworkSimulationResult result) => new
    {
        scenario = MapScenario(result.Scenario),
        feasibility = MapFeasibility(result.Feasibility),
        comparisonJson = result.ComparisonJson,
        recommendationExplanation = result.RecommendationExplanation,
    };

    internal static object MapAllocationEvaluation(NetworkAllocationEvaluation evaluation) => new
    {
        recommendedAction = evaluation.RecommendedAction,
        rationale = evaluation.Rationale,
        recommendationExplanation = evaluation.RecommendationExplanation,
        facilityComparisons = evaluation.FacilityComparisons.Select(MapFacilityComparison).ToList(),
        alternatives = evaluation.Alternatives.Select(MapScenario).ToList(),
    };

    internal static object MapAllocationResult(NetworkAllocationResult result) => new
    {
        allocation = MapAllocation(result.Allocation),
        items = result.Items.Select(MapAllocationItem).ToList(),
        approvalRequired = result.ApprovalRequired,
        autoExecuted = result.AutoExecuted,
    };

    internal static object MapNetwork(FactoryNetwork network) => new
    {
        id = network.Id,
        name = network.Name,
        planningHorizon = network.PlanningHorizon,
        status = network.Status,
        concurrencyToken = network.ConcurrencyToken,
    };

    internal static object MapFacility(FactoryNetworkFacility facility) => new
    {
        id = facility.Id,
        facilityId = facility.FacilityId,
        role = facility.Role,
        sortOrder = facility.SortOrder,
    };

    internal static object MapCapabilityProfile(FacilityCapabilityProfile profile) => new
    {
        id = profile.Id,
        facilityId = profile.FacilityId,
        technologiesJson = profile.TechnologiesJson,
        linesJson = profile.LinesJson,
        machineFamiliesJson = profile.MachineFamiliesJson,
        moldCompatibilityJson = profile.MoldCompatibilityJson,
        materialsJson = profile.MaterialsJson,
        qualityCertificationsJson = profile.QualityCertificationsJson,
        shiftsJson = profile.ShiftsJson,
        capacityJson = profile.CapacityJson,
        riskJson = profile.RiskJson,
    };

    internal static object MapFacilityState(NetworkFacilityStateRow state) => new
    {
        facilityId = state.FacilityId,
        facilityName = state.FacilityName,
        timezone = state.Timezone,
        loadPercent = state.LoadPercent,
        availableCapacityPercent = state.AvailableCapacityPercent,
        ordersAtRisk = state.OrdersAtRisk,
        activeCases = state.ActiveCases,
        planningState = state.PlanningState,
        freshnessUtc = state.FreshnessUtc,
    };

    internal static object MapCapacityRow(NetworkCapacityRow row) => new
    {
        facilityId = row.FacilityId,
        facilityName = row.FacilityName,
        availablePercent = row.AvailablePercent,
        committedPercent = row.CommittedPercent,
        atRiskPercent = row.AtRiskPercent,
        unusedPercent = row.UnusedPercent,
        overloadedPercent = row.OverloadedPercent,
        realizable = row.Realizable,
    };

    internal static object MapDemandRow(NetworkDemandRow row) => new
    {
        orderId = row.OrderId,
        assignedFacilityId = row.AssignedFacilityId,
        remainingQuantity = row.RemainingQuantity,
        deadlineUtc = row.DeadlineUtc,
        riskLevel = row.RiskLevel,
        requiredCapability = row.RequiredCapability,
        materialReady = row.MaterialReady,
        moldReady = row.MoldReady,
    };

    internal static object MapOpportunity(NetworkOpportunityRow opportunity) => new
    {
        opportunityCode = opportunity.OpportunityCode,
        title = opportunity.Title,
        summary = opportunity.Summary,
        sourceFacilityId = opportunity.SourceFacilityId,
        destinationFacilityId = opportunity.DestinationFacilityId,
        orderId = opportunity.OrderId,
        expectedBenefitScore = opportunity.ExpectedBenefitScore,
        dataClassification = opportunity.DataClassification,
    };

    internal static object MapScenario(NetworkScenario scenario) => new
    {
        id = scenario.Id,
        scenarioCode = scenario.ScenarioCode,
        title = scenario.Title,
        actionType = scenario.ActionType,
        sourceFacilityId = scenario.SourceFacilityId,
        destinationFacilityId = scenario.DestinationFacilityId,
        orderId = scenario.OrderId,
        score = scenario.Score,
        comparisonJson = scenario.ComparisonJson,
        status = scenario.Status,
        createdAtUtc = scenario.CreatedAtUtc,
    };

    internal static object MapAllocation(NetworkAllocation allocation) => new
    {
        id = allocation.Id,
        networkId = allocation.NetworkId,
        actionType = allocation.ActionType,
        status = allocation.Status,
        sourceFacilityId = allocation.SourceFacilityId,
        destinationFacilityId = allocation.DestinationFacilityId,
        triggerType = allocation.TriggerType,
        reasonJson = allocation.ReasonJson,
        expectedImpactJson = allocation.ExpectedImpactJson,
        actualImpactJson = allocation.ActualImpactJson,
        publishedAtUtc = allocation.PublishedAtUtc,
        verifiedAtUtc = allocation.VerifiedAtUtc,
        createdAtUtc = allocation.CreatedAtUtc,
        concurrencyToken = allocation.ConcurrencyToken,
    };

    internal static object MapAllocationItem(NetworkAllocationItem item) => new
    {
        id = item.Id,
        orderId = item.OrderId,
        sourceFacilityId = item.SourceFacilityId,
        destinationFacilityId = item.DestinationFacilityId,
        totalQuantity = item.TotalQuantity,
        sourceQuantity = item.SourceQuantity,
        destinationQuantity = item.DestinationQuantity,
        status = item.Status,
    };

    internal static object MapActivity(NetworkActivityEvent activity) => new
    {
        id = activity.Id,
        eventType = activity.EventType,
        summary = activity.Summary,
        occurredAtUtc = activity.OccurredAtUtc,
    };

    internal static object MapPolicy(FactoryNetworkPolicy policy) => new
    {
        id = policy.Id,
        autonomyMode = policy.AutonomyMode,
        policyEnvelopeJson = policy.PolicyEnvelopeJson,
        concurrencyToken = policy.ConcurrencyToken,
    };

    internal static object MapFeasibility(FeasibilityEvaluation feasibility) => new
    {
        isFeasible = feasibility.IsFeasible,
        hardViolations = feasibility.HardViolations,
        softWarnings = feasibility.SoftWarnings,
        explanation = feasibility.Explanation,
    };

    internal static object MapFacilityComparison(FacilityComparisonRow row) => new
    {
        facilityId = row.FacilityId,
        facilityName = row.FacilityName,
        feasible = row.Feasible,
        capacityScore = row.CapacityScore,
        deadlineScore = row.DeadlineScore,
        qualityScore = row.QualityScore,
        materialScore = row.MaterialScore,
        maintenanceScore = row.MaintenanceScore,
        riskScore = row.RiskScore,
        confidenceScore = row.ConfidenceScore,
        recommendationReason = row.RecommendationReason,
    };

    private static Dictionary<string, object?> FailureExtensions(string code) =>
        new() { ["code"] = code };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(
        FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                title: "Kimlik doğrulama gerekli.",
                statusCode: StatusCodes.Status401Unauthorized),
            FacilityAccessFailureCodes.PermissionDenied => TypedResults.Problem(
                title: "Yetki reddedildi.",
                statusCode: StatusCodes.Status403Forbidden),
            _ => TypedResults.Problem(
                title: accessResult.InternalReason,
                statusCode: StatusCodes.Status403Forbidden),
        };
}
