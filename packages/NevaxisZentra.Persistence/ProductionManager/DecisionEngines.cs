using System.Text.Json;

namespace NevaxisZentra.Persistence.ProductionManager;

public static class DecisionOptionGenerator
{
    public static IReadOnlyList<DecisionOptionDraft> Generate(
        string decisionType,
        FactoryStateSnapshot state,
        ProductionManagerPolicy policy)
    {
        var options = decisionType switch
        {
            ProductionManagerDecisionTypes.OrderDelay => new List<DecisionOptionDraft>
            {
                Draft("A", "Mevcut planı koru", "Plan değişikliği yapılmaz.", """{"onTimeProbability":0.35}""", ["Termin riski devam eder"], false),
                Draft("B", "PO-1842'yi IM-09'a taşı", "Alternatif makinede üretim.", """{"onTimeProbability":0.82,"machine":"IM-09"}""", ["Kalıp uyumu kontrolü gerekir"], true),
                Draft("C", "Sipariş önceliğini değiştir", "Öncelik yükseltme.", """{"onTimeProbability":0.7}""", ["Diğer siparişler etkilenir"], true),
                Draft("D", "Vardiyalar arası böl", "Üretimi iki vardiyaya yay.", """{"onTimeProbability":0.6}""", ["Setup maliyeti artar"], true),
                Draft("E", "Bakım penceresini yeniden planla", "Bakımı kaydır.", """{"onTimeProbability":0.55}""", ["Bakım riski artabilir"], true),
            },
            ProductionManagerDecisionTypes.MachineRisk => new List<DecisionOptionDraft>
            {
                Draft("A", "Üretime devam et", "Risk kabul edilir.", """{"downtimeHours":0}""", ["Arıza olasılığı"], false),
                Draft("B", "Sınırlı bakım penceresi aç", "Planlı mini bakım.", """{"downtimeHours":1.5,"recovery":0.9}""", ["Kısa duruş"], true),
            },
            ProductionManagerDecisionTypes.MaterialDepletion => new List<DecisionOptionDraft>
            {
                Draft("A", "Mevcut stokla devam", "Tükenmeye kadar.", """{"blockRisk":0.8}""", ["Üretim durabilir"], false),
                Draft("B", "Alternatif malzeme", "Onaylı alternatif.", """{"blockRisk":0.2}""", ["Kalite doğrulama"], true),
                Draft("C", "Tedarikçi hızlandırma", "Acil teslimat.", """{"blockRisk":0.15}""", ["Maliyet artışı"], true),
            },
            ProductionManagerDecisionTypes.QualityEscalation => new List<DecisionOptionDraft>
            {
                Draft("A", "İzlemeye devam", "Ek örneklem.", """{"scrapRisk":0.5}""", ["Trend devam edebilir"], false),
                Draft("B", "Lot hold", "Üretimi durdur.", """{"scrapRisk":0.2}""", ["Kapasite kaybı"], true),
            },
            ProductionManagerDecisionTypes.MoldRisk => new List<DecisionOptionDraft>
            {
                Draft("A", "Kalıpla devam", "Trial sonrası.", """{"qualityRisk":0.4}""", ["Hata tekrarı"], false),
                Draft("B", "Kalıp revizyonu", "Bakım + trial.", """{"qualityRisk":0.15}""", ["Süre kaybı"], true),
            },
            ProductionManagerDecisionTypes.CapacityOverload => new List<DecisionOptionDraft>
            {
                Draft("A", "Planı koru", "Overload kabul.", """{"utilization":0.95}""", ["Gecikme riski"], false),
                Draft("B", "Makine yeniden dağıt", "IM-09'a kaydır.", """{"utilization":0.78}""", ["Setup"], true),
                Draft("C", "Vardiya kaynağı ekle", "Operatör desteği.", """{"utilization":0.82}""", ["İnsan kaynağı"], true),
            },
            _ => new List<DecisionOptionDraft>
            {
                Draft("A", "Mevcut durumu koru", "Aksiyon yok.", "{}", [], false),
            },
        };

        return DecisionHardConstraintEngine.Apply(state, policy, options);
    }

    private static DecisionOptionDraft Draft(
        string code, string title, string desc, string outcome, IReadOnlyList<string> risks, bool approval) =>
        new(code, title, desc, outcome, risks, approval, false, null);
}

