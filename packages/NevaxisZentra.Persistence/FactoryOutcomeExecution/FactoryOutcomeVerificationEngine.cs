using System.Globalization;
using System.Text.Json;
using NevaxisZentra.Persistence.DigitalWorkforce;
using NevaxisZentra.Persistence.FactoryDecisionCenter;

namespace NevaxisZentra.Persistence.FactoryOutcomeExecution;

public static class FactoryOutcomeVerificationEngine
{
    public static FactoryOutcomeVerificationSnapshot Evaluate(
        DigitalWorkCase rootCase,
        IReadOnlyList<DigitalWorkCase> childCases,
        IReadOnlyList<DigitalWorkVerification> verifications,
        int? measuredProjection,
        int? requiredQuantity)
    {
        var metadata = FactoryDecisionCenterProjectionEngine.ParseMetadata(rootCase.MetadataJson);
        var required = requiredQuantity ??
                       (metadata.TryGetValue(FactoryDecisionCenterMetadataKeys.RequiredQuantity, out var requiredEl) &&
                        requiredEl.TryGetInt32(out var parsedRequired)
                           ? parsedRequired
                           : null);
        var measured = measuredProjection ??
                       (metadata.TryGetValue(FactoryDecisionCenterMetadataKeys.VerifiedProjection, out var verifiedEl) &&
                        verifiedEl.TryGetInt32(out var parsedVerified)
                           ? parsedVerified
                           : metadata.TryGetValue(FactoryDecisionCenterMetadataKeys.ProjectedQuantity, out var projectedEl) &&
                             projectedEl.TryGetInt32(out var parsedProjected)
                               ? parsedProjected
                               : null);

        var criteria = new List<FactoryOutcomeVerificationCriterion>
        {
            BuildQuantityCriterion(required, measured),
            BuildChildCompletionCriterion(childCases),
            BuildApprovalCriterion(rootCase),
            BuildEvidenceCriterion(verifications),
        };

        var passed = criteria.All(c => string.Equals(c.Result, DigitalWorkVerificationResults.Passed, StringComparison.OrdinalIgnoreCase));
        var summary = passed
            ? "Sonuç doğrulandı; iş sonucu korunuyor."
            : "Sonuç doğrulaması başarısız; vaka açık kalıyor.";

        return new FactoryOutcomeVerificationSnapshot(passed, criteria, summary);
    }

    public static string ToJson(FactoryOutcomeVerificationSnapshot snapshot) =>
        JsonSerializer.Serialize(new
        {
            passed = snapshot.Passed,
            summary = snapshot.Summary,
            criteria = snapshot.Criteria.Select(c => new
            {
                code = c.Code,
                label = c.Label,
                expectedValue = c.ExpectedValue,
                measuredValue = c.MeasuredValue,
                threshold = c.Threshold,
                result = c.Result,
                evidenceSummary = c.EvidenceSummary,
                recordedAtUtc = c.RecordedAtUtc,
            }),
        });

    public static bool CanCloseOutcomeCase(
        DigitalWorkCase rootCase,
        IReadOnlyList<DigitalWorkCase> childCases,
        IReadOnlyList<DigitalWorkVerification> verifications,
        FactoryOutcomeVerificationSnapshot snapshot)
    {
        if (rootCase.Status != DigitalWorkCaseStatuses.Verifying &&
            rootCase.Status != DigitalWorkCaseStatuses.Executing)
        {
            return false;
        }

        if (childCases.Any(c => c.Status != DigitalWorkCaseStatuses.Closed))
        {
            return false;
        }

        if (rootCase.Approvals.Any(a => a.Status == DigitalWorkApprovalStatuses.Pending))
        {
            return false;
        }

        if (!snapshot.Passed)
        {
            return false;
        }

        return DigitalWorkforceVerificationEngine.CanCloseCase(rootCase, verifications);
    }

    private static FactoryOutcomeVerificationCriterion BuildQuantityCriterion(int? required, int? measured)
    {
        if (!required.HasValue || !measured.HasValue)
        {
            return new FactoryOutcomeVerificationCriterion(
                "quantity-protected",
                "Gerekli çıktı korunuyor",
                required?.ToString(CultureInfo.InvariantCulture) ?? "n/a",
                measured?.ToString(CultureInfo.InvariantCulture),
                required?.ToString(CultureInfo.InvariantCulture),
                DigitalWorkVerificationResults.Pending,
                null,
                null);
        }

        var passed = measured.Value >= required.Value;
        return new FactoryOutcomeVerificationCriterion(
            "quantity-protected",
            "Gerekli çıktı korunuyor",
            required.Value.ToString(CultureInfo.InvariantCulture),
            measured.Value.ToString(CultureInfo.InvariantCulture),
            required.Value.ToString(CultureInfo.InvariantCulture),
            passed ? DigitalWorkVerificationResults.Passed : DigitalWorkVerificationResults.Failed,
            passed ? "Projeksiyon taahhüdü karşılıyor" : "Projeksiyon taahhüdün altında",
            DateTimeOffset.UtcNow);
    }

    private static FactoryOutcomeVerificationCriterion BuildChildCompletionCriterion(IReadOnlyList<DigitalWorkCase> childCases)
    {
        var openChildren = childCases.Count(c => c.Status != DigitalWorkCaseStatuses.Closed);
        return new FactoryOutcomeVerificationCriterion(
            "actions-complete",
            "Kurtarma aksiyonları tamamlandı",
            "0 açık aksiyon",
            openChildren.ToString(CultureInfo.InvariantCulture),
            "0",
            openChildren == 0 ? DigitalWorkVerificationResults.Passed : DigitalWorkVerificationResults.Failed,
            openChildren == 0 ? "Tüm alt aksiyonlar kapandı" : $"{openChildren} aksiyon açık",
            DateTimeOffset.UtcNow);
    }

    private static FactoryOutcomeVerificationCriterion BuildApprovalCriterion(DigitalWorkCase rootCase)
    {
        var pending = rootCase.Approvals.Any(a => a.Status == DigitalWorkApprovalStatuses.Pending);
        return new FactoryOutcomeVerificationCriterion(
            "approvals-resolved",
            "Gerekli insan kararları çözüldü",
            "Onay beklenmiyor",
            pending ? "Bekliyor" : "Çözüldü",
            "Çözüldü",
            pending ? DigitalWorkVerificationResults.Failed : DigitalWorkVerificationResults.Passed,
            pending ? "İnsan onayı bekleniyor" : "Onaylar tamamlandı",
            DateTimeOffset.UtcNow);
    }

    private static FactoryOutcomeVerificationCriterion BuildEvidenceCriterion(IReadOnlyList<DigitalWorkVerification> verifications)
    {
        var passed = verifications.Any(v =>
            string.Equals(v.Result, DigitalWorkVerificationResults.Passed, StringComparison.OrdinalIgnoreCase));
        return new FactoryOutcomeVerificationCriterion(
            "evidence-present",
            "Kanıt paketi mevcut",
            "Doğrulanmış kanıt",
            passed ? "Mevcut" : "Eksik",
            "Mevcut",
            passed ? DigitalWorkVerificationResults.Passed : DigitalWorkVerificationResults.Failed,
            passed ? "Doğrulama kaydı bulundu" : "Doğrulama kaydı eksik",
            DateTimeOffset.UtcNow);
    }
}
