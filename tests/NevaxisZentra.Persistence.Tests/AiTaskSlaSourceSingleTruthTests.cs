using NevaxisZentra.Persistence.AiTasks;

namespace NevaxisZentra.Persistence.Tests;

/// <summary>
/// One task -> one SLA truth and one source truth. The list summary and the detail read model both call
/// <see cref="AiTaskSlaEngine"/> / <see cref="AiTaskListOriginResolver"/>, so these rules hold for list, detail and KPIs alike.
/// </summary>
public sealed class AiTaskSlaSourceSingleTruthTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
    private const string MaintenanceSource = """[{"sourceType":"maintenance","sourceId":null,"sourceLabel":"Bakım talebi","observedAt":null,"valueSummary":null}]""";

    private static AiTask Task(DateTimeOffset? dueAt, string sourcesJson, string? sourceModule = null) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        Priority = AiTaskPriorities.High,
        Status = AiTaskStatuses.Draft,
        CreatedAtUtc = Now.AddDays(-1),
        DueAtUtc = dueAt,
        SourcesJson = sourcesJson,
        SourceModule = sourceModule,
        ConfidenceJson = """{"score":0.7,"explanation":"Kural tabanlı eşleşme.","modelVersion":null}""",
    };

    private static AiTaskSlaStateReadModel Sla(AiTask task) =>
        AiTaskSlaEngine.DeriveSlaState(task, AiTaskSlaEngine.ResolvePolicy(task, []), Now);

    [Fact]
    public void SourceWithoutSlaTargetIsNotApplicableNotOverdueAndHasKnownSource()
    {
        var task = Task(null, MaintenanceSource);

        var sla = Sla(task);

        Assert.Equal(AiTaskSlaStates.NotApplicable, sla.State);
        Assert.False(sla.IsOverdue);
        Assert.Null(sla.BreachType);
        Assert.NotEqual(AiTaskListOriginKinds.Unspecified, AiTaskListOriginResolver.Resolve(task));
    }

    [Fact]
    public void PastDueSlaTargetIsBreachedAndOverdue()
    {
        var task = Task(Now.AddHours(-3), MaintenanceSource);

        var sla = Sla(task);

        Assert.Equal(AiTaskSlaStates.Breached, sla.State);
        Assert.True(sla.IsOverdue);
        Assert.Equal(AiTaskListOriginKinds.OperationalTask, AiTaskListOriginResolver.Resolve(task));
    }

    [Fact]
    public void TaskWithoutSourceStaysUnspecified()
    {
        var task = Task(null, "[]");

        Assert.Equal(AiTaskListOriginKinds.Unspecified, AiTaskListOriginResolver.Resolve(task));
        Assert.Equal(AiTaskSlaStates.NotApplicable, Sla(task).State);
    }

    [Fact]
    public void SourceEntityLinkWithoutModuleIsASource()
    {
        var task = Task(null, "[]");
        task.SourceEntityId = "req-1";

        Assert.Equal(AiTaskListOriginKinds.OperationalTask, AiTaskListOriginResolver.Resolve(task));
    }
}
