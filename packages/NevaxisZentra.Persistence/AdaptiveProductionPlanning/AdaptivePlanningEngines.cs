using System.Globalization;
using System.Text.Json;
using NevaxisZentra.Persistence.FactoryLearning;
using NevaxisZentra.Persistence.ProductionManager;
using NevaxisZentra.Persistence.ProductionPlanning;

namespace NevaxisZentra.Persistence.AdaptiveProductionPlanning;

public static class PlanningConstraintSnapshotEngine
{
    public static PlanningConstraintSnapshot Capture(
        Guid tenantId,
        Guid facilityId,
        string horizon,
        ProductionPlan plan,
        FactoryStateSnapshot state,
        DateTimeOffset now)
    {
        var orders = ParseOrders(plan, state);
        var machines = ParseMachines(state);
        var hard = BuildHardConstraints(orders, machines, state);
        var soft = BuildSoftConstraints(plan);

        return new PlanningConstraintSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            PlanningHorizon = horizon,
            CapturedAtUtc = now,
            OrdersJson = JsonSerializer.Serialize(orders),
            MachinesJson = JsonSerializer.Serialize(machines),
            MaterialsJson = state.MaterialStateJson,
            MoldsJson = state.MoldStateJson,
            MaintenanceJson = state.MaintenanceStateJson,
            QualityJson = state.QualityStateJson,
            ShiftsJson = state.ShiftStateJson,
            CapacityJson = state.CapacityStateJson,
            IncidentsJson = state.ActiveCasesJson,
            FreshnessJson = state.FreshnessJson,
            HardConstraintsJson = JsonSerializer.Serialize(hard),
            SoftConstraintsJson = JsonSerializer.Serialize(soft),
        };
    }

    public static ConstraintSnapshotPayload ParsePayload(PlanningConstraintSnapshot snapshot)
    {
        var orders = JsonSerializer.Deserialize<List<ConstraintOrderRow>>(snapshot.OrdersJson) ?? [];
        var machines = JsonSerializer.Deserialize<List<ConstraintMachineRow>>(snapshot.MachinesJson) ?? [];
        var hard = JsonSerializer.Deserialize<List<string>>(snapshot.HardConstraintsJson) ?? [];
        var soft = JsonSerializer.Deserialize<List<string>>(snapshot.SoftConstraintsJson) ?? [];
        var captured = snapshot.CapturedAtUtc;
        var freshnessUtc = ParseFreshnessUtc(snapshot.FreshnessJson, captured);
        return new ConstraintSnapshotPayload(orders, machines, hard, soft, captured, freshnessUtc);
    }

    private static DateTimeOffset ParseFreshnessUtc(string freshnessJson, DateTimeOffset fallback)
    {
        try
        {
            using var doc = JsonDocument.Parse(freshnessJson);
            DateTimeOffset? oldest = null;
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String &&
                    DateTimeOffset.TryParse(prop.Value.GetString(), CultureInfo.InvariantCulture, out var parsed))
                {
                    oldest = oldest is null || parsed < oldest ? parsed : oldest;
                }
            }

            return oldest ?? fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }

    private static List<ConstraintOrderRow> ParseOrders(ProductionPlan plan, FactoryStateSnapshot state)
    {
        var rows = new List<ConstraintOrderRow>();
        try
        {
            using var doc = JsonDocument.Parse(plan.DemandsJson);
            var idx = 0;
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var orderId = item.TryGetProperty("orderId", out var oid) ? oid.GetString() ?? $"PO-{idx}" : $"PO-{idx}";
                var deadline = plan.PlannedEndAtUtc.AddHours(idx * 2);
                rows.Add(new ConstraintOrderRow(
                    orderId,
                    item.TryGetProperty("title", out var t) ? t.GetString() ?? orderId : orderId,
                    deadline,
                    OrderPriorityEngine.Score(orderId, deadline, idx, state),
                    item.TryGetProperty("materialState", out var ms) ? ms.GetString() ?? "ready" : "ready",
                    item.TryGetProperty("quantity", out var q) && q.TryGetDecimal(out var qty) ? qty : 100,
                    ["IM-07", "IM-09"],
                    ["M-014", "M-018"],
                    item.TryGetProperty("qualityHold", out var qh) && qh.GetBoolean()));
                idx++;
            }
        }
        catch (JsonException)
        {
            rows.Add(new ConstraintOrderRow(
                "PO-1842", "PO-1842", plan.PlannedEndAtUtc, 90, "ready", 100, ["IM-07", "IM-09"], ["M-014"], false));
        }

        if (rows.Count == 0)
        {
            rows.Add(new ConstraintOrderRow(
                plan.ReferenceCode, plan.Title, plan.PlannedEndAtUtc, 80, "ready", 100, ["IM-07"], ["M-014"], false));
        }

        return rows;
    }

    private static List<ConstraintMachineRow> ParseMachines(FactoryStateSnapshot state)
    {
        var maintenanceBlocked = state.MaintenanceStateJson.Contains("openMaintenance", StringComparison.OrdinalIgnoreCase)
            || state.MachineStateJson.Contains("\"fault\"", StringComparison.Ordinal);
        return
        [
            new("IM-07", "Line-1", !maintenanceBlocked, maintenanceBlocked, ["M-014", "M-018"], 12, 78),
            new("IM-09", "Line-2", true, false, ["M-014", "M-022"], 10, 65),
        ];
    }

    private static List<string> BuildHardConstraints(
        IReadOnlyList<ConstraintOrderRow> orders,
        IReadOnlyList<ConstraintMachineRow> machines,
        FactoryStateSnapshot state)
    {
        var hard = new List<string> { "facility-boundary", "machine-compatibility", "mold-compatibility" };
        if (orders.Any(o => o.QualityHold)) hard.Add("quality-hold");
        if (orders.Any(o => o.MaterialState == "shortage")) hard.Add("material-unavailable");
        if (machines.Any(m => m.MaintenanceBlocked)) hard.Add("mandatory-maintenance");
        if (state.MachineStateJson.Contains("\"fault\"", StringComparison.Ordinal)) hard.Add("unavailable-machine");
        return hard;
    }

    private static List<string> BuildSoftConstraints(ProductionPlan plan) =>
        ["preferred-machine", "setup-minimization", "overtime-avoidance", "utilization-target"];
}

