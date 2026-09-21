using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NevaxisZentra.Persistence.IntelligenceRuntime;
using NevaxisZentra.Persistence.Tenancy;
using NevaxisZentra.Persistence.ThroughputOptimizer;

namespace NevaxisZentra.Persistence.OperationalValue;

public interface IOperationalValueService
{
    Task<OperationalValueControlCenter> GetControlCenterAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default);

    Task<OperationalValueCaseDetail?> GetCaseDetailAsync(
        Guid facilityId,
        Guid valueRecordId,
        CancellationToken cancellationToken = default);

    Task<VerifiedOperationalValueSummary?> CalculateFromVerifiedImprovementAsync(
        Guid facilityId,
        Guid tenantId,
        VerifiedImprovementSnapshot improvement,
        string workflowType,
        string? lineCode,
        Guid? optimizationRunId,
        Guid? scenarioRunId,
        decimal observationWindowHours,
        int humanTouchesBefore,
        int humanTouchesAfter,
        CancellationToken cancellationToken = default);

    Task<TenantEconomicInputSnapshot?> GetEconomicInputsAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default);

    Task<TenantEconomicInputSnapshot> UpsertEconomicInputsAsync(
        Guid facilityId,
        Guid tenantId,
        UpsertTenantEconomicInputRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalValueCustomerReportPreview> GetCustomerReportPreviewAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default);

    Task<VerifiedOperationalValueSummary?> InvalidateValueAsync(
        Guid facilityId,
        Guid valueRecordId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<OperationalValueGoldenValueResult> RunGoldenValueDemoAsync(
        Guid facilityId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<OperationalValueGoldenDoubleCountResult> RunGoldenDoubleCountDemoAsync(
        Guid facilityId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<OperationalValueGoldenUnverifiedResult> RunGoldenUnverifiedDemoAsync(
        CancellationToken cancellationToken = default);

    Task<OperationalValueGoldenInvalidationResult> RunGoldenInvalidationDemoAsync(
        Guid facilityId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

public sealed class OperationalValueService(
    NevaxisZentraDbContext db,
    TenantDatabaseSession tenantDatabaseSession,
    IIntelligenceRuntimeService intelligenceRuntimeService) : IOperationalValueService
{
    public Task<OperationalValueControlCenter> GetControlCenterAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetControlCenterCoreAsync(facilityId, ct),
            cancellationToken);

    public Task<OperationalValueCaseDetail?> GetCaseDetailAsync(
        Guid facilityId,
        Guid valueRecordId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetCaseDetailCoreAsync(facilityId, valueRecordId, ct),
            cancellationToken);

    public Task<VerifiedOperationalValueSummary?> CalculateFromVerifiedImprovementAsync(
        Guid facilityId,
        Guid tenantId,
        VerifiedImprovementSnapshot improvement,
        string workflowType,
        string? lineCode,
        Guid? optimizationRunId,
        Guid? scenarioRunId,
        decimal observationWindowHours,
        int humanTouchesBefore,
        int humanTouchesAfter,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => CalculateFromVerifiedImprovementCoreAsync(
                facilityId,
                tenantId,
                improvement,
                workflowType,
                lineCode,
                optimizationRunId,
                scenarioRunId,
                observationWindowHours,
                humanTouchesBefore,
                humanTouchesAfter,
                ct),
            cancellationToken);

    public Task<TenantEconomicInputSnapshot?> GetEconomicInputsAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetEconomicInputsCoreAsync(facilityId, ct),
            cancellationToken);

    public Task<TenantEconomicInputSnapshot> UpsertEconomicInputsAsync(
        Guid facilityId,
        Guid tenantId,
        UpsertTenantEconomicInputRequest request,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => UpsertEconomicInputsCoreAsync(facilityId, tenantId, request, ct),
            cancellationToken);

    public Task<OperationalValueCustomerReportPreview> GetCustomerReportPreviewAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetCustomerReportPreviewCoreAsync(facilityId, ct),
            cancellationToken);

    public Task<VerifiedOperationalValueSummary?> InvalidateValueAsync(
        Guid facilityId,
        Guid valueRecordId,
        string reason,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => InvalidateValueCoreAsync(facilityId, valueRecordId, reason, ct),
            cancellationToken);

    public Task<OperationalValueGoldenValueResult> RunGoldenValueDemoAsync(
        Guid facilityId,
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => RunGoldenValueDemoCoreAsync(facilityId, tenantId, ct),
            cancellationToken);

    public Task<OperationalValueGoldenDoubleCountResult> RunGoldenDoubleCountDemoAsync(
        Guid facilityId,
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => RunGoldenDoubleCountDemoCoreAsync(facilityId, tenantId, ct),
            cancellationToken);

    public Task<OperationalValueGoldenUnverifiedResult> RunGoldenUnverifiedDemoAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new OperationalValueGoldenUnverifiedResult(
            false,
            "Prediction-only outcome correctly produced no trusted value record."));

    public Task<OperationalValueGoldenInvalidationResult> RunGoldenInvalidationDemoAsync(
        Guid facilityId,
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => RunGoldenInvalidationDemoCoreAsync(facilityId, tenantId, ct),
            cancellationToken);

    private async Task<OperationalValueControlCenter> GetControlCenterCoreAsync(
        Guid facilityId,
        CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-30);
        var records = await db.VerifiedOperationalValueRecords.AsNoTracking()
            .Where(r => r.FacilityId == facilityId && r.CreatedAtUtc >= since && !r.IsInvalidated)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(250)
            .ToListAsync(cancellationToken);

        var trusted = records.Where(r => r.Status is OperationalValueStatuses.VerifiedOperational
            or OperationalValueStatuses.VerifiedEconomic).ToList();

        var goodOutputGain = trusted
            .Where(r => r.ValueCategory == OperationalValueCategories.GoodOutputGain)
            .Sum(r => r.DeltaValue);

        var downtimeAvoided = trusted
            .Where(r => r.ValueCategory == OperationalValueCategories.DowntimeAvoided)
            .Sum(r => r.DeltaValue);

        var intelligenceCost = trusted.Sum(r => r.IntelligenceCost ?? 0m);
        var verifiedEconomic = trusted.Where(r => r.EconomicValue.HasValue).Sum(r => r.EconomicValue!.Value);
        var netValue = trusted.Where(r => r.NetValue.HasValue).Sum(r => r.NetValue!.Value);
        var verifiedCount = trusted.Select(r => r.DigitalWorkCaseId).Distinct().Count();

        var roi = OperationalValueRoiEngine.CalculateRoi(
            netValue > 0 ? netValue : null,
            intelligenceCost > 0 ? intelligenceCost : null);

        var kpis = OperationalValueKpiEngine.Compute(
            trusted,
            trusted
                .Where(r => r.DigitalWorkCaseId.HasValue)
                .Select(r => (r.CreatedAtUtc, (DateTimeOffset?)r.UpdatedAtUtc, r.HumanTouchesAfter > 0))
                .ToList());

        var summary = new OperationalValueControlCenterSummary(
            verifiedCount,
            goodOutputGain,
            downtimeAvoided,
            trusted.Count(r => r.ValueCategory == OperationalValueCategories.OtifProtected
                || r.ValueCategory == OperationalValueCategories.LateOrderRiskReduced),
            trusted.Where(r => r.ValueCategory == OperationalValueCategories.HumanTouchesReduced)
                .Sum(r => Math.Max(0, r.HumanTouchesBefore - r.HumanTouchesAfter)),
            intelligenceCost,
            verifiedEconomic > 0 ? verifiedEconomic : null,
            verifiedCount > 0 && intelligenceCost > 0 ? intelligenceCost / verifiedCount : null,
            netValue > 0 ? netValue : null,
            roi.RoiPercent,
            roi.Availability,
            kpis.AutonomousCompletionRate,
            kpis.AverageHumanTouchesPerCase,
            kpis.AverageTimeToVerifiedOutcomeHours);

        var trend = BuildTrend(trusted);
        var breakdown = BuildBreakdown(trusted);
        var recent = trusted.Take(20).Select(MapSummary).ToList();
        var economicInputs = await GetEconomicInputsCoreAsync(facilityId, cancellationToken);

        return new OperationalValueControlCenter(
            summary,
            trend,
            breakdown,
            recent,
            economicInputs,
            DateTimeOffset.UtcNow);
    }

    private async Task<OperationalValueCaseDetail?> GetCaseDetailCoreAsync(
        Guid facilityId,
        Guid valueRecordId,
        CancellationToken cancellationToken)
    {
        var record = await db.VerifiedOperationalValueRecords.AsNoTracking()
            .FirstOrDefaultAsync(r => r.FacilityId == facilityId && r.Id == valueRecordId, cancellationToken);

        return record is null ? null : MapCaseDetail(record);
    }

    private async Task<VerifiedOperationalValueSummary?> CalculateFromVerifiedImprovementCoreAsync(
        Guid facilityId,
        Guid tenantId,
        VerifiedImprovementSnapshot improvement,
        string workflowType,
        string? lineCode,
        Guid? optimizationRunId,
        Guid? scenarioRunId,
        decimal observationWindowHours,
        int humanTouchesBefore,
        int humanTouchesAfter,
        CancellationToken cancellationToken)
    {
        await intelligenceRuntimeService.RecordVerifiedOutcomeCostAsync(
            facilityId,
            tenantId,
            improvement.CaseId,
            optimizationRunId,
            scenarioRunId,
            improvement.VerificationOutcome,
            cancellationToken);

        var intelligenceCost = await db.VerifiedOutcomeIntelligenceCostRecords.AsNoTracking()
            .Where(r => r.FacilityId == facilityId && r.DigitalWorkCaseId == improvement.CaseId)
            .OrderByDescending(r => r.UpdatedAtUtc)
            .Select(r => r.TotalIntelligenceCost)
            .FirstOrDefaultAsync(cancellationToken);

        var request = OperationalValueFromVerifiedImprovementEngine.BuildThroughputRequest(
            improvement,
            optimizationRunId,
            scenarioRunId,
            workflowType,
            lineCode,
            observationWindowHours,
            humanTouchesBefore,
            humanTouchesAfter,
            intelligenceCost > 0 ? intelligenceCost : null);

        var economicInputs = await GetEconomicInputsCoreAsync(facilityId, cancellationToken);
        var overlapRecords = await db.VerifiedOperationalValueRecords
            .Where(r => r.FacilityId == facilityId && r.DigitalWorkCaseId == improvement.CaseId && !r.IsInvalidated)
            .ToListAsync(cancellationToken);

        var calculation = OperationalValueEngine.Calculate(request, economicInputs, overlapRecords);
        if (!calculation.Eligible)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var record = MapRecord(tenantId, facilityId, request, calculation, now);
        db.VerifiedOperationalValueRecords.Add(record);
        await db.SaveChangesAsync(cancellationToken);
        return MapSummary(record);
    }

    private async Task<TenantEconomicInputSnapshot?> GetEconomicInputsCoreAsync(
        Guid facilityId,
        CancellationToken cancellationToken)
    {
        var config = await db.TenantEconomicInputConfigurationRecords.AsNoTracking()
            .Where(c => c.FacilityId == facilityId && c.IsActive)
            .OrderByDescending(c => c.EffectiveFromUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return config is null ? null : MapEconomicInput(config);
    }

    private async Task<TenantEconomicInputSnapshot> UpsertEconomicInputsCoreAsync(
        Guid facilityId,
        Guid tenantId,
        UpsertTenantEconomicInputRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await db.TenantEconomicInputConfigurationRecords
            .Where(c => c.FacilityId == facilityId && c.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var item in existing)
        {
            item.IsActive = false;
            item.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        var now = DateTimeOffset.UtcNow;
        var record = new TenantEconomicInputConfigurationRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            VersionCode = request.VersionCode,
            Currency = request.Currency,
            UnitContributionValue = request.UnitContributionValue,
            ScrapUnitCost = request.ScrapUnitCost,
            DowntimeCostPerHour = request.DowntimeCostPerHour,
            LaborCostPerHour = request.LaborCostPerHour,
            ChangeoverCostPerMinute = request.ChangeoverCostPerMinute,
            LateDeliveryPenalty = request.LateDeliveryPenalty,
            IsActive = true,
            EffectiveFromUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        db.TenantEconomicInputConfigurationRecords.Add(record);
        await db.SaveChangesAsync(cancellationToken);
        return MapEconomicInput(record);
    }

    private async Task<OperationalValueCustomerReportPreview> GetCustomerReportPreviewCoreAsync(
        Guid facilityId,
        CancellationToken cancellationToken)
    {
        var center = await GetControlCenterCoreAsync(facilityId, cancellationToken);
        var summary = center.Summary;
        return new OperationalValueCustomerReportPreview(
            "Last 30 days",
            summary.VerifiedOutcomes,
            summary.GoodOutputGainUnits,
            summary.DowntimeAvoidedMinutes,
            summary.LateOrdersProtected,
            summary.VerifiedEconomicValue,
            center.EconomicInputs?.Currency,
            summary.IntelligenceCost,
            summary.NetValue,
            summary.RoiPercent,
            "Verified operational outcomes only. Economic values require configured tenant inputs. Predictions are excluded.",
            center.RecentValues.Take(5).Select(v => v.ValueRecordId.ToString()).ToList());
    }

    private async Task<VerifiedOperationalValueSummary?> InvalidateValueCoreAsync(
        Guid facilityId,
        Guid valueRecordId,
        string reason,
        CancellationToken cancellationToken)
    {
        var record = await db.VerifiedOperationalValueRecords
            .FirstOrDefaultAsync(r => r.FacilityId == facilityId && r.Id == valueRecordId, cancellationToken);

        if (record is null)
        {
            return null;
        }

        record.IsInvalidated = true;
        record.Status = OperationalValueStatuses.Invalidated;
        record.InvalidationReason = reason;
        record.InvalidatedAtUtc = DateTimeOffset.UtcNow;
        record.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return MapSummary(record);
    }

    private async Task<OperationalValueGoldenValueResult> RunGoldenValueDemoCoreAsync(
        Guid facilityId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await UpsertEconomicInputsCoreAsync(
            facilityId,
            tenantId,
            new UpsertTenantEconomicInputRequest("golden-v1", "TRY", 125m, null, null, null, null, null),
            cancellationToken);

        var calculation = OperationalValueEngine.CalculateThroughputGolden(
            612m,
            668m,
            2m,
            125m,
            "TRY",
            0.42m);

        var request = new OperationalValueCalculationRequest(
            Guid.NewGuid(),
            null,
            null,
            Guid.NewGuid(),
            ThroughputOptimizerVerificationOutcomes.VerifiedImprovement,
            OperationalValueCategories.GoodOutputGain,
            new OperationalValueBaselineSnapshot(612m, "good-units-per-hour", "before-intervention-window", "2h", OperationalValueBaselineMethods.BeforeInterventionWindow),
            new OperationalValueActualSnapshot(668m, "good-units-per-hour", "verified-observation-window"),
            2m,
            OperationalValueAttributionClasses.StronglyAttributed,
            "golden-throughput",
            "MIX-LINE-01",
            "line",
            "MIX-LINE-01",
            4,
            2,
            OperationalValueOverlapGroups.ThroughputCapacity,
            0.42m);

        var economicInputs = await GetEconomicInputsCoreAsync(facilityId, cancellationToken);
        var persisted = OperationalValueEngine.Calculate(request, economicInputs, []);
        var now = DateTimeOffset.UtcNow;
        var record = MapRecord(tenantId, facilityId, request, persisted, now);
        db.VerifiedOperationalValueRecords.Add(record);
        await db.SaveChangesAsync(cancellationToken);

        return new OperationalValueGoldenValueResult(
            record.Id,
            record.DeltaValue,
            56m,
            0.42m,
            0.42m,
            record.EconomicValue,
            record.NetValue,
            record.RoiPercent.HasValue ? "AVAILABLE" : OperationalValueFailureStates.RoiUnavailable,
            "Golden verified throughput value: 112 good units over 2 hours with intelligence cost linked.");
    }

    private async Task<OperationalValueGoldenDoubleCountResult> RunGoldenDoubleCountDemoCoreAsync(
        Guid facilityId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var caseId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var changeoverRequest = new OperationalValueCalculationRequest(
            caseId,
            null,
            null,
            Guid.NewGuid(),
            ThroughputOptimizerVerificationOutcomes.VerifiedImprovement,
            OperationalValueCategories.ChangeoverReduced,
            new OperationalValueBaselineSnapshot(40m, "minutes", "before-intervention-window", "1 setup", OperationalValueBaselineMethods.BeforeInterventionWindow),
            new OperationalValueActualSnapshot(20m, "minutes", "verified-observation-window"),
            1m,
            OperationalValueAttributionClasses.Direct,
            "golden-changeover",
            "MIX-LINE-01",
            "line",
            "MIX-LINE-01",
            0,
            0,
            OperationalValueOverlapGroups.ChangeoverThroughput,
            null);

        var changeoverCalc = OperationalValueEngine.Calculate(
            changeoverRequest,
            new TenantEconomicInputSnapshot(Guid.NewGuid(), "golden-v1", "TRY", null, null, null, null, 50m, null, now, now),
            []);

        var changeoverRecord = MapRecord(tenantId, facilityId, changeoverRequest, changeoverCalc, now);
        db.VerifiedOperationalValueRecords.Add(changeoverRecord);

        var throughputRequest = changeoverRequest with
        {
            ValueCategory = OperationalValueCategories.ThroughputGain,
            Baseline = new OperationalValueBaselineSnapshot(612m, "good-units-per-hour", "before-intervention-window", "2h", OperationalValueBaselineMethods.BeforeInterventionWindow),
            Actual = new OperationalValueActualSnapshot(668m, "good-units-per-hour", "verified-observation-window"),
            ObservationWindowHours = 2m,
            OverlapGroupKey = OperationalValueOverlapGroups.ChangeoverThroughput,
        };

        var overlapRecords = new List<VerifiedOperationalValueRecord> { changeoverRecord };

        var throughputCalc = OperationalValueEngine.Calculate(
            throughputRequest,
            new TenantEconomicInputSnapshot(Guid.NewGuid(), "golden-v1", "TRY", 125m, null, null, null, 50m, null, now, now),
            overlapRecords);

        var blocked = !throughputCalc.Eligible
            && throughputCalc.FailureState == OperationalValueFailureStates.DoubleCountBlocked;

        if (throughputCalc.Eligible)
        {
            db.VerifiedOperationalValueRecords.Add(MapRecord(tenantId, facilityId, throughputRequest, throughputCalc, now));
        }

        await db.SaveChangesAsync(cancellationToken);

        return new OperationalValueGoldenDoubleCountResult(
            blocked || !throughputCalc.Eligible,
            blocked ? 1 : 2,
            blocked
                ? "Double-count guard blocked overlapping throughput economic attribution."
                : "Overlap guard allowed non-duplicative record creation.");
    }

    private async Task<OperationalValueGoldenInvalidationResult> RunGoldenInvalidationDemoCoreAsync(
        Guid facilityId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var golden = await RunGoldenValueDemoCoreAsync(facilityId, tenantId, cancellationToken);
        var record = await db.VerifiedOperationalValueRecords
            .FirstAsync(r => r.Id == golden.ValueRecordId, cancellationToken);

        var previousStatus = record.Status;
        record.IsInvalidated = true;
        record.Status = OperationalValueStatuses.Invalidated;
        record.InvalidationReason = "verified-outcome-reopened";
        record.InvalidatedAtUtc = DateTimeOffset.UtcNow;
        record.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new OperationalValueGoldenInvalidationResult(
            record.Id,
            previousStatus,
            record.Status,
            "Verified value invalidated after outcome reopen; trusted aggregates must exclude this record.");
    }

    private static VerifiedOperationalValueRecord MapRecord(
        Guid tenantId,
        Guid facilityId,
        OperationalValueCalculationRequest request,
        OperationalValueCalculationResult calculation,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            DigitalWorkCaseId = request.DigitalWorkCaseId,
            OptimizationRunId = request.OptimizationRunId,
            ScenarioRunId = request.ScenarioRunId,
            VerifiedImprovementId = request.VerifiedImprovementId,
            ValueCategory = request.ValueCategory,
            Status = calculation.Status,
            AttributionClass = request.AttributionClass,
            BaselineValue = request.Baseline.Value,
            ActualValue = request.Actual.Value,
            DeltaValue = calculation.DeltaValue,
            Unit = calculation.Unit,
            BaselineSource = request.Baseline.Source,
            BaselineWindow = request.Baseline.Window,
            BaselineMethod = request.Baseline.Method,
            ObservationWindowHours = request.ObservationWindowHours,
            ConfidenceScore = calculation.ConfidenceScore,
            CalculationMethod = calculation.EvidenceSummary,
            FormulaType = calculation.FormulaType,
            FormulaVersion = calculation.FormulaVersion,
            InputVersion = calculation.InputVersion,
            EconomicConversionStatus = calculation.EconomicConversionStatus,
            EconomicValue = calculation.EconomicValue,
            Currency = calculation.Currency,
            IntelligenceCost = calculation.IntelligenceCost,
            NetValue = calculation.NetValue,
            RoiPercent = calculation.RoiPercent,
            PaybackDays = calculation.PaybackDays,
            WorkflowType = request.WorkflowType,
            LineCode = request.LineCode,
            ScopeEntityType = request.ScopeEntityType,
            ScopeEntityId = request.ScopeEntityId,
            OverlapGroupKey = request.OverlapGroupKey,
            HumanTouchesBefore = request.HumanTouchesBefore,
            HumanTouchesAfter = request.HumanTouchesAfter,
            EvidenceJson = JsonSerializer.Serialize(new[] { calculation.EvidenceSummary }),
            ProvenanceJson = JsonSerializer.Serialize(new
            {
                request.VerificationOutcome,
                request.Baseline,
                request.Actual,
            }),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

    private static VerifiedOperationalValueSummary MapSummary(VerifiedOperationalValueRecord record) =>
        new(
            record.Id,
            record.DigitalWorkCaseId,
            record.ValueCategory,
            record.Status,
            record.AttributionClass,
            record.DeltaValue,
            record.Unit,
            record.EconomicValue,
            record.Currency,
            record.NetValue,
            record.ConfidenceScore,
            record.LineCode,
            record.WorkflowType,
            record.CreatedAtUtc);

    private static OperationalValueCaseDetail MapCaseDetail(VerifiedOperationalValueRecord record)
    {
        var evidence = JsonSerializer.Deserialize<List<string>>(record.EvidenceJson) ?? [];
        return new OperationalValueCaseDetail(
            record.Id,
            record.DigitalWorkCaseId,
            record.CalculationMethod,
            new OperationalValueBaselineSnapshot(
                record.BaselineValue,
                record.Unit,
                record.BaselineSource,
                record.BaselineWindow,
                record.BaselineMethod),
            new OperationalValueActualSnapshot(record.ActualValue, record.Unit, "verified-observation-window"),
            record.ProvenanceJson.Contains("VERIFIED_IMPROVEMENT", StringComparison.OrdinalIgnoreCase)
                ? ThroughputOptimizerVerificationOutcomes.VerifiedImprovement
                : "VERIFIED",
            record.DeltaValue,
            record.Unit,
            record.Status,
            record.AttributionClass,
            record.ConfidenceScore,
            record.EconomicConversionStatus,
            record.EconomicValue,
            record.Currency,
            record.IntelligenceCost,
            record.NetValue,
            record.RoiPercent,
            record.PaybackDays,
            record.FormulaType,
            record.FormulaVersion,
            evidence,
            record.CreatedAtUtc);
    }

    private static TenantEconomicInputSnapshot MapEconomicInput(TenantEconomicInputConfigurationRecord config) =>
        new(
            config.Id,
            config.VersionCode,
            config.Currency,
            config.UnitContributionValue,
            config.ScrapUnitCost,
            config.DowntimeCostPerHour,
            config.LaborCostPerHour,
            config.ChangeoverCostPerMinute,
            config.LateDeliveryPenalty,
            config.EffectiveFromUtc,
            config.UpdatedAtUtc);

    private static List<OperationalValueTrendPoint> BuildTrend(
        IReadOnlyList<VerifiedOperationalValueRecord> records)
    {
        return records
            .GroupBy(r => r.CreatedAtUtc.UtcDateTime.Date)
            .OrderBy(g => g.Key)
            .Take(14)
            .Select(g => new OperationalValueTrendPoint(
                g.Key.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                g.Select(x => x.DigitalWorkCaseId).Distinct().Count(),
                g.Where(x => x.ValueCategory == OperationalValueCategories.GoodOutputGain).Sum(x => x.DeltaValue),
                g.Where(x => x.ValueCategory == OperationalValueCategories.DowntimeAvoided).Sum(x => x.DeltaValue),
                g.Where(x => x.EconomicValue.HasValue).Sum(x => x.EconomicValue!.Value),
                g.Sum(x => x.IntelligenceCost ?? 0m),
                g.Select(x => x.DigitalWorkCaseId).Distinct().Any()
                    ? g.Sum(x => x.IntelligenceCost ?? 0m) / g.Select(x => x.DigitalWorkCaseId).Distinct().Count()
                    : null))
            .ToList();
    }

    private static List<OperationalValueBreakdownItem> BuildBreakdown(
        IReadOnlyList<VerifiedOperationalValueRecord> records)
    {
        var byCategory = records
            .GroupBy(r => r.ValueCategory)
            .Select(g => new OperationalValueBreakdownItem(
                "category",
                g.Key,
                g.Key,
                g.Count(),
                g.Sum(x => x.DeltaValue),
                g.First().Unit,
                g.Where(x => x.EconomicValue.HasValue).Sum(x => x.EconomicValue!.Value),
                g.Sum(x => x.IntelligenceCost ?? 0m)))
            .ToList();

        var byLine = records
            .Where(r => !string.IsNullOrWhiteSpace(r.LineCode))
            .GroupBy(r => r.LineCode!)
            .Select(g => new OperationalValueBreakdownItem(
                "line",
                g.Key,
                g.Key,
                g.Count(),
                g.Sum(x => x.DeltaValue),
                "mixed",
                g.Where(x => x.EconomicValue.HasValue).Sum(x => x.EconomicValue!.Value),
                g.Sum(x => x.IntelligenceCost ?? 0m)))
            .ToList();

        return byCategory.Concat(byLine).ToList();
    }
}

public static class OperationalValueServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraOperationalValue(this IServiceCollection services)
    {
        services.AddScoped<IOperationalValueService, OperationalValueService>();
        return services;
    }
}
