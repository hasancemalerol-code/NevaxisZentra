using CrevaFactOS.Persistence.Development;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CrevaFactOS.Persistence.CommercialIntelligence;

internal static class CommercialSalesPriceComparisonOperations
{
    internal static async Task<IReadOnlyList<CommercialSalesPriceComparisonSummaryReadModel>> ListComparisonsAsync(
        CrevaFactOSDbContext dbContext,
        CommercialIntelligenceOperationContext context,
        CommercialSalesPriceComparisonListFilters filters,
        CancellationToken cancellationToken)
    {
        var page = filters.Page < 1 ? 1 : filters.Page;
        var pageSize = filters.PageSize is < 1 or > 100 ? 25 : filters.PageSize;

        var query = dbContext.CommercialSalesPriceComparisonRuns.AsNoTracking()
            .Include(r => r.Profile)
            .Where(r => r.TenantId == context.TenantId && r.FacilityId == context.FacilityId);

        if (!string.IsNullOrWhiteSpace(filters.ComparisonType))
        {
            query = query.Where(r => r.ComparisonType == filters.ComparisonType.Trim());
        }

        if (filters.MaterialId.HasValue)
        {
            query = query.Where(r => r.MaterialId == filters.MaterialId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.Outcome))
        {
            query = query.Where(r => r.ComparisonOutcome == filters.Outcome.Trim());
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
            query = query.Where(r => dbContext.CommercialSalesPriceComparisonResults.Any(result =>
                result.ComparisonRunId == r.Id && result.CustomerReference == customerReference));
        }

        var runs = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return runs.Select(MapComparisonSummary).ToList();
    }

