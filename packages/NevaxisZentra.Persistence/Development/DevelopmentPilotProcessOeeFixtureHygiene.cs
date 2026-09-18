using NevaxisZentra.Persistence.ProcessOee;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Development;

internal static class DevelopmentPilotProcessOeeFixtureHygiene
{
    private static readonly Guid[] PreservedWindowIds =
    [
        DevelopmentPilotIds.ProcessOeePilotWindowCalculatedId,
        DevelopmentPilotIds.ProcessOeePilotWindowInsufficientId,
        DevelopmentPilotIds.ProcessOeePilotWindowReviewRequiredId,
    ];

    private static readonly Guid[] PreservedAssessmentIds =
    [
        DevelopmentPilotIds.ProcessOeePilotAssessmentCalculatedId,
        DevelopmentPilotIds.ProcessOeePilotAssessmentInsufficientId,
        DevelopmentPilotIds.ProcessOeePilotAssessmentReviewRequiredId,
    ];

    private static readonly string[] AttributableWindowKeyPrefixes =
    [
        "POEE1-CLOSURE",
        "POEE2-CLOSURE",
        "POEE3-CLOSURE",
        "poe1-calc-",
    ];

    private static readonly string[] AttributableAssessmentKeyPrefixes =
    [
        "POEE1-CLOSURE",
        "POEE2-CLOSURE",
        "POEE3-CLOSURE",
        "poe1-calc-",
    ];

    internal sealed record ProcessOeeFixtureHygienePreview(
        int WindowCount,
        int AssessmentCount,
        int LossDriverCount,
        int StabilityAssessmentCount,
        int StabilityDriverCount,
        int DowntimeRowCount,
        int ShiftComparisonBatchCount,
        int ShiftComparisonRowCount);

    internal sealed record ProcessOeeFixtureHygieneResult(
        ProcessOeeFixtureHygienePreview Preview,
        ProcessOeeFixtureHygienePreview Removed,
        int RemainingClosureWindows,
        int RemainingClosureAssessments);

