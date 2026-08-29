using CrevaFactOS.Persistence.Production;
using CrevaFactOS.Persistence.ProductionPlanning;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Development;

internal static class DevelopmentPilotProductionExceptionSeed
{
    public static async Task EnsureProductionFollowUpPilotFixturesAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string createdBySubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.ProductionExecutionExceptions
            .AsNoTracking()
            .AnyAsync(
                e => e.Id == DevelopmentPilotIds.ProductionPilotExceptionFollowUpId,
                cancellationToken);

        if (existing)
        {
            return;
        }

        if (!await dbContext.ProductionPlanHandoffs.AsNoTracking()
                .AnyAsync(h => h.Id == DevelopmentPilotIds.ProductionPilotFollowUpHandoffId, cancellationToken))
        {
            dbContext.ProductionPlanHandoffs.Add(new ProductionPlanHandoff
            {
                Id = DevelopmentPilotIds.ProductionPilotFollowUpHandoffId,
                TenantId = tenantId,
                FacilityId = facilityId,
                ProductionPlanId = DevelopmentPilotIds.PlanScheduledId,
                ProductionPlanVersion = 1,
                ReferenceCode = "PILOT-HANDOFF-PROD-FU-001",
                Status = ProductionPlanHandoffStatus.Consumed,
                IdempotencyKey = "pilot-production-follow-up-handoff-001",
                PreparedAtUtc = seedTimestampUtc.AddDays(-1),
                PreparedBySubjectId = createdBySubjectId,
                ConsumedAtUtc = seedTimestampUtc.AddHours(-12),
                ProductionReference = "PILOT-PO-PROD-FU-001",
                CorrelationId = DevelopmentPilotIds.ProductionPilotFollowUpHandoffId,
                Version = 1,
                PayloadJson =
                    """
                    {"scheduledStartAtUtc":"2026-08-28T06:00:00.000Z","scheduledEndAtUtc":"2026-08-28T14:00:00.000Z","shiftName":"Gündüz","machineId":"IM-120","moldId":"MOLD-PILOT-001","demands":[{"productId":"PP-CAP-32","quantity":12000,"unit":"adet"}]}
                    """,
                CreatedAtUtc = seedTimestampUtc.AddDays(-1),
                UpdatedAtUtc = seedTimestampUtc.AddHours(-12),
            });
        }