    internal static async Task<CommercialSalesPriceComparisonDetailReadModel?> GetComparisonByIdAsync(
        CrevaFactOSDbContext dbContext,
        CommercialIntelligenceOperationContext context,
        Guid comparisonRunId,
        CancellationToken cancellationToken)
    {
        var run = await dbContext.CommercialSalesPriceComparisonRuns.AsNoTracking()
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

    internal static async Task<CommercialSalesPriceWriteResult> EvaluateComparisonAsync(
        CrevaFactOSDbContext dbContext,
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialSalesPriceComparisonCommand command,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateEvaluateComparisonCommand(command);
        if (validationError is not null)
        {
            return CommercialSalesPriceWriteResult.Fail(
                CommercialIntelligenceFailureCodes.ValidationFailed,
                validationError);
        }

        var fingerprint = CommercialSalesPriceIdempotencyFingerprint.ComputeComparisonFingerprint(context, command);
        var existing = await dbContext.CommercialSalesPriceComparisonRuns.AsNoTracking()
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

        var profile = await dbContext.CommercialSalesPriceProfiles
            .SingleOrDefaultAsync(
                p => p.TenantId == context.TenantId
                     && p.FacilityId == context.FacilityId
                     && p.Id == command.ProfileId,
                cancellationToken);

        if (profile is null)
        {
            return CommercialSalesPriceWriteResult.Fail(
                CommercialIntelligenceFailureCodes.NotFound,
                "Sales price profile not found.");
        }

        if (profile.Version != command.ExpectedProfileVersion)
        {
            return CommercialSalesPriceWriteResult.Fail(
                CommercialIntelligenceFailureCodes.ConcurrencyConflict,
                "Sales price profile version is stale.");
        }

        if (!string.Equals(profile.Status, CommercialIntelligenceProfileStatuses.Approved, StringComparison.Ordinal))
        {
            return CommercialSalesPriceWriteResult.Fail(
                CommercialIntelligenceFailureCodes.ValidationFailed,
                "Only approved sales price profiles may execute comparisons.");
        }

        var comparisonType = command.ComparisonType.Trim();
        var currencyCode = command.CurrencyCode.Trim().ToUpperInvariant();
        var normalizedUnit = command.NormalizedUnit.Trim();
        var grossNet = command.PriceBasisGrossNet.Trim();
        var taxInclusion = command.PriceBasisTaxInclusion.Trim();
        var discountInclusion = command.PriceBasisDiscountInclusion.Trim();
        var freightInclusion = command.PriceBasisFreightInclusion.Trim();
        var sourceEvidenceKind = command.SourceEvidenceKind.Trim();
        var customerReference = string.IsNullOrWhiteSpace(command.CustomerReference)
            ? null
            : command.CustomerReference.Trim();

        CommercialSalesPriceComparisonEvaluationOutput evaluation;
        if (string.Equals(comparisonType, CommercialSalesPriceComparisonTypes.TemporalSalesPriceDeviation, StringComparison.Ordinal))
        {
            var currentEvidence = await CommercialSalesPriceEvidenceReader.ReadForComparisonAsync(
                dbContext,
                context,
                profile,
                command.MaterialId,
                command.CurrentWindowStartUtc,
                command.CurrentWindowEndUtc,
                sourceEvidenceKind,
                currencyCode,
                normalizedUnit,
                grossNet,
                taxInclusion,
                discountInclusion,
                freightInclusion,
                customerReference,
                cancellationToken);
            var referenceEvidence = await CommercialSalesPriceEvidenceReader.ReadForComparisonAsync(
                dbContext,
                context,
                profile,
                command.MaterialId,
                command.ReferenceWindowStartUtc!.Value,
                command.ReferenceWindowEndUtc!.Value,
                sourceEvidenceKind,
                currencyCode,
                normalizedUnit,
                grossNet,
                taxInclusion,
                discountInclusion,
                freightInclusion,
                customerReference,
                cancellationToken);

            evaluation = CommercialSalesPriceComparisonEvaluator.EvaluateTemporalDeviation(
                profile,
                normalizedUnit,
                currentEvidence,
                referenceEvidence,
                CommercialSalesPriceEvidenceReader.SummarizeExclusions(currentEvidence.Excluded),
                CommercialSalesPriceEvidenceReader.SummarizeExclusions(referenceEvidence.Excluded));
        }
        else
        {
            var evidence = await CommercialSalesPriceEvidenceReader.ReadForComparisonAsync(
                dbContext,
                context,
                profile,
                command.MaterialId,
                command.CurrentWindowStartUtc,
                command.CurrentWindowEndUtc,
                sourceEvidenceKind,
                currencyCode,
                normalizedUnit,
                grossNet,
                taxInclusion,
                discountInclusion,
                freightInclusion,
                customerReference,
                cancellationToken);

            evaluation = CommercialSalesPriceComparisonEvaluator.EvaluateCustomerPricePosition(
                profile,
                normalizedUnit,
                evidence,
                CommercialSalesPriceEvidenceReader.SummarizeExclusions(evidence.Excluded));
        }

        var now = DateTimeOffset.UtcNow;
        var actor = context.IdentitySubjectId.ToString();
        var run = new CommercialSalesPriceComparisonRun
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            ComparisonReference = await GenerateComparisonReferenceAsync(dbContext, context, cancellationToken),
            ProfileId = profile.Id,
            ProfileVersion = profile.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ComparisonType = comparisonType,
            MaterialId = command.MaterialId,
            CustomerReference = customerReference,
            SourceEvidenceKind = sourceEvidenceKind,
            CurrentWindowStartUtc = command.CurrentWindowStartUtc,
            CurrentWindowEndUtc = command.CurrentWindowEndUtc,
            ReferenceWindowStartUtc = command.ReferenceWindowStartUtc,
            ReferenceWindowEndUtc = command.ReferenceWindowEndUtc,
            CurrencyCode = currencyCode,
            NormalizedUnit = normalizedUnit,
            PriceBasisGrossNet = grossNet,
            PriceBasisTaxInclusion = taxInclusion,
            PriceBasisDiscountInclusion = discountInclusion,
            PriceBasisFreightInclusion = freightInclusion,
            EvidenceStatus = evaluation.EvidenceStatus,
            ComparisonOutcome = evaluation.ComparisonOutcome,
            CurrentMedianUnitPrice = evaluation.CurrentMedianUnitPrice,
            ReferenceMedianUnitPrice = evaluation.ReferenceMedianUnitPrice,
            SignedVarianceRatio = evaluation.SignedVarianceRatio,
            CustomerSpreadRatio = evaluation.CustomerSpreadRatio,
            CandidateObservationCount = evaluation.CandidateObservationCount,
            ComparableObservationCount = evaluation.ComparableObservationCount,
            ExcludedObservationCount = evaluation.ExcludedObservationCount,
            LeadingDriverCode = evaluation.LeadingDriverCode,
            RuleSetVersion = CommercialIntelligenceConstants.SalesComparisonRuleSetVersion,
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
            run.Results.Add(new CommercialSalesPriceComparisonResult
            {
                Id = resultId,
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ComparisonRunId = run.Id,
                CustomerReference = customerResult.CustomerReference,
                ComparableObservationCount = customerResult.ComparableObservationCount,
                CustomerMedianUnitPrice = customerResult.CustomerMedianUnitPrice,
                LowestComparableMedianUnitPrice = customerResult.LowestComparableMedianUnitPrice,
                RelativePricePositionRatio = customerResult.RelativePricePositionRatio,
                PriceRank = customerResult.PriceRank,
                IsTied = customerResult.IsTied,
                PositionState = customerResult.PositionState,
                EvidenceStatus = customerResult.EvidenceStatus,
                CreatedAtUtc = now,
            });
        }

        var orderedDrivers = CommercialSalesPriceComparisonEvaluator.OrderDrivers(evaluation.Drivers);
        for (var index = 0; index < orderedDrivers.Count; index++)
        {
            var driver = orderedDrivers[index];
            run.Drivers.Add(new CommercialSalesPriceComparisonDriver
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
                AffectedObservationCount = driver.AffectedObservationCount,
                SupportingText = driver.SupportingText,
                SourceProvenance = command.SourceProvenance.Trim(),
                SortOrder = index + 1,
                IsLeadingDriver = index == 0,
                CreatedAtUtc = now,
            });
        }

