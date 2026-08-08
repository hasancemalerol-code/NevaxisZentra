namespace CrevaFactOS.Persistence.Export;

/// <summary>Pure deterministic country compliance evaluator (no I/O, no LLM).</summary>
public static class ExportComplianceEngine
{
    public sealed record EvaluationInput(
        string DestinationCountryCode,
        string? OriginCountryCode,
        ExportOriginConfirmationStatus OriginStatus,
        string? HsCode,
        ExportHsCodeStatus HsCodeStatus,
        string? ProductReference,
        string? ProductGroup,
        string? CustomerReference,
        string? IncotermCode,
        string? TransportMode,
        string? PackagingType,
        IReadOnlyList<ExportDocument> Documents,
        ExportCountryRulePack? VerifiedRulePack,
        IReadOnlyList<ExportCountryComplianceRule> ApplicableRules,
        IReadOnlyList<ExportCustomerRequirement> CustomerRequirements);

    public sealed record RuleEvaluation(
        string RequirementCode,
        string Title,
        string Description,
        ExportComplianceRequirementStatus Status,
        ExportComplianceSeverity Severity,
        bool Blocking,
        string? RequiredDocumentType,
        Guid? RelatedDocumentId,
        string SourceReference,
        Guid? RulePackId,
        Guid? RuleId,
        int? RuleVersionSnapshot);

    public sealed record EvaluationOutput(
        ExportComplianceEvaluationResult Result,
        int ReadinessPercent,
        int BlockingCount,
        int MissingCount,
        bool UnableToEvaluate,
        bool RevalidationRequired,
        IReadOnlyList<RuleEvaluation> Requirements);

    public static EvaluationOutput Evaluate(EvaluationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Documents);
        ArgumentNullException.ThrowIfNull(input.ApplicableRules);
        ArgumentNullException.ThrowIfNull(input.CustomerRequirements);

        if (input.VerifiedRulePack is null)
        {
            return new EvaluationOutput(
                ExportComplianceEvaluationResult.UnableToEvaluate,
                0,
                1,
                1,
                UnableToEvaluate: true,
                RevalidationRequired: false,
                [
                    new RuleEvaluation(
                        "compliance-rule-pack-missing",
                        "Verified country rule pack missing",
                        "No verified rule pack exists for this destination/product combination.",
                        ExportComplianceRequirementStatus.UnableToEvaluate,
                        ExportComplianceSeverity.Blocking,
                        Blocking: true,
                        RequiredDocumentType: null,
                        RelatedDocumentId: null,
                        SourceReference: "system:compliance-engine",
                        RulePackId: null,
                        RuleId: null,
                        RuleVersionSnapshot: null),
                ]);
        }

