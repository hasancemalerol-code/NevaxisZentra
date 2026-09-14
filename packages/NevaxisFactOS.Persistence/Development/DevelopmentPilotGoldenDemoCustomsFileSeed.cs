using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.GoldenDemoCustomsFile;

namespace NevaxisFactOS.Persistence.Development;

internal static class DevelopmentPilotGoldenDemoCustomsFileSeed
{
    private const string AssignedOutcomeText =
        "Bu siparişin gümrük dosyasını hazırla ve gümrük müşavirine gönderime hazır hale getir.";

    internal static async Task EnsureGoldenDemoCustomsFilePilotDataAsync(
        NevaxisFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (tenantId != DevelopmentPilotIds.TenantId || facilityId != DevelopmentPilotIds.FacilityId)
        {
            return;
        }

        var existing = await dbContext.GoldenDemoCustomsFileOutcomes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == DevelopmentPilotGoldenDemoCustomsFileIds.OutcomeId, cancellationToken);
        if (existing is not null)
        {
            existing.Status = GoldenDemoCustomsFileOutcomeStatuses.Assigned;
            existing.ApprovedBySubjectId = null;
            existing.ApprovedAtUtc = null;
            existing.CurrentPackageId = null;
            existing.UpdatedAtUtc = seedTimestampUtc;
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var productLines = new[]
        {
            new GoldenDemoCustomsFileProductLine(
                "PP-LID-240",
                "PP Container Lid 240mm",
                12000,
                0.42m,
                "EUR",
                "3923.50.00",
                "TR"),
            new GoldenDemoCustomsFileProductLine(
                "PP-BOWL-240",
                "PP Container Bowl 240mm",
                8000,
                0.38m,
                "EUR",
                "3923.50.00",
                "TR"),
        };

        dbContext.GoldenDemoCustomsFileOutcomes.Add(new GoldenDemoCustomsFileOutcome
        {
            Id = DevelopmentPilotGoldenDemoCustomsFileIds.OutcomeId,
            TenantId = tenantId,
            FacilityId = facilityId,
            WorkerCode = GoldenDemoCustomsFileWorkerCodes.CustomsFileDigitalWorker,
            AssignedOutcomeText = AssignedOutcomeText,
            Status = GoldenDemoCustomsFileOutcomeStatuses.Assigned,
            OrderNumber = "EXP-DEMO-DE-001",
            CustomerName = "PackPro GmbH",
            DestinationCountry = "Germany",
            DeliveryAddress = "Industriestrasse 14, 70469 Stuttgart, Germany",
            Currency = "EUR",
            Incoterm = "DAP",
            ProductLinesJson = GoldenDemoCustomsFileJson.SerializeProductLines(productLines),
            PackageCount = 12,
            OrderGrossWeightKg = 510.0m,
            ShipmentReference = "SHP-DE-2026-0142",
            ShipmentNetWeightKg = 485.0m,
            ShipmentGrossWeightKg = 510.0m,
            TradeRequirementsJson = GoldenDemoCustomsFileJson.SerializeTradeRequirements(
                new GoldenDemoCustomsFileTradeRequirements(
                    RequiresAtr: false,
                    RequiresEur1: false,
                    RequiresCertificateOfOrigin: false)),
            SellerName = "Nevaxis Plastik A.Ş.",
            BuyerName = "PackPro GmbH",
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
