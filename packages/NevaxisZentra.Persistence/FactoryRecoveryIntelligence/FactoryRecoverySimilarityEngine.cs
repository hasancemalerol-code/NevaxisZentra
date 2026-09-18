using NevaxisZentra.Persistence.FactoryOutcomeExecution;

namespace NevaxisZentra.Persistence.FactoryRecoveryIntelligence;

public static class FactoryRecoverySimilarityEngine
{
    public static FactoryRecoverySimilarCaseMatch Score(
        FactoryRecoveryPattern current,
        FactoryOutcomeLearningRecord record)
    {
        var historical = FactoryRecoveryPatternEngine.Deserialize(record.PatternJson);
        var reasons = new List<string>();
        var score = 0;

        if (string.Equals(current.OutcomeType, record.OutcomeType, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(current.OutcomeType, historical.OutcomeType, StringComparison.OrdinalIgnoreCase))
        {
            score += 25;
            reasons.Add("aynı sonuç riski tipi");
        }

        if (string.Equals(current.EventCategory, record.EventCategory, StringComparison.OrdinalIgnoreCase))
        {
            score += 20;
            reasons.Add("aynı olay kategorisi");
        }

        if (MachineFamilyMatch(current.MachineCode, record.MachineCode ?? historical.MachineCode))
        {
            score += 20;
            reasons.Add("aynı makine / makine ailesi");
        }

        if (!string.IsNullOrWhiteSpace(current.OrderReference) &&
            string.Equals(current.OrderReference, record.OrderReference, StringComparison.OrdinalIgnoreCase))
        {
            score += 15;
            reasons.Add("aynı sipariş bağlamı");
        }

        if (!string.IsNullOrWhiteSpace(current.MaterialCode) &&
            string.Equals(current.MaterialCode, record.MaterialCode, StringComparison.OrdinalIgnoreCase))
        {
            score += 10;
            reasons.Add("aynı malzeme bağımlılığı");
        }

        if (record.VerifiedAtUtc >= DateTimeOffset.UtcNow.AddDays(-30))
        {
            score += 5;
            reasons.Add("yakın tarihli doğrulanmış vaka");
        }

        score = Math.Min(100, score);
        if (reasons.Count == 0)
        {
            reasons.Add("yapılandırılmış desen eşleşmesi");
            score = Math.Max(score, 40);
        }

        var deliveryProtected = record.DeliveryProtected;
        var outcomeSummary = deliveryProtected == true
            ? "Teslimat korundu"
            : record.LearningDisposition == FactoryRecoveryLearningDisposition.VerifiedSuccess
                ? "Sonuç doğrulandı"
                : "Kısmi sonuç";

        return new FactoryRecoverySimilarCaseMatch(
            record.Id,
            record.SourceCaseId,
            score,
            reasons,
            record.OrderReference ?? "—",
            record.MachineCode ?? "—",
            record.CausePattern,
            record.SelectedStrategyKey,
            outcomeSummary,
            record.TimeToRecoveryMinutes,
            deliveryProtected,
            record.VerifiedAtUtc);
    }

    public static string ResolveEvidenceStrength(int sampleSize, int similarCount)
    {
        if (sampleSize >= 10 || similarCount >= 5)
        {
            return FactoryRecoveryEvidenceStrength.Strong;
        }

        if (sampleSize >= 3 || similarCount >= 2)
        {
            return FactoryRecoveryEvidenceStrength.Medium;
        }

        return FactoryRecoveryEvidenceStrength.Low;
    }

    public static string ResolveSampleConfidence(int verifiedUses)
    {
        if (verifiedUses >= 10)
        {
            return FactoryRecoverySampleConfidence.Strong;
        }

        if (verifiedUses >= 3)
        {
            return FactoryRecoverySampleConfidence.Medium;
        }

        return FactoryRecoverySampleConfidence.Low;
    }

    private static bool MachineFamilyMatch(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var leftFamily = left.Split('-')[0];
        var rightFamily = right.Split('-')[0];
        return string.Equals(leftFamily, rightFamily, StringComparison.OrdinalIgnoreCase);
    }
}
