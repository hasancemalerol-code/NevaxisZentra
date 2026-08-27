using CrevaFactOS.Persistence.Development;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CrevaFactOS.Persistence.CommercialIntelligence;

internal static class CommercialProfitabilityComparisonOperations
{
    internal static async Task<IReadOnlyList<CommercialProfitabilityComparisonSummaryReadModel>> ListComparisonsAsync(
        CrevaFactOSDbContext dbContext,
        CommercialIntelligenceOperationContext context,
        CommercialProfitabilityComparisonListFilters filters,
        CancellationToken cancellationToken)
    {
        var page = filters.Page < 1 ? 1 : filters.Page;
        var pageSize = filters.PageSize is < 1 or > 100 ? 25 : filters.PageSize;

        var query = dbContext.CommercialProfitabilityComparisonRuns.AsNoTracking()
            .Include(r => r.Profile)
            .Where(r => r.TenantId == context.TenantId && r.FacilityId == context.FacilityId);

        if (!string.IsNullOrWhiteSpace(filters.ComparisonType))
        {
            query = query.Where(r => r.ComparisonType == filters.ComparisonType.Trim());
        }

        if (filters.ProfileId.HasValue)
        {
            query = query.Where(r => r.ProfileId == filters.ProfileId.Value);
        }

        if (filters.MaterialId.HasValue)
        {
            query = query.Where(r => r.MaterialId == filters.MaterialId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.Outcome))
        {
            query = query.Where(r => r.ComparisonOutcome == filters.Outcome.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filters.EvidenceStatus))
        {
            query = query.Where(r => r.EvidenceStatus == filters.EvidenceStatus.Trim());
        }

        if (filters.WindowFromUtc.HasValue)
        {
            query = query.Where(r => r.CurrentWindowEndUtc >= filters.WindowFromUtc.Value);
        }

        if (filters.WindowToUtc.HasValue)
        {
            query = query.Where(r => r.CurrentWindowStartUtc <= filters.WindowToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.CustomerReference))
        {
            var customerReference = filters.CustomerReference.Trim();
            query = query.Where(r =>
                r.CustomerReference == customerReference
                || dbContext.CommercialProfitabilityComparisonResults.Any(result =>
                    result.ComparisonRunId == r.Id && result.CustomerReference == customerReference));
        }

        var runs = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return runs.Select(MapComparisonSummary).ToList();
    }

    internal static async Task<CommercialProfitabilityComparisonDetailReadModel?> GetComparisonByIdAsync(
        CrevaFactOSDbContext dbContext,
        CommercialIntelligenceOperationContext context,
        Guid comparisonRunId,
        CancellationToken cancellationToken)
    {
        var run = await dbContext.CommercialProfitabilityComparisonRuns.AsNoTracking()
            .Include(r => r.Profile)
            .Include(r => r.Results)
            .Include(r => r.Drivers)
            .SingleOrDefaultAsync(
                r => r.TenantId == context.TenantId
                     && r.FacilityId == context.FacilityId
                     && r.Id == comparisonRunId,
                cancellationToken);

        return run is null ? null : MapComparisonDetail(run);
    }

