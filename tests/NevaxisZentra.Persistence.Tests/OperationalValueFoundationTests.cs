using Microsoft.EntityFrameworkCore;
using NevaxisZentra.Persistence.OperationalValue;
using NevaxisZentra.Persistence.ThroughputOptimizer;

namespace NevaxisZentra.Persistence.Tests;

public sealed class OperationalValuePermissionTests
{
    [Fact]
    public void OperationalValuePermissionKeysAreStable()
    {
        Assert.Equal("operational-value.read", OperationalValuePermissionKeys.Read);
        Assert.Equal("operational-value.manage-economic-inputs", OperationalValuePermissionKeys.ManageEconomicInputs);
        Assert.Equal("operational-value.export", OperationalValuePermissionKeys.Export);
    }
}

public sealed class OperationalValueEntityMetadataTests
{
    [Fact]
    public void OperationalValueTenantFacilityEntitiesRequireTenantAndFacility()
    {
        using var context = CreateContext();
        Assert.False(context.Model.FindEntityType(typeof(VerifiedOperationalValueRecord))!.FindProperty(nameof(VerifiedOperationalValueRecord.TenantId))!.IsNullable);
        Assert.False(context.Model.FindEntityType(typeof(VerifiedOperationalValueRecord))!.FindProperty(nameof(VerifiedOperationalValueRecord.FacilityId))!.IsNullable);
        Assert.False(context.Model.FindEntityType(typeof(TenantEconomicInputConfigurationRecord))!.FindProperty(nameof(TenantEconomicInputConfigurationRecord.TenantId))!.IsNullable);
        Assert.False(context.Model.FindEntityType(typeof(TenantEconomicInputConfigurationRecord))!.FindProperty(nameof(TenantEconomicInputConfigurationRecord.FacilityId))!.IsNullable);
        Assert.False(context.Model.FindEntityType(typeof(OperationalValuePeriodSummaryRecord))!.FindProperty(nameof(OperationalValuePeriodSummaryRecord.TenantId))!.IsNullable);
        Assert.False(context.Model.FindEntityType(typeof(OperationalValuePeriodSummaryRecord))!.FindProperty(nameof(OperationalValuePeriodSummaryRecord.FacilityId))!.IsNullable);
    }

    [Fact]
    public void OperationalValueTenantEntitiesHaveTenantQueryFilters()
    {
        using var context = CreateContext();
        Assert.NotEmpty(context.Model.FindEntityType(typeof(VerifiedOperationalValueRecord))!.GetDeclaredQueryFilters());
        Assert.NotEmpty(context.Model.FindEntityType(typeof(TenantEconomicInputConfigurationRecord))!.GetDeclaredQueryFilters());
        Assert.NotEmpty(context.Model.FindEntityType(typeof(OperationalValuePeriodSummaryRecord))!.GetDeclaredQueryFilters());
    }

    private static NevaxisZentraDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NevaxisZentraDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=nevaxiszentra_test;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsAssembly(NevaxisZentraDbContext.MigrationsAssemblyName))
            .Options;
        return new NevaxisZentraDbContext(options);
    }
}

public sealed class OperationalValueCategoryTests
{
    private static readonly string[] ExpectedCategories =
    [
        OperationalValueCategories.GoodOutputGain,
        OperationalValueCategories.ThroughputGain,
        OperationalValueCategories.CapacityRecovered,
        OperationalValueCategories.DowntimeAvoided,
        OperationalValueCategories.LateOrderRiskReduced,
        OperationalValueCategories.OtifProtected,
        OperationalValueCategories.ScrapReduced,
        OperationalValueCategories.ReworkReduced,
        OperationalValueCategories.ChangeoverReduced,
        OperationalValueCategories.WipReduced,
        OperationalValueCategories.MaterialWaitReduced,
        OperationalValueCategories.LaborTouchReduced,
        OperationalValueCategories.HumanTouchesReduced,
        OperationalValueCategories.EnergyReduction,
        OperationalValueCategories.IntelligenceCostReduced,
    ];

    [Fact]
    public void ValueCategoriesAreStable()
    {
        foreach (var category in ExpectedCategories)
        {
            Assert.False(string.IsNullOrWhiteSpace(category));
            Assert.DoesNotContain(' ', category);
        }

        Assert.Equal(ExpectedCategories.Length, ExpectedCategories.Distinct(StringComparer.Ordinal).Count());
    }
}

