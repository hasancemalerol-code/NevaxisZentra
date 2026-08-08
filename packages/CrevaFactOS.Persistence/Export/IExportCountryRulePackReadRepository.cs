namespace CrevaFactOS.Persistence.Export;

public interface IExportCountryRulePackReadRepository
{
    Task<ExportCountryRulePackListResult> ListRulePacksAsync(
        ExportOperationContext context,
        string? countryCode,
        string? status,
        CancellationToken cancellationToken = default);

    Task<ExportCountryRulePackDetailResult> GetRulePackAsync(
        ExportOperationContext context,
        Guid rulePackId,
        CancellationToken cancellationToken = default);
}

public interface IExportCountryRulePackWriteRepository
{
    Task<ExportCountryRulePackWriteResult> CreateRulePackAsync(
        ExportOperationContext context,
        CreateCountryRulePackCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportCountryComplianceRuleWriteResult> AddRuleAsync(
        ExportOperationContext context,
        AddCountryComplianceRuleCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportCountryRulePackWriteResult> VerifyRulePackAsync(
        ExportOperationContext context,
        VerifyCountryRulePackCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportCountryRulePackWriteResult> SupersedeRulePackAsync(
        ExportOperationContext context,
        SupersedeCountryRulePackCommand command,
        CancellationToken cancellationToken = default);
}
