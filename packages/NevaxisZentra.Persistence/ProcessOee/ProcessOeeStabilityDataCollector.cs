using NevaxisZentra.Persistence.Edge;
using Microsoft.EntityFrameworkCore;
using static NevaxisZentra.Persistence.ProcessOee.ProcessOeeOperatingTimeCalculator;

namespace NevaxisZentra.Persistence.ProcessOee;

public sealed class ProcessOeeStabilityDataCollector(NevaxisZentraDbContext dbContext)
{
    public async Task<ProcessOeeStabilityDataBundle> CollectAsync(
        ProcessOeeOperationContext context,
        ProcessOeeEvaluationWindow window,
        ProcessOeeEvidenceBundle evidence,
        CancellationToken cancellationToken = default)
    {
        var rawCycles = await dbContext.MachineObservedCycles.AsNoTracking()
            .Where(c => c.TenantId == context.TenantId
                        && c.FacilityId == context.FacilityId
                        && c.MachineId == window.MachineAssetId
                        && c.SourceTimestampUtc >= window.WindowStartUtc
                        && c.SourceTimestampUtc <= window.WindowEndUtc)
            .OrderBy(c => c.SourceTimestampUtc)
            .Select(c => new ProcessOeeCycleObservation(
                c.CycleDurationMs,
                c.Quality,
                c.IsDuplicate,
                c.SourceTimestampUtc))
            .ToListAsync(cancellationToken);

        var downtimeEpisodes = await dbContext.MachineDowntimeEpisodes.AsNoTracking()
            .Where(e => e.TenantId == context.TenantId
                        && e.FacilityId == context.FacilityId
                        && e.MachineId == window.MachineAssetId
                        && e.StartedAtSourceUtc < window.WindowEndUtc
                        && (e.EndedAtSourceUtc == null || e.EndedAtSourceUtc > window.WindowStartUtc))
            .ToListAsync(cancellationToken);

        var clippedEpisodes = downtimeEpisodes
            .Select(e => new ProcessOeeDowntimeObservation(
                e.Id,
                e.ReasonCode,
                e.ReasonCategory,
                e.IsMicroStop,
                e.Quality,
                ClipInterval(
                    e.StartedAtSourceUtc,
                    e.EndedAtSourceUtc ?? window.WindowEndUtc,
                    window.WindowStartUtc,
                    window.WindowEndUtc)))
            .Where(e => e.ClippedDurationMs > 0)
            .ToList();

        IReadOnlyList<ProcessOeeShiftWindowLink> shiftLinks = [];
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
                var assignments = await dbContext.ProductionOrderShiftAssignments.AsNoTracking()
                    .Where(a => a.TenantId == context.TenantId
                                && a.FacilityId == context.FacilityId
                                && a.ProductionOrderId == order.Id
                                && a.ShiftEndAtUtc > window.WindowStartUtc
                                && a.ShiftStartAtUtc < window.WindowEndUtc)
                    .ToListAsync(cancellationToken);

                if (assignments.Count > 0)
                {
                    var shiftIds = assignments.Select(a => a.ProductionShiftId).Distinct().ToList();
                    var shifts = await dbContext.ProductionShifts.AsNoTracking()
                        .Where(s => s.TenantId == context.TenantId
                                    && s.FacilityId == context.FacilityId
                                    && shiftIds.Contains(s.Id))
                        .ToDictionaryAsync(s => s.Id, cancellationToken);

                    shiftLinks = assignments
                        .Select(a =>
                        {
                            shifts.TryGetValue(a.ProductionShiftId, out var shift);
                            var label = shift is not null
                                ? $"{shift.Code} — {shift.Name}"
                                : a.ShiftCode;
                            return new ProcessOeeShiftWindowLink(
                                a.ShiftCode,
                                label,
                                a.ShiftStartAtUtc,
                                a.ShiftEndAtUtc);
                        })
                        .ToList();
                }
            }
        }

        return new ProcessOeeStabilityDataBundle(
            rawCycles,
            clippedEpisodes,
            evidence.IdealCycleTimeSeconds,
            evidence.SourceFreshnessUtc,
            evidence.Conflicts,
            shiftLinks);
    }

    private static long ClipInterval(
        DateTimeOffset start,
        DateTimeOffset end,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd)
    {
        var clippedStart = start < windowStart ? windowStart : start;
        var clippedEnd = end > windowEnd ? windowEnd : end;
        if (clippedEnd <= clippedStart)
        {
            return 0;
        }

        return (long)(clippedEnd - clippedStart).TotalMilliseconds;
    }
}

public sealed record ProcessOeeCycleObservation(
    long CycleDurationMs,
    string Quality,
    bool IsDuplicate,
    DateTimeOffset SourceTimestampUtc);

public sealed record ProcessOeeDowntimeObservation(
    Guid EpisodeId,
    string? ReasonCode,
    string? ReasonCategory,
    bool IsMicroStop,
    string Quality,
    long ClippedDurationMs);

public sealed record ProcessOeeShiftWindowLink(
    string ShiftCode,
    string ShiftLabel,
    DateTimeOffset ShiftStartAtUtc,
    DateTimeOffset ShiftEndAtUtc);

public sealed record ProcessOeeStabilityDataBundle(
    IReadOnlyList<ProcessOeeCycleObservation> RawCycles,
    IReadOnlyList<ProcessOeeDowntimeObservation> DowntimeEpisodes,
    decimal? IdealCycleTimeSeconds,
    DateTimeOffset? SourceFreshnessUtc,
    IReadOnlyList<string> Conflicts,
    IReadOnlyList<ProcessOeeShiftWindowLink> ShiftLinks);
