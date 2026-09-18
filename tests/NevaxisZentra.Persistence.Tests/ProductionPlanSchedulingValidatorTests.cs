using NevaxisZentra.Persistence.ProductionPlanning;

namespace NevaxisZentra.Persistence.Tests;

public sealed class ProductionPlanSchedulingValidatorTests
{
    private static readonly Guid PlanId = Guid.Parse("88000002-0000-4000-8000-000000000001");

    private static readonly DateTimeOffset ValidStart = new(2026, 9, 1, 6, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ValidEnd = new(2026, 9, 1, 14, 0, 0, TimeSpan.Zero);

    // ScheduleProductionPlanCommand(Guid ProductionPlanId, uint ExpectedVersion,
    //   DateTimeOffset PlannedStartAtUtc, DateTimeOffset PlannedEndAtUtc,
    //   string? ShiftName, string? SchedulingComment)

    // ── Schedule validation ───────────────────────────────────────────────────

    [Fact]
    public void ScheduleWithValidCommandReturnsNoErrors()
    {
        var command = new ScheduleProductionPlanCommand(
            PlanId, 1, ValidStart, ValidEnd, "Sabah Vardiyası", null);

        var errors = ProductionPlanWorkflowValidator.ValidateSchedule(command);

        Assert.Empty(errors);
    }

    [Fact]
    public void ScheduleWithZeroExpectedVersionReturnsError()
    {
        var command = new ScheduleProductionPlanCommand(
            PlanId, 0, ValidStart, ValidEnd, null, null);

        var errors = ProductionPlanWorkflowValidator.ValidateSchedule(command);

        Assert.Contains(errors, e => e.Field == "expectedVersion" && e.Code == "required");
    }

    [Fact]
    public void ScheduleWithDefaultStartReturnsError()
    {
        var command = new ScheduleProductionPlanCommand(
            PlanId, 1, default, ValidEnd, null, null);

        var errors = ProductionPlanWorkflowValidator.ValidateSchedule(command);

        Assert.Contains(errors, e => e.Field == "plannedStartAtUtc" && e.Code == "required");
    }

    [Fact]
    public void ScheduleWithDefaultEndReturnsError()
    {
        var command = new ScheduleProductionPlanCommand(
            PlanId, 1, ValidStart, default, null, null);

        var errors = ProductionPlanWorkflowValidator.ValidateSchedule(command);

        Assert.Contains(errors, e => e.Field == "plannedEndAtUtc" && e.Code == "required");
    }

    [Fact]
    public void ScheduleWithEndBeforeStartReturnsError()
    {
        var command = new ScheduleProductionPlanCommand(
            PlanId, 1, ValidEnd, ValidStart, null, null);

        var errors = ProductionPlanWorkflowValidator.ValidateSchedule(command);

        Assert.Contains(errors, e => e.Field == "plannedEndAtUtc" && e.Code == "invalid-window");
    }

    [Fact]
    public void ScheduleWithEndEqualToStartReturnsError()
    {
        var command = new ScheduleProductionPlanCommand(
            PlanId, 1, ValidStart, ValidStart, null, null);

        var errors = ProductionPlanWorkflowValidator.ValidateSchedule(command);

        Assert.Contains(errors, e => e.Field == "plannedEndAtUtc" && e.Code == "invalid-window");
    }

    [Fact]
    public void ScheduleWithTooLongShiftNameReturnsError()
    {
        var longShiftName = new string('x', ProductionPlanWorkflowValidator.MaxShiftNameLength + 1);
        var command = new ScheduleProductionPlanCommand(
            PlanId, 1, ValidStart, ValidEnd, longShiftName, null);

        var errors = ProductionPlanWorkflowValidator.ValidateSchedule(command);

        Assert.Contains(errors, e => e.Field == "shiftName" && e.Code == "too-long");
    }

    [Fact]
    public void ScheduleWithShiftNameAtLimitReturnsNoErrors()
    {
        var shiftName = new string('x', ProductionPlanWorkflowValidator.MaxShiftNameLength);
        var command = new ScheduleProductionPlanCommand(
            PlanId, 1, ValidStart, ValidEnd, shiftName, null);

        var errors = ProductionPlanWorkflowValidator.ValidateSchedule(command);

        Assert.DoesNotContain(errors, e => e.Field == "shiftName");
    }

    [Fact]
    public void ScheduleWithTooLongCommentReturnsError()
    {
        var longComment = new string('x', ProductionPlanWorkflowValidator.MaxSchedulingCommentLength + 1);
        var command = new ScheduleProductionPlanCommand(
            PlanId, 1, ValidStart, ValidEnd, null, longComment);

        var errors = ProductionPlanWorkflowValidator.ValidateSchedule(command);

        Assert.Contains(errors, e => e.Field == "schedulingComment" && e.Code == "too-long");
    }

    [Fact]
    public void ScheduleWithNullCommandThrows()
    {
        Assert.Throws<ArgumentNullException>(
            () => ProductionPlanWorkflowValidator.ValidateSchedule(null!));
    }

    // ── State transition validation ───────────────────────────────────────────

    [Fact]
    public void ScheduleTransitionIsValidFromApproved()
    {
        Assert.True(ProductionPlanWorkflowValidator.IsValidScheduleTransition(ProductionPlanStatus.Approved));
    }

    [Theory]
    [InlineData(ProductionPlanStatus.Draft)]
    [InlineData(ProductionPlanStatus.NeedsReview)]
    [InlineData(ProductionPlanStatus.ReadyForApproval)]
    [InlineData(ProductionPlanStatus.Rejected)]
    [InlineData(ProductionPlanStatus.Scheduled)]
    [InlineData(ProductionPlanStatus.InProgress)]
    [InlineData(ProductionPlanStatus.Completed)]
    [InlineData(ProductionPlanStatus.Cancelled)]
    public void ScheduleTransitionIsInvalidFromNonApprovedStatuses(ProductionPlanStatus status)
    {
        Assert.False(ProductionPlanWorkflowValidator.IsValidScheduleTransition(status));
    }
}
