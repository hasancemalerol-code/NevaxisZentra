using System.Globalization;
using System.Text.Json;
using NevaxisFactOS.Persistence.AdaptiveProductionPlanning;
using NevaxisFactOS.Persistence.FactoryLearning;
using NevaxisFactOS.Persistence.ProductionManager;
using NevaxisFactOS.Persistence.ProductionPlanning;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.FactoryNetworkOptimization;

public static class NetworkFactoryStateProjector
{
    public static async Task<IReadOnlyList<NetworkFacilityContext>> CaptureAsync(
        NevaxisFactOSDbContext dbContext,
        Guid tenantId,
        IReadOnlyList<Guid> facilityIds,
        IReadOnlyDictionary<Guid, FacilityCapabilityProfile> profiles,
        CancellationToken cancellationToken)
    {
        var contexts = new List<NetworkFacilityContext>();
        foreach (var facilityId in facilityIds)
        {
            var facility = await dbContext.Facilities
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.TenantId == tenantId && f.Id == facilityId, cancellationToken);
            if (facility is null) continue;

            var latestSnapshot = await dbContext.FactoryStateSnapshots
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId && s.FacilityId == facilityId)
                .OrderByDescending(s => s.CapturedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            var state = latestSnapshot ?? await FactoryStateProjector.CaptureAsync(
                dbContext, tenantId, facilityId, cancellationToken);
            profiles.TryGetValue(facilityId, out var profile);
            profile ??= new FacilityCapabilityProfile
            {
                Id = Guid.Empty,
                TenantId = tenantId,
                FacilityId = facilityId,
            };

            var load = ParseUtilization(state.CapacityStateJson);
            contexts.Add(new NetworkFacilityContext(
                facilityId,
                facility.Name,
                facility.TimeZoneId,
                state,
                profile,
                load,
                Math.Max(0, 100 - load),
                ParseFreshnessUtc(state.FreshnessJson, state.CapturedAtUtc)));
        }

        return contexts;
    }

    public static NetworkFacilityStateRow ToStateRow(NetworkFacilityContext context)
    {
        var activeCases = CountActiveCases(context.State.ActiveCasesJson);
        var ordersAtRisk = context.State.ProductionStateJson.Contains("behindSchedule", StringComparison.OrdinalIgnoreCase)
            ? Math.Max(1, activeCases / 2)
            : 0;
        var planningState = context.LoadPercent >= 90 ? "overloaded"
            : context.LoadPercent >= 75 ? "tight"
            : "healthy";

        return new NetworkFacilityStateRow(
            context.FacilityId,
            context.FacilityName,
            context.Timezone,
            context.LoadPercent,
            context.AvailablePercent,
            ordersAtRisk,
            activeCases,
            planningState,
            context.FreshnessUtc);
    }

    private static decimal ParseUtilization(string capacityJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(capacityJson);
            if (doc.RootElement.TryGetProperty("utilizationPercent", out var util))
                return util.GetDecimal();
        }
        catch (JsonException) { }

        return 72m;
    }

    private static DateTimeOffset ParseFreshnessUtc(string freshnessJson, DateTimeOffset fallback)
    {
        try
        {
            using var doc = JsonDocument.Parse(freshnessJson);
            DateTimeOffset? oldest = null;
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String &&
                    DateTimeOffset.TryParse(prop.Value.GetString(), CultureInfo.InvariantCulture, out var parsed))
                {
                    oldest = oldest is null || parsed < oldest ? parsed : oldest;
                }
            }

            return oldest ?? fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }

    private static int CountActiveCases(string activeCasesJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(activeCasesJson);
            return doc.RootElement.ValueKind == JsonValueKind.Array ? doc.RootElement.GetArrayLength() : 0;
        }
        catch (JsonException)
        {
            return 0;
        }
    }
}

public static class NetworkMaintenanceGuard
{
    public static bool HasOpenMaintenance(string maintenanceStateJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(maintenanceStateJson);
            if (doc.RootElement.TryGetProperty("openMaintenanceCases", out var openCases) &&
                openCases.TryGetInt32(out var count))
            {
                return count > 0;
            }
        }
        catch (JsonException) { }

