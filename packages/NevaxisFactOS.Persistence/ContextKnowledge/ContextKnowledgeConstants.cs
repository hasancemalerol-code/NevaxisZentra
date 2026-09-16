namespace NevaxisFactOS.Persistence.ContextKnowledge;

public static class ContextKnowledgePermissionKeys
{
    public const string Read = "context-knowledge.read";
    public const string Manage = "context-knowledge.manage";
    public const string Promote = "context-knowledge.promote";
}

public static class ContextKnowledgeFailureCodes
{
    public const string NotFound = "context-knowledge.not-found";
    public const string ValidationFailed = "context-knowledge.validation-failed";
    public const string InsufficientContext = "context-knowledge.insufficient-context";
    public const string StaleContext = "context-knowledge.stale-context";
    public const string ConflictedState = "context-knowledge.conflicted-state";
    public const string SourceUnavailable = "context-knowledge.source-unavailable";
    public const string KnowledgeNotFound = "context-knowledge.knowledge-not-found";
    public const string InvalidContext = "context-knowledge.invalid-context";
    public const string PromotionRejected = "context-knowledge.promotion-rejected";
}

public static class ContextKnowledgeDataTiers
{
    public const string HotOperational = "HOT_OPERATIONAL";
    public const string HistoricalOperational = "HISTORICAL_OPERATIONAL";
    public const string VerifiedFactoryKnowledge = "VERIFIED_FACTORY_KNOWLEDGE";
}

public static class ContextKnowledgeResolutionStatuses
{
    public const string Resolved = "RESOLVED";
    public const string Conflicted = "CONFLICTED";
    public const string Unknown = "UNKNOWN";
}

public static class ContextKnowledgeDataQualityReasons
{
    public const string Duplicate = "DUPLICATE";
    public const string Stale = "STALE";
    public const string Conflicting = "CONFLICTING";
    public const string Incomplete = "INCOMPLETE";
    public const string Quarantined = "QUARANTINED";
    public const string Invalid = "INVALID";
    public const string UntrustedSource = "UNTRUSTED_SOURCE";
    public const string InvalidTimestamp = "INVALID_TIMESTAMP";
    public const string InvalidTenant = "INVALID_TENANT";
    public const string InvalidRelationship = "INVALID_RELATIONSHIP";
    public const string Accepted = "ACCEPTED";
}

public static class ContextKnowledgeFailureStates
{
    public const string InsufficientContext = "INSUFFICIENT_CONTEXT";
    public const string StaleContext = "STALE_CONTEXT";
    public const string ConflictedState = "CONFLICTED_STATE";
    public const string SourceUnavailable = "SOURCE_UNAVAILABLE";
    public const string KnowledgeNotFound = "KNOWLEDGE_NOT_FOUND";
    public const string InvalidContext = "INVALID_CONTEXT";
}

public static class ContextKnowledgePromotionStatuses
{
    public const string Verified = "VERIFIED";
    public const string LearningOnly = "LEARNING_ONLY";
    public const string Rejected = "REJECTED";
    public const string Pending = "PENDING";
}

public static class ContextKnowledgeScopeEntityTypes
{
    public const string Tenant = "tenant";
    public const string Facility = "facility";
    public const string Area = "area";
    public const string Line = "line";
    public const string Station = "station";
    public const string Operation = "operation";
    public const string Resource = "resource";
    public const string Machine = "machine";
    public const string ProductionOrder = "production_order";
    public const string Mold = "mold";
    public const string Material = "material";
    public const string DigitalWorkCase = "digital_work_case";
}

public static class ContextKnowledgeSourceTypes
{
    public const string Erp = "ERP";
    public const string Mes = "MES";
    public const string Plc = "PLC";
    public const string Operator = "OPERATOR";
    public const string Integration = "INTEGRATION";
    public const string CanonicalEvent = "CANONICAL_EVENT";
    public const string DigitalWorkCase = "DIGITAL_WORK_CASE";
    public const string VerifiedKnowledge = "VERIFIED_KNOWLEDGE";
}

public static class ContextKnowledgeLifecycleTiers
{
    public const string Hot = "HOT";
    public const string Warm = "WARM";
    public const string Cold = "COLD";
}

public static class ContextKnowledgeAggregationGranularities
{
    public const string Raw = "RAW";
    public const string Minute = "MINUTE";
    public const string Hour = "HOUR";
    public const string Day = "DAY";
}

public static class ContextKnowledgeEngineVersions
{
    public const string CanonicalState = "context-canonical-state-v1";
    public const string ContextBuilder = "context-builder-v1";
    public const string HybridRetrieval = "context-hybrid-retrieval-v1";
    public const string KnowledgePromotion = "context-knowledge-promotion-v1";
    public const string Snapshot = "context-snapshot-v1";
}

public static class ContextKnowledgeDefaultSourcePriorities
{
    public static readonly IReadOnlyDictionary<string, int> Priorities =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [ContextKnowledgeSourceTypes.Plc] = 100,
            [ContextKnowledgeSourceTypes.Mes] = 90,
            [ContextKnowledgeSourceTypes.CanonicalEvent] = 85,
            [ContextKnowledgeSourceTypes.Operator] = 70,
            [ContextKnowledgeSourceTypes.Erp] = 60,
            [ContextKnowledgeSourceTypes.Integration] = 55,
            [ContextKnowledgeSourceTypes.DigitalWorkCase] = 50,
            [ContextKnowledgeSourceTypes.VerifiedKnowledge] = 40,
        };
}
