namespace CrevaFactOS.Persistence.ProductionPlanning;

/// <summary>
/// Development/test-only fake transfer adapter.
/// Must never be registered in production environments.
/// Scenario is controlled by a suffix in the plan reference code or explicit configuration.
/// </summary>
public sealed class FakeProductionPlanTransferAdapter : IProductionPlanTransferAdapter
{
    public const string FakeTargetSystem = "erp";

    private readonly FakeTransferScenario _scenario;

    public FakeProductionPlanTransferAdapter(FakeTransferScenario scenario = FakeTransferScenario.Success)
    {
        _scenario = scenario;
    }

    public string TargetSystem => FakeTargetSystem;

    public Task<ProductionPlanTransferAdapterResult> TransferAsync(
        ProductionPlanTransferPayload payload,
        string idempotencyKey,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var scenario = DetermineScenario(payload);
        var result = scenario switch
        {
            FakeTransferScenario.Success => (ProductionPlanTransferAdapterResult)new ProductionPlanTransferAdapterResult.Success(
                ExternalReference: $"FAKE-ERP-{payload.PlanId:N}",
                ResponseFingerprint: $"fake-fp-{idempotencyKey.GetHashCode():x8}",
                CompletedAtUtc: DateTimeOffset.UtcNow),

            FakeTransferScenario.RetryableFailure => new ProductionPlanTransferAdapterResult.Failure(
                Code: "erp-timeout",
                Retryable: true,
                SafeMessage: "ERP system timed out. Request may be retried."),

            FakeTransferScenario.PermanentFailure => new ProductionPlanTransferAdapterResult.Failure(
                Code: "erp-validation-error",
                Retryable: false,
                SafeMessage: "ERP rejected the plan due to a validation error. Manual correction required."),

            FakeTransferScenario.Uncertain => new ProductionPlanTransferAdapterResult.Uncertain(
                Code: "erp-no-response",
                ReconciliationRequired: true,
                SafeMessage: "No confirmation received from ERP. Manual reconciliation required."),

            _ => new ProductionPlanTransferAdapterResult.Success(
                ExternalReference: $"FAKE-ERP-{payload.PlanId:N}",
                ResponseFingerprint: null,
                CompletedAtUtc: DateTimeOffset.UtcNow),
        };

        return Task.FromResult(result);
    }

    private FakeTransferScenario DetermineScenario(ProductionPlanTransferPayload payload)
    {
        if (payload.ReferenceCode.EndsWith("-RETRY", StringComparison.OrdinalIgnoreCase))
        {
            return FakeTransferScenario.RetryableFailure;
        }

        if (payload.ReferenceCode.EndsWith("-PERM", StringComparison.OrdinalIgnoreCase))
        {
            return FakeTransferScenario.PermanentFailure;
        }

        if (payload.ReferenceCode.EndsWith("-UNCERTAIN", StringComparison.OrdinalIgnoreCase))
        {
            return FakeTransferScenario.Uncertain;
        }

        return _scenario;
    }
}

public enum FakeTransferScenario
{
    Success,
    RetryableFailure,
    PermanentFailure,
    Uncertain,
}
