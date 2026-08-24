using CrevaFactOS.Persistence.Development;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.CommercialIntelligence;

public sealed class CommercialIntelligenceRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : ICommercialIntelligenceRepository
{
    public Task<CommercialPurchasePriceWorkspaceReadModel> GetWorkspaceAsync(
        CommercialIntelligenceOperationContext context,
        bool canManage,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new CommercialPurchasePriceWorkspaceReadModel(
            CommercialIntelligenceConstants.RuleSetVersion,
            CommercialIntelligenceConstants.DevelopmentPilotProvenance,
            CommercialIntelligenceConstants.ReferenceCenterMedian,
            CommercialIntelligenceConstants.DefaultMinimumComparableObservations,
            true,
            canManage));

    public Task<IReadOnlyList<CommercialPurchasePriceProfileReadModel>> ListProfilesAsync(
        CommercialIntelligenceOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var profiles = await dbContext.CommercialPurchasePriceProfiles.AsNoTracking()
                    .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId)
                    .OrderBy(p => p.ProfileCode)
                    .ToListAsync(ct);

                return (IReadOnlyList<CommercialPurchasePriceProfileReadModel>)profiles.Select(MapProfile).ToList();
            },
            cancellationToken);

    public Task<CommercialPurchasePriceProfileReadModel?> GetProfileByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid profileId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var profile = await dbContext.CommercialPurchasePriceProfiles.AsNoTracking()
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId
                             && p.Id == profileId,
                        ct);

                return profile is null ? null : MapProfile(profile);
            },
            cancellationToken);

    public Task<CommercialIntelligenceWriteResult> CreateProfileAsync(
        CommercialIntelligenceOperationContext context,
        CreateCommercialPurchasePriceProfileCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateCreateProfileCommand(command);
                if (validationError is not null)
                {
                    return CommercialIntelligenceWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        validationError);
                }

                var fingerprint = CommercialIntelligenceIdempotencyFingerprint.ComputeProfileFingerprint(context, command);
                var existing = await dbContext.CommercialPurchasePriceProfiles.AsNoTracking()
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId && p.CreateIdempotencyKey == command.IdempotencyKey.Trim(),
                        ct);

                if (existing is not null)
                {
                    return ResolveIdempotentProfileReplay(existing, fingerprint);
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var profile = new CommercialPurchasePriceProfile
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    ProfileCode = command.ProfileCode.Trim(),
                    DisplayName = command.DisplayName.Trim(),
                    Status = CommercialIntelligenceProfileStatuses.Draft,
                    ComparisonDimension = command.ComparisonDimension.Trim(),
                    ReferenceWindowDurationDays = command.ReferenceWindowDurationDays,
                    MinimumComparableObservations = command.MinimumComparableObservations,
                    MaximumPositiveVarianceRatio = command.MaximumPositiveVarianceRatio,
                    SupplierSpreadThreshold = command.SupplierSpreadThreshold,
                    AllowedEvidenceStatuses = command.AllowedEvidenceStatuses.Trim(),
                    RequiredTaxInclusion = command.RequiredTaxInclusion.Trim(),
                    RequiredDiscountInclusion = command.RequiredDiscountInclusion.Trim(),
                    RequiredFreightInclusion = command.RequiredFreightInclusion.Trim(),
                    ReferenceCenter = CommercialIntelligenceConstants.ReferenceCenterMedian,
                    RuleSetVersion = CommercialIntelligenceConstants.RuleSetVersion,
                    SourceProvenance = command.SourceProvenance.Trim(),
                    CreateIdempotencyKey = command.IdempotencyKey.Trim(),
                    CreateIdempotencyPayloadFingerprint = fingerprint,
                    Version = 1,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedBy = actor,
                    UpdatedBy = actor,
                };

                dbContext.CommercialPurchasePriceProfiles.Add(profile);
                await dbContext.SaveChangesAsync(ct);
                return CommercialIntelligenceWriteResult.Success(profile.Id, profile.Version);
            },
            cancellationToken);

    public Task<CommercialIntelligenceWriteResult> UpdateProfileAsync(
        CommercialIntelligenceOperationContext context,
        UpdateCommercialPurchasePriceProfileCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateUpdateProfileCommand(command);
                if (validationError is not null)
                {
                    return CommercialIntelligenceWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        validationError);
                }

                var profile = await dbContext.CommercialPurchasePriceProfiles
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId
                             && p.Id == command.ProfileId,
                        ct);

                if (profile is null)
                {
                    return CommercialIntelligenceWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.NotFound,
                        "Purchase price profile not found.");
                }

                if (profile.Version != command.ExpectedVersion)
                {
                    return CommercialIntelligenceWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ConcurrencyConflict,
                        "Purchase price profile version is stale.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                profile.DisplayName = command.DisplayName.Trim();
                profile.Status = command.Status.Trim();
                profile.ComparisonDimension = command.ComparisonDimension.Trim();
                profile.ReferenceWindowDurationDays = command.ReferenceWindowDurationDays;
                profile.MinimumComparableObservations = command.MinimumComparableObservations;
                profile.MaximumPositiveVarianceRatio = command.MaximumPositiveVarianceRatio;
                profile.SupplierSpreadThreshold = command.SupplierSpreadThreshold;
                profile.AllowedEvidenceStatuses = command.AllowedEvidenceStatuses.Trim();
                profile.RequiredTaxInclusion = command.RequiredTaxInclusion.Trim();
                profile.RequiredDiscountInclusion = command.RequiredDiscountInclusion.Trim();
                profile.RequiredFreightInclusion = command.RequiredFreightInclusion.Trim();
                profile.SourceProvenance = command.SourceProvenance.Trim();
                profile.UpdatedAtUtc = now;
                profile.UpdatedBy = actor;
                profile.Version += 1;

                await dbContext.SaveChangesAsync(ct);
                return CommercialIntelligenceWriteResult.Success(profile.Id, profile.Version);
            },
            cancellationToken);

    public Task<IReadOnlyList<CommercialPurchasePriceAssessmentSummaryReadModel>> ListAssessmentsAsync(
        CommercialIntelligenceOperationContext context,
        CommercialPurchasePriceAssessmentListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var page = filters.Page < 1 ? 1 : filters.Page;
                var pageSize = filters.PageSize is < 1 or > 100 ? 25 : filters.PageSize;

                var query = dbContext.CommercialPurchasePriceAssessments.AsNoTracking()
                    .Include(a => a.Profile)
                    .Where(a => a.TenantId == context.TenantId && a.FacilityId == context.FacilityId);

                if (filters.MaterialId.HasValue)
                {
                    query = query.Where(a => a.MaterialId == filters.MaterialId.Value);
                }

                if (!string.IsNullOrWhiteSpace(filters.AssessmentOutcome))
                {
                    query = query.Where(a => a.AssessmentOutcome == filters.AssessmentOutcome.Trim());
                }

                var assessments = await query
                    .OrderByDescending(a => a.CreatedAtUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                return (IReadOnlyList<CommercialPurchasePriceAssessmentSummaryReadModel>)assessments
                    .Select(MapAssessmentSummary)
                    .ToList();
            },
            cancellationToken);

    public Task<CommercialPurchasePriceAssessmentDetailReadModel?> GetAssessmentByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var assessment = await dbContext.CommercialPurchasePriceAssessments.AsNoTracking()
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

    public Task<CommercialIntelligenceWriteResult> EvaluateAssessmentAsync(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialPurchasePriceAssessmentCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateEvaluateCommand(command);
                if (validationError is not null)
                {
                    return CommercialIntelligenceWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        validationError);
                }

                var fingerprint = CommercialIntelligenceIdempotencyFingerprint.ComputeAssessmentFingerprint(context, command);
                var existing = await dbContext.CommercialPurchasePriceAssessments.AsNoTracking()
                    .Include(a => a.Profile)
                    .Include(a => a.Drivers)
                    .SingleOrDefaultAsync(
                        a => a.TenantId == context.TenantId && a.IdempotencyKey == command.IdempotencyKey.Trim(),
                        ct);

                if (existing is not null)
                {
                    return ResolveIdempotentAssessmentReplay(existing, fingerprint);
                }

                var profile = await dbContext.CommercialPurchasePriceProfiles
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId
                             && p.Id == command.ProfileId,
                        ct);

                if (profile is null)
                {
                    return CommercialIntelligenceWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.NotFound,
                        "Purchase price profile not found.");
                }

                if (!string.Equals(profile.Status, CommercialIntelligenceProfileStatuses.Approved, StringComparison.Ordinal))
                {
                    return CommercialIntelligenceWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        "Only approved purchase price profiles may execute assessments.");
                }

                var evidence = await CommercialIntelligencePriceEvidenceReader.ReadAsync(
                    dbContext,
                    context,
                    profile,
                    command,
                    ct);
                var exclusionCounts = CommercialIntelligencePriceEvidenceReader.SummarizeExclusions(evidence.Excluded);
                var evaluation = CommercialPurchasePriceEvaluator.Evaluate(
                    profile,
                    command.CurrentUnitPrice,
                    command.NormalizedUnit.Trim(),
                    evidence,
                    exclusionCounts);

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var assessment = new CommercialPurchasePriceAssessment
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    AssessmentReference = await GenerateAssessmentReferenceAsync(context, ct),
                    ProfileId = profile.Id,
                    ProfileVersion = profile.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    MaterialId = command.MaterialId,
                    SupplierReference = string.IsNullOrWhiteSpace(command.SupplierReference)
                        ? null
                        : command.SupplierReference.Trim(),
                    ComparisonDimension = command.ComparisonDimension.Trim(),
                    CurrentUnitPrice = command.CurrentUnitPrice,
                    ReferenceMedianUnitPrice = evaluation.ReferenceMedianUnitPrice,
                    AbsoluteVariance = evaluation.AbsoluteVariance,
                    VarianceRatio = evaluation.VarianceRatio,
                    ComparableObservationCount = evaluation.ComparableObservationCount,
                    ExcludedObservationCount = evaluation.ExcludedObservationCount,
                    CurrencyCode = command.CurrencyCode.Trim().ToUpperInvariant(),
                    NormalizedUnit = command.NormalizedUnit.Trim(),
                    WindowStartUtc = command.WindowStartUtc,
                    WindowEndUtc = command.WindowEndUtc,
                    PriceBasisTaxInclusion = command.PriceBasisTaxInclusion.Trim(),
                    PriceBasisDiscountInclusion = command.PriceBasisDiscountInclusion.Trim(),
                    PriceBasisFreightInclusion = command.PriceBasisFreightInclusion.Trim(),
                    EvidenceStatus = evaluation.EvidenceStatus,
                    AssessmentOutcome = evaluation.AssessmentOutcome,
                    LeadingDriverCode = evaluation.LeadingDriverCode,
                    RuleSetVersion = CommercialIntelligenceConstants.RuleSetVersion,
                    IdempotencyKey = command.IdempotencyKey.Trim(),
                    IdempotencyPayloadFingerprint = fingerprint,
                    SourceProvenance = command.SourceProvenance.Trim(),
                    EvaluatedAtUtc = now,
                    CreatedAtUtc = now,
                    CreatedBy = actor,
                };

                var orderedDrivers = CommercialPurchasePriceEvaluator.OrderDrivers(evaluation.Drivers);
                for (var index = 0; index < orderedDrivers.Count; index++)
                {
                    var driver = orderedDrivers[index];
                    assessment.Drivers.Add(new CommercialPurchasePriceAssessmentDriver
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

                dbContext.CommercialPurchasePriceAssessments.Add(assessment);
                await dbContext.SaveChangesAsync(ct);

                var detail = MapAssessmentDetail(assessment);
                return CommercialIntelligenceWriteResult.Success(assessment.Id, 0, assessment: detail);
            },
            cancellationToken);

    public Task<IReadOnlyList<CommercialPurchasePriceComparisonSummaryReadModel>> ListComparisonsAsync(
        CommercialIntelligenceOperationContext context,
        CommercialPurchasePriceComparisonListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => CommercialIntelligenceComparisonOperations.ListComparisonsAsync(
                dbContext,
                context,
                filters,
                ct),
            cancellationToken);

    public Task<CommercialPurchasePriceComparisonDetailReadModel?> GetComparisonByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid comparisonRunId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => CommercialIntelligenceComparisonOperations.GetComparisonByIdAsync(
                dbContext,
                context,
                comparisonRunId,
                ct),
            cancellationToken);

    public Task<CommercialIntelligenceWriteResult> EvaluateComparisonAsync(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialPurchasePriceComparisonCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => CommercialIntelligenceComparisonOperations.EvaluateComparisonAsync(
                dbContext,
                context,
                command,
                ct),
            cancellationToken);

    private async Task<string> GenerateAssessmentReferenceAsync(
        CommercialIntelligenceOperationContext context,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var reference = $"CI-PP-{suffix}";
            var exists = await dbContext.CommercialPurchasePriceAssessments.AsNoTracking()
                .AnyAsync(a => a.TenantId == context.TenantId && a.AssessmentReference == reference, cancellationToken);
            if (!exists)
            {
                return reference;
            }
        }

        return $"CI-PP-{Guid.NewGuid():N}".ToUpperInvariant();
    }

    private static CommercialIntelligenceWriteResult ResolveIdempotentProfileReplay(
        CommercialPurchasePriceProfile existing,
        string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(existing.CreateIdempotencyPayloadFingerprint))
        {
            return CommercialIntelligenceWriteResult.Fail(
                CommercialIntelligenceFailureCodes.IdempotencyLegacyUnverifiable,
                "Existing profile idempotency fingerprint is unavailable.");
        }

        if (!string.Equals(existing.CreateIdempotencyPayloadFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return CommercialIntelligenceWriteResult.Fail(
                CommercialIntelligenceFailureCodes.IdempotencyConflict,
                "Idempotency key was already used with a different payload.");
        }

        return CommercialIntelligenceWriteResult.Success(existing.Id, existing.Version, idempotentReplay: true);
    }

    private static CommercialIntelligenceWriteResult ResolveIdempotentAssessmentReplay(
        CommercialPurchasePriceAssessment existing,
        string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(existing.IdempotencyPayloadFingerprint))
        {
            return CommercialIntelligenceWriteResult.Fail(
                CommercialIntelligenceFailureCodes.IdempotencyLegacyUnverifiable,
                "Existing assessment idempotency fingerprint is unavailable.");
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
            assessment: MapAssessmentDetail(existing));
    }

    private static string? ValidateCreateProfileCommand(CreateCommercialPurchasePriceProfileCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ProfileCode))
        {
            return "Profile code is required.";
        }

        if (string.IsNullOrWhiteSpace(command.DisplayName))
        {
            return "Display name is required.";
        }

        if (!CommercialIntelligenceComparisonDimensions.All.Contains(command.ComparisonDimension.Trim()))
        {
            return "Comparison dimension is invalid.";
        }

        if (command.MinimumComparableObservations < 1)
        {
            return "Minimum comparable observations must be at least 1.";
        }

        if (command.MaximumPositiveVarianceRatio < 0m)
        {
            return "Maximum positive variance ratio must be non-negative.";
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

    private static string? ValidateUpdateProfileCommand(UpdateCommercialPurchasePriceProfileCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.DisplayName))
        {
            return "Display name is required.";
        }

        if (!CommercialIntelligenceProfileStatuses.All.Contains(command.Status.Trim()))
        {
            return "Profile status is invalid.";
        }

        if (!CommercialIntelligenceComparisonDimensions.All.Contains(command.ComparisonDimension.Trim()))
        {
            return "Comparison dimension is invalid.";
        }

        if (command.MinimumComparableObservations < 1)
        {
            return "Minimum comparable observations must be at least 1.";
        }

        return null;
    }

    private static string? ValidateEvaluateCommand(EvaluateCommercialPurchasePriceAssessmentCommand command)
    {
        if (command.WindowEndUtc <= command.WindowStartUtc)
        {
            return "Window end must be greater than window start.";
        }

        if (command.CurrentUnitPrice <= 0m)
        {
            return "Current unit price must be positive.";
        }

        if (string.IsNullOrWhiteSpace(command.CurrencyCode))
        {
            return "Currency code is required.";
        }

        if (string.IsNullOrWhiteSpace(command.NormalizedUnit))
        {
            return "Normalized unit is required.";
        }

        if (!CommercialIntelligenceComparisonDimensions.All.Contains(command.ComparisonDimension.Trim()))
        {
            return "Comparison dimension is invalid.";
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

    private static CommercialPurchasePriceProfileReadModel MapProfile(CommercialPurchasePriceProfile profile) =>
        new(
            profile.Id,
            profile.ProfileCode,
            profile.DisplayName,
            profile.Status,
            profile.ComparisonDimension,
            profile.ReferenceWindowDurationDays,
            profile.MinimumComparableObservations,
            profile.MaximumPositiveVarianceRatio,
            profile.SupplierSpreadThreshold,
            profile.AllowedEvidenceStatuses,
            profile.RequiredTaxInclusion,
            profile.RequiredDiscountInclusion,
            profile.RequiredFreightInclusion,
            profile.ReferenceCenter,
            profile.RuleSetVersion,
            profile.SourceProvenance,
            profile.Version,
            IsPilotProfile(profile.Id));

    private static CommercialPurchasePriceAssessmentSummaryReadModel MapAssessmentSummary(
        CommercialPurchasePriceAssessment assessment) =>
        new(
            assessment.Id,
            assessment.AssessmentReference,
            assessment.ProfileId,
            assessment.Profile?.ProfileCode ?? string.Empty,
            assessment.MaterialId,
            assessment.SupplierReference,
            assessment.ComparisonDimension,
            assessment.AssessmentOutcome,
            assessment.CurrentUnitPrice,
            assessment.ReferenceMedianUnitPrice,
            assessment.VarianceRatio,
            assessment.ComparableObservationCount,
            assessment.ExcludedObservationCount,
            assessment.CurrencyCode,
            assessment.NormalizedUnit,
            assessment.WindowStartUtc,
            assessment.WindowEndUtc,
            assessment.LeadingDriverCode,
            assessment.RuleSetVersion,
            assessment.EvaluatedAtUtc,
            assessment.CreatedAtUtc,
            IsPilotAssessment(assessment.Id));

    private static CommercialPurchasePriceAssessmentDetailReadModel MapAssessmentDetail(
        CommercialPurchasePriceAssessment assessment) =>
        new(
            assessment.Id,
            assessment.AssessmentReference,
            assessment.ProfileId,
            assessment.Profile?.ProfileCode ?? string.Empty,
            assessment.MaterialId,
            assessment.SupplierReference,
            assessment.ComparisonDimension,
            assessment.CurrentUnitPrice,
            assessment.ReferenceMedianUnitPrice,
            assessment.AbsoluteVariance,
            assessment.VarianceRatio,
            assessment.ComparableObservationCount,
            assessment.ExcludedObservationCount,
            assessment.CurrencyCode,
            assessment.NormalizedUnit,
            assessment.WindowStartUtc,
            assessment.WindowEndUtc,
            assessment.PriceBasisTaxInclusion,
            assessment.PriceBasisDiscountInclusion,
            assessment.PriceBasisFreightInclusion,
            assessment.EvidenceStatus,
            assessment.AssessmentOutcome,
            assessment.LeadingDriverCode,
            assessment.RuleSetVersion,
            assessment.SourceProvenance,
            assessment.EvaluatedAtUtc,
            assessment.CreatedAtUtc,
            assessment.Drivers
                .OrderBy(d => d.SortOrder)
                .Select(d => new CommercialPurchasePriceAssessmentDriverReadModel(
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

    private static bool IsPilotProfile(Guid id) =>
        id == DevelopmentPilotIds.CommercialIntelligencePilotProfileId;

    private static bool IsPilotAssessment(Guid id) => false;
}

public sealed class UnavailableCommercialIntelligenceRepository : ICommercialIntelligenceRepository
{
    private static CommercialIntelligenceWriteResult Unavailable() =>
        CommercialIntelligenceWriteResult.Fail(
            CommercialIntelligenceFailureCodes.Unavailable,
            "Commercial intelligence is unavailable.");

    public Task<CommercialPurchasePriceWorkspaceReadModel> GetWorkspaceAsync(
        CommercialIntelligenceOperationContext context,
        bool canManage,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new CommercialPurchasePriceWorkspaceReadModel(
            CommercialIntelligenceConstants.RuleSetVersion,
            CommercialIntelligenceConstants.DevelopmentPilotProvenance,
            CommercialIntelligenceConstants.ReferenceCenterMedian,
            CommercialIntelligenceConstants.DefaultMinimumComparableObservations,
            false,
            false));

    public Task<IReadOnlyList<CommercialPurchasePriceProfileReadModel>> ListProfilesAsync(
        CommercialIntelligenceOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CommercialPurchasePriceProfileReadModel>>([]);

    public Task<CommercialPurchasePriceProfileReadModel?> GetProfileByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid profileId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CommercialPurchasePriceProfileReadModel?>(null);

    public Task<CommercialIntelligenceWriteResult> CreateProfileAsync(
        CommercialIntelligenceOperationContext context,
        CreateCommercialPurchasePriceProfileCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<CommercialIntelligenceWriteResult> UpdateProfileAsync(
        CommercialIntelligenceOperationContext context,
        UpdateCommercialPurchasePriceProfileCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<IReadOnlyList<CommercialPurchasePriceAssessmentSummaryReadModel>> ListAssessmentsAsync(
        CommercialIntelligenceOperationContext context,
        CommercialPurchasePriceAssessmentListFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CommercialPurchasePriceAssessmentSummaryReadModel>>([]);

    public Task<CommercialPurchasePriceAssessmentDetailReadModel?> GetAssessmentByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CommercialPurchasePriceAssessmentDetailReadModel?>(null);

    public Task<CommercialIntelligenceWriteResult> EvaluateAssessmentAsync(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialPurchasePriceAssessmentCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<IReadOnlyList<CommercialPurchasePriceComparisonSummaryReadModel>> ListComparisonsAsync(
        CommercialIntelligenceOperationContext context,
        CommercialPurchasePriceComparisonListFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CommercialPurchasePriceComparisonSummaryReadModel>>([]);

    public Task<CommercialPurchasePriceComparisonDetailReadModel?> GetComparisonByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid comparisonRunId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CommercialPurchasePriceComparisonDetailReadModel?>(null);

    public Task<CommercialIntelligenceWriteResult> EvaluateComparisonAsync(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialPurchasePriceComparisonCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}
