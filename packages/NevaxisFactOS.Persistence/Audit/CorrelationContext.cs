namespace NevaxisFactOS.Persistence.Audit;

public sealed class CorrelationContext
{
    private Guid? correlationId;

    public bool IsAvailable => correlationId.HasValue;

    public Guid CorrelationId => correlationId
        ?? throw new InvalidOperationException("Correlation context is unavailable.");

    public string? RequestId { get; private set; }

    public void Set(Guid value, string? requestId)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Correlation ID cannot be empty.", nameof(value));
        }

        if (correlationId.HasValue)
        {
            throw new InvalidOperationException("Correlation context can be established only once per scope.");
        }

        correlationId = value;
        RequestId = string.IsNullOrWhiteSpace(requestId) ? null : requestId;
    }
}
