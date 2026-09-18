using System.Globalization;

namespace NevaxisZentra.Persistence.Energy;

public static class EnergyCalculator
{
    private static readonly MidpointRounding Rounding = MidpointRounding.AwayFromZero;

    public static EnergyConsumptionCalculationResult CalculateConsumption(
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        IReadOnlyList<EnergyReadingInput> readings,
        EnergyTariffSnapshot? tariff)
    {
        if (windowEndUtc <= windowStartUtc)
        {
            return EnergyConsumptionCalculationResult.InvalidWindow(
                "Window end must be after window start.");
        }

        var windowReadings = readings
            .Where(r => r.OccurredAtUtc >= windowStartUtc && r.OccurredAtUtc <= windowEndUtc)
            .OrderBy(r => r.OccurredAtUtc)
            .ThenBy(r => r.ReadingId)
            .ToList();

        if (windowReadings.Count == 0)
        {
            return new EnergyConsumptionCalculationResult(
                0m,
                null,
                null,
                EnergyCalculationMethods.Unavailable,
                EnergyEvidenceStates.InsufficientData,
                0,
                EnergyFreshnessStates.Unknown,
                [],
                "No readings available for the requested window.",
                null,
                null,
                null);
        }

        var intervalReadings = windowReadings
            .Where(r => r.IntervalEnergyKwh is > 0)
            .ToList();
        var cumulativeReadings = windowReadings
            .Where(r => r.CumulativeEnergyKwh.HasValue)
            .ToList();

        decimal consumedKwh = 0m;
        string calculationMethod;
        string evidenceStatus;
        string? missingExplanation = null;
        decimal? maxDemand = null;

        if (intervalReadings.Count > 0 && cumulativeReadings.Count == 0)
        {
            consumedKwh = RoundEnergy(intervalReadings.Sum(r => r.IntervalEnergyKwh!.Value));
            calculationMethod = EnergyCalculationMethods.IntervalSum;
            evidenceStatus = ResolveAggregateEvidence(intervalReadings.Select(r => r.EvidenceState));
        }
        else if (cumulativeReadings.Count > 0)
        {
            var deltaResult = CalculateCumulativeDelta(cumulativeReadings);
            consumedKwh = deltaResult.ConsumedKwh;
            calculationMethod = intervalReadings.Count > 0
                ? EnergyCalculationMethods.Mixed
                : EnergyCalculationMethods.CumulativeDelta;
            evidenceStatus = deltaResult.EvidenceStatus;
            missingExplanation = deltaResult.MissingExplanation;
        }
        else
        {
            return new EnergyConsumptionCalculationResult(
                0m,
                null,
                null,
                EnergyCalculationMethods.Unavailable,
                EnergyEvidenceStates.InsufficientData,
                windowReadings.Count,
                ResolveFreshness(windowReadings),
                windowReadings.Select(r => r.SourceReference ?? r.ReadingId.ToString()).Distinct().ToList(),
                "Readings lack valid interval or cumulative energy values.",
                null,
                null,
                null);
        }

        maxDemand = windowReadings
            .Where(r => r.MaximumDemandKw.HasValue)
            .Select(r => r.MaximumDemandKw!.Value)
            .DefaultIfEmpty()
            .Max();
        if (maxDemand == 0m)
        {
            maxDemand = null;
        }

        decimal? averagePower = null;
        var durationHours = (decimal)(windowEndUtc - windowStartUtc).TotalHours;
        if (durationHours > 0 && consumedKwh > 0)
        {
            averagePower = RoundEnergy(consumedKwh / durationHours);
        }

        var costResult = CalculateCost(consumedKwh, maxDemand, tariff);

        return new EnergyConsumptionCalculationResult(
            consumedKwh,
            averagePower,
            maxDemand,
            calculationMethod,
            evidenceStatus,
            windowReadings.Count,
            ResolveFreshness(windowReadings),
            windowReadings.Select(r => r.SourceReference ?? r.ReadingId.ToString()).Distinct().ToList(),
            missingExplanation,
            costResult.CostAmount,
            costResult.CurrencyCode,
            tariff?.Id);
    }

