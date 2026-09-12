using System.Globalization;
using NevaxisFactOS.Persistence.Edge;
using NevaxisFactOS.Persistence.Mold;
using NevaxisFactOS.Persistence.Production;
using Microsoft.EntityFrameworkCore;
using static NevaxisFactOS.Persistence.ProcessOee.ProcessOeeOperatingTimeCalculator;

namespace NevaxisFactOS.Persistence.ProcessOee;

public sealed class ProcessOeeEvidenceCollector(NevaxisFactOSDbContext dbContext)
{
    private static string Inv(int value) => value.ToString(CultureInfo.InvariantCulture);
    private static string? Inv(int? value) => value?.ToString(CultureInfo.InvariantCulture);
    private static string? Inv(decimal? value) => value?.ToString("0.####", CultureInfo.InvariantCulture);
    public async Task<ProcessOeeEvidenceBundle> CollectAsync(
        ProcessOeeOperationContext context,
        ProcessOeeEvaluationWindow window,
        CancellationToken cancellationToken = default)
    {
        var evidence = new List<ProcessOeeEvidenceReadModel>();
        var conflicts = new List<string>();
        var windowDurationSeconds = ComputeWindowDurationSeconds(window.WindowStartUtc, window.WindowEndUtc);

        var machine = await dbContext.MachineRegistries.AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.Id == window.MachineAssetId
                     && m.TenantId == context.TenantId
                     && m.FacilityId == context.FacilityId,
                cancellationToken);

        evidence.Add(new ProcessOeeEvidenceReadModel(
            "windowDurationSeconds",
            windowDurationSeconds.ToString(CultureInfo.InvariantCulture),
            "process-oee-window",
            window.UpdatedAtUtc,
            false,
            false,
            null));

        int? plannedProductionSeconds = window.PlannedProductionSeconds;
        int? productionGood = null;
        int? productionScrap = null;
        int? productionManualCycles = null;
        int? productionDowntimeSeconds = null;
        DateTimeOffset? productionFreshness = null;
        string? productionReference = null;

        if (!string.IsNullOrWhiteSpace(window.ProductionOrderReference))
        {
            var order = await dbContext.ProductionOrders.AsNoTracking()
                .FirstOrDefaultAsync(
                    o => o.TenantId == context.TenantId
                         && o.FacilityId == context.FacilityId
                         && o.ReferenceCode == window.ProductionOrderReference.Trim(),
                    cancellationToken);

            if (order is not null)
            {
                productionReference = order.ReferenceCode;
                productionGood = (int)order.GoodQuantity;
                productionScrap = (int)order.ScrapQuantity;
                productionManualCycles = order.ManualCycleCount;
                productionDowntimeSeconds = (int?)order.TotalDowntimeSeconds;
                productionFreshness = order.UpdatedAtUtc;

                if (!plannedProductionSeconds.HasValue && order.ActualStartAtUtc.HasValue && order.ActualEndAtUtc.HasValue)
                {
                    plannedProductionSeconds = ComputeWindowDurationSeconds(order.ActualStartAtUtc.Value, order.ActualEndAtUtc.Value);
                }

                evidence.Add(new ProcessOeeEvidenceReadModel(
                    "productionGoodQuantity",
                    Inv(productionGood),
                    "production-order",
                    productionFreshness,
                    !productionGood.HasValue,
                    false,
                    null));
                evidence.Add(new ProcessOeeEvidenceReadModel(
                    "productionScrapQuantity",
                    Inv(productionScrap),
                    "production-order",
                    productionFreshness,
                    !productionScrap.HasValue,
                    false,
                    null));
            }
        }

        var edgeCycles = await dbContext.MachineObservedCycles.AsNoTracking()
            .Where(c => c.TenantId == context.TenantId
                        && c.FacilityId == context.FacilityId
                        && c.MachineId == window.MachineAssetId
                        && !c.IsDuplicate
                        && c.SourceTimestampUtc >= window.WindowStartUtc
                        && c.SourceTimestampUtc <= window.WindowEndUtc)
            .CountAsync(cancellationToken);