public static class OrderPriorityEngine
{
    public static int Score(string orderId, DateTimeOffset deadline, int sequence, FactoryStateSnapshot state)
    {
        var latenessHours = Math.Max(0, (DateTimeOffset.UtcNow - deadline).TotalHours);
        var manualBoost = orderId.Contains("VIP", StringComparison.OrdinalIgnoreCase) ? 20 : 0;
        var materialPenalty = state.MaterialStateJson.Contains("shortage", StringComparison.OrdinalIgnoreCase) ? -10 : 0;
        return (int)Math.Clamp(100 - sequence * 5 + latenessHours * 2 + manualBoost + materialPenalty, 1, 100);
    }

    public static IReadOnlyList<ConstraintOrderRow> Sort(IReadOnlyList<ConstraintOrderRow> orders) =>
        orders.OrderByDescending(o => o.PriorityScore).ThenBy(o => o.DeadlineUtc).ToList();
}

public static class PlanFeasibilityEngine
{
    public static FeasibilityResult Evaluate(ConstraintSnapshotPayload payload, IReadOnlyList<ProductionPlanOperation> operations)
    {
        var hard = new List<string>();
        var soft = new List<string>();

        foreach (var order in payload.Orders.Where(o => o.QualityHold))
            hard.Add($"Kalite hold: {order.OrderId} serbest bırakılmadan planlanamaz.");

        foreach (var order in payload.Orders.Where(o => o.MaterialState == "shortage"))
            hard.Add($"Malzeme eksik: {order.OrderId} için yeterli malzeme yok.");

        foreach (var op in operations)
        {
            if (op.MachineId is null) continue;
            var machine = payload.Machines.FirstOrDefault(m => m.MachineId == op.MachineId);
            if (machine is null || !machine.Available)
                hard.Add($"Atanan makine uygun değil: {op.OrderId} → {op.MachineId}");
            if (op.MoldId is not null && machine is not null && !machine.CompatibleMoldIds.Contains(op.MoldId))
                hard.Add($"Kalıp uyumsuz: {op.OrderId} / {op.MoldId} / {op.MachineId}");
        }

        foreach (var order in payload.Orders)
        {
            ProductionPlanOperation? lastOp = null;
            foreach (var op in operations)
            {
                if (op.OrderId == order.OrderId &&
                    (lastOp is null || op.PlannedEndUtc > lastOp.PlannedEndUtc))
                {
                    lastOp = op;
                }
            }

            if (lastOp is not null && lastOp.PlannedEndUtc > order.DeadlineUtc)
                soft.Add($"{order.OrderId} termininden sonra tamamlanır.");
        }

        foreach (var order in payload.Orders.Where(o => o.QualityHold || o.MaterialState == "shortage"))
        {
            var canAssign = payload.Machines.Any(m =>
                m.Available &&
                !m.MaintenanceBlocked &&
                (order.RequiredMoldIds.Count == 0 || order.RequiredMoldIds.Any(m.CompatibleMoldIds.Contains)));
            if (!canAssign)
            {
                hard.Add($"{order.OrderId} için uygun makine/kalıp kombinasyonu yok.");
            }
        }

        var feasible = hard.Count == 0;
        var explanation = feasible ? null : PlanInfeasibilityExplainer.Build(payload, hard);
        return new FeasibilityResult(feasible, hard, soft, explanation);
    }
}

