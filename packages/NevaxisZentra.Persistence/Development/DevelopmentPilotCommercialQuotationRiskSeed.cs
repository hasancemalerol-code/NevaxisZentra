using NevaxisZentra.Persistence.CommercialIntelligence;
using NevaxisZentra.Persistence.Material;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Development;

internal static class DevelopmentPilotCommercialQuotationRiskSeed
{
    private const string SalesEvidenceKind = CommercialSalesPriceEvidenceKinds.ManualDevelopmentSimulator;
    private const string QuotationSourceKind = CommercialQuotationRiskSourceKinds.ManualDevelopmentSimulator;

    internal static async Task EnsureCommercialQuotationRiskPilotRecordsAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var provenance = CommercialIntelligenceConstants.QuotationRiskDevelopmentPilotProvenance;
        var evaluatedAt = DevelopmentPilotIds.CommercialQuotationRiskPilotSeedTimeUtc;
        var windowStart = DevelopmentPilotIds.CommercialQuotationRiskPilotReferenceWindowStartUtc;
        var windowEnd = DevelopmentPilotIds.CommercialQuotationRiskPilotReferenceWindowEndUtc;

        var profile = await EnsurePilotProfileAsync(
            dbContext,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            cancellationToken);

        foreach (var scenario in PilotScenarios)
        {
            await EnsurePilotMaterialAsync(
                dbContext,
                scenario,
                tenantId,
                facilityId,
                actorSubjectId,
                seedTimestampUtc,
                cancellationToken);

            await SeedSalesEvidenceAsync(
                dbContext,
                scenario,
                tenantId,
                facilityId,
                actorSubjectId,
                seedTimestampUtc,
                provenance,
                windowStart,
                cancellationToken);

            await EnsureQuotationEvidenceAsync(
                dbContext,
                scenario,
                tenantId,
                facilityId,
                actorSubjectId,
                seedTimestampUtc,
                provenance,
                evaluatedAt,
                cancellationToken);
        }

