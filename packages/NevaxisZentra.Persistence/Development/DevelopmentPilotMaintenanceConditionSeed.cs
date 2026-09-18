using NevaxisZentra.Persistence.Edge;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Development;

internal static class DevelopmentPilotMaintenanceConditionSeed
{
    public static async Task EnsureMaintenanceConditionPilotDataAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        await EnsureMappingAsync(
            dbContext,
            DevelopmentPilotIds.MaintenanceConditionCncMappingId,
            tenantId,
            facilityId,
            "ASSET-CNC-001",
            "machine",
            "asset-cnc-001",
            "CNC Tezgâh 001",
            DevelopmentPilotIds.EdgePilotCncMachineId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureMappingAsync(
            dbContext,
            DevelopmentPilotIds.MaintenanceConditionPressMappingId,
            tenantId,
            facilityId,
            "ASSET-PRESS-001",
            "machine",
            "asset-press-001",
            "Pres Makinesi 001",
            DevelopmentPilotIds.EdgePilotPressMachineId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureMetricDefinitionAsync(
            dbContext,
            DevelopmentPilotIds.MaintenanceConditionMetricTemperatureId,
            tenantId,
            facilityId,
            TelemetryMetricKeys.TemperatureZone1,
            "Sıcaklık (Bölge 1)",
            "°C",
            deviationThresholdPercent: 4m,
            volatilityThresholdPercent: null,
            seedTimestampUtc,
            cancellationToken);

        await EnsureMetricDefinitionAsync(
            dbContext,
            DevelopmentPilotIds.MaintenanceConditionMetricPressureId,
            tenantId,
            facilityId,
            TelemetryMetricKeys.HydraulicPressure,
            "Hidrolik Basınç",
            "bar",
            deviationThresholdPercent: 8m,
            volatilityThresholdPercent: 12m,
            seedTimestampUtc,
            cancellationToken);

        await EnsureMetricDefinitionAsync(
            dbContext,
            DevelopmentPilotIds.MaintenanceConditionMetricCycleTimeId,
            tenantId,
            facilityId,
            TelemetryMetricKeys.CycleTimeMs,
            "Çevrim Süresi",
            "ms",
            deviationThresholdPercent: 8m,
            volatilityThresholdPercent: null,
            seedTimestampUtc,
            cancellationToken);
    }

    private static async Task EnsureMappingAsync(
        NevaxisZentraDbContext dbContext,
        Guid id,
        Guid tenantId,
        Guid facilityId,
        string assetReference,
        string assetType,
        string assetId,
        string assetName,
        Guid machineId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.MaintenanceAssetMachineMappings
            .IgnoreQueryFilters()
            .AnyAsync(m => m.Id == id, cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.MaintenanceAssetMachineMappings.Add(new Maintenance.MaintenanceAssetMachineMapping
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            AssetType = assetType,
            AssetId = assetId,
            AssetReference = assetReference,
            AssetName = assetName,
            MachineId = machineId,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            Version = 1,
        });
    }

    private static async Task EnsureMetricDefinitionAsync(
        NevaxisZentraDbContext dbContext,
        Guid id,
        Guid tenantId,
        Guid facilityId,
        string metricKey,
        string displayName,
        string expectedUnit,
        decimal deviationThresholdPercent,
        decimal? volatilityThresholdPercent,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.MaintenanceConditionMetricDefinitions
            .IgnoreQueryFilters()
            .AnyAsync(d => d.Id == id, cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.MaintenanceConditionMetricDefinitions.Add(new Maintenance.MaintenanceConditionMetricDefinition
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            MetricKey = metricKey,
            DisplayName = displayName,
            ExpectedUnit = expectedUnit,
            EvaluationWindowHours = 24,
            MinimumSamples = 8,
            BaselineMethod = Maintenance.MaintenanceConditionBaselineMethods.Median,
            DeviationThresholdPercent = deviationThresholdPercent,
            TrendThresholdPercent = 3m,
            VolatilityThresholdPercent = volatilityThresholdPercent,
            Enabled = true,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            Version = 1,
        });
    }
}
