using CrevaFactOS.Persistence.ProductionPlanning;

namespace CrevaFactOS.Api.ProductionPlanning;

public sealed record ProductionPlanWriteOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public sealed record CreateProductionPlanDraftRequest(
    Guid FacilityId,
    string ReferenceCode,
    string Title,
    string? Notes,
    string Origin,
    bool AiGenerated,
    string? Confidence,
    DateTimeOffset PlannedStartAtUtc,
    DateTimeOffset PlannedEndAtUtc,
    IReadOnlyList<ProductionPlanDemandWriteModel> Demands,
    IReadOnlyList<ProductionPlanRiskWriteModel> Risks,
    ProductionPlanResourceAssignmentWriteModel Resources,
    ProductionPlanTimeWindowWriteModel? TimeWindow,
    ProductionPlanApprovalWriteModel? Approval,
    ProductionPlanTransferWriteModel? Transfer,
    IReadOnlyList<ProductionPlanSourceReferenceWriteModel> SourceReferences,
    string? AiExplanation);

public sealed record UpdateProductionPlanDraftRequest(
    Guid FacilityId,
    uint ExpectedVersion,
    string ReferenceCode,
    string Title,
    string? Notes,
    string Origin,
    bool AiGenerated,
    string? Confidence,
    DateTimeOffset PlannedStartAtUtc,
    DateTimeOffset PlannedEndAtUtc,
    IReadOnlyList<ProductionPlanDemandWriteModel> Demands,
    IReadOnlyList<ProductionPlanRiskWriteModel> Risks,
    ProductionPlanResourceAssignmentWriteModel Resources,
    ProductionPlanTimeWindowWriteModel? TimeWindow,
    ProductionPlanApprovalWriteModel? Approval,
    ProductionPlanTransferWriteModel? Transfer,
    IReadOnlyList<ProductionPlanSourceReferenceWriteModel> SourceReferences,
    string? AiExplanation);

public sealed record ProductionPlanWriteApiResult(
    bool Succeeded,
    Guid? ProductionPlanId,
    string? ReferenceCode,
    string? Status,
    uint? Version,
    DateTimeOffset? CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    string? FailureCode,
    string? FailureReason,
    IReadOnlyList<ProductionPlanWriteFieldError>? FieldErrors);

public interface IProductionPlanDraftWriteService
{
    Task<ProductionPlanWriteApiResult> CreateDraftAsync(
        ProductionPlanWriteOperationContext operationContext,
        CreateProductionPlanDraftRequest request,
        CancellationToken cancellationToken = default);

    Task<ProductionPlanWriteApiResult> UpdateDraftAsync(
        ProductionPlanWriteOperationContext operationContext,
        Guid productionPlanId,
        UpdateProductionPlanDraftRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ProductionPlanDraftWriteService(IProductionPlanWriteRepository writeRepository)
    : IProductionPlanDraftWriteService
{
    public async Task<ProductionPlanWriteApiResult> CreateDraftAsync(
        ProductionPlanWriteOperationContext operationContext,
        CreateProductionPlanDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operationContext);
        ArgumentNullException.ThrowIfNull(request);

        if (operationContext.FacilityId != request.FacilityId)
        {
            return Fail(
                ProductionPlanWriteFailureCodes.InvalidScope,
                "Facility identifier mismatch between access context and request.");
        }

        var writeContext = new ProductionPlanWriteContext(
            operationContext.TenantId,
            operationContext.FacilityId,
            operationContext.MembershipId,
            operationContext.IdentitySubjectId);

        var command = new CreateProductionPlanDraftCommand(
            request.FacilityId,
            request.ReferenceCode ?? string.Empty,
            request.Title ?? string.Empty,
            request.Notes,
            request.Origin ?? string.Empty,
            request.AiGenerated,
            request.Confidence,
            request.PlannedStartAtUtc,
            request.PlannedEndAtUtc,
            request.Demands ?? [],
            request.Risks ?? [],
            request.Resources,
            request.TimeWindow,
            request.Approval,
            request.Transfer,
            request.SourceReferences ?? [],
            request.AiExplanation);

        var result = await writeRepository.CreateDraftAsync(writeContext, command, cancellationToken);

        return Map(result);
    }

    public async Task<ProductionPlanWriteApiResult> UpdateDraftAsync(
        ProductionPlanWriteOperationContext operationContext,
        Guid productionPlanId,
        UpdateProductionPlanDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operationContext);
        ArgumentNullException.ThrowIfNull(request);

        if (productionPlanId == Guid.Empty)
        {
            return Fail(
                ProductionPlanWriteFailureCodes.InvalidScope,
                "Production plan identifier is required.");
        }

        if (operationContext.FacilityId != request.FacilityId)
        {
            return Fail(
                ProductionPlanWriteFailureCodes.InvalidScope,
                "Facility identifier mismatch between access context and request.");
        }

        var writeContext = new ProductionPlanWriteContext(
            operationContext.TenantId,
            operationContext.FacilityId,
            operationContext.MembershipId,
            operationContext.IdentitySubjectId);

        var command = new UpdateProductionPlanDraftCommand(
            request.FacilityId,
            productionPlanId,
            request.ExpectedVersion,
            request.ReferenceCode ?? string.Empty,
            request.Title ?? string.Empty,
            request.Notes,
            request.Origin ?? string.Empty,
            request.AiGenerated,
            request.Confidence,
            request.PlannedStartAtUtc,
            request.PlannedEndAtUtc,
            request.Demands ?? [],
            request.Risks ?? [],
            request.Resources,
            request.TimeWindow,
            request.Approval,
            request.Transfer,
            request.SourceReferences ?? [],
            request.AiExplanation);

        var result = await writeRepository.UpdateDraftAsync(writeContext, command, cancellationToken);

        return Map(result);
    }

    private static ProductionPlanWriteApiResult Map(ProductionPlanWriteResult result) =>
        new(
            result.Succeeded,
            result.ProductionPlanId,
            result.ReferenceCode,
            result.Status,
            result.Version,
            result.CreatedAtUtc,
            result.UpdatedAtUtc,
            result.FailureCode,
            result.FailureReason,
            result.FieldErrors);

    private static ProductionPlanWriteApiResult Fail(string failureCode, string reason) =>
        new(
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            failureCode,
            reason,
            null);
}