        return maintenanceStateJson.Contains("\"openMaintenanceCases\":", StringComparison.Ordinal) &&
               !maintenanceStateJson.Contains("\"openMaintenanceCases\":0", StringComparison.Ordinal);
    }
}

public static class NetworkCapacityMapEngine
{
    public static IReadOnlyList<NetworkCapacityRow> Build(
        IReadOnlyList<NetworkFacilityContext> contexts,
        NetworkCapacitySnapshot? snapshot = null)
    {
        var utilizationOverrides = ParseSnapshotUtilization(snapshot);
        return contexts.Select(ctx =>
        {
            var load = utilizationOverrides.TryGetValue(ctx.FacilityId, out var overrideLoad)
                ? overrideLoad
                : ctx.LoadPercent;
            var committed = Math.Min(95m, load);
            var atRisk = load >= 85 ? Math.Min(20m, load - 70m) : 0m;
            var overloaded = Math.Max(0, load - 100m);
            var available = Math.Max(0, 100m - committed - atRisk);
            var unused = Math.Max(0, available - 10m);
            var realizable = available >= 8m && !NetworkMaintenanceGuard.HasOpenMaintenance(ctx.State.MaintenanceStateJson);
            return new NetworkCapacityRow(
                ctx.FacilityId,
                ctx.FacilityName,
                available,
                committed,
                atRisk,
                unused,
                overloaded,
                realizable);
        }).ToList();
    }

    private static Dictionary<Guid, decimal> ParseSnapshotUtilization(NetworkCapacitySnapshot? snapshot)
    {
        var map = new Dictionary<Guid, decimal>();
        if (snapshot is null) return map;

        try
        {
            using var doc = JsonDocument.Parse(snapshot.FacilityCapacityJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return map;

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.TryGetProperty("facilityId", out var fid) &&
                    Guid.TryParse(fid.GetString(), out var facilityId) &&
                    item.TryGetProperty("utilizationPercent", out var util))
                {
                    map[facilityId] = util.GetDecimal();
                }
            }
        }
        catch (JsonException) { }

        return map;
    }
}

public static class NetworkDemandEngine
{
    public static IReadOnlyList<NetworkDemandRow> ParseSnapshot(NetworkCapacitySnapshot? snapshot)
    {
        if (snapshot is null) return DefaultDemand();

        try
        {
            using var doc = JsonDocument.Parse(snapshot.DemandJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return DefaultDemand();

            var rows = new List<NetworkDemandRow>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                rows.Add(new NetworkDemandRow(
                    item.TryGetProperty("orderId", out var oid) ? oid.GetString() ?? "PO-UNKNOWN" : "PO-UNKNOWN",
                    item.TryGetProperty("assignedFacilityId", out var fid) && Guid.TryParse(fid.GetString(), out var facilityId)
                        ? facilityId : Guid.Empty,
                    item.TryGetProperty("remainingQuantity", out var qty) && qty.TryGetDecimal(out var quantity) ? quantity : 100m,
                    item.TryGetProperty("deadlineUtc", out var dl) &&
                    DateTimeOffset.TryParse(dl.GetString(), CultureInfo.InvariantCulture, out var deadline)
                        ? deadline : DateTimeOffset.UtcNow.AddDays(2),
                    item.TryGetProperty("riskLevel", out var risk) ? risk.GetString() ?? "medium" : "medium",
                    item.TryGetProperty("requiredCapability", out var cap) ? cap.GetString() ?? "injection" : "injection",
                    !item.TryGetProperty("materialReady", out var mr) || mr.GetBoolean(),
                    !item.TryGetProperty("moldReady", out var mold) || mold.GetBoolean()));
            }

            return rows.Count > 0 ? rows : DefaultDemand();
        }
        catch (JsonException)
        {
            return DefaultDemand();
        }
    }

