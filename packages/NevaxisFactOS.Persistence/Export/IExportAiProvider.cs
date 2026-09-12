namespace NevaxisFactOS.Persistence.Export;

public interface IExportAiProvider
{
    string ProviderName { get; }
    string ModelName { get; }
    bool IsDevelopmentProvider { get; }

    Task<ExportAiProviderOutput> AnalyzeExportCaseAsync(
        ExportAiAnalysisRunType runType,
        string promptVersion,
        ExportAiContextSnapshotModel context,
        CancellationToken cancellationToken = default);

    Task<ExportAiProviderOutput> ExplainComplianceAsync(
        string promptVersion,
        ExportAiContextSnapshotModel context,
        CancellationToken cancellationToken = default);

    Task<ExportAiProviderOutput> AnalyzeMissingDataAsync(
        string promptVersion,
        ExportAiContextSnapshotModel context,
        CancellationToken cancellationToken = default);

    Task<ExportAiProviderOutput> SummarizeCountryResearchAsync(
        string promptVersion,
        ExportAiContextSnapshotModel context,
        CancellationToken cancellationToken = default);

    Task<ExportAiProviderOutput> SuggestNextActionsAsync(
        string promptVersion,
        ExportAiContextSnapshotModel context,
        CancellationToken cancellationToken = default);
}
