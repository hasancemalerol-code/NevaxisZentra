using CrevaFactOS.Persistence.Energy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Development;

internal static class DevelopmentPilotEnergySeed
{
    internal static async Task EnsureEnergyPilotRecordsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        await EnsureMeasurementPointsAsync(
            dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);
        await EnsureTariffAsync(
            dbContext, tenantId, facilityId, seedTimestampUtc, cancellationToken);
        await EnsureReadingsAsync(
            dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);
        await EnsureConsumptionWindowsAsync(
            dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);
    }

    private static async Task EnsureMeasurementPointsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var provenance = EnergyConstants.DevelopmentPilotProvenance;

        await EnsurePointAsync(
            dbContext,
            DevelopmentPilotIds.EnergyPilotPointInj1Id,
            tenantId,
            facilityId,
            "EM-INJ-001",
            "Enjeksiyon Makinesi INJ-001 Enerji Noktası",
            EnergyMeasurementScopes.Machine,
            DevelopmentPilotIds.EdgePilotPressMachineId,
            "ASSET-PRESS-001",
            EnergyEquipmentCategories.InjectionMachine,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsurePointAsync(
            dbContext,
            DevelopmentPilotIds.EnergyPilotPointInj2Id,
            tenantId,
            facilityId,
            "EM-INJ-002",
            "Enjeksiyon Makinesi INJ-002 Enerji Noktası",
            EnergyMeasurementScopes.Machine,
            DevelopmentPilotIds.EdgePilotInjectionMachine2Id,
            "ASSET-INJ-002",
            EnergyEquipmentCategories.InjectionMachine,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsurePointAsync(
            dbContext,
            DevelopmentPilotIds.EnergyPilotPointInj3Id,
            tenantId,
            facilityId,
            "EM-INJ-003",
            "Enjeksiyon Makinesi INJ-003 Enerji Noktası",
            EnergyMeasurementScopes.Machine,
            DevelopmentPilotIds.EdgePilotInjectionMachine3Id,
            "ASSET-INJ-003",
            EnergyEquipmentCategories.InjectionMachine,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsurePointAsync(
            dbContext,
            DevelopmentPilotIds.EnergyPilotPointChillerId,
            tenantId,
            facilityId,
            "EM-CHILLER-001",
            "Soğutma Grubu Chiller Enerji Noktası",
            EnergyMeasurementScopes.AuxiliaryEquipment,
            null,
            null,
            EnergyEquipmentCategories.Chiller,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsurePointAsync(
            dbContext,
            DevelopmentPilotIds.EnergyPilotPointCompressorId,
            tenantId,
            facilityId,
            "EM-COMP-001",
            "Hava Kompresörü Enerji Noktası",
            EnergyMeasurementScopes.AuxiliaryEquipment,
            null,
            null,
            EnergyEquipmentCategories.Compressor,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsurePointAsync(
            dbContext,
            DevelopmentPilotIds.EnergyPilotPointDryerId,
            tenantId,
            facilityId,
            "EM-DRYER-001",
            "Malzeme Kurutucu Enerji Noktası",
            EnergyMeasurementScopes.AuxiliaryEquipment,
            null,
            null,
            EnergyEquipmentCategories.MaterialDryer,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);
    }

    private static async Task EnsurePointAsync(
        CrevaFactOSDbContext dbContext,
        Guid id,
        Guid tenantId,
        Guid facilityId,
        string pointCode,
        string displayName,
        string scope,
        Guid? machineAssetId,
        string? edgeAssetReference,
        string equipmentCategory,
        string provenance,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.EnergyMeasurementPoints
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Id == id, cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.EnergyMeasurementPoints.Add(new EnergyMeasurementPoint
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            PointCode = pointCode,
            DisplayName = displayName,
            MeasurementScope = scope,
            MachineAssetId = machineAssetId,
            EdgeAssetReference = edgeAssetReference,
            EquipmentCategory = equipmentCategory,
            MeasurementUnit = "kWh",
            Status = EnergyMeasurementPointStatuses.Active,
            SourceProvenance = provenance,
            Version = 1,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
            UpdatedBy = actorSubjectId,
        });
    }

    private static async Task EnsureTariffAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.EnergyTariffSnapshots
            .IgnoreQueryFilters()
            .AnyAsync(t => t.Id == DevelopmentPilotIds.EnergyPilotTariffId, cancellationToken);

        if (exists)
        {
            return;
        }

        var anchor = seedTimestampUtc.UtcDateTime.Date;
        dbContext.EnergyTariffSnapshots.Add(new EnergyTariffSnapshot
        {
            Id = DevelopmentPilotIds.EnergyPilotTariffId,
            TenantId = tenantId,
            FacilityId = facilityId,
            CurrencyCode = "TRY",
            ValidFromUtc = new DateTimeOffset(anchor.AddDays(-30), TimeSpan.Zero),
            ValidToUtc = new DateTimeOffset(anchor.AddDays(365), TimeSpan.Zero),
            EnergyUnitPrice = 0.0001m,
            DemandUnitPrice = 0.01m,
            Provenance = EnergyConstants.DevelopmentTariffProvenance,
            Version = 1,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
        });
    }

    private static async Task EnsureReadingsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var windowStart = new DateTimeOffset(seedTimestampUtc.UtcDateTime.Date.AddDays(-1).AddHours(6), TimeSpan.Zero);
        var interval1Time = windowStart.AddHours(1);
        var interval2Time = windowStart.AddHours(3);
        var interval3Time = windowStart.AddHours(5);

        await EnsureReadingAsync(
            dbContext,
            DevelopmentPilotIds.EnergyPilotReadingInj1Interval1Id,
            tenantId,
            facilityId,
            DevelopmentPilotIds.EnergyPilotPointInj1Id,
            interval1Time,
            seedTimestampUtc,
            EnergyEvidenceStates.Simulated,
            "development-pilot",
            "pilot-interval-1",
            actorSubjectId,
            cancellationToken,
            intervalEnergyKwh: 12.5m,
            activePowerKw: 12.5m);

        await EnsureReadingAsync(
            dbContext,
            DevelopmentPilotIds.EnergyPilotReadingInj1Interval2Id,
            tenantId,
            facilityId,
            DevelopmentPilotIds.EnergyPilotPointInj1Id,
            interval2Time,
            seedTimestampUtc,
            EnergyEvidenceStates.Simulated,
            "development-pilot",
            "pilot-interval-2",
            actorSubjectId,
            cancellationToken,
            intervalEnergyKwh: 15.25m,
            activePowerKw: 15.25m);

        await EnsureReadingAsync(
            dbContext,
            DevelopmentPilotIds.EnergyPilotReadingInj1Interval3Id,
            tenantId,
            facilityId,
            DevelopmentPilotIds.EnergyPilotPointInj1Id,
            interval3Time,
            seedTimestampUtc,
            EnergyEvidenceStates.Simulated,
            "development-pilot",
            "pilot-interval-3",
            actorSubjectId,
            cancellationToken,
            intervalEnergyKwh: 10.75m,
            activePowerKw: 10.75m,
            maximumDemandKw: 18.5m);

        var conflictStart = windowStart.AddHours(6);
        await EnsureReadingAsync(
            dbContext,
            DevelopmentPilotIds.EnergyPilotReadingConflict1Id,
            tenantId,
            facilityId,
            DevelopmentPilotIds.EnergyPilotPointInj3Id,
            conflictStart,
            seedTimestampUtc,
            EnergyEvidenceStates.Simulated,
            "development-pilot",
            "pilot-cumulative-before-reset",
            actorSubjectId,
            cancellationToken,
            cumulativeEnergyKwh: 1000m);

        await EnsureReadingAsync(
            dbContext,
            DevelopmentPilotIds.EnergyPilotReadingConflict2Id,
            tenantId,
            facilityId,
            DevelopmentPilotIds.EnergyPilotPointInj3Id,
            conflictStart.AddHours(2),
            seedTimestampUtc,
            EnergyEvidenceStates.Simulated,
            "development-pilot",
            "pilot-cumulative-after-reset",
            actorSubjectId,
            cancellationToken,
            cumulativeEnergyKwh: 50m);
    }

    private static async Task EnsureReadingAsync(
        CrevaFactOSDbContext dbContext,
        Guid id,
        Guid tenantId,
        Guid facilityId,
        Guid measurementPointId,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset receivedAtUtc,
        string evidenceState,
        string sourceType,
        string sourceReference,
        string actorSubjectId,
        CancellationToken cancellationToken,
        decimal? intervalEnergyKwh = null,
        decimal? cumulativeEnergyKwh = null,
        decimal? activePowerKw = null,
        decimal? maximumDemandKw = null)
    {
        var exists = await dbContext.EnergyReadings
            .IgnoreQueryFilters()
            .AnyAsync(r => r.Id == id, cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.EnergyReadings.Add(new EnergyReading
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            MeasurementPointId = measurementPointId,
            OccurredAtUtc = occurredAtUtc,
            ReceivedAtUtc = receivedAtUtc,
            CumulativeEnergyKwh = cumulativeEnergyKwh,
            IntervalEnergyKwh = intervalEnergyKwh,
            ActivePowerKw = activePowerKw,
            MaximumDemandKw = maximumDemandKw,
            SourceType = sourceType,
            SourceReference = sourceReference,
            FreshnessState = EnergyFreshnessStates.Fresh,
            EvidenceState = evidenceState,
            CreatedAtUtc = receivedAtUtc,
            CreatedBy = actorSubjectId,
        });
    }

    private static async Task EnsureConsumptionWindowsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var windowStart = new DateTimeOffset(seedTimestampUtc.UtcDateTime.Date.AddDays(-1).AddHours(6), TimeSpan.Zero);
        var windowEnd = windowStart.AddHours(8);
        var consumedKwh = EnergyCalculator.RoundEnergy(12.5m + 15.25m + 10.75m);
        var cost = EnergyCalculator.CalculateCost(consumedKwh, 18.5m, new EnergyTariffSnapshot
        {
            Id = DevelopmentPilotIds.EnergyPilotTariffId,
            CurrencyCode = "TRY",
            EnergyUnitPrice = 0.0001m,
            DemandUnitPrice = 0.01m,
        });

        await EnsureWindowAsync(
            dbContext,
            DevelopmentPilotIds.EnergyPilotWindowCalculatedId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.EnergyPilotPointInj1Id,
            windowStart,
            windowEnd,
            consumedKwh,
            EnergyCalculationMethods.IntervalSum,
            EnergyEvidenceStates.Simulated,
            3,
            cost.CostAmount,
            cost.CurrencyCode,
            DevelopmentPilotIds.EnergyPilotTariffId,
            null,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureAllocationsAsync(
            dbContext,
            tenantId,
            facilityId,
            DevelopmentPilotIds.EnergyPilotWindowCalculatedId,
            consumedKwh,
            seedTimestampUtc,
            cancellationToken);

        var insufficientStart = windowEnd.AddHours(1);
        var insufficientEnd = insufficientStart.AddHours(4);
        await EnsureWindowAsync(
            dbContext,
            DevelopmentPilotIds.EnergyPilotWindowInsufficientId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.EnergyPilotPointInj2Id,
            insufficientStart,
            insufficientEnd,
            0m,
            EnergyCalculationMethods.Unavailable,
            EnergyEvidenceStates.InsufficientData,
            0,
            null,
            null,
            null,
            "No readings available for the requested window.",
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        var conflictStart = windowStart.AddHours(6);
        var conflictEnd = conflictStart.AddHours(4);
        await EnsureWindowAsync(
            dbContext,
            DevelopmentPilotIds.EnergyPilotWindowConflictId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.EnergyPilotPointInj3Id,
            conflictStart,
            conflictEnd,
            0m,
            EnergyCalculationMethods.CumulativeDelta,
            EnergyEvidenceStates.SourceConflict,
            2,
            null,
            null,
            null,
            "Counter reset or rollover detected without safe evidence.",
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);
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
        string calculationMethod,
        string evidenceStatus,
        int readingCount,
        decimal? costAmount,
        string? costCurrencyCode,
        Guid? tariffSnapshotId,
        string? missingDataExplanation,
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

        decimal? averagePower = null;
        var durationHours = (decimal)(windowEndUtc - windowStartUtc).TotalHours;
        if (durationHours > 0 && consumedKwh > 0)
        {
            averagePower = EnergyCalculator.RoundEnergy(consumedKwh / durationHours);
        }

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
            MaximumDemandKw = evidenceStatus == EnergyEvidenceStates.Simulated ? 18.5m : null,
            CalculationMethod = calculationMethod,
            EvidenceStatus = evidenceStatus,
            ReadingCount = readingCount,
            FreshnessState = EnergyFreshnessStates.Fresh,
            SourceReferencesJson = "[\"development-pilot\"]",
            RuleSetVersion = EnergyConstants.RuleSetVersion,
            CostAmount = costAmount,
            CostCurrencyCode = costCurrencyCode,
            TariffSnapshotId = tariffSnapshotId,
            MissingDataExplanation = missingDataExplanation,
            CalculatedAtUtc = seedTimestampUtc,
            CreatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
        });
    }

    private static async Task EnsureAllocationsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        Guid windowId,
        decimal consumedKwh,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var machineExists = await dbContext.EnergyAllocations
            .IgnoreQueryFilters()
            .AnyAsync(a => a.Id == DevelopmentPilotIds.EnergyPilotAllocationMachineId, cancellationToken);

        if (!machineExists)
        {
            var allocated = EnergyCalculator.RoundEnergy(consumedKwh * 0.85m);
            dbContext.EnergyAllocations.Add(new EnergyAllocation
            {
                Id = DevelopmentPilotIds.EnergyPilotAllocationMachineId,
                TenantId = tenantId,
                FacilityId = facilityId,
                ConsumptionWindowId = windowId,
                TargetType = EnergyAllocationTargetTypes.Machine,
                TargetReference = DevelopmentPilotIds.EdgePilotPressMachineId.ToString(),
                AllocatedKwh = allocated,
                AllocationMethod = EnergyAllocationMethods.DirectAssignment,
                EvidenceSource = "development-pilot",
                ConfidenceStatus = EnergyEvidenceStates.Simulated,
                RuleSetVersion = EnergyConstants.RuleSetVersion,
                CalculatedAtUtc = seedTimestampUtc,
                CreatedAtUtc = seedTimestampUtc,
            });
        }

        var residualExists = await dbContext.EnergyAllocations
            .IgnoreQueryFilters()
            .AnyAsync(a => a.Id == DevelopmentPilotIds.EnergyPilotAllocationResidualId, cancellationToken);

        if (!residualExists)
        {
            var allocated = EnergyCalculator.RoundEnergy(consumedKwh * 0.85m);
            var residual = EnergyCalculator.RoundEnergy(consumedKwh - allocated);
            dbContext.EnergyAllocations.Add(new EnergyAllocation
            {
                Id = DevelopmentPilotIds.EnergyPilotAllocationResidualId,
                TenantId = tenantId,
                FacilityId = facilityId,
                ConsumptionWindowId = windowId,
                TargetType = EnergyAllocationTargetTypes.Unallocated,
                TargetReference = null,
                AllocatedKwh = residual,
                AllocationMethod = EnergyAllocationMethods.Residual,
                EvidenceSource = "development-pilot",
                ConfidenceStatus = EnergyEvidenceStates.Calculated,
                RuleSetVersion = EnergyConstants.RuleSetVersion,
                CalculatedAtUtc = seedTimestampUtc,
                CreatedAtUtc = seedTimestampUtc,
            });
        }
    }
}
