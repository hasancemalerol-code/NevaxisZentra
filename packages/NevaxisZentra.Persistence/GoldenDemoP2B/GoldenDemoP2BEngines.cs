using System.Globalization;
using System.Text.Json;
using NevaxisZentra.Persistence.GoldenDemoP1;

namespace NevaxisZentra.Persistence.GoldenDemoP2B;

public static class GoldenDemoP2BAgentEvidenceEngine
{
    private static readonly Dictionary<string, string> AgentLabels = new()
    {
        [GoldenDemoAgentCodes.ProductionPerformance] = "Üretim Performans Ajanı",
        [GoldenDemoAgentCodes.MachinePerformance] = "Makine Performans Ajanı",
        [GoldenDemoAgentCodes.ProcessOptimization] = "Proses Optimizasyon Ajanı",
        [GoldenDemoAgentCodes.MoldHealth] = "Kalıp Sağlık Ajanı",
        [GoldenDemoAgentCodes.Maintenance] = "Bakım Ajanı",
        [GoldenDemoAgentCodes.Energy] = "Enerji Ajanı",
        [GoldenDemoAgentCodes.Orchestrator] = "FactOS Orchestrator",
    };

    public static IReadOnlyList<AgentEvidenceDetail> Build(
        GoldenDemoWorkspaceSnapshot workspace,
        GoldenDemoRecoveryPlan? plan)
    {
        return workspace.AgentFindings
            .Where(f => f.AgentCode != GoldenDemoAgentCodes.Orchestrator)
            .Select(f => BuildDetail(f, workspace, plan))
            .ToList();
    }

    public static AgentEvidenceDetail BuildDetail(
        GoldenDemoAgentFinding finding,
        GoldenDemoWorkspaceSnapshot workspace,
        GoldenDemoRecoveryPlan? plan)
    {
        var detail = ParseDetail(finding.DetailJson);
        var inputs = BuildDataInputsReviewed(finding.AgentCode, workspace);
        var calcInputs = BuildCalculationInputs(finding.AgentCode, workspace, detail);
        var calcResults = BuildCalculationResults(finding.AgentCode, workspace, detail);
        var insufficient = BuildInsufficientState(finding.AgentCode, workspace, finding);
        var confidence = BuildConfidence(finding.AgentCode, workspace, insufficient);
        var resultingAction = BuildResultingAction(finding.AgentCode, workspace, plan);
        var conclusion = string.IsNullOrWhiteSpace(detail.Conclusion) ? finding.Summary : detail.Conclusion;

        return new AgentEvidenceDetail(
            finding.Id,
            finding.AgentCode,
            finding.WorkerCode,
            AgentLabels.GetValueOrDefault(finding.AgentCode, finding.AgentCode),
            finding.Summary,
            finding.EvidenceClassification,
            finding.GeneratedAtUtc,
            inputs,
            calcInputs,
            calcResults,
            conclusion,
            confidence,
            insufficient,
            workspace.Event.Id,
            workspace.Event.MachineAssetId,
            workspace.Event.MachineCode,
            resultingAction,
            plan?.Id.ToString(),
            plan?.Id.ToString());
    }

    private static AgentEvidenceDetailPayload ParseDetail(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new AgentEvidenceDetailPayload();
        }

