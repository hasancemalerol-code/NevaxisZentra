namespace NevaxisZentra.Persistence.FactoryCopilot;

public static class FactoryCopilotIntentClassifier
{
    public sealed record IntentClassification(
        string Intent,
        string RoutingClass,
        bool RequiresModel,
        IReadOnlyList<string> RequiredTools);

    public static IntentClassification Classify(string queryText, FactoryCopilotContextInput context)
    {
        var normalized = Fold(queryText);

        // Calendar-aware machine KPI questions (availability, unplanned downtime, per scheduled hour, ...) are answered
        // from the machine aggregate. They must not be captured by the single-keyword rules below ("scheduled" is not
        // a schedule-explanation request, "caused by" is not a cause-analysis request).
        if (MatchesAny(normalized, MachineKpiTerms))
        {
            return ClassifyMachineKpiQuestion();
        }

        if (MatchesAny(normalized,
                "ne durumda", "şu an", "current state", "durumu nedir", "çalışıyor mu"))
        {
            return ClassifyIntent(FactoryCopilotIntents.CurrentState, FactoryCopilotRoutingClasses.Deterministic,
                [FactoryCopilotToolIds.GetLineState, FactoryCopilotToolIds.GetThroughputOverview]);
        }

        if (MatchesAny(normalized,
                "neden hedef", "neden yavaş", "neden düştü", "throughput", "hedefin alt",
                "darboğaz nerede", "cause", "why slow", "why below"))
        {
            return ClassifyIntent(FactoryCopilotIntents.CauseAnalysis, FactoryCopilotRoutingClasses.Analytic,
                [
                    FactoryCopilotToolIds.GetThroughputOverview,
                    FactoryCopilotToolIds.GetCurrentConstraint,
                    FactoryCopilotToolIds.GetLossTree,
                ]);
        }

        if (MatchesAny(normalized,
                "son vardiya", "önceki vardiya", "karşılaştır", "trend", "bugün dün",
                "before after", "compare shift"))
        {
            return ClassifyIntent(FactoryCopilotIntents.TrendComparison, FactoryCopilotRoutingClasses.Analytic,
                [FactoryCopilotToolIds.GetThroughputOverview, FactoryCopilotToolIds.GetShiftContext]);
        }

        if (MatchesAny(normalized,
                "üretim, bakım", "bakım, kalite", "malzeme verisini birlikte",
                "cross-domain", "birlikte değerlendir", "recovery planını özetle"))
        {
            return ClassifyIntent(
                FactoryCopilotIntents.CauseAnalysis,
                FactoryCopilotRoutingClasses.ModelRequired,
                [
                    FactoryCopilotToolIds.GetThroughputOverview,
                    FactoryCopilotToolIds.GetVerifiedKnowledge,
                    FactoryCopilotToolIds.GetShiftContext,
                ],
                requiresModel: true);
        }

        if (MatchesAny(normalized, "neden önce plan", "schedule", "planlandı"))
        {
            return ClassifyIntent(FactoryCopilotIntents.ScheduleExplanation, FactoryCopilotRoutingClasses.Deterministic,
                [FactoryCopilotToolIds.GetRecoveryOptions]);
        }

        if (MatchesAny(normalized, "sipariş", "order risk", "teslimat risk") || OrderNumberReference.IsMatch(normalized))
        {
            return ClassifyIntent(FactoryCopilotIntents.OrderRisk, FactoryCopilotRoutingClasses.Analytic,
                [FactoryCopilotToolIds.GetThroughputOverview, FactoryCopilotToolIds.GetRecoveryOptions]);
        }

        if (MatchesAny(normalized,
                "optimizasyon", "optimization", "neden önerdi", "ne kazandır", "uygulanmadı", "sürekli optimizasyon"))
        {
            return ClassifyIntent(FactoryCopilotIntents.OptimizationExplanation, FactoryCopilotRoutingClasses.Deterministic,
                [FactoryCopilotToolIds.GetOptimizationRun]);
        }

        if (MatchesAny(normalized,
                "option b", "option a", "seçenek neden", "scenario", "senaryo",
                "daha iyi", "why option"))
        {
            return ClassifyIntent(FactoryCopilotIntents.ScenarioExplanation, FactoryCopilotRoutingClasses.Deterministic,
                [FactoryCopilotToolIds.GetScenarioComparison]);
        }

        if (MatchesAny(normalized,
                "iş neden", "kapanmadı", "ne bekliyor", "onay", "doğrulama ne durumda",
                "kim ilgilen", "work status", "iş durumu"))
        {
            return ClassifyIntent(FactoryCopilotIntents.WorkStatus, FactoryCopilotRoutingClasses.Deterministic,
                [FactoryCopilotToolIds.GetWorkDetail]);
        }

        if (MatchesAny(normalized,
                "işe yaradı", "gerçekten", "doğrulandı", "verified", "verification", "sonuç"))
        {
            return ClassifyIntent(FactoryCopilotIntents.VerificationStatus, FactoryCopilotRoutingClasses.Deterministic,
                [FactoryCopilotToolIds.GetVerificationResult, FactoryCopilotToolIds.GetWorkDetail]);
        }

        if (MatchesAny(normalized,
                "benzer", "doğrulanmış vaka", "similar case", "geçmişte ne yapılmış", "knowledge"))
        {
            return ClassifyIntent(FactoryCopilotIntents.SimilarCases, FactoryCopilotRoutingClasses.Retrieval,
                [FactoryCopilotToolIds.GetVerifiedKnowledge]);
        }

        if (MatchesAny(normalized, "ne yapabiliriz", "recovery", "seçenek", "müdahale", "what can we do"))
        {
            return ClassifyIntent(FactoryCopilotIntents.RecoveryOptions, FactoryCopilotRoutingClasses.Simulation,
                [FactoryCopilotToolIds.GetRecoveryOptions, FactoryCopilotToolIds.GetScenarioComparison]);
        }

        if (MatchesAny(normalized,
                "+1 operatör", "operatör koy", "simüle", "simulation", "what if", "ne olur"))
        {
            return ClassifyIntent(FactoryCopilotIntents.RecoveryOptions, FactoryCopilotRoutingClasses.Simulation,
                [FactoryCopilotToolIds.RunScenarioSimulation]);
        }

        if (MatchesAny(normalized,
                "kritik", "what matters", "özet", "executive", "en önemli", "şu an fabrika"))
        {
            return ClassifyIntent(FactoryCopilotIntents.ExecutiveSummary, FactoryCopilotRoutingClasses.Analytic,
                [FactoryCopilotToolIds.GetExecutiveSignals, FactoryCopilotToolIds.GetThroughputOverview]);
        }

        if (MatchesAny(normalized, "vardiya", "devir", "shift summary", "handover"))
        {
            return ClassifyIntent(FactoryCopilotIntents.ShiftSummary, FactoryCopilotRoutingClasses.Analytic,
                [FactoryCopilotToolIds.GetShiftContext]);
        }

        if (MatchesAny(normalized, "bakım", "maintenance"))
        {
            return ClassifyIntent(FactoryCopilotIntents.MaintenanceRisk, FactoryCopilotRoutingClasses.Retrieval,
                [FactoryCopilotToolIds.GetVerifiedKnowledge]);
        }

        if (MatchesAny(normalized, "kalite", "quality", "ncr", "hold"))
        {
            return ClassifyIntent(FactoryCopilotIntents.QualityRisk, FactoryCopilotRoutingClasses.Retrieval,
                [FactoryCopilotToolIds.GetVerifiedKnowledge]);
        }

        if (MatchesAny(normalized, "malzeme", "material", "shortage"))
        {
            return ClassifyIntent(FactoryCopilotIntents.MaterialRisk, FactoryCopilotRoutingClasses.Retrieval,
                [FactoryCopilotToolIds.GetVerifiedKnowledge]);
        }

        if (MatchesAny(normalized, "darboğaz değiş", "constraint"))
        {
            return ClassifyIntent(FactoryCopilotIntents.ConstraintAnalysis, FactoryCopilotRoutingClasses.Analytic,
                [FactoryCopilotToolIds.GetCurrentConstraint, FactoryCopilotToolIds.GetThroughputOverview]);
        }

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return ClassifyIntent(FactoryCopilotIntents.Unsupported, FactoryCopilotRoutingClasses.Unsupported, []);
        }

