using System.Globalization;

namespace NevaxisZentra.Persistence.Energy;

public static class EnergyIntelligenceCalculator
{
    private static readonly MidpointRounding Rounding = MidpointRounding.AwayFromZero;

    private static readonly string[] LeadingDriverReasonPriority = [
        EnergyIntelligenceDriverReasonCodes.SourceConflict,
        EnergyIntelligenceDriverReasonCodes.InsufficientComparableWindows,
        EnergyIntelligenceDriverReasonCodes.MissingProductionState,
        EnergyIntelligenceDriverReasonCodes.MissingProductionDenominator,
        EnergyIntelligenceDriverReasonCodes.PeakDemandExceeded,
        EnergyIntelligenceDriverReasonCodes.IdleConsumptionElevated,
        EnergyIntelligenceDriverReasonCodes.EnergyIntensityElevated,
        EnergyIntelligenceDriverReasonCodes.ConsumptionAboveTarget,
        EnergyIntelligenceDriverReasonCodes.ConsumptionAboveBaseline,
        EnergyIntelligenceDriverReasonCodes.AuxiliaryLoadElevated,
        EnergyIntelligenceDriverReasonCodes.SourceStale,
        EnergyIntelligenceDriverReasonCodes.MissingTarget,
        EnergyIntelligenceDriverReasonCodes.MissingTariff,
        EnergyIntelligenceDriverReasonCodes.WithinTarget,
    ];

    public static EnergyBaselineCalculationResult CalculateBaseline(
        IReadOnlyList<EnergyComparableWindowSample> samples,
        decimal relativeTolerance,
        bool allowSimulatedEvidence)
    {
        if (samples.Count < EnergyIntelligenceConstants.MinimumComparableWindows)
        {
            return EnergyBaselineCalculationResult.Failure(
                EnergyIntelligenceOutcomes.InsufficientData,
                EnergyIntelligenceDriverReasonCodes.InsufficientComparableWindows,
                $"At least {EnergyIntelligenceConstants.MinimumComparableWindows} comparable windows are required.");
        }

        var duration = samples[0].DurationSeconds;
        if (samples.Any(s => s.DurationSeconds != duration))
        {
            return EnergyBaselineCalculationResult.Failure(
                EnergyIntelligenceOutcomes.InsufficientData,
                EnergyIntelligenceDriverReasonCodes.InsufficientComparableWindows,
                "Comparable windows must have identical duration.");
        }

        if (samples.Any(s => s.ComparisonKey != samples[0].ComparisonKey))
        {
            return EnergyBaselineCalculationResult.Failure(
                EnergyIntelligenceOutcomes.InsufficientData,
                EnergyIntelligenceDriverReasonCodes.InsufficientComparableWindows,
                "Comparable windows must share compatible scope and context.");
        }

        if (samples.Any(s => s.EvidenceStatus == EnergyEvidenceStates.SourceConflict))
        {
            return EnergyBaselineCalculationResult.Failure(
                EnergyIntelligenceOutcomes.SourceConflict,
                EnergyIntelligenceDriverReasonCodes.SourceConflict,
                "Unresolved source conflict in comparable windows.");
        }

        if (!allowSimulatedEvidence && samples.Any(s => s.EvidenceStatus == EnergyEvidenceStates.Simulated))
        {
            return EnergyBaselineCalculationResult.Failure(
                EnergyIntelligenceOutcomes.InsufficientData,
                EnergyIntelligenceDriverReasonCodes.InsufficientComparableWindows,
                "Simulated evidence cannot be used for live baseline calculation.");
        }

        if (samples.Any(s => s.FreshnessState == EnergyFreshnessStates.Stale))
        {
            return EnergyBaselineCalculationResult.Failure(
                EnergyIntelligenceOutcomes.InsufficientData,
                EnergyIntelligenceDriverReasonCodes.SourceStale,
                "Comparable windows include stale source evidence.");
        }

        var consumedValues = samples.Select(s => s.ConsumedKwh).OrderBy(v => v).ToList();
        var median = CalculateMedian(consumedValues);
        var mad = CalculateMad(consumedValues, median);
        var normalizedDispersion = RoundEnergy(mad * EnergyIntelligenceConstants.MadScaleFactor);
        var relativeBoundary = RoundEnergy(median * (1m + relativeTolerance));
        var dispersionBoundary = RoundEnergy(median + EnergyIntelligenceConstants.SigmaMultiplier * normalizedDispersion);
        var upperThreshold = Math.Max(relativeBoundary, dispersionBoundary);

        decimal? demandMedian = null;
        decimal? demandThreshold = null;
        var demandSamples = samples.Where(s => s.MaximumDemandKw.HasValue).Select(s => s.MaximumDemandKw!.Value).ToList();
        if (demandSamples.Count >= EnergyIntelligenceConstants.MinimumComparableWindows)
        {
            demandMedian = CalculateMedian(demandSamples.OrderBy(v => v).ToList());
            var demandMad = CalculateMad(demandSamples.OrderBy(v => v).ToList(), demandMedian.Value);
            var demandNormalized = RoundEnergy(demandMad * EnergyIntelligenceConstants.MadScaleFactor);
            demandThreshold = Math.Max(
                RoundEnergy(demandMedian.Value * (1m + relativeTolerance)),
                RoundEnergy(demandMedian.Value + EnergyIntelligenceConstants.SigmaMultiplier * demandNormalized));
        }

        return new EnergyBaselineCalculationResult(
            true,
            samples.Count,
            median,
            mad,
            normalizedDispersion,
            relativeTolerance,
            upperThreshold,
            demandMedian,
            demandThreshold,
            median,
            demandMedian,
            null,
            null,
            "median-mad-robust",
            ResolveAggregateEvidence(samples.Select(s => s.EvidenceStatus)),
            samples.Select(s => s.WindowReference).Distinct().OrderBy(r => r).ToList(),
            null);
    }