    internal static async Task<CommercialProfitabilityWriteResult> EvaluateComparisonAsync(
        CrevaFactOSDbContext dbContext,
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialProfitabilityComparisonCommand command,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateEvaluateComparisonCommand(command);
        if (validationError is not null)
        {
            return CommercialProfitabilityWriteResult.Fail(
                CommercialIntelligenceFailureCodes.ValidationFailed,
                validationError);
        }

        var fingerprint = CommercialProfitabilityIdempotencyFingerprint.ComputeComparisonFingerprint(context, command);
        var existing = await dbContext.CommercialProfitabilityComparisonRuns.AsNoTracking()
            .Include(r => r.Profile)
            .Include(r => r.Results)
            .Include(r => r.Drivers)
            .SingleOrDefaultAsync(
                r => r.TenantId == context.TenantId && r.IdempotencyKey == command.IdempotencyKey.Trim(),
                cancellationToken);

        if (existing is not null)
        {
            return ResolveIdempotentComparisonReplay(existing, fingerprint);
        }

        var profile = await dbContext.CommercialProfitabilityProfiles
            .SingleOrDefaultAsync(
                p => p.TenantId == context.TenantId
                     && p.FacilityId == context.FacilityId
                     && p.Id == command.ProfileId,
                cancellationToken);

        if (profile is null)
        {
            return CommercialProfitabilityWriteResult.Fail(
                CommercialIntelligenceFailureCodes.NotFound,
                "Profitability profile not found.");
        }

        if (profile.Version != command.ExpectedProfileVersion)
        {
            return CommercialProfitabilityWriteResult.Fail(
                CommercialIntelligenceFailureCodes.ConcurrencyConflict,
                "Profitability profile version is stale.");
        }

        if (!string.Equals(profile.Status, CommercialIntelligenceProfileStatuses.Approved, StringComparison.Ordinal))
        {
            return CommercialProfitabilityWriteResult.Fail(
                CommercialIntelligenceFailureCodes.ValidationFailed,
                "Only approved profitability profiles may execute comparisons.");
        }

        var comparisonType = command.ComparisonType.Trim();
        CommercialProfitabilityComparisonEvaluationOutput evaluation;
        var includeCostComponents = string.Equals(
            comparisonType,
            CommercialProfitabilityComparisonTypes.CostComponentMixChange,
            StringComparison.Ordinal);

        if (RequiresTemporalWindows(comparisonType))
        {
            var referenceAssessments = await CommercialProfitabilityAssessmentReader.ReadAssessmentsInWindowAsync(
                dbContext,
                context,
                profile.Id,
                command.ReferenceWindowStartUtc!.Value,
                command.ReferenceWindowEndUtc!.Value,
                command.MaterialId,
                command.CustomerReference,
                includeCostComponents,
                cancellationToken);
            var currentAssessments = await CommercialProfitabilityAssessmentReader.ReadAssessmentsInWindowAsync(
                dbContext,
                context,
                profile.Id,
                command.CurrentWindowStartUtc,
                command.CurrentWindowEndUtc,
                command.MaterialId,
                command.CustomerReference,
                includeCostComponents,
                cancellationToken);

            var referenceSlice = CommercialProfitabilityAssessmentReader.SummarizeWindow(referenceAssessments);
            var currentSlice = CommercialProfitabilityAssessmentReader.SummarizeWindow(currentAssessments);

            evaluation = string.Equals(comparisonType, CommercialProfitabilityComparisonTypes.TemporalContributionTrend, StringComparison.Ordinal)
                ? CommercialProfitabilityComparisonEvaluator.EvaluateTemporalContributionTrend(
                    profile, referenceSlice, currentSlice, referenceAssessments, currentAssessments)
                : CommercialProfitabilityComparisonEvaluator.EvaluateCostComponentMixChange(
                    profile, referenceSlice, currentSlice, referenceAssessments, currentAssessments);
        }
        else if (string.Equals(comparisonType, CommercialProfitabilityComparisonTypes.CustomerContributionPosition, StringComparison.Ordinal))
        {
            var assessments = await CommercialProfitabilityAssessmentReader.ReadAssessmentsInWindowAsync(
                dbContext,
                context,
                profile.Id,
                command.CurrentWindowStartUtc,
                command.CurrentWindowEndUtc,
                command.MaterialId,
                null,
                includeCostComponents: false,
                cancellationToken);

            var customerReferences = assessments
                .Select(a => a.CustomerReference)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(c => c, StringComparer.Ordinal)
                .ToList();

            if (!string.IsNullOrWhiteSpace(command.CustomerReference))
            {
                customerReferences = [command.CustomerReference.Trim()];
            }

            evaluation = CommercialProfitabilityComparisonEvaluator.EvaluateCustomerContributionPosition(
                profile,
                assessments,
                customerReferences);
        }
        else
        {
            var assessments = await CommercialProfitabilityAssessmentReader.ReadAssessmentsInWindowAsync(
                dbContext,
                context,
                profile.Id,
                command.CurrentWindowStartUtc,
                command.CurrentWindowEndUtc,
                null,
                command.CustomerReference,
                includeCostComponents: false,
                cancellationToken);

            var materialIds = assessments
                .Select(a => a.MaterialId)
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            if (command.MaterialId.HasValue)
            {
                materialIds = [command.MaterialId.Value];
            }

            evaluation = CommercialProfitabilityComparisonEvaluator.EvaluateMaterialContributionPosition(
                profile,
                assessments,
                materialIds);
        }

        var now = DateTimeOffset.UtcNow;
        var actor = context.IdentitySubjectId.ToString();
        var run = new CommercialProfitabilityComparisonRun
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            ComparisonReference = await GenerateComparisonReferenceAsync(dbContext, context, cancellationToken),
            ProfileId = profile.Id,
            ProfileVersion = profile.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ComparisonType = comparisonType,
            MaterialId = command.MaterialId,
            CustomerReference = string.IsNullOrWhiteSpace(command.CustomerReference)
                ? null
                : command.CustomerReference.Trim(),
            ReferenceWindowStartUtc = command.ReferenceWindowStartUtc,
            ReferenceWindowEndUtc = command.ReferenceWindowEndUtc,
            CurrentWindowStartUtc = command.CurrentWindowStartUtc,
            CurrentWindowEndUtc = command.CurrentWindowEndUtc,
            AggregationMethod = evaluation.AggregationMethod,
            EvidenceStatus = evaluation.EvidenceStatus,
            ComparisonOutcome = evaluation.ComparisonOutcome,
            CandidateAssessmentCount = evaluation.CandidateAssessmentCount,
            ComparableAssessmentCount = evaluation.ComparableAssessmentCount,
            ExcludedAssessmentCount = evaluation.ExcludedAssessmentCount,
            CompleteCoverageCount = evaluation.CompleteCoverageCount,
            PartialCoverageCount = evaluation.PartialCoverageCount,
            InsufficientDataCount = evaluation.InsufficientDataCount,
            SourceConflictCount = evaluation.SourceConflictCount,
            NegativeContributionCount = evaluation.NegativeContributionCount,
            BelowThresholdCount = evaluation.BelowThresholdCount,
            ReferenceCandidateAssessmentCount = evaluation.ReferenceCandidateAssessmentCount,
            ReferenceComparableAssessmentCount = evaluation.ReferenceComparableAssessmentCount,
            ReferenceExcludedAssessmentCount = evaluation.ReferenceExcludedAssessmentCount,
            ReferenceCompleteCoverageCount = evaluation.ReferenceCompleteCoverageCount,
            ReferencePartialCoverageCount = evaluation.ReferencePartialCoverageCount,
            ReferenceInsufficientDataCount = evaluation.ReferenceInsufficientDataCount,
            ReferenceSourceConflictCount = evaluation.ReferenceSourceConflictCount,
            ReferenceNegativeContributionCount = evaluation.ReferenceNegativeContributionCount,
            ReferenceBelowThresholdCount = evaluation.ReferenceBelowThresholdCount,
            CurrentCandidateAssessmentCount = evaluation.CurrentCandidateAssessmentCount,
            CurrentComparableAssessmentCount = evaluation.CurrentComparableAssessmentCount,
            CurrentExcludedAssessmentCount = evaluation.CurrentExcludedAssessmentCount,
            CurrentCompleteCoverageCount = evaluation.CurrentCompleteCoverageCount,
            CurrentPartialCoverageCount = evaluation.CurrentPartialCoverageCount,
            CurrentInsufficientDataCount = evaluation.CurrentInsufficientDataCount,
            CurrentSourceConflictCount = evaluation.CurrentSourceConflictCount,
            CurrentNegativeContributionCount = evaluation.CurrentNegativeContributionCount,
            CurrentBelowThresholdCount = evaluation.CurrentBelowThresholdCount,
            ReferenceMedianContributionMarginRatio = evaluation.ReferenceMedianContributionMarginRatio,
            CurrentMedianContributionMarginRatio = evaluation.CurrentMedianContributionMarginRatio,
            ContributionMarginDelta = evaluation.ContributionMarginDelta,
            ReferenceMedianUnitContribution = evaluation.ReferenceMedianUnitContribution,
            CurrentMedianUnitContribution = evaluation.CurrentMedianUnitContribution,
            LeadingDriverCode = evaluation.LeadingDriverCode,
            RuleSetVersion = CommercialIntelligenceConstants.ProfitabilityComparisonRuleSetVersion,
            IdempotencyKey = command.IdempotencyKey.Trim(),
            IdempotencyPayloadFingerprint = fingerprint,
            SourceProvenance = command.SourceProvenance.Trim(),
            EvaluatedAtUtc = now,
            CreatedAtUtc = now,
            CreatedBy = actor,
        };

