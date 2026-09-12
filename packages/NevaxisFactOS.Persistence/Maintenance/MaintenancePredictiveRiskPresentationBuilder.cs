namespace NevaxisFactOS.Persistence.Maintenance;

public static class MaintenancePredictiveRiskPresentationBuilder
{
    public static string BuildTaskDraftIdempotencyKey(Guid assessmentId, string taskPurpose) =>
        $"predictive-risk-task:{assessmentId:D}:{taskPurpose}";

    public static string ResolveTaskPurpose(MaintenancePredictiveRiskAssessmentReadModel assessment)
    {
        if (assessment.RiskBand == MaintenancePredictiveRiskBands.InsufficientData)
        {
            return MaintenancePredictiveRiskConstants.TaskPurposeDataReviewDraft;
        }

        return MaintenancePredictiveRiskConstants.TaskPurposeHumanReviewDraft;
    }

    public static bool CanCreateTaskDraft(MaintenancePredictiveRiskAssessmentReadModel assessment)
    {
        if (assessment.LinkedAiTaskId.HasValue)
        {
            return false;
        }

        if (assessment.RiskBand == MaintenancePredictiveRiskBands.InsufficientData)
        {
            return true;
        }

        if (assessment.DataSufficiency == MaintenanceConditionDataSufficiencyLevels.Insufficient)
        {
            return false;
        }

        return assessment.RiskBand is MaintenancePredictiveRiskBands.Watch
            or MaintenancePredictiveRiskBands.High
            or MaintenancePredictiveRiskBands.Critical;
    }

    public static string BuildTaskTitle(MaintenancePredictiveRiskAssessmentReadModel assessment)
    {
        var machineLabel = !string.IsNullOrWhiteSpace(assessment.MachineName)
            ? assessment.MachineName
            : assessment.AssetReference;

        if (assessment.RiskBand == MaintenancePredictiveRiskBands.InsufficientData)
        {
            return $"Kestirimci bakım veri incelemesi — {machineLabel}";
        }

        return $"Kestirimci bakım risk incelemesi — {machineLabel}";
    }

    public static string BuildTaskSummary(MaintenancePredictiveRiskAssessmentReadModel assessment)
    {
        var recommendation = assessment.Recommendation?.Explanation
            ?? assessment.Explanation;

        return $"Risk bandı: {assessment.RiskBand}. Öneri: {recommendation}";
    }

    public static string BuildEvidenceSummary(MaintenancePredictiveRiskAssessmentReadModel assessment)
    {
        if (assessment.Evidence.Count == 0)
        {
            return assessment.Explanation;
        }

        return string.Join(
            " | ",
            assessment.Evidence.Take(3).Select(item => item.Summary));
    }

    public static string ResolveTaskPriority(MaintenancePredictiveRiskAssessmentReadModel assessment) =>
        assessment.RiskBand switch
        {
            MaintenancePredictiveRiskBands.Critical => "critical",
            MaintenancePredictiveRiskBands.High => "high",
            MaintenancePredictiveRiskBands.Watch => "medium",
            MaintenancePredictiveRiskBands.InsufficientData => "low",
            _ => "low",
        };

    public static string ResolveInitialFollowUpStatus(
        string riskBand,
        string recommendationCategory)
    {
        if (riskBand == MaintenancePredictiveRiskBands.InsufficientData)
        {
            return MaintenancePredictiveRiskFollowUpStatuses.DataReviewNeeded;
        }

        if (riskBand == MaintenancePredictiveRiskBands.Low &&
            recommendationCategory == MaintenancePredictiveRecommendationCategories.ContinueRoutineMonitoring)
        {
            return MaintenancePredictiveRiskFollowUpStatuses.Monitoring;
        }

        return MaintenancePredictiveRiskFollowUpStatuses.Active;
    }

    public static string BuildMaintenanceDeepLink(string assetReference) =>
        $"/bakim/kestirimci-riskler?asset={Uri.EscapeDataString(assetReference)}";

