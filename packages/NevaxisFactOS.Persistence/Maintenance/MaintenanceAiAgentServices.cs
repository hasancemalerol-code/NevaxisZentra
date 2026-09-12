using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Maintenance;

internal static partial class MaintenanceAiInputBuilder
{
    internal static async Task<(MaintenanceAiInputSnapshot? Snapshot, string? ErrorCode)> BuildAsync(
        NevaxisFactOSDbContext dbContext,
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken)
    {
        var mapping = await dbContext.MaintenanceAssetMachineMappings.AsNoTracking()
            .SingleOrDefaultAsync(
                m => m.FacilityId == context.FacilityId && m.AssetReference == assetReference,
                cancellationToken);

        if (mapping is null)
        {
            return (null, MaintenanceAiFailureCodes.NotFound);
        }

        var profile = await dbContext.MaintenanceConditionProfiles.AsNoTracking()
            .SingleOrDefaultAsync(
                p => p.FacilityId == context.FacilityId && p.AssetReference == assetReference,
                cancellationToken);

        var assessment = await dbContext.MaintenancePredictiveRiskAssessments.AsNoTracking()
            .Include(a => a.Drivers)
            .Include(a => a.EvidenceItems)
            .SingleOrDefaultAsync(
                a => a.FacilityId == context.FacilityId
                    && a.AssetReference == assetReference
                    && a.IsCurrent,
                cancellationToken);

        if (assessment is null)
        {
            return (null, MaintenanceAiFailureCodes.MissingRiskAssessment);
        }

        var recommendation = await dbContext.MaintenancePredictiveRecommendations.AsNoTracking()
            .SingleOrDefaultAsync(
                r => r.FacilityId == context.FacilityId
                    && r.AssetReference == assetReference
                    && r.AssessmentId == assessment.Id
                    && r.Status == MaintenancePredictiveRecommendationStatuses.Current,
                cancellationToken);

        var openAnomalies = await dbContext.MaintenanceConditionAnomalies.AsNoTracking()
            .Where(a => a.FacilityId == context.FacilityId
                && a.AssetReference == assetReference
                && a.Status == MaintenanceAnomalyStatuses.Open)
            .OrderByDescending(a => a.DetectedAtUtc)
            .Take(10)
            .ToListAsync(cancellationToken);

        var openRequests = await dbContext.MaintenanceRequests.AsNoTracking()
            .Where(r => r.FacilityId == context.FacilityId
                && r.AssetReference == assetReference
                && r.Status == MaintenanceRequestStatus.Open)
            .OrderByDescending(r => r.ReportedAtUtc)
            .Take(5)
            .ToListAsync(cancellationToken);

        var alarmCount = await dbContext.MachineAlarmObservations.AsNoTracking()
            .CountAsync(
                a => a.MachineId == mapping.MachineId
                    && a.SourceTimestampUtc >= DateTimeOffset.UtcNow.AddHours(-24),
                cancellationToken);

        var downtimeEpisodes = await dbContext.MachineDowntimeEpisodes.AsNoTracking()
            .Where(d => d.MachineId == mapping.MachineId
                && d.StartedAtSourceUtc >= DateTimeOffset.UtcNow.AddHours(-24))
            .ToListAsync(cancellationToken);

        var failureCount = await dbContext.MaintenanceRequests.AsNoTracking()
            .CountAsync(
                r => r.FacilityId == context.FacilityId
                    && r.AssetReference == assetReference
                    && r.Type == MaintenanceRequestType.Fault
                    && r.ReportedAtUtc >= DateTimeOffset.UtcNow.AddDays(-90),
                cancellationToken);

        var snapshot = new MaintenanceAiInputSnapshot(
            context.TenantId,
            context.FacilityId,
            assetReference,
            mapping.AssetName,
            mapping.AssetType,
            mapping.AssetId,
            profile?.ConditionStatus ?? MaintenanceConditionStatuses.Unknown,
            profile?.EvaluationIdentity ?? assessment.ConditionEvaluationIdentity ?? string.Empty,
            assessment.Id,
            assessment.RiskBand,
            recommendation?.Category ?? MaintenancePredictiveRecommendationCategories.ReviewDataQuality,
            profile?.DataFreshness ?? assessment.DataFreshness,
            profile?.DataSufficiency ?? assessment.DataSufficiency,
            profile?.ActiveAnomalyCount ?? assessment.ActiveAnomalyCount,
            assessment.RuleSetVersion,
            assessment.AssessedAtUtc,
            profile?.LastEvaluatedAtUtc,
            assessment.Drivers
                .OrderBy(d => d.SortOrder)
                .Select(d => new MaintenanceAiRiskDriverInput(d.DriverCode, d.Summary))
                .ToList(),
            assessment.EvidenceItems
                .OrderBy(e => e.SortOrder)
                .Select(e => new MaintenanceAiEvidenceReferenceInput(e.EvidenceType, e.EvidenceReference, e.Summary))
                .ToList(),
            openAnomalies
                .Select(a => new MaintenanceAiAnomalySummaryInput(
                    a.MetricKey,
                    a.Severity,
                    a.EvidenceSummary,
                    a.EvaluationIdentity))
                .ToList(),
            alarmCount,
            downtimeEpisodes.Count,
            downtimeEpisodes.Sum(d => d.DurationMs.HasValue
                ? (decimal)d.DurationMs.Value / 60000m
                : d.EndedAtSourceUtc.HasValue
                    ? (decimal)(d.EndedAtSourceUtc.Value - d.StartedAtSourceUtc).TotalMinutes
                    : 0m),
            failureCount,
            openRequests
                .Select(r => new MaintenanceAiOpenRequestSummaryInput(
                    r.Id,
                    r.RequestNumber,
                    r.Title,
                    MaintenanceValueConversions.ToStorage(r.Status)))
                .ToList());

        return (snapshot, null);
    }

