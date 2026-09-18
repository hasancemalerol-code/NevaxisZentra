using System.Globalization;

namespace NevaxisZentra.Persistence.Procurement;

public static class ProcurementDeliveryRiskFollowUpPresentationBuilder
{
    public static string BuildTaskDraftIdempotencyKey(Guid assessmentId, string taskPurpose) =>
        $"procurement-delivery-risk-task:{assessmentId:D}:{taskPurpose}";

    public static string BuildProcurementRiskDeepLink(Guid assessmentId) =>
        $"/satin-alma/tedarik-riskleri?assessment={Uri.EscapeDataString(assessmentId.ToString())}";

    public static bool IsTerminalFollowUp(string followUpStatus) =>
        ProcurementDeliveryRiskFollowUpStatuses.Terminal.Contains(followUpStatus);

    public static bool IsDataReviewEligible(ProcurementDeliveryRiskAssessment assessment) =>
        assessment.Result is ProcurementDeliveryRiskStatuses.InsufficientData
            or ProcurementDeliveryRiskStatuses.SourceConflict;

    public static bool HasCompleteProcurementReviewContext(ProcurementDeliveryRiskAssessment assessment)
    {
        if (assessment.Result is not (
                ProcurementDeliveryRiskStatuses.Overdue
                or ProcurementDeliveryRiskStatuses.DeliveryRisk))
        {
            return false;
        }

        if (!assessment.CommitmentDateUtc.HasValue)
        {
            return false;
        }

        return assessment.OpenQuantity is > 0m;
    }

    public static string ResolveTaskPurpose(ProcurementDeliveryRiskAssessment assessment)
    {
        if (IsDataReviewEligible(assessment))
        {
            return ProcurementDeliveryRiskFollowUpConstants.TaskPurposeDataReviewDraft;
        }

        return ProcurementDeliveryRiskFollowUpConstants.TaskPurposeHumanReviewDraft;
    }

    public static bool CanCreateTaskDraft(ProcurementDeliveryRiskAssessment assessment)
    {
        if (assessment.LinkedAiTaskId.HasValue)
        {
            return false;
        }

        if (IsTerminalFollowUp(assessment.FollowUpStatus))
        {
            return false;
        }

        if (string.Equals(assessment.Result, ProcurementDeliveryRiskStatuses.OnTrack, StringComparison.Ordinal))
        {
            return false;
        }

        return HasCompleteProcurementReviewContext(assessment) || IsDataReviewEligible(assessment);
    }

    public static string ResolveTaskPriority(string result) =>
        result switch
        {
            ProcurementDeliveryRiskStatuses.Overdue => "high",
            ProcurementDeliveryRiskStatuses.DeliveryRisk => "high",
            ProcurementDeliveryRiskStatuses.SourceConflict => "high",
            ProcurementDeliveryRiskStatuses.InsufficientData => "medium",
            _ => "low",
        };

    public static string BuildTaskTitle(ProcurementDeliveryRiskAssessment assessment, string taskPurpose)
    {
        var label = assessment.PurchaseOrderNumber;
        return taskPurpose == ProcurementDeliveryRiskFollowUpConstants.TaskPurposeDataReviewDraft
            ? $"Veri inceleme görev taslağı — {label}"
            : $"Tedarik inceleme görev taslağı — {label}";
    }

    public static string BuildTaskSummary(ProcurementDeliveryRiskAssessment assessment)
    {
        var parts = new List<string>
        {
            $"Satın alma: {assessment.PurchaseOrderNumber}",
            $"Durum: {assessment.Result}",
        };

        if (!string.IsNullOrWhiteSpace(assessment.SupplierName))
        {
            parts.Add($"Tedarikçi: {assessment.SupplierName}");
        }
        else if (!string.IsNullOrWhiteSpace(assessment.ExternalSupplierCode))
        {
            parts.Add($"Tedarikçi kodu: {assessment.ExternalSupplierCode}");
        }

        if (assessment.CommitmentDateUtc.HasValue)
        {
            parts.Add($"Taahhüt: {assessment.CommitmentDateUtc.Value:O}");
        }

        if (assessment.ReceivedQuantity.HasValue && !string.IsNullOrWhiteSpace(assessment.Unit))
        {
            parts.Add(
                $"Teslim alınan: {assessment.ReceivedQuantity.Value.ToString("F4", CultureInfo.InvariantCulture)} {assessment.Unit}");
        }

        if (assessment.OpenQuantity.HasValue && !string.IsNullOrWhiteSpace(assessment.Unit))
        {
            parts.Add(
                $"Açık miktar: {assessment.OpenQuantity.Value.ToString("F4", CultureInfo.InvariantCulture)} {assessment.Unit}");
        }

        if (!string.IsNullOrWhiteSpace(assessment.SourceFreshness))
        {
            parts.Add($"Kaynak tazeliği: {assessment.SourceFreshness}");
        }

        return string.Join(". ", parts);
    }

