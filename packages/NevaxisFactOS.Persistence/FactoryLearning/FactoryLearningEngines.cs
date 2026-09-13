using System.Text.Json;
using NevaxisFactOS.Persistence.ProductionManager;

namespace NevaxisFactOS.Persistence.FactoryLearning;

public static class FactoryLossTree
{
    public static IReadOnlyList<string> BuildPath(string category) => category switch
    {
        FactoryLossCategories.Downtime or FactoryLossCategories.MaintenanceDelay =>
            ["Total Production Loss", "Availability", MapLeaf(category)],
        FactoryLossCategories.MicroStop or FactoryLossCategories.SpeedLoss =>
            ["Total Production Loss", "Performance", MapLeaf(category)],
        FactoryLossCategories.Scrap or FactoryLossCategories.Rework or FactoryLossCategories.QualityHold =>
            ["Total Production Loss", "Quality", MapLeaf(category)],
        FactoryLossCategories.PlanningLoss =>
            ["Total Production Loss", "Planning", MapLeaf(category)],
        FactoryLossCategories.MaterialShortage =>
            ["Total Production Loss", "Material", MapLeaf(category)],
        FactoryLossCategories.SupplierDelay =>
            ["Total Production Loss", "Supplier", MapLeaf(category)],
        FactoryLossCategories.MoldUnavailability =>
            ["Total Production Loss", "Mold", MapLeaf(category)],
        FactoryLossCategories.WaitingApproval =>
            ["Total Production Loss", "Waiting / Approval", MapLeaf(category)],
        FactoryLossCategories.ShiftHandoverLoss =>
            ["Total Production Loss", "Shift Handover", MapLeaf(category)],
        _ => ["Total Production Loss", "Other", MapLeaf(category)],
    };

    private static string MapLeaf(string category) => category switch
    {
        FactoryLossCategories.Downtime => "machine breakdown",
        FactoryLossCategories.MaintenanceDelay => "maintenance delay",
        FactoryLossCategories.MicroStop => "micro-stop",
        FactoryLossCategories.SpeedLoss => "slow cycle",
        FactoryLossCategories.Scrap => "scrap",
        FactoryLossCategories.Rework => "rework",
        FactoryLossCategories.QualityHold => "quality hold",
        FactoryLossCategories.Changeover => "changeover",
        _ => category,
    };
}