public sealed class OperationalValueStatusTests
{
    [Theory]
    [InlineData(OperationalValueStatuses.Predicted)]
    [InlineData(OperationalValueStatuses.Observing)]
    [InlineData(OperationalValueStatuses.VerifiedOperational)]
    [InlineData(OperationalValueStatuses.VerifiedEconomic)]
    [InlineData(OperationalValueStatuses.Invalidated)]
    [InlineData(OperationalValueStatuses.Rejected)]
    public void OperationalValueStatusesAreStable(string status)
    {
        Assert.False(string.IsNullOrWhiteSpace(status));
    }
}

public sealed class OperationalValueEngineEligibilityTests
{
    [Fact]
    public void VerifiedImprovementIsEligible()
    {
        Assert.True(OperationalValueEngine.IsVerifiedOutcomeEligible(
            ThroughputOptimizerVerificationOutcomes.VerifiedImprovement));
    }

    [Theory]
    [InlineData(ThroughputOptimizerVerificationOutcomes.PartialImprovement)]
    [InlineData(ThroughputOptimizerVerificationOutcomes.NoImprovement)]
    [InlineData(ThroughputOptimizerVerificationOutcomes.Regression)]
    [InlineData(ThroughputOptimizerVerificationOutcomes.InsufficientObservation)]
    [InlineData(ThroughputOptimizerVerificationOutcomes.QualityGuardFailed)]
    [InlineData(ThroughputOptimizerVerificationOutcomes.SafetyGuardFailed)]
    public void NonVerifiedOutcomesAreRejected(string verificationOutcome)
    {
        Assert.False(OperationalValueEngine.IsVerifiedOutcomeEligible(verificationOutcome));

        var result = OperationalValueEngine.Calculate(
            OperationalValueTestHelpers.CreateRequest(verificationOutcome: verificationOutcome),
            OperationalValueTestHelpers.CreateEconomicInputs(),
            []);

        Assert.False(result.Eligible);
        Assert.Equal(OperationalValueFailureStates.UnverifiedOutcome, result.FailureState);
        Assert.Equal(OperationalValueStatuses.Rejected, result.Status);
    }
}

public sealed class OperationalValueEngineValidationTests
{
    [Fact]
    public void ValidateBaselineRequiresSourceMethodAndNonNegativeValue()
    {
        Assert.True(OperationalValueEngine.ValidateBaseline(new OperationalValueBaselineSnapshot(
            612m, "good-units-per-hour", "before-intervention-window", "2h",
            OperationalValueBaselineMethods.BeforeInterventionWindow)));

        Assert.False(OperationalValueEngine.ValidateBaseline(new OperationalValueBaselineSnapshot(
            612m, "good-units-per-hour", "", "2h",
            OperationalValueBaselineMethods.BeforeInterventionWindow)));

        Assert.False(OperationalValueEngine.ValidateBaseline(new OperationalValueBaselineSnapshot(
            612m, "good-units-per-hour", "before-intervention-window", "2h", "")));

        Assert.False(OperationalValueEngine.ValidateBaseline(new OperationalValueBaselineSnapshot(
            -1m, "good-units-per-hour", "before-intervention-window", "2h",
            OperationalValueBaselineMethods.BeforeInterventionWindow)));
    }

    [Fact]
    public void ValidateActualRequiresSourceAndNonNegativeValue()
    {
        Assert.True(OperationalValueEngine.ValidateActual(new OperationalValueActualSnapshot(
            668m, "good-units-per-hour", "verified-observation-window")));

        Assert.False(OperationalValueEngine.ValidateActual(new OperationalValueActualSnapshot(
            668m, "good-units-per-hour", "")));

        Assert.False(OperationalValueEngine.ValidateActual(new OperationalValueActualSnapshot(
            -5m, "good-units-per-hour", "verified-observation-window")));
    }

    [Fact]
    public void InvalidBaselineIsRejected()
    {
        var request = OperationalValueTestHelpers.CreateRequest() with
        {
            Baseline = new OperationalValueBaselineSnapshot(
                612m, "good-units-per-hour", "", "2h",
                OperationalValueBaselineMethods.BeforeInterventionWindow),
        };

        var result = OperationalValueEngine.Calculate(request, OperationalValueTestHelpers.CreateEconomicInputs(), []);
        Assert.False(result.Eligible);
        Assert.Equal(OperationalValueFailureStates.InvalidBaseline, result.FailureState);
    }

