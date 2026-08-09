namespace CrevaFactOS.Persistence.Quality;

public static class QualityNcrLifecycleEngine
{
    public static bool RequiresDisposition(QualityNcrSeverity severity) =>
        severity is QualityNcrSeverity.Major or QualityNcrSeverity.Critical;

    public static bool CanResolve(
        QualityNonconformance ncr,
        IReadOnlyList<QualityCorrectiveAction> correctiveActions,
        IReadOnlyList<QualityHold> holds)
    {
        if (ncr.Status is QualityNcrStatus.Closed or QualityNcrStatus.Cancelled or QualityNcrStatus.Resolved)
        {
            return false;
        }

        if (holds.Any(h => h.Status == QualityHoldRecordStatus.Active))
        {
            return false;
        }

        if (RequiresDisposition(ncr.Severity) && ncr.DispositionStatus != QualityNcrDispositionStatus.Decided)
        {
            return false;
        }

        if (correctiveActions.Any(a =>
                a.Status is QualityCorrectiveActionStatus.Open or QualityCorrectiveActionStatus.InProgress))
        {
            return false;
        }

        return true;
    }

    public static bool CanClose(
        QualityNonconformance ncr,
        IReadOnlyList<QualityCorrectiveAction> correctiveActions,
        IReadOnlyList<QualityHold> holds)
    {
        if (ncr.Status is QualityNcrStatus.Closed or QualityNcrStatus.Cancelled)
        {
            return false;
        }

        if (ncr.Status != QualityNcrStatus.Resolved)
        {
            return false;
        }

        if (holds.Any(h => h.Status == QualityHoldRecordStatus.Active))
        {
            return false;
        }

        if (ncr.Severity == QualityNcrSeverity.Critical && ncr.HoldStatus == QualityNcrHoldStatus.Active)
        {
            return false;
        }

        if (RequiresDisposition(ncr.Severity) && ncr.DispositionStatus != QualityNcrDispositionStatus.Decided)
        {
            return false;
        }

        if (correctiveActions.Any(a =>
                a.Status is QualityCorrectiveActionStatus.Open or QualityCorrectiveActionStatus.InProgress))
        {
            return false;
        }

        return true;
    }

    public static string? ResolveBlockReason(
        QualityNonconformance ncr,
        IReadOnlyList<QualityCorrectiveAction> correctiveActions,
        IReadOnlyList<QualityHold> holds,
        bool closing)
    {
        if (holds.Any(h => h.Status == QualityHoldRecordStatus.Active))
        {
            return "Active quality hold must be released before proceeding.";
        }

        if (RequiresDisposition(ncr.Severity) && ncr.DispositionStatus != QualityNcrDispositionStatus.Decided)
        {
            return "Disposition must be decided before proceeding.";
        }

        if (correctiveActions.Any(a =>
                a.Status is QualityCorrectiveActionStatus.Open or QualityCorrectiveActionStatus.InProgress))
        {
            return "All corrective actions must be completed or cancelled before proceeding.";
        }

        if (closing && ncr.Status != QualityNcrStatus.Resolved)
        {
            return "NCR must be resolved before closing.";
        }

        if (closing && ncr.Severity == QualityNcrSeverity.Critical && ncr.HoldStatus == QualityNcrHoldStatus.Active)
        {
            return "Critical NCR cannot be closed while quality hold is active.";
        }

        return null;
    }
}
