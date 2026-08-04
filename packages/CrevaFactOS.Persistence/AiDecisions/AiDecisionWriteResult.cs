namespace CrevaFactOS.Persistence.AiDecisions;

public sealed record AiDecisionWriteResult(
    bool Succeeded,
    Guid? DecisionRecordId,
    Guid? AuditRecordId,
    string? FailureCode)
{
    public static AiDecisionWriteResult Success(Guid decisionId, Guid auditId) =>
        new(true, decisionId, auditId, null);

    public static AiDecisionWriteResult Failure(string code) =>
        new(false, null, null, code);
}
