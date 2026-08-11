using CrevaFactOS.Persistence.Edge;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Maintenance;

public sealed class MaintenancePredictiveRiskRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IPredictiveRiskEvaluationService predictiveRiskEvaluationService) : IMaintenancePredictiveRiskRepository
{
    public Task<MaintenancePredictiveRiskOverviewReadModel?> GetOverviewAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync<MaintenancePredictiveRiskOverviewReadModel?>(async ct =>
        {
            var current = await dbContext.MaintenancePredictiveRiskAssessments
                .AsNoTracking()
                .Where(a => a.FacilityId == context.FacilityId && a.IsCurrent)
                .ToListAsync(ct);

            return new MaintenancePredictiveRiskOverviewReadModel(
                current.Count,
                current.Count(a => a.RiskBand == MaintenancePredictiveRiskBands.InsufficientData),
                current.Count(a => a.RiskBand == MaintenancePredictiveRiskBands.Low),
                current.Count(a => a.RiskBand == MaintenancePredictiveRiskBands.Watch),
                current.Count(a => a.RiskBand == MaintenancePredictiveRiskBands.High),
                current.Count(a => a.RiskBand == MaintenancePredictiveRiskBands.Critical),
                current.MaxBy(a => a.AssessedAtUtc)?.AssessedAtUtc);
        }, cancellationToken);

    public Task<IReadOnlyList<MaintenancePredictiveRiskAssetRowReadModel>> ListAssetRowsAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var mappings = await dbContext.MaintenanceAssetMachineMappings
                .AsNoTracking()
                .Where(m => m.FacilityId == context.FacilityId)
                .OrderBy(m => m.AssetReference)
                .ToListAsync(ct);

            var currentAssessments = await dbContext.MaintenancePredictiveRiskAssessments
                .AsNoTracking()
                .Where(a => a.FacilityId == context.FacilityId && a.IsCurrent)
                .ToDictionaryAsync(a => a.AssetReference, StringComparer.OrdinalIgnoreCase, ct);

            var machineIds = mappings.Select(m => m.MachineId).Distinct().ToList();
            var machines = await dbContext.MachineRegistries
                .AsNoTracking()
                .Where(m => m.FacilityId == context.FacilityId && machineIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, ct);

