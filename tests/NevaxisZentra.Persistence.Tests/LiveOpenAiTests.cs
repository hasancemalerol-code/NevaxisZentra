using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NevaxisZentra.Persistence.FactoryCopilot;
using NevaxisZentra.Persistence.IntelligenceRuntime;
using NevaxisZentra.Persistence.ModelGateway;

namespace NevaxisZentra.Persistence.Tests;

/// <summary>Skipped unless <c>LIVE_OPENAI_TESTS=1</c> and a server-side key are present. Normal CI never calls OpenAI.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class LiveOpenAiFactAttribute : FactAttribute
{
    public LiveOpenAiFactAttribute()
    {
        var enabled = Environment.GetEnvironmentVariable("LIVE_OPENAI_TESTS") == "1";
        var hasKey = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NEVAXIS_ZENTRA__OPENAI__API_KEY"))
            || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NEVAXIS_FACTOS__OPENAI__API_KEY"));
        if (!enabled || !hasKey)
        {
            Skip = "Live OpenAI test: set LIVE_OPENAI_TESTS=1 and the server-side OpenAI key to run.";
        }
    }
}

[Collection(OpenAiEnvironmentSerial.Name)]
public sealed class LiveOpenAiTests
{
    [LiveOpenAiFact]
    public async Task TheRealModelAnswersFromTheProvidedFactsAndDeclaresMissingData()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        using var provider = services.BuildServiceProvider();
        var client = new OpenAiFrontierModelClient(
            provider.GetRequiredService<IHttpClientFactory>(), Options.Create(new OpenAiModelOptions()));
        Assert.True(client.IsConfigured);

        var facts = new[]
        {
            new ModelGroundedFact("F1", "Scope: single line LINE-A (Line A)"),
            new ModelGroundedFact("F2", "Machine M-1: produced 1200 units, 4 stops totalling 95 min; top stop reasons: Setup 60 min x2"),
            new ModelGroundedFact("F3", "Machine M-2: produced 800 units, 9 stops totalling 310 min; top stop reasons: Breakdown 250 min x3"),
            new ModelGroundedFact("F4", "Machine with the most stop minutes: M-2 (310 min)"),
        };

        var answer = await client.InvokeStructuredAsync(Request("Hangi makinenin duruşu en fazla?", facts), CancellationToken.None);
        Assert.True(answer.Succeeded, answer.ResponseClassification);
        Assert.NotNull(answer.ActualModelName);
        Assert.True(answer.InputTokens > 0 && answer.OutputTokens > 0);
        Assert.Contains("M-2", answer.Proposal!.Hypothesis, StringComparison.Ordinal);
        Assert.Null(FactoryCopilotGrounding.FindUngroundedClaim(answer.Proposal.Hypothesis, facts, "Hangi makinenin duruşu en fazla?"));

        var missing = await client.InvokeStructuredAsync(Request("Dün gece vardiyasında hangi operatör hatalı üretim yaptı?", facts), CancellationToken.None);
        Assert.True(missing.Succeeded, missing.ResponseClassification);
        // The model either flags the data as insufficient or answers only from the cited facts; it never invents an operator.
        var flagged = missing.Proposal!.EvidenceRefs.Contains(OpenAiFrontierModelClient.InsufficientDataMarker);
        Assert.True(
            flagged || FactoryCopilotGrounding.FindUngroundedClaim(missing.Proposal.Hypothesis, facts, "q") is null,
            "an answer about unavailable data must not contain claims outside the facts");
    }

    [LiveOpenAiFact]
    public async Task AnInvalidKeyFailsSafelyAndNeverEchoesTheKey()
    {
        const string BadKey = "sk-invalid-live-test-0000000000000000";
        var previous = Environment.GetEnvironmentVariable("NEVAXIS_ZENTRA__OPENAI__API_KEY");
        Environment.SetEnvironmentVariable("NEVAXIS_ZENTRA__OPENAI__API_KEY", BadKey);
        try
        {
            var services = new ServiceCollection();
            services.AddHttpClient();
            using var provider = services.BuildServiceProvider();
            var client = new OpenAiFrontierModelClient(
                provider.GetRequiredService<IHttpClientFactory>(), Options.Create(new OpenAiModelOptions()));

            var result = await client.InvokeStructuredAsync(Request("q", [new ModelGroundedFact("F1", "fact")]), CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.Equal(ModelGatewayFailureStates.AuthenticationFailed, result.FailureState);
            Assert.DoesNotContain(BadKey, System.Text.Json.JsonSerializer.Serialize(result), StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEVAXIS_ZENTRA__OPENAI__API_KEY", previous);
        }
    }

    private static ModelProviderInvokeRequest Request(string question, IReadOnlyList<ModelGroundedFact> facts) =>
        new(
            new ModelGatewayRequest(
                Guid.NewGuid(), Guid.NewGuid(), null, null,
                ModelGatewayRequestClassifications.GroundedFactorySynthesis,
                FactoryCopilotIntents.GeneralQuestion,
                new ModelGatewayContextReference(null, "live", null, null, null, "SUFFICIENT"),
                1, true,
                Grounded: new ModelGroundedInput(question, facts)),
            new ModelGatewayRoutingDecision(ModelGatewayIntelligencePaths.FrontierModel, "openai", "gpt-4.1-mini", "1.0", "live", false));
}