        var requirements = new List<RuleEvaluation>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var rule in input.ApplicableRules.Where(r => RuleApplies(r, input, today)))
        {
            requirements.Add(EvaluateRule(rule, input));
        }

        foreach (var overlay in input.CustomerRequirements.Where(c => CustomerRequirementApplies(c, input, today)))
        {
            requirements.Add(EvaluateCustomerRequirement(overlay, input));
        }

        if (requirements.Count == 0)
        {
            requirements.Add(new RuleEvaluation(
                "compliance-no-applicable-rules",
                "No applicable rules in verified pack",
                "The verified rule pack contains no rules matching this shipment context.",
                ExportComplianceRequirementStatus.UnableToEvaluate,
                ExportComplianceSeverity.Blocking,
                Blocking: true,
                RequiredDocumentType: null,
                RelatedDocumentId: null,
                SourceReference: input.VerifiedRulePack.SourceTitle,
                RulePackId: input.VerifiedRulePack.Id,
                RuleId: null,
                RuleVersionSnapshot: input.VerifiedRulePack.VersionNumber));
        }

        var satisfied = requirements.Count(r => r.Status is ExportComplianceRequirementStatus.Satisfied or ExportComplianceRequirementStatus.NotApplicable);
        var total = requirements.Count;
        var blockingUnsatisfied = requirements.Count(r => r.Blocking && r.Status is not ExportComplianceRequirementStatus.Satisfied and not ExportComplianceRequirementStatus.NotApplicable);
        var missing = requirements.Count(r => r.Status is ExportComplianceRequirementStatus.Missing or ExportComplianceRequirementStatus.Pending);
        var unable = requirements.Any(r => r.Status == ExportComplianceRequirementStatus.UnableToEvaluate);

        var percent = total == 0 ? 0 : (int)Math.Round(satisfied / (double)total * 100d, MidpointRounding.AwayFromZero);

        ExportComplianceEvaluationResult result;
        if (unable)
        {
            result = ExportComplianceEvaluationResult.UnableToEvaluate;
        }
        else if (blockingUnsatisfied > 0)
        {
            result = ExportComplianceEvaluationResult.Blocked;
        }
        else if (missing > 0)
        {
            result = ExportComplianceEvaluationResult.Attention;
        }
        else
        {
            result = ExportComplianceEvaluationResult.Ready;
        }

        return new EvaluationOutput(result, percent, blockingUnsatisfied, missing, unable, RevalidationRequired: false, requirements);
    }

    private static bool RuleApplies(ExportCountryComplianceRule rule, EvaluationInput input, DateOnly today)
    {
        if (rule.EffectiveFrom.HasValue && today < rule.EffectiveFrom.Value)
        {
            return false;
        }

        if (rule.EffectiveTo.HasValue && today > rule.EffectiveTo.Value)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(rule.ProductScope)
            && !string.Equals(rule.ProductScope, input.ProductReference, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(rule.ProductGroup)
            && !string.Equals(rule.ProductGroup, input.ProductGroup, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(rule.HsCodePattern)
            && (string.IsNullOrWhiteSpace(input.HsCode)
                || !HsCodeMatches(input.HsCode, rule.HsCodePattern)))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(rule.OriginCountryCode)
            && !string.Equals(rule.OriginCountryCode, input.OriginCountryCode, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(rule.TransportMode)
            && !string.Equals(rule.TransportMode, input.TransportMode, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(rule.IncotermCode)
            && !string.Equals(rule.IncotermCode, input.IncotermCode, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(rule.CustomerCondition)
            && (string.IsNullOrWhiteSpace(input.CustomerReference)
                || !input.CustomerReference.Contains(rule.CustomerCondition, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return true;
    }

    private static bool CustomerRequirementApplies(
        ExportCustomerRequirement req,
        EvaluationInput input,
        DateOnly today)
    {
        if (req.Status != ExportCustomerRequirementStatus.Verified)
        {
            return false;
        }

        if (today < req.EffectiveFrom || (req.EffectiveTo.HasValue && today > req.EffectiveTo.Value))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(req.CountryCode)
            && !string.Equals(req.CountryCode, input.DestinationCountryCode, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(req.ProductScope)
            && !string.Equals(req.ProductScope, input.ProductReference, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(req.CustomerReference)
            && !string.Equals(req.CustomerReference, input.CustomerReference, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static RuleEvaluation EvaluateRule(ExportCountryComplianceRule rule, EvaluationInput input)
    {
        if (rule.RuleType == ExportComplianceRuleType.Origin)
        {
            var originConfirmed = input.OriginStatus is ExportOriginConfirmationStatus.UserConfirmed
                or ExportOriginConfirmationStatus.BrokerConfirmed
                or ExportOriginConfirmationStatus.ExternallyConfirmed;

            return BuildRuleResult(
                rule,
                originConfirmed ? ExportComplianceRequirementStatus.Satisfied : ExportComplianceRequirementStatus.Missing,
                null);
        }

        if (!string.IsNullOrWhiteSpace(rule.RequiredDocumentType))
        {
            var doc = FindBestDocument(input.Documents, rule.RequiredDocumentType);
            if (doc is null)
            {
                return BuildRuleResult(rule, ExportComplianceRequirementStatus.Missing, null);
            }

            var satisfied = doc.DocumentStatus is ExportDocumentStatus.Received or ExportDocumentStatus.Verified;
            return BuildRuleResult(
                rule,
                satisfied ? ExportComplianceRequirementStatus.Satisfied : ExportComplianceRequirementStatus.Pending,
                doc.Id);
        }

        if (!string.IsNullOrWhiteSpace(rule.RequiredCertification))
        {
            return BuildRuleResult(rule, ExportComplianceRequirementStatus.Missing, null);
        }

        if (rule.RequirementMode == ExportComplianceRequirementMode.Informational)
        {
            return BuildRuleResult(rule, ExportComplianceRequirementStatus.NotApplicable, null);
        }

        return BuildRuleResult(rule, ExportComplianceRequirementStatus.Missing, null);
    }

    private static RuleEvaluation EvaluateCustomerRequirement(
        ExportCustomerRequirement req,
        EvaluationInput input)
    {
        var blocking = req.Blocking;
        var severity = req.Severity;
        return new RuleEvaluation(
            $"customer-{req.Id:N}",
            req.Title,
            req.Description,
            ExportComplianceRequirementStatus.Missing,
            severity,
            blocking,
            RequiredDocumentType: null,
            RelatedDocumentId: null,
            SourceReference: $"customer:{req.CustomerReference}",
            RulePackId: null,
            RuleId: null,
            RuleVersionSnapshot: (int)req.Version);
    }

    private static RuleEvaluation BuildRuleResult(
        ExportCountryComplianceRule rule,
        ExportComplianceRequirementStatus status,
        Guid? relatedDocumentId) =>
        new(
            rule.RuleCode,
            rule.Title,
            rule.Description,
            status,
            rule.Severity,
            rule.Blocking,
            rule.RequiredDocumentType,
            relatedDocumentId,
            rule.SourceReference,
            rule.RulePackId,
            rule.Id,
            (int)rule.Version);

    private static ExportDocument? FindBestDocument(IReadOnlyList<ExportDocument> documents, string documentType) =>
        documents
            .Where(d => string.Equals(d.DocumentType, documentType, StringComparison.Ordinal))
            .OrderByDescending(d => d.DocumentStatus == ExportDocumentStatus.Verified)
            .ThenByDescending(d => d.UpdatedAtUtc)
            .FirstOrDefault();

    private static bool HsCodeMatches(string hsCode, string pattern)
    {
        if (pattern.EndsWith(".xx", StringComparison.OrdinalIgnoreCase))
        {
            var prefix = pattern[..^3];
            return hsCode.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(hsCode, pattern, StringComparison.OrdinalIgnoreCase);
    }
}