            return (IReadOnlyList<MaintenancePredictiveRiskAssetRowReadModel>)mappings
                .Select(mapping =>
                {
                    machines.TryGetValue(mapping.MachineId, out var machine);
                    currentAssessments.TryGetValue(mapping.AssetReference, out var assessment);
                    return new MaintenancePredictiveRiskAssetRowReadModel(
                        mapping.AssetReference,
                        mapping.AssetName,
                        machine?.MachineCode,
                        machine?.Name,
                        assessment?.RiskBand ?? MaintenancePredictiveRiskBands.InsufficientData,
                        assessment?.ReviewPriority ?? MaintenancePredictiveReviewPriorities.Routine,
                        assessment?.DataFreshness ?? MaintenanceConditionDataFreshnessLevels.Unknown,
                        assessment?.DataSufficiency ?? MaintenanceConditionDataSufficiencyLevels.Insufficient,
                        assessment?.AssessedAtUtc);
                })
                .ToList();
        }, cancellationToken);

    public Task<MaintenancePredictiveRiskAssessmentReadModel?> GetCurrentAssessmentAsync(
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var assessment = await dbContext.MaintenancePredictiveRiskAssessments
                .AsNoTracking()
                .Include(a => a.Drivers)
                .Include(a => a.EvidenceItems)
                .Where(a => a.FacilityId == context.FacilityId &&
                            a.AssetReference == assetReference &&
                            a.IsCurrent)
                .FirstOrDefaultAsync(ct);

            return assessment is null ? null : await MapAssessmentReadModelAsync(context.FacilityId, assessment, ct);
        }, cancellationToken);

    public Task<IReadOnlyList<MaintenancePredictiveRiskAssessmentSummaryReadModel>> ListAssessmentHistoryAsync(
        MaintenanceOperationContext context,
        string assetReference,
        int limit,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var normalizedLimit = Math.Clamp(limit, 1, 100);
            var assessments = await dbContext.MaintenancePredictiveRiskAssessments
                .AsNoTracking()
                .Where(a => a.FacilityId == context.FacilityId && a.AssetReference == assetReference)
                .OrderByDescending(a => a.AssessedAtUtc)
                .Take(normalizedLimit)
                .ToListAsync(ct);

            return (IReadOnlyList<MaintenancePredictiveRiskAssessmentSummaryReadModel>)assessments
                .Select(a => new MaintenancePredictiveRiskAssessmentSummaryReadModel(
                    a.Id,
                    a.RiskBand,
                    a.ReviewPriority,
                    a.AssessedAtUtc,
                    a.RuleSetVersion))
                .ToList();
        }, cancellationToken);

    public Task<bool> EvaluateFacilityRiskAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            var profiles = await dbContext.MaintenanceConditionProfiles
                .AsNoTracking()
                .Where(p => p.FacilityId == context.FacilityId)
                .ToListAsync(ct);

            if (profiles.Count == 0)
            {
                return false;
            }

            var mappings = await dbContext.MaintenanceAssetMachineMappings
                .AsNoTracking()
                .Where(m => m.FacilityId == context.FacilityId)
                .ToDictionaryAsync(m => m.AssetReference, StringComparer.OrdinalIgnoreCase, ct);

            var assessedAtUtc = DateTimeOffset.UtcNow;
            var evaluatedAny = false;

            foreach (var profile in profiles)
            {
                if (!mappings.TryGetValue(profile.AssetReference, out var mapping))
                {
                    continue;
                }

                var evaluationIdentity = profile.EvaluationIdentity ?? string.Empty;
                if (string.IsNullOrWhiteSpace(evaluationIdentity))
                {
                    evaluationIdentity = $"profile:{profile.Id:N}:{profile.Version}";
                }

                var existing = await dbContext.MaintenancePredictiveRiskAssessments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        a => a.FacilityId == context.FacilityId &&
                             a.AssetReference == profile.AssetReference &&
                             a.ConditionEvaluationIdentity == evaluationIdentity &&
                             a.RuleSetVersion == MaintenancePredictiveRiskRuleSetVersions.V1,
                        ct);

                if (existing is not null)
                {
                    continue;
                }

                var openAnomalies = await dbContext.MaintenanceConditionAnomalies
                    .AsNoTracking()
                    .Where(a => a.FacilityId == context.FacilityId &&
                                a.AssetReference == profile.AssetReference &&
                                a.Status == MaintenanceAnomalyStatuses.Open)
                    .OrderByDescending(a => a.DetectedAtUtc)
                    .Take(20)
                    .ToListAsync(ct);

                var maintenanceHistory = await LoadMaintenanceHistoryContextAsync(context, profile.AssetReference, ct);
                var alarmContext = await LoadAlarmContextAsync(context.FacilityId, profile.MachineId, ct);
                var downtimeContext = await LoadDowntimeContextAsync(context.FacilityId, profile.MachineId, ct);

                var evaluationInput = new PredictiveRiskEvaluationInput(
                    profile.AssetReference,
                    profile.MachineId,
                    mapping.AssetName,
                    null,
                    null,
                    profile.ConditionStatus,
                    profile.DataFreshness,
                    profile.DataSufficiency,
                    profile.TrendState,
                    profile.ActiveAnomalyCount,
                    profile.HighestAnomalySeverity,
                    evaluationIdentity,
                    profile.LastEvaluatedAtUtc,
                    openAnomalies.Select(a => new PredictiveRiskAnomalyInput(
                        a.MetricKey,
                        a.Severity,
                        a.EvidenceSummary,
                        a.DetectedAtUtc,
                        a.EvaluationIdentity)).ToList(),
                    alarmContext,
                    downtimeContext,
                    maintenanceHistory,
                    assessedAtUtc);

                var evaluationResult = predictiveRiskEvaluationService.EvaluateAsset(evaluationInput);
                await PersistAssessmentAsync(
                    context,
                    profile,
                    mapping,
                    evaluationIdentity,
                    evaluationResult,
                    assessedAtUtc,
                    ct);
                evaluatedAny = true;
            }

            await dbContext.SaveChangesAsync(ct);
            return evaluatedAny;
        }, cancellationToken);

    private async Task PersistAssessmentAsync(
        MaintenanceOperationContext context,
        MaintenanceConditionProfile profile,
        MaintenanceAssetMachineMapping mapping,
        string evaluationIdentity,
        PredictiveRiskEvaluationResult evaluationResult,
        DateTimeOffset assessedAtUtc,
        CancellationToken cancellationToken)
    {
        var previousCurrent = await dbContext.MaintenancePredictiveRiskAssessments
            .Where(a => a.FacilityId == context.FacilityId &&
                        a.AssetReference == profile.AssetReference &&
                        a.IsCurrent)
            .ToListAsync(cancellationToken);

        foreach (var previous in previousCurrent)
        {
            previous.IsCurrent = false;
        }

        var previousRecommendations = await dbContext.MaintenancePredictiveRecommendations
            .Where(r => r.FacilityId == context.FacilityId &&
                        r.AssetReference == profile.AssetReference &&
                        r.Status == MaintenancePredictiveRecommendationStatuses.Current)
            .ToListAsync(cancellationToken);

        foreach (var recommendation in previousRecommendations)
        {
            recommendation.Status = MaintenancePredictiveRecommendationStatuses.Superseded;
        }

        var assessment = new MaintenancePredictiveRiskAssessment
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            AssetReference = profile.AssetReference,
            MachineId = profile.MachineId,
            RiskBand = evaluationResult.RiskBand,
            ReviewPriority = evaluationResult.ReviewPriority,
            RuleSetVersion = MaintenancePredictiveRiskRuleSetVersions.V1,
            ConditionEvaluationIdentity = evaluationIdentity,
            DataFreshness = profile.DataFreshness,
            DataSufficiency = profile.DataSufficiency,
            ActiveAnomalyCount = profile.ActiveAnomalyCount,
            MaintenanceHistorySummary = evaluationResult.MaintenanceHistorySummary,
            Explanation = evaluationResult.Explanation,
            CreationSource = MaintenancePredictiveRiskCreationSources.DeterministicEvaluator,
            AssessedAtUtc = assessedAtUtc,
            IsCurrent = true,
            CreatedAtUtc = assessedAtUtc,
            Drivers = evaluationResult.Drivers
                .Select((driver, index) => new MaintenancePredictiveRiskDriver
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    DriverCode = driver.DriverCode,
                    Summary = driver.Summary,
                    SortOrder = driver.SortOrder == 0 ? index : driver.SortOrder,
                    CreatedAtUtc = assessedAtUtc,
                })
                .ToList(),
            EvidenceItems = evaluationResult.Evidence
                .Select((item, index) => new MaintenancePredictiveRiskEvidence
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    EvidenceType = item.EvidenceType,
                    EvidenceReference = item.EvidenceReference,
                    Summary = item.Summary,
                    SortOrder = item.SortOrder == 0 ? index : item.SortOrder,
                    CreatedAtUtc = assessedAtUtc,
                })
                .ToList(),
        };

        foreach (var driver in assessment.Drivers)
        {
            driver.AssessmentId = assessment.Id;
        }

        foreach (var evidence in assessment.EvidenceItems)
        {
            evidence.AssessmentId = assessment.Id;
        }

        dbContext.MaintenancePredictiveRiskAssessments.Add(assessment);

        var existingRecommendation = await dbContext.MaintenancePredictiveRecommendations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.FacilityId == context.FacilityId &&
                     r.AssessmentId == assessment.Id,
                cancellationToken);

        if (existingRecommendation is null)
        {
            dbContext.MaintenancePredictiveRecommendations.Add(new MaintenancePredictiveRecommendation
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                AssetReference = profile.AssetReference,
                AssessmentId = assessment.Id,
                Category = evaluationResult.RecommendationCategory,
                ReviewPriority = evaluationResult.ReviewPriority,
                Explanation = evaluationResult.RecommendationExplanation,
                RuleSetVersion = MaintenancePredictiveRiskRuleSetVersions.V1,
                Status = MaintenancePredictiveRecommendationStatuses.Current,
                GeneratedAtUtc = assessedAtUtc,
                CreatedAtUtc = assessedAtUtc,
            });
        }
    }

    private async Task<MaintenancePredictiveRiskAssessmentReadModel> MapAssessmentReadModelAsync(
        Guid facilityId,
        MaintenancePredictiveRiskAssessment assessment,
        CancellationToken cancellationToken)
    {
        var mapping = await dbContext.MaintenanceAssetMachineMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.FacilityId == facilityId && m.AssetReference == assessment.AssetReference,
                cancellationToken);

        var machine = mapping is null
            ? null
            : await dbContext.MachineRegistries
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.FacilityId == facilityId && m.Id == mapping.MachineId, cancellationToken);

        var recommendation = await dbContext.MaintenancePredictiveRecommendations
            .AsNoTracking()
            .Where(r => r.FacilityId == facilityId &&
                        r.AssessmentId == assessment.Id &&
                        r.Status == MaintenancePredictiveRecommendationStatuses.Current)
            .OrderByDescending(r => r.GeneratedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new MaintenancePredictiveRiskAssessmentReadModel(
            assessment.Id,
            assessment.AssetReference,
            mapping?.AssetName,
            machine?.MachineCode,
            machine?.Name,
            assessment.RiskBand,
            assessment.ReviewPriority,
            assessment.RuleSetVersion,
            assessment.ConditionEvaluationIdentity,
            assessment.DataFreshness,
            assessment.DataSufficiency,
            assessment.ActiveAnomalyCount,
            assessment.MaintenanceHistorySummary,
            assessment.Explanation,
            assessment.AssessedAtUtc,
            assessment.Drivers
                .OrderBy(d => d.SortOrder)
                .Select(d => new MaintenancePredictiveRiskDriverReadModel(d.DriverCode, d.Summary))
                .ToList(),
            assessment.EvidenceItems
                .OrderBy(e => e.SortOrder)
                .Select(e => new MaintenancePredictiveRiskEvidenceReadModel(e.EvidenceType, e.EvidenceReference, e.Summary))
                .ToList(),
            recommendation is null
                ? null
                : new MaintenancePredictiveRecommendationReadModel(
                    recommendation.Category,
                    recommendation.ReviewPriority,
                    recommendation.Explanation,
                    recommendation.RuleSetVersion,
                    recommendation.GeneratedAtUtc,
                    recommendation.Status));
    }

    private async Task<ConditionMaintenanceHistoryInput> LoadMaintenanceHistoryContextAsync(
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-90);
        var requests = await dbContext.MaintenanceRequests
            .AsNoTracking()
            .Where(r => r.FacilityId == context.FacilityId &&
                        r.AssetReference == assetReference &&
                        r.ReportedAtUtc >= since)
            .ToListAsync(cancellationToken);

        var completedWorkOrders = await dbContext.MaintenanceWorkOrders
            .AsNoTracking()
            .Where(w => w.FacilityId == context.FacilityId &&
                        w.AssetReference == assetReference &&
                        w.Status == MaintenanceWorkOrderStatus.Completed &&
                        w.CompletedAtUtc != null)
            .OrderByDescending(w => w.CompletedAtUtc)
            .Take(1)
            .ToListAsync(cancellationToken);

        var latestFailure = requests
            .Where(r => r.Type == MaintenanceRequestType.Fault)
            .OrderByDescending(r => r.ReportedAtUtc)
            .FirstOrDefault();

        return new ConditionMaintenanceHistoryInput(
            requests.Count(r => r.Type == MaintenanceRequestType.Fault),
            latestFailure?.FailureCategory.ToString(),
            completedWorkOrders.FirstOrDefault()?.CompletedAtUtc);
    }

    private async Task<ConditionAlarmContextInput> LoadAlarmContextAsync(
        Guid facilityId,
        Guid machineId,
        CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow.AddHours(-24);
        var alarms = await dbContext.MachineAlarmObservations
            .AsNoTracking()
            .Where(a => a.FacilityId == facilityId && a.MachineId == machineId)
            .OrderByDescending(a => a.SourceTimestampUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        var recent = alarms.Where(a => a.SourceTimestampUtc >= since).ToList();
        return new ConditionAlarmContextInput(
            recent.Count(a => string.Equals(a.EventKind, MachineEventKinds.AlarmRaised, StringComparison.OrdinalIgnoreCase)),
            recent.OrderByDescending(a => a.Severity).Select(a => a.Severity).FirstOrDefault());
    }

    private async Task<ConditionDowntimeContextInput> LoadDowntimeContextAsync(
        Guid facilityId,
        Guid machineId,
        CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow.AddHours(-24);
        var episodes = await dbContext.MachineDowntimeEpisodes
            .AsNoTracking()
            .Where(e => e.FacilityId == facilityId && e.MachineId == machineId)
            .OrderByDescending(e => e.StartedAtSourceUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        var recent = episodes.Where(e => e.StartedAtSourceUtc >= since).ToList();
        var totalMinutes = recent.Sum(e => e.DurationMs.HasValue ? (decimal)e.DurationMs.Value / 60000m : 0m);
        return new ConditionDowntimeContextInput(recent.Count, totalMinutes);
    }
}

public sealed class UnavailableMaintenancePredictiveRiskRepository : IMaintenancePredictiveRiskRepository
{
    public Task<MaintenancePredictiveRiskOverviewReadModel?> GetOverviewAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken) =>
        Task.FromResult<MaintenancePredictiveRiskOverviewReadModel?>(null);

    public Task<IReadOnlyList<MaintenancePredictiveRiskAssetRowReadModel>> ListAssetRowsAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MaintenancePredictiveRiskAssetRowReadModel>>([]);

    public Task<MaintenancePredictiveRiskAssessmentReadModel?> GetCurrentAssessmentAsync(
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken) =>
        Task.FromResult<MaintenancePredictiveRiskAssessmentReadModel?>(null);

    public Task<IReadOnlyList<MaintenancePredictiveRiskAssessmentSummaryReadModel>> ListAssessmentHistoryAsync(
        MaintenanceOperationContext context,
        string assetReference,
        int limit,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MaintenancePredictiveRiskAssessmentSummaryReadModel>>([]);

    public Task<bool> EvaluateFacilityRiskAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken) =>
        Task.FromResult(false);
}
