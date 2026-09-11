using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.DigitalWorkforce;
using CrevaFactOS.Persistence.FactoryOrchestration;

namespace CrevaFactOS.Persistence.Tests;

public sealed class FactoryOrchestrationPermissionKeyTests
{
    [Fact]
    public void PermissionKeysAreStableAndBounded()
    {
        Assert.Equal("factory-case.read", FactoryOrchestrationPermissionKeys.Read);
        Assert.Equal("factory-case.manage", FactoryOrchestrationPermissionKeys.Manage);
        Assert.Equal("factory-case.escalate", FactoryOrchestrationPermissionKeys.Escalate);
        Assert.Equal("factory-case.verify", FactoryOrchestrationPermissionKeys.Verify);
        Assert.Equal("factory-case.reopen", FactoryOrchestrationPermissionKeys.Reopen);
    }
}

public sealed class FactoryCaseCorrelationEngineTests
{
    [Fact]
    public void BuildCorrelationKeyUsesMachineAndFacility()
    {
        var eventData = new FactoryDomainEvent(
            "maintenance.machine-fault",
            "IM-07 durdu",
            "Makine alarmı",
            "high",
            "real",
            Guid.Parse("bb000001-0005-4000-8000-000000000005"),
            "plc",
            "alarm-07",
            "IM-07",
            null,
            null,
            null,
            null);

        var key = FactoryCaseCorrelationEngine.BuildCorrelationKey(eventData);
        Assert.Contains("machine:im-07", key, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("maintenance", key, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DuplicateEventsShareCorrelationKey()
    {
        var facilityId = Guid.Parse("bb000001-0005-4000-8000-000000000005");
        var plc = new FactoryDomainEvent(
            "iot.machine-stopped", "PLC", "desc", "high", "real", facilityId,
            "plc", "a1", "IM-07", null, null, null, null);
        var production = new FactoryDomainEvent(
            "production.exception", "Production", "desc", "high", "real", facilityId,
            "production", "e1", "IM-07", null, null, null, null);

        Assert.Equal(
            FactoryCaseCorrelationEngine.BuildCorrelationKey(plc),
            FactoryCaseCorrelationEngine.BuildCorrelationKey(production));
    }
}

public sealed class FactoryDomainImpactEngineTests
{
    [Fact]
    public void MachineFaultImpactsProductionAndMaintenance()
    {
        var impacts = FactoryDomainImpactEngine.Evaluate(new FactoryDomainEvent(
            "maintenance.machine-fault",
            "IM-07 durdu",
            "desc",
            "critical",
            "real",
            Guid.NewGuid(),
            "plc",
            "alarm-1",
            "IM-07",
            "PO-1842",
            null,
            null,
            null));

        Assert.Contains(impacts, i => i.Domain == FactoryOrchestrationDomains.Production);
        Assert.Contains(impacts, i => i.Domain == FactoryOrchestrationDomains.Maintenance);
        Assert.Contains(impacts, i => i.Domain == FactoryOrchestrationDomains.Planning);
    }
}

public sealed class FactoryDeadlockDetectorTests
{
    [Fact]
    public void DetectsDependencyCycle()
    {
        var dependencies = new List<FactoryCaseDependency>
        {
            new() { FromNodeKey = "production", ToNodeKey = "maintenance", EdgeType = FactoryDependencyEdgeTypes.DependsOn },
            new() { FromNodeKey = "maintenance", ToNodeKey = "material", EdgeType = FactoryDependencyEdgeTypes.DependsOn },
            new() { FromNodeKey = "material", ToNodeKey = "production", EdgeType = FactoryDependencyEdgeTypes.DependsOn },
        };

        Assert.True(FactoryDeadlockDetector.HasCycle(dependencies));
    }

    [Fact]
    public void AcyclicGraphIsNotDeadlock()
    {
        var dependencies = new List<FactoryCaseDependency>
        {
            new() { FromNodeKey = "event", ToNodeKey = "maintenance", EdgeType = FactoryDependencyEdgeTypes.Triggers },
            new() { FromNodeKey = "maintenance", ToNodeKey = "verification", EdgeType = FactoryDependencyEdgeTypes.Verifies },
        };

        Assert.False(FactoryDeadlockDetector.HasCycle(dependencies));
    }
}

public sealed class FactoryBlockerEngineTests
{
    [Fact]
    public void OpenChildMapsToMaintenanceBlocker()
    {
        var root = new DigitalWorkCase
        {
            Id = Guid.NewGuid(),
            Title = "Fabrika vakası",
            Status = DigitalWorkCaseStatuses.Executing,
            CurrentBlockerCode = FactoryCaseBlockerCodes.None,
            Approvals = [],
        };
        var children = new[]
        {
            new DigitalWorkCase
            {
                Title = "Bakım onarımı",
                Status = DigitalWorkCaseStatuses.Executing,
                PrimaryDomain = FactoryOrchestrationDomains.Maintenance,
                PrimaryWorkerId = Guid.NewGuid(),
            },
        };

        var blocker = FactoryBlockerEngine.ResolvePrimaryBlocker(root, children);
        Assert.Equal(FactoryCaseBlockerCodes.WaitingMaintenance, blocker.Code);
    }

    [Fact]
    public void StaleBlockerDetectedAfterThreshold()
    {
        var root = new DigitalWorkCase
        {
            Title = "Tedarik bekliyor",
            Status = DigitalWorkCaseStatuses.Executing,
            CurrentBlockerCode = FactoryCaseBlockerCodes.WaitingSupplier,
            BlockerSinceUtc = DateTimeOffset.UtcNow.AddHours(-2),
            Approvals = [],
        };

        var blocker = FactoryBlockerEngine.ResolvePrimaryBlocker(root, []);
        Assert.True(blocker.IsStale);
    }
}

public sealed class FactoryConflictDetectorTests
{
    [Fact]
    public void DetectsMachineStateConflict()
    {
        var conflicts = FactoryConflictDetector.Detect(
            new FactoryDomainEvent(
                "iot.machine-stopped", "stopped", "desc", "high", "real", Guid.NewGuid(),
                "iot", "1", "IM-07", null, null, null, """{"state":"running"}"""),
            """{"state":"running"}""");

        Assert.Contains(conflicts, c => c.Code == "machine-state-mismatch");
    }
}
