using System.Text.Json;
using NevaxisZentra.Persistence.Material;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Development;

internal static class DevelopmentPilotMaterialAvailabilityRiskSeed
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static async Task EnsureMaterialAvailabilityRiskPilotRecordsAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var material = await dbContext.Materials.AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.Id == DevelopmentPilotIds.MaterialPilotPpId
                     && m.TenantId == tenantId
                     && m.FacilityId == facilityId,
                cancellationToken);

        var absMaterial = await dbContext.Materials.AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.Id == DevelopmentPilotIds.MaterialPilotAbsId
                     && m.TenantId == tenantId
                     && m.FacilityId == facilityId,
                cancellationToken);

        if (material is null || absMaterial is null)
        {
            return;
        }

        var completeProfile = await dbContext.MaterialRequirementProfiles.AsNoTracking()
            .Include(p => p.Material)
            .FirstOrDefaultAsync(p => p.Id == DevelopmentPilotIds.MaterialPilotRequirementPpCompleteId, cancellationToken);

        var partialProfile = await dbContext.MaterialRequirementProfiles.AsNoTracking()
            .Include(p => p.Material)
            .FirstOrDefaultAsync(p => p.Id == DevelopmentPilotIds.MaterialPilotRequirementAbsPartialId, cancellationToken);

        if (completeProfile is null || partialProfile is null)
        {
            return;
        }

        await EnsureSmallRequirementProfileAsync(
            dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);

        var erpBalances = await dbContext.CanonicalErpInventoryBalances.AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.FacilityId == facilityId)
            .ToListAsync(cancellationToken);

        var completeDetail = MapProfileDetail(completeProfile);
        var partialDetail = MapProfileDetail(partialProfile);
        var completeRequirement = MaterialRequirementCalculator.CalculateRequirement(completeDetail);
        var partialRequirement = MaterialRequirementCalculator.CalculateRequirement(partialDetail);

        var shortageResult = MaterialAvailabilityRiskCalculator.AssessAvailabilityRisk(
            material,
            completeDetail,
            completeRequirement,
            erpBalances,
            seedTimestampUtc,
            hasErpReadAccess: true);

        await EnsureAssessmentAsync(
            dbContext,
            DevelopmentPilotIds.MaterialPilotAvailabilityRiskShortageAssessmentId,
            tenantId,
            facilityId,
            material.Id,
            completeProfile.Id,
            shortageResult,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        var insufficientResult = MaterialAvailabilityRiskCalculator.AssessAvailabilityRisk(
            absMaterial,
            partialDetail,
            partialRequirement,
            erpBalances,
            seedTimestampUtc,
            hasErpReadAccess: true);

        await EnsureAssessmentAsync(
            dbContext,
            DevelopmentPilotIds.MaterialPilotAvailabilityRiskInsufficientAssessmentId,
            tenantId,
            facilityId,
            absMaterial.Id,
            partialProfile.Id,
            insufficientResult,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        var smallProfile = await dbContext.MaterialRequirementProfiles.AsNoTracking()
            .Include(p => p.Material)
            .FirstOrDefaultAsync(p => p.Id == DevelopmentPilotIds.MaterialPilotRequirementPpSmallId, cancellationToken);

        if (smallProfile is null)
        {
            return;
        }

        var smallDetail = MapProfileDetail(smallProfile);
        var smallRequirement = MaterialRequirementCalculator.CalculateRequirement(smallDetail);
        var sufficientResult = MaterialAvailabilityRiskCalculator.AssessAvailabilityRisk(
            material,
            smallDetail,
            smallRequirement,
            erpBalances,
            seedTimestampUtc,
            hasErpReadAccess: true);

        await EnsureAssessmentAsync(
            dbContext,
            DevelopmentPilotIds.MaterialPilotAvailabilityRiskSufficientAssessmentId,
            tenantId,
            facilityId,
            material.Id,
            smallProfile.Id,
            sufficientResult,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);
    }

    private static async Task EnsureSmallRequirementProfileAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.MaterialRequirementProfiles.AsNoTracking()
                .AnyAsync(p => p.Id == DevelopmentPilotIds.MaterialPilotRequirementPpSmallId, cancellationToken))
        {
            return;
        }

        dbContext.MaterialRequirementProfiles.Add(new MaterialRequirementProfile
        {
            Id = DevelopmentPilotIds.MaterialPilotRequirementPpSmallId,
            TenantId = tenantId,
            FacilityId = facilityId,
            MaterialId = DevelopmentPilotIds.MaterialPilotPpId,
            ProductionOrderReference = "PILOT-PO-PP-SMALL",
            ProductReference = "PILOT-PROD-PP-SAMPLE",
            MoldReference = "PILOT-MOLD-PP-4CAV",
            RequirementType = MaterialRequirementType.ProductionOrder,
            GoodPartTargetQuantity = 100m,
            PartWeightGrams = 45m,
            RunnerWeightGramsPerShot = 120m,
            EffectiveCavityCount = 4,
            SetupAllowanceGrams = 500m,
            ProcessLossAllowancePercent = 3m,
            PlannedVirginPercent = 80m,
            PlannedRegrindPercent = 20m,
            SourceType = MaterialSourceType.DevelopmentPilot,
            SourceReference = "material-requirement-pilot-pp-small",
            DataRecordedAtUtc = seedTimestampUtc,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = actorSubjectId,
            UpdatedBySubjectId = actorSubjectId,
            Version = 1,
        });
    }

    private static async Task EnsureAssessmentAsync(
        NevaxisZentraDbContext dbContext,
        Guid assessmentId,
        Guid tenantId,
        Guid facilityId,
        Guid materialId,
        Guid profileId,
        MaterialAvailabilityRiskAssessmentResult availability,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.MaterialRequirementAssessments.AsNoTracking()
                .AnyAsync(a => a.Id == assessmentId, cancellationToken))
        {
            var tracked = await dbContext.MaterialRequirementAssessments
                .FirstOrDefaultAsync(a => a.Id == assessmentId, cancellationToken);
            if (tracked is not null && tracked.Status == availability.Status)
            {
                return;
            }

            if (tracked is not null)
            {
                dbContext.MaterialRequirementAssessments.Remove(tracked);
            }
        }

        var inputJson = JsonSerializer.Serialize(availability, JsonOptions);
        var fingerprint = MaterialRequirementIdempotencyFingerprint.ComputeAssessmentInputFingerprint(
            MaterialRequirementAssessmentType.AvailabilityRisk,
            materialId,
            profileId,
            MaterialAvailabilityRiskConstants.RuleSetVersion,
            inputJson);

        dbContext.MaterialRequirementAssessments.Add(new MaterialRequirementAssessment
        {
            Id = assessmentId,
            TenantId = tenantId,
            FacilityId = facilityId,
            MaterialId = materialId,
            RequirementProfileId = profileId,
            AssessmentType = MaterialRequirementAssessmentType.AvailabilityRisk,
            Status = availability.Status,
            RuleSetVersion = MaterialAvailabilityRiskConstants.RuleSetVersion,
            InputFingerprint = fingerprint,
            StructuredDrivers = JsonSerializer.Serialize(availability.Drivers, JsonOptions),
            StructuredEvidence = JsonSerializer.Serialize(availability.Evidence, JsonOptions),
            TheoreticalRequirementGrams = availability.RequiredQuantityGrams,
            ErpAvailableQuantity = availability.ErpAvailableQuantity,
            ErpReservedQuantity = availability.ErpReservedQuantity,
            ErpUnit = availability.ErpUnit,
            AvailableAfterReservationQuantity = availability.AvailableAfterReservationQuantity,
            ShortageQuantity = availability.ShortageQuantity,
            ErpSourceUpdatedAtUtc = availability.ErpSourceUpdatedAtUtc,
            SourceFreshness = availability.SourceFreshness,
            EvaluatedAtUtc = seedTimestampUtc,
            CreatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = actorSubjectId,
            Version = 1,
            FollowUpStatus = MaterialAvailabilityRiskFollowUpStatuses.RequiresReview,
        });
    }

    private static MaterialRequirementProfileDetailReadModel MapProfileDetail(MaterialRequirementProfile profile) =>
        new(
            profile.Id,
            profile.FacilityId,
            profile.MaterialId,
            profile.Material?.Code ?? string.Empty,
            profile.Material?.Name ?? string.Empty,
            MaterialRequirementValueConversions.ToStorage(profile.RequirementType),
            profile.ProductionOrderReference,
            profile.ProductReference,
            profile.MoldReference,
            profile.PlannedProductionQuantity,
            profile.GoodPartTargetQuantity,
            profile.PartWeightGrams,
            profile.RunnerWeightGramsPerShot,
            profile.EffectiveCavityCount,
            profile.SetupAllowanceGrams,
            profile.ProcessLossAllowancePercent,
            profile.PlannedVirginPercent,
            profile.PlannedRegrindPercent,
            MaterialValueConversions.ToStorage(profile.SourceType),
            profile.SourceReference,
            profile.DataRecordedAtUtc,
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc,
            profile.Version);
}
