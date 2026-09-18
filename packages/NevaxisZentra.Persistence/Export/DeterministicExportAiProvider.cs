using System.Text.Json;

namespace NevaxisZentra.Persistence.Export;

/// <summary>
/// Deterministic development/test AI provider.
/// Output is derived from structured context only — NOT real AI intelligence.
/// </summary>
public sealed class DeterministicExportAiProvider : IExportAiProvider
{
    private const decimal DemoCreditsPerRun = 12m;

    public string ProviderName => ExportAiProviders.DeterministicDevelopment;
    public string ModelName => ExportAiProviders.DeterministicDevelopmentModel;
    public bool IsDevelopmentProvider => true;

    public Task<ExportAiProviderOutput> AnalyzeExportCaseAsync(
        ExportAiAnalysisRunType runType,
        string promptVersion,
        ExportAiContextSnapshotModel context,
        CancellationToken cancellationToken = default) =>
        runType switch
        {
            ExportAiAnalysisRunType.ComplianceExplanation => ExplainComplianceAsync(promptVersion, context, cancellationToken),
            ExportAiAnalysisRunType.ReadinessAnalysis => Task.FromResult(BuildReadinessOutput(context)),
            ExportAiAnalysisRunType.MissingDataAnalysis => AnalyzeMissingDataAsync(promptVersion, context, cancellationToken),
            ExportAiAnalysisRunType.NextActions => SuggestNextActionsAsync(promptVersion, context, cancellationToken),
            ExportAiAnalysisRunType.DocumentGapAnalysis => Task.FromResult(BuildDocumentGapOutput(context)),
            ExportAiAnalysisRunType.CrossModuleRiskAnalysis => Task.FromResult(BuildCrossModuleOutput(context)),
            ExportAiAnalysisRunType.CountryResearchAssist => SummarizeCountryResearchAsync(promptVersion, context, cancellationToken),
            _ => Task.FromResult(BuildCaseOverview(context)),
        };

    public Task<ExportAiProviderOutput> ExplainComplianceAsync(
        string promptVersion,
        ExportAiContextSnapshotModel context,
        CancellationToken cancellationToken = default)
    {
        var findings = new List<ExportAiFindingDraft>();
        var recommendations = new List<ExportAiRecommendationDraft>();

        if (context.Compliance is null || context.Compliance.Requirements.Count == 0)
        {
            findings.Add(new ExportAiFindingDraft(
                ExportAiFindingCategory.Compliance,
                "Doğrulanmış kaynak bulunamadı",
                "Doğrulanmış kaynak bulunmadığı için kesin değerlendirme yapılamıyor.",
                ExportAiFindingSeverity.Critical,
                0.3m,
                ExportAiSourceType.SystemDeterministic,
                [new ExportAiSourceReference("system", "compliance-engine", "Compliance engine", null)],
                null, null, null,
                true, true));
        }
        else
        {
            foreach (var req in context.Compliance.Requirements.Where(r => r.Blocking && r.Status is not "satisfied"))
            {
                var sourceType = req.SourceReference.StartsWith("dev:", StringComparison.OrdinalIgnoreCase)
                    || req.SourceReference.StartsWith("system:", StringComparison.OrdinalIgnoreCase)
                    ? ExportAiSourceType.VerifiedRule
                    : ExportAiSourceType.VerifiedRule;

                findings.Add(new ExportAiFindingDraft(
                    ExportAiFindingCategory.Compliance,
                    req.Title,
                    BuildComplianceExplanation(context, req),
                    ExportAiFindingSeverity.Critical,
                    0.85m,
                    sourceType,
                    BuildRequirementSources(context, req),
                    req.Id, null, null,
                    true, true));

                recommendations.Add(new ExportAiRecommendationDraft(
                    $"Uygunluk: {req.RequirementCode}",
                    $"Engelleyici gereksinim '{req.Title}' çözülmeli.",
                    ExportAiRecommendationPriority.High,
                    0.85m,
                    $"Kural {req.RequirementCode} engelleyici durumda.",
                    ExportAiProposedActionType.OpenComplianceRequirement,
                    JsonSerializer.Serialize(new { requirementId = req.Id, requirementCode = req.RequirementCode })));
            }
        }

        var summary = context.Compliance?.Result switch
        {
            "blocked" => "İhracat dosyası doğrulanmış kural seti nedeniyle bloke durumda.",
            "unable-to-evaluate" => "Doğrulanmış kural seti olmadığı için uygunluk değerlendirmesi yapılamıyor.",
            "ready" => "Uygunluk değerlendirmesi hazır durumda.",
            _ => "Uygunluk durumu analiz edildi.",
        };

        return Task.FromResult(Wrap(summary, findings, recommendations));
    }

