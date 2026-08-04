namespace CrevaFactOS.Persistence.Outbox;

public static class OutboxMessageTypes
{
    public const string AuditRecorded = "infrastructure.audit.recorded";
    public const string AiDecisionRecorded = "infrastructure.ai-decision.recorded";
    public const string Probe = "infrastructure.outbox.probe";

    public const int SchemaVersion = 1;

    public static readonly HashSet<string> All =
        new([AuditRecorded, AiDecisionRecorded, Probe], StringComparer.Ordinal);
}
