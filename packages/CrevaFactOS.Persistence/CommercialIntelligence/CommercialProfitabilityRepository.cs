using CrevaFactOS.Persistence.Development;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CrevaFactOS.Persistence.CommercialIntelligence;

public sealed class CommercialProfitabilityRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : ICommercialProfitabilityRepository
{
    public Task<CommercialProfitabilityWorkspaceReadModel> GetWorkspaceAsync(
        CommercialIntelligenceOperationContext context,
        bool canManage,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new CommercialProfitabilityWorkspaceReadModel(
            CommercialIntelligenceConstants.ProfitabilityRuleSetVersion,
            CommercialIntelligenceConstants.ProfitabilityDevelopmentPilotProvenance,
            CommercialIntelligenceConstants.DefaultProfitabilityMinimumContributionMarginRatio,
            CommercialIntelligenceConstants.DefaultProfitabilityMaximumEvidenceAgeDays,
            CommercialProfitabilityCostComponentKinds.DefaultRequiredKinds,
            RealRevenueMappingComplete: false,
            RealCostMappingComplete: false,
            FullCostMappingComplete: false,
            RealProfitabilityMappingComplete: false,
            true,
            canManage));

    public Task<IReadOnlyList<CommercialProfitabilityProfileReadModel>> ListProfilesAsync(
        CommercialIntelligenceOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var profiles = await dbContext.CommercialProfitabilityProfiles.AsNoTracking()
                    .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId)
                    .OrderBy(p => p.ProfileCode)
                    .ToListAsync(ct);

