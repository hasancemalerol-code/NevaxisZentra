using CrevaFactOS.Persistence.ProductionPlanning;

namespace CrevaFactOS.Persistence.Tests;

public sealed class ProductionPlanDraftValidatorTests
{
    private static readonly DateTimeOffset ValidStart = new(2026, 9, 1, 6, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ValidEnd = new(2026, 9, 1, 14, 0, 0, TimeSpan.Zero);

    private static CreateProductionPlanDraftCommand ValidCreateCommand() =>
        new(
            FacilityId: Guid.NewGuid(),
            ReferenceCode: "PP-TEST-001",
            Title: "Test Plan",
            Notes: null,
            Origin: "manual",
            AiGenerated: false,
            Confidence: null,
            PlannedStartAtUtc: ValidStart,
            PlannedEndAtUtc: ValidEnd,
            Demands:
            [
                new("d1", "PROD-100", "Product A", 100, "pcs", "normal", null, null, null, null, null),
            ],
            Risks: [],
            Resources: new(null, null, null, null, null, null, null, null),
            TimeWindow: null,
            Approval: null,
            Transfer: null,
            SourceReferences: [],
            AiExplanation: null);

    [Fact]
    public void ValidCreateCommandPassesValidation()
    {
        var errors = ProductionPlanDraftValidator.ValidateCreate(ValidCreateCommand());

        Assert.Empty(errors);
    }

    [Fact]
    public void MissingReferenceCodeFailsValidation()
    {
        var command = ValidCreateCommand() with { ReferenceCode = "   " };

        var errors = ProductionPlanDraftValidator.ValidateCreate(command);

        Assert.Contains(errors, e => e.Field == "referenceCode" && e.Code == "required");
    }

    [Fact]
    public void TooLongReferenceCodeFailsValidation()
    {
        var command = ValidCreateCommand() with
        {
            ReferenceCode = new string('A', ProductionPlanDraftValidator.MaxReferencCodeLength + 1),
        };

        var errors = ProductionPlanDraftValidator.ValidateCreate(command);

        Assert.Contains(errors, e => e.Field == "referenceCode" && e.Code == "too-long");
    }

    [Fact]
    public void InvalidReferenceCodeCharactersFailValidation()
    {
        var command = ValidCreateCommand() with { ReferenceCode = "PP TEST@001" };

        var errors = ProductionPlanDraftValidator.ValidateCreate(command);

        Assert.Contains(errors, e => e.Field == "referenceCode" && e.Code == "invalid-characters");
    }

    [Fact]
    public void MissingTitleFailsValidation()
    {
        var command = ValidCreateCommand() with { Title = "" };

        var errors = ProductionPlanDraftValidator.ValidateCreate(command);

        Assert.Contains(errors, e => e.Field == "title" && e.Code == "required");
    }

    [Fact]
    public void TooLongTitleFailsValidation()
    {
        var command = ValidCreateCommand() with
        {
            Title = new string('T', ProductionPlanDraftValidator.MaxTitleLength + 1),
        };

        var errors = ProductionPlanDraftValidator.ValidateCreate(command);

        Assert.Contains(errors, e => e.Field == "title" && e.Code == "too-long");
    }

    [Fact]
    public void InvalidDateRangeFailsValidation()
    {
        var command = ValidCreateCommand() with
        {
            PlannedStartAtUtc = ValidEnd,
            PlannedEndAtUtc = ValidStart,
        };

        var errors = ProductionPlanDraftValidator.ValidateCreate(command);

        Assert.Contains(errors, e => e.Field == "plannedEndAtUtc" && e.Code == "invalid-range");
    }

    [Fact]
    public void NonUtcDateFailsValidation()
    {
        var localOffset = new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.FromHours(3));
        var command = ValidCreateCommand() with
        {
            PlannedStartAtUtc = localOffset,
            PlannedEndAtUtc = localOffset.AddHours(8),
        };

        var errors = ProductionPlanDraftValidator.ValidateCreate(command);

        Assert.Contains(errors, e => e.Field == "plannedStartAtUtc" && e.Code == "invalid-utc");
    }

    [Fact]
    public void EmptyDemandsFailsValidation()
    {
        var command = ValidCreateCommand() with { Demands = [] };

        var errors = ProductionPlanDraftValidator.ValidateCreate(command);

        Assert.Contains(errors, e => e.Field == "demands" && e.Code == "required");
    }

    [Fact]
    public void ZeroQuantityDemandFailsValidation()
    {
        var command = ValidCreateCommand() with
        {
            Demands = [new("d1", "PROD-100", "Product A", 0, "pcs", "normal", null, null, null, null, null)],
        };

        var errors = ProductionPlanDraftValidator.ValidateCreate(command);

        Assert.Contains(errors, e => e.Field == "demands[0].requiredQuantity" && e.Code == "invalid-value");
    }

    [Fact]
    public void InvalidUnitDemandFailsValidation()
    {
        var command = ValidCreateCommand() with
        {
            Demands = [new("d1", "PROD-100", "Product A", 10, "barrels", "normal", null, null, null, null, null)],
        };

        var errors = ProductionPlanDraftValidator.ValidateCreate(command);

        Assert.Contains(errors, e => e.Field == "demands[0].unit" && e.Code == "invalid-value");
    }