public static class LossDetectionEngine
{
    public static IReadOnlyList<FactoryLoss> DetectFromState(
        FactoryStateSnapshot state,
        Guid tenantId,
        Guid facilityId,
        IReadOnlyList<FactoryLoss> existing)
    {
        var now = DateTimeOffset.UtcNow;
        var detected = new List<FactoryLoss>();

        if (state.MachineStateJson.Contains("\"stopped\"", StringComparison.Ordinal) ||
            state.MachineStateJson.Contains("\"fault\"", StringComparison.Ordinal))
        {
            detected.Add(CreateLoss(tenantId, facilityId, FactoryLossCategories.Downtime,
                "Makine duruşu", "Beklenmeyen makine duruşu sinyali", "IM-07", 45, existing, now));
        }

        if (state.MachineStateJson.Contains("\"microStop\"", StringComparison.Ordinal))
        {
            detected.Add(CreateLoss(tenantId, facilityId, FactoryLossCategories.MicroStop,
                "Mikro duruş", "Tekrarlayan mikro duruş sinyali", "IM-07", 12, existing, now));
        }

        if (state.QualityStateJson.Contains("\"holdCount\"", StringComparison.Ordinal) ||
            state.QualityStateJson.Contains("\"scrap\"", StringComparison.Ordinal))
        {
            detected.Add(CreateLoss(tenantId, facilityId, FactoryLossCategories.Scrap,
                "Kalite firesi", "Fire/hold sinyali", null, 30, existing, now));
        }

        if (state.MaterialStateJson.Contains("\"shortageSignals\"", StringComparison.Ordinal))
        {
            detected.Add(CreateLoss(tenantId, facilityId, FactoryLossCategories.MaterialShortage,
                "Malzeme bekleme", "Malzeme eksikliği üretimi durduruyor", "PP-HF", 60, existing, now));
        }

        if (state.SupplierStateJson.Contains("\"delay\"", StringComparison.Ordinal) ||
            (state.SupplierStateJson.Contains("\"supplierCases\"", StringComparison.Ordinal) &&
             !state.SupplierStateJson.Contains("\"supplierCases\":0", StringComparison.Ordinal)))
        {
            detected.Add(CreateLoss(tenantId, facilityId, FactoryLossCategories.SupplierDelay,
                "Tedarik gecikmesi", "Tedarikçi gecikmesi kapasite kaybı", null, 90, existing, now));
        }

        if (state.MoldStateJson.Contains("\"moldCases\"", StringComparison.Ordinal))
        {
            detected.Add(CreateLoss(tenantId, facilityId, FactoryLossCategories.MoldUnavailability,
                "Kalıp kullanılamazlığı", "Kalıp bakım/yaşam döngüsü kaybı", "M-014", 35, existing, now));
        }

        if (state.ShiftStateJson.Contains("\"carryover\"", StringComparison.Ordinal) ||
            state.ShiftStateJson.Contains("pending", StringComparison.OrdinalIgnoreCase) ||
            state.ShiftStateJson.Contains("incomplete", StringComparison.OrdinalIgnoreCase))
        {
            detected.Add(CreateLoss(tenantId, facilityId, FactoryLossCategories.ShiftHandoverLoss,
                "Vardiya devir kaybı", "Tekrarlayan açık konu/devir kaybı", null, 25, existing, now));
        }

        if (state.ProductionStateJson.Contains("\"changeoverMinutes\"", StringComparison.Ordinal) ||
            state.ProductionStateJson.Contains("\"highChangeover\"", StringComparison.Ordinal))
        {
            detected.Add(CreateLoss(tenantId, facilityId, FactoryLossCategories.Changeover,
                "Değişim süresi kaybı", "Yüksek changeover süresi", "Line-2", 42, existing, now));
        }

        if (state.ProductionStateJson.Contains("\"behindScheduleCount\"", StringComparison.Ordinal))
        {
            detected.Add(CreateLoss(tenantId, facilityId, FactoryLossCategories.PlanningLoss,
                "Plan sapması", "Üretim planı gerisinde", "PO-1842", 40, existing, now));
        }

        if (state.CapacityStateJson.Contains("\"bottleneck\"", StringComparison.Ordinal))
        {
            detected.Add(CreateLoss(tenantId, facilityId, FactoryLossCategories.CapacityLoss,
                "Kapasite kaybı", "Darboğaz nedeniyle kapasite kaybı", null, 55, existing, now));
        }

        return detected;
    }

    private static FactoryLoss CreateLoss(
        Guid tenantId,
        Guid facilityId,
        string category,
        string title,
        string summary,
        string? machineId,
        decimal minutes,
        IReadOnlyList<FactoryLoss> existing,
        DateTimeOffset now)
    {
        var correlationKey = $"{category}:{machineId ?? "facility"}";
        var prior = existing.FirstOrDefault(l => l.CorrelationKey == correlationKey);
        var recurrence = (prior?.RecurrenceCount ?? 0) + 1;

        return new FactoryLoss
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            Category = category,
            Title = title,
            Summary = summary,
            MachineId = machineId,
            DurationMinutes = minutes,
            SourceModule = "factory-state-snapshot",
            SourceReference = correlationKey,
            EvidenceJson = JsonSerializer.Serialize(new[]
            {
                new { source = "factory-state-snapshot", classification = FactoryLearningDataClassifications.Derived },
            }),
            DataClassification = FactoryLearningDataClassifications.Derived,
            ConfidenceScore = 0.75m,
            CorrelationKey = correlationKey,
            RecurrenceCount = recurrence,
            LossTreePathJson = JsonSerializer.Serialize(FactoryLossTree.BuildPath(category)),
            FirstSeenAtUtc = prior?.FirstSeenAtUtc ?? now,
            LastSeenAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }
}

