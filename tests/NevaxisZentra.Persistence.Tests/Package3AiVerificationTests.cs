using NevaxisZentra.Persistence.AiTasks;
using NevaxisZentra.Persistence.AiUsage;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.DigitalWorkforce;
using NevaxisZentra.Persistence.Export;
using NevaxisZentra.Persistence.FactoryCopilot;
using NevaxisZentra.Persistence.Maintenance;
using NevaxisZentra.Persistence.ModelGateway;

namespace NevaxisZentra.Persistence.Tests;

/// <summary>
/// RC3 Package 3: a provider failure or a degraded route is never a fabricated success, accepting a recommendation needs the
/// permission of the mutation it performs, the Production default of every rules-based assistant is "unavailable", and the
/// Copilot router no longer confuses words that merely contain a keyword.
/// </summary>
public sealed class ModelGatewayDegradationGuardTests
{
    private static ModelGatewayRequest Request(string classification, string? preferred = null) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            classification,
            "task",
            new ModelGatewayContextReference(null, "state", null, null, null, "TRUSTED"),
            1,
            true,
            preferred);

    private static ModelGatewayRoutingDecision Deterministic(string reason) =>
        new(
            ModelGatewayIntelligencePaths.DeterministicEngine,
            ModelGatewayProviderIds.DeterministicEngine,
            "deterministic-engine-v1",
            "1.0",
            reason,
            false);

    [Theory]
    [InlineData("cost-budget-limit-reached", ModelGatewayFailureStates.PolicyRejected)]
    [InlineData("cost-budget-fallback-to-deterministic", ModelGatewayFailureStates.PolicyRejected)]
    [InlineData("insufficient-trusted-context", ModelGatewayFailureStates.InsufficientContext)]
    [InlineData("provider-unavailable", ModelGatewayFailureStates.ProviderUnavailable)]
    [InlineData("high-risk-action-requires-deterministic-path", ModelGatewayFailureStates.ProviderUnavailable)]
    public void AModelDependentRequestThatDegradedToTheCannedEngineFailsClosed(string reason, string expectedState)
    {
        var failed = ModelGatewayDegradationGuard.TryFailClosed(
            Request(ModelGatewayRequestClassifications.GroundedFactorySynthesis),
            Deterministic(reason),
            out var result);

        Assert.True(failed);
        Assert.False(result.Succeeded);
        Assert.Equal(expectedState, result.FailureState);
        Assert.Null(result.Proposal);
        Assert.Equal(0, result.InputTokens + result.OutputTokens);
        Assert.Equal(0m, result.EstimatedCost);
    }

    [Theory]
    [InlineData(ModelGatewayRequestClassifications.OperationalExplanation)]
    [InlineData(ModelGatewayRequestClassifications.CrossModuleDiagnosis)]
    [InlineData(ModelGatewayRequestClassifications.HypothesisGeneration)]
    [InlineData(ModelGatewayRequestClassifications.RecoveryPlanReasoning)]
    public void EveryModelDependentClassificationIsGuarded(string classification)
    {
        Assert.True(ModelGatewayDegradationGuard.TryFailClosed(
            Request(classification), Deterministic("provider-unavailable"), out _));
    }

    [Theory]
    [InlineData(ModelGatewayRequestClassifications.ThroughputCalculation)]
    [InlineData(ModelGatewayRequestClassifications.PolicyEvaluation)]
    [InlineData(ModelGatewayRequestClassifications.SafetyGuard)]
    public void RequestsThatAreDeterministicByDesignStillUseTheEngine(string classification)
    {
        Assert.False(ModelGatewayDegradationGuard.TryFailClosed(
            Request(classification), Deterministic($"deterministic-capability:{classification}"), out _));
    }

    [Fact]
    public void AnExplicitDeterministicRequestIsNotTreatedAsADegradedRoute()
    {
        Assert.False(ModelGatewayDegradationGuard.TryFailClosed(
            Request(ModelGatewayRequestClassifications.OperationalExplanation, ModelGatewayIntelligencePaths.DeterministicEngine),
            Deterministic("explicit-deterministic-request"),
            out _));
    }

    [Fact]
    public void ARealModelRouteIsNeverBlockedByTheGuard()
    {
        var frontier = new ModelGatewayRoutingDecision(
            ModelGatewayIntelligencePaths.FrontierModel,
            ModelGatewayProviderIds.FrontierModel,
            "frontier-reasoning-v1",
            "1.0",
            "complex-reasoning",
            false);

        Assert.False(ModelGatewayDegradationGuard.TryFailClosed(
            Request(ModelGatewayRequestClassifications.GroundedFactorySynthesis), frontier, out _));
    }

    [Fact]
    public async Task ProductionLocalProviderIsUnavailableAndNeverAnswers()
    {
        var provider = new LocalModelProvider();

        Assert.False(provider.IsAvailable);
        var result = await provider.InvokeAsync(
            new ModelProviderInvokeRequest(
                Request(ModelGatewayRequestClassifications.GroundedFactorySynthesis),
                Deterministic("provider-unavailable")),
            CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Null(result.Proposal);
    }
}

