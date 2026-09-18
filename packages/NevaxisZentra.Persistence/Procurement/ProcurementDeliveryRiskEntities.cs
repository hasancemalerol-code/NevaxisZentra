namespace NevaxisZentra.Persistence.Procurement;

public sealed class ProcurementDeliveryRiskAssessment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public string? ExternalSupplierId { get; set; }
    public string? ExternalSupplierCode { get; set; }
    public string? SupplierName { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public string? ExternalProductId { get; set; }
    public string? ExternalProductCode { get; set; }
    public string? LineDescription { get; set; }
    public DateTimeOffset? CommitmentDateUtc { get; set; }
    public decimal? OrderedQuantity { get; set; }
    public decimal? ReceivedQuantity { get; set; }
    public decimal? OpenQuantity { get; set; }
    public string? Unit { get; set; }
    public string Result { get; set; } = ProcurementDeliveryRiskStatuses.InsufficientData;
    public string RuleSetVersion { get; set; } = ProcurementDeliveryRiskConstants.RuleSetVersion;
    public string InputFingerprint { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public string StructuredDrivers { get; set; } = "[]";
    public string StructuredEvidence { get; set; } = "[]";
    public DateTimeOffset? SourceUpdatedAtUtc { get; set; }
    public string? SourceFreshness { get; set; }
    public DateTimeOffset EvaluatedAtUtc { get; set; }
    public bool IsCurrent { get; set; }
    public uint Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public Guid? LinkedAiTaskId { get; set; }
    public string FollowUpStatus { get; set; } = ProcurementDeliveryRiskFollowUpStatuses.RequiresReview;
    public string? FollowUpUpdatedBy { get; set; }
}

public sealed class ProcurementDeliveryRiskFollowUpAuditEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid AssessmentId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string Actor { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }

    public ProcurementDeliveryRiskAssessment? Assessment { get; set; }
}
