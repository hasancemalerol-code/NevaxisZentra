using NevaxisZentra.Persistence.ScenarioRecovery;
using NevaxisZentra.Persistence.ThroughputIntelligence;

namespace NevaxisZentra.Persistence.FactoryCopilot;

internal static class FactoryCopilotResponseComposer
{
    public static FactoryCopilotStructuredResponse Compose(
        string intent,
        FactoryCopilotToolBundle bundle,
        FactoryCopilotResolvedContext resolved,
        string contextQuality,
        string? modelAnswer = null)
    {
        if (contextQuality is FactoryCopilotContextQualityStates.Insufficient
            or FactoryCopilotContextQualityStates.SourceUnavailable)
        {
            return new FactoryCopilotStructuredResponse(
                Answer: "Bu soruyu yanıtlamak için yeterli operasyonel bağlam yok.",
                WhatUsed: "Context quality gate",
                KeyFacts: [ $"Context quality: {contextQuality}" ],
                Causes: [],
                Impact: null,
                RecommendedNextStep: "İlgili hat, iş veya senaryo bağlamını seçip tekrar deneyin.",
                Uncertainty: "Yanıt üretilmedi; bağlam yetersiz.",
                Evidence: [],
                RelatedActions: BuildContextActions(resolved));
        }

        if (intent == FactoryCopilotIntents.Unsupported)
        {
            return new FactoryCopilotStructuredResponse(
                Answer: "Bu soru Factory Copilot kapsamında desteklenmiyor.",
                WhatUsed: "Intent classifier",
                KeyFacts: [],
                Causes: [],
                Impact: null,
                RecommendedNextStep: "Kayıtlı operasyonel soru kalıplarından birini kullanın.",
                Uncertainty: null,
                Evidence: [],
                RelatedActions: []);
        }

        if (!string.IsNullOrWhiteSpace(modelAnswer))
        {
            return new FactoryCopilotStructuredResponse(
                Answer: modelAnswer,
                WhatUsed: "Deterministic tools + Model Gateway structured synthesis",
                KeyFacts: CollectKeyFacts(bundle, resolved),
                Causes: CollectCauses(bundle),
                Impact: DescribeImpact(bundle),
                RecommendedNextStep: "Önerilen müdahaleyi senaryo çalışma alanında doğrulayın.",
                Uncertainty: "Model-assisted synthesis; iddialar kanıt referanslarıyla sınırlandırıldı.",
                Evidence: CollectEvidence(bundle, resolved),
                RelatedActions: BuildActions(intent, resolved, bundle));
        }

        return intent switch
        {
            FactoryCopilotIntents.CurrentState => ComposeCurrentState(bundle, resolved),
            FactoryCopilotIntents.CauseAnalysis or FactoryCopilotIntents.ConstraintAnalysis =>
                ComposeCauseAnalysis(bundle, resolved),
            FactoryCopilotIntents.TrendComparison or FactoryCopilotIntents.ShiftSummary =>
                ComposeTrendComparison(bundle, resolved),
            FactoryCopilotIntents.OrderRisk => ComposeOrderRisk(bundle, resolved),
            FactoryCopilotIntents.ScheduleExplanation => ComposeScheduleExplanation(bundle, resolved),
            FactoryCopilotIntents.ScenarioExplanation => ComposeScenarioExplanation(bundle, resolved),
            FactoryCopilotIntents.WorkStatus or FactoryCopilotIntents.VerificationStatus =>
                ComposeWorkExplanation(bundle, resolved, intent),
            FactoryCopilotIntents.SimilarCases or FactoryCopilotIntents.KnowledgeLookup
                or FactoryCopilotIntents.MaintenanceRisk or FactoryCopilotIntents.QualityRisk
                or FactoryCopilotIntents.MaterialRisk => ComposeKnowledge(bundle, resolved),
            FactoryCopilotIntents.RecoveryOptions => ComposeRecoveryOptions(bundle, resolved),
            FactoryCopilotIntents.ExecutiveSummary => ComposeExecutiveSummary(bundle, resolved),
            _ => ComposeCauseAnalysis(bundle, resolved),
        };
    }

