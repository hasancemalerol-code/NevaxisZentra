using CrevaFactOS.Persistence.ProductionPlanning;

namespace CrevaFactOS.Api.ProductionPlanning;

public sealed record SubmitProductionPlanForReviewRequest(
    uint ExpectedVersion,
    string? Comment);

public sealed record RequestProductionPlanRevisionRequest(
    uint ExpectedVersion,
    string Reason);

public sealed record ApproveProductionPlanRequest(
    uint ExpectedVersion,
    string? Note);

public sealed record RejectProductionPlanRequest(
    uint ExpectedVersion,
    string Reason);

public sealed record ScheduleProductionPlanRequest(
    uint ExpectedVersion,
    DateTimeOffset PlannedStartAtUtc,
    DateTimeOffset PlannedEndAtUtc,
    string? ShiftName,
    string? SchedulingComment);

public sealed record ProductionPlanWorkflowApiResult(
    bool Succeeded,
    Guid? ProductionPlanId,
    string? ReferenceCode,
    string? PreviousStatus,
    string? Status,
    uint? Version,
    DateTimeOffset? UpdatedAtUtc,
    string? ApprovalStatus,
    DateTimeOffset? PlannedStartAtUtc,
    DateTimeOffset? PlannedEndAtUtc,
    DateTimeOffset? ScheduledAtUtc,
    string? FailureCode,
    string? FailureReason,
    IReadOnlyList<ProductionPlanWorkflowFieldError>? FieldErrors);

public sealed record ProductionPlanWorkflowSuccessResponse(
    Guid Id,
    string ReferenceCode,
    string PreviousStatus,
    string Status,
    uint Version,
    DateTimeOffset UpdatedAtUtc,
    string ApprovalStatus);

public sealed record ProductionPlanScheduleSuccessResponse(
    Guid Id,
    string ReferenceCode,
    string PreviousStatus,
    string Status,
    uint Version,
    DateTimeOffset UpdatedAtUtc,
    string ApprovalStatus,
    DateTimeOffset PlannedStartAtUtc,
    DateTimeOffset PlannedEndAtUtc,
    DateTimeOffset ScheduledAtUtc);

public interface IProductionPlanWorkflowService
{
    Task<ProductionPlanWorkflowApiResult> SubmitForReviewAsync(
        ProductionPlanWriteOperationContext operationContext,
        Guid productionPlanId,
        SubmitProductionPlanForReviewRequest request,
        CancellationToken cancellationToken = default);

    Task<ProductionPlanWorkflowApiResult> RequestRevisionAsync(
        ProductionPlanWriteOperationContext operationContext,
        Guid productionPlanId,
        RequestProductionPlanRevisionRequest request,
        CancellationToken cancellationToken = default);

    Task<ProductionPlanWorkflowApiResult> ApproveAsync(
        ProductionPlanWriteOperationContext operationContext,
        Guid productionPlanId,
        ApproveProductionPlanRequest request,
        CancellationToken cancellationToken = default);

    Task<ProductionPlanWorkflowApiResult> RejectAsync(
        ProductionPlanWriteOperationContext operationContext,
        Guid productionPlanId,
        RejectProductionPlanRequest request,
        CancellationToken cancellationToken = default);

    Task<ProductionPlanWorkflowApiResult> ScheduleAsync(
        ProductionPlanWriteOperationContext operationContext,
        Guid productionPlanId,
        ScheduleProductionPlanRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ProductionPlanWorkflowService(IProductionPlanWorkflowRepository workflowRepository)
    : IProductionPlanWorkflowService
{
    public async Task<ProductionPlanWorkflowApiResult> SubmitForReviewAsync(
        ProductionPlanWriteOperationContext operationContext,
        Guid productionPlanId,
        SubmitProductionPlanForReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operationContext);
        ArgumentNullException.ThrowIfNull(request);

        if (productionPlanId == Guid.Empty)
        {
            return Fail(ProductionPlanWorkflowFailureCodes.InvalidScope, "Production plan identifier is required.");
        }

        var context = BuildContext(operationContext);
        var command = new SubmitProductionPlanForReviewCommand(
            productionPlanId,
            request.ExpectedVersion,
            request.Comment);

        var result = await workflowRepository.SubmitForReviewAsync(context, command, cancellationToken);
        return Map(result);
    }

    public async Task<ProductionPlanWorkflowApiResult> RequestRevisionAsync(
        ProductionPlanWriteOperationContext operationContext,
        Guid productionPlanId,
        RequestProductionPlanRevisionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operationContext);
        ArgumentNullException.ThrowIfNull(request);

        if (productionPlanId == Guid.Empty)
        {
            return Fail(ProductionPlanWorkflowFailureCodes.InvalidScope, "Production plan identifier is required.");
        }

        var context = BuildContext(operationContext);
        var command = new RequestProductionPlanRevisionCommand(
            productionPlanId,
            request.ExpectedVersion,
            request.Reason ?? string.Empty);

        var result = await workflowRepository.RequestRevisionAsync(context, command, cancellationToken);
        return Map(result);
    }