        try
        {
            return JsonSerializer.Deserialize<AgentEvidenceDetailPayload>(json) ?? new AgentEvidenceDetailPayload();
        }
        catch (JsonException)
        {
            return new AgentEvidenceDetailPayload();
        }
    }

    private static IReadOnlyList<string> BuildDataInputsReviewed(string agentCode, GoldenDemoWorkspaceSnapshot ws) =>
        agentCode switch
        {
            GoldenDemoAgentCodes.ProductionPerformance =>
            [
                "Vardiya hedefi",
                "Gerçekleşen iyi parça",
                "Kalan vardiya süresi",
                "Verim / red oranı",
            ],
            GoldenDemoAgentCodes.MachinePerformance =>
            [
                "Referans cycle",
                "Mevcut cycle",
                "Cycle faz ayrıştırması",
                "Mikro-duruş verisi",
            ],
            GoldenDemoAgentCodes.ProcessOptimization =>
            [
                "Referans faz süreleri",
                "Mevcut faz süreleri",
                "Kök katkıda bulunan analizi",
            ],
            GoldenDemoAgentCodes.MoldHealth =>
            [
                "Kalıp referansı",
                "Makine durumu",
                "Referans kaynağı",
            ],
            GoldenDemoAgentCodes.Maintenance =>
            [
                "Mikro-duruş sayısı",
                "Mikro-duruş süresi",
                "Bakım müdahale eylemleri",
            ],
            GoldenDemoAgentCodes.Energy =>
            [
                "kWh / iyi parça",
                "Referans enerji yoğunluğu",
            ],
            _ => ["Üretim olayı verisi"],
        };

    private static Dictionary<string, string> BuildCalculationInputs(
        string agentCode,
        GoldenDemoWorkspaceSnapshot ws,
        AgentEvidenceDetailPayload detail)
    {
        var map = new Dictionary<string, string>();
        if (ws.CycleLoss.HasBaseline)
        {
            map["referenceCycleSeconds"] = Format(ws.CycleLoss.ReferenceCycleSeconds);
            map["currentCycleSeconds"] = Format(ws.CycleLoss.CurrentCycleSeconds);
        }

        map["shiftTargetGoodParts"] = ws.Event.ShiftTargetGoodParts.ToString(CultureInfo.InvariantCulture);
        map["currentExpectedShiftGoodParts"] = ws.Recovery.CurrentExpectedShiftGoodParts.ToString(CultureInfo.InvariantCulture);
        map["recoveredExpectedShiftGoodParts"] = ws.Recovery.RecoveredExpectedShiftGoodParts.ToString(CultureInfo.InvariantCulture);

        foreach (var pair in detail.CalculationInputs)
        {
            map[pair.Key] = pair.Value;
        }

        return map;
    }

    private static Dictionary<string, string> BuildCalculationResults(
        string agentCode,
        GoldenDemoWorkspaceSnapshot ws,
        AgentEvidenceDetailPayload detail)
    {
        var map = new Dictionary<string, string>();
        switch (agentCode)
        {
            case GoldenDemoAgentCodes.ProductionPerformance:
                map["productionRecoveryGoodParts"] = ws.Recovery.ProductionRecoveryGoodParts.ToString(CultureInfo.InvariantCulture);
                map["productionRecoveryPercent"] = Format(ws.Recovery.ProductionRecoveryPercent);
                break;
            case GoldenDemoAgentCodes.MachinePerformance:
                if (ws.CycleLoss.HasBaseline)
                {
                    map["cycleLossSeconds"] = Format(ws.CycleLoss.CycleLossSeconds);
                    map["microStopEquivalentLossSeconds"] = Format(ws.CycleLoss.MicroStopEquivalentLossSeconds);
                }
                break;
            case GoldenDemoAgentCodes.ProcessOptimization:
                if (ws.Contributors.Count > 0)
                {
                    var top = ws.Contributors[0];
                    map["topContributor"] = top.Label;
                    map["measuredDeviationSeconds"] = Format(top.MeasuredDeviationSeconds);
                }
                break;
            case GoldenDemoAgentCodes.Energy:
                if (ws.Metrics.EnergyIntensityDeviationPercent is not null)
                {
                    map["energyIntensityDeviationPercent"] = Format(ws.Metrics.EnergyIntensityDeviationPercent.Value);
                }
                break;
        }

        foreach (var pair in detail.CalculationResults)
        {
            map[pair.Key] = pair.Value;
        }

        return map;
    }

    private static string? BuildInsufficientState(
        string agentCode,
        GoldenDemoWorkspaceSnapshot ws,
        GoldenDemoAgentFinding finding)
    {
        if (agentCode == GoldenDemoAgentCodes.MachinePerformance && !ws.CycleLoss.HasBaseline)
        {
            return "INSUFFICIENT BASELINE";
        }

        if (agentCode == GoldenDemoAgentCodes.MoldHealth &&
            string.IsNullOrWhiteSpace(ws.Event.MoldReference))
        {
            return "INSUFFICIENT MOLD EVIDENCE";
        }

        if (agentCode == GoldenDemoAgentCodes.Energy && ws.Metrics.KwhPerGoodPart is null)
        {
            return "ENERGY DATA INSUFFICIENT";
        }

        if (finding.Summary.Contains("Insufficient", StringComparison.OrdinalIgnoreCase))
        {
            return finding.Summary;
        }

        return null;
    }

    private static string BuildConfidence(string agentCode, GoldenDemoWorkspaceSnapshot ws, string? insufficient)
    {
        if (!string.IsNullOrWhiteSpace(insufficient))
        {
            return "low";
        }

        return agentCode switch
        {
            GoldenDemoAgentCodes.MachinePerformance => ws.CycleLoss.HasBaseline ? "high" : "low",
            GoldenDemoAgentCodes.ProcessOptimization => ws.Contributors.Count > 0 ? "high" : "medium",
            _ => ws.Recovery.ConfidenceLevel,
        };
    }

    private static string BuildResultingAction(string agentCode, GoldenDemoWorkspaceSnapshot ws, GoldenDemoRecoveryPlan? plan)
    {
        return agentCode switch
        {
            GoldenDemoAgentCodes.ProductionPerformance =>
                "Üretim kurtarma fırsatı hesaplandı ve kurtarma planına girdi olarak aktarıldı.",
            GoldenDemoAgentCodes.MachinePerformance =>
                "Cycle loss ayrıştırması kurtarma planı eylemlerine teknik gerekçe sağladı.",
            GoldenDemoAgentCodes.ProcessOptimization =>
                "Proses faz katkıda bulunanları bakım/proses eylemlerine eşlendi.",
            GoldenDemoAgentCodes.MoldHealth =>
                "Kalıp/makine bağlamı kurtarma değerlendirmesine dahil edildi.",
            GoldenDemoAgentCodes.Maintenance =>
                "FactOS teknik müdahale iş planını oluşturdu ve izlemeye aldı.",
            GoldenDemoAgentCodes.Energy =>
                "Enerji yoğunluğu ikincil etki katmanına aktarıldı.",
            _ => plan is null ? "Analiz tamamlandı." : "Kurtarma planı değerlendirmesine katkı sağlandı.",
        };
    }

    private static string Format(decimal value) => value.ToString("0.0", CultureInfo.InvariantCulture);

    private sealed class AgentEvidenceDetailPayload
    {
        public string Conclusion { get; init; } = string.Empty;
        public Dictionary<string, string> CalculationInputs { get; init; } = new();
        public Dictionary<string, string> CalculationResults { get; init; } = new();
    }
}

