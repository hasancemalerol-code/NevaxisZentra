using System.Text.Json;
using NevaxisFactOS.Persistence.Integration;
using NevaxisFactOS.Persistence.MixedProduction;
using NevaxisFactOS.Persistence.ThroughputIntelligence;
using NevaxisFactOS.Persistence.ThroughputOptimizer;

namespace NevaxisFactOS.Persistence.RealFactoryPilot;

public static class RealFactoryDiscoveryEngine
{
    public static IReadOnlyList<FactoryDiscoveryField> DiscoverPlcFields() =>
    [
        new("PLC.M04.RunState", "bool", "RUN", MixedProductionEventTypes.Run),
        new("PLC.M04.CycleCounter", "int", "CYCLE_END", MixedProductionEventTypes.CycleEnd),
        new("PLC.M04.GoodPulse", "bool", "GOOD", MixedProductionEventTypes.Good),
        new("PLC.M04.StopReason", "string", "STOP", MixedProductionEventTypes.Stop),
        new("BUF03.Count", "int", "BUFFER_LEVEL", MixedProductionEventTypes.BufferChange),
    ];

    public static IReadOnlyList<FactoryDiscoveryField> DiscoverMesFields() =>
    [
        new("MES.GoodQty", "decimal", "GOOD", MixedProductionEventTypes.Good),
        new("MES.RejectQty", "decimal", "REJECT", MixedProductionEventTypes.Reject),
        new("MES.OperatorStation07.Complete", "bool", "OPERATION_COMPLETE", MixedProductionEventTypes.OperatorComplete),
        new("MES.MaterialWaitFlag", "bool", "MATERIAL_WAIT", MixedProductionEventTypes.MaterialWait),
        new("MES.OrderRef", "string", null, null),
    ];

    public static string ToJson(IReadOnlyList<FactoryDiscoveryField> fields) =>
        JsonSerializer.Serialize(fields);
}

public static class RealFactoryFieldMappingEngine
{
    public static IReadOnlyList<FactoryFieldMappingRule> DefaultPlcMappings() =>
    [
        new("PLC.M04.RunState", "bool", "resource", "WELD-01", MixedProductionEventTypes.Run, null, null, null, null, "source"),
        new("PLC.M04.CycleCounter", "int", "resource", "WELD-01", MixedProductionEventTypes.CycleEnd, null, null, null, null, "source"),
        new("PLC.M04.GoodPulse", "bool", "resource", "WELD-01", MixedProductionEventTypes.Good, null, "pcs", 1m, null, "source"),
        new("BUF03.Count", "int", "buffer", "BUF-20", MixedProductionEventTypes.BufferChange, null, "pcs", 1m, null, "source"),
    ];

    public static IReadOnlyList<FactoryFieldMappingRule> DefaultMesMappings() =>
    [
        new("MES.GoodQty", "decimal", "station", "ST-30", MixedProductionEventTypes.Good, null, "pcs", 1m, null, "source"),
        new("MES.RejectQty", "decimal", "station", "ST-50", MixedProductionEventTypes.Reject, null, "pcs", 1m, null, "source"),
        new("MES.OperatorStation07.Complete", "bool", "station", "ST-30", MixedProductionEventTypes.OperatorComplete, null, null, null, null, "source"),
        new("MES.MaterialWaitFlag", "bool", "station", "ST-30", MixedProductionEventTypes.MaterialWait, null, null, null, null, "source"),
    ];

    public static (bool IsValid, IReadOnlyList<string> Errors) Validate(
        IReadOnlyList<FactoryFieldMappingRule> mappings)
    {
        var errors = new List<string>();
        foreach (var mapping in mappings)
        {
            if (string.IsNullOrWhiteSpace(mapping.SourceField))
            {
                errors.Add("source-field-required");
            }

            if (string.IsNullOrWhiteSpace(mapping.CanonicalEventType))
            {
                errors.Add($"canonical-event-type-required:{mapping.SourceField}");
            }

            if (mapping.CanonicalEventType.Contains("plc.write", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"unsafe-mapping-blocked:{mapping.SourceField}");
            }
        }

        return (errors.Count == 0, errors);
    }