    public static EnergyIntelligenceEvaluationResult EvaluateAssessment(
        EnergyComparableWindowSample actualWindow,
        EnergyBaselineProfile? baseline,
        EnergyTarget? consumptionTarget,
        EnergyTarget? demandTarget,
        EnergyTarget? idleTarget,
        EnergyTarget? intensityTarget,
        EnergyProductionStateEvidence? productionState,
        EnergyTariffSnapshot? tariff,
        bool allowSimulatedEvidence)
    {
        var drivers = new List<EnergyIntelligenceDriverDraft>();

        if (actualWindow.EvidenceStatus == EnergyEvidenceStates.SourceConflict)
        {
            drivers.Add(Driver(
                EnergyIntelligenceDriverReasonCodes.SourceConflict,
                EnergyIntelligenceDriverCategories.Evidence,
                EnergyIntelligenceDriverSeverities.Fail,
                actualWindow.ConsumedKwh.ToString(CultureInfo.InvariantCulture),
                "kWh",
                null,
                null,
                null,
                "Source conflict in evaluation window evidence."));
            return BuildResult(actualWindow, drivers, EnergyIntelligenceOutcomes.SourceConflict, tariff, consumptionTarget);
        }

        if (!allowSimulatedEvidence && actualWindow.EvidenceStatus == EnergyEvidenceStates.Simulated)
        {
            drivers.Add(Driver(
                EnergyIntelligenceDriverReasonCodes.InsufficientComparableWindows,
                EnergyIntelligenceDriverCategories.Evidence,
                EnergyIntelligenceDriverSeverities.Info,
                null, null, null, null, null,
                "Simulated evidence cannot be evaluated as live factory evidence."));
            return BuildResult(actualWindow, drivers, EnergyIntelligenceOutcomes.InsufficientData, tariff, consumptionTarget);
        }

        if (baseline is null)
        {
            drivers.Add(Driver(
                EnergyIntelligenceDriverReasonCodes.InsufficientComparableWindows,
                EnergyIntelligenceDriverCategories.Evidence,
                EnergyIntelligenceDriverSeverities.Info,
                null, null, null, null, null,
                "No comparable baseline profile is available."));
        }
        else if (actualWindow.ConsumedKwh > baseline.UpperThresholdConsumedKwh)
        {
            var variance = RoundEnergy(actualWindow.ConsumedKwh - baseline.BaselineConsumedKwh);
            var normalized = baseline.NormalizedDispersion > 0
                ? RoundEnergy((actualWindow.ConsumedKwh - baseline.MedianCenter) / baseline.NormalizedDispersion)
                : (decimal?)null;
            drivers.Add(Driver(
                EnergyIntelligenceDriverReasonCodes.ConsumptionAboveBaseline,
                EnergyIntelligenceDriverCategories.Consumption,
                EnergyIntelligenceDriverSeverities.Warn,
                FormatDecimal(actualWindow.ConsumedKwh),
                "kWh",
                FormatDecimal(baseline.BaselineConsumedKwh),
                "kWh",
                normalized,
                EnergyOpportunityLabels.AboveBaseline));
        }

        if (consumptionTarget is not null && consumptionTarget.Status == EnergyTargetStatuses.Approved)
        {
            var variance = RoundEnergy(actualWindow.ConsumedKwh - consumptionTarget.TargetValue);
            var variancePercent = consumptionTarget.TargetValue > 0
                ? RoundPercent(variance / consumptionTarget.TargetValue * 100m)
                : (decimal?)null;
            if (variance > consumptionTarget.Tolerance * consumptionTarget.TargetValue)
            {
                drivers.Add(Driver(
                    EnergyIntelligenceDriverReasonCodes.ConsumptionAboveTarget,
                    EnergyIntelligenceDriverCategories.Target,
                    EnergyIntelligenceDriverSeverities.Warn,
                    FormatDecimal(actualWindow.ConsumedKwh),
                    consumptionTarget.Unit,
                    FormatDecimal(consumptionTarget.TargetValue),
                    consumptionTarget.Unit,
                    variancePercent,
                    EnergyOpportunityLabels.AboveTarget));
            }
        }
        else if (consumptionTarget is null)
        {
            drivers.Add(Driver(
                EnergyIntelligenceDriverReasonCodes.MissingTarget,
                EnergyIntelligenceDriverCategories.Target,
                EnergyIntelligenceDriverSeverities.Info,
                null, null, null, null, null,
                "No approved consumption target configured."));
        }

        if (actualWindow.MaximumDemandKw.HasValue)
        {
            if (demandTarget is not null && demandTarget.Status == EnergyTargetStatuses.Approved)
            {
                if (actualWindow.MaximumDemandKw.Value > demandTarget.TargetValue + demandTarget.Tolerance)
                {
                    drivers.Add(Driver(
                        EnergyIntelligenceDriverReasonCodes.PeakDemandExceeded,
                        EnergyIntelligenceDriverCategories.PeakDemand,
                        EnergyIntelligenceDriverSeverities.Fail,
                        FormatDecimal(actualWindow.MaximumDemandKw.Value),
                        demandTarget.Unit,
                        FormatDecimal(demandTarget.TargetValue),
                        demandTarget.Unit,
                        RoundEnergy(actualWindow.MaximumDemandKw.Value - demandTarget.TargetValue),
                        "Peak demand exceeded approved threshold."));
                }
            }
            else if (baseline?.UpperThresholdDemandKw is decimal demandThreshold)
            {
                if (actualWindow.MaximumDemandKw.Value > demandThreshold)
                {
                    drivers.Add(Driver(
                        EnergyIntelligenceDriverReasonCodes.PeakDemandExceeded,
                        EnergyIntelligenceDriverCategories.PeakDemand,
                        EnergyIntelligenceDriverSeverities.Warn,
                        FormatDecimal(actualWindow.MaximumDemandKw.Value),
                        "kW",
                        FormatDecimal(demandThreshold),
                        "kW",
                        RoundEnergy(actualWindow.MaximumDemandKw.Value - demandThreshold),
                        "Peak demand exceeded baseline threshold."));
                }
            }
            else
            {
                drivers.Add(Driver(
                    EnergyIntelligenceDriverReasonCodes.MissingTarget,
                    EnergyIntelligenceDriverCategories.PeakDemand,
                    EnergyIntelligenceDriverSeverities.Info,
                    FormatDecimal(actualWindow.MaximumDemandKw.Value),
                    "kW",
                    null, null, null,
                    "Peak demand observed without an approved demand target or baseline threshold."));
            }
        }

        EvaluateIdleConsumption(actualWindow, idleTarget, baseline, productionState, drivers);
        EvaluateIntensity(actualWindow, intensityTarget, productionState, drivers);

        if (actualWindow.FreshnessState == EnergyFreshnessStates.Stale)
        {
            drivers.Add(Driver(
                EnergyIntelligenceDriverReasonCodes.SourceStale,
                EnergyIntelligenceDriverCategories.Evidence,
                EnergyIntelligenceDriverSeverities.Info,
                null, null, null, null, null,
                "Evaluation window evidence is stale."));
        }

        var outcome = ResolveOutcome(drivers);
        if (outcome == EnergyIntelligenceOutcomes.WithinTarget && drivers.All(d => d.Severity == EnergyIntelligenceDriverSeverities.Pass || d.Severity == EnergyIntelligenceDriverSeverities.Info))
        {
            drivers.Add(Driver(
                EnergyIntelligenceDriverReasonCodes.WithinTarget,
                EnergyIntelligenceDriverCategories.Target,
                EnergyIntelligenceDriverSeverities.Pass,
                FormatDecimal(actualWindow.ConsumedKwh),
                "kWh",
                consumptionTarget is not null ? FormatDecimal(consumptionTarget.TargetValue) : null,
                consumptionTarget?.Unit,
                null,
                "Consumption is within approved target tolerance."));
        }

        return BuildResult(actualWindow, drivers, outcome, tariff, consumptionTarget);
    }

