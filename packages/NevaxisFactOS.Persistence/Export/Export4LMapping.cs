using System.Text.Json;
using NevaxisFactOS.Persistence.AiUsage;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Export;

internal static class Export4LMapping
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static ExportAiAnalysisRunReadModel MapRun(ExportAiAnalysisRun run) =>
        new(
            run.Id,
            run.ExportCaseId,
            run.CountryResearchCaseId,
            run.ContextSnapshotId,
            ExportValueConversions.ToStorage(run.RunType),
            ExportValueConversions.ToStorage(run.Status),
            run.Provider,
            run.Model,
            run.PromptVersion,
            run.StartedAtUtc,
            run.CompletedAtUtc,
            run.RequestedBySubjectId,
            run.InputUsageUnits,
            run.OutputUsageUnits,
            run.AiCreditsConsumed,
            run.FailureCode,
            run.FailureMessage,
            run.ReasoningSummary,
            run.Version);

    public static ExportAiFindingReadModel MapFinding(ExportAiFinding finding) =>
        new(
            finding.Id,
            finding.AnalysisRunId,
            ExportValueConversions.ToStorage(finding.Category),
            finding.Title,
            finding.Summary,
            ExportValueConversions.ToStorage(finding.Severity),
            finding.Confidence,
            ExportValueConversions.ToStorage(finding.SourceType),
            DeserializeSources(finding.SourceReferencesJson),
            finding.RelatedRequirementId,
            finding.RelatedDocumentId,
            finding.RelatedShipmentId,
            finding.Actionable,
            finding.HumanReviewRequired,
            finding.CreatedAtUtc);

    public static ExportAiRecommendationReadModel MapRecommendation(ExportAiRecommendation rec) =>
        new(
            rec.Id,
            rec.AnalysisRunId,
            rec.Title,
            rec.Description,
            ExportValueConversions.ToStorage(rec.Priority),
            rec.Confidence,
            rec.ReasoningSummary,
            rec.ProposedActionType.HasValue ? ExportValueConversions.ToStorage(rec.ProposedActionType.Value) : null,
            rec.ProposedActionPayloadJson,
            ExportValueConversions.ToStorage(rec.Status),
            rec.CreatedAtUtc,
            rec.ReviewedAtUtc,
            rec.ReviewedBySubjectId,
            rec.ReviewDecision.HasValue ? ExportValueConversions.ToStorage(rec.ReviewDecision.Value) : null,
            rec.ReviewNote,
            rec.Version);

    public static IReadOnlyList<ExportAiSourceReference> DeserializeSources(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<ExportAiSourceReference>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static string SerializeSources(IReadOnlyList<ExportAiSourceReference> sources) =>
        JsonSerializer.Serialize(sources, JsonOptions);
}
