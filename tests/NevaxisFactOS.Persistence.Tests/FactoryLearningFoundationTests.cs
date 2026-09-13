using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.FactoryLearning;
using NevaxisFactOS.Persistence.ProductionManager;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class ContinuousImprovementPermissionKeyTests
{
    [Fact]
    public void PermissionKeysAreStableAndBounded()
    {
        Assert.Equal("continuous-improvement.read", ContinuousImprovementPermissionKeys.Read);
        Assert.Equal("continuous-improvement.manage", ContinuousImprovementPermissionKeys.Manage);
        Assert.Equal("experiment.create", ContinuousImprovementPermissionKeys.ExperimentCreate);
        Assert.Equal("experiment.approve", ContinuousImprovementPermissionKeys.ExperimentApprove);
        Assert.Equal("experiment.stop", ContinuousImprovementPermissionKeys.ExperimentStop);
        Assert.Equal("improvement.standardize", ContinuousImprovementPermissionKeys.ImprovementStandardize);
    }
}

public sealed class LossDetectionEngineTests
{
    [Fact]
    public void DetectsDowntimeFromStoppedMachineState()
    {
        var tenantId = Guid.NewGuid();
        var facilityId = Guid.NewGuid();
        var state = new FactoryStateSnapshot { MachineStateJson = """{"stopped":1}""" };

        var losses = LossDetectionEngine.DetectFromState(state, tenantId, facilityId, []);

        Assert.Contains(losses, l => l.Category == FactoryLossCategories.Downtime);
        Assert.Contains(losses, l => l.MachineId == "IM-07");
        Assert.All(losses, l => Assert.Equal(FactoryLearningDataClassifications.Derived, l.DataClassification));
    }

    [Fact]
    public void IncrementsRecurrenceForExistingCorrelationKey()
    {
        var tenantId = Guid.NewGuid();
        var facilityId = Guid.NewGuid();
        var state = new FactoryStateSnapshot { MachineStateJson = """{"stopped":1}""" };
        var existing = new List<FactoryLoss>
        {
            new()
            {
                CorrelationKey = $"{FactoryLossCategories.Downtime}:IM-07",
                RecurrenceCount = 2,
                FirstSeenAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            },
        };

        var losses = LossDetectionEngine.DetectFromState(state, tenantId, facilityId, existing);

        Assert.Single(losses);
        Assert.Equal(3, losses[0].RecurrenceCount);
    }
}

public sealed class RecurringLossDetectionEngineTests
{
    [Fact]
    public void DetectsRecurringPatternsWhenRecurrenceCountAtLeastTwo()
    {
        var losses = new List<FactoryLoss>
        {
            SampleLoss("downtime:IM-07", 2),
            SampleLoss("downtime:IM-07", 3),
        };

        var patterns = RecurringLossDetectionEngine.DetectPatterns(losses);

        Assert.Single(patterns);
        Assert.Equal("recurring-loss", patterns[0].PatternType);
        Assert.Equal("downtime:IM-07", patterns[0].PatternKey);
    }

    [Fact]
    public void IgnoresSingleOccurrenceLosses()
    {
        var losses = new List<FactoryLoss> { SampleLoss("downtime:IM-07", 1) };

        var patterns = RecurringLossDetectionEngine.DetectPatterns(losses);

        Assert.Empty(patterns);
    }

    private static FactoryLoss SampleLoss(string correlationKey, int recurrence) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        FacilityId = Guid.NewGuid(),
        Category = FactoryLossCategories.Downtime,
        Title = "Makine duruşu",
        Summary = "Test",
        MachineId = "IM-07",
        CorrelationKey = correlationKey,
        RecurrenceCount = recurrence,
        EvidenceJson = "[]",
        FirstSeenAtUtc = DateTimeOffset.UtcNow.AddHours(-2),
        LastSeenAtUtc = DateTimeOffset.UtcNow,
    };
}

