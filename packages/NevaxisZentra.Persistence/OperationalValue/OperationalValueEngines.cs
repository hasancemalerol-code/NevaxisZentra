using NevaxisZentra.Persistence.ThroughputOptimizer;

namespace NevaxisZentra.Persistence.OperationalValue;

public sealed class OperationalValueException : Exception
{
    public OperationalValueException(string failureState, string message) : base(message) =>
        FailureState = failureState;

    public string FailureState { get; }
}

public static class OperationalValueEngine
{
    public const string CurrentFormulaVersion = "1.0";

    public static bool IsVerifiedOutcomeEligible(string verificationOutcome) =>
        verificationOutcome == ThroughputOptimizerVerificationOutcomes.VerifiedImprovement;

    public static bool ValidateBaseline(OperationalValueBaselineSnapshot baseline) =>
        baseline.Value >= 0 && !string.IsNullOrWhiteSpace(baseline.Source)
        && !string.IsNullOrWhiteSpace(baseline.Method);

    public static bool ValidateActual(OperationalValueActualSnapshot actual) =>
        actual.Value >= 0 && !string.IsNullOrWhiteSpace(actual.Source);

    public static decimal CalculateDelta(decimal baseline, decimal actual) =>
        Math.Round(actual - baseline, 4);

    public static decimal CalculateConfidence(
        OperationalValueBaselineSnapshot baseline,
        OperationalValueActualSnapshot actual,
        decimal observationWindowHours,
        string attributionClass)
    {
        var score = 0.55m;
        if (!string.IsNullOrWhiteSpace(baseline.Source))
        {
            score += 0.1m;
        }

        if (!string.IsNullOrWhiteSpace(actual.Source))
        {
            score += 0.1m;
        }

        if (observationWindowHours >= 1m)
        {
            score += 0.1m;
        }

        if (observationWindowHours >= 2m)
        {
            score += 0.05m;
        }

        score += attributionClass switch
        {
            OperationalValueAttributionClasses.Direct => 0.15m,
            OperationalValueAttributionClasses.StronglyAttributed => 0.1m,
            OperationalValueAttributionClasses.PartiallyAttributed => 0.05m,
            _ => 0m,
        };

        return Math.Min(0.99m, score);
    }

    public static OperationalValueCalculationResult Calculate(
        OperationalValueCalculationRequest request,
        TenantEconomicInputSnapshot? economicInputs,
        IReadOnlyList<VerifiedOperationalValueRecord> existingOverlapRecords)
    {
        if (!IsVerifiedOutcomeEligible(request.VerificationOutcome))
        {
            return Reject(OperationalValueFailureStates.UnverifiedOutcome, "Only verified outcomes can create trusted value.");
        }

        if (!ValidateBaseline(request.Baseline))
        {
            return Reject(OperationalValueFailureStates.InvalidBaseline, "Baseline source/method is required.");
        }

        if (!ValidateActual(request.Actual))
        {
            return Reject(OperationalValueFailureStates.InvalidActual, "Actual measured outcome is required.");
        }

        if (OperationalValueDoubleCountGuard.IsBlocked(request, existingOverlapRecords))
        {
            return Reject(OperationalValueFailureStates.DoubleCountBlocked, "Overlap guard blocked duplicate economic attribution.");
        }

        var delta = request.ValueCategory switch
        {
            OperationalValueCategories.GoodOutputGain => Math.Round(
                CalculateDelta(request.Baseline.Value, request.Actual.Value) * Math.Max(0.01m, request.ObservationWindowHours),
                4),
            _ => CalculateDelta(request.Baseline.Value, request.Actual.Value),
        };

        var unit = request.ValueCategory == OperationalValueCategories.GoodOutputGain
            ? "good-units"
            : request.Actual.Unit;
        if (delta <= 0 && request.ValueCategory is OperationalValueCategories.GoodOutputGain
            or OperationalValueCategories.ThroughputGain
            or OperationalValueCategories.CapacityRecovered)
        {
            return Reject(OperationalValueFailureStates.UnverifiedOutcome, "No positive verified delta for value category.");
        }

        var confidence = CalculateConfidence(
            request.Baseline,
            request.Actual,
            request.ObservationWindowHours,
            request.AttributionClass);

        var economic = OperationalValueEconomicConverter.Convert(
            request,
            delta,
            economicInputs);

        var intelligenceCost = request.IntelligenceCost ?? 0m;
        decimal? netValue = economic.EconomicValue.HasValue
            ? Math.Round(economic.EconomicValue.Value - intelligenceCost, 4)
            : null;

        var roi = OperationalValueRoiEngine.CalculateRoi(netValue, intelligenceCost);
        var payback = OperationalValueRoiEngine.CalculatePaybackDays(netValue, null);

        var status = economic.EconomicValue.HasValue
            ? OperationalValueStatuses.VerifiedEconomic
            : OperationalValueStatuses.VerifiedOperational;

        return new OperationalValueCalculationResult(
            true,
            null,
            status,
            delta,
            unit,
            confidence,
            economic.ConversionStatus,
            economic.EconomicValue,
            economic.Currency,
            intelligenceCost > 0 ? intelligenceCost : null,
            netValue,
            roi.RoiPercent,
            payback.PaybackDays,
            economic.FormulaType,
            CurrentFormulaVersion,
            economicInputs?.VersionCode ?? string.Empty,
            BuildEvidenceSummary(request, delta));
    }

