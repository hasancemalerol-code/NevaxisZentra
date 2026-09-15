using System.Text.Json;
using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.FactoryDecisionCenter;
using NevaxisFactOS.Persistence.FactoryOrchestration;
using NevaxisFactOS.Persistence.FactoryOutcomeExecution;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class FactoryOutcomeEventQualificationEngineTests
{
    [Fact]
    public void LowSeverityNoiseEventIsRejected()
    {
        var result = FactoryOutcomeEventQualificationEngine.Qualify(
            new FactoryDomainEvent(
                "production.note", "Not", "desc", "info", "real", Guid.NewGuid(),
                "note", "n1", null, null, null, null, null),
            null);

        Assert.False(result.IsQualified);
    }

    [Fact]
    public void MachineAndOrderEventIsQualified()
    {
        var result = FactoryOutcomeEventQualificationEngine.Qualify(
            DevelopmentFactoryEventSimulator.CreateInj004CycleDegradationEvent(Guid.NewGuid()),
            null);

        Assert.True(result.IsQualified);
    }
}

public sealed class FactoryOutcomeImpactEngineTests
{
    [Fact]
    public void ImpactEngineCalculatesGapWithoutFakeFinancials()
    {
        var impact = FactoryOutcomeImpactEngine.Evaluate(
            DevelopmentFactoryEventSimulator.CreateInj004CycleDegradationEvent(Guid.NewGuid()));

        Assert.Equal(FactoryOutcomeExecutionConstants.OrderReference, impact.OrderReference);
        Assert.Equal(220, impact.ProjectedGap);
        Assert.Null(impact.FinancialImpactTry);
    }
}

public sealed class FactoryOutcomeCorrelationEngineTests
{
    [Fact]
    public void MachineAndMaterialEventsForSameOrderShareCorrelationKey()
    {
        var facilityId = Guid.NewGuid();
        var machine = DevelopmentFactoryEventSimulator.CreateInj004CycleDegradationEvent(facilityId);
        var material = DevelopmentFactoryEventSimulator.CreateMaterialShortageEvent(facilityId);

        Assert.Equal(
            FactoryCaseCorrelationEngine.BuildCorrelationKey(machine),
            FactoryCaseCorrelationEngine.BuildCorrelationKey(material));
    }
}

public sealed class FactoryOutcomeRecoveryPlanEngineTests
{
    [Fact]
    public void RecoveryPlanIncludesPrimaryAndAlternativeOptions()
    {
        var root = new DigitalWorkCase
        {
            Id = Guid.NewGuid(),
            Title = "Sonuç vakası",
        };
        var impact = FactoryOutcomeImpactEngine.Evaluate(
            DevelopmentFactoryEventSimulator.CreateInj004CycleDegradationEvent(Guid.NewGuid()));
        var plan = FactoryOutcomeRecoveryPlanEngine.Build(root, impact, []);

        Assert.Equal(2, plan.Plans.Count);
        Assert.Contains(plan.Plans, p => p.PlanKey == FactoryOutcomeExecutionPlanKeys.Primary);
        Assert.Contains(plan.Plans, p => p.PlanKey == FactoryOutcomeExecutionPlanKeys.Alternative);
        Assert.True(plan.Plans.First(p => p.PlanKey == FactoryOutcomeExecutionPlanKeys.Primary).Steps.Count >= 4);
    }
}

public sealed class FactoryOutcomeVerificationEngineTests
{
    [Fact]
    public void VerificationFailureKeepsCaseOpen()
    {
        var root = new DigitalWorkCase
        {
            Status = DigitalWorkCaseStatuses.Verifying,
            VerificationState = DigitalWorkVerificationResults.Pending,
            Approvals = [],
        };
        var snapshot = FactoryOutcomeVerificationEngine.Evaluate(root, [], [], 900, 1200);

        Assert.False(snapshot.Passed);
        Assert.False(FactoryOutcomeVerificationEngine.CanCloseOutcomeCase(root, [], [], snapshot));
    }