                return (IReadOnlyList<CommercialProfitabilityProfileReadModel>)profiles.Select(MapProfile).ToList();
            },
            cancellationToken);

    public Task<CommercialProfitabilityProfileReadModel?> GetProfileByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid profileId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var profile = await dbContext.CommercialProfitabilityProfiles.AsNoTracking()
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId
                             && p.Id == profileId,
                        ct);

                return profile is null ? null : MapProfile(profile);
            },
            cancellationToken);

    public Task<CommercialProfitabilityWriteResult> CreateProfileAsync(
        CommercialIntelligenceOperationContext context,
        CreateCommercialProfitabilityProfileCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateCreateProfileCommand(command);
                if (validationError is not null)
                {
                    return CommercialProfitabilityWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        validationError);
                }

                var fingerprint = CommercialProfitabilityIdempotencyFingerprint.ComputeProfileFingerprint(context, command);
                var existing = await dbContext.CommercialProfitabilityProfiles.AsNoTracking()
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId && p.CreateIdempotencyKey == command.IdempotencyKey.Trim(),
                        ct);

                if (existing is not null)
                {
                    return ResolveIdempotentProfileReplay(existing, fingerprint);
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var profile = new CommercialProfitabilityProfile
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    ProfileCode = command.ProfileCode.Trim(),
                    Name = command.Name.Trim(),
                    Status = CommercialIntelligenceProfileStatuses.Draft,
                    MinimumContributionMarginRatio = command.MinimumContributionMarginRatio,
                    MaximumEvidenceAgeDays = command.MaximumEvidenceAgeDays,
                    RequiredCostComponentKinds = command.RequiredCostComponentKinds.Trim(),
                    RequiredCurrencyCode = command.RequiredCurrencyCode.Trim(),
                    RequiredUnitOfMeasure = command.RequiredUnitOfMeasure.Trim(),
                    RequiredGrossNetBasis = command.RequiredGrossNetBasis.Trim(),
                    RequiredTaxInclusion = command.RequiredTaxInclusion.Trim(),
                    RequiredDiscountInclusion = command.RequiredDiscountInclusion.Trim(),
                    RequiredFreightInclusion = command.RequiredFreightInclusion.Trim(),
                    AllowedEvidenceStatuses = command.AllowedEvidenceStatuses.Trim(),
                    RuleSetVersion = CommercialIntelligenceConstants.ProfitabilityRuleSetVersion,
                    SourceProvenance = command.SourceProvenance.Trim(),
                    CreateIdempotencyKey = command.IdempotencyKey.Trim(),
                    CreateIdempotencyPayloadFingerprint = fingerprint,
                    Version = 1,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedBy = actor,
                    UpdatedBy = actor,
                };

                dbContext.CommercialProfitabilityProfiles.Add(profile);
                await dbContext.SaveChangesAsync(ct);
                return CommercialProfitabilityWriteResult.Success(profile.Id, profile.Version);
            },
            cancellationToken);

    public Task<CommercialProfitabilityWriteResult> UpdateProfileAsync(
        CommercialIntelligenceOperationContext context,
        UpdateCommercialProfitabilityProfileCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateUpdateProfileCommand(command);
                if (validationError is not null)
                {
                    return CommercialProfitabilityWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        validationError);
                }

                var profile = await dbContext.CommercialProfitabilityProfiles
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId
                             && p.Id == command.ProfileId,
                        ct);

                if (profile is null)
                {
                    return CommercialProfitabilityWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.NotFound,
                        "Profitability profile not found.");
                }

                if (profile.Version != command.ExpectedVersion)
                {
                    return CommercialProfitabilityWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ConcurrencyConflict,
                        "Profitability profile version is stale.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                profile.Name = command.Name.Trim();
                profile.Status = command.Status.Trim();
                profile.MinimumContributionMarginRatio = command.MinimumContributionMarginRatio;
                profile.MaximumEvidenceAgeDays = command.MaximumEvidenceAgeDays;
                profile.RequiredCostComponentKinds = command.RequiredCostComponentKinds.Trim();
                profile.RequiredCurrencyCode = command.RequiredCurrencyCode.Trim();
                profile.RequiredUnitOfMeasure = command.RequiredUnitOfMeasure.Trim();
                profile.RequiredGrossNetBasis = command.RequiredGrossNetBasis.Trim();
                profile.RequiredTaxInclusion = command.RequiredTaxInclusion.Trim();
                profile.RequiredDiscountInclusion = command.RequiredDiscountInclusion.Trim();
                profile.RequiredFreightInclusion = command.RequiredFreightInclusion.Trim();
                profile.AllowedEvidenceStatuses = command.AllowedEvidenceStatuses.Trim();
                profile.SourceProvenance = command.SourceProvenance.Trim();
                profile.UpdatedAtUtc = now;
                profile.UpdatedBy = actor;
                profile.Version += 1;

                await dbContext.SaveChangesAsync(ct);
                return CommercialProfitabilityWriteResult.Success(profile.Id, profile.Version);
            },
            cancellationToken);

    public Task<IReadOnlyList<CommercialProfitabilityAssessmentSummaryReadModel>> ListAssessmentsAsync(
        CommercialIntelligenceOperationContext context,
        CommercialProfitabilityAssessmentListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var page = filters.Page < 1 ? 1 : filters.Page;
                var pageSize = filters.PageSize is < 1 or > 100 ? 25 : filters.PageSize;

                var query = dbContext.CommercialProfitabilityAssessments.AsNoTracking()
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

                return (IReadOnlyList<CommercialProfitabilityAssessmentSummaryReadModel>)assessments
                    .Select(MapAssessmentSummary)
                    .ToList();
            },
            cancellationToken);

    public Task<CommercialProfitabilityAssessmentDetailReadModel?> GetAssessmentByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var assessment = await dbContext.CommercialProfitabilityAssessments.AsNoTracking()
                    .Include(a => a.Profile)
                    .Include(a => a.CostComponents)
                    .Include(a => a.Drivers)
                    .SingleOrDefaultAsync(
                        a => a.TenantId == context.TenantId
                             && a.FacilityId == context.FacilityId
                             && a.Id == assessmentId,
                        ct);

                return assessment is null ? null : MapAssessmentDetail(assessment);
            },
            cancellationToken);

    public Task<CommercialProfitabilityWriteResult> EvaluateAssessmentAsync(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialProfitabilityAssessmentCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateEvaluateCommand(command);
                if (validationError is not null)
                {
                    return CommercialProfitabilityWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        validationError);
                }

                var fingerprint = CommercialProfitabilityIdempotencyFingerprint.ComputeAssessmentFingerprint(context, command);
                var existing = await dbContext.CommercialProfitabilityAssessments.AsNoTracking()
                    .Include(a => a.Profile)
                    .Include(a => a.CostComponents)
                    .Include(a => a.Drivers)
                    .SingleOrDefaultAsync(
                        a => a.TenantId == context.TenantId && a.IdempotencyKey == command.IdempotencyKey.Trim(),
                        ct);

                if (existing is not null)
                {
                    return ResolveIdempotentAssessmentReplay(existing, fingerprint);
                }

                var profile = await dbContext.CommercialProfitabilityProfiles
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId
                             && p.Id == command.ProfileId,
                        ct);

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
                        "Only approved profitability profiles may execute assessments.");
                }

                var salesEvidence = await dbContext.CommercialProfitabilityEvidenceObservations.AsNoTracking()
                    .SingleOrDefaultAsync(
                        e => e.TenantId == context.TenantId
                             && e.FacilityId == context.FacilityId
                             && e.Id == command.SalesEvidenceObservationId
                             && e.ObservationKind == CommercialProfitabilityObservationKinds.Sales,
                        ct);

                if (salesEvidence is null)
                {
                    return CommercialProfitabilityWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.NotFound,
                        "Sales evidence observation not found.");
                }

                if (!salesEvidence.ComparableNetSalesUnitPrice.HasValue
                    || salesEvidence.ComparableNetSalesUnitPrice.Value <= 0m)
                {
                    return CommercialProfitabilityWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        "Comparable net sales unit price must be positive.");
                }

                var now = DateTimeOffset.UtcNow;
                var costEvidence = await CommercialProfitabilityCostEvidenceReader.ReadAsync(
                    dbContext,
                    context,
                    profile,
                    salesEvidence,
                    now,
                    ct);
                var requiredKinds = CommercialProfitabilityEvaluator.ParseRequiredKinds(profile.RequiredCostComponentKinds);
                var evaluation = CommercialProfitabilityEvaluator.Evaluate(
                    profile,
                    salesEvidence,
                    costEvidence,
                    requiredKinds,
                    now);

                var actor = context.IdentitySubjectId.ToString();
                var assessment = new CommercialProfitabilityAssessment
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    AssessmentReference = await GenerateAssessmentReferenceAsync(context, ct),
                    ProfileId = profile.Id,
                    ProfileVersion = profile.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    SalesEvidenceObservationId = salesEvidence.Id,
                    CustomerReference = salesEvidence.CustomerReference,
                    MaterialId = salesEvidence.MaterialId,
                    OrderReference = salesEvidence.OrderReference,
                    QuotationReference = salesEvidence.QuotationReference,
                    EvidenceSetReference = salesEvidence.EvidenceSetReference,
                    ComparableNetSalesUnitPrice = evaluation.ComparableNetSalesUnitPrice,
                    TotalIncludedUnitCost = evaluation.TotalIncludedUnitCost,
                    UnitContribution = evaluation.UnitContribution,
                    ContributionMarginRatio = evaluation.ContributionMarginRatio,
                    MarkupRatio = evaluation.MarkupRatio,
                    CandidateCostComponentCount = evaluation.CandidateCostComponentCount,
                    IncludedCostComponentCount = evaluation.IncludedCostComponentCount,
                    ExcludedCostComponentCount = evaluation.ExcludedCostComponentCount,
                    RequiredCostComponentCount = evaluation.RequiredCostComponentCount,
                    AvailableRequiredCostComponentCount = evaluation.AvailableRequiredCostComponentCount,
                    MissingRequiredCostComponentCount = evaluation.MissingRequiredCostComponentCount,
                    MissingRequiredComponentKinds = evaluation.MissingRequiredComponentKinds,
                    CostCoverageStatus = evaluation.CostCoverageStatus,
                    CurrencyCode = salesEvidence.CurrencyCode,
                    UnitOfMeasure = salesEvidence.UnitOfMeasure,
                    GrossNetBasis = salesEvidence.GrossNetBasis,
                    TaxBasis = salesEvidence.TaxBasis,
                    DiscountBasis = salesEvidence.DiscountBasis,
                    FreightBasis = salesEvidence.FreightBasis,
                    EvidenceStatus = evaluation.EvidenceStatus,
                    Outcome = evaluation.Outcome,
                    LeadingDriverCode = evaluation.LeadingDriverCode,
                    RuleSetVersion = CommercialIntelligenceConstants.ProfitabilityRuleSetVersion,
                    IdempotencyKey = command.IdempotencyKey.Trim(),
                    IdempotencyPayloadFingerprint = fingerprint,
                    SourceProvenance = command.SourceProvenance.Trim(),
                    EvaluatedAtUtc = now,
                    CreatedAtUtc = now,
                    CreatedBy = actor,
                };

                var componentOrder = 0;
                foreach (var component in evaluation.ComponentSnapshots)
                {
                    componentOrder++;
                    assessment.CostComponents.Add(new CommercialProfitabilityAssessmentCostComponent
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        AssessmentId = assessment.Id,
                        SourceEvidenceObservationId = component.ObservationId == Guid.Empty ? null : component.ObservationId,
                        ComponentKind = component.ComponentKind,
                        UnitCost = component.UnitCost,
                        CurrencyCode = component.CurrencyCode,
                        UnitOfMeasure = component.UnitOfMeasure,
                        CostBasis = component.CostBasis,
                        AllocationBasis = component.AllocationBasis,
                        EvidenceTimestampUtc = component.EvidenceTimestampUtc,
                        SourceKind = component.SourceKind,
                        SourceReference = component.SourceReference,
                        EvidenceStatus = component.EvidenceStatus,
                        InclusionStatus = component.InclusionStatus,
                        ExclusionReason = component.ExclusionReason,
                        SourceProvenance = command.SourceProvenance.Trim(),
                        SortOrder = componentOrder,
                        CreatedAtUtc = now,
                    });
                }

                var orderedDrivers = CommercialProfitabilityEvaluator.OrderDrivers(evaluation.Drivers);
                for (var index = 0; index < orderedDrivers.Count; index++)
                {
                    var driver = orderedDrivers[index];
                    assessment.Drivers.Add(new CommercialProfitabilityAssessmentDriver
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
                        AffectedComponentCount = driver.AffectedComponentCount,
                        SupportingText = driver.SupportingText,
                        SourceProvenance = command.SourceProvenance.Trim(),
                        SortOrder = index + 1,
                        IsLeadingDriver = index == 0,
                        CreatedAtUtc = now,
                    });
                }

                dbContext.CommercialProfitabilityAssessments.Add(assessment);
                try
                {
                    await dbContext.SaveChangesAsync(ct);
                }
                catch (DbUpdateException ex) when (IsUniqueIdempotencyViolation(ex))
                {
                    var replay = await dbContext.CommercialProfitabilityAssessments.AsNoTracking()
                        .Include(a => a.Profile)
                        .Include(a => a.CostComponents)
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

                return CommercialProfitabilityWriteResult.Success(
                    assessment.Id,
                    0,
                    assessment: MapAssessmentDetail(assessment));
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
            var reference = $"CI-PF-{suffix}";
            var exists = await dbContext.CommercialProfitabilityAssessments.AsNoTracking()
                .AnyAsync(a => a.TenantId == context.TenantId && a.AssessmentReference == reference, cancellationToken);
            if (!exists)
            {
                return reference;
            }
        }

        return $"CI-PF-{Guid.NewGuid():N}".ToUpperInvariant();
    }

    private static CommercialProfitabilityWriteResult ResolveIdempotentProfileReplay(
        CommercialProfitabilityProfile existing,
        string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(existing.CreateIdempotencyPayloadFingerprint))
        {
            return CommercialProfitabilityWriteResult.Fail(
                CommercialIntelligenceFailureCodes.IdempotencyLegacyUnverifiable,
                "Existing profile idempotency fingerprint is unavailable.");
        }

        if (!string.Equals(existing.CreateIdempotencyPayloadFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return CommercialProfitabilityWriteResult.Fail(
                CommercialIntelligenceFailureCodes.IdempotencyConflict,
                "Idempotency key was already used with a different payload.");
        }

        return CommercialProfitabilityWriteResult.Success(existing.Id, existing.Version, idempotentReplay: true);
    }

    private static CommercialProfitabilityWriteResult ResolveIdempotentAssessmentReplay(
        CommercialProfitabilityAssessment existing,
        string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(existing.IdempotencyPayloadFingerprint))
        {
            return CommercialProfitabilityWriteResult.Fail(
                CommercialIntelligenceFailureCodes.IdempotencyLegacyUnverifiable,
                "Existing assessment idempotency fingerprint is unavailable.");
        }

        if (!string.Equals(existing.IdempotencyPayloadFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return CommercialProfitabilityWriteResult.Fail(
                CommercialIntelligenceFailureCodes.IdempotencyConflict,
                "Idempotency key was already used with a different payload.");
        }

        return CommercialProfitabilityWriteResult.Success(
            existing.Id,
            0,
            idempotentReplay: true,
            assessment: MapAssessmentDetail(existing));
    }

    private static string? ValidateCreateProfileCommand(CreateCommercialProfitabilityProfileCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ProfileCode)) return "Profile code is required.";
        if (string.IsNullOrWhiteSpace(command.Name)) return "Profile name is required.";
        if (command.MinimumContributionMarginRatio < 0m) return "Minimum contribution margin ratio must be non-negative.";
        if (command.MaximumEvidenceAgeDays < 1) return "Maximum evidence age days must be at least 1.";
        if (string.IsNullOrWhiteSpace(command.RequiredCostComponentKinds)) return "Required cost component kinds are required.";
        if (string.IsNullOrWhiteSpace(command.SourceProvenance)) return "Source provenance is required.";
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey)) return "Idempotency key is required.";
        if (!CommercialSalesPriceGrossNetBasis.All.Contains(command.RequiredGrossNetBasis.Trim()))
            return "Required gross/net basis is invalid.";
        return null;
    }

    private static string? ValidateUpdateProfileCommand(UpdateCommercialProfitabilityProfileCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name)) return "Profile name is required.";
        if (!CommercialIntelligenceProfileStatuses.All.Contains(command.Status.Trim())) return "Profile status is invalid.";
        if (command.MinimumContributionMarginRatio < 0m) return "Minimum contribution margin ratio must be non-negative.";
        if (!CommercialSalesPriceGrossNetBasis.All.Contains(command.RequiredGrossNetBasis.Trim()))
            return "Required gross/net basis is invalid.";
        return null;
    }

    private static string? ValidateEvaluateCommand(EvaluateCommercialProfitabilityAssessmentCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.SourceProvenance)) return "Source provenance is required.";
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey)) return "Idempotency key is required.";
        return null;
    }

    private static CommercialProfitabilityProfileReadModel MapProfile(CommercialProfitabilityProfile profile) =>
        new(
            profile.Id,
            profile.ProfileCode,
            profile.Name,
            profile.Status,
            profile.MinimumContributionMarginRatio,
            profile.MaximumEvidenceAgeDays,
            profile.RequiredCostComponentKinds,
            profile.RequiredCurrencyCode,
            profile.RequiredUnitOfMeasure,
            profile.RequiredGrossNetBasis,
            profile.RequiredTaxInclusion,
            profile.RequiredDiscountInclusion,
            profile.RequiredFreightInclusion,
            profile.AllowedEvidenceStatuses,
            profile.RuleSetVersion,
            profile.SourceProvenance,
            profile.Version,
            IsPilotProfile(profile.Id));

    private static CommercialProfitabilityAssessmentSummaryReadModel MapAssessmentSummary(
        CommercialProfitabilityAssessment assessment) =>
        new(
            assessment.Id,
            assessment.AssessmentReference,
            assessment.ProfileId,
            assessment.Profile?.ProfileCode ?? string.Empty,
            assessment.SalesEvidenceObservationId,
            assessment.CustomerReference,
            assessment.MaterialId,
            assessment.Outcome,
            assessment.ComparableNetSalesUnitPrice,
            assessment.TotalIncludedUnitCost,
            assessment.UnitContribution,
            assessment.ContributionMarginRatio,
            assessment.MarkupRatio,
            assessment.CostCoverageStatus,
            assessment.CandidateCostComponentCount,
            assessment.IncludedCostComponentCount,
            assessment.ExcludedCostComponentCount,
            assessment.LeadingDriverCode,
            assessment.RuleSetVersion,
            assessment.EvaluatedAtUtc,
            assessment.CreatedAtUtc,
            IsPilotAssessment(assessment.Id));

    private static CommercialProfitabilityAssessmentDetailReadModel MapAssessmentDetail(
        CommercialProfitabilityAssessment assessment) =>
        new(
            assessment.Id,
            assessment.AssessmentReference,
            assessment.ProfileId,
            assessment.Profile?.ProfileCode ?? string.Empty,
            assessment.ProfileVersion,
            assessment.SalesEvidenceObservationId,
            assessment.CustomerReference,
            assessment.MaterialId,
            assessment.OrderReference,
            assessment.QuotationReference,
            assessment.EvidenceSetReference,
            assessment.ComparableNetSalesUnitPrice,
            assessment.TotalIncludedUnitCost,
            assessment.UnitContribution,
            assessment.ContributionMarginRatio,
            assessment.MarkupRatio,
            assessment.CandidateCostComponentCount,
            assessment.IncludedCostComponentCount,
            assessment.ExcludedCostComponentCount,
            assessment.RequiredCostComponentCount,
            assessment.AvailableRequiredCostComponentCount,
            assessment.MissingRequiredCostComponentCount,
            assessment.MissingRequiredComponentKinds,
            assessment.CostCoverageStatus,
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
            assessment.CostComponents
                .OrderBy(c => c.SortOrder)
                .Select(c => new CommercialProfitabilityAssessmentCostComponentReadModel(
                    c.Id,
                    c.ComponentKind,
                    c.UnitCost,
                    c.CurrencyCode,
                    c.UnitOfMeasure,
                    c.CostBasis,
                    c.AllocationBasis,
                    c.EvidenceTimestampUtc,
                    c.SourceKind,
                    c.SourceReference,
                    c.EvidenceStatus,
                    c.InclusionStatus,
                    c.ExclusionReason,
                    c.SortOrder))
                .ToList(),
            assessment.Drivers
                .OrderBy(d => d.SortOrder)
                .Select(d => new CommercialProfitabilityAssessmentDriverReadModel(
                    d.Id,
                    d.ReasonCode,
                    d.Severity,
                    d.ActualValue,
                    d.ThresholdValue,
                    d.Unit,
                    d.NormalizedVariance,
                    d.AffectedComponentCount,
                    d.SupportingText,
                    d.SortOrder,
                    d.IsLeadingDriver))
                .ToList(),
            IsPilotAssessment(assessment.Id));

    private static bool IsPilotProfile(Guid profileId) =>
        profileId == DevelopmentPilotIds.CommercialProfitabilityPilotProfileId;

    private static bool IsPilotAssessment(Guid assessmentId) =>
        DevelopmentPilotIds.CommercialProfitabilityPilotAssessmentIds.Contains(assessmentId);
}

