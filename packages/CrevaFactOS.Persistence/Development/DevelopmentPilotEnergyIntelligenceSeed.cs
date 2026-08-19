using System.Globalization;
using CrevaFactOS.Persistence.Energy;
using CrevaFactOS.Persistence.Edge;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Development;

internal static class DevelopmentPilotEnergyIntelligenceSeed
{
    internal static async Task EnsureEnergyIntelligencePilotRecordsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var provenance = EnergyIntelligenceConstants.DevelopmentPilotProvenance;
        var windowDurationHours = 8;
        var durationSeconds = windowDurationHours * 3600;
        var anchor = seedTimestampUtc.UtcDateTime.Date;
        var baselineWindowStart = new DateTimeOffset(anchor.AddDays(-1).AddHours(6), TimeSpan.Zero);

        await EnsureBaselineSampleWindowsAsync(
            dbContext,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTimestampUtc,
            baselineWindowStart,
            durationSeconds,
            cancellationToken);

        await EnsureAuxiliaryWindowsAsync(
            dbContext,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTimestampUtc,
            anchor,
            cancellationToken);

        await EnsureIntensityCyclesAsync(
            dbContext,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureBaselineProfileAsync(
            dbContext,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            durationSeconds,
            cancellationToken);

        await EnsureTargetsAsync(
            dbContext,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            cancellationToken);

        await EnsureAssessmentsAsync(
            dbContext,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            baselineWindowStart,
            durationSeconds,
            cancellationToken);
    }

    private static async Task EnsureBaselineSampleWindowsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        DateTimeOffset baselineWindowStart,
        int durationSeconds,
        CancellationToken cancellationToken)
    {
        var samples = new (Guid Id, int DayOffset, decimal Kwh)[]
        {
            (DevelopmentPilotIds.Energy2PilotWindowBaselineSample1Id, -14, 36.2m),
            (DevelopmentPilotIds.Energy2PilotWindowBaselineSample2Id, -12, 37.8m),
            (DevelopmentPilotIds.Energy2PilotWindowBaselineSample3Id, -10, 38.1m),
            (DevelopmentPilotIds.Energy2PilotWindowBaselineSample4Id, -8, 39.4m),
            (DevelopmentPilotIds.Energy2PilotWindowBaselineSample5Id, -6, 37.5m),
        };

        foreach (var sample in samples)
        {
            var start = baselineWindowStart.AddDays(sample.DayOffset);
            var end = start.AddSeconds(durationSeconds);
            await EnsureWindowAsync(
                dbContext,
                sample.Id,
                tenantId,
                facilityId,
                DevelopmentPilotIds.EnergyPilotPointInj1Id,
                start,
                end,
                EnergyCalculator.RoundEnergy(sample.Kwh),
                17.5m,
                actorSubjectId,
                seedTimestampUtc,
                cancellationToken);
        }
    }

    private static async Task EnsureAuxiliaryWindowsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        DateTime anchor,
        CancellationToken cancellationToken)
    {
        var peakStart = new DateTimeOffset(anchor.AddDays(-3).AddHours(8), TimeSpan.Zero);
        var peakEnd = peakStart.AddHours(8);
        await EnsureWindowAsync(
            dbContext,
            DevelopmentPilotIds.Energy2PilotWindowPeakCompressorId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.EnergyPilotPointCompressorId,
            peakStart,
            peakEnd,
            48.5m,
            22.5m,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        var idleStart = new DateTimeOffset(anchor.AddDays(-2).AddHours(22), TimeSpan.Zero);
        var idleEnd = idleStart.AddHours(8);
        await EnsureWindowAsync(
            dbContext,
            DevelopmentPilotIds.Energy2PilotWindowIdleDryerId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.EnergyPilotPointDryerId,
            idleStart,
            idleEnd,
            8.2m,
            4.5m,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        var intensityStart = new DateTimeOffset(anchor.AddDays(-4).AddHours(6), TimeSpan.Zero);
        var intensityEnd = intensityStart.AddHours(8);
        await EnsureWindowAsync(
            dbContext,
            DevelopmentPilotIds.Energy2PilotWindowIntensityId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.EnergyPilotPointInj1Id,
            intensityStart,
            intensityEnd,
            20m,
            16m,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);
    }

    private static async Task EnsureIntensityCyclesAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var anchor = seedTimestampUtc.UtcDateTime.Date;
        var intensityStart = new DateTimeOffset(anchor.AddDays(-4).AddHours(6), TimeSpan.Zero);

        for (var i = 0; i < 500; i++)
        {
            var cycleId = Guid.Parse($"bb000001-0185-4000-8000-{(0x185000L + i).ToString("X12", CultureInfo.InvariantCulture)}");
            var exists = await dbContext.MachineObservedCycles
                .IgnoreQueryFilters()
                .AnyAsync(c => c.Id == cycleId, cancellationToken);

            if (exists)
            {
                continue;
            }

            dbContext.MachineObservedCycles.Add(new MachineObservedCycle
            {
                Id = cycleId,
                TenantId = tenantId,
                FacilityId = facilityId,
                MachineId = DevelopmentPilotIds.EdgePilotPressMachineId,
                GatewayId = DevelopmentPilotIds.EdgePilotSimulatorGatewayId,
                CycleIdentity = $"energy2-intensity-{i}",
                CycleDurationMs = 1200,
                ObservedCounter = i + 1,
                Quality = TelemetryQualities.Good,
                SourceTimestampUtc = intensityStart.AddMinutes(i * 2),
                ReceivedAtUtc = seedTimestampUtc,
                SourceSequence = i + 1,
                IdempotencyKey = $"energy2-intensity-cycle-{i}",
                IsDuplicate = false,
                IsOutOfOrder = false,
                CreatedAtUtc = seedTimestampUtc,
            });
        }
    }

    private static async Task EnsureBaselineProfileAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        int durationSeconds,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.EnergyBaselineProfiles
            .IgnoreQueryFilters()
            .AnyAsync(b => b.Id == DevelopmentPilotIds.Energy2PilotBaselineInj1Id, cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.EnergyBaselineProfiles.Add(new EnergyBaselineProfile
        {
            Id = DevelopmentPilotIds.Energy2PilotBaselineInj1Id,
            TenantId = tenantId,
            FacilityId = facilityId,
            BaselineReference = "BL-machine-EM-INJ-001-28800-none-none",
            ComparisonDimension = EnergyBaselineDimensions.Machine,
            MeasurementPointId = DevelopmentPilotIds.EnergyPilotPointInj1Id,
            MachineAssetId = DevelopmentPilotIds.EdgePilotPressMachineId,
            ReportingWindowDurationSeconds = durationSeconds,
            SampleCount = 6,
            BaselineConsumedKwh = 37.8m,
            BaselineMaximumDemandKw = 18.5m,
            MedianCenter = 37.8m,
            MadDispersion = 0.9m,
            NormalizedDispersion = 1.3343m,
            RelativeTolerance = EnergyIntelligenceConstants.DefaultRelativeTolerance,
            UpperThresholdConsumedKwh = 41.58m,
            UpperThresholdDemandKw = 20.35m,
            CalculationMethod = "median-mad-robust",
            EvidenceStatus = EnergyEvidenceStates.Simulated,
            SourceWindowReferencesJson = "[\"development-pilot-baseline-samples\"]",
            ValidFromUtc = seedTimestampUtc,
            ValidToUtc = seedTimestampUtc.AddYears(1),
            RuleSetVersion = EnergyIntelligenceConstants.RuleSetVersion,
            Provenance = provenance,
            Version = 1,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
            UpdatedBy = actorSubjectId,
        });
    }

    private static async Task EnsureTargetsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CancellationToken cancellationToken)
    {
        await EnsureTargetAsync(
            dbContext,
            DevelopmentPilotIds.Energy2PilotTargetConsumptionInj1Id,
            tenantId,
            facilityId,
            "TGT-CONSUMPTION-INJ-001",
            "measurement-point",
            "EM-INJ-001",
            EnergyTargetMetrics.ConsumedKwh,
            45m,
            "kWh",
            0.15m,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureTargetAsync(
            dbContext,
            DevelopmentPilotIds.Energy2PilotTargetDemandCompressorId,
            tenantId,
            facilityId,
            "TGT-DEMAND-COMP-001",
            "measurement-point",
            "EM-COMP-001",
            EnergyTargetMetrics.MaximumDemandKw,
            15m,
            "kW",
            0m,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureTargetAsync(
            dbContext,
            DevelopmentPilotIds.Energy2PilotTargetIdleDryerId,
            tenantId,
            facilityId,
            "TGT-IDLE-DRYER-001",
            "measurement-point",
            "EM-DRYER-001",
            EnergyTargetMetrics.IdleConsumptionKwh,
            5m,
            "kWh",
            0.5m,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureTargetAsync(
            dbContext,
            DevelopmentPilotIds.Energy2PilotTargetIntensityId,
            tenantId,
            facilityId,
            "TGT-INTENSITY-INJ-001",
            "production-context",
            DevelopmentPilotIds.Energy2PilotProductionContext,
            EnergyTargetMetrics.KwhPer1000GoodParts,
            8m,
            "kWh/1000-good-parts",
            0.5m,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);
    }

    private static async Task EnsureTargetAsync(
        CrevaFactOSDbContext dbContext,
        Guid id,
        Guid tenantId,
        Guid facilityId,
        string reference,
        string scope,
        string scopeReference,
        string metric,
        decimal value,
        string unit,
        decimal tolerance,
        string provenance,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.EnergyTargets
            .IgnoreQueryFilters()
            .AnyAsync(t => t.Id == id, cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.EnergyTargets.Add(new EnergyTarget
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            TargetReference = reference,
            TargetScope = scope,
            ScopeReference = scopeReference,
            TargetMetric = metric,
            TargetValue = value,
            Unit = unit,
            Tolerance = tolerance,
            EffectiveFromUtc = seedTimestampUtc.AddYears(-1),
            EffectiveToUtc = seedTimestampUtc.AddYears(1),
            SourceProvenance = provenance,
            Status = EnergyTargetStatuses.Approved,
            Version = 1,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
            UpdatedBy = actorSubjectId,
        });
    }

    private static async Task EnsureAssessmentsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        DateTimeOffset baselineWindowStart,
        int durationSeconds,
        CancellationToken cancellationToken)
    {
        var baselineEnd = baselineWindowStart.AddSeconds(durationSeconds);

        await EnsureAssessmentAsync(
            dbContext,
            DevelopmentPilotIds.Energy2PilotAssessmentWithinTargetId,
            DevelopmentPilotIds.Energy2PilotAssessmentWithinTargetRef,
            tenantId,
            facilityId,
            EnergyBaselineDimensions.Machine,
            DevelopmentPilotIds.EnergyPilotPointInj1Id,
            DevelopmentPilotIds.EnergyPilotWindowCalculatedId,
            DevelopmentPilotIds.Energy2PilotBaselineInj1Id,
            DevelopmentPilotIds.Energy2PilotTargetConsumptionInj1Id,
            baselineWindowStart,
            baselineEnd,
            38.5m,
            18.5m,
            -6.5m,
            -14.44m,
            null,
            null,
            null,
            null,
            EnergyIntelligenceOutcomes.WithinTarget,
            EnergyIntelligenceDriverReasonCodes.WithinTarget,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        var peakStart = baselineWindowStart.AddDays(-3).AddHours(2);
        var peakEnd = peakStart.AddHours(8);
        await EnsureAssessmentAsync(
            dbContext,
            DevelopmentPilotIds.Energy2PilotAssessmentPeakDemandId,
            DevelopmentPilotIds.Energy2PilotAssessmentPeakDemandRef,
            tenantId,
            facilityId,
            EnergyBaselineDimensions.AuxiliaryEquipment,
            DevelopmentPilotIds.EnergyPilotPointCompressorId,
            DevelopmentPilotIds.Energy2PilotWindowPeakCompressorId,
            null,
            DevelopmentPilotIds.Energy2PilotTargetDemandCompressorId,
            peakStart,
            peakEnd,
            48.5m,
            22.5m,
            null,
            null,
            null,
            null,
            null,
            null,
            EnergyIntelligenceOutcomes.Attention,
            EnergyIntelligenceDriverReasonCodes.PeakDemandExceeded,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken,
            peakDriverActual: "22.5",
            peakDriverComparison: "15");

        var idleStart = baselineWindowStart.AddDays(-2).AddHours(16);
        var idleEnd = idleStart.AddHours(8);
        await EnsureAssessmentAsync(
            dbContext,
            DevelopmentPilotIds.Energy2PilotAssessmentIdleId,
            DevelopmentPilotIds.Energy2PilotAssessmentIdleRef,
            tenantId,
            facilityId,
            EnergyBaselineDimensions.AuxiliaryEquipment,
            DevelopmentPilotIds.EnergyPilotPointDryerId,
            DevelopmentPilotIds.Energy2PilotWindowIdleDryerId,
            null,
            DevelopmentPilotIds.Energy2PilotTargetIdleDryerId,
            idleStart,
            idleEnd,
            8.2m,
            4.5m,
            null,
            null,
            null,
            null,
            null,
            null,
            EnergyIntelligenceOutcomes.Attention,
            EnergyIntelligenceDriverReasonCodes.IdleConsumptionElevated,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken,
            idleDriverActual: "8.2",
            idleDriverComparison: "5.5");

        var anchor = seedTimestampUtc.UtcDateTime.Date;
        var intensityStart = new DateTimeOffset(anchor.AddDays(-4).AddHours(6), TimeSpan.Zero);
        var intensityEnd = intensityStart.AddHours(8);
        await EnsureAssessmentAsync(
            dbContext,
            DevelopmentPilotIds.Energy2PilotAssessmentIntensityId,
            DevelopmentPilotIds.Energy2PilotAssessmentIntensityRef,
            tenantId,
            facilityId,
            EnergyBaselineDimensions.ProductionContext,
            DevelopmentPilotIds.EnergyPilotPointInj1Id,
            DevelopmentPilotIds.Energy2PilotWindowIntensityId,
            null,
            DevelopmentPilotIds.Energy2PilotTargetIntensityId,
            intensityStart,
            intensityEnd,
            20m,
            16m,
            null,
            null,
            null,
            null,
            null,
            null,
            EnergyIntelligenceOutcomes.Attention,
            EnergyIntelligenceDriverReasonCodes.EnergyIntensityElevated,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken,
            intensityDriverActual: "40",
            intensityDriverComparison: "8",
            productionContext: DevelopmentPilotIds.Energy2PilotProductionContext);

        var insufficientStart = baselineEnd.AddHours(1);
        var insufficientEnd = insufficientStart.AddHours(4);
        await EnsureAssessmentAsync(
            dbContext,
            DevelopmentPilotIds.Energy2PilotAssessmentInsufficientId,
            DevelopmentPilotIds.Energy2PilotAssessmentInsufficientRef,
            tenantId,
            facilityId,
            EnergyBaselineDimensions.Machine,
            DevelopmentPilotIds.EnergyPilotPointInj2Id,
            DevelopmentPilotIds.EnergyPilotWindowInsufficientId,
            null,
            null,
            insufficientStart,
            insufficientEnd,
            0m,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            EnergyIntelligenceOutcomes.InsufficientData,
            EnergyIntelligenceDriverReasonCodes.InsufficientComparableWindows,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        var conflictStart = baselineWindowStart.AddHours(6);
        var conflictEnd = conflictStart.AddHours(4);
        await EnsureAssessmentAsync(
            dbContext,
            DevelopmentPilotIds.Energy2PilotAssessmentSourceConflictId,
            DevelopmentPilotIds.Energy2PilotAssessmentSourceConflictRef,
            tenantId,
            facilityId,
            EnergyBaselineDimensions.Machine,
            DevelopmentPilotIds.EnergyPilotPointInj3Id,
            DevelopmentPilotIds.EnergyPilotWindowConflictId,
            null,
            null,
            conflictStart,
            conflictEnd,
            0m,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            EnergyIntelligenceOutcomes.SourceConflict,
            EnergyIntelligenceDriverReasonCodes.SourceConflict,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);
    }

    private static async Task EnsureAssessmentAsync(
        CrevaFactOSDbContext dbContext,
        Guid id,
        string reference,
        Guid tenantId,
        Guid facilityId,
        string comparisonDimension,
        Guid measurementPointId,
        Guid consumptionWindowId,
        Guid? baselineId,
        Guid? targetId,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        decimal consumedKwh,
        decimal? maximumDemandKw,
        decimal? varianceKwh,
        decimal? variancePercent,
        decimal? potentialReducible,
        decimal? costImpact,
        string? currency,
        string? opportunityLabel,
        string outcome,
        string leadingDriverCode,
        string provenance,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken,
        string? peakDriverActual = null,
        string? peakDriverComparison = null,
        string? idleDriverActual = null,
        string? idleDriverComparison = null,
        string? intensityDriverActual = null,
        string? intensityDriverComparison = null,
        string? productionContext = null)
    {
        var exists = await dbContext.EnergyIntelligenceAssessments
            .IgnoreQueryFilters()
            .AnyAsync(a => a.Id == id, cancellationToken);

        if (exists)
        {
            return;
        }

        var driverId = Guid.NewGuid();
        var assessment = new EnergyIntelligenceAssessment
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            AssessmentReference = reference,
            ComparisonDimension = comparisonDimension,
            MeasurementPointId = measurementPointId,
            ConsumptionWindowId = consumptionWindowId,
            BaselineProfileId = baselineId,
            TargetId = targetId,
            EvaluationWindowStartUtc = windowStart,
            EvaluationWindowEndUtc = windowEnd,
            ActualConsumedKwh = consumedKwh,
            ActualMaximumDemandKw = maximumDemandKw,
            VarianceConsumedKwh = varianceKwh,
            VariancePercent = variancePercent,
            PotentialReducibleKwh = potentialReducible,
            EstimatedCostImpact = costImpact,
            CostCurrencyCode = currency,
            OpportunityLabel = opportunityLabel,
            Outcome = outcome,
            LeadingDriverCode = leadingDriverCode,
            LeadingDriverId = driverId,
            RuleSetVersion = EnergyIntelligenceConstants.RuleSetVersion,
            CalculationMethod = "deterministic-intelligence",
            EvidenceStatus = outcome == EnergyIntelligenceOutcomes.SourceConflict
                ? EnergyEvidenceStates.SourceConflict
                : outcome == EnergyIntelligenceOutcomes.InsufficientData
                    ? EnergyEvidenceStates.InsufficientData
                    : EnergyEvidenceStates.Simulated,
            FreshnessState = EnergyFreshnessStates.Fresh,
            Version = 1,
            EvaluatedAtUtc = seedTimestampUtc,
            CreatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
        };

        var driver = BuildLeadingDriver(
            driverId,
            tenantId,
            facilityId,
            assessment.Id,
            leadingDriverCode,
            peakDriverActual,
            peakDriverComparison,
            idleDriverActual,
            idleDriverComparison,
            intensityDriverActual,
            intensityDriverComparison,
            productionContext);

        assessment.Drivers.Add(driver);
        dbContext.EnergyIntelligenceAssessments.Add(assessment);
    }

    private static EnergyIntelligenceDriver BuildLeadingDriver(
        Guid driverId,
        Guid tenantId,
        Guid facilityId,
        Guid assessmentId,
        string reasonCode,
        string? peakActual,
        string? peakComparison,
        string? idleActual,
        string? idleComparison,
        string? intensityActual,
        string? intensityComparison,
        string? productionContext)
    {
        var (severity, category, actual, actualUnit, comparison, comparisonUnit, explanation) = reasonCode switch
        {
            EnergyIntelligenceDriverReasonCodes.WithinTarget => (
                EnergyIntelligenceDriverSeverities.Pass,
                EnergyIntelligenceDriverCategories.Target,
                "38.5", "kWh", "45", "kWh",
                "Consumption is within approved target tolerance."),
            EnergyIntelligenceDriverReasonCodes.PeakDemandExceeded => (
                EnergyIntelligenceDriverSeverities.Fail,
                EnergyIntelligenceDriverCategories.PeakDemand,
                peakActual, "kW", peakComparison, "kW",
                "Peak demand exceeded approved threshold."),
            EnergyIntelligenceDriverReasonCodes.IdleConsumptionElevated => (
                EnergyIntelligenceDriverSeverities.Warn,
                EnergyIntelligenceDriverCategories.Idle,
                idleActual, "kWh", idleComparison, "kWh",
                "Idle consumption exceeds allowed threshold with verified zero production."),
            EnergyIntelligenceDriverReasonCodes.EnergyIntensityElevated => (
                EnergyIntelligenceDriverSeverities.Warn,
                EnergyIntelligenceDriverCategories.Intensity,
                intensityActual, "kWh/1000-good-parts", intensityComparison, "kWh/1000-good-parts",
                "Energy intensity exceeds approved target."),
            EnergyIntelligenceDriverReasonCodes.InsufficientComparableWindows => (
                EnergyIntelligenceDriverSeverities.Info,
                EnergyIntelligenceDriverCategories.Evidence,
                null, null, null, null,
                "No comparable baseline profile is available."),
            EnergyIntelligenceDriverReasonCodes.SourceConflict => (
                EnergyIntelligenceDriverSeverities.Fail,
                EnergyIntelligenceDriverCategories.Evidence,
                "0", "kWh", null, null,
                "Source conflict in evaluation window evidence."),
            _ => (
                EnergyIntelligenceDriverSeverities.Info,
                EnergyIntelligenceDriverCategories.Evidence,
                null, null, null, null,
                reasonCode),
        };

        return new EnergyIntelligenceDriver
        {
            Id = driverId,
            TenantId = tenantId,
            FacilityId = facilityId,
            AssessmentId = assessmentId,
            ReasonCode = reasonCode,
            LabelTr = reasonCode,
            Severity = severity,
            Category = category,
            ActualValue = actual,
            ActualUnit = actualUnit,
            ComparisonValue = comparison,
            ComparisonUnit = comparisonUnit,
            StructuredEvidenceJson = $"{{\"productionContext\":\"{productionContext ?? ""}\"}}",
            EvidenceSource = "energy-intelligence",
            FreshnessState = EnergyFreshnessStates.Fresh,
            Explanation = explanation,
            RuleSetVersion = EnergyIntelligenceConstants.RuleSetVersion,
            IsLeadingDriver = true,
        };
    }

    private static async Task EnsureWindowAsync(
        CrevaFactOSDbContext dbContext,
        Guid id,
        Guid tenantId,
        Guid facilityId,
        Guid measurementPointId,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        decimal consumedKwh,
        decimal? maximumDemandKw,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.EnergyConsumptionWindows
            .IgnoreQueryFilters()
            .AnyAsync(w => w.Id == id, cancellationToken);

        if (exists)
        {
            return;
        }

        var durationHours = (decimal)(windowEndUtc - windowStartUtc).TotalHours;
        decimal? averagePower = durationHours > 0 && consumedKwh > 0
            ? EnergyCalculator.RoundEnergy(consumedKwh / durationHours)
            : null;

        dbContext.EnergyConsumptionWindows.Add(new EnergyConsumptionWindow
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            MeasurementPointId = measurementPointId,
            WindowStartUtc = windowStartUtc,
            WindowEndUtc = windowEndUtc,
            ConsumedKwh = consumedKwh,
            AveragePowerKw = averagePower,
            MaximumDemandKw = maximumDemandKw,
            CalculationMethod = EnergyCalculationMethods.IntervalSum,
            EvidenceStatus = EnergyEvidenceStates.Simulated,
            ReadingCount = 3,
            FreshnessState = EnergyFreshnessStates.Fresh,
            SourceReferencesJson = "[\"development-pilot-energy2\"]",
            RuleSetVersion = EnergyConstants.RuleSetVersion,
            CalculatedAtUtc = seedTimestampUtc,
            CreatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
        });
    }
}
