using NevaxisZentra.Persistence.Quality;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Development;

internal static class DevelopmentPilotQualitySeed
{
    public static async Task EnsureQualityPilotControlPlanAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string createdBySubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.QualityControlPlans
            .AsNoTracking()
            .AnyAsync(
                p => p.Id == DevelopmentPilotIds.QualityPilotControlPlanId,
                cancellationToken);

        if (existing)
        {
            return;
        }

        var plan = new QualityControlPlan
        {
            Id = DevelopmentPilotIds.QualityPilotControlPlanId,
            TenantId = tenantId,
            FacilityId = facilityId,
            Code = "QC-PLAN-001",
            Name = "Pilot ürün ölçüm planı",
            Description = "Development pilot quality control plan for POM-WHEEL-55.",
            ProductReference = "POM-WHEEL-55",
            ProductGroup = "pilot-products",
            OperationReference = "final-inspection",
            Status = QualityControlPlanStatus.Active,
            Revision = "1",
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = createdBySubjectId,
            Version = 1,
            InspectionDefinitions =
            [
                new QualityInspectionDefinition
                {
                    Id = DevelopmentPilotIds.QualityPilotInspectionDefinitionId,
                    TenantId = tenantId,
                    FacilityId = facilityId,
                    QualityControlPlanId = DevelopmentPilotIds.QualityPilotControlPlanId,
                    Code = "DIM-001",
                    Name = "Çap ölçümü",
                    Description = "Nominal çap tolerans kontrolü",
                    InspectionType = QualityInspectionType.Measurement,
                    Sequence = 1,
                    Unit = "mm",
                    NominalValue = 55.0m,
                    LowerLimit = 54.8m,
                    UpperLimit = 55.2m,
                    Required = true,
                    Critical = true,
                    Instruction = "Kalibre mikrometre ile ölçün.",
                    Version = 1,
                },
                new QualityInspectionDefinition
                {
                    Id = Guid.Parse("bb000001-0062-4000-8000-000000000062"),
                    TenantId = tenantId,
                    FacilityId = facilityId,
                    QualityControlPlanId = DevelopmentPilotIds.QualityPilotControlPlanId,
                    Code = "VIS-001",
                    Name = "Yüzey görsel kontrol",
                    InspectionType = QualityInspectionType.Visual,
                    Sequence = 2,
                    Required = true,
                    Critical = false,
                    Instruction = "pass / fail / n/a",
                    Version = 1,
                },
            ],
        };

        dbContext.QualityControlPlans.Add(plan);
    }

    public static async Task EnsureQualityPilotNcrFollowUpFixturesAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string createdBySubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.QualityNonconformances
            .AsNoTracking()
            .AnyAsync(
                n => n.Id == DevelopmentPilotIds.QualityPilotNcrConfirmedId,
                cancellationToken);

        if (existing)
        {
            return;
        }

        dbContext.QualityNonconformances.AddRange(
            new QualityNonconformance
            {
                Id = DevelopmentPilotIds.QualityPilotNcrConfirmedId,
                TenantId = tenantId,
                FacilityId = facilityId,
                NcrNumber = "NCR-PILOT-001",
                ProductReference = "POM-WHEEL-55",
                OperationReference = "final-inspection",
                Title = "Pilot çap tolerans aşımı",
                Description = "Son muayenede nominal çap toleransı dışında ölçüm kaydı tespit edildi.",
                Category = "measurement",
                Severity = QualityNcrSeverity.Major,
                Status = QualityNcrStatus.Open,
                DetectedAtUtc = seedTimestampUtc,
                DetectedBySubjectId = createdBySubjectId,
                DispositionStatus = QualityNcrDispositionStatus.Pending,
                HoldStatus = QualityNcrHoldStatus.None,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                Version = 1,
                FollowUpStatus = QualityNcrFollowUpStatuses.RequiresReview,
            },
            new QualityNonconformance
            {
                Id = DevelopmentPilotIds.QualityPilotNcrInsufficientEvidenceId,
                TenantId = tenantId,
                FacilityId = facilityId,
                NcrNumber = "NCR-PILOT-002",
                Title = "Pilot eksik kanıt kaydı",
                Description = "Muayene, üretim emri veya ürün referansı olmadan açılmış pilot uygunsuzluk kaydı.",
                Category = "data-quality",
                Severity = QualityNcrSeverity.Minor,
                Status = QualityNcrStatus.Open,
                DetectedAtUtc = seedTimestampUtc,
                DetectedBySubjectId = createdBySubjectId,
                DispositionStatus = QualityNcrDispositionStatus.Pending,
                HoldStatus = QualityNcrHoldStatus.None,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                Version = 1,
                FollowUpStatus = QualityNcrFollowUpStatuses.RequiresReview,
            });
    }
}
