namespace NevaxisZentra.Persistence.Production;

public static class ProductionFollowUpPresentationBuilder
{
    public static string BuildTaskDraftIdempotencyKey(Guid exceptionId, string taskPurpose) =>
        $"production-follow-up-task:{exceptionId:D}:{taskPurpose}";

    public static bool HasInsufficientEvidence(
        ProductionExecutionExceptionRecord exception,
        ProductionOrder? order)
    {
        if (string.IsNullOrWhiteSpace(exception.Message))
        {
            return true;
        }

        if (order is null)
        {
            return true;
        }

        return string.IsNullOrWhiteSpace(order.MachineReference)
               && string.IsNullOrWhiteSpace(order.ProductReference)
               && string.IsNullOrWhiteSpace(order.MoldReference);
    }

    public static bool HasAuthoritativeExceptionContext(
        ProductionExecutionExceptionRecord exception,
        ProductionOrder? order)
    {
        if (ProductionExceptionStatuses.Resolved == exception.Status)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(exception.Message))
        {
            return false;
        }

        if (!ProductionExceptionTypes.All.Contains(exception.ExceptionType))
        {
            return false;
        }

        return order is not null
               && (!string.IsNullOrWhiteSpace(order.ReferenceCode)
                   || !string.IsNullOrWhiteSpace(order.MachineReference)
                   || !string.IsNullOrWhiteSpace(order.ProductReference));
    }

    public static bool IsBlockedOrDelayedIndicator(ProductionExecutionExceptionRecord exception) =>
        exception.ExceptionType is ProductionExceptionTypes.StaleDowntime
            or ProductionExceptionTypes.ScheduleOverrun
            or ProductionExceptionTypes.StaleSetup
        || exception.Severity == ProductionExceptionSeverities.Critical;

    public static bool IsEligibleForFollowUp(ProductionExecutionExceptionRecord exception) =>
        ProductionExceptionStatuses.Unresolved.Contains(exception.Status)
        && !ProductionFollowUpStatuses.Terminal.Contains(exception.FollowUpStatus);

    public static string ResolveTaskPurpose(
        ProductionExecutionExceptionRecord exception,
        ProductionOrder? order) =>
        HasInsufficientEvidence(exception, order)
            ? ProductionFollowUpConstants.TaskPurposeDataReviewDraft
            : ProductionFollowUpConstants.TaskPurposeHumanReviewDraft;

    public static bool CanCreateTaskDraft(
        ProductionExecutionExceptionRecord exception,
        ProductionOrder? order)
    {
        if (exception.LinkedAiTaskId.HasValue)
        {
            return false;
        }

        if (!IsEligibleForFollowUp(exception))
        {
            return false;
        }

        return HasInsufficientEvidence(exception, order)
               || HasAuthoritativeExceptionContext(exception, order);
    }

    public static string BuildTaskTitle(
        ProductionExecutionExceptionRecord exception,
        ProductionOrder? order)
    {
        var label = order?.ReferenceCode ?? exception.ExceptionType;
        return HasInsufficientEvidence(exception, order)
            ? $"Üretim veri incelemesi — {label}"
            : $"Üretim incelemesi — {label}";
    }

    public static string BuildTaskSummary(
        ProductionExecutionExceptionRecord exception,
        ProductionOrder? order) =>
        $"Emir: {order?.ReferenceCode ?? "—"}. İstisna: {exception.ExceptionType}. " +
        $"Durum: {exception.Status}. Şiddet: {exception.Severity}.";

    public static string BuildEvidenceSummary(
        ProductionExecutionExceptionRecord exception,
        ProductionOrder? order)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(exception.Message))
        {
            parts.Add(exception.Message);
        }

        parts.Add($"İstisna türü: {exception.ExceptionType}");
        parts.Add($"Şiddet: {exception.Severity}");

        if (order is not null)
        {
            if (!string.IsNullOrWhiteSpace(order.ReferenceCode))
            {
                parts.Add($"Emir: {order.ReferenceCode}");
            }

            if (!string.IsNullOrWhiteSpace(order.MachineReference))
            {
                parts.Add($"Makine: {order.MachineReference}");
            }

            if (!string.IsNullOrWhiteSpace(order.ProductReference))
            {
                parts.Add($"Ürün: {order.ProductReference}");
            }

            if (!string.IsNullOrWhiteSpace(order.MoldReference))
            {
                parts.Add($"Kalıp: {order.MoldReference}");
            }
        }

        return string.Join(" | ", parts.Where(p => !string.IsNullOrWhiteSpace(p)).Take(5));
    }

    public static string ResolveTaskPriority(ProductionExecutionExceptionRecord exception) =>
        exception.Severity switch
        {
            ProductionExceptionSeverities.Critical => "critical",
            ProductionExceptionSeverities.Warning => "high",
            _ => "medium",
        };

    public static string BuildProductionDeepLink(Guid exceptionId) =>
        $"/uretim/istisnalar-ve-gorevler?exception={Uri.EscapeDataString(exceptionId.ToString())}";

    public static bool IsValidFollowUpTransition(string currentStatus, string targetStatus)
    {
        if (!ProductionFollowUpStatuses.All.Contains(targetStatus))
        {
            return false;
        }

        if (ProductionFollowUpStatuses.Terminal.Contains(currentStatus))
        {
            return false;
        }

        return targetStatus switch
        {
            ProductionFollowUpStatuses.Acknowledged =>
                currentStatus == ProductionFollowUpStatuses.RequiresReview,
            ProductionFollowUpStatuses.TaskCreated =>
                currentStatus is ProductionFollowUpStatuses.RequiresReview or ProductionFollowUpStatuses.Acknowledged,
            ProductionFollowUpStatuses.Resolved =>
                currentStatus == ProductionFollowUpStatuses.TaskCreated,
            ProductionFollowUpStatuses.Dismissed =>
                currentStatus is ProductionFollowUpStatuses.RequiresReview or ProductionFollowUpStatuses.Acknowledged,
            _ => false,
        };
    }
}
