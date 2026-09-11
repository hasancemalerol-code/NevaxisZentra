namespace CrevaFactOS.Persistence.ShiftOperations;

public sealed record ShiftNothingFallsThroughResult(
    bool Allowed,
    IReadOnlyList<string> MissingMandatoryItems,
    IReadOnlyList<string> UnreviewedCriticalItems);

public static class ShiftNothingFallsThroughGate
{
    public static ShiftNothingFallsThroughResult Evaluate(
        IEnumerable<ShiftOpenItem> requiredItems,
        IEnumerable<string> handoverPackageSourceIds)
    {
        var packageIds = handoverPackageSourceIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = requiredItems
            .Where(i => i.RequiresHandover && i.Status == ShiftOpenItemStatuses.Open)
            .Where(i => !packageIds.Contains($"{i.SourceType}:{i.SourceId}"))
            .Select(i => i.Title)
            .ToList();

        var unreviewedCritical = requiredItems
            .Where(i => i.HandoverPriority == ShiftHandoverPriorities.Critical)
            .Where(i => i.Status == ShiftOpenItemStatuses.Open && !packageIds.Contains($"{i.SourceType}:{i.SourceId}"))
            .Select(i => i.Title)
            .ToList();

        return new ShiftNothingFallsThroughResult(
            missing.Count == 0 && unreviewedCritical.Count == 0,
            missing,
            unreviewedCritical);
    }
}
