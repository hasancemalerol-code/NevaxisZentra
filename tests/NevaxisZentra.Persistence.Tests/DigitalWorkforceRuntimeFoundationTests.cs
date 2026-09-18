using NevaxisZentra.Persistence.DigitalWorkforce;

namespace NevaxisZentra.Persistence.Tests;

public sealed class DigitalWorkforceActionClassifierTests
{
    [Fact]
    public void FinancialActionIsClassifiedCorrectly()
    {
        var classification = DigitalWorkforceActionClassifier.Classify("payment.release", amount: 1000m);
        Assert.Equal(DigitalWorkActionClassifications.Financial, classification);
        Assert.True(DigitalWorkforceActionClassifier.RequiresMandatoryApproval(classification));
    }

    [Fact]
    public void MachineSafetyActionIsSafetyCritical()
    {
        var classification = DigitalWorkforceActionClassifier.Classify("machine.command");
        Assert.Equal(DigitalWorkActionClassifications.SafetyCritical, classification);
    }
}

public sealed class DigitalWorkforceExecutionLeaseEngineTests
{
    [Fact]
    public void ExpiredLeaseCanBeReclaimed()
    {
        var execution = new DigitalWorkExecution
        {
            Status = DigitalWorkExecutionStatuses.Running,
            LeaseExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
        };
        Assert.True(DigitalWorkforceExecutionLeaseEngine.CanReclaim(execution, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void HeartbeatRefreshesLeaseForOwner()
    {
        var execution = new DigitalWorkExecution
        {
            ClaimOwner = "worker-1",
            ClaimToken = "token-1",
            LeaseExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(1),
        };
        DigitalWorkforceExecutionLeaseEngine.RefreshHeartbeat(
            execution, "worker-1", "token-1", DateTimeOffset.UtcNow);
        Assert.True(execution.LeaseExpiresAtUtc > DateTimeOffset.UtcNow);
    }
}

public sealed class DigitalWorkforceRetryEngineTests
{
    [Fact]
    public void TransientFailureRetriesWithBackoff()
    {
        var execution = new DigitalWorkExecution { AttemptCount = 1, MaxAttempts = 3 };
        var decision = DigitalWorkforceRetryEngine.Evaluate(execution, "timeout", null, DateTimeOffset.UtcNow);
        Assert.True(decision.ShouldRetry);
        Assert.Equal(DigitalWorkExecutionStatuses.RetryWait, decision.Status);
    }

    [Fact]
    public void PermanentFailureDoesNotRetry()
    {
        var execution = new DigitalWorkExecution { AttemptCount = 3, MaxAttempts = 3 };
        var decision = DigitalWorkforceRetryEngine.Evaluate(execution, "invalid payload", null, DateTimeOffset.UtcNow);
        Assert.False(decision.ShouldRetry);
        Assert.Equal(DigitalWorkExecutionStatuses.Failed, decision.Status);
    }
}

public sealed class DigitalWorkforceApprovalGuardTests
{
    [Fact]
    public void StaleApprovalDetectedWhenCaseVersionChanges()
    {
        var approval = new DigitalWorkApproval { CaseConcurrencyTokenAtRequest = 1, Status = DigitalWorkApprovalStatuses.Approved };
        var workCase = new DigitalWorkCase { ConcurrencyToken = 2 };
        Assert.True(DigitalWorkforceApprovalGuard.IsStale(approval, workCase));
        Assert.False(DigitalWorkforceApprovalGuard.CanResumeExecution(approval, workCase, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ExpiredApprovalCannotResumeExecution()
    {
        var approval = new DigitalWorkApproval
        {
            Status = DigitalWorkApprovalStatuses.Approved,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
            CaseConcurrencyTokenAtRequest = 1,
        };
        var workCase = new DigitalWorkCase { ConcurrencyToken = 1 };
        Assert.False(DigitalWorkforceApprovalGuard.CanResumeExecution(approval, workCase, DateTimeOffset.UtcNow));
    }
}

public sealed class DigitalWorkforceOperationalLimitEngineTests
{
    [Fact]
    public void ConcurrentExecutionLimitBlocksNewWork()
    {
        var result = DigitalWorkforceOperationalLimitEngine.Evaluate(
            10, 2, runningExecutions: 5, maxConcurrentExecutions: 5);
        Assert.False(result.Allowed);
    }
}

public sealed class DigitalWorkforceWorkerHealthEngineTests
{
    [Fact]
    public void WaitingApprovalMapsToHealthState()
    {
        var worker = new DigitalWorker { Enabled = true };
        var health = DigitalWorkforceWorkerHealthEngine.Evaluate(
            worker, 0, 0, 0, waitingApprovals: 2, null, null, false, false);
        Assert.Equal(DigitalWorkerHealthStates.WaitingApproval, health.HealthState);
    }

    [Fact]
    public void KillSwitchMapsToOffline()
    {
        var worker = new DigitalWorker { Enabled = true, IsKilled = true };
        var health = DigitalWorkforceWorkerHealthEngine.Evaluate(
            worker, 0, 0, 0, 0, null, null, true, false);
        Assert.Equal(DigitalWorkerHealthStates.Offline, health.HealthState);
    }
}

public sealed class DigitalWorkforceExecutionIdempotencyTests
{
    [Fact]
    public void DuplicateActionUsesDeterministicKey()
    {
        var tenantId = Guid.NewGuid();
        var caseId = Guid.NewGuid();
        var payload = """{"caseId":"x","action":"execute"}""";
        var key1 = DigitalWorkforceExecutionRuntimeService.BuildIdempotencyKey(
            tenantId, caseId, "execute", "production.plan", payload);
        var key2 = DigitalWorkforceExecutionRuntimeService.BuildIdempotencyKey(
            tenantId, caseId, "execute", "production.plan", payload);
        Assert.Equal(key1, key2);
    }
}

public sealed class DigitalWorkforceToolRegistryTests
{
    [Fact]
    public void UnknownToolIsRejected()
    {
        Assert.Throws<DigitalWorkforceException>(() => DigitalWorkforceToolRegistry.Require("unknown.tool"));
    }

    [Fact]
    public void ProductionPlanToolExecutesWithExternalReference()
    {
        var execution = new DigitalWorkExecution
        {
            Id = Guid.NewGuid(),
            Tool = DigitalWorkforceToolCapabilities.ProductionPlan,
        };
        var definition = DigitalWorkforceToolRegistry.Require(execution.Tool);
        var result = DigitalWorkforceToolExecutor.Execute(execution, definition);
        Assert.Equal(DigitalWorkExternalActionStatuses.Acknowledged, result.ExternalActionStatus);
        Assert.False(string.IsNullOrWhiteSpace(result.ExternalReferenceId));
    }
}

public sealed class DigitalWorkforceAutonomyRuntimeTests
{
    [Fact]
    public void FullModeAllowsExecuteWhenPolicyAllows()
    {
        var worker = new DigitalWorker { AutonomyMode = DigitalWorkforceAutonomyModes.Full };
        var result = DigitalWorkforceAutonomyEngine.Evaluate(worker, DigitalWorkforceActionKind.Execute, false);
        Assert.True(result.Allowed);
    }

    [Fact]
    public void ObserveModeBlocksExecute()
    {
        var worker = new DigitalWorker { AutonomyMode = DigitalWorkforceAutonomyModes.Observe };
        var result = DigitalWorkforceAutonomyEngine.Evaluate(worker, DigitalWorkforceActionKind.Execute, false);
        Assert.False(result.Allowed);
    }
}

public sealed class DigitalWorkforceCoordinatorRuntimeContractTests
{
    [Fact]
    public void ExecutionStatusesCoverDurableLifecycle()
    {
        Assert.Equal("queued", DigitalWorkExecutionStatuses.Queued);
        Assert.Equal("claimed", DigitalWorkExecutionStatuses.Claimed);
        Assert.Equal("waiting-approval", DigitalWorkExecutionStatuses.WaitingApproval);
        Assert.Equal("retry-wait", DigitalWorkExecutionStatuses.RetryWait);
        Assert.Equal("verified", DigitalWorkExecutionStatuses.Verified);
    }
}

public sealed class DigitalWorkforceRuntimeSecurityTests
{
    [Fact]
    public void CrossFacilityWorkerCaseCombinationIsScopeViolation()
    {
        var worker = new DigitalWorker
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            FacilityId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        };
        var policy = new DigitalWorkerPolicy { MaxRiskLevel = 5, MaxTransactionValue = 10000m };
        var eval = DigitalWorkforcePolicyEngine.EvaluateAction(
            worker,
            policy,
            "production.plan",
            facilityId: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        Assert.False(eval.Allowed);
        Assert.Equal(DigitalWorkforceFailureCodes.ScopeViolation, eval.FailureCode);
    }
}

public sealed class DigitalWorkforceRuntimePerformanceTests
{
    [Fact]
    public void SyntheticActionClassificationThroughputMeetsBaseline()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (var i = 0; i < 50_000; i++)
        {
            DigitalWorkforceActionClassifier.Classify($"production.plan.{i}", "production.plan");
            DigitalWorkforceExecutionRuntimeService.BuildIdempotencyKey(
                Guid.NewGuid(), Guid.NewGuid(), "execute", "production.plan", $"{{\"i\":{i}}}");
        }

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 30_000, $"50K classifications took {sw.ElapsedMilliseconds}ms");
    }
}
