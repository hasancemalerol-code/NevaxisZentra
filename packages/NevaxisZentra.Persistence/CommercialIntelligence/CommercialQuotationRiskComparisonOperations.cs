using NevaxisZentra.Persistence.Development;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace NevaxisZentra.Persistence.CommercialIntelligence;

internal static class CommercialQuotationRiskComparisonOperations
{
    internal static async Task<IReadOnlyList<CommercialQuotationRiskComparisonSummaryReadModel>> ListComparisonsAsync(
        NevaxisZentraDbContext dbContext,
        CommercialIntelligenceOperationContext context,
        CommercialQuotationRiskComparisonListFilters filters,
        CancellationToken cancellationToken)
    {
        var page = filters.Page < 1 ? 1 : filters.Page;
        var pageSize = filters.PageSize is < 1 or > 100 ? 25 : filters.PageSize;

        var query = dbContext.CommercialQuotationRiskComparisonRuns.AsNoTracking()
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
                || dbContext.CommercialQuotationRiskComparisonResults.Any(result =>
                    result.ComparisonRunId == r.Id && result.CustomerReference == customerReference));
        }

        var runs = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return runs.Select(MapComparisonSummary).ToList();
    }

    internal static async Task<CommercialQuotationRiskComparisonDetailReadModel?> GetComparisonByIdAsync(
        NevaxisZentraDbContext dbContext,
        CommercialIntelligenceOperationContext context,
        Guid comparisonRunId,
        CancellationToken cancellationToken)
    {
        var run = await dbContext.CommercialQuotationRiskComparisonRuns.AsNoTracking()
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

    internal static async Task<CommercialQuotationRiskWriteResult> EvaluateComparisonAsync(
        NevaxisZentraDbContext dbContext,
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialQuotationRiskComparisonCommand command,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateEvaluateComparisonCommand(command);
        if (validationError is not null)
        {
            return CommercialQuotationRiskWriteResult.Fail(
                CommercialIntelligenceFailureCodes.ValidationFailed,
                validationError);
        }

        var fingerprint = CommercialQuotationRiskIdempotencyFingerprint.ComputeComparisonFingerprint(context, command);
        var existing = await dbContext.CommercialQuotationRiskComparisonRuns.AsNoTracking()
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

        var profile = await dbContext.CommercialQuotationRiskProfiles
            .SingleOrDefaultAsync(
                p => p.TenantId == context.TenantId
                     && p.FacilityId == context.FacilityId
                     && p.Id == command.ProfileId,
                cancellationToken);

        if (profile is null)
        {
            return CommercialQuotationRiskWriteResult.Fail(
                CommercialIntelligenceFailureCodes.NotFound,
                "Quotation risk profile not found.");
        }

        if (profile.Version != command.ExpectedProfileVersion)
        {
            return CommercialQuotationRiskWriteResult.Fail(
                CommercialIntelligenceFailureCodes.ConcurrencyConflict,
                "Quotation risk profile version is stale.");
        }

        if (!string.Equals(profile.Status, CommercialIntelligenceProfileStatuses.Approved, StringComparison.Ordinal))
        {
            return CommercialQuotationRiskWriteResult.Fail(
                CommercialIntelligenceFailureCodes.ValidationFailed,
                "Only approved quotation risk profiles may execute comparisons.");
        }

        var comparisonType = command.ComparisonType.Trim();
        CommercialQuotationRiskComparisonEvaluationOutput evaluation;

        if (string.Equals(comparisonType, CommercialQuotationRiskComparisonTypes.TemporalRiskTrend, StringComparison.Ordinal))
        {
            var referenceAssessments = await CommercialQuotationRiskAssessmentReader.ReadAssessmentsInWindowAsync(
                dbContext,
                context,
                profile.Id,
                command.ReferenceWindowStartUtc!.Value,
                command.ReferenceWindowEndUtc!.Value,
                command.MaterialId,
                command.CustomerReference,
                cancellationToken);
            var currentAssessments = await CommercialQuotationRiskAssessmentReader.ReadAssessmentsInWindowAsync(
                dbContext,
                context,
                profile.Id,
                command.CurrentWindowStartUtc,
                command.CurrentWindowEndUtc,
                command.MaterialId,
                command.CustomerReference,
                cancellationToken);

            evaluation = CommercialQuotationRiskComparisonEvaluator.EvaluateTemporalRiskTrend(
                profile,
                CommercialQuotationRiskAssessmentReader.SummarizeWindow(referenceAssessments),
                CommercialQuotationRiskAssessmentReader.SummarizeWindow(currentAssessments));
        }
        else
        {
            var assessments = await CommercialQuotationRiskAssessmentReader.ReadAssessmentsInWindowAsync(
                dbContext,
                context,
                profile.Id,
                command.CurrentWindowStartUtc,
                command.CurrentWindowEndUtc,
                command.MaterialId,
                null,
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

            evaluation = CommercialQuotationRiskComparisonEvaluator.EvaluateCustomerRiskPosition(
                profile,
                assessments,
                customerReferences);
        }

        var now = DateTimeOffset.UtcNow;
        var actor = context.IdentitySubjectId.ToString();
        var run = new CommercialQuotationRiskComparisonRun
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
            EvidenceStatus = evaluation.EvidenceStatus,
            ComparisonOutcome = evaluation.ComparisonOutcome,
            CandidateAssessmentCount = evaluation.CandidateAssessmentCount,
            ComparableAssessmentCount = evaluation.ComparableAssessmentCount,
            ExcludedAssessmentCount = evaluation.ExcludedAssessmentCount,
            RiskBearingAssessmentCount = evaluation.RiskBearingAssessmentCount,
            InsufficientDataCount = evaluation.InsufficientDataCount,
            SourceConflictCount = evaluation.SourceConflictCount,
            PriceRiskCount = evaluation.PriceRiskCount,
            ExpiryRiskCount = evaluation.ExpiryRiskCount,
            PaymentTermRiskCount = evaluation.PaymentTermRiskCount,
            ReferenceCandidateAssessmentCount = evaluation.ReferenceCandidateAssessmentCount,
            ReferenceComparableAssessmentCount = evaluation.ReferenceComparableAssessmentCount,
            ReferenceExcludedAssessmentCount = evaluation.ReferenceExcludedAssessmentCount,
            ReferenceRiskBearingAssessmentCount = evaluation.ReferenceRiskBearingAssessmentCount,
            CurrentCandidateAssessmentCount = evaluation.CurrentCandidateAssessmentCount,
            CurrentComparableAssessmentCount = evaluation.CurrentComparableAssessmentCount,
            CurrentExcludedAssessmentCount = evaluation.CurrentExcludedAssessmentCount,
            CurrentRiskBearingAssessmentCount = evaluation.CurrentRiskBearingAssessmentCount,
            ReferenceRiskIncidence = evaluation.ReferenceRiskIncidence,
            CurrentRiskIncidence = evaluation.CurrentRiskIncidence,
            RiskIncidenceDelta = evaluation.RiskIncidenceDelta,
            LeadingDriverCode = evaluation.LeadingDriverCode,
            RuleSetVersion = CommercialIntelligenceConstants.QuotationRiskComparisonRuleSetVersion,
            IdempotencyKey = command.IdempotencyKey.Trim(),
            IdempotencyPayloadFingerprint = fingerprint,
            SourceProvenance = command.SourceProvenance.Trim(),
            EvaluatedAtUtc = now,
            CreatedAtUtc = now,
            CreatedBy = actor,
        };

        var resultIdByCustomer = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var customerResult in evaluation.CustomerResults)
        {
            var resultId = Guid.NewGuid();
            resultIdByCustomer[customerResult.CustomerReference] = resultId;
            run.Results.Add(new CommercialQuotationRiskComparisonResult
            {
                Id = resultId,
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ComparisonRunId = run.Id,
                CustomerReference = customerResult.CustomerReference,
                CandidateAssessmentCount = customerResult.CandidateAssessmentCount,
                ComparableAssessmentCount = customerResult.ComparableAssessmentCount,
                ExcludedAssessmentCount = customerResult.ExcludedAssessmentCount,
                RiskBearingAssessmentCount = customerResult.RiskBearingAssessmentCount,
                RiskIncidenceFraction = customerResult.RiskIncidenceFraction,
                PriceRiskCount = customerResult.PriceRiskCount,
                ExpiryRiskCount = customerResult.ExpiryRiskCount,
                PaymentTermRiskCount = customerResult.PaymentTermRiskCount,
                PositionState = customerResult.PositionState,
                RiskRank = customerResult.RiskRank,
                IsTied = customerResult.IsTied,
                EvidenceStatus = customerResult.EvidenceStatus,
                CreatedAtUtc = now,
            });
        }

        var orderedDrivers = CommercialQuotationRiskComparisonEvaluator.OrderDrivers(evaluation.Drivers);
        for (var index = 0; index < orderedDrivers.Count; index++)
        {
            var driver = orderedDrivers[index];
            run.Drivers.Add(new CommercialQuotationRiskComparisonDriver
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

        dbContext.CommercialQuotationRiskComparisonRuns.Add(run);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueIdempotencyViolation(ex))
        {
            var replay = await dbContext.CommercialQuotationRiskComparisonRuns.AsNoTracking()
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

        return CommercialQuotationRiskWriteResult.Success(run.Id, profile.Version, comparison: MapComparisonDetail(run));
    }

    private static async Task<string> GenerateComparisonReferenceAsync(
        NevaxisZentraDbContext dbContext,
        CommercialIntelligenceOperationContext context,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var reference = $"CI-QRC-{suffix}";
            var exists = await dbContext.CommercialQuotationRiskComparisonRuns.AsNoTracking()
                .AnyAsync(r => r.TenantId == context.TenantId && r.ComparisonReference == reference, cancellationToken);
            if (!exists)
            {
                return reference;
            }
        }

        return $"CI-QRC-{Guid.NewGuid():N}".ToUpperInvariant();
    }

    private static CommercialQuotationRiskWriteResult ResolveIdempotentComparisonReplay(
        CommercialQuotationRiskComparisonRun existing,
        string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(existing.IdempotencyPayloadFingerprint))
        {
            return CommercialQuotationRiskWriteResult.Fail(
                CommercialIntelligenceFailureCodes.IdempotencyLegacyUnverifiable,
                "Existing comparison idempotency fingerprint is unavailable.");
        }

        if (!string.Equals(existing.IdempotencyPayloadFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return CommercialQuotationRiskWriteResult.Fail(
                CommercialIntelligenceFailureCodes.IdempotencyConflict,
                "Idempotency key was already used with a different payload.");
        }

        return CommercialQuotationRiskWriteResult.Success(
            existing.Id,
            uint.TryParse(existing.ProfileVersion, out var version) ? version : 1,
            idempotentReplay: true,
            comparison: MapComparisonDetail(existing));
    }

    private static string? ValidateEvaluateComparisonCommand(EvaluateCommercialQuotationRiskComparisonCommand command)
    {
        if (command.CurrentWindowEndUtc <= command.CurrentWindowStartUtc)
        {
            return "Current window end must be greater than current window start.";
        }

        if (!CommercialQuotationRiskComparisonTypes.All.Contains(command.ComparisonType.Trim()))
        {
            return "Comparison type is invalid.";
        }

        if (string.Equals(command.ComparisonType.Trim(), CommercialQuotationRiskComparisonTypes.TemporalRiskTrend, StringComparison.Ordinal))
        {
            if (!command.ReferenceWindowStartUtc.HasValue || !command.ReferenceWindowEndUtc.HasValue)
            {
                return "Reference window start and end are required for temporal risk trend comparisons.";
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
        DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotRunIds.Contains(runId);

    internal static CommercialQuotationRiskComparisonSummaryReadModel MapComparisonSummary(
        CommercialQuotationRiskComparisonRun run) =>
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
            run.CandidateAssessmentCount,
            run.ComparableAssessmentCount,
            run.ExcludedAssessmentCount,
            run.ReferenceRiskIncidence,
            run.CurrentRiskIncidence,
            run.RiskIncidenceDelta,
            run.ReferenceWindowStartUtc,
            run.ReferenceWindowEndUtc,
            run.CurrentWindowStartUtc,
            run.CurrentWindowEndUtc,
            run.LeadingDriverCode,
            run.RuleSetVersion,
            run.EvaluatedAtUtc,
            run.CreatedAtUtc,
            IsPilotComparison(run.Id));

    internal static CommercialQuotationRiskComparisonDetailReadModel MapComparisonDetail(
        CommercialQuotationRiskComparisonRun run) =>
        new(
            run.Id,
            run.ComparisonReference,
            run.ProfileId,
            run.Profile?.ProfileCode ?? string.Empty,
            run.ProfileVersion,
            run.ComparisonType,
            run.MaterialId,
            run.CustomerReference,
            run.CandidateAssessmentCount,
            run.ComparableAssessmentCount,
            run.ExcludedAssessmentCount,
            run.RiskBearingAssessmentCount,
            run.InsufficientDataCount,
            run.SourceConflictCount,
            run.PriceRiskCount,
            run.ExpiryRiskCount,
            run.PaymentTermRiskCount,
            run.ReferenceCandidateAssessmentCount,
            run.ReferenceComparableAssessmentCount,
            run.ReferenceExcludedAssessmentCount,
            run.ReferenceRiskBearingAssessmentCount,
            run.CurrentCandidateAssessmentCount,
            run.CurrentComparableAssessmentCount,
            run.CurrentExcludedAssessmentCount,
            run.CurrentRiskBearingAssessmentCount,
            run.ReferenceRiskIncidence,
            run.CurrentRiskIncidence,
            run.RiskIncidenceDelta,
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
                .OrderBy(r => r.RiskRank ?? int.MaxValue)
                .ThenByDescending(r => r.RiskIncidenceFraction ?? -1m)
                .ThenBy(r => r.CustomerReference, StringComparer.Ordinal)
                .Select(r => new CommercialQuotationRiskComparisonResultReadModel(
                    r.Id,
                    r.CustomerReference,
                    r.CandidateAssessmentCount,
                    r.ComparableAssessmentCount,
                    r.ExcludedAssessmentCount,
                    r.RiskBearingAssessmentCount,
                    r.RiskIncidenceFraction,
                    r.PriceRiskCount,
                    r.ExpiryRiskCount,
                    r.PaymentTermRiskCount,
                    r.PositionState,
                    r.RiskRank,
                    r.IsTied,
                    r.EvidenceStatus))
                .ToList(),
            run.Drivers
                .OrderBy(d => d.SortOrder)
                .Select(d => new CommercialQuotationRiskComparisonDriverReadModel(
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
