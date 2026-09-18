namespace NevaxisZentra.Persistence.Mold;

using System.Globalization;
public static class MoldFollowUpPresentationBuilder
{
    public static string BuildTaskDraftIdempotencyKey(Guid moldId, string taskPurpose) =>
        $"mold-follow-up-task:{moldId:D}:{taskPurpose}";

    public static bool IsAtOrAboveNominalShotLife(MoldRegistryEntry mold) =>
        mold.NominalShotLife.HasValue
        && mold.CurrentShotCount.HasValue
        && mold.CurrentShotCount.Value >= mold.NominalShotLife.Value;

    public static bool HasMaintenanceIndicator(MoldRegistryEntry mold) =>
        mold.LifecycleState == MoldLifecycleState.Maintenance;

    public static bool HasInsufficientEvidence(MoldRegistryEntry mold)
    {
        if (HasMaintenanceIndicator(mold)
            && !string.IsNullOrWhiteSpace(mold.Code)
            && !string.IsNullOrWhiteSpace(mold.Name))
        {
            return false;
        }

        if (IsAtOrAboveNominalShotLife(mold)
            && !string.IsNullOrWhiteSpace(mold.Code)
            && mold.NominalShotLife.HasValue
            && mold.CurrentShotCount.HasValue)
        {
            return false;
        }

        return true;
    }

    public static bool IsEligibleForFollowUp(MoldRegistryEntry mold) =>
        mold.LifecycleState is not (MoldLifecycleState.Retired or MoldLifecycleState.Scrapped)
        && !MoldFollowUpStatuses.Terminal.Contains(mold.FollowUpStatus)
        && (HasMaintenanceIndicator(mold) || IsAtOrAboveNominalShotLife(mold));

    public static string ResolveTaskPurpose(MoldRegistryEntry mold) =>
        HasInsufficientEvidence(mold)
            ? MoldFollowUpConstants.TaskPurposeDataReviewDraft
            : MoldFollowUpConstants.TaskPurposeHumanReviewDraft;

    public static bool CanCreateTaskDraft(MoldRegistryEntry mold)
    {
        if (mold.LinkedAiTaskId.HasValue)
        {
            return false;
        }

        if (!IsEligibleForFollowUp(mold))
        {
            return false;
        }

        return HasInsufficientEvidence(mold) || HasMaintenanceIndicator(mold) || IsAtOrAboveNominalShotLife(mold);
    }

    public static string BuildTaskTitle(MoldRegistryEntry mold)
    {
        var label = !string.IsNullOrWhiteSpace(mold.Code) ? mold.Code : mold.Name;
        return HasInsufficientEvidence(mold)
            ? $"Kalıp veri incelemesi — {label}"
            : $"Kalıp teknik incelemesi — {label}";
    }

    public static string BuildTaskSummary(MoldRegistryEntry mold) =>
        $"Kalıp: {mold.Code}. Yaşam döngüsü: {MoldValueConversions.ToStorage(mold.LifecycleState)}. " +
        $"Vuruş: {mold.CurrentShotCount?.ToString(CultureInfo.InvariantCulture) ?? "—"}/{mold.NominalShotLife?.ToString(CultureInfo.InvariantCulture) ?? "—"}.";

    public static string BuildEvidenceSummary(MoldRegistryEntry mold, long? latestSnapshotShotCount)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(mold.Name))
        {
            parts.Add(mold.Name);
        }

        parts.Add($"Yaşam döngüsü: {MoldValueConversions.ToStorage(mold.LifecycleState)}");

        if (mold.CurrentShotCount.HasValue || latestSnapshotShotCount.HasValue)
        {
            var current = mold.CurrentShotCount ?? latestSnapshotShotCount;
            parts.Add($"Vuruş sayacı: {current?.ToString(CultureInfo.InvariantCulture)}/{mold.NominalShotLife?.ToString(CultureInfo.InvariantCulture) ?? "—"}");
        }

        if (!string.IsNullOrWhiteSpace(mold.PrimaryProductReference))
        {
            parts.Add($"Ürün: {mold.PrimaryProductReference}");
        }

        if (!string.IsNullOrWhiteSpace(mold.MaterialFamilyReference))
        {
            parts.Add($"Malzeme: {mold.MaterialFamilyReference}");
        }

        return string.Join(" | ", parts.Where(p => !string.IsNullOrWhiteSpace(p)).Take(4));
    }

    public static string ResolveTaskPriority(MoldRegistryEntry mold)
    {
        if (HasMaintenanceIndicator(mold))
        {
            return "high";
        }

        if (IsAtOrAboveNominalShotLife(mold))
        {
            return "high";
        }

        return "medium";
    }

    public static string BuildMoldDeepLink(Guid moldId) =>
        $"/kaliplar/takip-ve-gorevler?mold={Uri.EscapeDataString(moldId.ToString())}";

    public static bool IsValidFollowUpTransition(string currentStatus, string targetStatus)
    {
        if (!MoldFollowUpStatuses.All.Contains(targetStatus))
        {
            return false;
        }

        if (MoldFollowUpStatuses.Terminal.Contains(currentStatus))
        {
            return false;
        }

        return targetStatus switch
        {
            MoldFollowUpStatuses.Acknowledged =>
                currentStatus == MoldFollowUpStatuses.RequiresReview,
            MoldFollowUpStatuses.TaskCreated =>
                currentStatus is MoldFollowUpStatuses.RequiresReview or MoldFollowUpStatuses.Acknowledged,
            MoldFollowUpStatuses.Resolved =>
                currentStatus == MoldFollowUpStatuses.TaskCreated,
            MoldFollowUpStatuses.Dismissed =>
                currentStatus is MoldFollowUpStatuses.RequiresReview or MoldFollowUpStatuses.Acknowledged,
            _ => false,
        };
    }
}
