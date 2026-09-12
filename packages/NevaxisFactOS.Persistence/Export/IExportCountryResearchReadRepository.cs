namespace NevaxisFactOS.Persistence.Export;

public interface IExportCountryResearchReadRepository
{
    Task<ExportCountryResearchListResult> ListResearchCasesAsync(
        ExportOperationContext context,
        string? countryCode,
        string? status,
        CancellationToken cancellationToken = default);

    Task<ExportCountryResearchDetailResult> GetResearchCaseAsync(
        ExportOperationContext context,
        Guid researchCaseId,
        CancellationToken cancellationToken = default);
}

public interface IExportCountryResearchWriteRepository
{
    Task<ExportCountryResearchWriteResult> CreateResearchCaseAsync(
        ExportOperationContext context,
        CreateCountryResearchCaseCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportCountryResearchFindingWriteResult> AddFindingAsync(
        ExportOperationContext context,
        AddCountryResearchFindingCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportCountryResearchWriteResult> ReviewResearchCaseAsync(
        ExportOperationContext context,
        ReviewCountryResearchCaseCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportCountryResearchWriteResult> CompleteResearchCaseAsync(
        ExportOperationContext context,
        CompleteCountryResearchCaseCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportCountryResearchWriteResult> LinkToExportCaseAsync(
        ExportOperationContext context,
        LinkResearchToExportCaseCommand command,
        CancellationToken cancellationToken = default);
}