    public static OperationalValueCalculationResult CalculateThroughputGolden(
        decimal baselineGoodPerHour,
        decimal actualGoodPerHour,
        decimal observationHours,
        decimal? unitContributionValue,
        string currency,
        decimal intelligenceCost)
    {
        var deltaPerHour = CalculateDelta(baselineGoodPerHour, actualGoodPerHour);
        var goodUnits = Math.Round(deltaPerHour * observationHours, 4);
        var request = new OperationalValueCalculationRequest(
            Guid.NewGuid(),
            null,
            null,
            Guid.NewGuid(),
            ThroughputOptimizerVerificationOutcomes.VerifiedImprovement,
            OperationalValueCategories.GoodOutputGain,
            new OperationalValueBaselineSnapshot(
                baselineGoodPerHour,
                "good-units-per-hour",
                "before-intervention-window",
                $"{observationHours:0.##}h",
                OperationalValueBaselineMethods.BeforeInterventionWindow),
            new OperationalValueActualSnapshot(
                actualGoodPerHour,
                "good-units-per-hour",
                "verified-observation-window"),
            observationHours,
            OperationalValueAttributionClasses.StronglyAttributed,
            "throughput-optimizer",
            "MIX-LINE-01",
            "line",
            "MIX-LINE-01",
            4,
            2,
            OperationalValueOverlapGroups.ThroughputCapacity,
            intelligenceCost);

        var economicInputs = unitContributionValue.HasValue
            ? new TenantEconomicInputSnapshot(
                Guid.NewGuid(),
                "golden-v1",
                currency,
                unitContributionValue,
                null,
                null,
                null,
                null,
                null,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow)
            : null;

        return Calculate(request, economicInputs, []);
    }

    private static OperationalValueCalculationResult Reject(string failureState, string message) =>
        new(
            false,
            failureState,
            OperationalValueStatuses.Rejected,
            0m,
            string.Empty,
            0m,
            OperationalValueEconomicConversionStatuses.InsufficientInputs,
            null,
            null,
            null,
            null,
            null,
            null,
            string.Empty,
            CurrentFormulaVersion,
            string.Empty,
            message);

    private static string BuildEvidenceSummary(OperationalValueCalculationRequest request, decimal delta) =>
        $"baseline={request.Baseline.Value} actual={request.Actual.Value} delta={delta} window={request.ObservationWindowHours}h";
}

