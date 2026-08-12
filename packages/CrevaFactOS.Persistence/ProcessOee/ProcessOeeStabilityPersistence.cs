using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.ProcessOee;

internal static class ProcessOeeStabilityPersistence
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task EnsureStabilityPersistedAsync(
        CrevaFactOSDbContext dbContext,
        ProcessOeeOperationContext context,
        ProcessOeeEvaluationWindow window,
        ProcessOeeEvidenceBundle evidence,
        ProcessOeeCalculationResult calculation,
        ProcessOeeAssessment assessment,
        ProcessOeeStabilityDataCollector stabilityDataCollector,
        CancellationToken cancellationToken)
    {
        var hasStability = await dbContext.ProcessOeeStabilityAssessments.AsNoTracking()
            .AnyAsync(s => s.TenantId == context.TenantId && s.AssessmentId == assessment.Id, cancellationToken);
        if (hasStability)
        {
            return;
        }

        var stabilityData = await stabilityDataCollector.CollectAsync(context, window, evidence, cancellationToken);
        var analysis = ProcessOeeStabilityCalculator.Analyze(window, evidence, calculation, stabilityData);
        PersistStability(context, assessment, analysis, context.IdentitySubjectId.ToString(), dbContext);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public static void PersistStability(
        ProcessOeeOperationContext context,
        ProcessOeeAssessment assessment,
        ProcessOeeStabilityAnalysisResult analysis,
        string actor,
        CrevaFactOSDbContext dbContext)
    {
        var now = DateTimeOffset.UtcNow;
        var stabilityAssessment = new ProcessOeeStabilityAssessment
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            AssessmentId = assessment.Id,
            ResultState = analysis.ResultState,
            MedianCycleMs = analysis.MedianCycleMs,
            MadCycleMs = analysis.MadCycleMs,
            SampleCount = analysis.SampleCount,
            DriftDirection = analysis.DriftDirection,
            MicroStopCount = analysis.MicroStopCount,
            MicroStopTotalSeconds = analysis.MicroStopTotalSeconds,
            MicroStopMedianSeconds = analysis.MicroStopMedianSeconds,
            ClassifiedDowntimeSeconds = analysis.ClassifiedDowntimeSeconds,
            UnclassifiedDowntimeSeconds = analysis.UnclassifiedDowntimeSeconds,
            EvidenceFingerprint = analysis.EvidenceFingerprint,
            RuleSetVersion = analysis.RuleSetVersion,
            CreatedAtUtc = now,
            CreatedBySubjectId = actor,
        };

        dbContext.ProcessOeeStabilityAssessments.Add(stabilityAssessment);

        foreach (var draft in analysis.Drivers)
        {
            dbContext.ProcessOeeStabilityDrivers.Add(new ProcessOeeStabilityDriver
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                AssessmentId = assessment.Id,
                StabilityAssessmentId = stabilityAssessment.Id,
                ReasonCode = draft.ReasonCode,
                Category = draft.Category,
                ResultState = draft.ResultState,
                Severity = draft.Severity,
                OperationalPriority = draft.OperationalPriority,
                ObservedValue = draft.ObservedValue,
                ReferenceValue = draft.ReferenceValue,
                Unit = draft.Unit,
                SampleCount = draft.SampleCount,
                TimeRangeStartUtc = draft.TimeRangeStartUtc,
                TimeRangeEndUtc = draft.TimeRangeEndUtc,
                SourceReferencesJson = JsonSerializer.Serialize(draft.SourceReferences, JsonOptions),
                FreshnessState = draft.FreshnessState,
                ExplanationCode = draft.ExplanationCode,
                DataLimitationsJson = JsonSerializer.Serialize(draft.DataLimitations, JsonOptions),
                RuleSetVersion = analysis.RuleSetVersion,
                CreatedAtUtc = now,
                CreatedBySubjectId = actor,
            });
        }

        foreach (var row in analysis.BreakdownRows)
        {
            dbContext.ProcessOeeDowntimeBreakdownRows.Add(new ProcessOeeDowntimeBreakdownRow
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                AssessmentId = assessment.Id,
                StabilityAssessmentId = stabilityAssessment.Id,
                ReasonCode = row.ReasonCode,
                DurationSeconds = row.DurationSeconds,
                OccurrenceCount = row.OccurrenceCount,
                SortRank = row.SortRank,
                IsClassified = row.IsClassified,
                CreatedAtUtc = now,
                CreatedBySubjectId = actor,
            });
        }
    }

    public static ProcessOeeStabilityDriverReadModel MapStabilityDriver(ProcessOeeStabilityDriver driver)
    {
        var sourceReferences = JsonSerializer.Deserialize<List<string>>(driver.SourceReferencesJson, JsonOptions) ?? [];
        var limitations = JsonSerializer.Deserialize<List<string>>(driver.DataLimitationsJson, JsonOptions) ?? [];
        return new ProcessOeeStabilityDriverReadModel(
            driver.Id,
            driver.ReasonCode,
            driver.Category,
            driver.ResultState,
            driver.Severity,
            driver.OperationalPriority,
            driver.ObservedValue,
            driver.ReferenceValue,
            driver.Unit,
            driver.SampleCount,
            driver.TimeRangeStartUtc,
            driver.TimeRangeEndUtc,
            sourceReferences,
            driver.FreshnessState,
            driver.ExplanationCode,
            limitations,
            driver.RuleSetVersion);
    }

    public static ProcessOeeDowntimeBreakdownReadModel MapBreakdownRow(
        ProcessOeeDowntimeBreakdownRow row,
        int totalDowntimeSeconds)
    {
        decimal? percent = totalDowntimeSeconds > 0
            ? Math.Round((decimal)row.DurationSeconds / totalDowntimeSeconds * 100m, 2)
            : null;
        return new ProcessOeeDowntimeBreakdownReadModel(
            row.ReasonCode,
            row.DurationSeconds,
            row.OccurrenceCount,
            row.SortRank,
            row.IsClassified,
            percent);
    }
}
