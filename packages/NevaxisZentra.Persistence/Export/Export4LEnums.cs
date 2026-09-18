namespace NevaxisZentra.Persistence.Export;

public enum ExportAiAnalysisRunType
{
    CaseOverview,
    ComplianceExplanation,
    ReadinessAnalysis,
    MissingDataAnalysis,
    CountryResearchAssist,
    NextActions,
    DocumentGapAnalysis,
    CrossModuleRiskAnalysis,
}

public enum ExportAiAnalysisRunStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled,
}

public enum ExportAiFindingCategory
{
    Compliance,
    Document,
    Packing,
    Shipment,
    Broker,
    CountryResearch,
    Quality,
    Production,
    Planning,
    Erp,
    Risk,
    DataQuality,
    Other,
}

public enum ExportAiFindingSeverity
{
    Info,
    Warning,
    Critical,
}

public enum ExportAiSourceType
{
    VerifiedRule,
    VerifiedKnowledge,
    HistoricalObservation,
    BrokerInput,
    OperationalData,
    AiInterpretation,
    SystemDeterministic,
}

public enum ExportAiRecommendationPriority
{
    Low,
    Medium,
    High,
    Critical,
}

public enum ExportAiRecommendationStatus
{
    Proposed,
    Accepted,
    Rejected,
    Dismissed,
    Expired,
}

public enum ExportAiProposedActionType
{
    CreateTask,
    OpenDocument,
    OpenComplianceRequirement,
    OpenBrokerCorrection,
    OpenShipment,
    OpenProductionOrder,
    OpenPlanningRecord,
    RequestHumanReview,
    DraftBrokerQuestion,
}

public enum ExportAiReviewDecision
{
    Accepted,
    Rejected,
    Dismissed,
}

public enum ExportCrossModuleRiskType
{
    ProductionDelay,
    PlanningDeadlineRisk,
    QualityHold,
    MissingMaterial,
    ErpDataMissing,
    ShipmentCutoffRisk,
    ContextUnavailable,
}

public enum ExportCrossModuleRiskSeverity
{
    Info,
    Warning,
    Critical,
}

public enum ExportCrossModuleSourceModule
{
    Production,
    Planning,
    Quality,
    Erp,
    Shipment,
    Export,
}

public enum ExportAiConfidenceBand
{
    Low,
    Medium,
    High,
}
