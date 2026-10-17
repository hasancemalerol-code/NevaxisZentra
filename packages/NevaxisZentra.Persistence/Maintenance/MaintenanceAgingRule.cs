namespace NevaxisZentra.Persistence.Maintenance;

/// <summary>
/// The one definition of the age of an open maintenance record. Age is counted in WHOLE days (floor), the same way the
/// workspace shows "Yaş" (for example "1 gün"), so a record that reads "1 gün" can never sit in the "2-3 gün" bucket.
/// </summary>
internal static class MaintenanceAgingRule
{
    public static int WholeDays(DateTimeOffset timestamp, DateTimeOffset now) =>
        Math.Max(0, (int)Math.Floor((now - timestamp).TotalDays));

    public static MaintenanceAgingBucketsReadModel Bucketize(IEnumerable<DateTimeOffset> timestamps, DateTimeOffset now)
    {
        var buckets = new MaintenanceAgingBucketsReadModel(0, 0, 0, 0, 0);
        foreach (var timestamp in timestamps)
        {
            buckets = WholeDays(timestamp, now) switch
            {
                <= 1 => buckets with { Day0To1 = buckets.Day0To1 + 1 },
                <= 3 => buckets with { Day2To3 = buckets.Day2To3 + 1 },
                <= 7 => buckets with { Day4To7 = buckets.Day4To7 + 1 },
                <= 14 => buckets with { Day8To14 = buckets.Day8To14 + 1 },
                _ => buckets with { Day15Plus = buckets.Day15Plus + 1 },
            };
        }

        return buckets;
    }
}
