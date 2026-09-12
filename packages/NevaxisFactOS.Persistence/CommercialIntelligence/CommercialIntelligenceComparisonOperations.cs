using NevaxisFactOS.Persistence.Development;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace NevaxisFactOS.Persistence.CommercialIntelligence;

internal static class CommercialIntelligenceComparisonOperations
{
    internal static async Task<IReadOnlyList<CommercialPurchasePriceComparisonSummaryReadModel>> ListComparisonsAsync(
        NevaxisFactOSDbContext dbContext,
        CommercialIntelligenceOperationContext context,
        CommercialPurchasePriceComparisonListFilters filters,
        CancellationToken cancellationToken)
    {
        var page = filters.Page < 1 ? 1 : filters.Page;
        var pageSize = filters.PageSize is < 1 or > 100 ? 25 : filters.PageSize;

        var query = dbContext.CommercialPurchasePriceComparisonRuns.AsNoTracking()
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

        if (!string.IsNullOrWhiteSpace(filters.SupplierReference))
        {
            var supplierReference = filters.SupplierReference.Trim();
            query = query.Where(r => dbContext.CommercialPurchasePriceComparisonResults.Any(result =>
                result.ComparisonRunId == r.Id && result.SupplierReference == supplierReference));
        }

        var runs = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return runs.Select(MapComparisonSummary).ToList();
    }

