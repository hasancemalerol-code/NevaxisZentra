using System.Globalization;

namespace CrevaFactOS.Persistence.Material;

public static class MaterialAvailabilityRiskFollowUpPresentationBuilder
{
    public static string BuildTaskDraftIdempotencyKey(Guid assessmentId, string taskPurpose) =>
        $"material-availability-risk-task:{assessmentId:D}:{taskPurpose}";

    public static string BuildMaterialRiskDeepLink(Guid assessmentId) =>
        $"/stok-ve-depo/malzeme-riskleri?assessment={Uri.EscapeDataString(assessmentId.ToString())}";

    public static bool IsTerminalFollowUp(string followUpStatus) =>
        MaterialAvailabilityRiskFollowUpStatuses.Terminal.Contains(followUpStatus);

    public static bool HasCompleteShortageContext(MaterialRequirementAssessment assessment)
    {
        if (!string.Equals(
                assessment.Status,
                MaterialAvailabilityRiskStatuses.Shortage,
                StringComparison.Ordinal))
        {
            return false;
        }

        if (!assessment.TheoreticalRequirementGrams.HasValue
            || !assessment.ErpAvailableQuantity.HasValue
            || !assessment.ErpReservedQuantity.HasValue
            || !assessment.AvailableAfterReservationQuantity.HasValue
            || !assessment.ShortageQuantity.HasValue
            || assessment.ShortageQuantity.Value <= 0m
            || string.IsNullOrWhiteSpace(assessment.ErpUnit))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(assessment.Material?.Code);
    }

    public static bool IsDataReviewEligible(MaterialRequirementAssessment assessment) =>
        assessment.Status is MaterialAvailabilityRiskStatuses.InsufficientData
            or MaterialAvailabilityRiskStatuses.SourceConflict;

    public static string ResolveTaskPurpose(MaterialRequirementAssessment assessment)
    {
        if (IsDataReviewEligible(assessment))
        {
            return MaterialAvailabilityRiskFollowUpConstants.TaskPurposeDataReviewDraft;
        }

        return MaterialAvailabilityRiskFollowUpConstants.TaskPurposeHumanReviewDraft;
    }

    public static bool CanCreateTaskDraft(MaterialRequirementAssessment assessment)
    {
        if (assessment.AssessmentType != MaterialRequirementAssessmentType.AvailabilityRisk)
        {
            return false;
        }

        if (assessment.LinkedAiTaskId.HasValue)
        {
            return false;
        }

        if (IsTerminalFollowUp(assessment.FollowUpStatus))
        {
            return false;
        }

        if (string.Equals(
                assessment.Status,
                MaterialAvailabilityRiskStatuses.Sufficient,
                StringComparison.Ordinal))
        {
            return false;
        }

        return HasCompleteShortageContext(assessment) || IsDataReviewEligible(assessment);
    }

    public static string ResolveTaskPriority(string status) =>
        status switch
        {
            MaterialAvailabilityRiskStatuses.Shortage => "high",
            MaterialAvailabilityRiskStatuses.SourceConflict => "high",
            MaterialAvailabilityRiskStatuses.InsufficientData => "medium",
            _ => "low",
        };

    public static string ResolveTaskPriority(MaterialRequirementAssessment assessment) =>
        ResolveTaskPriority(assessment.Status);

    public static string BuildTaskTitle(MaterialRequirementAssessment assessment, string taskPurpose)
    {
        var label = assessment.Material?.Code ?? assessment.MaterialId.ToString("D");
        return taskPurpose == MaterialAvailabilityRiskFollowUpConstants.TaskPurposeDataReviewDraft
            ? $"Veri inceleme görev taslağı — {label}"
            : $"Stok ve malzeme inceleme görev taslağı — {label}";
    }

    public static string BuildTaskSummary(MaterialRequirementAssessment assessment)
    {
        var parts = new List<string>
        {
            $"Malzeme: {assessment.Material?.Code ?? assessment.MaterialId.ToString("D")}",
            $"Durum: {assessment.Status}",
        };

        if (assessment.TheoreticalRequirementGrams.HasValue)
        {
            parts.Add(
                $"Gerekli: {assessment.TheoreticalRequirementGrams.Value.ToString("F4", CultureInfo.InvariantCulture)} g");
        }

        if (assessment.ErpAvailableQuantity.HasValue && !string.IsNullOrWhiteSpace(assessment.ErpUnit))
        {
            parts.Add(
                $"ERP kullanılabilir: {assessment.ErpAvailableQuantity.Value.ToString("F4", CultureInfo.InvariantCulture)} {assessment.ErpUnit}");
        }

        if (assessment.ErpReservedQuantity.HasValue && !string.IsNullOrWhiteSpace(assessment.ErpUnit))
        {
            parts.Add(
                $"ERP rezerve: {assessment.ErpReservedQuantity.Value.ToString("F4", CultureInfo.InvariantCulture)} {assessment.ErpUnit}");
        }

        if (assessment.ShortageQuantity.HasValue && !string.IsNullOrWhiteSpace(assessment.ErpUnit))
        {
            parts.Add(
                $"Eksik: {assessment.ShortageQuantity.Value.ToString("F4", CultureInfo.InvariantCulture)} {assessment.ErpUnit}");
        }

        if (!string.IsNullOrWhiteSpace(assessment.SourceFreshness))
        {
            parts.Add($"Kaynak tazeliği: {assessment.SourceFreshness}");
        }

        return string.Join(". ", parts);
    }