    [Fact]
    public void VerificationPassesWhenCriteriaMet()
    {
        var root = new DigitalWorkCase
        {
            Status = DigitalWorkCaseStatuses.Verifying,
            VerificationState = DigitalWorkVerificationResults.Pending,
            Approvals = [],
        };
        var child = new DigitalWorkCase { Status = DigitalWorkCaseStatuses.Closed };
        var verification = new DigitalWorkVerification
        {
            Result = DigitalWorkVerificationResults.Passed,
            VerificationType = "outcome-verified",
        };
        var snapshot = FactoryOutcomeVerificationEngine.Evaluate(root, [child], [verification], 1250, 1200);

        Assert.True(snapshot.Passed);
        Assert.True(FactoryOutcomeVerificationEngine.CanCloseOutcomeCase(root, [child], [verification], snapshot));
    }
}

public sealed class FactoryOutcomeFailureScenarioTests
{
    [Fact]
    public void DuplicateEventQualifiesAsEnrichmentWhenCaseExists()
    {
        var existing = new DigitalWorkCase
        {
            Status = DigitalWorkCaseStatuses.Executing,
            CorrelationKey = "outcome:order:ord-8742",
        };
        var result = FactoryOutcomeEventQualificationEngine.Qualify(
            DevelopmentFactoryEventSimulator.CreateMaterialShortageEvent(Guid.NewGuid()),
            existing);

        Assert.True(result.IsQualified);
        Assert.True(result.ShouldEnrichExisting);
    }

    [Fact]
    public void UnrelatedFacilityEventsDoNotShareCorrelationKey()
    {
        var facilityA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var facilityB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var eventA = DevelopmentFactoryEventSimulator.CreateInj004CycleDegradationEvent(facilityA);
        var eventB = DevelopmentFactoryEventSimulator.CreateInj004CycleDegradationEvent(facilityB);

        Assert.NotEqual(
            FactoryCaseCorrelationEngine.BuildCorrelationKey(eventA),
            FactoryCaseCorrelationEngine.BuildCorrelationKey(eventB));
    }

    [Fact]
    public void ShouldCorrelateMaterialEventIntoExistingOrderOutcomeCase()
    {
        var existing = new DigitalWorkCase
        {
            Status = DigitalWorkCaseStatuses.Executing,
            CorrelationKey = $"{Guid.NewGuid():N}|outcome:order:ord-8742",
            MetadataJson = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["machineCode"] = FactoryOutcomeExecutionConstants.MachineCode,
            }),
        };
        var material = DevelopmentFactoryEventSimulator.CreateMaterialShortageEvent(Guid.NewGuid());

        Assert.True(FactoryCaseCorrelationEngine.ShouldCorrelate(existing, material));
    }

    [Fact]
    public void ClosedCaseDoesNotAcceptCorrelation()
    {
        var existing = new DigitalWorkCase
        {
            Status = DigitalWorkCaseStatuses.Closed,
            CorrelationKey = "outcome:order:ord-8742",
        };

        Assert.False(FactoryCaseCorrelationEngine.ShouldCorrelate(
            existing,
            DevelopmentFactoryEventSimulator.CreateMaterialShortageEvent(Guid.NewGuid())));
    }
}

public sealed class FactoryDecisionCenterLiveModeTests
{
    [Fact]
    public void DecisionCenterIncludesOutcomeExecutionCases()
    {
        var outcomeCase = new DigitalWorkCase
        {
            Source = FactoryOutcomeExecutionConstants.SourceModule,
            CorrelationKey = "outcome:order:ord-8742",
            Severity = "critical",
            Status = DigitalWorkCaseStatuses.Executing,
        };

        Assert.True(FactoryDecisionCenterProjectionEngine.IsDecisionCenterCase(outcomeCase));
    }

    [Fact]
    public void LiveQueuesDeriveFromRealCaseState()
    {
        var cases = new List<DigitalWorkCase>
        {
            new()
            {
                Source = FactoryOutcomeExecutionConstants.SourceModule,
                Severity = "critical",
                Status = DigitalWorkCaseStatuses.AwaitingApproval,
                RequiresApproval = true,
                MetadataJson = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    [FactoryDecisionCenterMetadataKeys.OrderReference] = FactoryOutcomeExecutionConstants.OrderReference,
                }),
            },
        };

        var queues = FactoryDecisionCenterProjectionEngine.BuildLiveQueues(
            cases,
            c => FactoryDecisionCenterProjectionEngine.ProjectQueueItem(c, []));
        Assert.Single(queues.CriticalDecisions);
        Assert.Single(queues.PendingHumanApproval);
    }
}