    internal static async Task<CommercialPurchasePriceComparisonDetailReadModel?> GetComparisonByIdAsync(
        NevaxisFactOSDbContext dbContext,
        CommercialIntelligenceOperationContext context,
        Guid comparisonRunId,
        CancellationToken cancellationToken)
    {
        var run = await dbContext.CommercialPurchasePriceComparisonRuns.AsNoTracking()
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

    internal static async Task<CommercialIntelligenceWriteResult> EvaluateComparisonAsync(
        NevaxisFactOSDbContext dbContext,
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialPurchasePriceComparisonCommand command,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateEvaluateComparisonCommand(command);
        if (validationError is not null)
        {
            return CommercialIntelligenceWriteResult.Fail(
                CommercialIntelligenceFailureCodes.ValidationFailed,
                validationError);
        }

        var fingerprint = CommercialIntelligenceIdempotencyFingerprint.ComputeComparisonFingerprint(context, command);
        var existing = await dbContext.CommercialPurchasePriceComparisonRuns.AsNoTracking()
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

        var profile = await dbContext.CommercialPurchasePriceProfiles
            .SingleOrDefaultAsync(
                p => p.TenantId == context.TenantId
                     && p.FacilityId == context.FacilityId
                     && p.Id == command.ProfileId,
                cancellationToken);

        if (profile is null)
        {
            return CommercialIntelligenceWriteResult.Fail(
                CommercialIntelligenceFailureCodes.NotFound,
                "Purchase price profile not found.");
        }

        if (profile.Version != command.ExpectedProfileVersion)
        {
            return CommercialIntelligenceWriteResult.Fail(
                CommercialIntelligenceFailureCodes.ConcurrencyConflict,
                "Purchase price profile version is stale.");
        }

        if (!string.Equals(profile.Status, CommercialIntelligenceProfileStatuses.Approved, StringComparison.Ordinal))
        {
            return CommercialIntelligenceWriteResult.Fail(
                CommercialIntelligenceFailureCodes.ValidationFailed,
                "Only approved purchase price profiles may execute comparisons.");
        }

        var comparisonType = command.ComparisonType.Trim();
        var currencyCode = command.CurrencyCode.Trim().ToUpperInvariant();
        var normalizedUnit = command.NormalizedUnit.Trim();
        var taxInclusion = command.PriceBasisTaxInclusion.Trim();
        var discountInclusion = command.PriceBasisDiscountInclusion.Trim();
        var freightInclusion = command.PriceBasisFreightInclusion.Trim();

        CommercialPurchasePriceComparisonEvaluationOutput evaluation;
        if (string.Equals(comparisonType, CommercialIntelligenceComparisonTypes.TemporalPriceDeviation, StringComparison.Ordinal))
        {
            var currentEvidence = await CommercialIntelligencePriceEvidenceReader.ReadForComparisonAsync(
                dbContext,
                context,
                profile,
                command.MaterialId,
                command.CurrentWindowStartUtc,
                command.CurrentWindowEndUtc,
                currencyCode,
                normalizedUnit,
                taxInclusion,
                discountInclusion,
                freightInclusion,
                cancellationToken);
            var referenceEvidence = await CommercialIntelligencePriceEvidenceReader.ReadForComparisonAsync(
                dbContext,
                context,
                profile,
                command.MaterialId,
                command.ReferenceWindowStartUtc!.Value,
                command.ReferenceWindowEndUtc!.Value,
                currencyCode,
                normalizedUnit,
                taxInclusion,
                discountInclusion,
                freightInclusion,
                cancellationToken);

            evaluation = CommercialPurchasePriceComparisonEvaluator.EvaluateTemporalDeviation(
                profile,
                normalizedUnit,
                currentEvidence,
                referenceEvidence,
                CommercialIntelligencePriceEvidenceReader.SummarizeExclusions(currentEvidence.Excluded),
                CommercialIntelligencePriceEvidenceReader.SummarizeExclusions(referenceEvidence.Excluded));
        }
        else
        {
            var evidence = await CommercialIntelligencePriceEvidenceReader.ReadForComparisonAsync(
                dbContext,
                context,
                profile,
                command.MaterialId,
                command.CurrentWindowStartUtc,
                command.CurrentWindowEndUtc,
                currencyCode,
                normalizedUnit,
                taxInclusion,
                discountInclusion,
                freightInclusion,
                cancellationToken);

            evaluation = CommercialPurchasePriceComparisonEvaluator.EvaluateSupplierPricePosition(
                profile,
                normalizedUnit,
                evidence,
                CommercialIntelligencePriceEvidenceReader.SummarizeExclusions(evidence.Excluded));
        }

        var now = DateTimeOffset.UtcNow;
        var actor = context.IdentitySubjectId.ToString();
        var run = new CommercialPurchasePriceComparisonRun
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            ComparisonReference = await GenerateComparisonReferenceAsync(dbContext, context, cancellationToken),
            ProfileId = profile.Id,
            ProfileVersion = profile.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ComparisonType = comparisonType,
            MaterialId = command.MaterialId,
            CurrentWindowStartUtc = command.CurrentWindowStartUtc,
            CurrentWindowEndUtc = command.CurrentWindowEndUtc,
            ReferenceWindowStartUtc = command.ReferenceWindowStartUtc,
            ReferenceWindowEndUtc = command.ReferenceWindowEndUtc,
            CurrencyCode = currencyCode,
            NormalizedUnit = normalizedUnit,
            PriceBasisTaxInclusion = taxInclusion,
            PriceBasisDiscountInclusion = discountInclusion,
            PriceBasisFreightInclusion = freightInclusion,
            EvidenceStatus = evaluation.EvidenceStatus,
            ComparisonOutcome = evaluation.ComparisonOutcome,
            CurrentMedianUnitPrice = evaluation.CurrentMedianUnitPrice,
            ReferenceMedianUnitPrice = evaluation.ReferenceMedianUnitPrice,
            SignedVarianceRatio = evaluation.SignedVarianceRatio,
            SupplierSpreadRatio = evaluation.SupplierSpreadRatio,
            ComparableObservationCount = evaluation.ComparableObservationCount,
            ExcludedObservationCount = evaluation.ExcludedObservationCount,
            LeadingDriverCode = evaluation.LeadingDriverCode,
            RuleSetVersion = CommercialIntelligenceConstants.ComparisonRuleSetVersion,
            IdempotencyKey = command.IdempotencyKey.Trim(),
            IdempotencyPayloadFingerprint = fingerprint,
            SourceProvenance = command.SourceProvenance.Trim(),
            EvaluatedAtUtc = now,
            CreatedAtUtc = now,
            CreatedBy = actor,
        };

        var resultIdBySupplier = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var supplierResult in evaluation.SupplierResults)
        {
            var resultId = Guid.NewGuid();
            resultIdBySupplier[supplierResult.SupplierReference] = resultId;
            run.Results.Add(new CommercialPurchasePriceComparisonResult
            {
                Id = resultId,
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ComparisonRunId = run.Id,
                SupplierReference = supplierResult.SupplierReference,
                ComparableObservationCount = supplierResult.ComparableObservationCount,
                SupplierMedianUnitPrice = supplierResult.SupplierMedianUnitPrice,
                LowestComparableMedianUnitPrice = supplierResult.LowestComparableMedianUnitPrice,
                RelativePricePositionRatio = supplierResult.RelativePricePositionRatio,
                PriceRank = supplierResult.PriceRank,
                IsTied = supplierResult.IsTied,
                PositionState = supplierResult.PositionState,
                EvidenceStatus = supplierResult.EvidenceStatus,
                CreatedAtUtc = now,
            });
        }