        var signal = await dbContext.MachineSignalProjections.AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.TenantId == context.TenantId
                     && s.FacilityId == context.FacilityId
                     && s.MachineId == window.MachineAssetId,
                cancellationToken);

        int? observedCycleCount = edgeCycles > 0 ? edgeCycles : (signal?.ObservedCycleCount > 0 ? (int?)signal.ObservedCycleCount : null);
        var edgeFreshness = signal?.UpdatedAtUtc ?? machine?.UpdatedAtUtc;

        evidence.Add(new ProcessOeeEvidenceReadModel(
            "observedCycleCount",
            Inv(observedCycleCount),
            edgeCycles > 0 ? "edge-observed-cycles" : "edge-signal-projection",
            edgeFreshness,
            !observedCycleCount.HasValue,
            false,
            null));

        var downtimeEpisodes = await dbContext.MachineDowntimeEpisodes.AsNoTracking()
            .Where(e => e.TenantId == context.TenantId
                        && e.FacilityId == context.FacilityId
                        && e.MachineId == window.MachineAssetId
                        && e.StartedAtSourceUtc < window.WindowEndUtc
                        && (e.EndedAtSourceUtc == null || e.EndedAtSourceUtc > window.WindowStartUtc))
            .ToListAsync(cancellationToken);

        var downtimeIntervals = ClipIntervalsToWindow(
            downtimeEpisodes.Select(e => new TimeInterval(
                e.StartedAtSourceUtc,
                e.EndedAtSourceUtc ?? window.WindowEndUtc)),
            window.WindowStartUtc,
            window.WindowEndUtc);

        if (productionDowntimeSeconds.HasValue && SumIntervalSeconds(downtimeIntervals) > 0
            && productionDowntimeSeconds.Value != SumIntervalSeconds(downtimeIntervals))
        {
            conflicts.Add("production-downtime-vs-edge-downtime");
            evidence.Add(new ProcessOeeEvidenceReadModel(
                "productionDowntimeSeconds",
                Inv(productionDowntimeSeconds),
                "production-order",
                productionFreshness,
                false,
                true,
                "Production downtime differs from Edge downtime intervals"));
            evidence.Add(new ProcessOeeEvidenceReadModel(
                "edgeDowntimeSeconds",
                Inv(SumIntervalSeconds(downtimeIntervals)),
                "edge-downtime-episodes",
                edgeFreshness,
                false,
                true,
                "Edge downtime differs from Production downtime total"));
        }

        var mergedDowntime = MergeOverlappingIntervals(downtimeIntervals);
        var (downtimeSeconds, unknownSeconds, operatingSeconds) = ComputeOperatingBreakdown(
            windowDurationSeconds,
            plannedProductionSeconds,
            mergedDowntime);

        decimal? idealCycleTimeSeconds = window.IdealCycleTimeSeconds;
        int? effectiveCavityCount = window.EffectiveCavityCount;
        DateTimeOffset? moldFreshness = null;

        if (!string.IsNullOrWhiteSpace(window.MoldReference))
        {
            var mold = await dbContext.Molds.AsNoTracking()
                .FirstOrDefaultAsync(
                    m => m.TenantId == context.TenantId
                         && m.FacilityId == context.FacilityId
                         && m.Code == window.MoldReference.Trim(),
                    cancellationToken);

            if (mold is not null)
            {
                var requirement = await dbContext.MoldCompatibilityRequirements.AsNoTracking()
                    .FirstOrDefaultAsync(r => r.MoldId == mold.Id, cancellationToken);

                moldFreshness = requirement?.DataRecordedAtUtc ?? mold.UpdatedAtUtc;

                if (!idealCycleTimeSeconds.HasValue && requirement?.NominalCycleTimeSeconds is > 0)
                {
                    idealCycleTimeSeconds = requirement.NominalCycleTimeSeconds;
                }

                if (!effectiveCavityCount.HasValue && requirement?.EffectiveCavityCount is > 0)
                {
                    effectiveCavityCount = requirement.EffectiveCavityCount;
                }
                else if (!effectiveCavityCount.HasValue && mold.CavityCount > 0)
                {
                    effectiveCavityCount = mold.CavityCount;
                }

                evidence.Add(new ProcessOeeEvidenceReadModel(
                    "idealCycleTimeSeconds",
                    Inv(idealCycleTimeSeconds),
                    "mold-compatibility",
                    moldFreshness,
                    !idealCycleTimeSeconds.HasValue,
                    false,
                    null));
                evidence.Add(new ProcessOeeEvidenceReadModel(
                    "effectiveCavityCount",
                    Inv(effectiveCavityCount),
                    "mold-registry",
                    moldFreshness,
                    !effectiveCavityCount.HasValue,
                    false,
                    null));
            }
        }

        int? goodPieceCount = productionGood;
        int? scrapPieceCount = productionScrap;
        int? totalPieceCount = goodPieceCount.HasValue || scrapPieceCount.HasValue
            ? (goodPieceCount ?? 0) + (scrapPieceCount ?? 0)
            : null;

        int? derivedShotCount = null;
        if (!observedCycleCount.HasValue && totalPieceCount.HasValue && effectiveCavityCount is > 0)
        {
            derivedShotCount = (int)Math.Ceiling(totalPieceCount.Value / (decimal)effectiveCavityCount.Value);
            evidence.Add(new ProcessOeeEvidenceReadModel(
                "derivedShotCount",
                Inv(derivedShotCount),
                "derived-from-total-and-cavity",
                null,
                false,
                false,
                "Derived from total pieces and effective cavity count"));
        }

        var isSimulatorDerived = string.Equals(
            ProcessOeeValueConversions.NormalizeSourceType(window.SourceType),
            ProcessOeeSourceTypes.DevelopmentPilot,
            StringComparison.Ordinal)
            || machine?.MachineCode?.Contains("SIM", StringComparison.OrdinalIgnoreCase) == true;

        var sourceFreshness = new[] { productionFreshness, edgeFreshness, moldFreshness, window.DataRecordedAtUtc }
            .Where(v => v.HasValue)
            .OrderByDescending(v => v)
            .FirstOrDefault();

        return new ProcessOeeEvidenceBundle(
            windowDurationSeconds,
            plannedProductionSeconds,
            operatingSeconds,
            downtimeSeconds,
            unknownSeconds,
            observedCycleCount ?? productionManualCycles,
            derivedShotCount,
            totalPieceCount,
            goodPieceCount,
            scrapPieceCount,
            idealCycleTimeSeconds,
            effectiveCavityCount,
            productionReference ?? window.ProductionOrderReference,
            window.MoldReference,
            productionReference is not null ? "production-order" : window.SourceReference,
            sourceFreshness,
            isSimulatorDerived,
            conflicts.Count > 0,
            evidence,
            conflicts);
    }
}