public static class GoldenDemoP2BOrchestratorEngine
{
    public static OrchestratorAssessment Build(GoldenDemoWorkspaceSnapshot workspace, GoldenDemoRecoveryPlan? plan)
    {
        var topContributors = workspace.Contributors.Take(3).Select(c => c.Label).ToList();
        var inputsUsed = workspace.AgentFindings
            .Where(f => f.AgentCode != GoldenDemoAgentCodes.Orchestrator)
            .Select(f => GoldenDemoP2BAgentEvidenceEngine.BuildDetail(f, workspace, plan).AgentLabel)
            .ToList();

        var insufficient = workspace.AgentFindings
            .Where(f => f.AgentCode != GoldenDemoAgentCodes.Orchestrator)
            .Select(f => GoldenDemoP2BAgentEvidenceEngine.BuildDetail(f, workspace, plan))
            .Where(d => !string.IsNullOrWhiteSpace(d.InsufficientEvidenceState))
            .Select(d => $"{d.AgentLabel} — {d.InsufficientEvidenceState}")
            .ToList();

        var recommended = workspace.WhatIfScenarios.FirstOrDefault(s => s.IsRecommended);
        var risk = recommended?.RiskExposure ?? workspace.Recovery.SafetyMessage;

        return new OrchestratorAssessment(
            $"{workspace.Event.MachineCode}: vardiya hedefi risk altında; mevcut cycle {Format(workspace.CycleLoss.CurrentCycleSeconds)} sn.",
            topContributors.Count > 0
                ? $"En güçlü kanıtlar: {string.Join(", ", topContributors)}."
                : "Yeterli faz kaybı kanıtı bulunamadı.",
            workspace.Recovery.CurrentExpectedShiftGoodParts,
            workspace.Recovery.RecoveredExpectedShiftGoodParts,
            workspace.Recovery.ProductionRecoveryGoodParts,
            plan?.RecommendationSummary ?? "Kontrollü teknik müdahale önerilir.",
            plan?.RequiresApproval == true && plan.ApprovedAtUtc is null,
            "production-manager / planning-pilot-reviewer",
            "İnsan onayı, fiziksel müdahaleyi yetkili operatör/bakım rolüne devreder; FactOS PLC parametresi değiştirmez.",
            workspace.Recovery.SafetyMessage,
            inputsUsed,
            insufficient);
    }