public sealed class FactoryPatternEngineTests
{
    [Fact]
    public void FindDeterministicPatternsIncludesMachineFaultRecurrence()
    {
        var tenantId = Guid.NewGuid();
        var facilityId = Guid.NewGuid();
        var losses = new List<FactoryLoss>
        {
            DowntimeLoss(tenantId, facilityId, "IM-07"),
            DowntimeLoss(tenantId, facilityId, "IM-07"),
        };

        var patterns = FactoryPatternEngine.FindDeterministicPatterns(losses);

        Assert.Contains(patterns, p => p.PatternType == "machine-fault-recurrence" && p.PatternKey == "machine-fault:IM-07");
    }

    private static FactoryLoss DowntimeLoss(Guid tenantId, Guid facilityId, string machineId) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        FacilityId = facilityId,
        Category = FactoryLossCategories.Downtime,
        Title = "Makine duruşu",
        Summary = "Test",
        MachineId = machineId,
        CorrelationKey = $"{FactoryLossCategories.Downtime}:{machineId}",
        RecurrenceCount = 2,
        EvidenceJson = "[]",
        FirstSeenAtUtc = DateTimeOffset.UtcNow.AddHours(-2),
        LastSeenAtUtc = DateTimeOffset.UtcNow,
    };
}

public sealed class LossParetoEngineTests
{
    [Fact]
    public void CalculateProducesContributionPercentages()
    {
        var losses = new List<FactoryLoss>
        {
            new() { Category = FactoryLossCategories.Downtime, DurationMinutes = 60 },
            new() { Category = FactoryLossCategories.Scrap, DurationMinutes = 40 },
        };

        var rows = LossParetoEngine.Calculate(losses);

        Assert.Equal(2, rows.Count);
        Assert.Equal(60m, rows[0].TotalMinutes);
        Assert.Equal(60m, rows[0].ContributionPercent);
        Assert.Equal(40m, rows[1].ContributionPercent);
    }
}

public sealed class ImprovementPriorityEngineTests
{
    [Fact]
    public void ScoreIncreasesWithOccurrenceCount()
    {
        var low = new FactoryLossPattern { OccurrenceCount = 1, ConfidenceScore = 0.5m, PatternType = "recurring-loss" };
        var high = new FactoryLossPattern { OccurrenceCount = 5, ConfidenceScore = 0.9m, PatternType = "recurring-loss" };

        Assert.True(ImprovementPriorityEngine.Score(high, null) > ImprovementPriorityEngine.Score(low, null));
    }

    [Fact]
    public void ScoreIsBoundedBetweenZeroAndOneHundred()
    {
        var pattern = new FactoryLossPattern
        {
            OccurrenceCount = 20,
            ConfidenceScore = 1m,
            PatternType = "recurring-loss",
        };

        var score = ImprovementPriorityEngine.Score(pattern, null);

        Assert.InRange(score, 0m, 100m);
    }
}

public sealed class ExperimentOutcomeEngineTests
{
    [Fact]
    public void ClassifyReturnsSuccessWhenEffectivenessMeetsThreshold()
    {
        var outcome = ExperimentOutcomeEngine.Classify(0.2m, true, true, true);

        Assert.Equal(FactoryExperimentOutcomes.Success, outcome);
    }

    [Fact]
    public void ClassifyReturnsInconclusiveWithoutMeasurements()
    {
        var outcome = ExperimentOutcomeEngine.Classify(0.2m, true, true, false);

        Assert.Equal(FactoryExperimentOutcomes.Inconclusive, outcome);
    }

    [Fact]
    public void ClassifyReturnsNegativeEffectWhenGuardrailsFail()
    {
        var outcome = ExperimentOutcomeEngine.Classify(0.2m, false, true, true);

        Assert.Equal(FactoryExperimentOutcomes.NegativeEffect, outcome);
    }
}

public sealed class ExperimentSafetyEngineTests
{
    [Fact]
    public void ValidateGuardrailsRejectsHighScrapRate()
    {
        var experiment = new FactoryExperiment { GuardrailsJson = """["quality-regression"]""" };
        var measurements = new List<FactoryExperimentMeasurement>
        {
            new() { MetricKey = "scrap-rate", MetricValue = 0.08m },
        };

        Assert.False(ExperimentSafetyEngine.ValidateGuardrails(experiment, measurements));
    }

