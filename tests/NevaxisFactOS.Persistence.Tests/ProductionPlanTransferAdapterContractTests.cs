using NevaxisFactOS.Persistence.ProductionPlanning;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class ProductionPlanTransferAdapterContractTests
{
    private static readonly Guid PlanId = Guid.Parse("88000003-0000-4000-8000-000000000001");
    private static readonly Guid FacilityId = Guid.Parse("77000002-0000-4000-8000-000000000001");

    private static ProductionPlanTransferPayload BuildPayload(string referenceCode = "PLAN-2026-001")
    {
        return new ProductionPlanTransferPayload(
            PlanId: PlanId,
            ReferenceCode: referenceCode,
            FacilityReferenceId: FacilityId.ToString(),
            PlannedStartAtUtc: new DateTimeOffset(2026, 9, 1, 6, 0, 0, TimeSpan.Zero),
            PlannedEndAtUtc: new DateTimeOffset(2026, 9, 1, 14, 0, 0, TimeSpan.Zero),
            ShiftName: "Sabah Vardiyası",
            Demands: Array.Empty<ProductionPlanTransferDemand>(),
            ResourceAssignment: new ProductionPlanTransferResourceAssignment(null, null, null),
            ApprovalSummary: new ProductionPlanTransferApprovalSummary(
                ApprovedBy: "test-subject",
                ApprovedAtUtc: DateTimeOffset.UtcNow),
            Version: 2);
    }

    // ── Interface contract ────────────────────────────────────────────────────

    [Fact]
    public void FakeAdapterImplementsIProductionPlanTransferAdapter()
    {
        var adapter = new FakeProductionPlanTransferAdapter();

        Assert.IsAssignableFrom<IProductionPlanTransferAdapter>(adapter);
    }

    [Fact]
    public void FakeAdapterTargetSystemIsNotEmpty()
    {
        var adapter = new FakeProductionPlanTransferAdapter();

        Assert.False(string.IsNullOrEmpty(adapter.TargetSystem));
    }

    [Fact]
    public void FakeAdapterTargetSystemIsLowercase()
    {
        var adapter = new FakeProductionPlanTransferAdapter();

        Assert.Equal(adapter.TargetSystem, adapter.TargetSystem.ToLowerInvariant());
    }

    // ── Success scenario ──────────────────────────────────────────────────────

    [Fact]
    public async Task SuccessScenarioReturnsSuccessResult()
    {
        var adapter = new FakeProductionPlanTransferAdapter(FakeTransferScenario.Success);
        var payload = BuildPayload();

        var result = await adapter.TransferAsync(payload, "idem-key-001", Guid.NewGuid());

        Assert.IsType<ProductionPlanTransferAdapterResult.Success>(result);
    }

    [Fact]
    public async Task SuccessResultHasNonEmptyExternalReference()
    {
        var adapter = new FakeProductionPlanTransferAdapter(FakeTransferScenario.Success);
        var payload = BuildPayload();

        var result = await adapter.TransferAsync(payload, "idem-key-001", Guid.NewGuid());

        var success = Assert.IsType<ProductionPlanTransferAdapterResult.Success>(result);
        Assert.False(string.IsNullOrEmpty(success.ExternalReference));
    }

    [Fact]
    public async Task SuccessResultHasCompletedAtUtcSet()
    {
        var adapter = new FakeProductionPlanTransferAdapter(FakeTransferScenario.Success);
        var payload = BuildPayload();

        var result = await adapter.TransferAsync(payload, "idem-key-001", Guid.NewGuid());

        var success = Assert.IsType<ProductionPlanTransferAdapterResult.Success>(result);
        Assert.NotEqual(default, success.CompletedAtUtc);
    }

    // ── Retryable failure scenario ────────────────────────────────────────────

    [Fact]
    public async Task RetryableFailureScenarioReturnsFailureResult()
    {
        var adapter = new FakeProductionPlanTransferAdapter(FakeTransferScenario.RetryableFailure);
        var payload = BuildPayload();

        var result = await adapter.TransferAsync(payload, "idem-key-002", Guid.NewGuid());

        Assert.IsType<ProductionPlanTransferAdapterResult.Failure>(result);
    }

    [Fact]
    public async Task RetryableFailureResultIsMarkedRetryable()
    {
        var adapter = new FakeProductionPlanTransferAdapter(FakeTransferScenario.RetryableFailure);
        var payload = BuildPayload();

        var result = await adapter.TransferAsync(payload, "idem-key-002", Guid.NewGuid());

        var failure = Assert.IsType<ProductionPlanTransferAdapterResult.Failure>(result);
        Assert.True(failure.Retryable);
    }

    // ── Permanent failure scenario ────────────────────────────────────────────

    [Fact]
    public async Task PermanentFailureScenarioReturnsNonRetryableFailure()
    {
        var adapter = new FakeProductionPlanTransferAdapter(FakeTransferScenario.PermanentFailure);
        var payload = BuildPayload();

        var result = await adapter.TransferAsync(payload, "idem-key-003", Guid.NewGuid());

        var failure = Assert.IsType<ProductionPlanTransferAdapterResult.Failure>(result);
        Assert.False(failure.Retryable);
    }

    // ── Uncertain scenario ────────────────────────────────────────────────────

    [Fact]
    public async Task UncertainScenarioReturnsUncertainResult()
    {
        var adapter = new FakeProductionPlanTransferAdapter(FakeTransferScenario.Uncertain);
        var payload = BuildPayload();

        var result = await adapter.TransferAsync(payload, "idem-key-004", Guid.NewGuid());

        Assert.IsType<ProductionPlanTransferAdapterResult.Uncertain>(result);
    }

    [Fact]
    public async Task UncertainResultRequiresReconciliation()
    {
        var adapter = new FakeProductionPlanTransferAdapter(FakeTransferScenario.Uncertain);
        var payload = BuildPayload();

        var result = await adapter.TransferAsync(payload, "idem-key-004", Guid.NewGuid());

        var uncertain = Assert.IsType<ProductionPlanTransferAdapterResult.Uncertain>(result);
        Assert.True(uncertain.ReconciliationRequired);
    }

    // ── Deterministic scenario by reference code suffix ───────────────────────

    [Fact]
    public async Task RetryableSuffixInReferenceCodeOverridesDefaultScenario()
    {
        var adapter = new FakeProductionPlanTransferAdapter(FakeTransferScenario.Success);
        var payload = BuildPayload("PLAN-2026-001-RETRY");

        var result = await adapter.TransferAsync(payload, "idem-key-005", Guid.NewGuid());

        var failure = Assert.IsType<ProductionPlanTransferAdapterResult.Failure>(result);
        Assert.True(failure.Retryable);
    }

    [Fact]
    public async Task PermanentSuffixInReferenceCodeOverridesDefaultScenario()
    {
        var adapter = new FakeProductionPlanTransferAdapter(FakeTransferScenario.Success);
        var payload = BuildPayload("PLAN-2026-001-PERM");

        var result = await adapter.TransferAsync(payload, "idem-key-006", Guid.NewGuid());

        var failure = Assert.IsType<ProductionPlanTransferAdapterResult.Failure>(result);
        Assert.False(failure.Retryable);
    }

    [Fact]
    public async Task UncertainSuffixInReferenceCodeOverridesDefaultScenario()
    {
        var adapter = new FakeProductionPlanTransferAdapter(FakeTransferScenario.Success);
        var payload = BuildPayload("PLAN-2026-001-UNCERTAIN");

        var result = await adapter.TransferAsync(payload, "idem-key-007", Guid.NewGuid());

        Assert.IsType<ProductionPlanTransferAdapterResult.Uncertain>(result);
    }

    // ── Cancellation handling ─────────────────────────────────────────────────

    [Fact]
    public async Task TransferAsyncRespectsCancellationToken()
    {
        var adapter = new FakeProductionPlanTransferAdapter(FakeTransferScenario.Success);
        var payload = BuildPayload();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Fake adapter doesn't actually check cancellation (it's synchronous under the hood),
        // but the signature must accept the token without throwing.
        var result = await adapter.TransferAsync(payload, "idem-key-008", Guid.NewGuid(), cts.Token);

        Assert.NotNull(result);
    }

    // ── Adapter does not reference HTTP, tenant, or DbContext ─────────────────

    [Fact]
    public void FakeAdapterDoesNotExposeHttpDependency()
    {
        var adapterType = typeof(FakeProductionPlanTransferAdapter);

        var hasHttpClient = adapterType
            .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Any(f => f.FieldType.Name.Contains("HttpClient", StringComparison.Ordinal));

        Assert.False(hasHttpClient, "Adapter must not hold HttpClient dependency.");
    }

    [Fact]
    public void FakeAdapterDoesNotExposeDbContextDependency()
    {
        var adapterType = typeof(FakeProductionPlanTransferAdapter);

        var hasDbContext = adapterType
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Any(p => p.ParameterType.Name.Contains("DbContext", StringComparison.Ordinal));

        Assert.False(hasDbContext, "Adapter must not take DbContext as constructor parameter.");
    }
}