    private static string Format(decimal value) => value.ToString("0.0", CultureInfo.InvariantCulture);
}

public static class GoldenDemoP2BActivityProjector
{
    public static IReadOnlyList<GoldenDemoActivityItem> Build(GoldenDemoWorkspaceSnapshot workspace, GoldenDemoRecoveryPlan? plan)
    {
        var items = new List<GoldenDemoActivityItem>();

        foreach (var finding in workspace.AgentFindings.Where(f => f.AgentCode != GoldenDemoAgentCodes.Orchestrator))
        {
            items.Add(new GoldenDemoActivityItem(
                finding.GeneratedAtUtc,
                GetGoldenDemoAgentLabel(finding.AgentCode),
                GetAgentActivityAction(finding.AgentCode),
                "completed",
                $"finding:{finding.Id}",
                finding.EvidenceClassification,
                finding.Summary));
        }

        if (plan is not null)
        {
            items.Add(new GoldenDemoActivityItem(
                plan.CreatedAtUtc,
                "FactOS Orchestrator",
                "Recovery plan created",
                plan.Status,
                $"plan:{plan.Id}",
                GoldenDemoImpactClassifications.Calculated,
                plan.RecommendationSummary));

            if (plan.RequiresApproval && plan.ApprovedAtUtc is null)
            {
                items.Add(new GoldenDemoActivityItem(
                    plan.CreatedAtUtc,
                    "FactOS Orchestrator",
                    "Approval requested",
                    "pending",
                    $"plan:{plan.Id}",
                    GoldenDemoImpactClassifications.Calculated,
                    "Human approval required before intervention execution."));
            }

            if (plan.ApprovedAtUtc is not null)
            {
                items.Add(new GoldenDemoActivityItem(
                    plan.ApprovedAtUtc.Value,
                    plan.ApprovedBySubjectId ?? "reviewer",
                    "Approval granted",
                    "approved",
                    $"plan:{plan.Id}",
                    GoldenDemoImpactClassifications.Verified,
                    null));
            }
        }

        if (workspace.Event.Status is GoldenDemoP1EventStatuses.InterventionInProgress
            or GoldenDemoP1EventStatuses.ValidationRun
            or GoldenDemoP1EventStatuses.Verified)
        {
            items.Add(new GoldenDemoActivityItem(
                plan?.ApprovedAtUtc ?? workspace.Event.UpdatedAtUtc,
                "production-pilot-operator",
                "Intervention started",
                workspace.Event.Status,
                $"event:{workspace.Event.Id}",
                GoldenDemoImpactClassifications.Calculated,
                "FactOS coordinated the technical intervention; authorized role executes physical work."));
        }

        if (workspace.Event.Status is GoldenDemoP1EventStatuses.ValidationRun or GoldenDemoP1EventStatuses.Verified)
        {
            items.Add(new GoldenDemoActivityItem(
                workspace.Event.UpdatedAtUtc,
                "production-pilot-operator",
                "Intervention completed",
                "completed",
                $"event:{workspace.Event.Id}",
                GoldenDemoImpactClassifications.Measured,
                "Post-intervention observation window recorded."));
            items.Add(new GoldenDemoActivityItem(
                workspace.Event.UpdatedAtUtc,
                "production-pilot-operator",
                "Validation run started",
                "in-progress",
                $"event:{workspace.Event.Id}",
                GoldenDemoImpactClassifications.Calculated,
                null));
        }

        if (workspace.Verification is not null)
        {
            items.Add(new GoldenDemoActivityItem(
                workspace.Verification.VerifiedAtUtc,
                workspace.Verification.TriggeredBySubjectId,
                "Outcome verified",
                workspace.Verification.Result,
                $"verification:{workspace.Verification.Id}",
                GoldenDemoImpactClassifications.Verified,
                workspace.Verification.Summary));
        }

        return items.OrderBy(i => i.TimestampUtc).ToList();
    }

