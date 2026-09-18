using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NevaxisZentra.Persistence.GoldenDemoP1;

namespace NevaxisZentra.Persistence.Development;

internal static class DevelopmentPilotGoldenDemoP1Seed
{
    private static readonly Dictionary<string, decimal> ReferencePhases = new()
    {
        [GoldenDemoCyclePhaseCodes.Injection] = 2.5m,
        [GoldenDemoCyclePhaseCodes.Hold] = 3.0m,
        [GoldenDemoCyclePhaseCodes.Cooling] = 12.0m,
        [GoldenDemoCyclePhaseCodes.ScrewRecovery] = 4.5m,
        [GoldenDemoCyclePhaseCodes.MoldMovement] = 4.0m,
        [GoldenDemoCyclePhaseCodes.OtherProcess] = 2.0m,
    };

    private static readonly Dictionary<string, decimal> CurrentPhases = new()
    {
        [GoldenDemoCyclePhaseCodes.Injection] = 2.5m,
        [GoldenDemoCyclePhaseCodes.Hold] = 3.0m,
        [GoldenDemoCyclePhaseCodes.Cooling] = 13.5m,
        [GoldenDemoCyclePhaseCodes.ScrewRecovery] = 5.3m,
        [GoldenDemoCyclePhaseCodes.MoldMovement] = 4.7m,
        [GoldenDemoCyclePhaseCodes.OtherProcess] = 2.2m,
    };

    private static readonly Dictionary<string, decimal> ReferenceProcessParameters = new()
    {
        ["injection_time"] = 2.5m,
        ["holding_time"] = 3.0m,
        ["cooling_time"] = 12.0m,
        ["screw_recovery_time"] = 4.5m,
        ["mold_open_time"] = 2.0m,
        ["mold_close_time"] = 2.0m,
        ["holding_pressure"] = 82.0m,
        ["back_pressure"] = 11.5m,
        ["cushion"] = 4.0m,
        ["melt_temperature"] = 218.0m,
        ["mold_temperature"] = 44.0m,
    };

    private static readonly Dictionary<string, decimal> CurrentProcessParameters = new()
    {
        ["injection_time"] = 2.5m,
        ["holding_time"] = 3.0m,
        ["cooling_time"] = 13.5m,
        ["screw_recovery_time"] = 5.3m,
        ["mold_open_time"] = 2.5m,
        ["mold_close_time"] = 2.2m,
        ["holding_pressure"] = 85.0m,
        ["back_pressure"] = 12.0m,
        ["cushion"] = 4.2m,
        ["melt_temperature"] = 220.0m,
        ["mold_temperature"] = 45.0m,
    };

    internal static async Task EnsureGoldenDemoP1PilotDataAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (tenantId != DevelopmentPilotIds.TenantId || facilityId != DevelopmentPilotIds.FacilityId)
        {
            return;
        }

        var shiftStart = seedTimestampUtc.AddHours(-4);
        var shiftEnd = shiftStart.AddHours(8);
        var observationWindowStart = seedTimestampUtc.AddMinutes(-90);
        var observationWindowEnd = seedTimestampUtc;