    private static void EvaluateIdleConsumption(
        EnergyComparableWindowSample actualWindow,
        EnergyTarget? idleTarget,
        EnergyBaselineProfile? baseline,
        EnergyProductionStateEvidence? productionState,
        List<EnergyIntelligenceDriverDraft> drivers)
    {
        if (productionState is null)
        {
            drivers.Add(Driver(
                EnergyIntelligenceDriverReasonCodes.MissingProductionState,
                EnergyIntelligenceDriverCategories.Idle,
                EnergyIntelligenceDriverSeverities.Info,
                null, null, null, null, null,
                "Production state evidence is unavailable; idle consumption cannot be inferred."));
            return;
        }

        if (!productionState.WindowStartUtc.Equals(actualWindow.WindowStartUtc)
            || !productionState.WindowEndUtc.Equals(actualWindow.WindowEndUtc))
        {
            return;
        }

        if (productionState.HasActiveProduction)
        {
            return;
        }

        decimal? threshold = null;
        string? thresholdUnit = "kWh";
        if (idleTarget is not null && idleTarget.Status == EnergyTargetStatuses.Approved)
        {
            threshold = idleTarget.TargetValue + idleTarget.Tolerance;
            thresholdUnit = idleTarget.Unit;
        }
        else if (baseline is not null)
        {
            threshold = baseline.UpperThresholdConsumedKwh;
        }

        if (threshold is null)
        {
            drivers.Add(Driver(
                EnergyIntelligenceDriverReasonCodes.MissingTarget,
                EnergyIntelligenceDriverCategories.Idle,
                EnergyIntelligenceDriverSeverities.Info,
                FormatDecimal(actualWindow.ConsumedKwh),
                "kWh",
                null, null, null,
                "Idle period observed without an explicit idle target or baseline."));
            return;
        }

        if (actualWindow.ConsumedKwh > threshold.Value)
        {
            drivers.Add(Driver(
                EnergyIntelligenceDriverReasonCodes.IdleConsumptionElevated,
                EnergyIntelligenceDriverCategories.Idle,
                EnergyIntelligenceDriverSeverities.Warn,
                FormatDecimal(actualWindow.ConsumedKwh),
                thresholdUnit,
                FormatDecimal(threshold.Value),
                thresholdUnit,
                RoundEnergy(actualWindow.ConsumedKwh - threshold.Value),
                "Idle consumption exceeds allowed threshold with verified zero production."));
        }
    }