    public async Task<ProductionPlanWorkflowApiResult> ApproveAsync(
        ProductionPlanWriteOperationContext operationContext,
        Guid productionPlanId,
        ApproveProductionPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operationContext);
        ArgumentNullException.ThrowIfNull(request);

        if (productionPlanId == Guid.Empty)
        {
            return Fail(ProductionPlanWorkflowFailureCodes.InvalidScope, "Production plan identifier is required.");
        }

        var context = BuildContext(operationContext);
        var command = new ApproveProductionPlanCommand(
            productionPlanId,
            request.ExpectedVersion,
            request.Note);

        var result = await workflowRepository.ApproveAsync(context, command, cancellationToken);
        return Map(result);
    }

    public async Task<ProductionPlanWorkflowApiResult> RejectAsync(
        ProductionPlanWriteOperationContext operationContext,
        Guid productionPlanId,
        RejectProductionPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operationContext);
        ArgumentNullException.ThrowIfNull(request);

        if (productionPlanId == Guid.Empty)
        {
            return Fail(ProductionPlanWorkflowFailureCodes.InvalidScope, "Production plan identifier is required.");
        }

        var context = BuildContext(operationContext);
        var command = new RejectProductionPlanCommand(
            productionPlanId,
            request.ExpectedVersion,
            request.Reason ?? string.Empty);

        var result = await workflowRepository.RejectAsync(context, command, cancellationToken);
        return Map(result);
    }

    public async Task<ProductionPlanWorkflowApiResult> ScheduleAsync(
        ProductionPlanWriteOperationContext operationContext,
        Guid productionPlanId,
        ScheduleProductionPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operationContext);
        ArgumentNullException.ThrowIfNull(request);

        if (productionPlanId == Guid.Empty)
        {
            return Fail(ProductionPlanWorkflowFailureCodes.InvalidScope, "Production plan identifier is required.");
        }

        var context = BuildContext(operationContext);
        var command = new ScheduleProductionPlanCommand(
            productionPlanId,
            request.ExpectedVersion,
            request.PlannedStartAtUtc,
            request.PlannedEndAtUtc,
            request.ShiftName,
            request.SchedulingComment);

        var result = await workflowRepository.ScheduleAsync(context, command, cancellationToken);
        return Map(result);
    }

    private static ProductionPlanWorkflowContext BuildContext(ProductionPlanWriteOperationContext operationContext) =>
        new(
            operationContext.TenantId,
            operationContext.FacilityId,
            operationContext.MembershipId,
            operationContext.IdentitySubjectId,
            operationContext.PermissionKey,
            operationContext.CorrelationId);

    private static ProductionPlanWorkflowApiResult Map(ProductionPlanWorkflowResult result) =>
        new(
            result.Succeeded,
            result.ProductionPlanId,
            result.ReferenceCode,
            result.PreviousStatus,
            result.Status,
            result.Version,
            result.UpdatedAtUtc,
            result.ApprovalStatus,
            result.PlannedStartAtUtc,
            result.PlannedEndAtUtc,
            result.ScheduledAtUtc,
            result.FailureCode,
            result.FailureReason,
            result.FieldErrors);

    private static ProductionPlanWorkflowApiResult Fail(string failureCode, string reason) =>
        new(false, null, null, null, null, null, null, null, null, null, null, failureCode, reason, null);
}
