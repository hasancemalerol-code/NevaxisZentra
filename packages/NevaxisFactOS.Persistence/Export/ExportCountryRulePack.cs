using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.Export;

public sealed class ExportCountryRulePack : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? FacilityId { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ExportCountryRulePackStatus Status { get; set; }
    public int VersionNumber { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string SourceTitle { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
    public string SourcePublisher { get; set; } = string.Empty;
    public DateOnly? SourcePublicationDate { get; set; }
    public DateTimeOffset SourceAccessedAtUtc { get; set; }
    public string? SourceVersion { get; set; }
    public DateTimeOffset? VerifiedAtUtc { get; set; }
    public string? VerifiedBySubjectId { get; set; }
    public Guid? SupersedesRulePackId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string LastModifiedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; }
}

public sealed class ExportCountryComplianceRule : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RulePackId { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ExportComplianceRuleType RuleType { get; set; }
    public ExportComplianceSeverity Severity { get; set; }
    public ExportComplianceRequirementMode RequirementMode { get; set; }
    public string? ProductScope { get; set; }
    public string? ProductGroup { get; set; }
    public string? HsCodePattern { get; set; }
    public string? OriginCountryCode { get; set; }
    public string? TransportMode { get; set; }
    public string? IncotermCode { get; set; }
    public string? PackagingType { get; set; }
    public string? CustomerCondition { get; set; }
    public string? RequiredDocumentType { get; set; }
    public string? RequiredField { get; set; }
    public string? RequiredCertification { get; set; }
    public bool Blocking { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string SourceReference { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public uint Version { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
}

public sealed class ExportCustomerRequirement : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string CustomerReference { get; set; } = string.Empty;
    public string? CountryCode { get; set; }
    public string? ProductScope { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ExportComplianceRuleType RequirementType { get; set; }
    public ExportComplianceSeverity Severity { get; set; }
    public bool Blocking { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public ExportCustomerRequirementStatus Status { get; set; }
    public string? VerifiedBySubjectId { get; set; }
    public DateTimeOffset? VerifiedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string LastModifiedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; }
}