    [Fact]
    public void DuplicateDemandIdsFailValidation()
    {
        var command = ValidCreateCommand() with
        {
            Demands =
            [
                new("d1", "PROD-100", "Product A", 10, "pcs", "normal", null, null, null, null, null),
                new("d1", "PROD-101", "Product B", 5, "kg", "high", null, null, null, null, null),
            ],
        };

        var errors = ProductionPlanDraftValidator.ValidateCreate(command);

        Assert.Contains(errors, e => e.Field == "demands[1].id" && e.Code == "duplicate");
    }

    [Fact]
    public void InvalidRiskTypeFailsValidation()
    {
        var command = ValidCreateCommand() with
        {
            Risks = [new("r1", "alien-invasion", "critical", "Risk Title", "Description", false, null)],
        };

        var errors = ProductionPlanDraftValidator.ValidateCreate(command);

        Assert.Contains(errors, e => e.Field == "risks[0].type" && e.Code == "invalid-value");
    }

    [Fact]
    public void BlockingRiskWithoutDescriptionFailsValidation()
    {
        var command = ValidCreateCommand() with
        {
            Risks = [new("r1", "capacity-shortage", "critical", "Risk Title", "", true, null)],
        };

        var errors = ProductionPlanDraftValidator.ValidateCreate(command);

        Assert.Contains(errors, e => e.Field == "risks[0].description");
    }

    [Fact]
    public void ApprovedApprovalStatusIsRejected()
    {
        var command = ValidCreateCommand() with
        {
            Approval = new("approved", null),
        };

        var errors = ProductionPlanDraftValidator.ValidateCreate(command);

        Assert.Contains(errors, e => e.Field == "approval.status" && e.Code == "invalid-value");
    }

    [Fact]
    public void TransferredStatusIsRejected()
    {
        var command = ValidCreateCommand() with
        {
            Transfer = new("completed", null),
        };

        var errors = ProductionPlanDraftValidator.ValidateCreate(command);

        Assert.Contains(errors, e => e.Field == "transfer.status" && e.Code == "invalid-value");
    }

    [Fact]
    public void InvalidConfidenceFailsValidation()
    {
        var command = ValidCreateCommand() with { Confidence = "very-high" };

        var errors = ProductionPlanDraftValidator.ValidateCreate(command);

        Assert.Contains(errors, e => e.Field == "confidence" && e.Code == "invalid-value");
    }

    [Fact]
    public void ValidUpdateCommandPassesValidation()
    {
        var updateCommand = new UpdateProductionPlanDraftCommand(
            FacilityId: Guid.NewGuid(),
            ProductionPlanId: Guid.NewGuid(),
            ExpectedVersion: 1,
            ReferenceCode: "PP-TEST-001",
            Title: "Updated Plan",
            Notes: "Some notes",
            Origin: "manual",
            AiGenerated: false,
            Confidence: null,
            PlannedStartAtUtc: ValidStart,
            PlannedEndAtUtc: ValidEnd,
            Demands: [new("d1", "PROD-100", "Product A", 10, "pcs", "normal", null, null, null, null, null)],
            Risks: [],
            Resources: new(null, null, null, null, null, null, null, null),
            TimeWindow: null,
            Approval: null,
            Transfer: null,
            SourceReferences: [],
            AiExplanation: null);

        var errors = ProductionPlanDraftValidator.ValidateUpdate(updateCommand);

        Assert.Empty(errors);
    }

    [Fact]
    public void EditableStatusDraftIsEditableReturnsTrueForDraft()
    {
        Assert.True(ProductionPlanDraftValidator.IsEditableStatus(ProductionPlanStatus.Draft));
    }

    [Fact]
    public void EditableStatusNeedsReviewIsEditable()
    {
        Assert.True(ProductionPlanDraftValidator.IsEditableStatus(ProductionPlanStatus.NeedsReview));
    }

    [Fact]
    public void ApprovedStatusIsNotEditable()
    {
        Assert.False(ProductionPlanDraftValidator.IsEditableStatus(ProductionPlanStatus.Approved));
    }

    [Fact]
    public void ScheduledStatusIsNotEditable()
    {
        Assert.False(ProductionPlanDraftValidator.IsEditableStatus(ProductionPlanStatus.Scheduled));
    }

    [Fact]
    public void CompletedStatusIsNotEditable()
    {
        Assert.False(ProductionPlanDraftValidator.IsEditableStatus(ProductionPlanStatus.Completed));
    }

    [Fact]
    public void CancelledStatusIsNotEditable()
    {
        Assert.False(ProductionPlanDraftValidator.IsEditableStatus(ProductionPlanStatus.Cancelled));
    }

    [Fact]
    public void AllValidationErrorsReturnedInSingleCall()
    {
        var command = ValidCreateCommand() with
        {
            ReferenceCode = "",
            Title = "",
            Demands = [],
        };

        var errors = ProductionPlanDraftValidator.ValidateCreate(command);

        Assert.True(errors.Count >= 3, $"Expected at least 3 errors but got {errors.Count}.");
    }
}
