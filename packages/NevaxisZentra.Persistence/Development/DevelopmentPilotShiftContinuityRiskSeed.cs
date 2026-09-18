using NevaxisZentra.Persistence.Production;
using NevaxisZentra.Persistence.ShiftAssignment;
using NevaxisZentra.Persistence.ShiftRisk;
using NevaxisZentra.Persistence.ShiftWorkforce;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Development;

internal static class DevelopmentPilotShiftContinuityRiskSeed
{
    internal static async Task EnsureShiftContinuityRiskPilotRecordsAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var pilotAssessmentIds = new[]
        {
            DevelopmentPilotIds.ShiftContinuityPilotAssessmentClearId,
            DevelopmentPilotIds.ShiftContinuityPilotAssessmentAbsenceId,
            DevelopmentPilotIds.ShiftContinuityPilotAssessmentExpiredId,
            DevelopmentPilotIds.ShiftContinuityPilotAssessmentExpiringUrgentId,
            DevelopmentPilotIds.ShiftContinuityPilotAssessmentExpiringUpcomingId,
            DevelopmentPilotIds.ShiftContinuityPilotAssessmentPendingVerificationId,
            DevelopmentPilotIds.ShiftContinuityPilotAssessmentStaleSourceId,
            DevelopmentPilotIds.ShiftContinuityPilotAssessmentUnknownAvailabilityId,
            DevelopmentPilotIds.ShiftContinuityPilotAssessmentOverlapId,
            DevelopmentPilotIds.ShiftContinuityPilotAssessmentWorkloadFactsId,
            DevelopmentPilotIds.ShiftContinuityPilotAssessmentCriticalCoverageId,
        };

        var assessmentReady = await dbContext.ShiftRiskAssessments.AsNoTracking()
            .CountAsync(
                a => pilotAssessmentIds.Contains(a.Id)
                     && a.AssessmentType == ShiftWorkforceContinuityConstants.AssessmentType,
                cancellationToken) == pilotAssessmentIds.Length
            && await dbContext.ShiftRiskAssessments.AsNoTracking()
                .AnyAsync(
                    a => a.Id == DevelopmentPilotIds.ShiftContinuityPilotAssessmentClearId
                         && a.Outcome == ShiftRiskOutcomes.Clear,
                    cancellationToken)
            && await dbContext.ShiftRiskAssessments.AsNoTracking()
                .AnyAsync(
                    a => a.Id == DevelopmentPilotIds.ShiftContinuityPilotAssessmentExpiredId
                         && a.WorkforcePersonId == DevelopmentPilotIds.ShiftWorkforcePilotPerson002Id,
                    cancellationToken)
            && await dbContext.ShiftRiskAssessments.AsNoTracking()
                .AnyAsync(
                    a => a.Id == DevelopmentPilotIds.ShiftContinuityPilotAssessmentUnknownAvailabilityId
                         && a.Outcome == ShiftRiskOutcomes.InsufficientData,
                    cancellationToken)
            && await dbContext.ShiftRiskAssessments.AsNoTracking()
                .AnyAsync(
                    a => a.Id == DevelopmentPilotIds.ShiftContinuityPilotAssessmentExpiringUpcomingId
                         && a.Outcome == ShiftRiskOutcomes.Attention,
                    cancellationToken);

        if (assessmentReady)
        {
            return;
        }

        var pilotDrivers = await dbContext.ShiftRiskDrivers
            .Where(d => pilotAssessmentIds.Contains(d.AssessmentId))
            .ToListAsync(cancellationToken);
        dbContext.ShiftRiskDrivers.RemoveRange(pilotDrivers);

        var pilotAssessments = await dbContext.ShiftRiskAssessments
            .Where(a => pilotAssessmentIds.Contains(a.Id))
            .ToListAsync(cancellationToken);
        dbContext.ShiftRiskAssessments.RemoveRange(pilotAssessments);

        var pilotAuditEvents = await dbContext.ShiftRiskAuditEvents
            .Where(e => e.EntityType == ShiftRiskEntityTypes.Assessment
                        && pilotAssessmentIds.Contains(e.EntityId))
            .ToListAsync(cancellationToken);
        dbContext.ShiftRiskAuditEvents.RemoveRange(pilotAuditEvents);

        await dbContext.SaveChangesAsync(cancellationToken);

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

