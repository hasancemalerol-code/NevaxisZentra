namespace NevaxisZentra.Persistence.MixedProduction;

public sealed class MixedProductionException : Exception
{
    public MixedProductionException(string failureCode, string message) : base(message)
    {
        FailureCode = failureCode;
    }

    public string FailureCode { get; }
}

public sealed record MixedProductionLineSummary(
    Guid LineId,
    string Code,
    string Name,
    string? CurrentOrderReference,
    string? CurrentProductCode,
    string? CurrentShiftCode,
    string LineState,
    int GoodOutputToday,
    int RejectOutputToday);

public sealed record MixedProductionStationSnapshot(
    Guid StationId,
    string Code,
    string Name,
    string StationKind,
    int SequenceOrder,
    string State,
    string? ResourceCode,
    string? ResourceType,
    string? OperationCode,
    string? OperatorCode,
    int? BufferQuantity,
    int? BufferCapacity,
    int GoodCount,
    int RejectCount);

public sealed record MixedProductionLineDetailSnapshot(
    MixedProductionLineSummary Line,
    IReadOnlyList<MixedProductionStationSnapshot> Stations,
    IReadOnlyList<MixedProductionTimelineEntry> Timeline,
    string PlaybackWindowLabel);

public sealed record MixedProductionTimelineEntry(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    string EventType,
    string SourceCategory,
    string StationCode,
    string? ResourceCode,
    string? OperatorCode,
    string Summary,
    decimal? Quantity,
    string? CorrelationKey);

public sealed record MixedProductionPlaybackFrame(
    DateTimeOffset TimestampUtc,
    string LineState,
    IReadOnlyList<MixedProductionStationPlaybackState> Stations);

public sealed record MixedProductionStationPlaybackState(
    string StationCode,
    string State,
    int? BufferQuantity,
    string? OperatorCode,
    string? LastEventType);

public sealed record MixedProductionPlaybackSnapshot(
    Guid LineId,
    string LineCode,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    IReadOnlyList<MixedProductionPlaybackFrame> Frames);

public sealed record RecordProductionEventRequest(
    Guid FacilityId,
    Guid? LineId,
    Guid? StationId,
    Guid? ResourceId,
    string EventType,
    string? OperationCode,
    string? OrderReference,
    string? ProductCode,
    string? OperatorCode,
    string? ShiftCode,
    decimal? Quantity,
    string? Unit,
    string? ReasonCode,
    string? CorrelationKey,
    string? Source,
    string? SourceEventId,
    string IdempotencyKey,
    DateTimeOffset? OccurredAtUtc);

public sealed record OperatorActionRequest(
    Guid FacilityId,
    Guid StationId,
    string OperatorCode,
    string OperationCode,
    string Action,
    string? OrderReference,
    string? ShiftCode,
    string? Outcome,
    string? ReasonCode,
    Guid? WorkInstructionId);

public sealed record MixedProductionSeedResult(
    Guid LineId,
    string LineCode,
    int StationCount,
    int ResourceCount,
    int EventCount,
    string Message);

public sealed record MixedProductionResourceWorkspaceRow(
    Guid ResourceId,
    string Code,
    string Name,
    string ResourceType,
    string? MachineRegistryCode,
    Guid StationId,
    string StationCode,
    string StationName,
    string StationKind,
    Guid LineId,
    string LineCode,
    string LineName,
    string AreaCode,
    string State,
    string? OperationCode,
    string? OperatorCode,
    string? OrderReference,
    int GoodCount,
    int RejectCount,
    int? BufferQuantity,
    int? BufferCapacity,
    bool IsStarved,
    bool IsBlocked);

public sealed record MixedProductionResourceWorkspaceStatusSummary(
    int Total,
    int Running,
    int Idle,
    int Waiting,
    int Stopped,
    int Fault,
    int AtRisk);

public sealed record MixedProductionResourceWorkspacePayload(
    IReadOnlyList<MixedProductionResourceWorkspaceRow> Resources,
    MixedProductionResourceWorkspaceStatusSummary Status);
