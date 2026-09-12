using NevaxisFactOS.Persistence.OperationalRisk;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Development;

internal static class DevelopmentPilotOperationalRiskSeed
{
    internal static async Task EnsureOperationalRiskPilotRecordsAsync(
        NevaxisFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var provenance = OperationalRiskConstants.DevelopmentPilotProvenance;
        var observedAt = DevelopmentPilotIds.OperationalRiskPilotSeedTimeUtc;

        await EnsureCaseAsync(
            dbContext,
            DevelopmentPilotIds.OperationalRiskPilotMaterialCaseId,
            tenantId,
            facilityId,
            "OR-PILOT-MAT-001",
            OperationalRiskCategories.MaterialAvailability,
            OperationalRiskScopeTypes.Material,
            DevelopmentPilotIds.MaterialPilotPpId.ToString(),
            "PP granül stok yetersizliği riski",
            "Simüle edilmiş malzeme kullanılabilirlik riski — gerçek stok garantisi yok.",
            OperationalRiskEvidenceStates.Simulated,
            4,
            4,
            "development-pilot",
            "material-availability-check",
            provenance,
            observedAt.AddHours(-12),
            observedAt,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureEvidenceAsync(
            dbContext,
            DevelopmentPilotIds.OperationalRiskPilotMaterialEvidenceId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.OperationalRiskPilotMaterialCaseId,
            "material-stock-level",
            OperationalRiskEvidenceStates.Simulated,
            observedAt,
            "development-pilot",
            DevelopmentPilotIds.MaterialPilotPpId.ToString(),
            "Simüle edilmiş PP stok seviyesi gözlemi.",
            420m,
            "kg",
            "minimum-threshold-500kg",
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureCaseAsync(
            dbContext,
            DevelopmentPilotIds.OperationalRiskPilotSupplierCaseId,
            tenantId,
            facilityId,
            "OR-PILOT-SUP-001",
            OperationalRiskCategories.Supplier,
            OperationalRiskScopeTypes.Facility,
            null,
            "Tedarikçi teslimat gecikmesi riski",
            "Simüle edilmiş tedarikçi riski — tesis kapsamında, gerçek tedarikçi kaydı oluşturulmadı.",
            OperationalRiskEvidenceStates.Simulated,
            3,
            4,
            "development-pilot",
            "supplier-lead-time",
            provenance,
            observedAt.AddDays(-2),
            observedAt,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureCaseAsync(
            dbContext,
            DevelopmentPilotIds.OperationalRiskPilotOrderDeliveryCaseId,
            tenantId,
            facilityId,
            "OR-PILOT-ORD-001",
            OperationalRiskCategories.OrderDelivery,
            OperationalRiskScopeTypes.ProductionOrder,
            "PILOT-PO-PP-001",
            "Sipariş teslim tarihi kayması riski",
            "Simüle edilmiş üretim siparişi teslim riski — mevcut pilot sipariş referansı kullanıldı.",
            OperationalRiskEvidenceStates.Simulated,
            3,
            5,
            "development-pilot",
            "PILOT-PO-PP-001",
            provenance,
            observedAt.AddDays(-1),
            observedAt,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureCaseAsync(
            dbContext,
            DevelopmentPilotIds.OperationalRiskPilotCostDeviationCaseId,
            tenantId,
            facilityId,
            "OR-PILOT-CST-001",
            OperationalRiskCategories.CostDeviation,
            OperationalRiskScopeTypes.Facility,
            null,
            "Enerji maliyet sapması riski",
            "Simüle edilmiş maliyet sapması riski — garanti edilmiş kayıp hesabı yok.",
            OperationalRiskEvidenceStates.Simulated,
            2,
            4,
            "development-pilot",
            "energy-cost-variance",
            provenance,
            observedAt.AddHours(-6),
            observedAt,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureCaseAsync(
            dbContext,
            DevelopmentPilotIds.OperationalRiskPilotInsufficientDataCaseId,
            tenantId,
            facilityId,
            "OR-PILOT-INS-001",
            OperationalRiskCategories.Other,
            OperationalRiskScopeTypes.Facility,
            null,
            "Yetersiz kanıt — değerlendirme bekliyor",
            "Simüle edilmiş yetersiz veri senaryosu — skor üretilmedi.",
            OperationalRiskEvidenceStates.InsufficientData,
            null,
            null,
            "development-pilot",
            "insufficient-data-scenario",
            provenance,
            observedAt.AddHours(-3),
            observedAt,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureCaseAsync(
            dbContext,
            DevelopmentPilotIds.OperationalRiskPilotSourceConflictCaseId,
            tenantId,
            facilityId,
            "OR-PILOT-CNF-001",
            OperationalRiskCategories.Other,
            OperationalRiskScopeTypes.Facility,
            null,
            "Kaynak çakışması — değerlendirme askıda",
            "Simüle edilmiş kaynak çakışması — skor üretilmedi.",
            OperationalRiskEvidenceStates.SourceConflict,
            4,
            4,
            "development-pilot",
            "source-conflict-scenario",
            provenance,
            observedAt.AddHours(-2),
            observedAt,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureResolvedCaseAsync(
            dbContext,
            tenantId,
            facilityId,
            actorSubjectId,
            provenance,
            observedAt,
            seedTimestampUtc,
            cancellationToken);
    }

    private static async Task EnsureResolvedCaseAsync(
        NevaxisFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        string provenance,
        DateTimeOffset observedAt,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.OperationalRiskCases
            .AnyAsync(c => c.Id == DevelopmentPilotIds.OperationalRiskPilotResolvedCaseId, cancellationToken);
        if (exists)
        {
            return;
        }

        var assessment = OperationalRiskAssessor.Assess(
            OperationalRiskEvidenceStates.Simulated,
            2,
            3);

        var riskCase = new OperationalRiskCase
        {
            Id = DevelopmentPilotIds.OperationalRiskPilotResolvedCaseId,
            TenantId = tenantId,
            FacilityId = facilityId,
            CaseReference = "OR-PILOT-RES-001",
            Category = OperationalRiskCategories.Maintenance,
            ScopeType = OperationalRiskScopeTypes.Facility,
            Title = "Çözümlenmiş bakım sürekliliği riski",
            Description = "Simüle edilmiş çözümlenmiş risk örneği — geçmiş durum geçmişi içerir.",
            EvidenceState = OperationalRiskEvidenceStates.Simulated,
            LikelihoodLevel = assessment.LikelihoodLevel,
            ImpactLevel = assessment.ImpactLevel,
            RiskScore = assessment.RiskScore,
            SeverityBand = assessment.SeverityBand,
            AssessmentOutcome = assessment.AssessmentOutcome,
            LifecycleStatus = OperationalRiskLifecycleStatuses.Resolved,
            RuleSetVersion = OperationalRiskConstants.RuleSetVersion,
            SourceModule = "development-pilot",
            SourceReference = "resolved-lifecycle-example",
            SourceProvenance = provenance,
            FirstDetectedAtUtc = observedAt.AddDays(-5),
            LastObservedAtUtc = observedAt.AddDays(-1),
            Version = 3,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
            UpdatedBy = actorSubjectId,
        };

        dbContext.OperationalRiskCases.Add(riskCase);
        dbContext.OperationalRiskStatusHistory.Add(new OperationalRiskStatusHistory
        {
            Id = DevelopmentPilotIds.OperationalRiskPilotResolvedHistoryId,
            TenantId = tenantId,
            FacilityId = facilityId,
            CaseId = riskCase.Id,
            PreviousStatus = OperationalRiskLifecycleStatuses.Monitoring,
            NewStatus = OperationalRiskLifecycleStatuses.Resolved,
            ReasonNote = "Simüle edilmiş çözüm — pilot risk kapatıldı.",
            ActorSubjectId = actorSubjectId,
            OccurredAtUtc = observedAt.AddDays(-1),
            ExpectedCaseVersion = 2,
            SourceProvenance = provenance,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureCaseAsync(
        NevaxisFactOSDbContext dbContext,
        Guid id,
        Guid tenantId,
        Guid facilityId,
        string caseReference,
        string category,
        string scopeType,
        string? scopeReference,
        string title,
        string description,
        string evidenceState,
        int? likelihood,
        int? impact,
        string? sourceModule,
        string? sourceReference,
        string provenance,
        DateTimeOffset firstDetectedAtUtc,
        DateTimeOffset lastObservedAtUtc,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.OperationalRiskCases
            .AnyAsync(c => c.Id == id, cancellationToken);
        if (exists)
        {
            return;
        }

        var assessment = OperationalRiskAssessor.Assess(evidenceState, likelihood, impact);
        dbContext.OperationalRiskCases.Add(new OperationalRiskCase
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            CaseReference = caseReference,
            Category = category,
            ScopeType = scopeType,
            ScopeReference = scopeReference,
            Title = title,
            Description = description,
            EvidenceState = evidenceState,
            LikelihoodLevel = assessment.LikelihoodLevel,
            ImpactLevel = assessment.ImpactLevel,
            RiskScore = assessment.RiskScore,
            SeverityBand = assessment.SeverityBand,
            AssessmentOutcome = assessment.AssessmentOutcome,
            LifecycleStatus = OperationalRiskLifecycleStatuses.Detected,
            RuleSetVersion = OperationalRiskConstants.RuleSetVersion,
            SourceModule = sourceModule,
            SourceReference = sourceReference,
            SourceProvenance = provenance,
            FirstDetectedAtUtc = firstDetectedAtUtc,
            LastObservedAtUtc = lastObservedAtUtc,
            Version = 1,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
            UpdatedBy = actorSubjectId,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureEvidenceAsync(
        NevaxisFactOSDbContext dbContext,
        Guid id,
        Guid tenantId,
        Guid facilityId,
        Guid caseId,
        string evidenceTypeCode,
        string evidenceState,
        DateTimeOffset observedAtUtc,
        string? sourceModule,
        string? sourceReference,
        string summary,
        decimal? numericValue,
        string? unit,
        string? comparisonReference,
        string provenance,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.OperationalRiskEvidence
            .AnyAsync(e => e.Id == id, cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.OperationalRiskEvidence.Add(new OperationalRiskEvidence
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            CaseId = caseId,
            EvidenceTypeCode = evidenceTypeCode,
            EvidenceState = evidenceState,
            ObservedAtUtc = observedAtUtc,
            SourceModule = sourceModule,
            SourceReference = sourceReference,
            Summary = summary,
            NumericValue = numericValue,
            Unit = unit,
            ComparisonReference = comparisonReference,
            SourceProvenance = provenance,
            CreatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
