namespace CrevaFactOS.Persistence.Quality;

public static class QualityFollowUpPresentationBuilder
{
    public static string BuildTaskDraftIdempotencyKey(Guid ncrId, string taskPurpose) =>
        $"quality-follow-up-task:{ncrId:D}:{taskPurpose}";

    public static bool HasInsufficientEvidence(QualityNonconformance ncr) =>
        !ncr.QualityInspectionId.HasValue
        && !ncr.QualityInspectionResultId.HasValue
        && !ncr.ProductionOrderId.HasValue
        && string.IsNullOrWhiteSpace(ncr.ProductReference);

    public static bool HasConfirmedQualityIssue(QualityNonconformance ncr) =>
        ncr.QualityInspectionId.HasValue
        || ncr.QualityInspectionResultId.HasValue
        || ncr.ProductionOrderId.HasValue
        || !string.IsNullOrWhiteSpace(ncr.ProductReference)
        || ncr.HoldStatus == QualityNcrHoldStatus.Active
        || ncr.Severity is QualityNcrSeverity.Major or QualityNcrSeverity.Critical;

    public static bool IsEligibleForFollowUp(QualityNonconformance ncr) =>
        ncr.Status is QualityNcrStatus.Open or QualityNcrStatus.UnderReview or QualityNcrStatus.ActionRequired
        && !QualityNcrFollowUpStatuses.Terminal.Contains(ncr.FollowUpStatus);

    public static string ResolveTaskPurpose(QualityNonconformance ncr) =>
        HasInsufficientEvidence(ncr)
            ? QualityFollowUpConstants.TaskPurposeDataReviewDraft
            : QualityFollowUpConstants.TaskPurposeHumanReviewDraft;

    public static bool CanCreateTaskDraft(QualityNonconformance ncr)
    {
        if (ncr.LinkedAiTaskId.HasValue)
        {
            return false;
        }

        if (!IsEligibleForFollowUp(ncr))
        {
            return false;
        }

        return HasInsufficientEvidence(ncr) || HasConfirmedQualityIssue(ncr);
    }

    public static string BuildTaskTitle(QualityNonconformance ncr)
    {
        var label = !string.IsNullOrWhiteSpace(ncr.NcrNumber) ? ncr.NcrNumber : ncr.Title;
        return HasInsufficientEvidence(ncr)
            ? $"Kalite veri incelemesi — {label}"
            : $"Kalite uygunsuzluk incelemesi — {label}";
    }

    public static string BuildTaskSummary(QualityNonconformance ncr) =>
        $"NCR: {ncr.NcrNumber}. Durum: {QualityValueConversions.ToStorage(ncr.Status)}. " +
        $"Şiddet: {QualityValueConversions.ToStorage(ncr.Severity)}. {ncr.Title}";

    public static string BuildEvidenceSummary(QualityNonconformance ncr)
    {
        var parts = new List<string> { ncr.Description };
        if (!string.IsNullOrWhiteSpace(ncr.ProductReference))
        {
            parts.Add($"Ürün: {ncr.ProductReference}");
        }

        if (!string.IsNullOrWhiteSpace(ncr.OperationReference))
        {
            parts.Add($"Operasyon: {ncr.OperationReference}");
        }

        if (ncr.QualityInspectionId.HasValue)
        {
            parts.Add($"Muayene: {ncr.QualityInspectionId.Value:D}");
        }

        return string.Join(" | ", parts.Where(p => !string.IsNullOrWhiteSpace(p)).Take(3));
    }

    public static string ResolveTaskPriority(QualityNonconformance ncr) =>
        ncr.Severity switch
        {
            QualityNcrSeverity.Critical => "critical",
            QualityNcrSeverity.Major => "high",
            QualityNcrSeverity.Minor => "medium",
            _ => "low",
        };

    public static string ResolveInitialFollowUpStatus(QualityNonconformance ncr) =>
        HasInsufficientEvidence(ncr)
            ? QualityNcrFollowUpStatuses.RequiresReview
            : QualityNcrFollowUpStatuses.RequiresReview;

    public static string BuildQualityDeepLink(Guid ncrId) =>
        $"/kalite/takip-ve-gorevler?finding={Uri.EscapeDataString(ncrId.ToString())}";

    public static bool IsValidFollowUpTransition(string currentStatus, string targetStatus)
    {
        if (!QualityNcrFollowUpStatuses.All.Contains(targetStatus))
        {
            return false;
        }

        if (QualityNcrFollowUpStatuses.Terminal.Contains(currentStatus))
        {
            return false;
        }

        return targetStatus switch
        {
            QualityNcrFollowUpStatuses.Acknowledged =>
                currentStatus == QualityNcrFollowUpStatuses.RequiresReview,
            QualityNcrFollowUpStatuses.TaskCreated =>
                currentStatus is QualityNcrFollowUpStatuses.RequiresReview or QualityNcrFollowUpStatuses.Acknowledged,
            QualityNcrFollowUpStatuses.Resolved =>
                currentStatus == QualityNcrFollowUpStatuses.TaskCreated,
            QualityNcrFollowUpStatuses.Dismissed =>
                currentStatus is QualityNcrFollowUpStatuses.RequiresReview or QualityNcrFollowUpStatuses.Acknowledged,
            _ => false,
        };
    }
}
