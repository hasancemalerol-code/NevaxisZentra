namespace NevaxisFactOS.Persistence.ProductionManager;

public static class CapacityEngine
{
    public static object Evaluate(int activeCaseCount, ShiftOperations.OperationalShiftInstance? shift)
    {
        var plannedCapacity = 100m;
        var consumed = Math.Min(95m, activeCaseCount * 8m);
        var remaining = Math.Max(0, plannedCapacity - consumed);
        var utilization = plannedCapacity == 0 ? 0 : consumed / plannedCapacity * 100m;

        return new
        {
            plannedCapacityPercent = plannedCapacity,
            remainingCapacityPercent = remaining,
            utilizationPercent = utilization,
            shiftRemainingHours = shift is null ? 0 : Math.Max(0, (shift.PlannedEndUtc - DateTimeOffset.UtcNow).TotalHours),
            bottleneck = utilization >= 85 ? "capacity" : "none",
            classification = ProductionManagerDataClassifications.Observed,
        };
    }
}

public static class BottleneckDetector
{
    public static IReadOnlyList<BottleneckSignal> Detect(FactoryStateSnapshot state)
    {
        var signals = new List<BottleneckSignal>();

        if (state.MaintenanceStateJson.Contains("\"openMaintenanceCases\":", StringComparison.Ordinal) &&
            state.MaintenanceStateJson != """{"openMaintenanceCases":0,"classification":"observed"}""")
        {
            signals.Add(new BottleneckSignal("maintenance", "Bakım kaynaklı kapasite darboğazı", "high", ["PO-1842"]));
        }

        if (state.MaterialStateJson.Contains("\"shortageSignals\":", StringComparison.Ordinal) &&
            !state.MaterialStateJson.Contains("\"shortageSignals\":0"))
        {
            signals.Add(new BottleneckSignal("material", "Malzeme kısıtı üretimi sınırlıyor", "high", ["PO-1842"]));
        }

        if (state.CapacityStateJson.Contains("\"bottleneck\":\"capacity\"", StringComparison.Ordinal))
        {
            signals.Add(new BottleneckSignal("capacity", "Vardiya kapasitesi yüksek kullanımda", "medium", []));
        }

        return signals;
    }
}
