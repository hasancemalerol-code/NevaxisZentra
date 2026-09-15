using System.Text.Json;
using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.FactoryDecisionCenter;
using NevaxisFactOS.Persistence.FactoryOrchestration;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class FactoryDecisionCenterConstantsTests
{
    [Fact]
    public void CriticalOrderRecoveryScenarioValuesAreStable()
    {
        Assert.Equal("ORD-2026-0915-047", FactoryDecisionCenterConstants.OrderReference);
        Assert.Equal("Atlas Automotive", FactoryDecisionCenterConstants.CustomerName);
        Assert.Equal(480, FactoryDecisionCenterConstants.RequiredQuantity);
        Assert.Equal(412, FactoryDecisionCenterConstants.InitialProjectedQuantity);
        Assert.Equal(68, FactoryDecisionCenterConstants.InitialGapQuantity);
        Assert.Equal("INJ-001", FactoryDecisionCenterConstants.MachineCode);
        Assert.Equal(28.0, FactoryDecisionCenterConstants.CycleTimeReferenceSec);
        Assert.Equal(31.2, FactoryDecisionCenterConstants.CycleTimeCurrentSec);
        Assert.Equal(8, FactoryDecisionCenterConstants.MicroStopCount);
        Assert.Equal(120, FactoryDecisionCenterConstants.MicroStopDurationSec);
    }
}

public sealed class FactoryDecisionCenterProjectionEngineTests
{
    [Fact]
    public void ProjectsQueueItemFromDecisionCaseMetadata()
    {
        var metadata = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            [FactoryDecisionCenterMetadataKeys.OrderReference] = FactoryDecisionCenterConstants.OrderReference,
            [FactoryDecisionCenterMetadataKeys.CustomerName] = FactoryDecisionCenterConstants.CustomerName,
            [FactoryDecisionCenterMetadataKeys.RequiredQuantity] = 480,
            [FactoryDecisionCenterMetadataKeys.ProjectedQuantity] = 412,
            [FactoryDecisionCenterMetadataKeys.GapQuantity] = 68,
            [FactoryDecisionCenterMetadataKeys.DeliveryCommitmentUtc] = "2026-09-16T05:00:00Z",
            [FactoryDecisionCenterMetadataKeys.RecommendedStrategy] = "INJ-001 recovery",
        });

        var rootCase = new DigitalWorkCase
        {
            Id = DevelopmentPilotFactoryDecisionCenterIds.RootCaseId,
            Title = "Kritik müşteri siparişi teslimat riski",
            Severity = "critical",
            Status = DigitalWorkCaseStatuses.AwaitingApproval,
            Source = FactoryDecisionCenterConstants.SourceModule,
            MetadataJson = metadata,
            RequiresApproval = true,
            VerificationState = DigitalWorkVerificationResults.Pending,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            ConcurrencyToken = 1,
            PrimaryWorker = new DigitalWorker { Name = "Fabrika Operasyon Koordinatörü" },
        };

        var item = FactoryDecisionCenterProjectionEngine.ProjectQueueItem(rootCase, []);
        Assert.Equal(FactoryDecisionCenterConstants.OrderReference, item.OrderReference);
        Assert.Equal(FactoryDecisionCenterConstants.CustomerName, item.CustomerName);
        Assert.Equal(480, item.RequiredQuantity);
        Assert.Equal(412, item.ProjectedQuantity);
        Assert.Equal(68, item.GapQuantity);
        Assert.NotNull(item.DecisionRequired);
    }

    [Fact]
    public void BuildSummaryDerivesCountersFromLoadedCases()
    {
        var cases = new List<DigitalWorkCase>
        {
            new()
            {
                Source = FactoryDecisionCenterConstants.SourceModule,
                Severity = "critical",
                Status = DigitalWorkCaseStatuses.AwaitingApproval,
                RequiresApproval = true,
                VerificationState = DigitalWorkVerificationResults.Pending,
            },
        };

        var summary = FactoryDecisionCenterProjectionEngine.BuildSummary(cases);
        Assert.Equal(1, summary.CriticalDecisions);
        Assert.Equal(1, summary.OutcomesAtRisk);
        Assert.Equal(1, summary.PendingHumanApproval);
    }

    [Fact]
    public void IsDecisionCenterCaseMatchesSourceAndCorrelation()
    {
        var bySource = new DigitalWorkCase { Source = FactoryDecisionCenterConstants.SourceModule };
        var byCorrelation = new DigitalWorkCase { CorrelationKey = FactoryDecisionCenterConstants.CorrelationKey };
        var unrelated = new DigitalWorkCase { Source = "other" };

        Assert.True(FactoryDecisionCenterProjectionEngine.IsDecisionCenterCase(bySource));
        Assert.True(FactoryDecisionCenterProjectionEngine.IsDecisionCenterCase(byCorrelation));
        Assert.False(FactoryDecisionCenterProjectionEngine.IsDecisionCenterCase(unrelated));
    }
}

