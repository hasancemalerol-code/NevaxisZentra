using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Persistence.ProductionPlanning;

public sealed class ProductionPlanTransferRequest : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductionPlanId { get; set; }
    public string TargetSystem { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public ProductionPlanTransferRequestStatus Status { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public string RequestedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset RequestedAtUtc { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? ExternalReference { get; set; }
    public string? FailureCode { get; set; }
    public string? SafeFailureMessage { get; set; }
    public bool? RetryableFailure { get; set; }
    public string? ResponseFingerprint { get; set; }
    public Guid CorrelationId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
