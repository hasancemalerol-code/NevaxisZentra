namespace NevaxisZentra.Persistence.Export;

/// <summary>Pure static packing reconciliation engine — no I/O.</summary>
public static class ExportPackingReconciliationEngine
{
    public sealed record ReconciliationResult(
        ExportPackingReconciliationStatus Status,
        string? Explanation,
        int ItemCount,
        decimal TotalQuantity);

    public static ReconciliationResult Reconcile(
        IReadOnlyList<ExportPackingItem> items,
        decimal? orderedQuantityTotal)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
        {
            return new ReconciliationResult(
                ExportPackingReconciliationStatus.NotStarted,
                "No packing items have been recorded.",
                0,
                0m);
        }

        var hasSourceOrderLines = items.Any(i => i.SourceOrderLineId.HasValue);

        if (!hasSourceOrderLines)
        {
            return new ReconciliationResult(
                ExportPackingReconciliationStatus.UnableToValidate,
                "No items have a linked order line; quantity matching cannot be performed.",
                items.Count,
                items.Sum(i => i.Quantity));
        }

        if (!orderedQuantityTotal.HasValue)
        {
            return new ReconciliationResult(
                ExportPackingReconciliationStatus.UnableToValidate,
                "Ordered quantity total was not provided; quantity matching cannot be performed.",
                items.Count,
                items.Sum(i => i.Quantity));
        }

        var packedTotal = items.Sum(i => i.Quantity);
        var ordered = orderedQuantityTotal.Value;

        if (packedTotal == ordered)
        {
            return new ReconciliationResult(
                ExportPackingReconciliationStatus.Matched,
                $"Packed quantity {packedTotal} matches ordered quantity {ordered}.",
                items.Count,
                packedTotal);
        }

        if (packedTotal < ordered)
        {
            return new ReconciliationResult(
                ExportPackingReconciliationStatus.Incomplete,
                $"Packed quantity {packedTotal} is less than ordered quantity {ordered}.",
                items.Count,
                packedTotal);
        }

        return new ReconciliationResult(
            ExportPackingReconciliationStatus.Overpacked,
            $"Packed quantity {packedTotal} exceeds ordered quantity {ordered}.",
            items.Count,
            packedTotal);
    }
}
