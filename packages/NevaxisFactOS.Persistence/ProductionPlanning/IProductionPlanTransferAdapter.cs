namespace NevaxisFactOS.Persistence.ProductionPlanning;

/// <summary>
/// Adapter boundary for writing a production plan to an external ERP/MES target system.
/// Implementations must not hold a DbContext, HTTP framework types, or tenant secrets directly.
/// External idempotency must be supported via the provided IdempotencyKey.
/// </summary>
public interface IProductionPlanTransferAdapter
{
    string TargetSystem { get; }

    Task<ProductionPlanTransferAdapterResult> TransferAsync(
        ProductionPlanTransferPayload payload,
        string idempotencyKey,
        Guid correlationId,
        CancellationToken cancellationToken = default);
}

public abstract record ProductionPlanTransferAdapterResult
{
    private ProductionPlanTransferAdapterResult() { }

    public sealed record Success(
        string ExternalReference,
        string? ResponseFingerprint,
        DateTimeOffset CompletedAtUtc) : ProductionPlanTransferAdapterResult;

    public sealed record Failure(
        string Code,
        bool Retryable,
        string SafeMessage) : ProductionPlanTransferAdapterResult;

    public sealed record Uncertain(
        string Code,
        bool ReconciliationRequired,
        string SafeMessage) : ProductionPlanTransferAdapterResult;
}