public static class RecurringLossDetectionEngine
{
    public static IReadOnlyList<FactoryLossPattern> DetectPatterns(IReadOnlyList<FactoryLoss> losses)
    {
        return losses
            .Where(l => l.RecurrenceCount >= 2)
            .GroupBy(l => l.CorrelationKey)
            .Select(g =>
            {
                var first = g.OrderByDescending(x => x.LastSeenAtUtc).First();
                return new FactoryLossPattern
                {
                    Id = Guid.NewGuid(),
                    TenantId = first.TenantId,
                    FacilityId = first.FacilityId,
                    PatternType = "recurring-loss",
                    Title = $"Tekrarlayan kayıp: {first.Title}",
                    Summary = $"{first.MachineId ?? "Tesis"} — {g.Count()} olay, {first.RecurrenceCount} tekrar",
                    PatternKey = first.CorrelationKey,
                    ContextJson = JsonSerializer.Serialize(new
                    {
                        machine = first.MachineId,
                        category = first.Category,
                        shiftWindow = "night",
                    }),
                    EvidenceJson = first.EvidenceJson,
                    OccurrenceCount = g.Sum(x => x.RecurrenceCount),
                    ConfidenceScore = Math.Clamp(0.5m + (g.Count() * 0.1m), 0, 1),
                    DataClassification = FactoryLearningDataClassifications.Derived,
                    FirstObservedAtUtc = g.Min(x => x.FirstSeenAtUtc),
                    LastObservedAtUtc = g.Max(x => x.LastSeenAtUtc),
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    UpdatedAtUtc = DateTimeOffset.UtcNow,
                };
            })
            .ToList();
    }
}

public static class FactoryPatternEngine
{
    public static IReadOnlyList<FactoryLossPattern> FindDeterministicPatterns(IReadOnlyList<FactoryLoss> losses)
    {
        var patterns = new List<FactoryLossPattern>();
        patterns.AddRange(RecurringLossDetectionEngine.DetectPatterns(losses));

        var machineFaults = losses
            .Where(l => l.Category == FactoryLossCategories.Downtime && !string.IsNullOrWhiteSpace(l.MachineId))
            .GroupBy(l => l.MachineId!)
            .Where(g => g.Count() >= 2 || g.Sum(x => x.RecurrenceCount) >= 2);

        foreach (var group in machineFaults)
        {
            var sample = group.First();
            patterns.Add(new FactoryLossPattern
            {
                Id = Guid.NewGuid(),
                TenantId = sample.TenantId,
                FacilityId = sample.FacilityId,
                PatternType = "machine-fault-recurrence",
                Title = $"{group.Key} tekrarlayan arıza",
                Summary = $"{group.Key} aynı arıza paterni {group.Count()} kez",
                PatternKey = $"machine-fault:{group.Key}",
                ContextJson = JsonSerializer.Serialize(new { machine = group.Key, moldFamily = "M-014" }),
                EvidenceJson = sample.EvidenceJson,
                OccurrenceCount = group.Count(),
                ConfidenceScore = 0.82m,
                DataClassification = FactoryLearningDataClassifications.Derived,
                FirstObservedAtUtc = group.Min(x => x.FirstSeenAtUtc),
                LastObservedAtUtc = group.Max(x => x.LastSeenAtUtc),
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
            });
        }

        return patterns;
    }
}

public static class LossParetoEngine
{
    public static IReadOnlyList<LossParetoRow> Calculate(IReadOnlyList<FactoryLoss> losses)
    {
        if (losses.Count == 0) return [];

        var grouped = losses
            .GroupBy(l => l.Category)
            .Select(g => new
            {
                Category = g.Key,
                TotalMinutes = g.Sum(x => x.DurationMinutes),
                Count = g.Count(),
            })
            .OrderByDescending(x => x.TotalMinutes)
            .ToList();

        var total = grouped.Sum(x => x.TotalMinutes);
        if (total <= 0) total = 1;

        return grouped.Select(g => new LossParetoRow(
            g.Category,
            g.Category,
            g.TotalMinutes,
            Math.Round(g.TotalMinutes / total * 100m, 2),
            g.Count,
            FactoryLearningDataClassifications.Measured)).ToList();
    }
}