    private static FactoryCopilotStructuredResponse ComposeCurrentState(
        FactoryCopilotToolBundle bundle,
        FactoryCopilotResolvedContext resolved)
    {
        var overview = bundle.ThroughputOverview;
        var state = bundle.LineState?.ResolvedStateValue ?? "unknown";
        var throughput = overview?.Measurement.GoodUnitsPerHour;
        var target = overview?.Measurement.TargetGoodUnitsPerHour;
        var answer = overview is null
            ? $"{resolved.LineCode} canonical state: {state}."
            : $"{resolved.LineCode} şu an {throughput:0.#}/saat good throughput üretiyor; hedef {target:0.#}/saat.";

        return new FactoryCopilotStructuredResponse(
            Answer: answer,
            WhatUsed: "GetLineState, GetThroughputOverview",
            KeyFacts: CollectKeyFacts(bundle, resolved),
            Causes: [],
            Impact: DescribeImpact(bundle),
            RecommendedNextStep: "Darboğaz veya kayıp ağacı için neden analizi sorabilirsiniz.",
            Uncertainty: overview?.DataQuality is "STALE" or "CONFLICTED" ? overview.DataQuality : null,
            Evidence: CollectEvidence(bundle, resolved),
            RelatedActions: BuildActions(FactoryCopilotIntents.CurrentState, resolved, bundle));
    }

    private static FactoryCopilotStructuredResponse ComposeCauseAnalysis(
        FactoryCopilotToolBundle bundle,
        FactoryCopilotResolvedContext resolved)
    {
        var overview = bundle.ThroughputOverview;
        var gap = overview?.ProductionGap.GapPerHour ?? 0m;
        var primary = overview?.Constraints.FirstOrDefault(c => c.Role == "primary");
        var topLosses = overview?.LossTree.Nodes
            .OrderByDescending(n => n.LostGoodUnitsPerHour)
            .Take(3)
            .Select(n => $"{n.Label}: {n.LostGoodUnitsPerHour:0.#}/saat")
            .ToList() ?? [];

        var answer = overview is null
            ? $"{resolved.LineCode} için throughput analizi yüklenemedi."
            : $"{resolved.LineCode} hedefin {gap:0.#}/saat altında. Birincil darboğaz {primary?.StationCode ?? "bilinmiyor"} ({primary?.ConstraintType ?? "n/a"}).";

        return new FactoryCopilotStructuredResponse(
            Answer: answer,
            WhatUsed: "MP2 throughput, constraint, loss tree",
            KeyFacts: CollectKeyFacts(bundle, resolved),
            Causes: topLosses,
            Impact: DescribeImpact(bundle),
            RecommendedNextStep: "Ne yapabiliriz? veya +1 operatör simülasyonu sorabilirsiniz.",
            Uncertainty: overview?.LossTree.ReconciliationQuality is "LOW" ? "Loss attribution partially reconciled." : null,
            Evidence: CollectEvidence(bundle, resolved),
            RelatedActions: BuildActions(FactoryCopilotIntents.CauseAnalysis, resolved, bundle));
    }

    private static FactoryCopilotStructuredResponse ComposeTrendComparison(
        FactoryCopilotToolBundle bundle,
        FactoryCopilotResolvedContext resolved) =>
        new(
            Answer: string.Join(" ", bundle.ShiftSignals),
            WhatUsed: "Current vs prior shift throughput windows",
            KeyFacts: bundle.ShiftSignals.ToList(),
            Causes: [],
            Impact: null,
            RecommendedNextStep: "Değişimin istasyon düzeyinde nedenini darboğaz analizi ile inceleyin.",
            Uncertainty: null,
            Evidence: CollectEvidence(bundle, resolved),
            RelatedActions: BuildActions(FactoryCopilotIntents.TrendComparison, resolved, bundle));

