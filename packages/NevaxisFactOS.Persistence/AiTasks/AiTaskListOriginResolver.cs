using System.Text.Json;
using NevaxisFactOS.Persistence.Teams;

namespace NevaxisFactOS.Persistence.AiTasks;

public static class AiTaskListOriginKinds
{
    public const string AiAnalysis = "ai-analysis";
    public const string ManualTask = "manual-task";
    public const string TeamsMessage = "teams-message";
    public const string OperationalTask = "operational-task";
    public const string Unspecified = "unspecified";
}

public static class AiTaskListOriginResolver
{
    private const string ManualCreateExplanation = "Manuel taslak görev oluşturma.";
    private const string TeamsManualDraftExplanationMarker = "Teams mesajından manuel görev taslağı";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string Resolve(AiTask task)
    {
        if (string.Equals(task.SourceModule, TeamsConstants.TeamsSourceModule, StringComparison.Ordinal))
        {
            return AiTaskListOriginKinds.TeamsMessage;
        }

        var confidence = DeserializeConfidence(task.ConfidenceJson);
        var sources = DeserializeSources(task.SourcesJson);

        if (sources.Any(source =>
                string.Equals(source.SourceType, "manual", StringComparison.OrdinalIgnoreCase)))
        {
            return AiTaskListOriginKinds.ManualTask;
        }

        var explanation = confidence.Explanation ?? string.Empty;

        if (string.Equals(explanation.Trim(), ManualCreateExplanation, StringComparison.Ordinal))
        {
            return AiTaskListOriginKinds.ManualTask;
        }

        if (explanation.Contains(TeamsManualDraftExplanationMarker, StringComparison.Ordinal))
        {
            return AiTaskListOriginKinds.ManualTask;
        }

        if (!string.IsNullOrWhiteSpace(confidence.ModelVersion))
        {
            return AiTaskListOriginKinds.AiAnalysis;
        }

        if (!string.IsNullOrWhiteSpace(task.SourceModule))
        {
            return AiTaskListOriginKinds.OperationalTask;
        }

        return AiTaskListOriginKinds.Unspecified;
    }

    private static AiTaskConfidenceModel DeserializeConfidence(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<AiTaskConfidenceModel>(json, JsonOptions)
                   ?? new AiTaskConfidenceModel(0m, string.Empty, null);
        }
        catch (JsonException)
        {
            return new AiTaskConfidenceModel(0m, string.Empty, null);
        }
    }

    private static List<AiTaskSourceReferenceModel> DeserializeSources(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<AiTaskSourceReferenceModel>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