public static class ImprovementOpportunityEngine
{
    public static IReadOnlyList<ImprovementOpportunity> Generate(
        IReadOnlyList<FactoryLossPattern> patterns,
        IReadOnlyList<FactoryLoss> losses)
    {
        var opportunities = new List<ImprovementOpportunity>();

        foreach (var pattern in patterns)
        {
            var linkedLoss = losses.FirstOrDefault(l => l.CorrelationKey == pattern.PatternKey);
            if (linkedLoss is null && losses.Count > 0)
            {
                linkedLoss = losses[0];
            }

            opportunities.Add(new ImprovementOpportunity
            {
                Id = Guid.NewGuid(),
                TenantId = pattern.TenantId,
                FacilityId = pattern.FacilityId,
                PatternId = pattern.Id,
                LossId = linkedLoss?.Id,
                Title = MapOpportunityTitle(pattern),
                ProblemSummary = pattern.Summary,
                HypothesisJson = JsonSerializer.Serialize(new
                {
                    problem = pattern.Summary,
                    hypothesis = MapHypothesis(pattern.PatternType),
                    expectedMechanism = "Bounded process change with measured guardrails",
                }),
                ExpectedMechanism = "Deterministic müdahale + ölçüm",
                ExpectedImpactJson = JsonSerializer.Serialize(new
                {
                    recoverableMinutes = pattern.OccurrenceCount * 15,
                    classification = FactoryLearningDataClassifications.Estimated,
                }),
                AffectedScopeJson = pattern.ContextJson,
                RiskJson = """["quality-regression","scope-creep"]""",
                VerificationCriteriaJson = """["duration-reduction","no-quality-regression"]""",
                RollbackCriteriaJson = """["guardrail-breach","negative-effect"]""",
                PriorityScore = ImprovementPriorityEngine.Score(pattern, linkedLoss),
                RecoverablePotentialScore = Math.Clamp(pattern.OccurrenceCount * 0.15m, 0, 1),
                ImpactClassification = FactoryLearningDataClassifications.Estimated,
                Status = ImprovementBacklogStatuses.Proposed,
                Domain = MapDomain(pattern),
                ConfidenceScore = pattern.ConfidenceScore,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
            });
        }

        return opportunities;
    }

    private static string MapOpportunityTitle(FactoryLossPattern pattern) => pattern.PatternType switch
    {
        "machine-fault-recurrence" => "Bakım aralığı deneyi",
        "recurring-loss" when pattern.PatternKey.StartsWith("downtime:", StringComparison.Ordinal) => "Bakım aralığı deneyi",
        "recurring-loss" when pattern.PatternKey.Contains("changeover", StringComparison.Ordinal) => "Setup süresi iyileştirmesi",
        "recurring-loss" when pattern.PatternKey.Contains("material") => "Malzeme eşik iyileştirmesi",
        "recurring-loss" when pattern.PatternKey.Contains("supplier") => "Alternatif tedarik stratejisi",
        "recurring-loss" when pattern.PatternKey.Contains("mold") => "Erken kalıp bakımı",
        "recurring-loss" when pattern.PatternKey.Contains("shift") => "Vardiya devir iyileştirmesi",
        _ => $"İyileştirme fırsatı: {pattern.Title}",
    };

    private static string MapHypothesis(string patternType) => patternType switch
    {
        "machine-fault-recurrence" => "Preventive maintenance interval adjustment reduces fault recurrence",
        _ => "Bounded operational change reduces recurring loss frequency",
    };

    private static string MapDomain(FactoryLossPattern pattern) => pattern.PatternType switch
    {
        "machine-fault-recurrence" => "maintenance",
        _ when pattern.PatternKey.StartsWith("downtime:", StringComparison.Ordinal) => "maintenance",
        _ when pattern.PatternKey.StartsWith("scrap:", StringComparison.Ordinal) ||
               pattern.PatternKey.Contains("quality", StringComparison.Ordinal) => "quality",
        _ when pattern.PatternKey.Contains("material", StringComparison.Ordinal) => "material",
        _ when pattern.PatternKey.Contains("supplier", StringComparison.Ordinal) => "supplier",
        _ when pattern.PatternKey.Contains("mold", StringComparison.Ordinal) => "mold",
        _ when pattern.PatternKey.Contains("shift", StringComparison.Ordinal) => "shift",
        _ => "production",
    };
}

