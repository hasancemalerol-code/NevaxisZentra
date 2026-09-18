using NevaxisZentra.Persistence.Development;
using NevaxisZentra.Persistence.ProductionPlanning;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Tests;

public sealed class ProductionPlanJsonMapperTests
{
    [Fact]
    public void MapDetailMapsValidJsonbFields()
    {
        var result = ProductionPlanJsonMapper.MapDetail(CreateValidDetailRow());

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value!.Demands);
        Assert.Single(result.Value.Risks);
        Assert.Equal("needs-review", result.Value.Summary.Status);
        Assert.Equal("urgent", result.Value.Demands[0].Priority);
    }

    [Fact]
    public void MapDetailRejectsObjectWhereArrayExpectedForDemands()
    {
        var plan = CreateValidDetailRow() with { DemandsJson = "{}" };
        var result = ProductionPlanJsonMapper.MapDetail(plan);

        Assert.False(result.Succeeded);
        Assert.Equal("demands", result.Failure!.Field);
        Assert.Equal(ProductionPlanReadFailureCodes.InvalidRow, result.Failure.Code);
    }

    [Fact]
    public void MapDetailRejectsArrayWhereObjectExpectedForResources()
    {
        var plan = CreateValidDetailRow() with { ResourceAssignmentJson = "[]" };
        var result = ProductionPlanJsonMapper.MapDetail(plan);

        Assert.False(result.Succeeded);
        Assert.Equal("resources", result.Failure!.Field);
    }

    [Fact]
    public void MapDetailRejectsInvalidEnumValues()
    {
        var plan = CreateValidDetailRow() with
        {
            DemandsJson =
                """
                [{"id":"d1","productId":"p1","productName":"Product","requiredQuantity":1,"unit":"ea","priority":"invalid"}]
                """,
        };

        var result = ProductionPlanJsonMapper.MapDetail(plan);

        Assert.False(result.Succeeded);
        Assert.Equal("demands.priority", result.Failure!.Field);
    }

    [Fact]
    public void MapDetailRejectsInvalidDateTimeValues()
    {
        var plan = CreateValidDetailRow() with
        {
            TimeWindowJson =
                """
                {"plannedStartAt":"not-a-date","plannedEndAt":"2026-08-07T17:00:00.000Z"}
                """,
        };

        var result = ProductionPlanJsonMapper.MapDetail(plan);

        Assert.False(result.Succeeded);
        Assert.Equal("timeWindow.plannedStartAt", result.Failure!.Field);
    }

    [Fact]
    public void MapDetailPreservesNullOptionalFields()
    {
        var plan = CreateValidDetailRow() with
        {
            Notes = null,
            AiExplanation = null,
            TransferTargetSystem = null,
        };

        var result = ProductionPlanJsonMapper.MapDetail(plan);

        Assert.True(result.Succeeded);
        Assert.Null(result.Value!.Notes);
        Assert.Null(result.Value.AiExplanation);
    }

    [Fact]
    public void MapDetailPreservesEmptyArrayAndObjectShapes()
    {
        var plan = CreateValidDetailRow() with
        {
            DemandsJson = "[]",
            RisksJson = "[]",
            ResourceAssignmentJson = "{}",
            TimeWindowJson = "{}",
            ApprovalJson = """{"status":"not-required"}""",
            TransferJson = """{"status":"not-started"}""",
            SourceReferencesJson = "[]",
            HistoryJson = "[]",
        };

        var result = ProductionPlanJsonMapper.MapDetail(plan);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Value!.Demands);
        Assert.Empty(result.Value.Risks);
        Assert.Empty(result.Value.SourceReferences);
        Assert.Empty(result.Value.History);
        Assert.Null(result.Value.Resources.MachineId);
    }

    [Fact]
    public void MapDetailFailureDoesNotIncludeRawJson()
    {
        var plan = CreateValidDetailRow() with { RisksJson = "not-json" };
        var result = ProductionPlanJsonMapper.MapDetail(plan);

        Assert.False(result.Succeeded);
        Assert.DoesNotContain("[", result.Failure!.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("{", result.Failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MapListOperationalSummaryCountsDemandsAndRisks()
    {
        var result = ProductionPlanJsonMapper.MapListOperationalSummary(
            """
            [
              {"id":"d1","productId":"p1","productName":"Kapak","productCode":"PP-100","requiredQuantity":1200,"unit":"adet","priority":"high"},
              {"id":"d2","productId":"p2","productName":"Gövde","productCode":"PP-200","requiredQuantity":800,"unit":"adet","priority":"normal"}
            ]
            """,
            """
            [
              {"id":"r1","type":"mold-conflict","severity":"critical","title":"Kalıp","description":"Desc","blocking":true},
              {"id":"r2","type":"delivery-risk","severity":"warning","title":"Teslim","description":"Desc","blocking":false}
            ]
            """,
            """{"machineId":"m1","machineName":"IM-07","moldId":"mo1","moldName":"KLP-42"}""",
            """{"plannedStartAt":"2026-08-06T05:00:00.000Z","plannedEndAt":"2026-08-07T17:00:00.000Z","shiftName":"A vardiyası"}""");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.DemandCount);
        Assert.Equal(2000, result.Value.TotalRequiredQuantity);
        Assert.Equal("adet", result.Value.TotalRequiredQuantityUnit);
        Assert.Equal(1200, result.Value.PrimaryRequiredQuantity);
        Assert.Equal("high", result.Value.HighestDemandPriority);
        Assert.Equal(2, result.Value.RiskCount);
        Assert.Equal(1, result.Value.CriticalRiskCount);
        Assert.Equal(1, result.Value.BlockingRiskCount);
        Assert.True(result.Value.HasBlockingRisk);
        Assert.True(result.Value.HasMachineAssignment);
        Assert.True(result.Value.HasMoldAssignment);
        Assert.Equal("A vardiyası", result.Value.ShiftName);
    }

    [Fact]
    public void MapListOperationalSummaryRejectsInvalidDemandJson()
    {
        var result = ProductionPlanJsonMapper.MapListOperationalSummary(
            "{}",
            "[]",
            "{}",
            "{}");

        Assert.False(result.Succeeded);
        Assert.Equal("demands", result.Failure!.Field);
        Assert.Equal(ProductionPlanReadFailureCodes.InvalidRow, result.Failure.Code);
    }

    [Fact]
    public void MapListOperationalSummaryTreatsEmptyArraysAsZeroCounts()
    {
        var result = ProductionPlanJsonMapper.MapListOperationalSummary(
            "[]",
            "[]",
            "{}",
            "{}");

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.Value!.DemandCount);
        Assert.Equal(0, result.Value.RiskCount);
        Assert.False(result.Value.HasBlockingRisk);
        Assert.False(result.Value.HasMachineAssignment);
    }

    [Fact]
    public void MapListOperationalSummaryDoesNotSumMixedUnits()
    {
        var result = ProductionPlanJsonMapper.MapListOperationalSummary(
            """
            [
              {"id":"d1","productId":"p1","productName":"A","requiredQuantity":10,"unit":"adet","priority":"normal"},
              {"id":"d2","productId":"p2","productName":"B","requiredQuantity":5,"unit":"kg","priority":"normal"}
            ]
            """,
            "[]",
            "{}",
            "{}");

        Assert.True(result.Succeeded);
        Assert.Null(result.Value!.TotalRequiredQuantity);
        Assert.Equal(10, result.Value.PrimaryRequiredQuantity);
    }

    private static ProductionPlanDetailReadModel CreateValidDetailRow() => new(
        Guid.Parse("88000000-0000-4000-8000-000000000030"),
        "plan-a",
        "Plan A",
        Guid.Parse("88000000-0000-4000-8000-000000000010"),
        "needs-review",
        "manual",
        true,
        "medium",
        new DateTimeOffset(2026, 8, 6, 5, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 8, 7, 17, 0, 0, TimeSpan.Zero),
        "not-required",
        "not-started",
        "mes",
        new DateTimeOffset(2026, 8, 5, 7, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 8, 6, 8, 0, 0, TimeSpan.Zero),
        1,
        "notes",
        "creator",
        "explanation",
        """
        [{"id":"d1","productId":"p1","productName":"Product","requiredQuantity":10,"unit":"ea","priority":"urgent"}]
        """,
        """
        [{"id":"r1","type":"delivery-risk","severity":"warning","title":"Risk","description":"Desc","blocking":false}]
        """,
        """{"machineId":"m1","machineName":"Machine"}""",
        """{"plannedStartAt":"2026-08-06T05:00:00.000Z","plannedEndAt":"2026-08-07T17:00:00.000Z"}""",
        """{"status":"not-required","requiredRole":"planning-manager"}""",
        """{"status":"not-started","targetSystem":"mes"}""",
        """
        [{"sourceType":"order-draft","sourceId":"order-1","sourceUpdatedAt":"2026-08-05T07:40:00.000Z"}]
        """,
        """
        [{"id":"h1","type":"created","title":"Created","description":"Created","occurredAt":"2026-08-05T07:45:00.000Z"}]
        """);
}
