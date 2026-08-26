using CrevaFactOS.Persistence.Development;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CrevaFactOS.Persistence.CommercialIntelligence;

public sealed class CommercialQuotationRiskRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : ICommercialQuotationRiskRepository
{
    public Task<CommercialQuotationRiskWorkspaceReadModel> GetWorkspaceAsync(
        CommercialIntelligenceOperationContext context,
        bool canManage,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new CommercialQuotationRiskWorkspaceReadModel(
            CommercialIntelligenceConstants.QuotationRiskRuleSetVersion,
            CommercialIntelligenceConstants.QuotationRiskDevelopmentPilotProvenance,
            CommercialIntelligenceConstants.DefaultMinimumComparableObservations,
            CommercialIntelligenceConstants.DefaultQuotationRiskMaximumNegativeVarianceRatio,
            CommercialIntelligenceConstants.DefaultQuotationRiskMaximumPositiveVarianceRatio,
            CommercialIntelligenceConstants.DefaultQuotationRiskMaximumPaymentTermDays,
            CommercialIntelligenceConstants.DefaultQuotationRiskExpiryWarningDays,
            RealQuotationMappingComplete: false,
            true,
            canManage));

    public Task<IReadOnlyList<CommercialQuotationRiskProfileReadModel>> ListProfilesAsync(
        CommercialIntelligenceOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var profiles = await dbContext.CommercialQuotationRiskProfiles.AsNoTracking()
                    .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId)
                    .OrderBy(p => p.ProfileCode)
                    .ToListAsync(ct);

