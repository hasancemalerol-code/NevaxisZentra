namespace NevaxisFactOS.Persistence.Audit;

public sealed record AuditWriteResult(bool Succeeded, Guid? AuditRecordId, string? FailureCode)
{
    public static AuditWriteResult Success(Guid id) => new(true, id, null);

    public static AuditWriteResult Failure(string code) => new(false, null, code);
}