    private static FactoryCopilotStructuredResponse ComposeOrderRisk(
        FactoryCopilotToolBundle bundle,
        FactoryCopilotResolvedContext resolved)
    {
        var orderRef = resolved.OrderReference ?? "Order";
        var gap = bundle.ThroughputOverview?.ProductionGap.GapPerHour;
        return new FactoryCopilotStructuredResponse(
            Answer: $"{orderRef} riski; hat {resolved.LineCode} throughput gap {gap:0.#}/saat ve kayıtlı recovery seçenekleri mevcut.",
            WhatUsed: "Throughput overview + recovery registry",
            KeyFacts: CollectKeyFacts(bundle, resolved),
            Causes: bundle.RecoveryOptions.ToList(),
            Impact: DescribeImpact(bundle),
            RecommendedNextStep: "Plan ve senaryo karşılaştırmasını açın.",
            Uncertainty: resolved.OrderReference is null ? "Sipariş referansı bağlamdan çıkarılamadı." : null,
            Evidence: CollectEvidence(bundle, resolved),
            RelatedActions: BuildActions(FactoryCopilotIntents.OrderRisk, resolved, bundle));
    }

    private static FactoryCopilotStructuredResponse ComposeScheduleExplanation(
        FactoryCopilotToolBundle bundle,
        FactoryCopilotResolvedContext resolved) =>
        new(
            Answer: "Plan sıralaması OTIF, changeover, kaynak uygunluğu ve malzeme kısıtlarına göre belirlenir; Copilot yalnızca kayıtlı plan/senaryo gerçeklerini açıklar.",
            WhatUsed: "Recovery/scenario registry",
            KeyFacts: bundle.RecoveryOptions.ToList(),
            Causes: [],
            Impact: null,
            RecommendedNextStep: "Canlı plan detayını PLAN çalışma alanında açın.",
            Uncertainty: null,
            Evidence: CollectEvidence(bundle, resolved),
            RelatedActions:
            [
                new FactoryCopilotActionLink("Planı aç", "/planlama", "navigate"),
            ]);

    private static FactoryCopilotStructuredResponse ComposeScenarioExplanation(
        FactoryCopilotToolBundle bundle,
        FactoryCopilotResolvedContext resolved)
    {
        var rows = bundle.ScenarioComparison ?? [];
        var recommended = bundle.ScenarioWorkspace?.SelectedOptionLabel;
        var facts = rows.Select(row =>
                $"{row.ScenarioCode}: +{row.DeltaGoodThroughputPerHour:0.#}/saat, risk {row.OperationalRiskScore:0.##}, eligibility {row.Eligibility}")
            .ToList();

        var answer = recommended is not null
            ? $"Seçilen seçenek {recommended}; karşılaştırma matrisi trade-off ve kısıt gerçeklerine dayanır."
            : facts.FirstOrDefault() ?? "Senaryo karşılaştırma verisi bulunamadı.";

        return new FactoryCopilotStructuredResponse(
            Answer: answer,
            WhatUsed: "Scenario recovery comparison matrix",
            KeyFacts: facts,
            Causes: [],
            Impact: DescribeImpact(bundle),
            RecommendedNextStep: "Senaryo çalışma alanında A/B/C metriklerini inceleyin.",
            Uncertainty: null,
            Evidence: CollectEvidence(bundle, resolved),
            RelatedActions: BuildActions(FactoryCopilotIntents.ScenarioExplanation, resolved, bundle));
    }

