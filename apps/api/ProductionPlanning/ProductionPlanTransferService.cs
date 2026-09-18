using NevaxisZentra.Persistence.ProductionPlanning;

namespace NevaxisZentra.Api.ProductionPlanning;

public sealed record CreateTransferRequestApiRequest(
    uint ExpectedVersion,
    string TargetSystem);

public sealed record ExecuteTransferApiRequest(uint ExpectedPlanVersion);

public sealed record TransferRequestCreatedResponse(
    Guid TransferRequestId,
    Guid ProductionPlanId,
    string TargetSystem,
    string Status,
    DateTimeOffset RequestedAtUtc,
    bool IsIdempotentDuplicate);

public sealed record TransferExecuteResponse(
    Guid TransferRequestId,
    string FinalStatus,
    string? ExternalReference,
    bool? Retryable,
    string? SafeFailureMessage,
    bool ReconciliationRequired);

public interface IProductionPlanTransferService
{
    Task<(ProductionPlanTransferRequestResult Result, bool IsCreated)> CreateTransferRequestAsync(
        ProductionPlanWriteOperationContext operationContext,
        Guid productionPlanId,
        CreateTransferRequestApiRequest request,
        CancellationToken cancellationToken = default);

    Task<ProductionPlanTransferExecuteResult> ExecuteTransferAsync(
        ProductionPlanWriteOperationContext operationContext,
        Guid productionPlanId,
        Guid transferRequestId,
        ExecuteTransferApiRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ProductionPlanTransferService(
    IProductionPlanTransferRepository transferRepository,
    IEnumerable<IProductionPlanTransferAdapter> adapters)
    : IProductionPlanTransferService
{
    public async Task<(ProductionPlanTransferRequestResult Result, bool IsCreated)> CreateTransferRequestAsync(
        ProductionPlanWriteOperationContext operationContext,
        Guid productionPlanId,
        CreateTransferRequestApiRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operationContext);
        ArgumentNullException.ThrowIfNull(request);

        if (productionPlanId == Guid.Empty)
        {
            return (ProductionPlanTransferRequestResult.Fail(
                ProductionPlanTransferFailureCodes.InvalidScope,
                "Production plan identifier is required."), false);
        }

        var context = BuildContext(operationContext);
        var command = new CreateTransferRequestCommand(
            productionPlanId,
            request.ExpectedVersion,
            request.TargetSystem ?? string.Empty);

        var result = await transferRepository.CreateTransferRequestAsync(context, command, cancellationToken);
        var isCreated = result.Succeeded && !result.IsIdempotentDuplicate;
        return (result, isCreated);
    }

    public async Task<ProductionPlanTransferExecuteResult> ExecuteTransferAsync(
        ProductionPlanWriteOperationContext operationContext,
        Guid productionPlanId,
        Guid transferRequestId,
        ExecuteTransferApiRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operationContext);
        ArgumentNullException.ThrowIfNull(request);

        if (productionPlanId == Guid.Empty || transferRequestId == Guid.Empty)
        {
            return ProductionPlanTransferExecuteResult.Fail(
                ProductionPlanTransferFailureCodes.InvalidScope,
                "Production plan and transfer request identifiers are required.");
        }

        var context = BuildContext(operationContext);
        var command = new ExecuteTransferCommand(
            productionPlanId,
            transferRequestId,
            request.ExpectedPlanVersion);

        return await transferRepository.ExecuteTransferAsync(context, command, adapters, cancellationToken);
    }

    private static ProductionPlanTransferContext BuildContext(ProductionPlanWriteOperationContext operationContext) =>
        new(
            operationContext.TenantId,
            operationContext.FacilityId,
            operationContext.MembershipId,
            operationContext.IdentitySubjectId,
            operationContext.PermissionKey,
            operationContext.CorrelationId);
}

public sealed class UnavailableProductionPlanTransferService : IProductionPlanTransferService
{
    private static readonly ProductionPlanTransferRequestResult UnavailableRequest =
        ProductionPlanTransferRequestResult.Fail(
            ProductionPlanTransferFailureCodes.SourceUnavailable,
            "Transfer service is unavailable.");

    private static readonly ProductionPlanTransferExecuteResult UnavailableExecute =
        ProductionPlanTransferExecuteResult.Fail(
            ProductionPlanTransferFailureCodes.SourceUnavailable,
            "Transfer service is unavailable.");

    public Task<(ProductionPlanTransferRequestResult Result, bool IsCreated)> CreateTransferRequestAsync(
        ProductionPlanWriteOperationContext operationContext,
        Guid productionPlanId,
        CreateTransferRequestApiRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult((UnavailableRequest, false));

    public Task<ProductionPlanTransferExecuteResult> ExecuteTransferAsync(
        ProductionPlanWriteOperationContext operationContext,
        Guid productionPlanId,
        Guid transferRequestId,
        ExecuteTransferApiRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableExecute);
}