public sealed class FactoryDecisionCenterRecoveryPlanTests
{
    [Fact]
    public void RecoveryPlanProjectionReadsOrderedSteps()
    {
        var json = JsonSerializer.Serialize(new
        {
            objective = "Protect delivery",
            steps = new[]
            {
                new { order = 1, title = "Recover cycle time", workerCode = DigitalWorkforceWorkerCodes.ProductionRecovery, workerName = "Üretim Kurtarma", status = "planned" },
                new { order = 2, title = "Reserve material", workerCode = DigitalWorkforceWorkerCodes.MaterialShortage, workerName = "Malzeme Açığı", status = "planned" },
            },
        });

        var plan = FactoryRecoveryPlanProjection.FromJson(json);
        Assert.Equal(2, plan.Steps.Count);
        Assert.Equal("Recover cycle time", plan.Steps[0].Title);
    }
}

public sealed class FactoryDecisionCenterVerificationGateTests
{
    [Fact]
    public void VerifiedOutcomeRequiresPassedVerificationState()
    {
        var metadata = FactoryDecisionCenterProjectionEngine.ParseMetadata(
            JsonSerializer.Serialize(new Dictionary<string, object>
            {
                [FactoryDecisionCenterMetadataKeys.RequiredQuantity] = 480,
                [FactoryDecisionCenterMetadataKeys.VerifiedProjection] = 493,
                [FactoryDecisionCenterMetadataKeys.DeliveryProtected] = true,
                [FactoryDecisionCenterMetadataKeys.RecoveryGain] = 13,
            }));

        var openCase = new DigitalWorkCase
        {
            Status = DigitalWorkCaseStatuses.Verifying,
            VerificationState = DigitalWorkVerificationResults.Pending,
            MetadataJson = JsonSerializer.Serialize(metadata),
        };
        var closedCase = new DigitalWorkCase
        {
            Status = DigitalWorkCaseStatuses.Closed,
            VerificationState = DigitalWorkVerificationResults.Passed,
            VerifiedAtUtc = DateTimeOffset.UtcNow,
            MetadataJson = JsonSerializer.Serialize(metadata),
        };

        var openOutcome = FactoryDecisionCenterProjectionEngine.ProjectDetail(
            openCase, [], [], null, "Pilot").VerifiedOutcome;
        var closedOutcome = FactoryDecisionCenterProjectionEngine.ProjectDetail(
            closedCase, [], [], null, "Pilot").VerifiedOutcome;

        Assert.False(openOutcome.IsVerified);
        Assert.True(closedOutcome.IsVerified);
        Assert.Equal(493, closedOutcome.VerifiedProjection);
        Assert.True(closedOutcome.DeliveryProtected);
    }
}

public sealed class FactoryDecisionCenterWorkerOrchestrationTests
{
    [Fact]
    public void LinkedAiTaskIdsAreStableForSynchronization()
    {
        Assert.Equal(4, DevelopmentPilotFactoryDecisionCenterIds.LinkedAiTaskIds.Length);
        Assert.Contains(
            DevelopmentPilotFactoryDecisionCenterIds.AiTaskApproveRecoveryPlanId,
            DevelopmentPilotFactoryDecisionCenterIds.LinkedAiTaskIds);
        Assert.Contains(
            DevelopmentPilotFactoryDecisionCenterIds.AiTaskDeliveryProjectionId,
            DevelopmentPilotFactoryDecisionCenterIds.LinkedAiTaskIds);
    }

    [Fact]
    public void CorrelationKeyIsTenantSafeAndDeterministic()
    {
        Assert.Equal("order:ord-2026-0915-047:facility", FactoryDecisionCenterConstants.CorrelationKey);
        Assert.Contains("ord-2026-0915-047", FactoryDecisionCenterConstants.CorrelationKey, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ScenarioUsesCoordinatorAndThreeSpecialistWorkers()
    {
        Assert.NotEqual(Guid.Empty, DevelopmentPilotFactoryDecisionCenterIds.RootCaseId);
        Assert.NotEqual(Guid.Empty, DevelopmentPilotFactoryDecisionCenterIds.ProductionChildCaseId);
        Assert.NotEqual(Guid.Empty, DevelopmentPilotFactoryDecisionCenterIds.MaintenanceChildCaseId);
        Assert.NotEqual(Guid.Empty, DevelopmentPilotFactoryDecisionCenterIds.MaterialChildCaseId);
    }
}
