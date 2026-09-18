namespace NevaxisZentra.Persistence.ProductionPlanning;

public sealed record PlanningLogisticsBufferPolicy(
    int PackingBufferMinutes,
    int QualityReleaseBufferMinutes,
    int ShipmentPreparationBufferMinutes,
    int CustomsBufferMinutes)
{
    public static PlanningLogisticsBufferPolicy DevelopmentDefault { get; } = new(
        PackingBufferMinutes: 240,
        QualityReleaseBufferMinutes: 120,
        ShipmentPreparationBufferMinutes: 60,
        CustomsBufferMinutes: 0);
}

public sealed record PlanningLogisticsSourceProjection(
    bool Linked,
    string? UnavailableReason,
    Guid? ExportCaseId,
    string? ExportCaseNumber,
    Guid? ShipmentId,
    string? ShipmentReference,
    string? SourceOrderReference,
    DateTimeOffset? PlannedPickupAtUtc,
    DateTimeOffset? PlannedDepartureAtUtc,
    string ExportReadinessStatus,
    bool ExportHandoffReady,
    bool ExportHandoffBlocked,
    bool PackingReady,
    bool ShipmentReady,
    string? NormalizedBlockingReason,
    uint SourceVersion,
    DateTimeOffset SourceUpdatedAtUtc);

public sealed record PlanningLogisticsConstraintReadModel(
    Guid Id,
    PlanningLogisticsConstraintType ConstraintType,
    PlanningLogisticsConstraintStatus Status,
    PlanningLogisticsRiskLevel Severity,
    DateTimeOffset? RequiredByUtc,
    DateTimeOffset? CalculatedLatestStartUtc,
    DateTimeOffset? CalculatedLatestCompletionUtc,
    int? BufferMinutes,
    int? SlackMinutes,
    bool Blocking,
    PlanningLogisticsRiskLevel RiskLevel,
    string Reason,
    string SourceReference,
    PlanningLogisticsSourceType SourceType,
    Guid? SourceRecordId,
    Guid? SourceExportCaseId,
    Guid? SourceShipmentId);

public sealed record PlanningLogisticsEvaluationReadModel(
    Guid EvaluationId,
    Guid ProductionPlanId,
    Guid? ExportCaseId,
    string? ExportCaseNumber,
    Guid? ShipmentId,
    string? ShipmentReference,
    string? SourceOrderReference,
    DateTimeOffset? PickupDeadlineUtc,
    DateTimeOffset? DepartureDeadlineUtc,
    DateTimeOffset LatestProductionCompletionUtc,
    DateTimeOffset PlannedProductionCompletionUtc,
    int SlackMinutes,
    PlanningLogisticsRiskLevel RiskLevel,
    bool Blocking,
    string? BlockingReason,
    uint SourceVersion,
    DateTimeOffset EvaluatedAtUtc,
    uint Version,
    IReadOnlyList<PlanningLogisticsConstraintReadModel> Constraints,
    string LinkageStatus);

public sealed record PlanningLogisticsEvaluateCommand(
    Guid TenantId,
    Guid FacilityId,
    Guid ProductionPlanId,
    Guid? IdentitySubjectId,
    Guid? CorrelationId);

public sealed record PlanningLogisticsEvaluationWriteResult
{
    public required bool Succeeded { get; init; }
    public PlanningLogisticsEvaluationReadModel? Evaluation { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static PlanningLogisticsEvaluationWriteResult Success(
        PlanningLogisticsEvaluationReadModel evaluation) =>
        new() { Succeeded = true, Evaluation = evaluation };

    public static PlanningLogisticsEvaluationWriteResult Fail(string code, string reason) =>
        new() { Succeeded = false, FailureCode = code, FailureReason = reason };
}

public sealed record PlanningLogisticsEvaluationReadResult
{
    public required bool Succeeded { get; init; }
    public PlanningLogisticsEvaluationReadModel? Evaluation { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static PlanningLogisticsEvaluationReadResult Success(
        PlanningLogisticsEvaluationReadModel evaluation) =>
        new() { Succeeded = true, Evaluation = evaluation };

    public static PlanningLogisticsEvaluationReadResult Missing() =>
        new() { Succeeded = false, FailureCode = "not-found", FailureReason = "No logistics evaluation exists for this plan." };

    public static PlanningLogisticsEvaluationReadResult Fail(string code, string reason) =>
        new() { Succeeded = false, FailureCode = code, FailureReason = reason };
}

public interface IPlanningMaterialInboundContextReader
{
    Task<PlanningMaterialInboundContext> ReadAsync(
        Guid tenantId,
        Guid facilityId,
        string? orderReference,
        CancellationToken cancellationToken = default);
}

public sealed record PlanningMaterialInboundContext(
    bool Available,
    DateTimeOffset? MaterialEtaUtc,
    string? UnavailableReason);

public sealed class UnavailablePlanningMaterialInboundContextReader : IPlanningMaterialInboundContextReader
{
    public Task<PlanningMaterialInboundContext> ReadAsync(
        Guid tenantId,
        Guid facilityId,
        string? orderReference,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PlanningMaterialInboundContext(
            false,
            null,
            "material-context-unavailable"));
}
