using System.Security.Cryptography;
using System.Text;
using CrevaFactOS.Persistence.Energy;
using CrevaFactOS.Persistence.IotPlc;
using CrevaFactOS.Persistence.Maintenance;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.OperationalRisk;

public sealed class OperationalRiskEnergySourceAdapter(CrevaFactOSDbContext dbContext) : IOperationalRiskEvidenceSource
{
    public string SourceModule => OperationalRiskSourceModules.Energy;

    public async Task<OperationalRiskSourceAdapterResult> CollectSnapshotsAsync(
        OperationalRiskOperationContext context,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        CancellationToken cancellationToken = default)
    {
        var assessments = await dbContext.EnergyIntelligenceAssessments.AsNoTracking()
            .Where(a => a.TenantId == context.TenantId
                        && a.FacilityId == context.FacilityId
                        && a.EvaluatedAtUtc >= windowStartUtc
                        && a.EvaluatedAtUtc <= windowEndUtc)
            .OrderBy(a => a.AssessmentReference)
            .ToListAsync(cancellationToken);

        if (assessments.Count == 0)
        {
            return OperationalRiskSourceAdapterResult.Empty();
        }

        var snapshots = assessments.Select(a =>
        {
            var scopeType = a.MeasurementPointId.HasValue
                ? OperationalRiskScopeTypes.EnergyMeasurementPoint
                : OperationalRiskScopeTypes.Facility;
            var scopeRef = a.MeasurementPointId?.ToString();
            var isStale = string.Equals(a.FreshnessState, EnergyFreshnessStates.Stale, StringComparison.Ordinal);
            return new OperationalRiskSourceSnapshot(
                a.TenantId,
                a.FacilityId,
                SourceModule,
                "energy-intelligence-assessment",
                a.AssessmentReference,
                OperationalRiskCategories.Energy,
                scopeType,
                scopeRef,
                a.EvaluatedAtUtc,
                a.EvidenceStatus,
                a.Outcome,
                a.ActualConsumedKwh ?? a.ActualMaximumDemandKw,
                null,
                a.ActualConsumedKwh.HasValue ? "kWh" : a.ActualMaximumDemandKw.HasValue ? "kW" : null,
                EnergyIntelligenceConstants.DevelopmentPilotProvenance,
                a.RuleSetVersion,
                a.Id.ToString(),
                isStale,
                true);
        }).ToList();

        return OperationalRiskSourceAdapterResult.Success(snapshots);
    }
}

public sealed class OperationalRiskIotPlcSourceAdapter(CrevaFactOSDbContext dbContext) : IOperationalRiskEvidenceSource
{
    public string SourceModule => OperationalRiskSourceModules.IotPlc;

    public async Task<OperationalRiskSourceAdapterResult> CollectSnapshotsAsync(
        OperationalRiskOperationContext context,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        CancellationToken cancellationToken = default)
    {
        var connectivity = await dbContext.IotPlcConnectivityAssessments.AsNoTracking()
            .Where(a => a.TenantId == context.TenantId
                        && a.FacilityId == context.FacilityId
                        && a.EvaluatedAtUtc >= windowStartUtc
                        && a.EvaluatedAtUtc <= windowEndUtc)
            .OrderBy(a => a.TargetId)
            .ToListAsync(cancellationToken);

        var snapshots = connectivity.Select(a =>
        {
            var isStale = a.FreshnessSeconds.HasValue && a.FreshnessSeconds.Value > 300;
            return new OperationalRiskSourceSnapshot(
                a.TenantId,
                a.FacilityId,
                SourceModule,
                "iot-plc-connectivity-assessment",
                a.TargetId.ToString(),
                OperationalRiskCategories.IotConnectivity,
                OperationalRiskScopeTypes.IotEndpoint,
                a.TargetId.ToString(),
                a.EvaluatedAtUtc,
                MapEvidenceMode(a.EvidenceMode),
                a.Outcome,
                a.HealthObservationAvailabilityRatio,
                null,
                "ratio",
                a.SourceProvenance,
                a.RuleSetVersion,
                a.Id.ToString(),
                isStale,
                true);
        }).ToList();

        return OperationalRiskSourceAdapterResult.Success(snapshots);
    }

    private static string MapEvidenceMode(string evidenceMode) =>
        string.Equals(evidenceMode, IotPlcIntelligenceEvidenceModes.Measured, StringComparison.Ordinal)
            ? OperationalRiskEvidenceStates.Measured
            : OperationalRiskEvidenceStates.Simulated;
}

public sealed class OperationalRiskMaintenanceSourceAdapter(CrevaFactOSDbContext dbContext) : IOperationalRiskEvidenceSource
{
    public string SourceModule => OperationalRiskSourceModules.Maintenance;

    public async Task<OperationalRiskSourceAdapterResult> CollectSnapshotsAsync(
        OperationalRiskOperationContext context,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        CancellationToken cancellationToken = default)
    {
        var assessments = await dbContext.MaintenancePredictiveRiskAssessments.AsNoTracking()
            .Where(a => a.TenantId == context.TenantId
                        && a.FacilityId == context.FacilityId
                        && a.IsCurrent
                        && a.AssessedAtUtc >= windowStartUtc
                        && a.AssessedAtUtc <= windowEndUtc)
            .OrderBy(a => a.AssetReference)
            .ToListAsync(cancellationToken);

        if (assessments.Count == 0)
        {
            return OperationalRiskSourceAdapterResult.Empty();
        }

        var snapshots = assessments.Select(a =>
        {
            var isStale = string.Equals(a.DataFreshness, MaintenanceConditionDataFreshnessLevels.Stale, StringComparison.Ordinal);
            var evidenceState = string.Equals(a.RiskBand, MaintenancePredictiveRiskBands.InsufficientData, StringComparison.Ordinal)
                ? OperationalRiskEvidenceStates.InsufficientData
                : OperationalRiskEvidenceStates.Calculated;
            return new OperationalRiskSourceSnapshot(
                a.TenantId,
                a.FacilityId,
                SourceModule,
                "maintenance-predictive-risk-assessment",
                a.AssetReference,
                OperationalRiskCategories.Maintenance,
                OperationalRiskScopeTypes.Machine,
                a.MachineId.ToString(),
                a.AssessedAtUtc,
                evidenceState,
                a.RiskBand,
                a.ActiveAnomalyCount,
                null,
                "count",
                a.Explanation,
                a.RuleSetVersion,
                a.Id.ToString(),
                isStale,
                true);
        }).ToList();

        return OperationalRiskSourceAdapterResult.Success(snapshots);
    }
}

public sealed class OperationalRiskUnavailableSourceAdapter(string sourceModule) : IOperationalRiskEvidenceSource
{
    public string SourceModule { get; } = sourceModule;

    public Task<OperationalRiskSourceAdapterResult> CollectSnapshotsAsync(
        OperationalRiskOperationContext context,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(OperationalRiskSourceAdapterResult.Unavailable(
            SourceModule,
            "Canonical committed source contract is not available for cross-module detection."));
}

public static class OperationalRiskDetectionKeyBuilder
{
    public static string Build(
        Guid tenantId,
        Guid facilityId,
        string category,
        string scopeType,
        string? scopeReference,
        string sourceModule,
        string sourceReference,
        string ruleCode) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{tenantId:N}|{facilityId:N}|{category}|{scopeType}|{scopeReference ?? ""}|{sourceModule}|{sourceReference}|{ruleCode}")))
            .ToLowerInvariant();
}
