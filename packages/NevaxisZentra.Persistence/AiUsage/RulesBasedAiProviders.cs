using NevaxisZentra.Persistence.AiTasks;
using NevaxisZentra.Persistence.Export;
using NevaxisZentra.Persistence.Maintenance;

namespace NevaxisZentra.Persistence.AiUsage;

/// <summary>
/// The Export, Maintenance and AI Task assistants are rules over structured data, not a language model. They run in a
/// customer deployment only when the operator turns them on explicitly (<c>RulesBasedAi:Enabled</c>); otherwise the
/// feature reports itself unavailable instead of answering.
/// </summary>
public static class RulesBasedAiSettings
{
    public const string ConfigurationKey = "RulesBasedAi:Enabled";

    public static bool IsEnabled(bool isDevelopment, bool? configured) => isDevelopment || configured == true;
}

public sealed class UnavailableExportAiProvider : IExportAiProvider
{
    public string ProviderName => "unavailable";
    public string ModelName => "none";
    public bool IsDevelopmentProvider => false;

    public Task<ExportAiProviderOutput> AnalyzeExportCaseAsync(ExportAiAnalysisRunType runType, string promptVersion, ExportAiContextSnapshotModel context, CancellationToken cancellationToken = default) => Unavailable();
    public Task<ExportAiProviderOutput> ExplainComplianceAsync(string promptVersion, ExportAiContextSnapshotModel context, CancellationToken cancellationToken = default) => Unavailable();
    public Task<ExportAiProviderOutput> AnalyzeMissingDataAsync(string promptVersion, ExportAiContextSnapshotModel context, CancellationToken cancellationToken = default) => Unavailable();
    public Task<ExportAiProviderOutput> SummarizeCountryResearchAsync(string promptVersion, ExportAiContextSnapshotModel context, CancellationToken cancellationToken = default) => Unavailable();
    public Task<ExportAiProviderOutput> SuggestNextActionsAsync(string promptVersion, ExportAiContextSnapshotModel context, CancellationToken cancellationToken = default) => Unavailable();

    // The caller records a failed run (ProviderUnavailable); no analysis content is produced.
    private static Task<ExportAiProviderOutput> Unavailable() =>
        throw new InvalidOperationException("The export assistant is not enabled for this deployment.");
}

public sealed class UnavailableMaintenanceAiProvider : IMaintenanceAiProvider
{
    public string ProviderName => "unavailable";
    public string ModelName => "none";
    public bool IsDevelopmentProvider => false;

    public Task<MaintenanceAiProviderResult> GenerateDraftAsync(MaintenanceAiProviderRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new MaintenanceAiProviderResult(
            false,
            MaintenanceAiFailureCodes.ProviderFailed,
            "The maintenance assistant is not enabled for this deployment.",
            null,
            "{}",
            ProviderName,
            ModelName));
}

public sealed class UnavailableAiTaskDecisionProvider : IAiTaskDecisionProvider
{
    public string ProviderName => "unavailable";
    public string ModelName => "none";
    public bool IsDevelopmentProvider => false;
    public bool IsAvailable => false;

    public Task<AiTaskDecisionProviderResult> AnalyzeAsync(AiTaskDecisionProviderRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new AiTaskDecisionProviderResult(
            false,
            AiTaskDecisionIntelligenceFailureCodes.ProviderUnavailable,
            "The task assistant is not enabled for this deployment.",
            null,
            null,
            "{}",
            ProviderName,
            ModelName));
}
