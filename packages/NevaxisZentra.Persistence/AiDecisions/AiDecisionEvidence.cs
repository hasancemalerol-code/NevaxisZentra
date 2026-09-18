using System.Text.Json.Serialization;

namespace NevaxisZentra.Persistence.AiDecisions;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AiDecisionEvidence(
    string EvidenceType,
    string ReferenceId,
    string SourceSystem,
    DateTimeOffset ObservedAtUtc,
    string Version,
    string? Checksum = null,
    string? Summary = null);
