using System.Globalization;
using System.Text.Json;
using CrevaFactOS.Persistence.ProcessOee;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Development;

internal static class DevelopmentPilotProcessOeeSeed
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    internal static async Task EnsureProcessOeePilotRecordsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var windows = await EnsurePilotWindowsAsync(
            dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);
        await EnsurePilotAssessmentsAsync(
            dbContext, windows, actorSubjectId, seedTimestampUtc, cancellationToken);
    }

    private static async Task<IReadOnlyList<ProcessOeeEvaluationWindow>> EnsurePilotWindowsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var anchorUtc = seedTimestampUtc.UtcDateTime.Date;
        var windowStart = new DateTimeOffset(anchorUtc.AddDays(-1).AddHours(6), TimeSpan.Zero);
        var windowEnd = windowStart.AddHours(8);
        var windows = new List<ProcessOeeEvaluationWindow>();

        windows.Add(await EnsureWindowAsync(
            dbContext,
            DevelopmentPilotIds.ProcessOeePilotWindowCalculatedId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.EdgePilotPressMachineId,
            "PILOT-PO-PP-001",
            "PILOT-MOLD-PP-4CAV",
            windowStart,
            windowEnd,
            plannedProductionSeconds: 28_800,
            idealCycleTimeSeconds: 45m,
            effectiveCavityCount: 4,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken));

        windows.Add(await EnsureWindowAsync(
            dbContext,
            DevelopmentPilotIds.ProcessOeePilotWindowInsufficientId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.EdgePilotPressMachineId,
            "PILOT-PO-PP-002",
            null,
            windowStart.AddDays(-1),
            windowEnd.AddDays(-1),
            plannedProductionSeconds: null,
            idealCycleTimeSeconds: 45m,
            effectiveCavityCount: 4,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken));

        windows.Add(await EnsureWindowAsync(
            dbContext,
            DevelopmentPilotIds.ProcessOeePilotWindowReviewRequiredId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.EdgePilotPressMachineId,
            "PILOT-PO-PP-001",
            "PILOT-MOLD-PP-4CAV",
            windowStart.AddDays(-2),
            windowEnd.AddDays(-2),
            plannedProductionSeconds: 28_800,
            idealCycleTimeSeconds: 45m,
            effectiveCavityCount: 4,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken));

        return windows;
    }

    private static async Task<ProcessOeeEvaluationWindow> EnsureWindowAsync(
        CrevaFactOSDbContext dbContext,
        Guid id,
        Guid tenantId,
        Guid facilityId,
        Guid machineAssetId,
        string? productionOrderReference,
        string? moldReference,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        int? plannedProductionSeconds,
        decimal? idealCycleTimeSeconds,
        int? effectiveCavityCount,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.ProcessOeeEvaluationWindows.AsNoTracking()
                .AnyAsync(w => w.Id == id, cancellationToken))
        {
            return await dbContext.ProcessOeeEvaluationWindows.AsNoTracking()
                .FirstAsync(w => w.Id == id, cancellationToken);
        }

        var window = new ProcessOeeEvaluationWindow
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            MachineAssetId = machineAssetId,
            ProductionOrderReference = productionOrderReference,
            MoldReference = moldReference,
            WindowStartUtc = windowStartUtc,
            WindowEndUtc = windowEndUtc,
            PlannedProductionSeconds = plannedProductionSeconds,
            IdealCycleTimeSeconds = idealCycleTimeSeconds,
            EffectiveCavityCount = effectiveCavityCount,
            SourceType = ProcessOeeSourceTypes.DevelopmentPilot,
            SourceReference = "development-pilot",
            DataRecordedAtUtc = seedTimestampUtc,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = actorSubjectId,
            UpdatedBySubjectId = actorSubjectId,
            Version = 1,
        };

        dbContext.ProcessOeeEvaluationWindows.Add(window);
        return window;
    }

    private static async Task EnsurePilotAssessmentsAsync(
        CrevaFactOSDbContext dbContext,
        IReadOnlyList<ProcessOeeEvaluationWindow> windows,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var calculatedWindow = windows.First(w => w.Id == DevelopmentPilotIds.ProcessOeePilotWindowCalculatedId);
        var insufficientWindow = windows.First(w => w.Id == DevelopmentPilotIds.ProcessOeePilotWindowInsufficientId);
        var reviewWindow = windows.First(w => w.Id == DevelopmentPilotIds.ProcessOeePilotWindowReviewRequiredId);

        await EnsureAssessmentAsync(
            dbContext,
            DevelopmentPilotIds.ProcessOeePilotAssessmentCalculatedId,
            calculatedWindow,
            ProcessOeeAssessmentStatuses.Calculated,
            inputFingerprint: "pilot-calculated-fingerprint-v1",
            idempotencyKey: "pilot-calculated-eval-v1",
            plannedProductionSeconds: 28_800,
            operatingSeconds: 27_000,
            downtimeSeconds: 1_800,
            unknownSeconds: 0,
            observedCycleCount: 2_400,
            goodPieceCount: 9_600,
            scrapPieceCount: 400,
            availabilityPercent: 93.75m,
            performanceRawPercent: 88.89m,
            performancePercent: 88.89m,
            qualityPercent: 96.00m,
            oeePercent: 80.00m,
            conflicts: [],
            missingFields: [],
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureAssessmentAsync(
            dbContext,
            DevelopmentPilotIds.ProcessOeePilotAssessmentInsufficientId,
            insufficientWindow,
            ProcessOeeAssessmentStatuses.InsufficientData,
            inputFingerprint: "pilot-insufficient-fingerprint-v1",
            idempotencyKey: "pilot-insufficient-eval-v1",
            plannedProductionSeconds: null,
            operatingSeconds: null,
            downtimeSeconds: 0,
            unknownSeconds: 28_800,
            observedCycleCount: null,
            goodPieceCount: null,
            scrapPieceCount: null,
            availabilityPercent: null,
            performanceRawPercent: null,
            performancePercent: null,
            qualityPercent: null,
            oeePercent: null,
            conflicts: [],
            missingFields: ["plannedProductionSeconds", "operatingSeconds", "observedCycleCount", "goodPieceCount", "totalPieceCount"],
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureAssessmentAsync(
            dbContext,
            DevelopmentPilotIds.ProcessOeePilotAssessmentReviewRequiredId,
            reviewWindow,
            ProcessOeeAssessmentStatuses.ReviewRequired,
            inputFingerprint: "pilot-review-fingerprint-v1",
            idempotencyKey: "pilot-review-eval-v1",
            plannedProductionSeconds: 28_800,
            operatingSeconds: 26_400,
            downtimeSeconds: 2_400,
            unknownSeconds: 0,
            observedCycleCount: 2_200,
            goodPieceCount: 8_800,
            scrapPieceCount: 200,
            availabilityPercent: 91.67m,
            performanceRawPercent: 93.75m,
            performancePercent: 93.75m,
            qualityPercent: 97.78m,
            oeePercent: null,
            conflicts: ["production-downtime-vs-edge-downtime"],
            missingFields: [],
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);
    }

    private static async Task EnsureAssessmentAsync(
        CrevaFactOSDbContext dbContext,
        Guid id,
        ProcessOeeEvaluationWindow window,
        string status,
        string inputFingerprint,
        string idempotencyKey,
        int? plannedProductionSeconds,
        int? operatingSeconds,
        int? downtimeSeconds,
        int? unknownSeconds,
        int? observedCycleCount,
        int? goodPieceCount,
        int? scrapPieceCount,
        decimal? availabilityPercent,
        decimal? performanceRawPercent,
        decimal? performancePercent,
        decimal? qualityPercent,
        decimal? oeePercent,
        IReadOnlyList<string> conflicts,
        IReadOnlyList<string> missingFields,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.ProcessOeeAssessments.AsNoTracking()
                .AnyAsync(a => a.Id == id, cancellationToken))
        {
            return;
        }

        var totalPieces = goodPieceCount.HasValue || scrapPieceCount.HasValue
            ? (goodPieceCount ?? 0) + (scrapPieceCount ?? 0)
            : (int?)null;

        var drivers = new[]
        {
            new ProcessOeeDriverReadModel("availability", "Kullanılabilirlik = Çalışma Süresi / Planlı Üretim Süresi", availabilityPercent?.ToString("0.00", CultureInfo.InvariantCulture) + "%", "pass"),
            new ProcessOeeDriverReadModel("rule-set", "PROCESS-OEE-V1", ProcessOeeConstants.RuleSetVersion, "info"),
        };

        var evidencePayload = new
        {
            evidence = Array.Empty<ProcessOeeEvidenceReadModel>(),
            missingFields,
            conflicts,
            sourceSystem = "development-pilot",
            sourceFreshnessUtc = seedTimestampUtc,
            isSimulatorDerived = true,
            honestyDisclaimer = ProcessOeeConstants.CalculationHonestyDisclaimer,
            simulatorDisclaimer = ProcessOeeConstants.SimulatorHonestyDisclaimer,
        };

        dbContext.ProcessOeeAssessments.Add(new ProcessOeeAssessment
        {
            Id = id,
            TenantId = window.TenantId,
            FacilityId = window.FacilityId,
            MachineAssetId = window.MachineAssetId,
            ProductionOrderReference = window.ProductionOrderReference,
            MoldReference = window.MoldReference,
            EvaluationWindowId = window.Id,
            Status = status,
            RuleSetVersion = ProcessOeeConstants.RuleSetVersion,
            InputFingerprint = inputFingerprint,
            WindowStartUtc = window.WindowStartUtc,
            WindowEndUtc = window.WindowEndUtc,
            PlannedProductionSeconds = plannedProductionSeconds,
            OperatingSeconds = operatingSeconds,
            DowntimeSeconds = downtimeSeconds,
            UnknownSeconds = unknownSeconds,
            ObservedCycleCount = observedCycleCount,
            DerivedShotCount = null,
            TotalPieceCount = totalPieces,
            GoodPieceCount = goodPieceCount,
            ScrapPieceCount = scrapPieceCount,
            AvailabilityPercent = availabilityPercent,
            PerformanceRawPercent = performanceRawPercent,
            PerformancePercent = performancePercent,
            QualityPercent = qualityPercent,
            OeePercent = oeePercent,
            StructuredDrivers = JsonSerializer.Serialize(drivers, JsonOptions),
            StructuredEvidence = JsonSerializer.Serialize(evidencePayload, JsonOptions),
            EvaluatedAtUtc = seedTimestampUtc,
            IdempotencyKey = idempotencyKey,
            CreatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = actorSubjectId,
            Version = 1,
        });
    }
}