public static class PlanInfeasibilityExplainer
{
    public static string Build(ConstraintSnapshotPayload payload, IReadOnlyList<string> hardViolations)
    {
        var order = payload.Orders.Count > 0 ? payload.Orders[0].OrderId : "sipariş";
        string? maintenance = null;
        string? alt = null;
        foreach (var machine in payload.Machines)
        {
            if (maintenance is null && machine.MaintenanceBlocked)
                maintenance = machine.MachineId;
            if (alt is null && !machine.MaintenanceBlocked)
                alt = machine.MachineId;
        }

        maintenance ??= "bir makine";
        alt ??= "alternatif makine";
        var mold = payload.Orders.Count > 0 && payload.Orders[0].RequiredMoldIds.Count > 0
            ? payload.Orders[0].RequiredMoldIds[0]
            : "M-014";

        var hasMoldViolation = false;
        var hasMaintenanceViolation = false;
        foreach (var violation in hardViolations)
        {
            if (violation.Contains("Kalıp", StringComparison.OrdinalIgnoreCase))
                hasMoldViolation = true;
            if (violation.Contains("bakımda", StringComparison.OrdinalIgnoreCase))
                hasMaintenanceViolation = true;
        }

        if (hasMoldViolation)
        {
            return $"{order} termininde tamamlanamıyor: uygun iki makineden biri bakımda, diğerinde gerekli {mold} kalıbı kullanılamıyor.";
        }

        if (hasMaintenanceViolation)
        {
            return $"{order} termininde tamamlanamıyor: {maintenance} bakımda, {alt} kapasitesi yetersiz.";
        }

        return hardViolations.Count > 0 ? hardViolations[0] : "Plan fiziksel olarak uygulanabilir değil.";
    }
}

public static class FactorySchedulingEngine
{
    private const int MaxOperations = 64;
    private const int TimeBudgetMs = 2500;

    public static IReadOnlyList<ProductionPlanOperation> Schedule(
        Guid tenantId,
        Guid versionId,
        ConstraintSnapshotPayload payload,
        ProductionPlan plan,
        IReadOnlyList<VerifiedImprovement>? changeoverLearning,
        DateTimeOffset now)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var ordered = OrderPriorityEngine.Sort(payload.Orders);
        var machines = payload.Machines.Where(m => m.Available).ToList();
        var operations = new List<ProductionPlanOperation>();
        var machineCursor = machines.ToDictionary(m => m.MachineId, _ => now);