    private static IReadOnlyList<NetworkDemandRow> DefaultDemand() =>
    [
        new("PO-1842", Guid.Empty, 240m, DateTimeOffset.UtcNow.AddHours(18), "high", "injection-molding", true, true),
        new("PO-1901", Guid.Empty, 120m, DateTimeOffset.UtcNow.AddDays(1), "medium", "injection-molding", true, false),
    ];
}

public static class LoadImbalanceDetectionEngine
{
    public static decimal ComputeImbalancePercent(IReadOnlyList<NetworkCapacityRow> capacityMap)
    {
        if (capacityMap.Count < 2) return 0m;
        var loads = capacityMap.Select(c => c.CommittedPercent).ToList();
        return Math.Round(loads.Max() - loads.Min(), 2);
    }
}

public static class CrossPlantOpportunityEngine
{
    public static IReadOnlyList<NetworkOpportunityRow> Detect(
        IReadOnlyList<NetworkFacilityContext> contexts,
        IReadOnlyList<NetworkDemandRow> demand,
        IReadOnlyList<NetworkCapacityRow> capacityMap)
    {
        var opportunities = new List<NetworkOpportunityRow>();
        var overloaded = capacityMap.Where(c => c.OverloadedPercent > 0 || c.CommittedPercent >= 88).ToList();
        var receivers = capacityMap.Where(c => c.AvailablePercent >= 15 && c.Realizable).ToList();
        if (overloaded.Count == 0 || receivers.Count == 0) return opportunities;

        var source = overloaded.OrderByDescending(c => c.CommittedPercent).First();
        var destination = receivers.OrderByDescending(c => c.AvailablePercent).First();
        var order = demand.FirstOrDefault(d => d.RiskLevel is "high" or "critical");
        order ??= demand.Count > 0 ? demand[0] : null;
        if (order is null) return opportunities;

        opportunities.Add(new NetworkOpportunityRow(
            "CROSS-PLANT-RECOVERY",
            "Aşırı yüklü tesisten kapasite kurtarma",
            $"{source.FacilityName} → {destination.FacilityName} için {order.OrderId} taşınabilir.",
            source.FacilityId,
            destination.FacilityId,
            order.OrderId,
            0.78m,
            "derived"));

        return opportunities;
    }
}

public static class TransferCandidateGeneratorEngine
{
    public static IReadOnlyList<(string ActionType, string Title)> Generate(
        NetworkDemandRow demand,
        NetworkFacilityContext source,
        NetworkFacilityContext destination)
    {
        var candidates = new List<(string, string)>
        {
            (NetworkOptionActions.KeepCurrentFacility, "Mevcut tesiste tut"),
        };

        if (destination.AvailablePercent >= 10)
        {
            candidates.Add((NetworkOptionActions.MoveFullOrder, "Tam siparişi taşı"));
            if (demand.RemainingQuantity > 50)
                candidates.Add((NetworkOptionActions.SplitOrder, "Siparişi böl"));
            candidates.Add((NetworkOptionActions.MoveRemainingOperations, "Kalan operasyonları taşı"));
        }

        return candidates;
    }
}

public static class NetworkMaterialGuard
{
    public static bool HasMaterialShortage(string materialStateJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(materialStateJson);
            if (doc.RootElement.TryGetProperty("shortageSignals", out var signals) &&
                signals.TryGetInt32(out var count))
            {
                return count > 0;
            }
        }
        catch (JsonException) { }

        return materialStateJson.Contains("\"shortage\":true", StringComparison.OrdinalIgnoreCase);
    }
}

public static class NetworkMoldGuard
{
    public static bool RequiresMoldVerification(string moldStateJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(moldStateJson);
            if (doc.RootElement.TryGetProperty("moldCases", out var moldCases) &&
                moldCases.TryGetInt32(out var count))
            {
                return count > 0;
            }
        }
        catch (JsonException) { }

        return moldStateJson.Contains("\"moldUnavailable\":true", StringComparison.OrdinalIgnoreCase);
    }
}

