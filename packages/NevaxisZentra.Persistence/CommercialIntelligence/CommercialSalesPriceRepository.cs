using NevaxisZentra.Persistence.Development;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.CommercialIntelligence;

public sealed class CommercialSalesPriceRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : ICommercialSalesPriceRepository
{
    public Task<CommercialSalesPriceWorkspaceReadModel> GetWorkspaceAsync(
        CommercialIntelligenceOperationContext context,
        bool canManage,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new CommercialSalesPriceWorkspaceReadModel(
            CommercialIntelligenceConstants.SalesRuleSetVersion,
            CommercialIntelligenceConstants.SalesDevelopmentPilotProvenance,
            CommercialIntelligenceConstants.ReferenceCenterMedian,
            CommercialIntelligenceConstants.DefaultMinimumComparableObservations,
            RealTransactionMappingComplete: false,
            true,
            canManage));

    public Task<IReadOnlyList<CommercialSalesPriceProfileReadModel>> ListProfilesAsync(
        CommercialIntelligenceOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var profiles = await dbContext.CommercialSalesPriceProfiles.AsNoTracking()
                    .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId)
                    .OrderBy(p => p.ProfileCode)
                    .ToListAsync(ct);

                return (IReadOnlyList<CommercialSalesPriceProfileReadModel>)profiles.Select(MapProfile).ToList();
            },
            cancellationToken);

    public Task<CommercialSalesPriceProfileReadModel?> GetProfileByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid profileId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var profile = await dbContext.CommercialSalesPriceProfiles.AsNoTracking()
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId
                             && p.Id == profileId,
                        ct);

                return profile is null ? null : MapProfile(profile);
            },
            cancellationToken);

    public Task<CommercialSalesPriceWriteResult> CreateProfileAsync(
        CommercialIntelligenceOperationContext context,
        CreateCommercialSalesPriceProfileCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateCreateProfileCommand(command);
                if (validationError is not null)
                {
                    return CommercialSalesPriceWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        validationError);
                }

                var fingerprint = CommercialSalesPriceIdempotencyFingerprint.ComputeProfileFingerprint(context, command);
                var existing = await dbContext.CommercialSalesPriceProfiles.AsNoTracking()
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId && p.CreateIdempotencyKey == command.IdempotencyKey.Trim(),
                        ct);

                if (existing is not null)
                {
                    return ResolveIdempotentProfileReplay(existing, fingerprint);
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var profile = new CommercialSalesPriceProfile
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
                    CustomerSpreadThreshold = command.CustomerSpreadThreshold,
                    AllowedEvidenceStatuses = command.AllowedEvidenceStatuses.Trim(),
                    AllowedSourceEvidenceKinds = command.AllowedSourceEvidenceKinds.Trim(),
                    RequiredTaxInclusion = command.RequiredTaxInclusion.Trim(),
                    RequiredDiscountInclusion = command.RequiredDiscountInclusion.Trim(),
                    RequiredFreightInclusion = command.RequiredFreightInclusion.Trim(),
                    RequiredGrossNetBasis = command.RequiredGrossNetBasis.Trim(),
                    ReferenceCenter = CommercialIntelligenceConstants.ReferenceCenterMedian,
                    RuleSetVersion = CommercialIntelligenceConstants.SalesRuleSetVersion,
                    SourceProvenance = command.SourceProvenance.Trim(),
                    CreateIdempotencyKey = command.IdempotencyKey.Trim(),
                    CreateIdempotencyPayloadFingerprint = fingerprint,
                    Version = 1,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedBy = actor,
                    UpdatedBy = actor,
                };

                dbContext.CommercialSalesPriceProfiles.Add(profile);
                await dbContext.SaveChangesAsync(ct);
                return CommercialSalesPriceWriteResult.Success(profile.Id, profile.Version);
            },
            cancellationToken);

    public Task<CommercialSalesPriceWriteResult> UpdateProfileAsync(
        CommercialIntelligenceOperationContext context,
        UpdateCommercialSalesPriceProfileCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateUpdateProfileCommand(command);
                if (validationError is not null)
                {
                    return CommercialSalesPriceWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        validationError);
                }

                var profile = await dbContext.CommercialSalesPriceProfiles
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId
                             && p.Id == command.ProfileId,
                        ct);

                if (profile is null)
                {
                    return CommercialSalesPriceWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.NotFound,
                        "Sales price profile not found.");
                }

                if (profile.Version != command.ExpectedVersion)
                {
                    return CommercialSalesPriceWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ConcurrencyConflict,
                        "Sales price profile version is stale.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                profile.DisplayName = command.DisplayName.Trim();
                profile.Status = command.Status.Trim();
                profile.ComparisonDimension = command.ComparisonDimension.Trim();
                profile.ReferenceWindowDurationDays = command.ReferenceWindowDurationDays;
                profile.MinimumComparableObservations = command.MinimumComparableObservations;
                profile.MaximumPositiveVarianceRatio = command.MaximumPositiveVarianceRatio;
                profile.CustomerSpreadThreshold = command.CustomerSpreadThreshold;
                profile.AllowedEvidenceStatuses = command.AllowedEvidenceStatuses.Trim();
                profile.AllowedSourceEvidenceKinds = command.AllowedSourceEvidenceKinds.Trim();
                profile.RequiredTaxInclusion = command.RequiredTaxInclusion.Trim();
                profile.RequiredDiscountInclusion = command.RequiredDiscountInclusion.Trim();
                profile.RequiredFreightInclusion = command.RequiredFreightInclusion.Trim();
                profile.RequiredGrossNetBasis = command.RequiredGrossNetBasis.Trim();
                profile.SourceProvenance = command.SourceProvenance.Trim();
                profile.UpdatedAtUtc = now;
                profile.UpdatedBy = actor;
                profile.Version += 1;

                await dbContext.SaveChangesAsync(ct);
                return CommercialSalesPriceWriteResult.Success(profile.Id, profile.Version);
            },
            cancellationToken);

    public Task<IReadOnlyList<CommercialSalesPriceAssessmentSummaryReadModel>> ListAssessmentsAsync(
        CommercialIntelligenceOperationContext context,
        CommercialSalesPriceAssessmentListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var page = filters.Page < 1 ? 1 : filters.Page;
                var pageSize = filters.PageSize is < 1 or > 100 ? 25 : filters.PageSize;

                var query = dbContext.CommercialSalesPriceAssessments.AsNoTracking()
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

                return (IReadOnlyList<CommercialSalesPriceAssessmentSummaryReadModel>)assessments
                    .Select(MapAssessmentSummary)
                    .ToList();
            },
            cancellationToken);

    public Task<CommercialSalesPriceAssessmentDetailReadModel?> GetAssessmentByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var assessment = await dbContext.CommercialSalesPriceAssessments.AsNoTracking()
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

    public Task<CommercialSalesPriceWriteResult> EvaluateAssessmentAsync(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialSalesPriceAssessmentCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateEvaluateCommand(command);
                if (validationError is not null)
                {
                    return CommercialSalesPriceWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        validationError);
                }

                var fingerprint = CommercialSalesPriceIdempotencyFingerprint.ComputeAssessmentFingerprint(context, command);
                var existing = await dbContext.CommercialSalesPriceAssessments.AsNoTracking()
                    .Include(a => a.Profile)
                    .Include(a => a.Drivers)
                    .SingleOrDefaultAsync(
                        a => a.TenantId == context.TenantId && a.IdempotencyKey == command.IdempotencyKey.Trim(),
                        ct);

                if (existing is not null)
                {
                    return ResolveIdempotentAssessmentReplay(existing, fingerprint);
                }

                var profile = await dbContext.CommercialSalesPriceProfiles
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId
                             && p.Id == command.ProfileId,
                        ct);

                if (profile is null)
                {
                    return CommercialSalesPriceWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.NotFound,
                        "Sales price profile not found.");
                }

                if (!string.Equals(profile.Status, CommercialIntelligenceProfileStatuses.Approved, StringComparison.Ordinal))
                {
                    return CommercialSalesPriceWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        "Only approved sales price profiles may execute assessments.");
                }

                var evidence = await CommercialSalesPriceEvidenceReader.ReadAsync(
                    dbContext,
                    context,
                    profile,
                    command,
                    ct);
                var exclusionCounts = CommercialSalesPriceEvidenceReader.SummarizeExclusions(evidence.Excluded);
                var evaluation = CommercialSalesPriceEvaluator.Evaluate(
                    profile,
                    command.CurrentUnitPrice,
                    command.NormalizedUnit.Trim(),
                    evidence,
                    exclusionCounts);

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var assessment = new CommercialSalesPriceAssessment
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    AssessmentReference = await GenerateAssessmentReferenceAsync(context, ct),
                    ProfileId = profile.Id,
                    ProfileVersion = profile.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    MaterialId = command.MaterialId,
                    CustomerReference = string.IsNullOrWhiteSpace(command.CustomerReference)
                        ? null
                        : command.CustomerReference.Trim(),
                    SourceEvidenceKind = command.SourceEvidenceKind.Trim(),
                    ComparisonDimension = command.ComparisonDimension.Trim(),
                    CurrentUnitPrice = command.CurrentUnitPrice,
                    ReferenceMedianUnitPrice = evaluation.ReferenceMedianUnitPrice,
                    AbsoluteVariance = evaluation.AbsoluteVariance,
                    VarianceRatio = evaluation.VarianceRatio,
                    CandidateObservationCount = evaluation.CandidateObservationCount,
                    ComparableObservationCount = evaluation.ComparableObservationCount,
                    ExcludedObservationCount = evaluation.ExcludedObservationCount,
                    CurrencyCode = command.CurrencyCode.Trim().ToUpperInvariant(),
                    NormalizedUnit = command.NormalizedUnit.Trim(),
                    WindowStartUtc = command.WindowStartUtc,
                    WindowEndUtc = command.WindowEndUtc,
                    PriceBasisGrossNet = command.PriceBasisGrossNet.Trim(),
                    PriceBasisTaxInclusion = command.PriceBasisTaxInclusion.Trim(),
                    PriceBasisDiscountInclusion = command.PriceBasisDiscountInclusion.Trim(),
                    PriceBasisFreightInclusion = command.PriceBasisFreightInclusion.Trim(),
                    EvidenceStatus = evaluation.EvidenceStatus,
                    AssessmentOutcome = evaluation.AssessmentOutcome,
                    LeadingDriverCode = evaluation.LeadingDriverCode,
                    RuleSetVersion = CommercialIntelligenceConstants.SalesRuleSetVersion,
                    IdempotencyKey = command.IdempotencyKey.Trim(),
                    IdempotencyPayloadFingerprint = fingerprint,
                    SourceProvenance = command.SourceProvenance.Trim(),
                    EvaluatedAtUtc = now,
                    CreatedAtUtc = now,
                    CreatedBy = actor,
                };

                var orderedDrivers = CommercialSalesPriceEvaluator.OrderDrivers(evaluation.Drivers);
                for (var index = 0; index < orderedDrivers.Count; index++)
                {
                    var driver = orderedDrivers[index];
                    assessment.Drivers.Add(new CommercialSalesPriceAssessmentDriver
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

                dbContext.CommercialSalesPriceAssessments.Add(assessment);
                await dbContext.SaveChangesAsync(ct);

                var detail = MapAssessmentDetail(assessment);
                return CommercialSalesPriceWriteResult.Success(assessment.Id, 0, assessment: detail);
            },
            cancellationToken);

    public Task<IReadOnlyList<CommercialSalesPriceComparisonSummaryReadModel>> ListComparisonsAsync(
        CommercialIntelligenceOperationContext context,
        CommercialSalesPriceComparisonListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct => await CommercialSalesPriceComparisonOperations.ListComparisonsAsync(
                dbContext, context, filters, ct),
            cancellationToken);

    public Task<CommercialSalesPriceComparisonDetailReadModel?> GetComparisonByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid comparisonRunId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct => await CommercialSalesPriceComparisonOperations.GetComparisonByIdAsync(
                dbContext, context, comparisonRunId, ct),
            cancellationToken);

    public Task<CommercialSalesPriceWriteResult> EvaluateComparisonAsync(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialSalesPriceComparisonCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct => await CommercialSalesPriceComparisonOperations.EvaluateComparisonAsync(
                dbContext, context, command, ct),
            cancellationToken);

    private async Task<string> GenerateAssessmentReferenceAsync(
        CommercialIntelligenceOperationContext context,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var reference = $"CI-SP-{suffix}";
            var exists = await dbContext.CommercialSalesPriceAssessments.AsNoTracking()
                .AnyAsync(a => a.TenantId == context.TenantId && a.AssessmentReference == reference, cancellationToken);
            if (!exists)
            {
                return reference;
            }
        }

        return $"CI-SP-{Guid.NewGuid():N}".ToUpperInvariant();
    }

    private static CommercialSalesPriceWriteResult ResolveIdempotentProfileReplay(
        CommercialSalesPriceProfile existing,
        string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(existing.CreateIdempotencyPayloadFingerprint))
        {
            return CommercialSalesPriceWriteResult.Fail(
                CommercialIntelligenceFailureCodes.IdempotencyLegacyUnverifiable,
                "Existing profile idempotency fingerprint is unavailable.");
        }

        if (!string.Equals(existing.CreateIdempotencyPayloadFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return CommercialSalesPriceWriteResult.Fail(
                CommercialIntelligenceFailureCodes.IdempotencyConflict,
                "Idempotency key was already used with a different payload.");
        }

        return CommercialSalesPriceWriteResult.Success(existing.Id, existing.Version, idempotentReplay: true);
    }

    private static CommercialSalesPriceWriteResult ResolveIdempotentAssessmentReplay(
        CommercialSalesPriceAssessment existing,
        string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(existing.IdempotencyPayloadFingerprint))
        {
            return CommercialSalesPriceWriteResult.Fail(
                CommercialIntelligenceFailureCodes.IdempotencyLegacyUnverifiable,
                "Existing assessment idempotency fingerprint is unavailable.");
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
            assessment: MapAssessmentDetail(existing));
    }

    private static string? ValidateCreateProfileCommand(CreateCommercialSalesPriceProfileCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ProfileCode)) return "Profile code is required.";
        if (string.IsNullOrWhiteSpace(command.DisplayName)) return "Display name is required.";
        if (!CommercialSalesPriceComparisonDimensions.All.Contains(command.ComparisonDimension.Trim()))
            return "Comparison dimension is invalid.";
        if (command.MinimumComparableObservations < 1) return "Minimum comparable observations must be at least 1.";
        if (command.MaximumPositiveVarianceRatio < 0m) return "Maximum positive variance ratio must be non-negative.";
        if (string.IsNullOrWhiteSpace(command.SourceProvenance)) return "Source provenance is required.";
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey)) return "Idempotency key is required.";
        if (!CommercialSalesPriceGrossNetBasis.All.Contains(command.RequiredGrossNetBasis.Trim()))
            return "Required gross/net basis is invalid.";
        return null;
    }

    private static string? ValidateUpdateProfileCommand(UpdateCommercialSalesPriceProfileCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.DisplayName)) return "Display name is required.";
        if (!CommercialIntelligenceProfileStatuses.All.Contains(command.Status.Trim())) return "Profile status is invalid.";
        if (!CommercialSalesPriceComparisonDimensions.All.Contains(command.ComparisonDimension.Trim()))
            return "Comparison dimension is invalid.";
        if (command.MinimumComparableObservations < 1) return "Minimum comparable observations must be at least 1.";
        if (!CommercialSalesPriceGrossNetBasis.All.Contains(command.RequiredGrossNetBasis.Trim()))
            return "Required gross/net basis is invalid.";
        return null;
    }

    private static string? ValidateEvaluateCommand(EvaluateCommercialSalesPriceAssessmentCommand command)
    {
        if (command.WindowEndUtc <= command.WindowStartUtc) return "Window end must be greater than window start.";
        if (command.CurrentUnitPrice <= 0m) return "Current unit price must be positive.";
        if (string.IsNullOrWhiteSpace(command.CurrencyCode)) return "Currency code is required.";
        if (string.IsNullOrWhiteSpace(command.NormalizedUnit)) return "Normalized unit is required.";
        if (!CommercialSalesPriceComparisonDimensions.All.Contains(command.ComparisonDimension.Trim()))
            return "Comparison dimension is invalid.";
        if (!CommercialSalesPriceEvidenceKinds.All.Contains(command.SourceEvidenceKind.Trim()))
            return "Source evidence kind is invalid.";
        if (!CommercialSalesPriceGrossNetBasis.All.Contains(command.PriceBasisGrossNet.Trim()))
            return "Price basis gross/net is invalid.";
        if (string.IsNullOrWhiteSpace(command.SourceProvenance)) return "Source provenance is required.";
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey)) return "Idempotency key is required.";
        return null;
    }

    private static CommercialSalesPriceProfileReadModel MapProfile(CommercialSalesPriceProfile profile) =>
        new(
            profile.Id,
            profile.ProfileCode,
            profile.DisplayName,
            profile.Status,
            profile.ComparisonDimension,
            profile.ReferenceWindowDurationDays,
            profile.MinimumComparableObservations,
            profile.MaximumPositiveVarianceRatio,
            profile.CustomerSpreadThreshold,
            profile.AllowedEvidenceStatuses,
            profile.AllowedSourceEvidenceKinds,
            profile.RequiredTaxInclusion,
            profile.RequiredDiscountInclusion,
            profile.RequiredFreightInclusion,
            profile.RequiredGrossNetBasis,
            profile.ReferenceCenter,
            profile.RuleSetVersion,
            profile.SourceProvenance,
            profile.Version,
            IsPilotProfile(profile.Id));

    private static CommercialSalesPriceAssessmentSummaryReadModel MapAssessmentSummary(
        CommercialSalesPriceAssessment assessment) =>
        new(
            assessment.Id,
            assessment.AssessmentReference,
            assessment.ProfileId,
            assessment.Profile?.ProfileCode ?? string.Empty,
            assessment.MaterialId,
            assessment.CustomerReference,
            assessment.SourceEvidenceKind,
            assessment.ComparisonDimension,
            assessment.AssessmentOutcome,
            assessment.CurrentUnitPrice,
            assessment.ReferenceMedianUnitPrice,
            assessment.VarianceRatio,
            assessment.CandidateObservationCount,
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

    private static CommercialSalesPriceAssessmentDetailReadModel MapAssessmentDetail(
        CommercialSalesPriceAssessment assessment) =>
        new(
            assessment.Id,
            assessment.AssessmentReference,
            assessment.ProfileId,
            assessment.Profile?.ProfileCode ?? string.Empty,
            assessment.MaterialId,
            assessment.CustomerReference,
            assessment.SourceEvidenceKind,
            assessment.ComparisonDimension,
            assessment.CurrentUnitPrice,
            assessment.ReferenceMedianUnitPrice,
            assessment.AbsoluteVariance,
            assessment.VarianceRatio,
            assessment.CandidateObservationCount,
            assessment.ComparableObservationCount,
            assessment.ExcludedObservationCount,
            assessment.CurrencyCode,
            assessment.NormalizedUnit,
            assessment.WindowStartUtc,
            assessment.WindowEndUtc,
            assessment.PriceBasisGrossNet,
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
                .Select(d => new CommercialSalesPriceAssessmentDriverReadModel(
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
        id == DevelopmentPilotIds.CommercialSalesPricePilotProfileId;

    private static bool IsPilotAssessment(Guid id) => false;
}

public sealed class UnavailableCommercialSalesPriceRepository : ICommercialSalesPriceRepository
{
    private static CommercialSalesPriceWriteResult Unavailable() =>
        CommercialSalesPriceWriteResult.Fail(
            CommercialIntelligenceFailureCodes.Unavailable,
            "Commercial sales price intelligence is unavailable.");

    public Task<CommercialSalesPriceWorkspaceReadModel> GetWorkspaceAsync(
        CommercialIntelligenceOperationContext context,
        bool canManage,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new CommercialSalesPriceWorkspaceReadModel(
            CommercialIntelligenceConstants.SalesRuleSetVersion,
            CommercialIntelligenceConstants.SalesDevelopmentPilotProvenance,
            CommercialIntelligenceConstants.ReferenceCenterMedian,
            CommercialIntelligenceConstants.DefaultMinimumComparableObservations,
            false,
            false,
            false));

    public Task<IReadOnlyList<CommercialSalesPriceProfileReadModel>> ListProfilesAsync(
        CommercialIntelligenceOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CommercialSalesPriceProfileReadModel>>([]);

    public Task<CommercialSalesPriceProfileReadModel?> GetProfileByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid profileId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CommercialSalesPriceProfileReadModel?>(null);

    public Task<CommercialSalesPriceWriteResult> CreateProfileAsync(
        CommercialIntelligenceOperationContext context,
        CreateCommercialSalesPriceProfileCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<CommercialSalesPriceWriteResult> UpdateProfileAsync(
        CommercialIntelligenceOperationContext context,
        UpdateCommercialSalesPriceProfileCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<IReadOnlyList<CommercialSalesPriceAssessmentSummaryReadModel>> ListAssessmentsAsync(
        CommercialIntelligenceOperationContext context,
        CommercialSalesPriceAssessmentListFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CommercialSalesPriceAssessmentSummaryReadModel>>([]);

    public Task<CommercialSalesPriceAssessmentDetailReadModel?> GetAssessmentByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CommercialSalesPriceAssessmentDetailReadModel?>(null);

    public Task<CommercialSalesPriceWriteResult> EvaluateAssessmentAsync(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialSalesPriceAssessmentCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<IReadOnlyList<CommercialSalesPriceComparisonSummaryReadModel>> ListComparisonsAsync(
        CommercialIntelligenceOperationContext context,
        CommercialSalesPriceComparisonListFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CommercialSalesPriceComparisonSummaryReadModel>>([]);

    public Task<CommercialSalesPriceComparisonDetailReadModel?> GetComparisonByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid comparisonRunId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CommercialSalesPriceComparisonDetailReadModel?>(null);

    public Task<CommercialSalesPriceWriteResult> EvaluateComparisonAsync(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialSalesPriceComparisonCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}
