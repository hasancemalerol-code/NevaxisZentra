using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NevaxisZentra.Configuration;
using NevaxisZentra.Persistence.ModelGateway;

namespace NevaxisZentra.Persistence.IntelligenceRuntime;

public sealed class OpenAiModelOptions
{
    public const string SectionName = "OpenAi";

    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string ModelName { get; set; } = "gpt-4.1-mini";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
    public int TimeoutSeconds { get; set; } = 30;
}

public sealed class OpenAiFrontierModelClient(
    IHttpClientFactory httpClientFactory,
    IOptions<OpenAiModelOptions> optionsAccessor)
{
    private readonly OpenAiModelOptions options = ResolveOptions(optionsAccessor.Value);

    public bool IsConfigured => options.Enabled && !string.IsNullOrWhiteSpace(options.ApiKey);

    public Task<OpenAiLiveValidationResult> ValidateLiveAsync(CancellationToken cancellationToken) =>
        IsConfigured
            ? InvokeStructuredAsync(
                new ModelProviderInvokeRequest(
                    new ModelGatewayRequest(
                        Guid.Empty,
                        Guid.Empty,
                        null,
                        null,
                        ModelGatewayRequestClassifications.OperationalExplanation,
                        IntelligenceTaskTypes.Summarization,
                        new ModelGatewayContextReference(null, "validation", null, null, null, "TRUSTED"),
                        1,
                        true),
                    new ModelGatewayRoutingDecision(
                        ModelGatewayIntelligencePaths.FrontierModel,
                        "openai",
                        options.ModelName,
                        "1.0",
                        "live-validation",
                        false)),
                cancellationToken)
                .ContinueWith(
                    task =>
                    {
                        var result = task.Result;
                        return new OpenAiLiveValidationResult(
                            result.Succeeded ? "PASS" : "CONFIGURATION PENDING",
                            true,
                            result.Succeeded,
                            options.ModelName,
                            result.InputTokens,
                            result.OutputTokens,
                            result.EstimatedCost,
                            result.Succeeded
                                ? "Live OpenAI structured validation succeeded."
                                : result.FailureState ?? "Live OpenAI validation failed.");
                    },
                    cancellationToken,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default)
            : Task.FromResult(new OpenAiLiveValidationResult(
                "CONFIGURATION PENDING",
                false,
                false,
                null,
                null,
                null,
                null,
                "OpenAI credentials not configured."));

    public Task<ModelProviderInvokeResult> InvokeStructuredAsync(
        ModelProviderInvokeRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return Task.FromResult(new ModelProviderInvokeResult(
                false,
                ModelGatewayFailureStates.ProviderUnavailable,
                null,
                0,
                0,
                0m,
                "openai-unavailable"));
        }

        return InvokeCoreAsync(request, cancellationToken);
    }

    private async Task<ModelProviderInvokeResult> InvokeCoreAsync(
        ModelProviderInvokeRequest request,
        CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient(nameof(OpenAiFrontierModelClient));
        client.BaseAddress = new Uri(options.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

        var payload = new
        {
            model = options.ModelName,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content =
                        "Return strict JSON with keys hypothesis, evidenceRefs, proposedActions, expectedOutcome, assumptions, confidence, risks, requiredApprovals.",
                },
                new
                {
                    role = "user",
                    content =
                        $"Task={request.GatewayRequest.TaskType}; Classification={request.GatewayRequest.RequestClassification}; Context={request.GatewayRequest.Context.CanonicalStateSummary ?? "unknown"}; Constraint={request.GatewayRequest.Context.ConstraintSummary ?? "unknown"}",
                },
            },
        };

        try
        {
            using var response = await client.PostAsJsonAsync("chat/completions", payload, cancellationToken);
            if ((int)response.StatusCode == 429)
            {
                return new ModelProviderInvokeResult(
                    false,
                    ModelGatewayFailureStates.RateLimit,
                    null,
                    0,
                    0,
                    0m,
                    "openai-rate-limit");
            }

            if (!response.IsSuccessStatusCode)
            {
                return new ModelProviderInvokeResult(
                    false,
                    ModelGatewayFailureStates.ProviderUnavailable,
                    null,
                    0,
                    0,
                    0m,
                    $"openai-http-{(int)response.StatusCode}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var content = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            var usage = document.RootElement.TryGetProperty("usage", out var usageElement)
                ? usageElement
                : default;
            var inputTokens = usage.ValueKind == JsonValueKind.Object && usage.TryGetProperty("prompt_tokens", out var promptTokens)
                ? promptTokens.GetInt32()
                : 0;
            var outputTokens = usage.ValueKind == JsonValueKind.Object && usage.TryGetProperty("completion_tokens", out var completionTokens)
                ? completionTokens.GetInt32()
                : 0;

            var proposal = ParseProposal(content, request);
            var estimatedCost = ProviderPriceCatalogEngine.EstimateCost(
                new ProviderPriceSnapshot("openai", options.ModelName, 0.0004m, 0.0016m, "USD", "2026-09-21", DateTimeOffset.UtcNow),
                inputTokens,
                outputTokens);

            return new ModelProviderInvokeResult(
                proposal is not null,
                proposal is null ? ModelGatewayFailureStates.InvalidOutput : null,
                proposal,
                inputTokens,
                outputTokens,
                estimatedCost,
                "openai-structured-proposal");
        }
        catch (TaskCanceledException)
        {
            return new ModelProviderInvokeResult(
                false,
                ModelGatewayFailureStates.Timeout,
                null,
                0,
                0,
                0m,
                "openai-timeout");
        }
    }

    private static OperationalProposal? ParseProposal(string? content, ModelProviderInvokeRequest request)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            var evidence = root.TryGetProperty("evidenceRefs", out var evidenceElement)
                ? evidenceElement.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => x.Length > 0).ToList()
                : ["openai-response"];
            var actions = root.TryGetProperty("proposedActions", out var actionsElement) && actionsElement.ValueKind == JsonValueKind.Array
                ? actionsElement.EnumerateArray().Select(action => new OperationalProposalAction(
                    action.TryGetProperty("actionType", out var typeElement) ? typeElement.GetString() ?? "READ_ONLY_ANALYSIS" : "READ_ONLY_ANALYSIS",
                    action.TryGetProperty("targetEntityType", out var entityTypeElement) ? entityTypeElement.GetString() ?? "line" : "line",
                    action.TryGetProperty("targetEntityId", out var entityIdElement) ? entityIdElement.GetString() ?? "throughput" : "throughput",
                    action.TryGetProperty("summary", out var summaryElement) ? summaryElement.GetString() ?? "OpenAI recommendation" : "OpenAI recommendation",
                    ["openai"])).ToList()
                :
                [
                    new OperationalProposalAction(
                        "READ_ONLY_ANALYSIS",
                        "line",
                        "throughput",
                        $"OpenAI analysis for {request.GatewayRequest.TaskType}",
                        ["openai"]),
                ];

            return new OperationalProposal(
                root.TryGetProperty("hypothesis", out var hypothesisElement) ? hypothesisElement.GetString() ?? string.Empty : string.Empty,
                evidence,
                actions,
                root.TryGetProperty("expectedOutcome", out var expectedElement) ? expectedElement.GetString() ?? string.Empty : string.Empty,
                root.TryGetProperty("assumptions", out var assumptionsElement)
                    ? assumptionsElement.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => x.Length > 0).ToList()
                    : [],
                root.TryGetProperty("confidence", out var confidenceElement) && confidenceElement.TryGetDecimal(out var confidence)
                    ? confidence
                    : 0.7m,
                root.TryGetProperty("risks", out var risksElement)
                    ? risksElement.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => x.Length > 0).ToList()
                    : [],
                root.TryGetProperty("requiredApprovals", out var approvalsElement)
                    ? approvalsElement.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => x.Length > 0).ToList()
                    : []);
        }
        catch
        {
            return null;
        }
    }

    private static OpenAiModelOptions ResolveOptions(OpenAiModelOptions configured)
    {
        var apiKey = LegacyEnvironmentVariables.GetSectioned("OPENAI", "API_KEY")
            ?? Environment.GetEnvironmentVariable("NEVAXIS_ZENTRA__OPENAI__API_KEY")
            ?? configured.ApiKey;

        var enabledValue = LegacyEnvironmentVariables.GetSectioned("OPENAI", "ENABLED");
        var enabled = string.IsNullOrWhiteSpace(enabledValue)
            ? configured.Enabled || !string.IsNullOrWhiteSpace(apiKey)
            : bool.Parse(enabledValue);

        return new OpenAiModelOptions
        {
            Enabled = enabled,
            ApiKey = apiKey ?? string.Empty,
            ModelName = LegacyEnvironmentVariables.GetSectioned("OPENAI", "MODEL_NAME") ?? configured.ModelName,
            BaseUrl = LegacyEnvironmentVariables.GetSectioned("OPENAI", "BASE_URL") ?? configured.BaseUrl,
            TimeoutSeconds = configured.TimeoutSeconds,
        };
    }
}

public sealed class MockNevaxisCandidateModelProvider : IModelProvider
{
    public string ProviderId => "nevaxis-candidate-model";
    public string IntelligencePath => ModelGatewayIntelligencePaths.LocalModel;
    public bool IsAvailable => true;

    public Task<ModelProviderInvokeResult> InvokeAsync(
        ModelProviderInvokeRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new ModelProviderInvokeResult(
            true,
            null,
            new OperationalProposal(
                $"Mock Nevaxis candidate response for {request.GatewayRequest.TaskType}",
                ["mock-nevaxis-model"],
                [
                    new OperationalProposalAction(
                        "READ_ONLY_ANALYSIS",
                        "line",
                        "throughput",
                        "Mock candidate recommendation",
                        ["mock-nevaxis-model"]),
                ],
                "Mock candidate outcome",
                ["mock-shadow-mode"],
                0.82m,
                [],
                []),
            40,
            20,
            0.001m,
            "mock-nevaxis-proposal"));
}