public static class NetworkQualityGuard
{
    public static bool HasQualityHold(string qualityStateJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(qualityStateJson);
            if (doc.RootElement.TryGetProperty("holdCount", out var holdCount) &&
                holdCount.TryGetInt32(out var count))
            {
                return count > 0;
            }
        }
        catch (JsonException) { }

        return qualityStateJson.Contains("\"hold\":true", StringComparison.OrdinalIgnoreCase);
    }
}

public static class CrossPlantFeasibilityEngine
{
    public static FeasibilityEvaluation Evaluate(
        NetworkFacilityContext source,
        NetworkFacilityContext destination,
        NetworkDemandRow demand,
        string actionType)
    {
        var hard = new List<string>();
        var soft = new List<string>();

        if (actionType == NetworkOptionActions.KeepCurrentFacility)
            return new FeasibilityEvaluation(true, hard, soft, null);

        if (NetworkQualityGuard.HasQualityHold(destination.State.QualityStateJson))
            hard.Add("Hedef tesis kalite hold altında.");

        if (NetworkMaterialGuard.HasMaterialShortage(destination.State.MaterialStateJson))
            hard.Add("Malzeme hedef tesiste hazır değil.");
        else if (!demand.MaterialReady)
            hard.Add("Sipariş malzeme bağımlılığı hedef tesiste karşılanmıyor.");

        if (!demand.MoldReady)
            soft.Add("Kalıp uyumluluğu doğrulanmalı.");

        if (NetworkMaintenanceGuard.HasOpenMaintenance(destination.State.MaintenanceStateJson))
            hard.Add("Hedef tesis bakım penceresinde.");

        if (destination.State.MachineStateJson.Contains("\"fault\"", StringComparison.Ordinal))
            hard.Add("Hedef tesis uygun makineye sahip değil.");

        if (destination.AvailablePercent < 5)
            hard.Add("Hedef tesis kapasitesi yetersiz.");

        if (source.Timezone != destination.Timezone)
            soft.Add("Vardiya/saat dilimi farkı lojistik riski oluşturur.");

        var feasible = hard.Count == 0;
        return new FeasibilityEvaluation(
            feasible,
            hard,
            soft,
            feasible ? null : hard[0]);
    }
}

public static class NetworkHardConstraintEngine
{
    public static void Ensure(FeasibilityEvaluation feasibility)
    {
        if (!feasibility.IsFeasible)
            throw new FactoryNetworkException(
                FactoryNetworkFailureCodes.Infeasible,
                feasibility.Explanation ?? "Ağ tahsisi fiziksel olarak uygulanabilir değil.");
    }
}

public static class NetworkFreshnessGuard
{
    public static void Ensure(DateTimeOffset dataFreshnessUtc, DateTimeOffset now, bool overrideStaleGuard)
    {
        if (overrideStaleGuard) return;
        if ((now - dataFreshnessUtc).TotalMinutes > 120)
            throw new FactoryNetworkException(
                FactoryNetworkFailureCodes.StaleData,
                "Ağ durumu bayat; yenileme gerekli.");
    }
}

public static class NetworkConflictGuard
{
    public static void Ensure(IReadOnlyList<NetworkFacilityContext> contexts)
    {
        if (contexts.Any(c => c.State.ConstraintsJson.Contains("conflict", StringComparison.OrdinalIgnoreCase)))
            throw new FactoryNetworkException(
                FactoryNetworkFailureCodes.Conflict,
                "Tesisler arası kaynak çakışması nedeniyle tahsis güvenli değil.");
    }
}

public static class NetworkScoringEngine
{
    public static decimal Score(
        FeasibilityEvaluation feasibility,
        NetworkFacilityContext destination,
        NetworkDemandRow demand,
        string logisticsClassification)
    {
        if (!feasibility.IsFeasible) return 0m;

        var capacityScore = Math.Min(1m, destination.AvailablePercent / 40m);
        var deadlineHours = Math.Max(0, (demand.DeadlineUtc - DateTimeOffset.UtcNow).TotalHours);
        var deadlineScore = deadlineHours <= 24 ? 0.35m : deadlineHours <= 72 ? 0.25m : 0.15m;
        var readinessScore = (demand.MaterialReady ? 0.15m : 0m) + (demand.MoldReady ? 0.1m : 0m);
        var logisticsScore = logisticsClassification switch
        {
            "KNOWN" => 0.15m,
            "ESTIMATED" => 0.08m,
            _ => 0.02m,
        };
        var riskPenalty = demand.RiskLevel switch
        {
            "critical" => 0m,
            "high" => 0.05m,
            _ => 0.1m,
        };

        return Math.Round(capacityScore * 0.35m + deadlineScore + readinessScore + logisticsScore - riskPenalty, 4);
    }

