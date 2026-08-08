namespace CrevaFactOS.Persistence.Export;

internal static class Export3LMapping
{
    public static CountryRulePackReadModel MapRulePack(ExportCountryRulePack pack) =>
        new(
            pack.Id,
            pack.FacilityId,
            pack.CountryCode,
            pack.Name,
            ExportValueConversions.ToStorage(pack.Status),
            pack.VersionNumber,
            pack.EffectiveFrom,
            pack.EffectiveTo,
            pack.SourceTitle,
            pack.SourceUrl,
            pack.SourcePublisher,
            pack.VerifiedAtUtc,
            pack.VerifiedBySubjectId,
            pack.Version,
            pack.UpdatedAtUtc);

    public static CountryComplianceRuleReadModel MapRule(ExportCountryComplianceRule rule) =>
        new(
            rule.Id,
            rule.RulePackId,
            rule.RuleCode,
            rule.Title,
            rule.Description,
            ExportValueConversions.ToStorage(rule.RuleType),
            ExportValueConversions.ToStorage(rule.Severity),
            ExportValueConversions.ToStorage(rule.RequirementMode),
            rule.Blocking,
            rule.RequiredDocumentType,
            rule.ProductScope,
            rule.HsCodePattern,
            rule.SourceReference,
            rule.Version);

    public static ExportComplianceEvaluationReadModel MapEvaluation(ExportComplianceEvaluation evaluation) =>
        new(
            evaluation.Id,
            evaluation.ExportCaseId,
            evaluation.ShipmentId,
            evaluation.EvaluationNumber,
            evaluation.CountryCode,
            evaluation.HsCodeSnapshot,
            evaluation.RulePackId,
            evaluation.RulePackVersionNumber,
            ExportValueConversions.ToStorage(evaluation.Result),
            evaluation.ReadinessPercent,
            evaluation.IsStale,
            evaluation.RevalidationRequired,
            evaluation.SupersedesEvaluationId,
            evaluation.EvaluatedAtUtc,
            evaluation.Version);

    public static ExportComplianceRequirementReadModel MapRequirement(ExportComplianceRequirement requirement) =>
        new(
            requirement.Id,
            requirement.ExportCaseId,
            requirement.EvaluationId,
            requirement.RequirementCode,
            requirement.Title,
            requirement.Description,
            ExportValueConversions.ToStorage(requirement.Status),
            ExportValueConversions.ToStorage(requirement.Severity),
            requirement.Blocking,
            requirement.RequiredDocumentType,
            requirement.RelatedDocumentId,
            requirement.SourceReference,
            requirement.Version);

    public static CountryResearchCaseReadModel MapResearchCase(ExportCountryResearchCase researchCase) =>
        new(
            researchCase.Id,
            researchCase.FacilityId,
            researchCase.ResearchNumber,
            researchCase.CountryCode,
            researchCase.ProductReference,
            researchCase.ProductDescription,
            researchCase.ProductGroup,
            researchCase.HsCode,
            researchCase.OriginCountryCode,
            ExportValueConversions.ToStorage(researchCase.Status),
            researchCase.PurposeNotes,
            researchCase.LinkedExportCaseId,
            researchCase.CreatedAtUtc,
            researchCase.ReviewedAtUtc,
            researchCase.CompletedAtUtc,
            researchCase.Version,
            researchCase.UpdatedAtUtc);

    public static CountryResearchFindingReadModel MapFinding(ExportCountryResearchFinding finding) =>
        new(
            finding.Id,
            finding.CountryResearchCaseId,
            ExportValueConversions.ToStorage(finding.Category),
            finding.Title,
            finding.Summary,
            finding.SourcePublisher,
            finding.SourceTitle,
            finding.SourceUrl,
            finding.Confidence,
            ExportValueConversions.ToStorage(finding.VerificationStatus),
            finding.Version,
            finding.CreatedAtUtc);

    public static CountryKnowledgeEntryReadModel MapKnowledgeEntry(ExportCountryKnowledgeEntry entry) =>
        new(
            entry.Id,
            entry.CountryCode,
            ExportValueConversions.ToStorage(entry.Category),
            entry.Title,
            entry.Summary,
            entry.SourcePublisher,
            entry.SourceTitle,
            entry.SourceUrl,
            ExportValueConversions.ToStorage(entry.Authority),
            ExportValueConversions.ToStorage(entry.VerificationStatus),
            entry.VerifiedAtUtc,
            entry.VerificationStatus == ExportSourceVerificationStatus.RevalidationRequired,
            entry.Version,
            entry.UpdatedAtUtc);
}