                return (IReadOnlyList<CommercialQuotationRiskProfileReadModel>)profiles.Select(MapProfile).ToList();
            },
            cancellationToken);

    public Task<CommercialQuotationRiskProfileReadModel?> GetProfileByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid profileId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var profile = await dbContext.CommercialQuotationRiskProfiles.AsNoTracking()
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId
                             && p.Id == profileId,
                        ct);

                return profile is null ? null : MapProfile(profile);
            },
            cancellationToken);

    public Task<CommercialQuotationRiskWriteResult> CreateProfileAsync(
        CommercialIntelligenceOperationContext context,
        CreateCommercialQuotationRiskProfileCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateCreateProfileCommand(command);
                if (validationError is not null)
                {
                    return CommercialQuotationRiskWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        validationError);
                }

                var fingerprint = CommercialQuotationRiskIdempotencyFingerprint.ComputeProfileFingerprint(context, command);
                var existing = await dbContext.CommercialQuotationRiskProfiles.AsNoTracking()
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId && p.CreateIdempotencyKey == command.IdempotencyKey.Trim(),
                        ct);

                if (existing is not null)
                {
                    return ResolveIdempotentProfileReplay(existing, fingerprint);
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var profile = new CommercialQuotationRiskProfile
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    ProfileCode = command.ProfileCode.Trim(),
                    Name = command.Name.Trim(),
                    Status = CommercialIntelligenceProfileStatuses.Draft,
                    ReferenceWindowDurationDays = command.ReferenceWindowDurationDays,
                    MinimumComparableObservations = command.MinimumComparableObservations,
                    MaximumNegativePriceVarianceRatio = command.MaximumNegativePriceVarianceRatio,
                    MaximumPositivePriceVarianceRatio = command.MaximumPositivePriceVarianceRatio,
                    MaximumPaymentTermDays = command.MaximumPaymentTermDays,
                    ExpiryWarningDays = command.ExpiryWarningDays,
                    MaximumEvidenceAgeDays = command.MaximumEvidenceAgeDays,
                    AllowedEvidenceStatuses = command.AllowedEvidenceStatuses.Trim(),
                    AllowedSalesEvidenceKinds = command.AllowedSalesEvidenceKinds.Trim(),
                    RequiredTaxInclusion = command.RequiredTaxInclusion.Trim(),
                    RequiredDiscountInclusion = command.RequiredDiscountInclusion.Trim(),
                    RequiredFreightInclusion = command.RequiredFreightInclusion.Trim(),
                    RequiredGrossNetBasis = command.RequiredGrossNetBasis.Trim(),
                    RuleSetVersion = CommercialIntelligenceConstants.QuotationRiskRuleSetVersion,
                    SourceProvenance = command.SourceProvenance.Trim(),
                    CreateIdempotencyKey = command.IdempotencyKey.Trim(),
                    CreateIdempotencyPayloadFingerprint = fingerprint,
                    Version = 1,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedBy = actor,
                    UpdatedBy = actor,
                };

                dbContext.CommercialQuotationRiskProfiles.Add(profile);
                await dbContext.SaveChangesAsync(ct);
                return CommercialQuotationRiskWriteResult.Success(profile.Id, profile.Version);
            },
            cancellationToken);

    public Task<CommercialQuotationRiskWriteResult> UpdateProfileAsync(
        CommercialIntelligenceOperationContext context,
        UpdateCommercialQuotationRiskProfileCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateUpdateProfileCommand(command);
                if (validationError is not null)
                {
                    return CommercialQuotationRiskWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        validationError);
                }

                var profile = await dbContext.CommercialQuotationRiskProfiles
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId
                             && p.Id == command.ProfileId,
                        ct);

                if (profile is null)
                {
                    return CommercialQuotationRiskWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.NotFound,
                        "Quotation risk profile not found.");
                }

                if (profile.Version != command.ExpectedVersion)
                {
                    return CommercialQuotationRiskWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ConcurrencyConflict,
                        "Quotation risk profile version is stale.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                profile.Name = command.Name.Trim();
                profile.Status = command.Status.Trim();
                profile.ReferenceWindowDurationDays = command.ReferenceWindowDurationDays;
                profile.MinimumComparableObservations = command.MinimumComparableObservations;
                profile.MaximumNegativePriceVarianceRatio = command.MaximumNegativePriceVarianceRatio;
                profile.MaximumPositivePriceVarianceRatio = command.MaximumPositivePriceVarianceRatio;
                profile.MaximumPaymentTermDays = command.MaximumPaymentTermDays;
                profile.ExpiryWarningDays = command.ExpiryWarningDays;
                profile.MaximumEvidenceAgeDays = command.MaximumEvidenceAgeDays;
                profile.AllowedEvidenceStatuses = command.AllowedEvidenceStatuses.Trim();
                profile.AllowedSalesEvidenceKinds = command.AllowedSalesEvidenceKinds.Trim();
                profile.RequiredTaxInclusion = command.RequiredTaxInclusion.Trim();
                profile.RequiredDiscountInclusion = command.RequiredDiscountInclusion.Trim();
                profile.RequiredFreightInclusion = command.RequiredFreightInclusion.Trim();
                profile.RequiredGrossNetBasis = command.RequiredGrossNetBasis.Trim();
                profile.SourceProvenance = command.SourceProvenance.Trim();
                profile.UpdatedAtUtc = now;
                profile.UpdatedBy = actor;
                profile.Version += 1;

                await dbContext.SaveChangesAsync(ct);
                return CommercialQuotationRiskWriteResult.Success(profile.Id, profile.Version);
            },
            cancellationToken);

    public Task<IReadOnlyList<CommercialQuotationRiskAssessmentSummaryReadModel>> ListAssessmentsAsync(
        CommercialIntelligenceOperationContext context,
        CommercialQuotationRiskAssessmentListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var page = filters.Page < 1 ? 1 : filters.Page;
                var pageSize = filters.PageSize is < 1 or > 100 ? 25 : filters.PageSize;

                var query = dbContext.CommercialQuotationRiskAssessments.AsNoTracking()
                    .Include(a => a.Profile)
                    .Where(a => a.TenantId == context.TenantId && a.FacilityId == context.FacilityId);

                if (filters.MaterialId.HasValue)
                {
                    query = query.Where(a => a.MaterialId == filters.MaterialId.Value);
                }

                if (!string.IsNullOrWhiteSpace(filters.Outcome))
                {
                    query = query.Where(a => a.Outcome == filters.Outcome.Trim());
                }

                var assessments = await query
                    .OrderByDescending(a => a.CreatedAtUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                return (IReadOnlyList<CommercialQuotationRiskAssessmentSummaryReadModel>)assessments
                    .Select(MapAssessmentSummary)
                    .ToList();
            },
            cancellationToken);

    public Task<CommercialQuotationRiskAssessmentDetailReadModel?> GetAssessmentByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var assessment = await dbContext.CommercialQuotationRiskAssessments.AsNoTracking()
                    .Include(a => a.Profile)
                    .Include(a => a.Drivers)
                    .SingleOrDefaultAsync(
                        a => a.TenantId == context.TenantId
                             && a.FacilityId == context.FacilityId
                             && a.Id == assessmentId,
                        ct);

                return assessment is null ? null : MapAssessmentDetail(assessment);
            },
            cancellationToken);

    public Task<CommercialQuotationRiskWriteResult> EvaluateAssessmentAsync(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialQuotationRiskAssessmentCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateEvaluateCommand(command);
                if (validationError is not null)
                {
                    return CommercialQuotationRiskWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        validationError);
                }

                var fingerprint = CommercialQuotationRiskIdempotencyFingerprint.ComputeAssessmentFingerprint(context, command);
                var existing = await dbContext.CommercialQuotationRiskAssessments.AsNoTracking()
                    .Include(a => a.Profile)
                    .Include(a => a.Drivers)
                    .SingleOrDefaultAsync(
                        a => a.TenantId == context.TenantId && a.IdempotencyKey == command.IdempotencyKey.Trim(),
                        ct);

                if (existing is not null)
                {
                    return ResolveIdempotentAssessmentReplay(existing, fingerprint);
                }

                var profile = await dbContext.CommercialQuotationRiskProfiles
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId
                             && p.Id == command.ProfileId,
                        ct);

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
                        "Only approved quotation risk profiles may execute assessments.");
                }

                var quotationEvidence = await dbContext.CommercialQuotationRiskEvidenceObservations.AsNoTracking()
                    .SingleOrDefaultAsync(
                        e => e.TenantId == context.TenantId
                             && e.FacilityId == context.FacilityId
                             && e.Id == command.EvidenceObservationId,
                        ct);

                if (quotationEvidence is null)
                {
                    return CommercialQuotationRiskWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.NotFound,
                        "Quotation evidence observation not found.");
                }

                if (quotationEvidence.QuotedUnitPrice <= 0m)
                {
                    return CommercialQuotationRiskWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        "Quoted unit price must be positive.");
                }

                var salesEvidence = await CommercialQuotationRiskSalesEvidenceReader.ReadAsync(
                    dbContext,
                    context,
                    profile,
                    quotationEvidence,
                    command.ReferenceWindowStartUtc,
                    command.ReferenceWindowEndUtc,
                    ct);
                var exclusionCounts = CommercialQuotationRiskSalesEvidenceReader.SummarizeExclusions(salesEvidence.Excluded);
                var now = DateTimeOffset.UtcNow;
                var evaluation = CommercialQuotationRiskEvaluator.Evaluate(
                    profile,
                    quotationEvidence,
                    salesEvidence,
                    exclusionCounts,
                    now);

                var actor = context.IdentitySubjectId.ToString();
                var assessment = new CommercialQuotationRiskAssessment
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    AssessmentReference = await GenerateAssessmentReferenceAsync(context, ct),
                    ProfileId = profile.Id,
                    ProfileVersion = profile.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    EvidenceObservationId = quotationEvidence.Id,
                    QuotationReference = quotationEvidence.QuotationReference,
                    CustomerReference = quotationEvidence.CustomerReference,
                    MaterialId = quotationEvidence.MaterialId,
                    ReferenceWindowStartUtc = command.ReferenceWindowStartUtc,
                    ReferenceWindowEndUtc = command.ReferenceWindowEndUtc,
                    ReferenceMedianUnitPrice = evaluation.ReferenceMedianUnitPrice,
                    QuotedComparableUnitPrice = quotationEvidence.QuotedUnitPrice,
                    SignedPriceVarianceRatio = evaluation.SignedPriceVarianceRatio,
                    CandidateObservationCount = evaluation.CandidateObservationCount,
                    ComparableObservationCount = evaluation.ComparableObservationCount,
                    ExcludedObservationCount = evaluation.ExcludedObservationCount,
                    PaymentTermDays = quotationEvidence.PaymentTermDays,
                    RemainingValidityDays = evaluation.RemainingValidityDays,
                    CurrencyCode = quotationEvidence.CurrencyCode,
                    UnitOfMeasure = quotationEvidence.UnitOfMeasure,
                    GrossNetBasis = quotationEvidence.GrossNetBasis,
                    TaxBasis = quotationEvidence.TaxBasis,
                    DiscountBasis = quotationEvidence.DiscountBasis,
                    FreightBasis = quotationEvidence.FreightBasis,
                    EvidenceStatus = evaluation.EvidenceStatus,
                    Outcome = evaluation.Outcome,
                    LeadingDriverCode = evaluation.LeadingDriverCode,
                    RuleSetVersion = CommercialIntelligenceConstants.QuotationRiskRuleSetVersion,
                    IdempotencyKey = command.IdempotencyKey.Trim(),
                    IdempotencyPayloadFingerprint = fingerprint,
                    SourceProvenance = command.SourceProvenance.Trim(),
                    EvaluatedAtUtc = now,
                    CreatedAtUtc = now,
                    CreatedBy = actor,
                };

                var orderedDrivers = CommercialQuotationRiskEvaluator.OrderDrivers(evaluation.Drivers);
                for (var index = 0; index < orderedDrivers.Count; index++)
                {
                    var driver = orderedDrivers[index];
                    assessment.Drivers.Add(new CommercialQuotationRiskAssessmentDriver
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        AssessmentId = assessment.Id,
                        ReasonCode = driver.ReasonCode,
                        Severity = driver.Severity,
                        ActualValue = driver.ActualValue,
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

                dbContext.CommercialQuotationRiskAssessments.Add(assessment);
                try
                {
                    await dbContext.SaveChangesAsync(ct);
                }
                catch (DbUpdateException ex) when (IsUniqueIdempotencyViolation(ex))
                {
                    var replay = await dbContext.CommercialQuotationRiskAssessments.AsNoTracking()
                        .Include(a => a.Profile)
                        .Include(a => a.Drivers)
                        .SingleOrDefaultAsync(
                            a => a.TenantId == context.TenantId && a.IdempotencyKey == command.IdempotencyKey.Trim(),
                            ct);

                    if (replay is null)
                    {
                        throw;
                    }

                    return ResolveIdempotentAssessmentReplay(replay, fingerprint);
                }

                var detail = MapAssessmentDetail(assessment);
                return CommercialQuotationRiskWriteResult.Success(assessment.Id, 0, assessment: detail);
            },
            cancellationToken);

    private static bool IsUniqueIdempotencyViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: "23505" };

    private async Task<string> GenerateAssessmentReferenceAsync(
        CommercialIntelligenceOperationContext context,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var reference = $"CI-QR-{suffix}";
            var exists = await dbContext.CommercialQuotationRiskAssessments.AsNoTracking()
                .AnyAsync(a => a.TenantId == context.TenantId && a.AssessmentReference == reference, cancellationToken);
            if (!exists)
            {
                return reference;
            }
        }

        return $"CI-QR-{Guid.NewGuid():N}".ToUpperInvariant();
    }

    private static CommercialQuotationRiskWriteResult ResolveIdempotentProfileReplay(
        CommercialQuotationRiskProfile existing,
        string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(existing.CreateIdempotencyPayloadFingerprint))
        {
            return CommercialQuotationRiskWriteResult.Fail(
                CommercialIntelligenceFailureCodes.IdempotencyLegacyUnverifiable,
                "Existing profile idempotency fingerprint is unavailable.");
        }

        if (!string.Equals(existing.CreateIdempotencyPayloadFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return CommercialQuotationRiskWriteResult.Fail(
                CommercialIntelligenceFailureCodes.IdempotencyConflict,
                "Idempotency key was already used with a different payload.");
        }

        return CommercialQuotationRiskWriteResult.Success(existing.Id, existing.Version, idempotentReplay: true);
    }

    private static CommercialQuotationRiskWriteResult ResolveIdempotentAssessmentReplay(
        CommercialQuotationRiskAssessment existing,
        string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(existing.IdempotencyPayloadFingerprint))
        {
            return CommercialQuotationRiskWriteResult.Fail(
                CommercialIntelligenceFailureCodes.IdempotencyLegacyUnverifiable,
                "Existing assessment idempotency fingerprint is unavailable.");
        }

        if (!string.Equals(existing.IdempotencyPayloadFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return CommercialQuotationRiskWriteResult.Fail(
                CommercialIntelligenceFailureCodes.IdempotencyConflict,
                "Idempotency key was already used with a different payload.");
        }

        return CommercialQuotationRiskWriteResult.Success(
            existing.Id,
            0,
            idempotentReplay: true,
            assessment: MapAssessmentDetail(existing));
    }

    private static string? ValidateCreateProfileCommand(CreateCommercialQuotationRiskProfileCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ProfileCode)) return "Profile code is required.";
        if (string.IsNullOrWhiteSpace(command.Name)) return "Profile name is required.";
        if (command.MinimumComparableObservations < 1) return "Minimum comparable observations must be at least 1.";
        if (command.MaximumNegativePriceVarianceRatio < 0m) return "Maximum negative variance ratio must be non-negative.";
        if (command.MaximumPositivePriceVarianceRatio < 0m) return "Maximum positive variance ratio must be non-negative.";
        if (command.MaximumPaymentTermDays < 0) return "Maximum payment term days must be non-negative.";
        if (command.ExpiryWarningDays < 0) return "Expiry warning days must be non-negative.";
        if (command.MaximumEvidenceAgeDays < 1) return "Maximum evidence age days must be at least 1.";
        if (string.IsNullOrWhiteSpace(command.SourceProvenance)) return "Source provenance is required.";
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey)) return "Idempotency key is required.";
        if (!CommercialSalesPriceGrossNetBasis.All.Contains(command.RequiredGrossNetBasis.Trim()))
            return "Required gross/net basis is invalid.";
        return null;
    }

    private static string? ValidateUpdateProfileCommand(UpdateCommercialQuotationRiskProfileCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name)) return "Profile name is required.";
        if (!CommercialIntelligenceProfileStatuses.All.Contains(command.Status.Trim())) return "Profile status is invalid.";
        if (command.MinimumComparableObservations < 1) return "Minimum comparable observations must be at least 1.";
        if (!CommercialSalesPriceGrossNetBasis.All.Contains(command.RequiredGrossNetBasis.Trim()))
            return "Required gross/net basis is invalid.";
        return null;
    }

    private static string? ValidateEvaluateCommand(EvaluateCommercialQuotationRiskAssessmentCommand command)
    {
        if (command.ReferenceWindowEndUtc <= command.ReferenceWindowStartUtc)
            return "Reference window end must be greater than window start.";
        if (string.IsNullOrWhiteSpace(command.SourceProvenance)) return "Source provenance is required.";
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey)) return "Idempotency key is required.";
        return null;
    }

    private static CommercialQuotationRiskProfileReadModel MapProfile(CommercialQuotationRiskProfile profile) =>
        new(
            profile.Id,
            profile.ProfileCode,
            profile.Name,
            profile.Status,
            profile.ReferenceWindowDurationDays,
            profile.MinimumComparableObservations,
            profile.MaximumNegativePriceVarianceRatio,
            profile.MaximumPositivePriceVarianceRatio,
            profile.MaximumPaymentTermDays,
            profile.ExpiryWarningDays,
            profile.MaximumEvidenceAgeDays,
            profile.AllowedEvidenceStatuses,
            profile.AllowedSalesEvidenceKinds,
            profile.RequiredTaxInclusion,
            profile.RequiredDiscountInclusion,
            profile.RequiredFreightInclusion,
            profile.RequiredGrossNetBasis,
            profile.RuleSetVersion,
            profile.SourceProvenance,
            profile.Version,
            IsPilotProfile(profile.Id));

    private static CommercialQuotationRiskAssessmentSummaryReadModel MapAssessmentSummary(
        CommercialQuotationRiskAssessment assessment) =>
        new(
            assessment.Id,
            assessment.AssessmentReference,
            assessment.ProfileId,
            assessment.Profile?.ProfileCode ?? string.Empty,
            assessment.EvidenceObservationId,
            assessment.QuotationReference,
            assessment.CustomerReference,
            assessment.MaterialId,
            assessment.Outcome,
            assessment.QuotedComparableUnitPrice,
            assessment.ReferenceMedianUnitPrice,
            assessment.SignedPriceVarianceRatio,
            assessment.CandidateObservationCount,
            assessment.ComparableObservationCount,
            assessment.ExcludedObservationCount,
            assessment.PaymentTermDays,
            assessment.RemainingValidityDays,
            assessment.CurrencyCode,
            assessment.UnitOfMeasure,
            assessment.ReferenceWindowStartUtc,
            assessment.ReferenceWindowEndUtc,
            assessment.LeadingDriverCode,
            assessment.RuleSetVersion,
            assessment.EvaluatedAtUtc,
            assessment.CreatedAtUtc,
            IsPilotAssessment(assessment.Id));

    private static CommercialQuotationRiskAssessmentDetailReadModel MapAssessmentDetail(
        CommercialQuotationRiskAssessment assessment) =>
        new(
            assessment.Id,
            assessment.AssessmentReference,
            assessment.ProfileId,
            assessment.Profile?.ProfileCode ?? string.Empty,
            assessment.ProfileVersion,
            assessment.EvidenceObservationId,
            assessment.QuotationReference,
            assessment.CustomerReference,
            assessment.MaterialId,
            assessment.ReferenceWindowStartUtc,
            assessment.ReferenceWindowEndUtc,
            assessment.ReferenceMedianUnitPrice,
            assessment.QuotedComparableUnitPrice,
            assessment.SignedPriceVarianceRatio,
            assessment.CandidateObservationCount,
            assessment.ComparableObservationCount,
            assessment.ExcludedObservationCount,
            assessment.PaymentTermDays,
            assessment.RemainingValidityDays,
            assessment.CurrencyCode,
            assessment.UnitOfMeasure,
            assessment.GrossNetBasis,
            assessment.TaxBasis,
            assessment.DiscountBasis,
            assessment.FreightBasis,
            assessment.EvidenceStatus,
            assessment.Outcome,
            assessment.LeadingDriverCode,
            assessment.RuleSetVersion,
            assessment.SourceProvenance,
            assessment.EvaluatedAtUtc,
            assessment.CreatedAtUtc,
            assessment.Drivers
                .OrderBy(d => d.SortOrder)
                .Select(d => new CommercialQuotationRiskAssessmentDriverReadModel(
                    d.Id,
                    d.ReasonCode,
                    d.Severity,
                    d.ActualValue,
                    d.ThresholdValue,
                    d.Unit,
                    d.NormalizedVariance,
                    d.AffectedObservationCount,
                    d.SupportingText,
                    d.SortOrder,
                    d.IsLeadingDriver))
                .ToList(),
            IsPilotAssessment(assessment.Id));

    private static bool IsPilotProfile(Guid profileId) =>
        profileId == DevelopmentPilotIds.CommercialQuotationRiskPilotProfileId;

    private static bool IsPilotAssessment(Guid assessmentId) =>
        DevelopmentPilotIds.CommercialQuotationRiskPilotAssessmentIds.Contains(assessmentId);
}

