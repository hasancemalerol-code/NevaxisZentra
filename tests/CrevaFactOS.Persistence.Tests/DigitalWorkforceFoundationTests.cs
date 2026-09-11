using CrevaFactOS.Persistence.DigitalWorkforce;

namespace CrevaFactOS.Persistence.Tests;

public sealed class DigitalWorkforcePermissionKeyTests
{
    [Fact]
    public void PermissionKeysAreStableAndBounded()
    {
        Assert.Equal("digital-workforce.read", DigitalWorkforcePermissionKeys.Read);
        Assert.Equal("digital-workforce.manage", DigitalWorkforcePermissionKeys.Manage);
        Assert.Equal("digital-workforce.autonomy.manage", DigitalWorkforcePermissionKeys.AutonomyManage);
        Assert.Equal("digital-workforce.policy.manage", DigitalWorkforcePermissionKeys.PolicyManage);
        Assert.Equal("digital-workforce.approval.decide", DigitalWorkforcePermissionKeys.ApprovalDecide);
        Assert.Equal("digital-workforce.kill", DigitalWorkforcePermissionKeys.Kill);
        Assert.Equal("digital-workforce.cost.read", DigitalWorkforcePermissionKeys.CostRead);
    }
}

public sealed class DigitalWorkforceCanonicalWorkerTests
{
    [Fact]
    public void RegistryDefinesExactlySevenWorkers()
    {
        Assert.Equal(7, DigitalWorkforceCanonicalWorkers.Definitions.Count);
        Assert.Equal(7, DigitalWorkforceCanonicalWorkers.Definitions.Select(d => d.Code).Distinct().Count());
    }

    [Fact]
    public void WorkerCodesMatchSpecification()
    {
        var codes = DigitalWorkforceCanonicalWorkers.Definitions.Select(d => d.Code).ToArray();
        Assert.Contains(DigitalWorkforceWorkerCodes.ProductionRecovery, codes);
        Assert.Contains(DigitalWorkforceWorkerCodes.FactoryCoordinator, codes);
    }
}

public sealed class DigitalWorkCaseStateMachineTests
{
    [Fact]
    public void DetectedToClosedDeniedWithoutEmergency()
    {
        Assert.False(DigitalWorkCaseStateMachine.CanTransition(
            DigitalWorkCaseStatuses.Detected, DigitalWorkCaseStatuses.Closed));
    }

    [Fact]
    public void ExecutingToClosedRequiresVerificationPath()
    {
        Assert.True(DigitalWorkCaseStateMachine.RequiresVerificationForClosure(
            DigitalWorkCaseStatuses.Executing));
    }

    [Fact]
    public void ValidProgressionAllowed()
    {
        Assert.True(DigitalWorkCaseStateMachine.CanTransition(
            DigitalWorkCaseStatuses.Detected, DigitalWorkCaseStatuses.Analyzing));
        Assert.True(DigitalWorkCaseStateMachine.CanTransition(
            DigitalWorkCaseStatuses.Verifying, DigitalWorkCaseStatuses.Closed));
    }
}

public sealed class DigitalWorkforceAutonomyEngineTests
{
    private static DigitalWorker ObserveWorker() => new()
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        AutonomyMode = DigitalWorkforceAutonomyModes.Observe,
    };

    [Fact]
    public void ObserveModeDeniesMutation()
    {
        var result = DigitalWorkforceAutonomyEngine.Evaluate(
            ObserveWorker(), DigitalWorkforceActionKind.Execute, false);
        Assert.False(result.Allowed);
    }

    [Fact]
    public void DraftModeRequiresApprovalForExecute()
    {
        var worker = ObserveWorker();
        worker.AutonomyMode = DigitalWorkforceAutonomyModes.Draft;
        var result = DigitalWorkforceAutonomyEngine.Evaluate(
            worker, DigitalWorkforceActionKind.Execute, false);
        Assert.False(result.Allowed);
        Assert.True(result.RequiresApproval);
    }
}

public sealed class DigitalWorkforcePolicyEngineTests
{
    [Fact]
    public void MachineSafetyActionsDenied()
    {
        var worker = new DigitalWorker { Id = Guid.NewGuid(), TenantId = Guid.NewGuid() };
        var policy = new DigitalWorkerPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = worker.TenantId,
            WorkerId = worker.Id,
            AllowedActionsJson = "[\"production.read\"]",
            ProhibitedActionsJson = "[]",
            AllowedToolsJson = "[\"production.read\"]",
        };

        var result = DigitalWorkforcePolicyEngine.EvaluateAction(worker, policy, "plc.write");
        Assert.False(result.Allowed);
    }
}

public sealed class DigitalWorkforceVerificationEngineTests
{
    [Fact]
    public void StatusOnlyClosureDeniedWithoutVerification()
    {
        var workCase = new DigitalWorkCase
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ClosureContractKey = DigitalWorkforceClosureContracts.MaintenanceResolution,
            VerificationState = DigitalWorkVerificationResults.Pending,
        };

        Assert.False(DigitalWorkforceVerificationEngine.CanCloseCase(workCase, []));
    }

    [Fact]
    public void VerifiedClosureAllowedWithPassedEvidence()
    {
        var workCase = new DigitalWorkCase
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ClosureContractKey = DigitalWorkforceClosureContracts.MaintenanceResolution,
            VerificationState = DigitalWorkVerificationResults.Pending,
        };

        var verifications = new[]
        {
            new DigitalWorkVerification
            {
                Id = Guid.NewGuid(),
                TenantId = workCase.TenantId,
                CaseId = workCase.Id,
                VerificationType = "machine-operational",
                Result = DigitalWorkVerificationResults.Passed,
            },
        };

        Assert.True(DigitalWorkforceVerificationEngine.CanCloseCase(workCase, verifications));
    }
}

public sealed class DigitalWorkforceCoordinatorClosureTests
{
    [Fact]
    public void CoordinatorClosureRequiresSubcaseMetadata()
    {
        var workCase = new DigitalWorkCase
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ClosureContractKey = DigitalWorkforceClosureContracts.Coordinator,
            VerificationState = DigitalWorkVerificationResults.Passed,
            MetadataJson = """{"allSubcasesResolved":true}""",
        };

        var verifications = new[]
        {
            new DigitalWorkVerification
            {
                Id = Guid.NewGuid(),
                TenantId = workCase.TenantId,
                CaseId = workCase.Id,
                VerificationType = "coordinator-subcases-resolved",
                Result = DigitalWorkVerificationResults.Passed,
            },
        };

        Assert.True(DigitalWorkforceVerificationEngine.CanCloseCase(workCase, verifications));
    }
}

public sealed class DigitalWorkforceBudgetEngineTests
{
    [Fact]
    public void HardLimitBlocksAutonomousAi()
    {
        var budget = new DigitalWorkerBudget
        {
            DailyLimit = 10m,
            MonthlyLimit = 100m,
            DailyConsumed = 10m,
            MonthlyConsumed = 10m,
            WarningThreshold = 0.8m,
        };

        var eval = DigitalWorkforceBudgetEngine.Evaluate(budget);
        Assert.Equal(DigitalWorkforceBudgetStates.LimitReached, eval.State);
        Assert.False(eval.AllowAutonomousAi);
    }
}