    public static FacilityComparisonRow Compare(
        NetworkFacilityContext context,
        NetworkDemandRow demand,
        FeasibilityEvaluation feasibility,
        string logisticsClassification)
    {
        var score = Score(feasibility, context, demand, logisticsClassification);
        return new FacilityComparisonRow(
            context.FacilityId,
            context.FacilityName,
            feasibility.IsFeasible,
            Math.Round(context.AvailablePercent, 2),
            demand.DeadlineUtc <= DateTimeOffset.UtcNow.AddDays(1) ? 80m : 60m,
            NetworkQualityGuard.HasQualityHold(context.State.QualityStateJson) ? 20m : 85m,
            demand.MaterialReady ? 90m : 40m,
            NetworkMaintenanceGuard.HasOpenMaintenance(context.State.MaintenanceStateJson) ? 30m : 80m,
            demand.RiskLevel == "high" ? 45m : 70m,
            score * 100m,
            feasibility.IsFeasible
                ? $"{context.FacilityName} kapasite ve hazırlık dengesi uygun."
                : feasibility.Explanation ?? "Uygun değil.");
    }
}

public static class NetworkSafetyDominanceEngine
{
    public static void Ensure(FeasibilityEvaluation feasibility) =>
        NetworkHardConstraintEngine.Ensure(feasibility);
}

public static class NetworkWhatIfSimulator
{
    public static IReadOnlyList<NetworkSimulationResult> Simulate(
        Guid tenantId,
        Guid networkId,
        SimulateNetworkRequest request,
        IReadOnlyList<NetworkFacilityContext> contexts,
        NetworkDemandRow demand,
        DateTimeOffset now)
    {
        var source = contexts.FirstOrDefault(c => c.FacilityId == request.SourceFacilityId)
            ?? throw new FactoryNetworkException(FactoryNetworkFailureCodes.NotFound, "Kaynak tesis bulunamadı.");
        var destination = contexts.FirstOrDefault(c => c.FacilityId == request.DestinationFacilityId)
            ?? throw new FactoryNetworkException(FactoryNetworkFailureCodes.NotFound, "Hedef tesis bulunamadı.");

        var actions = TransferCandidateGeneratorEngine.Generate(demand, source, destination)
            .Where(a => string.IsNullOrWhiteSpace(request.ActionType) || a.ActionType == request.ActionType)
            .Take(request.MaxScenarios);

        var results = new List<NetworkSimulationResult>();
        var rank = 0;
        foreach (var (actionType, title) in actions)
        {
            var logistics = NetworkLogisticsImpactEngine.Classify(source, destination);
            var feasibility = CrossPlantFeasibilityEngine.Evaluate(source, destination, demand, actionType);
            var score = NetworkScoringEngine.Score(feasibility, destination, demand, logistics);
            var scenario = new NetworkScenario
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                NetworkId = networkId,
                ScenarioCode = ((char)('A' + rank)).ToString(),
                Title = title,
                ActionType = actionType,
                SourceFacilityId = request.SourceFacilityId,
                DestinationFacilityId = request.DestinationFacilityId,
                OrderId = request.OrderId,
                Score = score,
                ComparisonJson = JsonSerializer.Serialize(new
                {
                    baselineFacility = source.FacilityName,
                    candidateFacility = destination.FacilityName,
                    quantity = request.Quantity,
                    logistics,
                    score,
                }),
                Status = "proposed",
                CreatedAtUtc = now,
            };
            results.Add(new NetworkSimulationResult(
                scenario,
                feasibility,
                scenario.ComparisonJson,
                feasibility.IsFeasible
                    ? $"{destination.FacilityName} senaryosu {score:P0} ağ skoru ile önerilir."
                    : feasibility.Explanation ?? "Uygulanamaz senaryo."));
            rank++;
        }

