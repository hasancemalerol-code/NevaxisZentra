using CrevaFactOS.Persistence.ProductionPlanning;

namespace CrevaFactOS.Api.ProductionPlanning;

public sealed record PrepareHandoffApiRequest(uint ExpectedVersion);

public sealed record ProductionPlanHandoffApiResponse(
    Guid HandoffId,
    Guid ProductionPlanId,
    string ReferenceCode,
    string Status,
    uint HandoffVersion,
    DateTimeOffset PreparedAtUtc);

public interface IProductionPlanHandoffService
{
    Task<ProductionPlanHandoffResult> PrepareHandoffAsync(
        ProductionPlanWriteOperationContext operationContext,
        Guid productionPlanId,
        PrepareHandoffApiRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ProductionPlanHandoffService(
    IProductionPlanHandoffRepository repository) : IProductionPlanHandoffService
{
    public Task<ProductionPlanHandoffResult> PrepareHandoffAsync(
        ProductionPlanWriteOperationContext operationContext,
        Guid productionPlanId,
        PrepareHandoffApiRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operationContext);
        ArgumentNullException.ThrowIfNull(request);

        var context = new ProductionPlanHandoffContext(
            operationContext.TenantId,
            operationContext.FacilityId,
            operationContext.MembershipId,
            operationContext.IdentitySubjectId,
            operationContext.PermissionKey,
            operationContext.CorrelationId);

        var command = new PrepareProductionPlanHandoffCommand(
            productionPlanId,
            request.ExpectedVersion);

        return repository.PrepareHandoffAsync(context, command, cancellationToken);
    }
}