    internal static string ComputeFingerprint(MaintenanceAiInputSnapshot snapshot) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            snapshot.ConditionEvaluationReference,
            snapshot.RiskAssessmentId,
            snapshot.RuleSetVersion,
            snapshot.RiskBand,
            snapshot.DataFreshness,
            snapshot.DataSufficiency,
            Evidence = snapshot.EvidenceReferences.Select(e => $"{e.EvidenceType}:{e.EvidenceReference}"),
        })))).ToLowerInvariant();
}

internal static partial class MaintenanceAiOutputValidator
{
    [GeneratedRegex(@"(?i)(failure\s*probability|arıza\s*olasılığı|\d+\s*%)", RegexOptions.CultureInvariant)]
    private static partial Regex FailureProbabilityPattern();

    [GeneratedRegex(@"(?i)(remaining\s*useful\s*life|\bRUL\b|kalan\s*ömür)", RegexOptions.CultureInvariant)]
    private static partial Regex RulPattern();

    [GeneratedRegex(@"(?i)(\d+\s*(gün|day)s?\s*(içinde|to\s*failure|kaldı))", RegexOptions.CultureInvariant)]
    private static partial Regex DaysToFailurePattern();

    [GeneratedRegex(@"(?i)(kesin\s*arıza\s*tarihi|exact\s*failure\s*date|\d{4}-\d{2}-\d{2}.*arıza)", RegexOptions.CultureInvariant)]
    private static partial Regex ExactFailureDatePattern();

    [GeneratedRegex(@"(?i)(machine\s*command|makine\s*komutu|write\s*tag|plc\s*write)", RegexOptions.CultureInvariant)]
    private static partial Regex MachineCommandPattern();

    internal static (bool Valid, string? FailureCode, string? Reason) Validate(
        MaintenanceAiStructuredOutput output,
        MaintenanceAiInputSnapshot input)
    {
        if (string.IsNullOrWhiteSpace(output.Summary)
            || string.IsNullOrWhiteSpace(output.SuggestedTitle)
            || string.IsNullOrWhiteSpace(output.SuggestedDescription)
            || string.IsNullOrWhiteSpace(output.SuggestedPriority)
            || string.IsNullOrWhiteSpace(output.SuggestedRequestType)
            || string.IsNullOrWhiteSpace(output.QualitativeConfidence)
            || !output.ProhibitedActionConfirmation)
        {
            return (false, MaintenanceAiFailureCodes.InvalidOutput, "Required structured output fields are missing.");
        }

        if (!IsAllowedConfidence(output.QualitativeConfidence))
        {
            return (false, MaintenanceAiFailureCodes.InvalidOutput, "Qualitative confidence is invalid.");
        }

        if (!IsAllowedRequestType(output.SuggestedRequestType))
        {
            return (false, MaintenanceAiFailureCodes.InvalidOutput, "Suggested request type is not allowed.");
        }

        var combined = string.Join('\n', new[]
        {
            output.Summary,
            output.ObservedCondition,
            output.SuggestedDescription,
            output.UncertaintyExplanation,
        });

        if (FailureProbabilityPattern().IsMatch(combined))
        {
            return (false, MaintenanceAiFailureCodes.InvalidOutput, "Unsupported failure probability language.");
        }

        if (RulPattern().IsMatch(combined))
        {
            return (false, MaintenanceAiFailureCodes.InvalidOutput, "Unsupported RUL language.");
        }

        if (DaysToFailurePattern().IsMatch(combined))
        {
            return (false, MaintenanceAiFailureCodes.InvalidOutput, "Unsupported days-to-failure language.");
        }

        if (ExactFailureDatePattern().IsMatch(combined))
        {
            return (false, MaintenanceAiFailureCodes.InvalidOutput, "Unsupported exact failure date language.");
        }

        if (MachineCommandPattern().IsMatch(combined))
        {
            return (false, MaintenanceAiFailureCodes.InvalidOutput, "Machine command language is prohibited.");
        }

        var allowedRefs = input.EvidenceReferences
            .Select(e => $"{e.EvidenceType}:{e.EvidenceReference}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var reference in output.EvidenceReferences)
        {
            if (!allowedRefs.Contains($"{reference.EvidenceType}:{reference.EvidenceReference}"))
            {
                return (false, MaintenanceAiFailureCodes.InvalidOutput, "Evidence reference is not in trusted input snapshot.");
            }
        }

        return (true, null, null);
    }

    private static bool IsAllowedConfidence(string value) =>
        value is MaintenanceAiQualitativeConfidenceLevels.Low
            or MaintenanceAiQualitativeConfidenceLevels.Medium
            or MaintenanceAiQualitativeConfidenceLevels.High;

    private static bool IsAllowedRequestType(string value) =>
        value is "fault" or "corrective" or "preventive" or "inspection" or "other";
}