    [Fact]
    public void RequiresApprovalForMaintenanceAndQualityExperiments()
    {
        Assert.True(ExperimentSafetyEngine.RequiresApproval(FactoryExperimentTypes.Maintenance));
        Assert.True(ExperimentSafetyEngine.RequiresApproval(FactoryExperimentTypes.Quality));
        Assert.False(ExperimentSafetyEngine.RequiresApproval(FactoryExperimentTypes.Planning));
    }
}

public sealed class ImprovementSustainabilityEngineTests
{
    [Fact]
    public void EvaluateReturnsRegressingWhenEffectivenessDrops()
    {
        var status = ImprovementSustainabilityEngine.Evaluate(0.8m, 0.3m);

        Assert.Equal("regressing", status);
    }

    [Fact]
    public void EvaluateReturnsSustainedWhenEffectivenessHolds()
    {
        var status = ImprovementSustainabilityEngine.Evaluate(0.8m, 0.75m);

        Assert.Equal("sustained", status);
    }
}

public sealed class RootCauseWorkbenchEngineTests
{
    [Fact]
    public void DraftSetsConfirmedCauseForConfirmedConfidence()
    {
        var request = new RootCauseDraftRequest(
            null,
            null,
            "5-why",
            ["why1", "why2"],
            "Worn bearing",
            RootCauseConfidenceLevels.Confirmed,
            "[]");

        var analysis = RootCauseWorkbenchEngine.Draft(request, Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(RootCauseConfidenceLevels.Confirmed, analysis.ConfidenceLevel);
        Assert.True(analysis.IsConfirmedCause);
    }

    [Fact]
    public void DraftLeavesCauseUnconfirmedForLikelyConfidence()
    {
        var request = new RootCauseDraftRequest(
            null,
            null,
            "5-why",
            ["why1"],
            "Operator variance",
            RootCauseConfidenceLevels.Likely,
            "[]");

        var analysis = RootCauseWorkbenchEngine.Draft(request, Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(RootCauseConfidenceLevels.Likely, analysis.ConfidenceLevel);
        Assert.False(analysis.IsConfirmedCause);
    }
}

public sealed class FactoryLearningMemoryEngineTests
{
    [Fact]
    public void CreateFromVerifiedImprovementUsesVerifiedMemoryQuality()
    {
        var improvement = new VerifiedImprovement
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            FacilityId = Guid.NewGuid(),
            OutcomeSummary = "Improved cycle time",
            EffectivenessScore = 0.2m,
            VerificationEvidenceJson = "[]",
        };
        var experiment = new FactoryExperiment { Title = "PM interval", Hypothesis = "Reduce faults", ScopeJson = "{}" };
        var opportunity = new ImprovementOpportunity
        {
            ProblemSummary = "Recurring downtime",
            AffectedScopeJson = "{}",
            ExpectedMechanism = "Preventive maintenance",
        };

        var memory = FactoryLearningMemoryEngine.CreateFromVerifiedImprovement(
            improvement, experiment, opportunity, LearningMemoryQuality.Verified);

        Assert.Equal(LearningMemoryQuality.Verified, memory.MemoryQuality);
        Assert.Equal(RootCauseConfidenceLevels.StrongEvidence, memory.RootCauseConfidence);
    }

    [Fact]
    public void BuildInsightsReturnsOnlyVerifiedMemories()
    {
        var memories = new List<FactoryLearningMemory>
        {
            new() { MemoryQuality = LearningMemoryQuality.Verified, ResultSummary = "Good", EffectivenessScore = 0.8m },
            new() { MemoryQuality = LearningMemoryQuality.Failed, ResultSummary = "Bad", EffectivenessScore = 0.1m },
        };

        var insights = FactoryLearningMemoryEngine.BuildInsights(memories);

        Assert.Single(insights);
        Assert.Equal("verified-learning", insights[0].InsightType);
    }

    [Fact]
    public void RevalidateRejectsWhenMachineContextMissing()
    {
        var memory = new FactoryLearningMemory
        {
            RevalidationCriteriaJson = """["machine:IM-07"]""",
        };
        var state = new FactoryStateSnapshot { MachineStateJson = """{"running":4}""" };

        Assert.False(FactoryLearningMemoryEngine.Revalidate(memory, state));
    }
}
