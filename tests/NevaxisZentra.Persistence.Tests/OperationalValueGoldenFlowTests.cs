using NevaxisZentra.Persistence.OperationalValue;
using NevaxisZentra.Persistence.ThroughputOptimizer;

namespace NevaxisZentra.Persistence.Tests;

public sealed class OperationalValueGoldenThroughputTests
{
    [Fact]
    public void GoldenThroughput612To668OverTwoHoursProduces112GoodUnits()
    {
        var result = OperationalValueEngine.CalculateThroughputGolden(
            baselineGoodPerHour: 612m,
            actualGoodPerHour: 668m,
            observationHours: 2m,
            unitContributionValue: null,
            currency: "TRY",
            intelligenceCost: 0.42m);

        Assert.True(result.Eligible);
        Assert.Equal(112m, result.DeltaValue);
        Assert.Equal("good-units", result.Unit);
        Assert.Equal(56m, OperationalValueEngine.CalculateDelta(612m, 668m));
        Assert.Contains("baseline=612", result.EvidenceSummary, StringComparison.Ordinal);
        Assert.Contains("actual=668", result.EvidenceSummary, StringComparison.Ordinal);
        Assert.Contains("delta=112", result.EvidenceSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void GoldenThroughputHourlyGainIsSeparatedFromGoodOutputUnits()
    {
        var hourlyRequest = OperationalValueGoldenFlowHelpers.CreateGoldenThroughputRequest(
            OperationalValueCategories.ThroughputGain);
        var goodOutputRequest = OperationalValueGoldenFlowHelpers.CreateGoldenThroughputRequest(
            OperationalValueCategories.GoodOutputGain);

        var hourly = OperationalValueEngine.Calculate(
            hourlyRequest,
            OperationalValueGoldenFlowHelpers.GoldenEconomicInputs,
            []);
        var goodOutput = OperationalValueEngine.Calculate(
            goodOutputRequest,
            OperationalValueGoldenFlowHelpers.GoldenEconomicInputs,
            []);

        Assert.Equal(56m, hourly.DeltaValue);
        Assert.Equal("good-units-per-hour", hourly.Unit);
        Assert.Equal(112m, goodOutput.DeltaValue);
        Assert.Equal("good-units", goodOutput.Unit);
    }
}

public sealed class OperationalValueGoldenEconomicFlowTests
{
    [Fact]
    public void GoldenEconomicFlowConvertsGoodUnitsAndLinksIntelligenceCost()
    {
        var result = OperationalValueEngine.CalculateThroughputGolden(
            612m,
            668m,
            2m,
            125m,
            "TRY",
            0.42m);

        Assert.True(result.Eligible);
        Assert.Equal(OperationalValueStatuses.VerifiedEconomic, result.Status);
        Assert.Equal(OperationalValueEconomicConversionStatuses.Converted, result.EconomicConversionStatus);
        Assert.Equal(OperationalValueFormulaTypes.ThroughputGoodUnits, result.FormulaType);
        Assert.Equal(OperationalValueEngine.CurrentFormulaVersion, result.FormulaVersion);
        Assert.Equal("golden-v1", result.InputVersion);
        Assert.Equal(14000m, result.EconomicValue);
        Assert.Equal("TRY", result.Currency);
        Assert.Equal(0.42m, result.IntelligenceCost);
        Assert.Equal(13999.58m, result.NetValue);
        Assert.NotNull(result.RoiPercent);
        Assert.Null(result.PaybackDays);
    }

    [Fact]
    public void GoldenEconomicFlowCostPerVerifiedOutcomeUsesIntelligenceCost()
    {
        const decimal intelligenceCost = 0.42m;
        var result = OperationalValueEngine.CalculateThroughputGolden(
            612m,
            668m,
            2m,
            125m,
            "TRY",
            intelligenceCost);

        Assert.Equal(intelligenceCost, result.IntelligenceCost);
        Assert.Equal(intelligenceCost, intelligenceCost / 1m);
    }

    [Fact]
    public void GoldenEconomicInputVersioningIsPreservedAcrossRecalculation()
    {
        var request = OperationalValueGoldenFlowHelpers.CreateGoldenThroughputRequest();
        var versionedInputs = OperationalValueGoldenFlowHelpers.GoldenEconomicInputs with
        {
            VersionCode = "golden-v2",
            UnitContributionValue = 130m,
        };

        var first = OperationalValueEngine.Calculate(request, versionedInputs, []);
        var second = OperationalValueEngine.Calculate(request, versionedInputs with { VersionCode = "golden-v3" }, []);

        Assert.Equal("golden-v2", first.InputVersion);
        Assert.Equal(14560m, first.EconomicValue);
        Assert.Equal("golden-v3", second.InputVersion);
        Assert.Equal(14560m, second.EconomicValue);
    }

    [Fact]
    public void GoldenMissingFinancialInputKeepsOperationalValueWithoutEconomicConversion()
    {
        var result = OperationalValueEngine.Calculate(
            OperationalValueGoldenFlowHelpers.CreateGoldenThroughputRequest(),
            null,
            []);

        Assert.True(result.Eligible);
        Assert.Equal(OperationalValueStatuses.VerifiedOperational, result.Status);
        Assert.Equal(OperationalValueEconomicConversionStatuses.NotConfigured, result.EconomicConversionStatus);
        Assert.Null(result.EconomicValue);
        Assert.Null(result.NetValue);
        Assert.Null(result.RoiPercent);
    }
}

public sealed class OperationalValueGoldenFormulaFlowTests
{
    [Fact]
    public void GoldenDowntimeAvoidedFormulaConvertsMinutesToEconomicValue()
    {
        var request = OperationalValueGoldenFlowHelpers.CreateCategoryRequest(
            OperationalValueCategories.DowntimeAvoided,
            baselineValue: 0m,
            actualValue: 30m,
            unit: "minutes");
        var inputs = OperationalValueGoldenFlowHelpers.GoldenEconomicInputs with
        {
            UnitContributionValue = null,
            DowntimeCostPerHour = 6000m,
        };

        var result = OperationalValueEngine.Calculate(request, inputs, []);

        Assert.True(result.Eligible);
        Assert.Equal(30m, result.DeltaValue);
        Assert.Equal(3000m, result.EconomicValue);
        Assert.Equal(OperationalValueFormulaTypes.DowntimeMinutes, result.FormulaType);
    }

    [Fact]
    public void GoldenScrapReducedFormulaUsesScrapUnitCost()
    {
        var request = OperationalValueGoldenFlowHelpers.CreateCategoryRequest(
            OperationalValueCategories.ScrapReduced,
            baselineValue: 10m,
            actualValue: 4m,
            unit: "scrap-units");
        var inputs = OperationalValueGoldenFlowHelpers.GoldenEconomicInputs with
        {
            UnitContributionValue = null,
            ScrapUnitCost = 80m,
        };

        var result = OperationalValueEngine.Calculate(request, inputs, []);

        Assert.True(result.Eligible);
        Assert.Equal(-6m, result.DeltaValue);
        Assert.Equal(-480m, result.EconomicValue);
        Assert.Equal(OperationalValueFormulaTypes.ScrapUnits, result.FormulaType);
    }

    [Fact]
    public void GoldenChangeoverReducedFormulaUsesPerMinuteCost()
    {
        var request = OperationalValueGoldenFlowHelpers.CreateCategoryRequest(
            OperationalValueCategories.ChangeoverReduced,
            baselineValue: 0m,
            actualValue: 20m,
            unit: "minutes");
        var inputs = OperationalValueGoldenFlowHelpers.GoldenEconomicInputs with
        {
            UnitContributionValue = null,
            ChangeoverCostPerMinute = 50m,
        };

        var result = OperationalValueEngine.Calculate(request, inputs, []);

        Assert.True(result.Eligible);
        Assert.Equal(20m, result.DeltaValue);
        Assert.Equal(1000m, result.EconomicValue);
        Assert.Equal(OperationalValueFormulaTypes.ChangeoverMinutes, result.FormulaType);
    }

    [Fact]
    public void GoldenCapacityRecoveredUsesDirectOperationalDelta()
    {
        var request = OperationalValueGoldenFlowHelpers.CreateCategoryRequest(
            OperationalValueCategories.CapacityRecovered,
            baselineValue: 80m,
            actualValue: 95m,
            unit: "capacity-units");

        var result = OperationalValueEngine.Calculate(request, null, []);

        Assert.True(result.Eligible);
        Assert.Equal(15m, result.DeltaValue);
        Assert.Equal(OperationalValueStatuses.VerifiedOperational, result.Status);
    }
}

public sealed class OperationalValueGoldenDoubleCountTests
{
    [Fact]
    public void GoldenDoubleCountFlowBlocksOverlappingThroughputAttribution()
    {
        var caseId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var changeoverRequest = OperationalValueGoldenFlowHelpers.CreateCategoryRequest(
            OperationalValueCategories.ChangeoverReduced,
            baselineValue: 0m,
            actualValue: 20m,
            unit: "minutes",
            digitalWorkCaseId: caseId,
            overlapGroupKey: OperationalValueOverlapGroups.ChangeoverThroughput);
        var changeoverInputs = OperationalValueGoldenFlowHelpers.GoldenEconomicInputs with
        {
            UnitContributionValue = null,
            ChangeoverCostPerMinute = 50m,
        };

        var changeoverResult = OperationalValueEngine.Calculate(changeoverRequest, changeoverInputs, []);
        Assert.True(changeoverResult.Eligible);
        Assert.Equal(1000m, changeoverResult.EconomicValue);

        var changeoverRecord = OperationalValueGoldenFlowHelpers.MapGoldenRecord(
            caseId,
            changeoverRequest,
            changeoverResult,
            now);

        var throughputRequest = changeoverRequest with
        {
            ValueCategory = OperationalValueCategories.ThroughputGain,
            Baseline = new OperationalValueBaselineSnapshot(
                612m, "good-units-per-hour", "before-intervention-window", "2h",
                OperationalValueBaselineMethods.BeforeInterventionWindow),
            Actual = new OperationalValueActualSnapshot(
                668m, "good-units-per-hour", "verified-observation-window"),
            ObservationWindowHours = 2m,
        };

        var throughputResult = OperationalValueEngine.Calculate(
            throughputRequest,
            OperationalValueGoldenFlowHelpers.GoldenEconomicInputs,
            [changeoverRecord]);

        Assert.False(throughputResult.Eligible);
        Assert.Equal(OperationalValueFailureStates.DoubleCountBlocked, throughputResult.FailureState);
    }

    [Fact]
    public void GoldenDoubleCountFlowAllowsNonOverlappingCategories()
    {
        var caseId = Guid.NewGuid();
        var existing = OperationalValueGoldenFlowHelpers.MapGoldenRecord(
            caseId,
            OperationalValueGoldenFlowHelpers.CreateGoldenThroughputRequest(
                OperationalValueCategories.GoodOutputGain,
                digitalWorkCaseId: caseId),
            OperationalValueEngine.CalculateThroughputGolden(612m, 668m, 2m, 125m, "TRY", 0.42m),
            DateTimeOffset.UtcNow);

        var downtimeRequest = OperationalValueGoldenFlowHelpers.CreateCategoryRequest(
            OperationalValueCategories.DowntimeAvoided,
            baselineValue: 0m,
            actualValue: 15m,
            unit: "minutes",
            digitalWorkCaseId: caseId,
            overlapGroupKey: OperationalValueOverlapGroups.ThroughputCapacity);
        var downtimeInputs = OperationalValueGoldenFlowHelpers.GoldenEconomicInputs with
        {
            UnitContributionValue = null,
            DowntimeCostPerHour = 6000m,
        };

        var downtimeResult = OperationalValueEngine.Calculate(downtimeRequest, downtimeInputs, [existing]);
        Assert.True(downtimeResult.Eligible);
    }
}

public sealed class OperationalValueGoldenUnverifiedTests
{
    [Theory]
    [InlineData(ThroughputOptimizerVerificationOutcomes.PartialImprovement)]
    [InlineData(ThroughputOptimizerVerificationOutcomes.NoImprovement)]
    [InlineData(ThroughputOptimizerVerificationOutcomes.Regression)]
    [InlineData(ThroughputOptimizerVerificationOutcomes.InsufficientObservation)]
    public void GoldenUnverifiedOutcomesDoNotCreateTrustedValue(string verificationOutcome)
    {
        var request = OperationalValueGoldenFlowHelpers.CreateGoldenThroughputRequest(
            verificationOutcome: verificationOutcome);

        var result = OperationalValueEngine.Calculate(
            request,
            OperationalValueGoldenFlowHelpers.GoldenEconomicInputs,
            []);

        Assert.False(result.Eligible);
        Assert.Equal(OperationalValueFailureStates.UnverifiedOutcome, result.FailureState);
        Assert.Equal(OperationalValueStatuses.Rejected, result.Status);
        Assert.Equal(OperationalValueEconomicConversionStatuses.InsufficientInputs, result.EconomicConversionStatus);
        Assert.Equal(0m, result.DeltaValue);
    }

    [Fact]
    public void GoldenPredictedStatusIsSeparatedFromVerifiedCalculation()
    {
        var predictedRecord = new VerifiedOperationalValueRecord
        {
            Status = OperationalValueStatuses.Predicted,
            ValueCategory = OperationalValueCategories.GoodOutputGain,
            DeltaValue = 200m,
            IsInvalidated = false,
        };

        var trusted = new List<VerifiedOperationalValueRecord>
        {
            predictedRecord,
            OperationalValueGoldenFlowHelpers.MapGoldenRecord(
                Guid.NewGuid(),
                OperationalValueGoldenFlowHelpers.CreateGoldenThroughputRequest(),
                OperationalValueEngine.CalculateThroughputGolden(612m, 668m, 2m, null, "TRY", 0m),
                DateTimeOffset.UtcNow),
        };

        var snapshot = OperationalValueKpiEngine.Compute(trusted, []);
        Assert.Equal(224m, snapshot.OperationalValuePerVerifiedCase);
    }
}

public sealed class OperationalValueGoldenAttributionTests
{
    [Fact]
    public void GoldenAttributionDirectProducesHighestConfidence()
    {
        var direct = OperationalValueEngine.Calculate(
            OperationalValueGoldenFlowHelpers.CreateGoldenThroughputRequest(
                attributionClass: OperationalValueAttributionClasses.Direct),
            OperationalValueGoldenFlowHelpers.GoldenEconomicInputs,
            []);
        var correlated = OperationalValueEngine.Calculate(
            OperationalValueGoldenFlowHelpers.CreateGoldenThroughputRequest(
                attributionClass: OperationalValueAttributionClasses.CorrelatedOnly),
            OperationalValueGoldenFlowHelpers.GoldenEconomicInputs,
            []);

        Assert.True(direct.ConfidenceScore > correlated.ConfidenceScore);
        Assert.Equal(0.99m, direct.ConfidenceScore);
    }
}

public sealed class OperationalValueGoldenInvalidationTests
{
    [Fact]
    public void GoldenInvalidationExcludesRecordFromTrustedKpis()
    {
        var caseId = Guid.NewGuid();
        var verified = OperationalValueGoldenFlowHelpers.MapGoldenRecord(
            caseId,
            OperationalValueGoldenFlowHelpers.CreateGoldenThroughputRequest(digitalWorkCaseId: caseId),
            OperationalValueEngine.CalculateThroughputGolden(612m, 668m, 2m, 125m, "TRY", 0.42m),
            DateTimeOffset.UtcNow);

        verified.IsInvalidated = true;
        verified.Status = OperationalValueStatuses.Invalidated;
        verified.InvalidationReason = "verified-outcome-reopened";

        var snapshot = OperationalValueKpiEngine.Compute([verified], []);
        Assert.Equal(0m, snapshot.OperationalValuePerVerifiedCase);
    }

    [Fact]
    public void GoldenInvalidationPreservesPreviousStatusSemantics()
    {
        var result = OperationalValueEngine.CalculateThroughputGolden(612m, 668m, 2m, 125m, "TRY", 0.42m);
        var record = OperationalValueGoldenFlowHelpers.MapGoldenRecord(
            Guid.NewGuid(),
            OperationalValueGoldenFlowHelpers.CreateGoldenThroughputRequest(),
            result,
            DateTimeOffset.UtcNow);

        var previousStatus = record.Status;
        record.IsInvalidated = true;
        record.Status = OperationalValueStatuses.Invalidated;
        record.InvalidationReason = "verified-outcome-reopened";

        Assert.Equal(OperationalValueStatuses.VerifiedEconomic, previousStatus);
        Assert.Equal(OperationalValueStatuses.Invalidated, record.Status);
        Assert.Equal("verified-outcome-reopened", record.InvalidationReason);
    }
}

public sealed class OperationalValueGoldenKpiFlowTests
{
    [Fact]
    public void GoldenKpiFlowComputesAutonomousCompletionAndTimeToOutcome()
    {
        var now = DateTimeOffset.UtcNow;
        var records = new List<VerifiedOperationalValueRecord>
        {
            OperationalValueGoldenFlowHelpers.MapGoldenRecord(
                Guid.NewGuid(),
                OperationalValueGoldenFlowHelpers.CreateGoldenThroughputRequest(),
                OperationalValueEngine.CalculateThroughputGolden(612m, 668m, 2m, 125m, "TRY", 0.42m),
                now),
            OperationalValueGoldenFlowHelpers.MapGoldenRecord(
                Guid.NewGuid(),
                OperationalValueGoldenFlowHelpers.CreateCategoryRequest(
                    OperationalValueCategories.HumanTouchesReduced,
                    baselineValue: 4m,
                    actualValue: 2m,
                    unit: "touches",
                    humanTouchesBefore: 4,
                    humanTouchesAfter: 2),
                OperationalValueEngine.Calculate(
                    OperationalValueGoldenFlowHelpers.CreateCategoryRequest(
                        OperationalValueCategories.HumanTouchesReduced,
                        baselineValue: 4m,
                        actualValue: 2m,
                        unit: "touches",
                        humanTouchesBefore: 4,
                        humanTouchesAfter: 2),
                    null,
                    []),
                now),
        };

        var cases = new List<(DateTimeOffset CreatedAtUtc, DateTimeOffset? VerifiedAtUtc, bool RequiredHumanIntervention)>
        {
            (now.AddHours(-6), now.AddHours(-2), false),
            (now.AddHours(-5), now.AddHours(-1), false),
            (now.AddHours(-4), null, true),
        };

        var snapshot = OperationalValueKpiEngine.Compute(records, cases);

        Assert.Equal(1m, snapshot.AutonomousCompletionRate);
        Assert.Equal(4m, snapshot.AverageTimeToVerifiedOutcomeHours);
        Assert.Equal(2m, snapshot.AverageHumanTouchesPerCase);
        Assert.True(snapshot.OperationalValuePerVerifiedCase > 0m);
    }
}

internal static class OperationalValueGoldenFlowHelpers
{
    public static readonly TenantEconomicInputSnapshot GoldenEconomicInputs = new(
        Guid.NewGuid(),
        "golden-v1",
        "TRY",
        125m,
        null,
        null,
        null,
        50m,
        null,
        DateTimeOffset.UtcNow,
        DateTimeOffset.UtcNow);

    public static OperationalValueCalculationRequest CreateGoldenThroughputRequest(
        string valueCategory = OperationalValueCategories.GoodOutputGain,
        string verificationOutcome = ThroughputOptimizerVerificationOutcomes.VerifiedImprovement,
        Guid? digitalWorkCaseId = null,
        string attributionClass = OperationalValueAttributionClasses.StronglyAttributed) =>
        new(
            digitalWorkCaseId ?? Guid.NewGuid(),
            null,
            null,
            Guid.NewGuid(),
            verificationOutcome,
            valueCategory,
            new OperationalValueBaselineSnapshot(
                612m,
                "good-units-per-hour",
                "before-intervention-window",
                "2h",
                OperationalValueBaselineMethods.BeforeInterventionWindow),
            new OperationalValueActualSnapshot(
                668m,
                "good-units-per-hour",
                "verified-observation-window"),
            2m,
            attributionClass,
            "golden-throughput",
            "MIX-LINE-01",
            "line",
            "MIX-LINE-01",
            4,
            2,
            OperationalValueOverlapGroups.ThroughputCapacity,
            0.42m);

    public static OperationalValueCalculationRequest CreateCategoryRequest(
        string valueCategory,
        decimal baselineValue,
        decimal actualValue,
        string unit,
        Guid? digitalWorkCaseId = null,
        string? overlapGroupKey = OperationalValueOverlapGroups.ThroughputCapacity,
        int humanTouchesBefore = 0,
        int humanTouchesAfter = 0) =>
        new(
            digitalWorkCaseId ?? Guid.NewGuid(),
            null,
            null,
            Guid.NewGuid(),
            ThroughputOptimizerVerificationOutcomes.VerifiedImprovement,
            valueCategory,
            new OperationalValueBaselineSnapshot(
                baselineValue,
                unit,
                "before-intervention-window",
                "1 window",
                OperationalValueBaselineMethods.BeforeInterventionWindow),
            new OperationalValueActualSnapshot(actualValue, unit, "verified-observation-window"),
            1m,
            OperationalValueAttributionClasses.Direct,
            "golden-category",
            "MIX-LINE-01",
            "line",
            "MIX-LINE-01",
            humanTouchesBefore,
            humanTouchesAfter,
            overlapGroupKey,
            null);

    public static VerifiedOperationalValueRecord MapGoldenRecord(
        Guid caseId,
        OperationalValueCalculationRequest request,
        OperationalValueCalculationResult calculation,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            FacilityId = Guid.NewGuid(),
            DigitalWorkCaseId = request.DigitalWorkCaseId ?? caseId,
            ValueCategory = request.ValueCategory,
            Status = calculation.Status,
            AttributionClass = request.AttributionClass,
            DeltaValue = calculation.DeltaValue,
            Unit = calculation.Unit,
            ObservationWindowHours = request.ObservationWindowHours,
            EconomicValue = calculation.EconomicValue,
            IntelligenceCost = calculation.IntelligenceCost,
            NetValue = calculation.NetValue,
            OverlapGroupKey = request.OverlapGroupKey,
            HumanTouchesBefore = request.HumanTouchesBefore,
            HumanTouchesAfter = request.HumanTouchesAfter,
            IsInvalidated = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
}