    private static string GetAgentActivityAction(string agentCode) => agentCode switch
    {
        GoldenDemoAgentCodes.ProductionPerformance => "Production gap calculated",
        GoldenDemoAgentCodes.MachinePerformance => "Machine cycle loss decomposed",
        GoldenDemoAgentCodes.ProcessOptimization => "Process contributors evaluated",
        GoldenDemoAgentCodes.MoldHealth => "Mold evidence reviewed",
        GoldenDemoAgentCodes.Energy => "Energy intensity reviewed",
        GoldenDemoAgentCodes.Maintenance => "Maintenance recovery work mapped",
        _ => "Specialist analysis completed",
    };

    private static string GetGoldenDemoAgentLabel(string agentCode) => agentCode switch
    {
        GoldenDemoAgentCodes.ProductionPerformance => "Üretim Performans Ajanı",
        GoldenDemoAgentCodes.MachinePerformance => "Makine Performans Ajanı",
        GoldenDemoAgentCodes.ProcessOptimization => "Proses Optimizasyon Ajanı",
        GoldenDemoAgentCodes.MoldHealth => "Kalıp Sağlık Ajanı",
        GoldenDemoAgentCodes.Maintenance => "Bakım Ajanı",
        GoldenDemoAgentCodes.Energy => "Enerji Ajanı",
        _ => agentCode,
    };
}

public static class GoldenDemoP2BApprovalSheetEngine
{
    public static ApprovalSheetModel Build(GoldenDemoWorkspaceSnapshot workspace, GoldenDemoRecoveryPlan? plan)
    {
        var recommended = workspace.WhatIfScenarios.FirstOrDefault(s => s.IsRecommended);
        var interruption = workspace.PlanActions.Sum(a => a.EstimatedDowntimeMinutes);
        var exposure = Math.Max(0, workspace.Event.ShiftTargetGoodParts - workspace.Metrics.ActualGoodParts);
        var evidenceRefs = workspace.AgentFindings
            .Where(f => f.AgentCode != GoldenDemoAgentCodes.Orchestrator)
            .Select(f => $"{f.AgentCode}:{f.Id}")
            .ToList();

        return new ApprovalSheetModel(
            plan?.RequiresApproval == true,
            plan?.ApprovedAtUtc is not null,
            plan?.RecommendationSummary ?? "Kontrollü teknik müdahale",
            recommended?.RecommendationReason ?? plan?.RecommendationSummary ?? workspace.Recovery.SafetyMessage,
            interruption,
            exposure,
            workspace.Recovery.CurrentExpectedShiftGoodParts,
            workspace.Recovery.RecoveredExpectedShiftGoodParts,
            workspace.Recovery.ProductionRecoveryGoodParts,
            recommended?.RiskExposure ?? "Operational risk if losses continue.",
            "planning-pilot-reviewer / production-manager",
            evidenceRefs,
            plan?.CreatedAtUtc ?? workspace.Event.CreatedAtUtc,
            "FactOS Digital Factory Team",
            plan?.ApprovedBySubjectId,
            plan?.ApprovedAtUtc,
            plan?.ApprovedAtUtc is not null ? "approved" : plan?.RequiresApproval == true ? "pending" : "not-required",
            plan?.Id.ToString() ?? workspace.Event.Id.ToString());
    }
}

