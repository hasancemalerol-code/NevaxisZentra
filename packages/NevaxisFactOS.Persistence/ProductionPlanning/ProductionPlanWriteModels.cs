namespace NevaxisFactOS.Persistence.ProductionPlanning;

public sealed record ProductionPlanDemandWriteModel(
    string Id,
    string ProductId,
    string ProductName,
    decimal RequiredQuantity,
    string Unit,
    string Priority,
    string? OrderReference,
    string? CustomerId,
    string? CustomerName,
    string? ProductCode,
    DateTimeOffset? RequestedDeliveryDateUtc);

public sealed record ProductionPlanRiskWriteModel(
    string Id,
    string Type,
    string Severity,
    string Title,
    string Description,
    bool Blocking,
    string? Source);

public sealed record ProductionPlanResourceAssignmentWriteModel(
    string? MachineId,
    string? MachineName,
    string? MoldId,
    string? MoldName,
    string? MaterialId,
    string? MaterialName,
    string? OperatorGroupId,
    string? OperatorGroupName);

public sealed record ProductionPlanTimeWindowWriteModel(
    DateTimeOffset? PlannedStartAtUtc,
    DateTimeOffset? PlannedEndAtUtc,
    int? EstimatedDurationMinutes,
    string? ShiftId,
    string? ShiftName);

public sealed record ProductionPlanApprovalWriteModel(
    string Status,
    string? RequiredRole);

public sealed record ProductionPlanTransferWriteModel(
    string Status,
    string? TargetSystem);

public sealed record ProductionPlanSourceReferenceWriteModel(
    string SourceType,
    string SourceId,
    DateTimeOffset? SourceUpdatedAtUtc);

public sealed record CreateProductionPlanDraftCommand(
    Guid FacilityId,
    string ReferenceCode,
    string Title,
    string? Notes,
    string Origin,
    bool AiGenerated,
    string? Confidence,
    DateTimeOffset PlannedStartAtUtc,
    DateTimeOffset PlannedEndAtUtc,
    IReadOnlyList<ProductionPlanDemandWriteModel> Demands,
    IReadOnlyList<ProductionPlanRiskWriteModel> Risks,
    ProductionPlanResourceAssignmentWriteModel Resources,
    ProductionPlanTimeWindowWriteModel? TimeWindow,
    ProductionPlanApprovalWriteModel? Approval,
    ProductionPlanTransferWriteModel? Transfer,
    IReadOnlyList<ProductionPlanSourceReferenceWriteModel> SourceReferences,
    string? AiExplanation);

public sealed record UpdateProductionPlanDraftCommand(
    Guid FacilityId,
    Guid ProductionPlanId,
    uint ExpectedVersion,
    string ReferenceCode,
    string Title,
    string? Notes,
    string Origin,
    bool AiGenerated,
    string? Confidence,
    DateTimeOffset PlannedStartAtUtc,
    DateTimeOffset PlannedEndAtUtc,
    IReadOnlyList<ProductionPlanDemandWriteModel> Demands,
    IReadOnlyList<ProductionPlanRiskWriteModel> Risks,
    ProductionPlanResourceAssignmentWriteModel Resources,
    ProductionPlanTimeWindowWriteModel? TimeWindow,
    ProductionPlanApprovalWriteModel? Approval,
    ProductionPlanTransferWriteModel? Transfer,
    IReadOnlyList<ProductionPlanSourceReferenceWriteModel> SourceReferences,
    string? AiExplanation);

public sealed record ProductionPlanWriteContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId);

public static class ProductionPlanWriteFailureCodes
{
    public const string InvalidScope = "invalid-scope";
    public const string ValidationFailed = "validation-failed";
    public const string NotFound = "not-found";
    public const string PlanNotEditable = "plan-not-editable";
    public const string ReferenceCodeConflict = "reference-code-conflict";
    public const string ConcurrencyConflict = "concurrency-conflict";
    public const string SourceUnavailable = "source-unavailable";
    public const string DataIntegrity = "data-integrity";
    public const string UnexpectedError = "unexpected-error";
}

public sealed record ProductionPlanWriteFieldError(
    string Field,
    string Code,
    string Message);

public sealed record ProductionPlanWriteResult
{
    public required bool Succeeded { get; init; }
    public Guid? ProductionPlanId { get; init; }
    public string? ReferenceCode { get; init; }
    public string? Status { get; init; }
    public uint? Version { get; init; }
    public DateTimeOffset? CreatedAtUtc { get; init; }
    public DateTimeOffset? UpdatedAtUtc { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public IReadOnlyList<ProductionPlanWriteFieldError>? FieldErrors { get; init; }

    public static ProductionPlanWriteResult Success(
        Guid productionPlanId,
        string referenceCode,
        string status,
        uint version,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new()
        {
            Succeeded = true,
            ProductionPlanId = productionPlanId,
            ReferenceCode = referenceCode,
            Status = status,
            Version = version,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = updatedAtUtc,
        };

    public static ProductionPlanWriteResult Fail(
        string failureCode,
        string reason,
        IReadOnlyList<ProductionPlanWriteFieldError>? fieldErrors = null) =>
        new()
        {
            Succeeded = false,
            FailureCode = failureCode,
            FailureReason = reason,
            FieldErrors = fieldErrors,
        };
}