public static class OperationalValueDoubleCountGuard
{
    public static bool IsBlocked(
        OperationalValueCalculationRequest request,
        IReadOnlyList<VerifiedOperationalValueRecord> existingRecords)
    {
        if (string.IsNullOrWhiteSpace(request.OverlapGroupKey))
        {
            return false;
        }

        if (request.DigitalWorkCaseId is null)
        {
            return false;
        }

        return existingRecords.Any(record =>
            !record.IsInvalidated
            && record.DigitalWorkCaseId == request.DigitalWorkCaseId
            && string.Equals(record.OverlapGroupKey, request.OverlapGroupKey, StringComparison.OrdinalIgnoreCase)
            && record.ValueCategory != request.ValueCategory
            && record.EconomicValue.GetValueOrDefault() > 0
            && request.ValueCategory is OperationalValueCategories.ThroughputGain
                or OperationalValueCategories.CapacityRecovered
                or OperationalValueCategories.ChangeoverReduced);
    }
}

public static class OperationalValueEconomicConverter
{
    public sealed record ConversionResult(
        string ConversionStatus,
        decimal? EconomicValue,
        string? Currency,
        string FormulaType);

    public static ConversionResult Convert(
        OperationalValueCalculationRequest request,
        decimal delta,
        TenantEconomicInputSnapshot? inputs)
    {
        if (inputs is null)
        {
            return new ConversionResult(
                OperationalValueEconomicConversionStatuses.NotConfigured,
                null,
                null,
                string.Empty);
        }

        return request.ValueCategory switch
        {
            OperationalValueCategories.GoodOutputGain or OperationalValueCategories.ThroughputGain
                when inputs.UnitContributionValue.HasValue =>
                new ConversionResult(
                    OperationalValueEconomicConversionStatuses.Converted,
                    Math.Round(delta * inputs.UnitContributionValue.Value, 4),
                    inputs.Currency,
                    OperationalValueFormulaTypes.ThroughputGoodUnits),
            OperationalValueCategories.ScrapReduced when inputs.ScrapUnitCost.HasValue =>
                new ConversionResult(
                    OperationalValueEconomicConversionStatuses.Converted,
                    Math.Round(delta * inputs.ScrapUnitCost.Value, 4),
                    inputs.Currency,
                    OperationalValueFormulaTypes.ScrapUnits),
            OperationalValueCategories.DowntimeAvoided when inputs.DowntimeCostPerHour.HasValue =>
                new ConversionResult(
                    OperationalValueEconomicConversionStatuses.Converted,
                    Math.Round((delta / 60m) * inputs.DowntimeCostPerHour.Value, 4),
                    inputs.Currency,
                    OperationalValueFormulaTypes.DowntimeMinutes),
            OperationalValueCategories.ChangeoverReduced when inputs.ChangeoverCostPerMinute.HasValue =>
                new ConversionResult(
                    OperationalValueEconomicConversionStatuses.Converted,
                    Math.Round(delta * inputs.ChangeoverCostPerMinute.Value, 4),
                    inputs.Currency,
                    OperationalValueFormulaTypes.ChangeoverMinutes),
            _ => new ConversionResult(
                OperationalValueEconomicConversionStatuses.OperationalOnly,
                null,
                inputs.Currency,
                string.Empty),
        };
    }
}

public static class OperationalValueRoiEngine
{
    public sealed record RoiResult(decimal? RoiPercent, string Availability);

    public sealed record PaybackResult(decimal? PaybackDays, string Availability);

    public static RoiResult CalculateRoi(decimal? netValue, decimal? relevantCost)
    {
        if (!netValue.HasValue || !relevantCost.HasValue || relevantCost.Value <= 0)
        {
            return new RoiResult(null, OperationalValueFailureStates.RoiUnavailable);
        }

        return new RoiResult(Math.Round(netValue.Value / relevantCost.Value * 100m, 2), "AVAILABLE");
    }

