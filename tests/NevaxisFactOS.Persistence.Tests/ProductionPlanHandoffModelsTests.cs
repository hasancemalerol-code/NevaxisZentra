using NevaxisFactOS.Persistence.ProductionPlanning;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class ProductionPlanHandoffModelsTests
{
    private static readonly Guid HandoffId = Guid.Parse("dd000001-0000-4000-8000-000000000001");
    private static readonly Guid PlanId = Guid.Parse("dd000001-0000-4000-8000-000000000002");

    // ── ProductionPlanHandoffStatus ───────────────────────────────────────────

    [Fact]
    public void HandoffStatusPreparedExists()
    {
        var status = ProductionPlanHandoffStatus.Prepared;

        Assert.Equal(ProductionPlanHandoffStatus.Prepared, status);
    }

    [Fact]
    public void HandoffStatusConsumedExists()
    {
        var status = ProductionPlanHandoffStatus.Consumed;

        Assert.Equal(ProductionPlanHandoffStatus.Consumed, status);
    }

    [Fact]
    public void HandoffStatusFailedExists()
    {
        var status = ProductionPlanHandoffStatus.Failed;

        Assert.Equal(ProductionPlanHandoffStatus.Failed, status);
    }

    [Fact]
    public void HandoffStatusValuesAreDistinct()
    {
        var statuses = new[]
        {
            ProductionPlanHandoffStatus.Prepared,
            ProductionPlanHandoffStatus.Consumed,
            ProductionPlanHandoffStatus.Failed,
        };

        var distinct = statuses.Distinct().ToArray();
        Assert.Equal(statuses.Length, distinct.Length);
    }

    // ── ProductionPlanHandoffFailureCodes ─────────────────────────────────────

    [Fact]
    public void AlreadyPreparedFailureCodeHasCorrectValue()
    {
        Assert.Equal("handoff-already-prepared", ProductionPlanHandoffFailureCodes.AlreadyPrepared);
    }

    [Fact]
    public void NotScheduledFailureCodeHasCorrectValue()
    {
        Assert.Equal("plan-not-scheduled", ProductionPlanHandoffFailureCodes.NotScheduled);
    }

    [Fact]
    public void BlockingRiskFailureCodeHasCorrectValue()
    {
        Assert.Equal("blocking-risk-present", ProductionPlanHandoffFailureCodes.BlockingRisk);
    }

    [Fact]
    public void HandoffFailureCodesAreDistinct()
    {
        var codes = new[]
        {
            ProductionPlanHandoffFailureCodes.InvalidScope,
            ProductionPlanHandoffFailureCodes.NotFound,
            ProductionPlanHandoffFailureCodes.NotEligible,
            ProductionPlanHandoffFailureCodes.NotScheduled,
            ProductionPlanHandoffFailureCodes.BlockingRisk,
            ProductionPlanHandoffFailureCodes.ConcurrencyConflict,
            ProductionPlanHandoffFailureCodes.AlreadyPrepared,
            ProductionPlanHandoffFailureCodes.AuditFailure,
            ProductionPlanHandoffFailureCodes.SourceUnavailable,
            ProductionPlanHandoffFailureCodes.UnexpectedError,
        };

        var distinct = codes.Distinct().ToArray();
        Assert.Equal(codes.Length, distinct.Length);
    }

    // ── ProductionPlanHandoffResult ───────────────────────────────────────────

    [Fact]
    public void SuccessResultHasSucceededTrue()
    {
        var result = ProductionPlanHandoffResult.Success(
            HandoffId,
            PlanId,
            "REF-001",
            "prepared",
            1u,
            DateTimeOffset.UtcNow);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void SuccessResultHasCorrectHandoffId()
    {
        var result = ProductionPlanHandoffResult.Success(
            HandoffId,
            PlanId,
            "REF-001",
            "prepared",
            1u,
            DateTimeOffset.UtcNow);

        Assert.Equal(HandoffId, result.HandoffId);
    }

    [Fact]
    public void SuccessResultHasNoFailureCode()
    {
        var result = ProductionPlanHandoffResult.Success(
            HandoffId,
            PlanId,
            "REF-001",
            "prepared",
            1u,
            DateTimeOffset.UtcNow);

        Assert.Null(result.FailureCode);
    }

    [Fact]
    public void FailResultHasSucceededFalse()
    {
        var result = ProductionPlanHandoffResult.Fail(
            ProductionPlanHandoffFailureCodes.AlreadyPrepared,
            "Handoff already exists.");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void FailResultHasCorrectFailureCode()
    {
        var result = ProductionPlanHandoffResult.Fail(
            ProductionPlanHandoffFailureCodes.NotScheduled,
            "Plan is not scheduled.");

        Assert.Equal(ProductionPlanHandoffFailureCodes.NotScheduled, result.FailureCode);
    }

    [Fact]
    public void FailResultHasCorrectReason()
    {
        var reason = "Plan must be in scheduled status before handoff.";
        var result = ProductionPlanHandoffResult.Fail(
            ProductionPlanHandoffFailureCodes.NotScheduled,
            reason);

        Assert.Equal(reason, result.FailureReason);
    }

    [Fact]
    public void FailResultHasNoHandoffId()
    {
        var result = ProductionPlanHandoffResult.Fail(
            ProductionPlanHandoffFailureCodes.NotScheduled,
            "Plan is not scheduled.");

        Assert.Null(result.HandoffId);
    }

    // ── PrepareProductionPlanHandoffCommand ───────────────────────────────────

    [Fact]
    public void PrepareCommandCanBeConstructed()
    {
        var command = new PrepareProductionPlanHandoffCommand(PlanId, 3u);

        Assert.Equal(PlanId, command.ProductionPlanId);
        Assert.Equal(3u, command.ExpectedVersion);
    }

    // ── ProductionPlanHandoff entity fields ───────────────────────────────────

    [Fact]
    public void HandoffEntityHasIdempotencyKeyProperty()
    {
        var property = typeof(ProductionPlanHandoff).GetProperty("IdempotencyKey");

        Assert.NotNull(property);
        Assert.Equal(typeof(string), property.PropertyType);
    }

    [Fact]
    public void HandoffEntityHasPayloadJsonProperty()
    {
        var property = typeof(ProductionPlanHandoff).GetProperty("PayloadJson");

        Assert.NotNull(property);
        Assert.Equal(typeof(string), property.PropertyType);
    }

    [Fact]
    public void HandoffEntityHasPreparedBySubjectIdProperty()
    {
        var property = typeof(ProductionPlanHandoff).GetProperty("PreparedBySubjectId");

        Assert.NotNull(property);
        Assert.Equal(typeof(string), property.PropertyType);
    }

    [Fact]
    public void HandoffEntityHasStatusProperty()
    {
        var property = typeof(ProductionPlanHandoff).GetProperty("Status");

        Assert.NotNull(property);
        Assert.Equal(typeof(ProductionPlanHandoffStatus), property.PropertyType);
    }

    [Fact]
    public void HandoffEntityHasVersionProperty()
    {
        var property = typeof(ProductionPlanHandoff).GetProperty("Version");

        Assert.NotNull(property);
        Assert.Equal(typeof(uint), property.PropertyType);
    }

    [Fact]
    public void HandoffEntityHasNullableConsumedAtUtcProperty()
    {
        var property = typeof(ProductionPlanHandoff).GetProperty("ConsumedAtUtc");

        Assert.NotNull(property);
        Assert.Equal(typeof(Nullable<DateTimeOffset>), property.PropertyType);
    }

    [Fact]
    public void HandoffEntityHasNullableProductionReferenceProperty()
    {
        var property = typeof(ProductionPlanHandoff).GetProperty("ProductionReference");

        Assert.NotNull(property);
        Assert.True(
            property.PropertyType == typeof(string),
            "ProductionReference should be a string type (nullable reference type)");
    }
}
