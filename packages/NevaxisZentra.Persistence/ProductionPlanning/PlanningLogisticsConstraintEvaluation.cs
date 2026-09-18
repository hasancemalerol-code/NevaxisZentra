using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.ProductionPlanning;

public sealed class PlanningLogisticsConstraintEvaluation : ITenantScopedEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid FacilityId { get; set; }

    public Guid ProductionPlanId { get; set; }

    public Guid? SourceExportCaseId { get; set; }

    public string? SourceExportCaseNumber { get; set; }

    public Guid? SourceShipmentId { get; set; }

    public string? SourceShipmentReference { get; set; }

    public string? SourceOrderReference { get; set; }

    public uint SourceVersion { get; set; }

    public DateTimeOffset? PickupDeadlineUtc { get; set; }

    public DateTimeOffset? DepartureDeadlineUtc { get; set; }

    public DateTimeOffset LatestProductionCompletionUtc { get; set; }

    public DateTimeOffset PlannedProductionCompletionUtc { get; set; }

    public int SlackMinutes { get; set; }

    public string RiskLevel { get; set; } = PlanningLogisticsRiskPolicy.ToStorage(PlanningLogisticsRiskLevel.None);

    public bool Blocking { get; set; }

    public string? BlockingReason { get; set; }

    public string LinkageStatus { get; set; } = "not-linked";

    public string ConstraintsJson { get; set; } = "[]";

    public DateTimeOffset EvaluatedAtUtc { get; set; }

    public uint Version { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