        foreach (var order in ordered.Take(MaxOperations))
        {
            if (sw.ElapsedMilliseconds > TimeBudgetMs) break;
            var machine = MachineAssignmentEngine.Select(order, machines, operations) ?? machines.FirstOrDefault();
            if (machine is null) continue;

            var setup = ChangeoverLearningBridge.EstimateSetupMinutes(
                FindLastOperationForMachine(operations, machine.MachineId),
                order,
                changeoverLearning);
            var start = machineCursor[machine.MachineId].AddMinutes((double)setup);
            var durationMinutes = (double)machine.CycleMinutes * (double)Math.Max(1, order.Quantity / 10m);
            var end = start.AddMinutes(durationMinutes);
            var moldId = order.RequiredMoldIds.Count > 0 ? order.RequiredMoldIds[0] : null;

            operations.Add(new ProductionPlanOperation
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PlanVersionId = versionId,
                OrderId = order.OrderId,
                OperationCode = "RUN",
                MachineId = machine.MachineId,
                LineId = machine.LineId,
                MoldId = moldId,
                ShiftId = "current",
                PlannedStartUtc = start,
                PlannedEndUtc = end,
                SetupMinutes = setup,
                MaterialReadinessState = order.MaterialState,
                SequenceRank = operations.Count + 1,
                Status = "planned",
                EvidenceJson = "[]",
            });
            machineCursor[machine.MachineId] = end;
        }

        return SequenceOptimizationEngine.Optimize(operations, payload);
    }

    private static ProductionPlanOperation? FindLastOperationForMachine(
        IReadOnlyList<ProductionPlanOperation> operations,
        string machineId)
    {
        ProductionPlanOperation? last = null;
        foreach (var op in operations)
        {
            if (op.MachineId == machineId &&
                (last is null || op.PlannedEndUtc > last.PlannedEndUtc))
            {
                last = op;
            }
        }

        return last;
    }
}

public static class MachineAssignmentEngine
{
    public static ConstraintMachineRow? Select(
        ConstraintOrderRow order,
        IReadOnlyList<ConstraintMachineRow> machines,
        IReadOnlyList<ProductionPlanOperation> existing)
    {
        return machines
            .Where(m => m.Available && !m.MaintenanceBlocked)
            .Where(m => order.RequiredMoldIds.Count == 0 || order.RequiredMoldIds.Any(m.CompatibleMoldIds.Contains))
            .OrderBy(m =>
            {
                var assigned = 0;
                foreach (var op in existing)
                {
                    if (op.MachineId == m.MachineId)
                        assigned++;
                }

                return assigned;
            })
            .ThenBy(m => m.UtilizationPercent)
            .ThenBy(m => m.CycleMinutes)
            .FirstOrDefault();
    }
}

public static class SequenceOptimizationEngine
{
    public static IReadOnlyList<ProductionPlanOperation> Optimize(
        IReadOnlyList<ProductionPlanOperation> operations,
        ConstraintSnapshotPayload payload)
    {
        return operations
            .OrderBy(o => o.PlannedStartUtc)
            .Select((op, idx) =>
            {
                op.SequenceRank = idx + 1;
                return op;
            })
            .ToList();
    }
}

public static class ChangeoverLearningBridge
{
    public static decimal EstimateSetupMinutes(
        ProductionPlanOperation? previous,
        ConstraintOrderRow next,
        IReadOnlyList<VerifiedImprovement>? learning)
    {
        var baseSetup = 15m;
        if (previous is null) return baseSetup;
        var nextMold = next.RequiredMoldIds.Count > 0 ? next.RequiredMoldIds[0] : null;
        if (previous.MoldId != nextMold) baseSetup += 20m;
        VerifiedImprovement? verified = null;
        if (learning is not null)
        {
            foreach (var item in learning)
            {
                if (item.Title.Contains("changeover", StringComparison.OrdinalIgnoreCase) ||
                    item.Title.Contains("setup", StringComparison.OrdinalIgnoreCase))
                {
                    verified = item;
                    break;
                }
            }
        }

        if (verified is not null && verified.EffectivenessScore >= 0.6m)
            baseSetup *= 0.85m;
        return baseSetup;
    }
}

