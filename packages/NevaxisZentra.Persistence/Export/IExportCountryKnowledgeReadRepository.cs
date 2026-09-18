namespace NevaxisZentra.Persistence.Export;

public interface IExportCountryKnowledgeReadRepository
{
    Task<ExportCountryKnowledgeListResult> ListKnowledgeEntriesAsync(
        ExportOperationContext context,
        string? countryCode,
        string? verificationStatus,
        CancellationToken cancellationToken = default);

    Task<ExportCountryKnowledgeListResult> MatchKnowledgeAsync(
        ExportOperationContext context,
        string countryCode,
        string? hsCode,
        string? productScope,
        CancellationToken cancellationToken = default);
}

public interface IExportCountryKnowledgeWriteRepository
{
    Task<ExportCountryKnowledgeWriteResult> CreateKnowledgeEntryAsync(
        ExportOperationContext context,
        CreateCountryKnowledgeEntryCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportCountryKnowledgeWriteResult> VerifyKnowledgeEntryAsync(
        ExportOperationContext context,
        VerifyCountryKnowledgeEntryCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportCountryKnowledgeWriteResult> SupersedeKnowledgeEntryAsync(
        ExportOperationContext context,
        SupersedeCountryKnowledgeEntryCommand command,
        CancellationToken cancellationToken = default);
}

public interface IExportTraceabilityReadRepository
{
    Task<ExportTraceabilityResult> GetTraceabilityAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default);
}