    private static FactoryCopilotStructuredResponse ComposeWorkExplanation(
        FactoryCopilotToolBundle bundle,
        FactoryCopilotResolvedContext resolved,
        string intent)
    {
        var work = bundle.WorkDetail;
        if (work is null)
        {
            return new FactoryCopilotStructuredResponse(
                Answer: "İş detayı yüklenemedi; iş bağlamı seçili değil.",
                WhatUsed: "GetWorkDetail",
                KeyFacts: [],
                Causes: [],
                Impact: null,
                RecommendedNextStep: "İŞLER alanından bir iş seçin.",
                Uncertainty: "Work context missing",
                Evidence: [],
                RelatedActions: [new FactoryCopilotActionLink("İşleri aç", "/isler", "navigate")]);
        }

        var answer = intent == FactoryCopilotIntents.VerificationStatus
            ? work.Verification?.OutcomeLabel ?? work.Summary.VerificationState
            : $"{work.Summary.DisplayState}: {work.NextStepLabel}";

        return new FactoryCopilotStructuredResponse(
            Answer: answer,
            WhatUsed: "ULTRA E work projection",
            KeyFacts:
            [
                work.Summary.Title,
                work.Summary.DisplayState,
                work.NextStepLabel,
            ],
            Causes: work.Blocker is not null ? [work.Blocker.Label] : [],
            Impact: work.ImpactSummary,
            RecommendedNextStep: work.NextStepLabel,
            Uncertainty: work.Blocker?.Detail,
            Evidence: work.Evidence
                .Select(item => new FactoryCopilotEvidenceRef(item.Kind, item.Label, item.Label, item.Detail))
                .ToList(),
            RelatedActions: BuildActions(intent, resolved, bundle));
    }

    private static FactoryCopilotStructuredResponse ComposeKnowledge(
        FactoryCopilotToolBundle bundle,
        FactoryCopilotResolvedContext resolved)
    {
        var records = bundle.VerifiedKnowledge ?? [];
        if (records.Count == 0)
        {
            return new FactoryCopilotStructuredResponse(
                Answer: "Benzer doğrulanmış vaka bulunamadı.",
                WhatUsed: "Verified factory knowledge",
                KeyFacts: [],
                Causes: [],
                Impact: null,
                RecommendedNextStep: "Doğrulanmış sonuçlar senaryo kapanışından knowledge'a promote edilir.",
                Uncertainty: null,
                Evidence: [],
                RelatedActions: []);
        }

        var top = records[0];
        return new FactoryCopilotStructuredResponse(
            Answer: $"{top.ProblemSymptom} → {top.RootCause}. Doğrulanmış sonuç: {top.VerifiedOutcome}",
            WhatUsed: "Factory Knowledge retrieval",
            KeyFacts: records.Take(3).Select(r => r.DecisionSummary).ToList(),
            Causes: [top.RootCause],
            Impact: top.VerifiedOutcome,
            RecommendedNextStep: "Geçmiş aksiyonun şimdiki bağlama uygunluğunu senaryo ile doğrulayın.",
            Uncertainty: "Historical action applicability is not automatic.",
            Evidence: records.Take(3)
                .Select(r => new FactoryCopilotEvidenceRef("knowledge", r.Id.ToString(), r.ProblemSymptom, r.RootCause))
                .ToList(),
            RelatedActions: BuildActions(FactoryCopilotIntents.SimilarCases, resolved, bundle));
    }