    [Fact]
    public void InvalidActualIsRejected()
    {
        var request = OperationalValueTestHelpers.CreateRequest() with
        {
            Actual = new OperationalValueActualSnapshot(668m, "good-units-per-hour", ""),
        };

        var result = OperationalValueEngine.Calculate(request, OperationalValueTestHelpers.CreateEconomicInputs(), []);
        Assert.False(result.Eligible);
        Assert.Equal(OperationalValueFailureStates.InvalidActual, result.FailureState);
    }

    [Fact]
    public void ZeroOrNegativeThroughputDeltaIsRejectedForGainCategories()
    {
        var request = OperationalValueTestHelpers.CreateRequest(
            valueCategory: OperationalValueCategories.GoodOutputGain,
            baselineValue: 668m,
            actualValue: 612m);

        var result = OperationalValueEngine.Calculate(request, OperationalValueTestHelpers.CreateEconomicInputs(), []);
        Assert.False(result.Eligible);
        Assert.Equal(OperationalValueFailureStates.UnverifiedOutcome, result.FailureState);
    }
}

public sealed class OperationalValueEngineDeltaTests
{
    [Fact]
    public void CalculateDeltaReturnsRoundedActualMinusBaseline()
    {
        Assert.Equal(56m, OperationalValueEngine.CalculateDelta(612m, 668m));
    }

    [Fact]
    public void GoodOutputGainMultipliesHourlyDeltaByObservationWindow()
    {
        var result = OperationalValueEngine.Calculate(
            OperationalValueTestHelpers.CreateRequest(
                valueCategory: OperationalValueCategories.GoodOutputGain,
                baselineValue: 612m,
                actualValue: 668m,
                observationHours: 2m),
            OperationalValueTestHelpers.CreateEconomicInputs(),
            []);

        Assert.True(result.Eligible);
        Assert.Equal(112m, result.DeltaValue);
        Assert.Equal("good-units", result.Unit);
    }

    [Fact]
    public void ThroughputGainUsesHourlyDeltaWithoutWindowMultiplier()
    {
        var result = OperationalValueEngine.Calculate(
            OperationalValueTestHelpers.CreateRequest(
                valueCategory: OperationalValueCategories.ThroughputGain,
                baselineValue: 612m,
                actualValue: 668m,
                observationHours: 2m),
            OperationalValueTestHelpers.CreateEconomicInputs(),
            []);

        Assert.True(result.Eligible);
        Assert.Equal(56m, result.DeltaValue);
        Assert.Equal("good-units-per-hour", result.Unit);
    }

    [Fact]
    public void CapacityRecoveredUsesDirectDelta()
    {
        var result = OperationalValueEngine.Calculate(
            OperationalValueTestHelpers.CreateRequest(
                valueCategory: OperationalValueCategories.CapacityRecovered,
                baselineValue: 80m,
                actualValue: 95m,
                observationHours: 1m,
                baselineUnit: "capacity-units",
                actualUnit: "capacity-units"),
            null,
            []);

        Assert.True(result.Eligible);
        Assert.Equal(15m, result.DeltaValue);
    }
}

public sealed class OperationalValueEngineConfidenceTests
{
    [Fact]
    public void ConfidenceIncreasesWithAttributionStrength()
    {
        var baseline = new OperationalValueBaselineSnapshot(
            612m, "good-units-per-hour", "before-intervention-window", "2h",
            OperationalValueBaselineMethods.BeforeInterventionWindow);
        var actual = new OperationalValueActualSnapshot(668m, "good-units-per-hour", "verified-observation-window");

        var direct = OperationalValueEngine.CalculateConfidence(
            baseline, actual, 2m, OperationalValueAttributionClasses.Direct);
        var stronglyAttributed = OperationalValueEngine.CalculateConfidence(
            baseline, actual, 2m, OperationalValueAttributionClasses.StronglyAttributed);
        var partiallyAttributed = OperationalValueEngine.CalculateConfidence(
            baseline, actual, 2m, OperationalValueAttributionClasses.PartiallyAttributed);
        var correlatedOnly = OperationalValueEngine.CalculateConfidence(
            baseline, actual, 2m, OperationalValueAttributionClasses.CorrelatedOnly);

        Assert.Equal(0.99m, direct);
        Assert.Equal(0.99m, stronglyAttributed);
        Assert.Equal(0.95m, partiallyAttributed);
        Assert.Equal(0.90m, correlatedOnly);
        Assert.True(stronglyAttributed >= partiallyAttributed);
        Assert.True(partiallyAttributed > correlatedOnly);
    }

