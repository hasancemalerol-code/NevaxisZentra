namespace CrevaFactOS.Persistence.ProductionPlanning;

public static class ProductionPlanWorkflowValidator
{
    public const int MaxCommentLength = 2000;
    public const int MinReasonLength = 3;
    public const int MaxReasonLength = 2000;
    public const int MaxNoteLength = 2000;

    private static readonly HashSet<ProductionPlanStatus> SubmitAllowedStatuses = new()
    {
        ProductionPlanStatus.Draft,
        ProductionPlanStatus.NeedsReview,
    };

    private static readonly HashSet<ProductionPlanStatus> RevisionAllowedStatuses = new()
    {
        ProductionPlanStatus.ReadyForApproval,
    };

    private static readonly HashSet<ProductionPlanStatus> ApproveAllowedStatuses = new()
    {
        ProductionPlanStatus.ReadyForApproval,
    };

    private static readonly HashSet<ProductionPlanStatus> RejectAllowedStatuses = new()
    {
        ProductionPlanStatus.ReadyForApproval,
    };

    public static IReadOnlyList<ProductionPlanWorkflowFieldError> ValidateSubmitForReview(
        SubmitProductionPlanForReviewCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var errors = new List<ProductionPlanWorkflowFieldError>();

        if (command.ProductionPlanId == Guid.Empty)
        {
            errors.Add(new("productionPlanId", "required", "Production plan identifier is required."));
        }

        if (command.ExpectedVersion == 0)
        {
            errors.Add(new("expectedVersion", "required", "Expected version must be greater than zero."));
        }

        var comment = command.Comment?.Trim();
        if (comment is not null && comment.Length > MaxCommentLength)
        {
            errors.Add(new("comment", "too-long",
                $"Comment must not exceed {MaxCommentLength} characters."));
        }

        return errors;
    }

    public static IReadOnlyList<ProductionPlanWorkflowFieldError> ValidateRequestRevision(
        RequestProductionPlanRevisionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var errors = new List<ProductionPlanWorkflowFieldError>();

        if (command.ProductionPlanId == Guid.Empty)
        {
            errors.Add(new("productionPlanId", "required", "Production plan identifier is required."));
        }

        if (command.ExpectedVersion == 0)
        {
            errors.Add(new("expectedVersion", "required", "Expected version must be greater than zero."));
        }

        var reason = command.Reason?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(reason))
        {
            errors.Add(new("reason", "required", "Revision reason is required."));
        }
        else if (reason.Length < MinReasonLength)
        {
            errors.Add(new("reason", "too-short",
                $"Revision reason must be at least {MinReasonLength} characters."));
        }
        else if (reason.Length > MaxReasonLength)
        {
            errors.Add(new("reason", "too-long",
                $"Revision reason must not exceed {MaxReasonLength} characters."));
        }

        return errors;
    }

    public static IReadOnlyList<ProductionPlanWorkflowFieldError> ValidateApprove(
        ApproveProductionPlanCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var errors = new List<ProductionPlanWorkflowFieldError>();

        if (command.ProductionPlanId == Guid.Empty)
        {
            errors.Add(new("productionPlanId", "required", "Production plan identifier is required."));
        }

        if (command.ExpectedVersion == 0)
        {
            errors.Add(new("expectedVersion", "required", "Expected version must be greater than zero."));
        }

        var note = command.Note?.Trim();
        if (note is not null && note.Length > MaxNoteLength)
        {
            errors.Add(new("note", "too-long",
                $"Approval note must not exceed {MaxNoteLength} characters."));
        }

        return errors;
    }

    public static IReadOnlyList<ProductionPlanWorkflowFieldError> ValidateReject(
        RejectProductionPlanCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var errors = new List<ProductionPlanWorkflowFieldError>();

        if (command.ProductionPlanId == Guid.Empty)
        {
            errors.Add(new("productionPlanId", "required", "Production plan identifier is required."));
        }

        if (command.ExpectedVersion == 0)
        {
            errors.Add(new("expectedVersion", "required", "Expected version must be greater than zero."));
        }

        var reason = command.Reason?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(reason))
        {
            errors.Add(new("reason", "required", "Rejection reason is required."));
        }
        else if (reason.Length < MinReasonLength)
        {
            errors.Add(new("reason", "too-short",
                $"Rejection reason must be at least {MinReasonLength} characters."));
        }
        else if (reason.Length > MaxReasonLength)        {
            errors.Add(new("reason", "too-long",
                $"Rejection reason must not exceed {MaxReasonLength} characters."));
        }

        return errors;
    }

    public static bool IsValidSubmitTransition(ProductionPlanStatus currentStatus) =>
        SubmitAllowedStatuses.Contains(currentStatus);

    public static bool IsValidRevisionTransition(ProductionPlanStatus currentStatus) =>
        RevisionAllowedStatuses.Contains(currentStatus);

    public static bool IsValidApproveTransition(ProductionPlanStatus currentStatus) =>
        ApproveAllowedStatuses.Contains(currentStatus);

    public static bool IsValidRejectTransition(ProductionPlanStatus currentStatus) =>
        RejectAllowedStatuses.Contains(currentStatus);
}