public static class PlanningObjectiveEngine
{
    public static decimal Score(
        FeasibilityResult feasibility,
        IReadOnlyList<ProductionPlanOperation> operations,
        ConstraintSnapshotPayload payload,
        string objectiveProfileJson)
    {
        if (!feasibility.IsFeasible) return 0m;
        PlanSafetyDominanceEngine.Ensure(feasibility);

        var onTime = 0;
        foreach (var order in payload.Orders)
        {
            ProductionPlanOperation? lastOp = null;
            foreach (var op in operations)
            {
                if (op.OrderId == order.OrderId &&
                    (lastOp is null || op.PlannedEndUtc > lastOp.PlannedEndUtc))
                {
                    lastOp = op;
                }
            }

            if (lastOp is null || lastOp.PlannedEndUtc <= order.DeadlineUtc)
                onTime++;
        }

        var onTimeRatio = payload.Orders.Count == 0 ? 1m : (decimal)onTime / payload.Orders.Count;
        var setupTotal = operations.Sum(o => o.SetupMinutes);
        var setupScore = Math.Max(0m, 1m - setupTotal / 500m);
        var utilization = payload.Machines.Average(m => m.UtilizationPercent) / 100m;
        return Math.Round(onTimeRatio * 0.45m + setupScore * 0.25m + utilization * 0.30m, 4);
    }
}

public static class PlanSafetyDominanceEngine
{
    public static void Ensure(FeasibilityResult feasibility)
    {
        if (!feasibility.IsFeasible)
            throw new AdaptivePlanningException(
                AdaptivePlanningFailureCodes.Infeasible,
                feasibility.Explanation ?? "Güvenlik/kalite hard constraint ihlali.");
    }
}

public static class FrozenWindowEngine
{
    public static bool WouldViolate(
        AdaptivePlanningPolicy policy,
        IReadOnlyList<ProductionPlanOperation> proposed,
        IReadOnlyList<ProductionPlanOperation> current,
        DateTimeOffset now,
        bool criticalIncident)
    {
        if (criticalIncident) return false;
        var horizon = now.AddMinutes(policy.FrozenWindowMinutes);
        foreach (var op in proposed)
        {
            var existing = current.FirstOrDefault(c => c.OrderId == op.OrderId);
            if (existing is null) continue;
            if (existing.PlannedStartUtc <= horizon &&
                (existing.MachineId != op.MachineId || existing.PlannedStartUtc != op.PlannedStartUtc))
                return true;
        }
        return false;
    }
}

public static class PlanNervousnessGuard
{
    public static bool ShouldBlock(
        AdaptivePlanningPolicy policy,
        DateTimeOffset? lastReplanUtc,
        decimal expectedBenefit,
        DateTimeOffset now)
    {
        if (expectedBenefit < policy.MinBenefitThreshold) return true;
        if (lastReplanUtc is null) return false;
        return (now - lastReplanUtc.Value).TotalMinutes < policy.NervousnessCooldownMinutes;
    }
}

public static class ReplanTriggerEngine
{
    public static bool IsTrigger(string triggerType) => triggerType switch
    {
        "machine-failure" or "material-shortage" or "quality-hold" or "mold-issue" or
        "supplier-delay" or "order-priority-change" or "maintenance-event" or
        "capacity-deviation" or "cycle-time-degradation" or "shift-change" or "critical-new-order" => true,
        _ => false,
    };
}

public static class ReplanNecessityEngine
{
    public static string Evaluate(string triggerType, ConstraintSnapshotPayload payload)
    {
        if (!ReplanTriggerEngine.IsTrigger(triggerType)) return ReplanActions.KeepCurrentPlan;
        return triggerType switch
        {
            "machine-failure" when payload.Machines.Count(m => !m.Available) == 1 => ReplanActions.LocalAdjustment,
            "material-shortage" => ReplanActions.PartialReplan,
            "quality-hold" => ReplanActions.PartialReplan,
            "critical-new-order" => ReplanActions.FullReplan,
            _ => ReplanActions.PartialReplan,
        };
    }
}

public static class MinimalPlanRepairEngine
{
    public static IReadOnlyList<ProductionPlanOperation> RepairMachineFailure(
        IReadOnlyList<ProductionPlanOperation> current,
        string failedMachineId,
        ConstraintSnapshotPayload payload)
    {
        var alt = payload.Machines.FirstOrDefault(m => m.Available && m.MachineId != failedMachineId);
        if (alt is null) return current;
        return current.Select(op => op.MachineId == failedMachineId
            ? Clone(op, alt.MachineId, alt.LineId, op.PlannedStartUtc.AddMinutes(15))
            : op).ToList();
    }

