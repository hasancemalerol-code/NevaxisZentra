using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.Export;

public sealed class ExportCountryResearchCase : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string ResearchNumber { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string? ProductReference { get; set; }
    public string? ProductDescription { get; set; }
    public string? ProductGroup { get; set; }
    public string? HsCode { get; set; }
    public string? OriginCountryCode { get; set; }
    public ExportCountryResearchCaseStatus Status { get; set; }
    public string? PurposeNotes { get; set; }
    public Guid? LinkedExportCaseId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public string? ReviewedBySubjectId { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string LastModifiedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; }
}

public sealed class ExportCountryResearchFinding : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CountryResearchCaseId { get; set; }
    public ExportCountryResearchFindingCategory Category { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public ExportComplianceRuleType? RequirementType { get; set; }
    public ExportComplianceSeverity? Severity { get; set; }
    public bool BlockingPotential { get; set; }
    public string SourcePublisher { get; set; } = string.Empty;
    public string SourceTitle { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
    public DateOnly? SourcePublicationDate { get; set; }
    public DateTimeOffset SourceAccessedAtUtc { get; set; }
    public string? SourceVersion { get; set; }
    public DateOnly? EffectiveDate { get; set; }
    public int Confidence { get; set; }
    public ExportSourceVerificationStatus VerificationStatus { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; }
}

public sealed class ExportCountryKnowledgeEntry : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string? ProductScope { get; set; }
    public string? ProductGroup { get; set; }
    public string? HsCodePattern { get; set; }
    public string? CustomerReference { get; set; }
    public ExportCountryResearchFindingCategory Category { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string SourcePublisher { get; set; } = string.Empty;
    public string SourceTitle { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
    public DateOnly? SourcePublicationDate { get; set; }
    public DateTimeOffset SourceAccessedAtUtc { get; set; }
    public string? SourceVersion { get; set; }
    public ExportCountryKnowledgeAuthority Authority { get; set; }
    public ExportSourceVerificationStatus VerificationStatus { get; set; }
    public DateTimeOffset? VerifiedAtUtc { get; set; }
    public string? VerifiedBySubjectId { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public Guid? SupersededById { get; set; }
    public Guid? CreatedFromResearchCaseId { get; set; }
    public Guid? CreatedFromExportCaseId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string LastModifiedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; }
}
