using NevaxisFactOS.Persistence.ShiftAssignment;
using NevaxisFactOS.Persistence.ShiftWorkforce;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Development;

internal static class DevelopmentPilotShiftAssignmentSeed
{
    internal static async Task EnsureShiftAssignmentPilotRecordsAsync(
        NevaxisFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var assignmentExists = await dbContext.ShiftOperatorAssignments.AsNoTracking()
            .AnyAsync(a => a.Id == DevelopmentPilotIds.ShiftAssignPilotAssignment002Id, cancellationToken);

        if (assignmentExists)
        {
            return;
        }

        var facility = await dbContext.Facilities.AsNoTracking()
            .SingleAsync(f => f.TenantId == tenantId && f.Id == facilityId, cancellationToken);

        var shift = await dbContext.ProductionShifts.AsNoTracking()
            .SingleAsync(
                s => s.Id == DevelopmentPilotIds.PilotShiftV1Id
                     && s.TenantId == tenantId
                     && s.FacilityId == facilityId,
                cancellationToken);

        var shiftDate = DateOnly.FromDateTime(seedTimestampUtc.UtcDateTime);
        var (plannedStartUtc, plannedEndUtc) = ShiftAssignmentShiftWindowCalculator.ComputeShiftWindowUtc(
            facility,
            shift,
            shiftDate);

        var productionOrderReference = "PILOT-PO-PP-001";
        var requiredCompetencyCode = ShiftWorkforceCompetencyCodes.InjectionMachineOperation;
        var evaluationContext = new ShiftAssignmentEvaluationContext(
            productionOrderReference,
            DevelopmentPilotIds.EdgePilotPressMachineId,
            DevelopmentPilotIds.PilotShiftV1Id,
            shiftDate,
            plannedStartUtc,
            plannedEndUtc,
            requiredCompetencyCode,
            null,
            null,
            shift.Code);

        var inputFingerprint = ShiftAssignmentIdempotencyFingerprint.ComputeAssessmentInputFingerprint(evaluationContext);

        var assessment = new ShiftOperatorEligibilityAssessment
        {
            Id = DevelopmentPilotIds.ShiftAssignPilotAssessmentId,
            TenantId = tenantId,
            FacilityId = facilityId,
            ProductionOrderReference = productionOrderReference,
            MachineAssetId = DevelopmentPilotIds.EdgePilotPressMachineId,
            ProductionShiftId = DevelopmentPilotIds.PilotShiftV1Id,
            ShiftDate = shiftDate,
            PlannedStartUtc = plannedStartUtc,
            PlannedEndUtc = plannedEndUtc,
            RequiredCompetencyCode = requiredCompetencyCode,
            RuleSetVersion = ShiftAssignmentConstants.RuleSetVersion,
            InputFingerprint = inputFingerprint,
            EvaluatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
        };

        var person = await dbContext.ShiftWorkforcePersons.AsNoTracking()
            .SingleAsync(p => p.Id == DevelopmentPilotIds.ShiftWorkforcePilotPerson002Id, cancellationToken);

        var competency = await dbContext.ShiftWorkforceCompetencyAssignments.AsNoTracking()
            .Include(a => a.CompetencyDefinition)
            .FirstAsync(
                a => a.Id == DevelopmentPilotIds.ShiftWorkforcePilotAssignmentInjectionId,
                cancellationToken);

        var evaluation = ShiftOperatorEligibilityEvaluator.EvaluatePerson(
            new ShiftOperatorEligibilityEvaluator.PersonEvaluationInput(
                person,
                competency,
                "Enjeksiyon makinesi operasyonu",
                shift.Code,
                seedTimestampUtc,
                false),
            missingRequiredCompetencyCode: false);

        var result = new ShiftOperatorEligibilityResult
        {
            Id = DevelopmentPilotIds.ShiftAssignPilotResult002Id,
            TenantId = tenantId,
            AssessmentId = assessment.Id,
            WorkforcePersonId = DevelopmentPilotIds.ShiftWorkforcePilotPerson002Id,
            Outcome = evaluation.Outcome,
            PrimaryReasonCode = evaluation.PrimaryReasonCode,
            EmployeeCodeSnapshot = person.EmployeeCode,
            DisplayNameSnapshot = person.DisplayName,
        };

        foreach (var reason in evaluation.Reasons)
        {
            result.Reasons.Add(new ShiftOperatorEligibilityReason
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ResultId = result.Id,
                ReasonCode = reason.ReasonCode,
                LabelTr = ShiftAssignmentReasonLabels.GetLabelTr(reason.ReasonCode),
                Severity = reason.Severity,
                RequiredValue = reason.RequiredValue,
                ObservedValue = reason.ObservedValue,
                EvidenceSource = reason.EvidenceSource,
                EvidenceTimestampUtc = reason.EvidenceTimestampUtc,
                FreshnessState = reason.FreshnessState,
            });
        }

        assessment.Results.Add(result);
        dbContext.ShiftOperatorEligibilityAssessments.Add(assessment);

        dbContext.ShiftOperatorAssignments.Add(new ShiftOperatorAssignment
        {
            Id = DevelopmentPilotIds.ShiftAssignPilotAssignment002Id,
            TenantId = tenantId,
            FacilityId = facilityId,
            ProductionOrderReference = productionOrderReference,
            MachineAssetId = DevelopmentPilotIds.EdgePilotPressMachineId,
            ProductionShiftId = DevelopmentPilotIds.PilotShiftV1Id,
            ShiftDate = shiftDate,
            WorkforcePersonId = DevelopmentPilotIds.ShiftWorkforcePilotPerson002Id,
            PlannedStartUtc = plannedStartUtc,
            PlannedEndUtc = plannedEndUtc,
            Status = ShiftAssignmentStatuses.Approved,
            EligibilityAssessmentId = assessment.Id,
            EligibilityResultId = result.Id,
            RequiredCompetencyCode = requiredCompetencyCode,
            ApprovedBySubjectId = actorSubjectId,
            ApprovedAtUtc = seedTimestampUtc,
            RuleSetVersion = ShiftAssignmentConstants.RuleSetVersion,
            Version = 1,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
            UpdatedBy = actorSubjectId,
        });

        dbContext.ShiftOperatorAssignmentAuditEvents.Add(new ShiftOperatorAssignmentAuditEvent
        {
            Id = Guid.Parse("bb000001-0103-4000-8000-000000000103"),
            TenantId = tenantId,
            FacilityId = facilityId,
            EventType = ShiftAssignmentAuditEventTypes.AssignmentApproved,
            EntityType = ShiftAssignmentEntityTypes.Assignment,
            EntityId = DevelopmentPilotIds.ShiftAssignPilotAssignment002Id,
            PayloadJson = """{"assignmentId":"bb000001-0102-4000-8000-000000000102","status":"approved"}""",
            ActorSubjectId = actorSubjectId,
            OccurredAtUtc = seedTimestampUtc,
        });
    }
}