    public static IReadOnlyList<MaintenancePredictiveRiskDriverReadModel> BackfillDrivers(
        MaintenancePredictiveRiskAssessment assessment,
        IReadOnlyList<MaintenancePredictiveRiskDriverReadModel> existing)
    {
        if (existing.Count > 0)
        {
            return existing;
        }

        var drivers = new List<MaintenancePredictiveRiskDriverReadModel>();
        if (assessment.ActiveAnomalyCount > 0)
        {
            drivers.Add(new MaintenancePredictiveRiskDriverReadModel(
                "active-anomaly",
                $"{assessment.ActiveAnomalyCount} aktif anomali kaydı izleniyor."));
        }

        if (assessment.RiskBand == MaintenancePredictiveRiskBands.InsufficientData)
        {
            drivers.Add(new MaintenancePredictiveRiskDriverReadModel(
                "insufficient-data",
                "Durum değerlendirmesi için kanıt yeterliliği sınırlı."));
        }

        if (!string.IsNullOrWhiteSpace(assessment.MaintenanceHistorySummary) &&
            !assessment.MaintenanceHistorySummary.StartsWith(
                "Son 90 günde ilişkilendirilebilir arıza kaydı bulunmuyor",
                StringComparison.Ordinal))
        {
            drivers.Add(new MaintenancePredictiveRiskDriverReadModel(
                "maintenance-history",
                assessment.MaintenanceHistorySummary));
        }

        if (drivers.Count == 0 && !string.IsNullOrWhiteSpace(assessment.Explanation))
        {
            drivers.Add(new MaintenancePredictiveRiskDriverReadModel(
                "condition-status",
                assessment.Explanation));
        }

        return drivers;
    }

    public static IReadOnlyList<MaintenancePredictiveRiskEvidenceReadModel> BackfillEvidence(
        MaintenancePredictiveRiskAssessment assessment,
        IReadOnlyList<MaintenancePredictiveRiskEvidenceReadModel> existing)
    {
        if (existing.Count > 0)
        {
            return existing;
        }

        var evidence = new List<MaintenancePredictiveRiskEvidenceReadModel>();
        if (!string.IsNullOrWhiteSpace(assessment.ConditionEvaluationIdentity))
        {
            evidence.Add(new MaintenancePredictiveRiskEvidenceReadModel(
                "condition-profile",
                assessment.ConditionEvaluationIdentity,
                $"Durum yeterliliği={assessment.DataSufficiency}, tazelik={assessment.DataFreshness}, aktif anomali={assessment.ActiveAnomalyCount}."));
        }

        if (!string.IsNullOrWhiteSpace(assessment.MaintenanceHistorySummary))
        {
            evidence.Add(new MaintenancePredictiveRiskEvidenceReadModel(
                "maintenance-history",
                assessment.AssetReference,
                assessment.MaintenanceHistorySummary));
        }

        if (evidence.Count == 0 && !string.IsNullOrWhiteSpace(assessment.Explanation))
        {
            evidence.Add(new MaintenancePredictiveRiskEvidenceReadModel(
                "condition-profile",
                assessment.Id.ToString(),
                assessment.Explanation));
        }

        return evidence;
    }

    public static MaintenancePredictiveRecommendationReadModel? BackfillRecommendation(
        MaintenancePredictiveRiskAssessment assessment,
        MaintenancePredictiveRecommendation? recommendation)
    {
        if (recommendation is not null)
        {
            return new MaintenancePredictiveRecommendationReadModel(
                recommendation.Category,
                recommendation.ReviewPriority,
                recommendation.Explanation,
                recommendation.RuleSetVersion,
                recommendation.GeneratedAtUtc,
                recommendation.Status);
        }

        var (category, explanation) = ResolveFallbackRecommendation(assessment.RiskBand);
        return new MaintenancePredictiveRecommendationReadModel(
            category,
            assessment.ReviewPriority,
            explanation,
            assessment.RuleSetVersion,
            assessment.AssessedAtUtc,
            MaintenancePredictiveRecommendationStatuses.Current);
    }

    public static (string Category, string Explanation) ResolveFallbackRecommendation(string riskBand) =>
        riskBand switch
        {
            MaintenancePredictiveRiskBands.Critical => (
                MaintenancePredictiveRecommendationCategories.UrgentAuthorizedTechnicalReview,
                "Yetkili teknik inceleme acilen planlanmalı; otomatik bakım emri oluşturulmaz."),
            MaintenancePredictiveRiskBands.High => (
                MaintenancePredictiveRecommendationCategories.PlanTechnicalReview,
                "Planlı teknik inceleme önerilir; karar kullanıcı incelemesine bırakılır."),
            MaintenancePredictiveRiskBands.Watch => (
                MaintenancePredictiveRecommendationCategories.CloseMonitoring,
                "Yakın izleme önerilir; tek zayıf sinyal tek başına acil müdahale gerektirmez."),
            MaintenancePredictiveRiskBands.InsufficientData => (
                MaintenancePredictiveRecommendationCategories.ReviewDataQuality,
                "Veri tazeliği, örnek yeterliliği ve kanıt bütünlüğü doğrulanmalı."),
            _ => (
                MaintenancePredictiveRecommendationCategories.ContinueRoutineMonitoring,
                "Rutin izlemeye devam edilmeli; bu sonuç arıza olasılığı veya kesin tahmin değildir."),
        };
}