    private static void EvaluateIntensity(
        EnergyComparableWindowSample actualWindow,
        EnergyTarget? intensityTarget,
        EnergyProductionStateEvidence? productionState,
        List<EnergyIntelligenceDriverDraft> drivers)
    {
        if (intensityTarget is null || intensityTarget.Status != EnergyTargetStatuses.Approved)
        {
            return;
        }

        if (productionState is null
            || !productionState.WindowStartUtc.Equals(actualWindow.WindowStartUtc)
            || !productionState.WindowEndUtc.Equals(actualWindow.WindowEndUtc)
            || productionState.GoodParts is not > 0)
        {
            drivers.Add(Driver(
                EnergyIntelligenceDriverReasonCodes.MissingProductionDenominator,
                EnergyIntelligenceDriverCategories.Intensity,
                EnergyIntelligenceDriverSeverities.Info,
                null, null, null, null, null,
                "Energy intensity comparison requires a positive production denominator in the same window."));
            return;
        }

        if (!string.Equals(productionState.ContextReference, actualWindow.ProductionContextReference, StringComparison.Ordinal))
        {
            drivers.Add(Driver(
                EnergyIntelligenceDriverReasonCodes.MissingProductionDenominator,
                EnergyIntelligenceDriverCategories.Intensity,
                EnergyIntelligenceDriverSeverities.Info,
                null, null, null, null, null,
                "Production context is not compatible for intensity comparison."));
            return;
        }

        var actualIntensity = RoundEnergy(actualWindow.ConsumedKwh / productionState.GoodParts!.Value * 1000m);
        if (actualIntensity > intensityTarget.TargetValue + intensityTarget.Tolerance)
        {
            drivers.Add(Driver(
                EnergyIntelligenceDriverReasonCodes.EnergyIntensityElevated,
                EnergyIntelligenceDriverCategories.Intensity,
                EnergyIntelligenceDriverSeverities.Warn,
                FormatDecimal(actualIntensity),
                intensityTarget.Unit,
                FormatDecimal(intensityTarget.TargetValue),
                intensityTarget.Unit,
                RoundPercent((actualIntensity - intensityTarget.TargetValue) / intensityTarget.TargetValue * 100m),
                "Energy intensity exceeds approved target."));
        }
    }

