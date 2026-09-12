namespace NevaxisFactOS.Persistence.Outbox;

public enum OutboxMessageStatus
{
    Pending,
    Processing,
    Processed,
    Failed,
}