public sealed class AiTaskRecommendationAcceptanceGuardTests
{
    [Theory]
    [InlineData(AiTaskDecisionRecommendationActionTypes.AssignTeam, AiTaskPermissionKeys.Assign)]
    [InlineData(AiTaskDecisionRecommendationActionTypes.SetPriority, AiTaskPermissionKeys.Manage)]
    [InlineData(AiTaskDecisionRecommendationActionTypes.Escalate, AiTaskPermissionKeys.Escalate)]
    [InlineData(AiTaskDecisionRecommendationActionTypes.StartPlaybook, AiTaskPermissionKeys.PlaybookRun)]
    public void EveryMutatingActionRequiresThePermissionOfItsMutation(string actionType, string permission)
    {
        Assert.Equal(permission, AiTaskRecommendationAcceptanceGuard.RequiredPermission(actionType));
    }

    [Theory]
    [InlineData(AiTaskDecisionRecommendationActionTypes.RequestEvidence)]
    [InlineData(AiTaskDecisionRecommendationActionTypes.ReviewProductionOrder)]
    [InlineData("anything-unknown")]
    public void AdvisoryActionsNeedOnlyFeedbackPermission(string actionType)
    {
        Assert.Null(AiTaskRecommendationAcceptanceGuard.RequiredPermission(actionType));
    }

    private const string StoredTeam = """{"teamChannelId":"11111111-1111-4111-8111-111111111111"}""";

    [Fact]
    public void AnEditedPayloadCannotRetargetTheRecommendedTeam()
    {
        Assert.False(AiTaskRecommendationAcceptanceGuard.IsEditedPayloadWithinRecommendation(
            StoredTeam, """{"teamChannelId":"22222222-2222-4222-8222-222222222222"}"""));
    }

    [Fact]
    public void AnEditedPayloadCannotIntroduceATargetTheRecommendationDidNotHave()
    {
        Assert.False(AiTaskRecommendationAcceptanceGuard.IsEditedPayloadWithinRecommendation(
            """{"priority":"high"}""", """{"playbookId":"33333333-3333-4333-8333-333333333333"}"""));
    }

    [Fact]
    public void AnEditedPayloadMayRepeatTheStoredTargetAndAdjustAValue()
    {
        Assert.True(AiTaskRecommendationAcceptanceGuard.IsEditedPayloadWithinRecommendation(
            """{"teamChannelId":"11111111-1111-4111-8111-111111111111","priority":"high"}""",
            """{"teamChannelId":"11111111-1111-4111-8111-111111111111","priority":"low"}"""));
        Assert.True(AiTaskRecommendationAcceptanceGuard.IsEditedPayloadWithinRecommendation(StoredTeam, null));
        Assert.True(AiTaskRecommendationAcceptanceGuard.IsEditedPayloadWithinRecommendation(StoredTeam, "  "));
    }

    [Fact]
    public void AMalformedEditedPayloadIsRefused()
    {
        Assert.False(AiTaskRecommendationAcceptanceGuard.IsEditedPayloadWithinRecommendation(StoredTeam, "{not json"));
        Assert.False(AiTaskRecommendationAcceptanceGuard.IsEditedPayloadWithinRecommendation(StoredTeam, "[1,2]"));
    }
}