public static class GoldenDemoP2BPlanTraceabilityEngine
{
    private static readonly Dictionary<string, string> ActionAgentMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cooling-circuit-inspection"] = GoldenDemoAgentCodes.ProcessOptimization,
        ["mold-movement-inspection"] = GoldenDemoAgentCodes.MachinePerformance,
        ["screw-recovery-review"] = GoldenDemoAgentCodes.ProcessOptimization,
        ["controlled-validation-run"] = GoldenDemoAgentCodes.Orchestrator,
    };

    public static IReadOnlyList<PlanActionTraceability> Build(
        GoldenDemoWorkspaceSnapshot workspace,
        GoldenDemoRecoveryPlan? plan)
    {
        if (plan is null)
        {
            return [];
        }

        return workspace.PlanActions.Select(action =>
        {
            var sourceAgent = ActionAgentMap.GetValueOrDefault(action.ActionCode, GoldenDemoAgentCodes.Maintenance);
            var finding = workspace.AgentFindings.FirstOrDefault(f => f.AgentCode == sourceAgent);
            return new PlanActionTraceability(
                action.Id,
                plan.Id,
                action.SortOrder,
                action.Title,
                action.ResponsibleRole,
                action.Status,
                action.EvidenceRequired,
                action.ValidationCriteria,
                sourceAgent,
                GetAgentLabel(sourceAgent),
                finding?.Summary ?? "Related technical finding unavailable.",
                action.CompletedAtUtc,
                $"plan-action:{action.Id}");
        }).ToList();
    }

    private static string GetAgentLabel(string agentCode) => agentCode switch
    {
        GoldenDemoAgentCodes.ProcessOptimization => "Proses Optimizasyon Ajanı",
        GoldenDemoAgentCodes.MachinePerformance => "Makine Performans Ajanı",
        GoldenDemoAgentCodes.Orchestrator => "FactOS Orchestrator",
        GoldenDemoAgentCodes.Maintenance => "Bakım Ajanı",
        _ => agentCode,
    };
}

public static class GoldenDemoP2BInterventionTimelineEngine
{
    public static IReadOnlyList<InterventionTimelineItem> Build(
        GoldenDemoWorkspaceSnapshot workspace,
        GoldenDemoRecoveryPlan? plan,
        GoldenDemoVerificationRun? verification)
    {
        var items = new List<InterventionTimelineItem>
        {
            Stage("plan-created", "Plan Created", "completed", plan?.CreatedAtUtc, "FactOS Orchestrator"),
        };

        if (plan?.RequiresApproval == true)
        {
            items.Add(Stage(
                "approval-required",
                "Approval Required",
                plan.ApprovedAtUtc is null ? "current" : "completed",
                plan.CreatedAtUtc,
                "FactOS Orchestrator"));
        }

        if (plan?.ApprovedAtUtc is not null)
        {
            items.Add(Stage("approved", "Approved", "completed", plan.ApprovedAtUtc, plan.ApprovedBySubjectId ?? "reviewer"));
        }

        if (workspace.Event.Status is GoldenDemoP1EventStatuses.InterventionInProgress
            or GoldenDemoP1EventStatuses.ValidationRun
            or GoldenDemoP1EventStatuses.Verified)
        {
            items.Add(Stage("intervention-started", "Intervention Started", "completed", plan?.ApprovedAtUtc, "production-pilot-operator"));
            items.Add(Stage(
                "in-progress",
                "In Progress",
                workspace.Event.Status == GoldenDemoP1EventStatuses.InterventionInProgress ? "current" : "completed",
                plan?.ApprovedAtUtc,
                "production-pilot-operator"));
        }

        if (workspace.Event.Status is GoldenDemoP1EventStatuses.ValidationRun or GoldenDemoP1EventStatuses.Verified)
        {
            items.Add(Stage("intervention-completed", "Intervention Completed", "completed", workspace.Event.UpdatedAtUtc, "production-pilot-operator"));
            items.Add(Stage("validation-run", "Validation Run", verification is null ? "current" : "completed", workspace.Event.UpdatedAtUtc, "production-pilot-operator"));
        }

        if (verification is not null)
        {
            items.Add(Stage(
                "verification",
                verification.Result switch
                {
                    GoldenDemoVerificationResults.Verified => "Verified",
                    GoldenDemoVerificationResults.PartiallyVerified => "Partially Verified",
                    GoldenDemoVerificationResults.NotVerified => "Not Verified",
                    _ => "Verification In Progress",
                },
                "completed",
                verification.VerifiedAtUtc,
                verification.TriggeredBySubjectId));
        }

        return items;
    }

    private static InterventionTimelineItem Stage(
        string code,
        string label,
        string status,
        DateTimeOffset? at,
        string actor) => new(code, label, status, at, actor);
}
