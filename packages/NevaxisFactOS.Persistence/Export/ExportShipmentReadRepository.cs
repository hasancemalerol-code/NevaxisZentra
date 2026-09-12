using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Export;

public sealed class ExportShipmentReadRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IExportShipmentReadRepository
{
    public async Task<ExportShipmentListResult> ListAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var shipments = await dbContext.ExportShipments
                        .AsNoTracking()
                        .Where(s => s.TenantId == context.TenantId
                            && s.FacilityId == context.FacilityId
                            && s.ExportCaseId == exportCaseId)
                        .OrderBy(s => s.CreatedAtUtc)
                        .ToListAsync(ct);

                    return ExportShipmentListResult.Success(shipments.Select(MapShipment).ToList());
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportShipmentListResult.Fail(ExportFailureCodes.SourceUnavailable, "Export shipment service is unavailable.");
        }
    }

    public async Task<ExportShipmentDetailResult> GetAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        Guid shipmentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var shipment = await dbContext.ExportShipments
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            s => s.TenantId == context.TenantId
                                && s.FacilityId == context.FacilityId
                                && s.ExportCaseId == exportCaseId
                                && s.Id == shipmentId,
                            ct);

                    if (shipment is null)
                    {
                        return ExportShipmentDetailResult.Missing();
                    }

                    return ExportShipmentDetailResult.Success(MapShipment(shipment));
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportShipmentDetailResult.Fail(ExportFailureCodes.SourceUnavailable, "Export shipment service is unavailable.");
        }
    }

    public async Task<ExportShipmentReadinessResult> GetReadinessAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        Guid shipmentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var shipment = await dbContext.ExportShipments
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            s => s.TenantId == context.TenantId
                                && s.FacilityId == context.FacilityId
                                && s.ExportCaseId == exportCaseId
                                && s.Id == shipmentId,
                            ct);

                    if (shipment is null)
                    {
                        return ExportShipmentReadinessResult.Missing();
                    }

                    var exportCase = await dbContext.ExportCases
                        .AsNoTracking()
                        .SingleAsync(c => c.Id == exportCaseId, ct);

                    var documents = await dbContext.ExportDocuments
                        .AsNoTracking()
                        .Where(d => d.TenantId == context.TenantId
                            && d.ExportCaseId == exportCaseId)
                        .ToListAsync(ct);

                    var hasOpenBlockingCorrection = await dbContext.ExportBrokerCorrections
                        .AsNoTracking()
                        .AnyAsync(c => c.TenantId == context.TenantId
                            && c.ExportCaseId == exportCaseId
                            && c.Severity == ExportBrokerCorrectionSeverity.Blocking
                            && c.Status == ExportBrokerCorrectionStatus.Open,
                            ct);

                    var model = ExportShipmentReadinessEngine.Calculate(
                        shipment, exportCase, exportCase.PackingStatus, documents, hasOpenBlockingCorrection);

                    return ExportShipmentReadinessResult.Success(model);
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportShipmentReadinessResult.Fail(ExportFailureCodes.SourceUnavailable, "Export shipment service is unavailable.");
        }
    }

    private static ExportShipmentReadModel MapShipment(ExportShipment s) =>
        new(
            s.Id,
            s.ExportCaseId,
            s.ShipmentReference,
            ExportValueConversions.ToStorage(s.Status),
            s.TransportMode,
            s.IncotermCode,
            s.DestinationCountryCode,
            s.DestinationCity,
            s.DeliveryAddress,
            s.ForwarderName,
            s.CarrierName,
            s.PlannedPickupAtUtc,
            s.PlannedDepartureAtUtc,
            s.PlannedArrivalAtUtc,
            s.ActualPickupAtUtc,
            s.ActualDepartureAtUtc,
            s.ActualArrivalAtUtc,
            s.BookingReference,
            s.TrackingReference,
            s.TotalGrossWeight,
            s.TotalNetWeight,
            s.TotalPackageCount,
            s.CancellationReason,
            s.Version,
            s.CreatedAtUtc,
            s.UpdatedAtUtc);
}

public sealed class UnavailableExportShipmentReadRepository : IExportShipmentReadRepository
{
    private static readonly ExportShipmentListResult ListUnavailable =
        ExportShipmentListResult.Fail(ExportFailureCodes.SourceUnavailable, "Export shipment persistence is disabled.");

    private static readonly ExportShipmentDetailResult DetailUnavailable =
        ExportShipmentDetailResult.Fail(ExportFailureCodes.SourceUnavailable, "Export shipment persistence is disabled.");

    private static readonly ExportShipmentReadinessResult ReadinessUnavailable =
        ExportShipmentReadinessResult.Fail(ExportFailureCodes.SourceUnavailable, "Export shipment persistence is disabled.");

    public Task<ExportShipmentListResult> ListAsync(ExportOperationContext context, Guid exportCaseId, CancellationToken cancellationToken = default) => Task.FromResult(ListUnavailable);
    public Task<ExportShipmentDetailResult> GetAsync(ExportOperationContext context, Guid exportCaseId, Guid shipmentId, CancellationToken cancellationToken = default) => Task.FromResult(DetailUnavailable);
    public Task<ExportShipmentReadinessResult> GetReadinessAsync(ExportOperationContext context, Guid exportCaseId, Guid shipmentId, CancellationToken cancellationToken = default) => Task.FromResult(ReadinessUnavailable);
}