public sealed class UnavailableCommercialQuotationRiskRepository : ICommercialQuotationRiskRepository
{
    private static CommercialQuotationRiskWriteResult Unavailable() =>
        CommercialQuotationRiskWriteResult.Fail(
            CommercialIntelligenceFailureCodes.Unavailable,
            "Commercial quotation risk persistence is unavailable.");

    public Task<CommercialQuotationRiskWorkspaceReadModel> GetWorkspaceAsync(
        CommercialIntelligenceOperationContext context,
        bool canManage,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Commercial quotation risk persistence is unavailable.");

    public Task<IReadOnlyList<CommercialQuotationRiskProfileReadModel>> ListProfilesAsync(
        CommercialIntelligenceOperationContext context,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Commercial quotation risk persistence is unavailable.");

    public Task<CommercialQuotationRiskProfileReadModel?> GetProfileByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid profileId,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Commercial quotation risk persistence is unavailable.");

    public Task<CommercialQuotationRiskWriteResult> CreateProfileAsync(
        CommercialIntelligenceOperationContext context,
        CreateCommercialQuotationRiskProfileCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<CommercialQuotationRiskWriteResult> UpdateProfileAsync(
        CommercialIntelligenceOperationContext context,
        UpdateCommercialQuotationRiskProfileCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<IReadOnlyList<CommercialQuotationRiskAssessmentSummaryReadModel>> ListAssessmentsAsync(
        CommercialIntelligenceOperationContext context,
        CommercialQuotationRiskAssessmentListFilters filters,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Commercial quotation risk persistence is unavailable.");

    public Task<CommercialQuotationRiskAssessmentDetailReadModel?> GetAssessmentByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Commercial quotation risk persistence is unavailable.");

    public Task<CommercialQuotationRiskWriteResult> EvaluateAssessmentAsync(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialQuotationRiskAssessmentCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}