    [Fact]
    public void ConfidenceRespectsObservationWindowThresholds()
    {
        var baseline = new OperationalValueBaselineSnapshot(
            100m, "units", "source", "1h", OperationalValueBaselineMethods.BeforeInterventionWindow);
        var actual = new OperationalValueActualSnapshot(110m, "units", "verified");

        var shortWindow = OperationalValueEngine.CalculateConfidence(
            baseline, actual, 0.5m, OperationalValueAttributionClasses.Unattributed);
        var longWindow = OperationalValueEngine.CalculateConfidence(
            baseline, actual, 2m, OperationalValueAttributionClasses.Unattributed);

        Assert.True(longWindow > shortWindow);
    }
}

public sealed class OperationalValueEconomicConverterTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void MissingEconomicInputsReturnsNotConfigured()
    {
        var request = OperationalValueTestHelpers.CreateRequest();
        var conversion = OperationalValueEconomicConverter.Convert(request, 112m, null);

        Assert.Equal(OperationalValueEconomicConversionStatuses.NotConfigured, conversion.ConversionStatus);
        Assert.Null(conversion.EconomicValue);
        Assert.Equal(string.Empty, conversion.FormulaType);
    }

    [Fact]
    public void GoodOutputGainConvertsWithUnitContribution()
    {
        var request = OperationalValueTestHelpers.CreateRequest(
            valueCategory: OperationalValueCategories.GoodOutputGain);
        var inputs = OperationalValueTestHelpers.CreateEconomicInputs(unitContribution: 125m, versionCode: "v2026-09");

        var conversion = OperationalValueEconomicConverter.Convert(request, 112m, inputs);

        Assert.Equal(OperationalValueEconomicConversionStatuses.Converted, conversion.ConversionStatus);
        Assert.Equal(14000m, conversion.EconomicValue);
        Assert.Equal("TRY", conversion.Currency);
        Assert.Equal(OperationalValueFormulaTypes.ThroughputGoodUnits, conversion.FormulaType);
    }

    [Fact]
    public void ThroughputGainConvertsWithUnitContribution()
    {
        var request = OperationalValueTestHelpers.CreateRequest(
            valueCategory: OperationalValueCategories.ThroughputGain);
        var conversion = OperationalValueEconomicConverter.Convert(
            request,
            56m,
            OperationalValueTestHelpers.CreateEconomicInputs(unitContribution: 125m));

        Assert.Equal(OperationalValueEconomicConversionStatuses.Converted, conversion.ConversionStatus);
        Assert.Equal(7000m, conversion.EconomicValue);
        Assert.Equal(OperationalValueFormulaTypes.ThroughputGoodUnits, conversion.FormulaType);
    }

    [Fact]
    public void ScrapReducedConvertsWithScrapUnitCost()
    {
        var request = OperationalValueTestHelpers.CreateRequest(
            valueCategory: OperationalValueCategories.ScrapReduced,
            baselineValue: 10m,
            actualValue: 4m,
            baselineUnit: "scrap-units",
            actualUnit: "scrap-units");
        var conversion = OperationalValueEconomicConverter.Convert(
            request,
            -6m,
            OperationalValueTestHelpers.CreateEconomicInputs(scrapUnitCost: 80m));

        Assert.Equal(OperationalValueEconomicConversionStatuses.Converted, conversion.ConversionStatus);
        Assert.Equal(-480m, conversion.EconomicValue);
        Assert.Equal(OperationalValueFormulaTypes.ScrapUnits, conversion.FormulaType);
    }

    [Fact]
    public void DowntimeAvoidedConvertsMinutesToHourlyCost()
    {
        var request = OperationalValueTestHelpers.CreateRequest(
            valueCategory: OperationalValueCategories.DowntimeAvoided,
            baselineValue: 0m,
            actualValue: 30m,
            baselineUnit: "minutes",
            actualUnit: "minutes");
        var conversion = OperationalValueEconomicConverter.Convert(
            request,
            30m,
            OperationalValueTestHelpers.CreateEconomicInputs(downtimeCostPerHour: 6000m));

        Assert.Equal(OperationalValueEconomicConversionStatuses.Converted, conversion.ConversionStatus);
        Assert.Equal(3000m, conversion.EconomicValue);
        Assert.Equal(OperationalValueFormulaTypes.DowntimeMinutes, conversion.FormulaType);
    }

    [Fact]
    public void ChangeoverReducedConvertsWithPerMinuteCost()
    {
        var request = OperationalValueTestHelpers.CreateRequest(
            valueCategory: OperationalValueCategories.ChangeoverReduced,
            baselineValue: 0m,
            actualValue: 20m,
            baselineUnit: "minutes",
            actualUnit: "minutes");
        var conversion = OperationalValueEconomicConverter.Convert(
            request,
            20m,
            OperationalValueTestHelpers.CreateEconomicInputs(changeoverCostPerMinute: 50m));

        Assert.Equal(OperationalValueEconomicConversionStatuses.Converted, conversion.ConversionStatus);
        Assert.Equal(1000m, conversion.EconomicValue);
        Assert.Equal(OperationalValueFormulaTypes.ChangeoverMinutes, conversion.FormulaType);
    }

    [Fact]
    public void MissingCategorySpecificInputReturnsOperationalOnly()
    {
        var request = OperationalValueTestHelpers.CreateRequest(
            valueCategory: OperationalValueCategories.ScrapReduced);
        var conversion = OperationalValueEconomicConverter.Convert(
            request,
            5m,
            OperationalValueTestHelpers.CreateEconomicInputs(unitContribution: 125m));

        Assert.Equal(OperationalValueEconomicConversionStatuses.OperationalOnly, conversion.ConversionStatus);
        Assert.Null(conversion.EconomicValue);
        Assert.Equal("TRY", conversion.Currency);
    }

    [Fact]
    public void EngineCalculationPreservesInputVersionCode()
    {
        var result = OperationalValueEngine.Calculate(
            OperationalValueTestHelpers.CreateRequest(),
            OperationalValueTestHelpers.CreateEconomicInputs(versionCode: "tenant-input-v3"),
            []);

        Assert.True(result.Eligible);
        Assert.Equal("tenant-input-v3", result.InputVersion);
        Assert.Equal(OperationalValueEngine.CurrentFormulaVersion, result.FormulaVersion);
    }

    [Fact]
    public void EngineCalculationWithoutFinancialInputsRemainsOperationalOnly()
    {
        var result = OperationalValueEngine.Calculate(
            OperationalValueTestHelpers.CreateRequest(),
            null,
            []);

        Assert.True(result.Eligible);
        Assert.Equal(OperationalValueStatuses.VerifiedOperational, result.Status);
        Assert.Equal(OperationalValueEconomicConversionStatuses.NotConfigured, result.EconomicConversionStatus);
        Assert.Null(result.EconomicValue);
        Assert.Null(result.NetValue);
        Assert.Null(result.RoiPercent);
        Assert.Equal(string.Empty, result.InputVersion);
    }
}