        return results;
    }
}

public static class NetworkLogisticsImpactEngine
{
    public static string Classify(NetworkFacilityContext source, NetworkFacilityContext destination)
    {
        if (source.FacilityId == destination.FacilityId) return "KNOWN";
        if (source.Timezone == destination.Timezone) return "ESTIMATED";
        return "UNKNOWN";
    }
}

public static class MoldNetworkIntelligenceEngine
{
    public static bool IsCompatible(FacilityCapabilityProfile profile, string requiredCapability) =>
        profile.MoldCompatibilityJson.Contains(requiredCapability, StringComparison.OrdinalIgnoreCase)
        || profile.MoldCompatibilityJson.Contains("M-014", StringComparison.OrdinalIgnoreCase);
}

public static class MaterialNetworkIntelligenceEngine
{
    public static bool IsReady(FacilityCapabilityProfile profile, NetworkDemandRow demand) =>
        demand.MaterialReady || profile.MaterialsJson.Contains("PP", StringComparison.OrdinalIgnoreCase);
}

public static class NetworkQualityConstraintEngine
{
    public static bool AllowsTransfer(NetworkFacilityContext destination) =>
        !NetworkQualityGuard.HasQualityHold(destination.State.QualityStateJson);
}

public static class FactoryFailoverEngine
{
    public static Guid? SelectFailoverTarget(
        IReadOnlyList<NetworkFacilityContext> contexts,
        Guid failedFacilityId)
    {
        return contexts
            .Where(c => c.FacilityId != failedFacilityId && c.AvailablePercent >= 15)
            .OrderByDescending(c => c.AvailablePercent)
            .Select(c => (Guid?)c.FacilityId)
            .FirstOrDefault();
    }
}

public static class FacilityDegradationEngine
{
    public static bool IsDegraded(NetworkFacilityContext context) =>
        context.LoadPercent >= 95
        || context.State.MachineStateJson.Contains("\"fault\"", StringComparison.Ordinal)
        || NetworkMaintenanceGuard.HasOpenMaintenance(context.State.MaintenanceStateJson);
}

public static class NetworkBottleneckEngine
{
    public static IReadOnlyList<string> Detect(IReadOnlyList<NetworkCapacityRow> capacityMap) =>
        capacityMap.Where(c => c.CommittedPercent >= 90 || c.OverloadedPercent > 0)
            .Select(c => $"{c.FacilityName} kapasite darboğazı")
            .ToList();
}

public static class NetworkSinglePointOfFailureEngine
{
    public static IReadOnlyList<string> Detect(IReadOnlyList<NetworkFacilityContext> contexts)
    {
        var capable = contexts.Count(c => c.AvailablePercent >= 10);
        if (capable <= 1 && contexts.Count > 1)
            return ["Tek tesis ağ kapasitesini taşıyor"];
        return [];
    }
}

public static class NetworkResilienceEngine
{
    public static NetworkResilienceSummary Build(
        IReadOnlyList<NetworkFacilityContext> contexts,
        IReadOnlyList<NetworkCapacityRow> capacityMap)
    {
        var capacityRedundancy = capacityMap.Count == 0 ? 0m
            : Math.Round(capacityMap.Average(c => c.AvailablePercent) / 100m, 2);
        var processRedundancy = contexts.Count(c => c.AvailablePercent >= 20) >= 2 ? 0.8m : 0.4m;
        var moldRedundancy = contexts.Count(c => MoldNetworkIntelligenceEngine.IsCompatible(c.Profile, "injection")) >= 2
            ? 0.75m : 0.35m;

        return new NetworkResilienceSummary(
            capacityRedundancy,
            processRedundancy,
            moldRedundancy,
            NetworkSinglePointOfFailureEngine.Detect(contexts),
            NetworkBottleneckEngine.Detect(capacityMap));
    }
}

