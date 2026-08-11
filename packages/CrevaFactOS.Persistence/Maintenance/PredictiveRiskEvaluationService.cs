namespace CrevaFactOS.Persistence.Maintenance;

public sealed class PredictiveRiskEvaluationService : IPredictiveRiskEvaluationService
{
    private static readonly HashSet<string> MaterialSeverities = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceAnomalySeverities.Warning,
        MaintenanceAnomalySeverities.High,
        MaintenanceAnomalySeverities.Critical,
    };

    private static readonly HashSet<string> CriticalSeverities = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceAnomalySeverities.High,
        MaintenanceAnomalySeverities.Critical,
    };

    public PredictiveRiskEvaluationResult EvaluateAsset(PredictiveRiskEvaluationInput input)
    {
        var drivers = new List<PredictiveRiskDriverCandidate>();
        var evidence = new List<PredictiveRiskEvidenceCandidate>();
        var historySummary = BuildMaintenanceHistorySummary(input.MaintenanceHistory);

        if (IsInsufficientEvidence(input))
        {
            AddInsufficientEvidence(input, drivers, evidence);
            return BuildResult(
                MaintenancePredictiveRiskBands.InsufficientData,
                MaintenancePredictiveReviewPriorities.Elevated,
                "Güvenilir değerlendirme için yeterli ve güncel kanıt bulunmuyor. Veri kalitesi ve kanıt toplama adımları gözden geçirilmeli.",
                historySummary,
                drivers,
                evidence,
                MaintenancePredictiveRecommendationCategories.ReviewDataQuality,
                "Veri tazeliği, örnek yeterliliği ve kanıt bütünlüğü doğrulanmalı.");
        }

        var materialAnomalies = input.OpenAnomalies
            .Where(a => MaterialSeverities.Contains(a.Severity))
            .ToList();
        var criticalAnomalies = input.OpenAnomalies
            .Where(a => CriticalSeverities.Contains(a.Severity))
            .ToList();
        var persistentSignals = materialAnomalies.Count >= 2 ||
                                HasPersistentTrend(input.TrendState, input.ConditionStatus);
        var alarmMaterial = input.AlarmContext.RecentAlarmCount24h >= 2 ||
                            IsHighSeverityAlarm(input.AlarmContext.HighestSeverity24h);
        var downtimeMaterial = input.DowntimeContext.EpisodeCount24h >= 2 ||
                               input.DowntimeContext.TotalDurationMinutes24h >= 30m;
        var recurringMaintenance = input.MaintenanceHistory.RecentFailureCount90d >= 2;

        AddConditionEvidence(input, drivers, evidence);
        AddAnomalyEvidence(input, materialAnomalies, criticalAnomalies, drivers, evidence);
        AddOperationalContextEvidence(input, alarmMaterial, downtimeMaterial, drivers, evidence);
        AddMaintenanceHistoryEvidence(input, recurringMaintenance, drivers, evidence);

        var corroboratedCritical =
            input.ConditionStatus == MaintenanceConditionStatuses.Critical &&
            (criticalAnomalies.Count >= 1 && (persistentSignals || alarmMaterial || downtimeMaterial || recurringMaintenance) ||
             criticalAnomalies.Count >= 2);

        if (corroboratedCritical)
        {
            drivers.Add(new PredictiveRiskDriverCandidate(
                "corroborated-critical-evidence",
                "Kritik durum kanıtı destekleyici alarm, duruş veya tekrarlayan bakım geçmişi ile doğrulandı.",
                drivers.Count));
            return BuildResult(
                MaintenancePredictiveRiskBands.Critical,
                MaintenancePredictiveReviewPriorities.Urgent,
                "Kritik durum kanıtı destekleyici operasyonel ve bakım geçmişi sinyalleri ile birlikte değerlendirildi. Yetkili teknik inceleme acilen planlanmalı.",
                historySummary,
                drivers,
                evidence,
                MaintenancePredictiveRecommendationCategories.UrgentAuthorizedTechnicalReview,
                "Yetkili teknik inceleme acilen planlanmalı; otomatik bakım emri oluşturulmaz.");
        }

        if (input.ConditionStatus == MaintenanceConditionStatuses.Critical ||
            input.ConditionStatus == MaintenanceConditionStatuses.Degraded ||
            persistentSignals ||
            (materialAnomalies.Count >= 2) ||
            (input.ConditionStatus == MaintenanceConditionStatuses.Degraded &&
             (alarmMaterial || downtimeMaterial || recurringMaintenance)) ||
            (input.TrendState is MaintenanceConditionTrendStates.Increasing or MaintenanceConditionTrendStates.Volatile &&
             materialAnomalies.Count >= 1 &&
             (alarmMaterial || recurringMaintenance)))
        {
            return BuildResult(
                MaintenancePredictiveRiskBands.High,
                MaintenancePredictiveReviewPriorities.Planned,
                "Anlamlı bozulma, çoklu kanıt veya tekrarlayan bakım geçmişi nedeniyle bakım dikkat seviyesi yükseldi. Teknik inceleme planlanmalı.",
                historySummary,
                drivers,
                evidence,
                MaintenancePredictiveRecommendationCategories.PlanTechnicalReview,
                "Planlı teknik inceleme önerilir; karar kullanıcı incelemesine bırakılır.");
        }

        if (input.ConditionStatus == MaintenanceConditionStatuses.Watch ||
            materialAnomalies.Count == 1 ||
            input.TrendState is MaintenanceConditionTrendStates.Increasing or MaintenanceConditionTrendStates.Decreasing)
        {
            return BuildResult(
                MaintenancePredictiveRiskBands.Watch,
                MaintenancePredictiveReviewPriorities.Elevated,
                "Sınırlı sapma veya zayıf sinyal gözlemlendi. Durum yakın izleme gerektiriyor.",
                historySummary,
                drivers,
                evidence,
                MaintenancePredictiveRecommendationCategories.CloseMonitoring,
                "Yakın izleme önerilir; tek zayıf sinyal tek başına acil müdahale gerektirmez.");
        }

        return BuildResult(
            MaintenancePredictiveRiskBands.Low,
            MaintenancePredictiveReviewPriorities.Routine,
            "Güncel kanıt yeterli; belirgin anomali veya anlamlı bozulma sinyali görülmedi. Rutin izleme uygun.",
            historySummary,
            drivers,
            evidence,
            MaintenancePredictiveRecommendationCategories.ContinueRoutineMonitoring,
            "Rutin izlemeye devam edilmeli; bu sonuç arıza olasılığı veya kesin tahmin değildir.");
    }

    private static bool IsInsufficientEvidence(PredictiveRiskEvaluationInput input) =>
        string.IsNullOrWhiteSpace(input.ConditionEvaluationIdentity) ||
        input.DataSufficiency == MaintenanceConditionDataSufficiencyLevels.Insufficient ||
        input.DataFreshness == MaintenanceConditionDataFreshnessLevels.Stale ||
        input.ConditionStatus == MaintenanceConditionStatuses.Unknown;

    private static bool HasPersistentTrend(string trendState, string conditionStatus) =>
        conditionStatus is MaintenanceConditionStatuses.Degraded or MaintenanceConditionStatuses.Critical &&
        trendState is MaintenanceConditionTrendStates.Increasing
            or MaintenanceConditionTrendStates.Decreasing
            or MaintenanceConditionTrendStates.Volatile;

    private static bool IsHighSeverityAlarm(string? severity) =>
        !string.IsNullOrWhiteSpace(severity) &&
        (severity.Contains("high", StringComparison.OrdinalIgnoreCase) ||
         severity.Contains("critical", StringComparison.OrdinalIgnoreCase));

    private static string BuildMaintenanceHistorySummary(ConditionMaintenanceHistoryInput history)
    {
        if (history.RecentFailureCount90d == 0 && history.LastMaintenanceCompletedAtUtc is null)
        {
            return "Son 90 günde ilişkilendirilebilir arıza kaydı bulunmuyor.";
        }

        var parts = new List<string>();
        if (history.RecentFailureCount90d > 0)
        {
            parts.Add($"Son 90 günde {history.RecentFailureCount90d} arıza kaydı");
        }

        if (!string.IsNullOrWhiteSpace(history.LastFailureCategory))
        {
            parts.Add($"son arıza kategorisi {history.LastFailureCategory}");
        }

        if (history.LastMaintenanceCompletedAtUtc is not null)
        {
            parts.Add("son tamamlanan bakım kaydı mevcut");
        }

        return string.Join("; ", parts) + ".";
    }

    private static void AddInsufficientEvidence(
        PredictiveRiskEvaluationInput input,
        List<PredictiveRiskDriverCandidate> drivers,
        List<PredictiveRiskEvidenceCandidate> evidence)
    {
        if (input.DataSufficiency == MaintenanceConditionDataSufficiencyLevels.Insufficient)
        {
            drivers.Add(new PredictiveRiskDriverCandidate(
                "insufficient-data",
                "Durum değerlendirmesi için örnek yeterliliği yetersiz.",
                0));
            evidence.Add(new PredictiveRiskEvidenceCandidate(
                "condition-profile",
                input.ConditionEvaluationIdentity ?? "missing-evaluation",
                "Veri yeterliliği yetersiz olarak işaretlendi.",
                evidence.Count));
        }

        if (input.DataFreshness == MaintenanceConditionDataFreshnessLevels.Stale)
        {
            drivers.Add(new PredictiveRiskDriverCandidate(
                "stale-evidence",
                "Makine kanıtı güncel değil.",
                drivers.Count));
            evidence.Add(new PredictiveRiskEvidenceCandidate(
                "condition-profile",
                input.ConditionEvaluationIdentity ?? "missing-evaluation",
                "Veri tazeliği bayat olarak işaretlendi.",
                evidence.Count));
        }

        if (string.IsNullOrWhiteSpace(input.ConditionEvaluationIdentity))
        {
            drivers.Add(new PredictiveRiskDriverCandidate(
                "missing-evaluation",
                "Güvenli değerlendirme kimliği bulunamadı.",
                drivers.Count));
        }
    }

    private static void AddConditionEvidence(
        PredictiveRiskEvaluationInput input,
        List<PredictiveRiskDriverCandidate> drivers,
        List<PredictiveRiskEvidenceCandidate> evidence)
    {
        drivers.Add(new PredictiveRiskDriverCandidate(
            "condition-status",
            $"Durum profili: {input.ConditionStatus}.",
            drivers.Count));
        evidence.Add(new PredictiveRiskEvidenceCandidate(
            "condition-profile",
            input.ConditionEvaluationIdentity ?? "unknown",
            $"Durum={input.ConditionStatus}, trend={input.TrendState}, yeterlilik={input.DataSufficiency}, tazelik={input.DataFreshness}.",
            evidence.Count));
    }

    private static void AddAnomalyEvidence(
        PredictiveRiskEvaluationInput input,
        List<PredictiveRiskAnomalyInput> materialAnomalies,
        List<PredictiveRiskAnomalyInput> criticalAnomalies,
        List<PredictiveRiskDriverCandidate> drivers,
        List<PredictiveRiskEvidenceCandidate> evidence)
    {
        if (input.ActiveAnomalyCount > 0)
        {
            drivers.Add(new PredictiveRiskDriverCandidate(
                "active-anomaly",
                $"{input.ActiveAnomalyCount} aktif anomali kaydı izleniyor.",
                drivers.Count));
        }

        foreach (var anomaly in materialAnomalies.Take(5))
        {
            evidence.Add(new PredictiveRiskEvidenceCandidate(
                "condition-anomaly",
                anomaly.EvaluationIdentity,
                $"{anomaly.MetricKey}: {anomaly.EvidenceSummary}",
                evidence.Count));
        }

        if (criticalAnomalies.Count >= 2)
        {
            drivers.Add(new PredictiveRiskDriverCandidate(
                "multiple-critical-anomalies",
                "Birden fazla yüksek/kritik anomali sinyali birlikte izleniyor.",
                drivers.Count));
        }
        else if (materialAnomalies.Count >= 2)
        {
            drivers.Add(new PredictiveRiskDriverCandidate(
                "persistent-anomaly",
                "Birden fazla anomali sinyali kalıcı sapma eğilimini destekliyor.",
                drivers.Count));
        }
    }

    private static void AddOperationalContextEvidence(
        PredictiveRiskEvaluationInput input,
        bool alarmMaterial,
        bool downtimeMaterial,
        List<PredictiveRiskDriverCandidate> drivers,
        List<PredictiveRiskEvidenceCandidate> evidence)
    {
        if (input.AlarmContext.RecentAlarmCount24h > 0)
        {
            evidence.Add(new PredictiveRiskEvidenceCandidate(
                "edge-alarm",
                "alarm-24h",
                $"Son 24 saatte {input.AlarmContext.RecentAlarmCount24h} alarm gözlemi.",
                evidence.Count));
        }

        if (alarmMaterial)
        {
            drivers.Add(new PredictiveRiskDriverCandidate(
                "alarm-context",
                "Son 24 saatte anlamlı alarm aktivitesi gözlendi.",
                drivers.Count));
        }

        if (input.DowntimeContext.EpisodeCount24h > 0)
        {
            evidence.Add(new PredictiveRiskEvidenceCandidate(
                "edge-downtime",
                "downtime-24h",
                $"Son 24 saatte {input.DowntimeContext.EpisodeCount24h} duruş olayı.",
                evidence.Count));
        }

        if (downtimeMaterial)
        {
            drivers.Add(new PredictiveRiskDriverCandidate(
                "downtime-context",
                "Son 24 saatte anlamlı duruş aktivitesi gözlendi.",
                drivers.Count));
        }
    }

    private static void AddMaintenanceHistoryEvidence(
        PredictiveRiskEvaluationInput input,
        bool recurringMaintenance,
        List<PredictiveRiskDriverCandidate> drivers,
        List<PredictiveRiskEvidenceCandidate> evidence)
    {
        if (input.MaintenanceHistory.RecentFailureCount90d > 0)
        {
            evidence.Add(new PredictiveRiskEvidenceCandidate(
                "maintenance-history",
                "fault-90d",
                $"Son 90 günde {input.MaintenanceHistory.RecentFailureCount90d} arıza kaydı.",
                evidence.Count));
        }

        if (input.MaintenanceHistory.LastMaintenanceCompletedAtUtc is not null)
        {
            evidence.Add(new PredictiveRiskEvidenceCandidate(
                "maintenance-history",
                "last-maintenance",
                "Son tamamlanan bakım kaydı mevcut.",
                evidence.Count));
        }

        if (recurringMaintenance)
        {
            drivers.Add(new PredictiveRiskDriverCandidate(
                "maintenance-history",
                "Son 90 günde tekrarlayan arıza/bakım geçmişi gözlendi.",
                drivers.Count));
        }
    }

    private static PredictiveRiskEvaluationResult BuildResult(
        string riskBand,
        string reviewPriority,
        string explanation,
        string historySummary,
        IReadOnlyList<PredictiveRiskDriverCandidate> drivers,
        IReadOnlyList<PredictiveRiskEvidenceCandidate> evidence,
        string recommendationCategory,
        string recommendationExplanation) =>
        new(
            riskBand,
            reviewPriority,
            explanation,
            historySummary,
            drivers,
            evidence,
            recommendationCategory,
            recommendationExplanation);
}