        await EnsureProductionEventAsync(
            dbContext,
            tenantId,
            facilityId,
            shiftStart,
            shiftEnd,
            seedTimestampUtc,
            cancellationToken);
        await EnsureBaselineAsync(
            dbContext,
            tenantId,
            facilityId,
            seedTimestampUtc,
            cancellationToken);
        await EnsureCurrentObservationAsync(
            dbContext,
            tenantId,
            facilityId,
            observationWindowStart,
            observationWindowEnd,
            seedTimestampUtc,
            cancellationToken);
        await EnsureRecoveryPlanAsync(
            dbContext,
            tenantId,
            facilityId,
            seedTimestampUtc,
            cancellationToken);
    }

    private static async Task EnsureProductionEventAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset shiftStartUtc,
        DateTimeOffset shiftEndUtc,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.GoldenDemoProductionEvents
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == DevelopmentPilotGoldenDemoP1Ids.ProductionEventId, cancellationToken);
        if (existing is not null)
        {
            existing.Status = GoldenDemoP1EventStatuses.Active;
            existing.ShiftTargetGoodParts = 480;
            existing.ShiftStartUtc = shiftStartUtc;
            existing.ShiftEndUtc = shiftEndUtc;
            existing.UpdatedAtUtc = seedTimestampUtc;
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        dbContext.GoldenDemoProductionEvents.Add(new GoldenDemoProductionEvent
        {
            Id = DevelopmentPilotGoldenDemoP1Ids.ProductionEventId,
            TenantId = tenantId,
            FacilityId = facilityId,
            MachineAssetId = DevelopmentPilotIds.EdgePilotPressMachineId,
            MachineCode = "INJ-001",
            Title = "INJ-001 vardiya üretim hedefi risk altında",
            Status = GoldenDemoP1EventStatuses.Active,
            MoldReference = "MOLD-PP-240",
            ProductReference = "PP-KAPAK-240",
            ShiftTargetGoodParts = 480,
            ShiftStartUtc = shiftStartUtc,
            ShiftEndUtc = shiftEndUtc,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureBaselineAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.GoldenDemoCycleBaselines
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == DevelopmentPilotGoldenDemoP1Ids.CycleBaselineId, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        dbContext.GoldenDemoCycleBaselines.Add(new GoldenDemoCycleBaseline
        {
            Id = DevelopmentPilotGoldenDemoP1Ids.CycleBaselineId,
            TenantId = tenantId,
            FacilityId = facilityId,
            ProductionEventId = DevelopmentPilotGoldenDemoP1Ids.ProductionEventId,
            ReferenceSource = GoldenDemoReferenceSources.StableHistoricalWindow,
            ReferenceSourceLabel = "Aynı makine / aynı kalıp / stabil geçmiş üretim penceresi",
            TotalCycleSeconds = ReferencePhases.Values.Sum(),
            PhaseDurationsJson = JsonSerializer.Serialize(ReferencePhases),
            ProcessParametersJson = JsonSerializer.Serialize(ReferenceProcessParameters),
            ReferenceKwhPerGoodPart = 0.38m,
            ValidFromUtc = seedTimestampUtc.AddDays(-14),
            CreatedAtUtc = seedTimestampUtc,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureCurrentObservationAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.GoldenDemoCycleObservations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == DevelopmentPilotGoldenDemoP1Ids.CurrentObservationId, cancellationToken);
        if (existing is not null)
        {
            existing.TotalCycleSeconds = CurrentPhases.Values.Sum();
            existing.PhaseDurationsJson = JsonSerializer.Serialize(CurrentPhases);
            existing.ProcessParametersJson = JsonSerializer.Serialize(CurrentProcessParameters);
            existing.MicroStopCount = 8;
            existing.MicroStopDurationSeconds = 120m;
            existing.GoodParts = 168;
            existing.RejectParts = 8;
            existing.AvailabilityPercent = 88m;
            existing.KwhPerGoodPart = 0.42m;
            existing.TotalEnergyKwh = 73.92m;
            existing.MachineState = "running-degraded";
            existing.ObservedAtUtc = seedTimestampUtc;
            existing.WindowStartUtc = windowStartUtc;
            existing.WindowEndUtc = windowEndUtc;
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        dbContext.GoldenDemoCycleObservations.Add(new GoldenDemoCycleObservation
        {
            Id = DevelopmentPilotGoldenDemoP1Ids.CurrentObservationId,
            TenantId = tenantId,
            FacilityId = facilityId,
            ProductionEventId = DevelopmentPilotGoldenDemoP1Ids.ProductionEventId,
            ObservationKind = "current",
            TotalCycleSeconds = CurrentPhases.Values.Sum(),
            PhaseDurationsJson = JsonSerializer.Serialize(CurrentPhases),
            ProcessParametersJson = JsonSerializer.Serialize(CurrentProcessParameters),
            MicroStopCount = 8,
            MicroStopDurationSeconds = 120m,
            GoodParts = 168,
            RejectParts = 8,
            AvailabilityPercent = 88m,
            KwhPerGoodPart = 0.42m,
            TotalEnergyKwh = 73.92m,
            MachineState = "running-degraded",
            ObservedAtUtc = seedTimestampUtc,
            WindowStartUtc = windowStartUtc,
            WindowEndUtc = windowEndUtc,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureRecoveryPlanAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.GoldenDemoRecoveryPlans
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == DevelopmentPilotGoldenDemoP1Ids.RecoveryPlanId, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        dbContext.GoldenDemoRecoveryPlans.Add(new GoldenDemoRecoveryPlan
        {
            Id = DevelopmentPilotGoldenDemoP1Ids.RecoveryPlanId,
            TenantId = tenantId,
            FacilityId = facilityId,
            ProductionEventId = DevelopmentPilotGoldenDemoP1Ids.ProductionEventId,
            Status = GoldenDemoPlanActionStatuses.Planned,
            RecommendationSummary =
                "Mevcut kayıplar, makinenin doğrulanmış stabil çalışma bandına dönmesi için teknik müdahale fırsatı gösteriyor.",
            RecommendedScenarioCode = GoldenDemoWhatIfScenarioCodes.ControlledInterventionNow,
            RequiresApproval = true,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
        });

        dbContext.GoldenDemoRecoveryPlanActions.AddRange(
            new GoldenDemoRecoveryPlanAction
            {
                Id = DevelopmentPilotGoldenDemoP1Ids.CoolingInspectionActionId,
                TenantId = tenantId,
                FacilityId = facilityId,
                RecoveryPlanId = DevelopmentPilotGoldenDemoP1Ids.RecoveryPlanId,
                SortOrder = 1,
                ActionCode = "cooling-circuit-inspection",
                Title = "Soğutma devresi ve mold sıcaklık stabilitesi kontrolü",
                ResponsibleRole = "process-technician",
                Priority = "high",
                Status = GoldenDemoPlanActionStatuses.Planned,
                EvidenceRequired = "Soğutma devresi basıncı ve mold sıcaklık trend kaydı",
                EstimatedDowntimeMinutes = 10,
                RequiresApproval = false,
                ValidationCriteria = "Soğutma faz süresi referans bandına yaklaşmalı",
            },
            new GoldenDemoRecoveryPlanAction
            {
                Id = DevelopmentPilotGoldenDemoP1Ids.MoldMovementActionId,
                TenantId = tenantId,
                FacilityId = facilityId,
                RecoveryPlanId = DevelopmentPilotGoldenDemoP1Ids.RecoveryPlanId,
                SortOrder = 2,
                ActionCode = "mold-movement-inspection",
                Title = "Kalıp aç/kapa mekanizması ve yağlama kontrolü",
                ResponsibleRole = "maintenance-technician",
                Priority = "high",
                Status = GoldenDemoPlanActionStatuses.Planned,
                EvidenceRequired = "Kalıp hareket süresi ölçüm kaydı",
                EstimatedDowntimeMinutes = 8,
                RequiresApproval = false,
                ValidationCriteria = "Mold movement faz kaybı azalmalı",
            },
            new GoldenDemoRecoveryPlanAction
            {
                Id = DevelopmentPilotGoldenDemoP1Ids.ScrewRecoveryActionId,
                TenantId = tenantId,
                FacilityId = facilityId,
                RecoveryPlanId = DevelopmentPilotGoldenDemoP1Ids.RecoveryPlanId,
                SortOrder = 3,
                ActionCode = "screw-recovery-review",
                Title = "Vida geri çekme profili ve basınç sensör doğrulaması",
                ResponsibleRole = "process-technician",
                Priority = "medium",
                Status = GoldenDemoPlanActionStatuses.Planned,
                EvidenceRequired = "Vida geri çekme trend grafiği",
                EstimatedDowntimeMinutes = 7,
                RequiresApproval = false,
                ValidationCriteria = "Screw recovery faz stabilitesi iyileşmeli",
            },
            new GoldenDemoRecoveryPlanAction
            {
                Id = DevelopmentPilotGoldenDemoP1Ids.ValidationRunActionId,
                TenantId = tenantId,
                FacilityId = facilityId,
                RecoveryPlanId = DevelopmentPilotGoldenDemoP1Ids.RecoveryPlanId,
                SortOrder = 4,
                ActionCode = "controlled-validation-run",
                Title = "Kontrollü doğrulama koşusu ve ilk 20 cycle izleme",
                ResponsibleRole = "production-supervisor",
                Priority = "high",
                Status = GoldenDemoPlanActionStatuses.Planned,
                EvidenceRequired = "20 cycle cycle-time ve kalite kaydı",
                EstimatedDowntimeMinutes = 0,
                RequiresApproval = true,
                ValidationCriteria = "Cycle time, throughput ve reject rate stabil kalmalı",
            });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