    public static string BuildIdempotencyKey(
        Guid tenantId, Guid facilityId, Guid connectorId, string sourceField, string eventType, string sourceSequence) =>
        $"rfip:{tenantId:N}:{facilityId:N}:{connectorId:N}:{sourceField}:{eventType}:{sourceSequence}";
}

public static class RealFactoryDataQualityEngine
{
    public static FactoryDataQualityAssessmentResult Assess(
        IntegrationConnector? plcConnector,
        IntegrationConnector? mesConnector,
        int canonicalEventCount,
        DateTimeOffset? lastEventAtUtc,
        int duplicateCount,
        int mappingFailureCount,
        DateTimeOffset now)
    {
        var issues = new List<string>();
        var completeness = canonicalEventCount >= 20 ? 1m : canonicalEventCount / 20m;
        var freshness = 1m;
        if (lastEventAtUtc.HasValue)
        {
            var ageMinutes = (now - lastEventAtUtc.Value).TotalMinutes;
            freshness = ageMinutes <= 15 ? 1m : ageMinutes <= 60 ? 0.7m : 0.2m;
            if (ageMinutes > 60)
            {
                issues.Add("stale-data");
            }
        }
        else
        {
            freshness = 0m;
            issues.Add("no-events");
        }

        var consistency = duplicateCount == 0 ? 1m : Math.Max(0m, 1m - duplicateCount * 0.2m);
        if (duplicateCount > 0)
        {
            issues.Add("duplicate-replay-detected");
        }

        var validity = mappingFailureCount == 0 ? 1m : 0m;
        if (mappingFailureCount > 0)
        {
            issues.Add("mapping-failures");
        }

        var plcHealth = plcConnector?.HealthStatus ?? IntegrationHealthStatuses.Unknown;
        var mesHealth = mesConnector?.HealthStatus ?? IntegrationHealthStatuses.Unknown;
        if (plcHealth is IntegrationHealthStatuses.Offline or IntegrationHealthStatuses.Stale)
        {
            issues.Add("plc-stale-or-offline");
        }

        if (mesHealth is IntegrationHealthStatuses.Offline or IntegrationHealthStatuses.Stale)
        {
            issues.Add("mes-stale-or-offline");
        }

        var sequenceIntegrity = duplicateCount == 0 ? 1m : 0.6m;
        var clockIntegrity = freshness;
        var mappingConfidence = mappingFailureCount == 0 ? 0.95m : 0.4m;

        var overallScore = (completeness + freshness + consistency + validity + sequenceIntegrity + clockIntegrity + mappingConfidence) / 7m;
        var state = overallScore >= 0.85m
            ? RealFactoryDataQualityStates.Good
            : overallScore >= 0.6m
                ? RealFactoryDataQualityStates.Degraded
                : overallScore >= 0.35m
                    ? RealFactoryDataQualityStates.Insufficient
                    : RealFactoryDataQualityStates.Invalid;

        var blocks = state is RealFactoryDataQualityStates.Insufficient or RealFactoryDataQualityStates.Invalid
            || issues.Contains("plc-stale-or-offline")
            || issues.Contains("mes-stale-or-offline");

        return new FactoryDataQualityAssessmentResult(
            state,
            completeness,
            freshness,
            consistency,
            validity,
            sequenceIntegrity,
            clockIntegrity,
            mappingConfidence,
            issues,
            blocks);
    }
}

public static class RealFactoryConflictResolutionEngine
{
    public static string ResolveEventType(
        IReadOnlyList<(string Source, string EventType, DateTimeOffset Timestamp)> candidates)
    {
        if (candidates.Count == 0)
        {
            return string.Empty;
        }

        var priority = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["plc"] = 3,
            ["mes"] = 2,
            ["operator"] = 1,
        };

        return candidates
            .OrderByDescending(c => priority.GetValueOrDefault(c.Source, 0))
            .ThenByDescending(c => c.Timestamp)
            .First()
            .EventType;
    }
}

public static class RealFactoryActionSafetyEngine
{
    public static string Classify(string interventionType) =>
        interventionType switch
        {
            ThroughputIntelligenceScenarioTypes.AddOperator or
                ThroughputIntelligenceScenarioTypes.MoveOperator =>
                RealFactoryActionSafetyClasses.ReversibleOperational,
            ThroughputIntelligenceScenarioTypes.ReassignResource or
                ThroughputIntelligenceScenarioTypes.RedistributeOperation or
                ThroughputIntelligenceScenarioTypes.ChangeBufferCapacity =>
                RealFactoryActionSafetyClasses.ControlledProduction,
            "plc.write" or "machine.parameter" =>
                RealFactoryActionSafetyClasses.PhysicalProcessChange,
            _ => RealFactoryActionSafetyClasses.Informational,
        };