public sealed class OperationalValueRoiEngineTests
{
    [Fact]
    public void RoiIsAvailableWhenNetValueAndCostArePositive()
    {
        var roi = OperationalValueRoiEngine.CalculateRoi(13999.58m, 0.42m);
        Assert.Equal("AVAILABLE", roi.Availability);
        Assert.Equal(3333233.33m, roi.RoiPercent);
    }

    [Fact]
    public void RoiUnavailableWithoutNetValue()
    {
        var roi = OperationalValueRoiEngine.CalculateRoi(null, 0.42m);
        Assert.Null(roi.RoiPercent);
        Assert.Equal(OperationalValueFailureStates.RoiUnavailable, roi.Availability);
    }

    [Fact]
    public void RoiUnavailableWithoutPositiveCost()
    {
        var roi = OperationalValueRoiEngine.CalculateRoi(100m, 0m);
        Assert.Null(roi.RoiPercent);
        Assert.Equal(OperationalValueFailureStates.RoiUnavailable, roi.Availability);
    }

    [Fact]
    public void PaybackUnavailableWithoutImplementationCost()
    {
        var payback = OperationalValueRoiEngine.CalculatePaybackDays(100m, null);
        Assert.Null(payback.PaybackDays);
        Assert.Equal(OperationalValueFailureStates.PaybackUnavailable, payback.Availability);
    }

    [Fact]
    public void PaybackUnavailableWhenNetValueIsNotPositive()
    {
        var payback = OperationalValueRoiEngine.CalculatePaybackDays(0m, 1000m);
        Assert.Null(payback.PaybackDays);
        Assert.Equal(OperationalValueFailureStates.PaybackUnavailable, payback.Availability);
    }

    [Fact]
    public void EngineLinksIntelligenceCostToNetValueAndRoi()
    {
        var result = OperationalValueEngine.CalculateThroughputGolden(612m, 668m, 2m, 125m, "TRY", 0.42m);

        Assert.True(result.Eligible);
        Assert.Equal(OperationalValueStatuses.VerifiedEconomic, result.Status);
        Assert.Equal(14000m, result.EconomicValue);
        Assert.Equal(0.42m, result.IntelligenceCost);
        Assert.Equal(13999.58m, result.NetValue);
        Assert.NotNull(result.RoiPercent);
        Assert.Null(result.PaybackDays);
    }
}

