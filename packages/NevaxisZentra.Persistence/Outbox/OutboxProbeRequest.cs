using System.Text.Json.Serialization;

namespace NevaxisZentra.Persistence.Outbox;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record OutboxProbeRequest(
    string IdempotencyKey,
    string ProbeId,
    string OutcomeCode,
    string? Summary = null,
    DateTimeOffset? AvailableAtUtc = null);