    public static bool BlocksAutonomousExecution(string safetyClass, bool shadowBlocksPhysical) =>
        shadowBlocksPhysical && safetyClass is
            RealFactoryActionSafetyClasses.ControlledProduction or
            RealFactoryActionSafetyClasses.PhysicalProcessChange;

    public static bool IsForbiddenWithoutExplicitPolicy(string safetyClass) =>
        safetyClass == RealFactoryActionSafetyClasses.PhysicalProcessChange;
}

public static class RealFactoryShadowModeEngine
{
    public static bool ShouldBlockPhysicalActions(string mode, bool connectorStale, bool dataQualityBlocks) =>
        connectorStale || dataQualityBlocks || mode is not RealFactoryShadowModes.VerifiedOperation;

    public static FactoryShadowRecommendation BuildRecommendation(
        string constraintStation,
        decimal predictedThroughput,
        decimal deltaPercent,
        IReadOnlyList<string> evidenceRefs) =>
        new(
            "throughput-intervention",
            $"Shadow recommendation: relieve constraint at {constraintStation}",
            constraintStation,
            predictedThroughput,
            deltaPercent,
            evidenceRefs,
            DateTimeOffset.UtcNow);

    public static FactoryShadowAccuracySnapshot Compare(
        string predictedConstraint,
        string? observedConstraint,
        decimal? predictedThroughput,
        decimal? actualThroughput,
        decimal? predictedDelta,
        decimal? actualDelta)
    {
        var constraintMatched = string.Equals(predictedConstraint, observedConstraint, StringComparison.OrdinalIgnoreCase);
        var throughputWithinTolerance = predictedThroughput.HasValue && actualThroughput.HasValue
            && Math.Abs(actualThroughput.Value - predictedThroughput.Value) <= predictedThroughput.Value * 0.15m;

        return new FactoryShadowAccuracySnapshot(
            predictedConstraint,
            observedConstraint,
            predictedThroughput,
            actualThroughput,
            predictedDelta,
            actualDelta,
            constraintMatched,
            throughputWithinTolerance);
    }
}

public static class RealFactoryPilotEvidenceEngine
{
    public static string BuildPackJson(
        string facilityCode,
        string lineCode,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        FactoryDataQualityAssessmentResult dataQuality,
        decimal baselineThroughput,
        decimal? postThroughput,
        string? constraint,
        IReadOnlyList<string> interventions,
        string verificationOutcome,
        IReadOnlyList<string> auditRefs) =>
        JsonSerializer.Serialize(new
        {
            facilityCode,
            lineCode,
            pilotPeriod = new { startUtc, endUtc },
            dataSources = new[] { RealFactoryPilotFixtureCodes.PlcConnectorName, RealFactoryPilotFixtureCodes.MesConnectorName },
            dataQuality = dataQuality.OverallState,
            baselineThroughput,
            postThroughput,
            constraint,
            interventions,
            verificationOutcome,
            auditRefs,
        });
}

public static class RealFactoryTimeNormalizationEngine
{
    public static (DateTimeOffset SourceTimestamp, DateTimeOffset IngestedAtUtc, DateTimeOffset CanonicalTimestamp, IReadOnlyList<string> Issues)
        Normalize(DateTimeOffset? sourceTimestamp, DateTimeOffset ingestedAtUtc, TimeSpan? clockOffset)
    {
        var issues = new List<string>();
        var source = sourceTimestamp ?? ingestedAtUtc;
        if (sourceTimestamp is null)
        {
            issues.Add("missing-source-timestamp");
        }

        if (source > ingestedAtUtc.AddMinutes(5))
        {
            issues.Add("future-timestamp");
        }

        if (ingestedAtUtc - source > TimeSpan.FromHours(24))
        {
            issues.Add("stale-timestamp");
        }

        var canonical = source + (clockOffset ?? TimeSpan.Zero);
        return (source, ingestedAtUtc, canonical, issues);
    }
}