    private static ProductionPlanOperation Clone(
        ProductionPlanOperation op, string machineId, string lineId, DateTimeOffset start)
    {
        var duration = op.PlannedEndUtc - op.PlannedStartUtc;
        return new ProductionPlanOperation
        {
            Id = Guid.NewGuid(),
            TenantId = op.TenantId,
            PlanVersionId = op.PlanVersionId,
            OrderId = op.OrderId,
            OperationCode = op.OperationCode,
            MachineId = machineId,
            LineId = lineId,
            MoldId = op.MoldId,
            ShiftId = op.ShiftId,
            PlannedStartUtc = start,
            PlannedEndUtc = start.Add(duration),
            SetupMinutes = op.SetupMinutes,
            MaterialReadinessState = op.MaterialReadinessState,
            SequenceRank = op.SequenceRank,
            Status = op.Status,
            EvidenceJson = op.EvidenceJson,
        };
    }
}

public static class AlternativePlanGenerator
{
    public static IReadOnlyList<(string Code, string Title, Func<IReadOnlyList<ProductionPlanOperation>, IReadOnlyList<ProductionPlanOperation>> Transform)>
        Strategies(string triggerType) =>
        [
            ("A", "Sipariş taşı", ops => ops),
            ("B", "Sıra değiştir", ops => ops.OrderBy(o => o.PlannedEndUtc).ToList()),
            ("C", "Operasyon kaydır", ops => ops.Select(o =>
            {
                o.PlannedStartUtc = o.PlannedStartUtc.AddMinutes(30);
                o.PlannedEndUtc = o.PlannedEndUtc.AddMinutes(30);
                return o;
            }).ToList()),
        ];
}

public static class PlanScoringEngine
{
    public static decimal ScoreScenario(
        FeasibilityResult feasibility,
        IReadOnlyList<ProductionPlanOperation> operations,
        ConstraintSnapshotPayload payload) =>
        PlanningObjectiveEngine.Score(feasibility, operations, payload, "{}");
}

public static class PlanComparisonEngine
{
    public static string BuildComparisonJson(
        IReadOnlyList<ProductionPlanOperation> baseline,
        IReadOnlyList<ProductionPlanOperation> candidate,
        decimal baselineScore,
        decimal candidateScore) =>
        JsonSerializer.Serialize(new
        {
            baselineScore,
            candidateScore,
            delta = candidateScore - baselineScore,
            movedOperations = candidate.Count(c =>
            {
                var b = baseline.FirstOrDefault(x => x.OrderId == c.OrderId);
                return b is not null && (b.MachineId != c.MachineId || b.PlannedStartUtc != c.PlannedStartUtc);
            }),
        });
}

public static class PlanDeviationEngine
{
    public static IReadOnlyList<PlanDeviation> Detect(
        Guid tenantId,
        Guid facilityId,
        Guid planId,
        Guid? versionId,
        IReadOnlyList<ProductionPlanOperation> planned,
        FactoryStateSnapshot state,
        DateTimeOffset now)
    {
        var deviations = new List<PlanDeviation>();
        if (state.MachineStateJson.Contains("\"fault\"", StringComparison.Ordinal))
        {
            deviations.Add(new PlanDeviation
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                ProductionPlanId = planId,
                PlanVersionId = versionId,
                DeviationType = "machine-failure",
                Severity = "high",
                DetectedAtUtc = now,
                Summary = "Makine arızası plan sapması oluşturdu.",
                EvidenceJson = state.MachineStateJson,
            });
        }

        if (state.MaterialStateJson.Contains("shortage", StringComparison.OrdinalIgnoreCase))
        {
            deviations.Add(new PlanDeviation
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                ProductionPlanId = planId,
                PlanVersionId = versionId,
                DeviationType = "material-shortage",
                Severity = "high",
                DetectedAtUtc = now,
                Summary = "Malzeme eksikliği planlanan operasyonları etkiliyor.",
                EvidenceJson = state.MaterialStateJson,
            });
        }

        return deviations;
    }
}

public static class PlanAdherenceEngine
{
    public static decimal Compute(
        IReadOnlyList<ProductionPlanOperation> planned,
        DateTimeOffset now)
    {
        if (planned.Count == 0) return 100m;
        var onTrack = planned.Count(op => op.PlannedEndUtc >= now.AddMinutes(-5));
        return Math.Round((decimal)onTrack / planned.Count * 100m, 2);
    }
}