public sealed class RulesBasedAiSettingsTests
{
    [Theory]
    [InlineData(false, null, false)]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, null, true)]
    [InlineData(true, false, true)]
    public void RulesBasedAssistantsRunInProductionOnlyWhenExplicitlyEnabled(bool development, bool? configured, bool expected)
    {
        Assert.Equal(expected, RulesBasedAiSettings.IsEnabled(development, configured));
    }

    [Fact]
    public async Task TheUnavailableAssistantsNeverProduceContent()
    {
        var maintenance = await new UnavailableMaintenanceAiProvider().GenerateDraftAsync(
            new MaintenanceAiProviderRequest("v1", null!, "c"));
        Assert.False(maintenance.Succeeded);
        Assert.Null(maintenance.Output);

        var aiTask = new UnavailableAiTaskDecisionProvider();
        Assert.False(aiTask.IsAvailable);
        Assert.False(aiTask.IsDevelopmentProvider);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new UnavailableExportAiProvider().ExplainComplianceAsync("v1", null!));
    }

    [Theory]
    [InlineData(false, null, false)]
    [InlineData(false, true, true)]
    [InlineData(true, null, true)]
    public void DigitalWorkforceExecutionFailsClosedUnlessExplicitlyEnabled(bool development, bool? configured, bool expected)
    {
        Assert.Equal(expected, DigitalWorkforceExecutionSettings.IsEnabled(development, configured));
    }

    [Fact]
    public async Task DisabledDigitalWorkforceExecutionQueuesAndExecutesNothing()
    {
        var service = new DigitalWorkforceExecutionDisabledService(null!);

        var enqueue = await service.EnqueueAsync(
            new EnqueueDigitalWorkExecutionRequest(Guid.NewGuid(), Guid.NewGuid(), "teams.send", "teams.send", "{}", null, 1),
            "subject",
            CancellationToken.None);
        Assert.False(enqueue.Accepted);
        Assert.Null(enqueue.ExecutionId);
        Assert.Equal(DigitalWorkforceExecutionSettings.DisabledErrorCode, enqueue.ErrorCode);

        var processed = await service.ProcessBatchAsync("owner", 25, CancellationToken.None);
        Assert.Equal(new DigitalWorkExecutionProcessingResult(0, 0, 0, 0), processed);
        var reclaimed = await service.ReclaimExpiredLeasesAsync("owner", 25, CancellationToken.None);
        Assert.Equal(new DigitalWorkExecutionProcessingResult(0, 0, 0, 0), reclaimed);
    }
}

public sealed class FactoryCopilotRoutingCollisionTests
{
    private static readonly FactoryCopilotContextInput Context = new(FactoryCopilotEntryPoints.Fabrika);

    private static string Intent(string question) =>
        FactoryCopilotIntentClassifier.Classify(question, Context).Intent;

    [Fact]
    public void NcrInsideAnotherWordIsNotAQualityQuestion()
    {
        Assert.NotEqual(FactoryCopilotIntents.QualityRisk, Intent("Did the cycle time increase on the line?"));
        Assert.Equal(FactoryCopilotIntents.QualityRisk, Intent("NCR-1042 açık mı?"));
    }

    [Fact]
    public void HoldInsideThresholdIsNotAQualityQuestion()
    {
        Assert.NotEqual(FactoryCopilotIntents.QualityRisk, Intent("Is the stop threshold exceeded on the line?"));
        Assert.Equal(FactoryCopilotIntents.QualityRisk, Intent("Which lots are on hold?"));
    }

    [Fact]
    public void CauseInsideBecauseIsNotACauseAnalysis()
    {
        Assert.NotEqual(FactoryCopilotIntents.CauseAnalysis, Intent("The line stopped because the machine overheated"));
        Assert.Equal(FactoryCopilotIntents.CauseAnalysis, Intent("What is the cause of the shortfall?"));
    }

    [Theory]
    [InlineData("İŞ NEDEN KAPANMADI?", FactoryCopilotIntents.WorkStatus)]
    [InlineData("HAT ŞU AN NE DURUMDA?", FactoryCopilotIntents.CurrentState)]
    [InlineData("MAKİNE ÇALIŞIYOR MU?", FactoryCopilotIntents.CurrentState)]
    public void CapitalisedTurkishMatchesTheSamePatternsAsLowerCase(string question, string expected)
    {
        Assert.Equal(expected, Intent(question));
    }

    [Theory]
    [InlineData("Siparişler gecikir mi?")]
    [InlineData("Order 90017 teslim edilebilir mi?")]
    [InlineData("order #42 risk")]
    public void OrderQuestionsAreRoutedWithoutAnyHardcodedOrderNumber(string question)
    {
        Assert.Equal(FactoryCopilotIntents.OrderRisk, Intent(question));
    }

    [Fact]
    public void TurkishSuffixesStillMatchAtTheStartOfTheWord()
    {
        Assert.Equal(FactoryCopilotIntents.OrderRisk, Intent("Siparişlerin durumu"));
        Assert.True(FactoryCopilotIntentClassifier.MatchesAny(FactoryCopilotIntentClassifier.Fold("Darboğazlar nerede"), "darboğaz"));
        Assert.False(FactoryCopilotIntentClassifier.MatchesAny("because", "cause"));
        Assert.False(FactoryCopilotIntentClassifier.MatchesAny("increase", "ncr"));
    }
}

