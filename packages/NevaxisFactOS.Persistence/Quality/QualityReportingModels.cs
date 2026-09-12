namespace NevaxisFactOS.Persistence.Quality;

public sealed record QualityReportingFilters(
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    string? ProductReference = null,
    Guid? ProductionOrderId = null,
    Guid? ControlPlanId = null,
    string? OverallResult = null,
    string? ReleaseStatus = null,
    string? NcrSeverity = null,
    string? NcrStatus = null,
    int Limit = 100,
    int Offset = 0);

public sealed record QualityReportingOverviewReadModel(
    int TotalInspectionCount,
    int OpenInspectionCount,
    int CompletedInspectionCount,
    int PassedInspectionCount,
    int FailedInspectionCount,
    int ReleasedInspectionCount,
    int RejectedInspectionCount,
    int OpenNcrCount,
    int CriticalNcrCount,
    int ActiveHoldCount,
    int BlockingProductionOrderCount,
    int OpenCorrectiveActionCount,
    int OverdueCorrectiveActionCount,
    decimal? FirstPassYieldPercent,
    decimal? PassRatePercent,
    decimal? ReleaseRatePercent);

public sealed record QualityTrendPeriodReadModel(
    string PeriodKey,
    DateTimeOffset PeriodStartUtc,
    int InspectionCount,
    int PassCount,
    int FailCount,
    int NcrCount,
    int CriticalNcrCount,
    int HoldCount);

public sealed record QualityFailureBreakdownItemReadModel(
    string Dimension,
    string Key,
    string? Label,
    int Count);

public sealed record QualityNcrAnalyticsReadModel(
    int OpenCount,
    int ResolvedCount,
    int ClosedCount,
    int CriticalCount,
    int MajorCount,
    int MinorCount,
    double? AverageAgeHours,
    double? AverageResolutionHours,
    int OverdueCorrectiveActionCount);

public sealed record QualityHoldAnalyticsReadModel(
    int ActiveCount,
    int ReleasedCount,
    int AffectedProductionOrderCount,
    double? AverageHoldDurationHours);

public sealed record QualityCorrectiveActionAnalyticsReadModel(
    int OpenCount,
    int InProgressCount,
    int CompletedCount,
    int OverdueCount,
    double? AverageCompletionLeadTimeHours);

public sealed record QualityDefinitionHistoryReadModel(
    Guid DefinitionId,
    string DefinitionCode,
    string DefinitionName,
    int SampleCount,
    int FailCount,
    decimal? MinValue,
    decimal? MaxValue,
    decimal? AverageValue);

public sealed record QualityReleaseIntelligenceReadModel(
    bool Available,
    string? InspectionStatus,
    string? OverallResult,
    string? ReleaseStatus,
    bool Blocking,
    int ActiveHoldCount,
    int OpenNcrCount,
    int CriticalNcrCount,
    int IncompleteCorrectiveActionCount,
    bool ReadyForRelease,
    IReadOnlyList<string> ReasonCodes,
    string? ReasonSummary);

public sealed record QualityInspectionControlSummaryReadModel(
    Guid InspectionId,
    string InspectionNumber,
    string FacilityReference,
    Guid ProductionOrderId,
    string? ProductionOrderReference,
    string? ProductReference,
    Guid ControlPlanId,
    string ControlPlanCode,
    string ControlPlanName,
    string ControlPlanRevision,
    DateTimeOffset? InspectionDateUtc,
    string? InspectorSubjectId,
    string OverallResult,
    string ReleaseStatus,
    IReadOnlyList<QualityInspectionControlSummaryMeasurementReadModel> Measurements,
    bool HasBlockingHold,
    int OpenNcrCount,
    string? BlockingSummary);

public sealed record QualityInspectionControlSummaryMeasurementReadModel(
    string DefinitionCode,
    string DefinitionName,
    string? Unit,
    decimal? NominalValue,
    decimal? LowerLimit,
    decimal? UpperLimit,
    bool Required,
    bool Critical,
    decimal? MeasuredNumericValue,
    string? MeasuredTextValue,
    bool? BooleanResult,
    string Result);

public sealed record ProductionOrderTraceabilityReadModel(
    Guid ProductionOrderId,
    string? ProductionOrderReference,
    string? ProductReference,
    IReadOnlyList<QualityInspectionTraceabilityItemReadModel> Inspections,
    IReadOnlyList<QualityNcrTraceabilityItemReadModel> Nonconformances,
    IReadOnlyList<QualityHoldTraceabilityItemReadModel> Holds,
    IReadOnlyList<QualityCorrectiveActionTraceabilityItemReadModel> CorrectiveActions);

public sealed record QualityInspectionTraceabilityItemReadModel(
    Guid Id,
    string InspectionNumber,
    string Status,
    string OverallResult,
    string ReleaseStatus,
    DateTimeOffset? CompletedAtUtc,
    string? InspectorSubjectId,
    IReadOnlyList<QualityResultTraceabilityItemReadModel> Results);

public sealed record QualityResultTraceabilityItemReadModel(
    Guid Id,
    string DefinitionCode,
    string DefinitionName,
    string Result,
    DateTimeOffset RecordedAtUtc,
    string RecordedBySubjectId);

public sealed record QualityNcrTraceabilityItemReadModel(
    Guid Id,
    string NcrNumber,
    string Severity,
    string Status,
    string? DispositionType,
    string HoldStatus,
    DateTimeOffset DetectedAtUtc,
    string? OwnerSubjectId);

public sealed record QualityHoldTraceabilityItemReadModel(
    Guid Id,
    Guid NcrId,
    string Scope,
    string Status,
    string Reason,
    DateTimeOffset PlacedAtUtc,
    string PlacedBySubjectId,
    DateTimeOffset? ReleasedAtUtc,
    string? ReleasedBySubjectId);

public sealed record QualityCorrectiveActionTraceabilityItemReadModel(
    Guid Id,
    Guid NcrId,
    string Title,
    string Status,
    DateTimeOffset? DueAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? OwnerSubjectId);

public sealed record ProductQualityHistoryReadModel(
    string ProductReference,
    IReadOnlyList<QualityInspectionControlSummaryReadModel> RecentInspections,
    IReadOnlyList<QualityNcrTraceabilityItemReadModel> Nonconformances,
    IReadOnlyList<QualityHoldTraceabilityItemReadModel> Holds);

public sealed record QualityExportReadinessProjectionReadModel(
    string? QualityReleaseStatus,
    bool QualityBlocking,
    bool QualityReady,
    string? Reason);