        var orderedDrivers = CommercialPurchasePriceComparisonEvaluator.OrderDrivers(evaluation.Drivers);
        for (var index = 0; index < orderedDrivers.Count; index++)
        {
            var driver = orderedDrivers[index];
            Guid? comparisonResultId = null;
            if (driver.ComparisonResultId.HasValue)
            {
                comparisonResultId = driver.ComparisonResultId;
            }

            run.Drivers.Add(new CommercialPurchasePriceComparisonDriver
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ComparisonRunId = run.Id,
                ComparisonResultId = comparisonResultId,
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

        dbContext.CommercialPurchasePriceComparisonRuns.Add(run);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueIdempotencyViolation(ex))
        {
            var replay = await dbContext.CommercialPurchasePriceComparisonRuns.AsNoTracking()
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

        return CommercialIntelligenceWriteResult.Success(run.Id, 0, comparison: MapComparisonDetail(run));
    }

    private static async Task<string> GenerateComparisonReferenceAsync(
        NevaxisFactOSDbContext dbContext,
        CommercialIntelligenceOperationContext context,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var reference = $"CI-CMP-{suffix}";
            var exists = await dbContext.CommercialPurchasePriceComparisonRuns.AsNoTracking()
                .AnyAsync(r => r.TenantId == context.TenantId && r.ComparisonReference == reference, cancellationToken);
            if (!exists)
            {
                return reference;
            }
        }

        return $"CI-CMP-{Guid.NewGuid():N}".ToUpperInvariant();
    }

    private static CommercialIntelligenceWriteResult ResolveIdempotentComparisonReplay(
        CommercialPurchasePriceComparisonRun existing,
        string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(existing.IdempotencyPayloadFingerprint))
        {
            return CommercialIntelligenceWriteResult.Fail(
                CommercialIntelligenceFailureCodes.IdempotencyLegacyUnverifiable,
                "Existing comparison idempotency fingerprint is unavailable.");
        }

        if (!string.Equals(existing.IdempotencyPayloadFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return CommercialIntelligenceWriteResult.Fail(
                CommercialIntelligenceFailureCodes.IdempotencyConflict,
                "Idempotency key was already used with a different payload.");
        }

        return CommercialIntelligenceWriteResult.Success(
            existing.Id,
            0,
            idempotentReplay: true,
            comparison: MapComparisonDetail(existing));
    }

    private static string? ValidateEvaluateComparisonCommand(EvaluateCommercialPurchasePriceComparisonCommand command)
    {
        if (command.CurrentWindowEndUtc <= command.CurrentWindowStartUtc)
        {
            return "Current window end must be greater than current window start.";
        }

        if (!CommercialIntelligenceComparisonTypes.All.Contains(command.ComparisonType.Trim()))
        {
            return "Comparison type is invalid.";
        }

        if (string.Equals(command.ComparisonType.Trim(), CommercialIntelligenceComparisonTypes.TemporalPriceDeviation, StringComparison.Ordinal))
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

        if (string.IsNullOrWhiteSpace(command.CurrencyCode))
        {
            return "Currency code is required.";
        }

        if (string.IsNullOrWhiteSpace(command.NormalizedUnit))
        {
            return "Normalized unit is required.";
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

    private static CommercialPurchasePriceComparisonSummaryReadModel MapComparisonSummary(
        CommercialPurchasePriceComparisonRun run) =>
        new(
            run.Id,
            run.ComparisonReference,
            run.ProfileId,
            run.Profile?.ProfileCode ?? string.Empty,
            run.ComparisonType,
            run.MaterialId,
            run.ComparisonOutcome,
            run.CurrentMedianUnitPrice,
            run.ReferenceMedianUnitPrice,
            run.SignedVarianceRatio,
            run.SupplierSpreadRatio,
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

    internal static CommercialPurchasePriceComparisonDetailReadModel MapComparisonDetail(
        CommercialPurchasePriceComparisonRun run) =>
        new(
            run.Id,
            run.ComparisonReference,
            run.ProfileId,
            run.Profile?.ProfileCode ?? string.Empty,
            run.ProfileVersion,
            run.ComparisonType,
            run.MaterialId,
            run.CurrentMedianUnitPrice,
            run.ReferenceMedianUnitPrice,
            run.SignedVarianceRatio,
            run.SupplierSpreadRatio,
            run.ComparableObservationCount,
            run.ExcludedObservationCount,
            run.CurrencyCode,
            run.NormalizedUnit,
            run.CurrentWindowStartUtc,
            run.CurrentWindowEndUtc,
            run.ReferenceWindowStartUtc,
            run.ReferenceWindowEndUtc,
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
                .ThenBy(r => r.SupplierReference, StringComparer.Ordinal)
                .Select(r => new CommercialPurchasePriceComparisonResultReadModel(
                    r.Id,
                    r.SupplierReference,
                    r.ComparableObservationCount,
                    r.SupplierMedianUnitPrice,
                    r.LowestComparableMedianUnitPrice,
                    r.RelativePricePositionRatio,
                    r.PriceRank,
                    r.IsTied,
                    r.PositionState,
                    r.EvidenceStatus))
                .ToList(),
            run.Drivers
                .OrderBy(d => d.SortOrder)
                .Select(d => new CommercialPurchasePriceComparisonDriverReadModel(
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
        DevelopmentPilotIds.CommercialIntelligenceComparisonPilotRunIds.Contains(id);
}