    private static FactoryCopilotStructuredResponse ComposeRecoveryOptions(
        FactoryCopilotToolBundle bundle,
        FactoryCopilotResolvedContext resolved)
    {
        if (bundle.SimulationSnapshot is not null)
        {
            var sim = bundle.SimulationSnapshot;
            return new FactoryCopilotStructuredResponse(
                Answer:
                $"+1 operatör simülasyonu: {sim.BaselineGoodThroughputPerHour:0.#} → {sim.PredictedGoodThroughputPerHour:0.#}/saat (Δ {sim.DeltaGoodThroughputPerHour:+#.#;-#.#;0}).",
                WhatUsed: "RunScenarioSimulation",
                KeyFacts:
                [
                    $"Constraint before: {sim.ConstraintBefore?.StationCode ?? "n/a"}",
                    $"Constraint after: {sim.ConstraintAfter?.StationCode ?? "n/a"}",
                    $"Risk score: {sim.OperationalRiskScore:0.##}",
                ],
                Causes: sim.LimitingFactors.ToList(),
                Impact: DescribeImpact(bundle),
                RecommendedNextStep: "Senaryo çalışma alanında seçeneği karşılaştırın.",
                Uncertainty: sim.DataQuality,
                Evidence: CollectEvidence(bundle, resolved),
                RelatedActions: BuildActions(FactoryCopilotIntents.RecoveryOptions, resolved, bundle));
        }

        return new FactoryCopilotStructuredResponse(
            Answer: bundle.RecoveryOptions.Count > 0
                ? "Kayıtlı recovery seçenekleri: " + string.Join("; ", bundle.RecoveryOptions.Take(3))
                : "Kayıtlı recovery seçeneği bulunamadı.",
            WhatUsed: "Scenario comparison + recovery registry",
            KeyFacts: bundle.RecoveryOptions.ToList(),
            Causes: [],
            Impact: DescribeImpact(bundle),
            RecommendedNextStep: "Senaryo Kurtarma çalışma alanını açın.",
            Uncertainty: null,
            Evidence: CollectEvidence(bundle, resolved),
            RelatedActions: BuildActions(FactoryCopilotIntents.RecoveryOptions, resolved, bundle));
    }

    private static FactoryCopilotStructuredResponse ComposeExecutiveSummary(
        FactoryCopilotToolBundle bundle,
        FactoryCopilotResolvedContext resolved) =>
        new(
            Answer: string.Join(" · ", bundle.ExecutiveSignals),
            WhatUsed: "Executive signals + throughput + open work scan",
            KeyFacts: bundle.ExecutiveSignals.ToList(),
            Causes: [],
            Impact: DescribeImpact(bundle),
            RecommendedNextStep: "Kritik iş veya hat detayına gidin.",
            Uncertainty: null,
            Evidence: CollectEvidence(bundle, resolved),
            RelatedActions: BuildActions(FactoryCopilotIntents.ExecutiveSummary, resolved, bundle));

    private static List<string> CollectKeyFacts(
        FactoryCopilotToolBundle bundle,
        FactoryCopilotResolvedContext resolved)
    {
        var facts = new List<string> { $"Line: {resolved.LineCode}" };
        if (bundle.ThroughputOverview is { } overview)
        {
            facts.Add($"Throughput: {overview.Measurement.GoodUnitsPerHour:0.#}/saat");
            facts.Add($"Target: {overview.Measurement.TargetGoodUnitsPerHour:0.#}/saat");
            facts.Add($"Data quality: {overview.DataQuality}");
        }

        return facts;
    }

    private static List<string> CollectCauses(FactoryCopilotToolBundle bundle) =>
        bundle.ThroughputOverview?.LossTree.Nodes
            .OrderByDescending(n => n.LostGoodUnitsPerHour)
            .Take(5)
            .Select(n => n.Label)
            .ToList() ?? [];

    private static string? DescribeImpact(FactoryCopilotToolBundle bundle)
    {
        var gap = bundle.ThroughputOverview?.ProductionGap.GapPerHour;
        return gap is > 0 ? $"Production gap {gap:0.#} good units/hour" : null;
    }

    private static List<FactoryCopilotEvidenceRef> CollectEvidence(
        FactoryCopilotToolBundle bundle,
        FactoryCopilotResolvedContext resolved)
    {
        var evidence = new List<FactoryCopilotEvidenceRef>
        {
            new("line-state", resolved.LineId.ToString(), resolved.LineCode, bundle.LineState?.ResolutionStatus),
        };

        if (bundle.ThroughputOverview is { } overview)
        {
            evidence.Add(new(
                "throughput-measurement",
                overview.LineId.ToString(),
                overview.LineCode,
                $"{overview.Measurement.GoodUnitsPerHour:0.#}/saat"));
            var primary = overview.Constraints.FirstOrDefault(c => c.Role == "primary");
            if (primary?.StationCode is not null)
            {
                evidence.Add(new("constraint", primary.StationCode, primary.StationCode, primary.ConstraintType));
            }
        }

        if (bundle.ScenarioWorkspace is { } workspace)
        {
            evidence.Add(new("scenario-run", workspace.RunId.ToString(), workspace.LineCode, workspace.CurrentPhase));
        }

        if (bundle.WorkDetail is { } work)
        {
            evidence.Add(new("work-case", work.Summary.Id.ToString(), work.Summary.Title, work.Summary.DisplayState));
        }

        return evidence;
    }