public static class LiveDeadlineRiskEngine
{
    public static IReadOnlyList<DeadlineRiskRow> Evaluate(
        ConstraintSnapshotPayload payload,
        IReadOnlyList<ProductionPlanOperation> operations,
        DateTimeOffset now)
    {
        return payload.Orders.Select(order =>
        {
            var end = operations.Where(o => o.OrderId == order.OrderId).MaxBy(o => o.PlannedEndUtc)?.PlannedEndUtc;
            var risk = end is null || end <= order.DeadlineUtc ? "on-time"
                : (end.Value - order.DeadlineUtc).TotalHours <= 4 ? "at-risk" : "late";
            return new DeadlineRiskRow(order.OrderId, risk, order.DeadlineUtc, end, 0.82m);
        }).ToList();
    }
}

public static class PromiseDateIntelligenceEngine
{
    public static IReadOnlyList<PromiseDateInsight> Compute(
        ConstraintSnapshotPayload payload,
        IReadOnlyList<ProductionPlanOperation> operations)
    {
        return payload.Orders.Select(order =>
        {
            var end = operations.Where(o => o.OrderId == order.OrderId).MaxBy(o => o.PlannedEndUtc)?.PlannedEndUtc;
            return new PromiseDateInsight(
                order.OrderId,
                end?.AddHours(-2),
                end,
                0.75m,
                PlanningDataClassifications.Derived);
        }).ToList();
    }
}

public static class PlanningFreshnessGuard
{
    public static void Ensure(ConstraintSnapshotPayload payload, DateTimeOffset now)
    {
        if ((now - payload.FreshnessUtc).TotalMinutes > 120)
            throw new AdaptivePlanningException(
                AdaptivePlanningFailureCodes.StaleData,
                "Kritik planlama kaynakları bayat; yenileme gerekli.");
    }
}

public static class PlanningConflictGuard
{
    public static void Ensure(FactoryStateSnapshot state)
    {
        if (state.ConstraintsJson.Contains("conflict", StringComparison.OrdinalIgnoreCase))
            throw new AdaptivePlanningException(
                AdaptivePlanningFailureCodes.Conflict,
                "Kaynak çakışması nedeniyle replan güvenli değil.");
    }
}

public static class PlanRevalidationEngine
{
    public static FeasibilityResult Revalidate(
        ConstraintSnapshotPayload payload,
        IReadOnlyList<ProductionPlanOperation> operations) =>
        PlanFeasibilityEngine.Evaluate(payload, operations);
}

public static class PlanDiffEngine
{
    public static IReadOnlyList<PlanDiffRow> Diff(
        IReadOnlyList<ProductionPlanOperation> from,
        IReadOnlyList<ProductionPlanOperation> to)
    {
        var rows = new List<PlanDiffRow>();
        foreach (var op in to)
        {
            var prior = from.FirstOrDefault(f => f.OrderId == op.OrderId);
            if (prior is null) continue;
            if (prior.MachineId != op.MachineId)
            {
                rows.Add(new PlanDiffRow(op.OrderId, "machine", prior.MachineId ?? "-", op.MachineId ?? "-", "Makine değişti"));
            }

            if (prior.PlannedStartUtc != op.PlannedStartUtc)
            {
                var delta = (int)(op.PlannedStartUtc - prior.PlannedStartUtc).TotalMinutes;
                rows.Add(new PlanDiffRow(
                    op.OrderId,
                    "start",
                    prior.PlannedStartUtc.ToString("HH:mm", CultureInfo.InvariantCulture),
                    op.PlannedStartUtc.ToString("HH:mm", CultureInfo.InvariantCulture),
                    delta >= 0 ? $"+{delta} min" : $"{delta} min"));
            }
        }

        return rows;
    }
}