        // A factory question that matches no registered pattern is answered by the model from the retrieved facts only.
        if (MatchesAny(normalized, FactoryDomainTerms))
        {
            return ClassifyMachineKpiQuestion();
        }

        return ClassifyIntent(FactoryCopilotIntents.Unsupported, FactoryCopilotRoutingClasses.Unsupported, []);
    }

    public static IReadOnlyList<string> SuggestQuestions(FactoryCopilotContextInput context) =>
        context.EntryPoint switch
        {
            FactoryCopilotEntryPoints.Fabrika or FactoryCopilotEntryPoints.Line =>
            [
                "Bu hat neden hedefin altında?",
                "Darboğaz nerede?",
                "Son vardiyada ne değişti?",
                "Ne simüle edebiliriz?",
                "Benzer doğrulanmış vaka var mı?",
            ],
            FactoryCopilotEntryPoints.Plan =>
            [
                "Bu plan hangi kısıtlarla şekillendi?",
                "Hangi sipariş riskte?",
                "Planı bozacak bakım veya malzeme riski var mı?",
            ],
            FactoryCopilotEntryPoints.Isler =>
            [
                "Bu iş neden kapanmadı?",
                "Ne bekliyor?",
                "Doğrulama ne durumda?",
                "Benzer iş var mı?",
            ],
            FactoryCopilotEntryPoints.Scenario =>
            [
                "Neden bu seçenek önerildi?",
                "A/B/C arasındaki fark nedir?",
                "Onay neden gerekli?",
                "Tahmin ile gerçek sonuç ne?",
            ],
            FactoryCopilotEntryPoints.Bugun =>
            [
                "Şu an fabrikanın en kritik üç problemi nedir?",
                "Hangi onaylar bekliyor?",
                "Hangi işler engelli?",
            ],
            _ =>
            [
                "Bu bağlamda ne sorabilirim?",
                "Kritik riskler neler?",
            ],
        };

    private static IntentClassification ClassifyIntent(
        string intent,
        string routingClass,
        IReadOnlyList<string> tools,
        bool requiresModel = false)
    {
        if (intent == FactoryCopilotIntents.Unsupported)
        {
            return new IntentClassification(intent, FactoryCopilotRoutingClasses.Unsupported, false, tools);
        }

        return new IntentClassification(intent, routingClass, requiresModel, tools);
    }

    private static readonly string[] MachineKpiTerms =
    [
        "availability", "kullanılabilirlik", "unplanned", "planlanmamış", "scheduled production", "per scheduled hour",
        "planned break", "planlı mola", "working day", "iş günü", "outside-shift", "outside shift", "overtime",
        "fazla mesai", "stop minutes", "stop time", "downtime", "duruş", "machine failure", "makine arızası",
    ];

    private static IntentClassification ClassifyMachineKpiQuestion() =>
        ClassifyIntent(
            FactoryCopilotIntents.GeneralQuestion,
            FactoryCopilotRoutingClasses.ModelRequired,
            [
                FactoryCopilotToolIds.GetLineState,
                FactoryCopilotToolIds.GetThroughputOverview,
                FactoryCopilotToolIds.GetCurrentConstraint,
                FactoryCopilotToolIds.GetLossTree,
                FactoryCopilotToolIds.GetShiftContext,
                FactoryCopilotToolIds.GetExecutiveSignals,
                FactoryCopilotToolIds.GetMachinePerformance,
            ],
            requiresModel: true);

    private static readonly string[] FactoryDomainTerms =
    [
        "performans", "performance", "makine", "machine", "duruş", "downtime", "arıza", "breakdown",
        "verim", "efficiency", "oee", "üretim", "production", "hattı", "hatlar", "hattın", "line", "fire", "scrap",
        "stop", "durdu", "cycle", "çevrim", "hurda", "istasyon", "station", "operator", "operatör",
    ];

    private static readonly System.Text.RegularExpressions.Regex OrderNumberReference = new(
        @"\border\s*#?\d+",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    /// Lower-cases for matching. Turkish dotted and dotless i fold to one letter, so "İŞ NEDEN" and "ÇALIŞIYOR MU" in
    /// capitals match the same patterns as their lower-case forms.
    /// </summary>
    internal static string Fold(string value) =>
        value.Trim().ToLowerInvariant().Replace("\u0307", string.Empty, StringComparison.Ordinal).Replace('ı', 'i');

    /// <summary>
    /// A pattern matches at the start of a word, so Turkish suffixes still match ("sipariş" in "siparişler") while a
    /// pattern inside another word does not ("ncr" in "increase", "hold" in "threshold", "cause" in "because").
    /// </summary>
    internal static bool MatchesAny(string text, params string[] patterns)
    {
        foreach (var raw in patterns)
        {
            var pattern = Fold(raw);
            var index = 0;
            while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
            {
                if (index == 0 || !char.IsLetterOrDigit(text[index - 1]))
                {
                    return true;
                }

                index++;
            }
        }

        return false;
    }
}