    public static EnergyIntensityMetricsResult CalculateIntensityMetrics(
        decimal consumedKwh,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        EnergyProductionEvidence? productionEvidence)
    {
        var metrics = new List<EnergyIntensityMetricReadModel>();

        if (productionEvidence is null)
        {
            metrics.Add(UnavailableMetric("kwh-per-kg", "Production evidence not available for window."));
            metrics.Add(UnavailableMetric("kwh-per-1000-total-parts", "Production evidence not available for window."));
            metrics.Add(UnavailableMetric("kwh-per-1000-good-parts", "Production evidence not available for window."));
            return new EnergyIntensityMetricsResult(metrics);
        }

        if (productionEvidence.WindowStartUtc != windowStartUtc || productionEvidence.WindowEndUtc != windowEndUtc)
        {
            metrics.Add(UnavailableMetric("kwh-per-kg", "Production evidence window does not match reporting window."));
            metrics.Add(UnavailableMetric("kwh-per-1000-total-parts", "Production evidence window does not match reporting window."));
            metrics.Add(UnavailableMetric("kwh-per-1000-good-parts", "Production evidence window does not match reporting window."));
            return new EnergyIntensityMetricsResult(metrics);
        }

        if (productionEvidence.TotalMassKg is > 0)
        {
            metrics.Add(new EnergyIntensityMetricReadModel(
                "kwh-per-kg",
                RoundEnergy(consumedKwh / productionEvidence.TotalMassKg.Value),
                "kWh/kg",
                true,
                null));
        }
        else
        {
            metrics.Add(UnavailableMetric("kwh-per-kg", "Total mass denominator is not positive or not available."));
        }

        if (productionEvidence.TotalParts is > 0)
        {
            metrics.Add(new EnergyIntensityMetricReadModel(
                "kwh-per-1000-total-parts",
                RoundEnergy(consumedKwh / productionEvidence.TotalParts.Value * 1000m),
                "kWh/1000 parts",
                true,
                null));
        }
        else
        {
            metrics.Add(UnavailableMetric("kwh-per-1000-total-parts", "Total parts denominator is not positive or not available."));
        }

        if (productionEvidence.GoodParts is > 0)
        {
            metrics.Add(new EnergyIntensityMetricReadModel(
                "kwh-per-1000-good-parts",
                RoundEnergy(consumedKwh / productionEvidence.GoodParts.Value * 1000m),
                "kWh/1000 good parts",
                true,
                null));
        }
        else
        {
            metrics.Add(UnavailableMetric("kwh-per-1000-good-parts", "Good parts denominator is not positive or not available."));
        }

        return new EnergyIntensityMetricsResult(metrics);
    }

    public static EnergyAllocationConservationResult ValidateAllocations(
        decimal consumedKwh,
        IReadOnlyList<EnergyAllocationInput> allocations)
    {
        var allocatedSum = RoundEnergy(allocations.Sum(a => a.AllocatedKwh));
        var residual = RoundEnergy(consumedKwh - allocatedSum);
        var hasResidual = allocations.Any(a => a.TargetType == EnergyAllocationTargetTypes.Unallocated);

        if (allocatedSum > consumedKwh)
        {
            return new EnergyAllocationConservationResult(
                false,
                allocatedSum,
                residual,
                "Allocated kWh exceeds window consumption.");
        }

        if (!hasResidual && residual != 0m)
        {
            return new EnergyAllocationConservationResult(
                false,
                allocatedSum,
                residual,
                "Allocation total does not reconcile with window consumption; residual unallocated kWh required.");
        }

        if (hasResidual && residual < 0m)
        {
            return new EnergyAllocationConservationResult(
                false,
                allocatedSum,
                residual,
                "Residual unallocated kWh cannot be negative.");
        }

        return new EnergyAllocationConservationResult(true, allocatedSum, residual, null);
    }

    public static EnergyCostResult CalculateCost(decimal consumedKwh, decimal? maximumDemandKw, EnergyTariffSnapshot? tariff)
    {
        if (tariff is null)
        {
            return new EnergyCostResult(null, null);
        }

        var energyCost = RoundCost(consumedKwh * tariff.EnergyUnitPrice);
        decimal? demandCost = null;

        if (tariff.DemandUnitPrice.HasValue && maximumDemandKw.HasValue)
        {
            demandCost = RoundCost(maximumDemandKw.Value * tariff.DemandUnitPrice.Value);
        }

        var total = energyCost + (demandCost ?? 0m);
        return new EnergyCostResult(total, tariff.CurrencyCode);
    }

