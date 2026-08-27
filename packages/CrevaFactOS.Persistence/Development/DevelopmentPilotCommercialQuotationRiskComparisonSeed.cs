using CrevaFactOS.Persistence.CommercialIntelligence;
using CrevaFactOS.Persistence.Material;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Development;

internal static class DevelopmentPilotCommercialQuotationRiskComparisonSeed
{
    private const string QuotationSourceKind = CommercialQuotationRiskSourceKinds.ManualDevelopmentSimulator;

    internal static async Task EnsureCommercialQuotationRiskComparisonPilotRecordsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var provenance = CommercialIntelligenceConstants.QuotationRiskDevelopmentPilotProvenance;
        var profile = await dbContext.CommercialQuotationRiskProfiles
            .SingleOrDefaultAsync(p => p.Id == DevelopmentPilotIds.CommercialQuotationRiskPilotProfileId, cancellationToken);
        if (profile is null)
        {
            return;
        }

        await ReconcileProfileAsync(dbContext, profile, actorSubjectId, seedTimestampUtc, cancellationToken);

        foreach (var scenario in ComparisonScenarios)
        {
            await EnsurePilotMaterialAsync(
                dbContext,
                scenario,
                tenantId,
                facilityId,
                actorSubjectId,
                seedTimestampUtc,
                cancellationToken);

            await SeedScenarioAssessmentsAsync(
                dbContext,
                scenario,
                profile,
                tenantId,
                facilityId,
                actorSubjectId,
                seedTimestampUtc,
                provenance,
                cancellationToken);
        }