    internal static async Task<ProcessOeeFixtureHygieneResult> CleanAttributableVerifierFixturesAsync(
        NevaxisZentraDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var preview = await PreviewAsync(dbContext, cancellationToken);
        if (preview.AssessmentCount == 0 && preview.WindowCount == 0)
        {
            return new ProcessOeeFixtureHygieneResult(
                preview,
                new ProcessOeeFixtureHygienePreview(0, 0, 0, 0, 0, 0, 0, 0),
                await CountRemainingClosureWindowsAsync(dbContext, cancellationToken),
                await CountRemainingClosureAssessmentsAsync(dbContext, cancellationToken));
        }

        var assessmentIds = await ResolveRemovableAssessmentIdsAsync(dbContext, cancellationToken);
        var windowIds = await ResolveRemovableWindowIdsAsync(dbContext, cancellationToken);

        await DeleteShiftComparisonRowsAsync(dbContext, assessmentIds, windowIds, cancellationToken);
        await DeleteDowntimeRowsAsync(dbContext, assessmentIds, cancellationToken);
        await DeleteStabilityDriversAsync(dbContext, assessmentIds, cancellationToken);
        await DeleteStabilityAssessmentsAsync(dbContext, assessmentIds, cancellationToken);
        await DeleteLossDriversAsync(dbContext, assessmentIds, cancellationToken);

        if (assessmentIds.Count > 0)
        {
            await dbContext.ProcessOeeAssessments
                .Where(a => assessmentIds.Contains(a.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (windowIds.Count > 0)
        {
            await dbContext.ProcessOeeEvaluationWindows
                .Where(w => windowIds.Contains(w.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        var removed = new ProcessOeeFixtureHygienePreview(
            preview.WindowCount,
            preview.AssessmentCount,
            preview.LossDriverCount,
            preview.StabilityAssessmentCount,
            preview.StabilityDriverCount,
            preview.DowntimeRowCount,
            preview.ShiftComparisonBatchCount,
            preview.ShiftComparisonRowCount);

        return new ProcessOeeFixtureHygieneResult(
            preview,
            removed,
            await CountRemainingClosureWindowsAsync(dbContext, cancellationToken),
            await CountRemainingClosureAssessmentsAsync(dbContext, cancellationToken));
    }

    private static async Task<ProcessOeeFixtureHygienePreview> PreviewAsync(
        NevaxisZentraDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var assessmentIds = await ResolveRemovableAssessmentIdsAsync(dbContext, cancellationToken);
        var windowIds = await ResolveRemovableWindowIdsAsync(dbContext, cancellationToken);

        var lossDriverCount = assessmentIds.Count == 0
            ? 0
            : await dbContext.ProcessOeeLossDrivers
                .Where(d => assessmentIds.Contains(d.AssessmentId))
                .CountAsync(cancellationToken);

        var stabilityAssessmentCount = assessmentIds.Count == 0
            ? 0
            : await dbContext.ProcessOeeStabilityAssessments
                .Where(s => assessmentIds.Contains(s.AssessmentId))
                .CountAsync(cancellationToken);

        var stabilityDriverCount = assessmentIds.Count == 0
            ? 0
            : await dbContext.ProcessOeeStabilityDrivers
                .Where(d => assessmentIds.Contains(d.AssessmentId))
                .CountAsync(cancellationToken);

        var downtimeRowCount = assessmentIds.Count == 0
            ? 0
            : await dbContext.ProcessOeeDowntimeBreakdownRows
                .Where(d => assessmentIds.Contains(d.AssessmentId))
                .CountAsync(cancellationToken);

        var batchIds = await ResolveRemovableShiftComparisonBatchIdsAsync(
            dbContext, assessmentIds, windowIds, cancellationToken);

        var shiftComparisonRowCount = batchIds.Count == 0
            ? 0
            : await dbContext.ProcessOeeShiftComparisonRows
                .Where(r => batchIds.Contains(r.BatchId))
                .CountAsync(cancellationToken);

        return new ProcessOeeFixtureHygienePreview(
            windowIds.Count,
            assessmentIds.Count,
            lossDriverCount,
            stabilityAssessmentCount,
            stabilityDriverCount,
            downtimeRowCount,
            batchIds.Count,
            shiftComparisonRowCount);
    }

    private static async Task<List<Guid>> ResolveRemovableWindowIdsAsync(
        NevaxisZentraDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var windows = await dbContext.ProcessOeeEvaluationWindows
            .AsNoTracking()
            .Select(w => new { w.Id, w.CreateIdempotencyKey })
            .ToListAsync(cancellationToken);

        return windows
            .Where(w => !PreservedWindowIds.Contains(w.Id)
                        && MatchesAnyPrefix(w.CreateIdempotencyKey, AttributableWindowKeyPrefixes))
            .Select(w => w.Id)
            .ToList();
    }

    private static async Task<List<Guid>> ResolveRemovableAssessmentIdsAsync(
        NevaxisZentraDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var assessments = await dbContext.ProcessOeeAssessments
            .AsNoTracking()
            .Select(a => new { a.Id, a.IdempotencyKey, a.InputFingerprint, a.EvaluationWindowId })
            .ToListAsync(cancellationToken);

        var removableWindowIds = await ResolveRemovableWindowIdsAsync(dbContext, cancellationToken);
        var removableWindowIdSet = removableWindowIds.ToHashSet();

        return assessments
            .Where(a => !PreservedAssessmentIds.Contains(a.Id)
                        && (MatchesAnyPrefix(a.IdempotencyKey, AttributableAssessmentKeyPrefixes)
                            || MatchesAnyPrefix(a.InputFingerprint, AttributableAssessmentKeyPrefixes)
                            || removableWindowIdSet.Contains(a.EvaluationWindowId)))
            .Select(a => a.Id)
            .Distinct()
            .ToList();
    }

    private static async Task<List<Guid>> ResolveRemovableShiftComparisonBatchIdsAsync(
        NevaxisZentraDbContext dbContext,
        List<Guid> assessmentIds,
        List<Guid> windowIds,
        CancellationToken cancellationToken)
    {
        var batches = await dbContext.ProcessOeeShiftComparisonBatches
            .AsNoTracking()
            .Select(b => new { b.Id, b.IdempotencyKey })
            .ToListAsync(cancellationToken);

        return batches
            .Where(b => MatchesAnyPrefix(b.IdempotencyKey, AttributableAssessmentKeyPrefixes))
            .Select(b => b.Id)
            .ToList();
    }

    private static async Task DeleteShiftComparisonRowsAsync(
        NevaxisZentraDbContext dbContext,
        List<Guid> assessmentIds,
        List<Guid> windowIds,
        CancellationToken cancellationToken)
    {
        var batchIds = await ResolveRemovableShiftComparisonBatchIdsAsync(
            dbContext, assessmentIds, windowIds, cancellationToken);
        if (batchIds.Count == 0)
        {
            return;
        }

        await dbContext.ProcessOeeShiftComparisonRows
            .Where(r => batchIds.Contains(r.BatchId))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.ProcessOeeShiftComparisonBatches
            .Where(b => batchIds.Contains(b.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static async Task DeleteDowntimeRowsAsync(
        NevaxisZentraDbContext dbContext,
        List<Guid> assessmentIds,
        CancellationToken cancellationToken)
    {
        if (assessmentIds.Count == 0)
        {
            return;
        }

        await dbContext.ProcessOeeDowntimeBreakdownRows
            .Where(d => assessmentIds.Contains(d.AssessmentId))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static async Task DeleteStabilityDriversAsync(
        NevaxisZentraDbContext dbContext,
        List<Guid> assessmentIds,
        CancellationToken cancellationToken)
    {
        if (assessmentIds.Count == 0)
        {
            return;
        }

        await dbContext.ProcessOeeStabilityDrivers
            .Where(d => assessmentIds.Contains(d.AssessmentId))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static async Task DeleteStabilityAssessmentsAsync(
        NevaxisZentraDbContext dbContext,
        List<Guid> assessmentIds,
        CancellationToken cancellationToken)
    {
        if (assessmentIds.Count == 0)
        {
            return;
        }

        await dbContext.ProcessOeeStabilityAssessments
            .Where(s => assessmentIds.Contains(s.AssessmentId))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static async Task DeleteLossDriversAsync(
        NevaxisZentraDbContext dbContext,
        List<Guid> assessmentIds,
        CancellationToken cancellationToken)
    {
        if (assessmentIds.Count == 0)
        {
            return;
        }

        await dbContext.ProcessOeeLossDrivers
            .Where(d => assessmentIds.Contains(d.AssessmentId))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static async Task<int> CountRemainingClosureWindowsAsync(
        NevaxisZentraDbContext dbContext,
        CancellationToken cancellationToken) =>
        await dbContext.ProcessOeeEvaluationWindows
            .AsNoTracking()
            .CountAsync(
                w => w.CreateIdempotencyKey != null
                     && (w.CreateIdempotencyKey.StartsWith("POEE1-CLOSURE")
                         || w.CreateIdempotencyKey.StartsWith("POEE2-CLOSURE")
                         || w.CreateIdempotencyKey.StartsWith("POEE3-CLOSURE")),
                cancellationToken);

    private static async Task<int> CountRemainingClosureAssessmentsAsync(
        NevaxisZentraDbContext dbContext,
        CancellationToken cancellationToken) =>
        await dbContext.ProcessOeeAssessments
            .AsNoTracking()
            .CountAsync(
                a => a.IdempotencyKey != null
                     && (a.IdempotencyKey.StartsWith("POEE1-CLOSURE")
                         || a.IdempotencyKey.StartsWith("POEE2-CLOSURE")
                         || a.IdempotencyKey.StartsWith("POEE3-CLOSURE")),
                cancellationToken);

    private static bool MatchesAnyPrefix(string? value, IEnumerable<string> prefixes)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (var prefix in prefixes)
        {
            if (value.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
