using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.ShiftOperations;

public sealed record CoordinatorHandoverRecommendation(
    Guid CaseId,
    string Title,
    string Priority,
    string Recommendation,
    bool RequiresHandover,
    string WorkerCode);

public interface IShiftCoordinatorHandoverBridge
{
    Task<IReadOnlyList<CoordinatorHandoverRecommendation>> BuildRecommendationsAsync(
        OperationalShiftInstance shiftInstance,
        CancellationToken cancellationToken);
}

public sealed class ShiftCoordinatorHandoverBridge : IShiftCoordinatorHandoverBridge
{
    private readonly NevaxisFactOSDbContext dbContext;
    private readonly TenantDatabaseSession tenantDatabaseSession;

    public ShiftCoordinatorHandoverBridge(
        NevaxisFactOSDbContext dbContext,
        TenantDatabaseSession tenantDatabaseSession)
    {
        this.dbContext = dbContext;
        this.tenantDatabaseSession = tenantDatabaseSession;
    }

    public Task<IReadOnlyList<CoordinatorHandoverRecommendation>> BuildRecommendationsAsync(
        OperationalShiftInstance shiftInstance,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => BuildRecommendationsCoreAsync(shiftInstance, ct),
            cancellationToken);

    private async Task<IReadOnlyList<CoordinatorHandoverRecommendation>> BuildRecommendationsCoreAsync(
        OperationalShiftInstance shiftInstance,
        CancellationToken cancellationToken)
    {
        var activeCases = await dbContext.DigitalWorkCases
            .AsNoTracking()
            .Where(c => c.TenantId == shiftInstance.TenantId &&
                        c.FacilityId == shiftInstance.FacilityId &&
                        c.Status != DigitalWorkCaseStatuses.Closed)
            .Include(c => c.PrimaryWorker)
            .ToListAsync(cancellationToken);

        var recommendations = new List<CoordinatorHandoverRecommendation>();
        foreach (var workCase in activeCases)
        {
            var priority = ShiftHandoverPriorityEngine.ResolvePriority(
                MapCategory(workCase.PrimaryWorker?.Code),
                workCase.Severity,
                workCase.RequiresApproval,
                0,
                workCase.TargetAtUtc);

            recommendations.Add(new CoordinatorHandoverRecommendation(
                workCase.Id,
                workCase.Title,
                priority,
                BuildRecommendation(workCase),
                true,
                workCase.PrimaryWorker?.Code ?? DigitalWorkforceWorkerCodes.FactoryCoordinator));
        }

        return recommendations
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.Title)
            .ToList();
    }


    private static string BuildRecommendation(DigitalWorkCase workCase)
    {
        if (workCase.IsHumanControlled)
        {
            return "İnsan kontrolünde — vardiya devri sırasında sahiplik ve kalan aksiyon belirtilmeli.";
        }

        if (workCase.RequiresApproval)
        {
            return "Onay bekliyor — sonraki vardiya onay kararını izlemeli.";
        }

        return "Aktif vaka devam ediyor — aynı Digital Work Case sonraki vardiyada sürdürülmeli.";
    }

    private static string MapCategory(string? workerCode) => workerCode switch
    {
        DigitalWorkforceWorkerCodes.MaintenanceResolution => ShiftOpenItemCategories.Maintenance,
        DigitalWorkforceWorkerCodes.MaterialShortage => ShiftOpenItemCategories.Material,
        DigitalWorkforceWorkerCodes.QualityNcr => ShiftOpenItemCategories.Quality,
        DigitalWorkforceWorkerCodes.ProductionRecovery => ShiftOpenItemCategories.Production,
        _ => ShiftOpenItemCategories.DigitalWorkforce,
    };
}
