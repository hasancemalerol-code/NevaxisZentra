using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Persistence.Production;

public sealed class ProductionOrder : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductionPlanHandoffId { get; set; }
    public Guid SourceProductionPlanId { get; set; }
    public uint SourceProductionPlanVersion { get; set; }
    public string ReferenceCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public ProductionOrderStatus Status { get; set; }
    public DateTimeOffset PlannedStartAtUtc { get; set; }
    public DateTimeOffset PlannedEndAtUtc { get; set; }
    public DateTimeOffset? ActualStartAtUtc { get; set; }
    public DateTimeOffset? ActualEndAtUtc { get; set; }
    public string? ShiftName { get; set; }
    public string? MachineReference { get; set; }
    public string? MoldReference { get; set; }
    public string? ProductReference { get; set; }
    public decimal? PlannedQuantity { get; set; }
    public string? PlannedQuantityUnit { get; set; }
    public decimal GoodQuantity { get; set; }
    public decimal ScrapQuantity { get; set; }
    public DateTimeOffset? LastProductionEntryAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public string LastModifiedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string HistoryJson { get; set; } = "[]";

    // ── Operational summary fields (3L) ─────────────────────────────────────
    // Denormalized counters kept in sync atomically with each operational write.

    /// <summary>Cumulative duration of all ended downtime sessions in seconds.</summary>
    public long TotalDowntimeSeconds { get; set; }

    /// <summary>Cumulative duration of all completed setup sessions in seconds.</summary>
    public long TotalSetupSeconds { get; set; }

    /// <summary>Number of manual cycle entries recorded. Doubles as the next cycle number source.</summary>
    public int ManualCycleCount { get; set; }

    /// <summary>Cumulative manual cycle duration across all recorded cycles in milliseconds.</summary>
    public long TotalManualCycleDurationMilliseconds { get; set; }

    /// <summary>Timestamp of the most recently recorded manual cycle.</summary>
    public DateTimeOffset? LastCycleAtUtc { get; set; }

    // ── Operational summary fields (5L) ─────────────────────────────────────
    // Denormalized for cheap list + header rendering without joining assignments.

    public Guid? CurrentShiftId { get; set; }
    public string? CurrentShiftCode { get; set; }
    public DateTimeOffset? CurrentShiftStartAtUtc { get; set; }
    public DateTimeOffset? CurrentShiftEndAtUtc { get; set; }
    public string? PrimaryOperatorSubjectId { get; set; }
    public int ActiveOperatorCount { get; set; }
    public DateTimeOffset? LastAssignmentAtUtc { get; set; }
}
