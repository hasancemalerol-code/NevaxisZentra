namespace NevaxisZentra.Persistence.ShiftOperations;

public sealed record ShiftHandoverConflict(
    string Code,
    string Message,
    string? SourceType,
    string? SourceId);

public static class ShiftHandoverConflictDetector
{
    public static IReadOnlyList<ShiftHandoverConflict> Detect(
        IEnumerable<ShiftOpenItem> openItems,
        IReadOnlyDictionary<string, string> humanNotesBySourceId)
    {
        var conflicts = new List<ShiftHandoverConflict>();
        foreach (var item in openItems.Where(i => i.Status == ShiftOpenItemStatuses.Open))
        {
            if (humanNotesBySourceId.TryGetValue(item.SourceId, out var note) &&
                note.Contains("çözüldü", StringComparison.OrdinalIgnoreCase) &&
                item.RequiresHandover)
            {
                conflicts.Add(new ShiftHandoverConflict(
                    "human-note-vs-open-source",
                    $"İnsan notu çözüldü diyor ancak kaynak hâlâ açık: {item.Title}",
                    item.SourceType,
                    item.SourceId));
            }

            if (item.VerificationRequired && item.Status == ShiftOpenItemStatuses.Open)
            {
                conflicts.Add(new ShiftHandoverConflict(
                    "verification-pending",
                    $"Doğrulama bekleyen açık kalem: {item.Title}",
                    item.SourceType,
                    item.SourceId));
            }
        }

        return conflicts;
    }
}