    public Task<ExportAiProviderOutput> AnalyzeMissingDataAsync(
        string promptVersion,
        ExportAiContextSnapshotModel context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(BuildMissingDataOutput(context));

    public Task<ExportAiProviderOutput> SummarizeCountryResearchAsync(
        string promptVersion,
        ExportAiContextSnapshotModel context,
        CancellationToken cancellationToken = default)
    {
        var findings = new List<ExportAiFindingDraft>();
        var recommendations = new List<ExportAiRecommendationDraft>();

        if (context.Research is null)
        {
            return Task.FromResult(Wrap(
                "Ülke araştırma bağlamı bulunamadı.",
                findings,
                recommendations));
        }

        if (context.Research.UnverifiedFindingCount > 0)
        {
            findings.Add(new ExportAiFindingDraft(
                ExportAiFindingCategory.CountryResearch,
                "Doğrulanmamış bulgular",
                $"{context.Research.UnverifiedFindingCount} bulgu henüz doğrulanmadı.",
                ExportAiFindingSeverity.Warning,
                0.75m,
                ExportAiSourceType.OperationalData,
                [new ExportAiSourceReference("research", context.Research.ResearchCaseId?.ToString() ?? "", context.Research.ResearchNumber ?? "", null)],
                null, null, null,
                true, true));
        }

        recommendations.Add(new ExportAiRecommendationDraft(
            "Broker soruları taslağı",
            "Resmi kaynaklardan doğrulama için broker soruları hazırlanabilir.",
            ExportAiRecommendationPriority.Medium,
            0.7m,
            "Doğrulanmamış bulgular mevcut.",
            ExportAiProposedActionType.DraftBrokerQuestion,
            JsonSerializer.Serialize(new { researchCaseId = context.Research.ResearchCaseId })));

        return Task.FromResult(Wrap(
            $"Araştırma {context.Research.ResearchNumber}: {context.Research.FindingCount} bulgu, {context.Research.UnverifiedFindingCount} doğrulanmamış.",
            findings,
            recommendations));
    }

    public Task<ExportAiProviderOutput> SuggestNextActionsAsync(
        string promptVersion,
        ExportAiContextSnapshotModel context,
        CancellationToken cancellationToken = default)
    {
        var recommendations = new List<ExportAiRecommendationDraft>();
        var rank = 1;

        var blockingReq = context.Compliance?.Requirements
            .FirstOrDefault(r => r.Blocking && r.Status is not "satisfied");
        if (blockingReq is not null)
        {
            recommendations.Add(MakeAction(
                rank++, "Menşe/doküman kanıtını doğrula", blockingReq.RequirementCode,
                ExportAiProposedActionType.OpenComplianceRequirement,
                new { requirementId = blockingReq.Id }));
        }

        if (context.Broker?.OpenCorrectionCount > 0)
        {
            recommendations.Add(MakeAction(
                rank++, "Broker düzeltmesini çöz", "broker-correction",
                ExportAiProposedActionType.OpenBrokerCorrection,
                new { workspaceId = context.Broker.WorkspaceId }));
        }

        foreach (var risk in context.CrossModuleRisks.Where(r => r.Blocking).Take(2))
        {
            recommendations.Add(new ExportAiRecommendationDraft(
                $"Operasyonel risk: {risk.Type}",
                risk.Description,
                ExportAiRecommendationPriority.High,
                0.8m,
                risk.Description,
                ExportAiProposedActionType.RequestHumanReview,
                JsonSerializer.Serialize(new { riskType = risk.Type.ToString(), sourceModule = risk.SourceModule.ToString() })));
        }

        if (recommendations.Count == 0 && context.ExportCase?.ReadinessStatus != "ready")
        {
            recommendations.Add(MakeAction(
                1, "İhracat hazırlığını gözden geçir", "readiness",
                ExportAiProposedActionType.RequestHumanReview,
                new { exportCaseId = context.ExportCase?.ExportCaseId }));
        }

        return Task.FromResult(Wrap(
            "Öncelikli sonraki adımlar deterministik bağlamdan türetildi.",
            [],
            recommendations));
    }

    private static ExportAiProviderOutput BuildCaseOverview(ExportAiContextSnapshotModel context)
    {
        var findings = new List<ExportAiFindingDraft>();
        if (context.ExportCase is not null && context.ExportCase.ReadinessStatus is not "ready")
        {
            findings.Add(new ExportAiFindingDraft(
                ExportAiFindingCategory.Risk,
                "İhracat hazırlığı tamamlanmadı",
                $"Hazırlık durumu: {context.ExportCase.ReadinessStatus} ({context.ExportCase.ReadinessPercent}%).",
                ExportAiFindingSeverity.Warning,
                0.9m,
                ExportAiSourceType.OperationalData,
                [new ExportAiSourceReference("export-case", context.ExportCase.ExportCaseId.ToString(), context.ExportCase.CaseNumber, null)],
                null, null, null,
                true, false));
        }

        findings.AddRange(BuildCrossModuleFindings(context));
        return Wrap("İhracat dosyası genel bakış analizi (deterministik geliştirme sağlayıcı).", findings, []);
    }

    private static ExportAiProviderOutput BuildReadinessOutput(ExportAiContextSnapshotModel context)
    {
        var ec = context.ExportCase;
        var summary = ec is null
            ? "İhracat dosyası bağlamı bulunamadı."
            : $"Hazırlık: {ec.ReadinessStatus} ({ec.ReadinessPercent}%). Uygunluk: {ec.ComplianceStatus}.";

        var findings = new List<ExportAiFindingDraft>();
        if (ec?.ReadinessStatus is "not-ready" or "blocked")
        {
            findings.Add(new ExportAiFindingDraft(
                ExportAiFindingCategory.Risk,
                "Hazırlık engeli",
                summary,
                ExportAiFindingSeverity.Critical,
                0.9m,
                ExportAiSourceType.OperationalData,
                [new ExportAiSourceReference("readiness-engine", ec.ExportCaseId.ToString(), ec.CaseNumber, null)],
                null, null, null,
                true, true));
        }

        return Wrap(summary, findings, []);
    }

    private static ExportAiProviderOutput BuildDocumentGapOutput(ExportAiContextSnapshotModel context)
    {
        var findings = new List<ExportAiFindingDraft>();
        if (context.Compliance?.Requirements is null) return Wrap("Belge analizi için uygunluk bağlamı yok.", findings, []);

        foreach (var req in context.Compliance.Requirements.Where(r =>
            !string.IsNullOrWhiteSpace(r.RequiredDocumentType) && r.Status is "missing" or "pending"))
        {
            var docPresent = context.Documents.Any(d =>
                string.Equals(d.DocumentType, req.RequiredDocumentType, StringComparison.OrdinalIgnoreCase));

            findings.Add(new ExportAiFindingDraft(
                ExportAiFindingCategory.Document,
                docPresent ? $"Belge doğrulanmadı: {req.RequiredDocumentType}" : $"Eksik belge: {req.RequiredDocumentType}",
                docPresent
                    ? $"{req.RequiredDocumentType} kayıtlı ancak gereksinim karşılanmadı."
                    : $"{req.RequiredDocumentType} kural seti tarafından gerekli ancak mevcut değil.",
                ExportAiFindingSeverity.Critical,
                0.88m,
                ExportAiSourceType.VerifiedRule,
                BuildRequirementSources(context, req),
                req.Id,
                context.Documents.FirstOrDefault(d => string.Equals(d.DocumentType, req.RequiredDocumentType, StringComparison.OrdinalIgnoreCase))?.Id,
                null,
                true, true));
        }

        return Wrap("Belge boşluk analizi tamamlandı.", findings, []);
    }

    private static ExportAiProviderOutput BuildCrossModuleOutput(ExportAiContextSnapshotModel context) =>
        Wrap(
            "Çapraz modül risk analizi.",
            BuildCrossModuleFindings(context),
            context.CrossModuleRisks.Where(r => r.Blocking).Select(r =>
                new ExportAiRecommendationDraft(
                    r.Type.ToString(),
                    r.Description,
                    ExportAiRecommendationPriority.High,
                    0.8m,
                    r.Description,
                    ExportAiProposedActionType.RequestHumanReview,
                    JsonSerializer.Serialize(new { riskType = r.Type.ToString() }))).ToList());

    private static ExportAiProviderOutput BuildMissingDataOutput(ExportAiContextSnapshotModel context)
    {
        var findings = new List<ExportAiFindingDraft>();
        if (context.ExportCase?.HsCodeStatus is "unknown" or "draft")
        {
            findings.Add(MakeDataGap("GTIP/HS kodu eksik veya taslak", "data-quality", 0.7m));
        }

        if (context.ExportCase?.OriginConfirmationStatus is "unknown" or "draft")
        {
            findings.Add(MakeDataGap("Menşe onayı eksik", "data-quality", 0.75m));
        }

        return Wrap("Eksik veri analizi tamamlandı.", findings, []);
    }

    private static List<ExportAiFindingDraft> BuildCrossModuleFindings(ExportAiContextSnapshotModel context) =>
        context.CrossModuleRisks.Select(r => new ExportAiFindingDraft(
            r.Type == ExportCrossModuleRiskType.ContextUnavailable ? ExportAiFindingCategory.DataQuality : ExportAiFindingCategory.Risk,
            r.Type.ToString(),
            r.Description,
            r.Severity switch
            {
                ExportCrossModuleRiskSeverity.Critical => ExportAiFindingSeverity.Critical,
                ExportCrossModuleRiskSeverity.Warning => ExportAiFindingSeverity.Warning,
                _ => ExportAiFindingSeverity.Info,
            },
            0.75m,
            ExportAiSourceType.OperationalData,
            [new ExportAiSourceReference(r.SourceModule.ToString().ToLowerInvariant(), r.SourceRecordReference, r.SourceRecordReference, null)],
            null, null, null,
            r.Blocking,
            r.Blocking)).ToList();

    private static ExportAiFindingDraft MakeDataGap(string title, string sourceId, decimal confidence) =>
        new(
            ExportAiFindingCategory.DataQuality,
            title,
            title,
            ExportAiFindingSeverity.Warning,
            confidence,
            ExportAiSourceType.OperationalData,
            [new ExportAiSourceReference("export-case", sourceId, title, null)],
            null, null, null,
            true, false);

    private static string BuildComplianceExplanation(ExportAiContextSnapshotModel context, ExportAiContextRequirementRef req)
    {
        if (context.Compliance?.Result == "unable-to-evaluate")
        {
            return "Doğrulanmış kaynak bulunmadığı için kesin değerlendirme yapılamıyor.";
        }

        return $"Bu ihracat dosyası, doğrulanmış kural setindeki {req.RequirementCode} nedeniyle bloke durumda. {req.Title}";
    }

    private static List<ExportAiSourceReference> BuildRequirementSources(
        ExportAiContextSnapshotModel context,
        ExportAiContextRequirementRef req)
    {
        var refs = new List<ExportAiSourceReference>
        {
            new("compliance-requirement", req.Id.ToString(), req.RequirementCode, req.Title),
        };

        if (context.Compliance?.RulePackVersion is not null)
        {
            refs.Add(new ExportAiSourceReference(
                "rule-pack",
                context.Compliance.EvaluationId?.ToString() ?? "",
                $"Rule Pack v{context.Compliance.RulePackVersion}",
                context.Compliance.RulePackName));
        }

        refs.Add(new ExportAiSourceReference("rule", req.SourceReference, req.SourceReference, null));
        return refs;
    }

    private static ExportAiRecommendationDraft MakeAction(
        int rank,
        string title,
        string code,
        ExportAiProposedActionType actionType,
        object payload) =>
        new(
            $"{rank}. {title}",
            title,
            ExportAiRecommendationPriority.High,
            0.8m,
            code,
            actionType,
            JsonSerializer.Serialize(payload));

    private static ExportAiProviderOutput Wrap(
        string reasoningSummary,
        List<ExportAiFindingDraft> findings,
        List<ExportAiRecommendationDraft> recommendations) =>
        new(
            reasoningSummary,
            findings,
            recommendations,
            InputUsageUnits: 100,
            OutputUsageUnits: findings.Count * 50 + recommendations.Count * 30,
            AiCreditsConsumed: DemoCreditsPerRun);
}