    private static List<FactoryCopilotActionLink> BuildActions(
        string intent,
        FactoryCopilotResolvedContext resolved,
        FactoryCopilotToolBundle bundle)
    {
        var actions = new List<FactoryCopilotActionLink>
        {
            new("Fabrikayı aç", $"/fabrika?hat={resolved.LineId}", "navigate"),
        };

        if (intent is FactoryCopilotIntents.RecoveryOptions or FactoryCopilotIntents.ScenarioExplanation)
        {
            actions.Add(new("Senaryo Kurtarma", "/senaryo-kurtarma", "navigate"));
        }

        if (intent is FactoryCopilotIntents.WorkStatus or FactoryCopilotIntents.VerificationStatus)
        {
            actions.Add(new("İşleri aç", "/isler", "navigate"));
        }

        if (bundle.ScenarioWorkspace?.RunId is Guid runId)
        {
            actions.Add(new("Senaryoyu aç", $"/senaryo-kurtarma?run={runId}", "navigate"));
        }

        return actions;
    }

    private static IReadOnlyList<FactoryCopilotActionLink> BuildContextActions(FactoryCopilotResolvedContext resolved) =>
    [
        new("Fabrika bağlamı", $"/fabrika?hat={resolved.LineId}", "navigate"),
    ];
}

internal static class FactoryCopilotHallucinationGuard
{
    private static readonly string[] InjectionPatterns =
    [
        "ignore previous",
        "system policy",
        "override tenant",
        "execute now",
        "bypass approval",
    ];

    public static (bool Accepted, string? RejectionReason, FactoryCopilotStructuredResponse Response) Validate(
        FactoryCopilotStructuredResponse response,
        FactoryCopilotToolBundle bundle,
        string queryText)
    {
        var normalizedQuery = queryText.ToLowerInvariant();
        if (InjectionPatterns.Any(pattern => normalizedQuery.Contains(pattern, StringComparison.Ordinal)))
        {
            return (false, "prompt-injection-detected", response with
            {
                Answer = "Güvenlik politikası gereği bu istek işlenmedi.",
                Uncertainty = "Prompt injection pattern detected.",
            });
        }

        var unknownTokens = ExtractCandidateEntityTokens(response.Answer)
            .Where(token => !IsKnownToken(token, bundle.KnownEntityTokens))
            .Where(token => token.Length >= 3)
            .Take(3)
            .ToList();

        if (unknownTokens.Count > 0 && response.Evidence.Count == 0)
        {
            return (false, "fabricated-entity", response with
            {
                Answer = "Yanıt kanıtlanamayan operasyonel iddialar içerdiği için düşürüldü.",
                Uncertainty = $"Unknown references: {string.Join(", ", unknownTokens)}",
            });
        }

        if (response.Evidence.Count == 0 && response.KeyFacts.Count > 2)
        {
            return (false, "missing-evidence", response with
            {
                Uncertainty = "Substantive claims require evidence references.",
            });
        }

        return (true, null, response);
    }

    private static IEnumerable<string> ExtractCandidateEntityTokens(string text) =>
        text.Split([' ', ',', '.', ';', ':', '(', ')', '[', ']', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token.Any(char.IsDigit) || token.Contains('-', StringComparison.Ordinal));

    private static bool IsKnownToken(string token, IReadOnlyList<string> knownTokens) =>
        knownTokens.Any(known => token.Contains(known, StringComparison.OrdinalIgnoreCase)
                                 || known.Contains(token, StringComparison.OrdinalIgnoreCase));
}