        dbContext.CommercialSalesPriceComparisonRuns.Add(run);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueIdempotencyViolation(ex))
        {
            var replay = await dbContext.CommercialSalesPriceComparisonRuns.AsNoTracking()
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

        return CommercialSalesPriceWriteResult.Success(run.Id, 0, comparison: MapComparisonDetail(run));
    }

    private static async Task<string> GenerateComparisonReferenceAsync(
        CrevaFactOSDbContext dbContext,
        CommercialIntelligenceOperationContext context,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var reference = $"CSALE-CMP-{suffix}";
            var exists = await dbContext.CommercialSalesPriceComparisonRuns.AsNoTracking()
                .AnyAsync(r => r.TenantId == context.TenantId && r.ComparisonReference == reference, cancellationToken);
            if (!exists)
            {
                return reference;
            }
        }

        return $"CSALE-CMP-{Guid.NewGuid():N}".ToUpperInvariant();
    }

    private static CommercialSalesPriceWriteResult ResolveIdempotentComparisonReplay(
        CommercialSalesPriceComparisonRun existing,
        string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(existing.IdempotencyPayloadFingerprint))
        {
            return CommercialSalesPriceWriteResult.Fail(
                CommercialIntelligenceFailureCodes.IdempotencyLegacyUnverifiable,
                "Existing comparison idempotency fingerprint is unavailable.");
        }

        if (!string.Equals(existing.IdempotencyPayloadFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return CommercialSalesPriceWriteResult.Fail(
                CommercialIntelligenceFailureCodes.IdempotencyConflict,
                "Idempotency key was already used with a different payload.");
        }

        return CommercialSalesPriceWriteResult.Success(
            existing.Id,
            0,
            idempotentReplay: true,
            comparison: MapComparisonDetail(existing));
    }

    private static string? ValidateEvaluateComparisonCommand(EvaluateCommercialSalesPriceComparisonCommand command)
    {
        if (command.CurrentWindowEndUtc <= command.CurrentWindowStartUtc)
        {
            return "Current window end must be greater than current window start.";
        }

        if (!CommercialSalesPriceComparisonTypes.All.Contains(command.ComparisonType.Trim()))
        {
            return "Comparison type is invalid.";
        }

        if (string.Equals(command.ComparisonType.Trim(), CommercialSalesPriceComparisonTypes.TemporalSalesPriceDeviation, StringComparison.Ordinal))
        {
            if (!command.ReferenceWindowStartUtc.HasValue || !command.ReferenceWindowEndUtc.HasValue)
            {
                return "Reference window start and end are required for temporal comparisons.";
            }

            if (command.ReferenceWindowEndUtc.Value <= command.ReferenceWindowStartUtc.Value)
            {
                return "Reference window end must be greater than reference window start.";
            }

            if (WindowsOverlap(
                    command.CurrentWindowStartUtc,
                    command.CurrentWindowEndUtc,
                    command.ReferenceWindowStartUtc.Value,
                    command.ReferenceWindowEndUtc.Value))
            {
                return "Current and reference windows must not overlap.";
            }
        }
        else if (command.ReferenceWindowStartUtc.HasValue || command.ReferenceWindowEndUtc.HasValue)
        {
            return "Reference window is only valid for temporal comparisons.";
        }

        if (string.IsNullOrWhiteSpace(command.SourceEvidenceKind))
        {
            return "Source evidence kind is required.";
        }

        if (!CommercialSalesPriceEvidenceKinds.All.Contains(command.SourceEvidenceKind.Trim()))
        {
            return "Source evidence kind is invalid.";
        }

        if (string.IsNullOrWhiteSpace(command.CurrencyCode))
        {
            return "Currency code is required.";
        }

        if (string.IsNullOrWhiteSpace(command.NormalizedUnit))
        {
            return "Normalized unit is required.";
        }

        if (string.IsNullOrWhiteSpace(command.PriceBasisGrossNet)
            || !CommercialSalesPriceGrossNetBasis.All.Contains(command.PriceBasisGrossNet.Trim()))
        {
            return "Price basis gross/net is invalid.";
        }

        if (string.IsNullOrWhiteSpace(command.SourceProvenance))
        {
            return "Source provenance is required.";
        }

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return "Idempotency key is required.";
        }

        return null;
    }

    private static bool WindowsOverlap(
        DateTimeOffset currentStart,
        DateTimeOffset currentEnd,
        DateTimeOffset referenceStart,
        DateTimeOffset referenceEnd) =>
        currentStart < referenceEnd && referenceStart < currentEnd;

    private static bool IsUniqueIdempotencyViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static CommercialSalesPriceComparisonSummaryReadModel MapComparisonSummary(
        CommercialSalesPriceComparisonRun run) =>
        new(
            run.Id,
            run.ComparisonReference,
            run.ProfileId,
            run.Profile?.ProfileCode ?? string.Empty,
            run.ComparisonType,
            run.MaterialId,
            run.CustomerReference,
            run.ComparisonOutcome,
            run.CurrentMedianUnitPrice,
            run.ReferenceMedianUnitPrice,
            run.SignedVarianceRatio,
            run.CustomerSpreadRatio,
            run.CandidateObservationCount,
            run.ComparableObservationCount,
            run.ExcludedObservationCount,
            run.CurrencyCode,
            run.NormalizedUnit,
            run.CurrentWindowStartUtc,
            run.CurrentWindowEndUtc,
            run.ReferenceWindowStartUtc,
            run.ReferenceWindowEndUtc,
            run.LeadingDriverCode,
            run.RuleSetVersion,
            run.EvaluatedAtUtc,
            run.CreatedAtUtc,
            IsPilotComparison(run.Id));

    internal static CommercialSalesPriceComparisonDetailReadModel MapComparisonDetail(
        CommercialSalesPriceComparisonRun run) =>
        new(
            run.Id,
            run.ComparisonReference,
            run.ProfileId,
            run.Profile?.ProfileCode ?? string.Empty,
            run.ProfileVersion,
            run.ComparisonType,
            run.MaterialId,
            run.CustomerReference,
            run.SourceEvidenceKind,
            run.CurrentMedianUnitPrice,
            run.ReferenceMedianUnitPrice,
            run.SignedVarianceRatio,
            run.CustomerSpreadRatio,
            run.CandidateObservationCount,
            run.ComparableObservationCount,
            run.ExcludedObservationCount,
            run.CurrencyCode,
            run.NormalizedUnit,
            run.CurrentWindowStartUtc,
            run.CurrentWindowEndUtc,
            run.ReferenceWindowStartUtc,
            run.ReferenceWindowEndUtc,
            run.PriceBasisGrossNet,
            run.PriceBasisTaxInclusion,
            run.PriceBasisDiscountInclusion,
            run.PriceBasisFreightInclusion,
            run.EvidenceStatus,
            run.ComparisonOutcome,
            run.LeadingDriverCode,
            run.RuleSetVersion,
            run.SourceProvenance,
            run.EvaluatedAtUtc,
            run.CreatedAtUtc,
            run.Results
                .OrderBy(r => r.PriceRank)
                .ThenByDescending(r => r.ComparableObservationCount)
                .ThenBy(r => r.CustomerReference, StringComparer.Ordinal)
                .Select(r => new CommercialSalesPriceComparisonResultReadModel(
                    r.Id,
                    r.CustomerReference,
                    r.ComparableObservationCount,
                    r.CustomerMedianUnitPrice,
                    r.LowestComparableMedianUnitPrice,
                    r.RelativePricePositionRatio,
                    r.PriceRank,
                    r.IsTied,
                    r.PositionState,
                    r.EvidenceStatus))
                .ToList(),
            run.Drivers
                .OrderBy(d => d.SortOrder)
                .Select(d => new CommercialSalesPriceComparisonDriverReadModel(
                    d.Id,
                    d.ComparisonResultId,
                    d.ReasonCode,
                    d.Severity,
                    d.ActualValue,
                    d.ReferenceValue,
                    d.ThresholdValue,
                    d.Unit,
                    d.NormalizedVariance,
                    d.AffectedObservationCount,
                    d.SupportingText,
                    d.SortOrder,
                    d.IsLeadingDriver))
                .ToList(),
            IsPilotComparison(run.Id));

    private static bool IsPilotComparison(Guid id) =>
        DevelopmentPilotIds.CommercialSalesPriceComparisonPilotRunIds.Contains(id);
}
