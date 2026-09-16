using System.Text.Json;
using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.FactoryDecisionCenter;
using NevaxisFactOS.Persistence.FactoryOutcomeExecution;
using NevaxisFactOS.Persistence.FactoryRecoveryIntelligence;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class FactoryRecoveryPatternEngineTests
{
    [Fact]
    public void PatternKeyUsesStructuredDimensions()
    {
        var key = FactoryRecoveryPatternEngine.BuildPatternKey(
            "order-delivery-risk",
            "production.performance-degradation",
            "INJ-004",
            "PP-GRANULE");

        Assert.Contains("outcome:order-delivery-risk", key, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("machine:inj-004", key, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("material:pp-granule", key, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class FactoryRecoverySimilarityEngineTests
{
    [Fact]
    public void SimilarityIncludesExplainableReasons()
    {
        var current = new FactoryRecoveryPattern(
            "order-delivery-risk",
            "production.performance-degradation",
            "INJ-004",
            "ORD-8742",
            "PP-GRANULE",
            null,
            "critical",
            "outcome:order-delivery-risk|event:production.performance-degradation|machine:inj-004",
            "Kaynak kurtarma",
            [],
            []);

        var record = new FactoryOutcomeLearningRecord
        {
            OutcomeType = "order-delivery-risk",
            EventCategory = "production.performance-degradation",
            MachineCode = "INJ-004",
            OrderReference = "ORD-8742",
            MaterialCode = "PP-GRANULE",
            CausePattern = "Cycle degradation",
            SelectedStrategyKey = "Kaynak kurtarma + kapasite koruma",
            LearningDisposition = FactoryRecoveryLearningDisposition.VerifiedSuccess,
            DeliveryProtected = true,
            TimeToRecoveryMinutes = 41,
            VerifiedAtUtc = DateTimeOffset.UtcNow.AddDays(-2),
            PatternJson = FactoryRecoveryPatternEngine.Serialize(current),
        };

        var match = FactoryRecoverySimilarityEngine.Score(current, record);

        Assert.True(match.SimilarityPercent >= 70);
        Assert.NotEmpty(match.SimilarityReasons);
    }
}

public sealed class FactoryRecoveryStrategyIntelligenceEngineTests
{
    [Fact]
    public void LowSampleSizeUsesLowConfidence()
    {
        var records = new[]
        {
            CreateSuccessRecord("Kaynak kurtarma", 40),
            CreateSuccessRecord("Kaynak kurtarma", 42),
        };

        var performance = FactoryRecoveryStrategyIntelligenceEngine.BuildPerformance(records, []);
        Assert.Single(performance);
        Assert.Equal(FactoryRecoverySampleConfidence.Low, performance[0].SampleConfidence);
    }

    [Fact]
    public void FailureRecordsAreSeparatedFromSuccessPerformance()
    {
        var successes = new[] { CreateSuccessRecord("Kaynak kurtarma", 40) };
        var failures = new[]
        {
            new FactoryOutcomeLearningRecord
            {
                SelectedStrategyKey = "Kaynak kurtarma",
                LearningDisposition = FactoryRecoveryLearningDisposition.VerificationFailed,
                FailureSummary = "Doğrulama başarısız",
            },
        };

        var performance = FactoryRecoveryStrategyIntelligenceEngine.BuildPerformance(successes, failures);
        Assert.Equal(1, performance[0].FailureModeCount);
    }

    private static FactoryOutcomeLearningRecord CreateSuccessRecord(string strategy, int minutes) =>
        new()
        {
            SelectedStrategyKey = strategy,
            LearningDisposition = FactoryRecoveryLearningDisposition.VerifiedSuccess,
            TimeToRecoveryMinutes = minutes,
            DeliveryProtected = true,
            VerificationResult = DigitalWorkVerificationResults.Passed,
        };
}

public sealed class FactoryRecoveryLearningDispositionTests
{
    [Fact]
    public void EvidenceStrengthAvoidsFakePrecision()
    {
        Assert.Equal(FactoryRecoveryEvidenceStrength.Low, FactoryRecoverySimilarityEngine.ResolveEvidenceStrength(1, 1));
        Assert.Equal(FactoryRecoveryEvidenceStrength.Medium, FactoryRecoverySimilarityEngine.ResolveEvidenceStrength(4, 3));
        Assert.Equal(FactoryRecoveryEvidenceStrength.Strong, FactoryRecoverySimilarityEngine.ResolveEvidenceStrength(12, 6));
    }

    [Fact]
    public void VerifiedSuccessDispositionIsDistinctFromFailure()
    {
        Assert.NotEqual(
            FactoryRecoveryLearningDisposition.VerifiedSuccess,
            FactoryRecoveryLearningDisposition.VerificationFailed);
    }
}

public sealed class FactoryRecoveryPlanIntelligenceIntegrationTests
{
    [Fact]
    public void RecoveryPlanJsonCanEmbedIntelligence()
    {
        var plan = FactoryOutcomeRecoveryPlanEngine.Build(
            new DigitalWorkCase { Id = Guid.NewGuid(), Title = "Test" },
            FactoryOutcomeImpactEngine.Evaluate(
                DevelopmentFactoryEventSimulator.CreateInj004CycleDegradationEvent(Guid.NewGuid())),
            []);

        var intelligence = new FactoryRecoveryIntelligenceSnapshot(
            2,
            [],
            [],
            ["Bakım müdahalesi gecikti"],
            38,
            FactoryRecoveryEvidenceStrength.Medium,
            [
                new FactoryRecoveryPlanHistoricalEvidence(
                    FactoryOutcomeExecutionPlanKeys.Primary,
                    12,
                    10,
                    38,
                    FactoryRecoverySampleConfidence.Medium,
                    ["Kapasite transferi"],
                    []),
            ]);

        var json = FactoryOutcomeRecoveryPlanEngine.ToJson(plan, intelligence);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("recoveryIntelligence", out _));
    }
}