    private static CumulativeDeltaResult CalculateCumulativeDelta(IReadOnlyList<EnergyReadingInput> cumulativeReadings)
    {
        var deduped = new List<EnergyReadingInput>();
        DateTimeOffset? lastTimestamp = null;
        foreach (var reading in cumulativeReadings)
        {
            if (lastTimestamp.HasValue && reading.OccurredAtUtc == lastTimestamp.Value)
            {
                continue;
            }

            deduped.Add(reading);
            lastTimestamp = reading.OccurredAtUtc;
        }

        if (deduped.Count < 2)
        {
            return new CumulativeDeltaResult(
                0m,
                EnergyEvidenceStates.InsufficientData,
                "At least two cumulative readings are required for delta calculation.");
        }

        decimal totalDelta = 0m;
        var hasConflict = false;
        var hasInsufficient = false;

        for (var i = 1; i < deduped.Count; i++)
        {
            var prev = deduped[i - 1].CumulativeEnergyKwh!.Value;
            var curr = deduped[i].CumulativeEnergyKwh!.Value;
            var delta = curr - prev;

            if (delta < 0m)
            {
                if (curr < prev * 0.1m)
                {
                    hasConflict = true;
                }
                else
                {
                    hasInsufficient = true;
                }
                continue;
            }

            totalDelta += delta;
        }

        var evidence = EnergyEvidenceStates.Calculated;
        string? explanation = null;

        if (hasConflict)
        {
            evidence = EnergyEvidenceStates.SourceConflict;
            explanation = "Counter reset or rollover detected without safe evidence.";
        }
        else if (hasInsufficient)
        {
            evidence = EnergyEvidenceStates.InsufficientData;
            explanation = "Negative counter delta detected without rollover evidence.";
        }

        evidence = ResolveAggregateEvidence(deduped.Select(r => r.EvidenceState).Concat([evidence]));

        return new CumulativeDeltaResult(RoundEnergy(totalDelta), evidence, explanation);
    }

    private static string ResolveAggregateEvidence(IEnumerable<string> states)
    {
        var ordered = new[]
        {
            EnergyEvidenceStates.SourceConflict,
            EnergyEvidenceStates.InsufficientData,
            EnergyEvidenceStates.Simulated,
            EnergyEvidenceStates.Estimated,
            EnergyEvidenceStates.Calculated,
            EnergyEvidenceStates.Measured,
        };

        foreach (var priority in ordered)
        {
            if (states.Any(s => string.Equals(s, priority, StringComparison.OrdinalIgnoreCase)))
            {
                return priority;
            }
        }

        return EnergyEvidenceStates.InsufficientData;
    }

    private static string ResolveFreshness(IReadOnlyList<EnergyReadingInput> readings)
    {
        if (readings.Any(r => r.FreshnessState == EnergyFreshnessStates.Stale))
        {
            return EnergyFreshnessStates.Stale;
        }

        if (readings.All(r => r.FreshnessState == EnergyFreshnessStates.Fresh))
        {
            return EnergyFreshnessStates.Fresh;
        }

        return EnergyFreshnessStates.Unknown;
    }

    private static EnergyIntensityMetricReadModel UnavailableMetric(string code, string reason) =>
        new(code, null, null, false, reason);

    public static decimal RoundEnergy(decimal value) =>
        Math.Round(value, EnergyConstants.EnergyDecimalPlaces, Rounding);

    public static decimal RoundCost(decimal value) =>
        Math.Round(value, 2, Rounding);

    private sealed record CumulativeDeltaResult(decimal ConsumedKwh, string EvidenceStatus, string? MissingExplanation);
}

public sealed record EnergyReadingInput(
    Guid ReadingId,
    DateTimeOffset OccurredAtUtc,
    decimal? CumulativeEnergyKwh,
    decimal? IntervalEnergyKwh,
    decimal? MaximumDemandKw,
    string EvidenceState,
    string FreshnessState,
    string? SourceReference);

public sealed record EnergyConsumptionCalculationResult(
    decimal ConsumedKwh,
    decimal? AveragePowerKw,
    decimal? MaximumDemandKw,
    string CalculationMethod,
    string EvidenceStatus,
    int ReadingCount,
    string FreshnessState,
    IReadOnlyList<string> SourceReferences,
    string? MissingDataExplanation,
    decimal? CostAmount,
    string? CostCurrencyCode,
    Guid? TariffSnapshotId)
{
    public static EnergyConsumptionCalculationResult InvalidWindow(string reason) =>
        new(
            0m,
            null,
            null,
            EnergyCalculationMethods.Unavailable,
            EnergyEvidenceStates.InsufficientData,
            0,
            EnergyFreshnessStates.Unknown,
            [],
            reason,
            null,
            null,
            null);
}

public sealed record EnergyProductionEvidence(
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    decimal? TotalMassKg,
    int? TotalParts,
    int? GoodParts);

public sealed record EnergyIntensityMetricReadModel(
    string MetricCode,
    decimal? Value,
    string? Unit,
    bool IsAvailable,
    string? UnavailableReason);

public sealed record EnergyIntensityMetricsResult(IReadOnlyList<EnergyIntensityMetricReadModel> Metrics);

public sealed record EnergyAllocationInput(
    string TargetType,
    string? TargetReference,
    decimal AllocatedKwh,
    string AllocationMethod,
    string EvidenceSource,
    string ConfidenceStatus);

public sealed record EnergyAllocationConservationResult(
    bool IsConserved,
    decimal AllocatedSumKwh,
    decimal ResidualKwh,
    string? FailureReason);

public sealed record EnergyCostResult(decimal? CostAmount, string? CurrencyCode);