        var resultIdByKey = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var resultDraft in evaluation.Results)
        {
            var resultId = Guid.NewGuid();
            resultIdByKey[resultDraft.CanonicalResultReference] = resultId;
            run.Results.Add(new CommercialProfitabilityComparisonResult
            {
                Id = resultId,
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ComparisonRunId = run.Id,
                ResultDimension = resultDraft.ResultDimension,
                CanonicalResultReference = resultDraft.CanonicalResultReference,
                CustomerReference = resultDraft.CustomerReference,
                MaterialId = resultDraft.MaterialId,
                ComponentKind = resultDraft.ComponentKind,
                CandidateAssessmentCount = resultDraft.CandidateAssessmentCount,
                ComparableAssessmentCount = resultDraft.ComparableAssessmentCount,
                ExcludedAssessmentCount = resultDraft.ExcludedAssessmentCount,
                CompleteCoverageCount = resultDraft.CompleteCoverageCount,
                PartialCoverageCount = 0,
                NegativeContributionCount = resultDraft.NegativeContributionCount,
                BelowThresholdCount = resultDraft.BelowThresholdCount,
                ReferenceComparableAssessmentCount = resultDraft.ReferenceComparableAssessmentCount,
                CurrentComparableAssessmentCount = resultDraft.CurrentComparableAssessmentCount,
                ReferenceExcludedAssessmentCount = resultDraft.ReferenceExcludedAssessmentCount,
                CurrentExcludedAssessmentCount = resultDraft.CurrentExcludedAssessmentCount,
                ReferenceMedianContributionMarginRatio = resultDraft.ReferenceMedianContributionMarginRatio,
                CurrentMedianContributionMarginRatio = resultDraft.CurrentMedianContributionMarginRatio,
                ContributionMarginDelta = resultDraft.ContributionMarginDelta,
                ReferenceMedianUnitContribution = resultDraft.ReferenceMedianUnitContribution,
                CurrentMedianUnitContribution = resultDraft.CurrentMedianUnitContribution,
                ReferenceMedianUnitCost = resultDraft.ReferenceMedianUnitCost,
                CurrentMedianUnitCost = resultDraft.CurrentMedianUnitCost,
                SignedUnitCostChange = resultDraft.SignedUnitCostChange,
                SignedUnitCostChangeRatio = resultDraft.SignedUnitCostChangeRatio,
                ReferenceMedianCostShare = resultDraft.ReferenceMedianCostShare,
                CurrentMedianCostShare = resultDraft.CurrentMedianCostShare,
                CostShareDelta = resultDraft.CostShareDelta,
                PositionState = resultDraft.PositionState,
                ContributionRank = resultDraft.ContributionRank,
                IsTied = resultDraft.IsTied,
                EvidenceStatus = resultDraft.EvidenceStatus,
                SourceProvenance = command.SourceProvenance.Trim(),
                SortOrder = resultDraft.ContributionRank ?? 0,
                CreatedAtUtc = now,
            });
        }

        var orderedDrivers = CommercialProfitabilityComparisonEvaluator.OrderDrivers(evaluation.Drivers);
        for (var index = 0; index < orderedDrivers.Count; index++)
        {
            var driver = orderedDrivers[index];
            run.Drivers.Add(new CommercialProfitabilityComparisonDriver
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ComparisonRunId = run.Id,
                ComparisonResultId = driver.ComparisonResultId,
                ReasonCode = driver.ReasonCode,
                Severity = driver.Severity,
                ActualValue = driver.ActualValue,
                ReferenceValue = driver.ReferenceValue,
                ThresholdValue = driver.ThresholdValue,
                Unit = driver.Unit,
                NormalizedVariance = driver.NormalizedVariance,
                AffectedAssessmentCount = driver.AffectedAssessmentCount,
                SupportingText = driver.SupportingText,
                SourceProvenance = command.SourceProvenance.Trim(),
                SortOrder = index + 1,
                IsLeadingDriver = index == 0,
                CreatedAtUtc = now,
            });
        }

        dbContext.CommercialProfitabilityComparisonRuns.Add(run);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueIdempotencyViolation(ex))
        {
            var replay = await dbContext.CommercialProfitabilityComparisonRuns.AsNoTracking()
                .Include(r => r.Profile)
                .Include(r => r.Results)
                .Include(r => r.Drivers)
                .SingleOrDefaultAsync(
                    r => r.TenantId == context.TenantId && r.IdempotencyKey == command.IdempotencyKey.Trim(),
                    cancellationToken);

            if (replay is null)
            {
                throw;
            }

            return ResolveIdempotentComparisonReplay(replay, fingerprint);
        }

        return CommercialProfitabilityWriteResult.Success(run.Id, profile.Version, comparison: MapComparisonDetail(run));
    }

    private static bool RequiresTemporalWindows(string comparisonType) =>
        string.Equals(comparisonType, CommercialProfitabilityComparisonTypes.TemporalContributionTrend, StringComparison.Ordinal)
        || string.Equals(comparisonType, CommercialProfitabilityComparisonTypes.CostComponentMixChange, StringComparison.Ordinal);

    private static async Task<string> GenerateComparisonReferenceAsync(
        CrevaFactOSDbContext dbContext,
        CommercialIntelligenceOperationContext context,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var reference = $"CI-PRC-{suffix}";
            var exists = await dbContext.CommercialProfitabilityComparisonRuns.AsNoTracking()
                .AnyAsync(r => r.TenantId == context.TenantId && r.ComparisonReference == reference, cancellationToken);
            if (!exists)
            {
                return reference;
            }
        }

        return $"CI-PRC-{Guid.NewGuid():N}".ToUpperInvariant();
    }

    private static CommercialProfitabilityWriteResult ResolveIdempotentComparisonReplay(
        CommercialProfitabilityComparisonRun existing,
        string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(existing.IdempotencyPayloadFingerprint))
        {
            return CommercialProfitabilityWriteResult.Fail(
                CommercialIntelligenceFailureCodes.IdempotencyLegacyUnverifiable,
                "Existing comparison idempotency fingerprint is unavailable.");
        }

        if (!string.Equals(existing.IdempotencyPayloadFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return CommercialProfitabilityWriteResult.Fail(
                CommercialIntelligenceFailureCodes.IdempotencyConflict,
                "Idempotency key was already used with a different payload.");
        }

        return CommercialProfitabilityWriteResult.Success(
            existing.Id,
            uint.TryParse(existing.ProfileVersion, out var version) ? version : 1,
            idempotentReplay: true,
            comparison: MapComparisonDetail(existing));
    }

    private static string? ValidateEvaluateComparisonCommand(EvaluateCommercialProfitabilityComparisonCommand command)
    {
        if (command.CurrentWindowEndUtc <= command.CurrentWindowStartUtc)
        {
            return "Current window end must be greater than current window start.";
        }

        if (!CommercialProfitabilityComparisonTypes.All.Contains(command.ComparisonType.Trim()))
        {
            return "Comparison type is invalid.";
        }

        var comparisonType = command.ComparisonType.Trim();
        if (RequiresTemporalWindows(comparisonType))
        {
            if (!command.ReferenceWindowStartUtc.HasValue || !command.ReferenceWindowEndUtc.HasValue)
            {
                return "Reference window start and end are required for temporal comparison types.";
            }

            if (command.ReferenceWindowEndUtc.Value <= command.ReferenceWindowStartUtc.Value)
            {
                return "Reference window end must be greater than reference window start.";
            }

            if (WindowsOverlap(
                    command.ReferenceWindowStartUtc.Value,
                    command.ReferenceWindowEndUtc.Value,
                    command.CurrentWindowStartUtc,
                    command.CurrentWindowEndUtc))
            {
                return "Reference and current windows must not overlap.";
            }
        }

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return "Idempotency key is required.";
        }

        if (string.IsNullOrWhiteSpace(command.SourceProvenance))
        {
            return "Source provenance is required.";
        }

        return null;
    }

    private static bool WindowsOverlap(
        DateTimeOffset referenceStart,
        DateTimeOffset referenceEnd,
        DateTimeOffset currentStart,
        DateTimeOffset currentEnd) =>
        referenceStart < currentEnd && currentStart < referenceEnd;

    private static bool IsUniqueIdempotencyViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static bool IsPilotComparison(Guid runId) =>
        DevelopmentPilotIds.CommercialProfitabilityComparisonPilotRunIds.Contains(runId);

    internal static CommercialProfitabilityComparisonSummaryReadModel MapComparisonSummary(
        CommercialProfitabilityComparisonRun run) =>
        new(
            run.Id,
            run.ComparisonReference,
            run.ProfileId,
            run.Profile?.ProfileCode ?? string.Empty,
            run.ComparisonType,
            run.MaterialId,
            run.CustomerReference,
            run.ComparisonOutcome,
            run.EvidenceStatus,
            run.AggregationMethod,
            run.CandidateAssessmentCount,
            run.ComparableAssessmentCount,
            run.ExcludedAssessmentCount,
            run.ReferenceMedianContributionMarginRatio,
            run.CurrentMedianContributionMarginRatio,
            run.ContributionMarginDelta,
            run.ReferenceWindowStartUtc,
            run.ReferenceWindowEndUtc,
            run.CurrentWindowStartUtc,
            run.CurrentWindowEndUtc,
            run.LeadingDriverCode,
            run.RuleSetVersion,
            run.EvaluatedAtUtc,
            run.CreatedAtUtc,
            IsPilotComparison(run.Id));

    internal static CommercialProfitabilityComparisonDetailReadModel MapComparisonDetail(
        CommercialProfitabilityComparisonRun run) =>
        new(
            run.Id,
            run.ComparisonReference,
            run.ProfileId,
            run.Profile?.ProfileCode ?? string.Empty,
            run.ProfileVersion,
            run.ComparisonType,
            run.MaterialId,
            run.CustomerReference,
            run.AggregationMethod,
            run.CandidateAssessmentCount,
            run.ComparableAssessmentCount,
            run.ExcludedAssessmentCount,
            run.CompleteCoverageCount,
            run.PartialCoverageCount,
            run.InsufficientDataCount,
            run.SourceConflictCount,
            run.NegativeContributionCount,
            run.BelowThresholdCount,
            run.ReferenceCandidateAssessmentCount,
            run.ReferenceComparableAssessmentCount,
            run.ReferenceExcludedAssessmentCount,
            run.ReferenceCompleteCoverageCount,
            run.ReferencePartialCoverageCount,
            run.ReferenceInsufficientDataCount,
            run.ReferenceSourceConflictCount,
            run.ReferenceNegativeContributionCount,
            run.ReferenceBelowThresholdCount,
            run.CurrentCandidateAssessmentCount,
            run.CurrentComparableAssessmentCount,
            run.CurrentExcludedAssessmentCount,
            run.CurrentCompleteCoverageCount,
            run.CurrentPartialCoverageCount,
            run.CurrentInsufficientDataCount,
            run.CurrentSourceConflictCount,
            run.CurrentNegativeContributionCount,
            run.CurrentBelowThresholdCount,
            run.ReferenceMedianContributionMarginRatio,
            run.CurrentMedianContributionMarginRatio,
            run.ContributionMarginDelta,
            run.ReferenceMedianUnitContribution,
            run.CurrentMedianUnitContribution,
            run.ReferenceWindowStartUtc,
            run.ReferenceWindowEndUtc,
            run.CurrentWindowStartUtc,
            run.CurrentWindowEndUtc,
            run.EvidenceStatus,
            run.ComparisonOutcome,
            run.LeadingDriverCode,
            run.RuleSetVersion,
            run.SourceProvenance,
            run.EvaluatedAtUtc,
            run.CreatedAtUtc,
            run.Results
                .OrderBy(r => r.SortOrder)
                .ThenBy(r => r.ContributionRank ?? int.MaxValue)
                .ThenBy(r => r.CanonicalResultReference, StringComparer.Ordinal)
                .Select(r => new CommercialProfitabilityComparisonResultReadModel(
                    r.Id,
                    r.ResultDimension,
                    r.CanonicalResultReference,
                    r.CustomerReference,
                    r.MaterialId,
                    r.ComponentKind,
                    r.CandidateAssessmentCount,
                    r.ComparableAssessmentCount,
                    r.ExcludedAssessmentCount,
                    r.CompleteCoverageCount,
                    r.PartialCoverageCount,
                    r.NegativeContributionCount,
                    r.BelowThresholdCount,
                    r.ReferenceComparableAssessmentCount,
                    r.CurrentComparableAssessmentCount,
                    r.ReferenceExcludedAssessmentCount,
                    r.CurrentExcludedAssessmentCount,
                    r.ReferenceMedianContributionMarginRatio,
                    r.CurrentMedianContributionMarginRatio,
                    r.ContributionMarginDelta,
                    r.ReferenceMedianUnitContribution,
                    r.CurrentMedianUnitContribution,
                    r.ReferenceMedianUnitCost,
                    r.CurrentMedianUnitCost,
                    r.SignedUnitCostChange,
                    r.SignedUnitCostChangeRatio,
                    r.ReferenceMedianCostShare,
                    r.CurrentMedianCostShare,
                    r.CostShareDelta,
                    r.PositionState,
                    r.ContributionRank,
                    r.IsTied,
                    r.EvidenceStatus,
                    r.SortOrder))
                .ToList(),
            run.Drivers
                .OrderBy(d => d.SortOrder)
                .Select(d => new CommercialProfitabilityComparisonDriverReadModel(
                    d.Id,
                    d.ComparisonResultId,
                    d.ReasonCode,
                    d.Severity,
                    d.ActualValue,
                    d.ReferenceValue,
                    d.ThresholdValue,
                    d.Unit,
                    d.NormalizedVariance,
                    d.AffectedAssessmentCount,
                    d.SupportingText,
                    d.SortOrder,
                    d.IsLeadingDriver))
                .ToList(),
            IsPilotComparison(run.Id));
}
