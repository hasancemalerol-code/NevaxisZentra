namespace NevaxisFactOS.Persistence.Outbox;

public sealed record OutboxWriteResult(
    bool Succeeded,
    Guid? MessageId,
    bool Duplicate,
    string? FailureCode)
{
    public static OutboxWriteResult Success(Guid id, bool duplicate) =>
        new(true, id, duplicate, null);

    public static OutboxWriteResult Failure(string code) =>
        new(false, null, false, code);
}
