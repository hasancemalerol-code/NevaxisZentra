namespace CrevaFactOS.Persistence.ShiftHandover;

public sealed class FactoryShiftHandover
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string HandoverReference { get; set; } = string.Empty;
    public DateOnly ShiftDate { get; set; }
    public string ShiftPeriod { get; set; } = ShiftHandoverShiftPeriods.Morning;
    public string Department { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public Guid OutgoingMembershipId { get; set; }
    public Guid? IncomingMembershipId { get; set; }
    public string Status { get; set; } = ShiftHandoverStatuses.Draft;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? HandedOverAtUtc { get; set; }
    public DateTimeOffset? AcknowledgedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public uint Version { get; set; } = 1;

    public ICollection<ShiftHandoverItem> Items { get; set; } = [];
    public ICollection<ShiftHandoverEvent> Events { get; set; } = [];
}

public sealed class ShiftHandoverItem
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid HandoverId { get; set; }
    public int SortOrder { get; set; }
    public string Category { get; set; } = ShiftHandoverItemCategories.General;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = ShiftHandoverItemSeverities.Medium;
    public string? SourceModule { get; set; }
    public string? SourceEntityId { get; set; }
    public string Status { get; set; } = ShiftHandoverItemStatuses.Open;
    public bool RequiresFollowUp { get; set; }
    public Guid? LinkedAiTaskId { get; set; }
    public string? EvidenceMetadataJson { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Version { get; set; } = 1;

    public FactoryShiftHandover? Handover { get; set; }
}

public sealed class ShiftHandoverEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid HandoverId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string ActorMembershipId { get; set; } = string.Empty;
    public string ActorLabel { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset OccurredAtUtc { get; set; }

    public FactoryShiftHandover? Handover { get; set; }
}