        var competencyCode = ShiftWorkforceCompetencyCodes.InjectionMachineOperation;
        var injectionDefinitionId = DevelopmentPilotIds.ShiftWorkforceCompetencyInjectionId;

        await EnsureContinuityCompetencyAsync(
            dbContext,
            tenantId,
            facilityId,
            DevelopmentPilotIds.ShiftContinuityPilotCompetencyClearAssignmentId,
            DevelopmentPilotIds.ShiftWorkforcePilotPerson001Id,
            injectionDefinitionId,
            ShiftWorkforceVerificationStatuses.Verified,
            seedTimestampUtc.AddMonths(-6),
            seedTimestampUtc.AddMonths(12),
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureContinuityCompetencyAsync(
            dbContext,
            tenantId,
            facilityId,
            DevelopmentPilotIds.ShiftWorkforcePilotAssignmentInjectionId,
            DevelopmentPilotIds.ShiftWorkforcePilotPerson002Id,
            injectionDefinitionId,
            ShiftWorkforceVerificationStatuses.Verified,
            seedTimestampUtc.AddMonths(-12),
            seedTimestampUtc.AddDays(-1),
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureContinuityCompetencyAsync(
            dbContext,
            tenantId,
            facilityId,
            DevelopmentPilotIds.ShiftContinuityPilotCompetencyUrgentAssignmentId,
            DevelopmentPilotIds.ShiftWorkforcePilotPerson004Id,
            DevelopmentPilotIds.ShiftWorkforceCompetencyMoldChangeId,
            ShiftWorkforceVerificationStatuses.Verified,
            seedTimestampUtc.AddMonths(-6),
            seedTimestampUtc.AddDays(10),
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureContinuityCompetencyAsync(
            dbContext,
            tenantId,
            facilityId,
            DevelopmentPilotIds.ShiftContinuityPilotCompetencyUpcomingAssignmentId,
            DevelopmentPilotIds.ShiftWorkforcePilotPerson004Id,
            injectionDefinitionId,
            ShiftWorkforceVerificationStatuses.Verified,
            seedTimestampUtc.AddMonths(-6),
            seedTimestampUtc.AddDays(25),
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureContinuityCompetencyAsync(
            dbContext,
            tenantId,
            facilityId,
            DevelopmentPilotIds.ShiftContinuityPilotCompetencyPendingAssignmentId,
            DevelopmentPilotIds.ShiftWorkforcePilotPerson005Id,
            injectionDefinitionId,
            ShiftWorkforceVerificationStatuses.PendingVerification,
            seedTimestampUtc.AddMonths(-1),
            seedTimestampUtc.AddMonths(6),
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureCriticalRequirementAsync(
            dbContext,
            tenantId,
            facilityId,
            shift,
            shiftDate,
            plannedStartUtc,
            plannedEndUtc,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureStaleAssignmentAsync(
            dbContext,
            tenantId,
            facilityId,
            shiftDate,
            plannedStartUtc,
            plannedEndUtc,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureOverlapAssignmentsAsync(
            dbContext,
            tenantId,
            facilityId,
            shiftDate,
            plannedStartUtc,
            plannedEndUtc,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureWorkloadAssignmentAsync(
            dbContext,
            tenantId,
            facilityId,
            shiftDate,
            plannedStartUtc,
            plannedEndUtc,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        await SeedContinuityAssessmentsAsync(
            dbContext,
            tenantId,
            facilityId,
            actorSubjectId,
            shiftDate,
            plannedStartUtc,
            plannedEndUtc,
            competencyCode,
            seedTimestampUtc,
            cancellationToken);
    }

    private static async Task SeedContinuityAssessmentsAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateOnly shiftDate,
        DateTimeOffset plannedStartUtc,
        DateTimeOffset plannedEndUtc,
        string competencyCode,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var scenarios = new (Guid AssessmentId, Guid PersonId, string? OrderRef, Guid? MachineId, Guid? RequirementId, string? RequiredCompetencyCode)[]
        {
            (DevelopmentPilotIds.ShiftContinuityPilotAssessmentClearId,
                DevelopmentPilotIds.ShiftWorkforcePilotPerson001Id,
                "PILOT-PO-PP-001",
                DevelopmentPilotIds.EdgePilotPressMachineId,
                DevelopmentPilotIds.ShiftRiskPilotRequirementCoveredId,
                null),
            (DevelopmentPilotIds.ShiftContinuityPilotAssessmentAbsenceId,
                DevelopmentPilotIds.ShiftWorkforcePilotPerson001Id,
                "PILOT-PO-INJ-STALE",
                DevelopmentPilotIds.EdgePilotInjectionMachine2Id,
                DevelopmentPilotIds.ShiftRiskPilotRequirementStaleId,
                null),
            (DevelopmentPilotIds.ShiftContinuityPilotAssessmentExpiredId,
                DevelopmentPilotIds.ShiftWorkforcePilotPerson002Id,
                "PILOT-PO-INJ-002",
                DevelopmentPilotIds.EdgePilotInjectionMachine2Id,
                DevelopmentPilotIds.ShiftRiskPilotRequirementGapId,
                null),
            (DevelopmentPilotIds.ShiftContinuityPilotAssessmentExpiringUrgentId,
                DevelopmentPilotIds.ShiftWorkforcePilotPerson004Id,
                "PILOT-PO-INJ-002",
                DevelopmentPilotIds.EdgePilotInjectionMachine2Id,
                DevelopmentPilotIds.ShiftRiskPilotRequirementGapId,
                ShiftWorkforceCompetencyCodes.MoldChangeSetup),
            (DevelopmentPilotIds.ShiftContinuityPilotAssessmentExpiringUpcomingId,
                DevelopmentPilotIds.ShiftWorkforcePilotPerson004Id,
                "PILOT-PO-PP-001",
                DevelopmentPilotIds.EdgePilotPressMachineId,
                DevelopmentPilotIds.ShiftRiskPilotRequirementCoveredId,
                null),
            (DevelopmentPilotIds.ShiftContinuityPilotAssessmentPendingVerificationId,
                DevelopmentPilotIds.ShiftWorkforcePilotPerson005Id,
                "PILOT-PO-INJ-002",
                DevelopmentPilotIds.EdgePilotInjectionMachine2Id,
                null,
                null),
            (DevelopmentPilotIds.ShiftContinuityPilotAssessmentStaleSourceId,
                DevelopmentPilotIds.ShiftWorkforcePilotPerson003Id,
                "PILOT-PO-INJ-STALE",
                DevelopmentPilotIds.EdgePilotInjectionMachine2Id,
                null,
                null),
            (DevelopmentPilotIds.ShiftContinuityPilotAssessmentUnknownAvailabilityId,
                DevelopmentPilotIds.ShiftWorkforcePilotPerson001Id,
                null,
                null,
                null,
                null),
            (DevelopmentPilotIds.ShiftContinuityPilotAssessmentOverlapId,
                DevelopmentPilotIds.ShiftWorkforcePilotPerson004Id,
                "PILOT-PO-INJ-OVERLAP",
                DevelopmentPilotIds.EdgePilotInjectionMachine2Id,
                null,
                null),
            (DevelopmentPilotIds.ShiftContinuityPilotAssessmentWorkloadFactsId,
                DevelopmentPilotIds.ShiftWorkforcePilotPerson001Id,
                "PILOT-PO-WORKLOAD",
                DevelopmentPilotIds.EdgePilotInjectionMachine2Id,
                null,
                null),
            (DevelopmentPilotIds.ShiftContinuityPilotAssessmentCriticalCoverageId,
                DevelopmentPilotIds.ShiftWorkforcePilotPerson003Id,
                "PILOT-PO-CONT-CRIT",
                DevelopmentPilotIds.EdgePilotInjectionMachine3Id,
                DevelopmentPilotIds.ShiftContinuityPilotCriticalRequirementId,
                null),
        };

        foreach (var scenario in scenarios)
        {
            var scenarioCompetencyCode = scenario.RequiredCompetencyCode ?? competencyCode;
            var person = await dbContext.ShiftWorkforcePersons.AsNoTracking()
                .SingleAsync(p => p.Id == scenario.PersonId, cancellationToken);

            var competencies = await dbContext.ShiftWorkforceCompetencyAssignments.AsNoTracking()
                .Include(a => a.CompetencyDefinition)
                .Where(a => a.TenantId == tenantId
                            && a.FacilityId == facilityId
                            && a.WorkforcePersonId == scenario.PersonId)
                .ToListAsync(cancellationToken);

            var competencyEvidence = competencies.Select(a => new ShiftWorkforceContinuityRiskEvaluator.CompetencyContext(
                a.Id,
                a.CompetencyDefinition?.Code ?? string.Empty,
                a.CompetencyDefinition?.DisplayLabelTr ?? string.Empty,
                a.VerificationStatus,
                a.ValidFromUtc,
                a.ValidUntilUtc,
                true)).ToList();

            var assignments = await dbContext.ShiftOperatorAssignments.AsNoTracking()
                .Where(a => a.TenantId == tenantId
                            && a.FacilityId == facilityId
                            && a.WorkforcePersonId == scenario.PersonId
                            && a.ShiftDate == shiftDate)
                .ToListAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(scenario.OrderRef))
            {
                assignments = assignments
                    .Where(a => string.Equals(
                        a.ProductionOrderReference,
                        scenario.OrderRef,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            else
            {
                assignments = [];
            }

            var assignmentEvidence = assignments.Select(a => new ShiftWorkforceContinuityRiskEvaluator.AssignmentContext(
                a.Id,
                a.WorkforcePersonId,
                a.Status,
                a.PlannedStartUtc,
                a.PlannedEndUtc,
                a.ProductionOrderReference,
                a.MachineAssetId,
                a.RequiredCompetencyCode,
                true)).ToList();

            var requirements = await dbContext.ShiftCoverageRequirements.AsNoTracking()
                .Where(r => r.TenantId == tenantId && r.FacilityId == facilityId && r.ShiftDate == shiftDate)
                .ToListAsync(cancellationToken);

            var requirementEvidence = scenario.RequirementId is null
                ? []
                : requirements
                    .Where(r => r.Id == scenario.RequirementId)
                    .Select(r => new ShiftWorkforceContinuityRiskEvaluator.CoverageRequirementContext(
                    r.Id,
                    r.ProductionOrderReference,
                    r.MachineAssetId,
                    r.RequiredCompetencyCode,
                    r.MinimumApprovedOperatorCount,
                    r.IsCriticalRole))
                .ToList();

            var personContext = new ShiftWorkforceContinuityRiskEvaluator.PersonContext(
                person.Id,
                person.EmployeeCode,
                person.DisplayName,
                person.IsActive,
                scenario.AssessmentId == DevelopmentPilotIds.ShiftContinuityPilotAssessmentAbsenceId
                    ? ShiftWorkforceAvailabilityStates.Leave
                    : scenario.AssessmentId == DevelopmentPilotIds.ShiftContinuityPilotAssessmentUnknownAvailabilityId
                        ? ShiftWorkforceAvailabilityStates.Unknown
                        : person.AvailabilityState,
                person.FreshnessState,
                person.SourceUpdatedAtUtc,
                true);

            var evaluation = ShiftWorkforceContinuityRiskEvaluator.Evaluate(
                personContext,
                competencyEvidence,
                assignmentEvidence,
                requirementEvidence,
                DevelopmentPilotIds.PilotShiftV1Id,
                shiftDate,
                plannedStartUtc,
                plannedEndUtc,
                scenarioCompetencyCode,
                seedTimestampUtc);

            var evaluationContext = new ShiftRiskEvaluationContext(
                DevelopmentPilotIds.PilotShiftV1Id,
                shiftDate,
                plannedStartUtc,
                plannedEndUtc,
                scenario.OrderRef,
                scenario.MachineId,
                null,
                scenarioCompetencyCode);

            var inputFingerprint = ShiftRiskIdempotencyFingerprint.ComputeContinuityEvaluationInputFingerprint(
                scenario.PersonId,
                evaluationContext);

            var assessment = new ShiftRiskAssessment
            {
                Id = scenario.AssessmentId,
                TenantId = tenantId,
                FacilityId = facilityId,
                AssessmentType = ShiftWorkforceContinuityConstants.AssessmentType,
                WorkforcePersonId = scenario.PersonId,
                CoverageRequirementId = scenario.RequirementId,
                ProductionShiftId = DevelopmentPilotIds.PilotShiftV1Id,
                ShiftDate = shiftDate,
                PlannedStartUtc = plannedStartUtc,
                PlannedEndUtc = plannedEndUtc,
                ProductionOrderReference = scenario.OrderRef,
                MachineAssetId = scenario.MachineId,
                RequiredCompetencyCode = scenarioCompetencyCode,
                ApprovedEligibleCount = evaluation.TrustworthyAvailableCount,
                ApprovedAssignmentCount = evaluation.ApprovedAssignmentCount,
                PlannedAssignmentMinutes = evaluation.PlannedAssignmentMinutes,
                CoverageGap = evaluation.CoverageGap,
                RuleSetVersion = ShiftWorkforceContinuityConstants.RuleSetVersion,
                InputFingerprint = inputFingerprint,
                Outcome = evaluation.Outcome,
                LeadingDriverCategory = evaluation.LeadingDriverCategory,
                LeadingReasonCode = evaluation.LeadingReasonCode,
                StructuredEvidenceJson = """{"pilot":true}""",
                SourceFreshnessSnapshotJson = """{"pilot":true}""",
                EvaluatedAtUtc = seedTimestampUtc,
                EvaluateIdempotencyKey = $"pilot-shift-continuity-{scenario.AssessmentId:N}",
                EvaluateIdempotencyPayloadFingerprint = inputFingerprint,
                CreatedAtUtc = seedTimestampUtc,
                CreatedBy = actorSubjectId,
            };

            foreach (var driverDraft in evaluation.Drivers)
            {
                assessment.Drivers.Add(new ShiftRiskDriver
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    AssessmentId = assessment.Id,
                    ReasonCode = driverDraft.ReasonCode,
                    LabelTr = ShiftWorkforceContinuityReasonLabels.GetLabelTr(driverDraft.ReasonCode),
                    Category = driverDraft.Category,
                    Severity = driverDraft.Severity,
                    RequiredValue = driverDraft.RequiredValue,
                    ObservedValue = driverDraft.ObservedValue,
                    GapValue = driverDraft.GapValue,
                    WorkforcePersonId = driverDraft.WorkforcePersonId,
                    ProductionShiftId = driverDraft.ProductionShiftId,
                    ProductionOrderReference = driverDraft.ProductionOrderReference,
                    MachineAssetId = driverDraft.MachineAssetId,
                    RequiredCompetencyCode = driverDraft.RequiredCompetencyCode,
                    EvidenceSource = driverDraft.EvidenceSource,
                    EvidenceReference = driverDraft.EvidenceReference,
                    EvidenceTimestampUtc = driverDraft.EvidenceTimestampUtc,
                    FreshnessState = driverDraft.FreshnessState,
                    RuleSetVersion = ShiftWorkforceContinuityConstants.RuleSetVersion,
                    IsLeadingRisk = driverDraft.ReasonCode == evaluation.LeadingReasonCode,
                });
            }

            assessment.LeadingDriverId = assessment.Drivers.FirstOrDefault(d => d.IsLeadingRisk)?.Id;

            dbContext.ShiftRiskAssessments.Add(assessment);

            dbContext.ShiftRiskAuditEvents.Add(new ShiftRiskAuditEvent
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                EventType = ShiftRiskAuditEventTypes.AssessmentEvaluated,
                EntityType = ShiftRiskEntityTypes.Assessment,
                EntityId = assessment.Id,
                PayloadJson = $"{{\"assessmentId\":\"{assessment.Id}\",\"outcome\":\"{assessment.Outcome}\",\"assessmentType\":\"{assessment.AssessmentType}\"}}",
                ActorSubjectId = actorSubjectId,
                OccurredAtUtc = seedTimestampUtc,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureContinuityCompetencyAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        Guid assignmentId,
        Guid personId,
        Guid definitionId,
        string verificationStatus,
        DateTimeOffset validFrom,
        DateTimeOffset? validUntil,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var assignment = await dbContext.ShiftWorkforceCompetencyAssignments
            .FirstOrDefaultAsync(a => a.Id == assignmentId, cancellationToken);

        if (assignment is null)
        {
            dbContext.ShiftWorkforceCompetencyAssignments.Add(new ShiftWorkforceCompetencyAssignment
            {
                Id = assignmentId,
                TenantId = tenantId,
                FacilityId = facilityId,
                WorkforcePersonId = personId,
                CompetencyDefinitionId = definitionId,
                CompetencyLevel = ShiftWorkforceCompetencyLevels.Competent,
                VerificationStatus = verificationStatus,
                ValidFromUtc = validFrom,
                ValidUntilUtc = validUntil,
                CreateIdempotencyKey = $"pilot-shift-continuity-{assignmentId:N}",
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                CreatedBy = actorSubjectId,
                UpdatedBy = actorSubjectId,
                Version = 1,
            });
            return;
        }

        assignment.WorkforcePersonId = personId;
        assignment.CompetencyDefinitionId = definitionId;
        assignment.VerificationStatus = verificationStatus;
        assignment.ValidFromUtc = validFrom;
        assignment.ValidUntilUtc = validUntil;
        assignment.UpdatedAtUtc = seedTimestampUtc;
    }

    private static async Task EnsureCriticalRequirementAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        ProductionShift shift,
        DateOnly shiftDate,
        DateTimeOffset plannedStartUtc,
        DateTimeOffset plannedEndUtc,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.ShiftCoverageRequirements.AsNoTracking()
            .AnyAsync(r => r.Id == DevelopmentPilotIds.ShiftContinuityPilotCriticalRequirementId, cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.ShiftCoverageRequirements.Add(new ShiftCoverageRequirement
        {
            Id = DevelopmentPilotIds.ShiftContinuityPilotCriticalRequirementId,
            TenantId = tenantId,
            FacilityId = facilityId,
            ProductionShiftId = DevelopmentPilotIds.PilotShiftV1Id,
            ShiftDate = shiftDate,
            PlannedStartUtc = plannedStartUtc,
            PlannedEndUtc = plannedEndUtc,
            ProductionOrderReference = "PILOT-PO-CONT-CRIT",
            MachineAssetId = DevelopmentPilotIds.EdgePilotInjectionMachine3Id,
            RequiredCompetencyCode = ShiftWorkforceCompetencyCodes.InjectionMachineOperation,
            MinimumApprovedOperatorCount = 1,
            IsCriticalRole = true,
            SourceReference = "pilot-shift-continuity-critical",
            IsActive = true,
            Version = 1,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
            UpdatedBy = actorSubjectId,
        });

        _ = shift;
    }

    private static async Task EnsureOverlapAssignmentsAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateOnly shiftDate,
        DateTimeOffset plannedStartUtc,
        DateTimeOffset plannedEndUtc,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var assessment = await dbContext.ShiftOperatorEligibilityAssessments.AsNoTracking()
            .FirstAsync(a => a.Id == DevelopmentPilotIds.ShiftAssignPilotAssessmentId, cancellationToken);

        var midStart = plannedStartUtc.AddHours(2);
        var midEnd = plannedEndUtc.AddHours(-2);

        foreach (var (assignmentId, start, end) in new[]
                 {
                     (DevelopmentPilotIds.ShiftContinuityPilotOverlapAssignmentAId, plannedStartUtc, midEnd),
                     (DevelopmentPilotIds.ShiftContinuityPilotOverlapAssignmentBId, midStart, plannedEndUtc),
                 })
        {
            if (await dbContext.ShiftOperatorAssignments.AsNoTracking()
                    .AnyAsync(a => a.Id == assignmentId, cancellationToken))
            {
                continue;
            }

            dbContext.ShiftOperatorAssignments.Add(new ShiftOperatorAssignment
            {
                Id = assignmentId,
                TenantId = tenantId,
                FacilityId = facilityId,
                ProductionOrderReference = "PILOT-PO-INJ-OVERLAP",
                MachineAssetId = DevelopmentPilotIds.EdgePilotInjectionMachine2Id,
                ProductionShiftId = DevelopmentPilotIds.PilotShiftV1Id,
                ShiftDate = shiftDate,
                WorkforcePersonId = DevelopmentPilotIds.ShiftWorkforcePilotPerson004Id,
                PlannedStartUtc = start,
                PlannedEndUtc = end,
                Status = ShiftAssignmentStatuses.Approved,
                EligibilityAssessmentId = assessment.Id,
                EligibilityResultId = DevelopmentPilotIds.ShiftAssignPilotResult002Id,
                RequiredCompetencyCode = ShiftWorkforceCompetencyCodes.InjectionMachineOperation,
                ApprovedBySubjectId = actorSubjectId,
                ApprovedAtUtc = seedTimestampUtc,
                RuleSetVersion = ShiftAssignmentConstants.RuleSetVersion,
                Version = 1,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                CreatedBy = actorSubjectId,
                UpdatedBy = actorSubjectId,
            });
        }
    }

    private static async Task EnsureStaleAssignmentAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateOnly shiftDate,
        DateTimeOffset plannedStartUtc,
        DateTimeOffset plannedEndUtc,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.ShiftOperatorAssignments.AsNoTracking()
                .AnyAsync(
                    a => a.Id == DevelopmentPilotIds.ShiftContinuityPilotStaleAssignmentId
                         || (a.WorkforcePersonId == DevelopmentPilotIds.ShiftWorkforcePilotPerson003Id
                             && a.ShiftDate == shiftDate
                             && a.ProductionOrderReference == "PILOT-PO-INJ-STALE"),
                    cancellationToken))
        {
            return;
        }

        var assessment = await dbContext.ShiftOperatorEligibilityAssessments.AsNoTracking()
            .FirstAsync(a => a.Id == DevelopmentPilotIds.ShiftAssignPilotAssessmentId, cancellationToken);

        dbContext.ShiftOperatorAssignments.Add(new ShiftOperatorAssignment
        {
            Id = DevelopmentPilotIds.ShiftContinuityPilotStaleAssignmentId,
            TenantId = tenantId,
            FacilityId = facilityId,
            ProductionOrderReference = "PILOT-PO-INJ-STALE",
            MachineAssetId = DevelopmentPilotIds.EdgePilotInjectionMachine2Id,
            ProductionShiftId = DevelopmentPilotIds.PilotShiftV1Id,
            ShiftDate = shiftDate,
            WorkforcePersonId = DevelopmentPilotIds.ShiftWorkforcePilotPerson003Id,
            PlannedStartUtc = plannedStartUtc,
            PlannedEndUtc = plannedEndUtc,
            Status = ShiftAssignmentStatuses.Approved,
            EligibilityAssessmentId = assessment.Id,
            EligibilityResultId = DevelopmentPilotIds.ShiftAssignPilotResult002Id,
            RequiredCompetencyCode = ShiftWorkforceCompetencyCodes.InjectionMachineOperation,
            ApprovedBySubjectId = actorSubjectId,
            ApprovedAtUtc = seedTimestampUtc,
            RuleSetVersion = ShiftAssignmentConstants.RuleSetVersion,
            Version = 1,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
            UpdatedBy = actorSubjectId,
        });
    }

    private static async Task EnsureWorkloadAssignmentAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateOnly shiftDate,
        DateTimeOffset plannedStartUtc,
        DateTimeOffset plannedEndUtc,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.ShiftOperatorAssignments.AsNoTracking()
                .AnyAsync(
                    a => a.Id == DevelopmentPilotIds.ShiftContinuityPilotWorkloadAssignmentId
                         || (a.WorkforcePersonId == DevelopmentPilotIds.ShiftWorkforcePilotPerson001Id
                             && a.ShiftDate == shiftDate
                             && a.ProductionOrderReference == "PILOT-PO-WORKLOAD"),
                    cancellationToken))
        {
            return;
        }

        var assessment = await dbContext.ShiftOperatorEligibilityAssessments.AsNoTracking()
            .FirstAsync(a => a.Id == DevelopmentPilotIds.ShiftAssignPilotAssessmentId, cancellationToken);

        dbContext.ShiftOperatorAssignments.Add(new ShiftOperatorAssignment
        {
            Id = DevelopmentPilotIds.ShiftContinuityPilotWorkloadAssignmentId,
            TenantId = tenantId,
            FacilityId = facilityId,
            ProductionOrderReference = "PILOT-PO-WORKLOAD",
            MachineAssetId = DevelopmentPilotIds.EdgePilotInjectionMachine2Id,
            ProductionShiftId = DevelopmentPilotIds.PilotShiftV1Id,
            ShiftDate = shiftDate,
            WorkforcePersonId = DevelopmentPilotIds.ShiftWorkforcePilotPerson001Id,
            PlannedStartUtc = plannedStartUtc,
            PlannedEndUtc = plannedEndUtc,
            Status = ShiftAssignmentStatuses.Approved,
            EligibilityAssessmentId = assessment.Id,
            EligibilityResultId = DevelopmentPilotIds.ShiftAssignPilotResult002Id,
            RequiredCompetencyCode = ShiftWorkforceCompetencyCodes.InjectionMachineOperation,
            ApprovedBySubjectId = actorSubjectId,
            ApprovedAtUtc = seedTimestampUtc,
            RuleSetVersion = ShiftAssignmentConstants.RuleSetVersion,
            Version = 1,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
            UpdatedBy = actorSubjectId,
        });
    }
}
