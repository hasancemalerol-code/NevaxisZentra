using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.EnterpriseReliability;

namespace NevaxisZentra.Persistence.Tests;

public sealed class EnterpriseReliabilityFoundationTests
{
    [Fact]
    public void PermissionKeysFollowOperabilityConvention()
    {
        Assert.Equal("operability.read", EnterpriseReliabilityPermissionKeys.Read);
        Assert.Equal("operability.manage", EnterpriseReliabilityPermissionKeys.Manage);
        Assert.Equal("operability.alert.manage", EnterpriseReliabilityPermissionKeys.AlertManage);
        Assert.Equal("operability.reconcile.run", EnterpriseReliabilityPermissionKeys.ReconcileRun);
        Assert.Equal("operability.quarantine.replay", EnterpriseReliabilityPermissionKeys.QuarantineReplay);
        Assert.Equal("operability.backup.verify", EnterpriseReliabilityPermissionKeys.BackupVerify);
    }

    [Fact]
    public void CircuitBreakerOpensAfterThresholdFailures()
    {
        var state = IntegrationCircuitBreakerEngine.CreateDefault(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
        CircuitBreakerEvaluation? last = null;
        for (var i = 0; i < state.FailureThreshold; i++)
        {
            last = IntegrationCircuitBreakerEngine.Evaluate(state, false, DateTimeOffset.UtcNow);
        }

        Assert.NotNull(last);
        Assert.False(last!.AllowExecution);
        Assert.Equal(CircuitBreakerStates.Open, last.State);
    }

    [Fact]
    public void DegradedModeBlocksUnsafeMutationsWhenDatabaseUnavailable()
    {
        var snapshots = new List<SystemComponentHealthSnapshot>
        {
            new()
            {
                ComponentKind = SystemComponentKinds.Database,
                ComponentKey = "postgres",
                HealthStatus = SystemHealthStatuses.Unavailable,
            },
        };

        var mode = DegradedModeEngine.ResolveOperatingMode(snapshots);
        Assert.Equal(SystemOperatingModes.DegradedReadOnly, mode);
        Assert.True(DegradedModeEngine.BlocksUnsafeMutation(mode));
    }

    [Fact]
    public void ReconciliationConflictGuardBlocksSilentOverwrite()
    {
        var summary = RecoveryReconciliationEngine.BuildSummary(1, 1, 0, 0, ["conflict"]);
        Assert.Equal(ReconciliationRunStatuses.Conflict, summary.Status);
    }

    [Fact]
    public void CriticalEventsCannotBeSilentlyDropped()
    {
        Assert.False(EdgeEventClassificationEngine.AllowsSilentDrop(EdgeEventClassifications.Critical));
        Assert.True(EdgeEventClassificationEngine.AllowsSilentDrop(EdgeEventClassifications.Disposable));
    }

    [Fact]
    public void MetricCardinalityGuardRedactsGuidLabels()
    {
        var sanitized = MetricCardinalityGuard.SanitizeLabels(new Dictionary<string, string>
        {
            ["service"] = "api",
            ["tenant_bucket"] = Guid.NewGuid().ToString(),
            ["arbitrary_id"] = "ignored",
        });

        Assert.Equal("id-redacted", sanitized["tenant_bucket"]);
        Assert.False(sanitized.ContainsKey("arbitrary_id"));
    }

    [Fact]
    public void BoundedRetryPolicyNeverInfinite()
    {
        var decision = BoundedRetryPolicyEngine.Evaluate(IntegrationRetryCategories.Transient, 100);
        Assert.False(decision.ShouldRetry);
        Assert.Equal("retry.exhausted", decision.FailureCode);
    }
}
