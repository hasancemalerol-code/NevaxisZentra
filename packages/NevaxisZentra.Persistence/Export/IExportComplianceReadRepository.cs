namespace NevaxisZentra.Persistence.Export;

public interface IExportComplianceReadRepository
{
    Task<ExportComplianceSummaryResult> GetComplianceSummaryAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default);

    Task<ExportComplianceEvaluationListResult> ListEvaluationsAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default);
}

public interface IExportComplianceWriteRepository
{
    Task<ExportComplianceWriteResult> EvaluateComplianceAsync(
        ExportOperationContext context,
        EvaluateExportComplianceCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportComplianceWriteResult> RevalidateComplianceAsync(
        ExportOperationContext context,
        RevalidateExportComplianceCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportComplianceRequirementWriteResult> SatisfyRequirementAsync(
        ExportOperationContext context,
        SatisfyComplianceRequirementCommand command,
        CancellationToken cancellationToken = default);

    Task MarkDestinationChangeRevalidationAsync(
        ExportOperationContext context,
        ExportCase exportCase,
        CancellationToken cancellationToken = default);
}
