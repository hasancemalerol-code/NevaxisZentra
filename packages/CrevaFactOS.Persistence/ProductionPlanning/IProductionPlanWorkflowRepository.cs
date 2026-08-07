namespace CrevaFactOS.Persistence.ProductionPlanning;

public interface IProductionPlanWorkflowRepository
{
    Task<ProductionPlanWorkflowResult> SubmitForReviewAsync(
        ProductionPlanWorkflowContext context,
        SubmitProductionPlanForReviewCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionPlanWorkflowResult> RequestRevisionAsync(
        ProductionPlanWorkflowContext context,
        RequestProductionPlanRevisionCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionPlanWorkflowResult> ApproveAsync(
        ProductionPlanWorkflowContext context,
        ApproveProductionPlanCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionPlanWorkflowResult> RejectAsync(
        ProductionPlanWorkflowContext context,
        RejectProductionPlanCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionPlanWorkflowResult> ScheduleAsync(
        ProductionPlanWorkflowContext context,
        ScheduleProductionPlanCommand command,
        CancellationToken cancellationToken = default);
}
