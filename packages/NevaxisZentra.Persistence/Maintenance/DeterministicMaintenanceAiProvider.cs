using System.Text.Json;

namespace NevaxisZentra.Persistence.Maintenance;

public sealed class DeterministicMaintenanceAiProvider : IMaintenanceAiProvider
{
    public string ProviderName => MaintenanceAiAgentProviders.DeterministicDevelopment;
    public string ModelName => MaintenanceAiAgentProviders.DeterministicDevelopmentModel;
    public bool IsDevelopmentProvider => true;

    public Task<MaintenanceAiProviderResult> GenerateDraftAsync(
        MaintenanceAiProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.InputSnapshot.AssetReference.Contains("AI-PROVIDER-FAIL", StringComparison.OrdinalIgnoreCase)
            || (request.InputSnapshot.AssetName?.Contains("AI-PROVIDER-FAIL", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return Task.FromResult(new MaintenanceAiProviderResult(
                false,
                MaintenanceAiFailureCodes.ProviderFailed,
                "Deterministic provider failure simulation.",
                null,
                "{}",
                ProviderName,
                ModelName));
        }

        var input = request.InputSnapshot;
        var title = BuildTitle(input);
        var description = BuildDescription(input);
        var priority = MapPriority(input);
        var requestType = MapRequestType(input);
        var confidence = MapConfidence(input);
        var missing = BuildMissingInformation(input);
        var summary = BuildSummary(input, missing);
        var observed = BuildObservedCondition(input);
        var uncertainty = BuildUncertainty(input, missing);
        var safetyNotes = BuildSafetyNotes();
        var evidence = input.EvidenceReferences.Take(6).ToList();

        var output = new MaintenanceAiStructuredOutput(
            summary,
            observed,
            title,
            description,
            priority,
            requestType,
            "authorized-maintenance-review",
            evidence,
            missing,
            uncertainty,
            confidence,
            safetyNotes,
            ProhibitedActionConfirmation: true);

        if (input.AssetReference.Contains("INVALID-AI-OUTPUT", StringComparison.OrdinalIgnoreCase)
            || (input.AssetName?.Contains("INVALID-AI-OUTPUT", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            output = output with { Summary = "Failure probability 87% within 5 days" };
        }

        var raw = JsonSerializer.Serialize(new
        {
            provider = ProviderName,
            model = ModelName,
            promptVersion = request.PromptVersion,
            output,
            disclaimer = "Local deterministic validation provider — not real production model output.",
        });

        return Task.FromResult(new MaintenanceAiProviderResult(
            true,
            null,
            null,
            output,
            raw,
            ProviderName,
            ModelName));
    }

    private static string BuildTitle(MaintenanceAiInputSnapshot input)
    {
        var asset = input.AssetName ?? input.AssetReference;
        return input.RiskBand switch
        {
            MaintenancePredictiveRiskBands.Critical => $"{asset} — acil teknik inceleme taslağı",
            MaintenancePredictiveRiskBands.High => $"{asset} — planlı teknik inceleme taslağı",
            MaintenancePredictiveRiskBands.Watch => $"{asset} — yakın izleme inceleme taslağı",
            _ => $"{asset} — bakım talebi inceleme taslağı",
        };
    }

    private static string BuildDescription(MaintenanceAiInputSnapshot input)
    {
        var lines = new List<string>
        {
            "Bu taslak, güvenilir bakım kanıt özetine dayalı karar destek metnidir.",
            $"Risk bandı: {input.RiskBand}. Veri tazeliği: {input.DataFreshness}. Veri yeterliliği: {input.DataSufficiency}.",
        };

        if (input.RiskDrivers.Count > 0)
        {
            lines.Add("Destekleyici sürücüler:");
            lines.AddRange(input.RiskDrivers.Take(4).Select(d => $"- {d.Summary}"));
        }

        if (input.OpenRequests.Count > 0)
        {
            lines.Add("Açık bakım talepleri mevcut; çoğaltma riski gözden geçirilmeli.");
        }

        if (ContainsPromptInjectionMarker(input))
        {
            lines.Add("Kaynak metinlerde gömülü talimat ifadeleri yok sayıldı; yalnızca kanıt olarak değerlendirildi.");
        }

        return string.Join('\n', lines);
    }

    private static bool ContainsPromptInjectionMarker(MaintenanceAiInputSnapshot input) =>
        input.RiskDrivers.Any(d => d.Summary.Contains("IGNORE PREVIOUS", StringComparison.OrdinalIgnoreCase))
        || input.EvidenceReferences.Any(e => e.Summary.Contains("SYSTEM PROMPT", StringComparison.OrdinalIgnoreCase));

    private static string MapPriority(MaintenanceAiInputSnapshot input) =>
        input.RiskBand switch
        {
            MaintenancePredictiveRiskBands.Critical => "urgent",
            MaintenancePredictiveRiskBands.High => "high",
            MaintenancePredictiveRiskBands.Watch => "medium",
            _ => "low",
        };

    private static string MapRequestType(MaintenanceAiInputSnapshot input) =>
        input.RiskBand is MaintenancePredictiveRiskBands.Critical or MaintenancePredictiveRiskBands.High
            ? "corrective"
            : "inspection";

    private static string MapConfidence(MaintenanceAiInputSnapshot input)
    {
        if (input.DataSufficiency == MaintenanceConditionDataSufficiencyLevels.Insufficient
            || input.DataFreshness == MaintenanceConditionDataFreshnessLevels.Stale)
        {
            return MaintenanceAiQualitativeConfidenceLevels.Low;
        }

        if (input.DataSufficiency == MaintenanceConditionDataSufficiencyLevels.High
            && input.DataFreshness == MaintenanceConditionDataFreshnessLevels.Fresh)
        {
            return MaintenanceAiQualitativeConfidenceLevels.High;
        }

        return MaintenanceAiQualitativeConfidenceLevels.Medium;
    }

    private static List<string> BuildMissingInformation(MaintenanceAiInputSnapshot input)
    {
        var missing = new List<string>();
        if (input.DataFreshness == MaintenanceConditionDataFreshnessLevels.Stale)
        {
            missing.Add("Güncel makine kanıtı");
        }

        if (input.DataSufficiency == MaintenanceConditionDataSufficiencyLevels.Insufficient)
        {
            missing.Add("Yeterli örnek/kanıt seti");
        }

        if (input.EvidenceReferences.Count == 0)
        {
            missing.Add("Yapılandırılmış risk kanıtı");
        }

        return missing;
    }

    private static string BuildSummary(MaintenanceAiInputSnapshot input, List<string> missing) =>
        missing.Count > 0
            ? "Mevcut kanıt sınırlı; bakım talebi taslağı yalnızca yetkili insan incelemesi için hazırlanmıştır."
            : "Mevcut kanıt seti bakım talebi incelemesi için yeterli görünüyor; karar yetkili kullanıcıdadır.";

    private static string BuildObservedCondition(MaintenanceAiInputSnapshot input) =>
        $"Durum: {input.ConditionStatus}; aktif anomali: {input.ActiveAnomalyCount}; deterministik öneri: {input.DeterministicRecommendationCategory}.";

    private static string BuildUncertainty(MaintenanceAiInputSnapshot input, List<string> missing) =>
        missing.Count > 0
            ? "Eksik veya bayat kanıt nedeniyle belirsizlik yüksek; kesin arıza iddiası yapılmaz."
            : "Belirsizlik sınırlı; yine de otomatik bakım emri oluşturulmaz.";

    private static List<string> BuildSafetyNotes() =>
    [
        "AI çıktısı karar desteğidir.",
        "Bakım talebi yalnızca yetkili kullanıcı onayıyla oluşturulur.",
        "AI bakım emri oluşturmaz ve makineye komut göndermez.",
    ];
}