        await EnsurePilotComparisonRunsAsync(
            dbContext,
            tenantId,
            facilityId,
            profile,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            cancellationToken);
    }

    private static readonly ComparisonScenario[] ComparisonScenarios =
    [
        new(
            0,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialStableTrendId,
            "COMMQUOTE2-MAT-STABLE",
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotCustomerPrimary,
            [
                new(0, 6, CommercialQuotationRiskAssessmentOutcomes.WithinThreshold, CommercialQuotationRiskDriverReasonCodes.WithinProfile),
                new(1, 6, CommercialQuotationRiskAssessmentOutcomes.WithinThreshold, CommercialQuotationRiskDriverReasonCodes.WithinProfile),
            ]),
        new(
            1,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialRiskIncreaseId,
            "COMMQUOTE2-MAT-INCREASE",
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotCustomerPrimary,
            [
                new(0, 6, CommercialQuotationRiskAssessmentOutcomes.WithinThreshold, CommercialQuotationRiskDriverReasonCodes.WithinProfile),
                new(1, 6, CommercialQuotationRiskAssessmentOutcomes.Attention, CommercialQuotationRiskDriverReasonCodes.QuotedPriceBelowReference),
            ]),
        new(
            2,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialRiskDecreaseId,
            "COMMQUOTE2-MAT-DECREASE",
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotCustomerPrimary,
            [
                new(0, 6, CommercialQuotationRiskAssessmentOutcomes.Attention, CommercialQuotationRiskDriverReasonCodes.QuotedPriceBelowReference),
                new(1, 6, CommercialQuotationRiskAssessmentOutcomes.WithinThreshold, CommercialQuotationRiskDriverReasonCodes.WithinProfile),
            ]),
        new(
            3,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialExpiryConcentrationId,
            "COMMQUOTE2-MAT-EXPIRY",
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotCustomerPrimary,
            [
                new(0, 4, CommercialQuotationRiskAssessmentOutcomes.WithinThreshold, CommercialQuotationRiskDriverReasonCodes.WithinProfile),
                new(0, 2, CommercialQuotationRiskAssessmentOutcomes.Attention, CommercialQuotationRiskDriverReasonCodes.QuotedPriceBelowReference),
                new(1, 4, CommercialQuotationRiskAssessmentOutcomes.WithinThreshold, CommercialQuotationRiskDriverReasonCodes.WithinProfile),
                new(1, 2, CommercialQuotationRiskAssessmentOutcomes.Attention, CommercialQuotationRiskDriverReasonCodes.QuotationExpiringSoon),
            ]),
        new(
            4,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialPaymentConcentrationId,
            "COMMQUOTE2-MAT-PAYMENT",
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotCustomerPrimary,
            [
                new(0, 4, CommercialQuotationRiskAssessmentOutcomes.WithinThreshold, CommercialQuotationRiskDriverReasonCodes.WithinProfile),
                new(0, 2, CommercialQuotationRiskAssessmentOutcomes.Attention, CommercialQuotationRiskDriverReasonCodes.QuotedPriceBelowReference),
                new(1, 4, CommercialQuotationRiskAssessmentOutcomes.WithinThreshold, CommercialQuotationRiskDriverReasonCodes.WithinProfile),
                new(1, 2, CommercialQuotationRiskAssessmentOutcomes.Attention, CommercialQuotationRiskDriverReasonCodes.PaymentTermExceeded),
            ]),
        new(
            5,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialCustomerPositionId,
            "COMMQUOTE2-MAT-CUST-POS",
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotCustomerPrimary,
            [
                new(1, 6, CommercialQuotationRiskAssessmentOutcomes.Attention, CommercialQuotationRiskDriverReasonCodes.QuotedPriceBelowReference, DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotCustomerPrimary),
                new(1, 6, CommercialQuotationRiskAssessmentOutcomes.WithinThreshold, CommercialQuotationRiskDriverReasonCodes.WithinProfile, DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotCustomerSecondary),
            ]),
        new(
            6,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialCustomerTieId,
            "COMMQUOTE2-MAT-CUST-TIE",
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotCustomerTieA,
            [
                new(1, 3, CommercialQuotationRiskAssessmentOutcomes.Attention, CommercialQuotationRiskDriverReasonCodes.PaymentTermExceeded, DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotCustomerTieA),
                new(1, 3, CommercialQuotationRiskAssessmentOutcomes.WithinThreshold, CommercialQuotationRiskDriverReasonCodes.WithinProfile, DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotCustomerTieA),
                new(1, 3, CommercialQuotationRiskAssessmentOutcomes.Attention, CommercialQuotationRiskDriverReasonCodes.PaymentTermExceeded, DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotCustomerTieB),
                new(1, 3, CommercialQuotationRiskAssessmentOutcomes.WithinThreshold, CommercialQuotationRiskDriverReasonCodes.WithinProfile, DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotCustomerTieB),
            ]),
        new(
            7,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialInsufficientId,
            "COMMQUOTE2-MAT-INSUFFICIENT",
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotCustomerPrimary,
            [
                new(0, 6, CommercialQuotationRiskAssessmentOutcomes.WithinThreshold, CommercialQuotationRiskDriverReasonCodes.WithinProfile),
                new(1, 3, CommercialQuotationRiskAssessmentOutcomes.WithinThreshold, CommercialQuotationRiskDriverReasonCodes.WithinProfile),
            ]),
        new(
            8,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialSourceConflictId,
            "COMMQUOTE2-MAT-CONFLICT",
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotCustomerPrimary,
            [
                new(0, 6, CommercialQuotationRiskAssessmentOutcomes.WithinThreshold, CommercialQuotationRiskDriverReasonCodes.WithinProfile),
                new(1, 6, CommercialQuotationRiskAssessmentOutcomes.SourceConflict, CommercialQuotationRiskDriverReasonCodes.SourceConflict),
            ]),
    ];

    private static async Task ReconcileProfileAsync(
        CrevaFactOSDbContext dbContext,
        CommercialQuotationRiskProfile profile,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (profile.MaximumRiskIncidenceDelta
            == CommercialIntelligenceConstants.DefaultQuotationRiskMaximumRiskIncidenceDelta)
        {
            return;
        }

        profile.MaximumRiskIncidenceDelta =
            CommercialIntelligenceConstants.DefaultQuotationRiskMaximumRiskIncidenceDelta;
        profile.UpdatedAtUtc = seedTimestampUtc;
        profile.UpdatedBy = actorSubjectId;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsurePilotMaterialAsync(
        CrevaFactOSDbContext dbContext,
        ComparisonScenario scenario,
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
            return;
        }

        dbContext.Materials.Add(new MaterialRegistryEntry
        {
            Id = scenario.MaterialId,
            TenantId = tenantId,
            FacilityId = facilityId,
            Code = scenario.MaterialCode,
            CodeNormalized = scenario.MaterialCode,
            Name = $"Pilot Quotation Risk Comparison Material ({scenario.MaterialCode})",
            Description = "Deterministic development pilot material for quotation-risk comparison scenarios.",
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

    private static async Task SeedScenarioAssessmentsAsync(
        CrevaFactOSDbContext dbContext,
        ComparisonScenario scenario,
        CommercialQuotationRiskProfile profile,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CancellationToken cancellationToken)
    {
        var referenceStart = DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotReferenceWindowStartUtc;
        var referenceEnd = DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotReferenceWindowEndUtc;
        var currentStart = DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotCurrentWindowStartUtc;
        var currentEnd = DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotCurrentWindowEndUtc;
        var assessmentIndex = 0;

        foreach (var batch in scenario.AssessmentBatches)
        {
            var windowStart = batch.WindowKind == 0 ? referenceStart : currentStart;
            var windowEnd = batch.WindowKind == 0 ? referenceEnd : currentEnd;

            for (var index = 0; index < batch.Count; index++)
            {
                var customerReference = batch.CustomerReference ?? scenario.DefaultCustomerReference;
                var evaluatedAt = windowStart.AddDays(index * 20 + scenario.ScenarioIndex + batch.WindowKind);
                await EnsureComparisonAssessmentAsync(
                    dbContext,
                    ComparisonAssessmentId(scenario.ScenarioIndex, assessmentIndex),
                    ComparisonEvidenceId(scenario.ScenarioIndex, assessmentIndex),
                    scenario,
                    profile,
                    tenantId,
                    facilityId,
                    customerReference,
                    batch.Outcome,
                    batch.LeadingDriverCode,
                    evaluatedAt,
                    windowStart,
                    windowEnd,
                    assessmentIndex,
                    actorSubjectId,
                    seedTimestampUtc,
                    provenance,
                    cancellationToken);
                assessmentIndex++;
            }
        }
    }

    private static async Task EnsurePilotComparisonRunsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        CommercialQuotationRiskProfile profile,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CancellationToken cancellationToken)
    {
        var context = new CommercialIntelligenceOperationContext(
            tenantId,
            facilityId,
            Guid.Empty,
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            CommercialIntelligencePermissionKeys.Manage,
            null);

        var temporalCommandBase = new EvaluateCommercialQuotationRiskComparisonCommand(
            profile.Id,
            profile.Version,
            CommercialQuotationRiskComparisonTypes.TemporalRiskTrend,
            null,
            null,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotCurrentWindowStartUtc,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotCurrentWindowEndUtc,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotReferenceWindowStartUtc,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotReferenceWindowEndUtc,
            provenance,
            string.Empty);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotStableTrendRunId,
            "COMMQUOTE2-CMP-PILOT-STABLE",
            "commquote2-pilot-stable-trend",
            CommercialQuotationRiskComparisonTypes.TemporalRiskTrend,
            tenantId,
            facilityId,
            profile,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialStableTrendId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            temporalCommandBase with
            {
                MaterialId = DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialStableTrendId,
                IdempotencyKey = "commquote2-pilot-stable-trend",
            },
            cancellationToken);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotRiskIncreaseRunId,
            "COMMQUOTE2-CMP-PILOT-INCREASE",
            "commquote2-pilot-risk-increase",
            CommercialQuotationRiskComparisonTypes.TemporalRiskTrend,
            tenantId,
            facilityId,
            profile,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialRiskIncreaseId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            temporalCommandBase with
            {
                MaterialId = DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialRiskIncreaseId,
                IdempotencyKey = "commquote2-pilot-risk-increase",
            },
            cancellationToken);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotRiskDecreaseRunId,
            "COMMQUOTE2-CMP-PILOT-DECREASE",
            "commquote2-pilot-risk-decrease",
            CommercialQuotationRiskComparisonTypes.TemporalRiskTrend,
            tenantId,
            facilityId,
            profile,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialRiskDecreaseId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            temporalCommandBase with
            {
                MaterialId = DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialRiskDecreaseId,
                IdempotencyKey = "commquote2-pilot-risk-decrease",
            },
            cancellationToken);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotExpiryConcentrationRunId,
            "COMMQUOTE2-CMP-PILOT-EXPIRY",
            "commquote2-pilot-expiry-concentration",
            CommercialQuotationRiskComparisonTypes.TemporalRiskTrend,
            tenantId,
            facilityId,
            profile,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialExpiryConcentrationId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            temporalCommandBase with
            {
                MaterialId = DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialExpiryConcentrationId,
                IdempotencyKey = "commquote2-pilot-expiry-concentration",
            },
            cancellationToken);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotPaymentConcentrationRunId,
            "COMMQUOTE2-CMP-PILOT-PAYMENT",
            "commquote2-pilot-payment-concentration",
            CommercialQuotationRiskComparisonTypes.TemporalRiskTrend,
            tenantId,
            facilityId,
            profile,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialPaymentConcentrationId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            temporalCommandBase with
            {
                MaterialId = DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialPaymentConcentrationId,
                IdempotencyKey = "commquote2-pilot-payment-concentration",
            },
            cancellationToken);

        var customerCommandBase = new EvaluateCommercialQuotationRiskComparisonCommand(
            profile.Id,
            profile.Version,
            CommercialQuotationRiskComparisonTypes.CustomerRiskPosition,
            null,
            null,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotCurrentWindowStartUtc,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotCurrentWindowEndUtc,
            null,
            null,
            provenance,
            string.Empty);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotCustomerPositionRunId,
            "COMMQUOTE2-CMP-PILOT-CUST-POS",
            "commquote2-pilot-customer-position",
            CommercialQuotationRiskComparisonTypes.CustomerRiskPosition,
            tenantId,
            facilityId,
            profile,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialCustomerPositionId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            customerCommandBase with
            {
                MaterialId = DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialCustomerPositionId,
                IdempotencyKey = "commquote2-pilot-customer-position",
            },
            cancellationToken);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotCustomerTieRunId,
            "COMMQUOTE2-CMP-PILOT-CUST-TIE",
            "commquote2-pilot-customer-tie",
            CommercialQuotationRiskComparisonTypes.CustomerRiskPosition,
            tenantId,
            facilityId,
            profile,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialCustomerTieId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            customerCommandBase with
            {
                MaterialId = DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialCustomerTieId,
                IdempotencyKey = "commquote2-pilot-customer-tie",
            },
            cancellationToken);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotInsufficientRunId,
            "COMMQUOTE2-CMP-PILOT-INSUFFICIENT",
            "commquote2-pilot-insufficient",
            CommercialQuotationRiskComparisonTypes.TemporalRiskTrend,
            tenantId,
            facilityId,
            profile,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialInsufficientId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            temporalCommandBase with
            {
                MaterialId = DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialInsufficientId,
                IdempotencyKey = "commquote2-pilot-insufficient",
            },
            cancellationToken);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotSourceConflictRunId,
            "COMMQUOTE2-CMP-PILOT-CONFLICT",
            "commquote2-pilot-source-conflict",
            CommercialQuotationRiskComparisonTypes.TemporalRiskTrend,
            tenantId,
            facilityId,
            profile,
            DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialSourceConflictId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            temporalCommandBase with
            {
                MaterialId = DevelopmentPilotIds.CommercialQuotationRiskComparisonPilotMaterialSourceConflictId,
                IdempotencyKey = "commquote2-pilot-source-conflict",
            },
            cancellationToken);
    }

    private static async Task EnsureSeededRunAsync(
        CrevaFactOSDbContext dbContext,
        Guid runId,
        string comparisonReference,
        string idempotencyKey,
        string comparisonType,
        Guid tenantId,
        Guid facilityId,
        CommercialQuotationRiskProfile profile,
        Guid materialId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialQuotationRiskComparisonCommand command,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.CommercialQuotationRiskComparisonRuns
            .Include(r => r.Results)
            .Include(r => r.Drivers)
            .SingleOrDefaultAsync(r => r.Id == runId, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        CommercialQuotationRiskComparisonEvaluationOutput evaluation;
        if (string.Equals(comparisonType, CommercialQuotationRiskComparisonTypes.TemporalRiskTrend, StringComparison.Ordinal))
        {
            var referenceAssessments = await CommercialQuotationRiskAssessmentReader.ReadAssessmentsInWindowAsync(
                dbContext,
                context,
                profile.Id,
                command.ReferenceWindowStartUtc!.Value,
                command.ReferenceWindowEndUtc!.Value,
                materialId,
                command.CustomerReference,
                cancellationToken);
            var currentAssessments = await CommercialQuotationRiskAssessmentReader.ReadAssessmentsInWindowAsync(
                dbContext,
                context,
                profile.Id,
                command.CurrentWindowStartUtc,
                command.CurrentWindowEndUtc,
                materialId,
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
                materialId,
                null,
                cancellationToken);
            var customerReferences = assessments
                .Select(a => a.CustomerReference)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(c => c, StringComparer.Ordinal)
                .ToList();

            evaluation = CommercialQuotationRiskComparisonEvaluator.EvaluateCustomerRiskPosition(
                profile,
                assessments,
                customerReferences);
        }

        var fingerprint = CommercialQuotationRiskIdempotencyFingerprint.ComputeComparisonFingerprint(
            context,
            command with { IdempotencyKey = idempotencyKey });
        var now = seedTimestampUtc;
        var run = new CommercialQuotationRiskComparisonRun
        {
            Id = runId,
            TenantId = tenantId,
            FacilityId = facilityId,
            ComparisonReference = comparisonReference,
            ProfileId = profile.Id,
            ProfileVersion = profile.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ComparisonType = comparisonType,
            MaterialId = materialId,
            CustomerReference = command.CustomerReference,
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
            IdempotencyKey = idempotencyKey,
            IdempotencyPayloadFingerprint = fingerprint,
            SourceProvenance = provenance,
            EvaluatedAtUtc = now,
            CreatedAtUtc = now,
            CreatedBy = actorSubjectId,
        };

        var resultIdByCustomer = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var customerResult in evaluation.CustomerResults)
        {
            var resultId = Guid.NewGuid();
            resultIdByCustomer[customerResult.CustomerReference] = resultId;
            run.Results.Add(new CommercialQuotationRiskComparisonResult
            {
                Id = resultId,
                TenantId = tenantId,
                FacilityId = facilityId,
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
                TenantId = tenantId,
                FacilityId = facilityId,
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
                SourceProvenance = provenance,
                SortOrder = index + 1,
                IsLeadingDriver = index == 0,
                CreatedAtUtc = now,
            });
        }

        dbContext.CommercialQuotationRiskComparisonRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureComparisonAssessmentAsync(
        CrevaFactOSDbContext dbContext,
        Guid assessmentId,
        Guid evidenceId,
        ComparisonScenario scenario,
        CommercialQuotationRiskProfile profile,
        Guid tenantId,
        Guid facilityId,
        string customerReference,
        string outcome,
        string leadingDriverCode,
        DateTimeOffset evaluatedAtUtc,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        int assessmentIndex,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CancellationToken cancellationToken)
    {
        var evidenceStatus = string.Equals(outcome, CommercialQuotationRiskAssessmentOutcomes.SourceConflict, StringComparison.Ordinal)
            ? CommercialQuotationRiskEvidenceStatuses.SourceConflict
            : string.Equals(outcome, CommercialQuotationRiskAssessmentOutcomes.InsufficientData, StringComparison.Ordinal)
                ? CommercialQuotationRiskEvidenceStatuses.InsufficientData
                : CommercialQuotationRiskEvidenceStatuses.Simulated;

        await EnsureEvidenceAsync(
            dbContext,
            evidenceId,
            tenantId,
            facilityId,
            scenario.MaterialId,
            customerReference,
            evidenceStatus,
            evaluatedAtUtc,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            cancellationToken);

        var existing = await dbContext.CommercialQuotationRiskAssessments
            .SingleOrDefaultAsync(a => a.Id == assessmentId, cancellationToken);
        if (existing is not null)
        {
            var changed = false;
            if (existing.EvaluatedAtUtc != evaluatedAtUtc)
            {
                existing.EvaluatedAtUtc = evaluatedAtUtc;
                changed = true;
            }

            if (!string.Equals(existing.Outcome, outcome, StringComparison.Ordinal))
            {
                existing.Outcome = outcome;
                changed = true;
            }

            if (!string.Equals(existing.LeadingDriverCode, leadingDriverCode, StringComparison.Ordinal))
            {
                existing.LeadingDriverCode = leadingDriverCode;
                changed = true;
            }

            if (!string.Equals(existing.CustomerReference, customerReference, StringComparison.Ordinal))
            {
                existing.CustomerReference = customerReference;
                changed = true;
            }

            if (!string.Equals(existing.EvidenceStatus, evidenceStatus, StringComparison.Ordinal))
            {
                existing.EvidenceStatus = evidenceStatus;
                changed = true;
            }

            if (changed)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        var quotationReference = $"COMMQUOTE2-QTE-{scenario.MaterialCode}-{assessmentId:N}".Substring(0, 40);
        dbContext.CommercialQuotationRiskAssessments.Add(new CommercialQuotationRiskAssessment
        {
            Id = assessmentId,
            TenantId = tenantId,
            FacilityId = facilityId,
            AssessmentReference = $"COMMQUOTE2-ASM-{scenario.ScenarioIndex:D2}-{assessmentIndex:D3}",
            ProfileId = profile.Id,
            ProfileVersion = profile.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
            EvidenceObservationId = evidenceId,
            QuotationReference = quotationReference,
            CustomerReference = customerReference,
            MaterialId = scenario.MaterialId,
            ReferenceWindowStartUtc = windowStart,
            ReferenceWindowEndUtc = windowEnd,
            ReferenceMedianUnitPrice = 100m,
            QuotedComparableUnitPrice = 100m,
            SignedPriceVarianceRatio = 0m,
            CandidateObservationCount = 6,
            ComparableObservationCount = 6,
            ExcludedObservationCount = 0,
            PaymentTermDays = 30,
            RemainingValidityDays = 30,
            CurrencyCode = "TRY",
            UnitOfMeasure = "kg",
            GrossNetBasis = CommercialSalesPriceGrossNetBasis.Net,
            TaxBasis = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            DiscountBasis = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            FreightBasis = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            EvidenceStatus = evidenceStatus,
            Outcome = outcome,
            LeadingDriverCode = leadingDriverCode,
            RuleSetVersion = CommercialIntelligenceConstants.QuotationRiskRuleSetVersion,
            IdempotencyKey = $"commquote2-comparison-{assessmentId:N}",
            IdempotencyPayloadFingerprint = $"commquote2-comparison-{assessmentId:N}",
            SourceProvenance = provenance,
            EvaluatedAtUtc = evaluatedAtUtc,
            CreatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureEvidenceAsync(
        CrevaFactOSDbContext dbContext,
        Guid evidenceId,
        Guid tenantId,
        Guid facilityId,
        Guid materialId,
        string customerReference,
        string evidenceStatus,
        DateTimeOffset evaluatedAtUtc,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.CommercialQuotationRiskEvidenceObservations
            .SingleOrDefaultAsync(e => e.Id == evidenceId, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var quotationReference = $"COMMQUOTE2-QTE-{evidenceId:N}".Substring(0, 40);
        dbContext.CommercialQuotationRiskEvidenceObservations.Add(new CommercialQuotationRiskEvidenceObservation
        {
            Id = evidenceId,
            TenantId = tenantId,
            FacilityId = facilityId,
            QuotationReference = quotationReference,
            CustomerReference = customerReference,
            MaterialId = materialId,
            SourceKind = QuotationSourceKind,
            EvidenceStatus = evidenceStatus,
            QuotedAtUtc = evaluatedAtUtc.AddDays(-5),
            ExpiresAtUtc = evaluatedAtUtc.AddDays(30),
            QuotedUnitPrice = 100m,
            CurrencyCode = "TRY",
            UnitOfMeasure = "kg",
            GrossNetBasis = CommercialSalesPriceGrossNetBasis.Net,
            TaxBasis = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            DiscountBasis = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            FreightBasis = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            PaymentTermDays = 30,
            SourceReference = quotationReference,
            SourceProvenance = provenance,
            SourceFingerprint = $"commquote2-comparison-evidence-{evidenceId:N}",
            IngestIdempotencyKey = $"commquote2-comparison-evidence-{evidenceId:N}",
            IngestIdempotencyPayloadFingerprint = $"commquote2-comparison-evidence-{evidenceId:N}",
            ReceivedAtUtc = evaluatedAtUtc.AddDays(-5),
            CreatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Guid ComparisonAssessmentId(int scenarioIndex, int assessmentIndex)
    {
        var value = 0x2e00 + scenarioIndex * 20 + assessmentIndex;
        return Guid.Parse($"bb000001-{value:x4}-4000-8000-{value:x012}");
    }

    private static Guid ComparisonEvidenceId(int scenarioIndex, int assessmentIndex)
    {
        var value = 0x2f00 + scenarioIndex * 20 + assessmentIndex;
        return Guid.Parse($"bb000001-{value:x4}-4000-8000-{value:x012}");
    }

    private sealed record ComparisonScenario(
        int ScenarioIndex,
        Guid MaterialId,
        string MaterialCode,
        string DefaultCustomerReference,
        IReadOnlyList<AssessmentBatch> AssessmentBatches);

    private sealed record AssessmentBatch(
        int WindowKind,
        int Count,
        string Outcome,
        string LeadingDriverCode,
        string? CustomerReference = null);
}