    private static EnergyIntelligenceEvaluationResult BuildResult(
        EnergyComparableWindowSample actualWindow,
        List<EnergyIntelligenceDriverDraft> drivers,
        string outcome,
        EnergyTariffSnapshot? tariff,
        EnergyTarget? consumptionTarget)
    {
        var leading = SelectLeadingDriver(drivers, outcome);
        decimal? potentialReducible = null;
        string? opportunityLabel = null;

        if (consumptionTarget is not null
            && consumptionTarget.Status == EnergyTargetStatuses.Approved
            && actualWindow.ConsumedKwh > consumptionTarget.TargetValue)
        {
            potentialReducible = RoundEnergy(actualWindow.ConsumedKwh - consumptionTarget.TargetValue);
            opportunityLabel = EnergyOpportunityLabels.PotentialReducible;
        }
        else if (drivers.Any(d => d.ReasonCode == EnergyIntelligenceDriverReasonCodes.ConsumptionAboveBaseline))
        {
            opportunityLabel = EnergyOpportunityLabels.AboveBaseline;
        }

        decimal? costImpact = null;
        string? currency = null;
        if (potentialReducible is > 0 && tariff is not null)
        {
            costImpact = EnergyCalculator.RoundCost(potentialReducible.Value * tariff.EnergyUnitPrice);
            currency = tariff.CurrencyCode;
            opportunityLabel = EnergyOpportunityLabels.EstimatedCostImpact;
        }
        else if (potentialReducible is > 0 && tariff is null)
        {
            drivers.Add(Driver(
                EnergyIntelligenceDriverReasonCodes.MissingTariff,
                EnergyIntelligenceDriverCategories.Cost,
                EnergyIntelligenceDriverSeverities.Info,
                null, null, null, null, null,
                "Cost impact unavailable because no applicable tariff exists."));
        }

        var varianceKwh = consumptionTarget is not null
            ? RoundEnergy(actualWindow.ConsumedKwh - consumptionTarget.TargetValue)
            : (decimal?)null;
        var variancePercent = consumptionTarget is not null && consumptionTarget.TargetValue > 0
            ? RoundPercent((actualWindow.ConsumedKwh - consumptionTarget.TargetValue) / consumptionTarget.TargetValue * 100m)
            : (decimal?)null;

        return new EnergyIntelligenceEvaluationResult(
            outcome,
            leading?.ReasonCode,
            drivers,
            actualWindow.ConsumedKwh,
            actualWindow.MaximumDemandKw,
            varianceKwh,
            variancePercent,
            potentialReducible,
            costImpact,
            currency,
            opportunityLabel,
            actualWindow.EvidenceStatus,
            actualWindow.FreshnessState);
    }

    public static EnergyIntelligenceDriverDraft? SelectLeadingDriver(
        IReadOnlyList<EnergyIntelligenceDriverDraft> drivers,
        string outcome)
    {
        if (outcome == EnergyIntelligenceOutcomes.WithinTarget)
        {
            return drivers.FirstOrDefault(d => d.ReasonCode == EnergyIntelligenceDriverReasonCodes.WithinTarget)
                ?? drivers.OrderByDescending(d => SeverityRank(d.Severity))
                    .ThenByDescending(d => d.NormalizedVariance ?? 0m)
                    .ThenBy(d => ReasonOrder(d.ReasonCode))
                    .FirstOrDefault();
        }

        var ranked = drivers
            .OrderByDescending(d => SeverityRank(d.Severity))
            .ThenByDescending(d => d.NormalizedVariance ?? 0m)
            .ThenBy(d => ReasonOrder(d.ReasonCode))
            .ToList();

        return ranked.FirstOrDefault();
    }