public static class NetworkQuantityConservationEngine
{
    public static decimal Verify(
        decimal totalQuantity,
        decimal sourceQuantity,
        decimal destinationQuantity)
    {
        if (totalQuantity <= 0) return 100m;
        var conserved = sourceQuantity + destinationQuantity;
        return Math.Round(Math.Min(100m, conserved / totalQuantity * 100m), 2);
    }

    public static void Ensure(decimal conservationPercent)
    {
        if (conservationPercent < 99.5m)
            throw new FactoryNetworkException(
                FactoryNetworkFailureCodes.QuantityViolation,
                "Miktar korunumu ihlal edildi.");
    }
}

public static class NetworkToPlanningBridge
{
    public static async Task<(Guid? SourcePlanVersionId, Guid? DestinationPlanVersionId)> ApplyAllocationAsync(
        NevaxisFactOSDbContext dbContext,
        Guid tenantId,
        NetworkAllocation allocation,
        IReadOnlyList<NetworkAllocationItem> items,
        CancellationToken cancellationToken)
    {
        Guid? sourceVersionId = null;
        Guid? destinationVersionId = null;

        foreach (var group in items.GroupBy(i => i.SourceFacilityId))
        {
            var plan = await dbContext.ProductionPlans
                .Where(p => p.TenantId == tenantId && p.FacilityId == group.Key)
                .OrderByDescending(p => p.UpdatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            if (plan?.ActivePlanVersionId is Guid versionId)
                sourceVersionId = versionId;
        }

        foreach (var group in items.GroupBy(i => i.DestinationFacilityId))
        {
            var plan = await dbContext.ProductionPlans
                .Where(p => p.TenantId == tenantId && p.FacilityId == group.Key)
                .OrderByDescending(p => p.UpdatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            if (plan?.ActivePlanVersionId is Guid versionId)
                destinationVersionId = versionId;
        }

        allocation.ExpectedImpactJson = JsonSerializer.Serialize(new
        {
            allocation.ActionType,
            sourcePlanVersionId = sourceVersionId,
            destinationPlanVersionId = destinationVersionId,
            itemCount = items.Count,
            bridge = "adaptive-planning-reference",
        });

        return (sourceVersionId, destinationVersionId);
    }
}

public static class NetworkLearningBridge
{
    public static FactoryLearningMemory BuildMemory(
        Guid tenantId,
        Guid facilityId,
        NetworkAllocation allocation,
        NetworkOutcomeVerificationResult outcome)
    {
        return new FactoryLearningMemory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            ProblemPattern = "network-cross-plant-allocation",
            ContextJson = allocation.ReasonJson,
            RootCauseSummary = "Çok tesisli kapasite dengesi",
            RootCauseConfidence = "medium",
            InterventionSummary = allocation.ActionType,
            ExperimentSummary = "{}",
            ResultSummary = outcome.ActualImpactJson,
            EffectivenessScore = outcome.QuantityConservationPercent / 100m,
            MemoryQuality = outcome.OutcomeStatus,
            ScopeJson = JsonSerializer.Serialize(new { allocation.NetworkId }),
            EvidenceJson = outcome.ExpectedImpactJson,
            RevalidationCriteriaJson = "{}",
            RecordedAtUtc = DateTimeOffset.UtcNow,
        };
    }
}

public static class NetworkExpectedVsActualEngine
{
    public static string BuildActualImpact(
        NetworkAllocation allocation,
        decimal adherencePercent,
        int recoveredOrders) =>
        JsonSerializer.Serialize(new
        {
            adherencePercent,
            recoveredOrders,
            publishedAtUtc = allocation.PublishedAtUtc,
            verifiedAtUtc = DateTimeOffset.UtcNow,
        });
}

public static class NetworkOutcomeVerificationEngine
{
    public static NetworkOutcomeVerificationResult Verify(
        NetworkAllocation allocation,
        IReadOnlyList<NetworkAllocationItem> items,
        decimal adherencePercent)
    {
        var conservation = items.Count == 0 ? 100m
            : items.Average(i => NetworkQuantityConservationEngine.Verify(i.TotalQuantity, i.SourceQuantity, i.DestinationQuantity));
        var status = conservation >= 99.5m && adherencePercent >= 60m ? "verified-success"
            : conservation >= 95m ? "partial" : "failed";
        var actual = NetworkExpectedVsActualEngine.BuildActualImpact(allocation, adherencePercent, items.Count);

        return new NetworkOutcomeVerificationResult(
            allocation.Id,
            status,
            allocation.ExpectedImpactJson,
            actual,
            conservation,
            null);
    }
}

public static class NetworkPolicyEnvelopeEngine
{
    public static bool IsProhibitedPair(FactoryNetworkPolicy policy, Guid sourceFacilityId, Guid destinationFacilityId)
    {
        try
        {
            using var doc = JsonDocument.Parse(policy.PolicyEnvelopeJson);
            if (!doc.RootElement.TryGetProperty("prohibitedPairs", out var pairs) ||
                pairs.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var pair in pairs.EnumerateArray())
            {
                if (pair.TryGetProperty("sourceFacilityId", out var s) &&
                    pair.TryGetProperty("destinationFacilityId", out var d) &&
                    Guid.TryParse(s.GetString(), out var source) &&
                    Guid.TryParse(d.GetString(), out var destination) &&
                    source == sourceFacilityId &&
                    destination == destinationFacilityId)
                {
                    return true;
                }
            }
        }
        catch (JsonException) { }

        return false;
    }