    public static PaybackResult CalculatePaybackDays(decimal? netValue, decimal? implementationCost)
    {
        if (!netValue.HasValue || !implementationCost.HasValue || netValue.Value <= 0)
        {
            return new PaybackResult(null, OperationalValueFailureStates.PaybackUnavailable);
        }

        return new PaybackResult(Math.Round(implementationCost.Value / netValue.Value, 2), "AVAILABLE");
    }
}

public static class OperationalValueKpiEngine
{
    public static OperationalValueKpiSnapshot Compute(
        IReadOnlyList<VerifiedOperationalValueRecord> records,
        IReadOnlyList<(DateTimeOffset CreatedAtUtc, DateTimeOffset? VerifiedAtUtc, bool RequiredHumanIntervention)> cases)
    {
        var trusted = records.Where(r => !r.IsInvalidated && r.Status is OperationalValueStatuses.VerifiedOperational
            or OperationalValueStatuses.VerifiedEconomic).ToList();

        var eligibleCases = cases.Where(x => x.VerifiedAtUtc.HasValue).ToList();
        var autonomousEligible = eligibleCases.Where(x => !x.RequiredHumanIntervention).ToList();
        var autonomousCompleted = autonomousEligible.Count(x => x.VerifiedAtUtc.HasValue);
        var autonomousRate = autonomousEligible.Count == 0
            ? 1m
            : autonomousCompleted / (decimal)autonomousEligible.Count;

        var humanTouchRecords = trusted.Where(r => r.ValueCategory == OperationalValueCategories.HumanTouchesReduced).ToList();
        var averageHumanTouches = humanTouchRecords.Count == 0
            ? 0m
            : (decimal)humanTouchRecords.Average(r => Math.Max(0, r.HumanTouchesBefore - r.HumanTouchesAfter));

        var averageTimeToOutcome = eligibleCases.Count == 0
            ? 0m
            : eligibleCases
                .Where(x => x.VerifiedAtUtc.HasValue)
                .Average(x => (decimal)(x.VerifiedAtUtc!.Value - x.CreatedAtUtc).TotalHours);

        var perCaseOperational = trusted.Count == 0
            ? 0m
            : trusted.Where(r => r.ValueCategory == OperationalValueCategories.GoodOutputGain)
                .Sum(r => r.DeltaValue * Math.Max(1m, r.ObservationWindowHours)) / trusted.Count;

        return new OperationalValueKpiSnapshot(
            autonomousRate,
            averageHumanTouches,
            averageTimeToOutcome,
            perCaseOperational);
    }
}

public static class OperationalValueFromVerifiedImprovementEngine
{
    public static OperationalValueCalculationRequest BuildThroughputRequest(
        VerifiedImprovementSnapshot improvement,
        Guid? optimizationRunId,
        Guid? scenarioRunId,
        string workflowType,
        string? lineCode,
        decimal observationHours,
        int humanTouchesBefore,
        int humanTouchesAfter,
        decimal? intelligenceCost)
    {
        var actual = improvement.Actual
            ?? throw new OperationalValueException(
                OperationalValueFailureStates.InvalidActual,
                "Verified improvement actual outcome is required.");

        return new OperationalValueCalculationRequest(
            improvement.CaseId,
            optimizationRunId,
            scenarioRunId,
            improvement.Id,
            improvement.VerificationOutcome,
            OperationalValueCategories.GoodOutputGain,
            new OperationalValueBaselineSnapshot(
                improvement.Baseline.GoodThroughputPerHour,
                "good-units-per-hour",
                "before-intervention-window",
                $"{observationHours:0.##}h",
                OperationalValueBaselineMethods.BeforeInterventionWindow),
            new OperationalValueActualSnapshot(
                actual.GoodThroughputPerHour,
                "good-units-per-hour",
                "verified-observation-window"),
            observationHours,
            OperationalValueAttributionClasses.StronglyAttributed,
            workflowType,
            lineCode,
            "line",
            lineCode,
            humanTouchesBefore,
            humanTouchesAfter,
            OperationalValueOverlapGroups.ThroughputCapacity,
            intelligenceCost);
    }
}