public sealed class FactoryCopilotAuditOutcomeTests
{
    [Theory]
    [InlineData(null, AuditOutcome.Success)]
    [InlineData("", AuditOutcome.Success)]
    [InlineData(FactoryCopilotAiStates.InsufficientData, AuditOutcome.Success)]
    [InlineData(FactoryCopilotAiStates.ProviderUnavailable, AuditOutcome.Failure)]
    [InlineData(FactoryCopilotAiStates.NotConfigured, AuditOutcome.Failure)]
    [InlineData(FactoryCopilotAiStates.AuthenticationFailed, AuditOutcome.Failure)]
    [InlineData(FactoryCopilotAiStates.RateLimited, AuditOutcome.Failure)]
    [InlineData(FactoryCopilotAiStates.Timeout, AuditOutcome.Failure)]
    [InlineData(FactoryCopilotAiStates.InvalidOutput, AuditOutcome.Failure)]
    [InlineData(FactoryCopilotAiStates.UngroundedResponse, AuditOutcome.Failure)]
    [InlineData(FactoryCopilotAiStates.InjectionBlocked, AuditOutcome.Failure)]
    [InlineData(FactoryCopilotAiStates.BudgetLimited, AuditOutcome.Failure)]
    public void ABlockedOrUnansweredQueryIsNeverAuditedAsSuccess(string? failureState, AuditOutcome expected)
    {
        Assert.Equal(expected, FactoryCopilotService.ResolveAuditOutcome(failureState));
    }
}

/// <summary>
/// The factory event spine worker finds its tenants through its own discovery (a migration-defined function), not through
/// AI-task deliveries, and looks for work inside the tenant session (row level security hides rows outside one).
/// </summary>
public sealed class FactoryEventSpineWorkerDiscoveryTests
{
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "NevaxisZentra.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }

    [Fact]
    public void TheDiscoveryMigrationCoversEveryKindOfSpineWork()
    {
        var sql = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "packages", "NevaxisZentra.Persistence.Migrations", "Migrations",
            "20261012120000_AddFactoryEventSpineWorkTenantDiscovery.cs"));

        Assert.Contains("discover_factory_event_spine_work_tenants", sql, StringComparison.Ordinal);
        Assert.Contains("SECURITY DEFINER", sql, StringComparison.Ordinal);
        Assert.Contains("processing_status = 'received'", sql, StringComparison.Ordinal);
        Assert.Contains("processing_state = 'dispatch-pending'", sql, StringComparison.Ordinal);
        Assert.Contains("processing_state = 'processing'", sql, StringComparison.Ordinal);
        Assert.Contains("REVOKE ALL ON FUNCTION discover_factory_event_spine_work_tenants() FROM PUBLIC", sql, StringComparison.Ordinal);
        Assert.Contains("GRANT EXECUTE ON FUNCTION discover_factory_event_spine_work_tenants() TO crevafactos_runtime", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void TheWorkerNoLongerDependsOnAiTaskDeliveriesAndReadsInsideATenantSession()
    {
        var source = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "apps", "api", "Integration", "FactoryEventSpineEndpointRouteBuilderExtensions.cs"));
        var worker = source[source.IndexOf("class FactoryEventSpineBackgroundService", StringComparison.Ordinal)..];

        Assert.Contains("IFactoryEventSpineWorkTenantDiscoverer", worker, StringComparison.Ordinal);
        Assert.DoesNotContain("IAiTaskDeliveryWorkTenantDiscoverer", worker, StringComparison.Ordinal);
        Assert.Contains("tenantSession.ExecuteReadAsync", worker, StringComparison.Ordinal);
    }
}

/// <summary>
/// An evaluation's identity is its conclusion, not its time. A time-based identity made every page view create a new risk
/// assessment, which invalidated every Maintenance AI draft before it could be approved.
/// </summary>
public sealed class MaintenanceEvaluationIdentityTests
{
    private static readonly Guid MachineId = Guid.Parse("11111111-1111-4111-8111-111111111111");

    private static NevaxisZentra.Persistence.Maintenance.ConditionAssetEvaluationInput Input(DateTimeOffset at, bool stale) =>
        new(
            "ASSET-1",
            MachineId,
            "M-1",
            "Machine 1",
            at,
            [],
            [],
            new NevaxisZentra.Persistence.Maintenance.ConditionAlarmContextInput(0, null),
            new NevaxisZentra.Persistence.Maintenance.ConditionDowntimeContextInput(0, 0m),
            new NevaxisZentra.Persistence.Maintenance.ConditionMaintenanceHistoryInput(0, null, null),
            stale);

