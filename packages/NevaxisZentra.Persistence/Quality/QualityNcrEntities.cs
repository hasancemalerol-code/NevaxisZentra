namespace NevaxisZentra.Persistence.Quality;

public sealed class QualityNonconformance
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string NcrNumber { get; set; } = string.Empty;
    public Guid? ProductionOrderId { get; set; }
    public Guid? QualityInspectionId { get; set; }
    public Guid? QualityInspectionResultId { get; set; }
    public string? ProductReference { get; set; }
    public string? OperationReference { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }
    public QualityNcrSeverity Severity { get; set; } = QualityNcrSeverity.Minor;
    public QualityNcrStatus Status { get; set; } = QualityNcrStatus.Open;
    public DateTimeOffset DetectedAtUtc { get; set; }
    public string DetectedBySubjectId { get; set; } = string.Empty;
    public string? OwnerSubjectId { get; set; }
    public QualityNcrDispositionStatus DispositionStatus { get; set; } = QualityNcrDispositionStatus.Pending;
    public QualityNcrDispositionType? DispositionType { get; set; }
    public string? DispositionNote { get; set; }
    public string? DispositionDecidedBySubjectId { get; set; }
    public DateTimeOffset? DispositionDecidedAtUtc { get; set; }
    public QualityNcrHoldStatus HoldStatus { get; set; } = QualityNcrHoldStatus.None;
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public string? ClosedBySubjectId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Version { get; set; } = 1;
    public Guid? LinkedAiTaskId { get; set; }
    public string FollowUpStatus { get; set; } = QualityNcrFollowUpStatuses.RequiresReview;
    public string? FollowUpUpdatedBy { get; set; }

    public ICollection<QualityHold> Holds { get; set; } = [];
    public ICollection<QualityCorrectiveAction> CorrectiveActions { get; set; } = [];
}

public sealed class QualityNcrFollowUpAuditEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid NcrId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string Actor { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public QualityNonconformance? Nonconformance { get; set; }
}

public sealed class QualityHold
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid NcrId { get; set; }
    public Guid? ProductionOrderId { get; set; }
    public string Scope { get; set; } = "production-order";
    public QualityHoldRecordStatus Status { get; set; } = QualityHoldRecordStatus.Active;
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset PlacedAtUtc { get; set; }
    public string PlacedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset? ReleasedAtUtc { get; set; }
    public string? ReleasedBySubjectId { get; set; }
    public string? ReleaseReason { get; set; }
    public uint Version { get; set; } = 1;

    public QualityNonconformance? Nonconformance { get; set; }
}

public sealed class QualityCorrectiveAction
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid NcrId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? OwnerSubjectId { get; set; }
    public QualityCorrectiveActionStatus Status { get; set; } = QualityCorrectiveActionStatus.Open;
    public DateTimeOffset? DueAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? CompletedBySubjectId { get; set; }
    public string? CompletionNote { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Version { get; set; } = 1;

    public QualityNonconformance? Nonconformance { get; set; }
}