    private static string ResolveOutcome(IReadOnlyList<EnergyIntelligenceDriverDraft> drivers)
    {
        if (drivers.Any(d => d.ReasonCode == EnergyIntelligenceDriverReasonCodes.SourceConflict))
        {
            return EnergyIntelligenceOutcomes.SourceConflict;
        }

        if (drivers.Any(d => d.ReasonCode == EnergyIntelligenceDriverReasonCodes.InsufficientComparableWindows))
        {
            return EnergyIntelligenceOutcomes.InsufficientData;
        }

        if (drivers.Any(d => d.Severity is EnergyIntelligenceDriverSeverities.Fail or EnergyIntelligenceDriverSeverities.Warn
                             && d.ReasonCode is not EnergyIntelligenceDriverReasonCodes.MissingTarget
                                 and not EnergyIntelligenceDriverReasonCodes.MissingTariff
                                 and not EnergyIntelligenceDriverReasonCodes.MissingProductionState
                                 and not EnergyIntelligenceDriverReasonCodes.MissingProductionDenominator
                                 and not EnergyIntelligenceDriverReasonCodes.SourceStale))
        {
            return EnergyIntelligenceOutcomes.Attention;
        }

        if (drivers.Any(d => d.ReasonCode == EnergyIntelligenceDriverReasonCodes.MissingProductionDenominator
                             || d.ReasonCode == EnergyIntelligenceDriverReasonCodes.MissingProductionState
                             || d.ReasonCode == EnergyIntelligenceDriverReasonCodes.SourceStale))
        {
            return EnergyIntelligenceOutcomes.InsufficientData;
        }

        return EnergyIntelligenceOutcomes.WithinTarget;
    }

    private static int SeverityRank(string severity) =>
        severity switch
        {
            EnergyIntelligenceDriverSeverities.Fail => 4,
            EnergyIntelligenceDriverSeverities.Warn => 3,
            EnergyIntelligenceDriverSeverities.Info => 2,
            EnergyIntelligenceDriverSeverities.Pass => 1,
            _ => 0,
        };

    private static int ReasonOrder(string reasonCode)
    {
        for (var i = 0; i < LeadingDriverReasonPriority.Length; i++)
        {
            if (reasonCode == LeadingDriverReasonPriority[i])
            {
                return i;
            }
        }

        return LeadingDriverReasonPriority.Length;
    }

    private static decimal CalculateMedian(List<decimal> sortedValues)
    {
        if (sortedValues.Count == 0)
        {
            return 0m;
        }

        var mid = sortedValues.Count / 2;
        if (sortedValues.Count % 2 == 0)
        {
            return RoundEnergy((sortedValues[mid - 1] + sortedValues[mid]) / 2m);
        }

        return RoundEnergy(sortedValues[mid]);
    }

    private static decimal CalculateMad(List<decimal> values, decimal median)
    {
        var deviations = values.Select(v => Math.Abs(v - median)).OrderBy(v => v).ToList();
        return CalculateMedian(deviations);
    }

    private static string ResolveAggregateEvidence(IEnumerable<string> states)
    {
        if (states.Any(s => s == EnergyEvidenceStates.SourceConflict))
        {
            return EnergyEvidenceStates.SourceConflict;
        }

        if (states.Any(s => s == EnergyEvidenceStates.Simulated))
        {
            return EnergyEvidenceStates.Simulated;
        }

        if (states.All(s => s == EnergyEvidenceStates.Measured))
        {
            return EnergyEvidenceStates.Measured;
        }

        return EnergyEvidenceStates.Calculated;
    }

    private static EnergyIntelligenceDriverDraft Driver(
        string reasonCode,
        string category,
        string severity,
        string? actualValue,
        string? actualUnit,
        string? comparisonValue,
        string? comparisonUnit,
        decimal? normalizedVariance,
        string explanation) =>
        new(
            reasonCode,
            LabelForReason(reasonCode),
            severity,
            category,
            actualValue,
            actualUnit,
            comparisonValue,
            comparisonUnit,
            normalizedVariance,
            explanation,
            "energy-intelligence");

