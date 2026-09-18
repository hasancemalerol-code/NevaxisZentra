namespace NevaxisZentra.Persistence.Export;

public static class ExportValueConversions
{
    private static readonly Dictionary<ExportCaseStatus, string> CaseStatusToStorage =
        new()
        {
            [ExportCaseStatus.Open] = "open",
            [ExportCaseStatus.Preparing] = "preparing",
            [ExportCaseStatus.Blocked] = "blocked",
            [ExportCaseStatus.Ready] = "ready",
            [ExportCaseStatus.Closed] = "closed",
        };

    private static readonly Dictionary<string, ExportCaseStatus> StorageToCaseStatus =
        CaseStatusToStorage.ToDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal);

    private static readonly Dictionary<ExportReadinessStatus, string> ReadinessStatusToStorage =
        new()
        {
            [ExportReadinessStatus.NotReady] = "not-ready",
            [ExportReadinessStatus.Attention] = "attention",
            [ExportReadinessStatus.Ready] = "ready",
        };

    private static readonly Dictionary<string, ExportReadinessStatus> StorageToReadinessStatus =
        ReadinessStatusToStorage.ToDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal);

    private static readonly Dictionary<ExportDocumentStatus, string> DocumentStatusToStorage =
        new()
        {
            [ExportDocumentStatus.Missing] = "missing",
            [ExportDocumentStatus.Draft] = "draft",
            [ExportDocumentStatus.Received] = "received",
            [ExportDocumentStatus.Verified] = "verified",
            [ExportDocumentStatus.Rejected] = "rejected",
        };

    private static readonly Dictionary<string, ExportDocumentStatus> StorageToDocumentStatus =
        DocumentStatusToStorage.ToDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal);

    private static readonly Dictionary<ExportActivitySourceType, string> ActivitySourceToStorage =
        new()
        {
            [ExportActivitySourceType.System] = "system",
            [ExportActivitySourceType.User] = "user",
            [ExportActivitySourceType.Integration] = "integration",
            [ExportActivitySourceType.Ai] = "ai",
        };

    private static readonly Dictionary<string, ExportActivitySourceType> StorageToActivitySource =
        ActivitySourceToStorage.ToDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal);

    // EXPORT-2L enums
    private static readonly Dictionary<ExportPackingStatus, string> PackingStatusToStorage =
        new()
        {
            [ExportPackingStatus.Draft] = "draft",
            [ExportPackingStatus.Packing] = "packing",
            [ExportPackingStatus.Complete] = "complete",
            [ExportPackingStatus.Verified] = "verified",
        };

    private static readonly Dictionary<string, ExportPackingStatus> StorageToPackingStatus =
        PackingStatusToStorage.ToDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal);

    private static readonly Dictionary<ExportShipmentStatus, string> ShipmentStatusToStorage =
        new()
        {
            [ExportShipmentStatus.Draft] = "draft",
            [ExportShipmentStatus.Preparing] = "preparing",
            [ExportShipmentStatus.Ready] = "ready",
            [ExportShipmentStatus.HandedToBroker] = "handed-to-broker",
            [ExportShipmentStatus.ClearedForDispatch] = "cleared-for-dispatch",
            [ExportShipmentStatus.Dispatched] = "dispatched",
            [ExportShipmentStatus.Closed] = "closed",
            [ExportShipmentStatus.Cancelled] = "cancelled",
        };

    private static readonly Dictionary<string, ExportShipmentStatus> StorageToShipmentStatus =
        ShipmentStatusToStorage.ToDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal);

    private static readonly Dictionary<ExportBrokerWorkspaceStatus, string> BrokerWorkspaceStatusToStorage =
        new()
        {
            [ExportBrokerWorkspaceStatus.Draft] = "draft",
            [ExportBrokerWorkspaceStatus.PackageReady] = "package-ready",
            [ExportBrokerWorkspaceStatus.Sent] = "sent",
            [ExportBrokerWorkspaceStatus.Acknowledged] = "acknowledged",
            [ExportBrokerWorkspaceStatus.CorrectionRequested] = "correction-requested",
            [ExportBrokerWorkspaceStatus.Ready] = "ready",
            [ExportBrokerWorkspaceStatus.Closed] = "closed",
            [ExportBrokerWorkspaceStatus.Cancelled] = "cancelled",
        };

    private static readonly Dictionary<string, ExportBrokerWorkspaceStatus> StorageToBrokerWorkspaceStatus =
        BrokerWorkspaceStatusToStorage.ToDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal);

    private static readonly Dictionary<ExportBrokerPackageVersionStatus, string> PackageVersionStatusToStorage =
        new()
        {
            [ExportBrokerPackageVersionStatus.Draft] = "draft",
            [ExportBrokerPackageVersionStatus.Sent] = "sent",
            [ExportBrokerPackageVersionStatus.Stale] = "stale",
        };

    private static readonly Dictionary<string, ExportBrokerPackageVersionStatus> StorageToPackageVersionStatus =
        PackageVersionStatusToStorage.ToDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal);

    private static readonly Dictionary<ExportBrokerCorrectionSeverity, string> CorrectionSeverityToStorage =
        new()
        {
            [ExportBrokerCorrectionSeverity.Info] = "info",
            [ExportBrokerCorrectionSeverity.Warning] = "warning",
            [ExportBrokerCorrectionSeverity.Blocking] = "blocking",
        };

    private static readonly Dictionary<string, ExportBrokerCorrectionSeverity> StorageToCorrectionSeverity =
        CorrectionSeverityToStorage.ToDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal);

    private static readonly Dictionary<ExportBrokerCorrectionStatus, string> CorrectionStatusToStorage =
        new()
        {
            [ExportBrokerCorrectionStatus.Open] = "open",
            [ExportBrokerCorrectionStatus.Resolved] = "resolved",
            [ExportBrokerCorrectionStatus.Cancelled] = "cancelled",
        };

    private static readonly Dictionary<string, ExportBrokerCorrectionStatus> StorageToCorrectionStatus =
        CorrectionStatusToStorage.ToDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal);

    public static string ToStorage(ExportCaseStatus status) =>
        CaseStatusToStorage.TryGetValue(status, out var value)
            ? value
            : throw new InvalidOperationException($"Unknown ExportCaseStatus: {status}");

    public static ExportCaseStatus CaseStatusFromStorage(string value) =>
        StorageToCaseStatus.TryGetValue(value, out var status)
            ? status
            : throw new InvalidOperationException($"Unknown export case status value: '{value}'");

    public static bool TryCaseStatusFromStorage(string value, out ExportCaseStatus status) =>
        StorageToCaseStatus.TryGetValue(value, out status);

    public static string ToStorage(ExportReadinessStatus status) =>
        ReadinessStatusToStorage.TryGetValue(status, out var value)
            ? value
            : throw new InvalidOperationException($"Unknown ExportReadinessStatus: {status}");

    public static ExportReadinessStatus ReadinessStatusFromStorage(string value) =>
        StorageToReadinessStatus.TryGetValue(value, out var status)
            ? status
            : throw new InvalidOperationException($"Unknown export readiness status value: '{value}'");

    public static string ToStorage(ExportDocumentStatus status) =>
        DocumentStatusToStorage.TryGetValue(status, out var value)
            ? value
            : throw new InvalidOperationException($"Unknown ExportDocumentStatus: {status}");

    /// <summary>Physical document rows never persist <see cref="ExportDocumentStatus.Missing"/>.</summary>
    public static string ToPhysicalStorage(ExportDocumentStatus status) =>
        status == ExportDocumentStatus.Missing
            ? throw new InvalidOperationException("Missing document status is derived and cannot be persisted.")
            : ToStorage(status);

    public static ExportDocumentStatus DocumentStatusFromStorage(string value) =>
        StorageToDocumentStatus.TryGetValue(value, out var status)
            ? status
            : throw new InvalidOperationException($"Unknown export document status value: '{value}'");

    public static string ToStorage(ExportActivitySourceType sourceType) =>
        ActivitySourceToStorage.TryGetValue(sourceType, out var value)
            ? value
            : throw new InvalidOperationException($"Unknown ExportActivitySourceType: {sourceType}");

    public static ExportActivitySourceType ActivitySourceFromStorage(string value) =>
        StorageToActivitySource.TryGetValue(value, out var sourceType)
            ? sourceType
            : throw new InvalidOperationException($"Unknown export activity source value: '{value}'");

    // EXPORT-2L conversions
    public static string ToStorage(ExportPackingStatus status) =>
        PackingStatusToStorage.TryGetValue(status, out var value)
            ? value
            : throw new InvalidOperationException($"Unknown ExportPackingStatus: {status}");

    public static ExportPackingStatus PackingStatusFromStorage(string value) =>
        StorageToPackingStatus.TryGetValue(value, out var status)
            ? status
            : throw new InvalidOperationException($"Unknown export packing status value: '{value}'");

    public static bool TryPackingStatusFromStorage(string value, out ExportPackingStatus status) =>
        StorageToPackingStatus.TryGetValue(value, out status);

    public static string ToStorage(ExportShipmentStatus status) =>
        ShipmentStatusToStorage.TryGetValue(status, out var value)
            ? value
            : throw new InvalidOperationException($"Unknown ExportShipmentStatus: {status}");

    public static ExportShipmentStatus ShipmentStatusFromStorage(string value) =>
        StorageToShipmentStatus.TryGetValue(value, out var status)
            ? status
            : throw new InvalidOperationException($"Unknown export shipment status value: '{value}'");

    public static bool TryShipmentStatusFromStorage(string value, out ExportShipmentStatus status) =>
        StorageToShipmentStatus.TryGetValue(value, out status);

    public static string ToStorage(ExportBrokerWorkspaceStatus status) =>
        BrokerWorkspaceStatusToStorage.TryGetValue(status, out var value)
            ? value
            : throw new InvalidOperationException($"Unknown ExportBrokerWorkspaceStatus: {status}");

    public static ExportBrokerWorkspaceStatus BrokerWorkspaceStatusFromStorage(string value) =>
        StorageToBrokerWorkspaceStatus.TryGetValue(value, out var status)
            ? status
            : throw new InvalidOperationException($"Unknown export broker workspace status value: '{value}'");

    public static string ToStorage(ExportBrokerPackageVersionStatus status) =>
        PackageVersionStatusToStorage.TryGetValue(status, out var value)
            ? value
            : throw new InvalidOperationException($"Unknown ExportBrokerPackageVersionStatus: {status}");

    public static ExportBrokerPackageVersionStatus PackageVersionStatusFromStorage(string value) =>
        StorageToPackageVersionStatus.TryGetValue(value, out var status)
            ? status
            : throw new InvalidOperationException($"Unknown export broker package version status value: '{value}'");

    public static string ToStorage(ExportBrokerCorrectionSeverity severity) =>
        CorrectionSeverityToStorage.TryGetValue(severity, out var value)
            ? value
            : throw new InvalidOperationException($"Unknown ExportBrokerCorrectionSeverity: {severity}");

    public static ExportBrokerCorrectionSeverity CorrectionSeverityFromStorage(string value) =>
        StorageToCorrectionSeverity.TryGetValue(value, out var severity)
            ? severity
            : throw new InvalidOperationException($"Unknown export broker correction severity value: '{value}'");

    public static string ToStorage(ExportBrokerCorrectionStatus status) =>
        CorrectionStatusToStorage.TryGetValue(status, out var value)
            ? value
            : throw new InvalidOperationException($"Unknown ExportBrokerCorrectionStatus: {status}");

    public static ExportBrokerCorrectionStatus CorrectionStatusFromStorage(string value) =>
        StorageToCorrectionStatus.TryGetValue(value, out var status)
            ? status
            : throw new InvalidOperationException($"Unknown export broker correction status value: '{value}'");

    public static bool TryCorrectionSeverityFromStorage(string value, out ExportBrokerCorrectionSeverity severity) =>
        StorageToCorrectionSeverity.TryGetValue(value, out severity);

    public static bool TryCorrectionStatusFromStorage(string value, out ExportBrokerCorrectionStatus status) =>
        StorageToCorrectionStatus.TryGetValue(value, out status);

    // EXPORT-3L conversions
    public static string ToStorage(ExportCountryRulePackStatus status) =>
        status switch
        {
            ExportCountryRulePackStatus.Draft => "draft",
            ExportCountryRulePackStatus.UnderReview => "under-review",
            ExportCountryRulePackStatus.Verified => "verified",
            ExportCountryRulePackStatus.Superseded => "superseded",
            ExportCountryRulePackStatus.Archived => "archived",
            _ => throw new InvalidOperationException($"Unknown ExportCountryRulePackStatus: {status}"),
        };

    public static string ToStorage(ExportComplianceRuleType ruleType) =>
        ruleType switch
        {
            ExportComplianceRuleType.Document => "document",
            ExportComplianceRuleType.Certification => "certification",
            ExportComplianceRuleType.Labeling => "labeling",
            ExportComplianceRuleType.Packaging => "packaging",
            ExportComplianceRuleType.Origin => "origin",
            ExportComplianceRuleType.Customs => "customs",
            ExportComplianceRuleType.Technical => "technical",
            ExportComplianceRuleType.Restriction => "restriction",
            ExportComplianceRuleType.Inspection => "inspection",
            ExportComplianceRuleType.Transport => "transport",
            ExportComplianceRuleType.Other => "other",
            _ => throw new InvalidOperationException($"Unknown ExportComplianceRuleType: {ruleType}"),
        };

    public static string ToStorage(ExportComplianceSeverity severity) =>
        severity switch
        {
            ExportComplianceSeverity.Info => "info",
            ExportComplianceSeverity.Warning => "warning",
            ExportComplianceSeverity.Blocking => "blocking",
            _ => throw new InvalidOperationException($"Unknown ExportComplianceSeverity: {severity}"),
        };

    public static string ToStorage(ExportComplianceRequirementMode mode) =>
        mode switch
        {
            ExportComplianceRequirementMode.Mandatory => "mandatory",
            ExportComplianceRequirementMode.Conditional => "conditional",
            ExportComplianceRequirementMode.Informational => "informational",
            _ => throw new InvalidOperationException($"Unknown ExportComplianceRequirementMode: {mode}"),
        };

    public static string ToStorage(ExportComplianceRequirementStatus status) =>
        status switch
        {
            ExportComplianceRequirementStatus.Missing => "missing",
            ExportComplianceRequirementStatus.Pending => "pending",
            ExportComplianceRequirementStatus.Satisfied => "satisfied",
            ExportComplianceRequirementStatus.NotApplicable => "not-applicable",
            ExportComplianceRequirementStatus.UnableToEvaluate => "unable-to-evaluate",
            _ => throw new InvalidOperationException($"Unknown ExportComplianceRequirementStatus: {status}"),
        };

    public static string ToStorage(ExportComplianceEvaluationResult result) =>
        result switch
        {
            ExportComplianceEvaluationResult.Ready => "ready",
            ExportComplianceEvaluationResult.Attention => "attention",
            ExportComplianceEvaluationResult.Blocked => "blocked",
            ExportComplianceEvaluationResult.UnableToEvaluate => "unable-to-evaluate",
            _ => throw new InvalidOperationException($"Unknown ExportComplianceEvaluationResult: {result}"),
        };

    public static string ToStorage(ExportCountryResearchCaseStatus status) =>
        status switch
        {
            ExportCountryResearchCaseStatus.Draft => "draft",
            ExportCountryResearchCaseStatus.Researching => "researching",
            ExportCountryResearchCaseStatus.ReviewRequired => "review-required",
            ExportCountryResearchCaseStatus.Verified => "verified",
            ExportCountryResearchCaseStatus.Completed => "completed",
            ExportCountryResearchCaseStatus.Archived => "archived",
            _ => throw new InvalidOperationException($"Unknown ExportCountryResearchCaseStatus: {status}"),
        };

    public static string ToStorage(ExportCountryResearchFindingCategory category) =>
        category switch
        {
            ExportCountryResearchFindingCategory.Customs => "customs",
            ExportCountryResearchFindingCategory.Tariff => "tariff",
            ExportCountryResearchFindingCategory.Documentation => "documentation",
            ExportCountryResearchFindingCategory.Origin => "origin",
            ExportCountryResearchFindingCategory.Technical => "technical",
            ExportCountryResearchFindingCategory.Certification => "certification",
            ExportCountryResearchFindingCategory.Labeling => "labeling",
            ExportCountryResearchFindingCategory.Packaging => "packaging",
            ExportCountryResearchFindingCategory.RestrictedGoods => "restricted-goods",
            ExportCountryResearchFindingCategory.TradeAgreement => "trade-agreement",
            ExportCountryResearchFindingCategory.Logistics => "logistics",
            ExportCountryResearchFindingCategory.BrokerQuestion => "broker-question",
            ExportCountryResearchFindingCategory.Other => "other",
            _ => throw new InvalidOperationException($"Unknown ExportCountryResearchFindingCategory: {category}"),
        };

    public static string ToStorage(ExportSourceVerificationStatus status) =>
        status switch
        {
            ExportSourceVerificationStatus.Unverified => "unverified",
            ExportSourceVerificationStatus.UnderReview => "under-review",
            ExportSourceVerificationStatus.Verified => "verified",
            ExportSourceVerificationStatus.Superseded => "superseded",
            ExportSourceVerificationStatus.RevalidationRequired => "revalidation-required",
            _ => throw new InvalidOperationException($"Unknown ExportSourceVerificationStatus: {status}"),
        };

    public static string ToStorage(ExportCountryKnowledgeAuthority authority) =>
        authority switch
        {
            ExportCountryKnowledgeAuthority.VerifiedRule => "verified-rule",
            ExportCountryKnowledgeAuthority.HistoricalObservation => "historical-observation",
            ExportCountryKnowledgeAuthority.CustomerRequirement => "customer-requirement",
            ExportCountryKnowledgeAuthority.BrokerInput => "broker-input",
            ExportCountryKnowledgeAuthority.ManualEntry => "manual-entry",
            _ => throw new InvalidOperationException($"Unknown ExportCountryKnowledgeAuthority: {authority}"),
        };

    public static string ToStorage(ExportHsCodeStatus status) =>
        status switch
        {
            ExportHsCodeStatus.Unknown => "unknown",
            ExportHsCodeStatus.Draft => "draft",
            ExportHsCodeStatus.UserConfirmed => "user-confirmed",
            ExportHsCodeStatus.BrokerConfirmed => "broker-confirmed",
            ExportHsCodeStatus.ExternallyConfirmed => "externally-confirmed",
            _ => throw new InvalidOperationException($"Unknown ExportHsCodeStatus: {status}"),
        };

    public static string ToStorage(ExportOriginConfirmationStatus status) =>
        status switch
        {
            ExportOriginConfirmationStatus.Unknown => "unknown",
            ExportOriginConfirmationStatus.Draft => "draft",
            ExportOriginConfirmationStatus.UserConfirmed => "user-confirmed",
            ExportOriginConfirmationStatus.BrokerConfirmed => "broker-confirmed",
            ExportOriginConfirmationStatus.ExternallyConfirmed => "externally-confirmed",
            _ => throw new InvalidOperationException($"Unknown ExportOriginConfirmationStatus: {status}"),
        };

    // EXPORT-4L conversions
    public static string ToStorage(ExportAiAnalysisRunType runType) =>
        runType switch
        {
            ExportAiAnalysisRunType.CaseOverview => "case-overview",
            ExportAiAnalysisRunType.ComplianceExplanation => "compliance-explanation",
            ExportAiAnalysisRunType.ReadinessAnalysis => "readiness-analysis",
            ExportAiAnalysisRunType.MissingDataAnalysis => "missing-data-analysis",
            ExportAiAnalysisRunType.CountryResearchAssist => "country-research-assist",
            ExportAiAnalysisRunType.NextActions => "next-actions",
            ExportAiAnalysisRunType.DocumentGapAnalysis => "document-gap-analysis",
            ExportAiAnalysisRunType.CrossModuleRiskAnalysis => "cross-module-risk-analysis",
            _ => throw new InvalidOperationException($"Unknown ExportAiAnalysisRunType: {runType}"),
        };

    public static string ToStorage(ExportAiAnalysisRunStatus status) =>
        status switch
        {
            ExportAiAnalysisRunStatus.Queued => "queued",
            ExportAiAnalysisRunStatus.Running => "running",
            ExportAiAnalysisRunStatus.Completed => "completed",
            ExportAiAnalysisRunStatus.Failed => "failed",
            ExportAiAnalysisRunStatus.Cancelled => "cancelled",
            _ => throw new InvalidOperationException($"Unknown ExportAiAnalysisRunStatus: {status}"),
        };

    public static string ToStorage(ExportAiFindingCategory category) =>
        category.ToString().ToLowerInvariant().Replace("countryresearch", "country-research").Replace("dataquality", "data-quality");

    public static string ToStorage(ExportAiFindingSeverity severity) =>
        severity switch
        {
            ExportAiFindingSeverity.Info => "info",
            ExportAiFindingSeverity.Warning => "warning",
            ExportAiFindingSeverity.Critical => "critical",
            _ => throw new InvalidOperationException($"Unknown ExportAiFindingSeverity: {severity}"),
        };

    public static string ToStorage(ExportAiSourceType sourceType) =>
        sourceType switch
        {
            ExportAiSourceType.VerifiedRule => "verified-rule",
            ExportAiSourceType.VerifiedKnowledge => "verified-knowledge",
            ExportAiSourceType.HistoricalObservation => "historical-observation",
            ExportAiSourceType.BrokerInput => "broker-input",
            ExportAiSourceType.OperationalData => "operational-data",
            ExportAiSourceType.AiInterpretation => "ai-interpretation",
            ExportAiSourceType.SystemDeterministic => "system-deterministic",
            _ => throw new InvalidOperationException($"Unknown ExportAiSourceType: {sourceType}"),
        };

    public static string ToStorage(ExportAiRecommendationPriority priority) =>
        priority.ToString().ToLowerInvariant();

    public static string ToStorage(ExportAiRecommendationStatus status) =>
        status.ToString().ToLowerInvariant();

    public static string ToStorage(ExportAiProposedActionType actionType) =>
        actionType switch
        {
            ExportAiProposedActionType.CreateTask => "create-task",
            ExportAiProposedActionType.OpenDocument => "open-document",
            ExportAiProposedActionType.OpenComplianceRequirement => "open-compliance-requirement",
            ExportAiProposedActionType.OpenBrokerCorrection => "open-broker-correction",
            ExportAiProposedActionType.OpenShipment => "open-shipment",
            ExportAiProposedActionType.OpenProductionOrder => "open-production-order",
            ExportAiProposedActionType.OpenPlanningRecord => "open-planning-record",
            ExportAiProposedActionType.RequestHumanReview => "request-human-review",
            ExportAiProposedActionType.DraftBrokerQuestion => "draft-broker-question",
            _ => throw new InvalidOperationException($"Unknown ExportAiProposedActionType: {actionType}"),
        };

    public static string ToStorage(ExportAiReviewDecision decision) =>
        decision.ToString().ToLowerInvariant();
}