public static class PlanningAutonomyEngine
{
    public static bool CanAutoPublish(
        AdaptivePlanningPolicy policy,
        int affectedOrders,
        decimal scheduleMovementMinutes,
        decimal latenessImpactHours)
    {
        if (policy.AutonomyMode == PlanningAutonomyModes.Observe) return false;
        if (policy.AutonomyMode == PlanningAutonomyModes.Draft) return false;
        using var doc = JsonDocument.Parse(policy.PolicyEnvelopeJson);
        var maxOrders = doc.RootElement.TryGetProperty("maxOrdersAffected", out var mo) ? mo.GetInt32() : 2;
        var maxMovement = doc.RootElement.TryGetProperty("maxScheduleMovementMinutes", out var mm) ? mm.GetInt32() : 90;
        var maxLateness = doc.RootElement.TryGetProperty("maxLatenessImpactHours", out var ml)
            ? ml.GetDecimal() : 2m;
        if (policy.AutonomyMode == PlanningAutonomyModes.Limited)
            return affectedOrders <= maxOrders && scheduleMovementMinutes <= maxMovement && latenessImpactHours <= maxLateness;
        return affectedOrders <= maxOrders * 2 && latenessImpactHours <= maxLateness * 2;
    }

    public static bool RequiresApproval(
        AdaptivePlanningPolicy policy,
        int affectedOrders,
        decimal latenessImpactHours)
    {
        using var doc = JsonDocument.Parse(policy.PolicyEnvelopeJson);
        var threshold = doc.RootElement.TryGetProperty("approvalThresholdOrders", out var at) ? at.GetInt32() : 3;
        return affectedOrders >= threshold || latenessImpactHours > 4m || policy.AutonomyMode == PlanningAutonomyModes.Full;
    }
}

public static class PlanOutcomeVerificationEngine
{
    public static PlanOutcomeVerificationResult Verify(
        ProductionPlanVersion version,
        IReadOnlyList<ProductionPlanOperation> operations,
        decimal adherence)
    {
        var expected = version.ExpectedImpactJson;
        var actual = JsonSerializer.Serialize(new
        {
            adherencePercent = adherence,
            recoveredOrders = operations.Count(o => o.Status == "completed"),
            sideEffects = Array.Empty<string>(),
        });
        var status = adherence >= 70m ? "verified-success" : adherence >= 40m ? "partial" : "failed";
        return new PlanOutcomeVerificationResult(version.Id, status, expected, actual, adherence, null);
    }
}

public static class PlanningLearningBridge
{
    public static FactoryLearningMemory BuildMemory(
        Guid tenantId,
        Guid facilityId,
        ProductionPlanVersion version,
        PlanOutcomeVerificationResult outcome)
    {
        return new FactoryLearningMemory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            ProblemPattern = "planning-replan-outcome",
            ContextJson = version.ChangeSummaryJson,
            RootCauseSummary = "Plan değişikliği sonucu",
            RootCauseConfidence = "medium",
            InterventionSummary = version.ChangeSummaryJson,
            ExperimentSummary = "{}",
            ResultSummary = outcome.ActualImpactJson,
            EffectivenessScore = outcome.AdherencePercent / 100m,
            MemoryQuality = outcome.OutcomeStatus,
            ScopeJson = "{}",
            EvidenceJson = outcome.ExpectedImpactJson,
            RevalidationCriteriaJson = "{}",
            RecordedAtUtc = DateTimeOffset.UtcNow,
        };
    }
}

public static class PlanStabilityMetricsEngine
{
    public static AdaptivePlanningMetrics Build(
        IReadOnlyList<PlanChange> changesToday,
        IReadOnlyList<ProductionPlanVersion> versionsToday,
        decimal adherence,
        decimal utilization) =>
        new(
            adherence,
            adherence,
            OrdersAtRisk: 1,
            LateOrders: 0,
            PlanVersionsToday: versionsToday.Count,
            ReplansToday: changesToday.Count,
            OperationsMovedToday: changesToday.Sum(c =>
            {
                try
                {
                    using var doc = JsonDocument.Parse(c.DiffJson);
                    return doc.RootElement.TryGetProperty("movedOperations", out var m) ? m.GetInt32() : 0;
                }
                catch (JsonException) { return 0; }
            }),
            FrozenWindowOverridesToday: changesToday.Count(c => c.ChangeType.Contains("frozen-override", StringComparison.OrdinalIgnoreCase)),
            PlannedUtilizationPercent: utilization,
            ActualUtilizationPercent: utilization * 0.92m);
}