    private static string LabelForReason(string reasonCode) =>
        reasonCode switch
        {
            EnergyIntelligenceDriverReasonCodes.ConsumptionAboveBaseline => "Tüketim baseline üzerinde",
            EnergyIntelligenceDriverReasonCodes.ConsumptionAboveTarget => "Tüketim hedef üzerinde",
            EnergyIntelligenceDriverReasonCodes.PeakDemandExceeded => "Tepe talep eşiği aşıldı",
            EnergyIntelligenceDriverReasonCodes.IdleConsumptionElevated => "Boşta tüketim yüksek",
            EnergyIntelligenceDriverReasonCodes.EnergyIntensityElevated => "Enerji yoğunluğu yüksek",
            EnergyIntelligenceDriverReasonCodes.WithinTarget => "Hedef dahilinde",
            EnergyIntelligenceDriverReasonCodes.SourceConflict => "Kaynak çakışması",
            EnergyIntelligenceDriverReasonCodes.InsufficientComparableWindows => "Yetersiz karşılaştırılabilir pencere",
            EnergyIntelligenceDriverReasonCodes.MissingProductionState => "Üretim durumu eksik",
            EnergyIntelligenceDriverReasonCodes.MissingProductionDenominator => "Üretim paydası eksik",
            EnergyIntelligenceDriverReasonCodes.MissingTarget => "Hedef tanımlı değil",
            EnergyIntelligenceDriverReasonCodes.MissingTariff => "Tarife mevcut değil",
            EnergyIntelligenceDriverReasonCodes.SourceStale => "Kaynak verisi bayat",
            _ => reasonCode,
        };

    private static decimal RoundEnergy(decimal value) => EnergyCalculator.RoundEnergy(value);

    private static decimal RoundPercent(decimal value) =>
        Math.Round(value, 2, Rounding);

    private static string? FormatDecimal(decimal? value) =>
        value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : null;
}

public sealed record EnergyComparableWindowSample(
    string WindowReference,
    string ComparisonKey,
    int DurationSeconds,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    decimal ConsumedKwh,
    decimal? MaximumDemandKw,
    string EvidenceStatus,
    string FreshnessState,
    string? ProductionContextReference);

public sealed record EnergyProductionStateEvidence(
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    bool HasActiveProduction,
    int? TotalParts,
    int? GoodParts,
    string? ContextReference);

public sealed record EnergyBaselineCalculationResult(
    bool Succeeded,
    int SampleCount,
    decimal MedianCenter,
    decimal MadDispersion,
    decimal NormalizedDispersion,
    decimal RelativeTolerance,
    decimal UpperThresholdConsumedKwh,
    decimal? BaselineMaximumDemandKw,
    decimal? UpperThresholdDemandKw,
    decimal BaselineConsumedKwh,
    decimal? BaselineKwhPerKg,
    decimal? BaselineKwhPer1000Parts,
    decimal? BaselineKwhPer1000GoodParts,
    string CalculationMethod,
    string EvidenceStatus,
    IReadOnlyList<string> SourceWindowReferences,
    string? FailureReason)
{
    public static EnergyBaselineCalculationResult Failure(string outcomeEvidence, string reasonCode, string reason) =>
        new(false, 0, 0m, 0m, 0m, 0m, 0m, null, null, 0m, null, null, null, "unavailable", outcomeEvidence, [], reason);
}

public sealed record EnergyIntelligenceDriverDraft(
    string ReasonCode,
    string LabelTr,
    string Severity,
    string Category,
    string? ActualValue,
    string? ActualUnit,
    string? ComparisonValue,
    string? ComparisonUnit,
    decimal? NormalizedVariance,
    string Explanation,
    string EvidenceSource);

public sealed record EnergyIntelligenceEvaluationResult(
    string Outcome,
    string? LeadingDriverCode,
    IReadOnlyList<EnergyIntelligenceDriverDraft> Drivers,
    decimal ActualConsumedKwh,
    decimal? ActualMaximumDemandKw,
    decimal? VarianceConsumedKwh,
    decimal? VariancePercent,
    decimal? PotentialReducibleKwh,
    decimal? EstimatedCostImpact,
    string? CostCurrencyCode,
    string? OpportunityLabel,
    string EvidenceStatus,
    string FreshnessState);
