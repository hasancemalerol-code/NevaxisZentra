using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.Export;

public sealed class ExportComplianceEvaluation : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ExportCaseId { get; set; }
    public Guid? ShipmentId { get; set; }
    public int EvaluationNumber { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string? OriginCountryCode { get; set; }
    public string? HsCodeSnapshot { get; set; }
    public Guid? RulePackId { get; set; }
    public int? RulePackVersionNumber { get; set; }
    public ExportComplianceEvaluationResult Result { get; set; }
    public int ReadinessPercent { get; set; }
    public bool IsStale { get; set; }
    public bool RevalidationRequired { get; set; }
    public Guid? SupersedesEvaluationId { get; set; }
    public DateTimeOffset EvaluatedAtUtc { get; set; }
    public string EvaluatedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; }
}

public sealed class ExportComplianceRequirement : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ExportCaseId { get; set; }
    public Guid EvaluationId { get; set; }
    public Guid? ShipmentId { get; set; }
    public Guid? RulePackId { get; set; }
    public Guid? RuleId { get; set; }
    public string RequirementCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ExportComplianceRequirementStatus Status { get; set; }
    public ExportComplianceSeverity Severity { get; set; }
    public bool Blocking { get; set; }
    public string? RequiredDocumentType { get; set; }
    public Guid? RelatedDocumentId { get; set; }
    public string? ResolutionType { get; set; }
    public string? ResolutionNote { get; set; }
    public DateTimeOffset EvaluatedAtUtc { get; set; }
    public DateTimeOffset? SatisfiedAtUtc { get; set; }
    public string? SatisfiedBySubjectId { get; set; }
    public string SourceReference { get; set; } = string.Empty;
    public int? RuleVersionSnapshot { get; set; }
    public uint Version { get; set; }
}