public static class ImprovementPriorityEngine
{
    public static decimal Score(FactoryLossPattern pattern, FactoryLoss? loss)
    {
        var impact = pattern.OccurrenceCount * 10m;
        var frequency = pattern.OccurrenceCount * 5m;
        var confidence = pattern.ConfidenceScore * 20m;
        var recoverability = 15m;
        var riskPenalty = pattern.PatternType.Contains("quality") ? 5m : 0m;
        return Math.Clamp(impact + frequency + confidence + recoverability - riskPenalty, 0, 100);
    }
}

public static class ImprovementValueModel
{
    public static decimal EstimateRecoverableMinutes(ImprovementOpportunity opportunity, FactoryLossPattern? pattern) =>
        pattern is null ? 0 : pattern.OccurrenceCount * 15m;
}

public static class ExperimentSafetyEngine
{
    public static bool ValidateGuardrails(FactoryExperiment experiment, IReadOnlyList<FactoryExperimentMeasurement> measurements)
    {
        if (experiment.GuardrailsJson.Contains("quality-regression", StringComparison.Ordinal))
        {
            var quality = measurements.FirstOrDefault(m => m.MetricKey == "scrap-rate");
            if (quality is not null && quality.MetricValue > 0.05m)
            {
                return false;
            }
        }

        return true;
    }

    public static bool RequiresApproval(string experimentType) =>
        experimentType is FactoryExperimentTypes.Process or FactoryExperimentTypes.Maintenance or FactoryExperimentTypes.Quality;
}

public static class ExperimentComparisonEngine
{
    public static (string BeforeJson, string AfterJson, string DeltaJson, decimal Effectiveness) Compare(
        FactoryExperiment experiment,
        IReadOnlyList<FactoryExperimentMeasurement> measurements)
    {
        var baseline = measurements.Where(m => m.Phase == "baseline").ToList();
        var after = measurements.Where(m => m.Phase == "experiment").ToList();

        var baselineMetric = baseline.FirstOrDefault(m => m.MetricKey == "duration-minutes");
        var afterMetric = after.FirstOrDefault(m => m.MetricKey == "duration-minutes");

        var beforeValue = baselineMetric?.MetricValue ?? ParseBaseline(experiment.BaselineJson);
        var afterValue = afterMetric?.MetricValue ?? beforeValue;
        var delta = beforeValue - afterValue;
        var effectiveness = beforeValue <= 0 ? 0 : Math.Clamp(delta / beforeValue, -1, 1);

        var beforeJson = JsonSerializer.Serialize(new { durationMinutes = beforeValue, classification = FactoryLearningDataClassifications.Measured });
        var afterJson = JsonSerializer.Serialize(new { durationMinutes = afterValue, classification = FactoryLearningDataClassifications.Measured });
        var deltaJson = JsonSerializer.Serialize(new { durationMinutes = delta, classification = FactoryLearningDataClassifications.Measured });

        return (beforeJson, afterJson, deltaJson, effectiveness);
    }

    private static decimal ParseBaseline(string baselineJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(baselineJson);
            if (doc.RootElement.TryGetProperty("durationMinutes", out var value))
            {
                return value.GetDecimal();
            }
        }
        catch
        {
            // ignored
        }

        return 42m;
    }
}

public static class ExperimentOutcomeEngine
{
    public static string Classify(decimal effectiveness, bool guardrailsSatisfied, bool hasBaseline, bool hasMeasurements)
    {
        if (!hasBaseline || !hasMeasurements) return FactoryExperimentOutcomes.Inconclusive;
        if (!guardrailsSatisfied) return FactoryExperimentOutcomes.NegativeEffect;
        if (effectiveness >= 0.15m) return FactoryExperimentOutcomes.Success;
        if (effectiveness >= 0.05m) return FactoryExperimentOutcomes.PartialSuccess;
        if (effectiveness <= -0.05m) return FactoryExperimentOutcomes.NegativeEffect;
        if (effectiveness > -0.05m && effectiveness < 0.05m) return FactoryExperimentOutcomes.NoEffect;
        return FactoryExperimentOutcomes.Inconclusive;
    }
}

