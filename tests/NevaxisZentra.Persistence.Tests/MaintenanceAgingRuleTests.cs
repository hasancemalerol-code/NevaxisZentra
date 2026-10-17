using NevaxisZentra.Persistence.Maintenance;

namespace NevaxisZentra.Persistence.Tests;

public sealed class MaintenanceAgingRuleTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0.5, 0)]
    [InlineData(1.0, 1)]
    [InlineData(1.9, 1)]
    [InlineData(2.0, 2)]
    [InlineData(3.99, 3)]
    [InlineData(-2.0, 0)]
    public void AgeIsCountedInWholeDaysLikeTheWorkspaceLabel(double days, int expectedWholeDays) =>
        Assert.Equal(expectedWholeDays, MaintenanceAgingRule.WholeDays(Now.AddDays(-days), Now));

    [Fact]
    public void ARecordShownAsOneDayNeverLandsInTheTwoToThreeDayBucket()
    {
        // "1 gün" in the workspace table is floor(1.5) = 1, so the report bucket must be 0-1, not 2-3.
        var buckets = MaintenanceAgingRule.Bucketize(new[] { Now.AddDays(-1.5) }, Now);
        Assert.Equal(1, buckets.Day0To1);
        Assert.Equal(0, buckets.Day2To3);
    }

    [Theory]
    [InlineData(2, 0, 1, 0, 0, 0)]
    [InlineData(3.5, 0, 1, 0, 0, 0)]
    [InlineData(4, 0, 0, 1, 0, 0)]
    [InlineData(7.9, 0, 0, 1, 0, 0)]
    [InlineData(8, 0, 0, 0, 1, 0)]
    [InlineData(15, 0, 0, 0, 0, 1)]
    public void BucketBoundariesFollowWholeDays(double days, int b01, int b23, int b47, int b814, int b15)
    {
        var buckets = MaintenanceAgingRule.Bucketize(new[] { Now.AddDays(-days) }, Now);
        Assert.Equal(new[] { b01, b23, b47, b814, b15 },
            new[] { buckets.Day0To1, buckets.Day2To3, buckets.Day4To7, buckets.Day8To14, buckets.Day15Plus });
    }
}