    public static string BuildTaskSummary(ProcurementDeliveryRiskDetailReadModel assessment)
    {
        var parts = new List<string>
        {
            $"Satın alma: {assessment.PurchaseOrderNumber}",
            $"Durum: {assessment.Result}",
        };

        if (!string.IsNullOrWhiteSpace(assessment.SupplierName))
        {
            parts.Add($"Tedarikçi: {assessment.SupplierName}");
        }
        else if (!string.IsNullOrWhiteSpace(assessment.ExternalSupplierCode))
        {
            parts.Add($"Tedarikçi kodu: {assessment.ExternalSupplierCode}");
        }

        if (assessment.CommitmentDateUtc.HasValue)
        {
            parts.Add($"Taahhüt: {assessment.CommitmentDateUtc.Value:O}");
        }

        if (assessment.ReceivedQuantity.HasValue && !string.IsNullOrWhiteSpace(assessment.Unit))
        {
            parts.Add(
                $"Teslim alınan: {assessment.ReceivedQuantity.Value.ToString("F4", CultureInfo.InvariantCulture)} {assessment.Unit}");
        }

        if (assessment.OpenQuantity.HasValue && !string.IsNullOrWhiteSpace(assessment.Unit))
        {
            parts.Add(
                $"Açık miktar: {assessment.OpenQuantity.Value.ToString("F4", CultureInfo.InvariantCulture)} {assessment.Unit}");
        }

        if (!string.IsNullOrWhiteSpace(assessment.SourceFreshness))
        {
            parts.Add($"Kaynak tazeliği: {assessment.SourceFreshness}");
        }

        return string.Join(". ", parts);
    }

    public static string BuildEvidenceSummary(ProcurementDeliveryRiskAssessment assessment)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(assessment.SupplierName))
        {
            parts.Add($"Tedarikçi: {assessment.SupplierName}");
        }

        parts.Add($"Satın alma: {assessment.PurchaseOrderNumber}");

        if (assessment.CommitmentDateUtc.HasValue)
        {
            parts.Add($"Taahhüt: {assessment.CommitmentDateUtc.Value:O}");
        }

        if (!string.IsNullOrWhiteSpace(assessment.SourceFreshness))
        {
            parts.Add($"Kaynak tazeliği: {assessment.SourceFreshness}");
        }

        return string.Join(" | ", parts.Where(p => !string.IsNullOrWhiteSpace(p)).Take(4));
    }

    public static bool IsValidFollowUpTransition(string currentStatus, string targetStatus)
    {
        if (!ProcurementDeliveryRiskFollowUpStatuses.All.Contains(targetStatus))
        {
            return false;
        }

        if (ProcurementDeliveryRiskFollowUpStatuses.Terminal.Contains(currentStatus))
        {
            return false;
        }

        return targetStatus switch
        {
            ProcurementDeliveryRiskFollowUpStatuses.Acknowledged =>
                currentStatus == ProcurementDeliveryRiskFollowUpStatuses.RequiresReview,
            ProcurementDeliveryRiskFollowUpStatuses.TaskCreated =>
                currentStatus is ProcurementDeliveryRiskFollowUpStatuses.RequiresReview
                    or ProcurementDeliveryRiskFollowUpStatuses.Acknowledged,
            ProcurementDeliveryRiskFollowUpStatuses.Resolved =>
                currentStatus == ProcurementDeliveryRiskFollowUpStatuses.TaskCreated,
            ProcurementDeliveryRiskFollowUpStatuses.Dismissed =>
                currentStatus is ProcurementDeliveryRiskFollowUpStatuses.RequiresReview
                    or ProcurementDeliveryRiskFollowUpStatuses.Acknowledged,
            _ => false,
        };
    }
}
