namespace NevaxisZentra.Persistence.ShiftOperations;

public sealed class OperationalShiftInstance
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? ProductionShiftId { get; set; }
    public string ShiftCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateOnly ShiftDate { get; set; }
    public DateTimeOffset PlannedStartUtc { get; set; }
    public DateTimeOffset PlannedEndUtc { get; set; }
    public DateTimeOffset? ActualStartUtc { get; set; }
    public DateTimeOffset? ActualEndUtc { get; set; }
    public string Status { get; set; } = ShiftInstanceStatuses.Planned;
    public string HandoverState { get; set; } = "none";
    public Guid? SupervisorMembershipId { get; set; }
    public string? SupervisorLabel { get; set; }
    public string? TeamLabel { get; set; }
    public string ProductionContextJson { get; set; } = "{}";
    public decimal HandoverReadinessScore { get; set; }
    public string? AiSummaryJson { get; set; }
    public Guid? FactoryHandoverId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long ConcurrencyToken { get; set; } = 1;

    public ICollection<ShiftOpenItem> OpenItems { get; set; } = [];
}

public sealed class ShiftOpenItem
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ShiftInstanceId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string Category { get; set; } = ShiftOpenItemCategories.Other;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = ShiftHandoverPriorities.Normal;
    public string Status { get; set; } = ShiftOpenItemStatuses.Open;
    public string OwnerType { get; set; } = "membership";
    public string? OwnerId { get; set; }
    public Guid? PrimaryWorkerId { get; set; }
    public Guid? LinkedAiTaskId { get; set; }
    public Guid? DigitalWorkCaseId { get; set; }
    public DateTimeOffset? DeadlineUtc { get; set; }
    public bool RequiresHandover { get; set; } = true;
    public string HandoverPriority { get; set; } = ShiftHandoverPriorities.Normal;
    public bool VerificationRequired { get; set; }
    public string? ProvenanceJson { get; set; }
    public string? FreshnessJson { get; set; }
    public Guid? CarriedFromShiftInstanceId { get; set; }
    public Guid? CarriedToShiftInstanceId { get; set; }
    public int CarryoverCount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long ConcurrencyToken { get; set; } = 1;

    public OperationalShiftInstance? ShiftInstance { get; set; }
}

public sealed class ShiftHandoverAcceptance
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ShiftInstanceId { get; set; }
    public Guid FactoryHandoverId { get; set; }
    public Guid OutgoingMembershipId { get; set; }
    public Guid IncomingMembershipId { get; set; }
    public string? OutgoingSupervisorLabel { get; set; }
    public string? IncomingSupervisorLabel { get; set; }
    public DateTimeOffset PreparedAtUtc { get; set; }
    public DateTimeOffset? HandedOverAtUtc { get; set; }
    public DateTimeOffset? AcceptedAtUtc { get; set; }
    public string? OutgoingComments { get; set; }
    public string? IncomingAcknowledgement { get; set; }
    public bool Verified { get; set; }
    public string? VerificationNotes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public long ConcurrencyToken { get; set; } = 1;
}