public sealed class UnavailableCommercialProfitabilityRepository : ICommercialProfitabilityRepository
{
    private static CommercialProfitabilityWriteResult Unavailable() =>
        CommercialProfitabilityWriteResult.Fail(
            CommercialIntelligenceFailureCodes.Unavailable,
            "Commercial profitability persistence is unavailable.");

    public Task<CommercialProfitabilityWorkspaceReadModel> GetWorkspaceAsync(
        CommercialIntelligenceOperationContext context,
        bool canManage,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Commercial profitability persistence is unavailable.");

    public Task<IReadOnlyList<CommercialProfitabilityProfileReadModel>> ListProfilesAsync(
        CommercialIntelligenceOperationContext context,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Commercial profitability persistence is unavailable.");

    public Task<CommercialProfitabilityProfileReadModel?> GetProfileByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid profileId,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Commercial profitability persistence is unavailable.");

    public Task<CommercialProfitabilityWriteResult> CreateProfileAsync(
        CommercialIntelligenceOperationContext context,
        CreateCommercialProfitabilityProfileCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<CommercialProfitabilityWriteResult> UpdateProfileAsync(
        CommercialIntelligenceOperationContext context,
        UpdateCommercialProfitabilityProfileCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<IReadOnlyList<CommercialProfitabilityAssessmentSummaryReadModel>> ListAssessmentsAsync(
        CommercialIntelligenceOperationContext context,
        CommercialProfitabilityAssessmentListFilters filters,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Commercial profitability persistence is unavailable.");

    public Task<CommercialProfitabilityAssessmentDetailReadModel?> GetAssessmentByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Commercial profitability persistence is unavailable.");

    public Task<CommercialProfitabilityWriteResult> EvaluateAssessmentAsync(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialProfitabilityAssessmentCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}