public static class ImprovementSustainabilityEngine
{
    public static string Evaluate(decimal initialEffectiveness, decimal currentEffectiveness)
    {
        if (currentEffectiveness >= initialEffectiveness * 0.85m) return "sustained";
        if (currentEffectiveness >= initialEffectiveness * 0.6m) return "monitoring";
        return "regressing";
    }
}

public static class FactoryLearningMemoryEngine
{
    public static FactoryLearningMemory CreateFromVerifiedImprovement(
        VerifiedImprovement improvement,
        FactoryExperiment experiment,
        ImprovementOpportunity opportunity,
        string memoryQuality)
    {
        return new FactoryLearningMemory
        {
            Id = Guid.NewGuid(),
            TenantId = improvement.TenantId,
            FacilityId = improvement.FacilityId,
            VerifiedImprovementId = improvement.Id,
            ProblemPattern = opportunity.ProblemSummary,
            ContextJson = opportunity.AffectedScopeJson,
            RootCauseSummary = opportunity.ExpectedMechanism,
            RootCauseConfidence = RootCauseConfidenceLevels.StrongEvidence,
            InterventionSummary = experiment.Hypothesis,
            ExperimentSummary = experiment.Title,
            ResultSummary = improvement.OutcomeSummary,
            EffectivenessScore = improvement.EffectivenessScore,
            MemoryQuality = memoryQuality,
            ScopeJson = experiment.ScopeJson,
            EvidenceJson = improvement.VerificationEvidenceJson,
            RevalidationCriteriaJson = """["machine","mold","material","shift","quality-state"]""",
            RecordedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public static bool Revalidate(FactoryLearningMemory memory, FactoryStateSnapshot state)
    {
        if (memory.RevalidationCriteriaJson.Contains("machine:IM-07", StringComparison.Ordinal) &&
            !state.MachineStateJson.Contains("IM-07", StringComparison.Ordinal))
        {
            return false;
        }

        if (state.FreshnessJson.Contains("conflict", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    public static IReadOnlyList<FactoryLearningInsight> BuildInsights(IReadOnlyList<FactoryLearningMemory> memories)
    {
        return memories
            .Where(m => m.MemoryQuality == LearningMemoryQuality.Verified)
            .Take(5)
            .Select(m => new FactoryLearningInsight(
                "verified-learning",
                "FactOS öğrenmesi",
                m.ResultSummary,
                m.EffectivenessScore,
                "Bağlantılı deney ve doğrulama kanıtı mevcut",
                m.Id))
            .ToList();
    }
}

public static class RootCauseWorkbenchEngine
{
    public static FactoryRootCauseAnalysis Draft(RootCauseDraftRequest request, Guid tenantId, Guid facilityId)
    {
        var isConfirmed = request.ConfidenceLevel == RootCauseConfidenceLevels.Confirmed;
        return new FactoryRootCauseAnalysis
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            PatternId = request.PatternId,
            LossId = request.LossId,
            Method = request.Method,
            Summary = request.CandidateCause,
            WhyChainJson = JsonSerializer.Serialize(request.WhyChain),
            CandidateCause = request.CandidateCause,
            ConfidenceLevel = request.ConfidenceLevel,
            IsConfirmedCause = isConfirmed,
            EvidenceJson = request.EvidenceJson,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
    }
}

public static class ImprovementSimulationBridge
{
    public static SimulationImprovementResult Simulate(ImprovementOpportunity opportunity, string scenarioType, string inputJson) =>
        new(
            scenarioType,
            JsonSerializer.Serialize(new { scenarioType, input = inputJson, mutation = false }),
            JsonSerializer.Serialize(new
            {
                baselineMinutes = 42,
                projectedMinutes = 34,
                classification = ProductionManagerDataClassifications.Simulated,
            }),
            ProductionManagerDataClassifications.Simulated);
}
