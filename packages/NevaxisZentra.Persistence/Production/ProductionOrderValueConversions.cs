namespace NevaxisZentra.Persistence.Production;

public static class ProductionOrderValueConversions
{
    private static readonly Dictionary<ProductionOrderStatus, string> StatusToStorage =
        new()
        {
            [ProductionOrderStatus.Ready] = "ready",
            [ProductionOrderStatus.InProgress] = "in-progress",
            [ProductionOrderStatus.Paused] = "paused",
            [ProductionOrderStatus.Completed] = "completed",
            [ProductionOrderStatus.Cancelled] = "cancelled",
        };

    private static readonly Dictionary<string, ProductionOrderStatus> StorageToStatus =
        StatusToStorage.ToDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal);

    public static string ToStorage(ProductionOrderStatus status) =>
        StatusToStorage.TryGetValue(status, out var value)
            ? value
            : throw new InvalidOperationException($"Unknown ProductionOrderStatus: {status}");

    public static ProductionOrderStatus FromStorage(string value) =>
        StorageToStatus.TryGetValue(value, out var status)
            ? status
            : throw new InvalidOperationException($"Unknown production order status value: '{value}'");

    public static bool TryFromStorage(string value, out ProductionOrderStatus status) =>
        StorageToStatus.TryGetValue(value, out status);
}
