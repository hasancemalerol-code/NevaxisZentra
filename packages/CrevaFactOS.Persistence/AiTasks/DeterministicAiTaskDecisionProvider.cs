using System.Text.Json;
using System.Text.RegularExpressions;
using CrevaFactOS.Persistence.Development;

namespace CrevaFactOS.Persistence.AiTasks;

public sealed partial class DeterministicAiTaskDecisionProvider : IAiTaskDecisionProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static readonly string[] PromptInjectionMarkers =
    [
        "ignore previous instructions",
        "ignore all previous instructions",
        "disable approval",
        "assign this to ceo",
        "bypass approval",
        "system prompt",
        "you are now",
    ];

    public string ProviderName => AiTaskDecisionIntelligenceProviders.DeterministicDevelopment;
    public string ModelName => AiTaskDecisionIntelligenceProviders.DeterministicDevelopmentModel;
    public bool IsDevelopmentProvider => true;
    public bool IsAvailable => true;

    public Task<AiTaskDecisionProviderResult> AnalyzeAsync(
        AiTaskDecisionProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ContainsProviderFailureMarker(request.Task))
        {
            return Task.FromResult(new AiTaskDecisionProviderResult(
                false,
                AiTaskDecisionIntelligenceFailureCodes.ProviderFailed,
                "Deterministic provider failure simulation.",
                null,
                null,
                "{}",
                ProviderName,
                ModelName));
        }

        if (string.Equals(request.AnalysisType, AiTaskDecisionAnalysisTypes.ClosureReview, StringComparison.Ordinal))
        {
            var closure = BuildClosureReview(request.Task, request.RelatedTaskCandidates);
            var raw = JsonSerializer.Serialize(new { provider = ProviderName, model = ModelName, closure }, JsonOptions);
            return Task.FromResult(new AiTaskDecisionProviderResult(
                true,
                null,
                null,
                null,
                closure,
                raw,
                ProviderName,
                ModelName));
        }

        var triage = BuildTriage(request.Task, request.RelatedTaskCandidates);
        var triageRaw = JsonSerializer.Serialize(new { provider = ProviderName, model = ModelName, triage }, JsonOptions);
        return Task.FromResult(new AiTaskDecisionProviderResult(
            true,
            null,
            null,
            triage,
            null,
            triageRaw,
            ProviderName,
            ModelName));
    }

    private static AiTaskDecisionTriageResult BuildTriage(
        AiTaskDecisionInputSnapshot task,
        IReadOnlyList<AiTaskDecisionRelatedCandidateSnapshot> candidates)
    {
        var injectionDetected = ContainsPromptInjection(task);
        var insufficient = string.IsNullOrWhiteSpace(task.Title)
                           && string.IsNullOrWhiteSpace(task.Summary)
                           && string.IsNullOrWhiteSpace(task.DetectedIssue);

        if (insufficient)
        {
            return new AiTaskDecisionTriageResult(
                null, null, null, null, null, null,
                null, null,
                AiTaskDecisionImpactBands.InsufficientData,
                AiTaskDecisionUrgencyBands.InsufficientData,
                AiTaskDecisionSlaRiskBands.InsufficientData,
                "Görev bağlamı yetersiz.",
                [],
                null,
                "Yapılandırılmış analiz için görev başlığı veya özeti gerekli.",
                AiTaskDecisionConfidenceBands.InsufficientData,
                true,
                [],
                [],
                [],
                null);
        }

        var suggestedCategory = task.Category;
        var suggestedPriority = SuggestPriority(task);
        var suggestedSeverity = MapSeverity(task.RiskLevel);
        var suggestedTeam = SuggestTeamChannelId(task);
        var suggestedDepartment = MapDepartment(task.Category);
        var impactBand = MapImpactBand(task);
        var urgencyBand = MapUrgencyBand(task);
        var slaRisk = MapSlaRiskBand(task);
        var factors = BuildContributingFactors(task, injectionDetected);
        var intervention = BuildIntervention(task, slaRisk);
        var explanation = BuildExplanation(task, injectionDetected, slaRisk);
        var confidence = MapConfidence(task, candidates.Count);
        var nextActions = BuildNextActions(task, suggestedTeam, slaRisk);
        var evidence = BuildEvidenceRecommendations(task);
        var related = BuildRelatedCandidates(task, candidates);
        var policyComparison = BuildPolicyComparison(task, suggestedPriority, suggestedTeam);

        return new AiTaskDecisionTriageResult(
            suggestedCategory,
            MapTaskType(task),
            suggestedPriority,
            suggestedSeverity,
            suggestedDepartment,
            suggestedTeam,
            MapBusinessImpact(impactBand),
            BuildUrgencyReason(task, urgencyBand),
            impactBand,
            urgencyBand,
            slaRisk,
            BuildRiskReason(task, slaRisk),
            factors,
            intervention,
            explanation,
            confidence,
            false,
            nextActions,
            evidence,
            related,
            policyComparison);
    }

    private static AiTaskDecisionClosureReviewResult BuildClosureReview(
        AiTaskDecisionInputSnapshot task,
        IReadOnlyList<AiTaskDecisionRelatedCandidateSnapshot> candidates)
    {
        var concerns = new List<string>();
        var recommendations = new List<string>();

        if (task.EvidenceCount < task.MinimumEvidenceRequired)
        {
            concerns.Add($"Zorunlu kanıt sayısı karşılanmadı ({task.EvidenceCount}/{task.MinimumEvidenceRequired}).");
            recommendations.Add("Kapanış öncesi eksik kanıtları tamamlayın.");
        }

        if (string.IsNullOrWhiteSpace(task.ResolutionSummary))
        {
            concerns.Add("Çözüm özeti eksik.");
            recommendations.Add("Çözüm özetini güncelleyin.");
        }

        var openRelated = candidates.Count(c =>
            !string.Equals(c.Status, AiTaskStatuses.Completed, StringComparison.Ordinal)
            && !string.Equals(c.Status, AiTaskStatuses.Cancelled, StringComparison.Ordinal));

        if (openRelated > 0)
        {
            concerns.Add($"İlişkili açık görev sayısı: {openRelated}.");
            recommendations.Add("İlişkili açık görevleri kapatın veya ilişkilendirin.");
        }

        if (ContainsPromptInjection(task))
        {
            concerns.Add("Kaynak metinde gömülü talimat ifadeleri tespit edildi; insan incelemesi önerilir.");
        }

        var readiness = concerns.Count switch
        {
            0 => AiTaskDecisionClosureReadiness.Ready,
            1 when task.EvidenceCount >= task.MinimumEvidenceRequired => AiTaskDecisionClosureReadiness.ReviewNeeded,
            _ when concerns.Any(c => c.Contains("kanıt", StringComparison.OrdinalIgnoreCase)) => AiTaskDecisionClosureReadiness.RiskDetected,
            _ => AiTaskDecisionClosureReadiness.ReviewNeeded,
        };

        return new AiTaskDecisionClosureReviewResult(
            readiness,
            concerns,
            recommendations,
            "Kapanış hazırlığı mevcut görev bağlamı, kanıt ve ilişkili görevlere göre değerlendirildi.",
            concerns.Count == 0 ? AiTaskDecisionConfidenceBands.High : AiTaskDecisionConfidenceBands.Medium,
            false);
    }

    private static bool ContainsProviderFailureMarker(AiTaskDecisionInputSnapshot task) =>
        task.Title.Contains("AI-PROVIDER-FAIL", StringComparison.OrdinalIgnoreCase)
        || task.DetectedIssue.Contains("AI-PROVIDER-FAIL", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsPromptInjection(AiTaskDecisionInputSnapshot task)
    {
        var combined = string.Join(' ', task.Title, task.Summary, task.DetectedIssue, task.Reasoning);
        return PromptInjectionMarkers.Any(marker =>
            combined.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static string SuggestPriority(AiTaskDecisionInputSnapshot task) =>
        task.Priority switch
        {
            AiTaskPriorities.Critical => AiTaskPriorities.Critical,
            AiTaskPriorities.High => task.EscalationLevel > 0 ? AiTaskPriorities.Critical : AiTaskPriorities.High,
            _ when string.Equals(task.SourceModule, "shift-handover", StringComparison.Ordinal)
                   && string.Equals(task.Category, AiTaskCategories.Maintenance, StringComparison.Ordinal)
                => AiTaskPriorities.Critical,
            _ => task.Priority,
        };

    private static Guid? SuggestTeamChannelId(AiTaskDecisionInputSnapshot task)
    {
        if (task.AssignedTeamChannelId.HasValue)
        {
            return task.AssignedTeamChannelId;
        }

        return task.Category switch
        {
            AiTaskCategories.Maintenance => DevelopmentPilotIds.TeamsPilotChannelBakimId,
            AiTaskCategories.Quality => DevelopmentPilotIds.TeamsPilotChannelKaliteId,
            AiTaskCategories.ProductionPlanning or AiTaskCategories.LiveProduction
                => DevelopmentPilotIds.TeamsPilotChannelUretimId,
            _ => null,
        };
    }

    private static string MapDepartment(string category) => category switch
    {
        AiTaskCategories.Maintenance => "Bakım",
        AiTaskCategories.Quality => "Kalite",
        AiTaskCategories.LiveProduction or AiTaskCategories.ProductionPlanning => "Üretim",
        _ => "Operasyon",
    };

    private static string MapTaskType(AiTaskDecisionInputSnapshot task) =>
        string.Equals(task.SourceModule, "shift-handover", StringComparison.Ordinal)
            ? "shift-handover-follow-up"
            : task.Category;

    private static string MapSeverity(string riskLevel) => riskLevel;

    private static string MapImpactBand(AiTaskDecisionInputSnapshot task) => task.Priority switch
    {
        AiTaskPriorities.Critical => AiTaskDecisionImpactBands.Critical,
        AiTaskPriorities.High => AiTaskDecisionImpactBands.High,
        AiTaskPriorities.Medium => AiTaskDecisionImpactBands.Medium,
        _ => AiTaskDecisionImpactBands.Low,
    };

    private static string MapUrgencyBand(AiTaskDecisionInputSnapshot task)
    {
        if (task.EscalationLevel > 0 || string.Equals(task.SlaState, AiTaskSlaStates.Breached, StringComparison.Ordinal))
        {
            return AiTaskDecisionUrgencyBands.Critical;
        }

        if (string.Equals(task.SlaState, AiTaskSlaStates.DueSoon, StringComparison.Ordinal)
            || string.Equals(task.SlaState, AiTaskSlaStates.Overdue, StringComparison.Ordinal))
        {
            return AiTaskDecisionUrgencyBands.High;
        }

        return task.Priority switch
        {
            AiTaskPriorities.Critical => AiTaskDecisionUrgencyBands.Critical,
            AiTaskPriorities.High => AiTaskDecisionUrgencyBands.High,
            _ => AiTaskDecisionUrgencyBands.Medium,
        };
    }

    private static string MapSlaRiskBand(AiTaskDecisionInputSnapshot task)
    {
        if (string.IsNullOrWhiteSpace(task.SlaState) || string.Equals(task.SlaState, AiTaskSlaStates.NotApplicable, StringComparison.Ordinal))
        {
            return AiTaskDecisionSlaRiskBands.InsufficientData;
        }

        return task.SlaState switch
        {
            AiTaskSlaStates.Breached => AiTaskDecisionSlaRiskBands.Critical,
            AiTaskSlaStates.Overdue => AiTaskDecisionSlaRiskBands.High,
            AiTaskSlaStates.DueSoon => AiTaskDecisionSlaRiskBands.Watch,
            _ => AiTaskDecisionSlaRiskBands.Low,
        };
    }

    private static string MapBusinessImpact(string impactBand) => impactBand switch
    {
        AiTaskDecisionImpactBands.Critical => "Üretim duruşu veya güvenlik riski",
        AiTaskDecisionImpactBands.High => "Üretim performansı etkilenebilir",
        AiTaskDecisionImpactBands.Medium => "Operasyonel verimlilik etkilenebilir",
        _ => "Sınırlı operasyonel etki",
    };

    private static string BuildUrgencyReason(AiTaskDecisionInputSnapshot task, string urgencyBand) =>
        urgencyBand switch
        {
            AiTaskDecisionUrgencyBands.Critical => "Kritik öncelik, eskalasyon veya SLA ihlali sinyali mevcut.",
            AiTaskDecisionUrgencyBands.High => "Yüksek öncelik veya yaklaşan SLA son tarihi.",
            _ => "Standart operasyonel aciliyet.",
        };

    private static string BuildRiskReason(AiTaskDecisionInputSnapshot task, string slaRisk) =>
        slaRisk switch
        {
            AiTaskDecisionSlaRiskBands.Critical => "SLA ihlali gerçekleşmiş.",
            AiTaskDecisionSlaRiskBands.High => "SLA süresi aşılmış veya kritik eşiğe yakın.",
            AiTaskDecisionSlaRiskBands.Watch => "SLA son tarihi yaklaşıyor.",
            AiTaskDecisionSlaRiskBands.InsufficientData => "SLA durumu belirlenemedi.",
            _ => "SLA riski düşük.",
        };

    private static string BuildIntervention(AiTaskDecisionInputSnapshot task, string slaRisk)
    {
        if (slaRisk is AiTaskDecisionSlaRiskBands.Critical or AiTaskDecisionSlaRiskBands.High)
        {
            return "SLA kritik seviyeye gelmeden eskale et";
        }

        if (string.Equals(task.Category, AiTaskCategories.Maintenance, StringComparison.Ordinal))
        {
            return "Bakım ekibine yönlendir";
        }

        return "Operasyonel inceleme başlat";
    }

    private static List<string> BuildContributingFactors(AiTaskDecisionInputSnapshot task, bool injectionDetected)
    {
        var factors = new List<string>();
        if (!string.IsNullOrWhiteSpace(task.SourceModule))
        {
            factors.Add($"Kaynak modül: {task.SourceModule}");
        }

        if (!string.IsNullOrWhiteSpace(task.MachineId))
        {
            factors.Add($"Makine: {task.MachineId}");
        }

        if (!string.IsNullOrWhiteSpace(task.ProductionOrderId))
        {
            factors.Add($"Üretim emri: {task.ProductionOrderId}");
        }

        if (task.EscalationLevel > 0)
        {
            factors.Add($"Eskalasyon seviyesi: {task.EscalationLevel}");
        }

        if (!string.Equals(task.SlaState, AiTaskSlaStates.OnTrack, StringComparison.Ordinal))
        {
            factors.Add($"SLA durumu: {task.SlaState}");
        }

        if (task.AutomationRuleId.HasValue)
        {
            factors.Add("Deterministik otomasyon politikası uygulandı");
        }

        if (injectionDetected)
        {
            factors.Add("Kaynak metinde talimat enjeksiyonu işareti yok sayıldı");
        }

        return factors;
    }

    private static string BuildExplanation(AiTaskDecisionInputSnapshot task, bool injectionDetected, string slaRisk)
    {
        var parts = new List<string>
        {
            $"Görev kategorisi {task.Category}, öncelik {task.Priority}.",
        };

        if (!string.IsNullOrWhiteSpace(task.SourceModule))
        {
            parts.Add($"Kaynak: {task.SourceModule}.");
        }

        if (slaRisk is AiTaskDecisionSlaRiskBands.High or AiTaskDecisionSlaRiskBands.Critical)
        {
            parts.Add("SLA riski yükselmiş durumda.");
        }

        if (injectionDetected)
        {
            parts.Add("Gömülü talimat ifadeleri yok sayıldı; yalnızca operasyonel veri olarak değerlendirildi.");
        }

        return string.Join(' ', parts);
    }

    private static string MapConfidence(AiTaskDecisionInputSnapshot task, int relatedCount)
    {
        var score = 0;
        if (!string.IsNullOrWhiteSpace(task.Category)) score++;
        if (!string.IsNullOrWhiteSpace(task.Priority)) score++;
        if (!string.IsNullOrWhiteSpace(task.SourceModule)) score++;
        if (!string.IsNullOrWhiteSpace(task.MachineId) || !string.IsNullOrWhiteSpace(task.ProductionOrderId)) score++;
        if (relatedCount > 0) score++;

        return score switch
        {
            >= 4 => AiTaskDecisionConfidenceBands.High,
            >= 2 => AiTaskDecisionConfidenceBands.Medium,
            _ => AiTaskDecisionConfidenceBands.Low,
        };
    }

    private static List<AiTaskDecisionNextActionResult> BuildNextActions(
        AiTaskDecisionInputSnapshot task,
        Guid? suggestedTeam,
        string slaRisk)
    {
        var actions = new List<AiTaskDecisionNextActionResult>();

        if (suggestedTeam.HasValue && !task.AssignedTeamChannelId.HasValue)
        {
            actions.Add(new AiTaskDecisionNextActionResult(
                AiTaskDecisionRecommendationActionTypes.AssignTeam,
                "Bakım ekibine yönlendir",
                "Kategori ve kaynak modülü bakım yönlendirmesi önerir.",
                AiTaskDecisionConfidenceBands.Medium,
                AiTaskDecisionUrgencyBands.High,
                "team-channel",
                suggestedTeam.Value.ToString("D"),
                JsonSerializer.Serialize(new { teamChannelId = suggestedTeam.Value }, JsonOptions)));
        }

        if (string.Equals(task.Category, AiTaskCategories.Maintenance, StringComparison.Ordinal))
        {
            actions.Add(new AiTaskDecisionNextActionResult(
                AiTaskDecisionRecommendationActionTypes.RequestEvidence,
                "Operatörden fotoğraf kanıtı iste",
                "Makine arızası için görsel kanıt operasyonel doğrulamayı destekler.",
                AiTaskDecisionConfidenceBands.Medium,
                AiTaskDecisionUrgencyBands.Medium,
                "evidence",
                AiTaskEvidenceTypes.Photo,
                JsonSerializer.Serialize(new { evidenceType = AiTaskEvidenceTypes.Photo }, JsonOptions)));
        }

        if (slaRisk is AiTaskDecisionSlaRiskBands.High or AiTaskDecisionSlaRiskBands.Critical)
        {
            actions.Add(new AiTaskDecisionNextActionResult(
                AiTaskDecisionRecommendationActionTypes.Escalate,
                "SLA kritik seviyeye gelmeden eskale et",
                "SLA risk bandı yükselmiş durumda.",
                AiTaskDecisionConfidenceBands.High,
                AiTaskDecisionUrgencyBands.Critical,
                null,
                null,
                "{}"));
        }

        if (task.ClosureApprovalRequired && string.Equals(task.ClosureState, AiTaskClosureStates.AwaitingClosureApproval, StringComparison.Ordinal))
        {
            actions.Add(new AiTaskDecisionNextActionResult(
                AiTaskDecisionRecommendationActionTypes.RequestClosureApproval,
                "Kapanıştan önce ikinci onay iste",
                "Kapanış onayı politikası aktif.",
                AiTaskDecisionConfidenceBands.High,
                AiTaskDecisionUrgencyBands.Medium,
                null,
                null,
                "{}"));
        }

        if (!string.IsNullOrWhiteSpace(task.ProductionOrderId))
        {
            actions.Add(new AiTaskDecisionNextActionResult(
                AiTaskDecisionRecommendationActionTypes.ReviewProductionOrder,
                "İlgili üretim emrini kontrol et",
                "Görev bir üretim emrine bağlı.",
                AiTaskDecisionConfidenceBands.Medium,
                AiTaskDecisionUrgencyBands.Medium,
                "production-order",
                task.ProductionOrderId,
                JsonSerializer.Serialize(new { productionOrderId = task.ProductionOrderId }, JsonOptions)));
        }

        return actions;
    }

    private static List<AiTaskDecisionEvidenceRecommendationResult> BuildEvidenceRecommendations(
        AiTaskDecisionInputSnapshot task)
    {
        var items = new List<AiTaskDecisionEvidenceRecommendationResult>();

        if (string.Equals(task.Category, AiTaskCategories.Maintenance, StringComparison.Ordinal))
        {
            items.Add(new AiTaskDecisionEvidenceRecommendationResult(
                AiTaskEvidenceTypes.Photo,
                "Fotoğraf kanıtı",
                "Makine arızası görsel doğrulama gerektirir.",
                task.MinimumEvidenceRequired > 0,
                AiTaskDecisionConfidenceBands.Medium));

            items.Add(new AiTaskDecisionEvidenceRecommendationResult(
                AiTaskEvidenceTypes.MaintenanceRecord,
                "Bakım kaydı",
                "Müdahale kaydı kapanış için destekleyici kanıt sağlar.",
                false,
                AiTaskDecisionConfidenceBands.Low));
        }

        if (string.Equals(task.Category, AiTaskCategories.Quality, StringComparison.Ordinal))
        {
            items.Add(new AiTaskDecisionEvidenceRecommendationResult(
                AiTaskEvidenceTypes.QualityRecord,
                "Kalite kaydı",
                "Kalite sapması için kayıt kanıtı önerilir.",
                task.MinimumEvidenceRequired > 0,
                AiTaskDecisionConfidenceBands.Medium));
        }

        return items;
    }

    private static List<AiTaskDecisionRelatedCandidateResult> BuildRelatedCandidates(
        AiTaskDecisionInputSnapshot task,
        IReadOnlyList<AiTaskDecisionRelatedCandidateSnapshot> candidates)
    {
        return candidates
            .Select(candidate =>
            {
                var relationship = ResolveRelationshipType(task, candidate);
                var confidence = ResolveRelationshipConfidence(task, candidate);
                return new AiTaskDecisionRelatedCandidateResult(
                    candidate.TaskId,
                    relationship,
                    confidence,
                    BuildRelationshipExplanation(task, candidate, relationship));
            })
            .Where(c => c.ConfidenceBand != AiTaskDecisionConfidenceBands.Low || c.RelationshipType != AiTaskDecisionRelationshipTypes.Related)
            .Take(5)
            .ToList();
    }

    internal static string ResolveRelationshipType(
        AiTaskDecisionInputSnapshot task,
        AiTaskDecisionRelatedCandidateSnapshot candidate)
    {
        if (!string.IsNullOrWhiteSpace(task.SourceEntityId)
            && string.Equals(task.SourceEntityId, candidate.SourceEntityId, StringComparison.Ordinal))
        {
            return AiTaskDecisionRelationshipTypes.SameSource;
        }

        if (!string.IsNullOrWhiteSpace(task.MachineId)
            && string.Equals(task.MachineId, candidate.MachineId, StringComparison.Ordinal))
        {
            return TitleSimilarity(task.Title, candidate.Title) > 0.6
                ? AiTaskDecisionRelationshipTypes.PossibleDuplicate
                : AiTaskDecisionRelationshipTypes.SameIncident;
        }

        if (!string.IsNullOrWhiteSpace(task.ProductionOrderId)
            && string.Equals(task.ProductionOrderId, candidate.ProductionOrderId, StringComparison.Ordinal))
        {
            return AiTaskDecisionRelationshipTypes.Related;
        }

        if (TitleSimilarity(task.Title, candidate.Title) > 0.75)
        {
            return AiTaskDecisionRelationshipTypes.PossibleDuplicate;
        }

        return AiTaskDecisionRelationshipTypes.Related;
    }

    internal static string ResolveRelationshipConfidence(
        AiTaskDecisionInputSnapshot task,
        AiTaskDecisionRelatedCandidateSnapshot candidate)
    {
        var score = TitleSimilarity(task.Title, candidate.Title);
        if (!string.IsNullOrWhiteSpace(task.SourceEntityId)
            && string.Equals(task.SourceEntityId, candidate.SourceEntityId, StringComparison.Ordinal))
        {
            score += 0.35;
        }

        if (!string.IsNullOrWhiteSpace(task.MachineId)
            && string.Equals(task.MachineId, candidate.MachineId, StringComparison.Ordinal))
        {
            score += 0.25;
        }

        return score switch
        {
            >= 0.85 => AiTaskDecisionConfidenceBands.High,
            >= 0.55 => AiTaskDecisionConfidenceBands.Medium,
            _ => AiTaskDecisionConfidenceBands.Low,
        };
    }

    private static string BuildRelationshipExplanation(
        AiTaskDecisionInputSnapshot task,
        AiTaskDecisionRelatedCandidateSnapshot candidate,
        string relationship) =>
        relationship switch
        {
            AiTaskDecisionRelationshipTypes.SameSource => "Aynı kaynak varlığına bağlı görev.",
            AiTaskDecisionRelationshipTypes.PossibleDuplicate => "Benzer başlık ve ortak operasyonel bağlam.",
            AiTaskDecisionRelationshipTypes.SameIncident => "Aynı makine/tesis bağlamında ilişkili olay.",
            _ => $"Ortak bağlam: {candidate.SourceModule ?? task.SourceModule ?? "operasyon"}.",
        };

    private static AiTaskDecisionPolicyComparisonResult? BuildPolicyComparison(
        AiTaskDecisionInputSnapshot task,
        string suggestedPriority,
        Guid? suggestedTeam)
    {
        if (!task.AutomationRuleId.HasValue)
        {
            return null;
        }

        var policyActions = new List<string>();
        if (task.AssignedTeamChannelId.HasValue)
        {
            policyActions.Add("Takıma ata (otomasyon)");
        }

        if (task.MinimumEvidenceRequired > 0)
        {
            policyActions.Add("Kanıt zorunlu (politika)");
        }

        if (task.ClosureApprovalRequired)
        {
            policyActions.Add("Kapanış onayı zorunlu (politika)");
        }

        var differences = new List<string>();
        if (!string.Equals(suggestedPriority, task.Priority, StringComparison.Ordinal))
        {
            differences.Add($"AI öncelik önerisi: {suggestedPriority}, mevcut: {task.Priority}");
        }

        if (suggestedTeam.HasValue && task.AssignedTeamChannelId != suggestedTeam)
        {
            differences.Add("AI farklı ekip önerisi sunuyor; otomasyon ataması korunur.");
        }

        return new AiTaskDecisionPolicyComparisonResult(
            "Deterministik otomasyon politikası uygulandı",
            policyActions,
            "AI bağlamsal değerlendirme ve ek aksiyon önerileri",
            differences);
    }

    private static double TitleSimilarity(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return 0;
        }

        var tokensA = Tokenize(left);
        var tokensB = Tokenize(right);
        if (tokensA.Count == 0 || tokensB.Count == 0)
        {
            return 0;
        }

        var intersection = tokensA.Intersect(tokensB).Count();
        return intersection / (double)Math.Max(tokensA.Count, tokensB.Count);
    }

    private static HashSet<string> Tokenize(string value) =>
        TitleTokenRegex().Matches(value.ToLowerInvariant())
            .Select(m => m.Value)
            .Where(t => t.Length > 2)
            .ToHashSet(StringComparer.Ordinal);

    [GeneratedRegex(@"[a-z0-9çğıöşü]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TitleTokenRegex();
}