        await EnsurePilotAssessmentsAsync(
            dbContext,
            profile,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            evaluatedAt,
            windowStart,
            windowEnd,
            cancellationToken);
    }

    private static readonly PilotScenario[] PilotScenarios =
    [
        new(
            DevelopmentPilotIds.CommercialQuotationRiskPilotMaterialWithinThresholdId,
            "COMMQUOTE1-MAT-WITHIN",
            DevelopmentPilotIds.CommercialQuotationRiskPilotCustomerWithin,
            DevelopmentPilotIds.CommercialQuotationRiskPilotEvidenceWithinThresholdId,
            DevelopmentPilotIds.CommercialQuotationRiskPilotAssessmentWithinThresholdId,
            "COMMQUOTE1-ASM-WITHIN",
            "commquote1-pilot-within-threshold",
            "COMMQUOTE1-SET-WITHIN",
            100m,
            30,
            30,
            CommercialQuotationRiskEvidenceStatuses.Simulated,
            6,
            100m,
            0),
        new(
            DevelopmentPilotIds.CommercialQuotationRiskPilotMaterialBelowReferenceId,
            "COMMQUOTE1-MAT-BELOW",
            DevelopmentPilotIds.CommercialQuotationRiskPilotCustomerBelow,
            DevelopmentPilotIds.CommercialQuotationRiskPilotEvidenceBelowReferenceId,
            DevelopmentPilotIds.CommercialQuotationRiskPilotAssessmentBelowReferenceId,
            "COMMQUOTE1-ASM-BELOW",
            "commquote1-pilot-below-reference",
            "COMMQUOTE1-SET-BELOW",
            89m,
            30,
            30,
            CommercialQuotationRiskEvidenceStatuses.Simulated,
            6,
            100m,
            1),
        new(
            DevelopmentPilotIds.CommercialQuotationRiskPilotMaterialAboveReferenceId,
            "COMMQUOTE1-MAT-ABOVE",
            DevelopmentPilotIds.CommercialQuotationRiskPilotCustomerAbove,
            DevelopmentPilotIds.CommercialQuotationRiskPilotEvidenceAboveReferenceId,
            DevelopmentPilotIds.CommercialQuotationRiskPilotAssessmentAboveReferenceId,
            "COMMQUOTE1-ASM-ABOVE",
            "commquote1-pilot-above-reference",
            "COMMQUOTE1-SET-ABOVE",
            116m,
            30,
            30,
            CommercialQuotationRiskEvidenceStatuses.Simulated,
            6,
            100m,
            2),
        new(
            DevelopmentPilotIds.CommercialQuotationRiskPilotMaterialExpiringSoonId,
            "COMMQUOTE1-MAT-EXPIRING",
            DevelopmentPilotIds.CommercialQuotationRiskPilotCustomerExpiring,
            DevelopmentPilotIds.CommercialQuotationRiskPilotEvidenceExpiringSoonId,
            DevelopmentPilotIds.CommercialQuotationRiskPilotAssessmentExpiringSoonId,
            "COMMQUOTE1-ASM-EXPIRING",
            "commquote1-pilot-expiring-soon",
            "COMMQUOTE1-SET-EXPIRING",
            100m,
            30,
            7,
            CommercialQuotationRiskEvidenceStatuses.Simulated,
            6,
            100m,
            3),
        new(
            DevelopmentPilotIds.CommercialQuotationRiskPilotMaterialExpiredId,
            "COMMQUOTE1-MAT-EXPIRED",
            DevelopmentPilotIds.CommercialQuotationRiskPilotCustomerExpired,
            DevelopmentPilotIds.CommercialQuotationRiskPilotEvidenceExpiredId,
            DevelopmentPilotIds.CommercialQuotationRiskPilotAssessmentExpiredId,
            "COMMQUOTE1-ASM-EXPIRED",
            "commquote1-pilot-expired",
            "COMMQUOTE1-SET-EXPIRED",
            100m,
            30,
            0,
            CommercialQuotationRiskEvidenceStatuses.Simulated,
            6,
            100m,
            4),
        new(
            DevelopmentPilotIds.CommercialQuotationRiskPilotMaterialPaymentExceededId,
            "COMMQUOTE1-MAT-PAYMENT",
            DevelopmentPilotIds.CommercialQuotationRiskPilotCustomerPayment,
            DevelopmentPilotIds.CommercialQuotationRiskPilotEvidencePaymentExceededId,
            DevelopmentPilotIds.CommercialQuotationRiskPilotAssessmentPaymentExceededId,
            "COMMQUOTE1-ASM-PAYMENT",
            "commquote1-pilot-payment-exceeded",
            "COMMQUOTE1-SET-PAYMENT",
            100m,
            91,
            30,
            CommercialQuotationRiskEvidenceStatuses.Simulated,
            6,
            100m,
            5),
        new(
            DevelopmentPilotIds.CommercialQuotationRiskPilotMaterialInsufficientId,
            "COMMQUOTE1-MAT-INSUFFICIENT",
            DevelopmentPilotIds.CommercialQuotationRiskPilotCustomerInsufficient,
            DevelopmentPilotIds.CommercialQuotationRiskPilotEvidenceInsufficientId,
            DevelopmentPilotIds.CommercialQuotationRiskPilotAssessmentInsufficientId,
            "COMMQUOTE1-ASM-INSUFFICIENT",
            "commquote1-pilot-insufficient-evidence",
            "COMMQUOTE1-SET-INSUFFICIENT",
            100m,
            30,
            30,
            CommercialQuotationRiskEvidenceStatuses.Simulated,
            3,
            100m,
            6),
        new(
            DevelopmentPilotIds.CommercialQuotationRiskPilotMaterialSourceConflictId,
            "COMMQUOTE1-MAT-CONFLICT",
            DevelopmentPilotIds.CommercialQuotationRiskPilotCustomerConflict,
            DevelopmentPilotIds.CommercialQuotationRiskPilotEvidenceSourceConflictId,
            DevelopmentPilotIds.CommercialQuotationRiskPilotAssessmentSourceConflictId,
            "COMMQUOTE1-ASM-CONFLICT",
            "commquote1-pilot-source-conflict",
            "COMMQUOTE1-SET-CONFLICT",
            100m,
            30,
            30,
            CommercialQuotationRiskEvidenceStatuses.SourceConflict,
            6,
            100m,
            7),
    ];

    private static async Task<CommercialQuotationRiskProfile> EnsurePilotProfileAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.CommercialQuotationRiskProfiles
            .SingleOrDefaultAsync(
                p => p.Id == DevelopmentPilotIds.CommercialQuotationRiskPilotProfileId,
                cancellationToken);

        if (profile is not null)
        {
            var changed = false;
            if (profile.MinimumComparableObservations != CommercialIntelligenceConstants.DefaultMinimumComparableObservations)
            {
                profile.MinimumComparableObservations = CommercialIntelligenceConstants.DefaultMinimumComparableObservations;
                changed = true;
            }

            if (profile.MaximumNegativePriceVarianceRatio
                != CommercialIntelligenceConstants.DefaultQuotationRiskMaximumNegativeVarianceRatio)
            {
                profile.MaximumNegativePriceVarianceRatio =
                    CommercialIntelligenceConstants.DefaultQuotationRiskMaximumNegativeVarianceRatio;
                changed = true;
            }

            if (profile.MaximumPositivePriceVarianceRatio
                != CommercialIntelligenceConstants.DefaultQuotationRiskMaximumPositiveVarianceRatio)
            {
                profile.MaximumPositivePriceVarianceRatio =
                    CommercialIntelligenceConstants.DefaultQuotationRiskMaximumPositiveVarianceRatio;
                changed = true;
            }

            if (profile.MaximumPaymentTermDays != CommercialIntelligenceConstants.DefaultQuotationRiskMaximumPaymentTermDays)
            {
                profile.MaximumPaymentTermDays = CommercialIntelligenceConstants.DefaultQuotationRiskMaximumPaymentTermDays;
                changed = true;
            }

            if (profile.ExpiryWarningDays != CommercialIntelligenceConstants.DefaultQuotationRiskExpiryWarningDays)
            {
                profile.ExpiryWarningDays = CommercialIntelligenceConstants.DefaultQuotationRiskExpiryWarningDays;
                changed = true;
            }

            if (!string.Equals(profile.Status, CommercialIntelligenceProfileStatuses.Approved, StringComparison.Ordinal))
            {
                profile.Status = CommercialIntelligenceProfileStatuses.Approved;
                changed = true;
            }

            if (changed)
            {
                profile.UpdatedAtUtc = seedTimestampUtc;
                profile.UpdatedBy = actorSubjectId;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return profile;
        }

        profile = new CommercialQuotationRiskProfile
        {
            Id = DevelopmentPilotIds.CommercialQuotationRiskPilotProfileId,
            TenantId = tenantId,
            FacilityId = facilityId,
            ProfileCode = DevelopmentPilotIds.CommercialQuotationRiskPilotProfileCode,
            Name = "Development pilot quotation risk profile",
            Status = CommercialIntelligenceProfileStatuses.Approved,
            ReferenceWindowDurationDays = 90,
            MinimumComparableObservations = CommercialIntelligenceConstants.DefaultMinimumComparableObservations,
            MaximumNegativePriceVarianceRatio =
                CommercialIntelligenceConstants.DefaultQuotationRiskMaximumNegativeVarianceRatio,
            MaximumPositivePriceVarianceRatio =
                CommercialIntelligenceConstants.DefaultQuotationRiskMaximumPositiveVarianceRatio,
            MaximumPaymentTermDays = CommercialIntelligenceConstants.DefaultQuotationRiskMaximumPaymentTermDays,
            ExpiryWarningDays = CommercialIntelligenceConstants.DefaultQuotationRiskExpiryWarningDays,
            MaximumEvidenceAgeDays = CommercialIntelligenceConstants.DefaultQuotationRiskMaximumEvidenceAgeDays,
            AllowedEvidenceStatuses = CommercialQuotationRiskEvidenceStatuses.Simulated,
            AllowedSalesEvidenceKinds = CommercialSalesPriceEvidenceKinds.ManualDevelopmentSimulator,
            RequiredTaxInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            RequiredDiscountInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            RequiredFreightInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            RequiredGrossNetBasis = CommercialSalesPriceGrossNetBasis.Net,
            RuleSetVersion = CommercialIntelligenceConstants.QuotationRiskRuleSetVersion,
            SourceProvenance = provenance,
            CreateIdempotencyKey = "commquote1-pilot-profile-bootstrap",
            CreateIdempotencyPayloadFingerprint = "commquote1-pilot-profile-bootstrap-fingerprint",
            Version = 1,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
            UpdatedBy = actorSubjectId,
        };

        dbContext.CommercialQuotationRiskProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
        return profile;
    }

    private static async Task EnsurePilotMaterialAsync(
        NevaxisZentraDbContext dbContext,
        PilotScenario scenario,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Materials
            .SingleOrDefaultAsync(m => m.Id == scenario.MaterialId, cancellationToken);
        if (existing is not null)
        {
            var changed = false;
            if (!string.Equals(existing.Code, scenario.MaterialCode, StringComparison.Ordinal))
            {
                existing.Code = scenario.MaterialCode;
                existing.CodeNormalized = scenario.MaterialCode;
                changed = true;
            }

            if (!string.Equals(existing.Name, scenario.MaterialName, StringComparison.Ordinal))
            {
                existing.Name = scenario.MaterialName;
                changed = true;
            }

            if (changed)
            {
                existing.UpdatedAtUtc = seedTimestampUtc;
                existing.UpdatedBySubjectId = actorSubjectId;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        dbContext.Materials.Add(new MaterialRegistryEntry
        {
            Id = scenario.MaterialId,
            TenantId = tenantId,
            FacilityId = facilityId,
            Code = scenario.MaterialCode,
            CodeNormalized = scenario.MaterialCode,
            Name = scenario.MaterialName,
            Description = "Deterministic development pilot material for quotation-risk scenarios.",
            MaterialCategory = MaterialCategory.VirginPolymer,
            PolymerFamily = PolymerFamily.Pp,
            Manufacturer = "Pilot Polimer A.Ş.",
            TradeNameOrGrade = scenario.MaterialCode,
            ColorName = "Doğal",
            PhysicalForm = PhysicalForm.Granule,
            LifecycleStatus = MaterialLifecycleStatus.Active,
            UnitOfMeasure = "kg",
            IsActive = true,
            SourceType = MaterialSourceType.DevelopmentPilot,
            SourceSystem = "development-pilot",
            SourceReference = scenario.MaterialCode.ToLowerInvariant(),
            DataRecordedAtUtc = seedTimestampUtc,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = actorSubjectId,
            UpdatedBySubjectId = actorSubjectId,
            Version = 1,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedSalesEvidenceAsync(
        NevaxisZentraDbContext dbContext,
        PilotScenario scenario,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        DateTimeOffset windowStart,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < scenario.SalesObservationCount; index++)
        {
            var currencyCode = "TRY";
            var normalizedUnit = "kg";
            var grossNet = CommercialSalesPriceGrossNetBasis.Net;
            var price = scenario.SalesUnitPrice;

            await EnsureSalesObservationAsync(
                dbContext,
                SalesObservationId(scenario.ScenarioIndex, index),
                tenantId,
                facilityId,
                scenario.MaterialId,
                scenario.CustomerReference,
                scenario.ObservationSetCode,
                $"SO-SIM-QR-{scenario.ScenarioIndex + 1:00}-{index + 1:00}",
                "LINE-001",
                windowStart.AddDays(index * 18 + scenario.ScenarioIndex),
                price,
                provenance,
                actorSubjectId,
                seedTimestampUtc,
                cancellationToken,
                currencyCode,
                normalizedUnit,
                grossNet);
        }
    }

    private static async Task EnsureQuotationEvidenceAsync(
        NevaxisZentraDbContext dbContext,
        PilotScenario scenario,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        DateTimeOffset evaluatedAt,
        CancellationToken cancellationToken)
    {
        var expiresAt = evaluatedAt.AddDays(scenario.EvaluatedAtOffsetDays);
        var quotationReference = $"COMMQUOTE1-QTE-{scenario.AssessmentReferenceSuffix}";

        var existing = await dbContext.CommercialQuotationRiskEvidenceObservations
            .SingleOrDefaultAsync(e => e.Id == scenario.EvidenceId, cancellationToken);
        if (existing is not null)
        {
            var changed = false;
            if (existing.QuotedUnitPrice != scenario.QuotedPrice)
            {
                existing.QuotedUnitPrice = scenario.QuotedPrice;
                changed = true;
            }

            if (existing.PaymentTermDays != scenario.PaymentTermDays)
            {
                existing.PaymentTermDays = scenario.PaymentTermDays;
                changed = true;
            }

            if (existing.ExpiresAtUtc != expiresAt)
            {
                existing.ExpiresAtUtc = expiresAt;
                changed = true;
            }

            if (!string.Equals(existing.EvidenceStatus, scenario.EvidenceStatus, StringComparison.Ordinal))
            {
                existing.EvidenceStatus = scenario.EvidenceStatus;
                changed = true;
            }

            if (!string.Equals(existing.CustomerReference, scenario.CustomerReference, StringComparison.Ordinal))
            {
                existing.CustomerReference = scenario.CustomerReference;
                changed = true;
            }

            if (existing.MaterialId != scenario.MaterialId)
            {
                existing.MaterialId = scenario.MaterialId;
                changed = true;
            }

            if (changed)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        dbContext.CommercialQuotationRiskEvidenceObservations.Add(new CommercialQuotationRiskEvidenceObservation
        {
            Id = scenario.EvidenceId,
            TenantId = tenantId,
            FacilityId = facilityId,
            QuotationReference = quotationReference,
            CustomerReference = scenario.CustomerReference,
            MaterialId = scenario.MaterialId,
            SourceKind = QuotationSourceKind,
            EvidenceStatus = scenario.EvidenceStatus,
            QuotedAtUtc = evaluatedAt.AddDays(-5),
            ExpiresAtUtc = expiresAt,
            QuotedUnitPrice = scenario.QuotedPrice,
            CurrencyCode = "TRY",
            UnitOfMeasure = "kg",
            GrossNetBasis = CommercialSalesPriceGrossNetBasis.Net,
            TaxBasis = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            DiscountBasis = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            FreightBasis = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            PaymentTermDays = scenario.PaymentTermDays,
            SourceReference = quotationReference,
            SourceProvenance = provenance,
            SourceFingerprint = $"commquote1-evidence-{scenario.EvidenceId:N}",
            IngestIdempotencyKey = $"commquote1-evidence-{scenario.EvidenceId:N}",
            IngestIdempotencyPayloadFingerprint = $"commquote1-evidence-{scenario.EvidenceId:N}",
            ReceivedAtUtc = evaluatedAt.AddDays(-5),
            CreatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsurePilotAssessmentsAsync(
        NevaxisZentraDbContext dbContext,
        CommercialQuotationRiskProfile profile,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        DateTimeOffset evaluatedAt,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        CancellationToken cancellationToken)
    {
        var context = new CommercialIntelligenceOperationContext(
            tenantId,
            facilityId,
            Guid.Empty,
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            CommercialIntelligencePermissionKeys.Manage,
            null);

        foreach (var scenario in PilotScenarios)
        {
            await EnsureSeededAssessmentAsync(
                dbContext,
                scenario,
                profile,
                tenantId,
                facilityId,
                actorSubjectId,
                seedTimestampUtc,
                provenance,
                evaluatedAt,
                windowStart,
                windowEnd,
                context,
                cancellationToken);
        }
    }

    private static async Task EnsureSeededAssessmentAsync(
        NevaxisZentraDbContext dbContext,
        PilotScenario scenario,
        CommercialQuotationRiskProfile profile,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        DateTimeOffset evaluatedAt,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        CommercialIntelligenceOperationContext context,
        CancellationToken cancellationToken)
    {
        var quotationEvidence = await dbContext.CommercialQuotationRiskEvidenceObservations
            .AsNoTracking()
            .SingleAsync(e => e.Id == scenario.EvidenceId, cancellationToken);

        var salesEvidence = await CommercialQuotationRiskSalesEvidenceReader.ReadAsync(
            dbContext,
            context,
            profile,
            quotationEvidence,
            windowStart,
            windowEnd,
            cancellationToken);
        var exclusionCounts = CommercialQuotationRiskSalesEvidenceReader.SummarizeExclusions(salesEvidence.Excluded);
        var evaluation = CommercialQuotationRiskEvaluator.Evaluate(
            profile,
            quotationEvidence,
            salesEvidence,
            exclusionCounts,
            evaluatedAt);

        var command = new EvaluateCommercialQuotationRiskAssessmentCommand(
            profile.Id,
            profile.Version,
            scenario.EvidenceId,
            windowStart,
            windowEnd,
            provenance,
            scenario.IdempotencyKey);
        var fingerprint = CommercialQuotationRiskIdempotencyFingerprint.ComputeAssessmentFingerprint(context, command);

        var existing = await dbContext.CommercialQuotationRiskAssessments
            .Include(a => a.Drivers)
            .SingleOrDefaultAsync(a => a.Id == scenario.AssessmentId, cancellationToken);

        if (existing is not null)
        {
            var changed = ApplyAssessmentFields(
                existing,
                scenario,
                profile,
                quotationEvidence,
                evaluation,
                windowStart,
                windowEnd,
                provenance,
                evaluatedAt,
                seedTimestampUtc,
                actorSubjectId,
                fingerprint);

            var driversChanged = await SyncAssessmentDriversAsync(
                dbContext,
                existing,
                evaluation,
                provenance,
                seedTimestampUtc,
                tenantId,
                facilityId,
                cancellationToken);

            if (changed || driversChanged)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        var assessment = new CommercialQuotationRiskAssessment
        {
            Id = scenario.AssessmentId,
            TenantId = tenantId,
            FacilityId = facilityId,
            AssessmentReference = scenario.AssessmentReference,
            ProfileId = profile.Id,
            ProfileVersion = profile.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
            EvidenceObservationId = quotationEvidence.Id,
            QuotationReference = quotationEvidence.QuotationReference,
            CustomerReference = quotationEvidence.CustomerReference,
            MaterialId = quotationEvidence.MaterialId,
            ReferenceWindowStartUtc = windowStart,
            ReferenceWindowEndUtc = windowEnd,
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
            IdempotencyKey = scenario.IdempotencyKey,
            IdempotencyPayloadFingerprint = fingerprint,
            SourceProvenance = provenance,
            EvaluatedAtUtc = evaluatedAt,
            CreatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
        };

        AddDrivers(assessment, evaluation, provenance, seedTimestampUtc, tenantId, facilityId);
        dbContext.CommercialQuotationRiskAssessments.Add(assessment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool ApplyAssessmentFields(
        CommercialQuotationRiskAssessment assessment,
        PilotScenario scenario,
        CommercialQuotationRiskProfile profile,
        CommercialQuotationRiskEvidenceObservation quotationEvidence,
        CommercialQuotationRiskEvaluationOutput evaluation,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        string provenance,
        DateTimeOffset evaluatedAt,
        DateTimeOffset seedTimestampUtc,
        string actorSubjectId,
        string fingerprint)
    {
        var changed = false;

        void Set<T>(Func<T> getter, Action<T> setter, T value)
        {
            if (!EqualityComparer<T>.Default.Equals(getter(), value))
            {
                setter(value);
                changed = true;
            }
        }

        Set(() => assessment.AssessmentReference, v => assessment.AssessmentReference = v, scenario.AssessmentReference);
        Set(() => assessment.ProfileVersion, v => assessment.ProfileVersion = v,
            profile.Version.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Set(() => assessment.ReferenceMedianUnitPrice, v => assessment.ReferenceMedianUnitPrice = v, evaluation.ReferenceMedianUnitPrice);
        Set(() => assessment.QuotedComparableUnitPrice, v => assessment.QuotedComparableUnitPrice = v, quotationEvidence.QuotedUnitPrice);
        Set(() => assessment.SignedPriceVarianceRatio, v => assessment.SignedPriceVarianceRatio = v, evaluation.SignedPriceVarianceRatio);
        Set(() => assessment.CandidateObservationCount, v => assessment.CandidateObservationCount = v, evaluation.CandidateObservationCount);
        Set(() => assessment.ComparableObservationCount, v => assessment.ComparableObservationCount = v, evaluation.ComparableObservationCount);
        Set(() => assessment.ExcludedObservationCount, v => assessment.ExcludedObservationCount = v, evaluation.ExcludedObservationCount);
        Set(() => assessment.PaymentTermDays, v => assessment.PaymentTermDays = v, quotationEvidence.PaymentTermDays);
        Set(() => assessment.RemainingValidityDays, v => assessment.RemainingValidityDays = v, evaluation.RemainingValidityDays);
        Set(() => assessment.EvidenceStatus, v => assessment.EvidenceStatus = v, evaluation.EvidenceStatus);
        Set(() => assessment.Outcome, v => assessment.Outcome = v, evaluation.Outcome);
        Set(() => assessment.LeadingDriverCode, v => assessment.LeadingDriverCode = v, evaluation.LeadingDriverCode);
        Set(() => assessment.ReferenceWindowStartUtc, v => assessment.ReferenceWindowStartUtc = v, windowStart);
        Set(() => assessment.ReferenceWindowEndUtc, v => assessment.ReferenceWindowEndUtc = v, windowEnd);
        Set(() => assessment.SourceProvenance, v => assessment.SourceProvenance = v, provenance);
        Set(() => assessment.EvaluatedAtUtc, v => assessment.EvaluatedAtUtc = v, evaluatedAt);
        Set(() => assessment.IdempotencyPayloadFingerprint, v => assessment.IdempotencyPayloadFingerprint = v, fingerprint);
        _ = seedTimestampUtc;
        _ = actorSubjectId;

        return changed;
    }

    private static async Task<bool> SyncAssessmentDriversAsync(
        NevaxisZentraDbContext dbContext,
        CommercialQuotationRiskAssessment assessment,
        CommercialQuotationRiskEvaluationOutput evaluation,
        string provenance,
        DateTimeOffset seedTimestampUtc,
        Guid tenantId,
        Guid facilityId,
        CancellationToken cancellationToken)
    {
        var orderedDrivers = CommercialQuotationRiskEvaluator.OrderDrivers(evaluation.Drivers);
        var existingDrivers = assessment.Drivers.OrderBy(d => d.SortOrder).ToList();
        if (existingDrivers.Count == orderedDrivers.Count)
        {
            var matches = true;
            for (var index = 0; index < orderedDrivers.Count; index++)
            {
                var expected = orderedDrivers[index];
                var actual = existingDrivers[index];
                if (!string.Equals(actual.ReasonCode, expected.ReasonCode, StringComparison.Ordinal)
                    || !string.Equals(actual.Severity, expected.Severity, StringComparison.Ordinal)
                    || actual.ActualValue != expected.ActualValue
                    || actual.ThresholdValue != expected.ThresholdValue
                    || actual.NormalizedVariance != expected.NormalizedVariance
                    || actual.AffectedObservationCount != expected.AffectedObservationCount)
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                return false;
            }
        }

        if (existingDrivers.Count > 0)
        {
            dbContext.CommercialQuotationRiskAssessmentDrivers.RemoveRange(existingDrivers);
            assessment.Drivers.Clear();
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        AddDrivers(assessment, evaluation, provenance, seedTimestampUtc, tenantId, facilityId);
        return true;
    }

    private static void AddDrivers(
        CommercialQuotationRiskAssessment assessment,
        CommercialQuotationRiskEvaluationOutput evaluation,
        string provenance,
        DateTimeOffset seedTimestampUtc,
        Guid tenantId,
        Guid facilityId)
    {
        var orderedDrivers = CommercialQuotationRiskEvaluator.OrderDrivers(evaluation.Drivers);
        for (var index = 0; index < orderedDrivers.Count; index++)
        {
            var driver = orderedDrivers[index];
            assessment.Drivers.Add(new CommercialQuotationRiskAssessmentDriver
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                AssessmentId = assessment.Id,
                ReasonCode = driver.ReasonCode,
                Severity = driver.Severity,
                ActualValue = driver.ActualValue,
                ThresholdValue = driver.ThresholdValue,
                Unit = driver.Unit,
                NormalizedVariance = driver.NormalizedVariance,
                AffectedObservationCount = driver.AffectedObservationCount,
                SupportingText = driver.SupportingText,
                SourceProvenance = provenance,
                SortOrder = index + 1,
                IsLeadingDriver = index == 0,
                CreatedAtUtc = seedTimestampUtc,
            });
        }
    }

    private static async Task EnsureSalesObservationAsync(
        NevaxisZentraDbContext dbContext,
        Guid id,
        Guid tenantId,
        Guid facilityId,
        Guid materialId,
        string customerReference,
        string observationSetCode,
        string sourceDocumentReference,
        string sourceLineReference,
        DateTimeOffset commercialEventAtUtc,
        decimal comparableUnitPrice,
        string provenance,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken,
        string currencyCode = "TRY",
        string normalizedUnit = "kg",
        string grossNet = CommercialSalesPriceGrossNetBasis.Net)
    {
        var existing = await dbContext.CommercialSalesPriceEvidenceObservations
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (existing is not null)
        {
            var changed = false;
            if (existing.CommercialEventAtUtc != commercialEventAtUtc)
            {
                existing.CommercialEventAtUtc = commercialEventAtUtc;
                existing.ReceivedAtUtc = commercialEventAtUtc;
                changed = true;
            }

            if (!string.Equals(existing.ObservationSetCode, observationSetCode, StringComparison.Ordinal))
            {
                existing.ObservationSetCode = observationSetCode;
                changed = true;
            }

            if (existing.ComparableUnitPrice != comparableUnitPrice)
            {
                existing.ComparableUnitPrice = comparableUnitPrice;
                existing.OriginalUnitPrice = comparableUnitPrice;
                changed = true;
            }

            if (!string.Equals(existing.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
            {
                existing.CurrencyCode = currencyCode;
                changed = true;
            }

            if (!string.Equals(existing.NormalizedUnit, normalizedUnit, StringComparison.OrdinalIgnoreCase))
            {
                existing.NormalizedUnit = normalizedUnit;
                existing.SourceUnit = normalizedUnit;
                changed = true;
            }

            if (!string.Equals(existing.PriceBasisGrossNet, grossNet, StringComparison.Ordinal))
            {
                existing.PriceBasisGrossNet = grossNet;
                changed = true;
            }

            if (!string.Equals(existing.CustomerReference, customerReference, StringComparison.Ordinal))
            {
                existing.CustomerReference = customerReference;
                changed = true;
            }

            if (existing.MaterialId != materialId)
            {
                existing.MaterialId = materialId;
                changed = true;
            }

            if (changed)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        dbContext.CommercialSalesPriceEvidenceObservations.Add(new CommercialSalesPriceEvidenceObservation
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            MaterialId = materialId,
            CustomerReference = customerReference,
            SourceEvidenceKind = SalesEvidenceKind,
            SourceDocumentReference = sourceDocumentReference,
            SourceLineReference = sourceLineReference,
            CommercialEventAtUtc = commercialEventAtUtc,
            ReceivedAtUtc = commercialEventAtUtc,
            Quantity = 10m,
            SourceUnit = normalizedUnit,
            NormalizedQuantity = 10m,
            NormalizedUnit = normalizedUnit,
            OriginalUnitPrice = comparableUnitPrice,
            ComparableUnitPrice = comparableUnitPrice,
            CurrencyCode = currencyCode,
            PriceBasisGrossNet = grossNet,
            PriceBasisTaxInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            PriceBasisDiscountInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            PriceBasisFreightInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            IsCancelledOrReturned = false,
            EvidenceStatus = CommercialIntelligenceEvidenceStatuses.Simulated,
            SourceFingerprint = $"commquote1-sales-{id:N}",
            SourceProvenance = provenance,
            ObservationSetCode = observationSetCode,
            CreatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Guid SalesObservationId(int scenarioIndex, int observationIndex)
    {
        var value = 0x2c0 + (scenarioIndex * 8) + observationIndex;
        return Guid.Parse($"bb000001-{value:x4}-4000-8000-{value:x012}");
    }

    private sealed record PilotScenario(
        Guid MaterialId,
        string MaterialCode,
        string CustomerReference,
        Guid EvidenceId,
        Guid AssessmentId,
        string AssessmentReference,
        string IdempotencyKey,
        string ObservationSetCode,
        decimal QuotedPrice,
        int PaymentTermDays,
        int EvaluatedAtOffsetDays,
        string EvidenceStatus,
        int SalesObservationCount,
        decimal SalesUnitPrice,
        int ScenarioIndex)
    {
        public string MaterialName => $"Pilot Quotation Risk Material ({MaterialCode})";

        public string AssessmentReferenceSuffix =>
            AssessmentReference["COMMQUOTE1-ASM-".Length..];
    }
}
