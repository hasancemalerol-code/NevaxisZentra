namespace CrevaFactOS.Persistence.Export;

public enum ExportCountryRulePackStatus
{
    Draft,
    UnderReview,
    Verified,
    Superseded,
    Archived,
}

public enum ExportComplianceRuleType
{
    Document,
    Certification,
    Labeling,
    Packaging,
    Origin,
    Customs,
    Technical,
    Restriction,
    Inspection,
    Transport,
    Other,
}

public enum ExportComplianceRequirementMode
{
    Mandatory,
    Conditional,
    Informational,
}

public enum ExportComplianceSeverity
{
    Info,
    Warning,
    Blocking,
}

public enum ExportComplianceRequirementStatus
{
    Missing,
    Pending,
    Satisfied,
    NotApplicable,
    UnableToEvaluate,
}

public enum ExportComplianceEvaluationResult
{
    Ready,
    Attention,
    Blocked,
    UnableToEvaluate,
}

public enum ExportHsCodeStatus
{
    Unknown,
    Draft,
    UserConfirmed,
    BrokerConfirmed,
    ExternallyConfirmed,
}

public enum ExportOriginConfirmationStatus
{
    Unknown,
    Draft,
    UserConfirmed,
    BrokerConfirmed,
    ExternallyConfirmed,
}

public enum ExportCountryResearchCaseStatus
{
    Draft,
    Researching,
    ReviewRequired,
    Verified,
    Completed,
    Archived,
}

public enum ExportCountryResearchFindingCategory
{
    Customs,
    Tariff,
    Documentation,
    Origin,
    Technical,
    Certification,
    Labeling,
    Packaging,
    RestrictedGoods,
    TradeAgreement,
    Logistics,
    BrokerQuestion,
    Other,
}

public enum ExportSourceVerificationStatus
{
    Unverified,
    UnderReview,
    Verified,
    Superseded,
    RevalidationRequired,
}

public enum ExportCustomerRequirementStatus
{
    Draft,
    Verified,
    Superseded,
    Archived,
}

public enum ExportCountryKnowledgeAuthority
{
    VerifiedRule,
    HistoricalObservation,
    CustomerRequirement,
    BrokerInput,
    ManualEntry,
}