        if (!await dbContext.ProductionPlanHandoffs.AsNoTracking()
                .AnyAsync(h => h.Id == DevelopmentPilotIds.ProductionPilotFollowUpInsufficientHandoffId, cancellationToken))
        {
            dbContext.ProductionPlanHandoffs.Add(new ProductionPlanHandoff
            {
                Id = DevelopmentPilotIds.ProductionPilotFollowUpInsufficientHandoffId,
                TenantId = tenantId,
                FacilityId = facilityId,
                ProductionPlanId = DevelopmentPilotIds.PlanScheduledId,
                ProductionPlanVersion = 1,
                ReferenceCode = "PILOT-HANDOFF-PROD-FU-DATA",
                Status = ProductionPlanHandoffStatus.Consumed,
                IdempotencyKey = "pilot-production-follow-up-handoff-data-001",
                PreparedAtUtc = seedTimestampUtc.AddDays(-1),
                PreparedBySubjectId = createdBySubjectId,
                ConsumedAtUtc = seedTimestampUtc.AddHours(-8),
                ProductionReference = "PILOT-PO-PROD-FU-DATA",
                CorrelationId = DevelopmentPilotIds.ProductionPilotFollowUpInsufficientHandoffId,
                Version = 1,
                PayloadJson = "{}",
                CreatedAtUtc = seedTimestampUtc.AddDays(-1),
                UpdatedAtUtc = seedTimestampUtc.AddHours(-8),
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (!await dbContext.ProductionOrders.AsNoTracking()
                .AnyAsync(o => o.Id == DevelopmentPilotIds.ProductionPilotFollowUpOrderId, cancellationToken))
        {
            dbContext.ProductionOrders.AddRange(
                new ProductionOrder
                {
                    Id = DevelopmentPilotIds.ProductionPilotFollowUpOrderId,
                    TenantId = tenantId,
                    FacilityId = facilityId,
                    ProductionPlanHandoffId = DevelopmentPilotIds.ProductionPilotFollowUpHandoffId,
                    SourceProductionPlanId = DevelopmentPilotIds.PlanScheduledId,
                    SourceProductionPlanVersion = 1,
                    ReferenceCode = "PILOT-PO-PROD-FU-001",
                    Title = "Pilot üretim emri — istisna takibi",
                    Status = ProductionOrderStatus.InProgress,
                    PlannedStartAtUtc = seedTimestampUtc.AddHours(-10),
                    PlannedEndAtUtc = seedTimestampUtc.AddHours(2),
                    ActualStartAtUtc = seedTimestampUtc.AddHours(-9),
                    ShiftName = "Gündüz",
                    MachineReference = "IM-120",
                    MoldReference = "MOLD-PILOT-001",
                    ProductReference = "PP-CAP-32",
                    PlannedQuantity = 12000,
                    PlannedQuantityUnit = "adet",
                    GoodQuantity = 4200,
                    ScrapQuantity = 18,
                    CreatedBySubjectId = createdBySubjectId,
                    LastModifiedBySubjectId = createdBySubjectId,
                    Version = 1,
                    CreatedAtUtc = seedTimestampUtc.AddHours(-12),
                    UpdatedAtUtc = seedTimestampUtc.AddHours(-1),
                    HistoryJson = "[]",
                },
                new ProductionOrder
                {
                    Id = DevelopmentPilotIds.ProductionPilotFollowUpInsufficientOrderId,
                    TenantId = tenantId,
                    FacilityId = facilityId,
                    ProductionPlanHandoffId = DevelopmentPilotIds.ProductionPilotFollowUpInsufficientHandoffId,
                    SourceProductionPlanId = DevelopmentPilotIds.PlanScheduledId,
                    SourceProductionPlanVersion = 1,
                    ReferenceCode = "PILOT-PO-PROD-FU-DATA",
                    Title = "Pilot üretim emri — eksik kanıt",
                    Status = ProductionOrderStatus.Paused,
                    PlannedStartAtUtc = seedTimestampUtc.AddHours(-8),
                    PlannedEndAtUtc = seedTimestampUtc.AddHours(4),
                    CreatedBySubjectId = createdBySubjectId,
                    LastModifiedBySubjectId = createdBySubjectId,
                    Version = 1,
                    CreatedAtUtc = seedTimestampUtc.AddHours(-8),
                    UpdatedAtUtc = seedTimestampUtc.AddHours(-2),
                    HistoryJson = "[]",
                });

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        dbContext.ProductionExecutionExceptions.AddRange(
            new ProductionExecutionExceptionRecord
            {
                Id = DevelopmentPilotIds.ProductionPilotExceptionFollowUpId,
                TenantId = tenantId,
                FacilityId = facilityId,
                ProductionOrderId = DevelopmentPilotIds.ProductionPilotFollowUpOrderId,
                ExceptionType = ProductionExceptionTypes.StaleDowntime,
                Severity = ProductionExceptionSeverities.Critical,
                Status = ProductionExceptionStatuses.Open,
                Message = "Aktif duruş oturumu 45 dakikadan uzun süredir kapatılmadı; operatör müdahalesi gerekli.",
                RaisedAtUtc = seedTimestampUtc.AddHours(-1),
                RaisedBySubjectId = createdBySubjectId,
                CorrelationId = DevelopmentPilotIds.ProductionPilotExceptionFollowUpId,
                Version = 1,
                FollowUpStatus = ProductionFollowUpStatuses.RequiresReview,
            },
            new ProductionExecutionExceptionRecord
            {
                Id = DevelopmentPilotIds.ProductionPilotExceptionInsufficientEvidenceId,
                TenantId = tenantId,
                FacilityId = facilityId,
                ProductionOrderId = DevelopmentPilotIds.ProductionPilotFollowUpInsufficientOrderId,
                ExceptionType = ProductionExceptionTypes.Other,
                Severity = ProductionExceptionSeverities.Warning,
                Status = ProductionExceptionStatuses.Open,
                Message = " ",
                RaisedAtUtc = seedTimestampUtc.AddMinutes(-30),
                RaisedBySubjectId = createdBySubjectId,
                CorrelationId = DevelopmentPilotIds.ProductionPilotExceptionInsufficientEvidenceId,
                Version = 1,
                FollowUpStatus = ProductionFollowUpStatuses.RequiresReview,
            });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
