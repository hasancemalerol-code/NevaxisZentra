using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.Development;
using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.FactoryOutcomeExecution;

namespace NevaxisFactOS.Persistence.FactoryRecoveryIntelligence;

public static class DevelopmentFactoryRecoveryLearningFixtureSeeder
{
    public static async Task SeedAsync(
        NevaxisFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        CancellationToken cancellationToken)
    {
        if (facilityId != DevelopmentPilotIds.FacilityId)
        {
            return;
        }

        var exists = await dbContext.FactoryOutcomeLearningRecords.AnyAsync(
            r => r.TenantId == tenantId && r.FacilityId == facilityId && r.LogicVersion == "fixture-v1",
            cancellationToken);
        if (exists)
        {
            return;
        }

        var fixtures = BuildFixtures(tenantId, facilityId);
        dbContext.FactoryOutcomeLearningRecords.AddRange(fixtures);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<FactoryOutcomeLearningRecord> BuildFixtures(Guid tenantId, Guid facilityId)
    {
        var now = DateTimeOffset.UtcNow;
        return
        [
            CreateFixture(tenantId, facilityId, Guid.Parse("f1000001-0001-4000-8000-000000000001"),
                "order-delivery-risk", "production.performance-degradation", "INJ-002", "ORD-7128",
                "Kaynak kurtarma + kapasite koruma", FactoryRecoveryLearningDisposition.VerifiedSuccess,
                41, true, 1180, "Cycle degradation + material pressure", now.AddDays(-14)),
            CreateFixture(tenantId, facilityId, Guid.Parse("f1000002-0002-4000-8000-000000000002"),
                "order-delivery-risk", "production.performance-degradation", "INJ-004", "ORD-8742",
                "Kaynak kurtarma + kapasite koruma", FactoryRecoveryLearningDisposition.VerifiedSuccess,
                38, true, 1210, "Cycle degradation + delivery risk", now.AddDays(-7)),
            CreateFixture(tenantId, facilityId, Guid.Parse("f1000003-0003-4000-8000-000000000003"),
                "order-delivery-risk", "material.shortage", "INJ-004", "ORD-8742",
                "Malzeme rezervasyonu + üretim koruma", FactoryRecoveryLearningDisposition.VerifiedSuccess,
                52, true, 1195, "Material shortage recovery", now.AddDays(-5)),
            CreateFixture(tenantId, facilityId, Guid.Parse("f1000004-0004-4000-8000-000000000004"),
                "order-delivery-risk", "production.performance-degradation", "INJ-004", "ORD-8742",
                "Alternatif kapasiteye kaydırma", FactoryRecoveryLearningDisposition.VerifiedSuccess,
                47, true, 1205, "Alternate capacity required", now.AddDays(-3)),
            CreateFixture(tenantId, facilityId, Guid.Parse("f1000005-0005-4000-8000-000000000005"),
                "order-delivery-risk", "production.performance-degradation", "INJ-004", "ORD-8742",
                "Kaynak kurtarma + kapasite koruma", FactoryRecoveryLearningDisposition.VerificationFailed,
                95, false, 900, "Machine-only intervention insufficient", now.AddDays(-2)),
        ];
    }

    private static FactoryOutcomeLearningRecord CreateFixture(
        Guid tenantId,
        Guid facilityId,
        Guid sourceCaseId,
        string outcomeType,
        string eventCategory,
        string machineCode,
        string orderReference,
        string strategy,
        string disposition,
        int recoveryMinutes,
        bool deliveryProtected,
        int recoveredQty,
        string cause,
        DateTimeOffset verifiedAtUtc)
    {
        var patternKey = FactoryRecoveryPatternEngine.BuildPatternKey(
            outcomeType, eventCategory, machineCode, "PP-GRANULE");
        var pattern = new FactoryRecoveryPattern(
            outcomeType,
            eventCategory,
            machineCode,
            orderReference,
            "PP-GRANULE",
            null,
            "critical",
            patternKey,
            strategy,
            [DigitalWorkforceWorkerCodes.ProductionRecovery, DigitalWorkforceWorkerCodes.MaterialShortage],
            disposition == FactoryRecoveryLearningDisposition.VerificationFailed
                ? ["waiting-human"]
                : []);

        return new FactoryOutcomeLearningRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            SourceCaseId = sourceCaseId,
            RecordVersion = 1,
            IdempotencyKey = $"{sourceCaseId:N}:{disposition}:v1",
            LogicVersion = "fixture-v1",
            LearningDisposition = disposition,
            ProblemPattern = $"order-risk:{orderReference}",
            CausePattern = cause,
            OutcomeType = outcomeType,
            EventCategory = eventCategory,
            MachineCode = machineCode,
            OrderReference = orderReference,
            MaterialCode = "PP-GRANULE",
            Severity = "critical",
            PatternKey = patternKey,
            PatternJson = FactoryRecoveryPatternEngine.Serialize(pattern),
            SelectedStrategyKey = strategy,
            AlternativeStrategyKey = "Alternatif kapasiteye kaydırma",
            ActionsJson = JsonSerializer.Serialize(new List<string> { "assess-cycle-loss", "secure-material-reservation" }),
            WorkersJson = JsonSerializer.Serialize(pattern.WorkerCodes),
            BlockersJson = JsonSerializer.Serialize(pattern.BlockerProfile),
            FailureModesJson = JsonSerializer.Serialize(
                disposition == FactoryRecoveryLearningDisposition.VerifiedSuccess
                    ? new List<string>()
                    : new List<string> { "Yalnız makine müdahalesi yeterli olmadı; malzeme rezervasyon problemi devam etti." }),
            ActualRecoveredQuantity = recoveredQty,
            TimeToRecoveryMinutes = recoveryMinutes,
            TimeToVerifiedMinutes = recoveryMinutes,
            DeliveryProtected = deliveryProtected,
            EvidenceStrength = disposition == FactoryRecoveryLearningDisposition.VerifiedSuccess
                ? FactoryRecoveryEvidenceStrength.Medium
                : FactoryRecoveryEvidenceStrength.Low,
            VerificationResult = disposition == FactoryRecoveryLearningDisposition.VerifiedSuccess
                ? DigitalWorkVerificationResults.Passed
                : DigitalWorkVerificationResults.Failed,
            FailureSummary = disposition == FactoryRecoveryLearningDisposition.VerifiedSuccess
                ? null
                : cause,
            VerifiedAtUtc = verifiedAtUtc,
            CreatedAtUtc = verifiedAtUtc,
        };
    }
}
