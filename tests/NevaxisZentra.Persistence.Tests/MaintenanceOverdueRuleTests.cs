using NevaxisZentra.Persistence.Maintenance;

namespace NevaxisZentra.Persistence.Tests;

public sealed class MaintenanceOverdueRuleTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    private static PreventiveMaintenanceDue Due(PreventiveMaintenanceDueStatus status, int daysFromNow) =>
        new() { Id = Guid.NewGuid(), Status = status, DueAtUtc = Now.AddDays(daysFromNow) };

    [Theory]
    [InlineData(PreventiveMaintenanceDueStatus.Due, -3, true)]
    [InlineData(PreventiveMaintenanceDueStatus.WorkOrderCreated, -1, true)]
    [InlineData(PreventiveMaintenanceDueStatus.Due, 2, false)]
    [InlineData(PreventiveMaintenanceDueStatus.Completed, -5, false)]
    [InlineData(PreventiveMaintenanceDueStatus.Skipped, -5, false)]
    [InlineData(PreventiveMaintenanceDueStatus.Cancelled, -5, false)]
    public void OnlyOpenDuesPastTheirDueDateAreOverdue(PreventiveMaintenanceDueStatus status, int daysFromNow, bool expected) =>
        Assert.Equal(expected, MaintenanceOverdueRule.IsOverdueOpenDue(Due(status, daysFromNow), Now));

    [Fact]
    public void TheSameDueRecordsGiveTheSameCountForEverySurface()
    {
        var dues = new[]
        {
            Due(PreventiveMaintenanceDueStatus.Due, -3),
            Due(PreventiveMaintenanceDueStatus.WorkOrderCreated, -1),
            Due(PreventiveMaintenanceDueStatus.Due, 4),
            Due(PreventiveMaintenanceDueStatus.Completed, -9),
        };

        // Workspace summary (/bakim), reporting overview and asset reliability all count through the one rule.
        var summaryCount = dues.Count(d => MaintenanceOverdueRule.IsOverdueOpenDue(d, Now));
        var overviewCount = dues.Where(d => d.DueAtUtc < Now).Count(d => MaintenanceOverdueRule.IsOverdueOpenDue(d, Now));
        var assetCount = dues.Where(d => d.AssetReference is null or "").Count(d => MaintenanceOverdueRule.IsOverdueOpenDue(d, Now));

        Assert.Equal(2, summaryCount);
        Assert.Equal(summaryCount, overviewCount);
        Assert.Equal(summaryCount, assetCount);
    }
}