    public static string BuildTaskSummary(MaterialAvailabilityRiskDetailReadModel assessment)
    {
        var parts = new List<string>
        {
            $"Malzeme: {assessment.MaterialCode}",
            $"Durum: {assessment.Status}",
        };

        if (assessment.RequiredQuantityGrams.HasValue)
        {
            parts.Add(
                $"Gerekli: {assessment.RequiredQuantityGrams.Value.ToString("F4", CultureInfo.InvariantCulture)} g");
        }

        if (assessment.ErpAvailableQuantity.HasValue && !string.IsNullOrWhiteSpace(assessment.ErpUnit))
        {
            parts.Add(
                $"ERP kullanılabilir: {assessment.ErpAvailableQuantity.Value.ToString("F4", CultureInfo.InvariantCulture)} {assessment.ErpUnit}");
        }

        if (assessment.ErpReservedQuantity.HasValue && !string.IsNullOrWhiteSpace(assessment.ErpUnit))
        {
            parts.Add(
                $"ERP rezerve: {assessment.ErpReservedQuantity.Value.ToString("F4", CultureInfo.InvariantCulture)} {assessment.ErpUnit}");
        }

        if (assessment.ShortageQuantity.HasValue && !string.IsNullOrWhiteSpace(assessment.ErpUnit))
        {
            parts.Add(
                $"Eksik: {assessment.ShortageQuantity.Value.ToString("F4", CultureInfo.InvariantCulture)} {assessment.ErpUnit}");
        }

        if (!string.IsNullOrWhiteSpace(assessment.SourceFreshness))
        {
            parts.Add($"Kaynak tazeliği: {assessment.SourceFreshness}");
        }

        return string.Join(". ", parts);
    }

    public static string BuildEvidenceSummary(MaterialRequirementAssessment assessment)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(assessment.RequirementProfile?.ProductionOrderReference))
        {
            parts.Add($"Üretim emri: {assessment.RequirementProfile.ProductionOrderReference}");
        }

        if (!string.IsNullOrWhiteSpace(assessment.SourceFreshness))
        {
            parts.Add($"Kaynak tazeliği: {assessment.SourceFreshness}");
        }

        if (assessment.ErpSourceUpdatedAtUtc.HasValue)
        {
            parts.Add($"ERP güncelleme: {assessment.ErpSourceUpdatedAtUtc.Value:O}");
        }

        return string.Join(" | ", parts.Where(p => !string.IsNullOrWhiteSpace(p)).Take(3));
    }

    public static bool IsValidFollowUpTransition(string currentStatus, string targetStatus)
    {
        if (!MaterialAvailabilityRiskFollowUpStatuses.All.Contains(targetStatus))
        {
            return false;
        }

        if (MaterialAvailabilityRiskFollowUpStatuses.Terminal.Contains(currentStatus))
        {
            return false;
        }

        return targetStatus switch
        {
            MaterialAvailabilityRiskFollowUpStatuses.Acknowledged =>
                currentStatus == MaterialAvailabilityRiskFollowUpStatuses.RequiresReview,
            MaterialAvailabilityRiskFollowUpStatuses.TaskCreated =>
                currentStatus is MaterialAvailabilityRiskFollowUpStatuses.RequiresReview
                    or MaterialAvailabilityRiskFollowUpStatuses.Acknowledged,
            MaterialAvailabilityRiskFollowUpStatuses.Resolved =>
                currentStatus == MaterialAvailabilityRiskFollowUpStatuses.TaskCreated,
            MaterialAvailabilityRiskFollowUpStatuses.Dismissed =>
                currentStatus is MaterialAvailabilityRiskFollowUpStatuses.RequiresReview
                    or MaterialAvailabilityRiskFollowUpStatuses.Acknowledged,
            _ => false,
        };
    }
}