public static class DecisionHardConstraintEngine
{
    public static IReadOnlyList<DecisionOptionDraft> Apply(
        FactoryStateSnapshot state,
        ProductionManagerPolicy policy,
        IReadOnlyList<DecisionOptionDraft> options)
    {
        var prohibited = JsonSerializer.Deserialize<List<string>>(policy.ProhibitedDecisionsJson) ?? [];
        var result = new List<DecisionOptionDraft>();

        foreach (var option in options)
        {
            if (prohibited.Contains(option.OptionCode, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(option with { IsRejected = true, RejectionReason = "Politika yasaklı." });
                continue;
            }

            if (option.Title.Contains("IM-09", StringComparison.Ordinal) &&
                state.MachineStateJson.Contains("\"stopped\":2", StringComparison.Ordinal))
            {
                result.Add(option with { IsRejected = true, RejectionReason = "IM-09 uygun değil." });
                continue;
            }

            if (option.Title.Contains("PLC", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(option with { IsRejected = true, RejectionReason = "Makine güvenlik sınırı." });
                continue;
            }

            result.Add(option);
        }

        return result;
    }
}

public static class DecisionScoringEngine
{
    public static IReadOnlyList<DecisionOptionDraft> ScoreAndRank(
        IReadOnlyList<DecisionOptionDraft> options,
        string decisionType)
    {
        var scored = options
            .Where(o => !o.IsRejected)
            .Select(o =>
            {
                var baseScore = decisionType switch
                {
                    ProductionManagerDecisionTypes.OrderDelay => o.OptionCode switch
                    {
                        "B" => 0.88m,
                        "C" => 0.75m,
                        "D" => 0.65m,
                        "E" => 0.55m,
                        _ => 0.35m,
                    },
                    ProductionManagerDecisionTypes.MachineRisk => o.OptionCode == "B" ? 0.8m : 0.4m,
                    ProductionManagerDecisionTypes.MaterialDepletion => o.OptionCode switch
                    {
                        "C" => 0.78m,
                        "B" => 0.72m,
                        _ => 0.3m,
                    },
                    _ => 0.6m,
                };

                if (o.Risks.Any(r => r.Contains("güvenlik", StringComparison.OrdinalIgnoreCase)))
                {
                    baseScore = 0;
                }

                return o with { };
            })
            .OrderByDescending(o => ScoreOption(o, decisionType))
            .ToList();

        return scored;
    }

    private static decimal ScoreOption(DecisionOptionDraft option, string decisionType)
    {
        if (option.IsRejected) return -1;
        return decisionType switch
        {
            ProductionManagerDecisionTypes.OrderDelay => option.OptionCode switch
            {
                "B" => 0.88m, "C" => 0.75m, "D" => 0.65m, "E" => 0.55m, _ => 0.35m,
            },
            ProductionManagerDecisionTypes.MachineRisk => option.OptionCode == "B" ? 0.8m : 0.4m,
            ProductionManagerDecisionTypes.MaterialDepletion => option.OptionCode switch
            {
                "C" => 0.78m, "B" => 0.72m, _ => 0.3m,
            },
            _ => 0.6m,
        };
    }

    public static decimal GetScore(DecisionOptionDraft option, string decisionType) =>
        ScoreOption(option, decisionType);
}

public static class WhatIfSimulator
{
    public static SimulationResult Simulate(FactoryStateSnapshot state, SimulationRequest request)
    {
        var delay = request.DelayHours ?? 0;
        var capacityAdj = request.CapacityAdjustmentPercent ?? 0;

        var rows = new List<ScenarioComparisonRow>
        {
            new("current", "Mevcut Plan", 0.55m, 70m, 88m, 0m, "medium", "low", false, 0m),
            new("A", "Seçenek A", 0.45m, 68m, 85m, delay, "medium", "low", false, delay),
            new("B", "Seçenek B", Math.Min(0.95m, 0.7m + capacityAdj / 100m), 85m, 78m, 1.5m, "low", "medium", true, 2m),
            new("C", "Seçenek C", 0.72m, 80m, 82m, 0.5m, "low", "low", true, 3m),
        };

        if (!string.IsNullOrWhiteSpace(request.MachineId))
        {
            rows[1] = rows[1] with { Label = $"{request.MachineId} duruşu", DowntimeHours = delay > 0 ? delay : 3 };
        }

        return new SimulationResult(
            request.ScenarioType,
            JsonSerializer.Serialize(new
            {
                classification = ProductionManagerDataClassifications.Simulated,
                productionImpact = delay > 0 ? $"-{delay}h" : "neutral",
                deadlineImpact = delay > 2 ? "at-risk" : "stable",
            }),
            JsonSerializer.Serialize(rows),
            rows);
    }
}

public static class DecisionConfidenceEngine
{
    public static (decimal Score, string Band) Evaluate(
        FactoryStateSnapshot state,
        IReadOnlyList<FactoryDecisionMemory> similarMemories)
    {
        var freshnessPenalty = state.FreshnessJson.Contains("AddMinutes(-15)", StringComparison.Ordinal) ? 0m : 0.1m;
        var evidenceBoost = similarMemories.Count >= 2 ? 0.1m : 0m;
        var score = Math.Clamp(0.75m - freshnessPenalty + evidenceBoost, 0, 1);

        var band = score >= 0.75m ? ProductionManagerConfidenceBands.High
            : score >= 0.5m ? ProductionManagerConfidenceBands.Medium
            : score >= 0.3m ? ProductionManagerConfidenceBands.Low
            : ProductionManagerConfidenceBands.Insufficient;

        return (score, band);
    }
}

public static class AutonomyGuardEngine
{
    public static bool CanExecuteAutonomously(
        ProductionManagerPolicy policy,
        FactoryDecision decision,
        FactoryDecisionOption option,
        FactoryStateSnapshot state)
    {
        if (policy.AutonomyMode == ProductionManagerAutonomyModes.Observe)
        {
            return false;
        }

        if (policy.AutonomyMode == ProductionManagerAutonomyModes.Draft)
        {
            return false;
        }

        if (decision.ConfidenceScore < policy.LowConfidenceThreshold)
        {
            return false;
        }

        if (IsStale(state, policy))
        {
            return false;
        }

        if (HasConflict(state))
        {
            return false;
        }

        if (option.RequiresApproval)
        {
            return policy.AutonomyMode == ProductionManagerAutonomyModes.Full &&
                   decision.ConfidenceScore >= policy.ApprovalThresholdScore;
        }

        return policy.AutonomyMode is ProductionManagerAutonomyModes.Limited or ProductionManagerAutonomyModes.Full;
    }

    public static bool RequiresApproval(
        ProductionManagerPolicy policy,
        FactoryDecision decision,
        FactoryDecisionOption option,
        FactoryStateSnapshot state)
    {
        if (option.RequiresApproval) return true;
        if (decision.ConfidenceScore < policy.LowConfidenceThreshold) return true;
        if (IsStale(state, policy)) return true;
        if (HasConflict(state)) return true;
        if (policy.AutonomyMode is ProductionManagerAutonomyModes.Observe or ProductionManagerAutonomyModes.Draft) return true;
        return false;
    }

    private static bool IsStale(FactoryStateSnapshot state, ProductionManagerPolicy policy)
    {
        var age = DateTimeOffset.UtcNow - state.CapturedAtUtc;
        return age.TotalMinutes > policy.StaleDataMaxMinutes;
    }

    private static bool HasConflict(FactoryStateSnapshot state) =>
        state.FreshnessJson.Contains("conflict", StringComparison.OrdinalIgnoreCase);
}

public static class ExpectedVsActualEngine
{
    public static FactoryDecisionOutcome Compare(FactoryDecision decision, string actualOutcomeJson)
    {
        var expected = JsonDocument.Parse(decision.ExpectedOutcomeJson);
        var actual = JsonDocument.Parse(actualOutcomeJson);

        var expectedOnTime = expected.RootElement.TryGetProperty("onTimeProbability", out var e) ? e.GetDecimal() : 0.5m;
        var actualOnTime = actual.RootElement.TryGetProperty("onTimeProbability", out var a) ? a.GetDecimal() : expectedOnTime;
        var variance = actualOnTime - expectedOnTime;
        var effectiveness = Math.Clamp(0.5m + variance, 0, 1);

        return new FactoryDecisionOutcome
        {
            Id = Guid.NewGuid(),
            TenantId = decision.TenantId,
            DecisionId = decision.Id,
            ExpectedJson = decision.ExpectedOutcomeJson,
            ActualJson = actualOutcomeJson,
            VarianceJson = JsonSerializer.Serialize(new { onTimeDelta = variance }),
            EffectivenessScore = effectiveness,
            Result = effectiveness >= 0.7m ? "successful" : effectiveness >= 0.4m ? "partial" : "failed",
            DataClassification = ProductionManagerDataClassifications.Verified,
            RecordedAtUtc = DateTimeOffset.UtcNow,
        };
    }
}

public static class DecisionMemoryEngine
{
    public static IReadOnlyList<FactoryDecisionMemory> FindSimilar(
        IReadOnlyList<FactoryDecisionMemory> memories,
        string decisionType,
        string contextJson,
        int limit = 4)
    {
        return memories
            .Where(m => m.DecisionType == decisionType && m.EffectivenessScore >= 0.7m)
            .OrderByDescending(m => m.EffectivenessScore)
            .Take(limit)
            .ToList();
    }

    public static bool RevalidateConditions(FactoryDecisionMemory memory, FactoryStateSnapshot state)
    {
        if (memory.ConditionsJson.Contains("machine:IM-07", StringComparison.Ordinal) &&
            !state.MachineStateJson.Contains("IM-07", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    public static FactoryDecisionMemory CreateFromOutcome(
        FactoryDecision decision,
        FactoryDecisionOption option,
        FactoryDecisionOutcome outcome,
        FactoryStateSnapshot state) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = decision.TenantId,
        FacilityId = decision.FacilityId,
        DecisionId = decision.Id,
        DecisionType = decision.DecisionType,
        ContextJson = state.ProductionStateJson,
        SelectedOptionCode = option.OptionCode,
        OutcomeSummary = outcome.Result,
        EffectivenessScore = outcome.EffectivenessScore,
        ConditionsJson = JsonSerializer.Serialize(new { machine = "IM-07", shift = "active" }),
        RecordedAtUtc = DateTimeOffset.UtcNow,
    };
}
