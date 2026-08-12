using CrevaFactOS.Persistence.Material;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Development;

internal static class DevelopmentPilotMaterialLotSeed
{
    internal static async Task EnsureMaterialLotPilotRecordsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        await EnsureStorageLocationsAsync(
            dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);
        await EnsurePilotPpAvailableLotAsync(
            dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);
        await EnsurePilotAbsQualityHoldLotAsync(
            dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);
        await EnsurePilotBlockedExpiryRiskLotAsync(
            dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);
    }

    private static async Task EnsureStorageLocationsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.MaterialStorageLocations.AsNoTracking()
                .AnyAsync(s => s.Id == DevelopmentPilotIds.MaterialStorageLocationRawWarehouseId, cancellationToken))
        {
            dbContext.MaterialStorageLocations.Add(new MaterialStorageLocation
            {
                Id = DevelopmentPilotIds.MaterialStorageLocationRawWarehouseId,
                TenantId = tenantId,
                FacilityId = facilityId,
                WarehouseCode = "PILOT-HAM",
                WarehouseCodeNormalized = "PILOT-HAM",
                WarehouseName = "Pilot Ham Madde Deposu",
                LocationCode = "A-01",
                LocationCodeNormalized = "A-01",
                LocationName = "Raf A-01",
                IsActive = true,
                SourceType = MaterialSourceType.DevelopmentPilot,
                SourceReference = "material-lot-pilot-raw-a01",
                DataRecordedAtUtc = seedTimestampUtc,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                CreatedBySubjectId = actorSubjectId,
                UpdatedBySubjectId = actorSubjectId,
                Version = 1,
            });
        }

        if (!await dbContext.MaterialStorageLocations.AsNoTracking()
                .AnyAsync(s => s.Id == DevelopmentPilotIds.MaterialStorageLocationQuarantineId, cancellationToken))
        {
            dbContext.MaterialStorageLocations.Add(new MaterialStorageLocation
            {
                Id = DevelopmentPilotIds.MaterialStorageLocationQuarantineId,
                TenantId = tenantId,
                FacilityId = facilityId,
                WarehouseCode = "PILOT-KAL",
                WarehouseCodeNormalized = "PILOT-KAL",
                WarehouseName = "Pilot Kalite Bekleme",
                LocationCode = "Q-01",
                LocationCodeNormalized = "Q-01",
                LocationName = "Karantina Q-01",
                IsActive = true,
                SourceType = MaterialSourceType.DevelopmentPilot,
                SourceReference = "material-lot-pilot-quarantine-q01",
                DataRecordedAtUtc = seedTimestampUtc,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                CreatedBySubjectId = actorSubjectId,
                UpdatedBySubjectId = actorSubjectId,
                Version = 1,
            });
        }

        if (!await dbContext.MaterialStorageLocations.AsNoTracking()
                .AnyAsync(s => s.Id == DevelopmentPilotIds.MaterialStorageLocationBlockedBayId, cancellationToken))
        {
            dbContext.MaterialStorageLocations.Add(new MaterialStorageLocation
            {
                Id = DevelopmentPilotIds.MaterialStorageLocationBlockedBayId,
                TenantId = tenantId,
                FacilityId = facilityId,
                WarehouseCode = "PILOT-BLK",
                WarehouseCodeNormalized = "PILOT-BLK",
                WarehouseName = "Pilot Bloke Alan",
                LocationCode = "B-01",
                LocationCodeNormalized = "B-01",
                LocationName = "Bloke B-01",
                IsActive = true,
                SourceType = MaterialSourceType.DevelopmentPilot,
                SourceReference = "material-lot-pilot-blocked-b01",
                DataRecordedAtUtc = seedTimestampUtc,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                CreatedBySubjectId = actorSubjectId,
                UpdatedBySubjectId = actorSubjectId,
                Version = 1,
            });
        }
    }

    private static async Task EnsurePilotPpAvailableLotAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.MaterialLots.AsNoTracking()
                .AnyAsync(l => l.Id == DevelopmentPilotIds.MaterialPilotLotPpAvailableId, cancellationToken))
        {
            return;
        }

        var lot = new MaterialLot
        {
            Id = DevelopmentPilotIds.MaterialPilotLotPpAvailableId,
            TenantId = tenantId,
            FacilityId = facilityId,
            MaterialId = DevelopmentPilotIds.MaterialPilotPpId,
            LotNumber = "PILOT-PP-LOT-001",
            LotNumberNormalized = "PILOT-PP-LOT-001",
            SupplierLotNumber = "SUP-PP-2026-001",
            ManufacturerLotNumber = "MFG-PP-H110-001",
            SupplierReference = "pilot-supplier-ref-pp",
            GoodsReceiptReference = "pilot-gr-pp-001",
            ManufactureDate = DateOnly.FromDateTime(seedTimestampUtc.UtcDateTime.AddMonths(-2)),
            ReceivedAtUtc = seedTimestampUtc.AddDays(-14),
            ExpiryDate = DateOnly.FromDateTime(seedTimestampUtc.UtcDateTime.AddMonths(18)),
            InitialQuantity = 1000m,
            CurrentObservedQuantity = 1000m,
            UnitOfMeasure = "kg",
            Status = MaterialLotStatus.Available,
            QualityStatus = MaterialLotQualityStatus.Accepted,
            CurrentStorageLocationId = DevelopmentPilotIds.MaterialStorageLocationRawWarehouseId,
            CurrentWarehouseReference = "PILOT-HAM",
            CurrentLocationReference = "A-01",
            SourceType = MaterialSourceType.DevelopmentPilot,
            SourceReference = "material-lot-pilot-pp-available",
            DataRecordedAtUtc = seedTimestampUtc,
            Notes = "Deterministik geliştirme/pilot verisi — OEM/doğrulanmış tedarikçi lotu değildir.",
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = actorSubjectId,
            UpdatedBySubjectId = actorSubjectId,
            Version = 1,
        };

        dbContext.MaterialLots.Add(lot);
        dbContext.MaterialLotTraceabilityEvents.Add(new MaterialLotTraceabilityEvent
        {
            Id = Guid.Parse("bb000001-00ae-4000-8000-0000000000ae"),
            TenantId = tenantId,
            FacilityId = facilityId,
            MaterialLotId = lot.Id,
            EventType = MaterialLotEventType.LotCreated,
            EventAtUtc = seedTimestampUtc.AddDays(-15),
            SourceType = MaterialSourceType.DevelopmentPilot,
            SourceReference = "material-lot-pilot-pp-available",
            IdempotencyKey = $"lot-created:{lot.Id}",
            CreatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = actorSubjectId,
            Version = 1,
        });
        dbContext.MaterialLotTraceabilityEvents.Add(new MaterialLotTraceabilityEvent
        {
            Id = Guid.Parse("bb000001-00af-4000-8000-0000000000af"),
            TenantId = tenantId,
            FacilityId = facilityId,
            MaterialLotId = lot.Id,
            EventType = MaterialLotEventType.ReceiptObserved,
            EventAtUtc = seedTimestampUtc.AddDays(-14),
            Quantity = 1000m,
            UnitOfMeasure = "kg",
            ToWarehouseReference = "PILOT-HAM",
            ToLocationReference = "A-01",
            StatusBefore = MaterialLotStatus.Draft,
            StatusAfter = MaterialLotStatus.Received,
            SourceType = MaterialSourceType.DevelopmentPilot,
            SourceReference = "material-lot-pilot-pp-receipt",
            IdempotencyKey = "pilot-pp-receipt-001",
            CreatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = actorSubjectId,
            Version = 1,
        });
        dbContext.MaterialLotCertificateReferences.Add(new MaterialLotCertificateReference
        {
            Id = DevelopmentPilotIds.MaterialPilotLotPpCertificateId,
            TenantId = tenantId,
            FacilityId = facilityId,
            MaterialLotId = lot.Id,
            CertificateType = MaterialLotCertificateType.CertificateOfAnalysis,
            DocumentReference = "pilot-coa-ref-pp-001",
            DocumentNumber = "COA-PILOT-PP-001",
            Issuer = "Pilot Polimer A.Ş. (Geliştirme)",
            IssuedDate = DateOnly.FromDateTime(seedTimestampUtc.UtcDateTime.AddDays(-20)),
            SourceType = MaterialSourceType.DevelopmentPilot,
            SourceReference = "material-lot-pilot-pp-coa",
            IdempotencyKey = "pilot-pp-coa-001",
            CreatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = actorSubjectId,
            Version = 1,
        });
    }

    private static async Task EnsurePilotAbsQualityHoldLotAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.MaterialLots.AsNoTracking()
                .AnyAsync(l => l.Id == DevelopmentPilotIds.MaterialPilotLotAbsQualityHoldId, cancellationToken))
        {
            return;
        }

        dbContext.MaterialLots.Add(new MaterialLot
        {
            Id = DevelopmentPilotIds.MaterialPilotLotAbsQualityHoldId,
            TenantId = tenantId,
            FacilityId = facilityId,
            MaterialId = DevelopmentPilotIds.MaterialPilotAbsId,
            LotNumber = "PILOT-ABS-LOT-001",
            LotNumberNormalized = "PILOT-ABS-LOT-001",
            SupplierLotNumber = "SUP-ABS-2026-001",
            ManufacturerLotNumber = "MFG-ABS-GP22-001",
            ReceivedAtUtc = seedTimestampUtc.AddDays(-7),
            ExpiryDate = DateOnly.FromDateTime(seedTimestampUtc.UtcDateTime.AddMonths(12)),
            InitialQuantity = 500m,
            CurrentObservedQuantity = 500m,
            UnitOfMeasure = "kg",
            Status = MaterialLotStatus.QualityHold,
            QualityStatus = MaterialLotQualityStatus.Pending,
            CurrentStorageLocationId = DevelopmentPilotIds.MaterialStorageLocationQuarantineId,
            CurrentWarehouseReference = "PILOT-KAL",
            CurrentLocationReference = "Q-01",
            SourceType = MaterialSourceType.DevelopmentPilot,
            SourceReference = "material-lot-pilot-abs-quality-hold",
            DataRecordedAtUtc = seedTimestampUtc,
            Notes = "Deterministik geliştirme/pilot verisi — kalite bekleme örneği.",
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = actorSubjectId,
            UpdatedBySubjectId = actorSubjectId,
            Version = 1,
        });
    }

    private static async Task EnsurePilotBlockedExpiryRiskLotAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.MaterialLots.AsNoTracking()
                .AnyAsync(l => l.Id == DevelopmentPilotIds.MaterialPilotLotBlockedExpiryRiskId, cancellationToken))
        {
            return;
        }

        var expiryRiskDate = DateOnly.FromDateTime(seedTimestampUtc.UtcDateTime.AddDays(14));

        dbContext.MaterialLots.Add(new MaterialLot
        {
            Id = DevelopmentPilotIds.MaterialPilotLotBlockedExpiryRiskId,
            TenantId = tenantId,
            FacilityId = facilityId,
            MaterialId = DevelopmentPilotIds.MaterialPilotBlockedId,
            LotNumber = "PILOT-BLK-LOT-001",
            LotNumberNormalized = "PILOT-BLK-LOT-001",
            SupplierLotNumber = "SUP-MB-2025-009",
            ReceivedAtUtc = seedTimestampUtc.AddMonths(-6),
            ExpiryDate = expiryRiskDate,
            RetestDate = expiryRiskDate,
            InitialQuantity = 75m,
            CurrentObservedQuantity = 75m,
            UnitOfMeasure = "kg",
            Status = MaterialLotStatus.Blocked,
            QualityStatus = MaterialLotQualityStatus.Rejected,
            CurrentStorageLocationId = DevelopmentPilotIds.MaterialStorageLocationBlockedBayId,
            CurrentWarehouseReference = "PILOT-BLK",
            CurrentLocationReference = "B-01",
            SourceType = MaterialSourceType.DevelopmentPilot,
            SourceReference = "material-lot-pilot-blocked-expiry-risk",
            DataRecordedAtUtc = seedTimestampUtc,
            Notes = "Deterministik geliştirme/pilot verisi — bloke ve SKT riski örneği.",
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = actorSubjectId,
            UpdatedBySubjectId = actorSubjectId,
            Version = 1,
        });
    }
}
