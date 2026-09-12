using NevaxisFactOS.Persistence.ProductionPlanning;

namespace NevaxisFactOS.Api.ProductionPlanning;

public sealed record ProductionPlanServiceResult<T>(
    bool Succeeded,
    T? Value,
    string? FailureCode);

public interface IProductionPlanReadService
{
    Task<ProductionPlanServiceResult<ProductionPlanListResponse>> ListAsync(
        ProductionPlanReadOperationContext operationContext,
        ProductionPlanListRequest request,
        CancellationToken cancellationToken = default);

    Task<ProductionPlanServiceResult<ProductionPlanDetailResponse>> GetDetailAsync(
        ProductionPlanReadOperationContext operationContext,
        ProductionPlanDetailRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ProductionPlanReadService(IProductionPlanReadRepository repository)
    : IProductionPlanReadService
{
    public async Task<ProductionPlanServiceResult<ProductionPlanListResponse>> ListAsync(
        ProductionPlanReadOperationContext operationContext,
        ProductionPlanListRequest request,
        CancellationToken cancellationToken = default)
    {
        ProductionPlanStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            try
            {
                status = ProductionPlanValueConversions.StatusFromStorage(request.Status.Trim());
            }
            catch (InvalidOperationException)
            {
                return new ProductionPlanServiceResult<ProductionPlanListResponse>(
                    false,
                    null,
                    ProductionPlanApiFailureCodes.InvalidRequest);
            }
        }

        var limit = ProductionPlanReadRepository.NormalizeLimit(request.Limit);
        var repositoryResult = await repository.ListAsync(
            new ProductionPlanListReadInput(
                operationContext.TenantId,
                operationContext.FacilityId,
                status,
                request.Search,
                limit),
            cancellationToken);

        if (!repositoryResult.Succeeded)
        {
            return new ProductionPlanServiceResult<ProductionPlanListResponse>(
                false,
                null,
                MapRepositoryFailure(repositoryResult.Failure!.Code));
        }

        var items = repositoryResult.Items
            .Select(MapSummary)
            .ToArray();

        return new ProductionPlanServiceResult<ProductionPlanListResponse>(
            true,
            new ProductionPlanListResponse(operationContext.FacilityId, items.Length, items),
            null);
    }

    public async Task<ProductionPlanServiceResult<ProductionPlanDetailResponse>> GetDetailAsync(
        ProductionPlanReadOperationContext operationContext,
        ProductionPlanDetailRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ProductionPlanId == Guid.Empty)
        {
            return new ProductionPlanServiceResult<ProductionPlanDetailResponse>(
                false,
                null,
                ProductionPlanApiFailureCodes.InvalidRequest);
        }

        var repositoryResult = await repository.FindByIdAsync(
            new ProductionPlanDetailReadInput(
                operationContext.TenantId,
                operationContext.FacilityId,
                request.ProductionPlanId),
            cancellationToken);

        if (!repositoryResult.Succeeded)
        {
            return new ProductionPlanServiceResult<ProductionPlanDetailResponse>(
                false,
                null,
                MapRepositoryFailure(repositoryResult.Failure!.Code));
        }

        var mapped = ProductionPlanJsonMapper.MapDetail(repositoryResult.Plan!);
        if (!mapped.Succeeded)
        {
            return new ProductionPlanServiceResult<ProductionPlanDetailResponse>(
                false,
                null,
                ProductionPlanApiFailureCodes.InvalidRow);
        }

        return new ProductionPlanServiceResult<ProductionPlanDetailResponse>(
            true,
            new ProductionPlanDetailResponse(MapDetail(mapped.Value!)),
            null);
    }

    private static string MapRepositoryFailure(string repositoryFailureCode) =>
        repositoryFailureCode switch
        {
            ProductionPlanReadFailureCodes.InvalidScope => ProductionPlanApiFailureCodes.InvalidRequest,
            ProductionPlanReadFailureCodes.InvalidRequest => ProductionPlanApiFailureCodes.InvalidRequest,
            ProductionPlanReadFailureCodes.NotFound => ProductionPlanApiFailureCodes.NotFound,
            ProductionPlanReadFailureCodes.SourceUnavailable => ProductionPlanApiFailureCodes.SourceUnavailable,
            ProductionPlanReadFailureCodes.InvalidRow => ProductionPlanApiFailureCodes.InvalidRow,
            ProductionPlanReadFailureCodes.DataIntegrity => ProductionPlanApiFailureCodes.DataIntegrity,
            _ => ProductionPlanApiFailureCodes.UnexpectedError,
        };