public sealed class OperationalValueDoubleCountGuardTests
{
    [Fact]
    public void OverlapGuardDoesNotBlockWithoutDigitalWorkCaseId()
    {
        var request = OperationalValueTestHelpers.CreateRequest(
            valueCategory: OperationalValueCategories.ThroughputGain,
            digitalWorkCaseId: null);
        var existing = OperationalValueTestHelpers.CreateExistingRecord(
            OperationalValueCategories.ChangeoverReduced,
            economicValue: 1000m);

        Assert.False(OperationalValueDoubleCountGuard.IsBlocked(request, [existing]));
    }

    [Fact]
    public void OverlapGuardDoesNotBlockWithoutOverlapGroupKey()
    {
        var request = OperationalValueTestHelpers.CreateRequest(
            valueCategory: OperationalValueCategories.ThroughputGain,
            overlapGroupKey: null);
        var existing = OperationalValueTestHelpers.CreateExistingRecord(
            OperationalValueCategories.ChangeoverReduced,
            economicValue: 1000m);

        Assert.False(OperationalValueDoubleCountGuard.IsBlocked(request, [existing]));
    }

    [Fact]
    public void OverlapGuardBlocksThroughputWhenChangeoverEconomicValueAlreadyAttributed()
    {
        var caseId = Guid.NewGuid();
        var existing = OperationalValueTestHelpers.CreateExistingRecord(
            OperationalValueCategories.ChangeoverReduced,
            economicValue: 1000m,
            digitalWorkCaseId: caseId,
            overlapGroupKey: OperationalValueOverlapGroups.ChangeoverThroughput);

        var request = OperationalValueTestHelpers.CreateRequest(
            valueCategory: OperationalValueCategories.ThroughputGain,
            digitalWorkCaseId: caseId,
            overlapGroupKey: OperationalValueOverlapGroups.ChangeoverThroughput);

        Assert.True(OperationalValueDoubleCountGuard.IsBlocked(request, [existing]));
    }

    [Fact]
    public void OverlapGuardIgnoresInvalidatedRecords()
    {
        var caseId = Guid.NewGuid();
        var existing = OperationalValueTestHelpers.CreateExistingRecord(
            OperationalValueCategories.ChangeoverReduced,
            economicValue: 1000m,
            digitalWorkCaseId: caseId,
            overlapGroupKey: OperationalValueOverlapGroups.ChangeoverThroughput,
            isInvalidated: true);

        var request = OperationalValueTestHelpers.CreateRequest(
            valueCategory: OperationalValueCategories.ThroughputGain,
            digitalWorkCaseId: caseId,
            overlapGroupKey: OperationalValueOverlapGroups.ChangeoverThroughput);

        Assert.False(OperationalValueDoubleCountGuard.IsBlocked(request, [existing]));
    }

    [Fact]
    public void EngineRejectsWhenDoubleCountGuardBlocks()
    {
        var caseId = Guid.NewGuid();
        var existing = OperationalValueTestHelpers.CreateExistingRecord(
            OperationalValueCategories.ChangeoverReduced,
            economicValue: 1000m,
            digitalWorkCaseId: caseId,
            overlapGroupKey: OperationalValueOverlapGroups.ChangeoverThroughput);

        var request = OperationalValueTestHelpers.CreateRequest(
            valueCategory: OperationalValueCategories.ThroughputGain,
            digitalWorkCaseId: caseId,
            overlapGroupKey: OperationalValueOverlapGroups.ChangeoverThroughput);

        var result = OperationalValueEngine.Calculate(
            request,
            OperationalValueTestHelpers.CreateEconomicInputs(),
            [existing]);

        Assert.False(result.Eligible);
        Assert.Equal(OperationalValueFailureStates.DoubleCountBlocked, result.FailureState);
    }
}

public sealed class OperationalValueKpiEngineTests
{
    [Fact]
    public void AutonomousCompletionRateCountsVerifiedCasesWithoutHumanIntervention()
    {
        var now = DateTimeOffset.UtcNow;
        var cases = new List<(DateTimeOffset CreatedAtUtc, DateTimeOffset? VerifiedAtUtc, bool RequiredHumanIntervention)>
        {
            (now.AddHours(-4), now.AddHours(-2), false),
            (now.AddHours(-3), now.AddHours(-1), false),
            (now.AddHours(-2), null, false),
            (now.AddHours(-1), now, true),
        };

        var snapshot = OperationalValueKpiEngine.Compute([], cases);
        Assert.Equal(1m, snapshot.AutonomousCompletionRate);
    }