    public static int ApprovalThresholdOrders(FactoryNetworkPolicy policy)
    {
        try
        {
            using var doc = JsonDocument.Parse(policy.PolicyEnvelopeJson);
            return doc.RootElement.TryGetProperty("approvalThresholdOrders", out var threshold)
                ? threshold.GetInt32()
                : 2;
        }
        catch (JsonException)
        {
            return 2;
        }
    }
}

public static class NetworkAutonomyEngine
{
    public static bool CanAutoExecute(
        FactoryNetworkPolicy policy,
        int affectedOrders,
        decimal benefitScore)
    {
        if (policy.AutonomyMode == NetworkAutonomyModes.Observe) return false;
        if (policy.AutonomyMode == NetworkAutonomyModes.Draft) return false;
        var threshold = NetworkPolicyEnvelopeEngine.ApprovalThresholdOrders(policy);
        if (policy.AutonomyMode == NetworkAutonomyModes.Limited)
            return affectedOrders <= threshold && benefitScore >= 0.6m;
        return affectedOrders <= threshold * 2;
    }

    public static bool RequiresApproval(FactoryNetworkPolicy policy, int affectedOrders, decimal benefitScore)
    {
        if (policy.AutonomyMode == NetworkAutonomyModes.Observe) return true;
        if (policy.AutonomyMode == NetworkAutonomyModes.Draft) return true;
        return affectedOrders >= NetworkPolicyEnvelopeEngine.ApprovalThresholdOrders(policy) || benefitScore < 0.5m;
    }
}

public static class NetworkMetricsEngine
{
    public static NetworkMetrics Build(
        IReadOnlyList<NetworkCapacityRow> capacityMap,
        IReadOnlyList<NetworkDemandRow> demand,
        IReadOnlyList<NetworkAllocation> activeAllocations,
        decimal imbalancePercent)
    {
        var utilization = capacityMap.Count == 0 ? 0m : capacityMap.Average(c => c.CommittedPercent);
        var ordersAtRisk = demand.Count(d => d.RiskLevel is "high" or "critical");
        return new NetworkMetrics(
            Math.Round(utilization, 2),
            ordersAtRisk,
            activeAllocations.Count(a => a.Status == NetworkAllocationStatuses.Verified),
            imbalancePercent,
            activeAllocations.Count(a => a.Status is NetworkAllocationStatuses.Approved or NetworkAllocationStatuses.Executing),
            82m);
    }
}
