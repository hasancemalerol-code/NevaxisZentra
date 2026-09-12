using NevaxisFactOS.Persistence.ProductionPlanning;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class ProductionPlanWorkflowValidatorTests
{
    private static readonly Guid PlanId = Guid.Parse("88000001-0000-4000-8000-000000000001");

    // ── SubmitForReview ───────────────────────────────────────────────────────

    [Fact]
    public void SubmitWithZeroExpectedVersionReturnsError()
    {
        var command = new SubmitProductionPlanForReviewCommand(PlanId, 0, null);

        var errors = ProductionPlanWorkflowValidator.ValidateSubmitForReview(command);

        Assert.Contains(errors, e => e.Field == "expectedVersion" && e.Code == "required");
    }

    [Fact]
    public void SubmitWithValidCommandReturnsNoErrors()
    {
        var command = new SubmitProductionPlanForReviewCommand(PlanId, 1, null);

        var errors = ProductionPlanWorkflowValidator.ValidateSubmitForReview(command);

        Assert.Empty(errors);
    }

    [Fact]
    public void SubmitWithCommentWithinLimitReturnsNoErrors()
    {
        var comment = new string('x', ProductionPlanWorkflowValidator.MaxCommentLength);
        var command = new SubmitProductionPlanForReviewCommand(PlanId, 1, comment);

        var errors = ProductionPlanWorkflowValidator.ValidateSubmitForReview(command);

        Assert.Empty(errors);
    }

    [Fact]
    public void SubmitWithTooLongCommentReturnsError()
    {
        var comment = new string('x', ProductionPlanWorkflowValidator.MaxCommentLength + 1);
        var command = new SubmitProductionPlanForReviewCommand(PlanId, 1, comment);

        var errors = ProductionPlanWorkflowValidator.ValidateSubmitForReview(command);

        Assert.Contains(errors, e => e.Field == "comment" && e.Code == "too-long");
    }

    [Fact]
    public void SubmitWithNullCommandThrows()
    {
        Assert.Throws<ArgumentNullException>(
            () => ProductionPlanWorkflowValidator.ValidateSubmitForReview(null!));
    }

    // ── State transitions for SubmitForReview ─────────────────────────────────

    [Theory]
    [InlineData(ProductionPlanStatus.Draft)]
    [InlineData(ProductionPlanStatus.NeedsReview)]
    public void SubmitTransitionIsValidFromAllowedStatuses(ProductionPlanStatus status)
    {
        Assert.True(ProductionPlanWorkflowValidator.IsValidSubmitTransition(status));
    }

    [Theory]
    [InlineData(ProductionPlanStatus.ReadyForApproval)]
    [InlineData(ProductionPlanStatus.Approved)]
    [InlineData(ProductionPlanStatus.Rejected)]
    public void SubmitTransitionIsInvalidFromOtherStatuses(ProductionPlanStatus status)
    {
        Assert.False(ProductionPlanWorkflowValidator.IsValidSubmitTransition(status));
    }

    // ── RequestRevision ───────────────────────────────────────────────────────

    [Fact]
    public void RevisionWithEmptyReasonReturnsError()
    {
        var command = new RequestProductionPlanRevisionCommand(PlanId, 1, "");

        var errors = ProductionPlanWorkflowValidator.ValidateRequestRevision(command);

        Assert.Contains(errors, e => e.Field == "reason" && e.Code == "required");
    }

    [Fact]
    public void RevisionWithWhitespaceOnlyReasonReturnsError()
    {
        var command = new RequestProductionPlanRevisionCommand(PlanId, 1, "   ");

        var errors = ProductionPlanWorkflowValidator.ValidateRequestRevision(command);

        Assert.Contains(errors, e => e.Field == "reason" && e.Code == "required");
    }

    [Fact]
    public void RevisionWithTooShortReasonReturnsError()
    {
        var command = new RequestProductionPlanRevisionCommand(PlanId, 1,
            new string('x', ProductionPlanWorkflowValidator.MinReasonLength - 1));

        var errors = ProductionPlanWorkflowValidator.ValidateRequestRevision(command);

        Assert.Contains(errors, e => e.Field == "reason" && e.Code == "too-short");
    }

    [Fact]
    public void RevisionWithTooLongReasonReturnsError()
    {
        var command = new RequestProductionPlanRevisionCommand(PlanId, 1,
            new string('x', ProductionPlanWorkflowValidator.MaxReasonLength + 1));

        var errors = ProductionPlanWorkflowValidator.ValidateRequestRevision(command);

        Assert.Contains(errors, e => e.Field == "reason" && e.Code == "too-long");
    }

    [Fact]
    public void RevisionWithValidCommandReturnsNoErrors()
    {
        var command = new RequestProductionPlanRevisionCommand(PlanId, 1, "Needs more detail.");

        var errors = ProductionPlanWorkflowValidator.ValidateRequestRevision(command);

        Assert.Empty(errors);
    }

    [Fact]
    public void RevisionWithZeroExpectedVersionReturnsError()
    {
        var command = new RequestProductionPlanRevisionCommand(PlanId, 0, "Needs more detail.");

        var errors = ProductionPlanWorkflowValidator.ValidateRequestRevision(command);

        Assert.Contains(errors, e => e.Field == "expectedVersion" && e.Code == "required");
    }

    // ── State transitions for RequestRevision ────────────────────────────────

    [Theory]
    [InlineData(ProductionPlanStatus.ReadyForApproval)]
    public void RevisionTransitionIsValidFromAllowedStatuses(ProductionPlanStatus status)
    {
        Assert.True(ProductionPlanWorkflowValidator.IsValidRevisionTransition(status));
    }

    [Theory]
    [InlineData(ProductionPlanStatus.Draft)]
    [InlineData(ProductionPlanStatus.NeedsReview)]
    [InlineData(ProductionPlanStatus.Approved)]
    [InlineData(ProductionPlanStatus.Rejected)]
    public void RevisionTransitionIsInvalidFromOtherStatuses(ProductionPlanStatus status)
    {
        Assert.False(ProductionPlanWorkflowValidator.IsValidRevisionTransition(status));
    }

    // ── Approve ───────────────────────────────────────────────────────────────

    [Fact]
    public void ApproveWithZeroExpectedVersionReturnsError()
    {
        var command = new ApproveProductionPlanCommand(PlanId, 0, null);

        var errors = ProductionPlanWorkflowValidator.ValidateApprove(command);

        Assert.Contains(errors, e => e.Field == "expectedVersion" && e.Code == "required");
    }

    [Fact]
    public void ApproveWithValidCommandReturnsNoErrors()
    {
        var command = new ApproveProductionPlanCommand(PlanId, 1, null);

        var errors = ProductionPlanWorkflowValidator.ValidateApprove(command);

        Assert.Empty(errors);
    }

    [Fact]
    public void ApproveWithNoteWithinLimitReturnsNoErrors()
    {
        var note = new string('x', ProductionPlanWorkflowValidator.MaxNoteLength);
        var command = new ApproveProductionPlanCommand(PlanId, 1, note);

        var errors = ProductionPlanWorkflowValidator.ValidateApprove(command);

        Assert.Empty(errors);
    }

    [Fact]
    public void ApproveWithTooLongNoteReturnsError()
    {
        var note = new string('x', ProductionPlanWorkflowValidator.MaxNoteLength + 1);
        var command = new ApproveProductionPlanCommand(PlanId, 1, note);

        var errors = ProductionPlanWorkflowValidator.ValidateApprove(command);

        Assert.Contains(errors, e => e.Field == "note" && e.Code == "too-long");
    }

    // ── State transitions for Approve ────────────────────────────────────────

    [Theory]
    [InlineData(ProductionPlanStatus.ReadyForApproval)]
    public void ApproveTransitionIsValidFromAllowedStatuses(ProductionPlanStatus status)
    {
        Assert.True(ProductionPlanWorkflowValidator.IsValidApproveTransition(status));
    }

    [Theory]
    [InlineData(ProductionPlanStatus.Draft)]
    [InlineData(ProductionPlanStatus.NeedsReview)]
    [InlineData(ProductionPlanStatus.Approved)]
    [InlineData(ProductionPlanStatus.Rejected)]
    public void ApproveTransitionIsInvalidFromOtherStatuses(ProductionPlanStatus status)
    {
        Assert.False(ProductionPlanWorkflowValidator.IsValidApproveTransition(status));
    }

    // ── Reject ────────────────────────────────────────────────────────────────

    [Fact]
    public void RejectWithEmptyReasonReturnsError()
    {
        var command = new RejectProductionPlanCommand(PlanId, 1, "");

        var errors = ProductionPlanWorkflowValidator.ValidateReject(command);

        Assert.Contains(errors, e => e.Field == "reason" && e.Code == "required");
    }

    [Fact]
    public void RejectWithValidReasonReturnsNoErrors()
    {
        var command = new RejectProductionPlanCommand(PlanId, 1, "Not sufficient quality.");

        var errors = ProductionPlanWorkflowValidator.ValidateReject(command);

        Assert.Empty(errors);
    }

    [Fact]
    public void RejectWithZeroExpectedVersionReturnsError()
    {
        var command = new RejectProductionPlanCommand(PlanId, 0, "Not sufficient quality.");

        var errors = ProductionPlanWorkflowValidator.ValidateReject(command);

        Assert.Contains(errors, e => e.Field == "expectedVersion" && e.Code == "required");
    }

    [Fact]
    public void RejectWithTooLongReasonReturnsError()
    {
        var command = new RejectProductionPlanCommand(PlanId, 1,
            new string('x', ProductionPlanWorkflowValidator.MaxReasonLength + 1));

        var errors = ProductionPlanWorkflowValidator.ValidateReject(command);

        Assert.Contains(errors, e => e.Field == "reason" && e.Code == "too-long");
    }

    // ── State transitions for Reject ─────────────────────────────────────────

    [Theory]
    [InlineData(ProductionPlanStatus.ReadyForApproval)]
    public void RejectTransitionIsValidFromAllowedStatuses(ProductionPlanStatus status)
    {
        Assert.True(ProductionPlanWorkflowValidator.IsValidRejectTransition(status));
    }

    [Theory]
    [InlineData(ProductionPlanStatus.Draft)]
    [InlineData(ProductionPlanStatus.NeedsReview)]
    [InlineData(ProductionPlanStatus.Approved)]
    [InlineData(ProductionPlanStatus.Rejected)]
    public void RejectTransitionIsInvalidFromOtherStatuses(ProductionPlanStatus status)
    {
        Assert.False(ProductionPlanWorkflowValidator.IsValidRejectTransition(status));
    }
}