    [Fact]
    public void AutonomousCompletionRateDefaultsToOneWhenNoEligibleCases()
    {
        var snapshot = OperationalValueKpiEngine.Compute([], []);
        Assert.Equal(1m, snapshot.AutonomousCompletionRate);
    }

    [Fact]
    public void AverageHumanTouchesUsesHumanTouchesReducedRecords()
    {
        var records = new List<VerifiedOperationalValueRecord>
        {
            OperationalValueTestHelpers.CreateExistingRecord(
                OperationalValueCategories.HumanTouchesReduced,
                economicValue: null,
                humanTouchesBefore: 6,
                humanTouchesAfter: 2),
            OperationalValueTestHelpers.CreateExistingRecord(
                OperationalValueCategories.HumanTouchesReduced,
                economicValue: null,
                humanTouchesBefore: 4,
                humanTouchesAfter: 1),
        };

        var snapshot = OperationalValueKpiEngine.Compute(records, []);
        Assert.Equal(3.5m, snapshot.AverageHumanTouchesPerCase);
    }

    [Fact]
    public void AverageTimeToOutcomeUsesVerifiedCaseDurations()
    {
        var now = DateTimeOffset.UtcNow;
        var cases = new List<(DateTimeOffset CreatedAtUtc, DateTimeOffset? VerifiedAtUtc, bool RequiredHumanIntervention)>
        {
            (now.AddHours(-4), now.AddHours(-2), false),
            (now.AddHours(-3), now.AddHours(-1), false),
        };

        var snapshot = OperationalValueKpiEngine.Compute([], cases);
        Assert.Equal(2m, snapshot.AverageTimeToVerifiedOutcomeHours);
    }

    [Fact]
    public void InvalidatedRecordsAreExcludedFromTrustedKpis()
    {
        var records = new List<VerifiedOperationalValueRecord>
        {
            OperationalValueTestHelpers.CreateExistingRecord(
                OperationalValueCategories.GoodOutputGain,
                economicValue: 1000m,
                deltaValue: 112m,
                observationWindowHours: 2m,
                isInvalidated: true),
            OperationalValueTestHelpers.CreateExistingRecord(
                OperationalValueCategories.GoodOutputGain,
                economicValue: 2000m,
                deltaValue: 56m,
                observationWindowHours: 1m),
        };

        var snapshot = OperationalValueKpiEngine.Compute(records, []);
        Assert.Equal(56m, snapshot.OperationalValuePerVerifiedCase);
    }
}

