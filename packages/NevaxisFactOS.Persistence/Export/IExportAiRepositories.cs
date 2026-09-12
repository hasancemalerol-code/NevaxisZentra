namespace NevaxisFactOS.Persistence.Export;

public interface IExportAiReadRepository
{
    Task<ExportAiRunListResult> ListRunsForExportCaseAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default);

    Task<ExportAiRunListResult> ListRunsForResearchCaseAsync(
        ExportOperationContext context,
        Guid researchCaseId,
        CancellationToken cancellationToken = default);

    Task<ExportAiRunDetailResult> GetRunDetailAsync(
        ExportOperationContext context,
        Guid runId,
        CancellationToken cancellationToken = default);

    Task<ExportAiRecommendationListResult> ListRecommendationsForExportCaseAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default);
}

public interface IExportAiWriteRepository
{
    Task<ExportAiWriteResult> StartAnalysisAsync(
        ExportOperationContext context,
        StartExportAiAnalysisCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportAiRecommendationWriteResult> ReviewRecommendationAsync(
        ExportOperationContext context,
        ReviewExportAiRecommendationCommand command,
        CancellationToken cancellationToken = default);
}