    [Fact]
    public void ReEvaluatingTheSameConditionKeepsTheSameIdentity()
    {
        var service = new NevaxisZentra.Persistence.Maintenance.ConditionEvaluationService();
        var t1 = new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);

        var first = service.EvaluateAsset(Input(t1, stale: true));
        var later = service.EvaluateAsset(Input(t1.AddMinutes(7), stale: true));
        var muchLater = service.EvaluateAsset(Input(t1.AddHours(3), stale: true));

        Assert.Equal(first.EvaluationIdentity, later.EvaluationIdentity);
        Assert.Equal(first.EvaluationIdentity, muchLater.EvaluationIdentity);
    }

    [Fact]
    public void ReEvaluatingAnAssetWithAnomaliesKeepsTheSameAnomalyAndConditionIdentities()
    {
        var service = new NevaxisZentra.Persistence.Maintenance.ConditionEvaluationService();
        var t1 = new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);
        NevaxisZentra.Persistence.Maintenance.ConditionAssetEvaluationInput WithAlarms(DateTimeOffset at) =>
            Input(at, stale: true) with
            {
                AlarmContext = new NevaxisZentra.Persistence.Maintenance.ConditionAlarmContextInput(4, "critical"),
            };

        var first = service.EvaluateAsset(WithAlarms(t1));
        var later = service.EvaluateAsset(WithAlarms(t1.AddMinutes(11)));

        Assert.NotEmpty(first.Anomalies);
        Assert.Equal(
            first.Anomalies.Select(a => a.EvaluationIdentity).OrderBy(x => x),
            later.Anomalies.Select(a => a.EvaluationIdentity).OrderBy(x => x));
        Assert.Equal(first.EvaluationIdentity, later.EvaluationIdentity);
    }

    [Fact]
    public void ANewAlarmPatternGetsANewAnomalyAndConditionIdentity()
    {
        var service = new NevaxisZentra.Persistence.Maintenance.ConditionEvaluationService();
        var t = new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);

        var quiet = service.EvaluateAsset(Input(t, stale: true));
        var noisy = service.EvaluateAsset(Input(t, stale: true) with
        {
            AlarmContext = new NevaxisZentra.Persistence.Maintenance.ConditionAlarmContextInput(5, "critical"),
        });

        Assert.NotEqual(quiet.EvaluationIdentity, noisy.EvaluationIdentity);
    }

    [Fact]
    public void AChangedConclusionGetsANewIdentity()
    {
        var service = new NevaxisZentra.Persistence.Maintenance.ConditionEvaluationService();
        var t = new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);

        var stale = service.EvaluateAsset(Input(t, stale: true));
        var fresh = service.EvaluateAsset(Input(t, stale: false));

        Assert.NotEqual(stale.DataFreshness, fresh.DataFreshness);
        Assert.NotEqual(stale.EvaluationIdentity, fresh.EvaluationIdentity);
    }
}

/// <summary>A draft that failed to generate has an empty output; reading it must not fail with a null reference.</summary>
public sealed class MaintenanceAiFailedDraftTests
{
    private static readonly System.Text.Json.JsonSerializerOptions WebOptions = new(System.Text.Json.JsonSerializerDefaults.Web);

    [Fact]
    public void AnEmptyStoredOutputNormalisesToCompleteEmptyValues()
    {
        var parsed = System.Text.Json.JsonSerializer.Deserialize<NevaxisZentra.Persistence.Maintenance.MaintenanceAiStructuredOutput>(
            "{}", WebOptions);

        var normalised = NevaxisZentra.Persistence.Maintenance.MaintenanceAiDraftRepository.NormalizeOutput(parsed);

        Assert.NotNull(normalised.EvidenceReferences);
        Assert.NotNull(normalised.MissingInformation);
        Assert.NotNull(normalised.SafetyNotes);
        Assert.Empty(normalised.EvidenceReferences);
        Assert.Equal(string.Empty, normalised.Summary);
        Assert.Equal(string.Empty, normalised.UncertaintyExplanation);
        Assert.False(normalised.ProhibitedActionConfirmation);
    }

    [Fact]
    public void ANullOutputNormalisesToEmptyValues()
    {
        var normalised = NevaxisZentra.Persistence.Maintenance.MaintenanceAiDraftRepository.NormalizeOutput(null);

        Assert.Empty(normalised.EvidenceReferences);
        Assert.Equal(string.Empty, normalised.Summary);
    }
}