    private static ProductionPlanSummaryDto MapSummary(ProductionPlanSummaryReadModel model) =>
        new(
            model.Id,
            model.ReferenceCode,
            model.Title,
            model.FacilityId,
            model.Status,
            model.Origin,
            model.AiGenerated,
            model.Confidence,
            model.PlannedStartAtUtc,
            model.PlannedEndAtUtc,
            model.ApprovalStatus,
            model.TransferStatus,
            model.TransferTargetSystem,
            model.UpdatedAtUtc,
            model.DemandCount,
            model.PrimaryRequiredQuantity,
            model.PrimaryUnit,
            model.PrimaryProductCode,
            model.PrimaryProductName,
            model.HighestDemandPriority,
            model.RiskCount,
            model.CriticalRiskCount,
            model.BlockingRiskCount,
            model.HasBlockingRisk,
            model.HasMachineAssignment,
            model.HasMoldAssignment,
            model.HasMaterialAssignment,
            model.HasOperatorGroupAssignment,
            model.ShiftName,
            model.EarliestRequestedDeliveryDateUtc);

    private static ProductionPlanDetailDto MapDetail(ProductionPlanDetailMappedReadModel model) =>
        new(
            model.Summary.Id,
            model.Summary.ReferenceCode,
            model.Summary.Title,
            model.Summary.FacilityId,
            model.Summary.Status,
            model.Summary.Origin,
            model.Summary.AiGenerated,
            model.Summary.Confidence,
            model.Summary.PlannedStartAtUtc,
            model.Summary.PlannedEndAtUtc,
            model.Summary.ApprovalStatus,
            model.Summary.TransferStatus,
            model.Summary.TransferTargetSystem,
            model.CreatedAtUtc,
            model.Summary.UpdatedAtUtc,
            model.Version,
            model.Notes,
            model.CreatedBy,
            model.AiExplanation,
            model.Demands.Select(demand => new ProductionPlanDemandDto(
                demand.Id,
                demand.ProductId,
                demand.ProductName,
                demand.RequiredQuantity,
                demand.Unit,
                demand.Priority,
                demand.OrderReference,
                demand.CustomerId,
                demand.CustomerName,
                demand.ProductCode,
                demand.RequestedDeliveryDateUtc)).ToArray(),
            model.Risks.Select(risk => new ProductionPlanRiskDto(
                risk.Id,
                risk.Type,
                risk.Severity,
                risk.Title,
                risk.Description,
                risk.Blocking,
                risk.Source)).ToArray(),
            new ProductionPlanResourceAssignmentDto(
                model.Resources.MachineId,
                model.Resources.MachineName,
                model.Resources.MoldId,
                model.Resources.MoldName,
                model.Resources.MaterialId,
                model.Resources.MaterialName,
                model.Resources.OperatorGroupId,
                model.Resources.OperatorGroupName),
            new ProductionPlanTimeWindowDto(
                model.TimeWindow.PlannedStartAtUtc,
                model.TimeWindow.PlannedEndAtUtc,
                model.TimeWindow.EstimatedDurationMinutes,
                model.TimeWindow.ShiftId,
                model.TimeWindow.ShiftName),
            new ProductionPlanApprovalDto(
                model.Approval.Status,
                model.Approval.RequiredRole,
                model.Approval.RequestedAtUtc,
                model.Approval.RequestedBy,
                model.Approval.ApprovedAtUtc,
                model.Approval.ApprovedBy,
                model.Approval.RejectedAtUtc,
                model.Approval.RejectedBy,
                model.Approval.RejectionReason),
            new ProductionPlanTransferDto(
                model.Transfer.TargetSystem,
                model.Transfer.Status,
                model.Transfer.ExternalPlanId,
                model.Transfer.StartedAtUtc,
                model.Transfer.CompletedAtUtc,
                model.Transfer.ErrorCode,
                model.Transfer.ErrorMessage),
            model.SourceReferences.Select(source => new ProductionPlanSourceReferenceDto(
                source.SourceType,
                source.SourceId,
                source.SourceUpdatedAtUtc)).ToArray(),
            model.History.Select(entry => new ProductionPlanHistoryEntryDto(
                entry.Id,
                entry.Type,
                entry.Title,
                entry.Description,
                entry.OccurredAtUtc)).ToArray());
}
