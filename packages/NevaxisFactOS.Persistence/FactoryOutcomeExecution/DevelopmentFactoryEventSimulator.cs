using System.Text.Json;
using NevaxisFactOS.Persistence.FactoryOrchestration;

namespace NevaxisFactOS.Persistence.FactoryOutcomeExecution;

public static class DevelopmentFactoryEventSimulator
{
    public static FactoryDomainEvent CreateInj004CycleDegradationEvent(Guid facilityId) =>
        new(
            "production.performance-degradation",
            "INJ-004 çevrim performansı bozuldu",
            "Referans 27.5 sn, gerçek 32.1 sn; 14 mikro duruş tespit edildi.",
            "critical",
            "development-simulator",
            facilityId,
            "machine-intelligence",
            $"inj004-cycle-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            FactoryOutcomeExecutionConstants.MachineCode,
            FactoryOutcomeExecutionConstants.OrderReference,
            null,
            null,
            BuildOrderRecoveryMetadata());

    public static FactoryDomainEvent CreateMaterialShortageEvent(Guid facilityId) =>
        new(
            "material.shortage",
            "ORD-8742 malzeme kapsaması daraldı",
            $"Malzeme kapsaması {FactoryOutcomeExecutionConstants.MaterialCoverageHours} saat.",
            "high",
            "development-simulator",
            facilityId,
            "material-coverage",
            $"ord8742-material-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            FactoryOutcomeExecutionConstants.MachineCode,
            FactoryOutcomeExecutionConstants.OrderReference,
            null,
            "PP-GRANULE",
            BuildOrderRecoveryMetadata());

    public static FactoryDomainEvent CreateMaintenanceReliabilityWarningEvent(Guid facilityId) =>
        new(
            "maintenance.reliability-warning",
            "INJ-004 güvenilirlik uyarısı",
            "Bakım güvenilirlik uyarısı üretim çıktısını etkiliyor.",
            "high",
            "development-simulator",
            facilityId,
            "maintenance-monitor",
            $"inj004-maint-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            FactoryOutcomeExecutionConstants.MachineCode,
            FactoryOutcomeExecutionConstants.OrderReference,
            null,
            null,
            BuildOrderRecoveryMetadata());

    private static string BuildOrderRecoveryMetadata() =>
        JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["scenarioKey"] = FactoryOutcomeExecutionConstants.ScenarioOrderRecovery2,
            ["orderReference"] = FactoryOutcomeExecutionConstants.OrderReference,
            ["requiredQuantity"] = FactoryOutcomeExecutionConstants.RequiredQuantity,
            ["projectedQuantity"] = FactoryOutcomeExecutionConstants.InitialProjectedQuantity,
            ["gapQuantity"] = FactoryOutcomeExecutionConstants.RequiredQuantity - FactoryOutcomeExecutionConstants.InitialProjectedQuantity,
            ["machineCode"] = FactoryOutcomeExecutionConstants.MachineCode,
            ["cycleReferenceSec"] = FactoryOutcomeExecutionConstants.CycleTimeReferenceSec,
            ["cycleActualSec"] = FactoryOutcomeExecutionConstants.CycleTimeActualSec,
            ["microStopCount"] = FactoryOutcomeExecutionConstants.MicroStopCount,
            ["materialCoverageHours"] = FactoryOutcomeExecutionConstants.MaterialCoverageHours,
            ["deliveryCommitmentUtc"] = DateTimeOffset.UtcNow.AddHours(18).ToString("O"),
            ["confidence"] = 0.9m,
        });
}
