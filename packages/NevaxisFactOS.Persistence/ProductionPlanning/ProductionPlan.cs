using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.ProductionPlanning;

public sealed class ProductionPlan : ITenantScopedEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid FacilityId { get; set; }

    public string ReferenceCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public ProductionPlanStatus Status { get; set; }

    public ProductionPlanOrigin Origin { get; set; }

    public bool AiGenerated { get; set; }

    public ProductionPlanConfidence Confidence { get; set; }

    public DateTimeOffset PlannedStartAtUtc { get; set; }

    public DateTimeOffset PlannedEndAtUtc { get; set; }

    public string? Notes { get; set; }

    public ProductionPlanApprovalStatus ApprovalStatus { get; set; }

    public ProductionPlanTransferStatus TransferStatus { get; set; }

    public ProductionPlanTargetSystem? TransferTargetSystem { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public Guid? LastModifiedBySubjectId { get; set; }

    public string? AiExplanation { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public uint Version { get; set; }

    public string DemandsJson { get; set; } = "[]";

    public string RisksJson { get; set; } = "[]";

    public string ResourceAssignmentJson { get; set; } = "{}";

    public string TimeWindowJson { get; set; } = "{}";

    public string ApprovalJson { get; set; } = "{}";

    public string TransferJson { get; set; } = "{}";

    public string SourceReferencesJson { get; set; } = "[]";

    public string HistoryJson { get; set; } = "[]";
}
