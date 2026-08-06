using CrevaFactOS.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.ProductionPlanning;

public sealed class ProductionPlanReadRepository(CrevaFactOSDbContext dbContext)
    : IProductionPlanReadRepository
{
    public const int DefaultLimit = 50;
    public const int MinimumLimit = 1;
    public const int MaximumLimit = 200;

    public async Task<ProductionPlanListReadResult> ListAsync(
        ProductionPlanListReadInput input,
        CancellationToken cancellationToken = default)
    {
        var scopeFailure = ValidateScope(input.TenantId, input.FacilityId);
        if (scopeFailure is not null)
        {
            return ProductionPlanListReadResult.Fail(scopeFailure);
        }

        if (input.Limit is < MinimumLimit or > MaximumLimit)
        {
            return ProductionPlanListReadResult.Fail(
                ProductionPlanReadFailure.InvalidRequest("Limit must be between 1 and 200."));
        }

        try
        {
            var query = ApplyListFilters(dbContext.ProductionPlans.AsNoTracking(), input);
            var items = await query
                .OrderByDescending(plan => plan.UpdatedAtUtc)
                .ThenBy(plan => plan.Id)
                .Take(input.Limit)
                .Select(plan => new ProductionPlanSummaryReadModel(
                    plan.Id,
                    plan.ReferenceCode,
                    plan.Title,
                    plan.FacilityId,
                    ProductionPlanValueConversions.ToStorage(plan.Status),
                    ProductionPlanValueConversions.ToStorage(plan.Origin),
                    plan.AiGenerated,
                    ProductionPlanValueConversions.ToStorage(plan.Confidence),
                    plan.PlannedStartAtUtc,
                    plan.PlannedEndAtUtc,
                    ProductionPlanValueConversions.ToStorage(plan.ApprovalStatus),
                    ProductionPlanValueConversions.ToStorage(plan.TransferStatus),
                    plan.TransferTargetSystem.HasValue
                        ? ProductionPlanValueConversions.ToStorage(plan.TransferTargetSystem.Value)
                        : null,
                    plan.UpdatedAtUtc))
                .ToListAsync(cancellationToken);

            return ProductionPlanListReadResult.Success(items);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return ProductionPlanListReadResult.Fail(
                ProductionPlanReadFailure.SourceUnavailable("Production plan list could not be read."));
        }
    }

    public async Task<ProductionPlanDetailReadResult> FindByIdAsync(
        ProductionPlanDetailReadInput input,
        CancellationToken cancellationToken = default)
    {
        var scopeFailure = ValidateScope(input.TenantId, input.FacilityId);
        if (scopeFailure is not null)
        {
            return ProductionPlanDetailReadResult.Fail(scopeFailure);
        }

        if (input.ProductionPlanId == Guid.Empty)
        {
            return ProductionPlanDetailReadResult.Fail(
                ProductionPlanReadFailure.InvalidRequest("A production plan identifier is required."));
        }

        try
        {
            var matches = await dbContext.ProductionPlans
                .AsNoTracking()
                .Where(plan =>
                    plan.TenantId == input.TenantId
                    && plan.FacilityId == input.FacilityId
                    && plan.Id == input.ProductionPlanId)
                .Select(plan => new ProductionPlanDetailReadModel(
                    plan.Id,
                    plan.ReferenceCode,
                    plan.Title,
                    plan.FacilityId,
                    ProductionPlanValueConversions.ToStorage(plan.Status),
                    ProductionPlanValueConversions.ToStorage(plan.Origin),
                    plan.AiGenerated,
                    ProductionPlanValueConversions.ToStorage(plan.Confidence),
                    plan.PlannedStartAtUtc,
                    plan.PlannedEndAtUtc,
                    ProductionPlanValueConversions.ToStorage(plan.ApprovalStatus),
                    ProductionPlanValueConversions.ToStorage(plan.TransferStatus),
                    plan.TransferTargetSystem.HasValue
                        ? ProductionPlanValueConversions.ToStorage(plan.TransferTargetSystem.Value)
                        : null,
                    plan.CreatedAtUtc,
                    plan.UpdatedAtUtc,
                    plan.Version,
                    plan.Notes,
                    plan.CreatedBy,
                    plan.AiExplanation,
                    plan.DemandsJson,
                    plan.RisksJson,
                    plan.ResourceAssignmentJson,
                    plan.TimeWindowJson,
                    plan.ApprovalJson,
                    plan.TransferJson,
                    plan.SourceReferencesJson,
                    plan.HistoryJson))
                .ToListAsync(cancellationToken);

            return matches.Count switch
            {
                0 => ProductionPlanDetailReadResult.Fail(
                    ProductionPlanReadFailure.NotFound("Production plan was not found in the requested scope.")),
                1 => ProductionPlanDetailReadResult.Success(matches[0]),
                _ => ProductionPlanDetailReadResult.Fail(
                    ProductionPlanReadFailure.DataIntegrity(
                        "Multiple production plans matched a single identifier within the requested scope.")),
            };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return ProductionPlanDetailReadResult.Fail(
                ProductionPlanReadFailure.SourceUnavailable("Production plan detail could not be read."));
        }
    }

    internal static IQueryable<ProductionPlan> ApplyListFilters(
        IQueryable<ProductionPlan> query,
        ProductionPlanListReadInput input)
    {
        query = query.Where(plan =>
            plan.TenantId == input.TenantId
            && plan.FacilityId == input.FacilityId);

        if (input.Status.HasValue)
        {
            var status = input.Status.Value;
            query = query.Where(plan => plan.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.Trim();
            query = query.Where(plan =>
                plan.ReferenceCode.Contains(search, StringComparison.OrdinalIgnoreCase)
                || plan.Title.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        return query;
    }

    public static int NormalizeLimit(int? limit)
    {
        if (!limit.HasValue)
        {
            return DefaultLimit;
        }

        if (limit.Value < MinimumLimit)
        {
            return MinimumLimit;
        }

        return limit.Value > MaximumLimit ? MaximumLimit : limit.Value;
    }

    private static ProductionPlanReadFailure? ValidateScope(Guid tenantId, Guid facilityId)
    {
        if (tenantId == Guid.Empty || facilityId == Guid.Empty)
        {
            return ProductionPlanReadFailure.InvalidScope(
                "Tenant and facility identifiers are required for production plan reads.");
        }

        return null;
    }
}

public sealed class UnavailableProductionPlanReadRepository : IProductionPlanReadRepository
{
    public Task<ProductionPlanListReadResult> ListAsync(
        ProductionPlanListReadInput input,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(
            ProductionPlanListReadResult.Fail(
                ProductionPlanReadFailure.SourceUnavailable(
                    "Production plan reads are unavailable because persistence is disabled.")));

    public Task<ProductionPlanDetailReadResult> FindByIdAsync(
        ProductionPlanDetailReadInput input,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(
            ProductionPlanDetailReadResult.Fail(
                ProductionPlanReadFailure.SourceUnavailable(
                    "Production plan reads are unavailable because persistence is disabled.")));
}