public sealed class OperationalValueFromVerifiedImprovementEngineTests
{
    [Fact]
    public void BuildThroughputRequestMapsVerifiedImprovementSnapshot()
    {
        var caseId = Guid.NewGuid();
        var improvementId = Guid.NewGuid();
        var improvement = new VerifiedImprovementSnapshot(
            improvementId,
            caseId,
            ThroughputOptimizerVerificationOutcomes.VerifiedImprovement,
            new VerifiedImprovementBaseline(612m, 1200, 12, 3, 40m, "ST-30", "buffer-starvation"),
            56m,
            "buffer replenishment",
            new VerifiedImprovementActual(668m, 1336, 10, 2, 35m, "ST-30", "buffer-starvation"),
            true,
            true,
            DateTimeOffset.UtcNow.AddHours(-2),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        var request = OperationalValueFromVerifiedImprovementEngine.BuildThroughputRequest(
            improvement,
            optimizationRunId: Guid.NewGuid(),
            scenarioRunId: null,
            workflowType: "throughput-optimizer",
            lineCode: "MIX-LINE-01",
            observationHours: 2m,
            humanTouchesBefore: 4,
            humanTouchesAfter: 2,
            intelligenceCost: 0.42m);

        Assert.Equal(caseId, request.DigitalWorkCaseId);
        Assert.Equal(improvementId, request.VerifiedImprovementId);
        Assert.Equal(OperationalValueCategories.GoodOutputGain, request.ValueCategory);
        Assert.Equal(612m, request.Baseline.Value);
        Assert.Equal(668m, request.Actual.Value);
        Assert.Equal(OperationalValueOverlapGroups.ThroughputCapacity, request.OverlapGroupKey);
        Assert.Equal(0.42m, request.IntelligenceCost);
    }

    [Fact]
    public void BuildThroughputRequestRequiresActualOutcome()
    {
        var improvement = new VerifiedImprovementSnapshot(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ThroughputOptimizerVerificationOutcomes.VerifiedImprovement,
            new VerifiedImprovementBaseline(612m, 1200, 12, 3, 40m, "ST-30", "buffer-starvation"),
            56m,
            "buffer replenishment",
            null,
            true,
            true,
            null,
            null,
            null);

        var exception = Assert.Throws<OperationalValueException>(() =>
            OperationalValueFromVerifiedImprovementEngine.BuildThroughputRequest(
                improvement,
                null,
                null,
                "throughput-optimizer",
                "MIX-LINE-01",
                2m,
                4,
                2,
                null));

        Assert.Equal(OperationalValueFailureStates.InvalidActual, exception.FailureState);
    }
}

public sealed class OperationalValueEngineVersionTests
{
    [Fact]
    public void EngineAndGuardVersionsAreStable()
    {
        Assert.Equal("operational-value-engine-v1", OperationalValueEngineVersions.Engine);
        Assert.Equal("operational-value-double-count-v1", OperationalValueEngineVersions.DoubleCountGuard);
        Assert.Equal("operational-value-controller-v1", OperationalValueEngineVersions.Controller);
        Assert.Equal("1.0", OperationalValueEngine.CurrentFormulaVersion);
    }
}

internal static class OperationalValueTestHelpers
{
    public static OperationalValueCalculationRequest CreateRequest(
        string verificationOutcome = ThroughputOptimizerVerificationOutcomes.VerifiedImprovement,
        string valueCategory = OperationalValueCategories.GoodOutputGain,
        decimal baselineValue = 612m,
        decimal actualValue = 668m,
        decimal observationHours = 2m,
        string baselineUnit = "good-units-per-hour",
        string actualUnit = "good-units-per-hour",
        Guid? digitalWorkCaseId = null,
        string? overlapGroupKey = OperationalValueOverlapGroups.ThroughputCapacity,
        string attributionClass = OperationalValueAttributionClasses.StronglyAttributed) =>
        new(
            digitalWorkCaseId ?? Guid.NewGuid(),
            null,
            null,
            Guid.NewGuid(),
            verificationOutcome,
            valueCategory,
            new OperationalValueBaselineSnapshot(
                baselineValue,
                baselineUnit,
                "before-intervention-window",
                $"{observationHours:0.##}h",
                OperationalValueBaselineMethods.BeforeInterventionWindow),
            new OperationalValueActualSnapshot(actualValue, actualUnit, "verified-observation-window"),
            observationHours,
            attributionClass,
            "throughput-optimizer",
            "MIX-LINE-01",
            "line",
            "MIX-LINE-01",
            4,
            2,
            overlapGroupKey,
            0.42m);

    public static TenantEconomicInputSnapshot CreateEconomicInputs(
        string versionCode = "test-v1",
        decimal? unitContribution = 125m,
        decimal? scrapUnitCost = null,
        decimal? downtimeCostPerHour = null,
        decimal? changeoverCostPerMinute = null) =>
        new(
            Guid.NewGuid(),
            versionCode,
            "TRY",
            unitContribution,
            scrapUnitCost,
            downtimeCostPerHour,
            null,
            changeoverCostPerMinute,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

    public static VerifiedOperationalValueRecord CreateExistingRecord(
        string valueCategory,
        decimal? economicValue,
        Guid? digitalWorkCaseId = null,
        string? overlapGroupKey = OperationalValueOverlapGroups.ThroughputCapacity,
        bool isInvalidated = false,
        decimal deltaValue = 56m,
        decimal observationWindowHours = 2m,
        int humanTouchesBefore = 0,
        int humanTouchesAfter = 0) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            FacilityId = Guid.NewGuid(),
            DigitalWorkCaseId = digitalWorkCaseId ?? Guid.NewGuid(),
            ValueCategory = valueCategory,
            Status = OperationalValueStatuses.VerifiedEconomic,
            OverlapGroupKey = overlapGroupKey,
            EconomicValue = economicValue,
            DeltaValue = deltaValue,
            ObservationWindowHours = observationWindowHours,
            HumanTouchesBefore = humanTouchesBefore,
            HumanTouchesAfter = humanTouchesAfter,
            IsInvalidated = isInvalidated,
        };
}
