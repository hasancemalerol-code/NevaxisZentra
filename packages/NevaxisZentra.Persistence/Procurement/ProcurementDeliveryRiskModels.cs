using NevaxisZentra.Persistence.Erp;

namespace NevaxisZentra.Persistence.Procurement;

public sealed record ProcurementDeliveryRiskDriverReadModel(
    string Code,
    string Summary,
    string? Reference,
    string State);

public sealed record ProcurementDeliveryRiskEvidenceReadModel(
    string Field,
    string? Value,
    string Source,
    DateTimeOffset? RecordedAtUtc,
    bool IsUnknown);

public sealed record ProcurementDeliveryRiskAssessmentResult(
    string Result,
    DateTimeOffset? CommitmentDateUtc,
    decimal? OrderedQuantity,
    decimal? ReceivedQuantity,
    decimal? OpenQuantity,
    string? Unit,
    DateTimeOffset? SourceUpdatedAtUtc,
    string? SourceFreshness,
    IReadOnlyList<ProcurementDeliveryRiskDriverReadModel> Drivers,
    IReadOnlyList<ProcurementDeliveryRiskEvidenceReadModel> Evidence);

public sealed record ProcurementDeliveryRiskRowReadModel(
    Guid Id,
    Guid PurchaseOrderId,
    Guid PurchaseOrderLineId,
    string PurchaseOrderNumber,
    string? ExternalSupplierCode,
    string? SupplierName,
    DateTimeOffset? CommitmentDateUtc,
    decimal? ReceivedQuantity,
    decimal? OpenQuantity,
    string? Unit,
    string Result,
    string? SourceFreshness,
    DateTimeOffset EvaluatedAtUtc,
    string FollowUpStatus,
    Guid? LinkedAiTaskId,
    uint Version);

public sealed record ProcurementDeliveryRiskDetailReadModel(
    Guid Id,
    Guid FacilityId,
    Guid PurchaseOrderId,
    Guid PurchaseOrderLineId,
    string PurchaseOrderNumber,
    string? ExternalSupplierId,
    string? ExternalSupplierCode,
    string? SupplierName,
    string OrderStatus,
    int LineNumber,
    string? ExternalProductId,
    string? ExternalProductCode,
    string? LineDescription,
    DateTimeOffset? CommitmentDateUtc,
    decimal? OrderedQuantity,
    decimal? ReceivedQuantity,
    decimal? OpenQuantity,
    string? Unit,
    string Result,
    string RuleSetVersion,
    string InputFingerprint,
    DateTimeOffset? SourceUpdatedAtUtc,
    string? SourceFreshness,
    IReadOnlyList<ProcurementDeliveryRiskDriverReadModel> Drivers,
    IReadOnlyList<ProcurementDeliveryRiskEvidenceReadModel> Evidence,
    DateTimeOffset EvaluatedAtUtc,
    uint Version);

public sealed record ProcurementDeliveryRiskAssessmentSummaryReadModel(
    Guid Id,
    string Result,
    DateTimeOffset EvaluatedAtUtc,
    string RuleSetVersion);

public sealed record ProcurementDeliveryRiskWorkspaceReadModel(
    string RuleSetVersion,
    string AutomationDisclaimer,
    int OverdueCount,
    int DeliveryRiskCount,
    int OnTrackCount,
    int InsufficientDataCount,
    int SourceConflictCount,
    int RequiresReviewCount,
    int TaskLinkedCount,
    int DataReviewCount,
    DateTimeOffset? LatestEvaluatedAtUtc,
    bool CanRead,
    bool CanManage,
    bool CanCreateTask,
    IReadOnlyList<ProcurementDeliveryRiskRowReadModel> Assessments);

public sealed record ProcurementDeliveryRiskListFilters(
    string? SupplierReference,
    string? Status,
    string? PurchaseOrderReference,
    string? SourceFreshness,
    int Page,
    int PageSize);

public sealed record EvaluateProcurementDeliveryRiskCommand(
    Guid PurchaseOrderLineId,
    string? IdempotencyKey);

public sealed record ProcurementDeliveryRiskWriteResult(
    bool Succeeded,
    Guid? AssessmentId,
    uint? Version,
    bool IdempotentReplay,
    string? FailureCode,
    string? FailureReason)
{
    public static ProcurementDeliveryRiskWriteResult Success(
        Guid assessmentId,
        uint version,
        bool idempotentReplay = false) =>
        new(true, assessmentId, version, idempotentReplay, null, null);

    public static ProcurementDeliveryRiskWriteResult Fail(string failureCode, string? failureReason) =>
        new(false, null, null, false, failureCode, failureReason);
}

public interface IProcurementDeliveryRiskRepository
{
    Task<ProcurementDeliveryRiskWorkspaceReadModel> GetWorkspaceAsync(
        ErpOperationContext context,
        ProcurementDeliveryRiskListFilters filters,
        bool canManage,
        bool canCreateTask,
        CancellationToken cancellationToken = default);

    Task<ProcurementDeliveryRiskDetailReadModel?> GetAssessmentByIdAsync(
        ErpOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProcurementDeliveryRiskAssessmentSummaryReadModel>> ListAssessmentHistoryAsync(
        ErpOperationContext context,
        Guid purchaseOrderLineId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<ProcurementDeliveryRiskWriteResult> EvaluateAsync(
        ErpOperationContext context,
        EvaluateProcurementDeliveryRiskCommand command,
        CancellationToken cancellationToken = default);

    Task<ProcurementDeliveryRiskWriteResult> EvaluateAllOpenLinesAsync(
        ErpOperationContext context,
        string? idempotencyKeyPrefix,
        CancellationToken cancellationToken = default);

    Task<ProcurementDeliveryRiskFollowUpDetailReadModel?> GetFollowUpDetailAsync(
        ErpOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task<ProcurementDeliveryRiskFollowUpWriteResult> LinkTaskAsync(
        ErpOperationContext context,
        LinkProcurementDeliveryRiskFollowUpTaskCommand command,
        CancellationToken cancellationToken = default);

    Task<ProcurementDeliveryRiskFollowUpWriteResult> TransitionFollowUpAsync(
        ErpOperationContext context,
        TransitionProcurementDeliveryRiskFollowUpCommand command,
        CancellationToken cancellationToken = default);
}
