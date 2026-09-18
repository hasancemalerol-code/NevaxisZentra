using NevaxisZentra.Persistence.ShiftAssignment;
using NevaxisZentra.Persistence.ShiftRisk;
using NevaxisZentra.Persistence.ShiftWorkforce;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Development;

internal static class DevelopmentPilotShiftRiskSeed
{
    internal static async Task EnsureShiftRiskPilotRecordsAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var requirementExists = await dbContext.ShiftCoverageRequirements.AsNoTracking()
            .AnyAsync(r => r.Id == DevelopmentPilotIds.ShiftRiskPilotRequirementCoveredId, cancellationToken);

        var assessmentReady = await dbContext.ShiftRiskAssessments.AsNoTracking()
            .AnyAsync(
                a => a.Id == DevelopmentPilotIds.ShiftRiskPilotAssessmentCoveredId
                     && a.Outcome == ShiftRiskOutcomes.Clear,
                cancellationToken)
            && await dbContext.ShiftRiskAssessments.AsNoTracking()
                .AnyAsync(
                    a => a.Id == DevelopmentPilotIds.ShiftRiskPilotAssessmentStaleId
                         && a.Outcome == ShiftRiskOutcomes.InsufficientData,
                    cancellationToken);

        if (requirementExists && assessmentReady)
        {
            return;
        }

        if (requirementExists)
        {
            var pilotAssessmentIds = new[]
            {
                DevelopmentPilotIds.ShiftRiskPilotAssessmentCoveredId,
                DevelopmentPilotIds.ShiftRiskPilotAssessmentGapId,
                DevelopmentPilotIds.ShiftRiskPilotAssessmentCriticalId,
                DevelopmentPilotIds.ShiftRiskPilotAssessmentConflictId,
                DevelopmentPilotIds.ShiftRiskPilotAssessmentStaleId,
            };

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

        if (!requirementExists)
        {
        var competencyCode = ShiftWorkforceCompetencyCodes.InjectionMachineOperation;
        var requirements = new[]
        {
            new ShiftCoverageRequirement
            {
                Id = DevelopmentPilotIds.ShiftRiskPilotRequirementCoveredId,
                TenantId = tenantId,
                FacilityId = facilityId,
                ProductionShiftId = DevelopmentPilotIds.PilotShiftV1Id,
                ShiftDate = shiftDate,
                PlannedStartUtc = plannedStartUtc,
                PlannedEndUtc = plannedEndUtc,
                ProductionOrderReference = "PILOT-PO-PP-001",
                MachineAssetId = DevelopmentPilotIds.EdgePilotPressMachineId,
                RequiredCompetencyCode = competencyCode,
                MinimumApprovedOperatorCount = 1,
                IsCriticalRole = false,
                SourceReference = "pilot-shift-risk-covered",
                IsActive = true,
                Version = 1,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                CreatedBy = actorSubjectId,
                UpdatedBy = actorSubjectId,
            },
            new ShiftCoverageRequirement
            {
                Id = DevelopmentPilotIds.ShiftRiskPilotRequirementGapId,
                TenantId = tenantId,
                FacilityId = facilityId,
                ProductionShiftId = DevelopmentPilotIds.PilotShiftV1Id,
                ShiftDate = shiftDate,
                PlannedStartUtc = plannedStartUtc,
                PlannedEndUtc = plannedEndUtc,
                ProductionOrderReference = "PILOT-PO-INJ-002",
                MachineAssetId = DevelopmentPilotIds.EdgePilotInjectionMachine2Id,
                RequiredCompetencyCode = competencyCode,
                MinimumApprovedOperatorCount = 2,
                IsCriticalRole = false,
                SourceReference = "pilot-shift-risk-gap",
                IsActive = true,
                Version = 1,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                CreatedBy = actorSubjectId,
                UpdatedBy = actorSubjectId,
            },
            new ShiftCoverageRequirement
            {
                Id = DevelopmentPilotIds.ShiftRiskPilotRequirementCriticalId,
                TenantId = tenantId,
                FacilityId = facilityId,
                ProductionShiftId = DevelopmentPilotIds.PilotShiftV1Id,
                ShiftDate = shiftDate,
                PlannedStartUtc = plannedStartUtc,
                PlannedEndUtc = plannedEndUtc,
                ProductionOrderReference = "PILOT-PO-INJ-003",
                MachineAssetId = DevelopmentPilotIds.EdgePilotInjectionMachine3Id,
                RequiredCompetencyCode = competencyCode,
                MinimumApprovedOperatorCount = 1,
                IsCriticalRole = true,
                SourceReference = "pilot-shift-risk-critical",
                IsActive = true,
                Version = 1,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                CreatedBy = actorSubjectId,
                UpdatedBy = actorSubjectId,
            },
            new ShiftCoverageRequirement
            {
                Id = DevelopmentPilotIds.ShiftRiskPilotRequirementConflictId,
                TenantId = tenantId,
                FacilityId = facilityId,
                ProductionShiftId = DevelopmentPilotIds.PilotShiftV1Id,
                ShiftDate = shiftDate,
                PlannedStartUtc = plannedStartUtc,
                PlannedEndUtc = plannedEndUtc,
                ProductionOrderReference = "PILOT-PO-INJ-CONFLICT",
                MachineAssetId = DevelopmentPilotIds.EdgePilotInjectionMachine2Id,
                RequiredCompetencyCode = competencyCode,
                MinimumApprovedOperatorCount = 1,
                IsCriticalRole = false,
                SourceReference = "pilot-shift-risk-conflict",
                IsActive = true,
                Version = 1,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                CreatedBy = actorSubjectId,
                UpdatedBy = actorSubjectId,
            },
            new ShiftCoverageRequirement
            {
                Id = DevelopmentPilotIds.ShiftRiskPilotRequirementStaleId,
                TenantId = tenantId,
                FacilityId = facilityId,
                ProductionShiftId = DevelopmentPilotIds.PilotShiftV1Id,
                ShiftDate = shiftDate,
                PlannedStartUtc = plannedStartUtc,
                PlannedEndUtc = plannedEndUtc,
                ProductionOrderReference = "PILOT-PO-INJ-STALE",
                MachineAssetId = DevelopmentPilotIds.EdgePilotInjectionMachine2Id,
                RequiredCompetencyCode = competencyCode,
                MinimumApprovedOperatorCount = 1,
                IsCriticalRole = false,
                SourceReference = "pilot-shift-risk-stale",
                IsActive = true,
                Version = 1,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                CreatedBy = actorSubjectId,
                UpdatedBy = actorSubjectId,
            },
        };

        dbContext.ShiftCoverageRequirements.AddRange(requirements);
        }

        await EnsureDraftConflictAssignmentAsync(
            dbContext,
            tenantId,
            facilityId,
            actorSubjectId,
            shiftDate,
            plannedStartUtc,
            plannedEndUtc,
            seedTimestampUtc,
            cancellationToken);

        await SeedStaleApprovedAssignmentAsync(
            dbContext,
            tenantId,
            facilityId,
            actorSubjectId,
            shiftDate,
            plannedStartUtc,
            plannedEndUtc,
            seedTimestampUtc,
            cancellationToken);

        await EnsurePilotStaleWorkforceEvidenceAsync(
            dbContext,
            tenantId,
            seedTimestampUtc,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        await SeedPilotAssessmentsAsync(
            dbContext,
            tenantId,
            facilityId,
            actorSubjectId,
            shiftDate,
            plannedStartUtc,
            plannedEndUtc,
            seedTimestampUtc,
            cancellationToken);
    }

    private static async Task EnsurePilotStaleWorkforceEvidenceAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var person = await dbContext.ShiftWorkforcePersons
            .SingleOrDefaultAsync(
                p => p.TenantId == tenantId && p.Id == DevelopmentPilotIds.ShiftWorkforcePilotPerson003Id,
                cancellationToken);

        if (person is null)
        {
            return;
        }

        if (person.FreshnessState == ShiftWorkforceFreshnessStates.Stale
            && person.AvailabilityState == ShiftWorkforceAvailabilityStates.Available)
        {
            return;
        }

        person.FreshnessState = ShiftWorkforceFreshnessStates.Stale;
        person.AvailabilityState = ShiftWorkforceAvailabilityStates.Available;
        person.UpdatedAtUtc = seedTimestampUtc;
    }

    private static async Task EnsureDraftConflictAssignmentAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateOnly shiftDate,
        DateTimeOffset plannedStartUtc,
        DateTimeOffset plannedEndUtc,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.ShiftOperatorAssignments.AsNoTracking()
            .AnyAsync(a => a.Id == DevelopmentPilotIds.ShiftRiskPilotDraftConflictAssignmentId, cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.ShiftOperatorAssignments.Add(new ShiftOperatorAssignment
        {
            Id = DevelopmentPilotIds.ShiftRiskPilotDraftConflictAssignmentId,
            TenantId = tenantId,
            FacilityId = facilityId,
            ProductionOrderReference = "PILOT-PO-INJ-CONFLICT",
            MachineAssetId = DevelopmentPilotIds.EdgePilotInjectionMachine2Id,
            ProductionShiftId = DevelopmentPilotIds.PilotShiftV1Id,
            ShiftDate = shiftDate,
            WorkforcePersonId = DevelopmentPilotIds.ShiftWorkforcePilotPerson002Id,
            PlannedStartUtc = plannedStartUtc,
            PlannedEndUtc = plannedEndUtc,
            Status = ShiftAssignmentStatuses.Draft,
            EligibilityAssessmentId = DevelopmentPilotIds.ShiftAssignPilotAssessmentId,
            EligibilityResultId = DevelopmentPilotIds.ShiftAssignPilotResult002Id,
            RequiredCompetencyCode = ShiftWorkforceCompetencyCodes.InjectionMachineOperation,
            RuleSetVersion = ShiftAssignmentConstants.RuleSetVersion,
            Version = 1,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
            UpdatedBy = actorSubjectId,
        });
    }

    private static async Task SeedPilotAssessmentsAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateOnly shiftDate,
        DateTimeOffset plannedStartUtc,
        DateTimeOffset plannedEndUtc,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var assignments = await dbContext.ShiftOperatorAssignments.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.FacilityId == facilityId && a.ShiftDate == shiftDate)
            .ToListAsync(cancellationToken);

        var personIds = assignments.Select(a => a.WorkforcePersonId).Distinct().ToList();
        var persons = await dbContext.ShiftWorkforcePersons.AsNoTracking()
            .Where(p => p.TenantId == tenantId && personIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var eligibilityResults = await dbContext.ShiftOperatorEligibilityResults.AsNoTracking()
            .Where(r => r.TenantId == tenantId)
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        var assignmentEvidence = assignments.Select(a =>
        {
            var person = persons.GetValueOrDefault(a.WorkforcePersonId);
            var eligibility = eligibilityResults.GetValueOrDefault(a.EligibilityResultId);
            return new ShiftCoverageRiskEvaluator.AssignmentEvidence(
                a.Id,
                a.WorkforcePersonId,
                person?.EmployeeCode ?? "—",
                person?.DisplayName ?? "—",
                a.Status,
                a.PlannedStartUtc,
                a.PlannedEndUtc,
                a.RequiredCompetencyCode,
                a.MachineAssetId,
                a.ProductionOrderReference,
                a.EligibilityResultId,
                eligibility?.Outcome,
                eligibility?.PrimaryReasonCode,
                person?.SourceSystem == ShiftWorkforceSourceSystems.LocalDevelopmentPilot);
        }).ToList();

        var personEvidence = persons.Values.ToDictionary(
            p => p.Id,
            p => new ShiftCoverageRiskEvaluator.PersonEvidence(
                p.Id,
                p.EmployeeCode,
                p.DisplayName,
                p.IsActive,
                p.AvailabilityState,
                p.FreshnessState,
                p.SourceUpdatedAtUtc,
                p.SourceSystem == ShiftWorkforceSourceSystems.LocalDevelopmentPilot));

        var requirements = await dbContext.ShiftCoverageRequirements.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.FacilityId == facilityId)
            .ToListAsync(cancellationToken);

        var scenarios = new (Guid AssessmentId, Guid RequirementId, string? OrderRef, Guid? MachineId)[]
        {
            (DevelopmentPilotIds.ShiftRiskPilotAssessmentCoveredId,
                DevelopmentPilotIds.ShiftRiskPilotRequirementCoveredId,
                "PILOT-PO-PP-001",
                DevelopmentPilotIds.EdgePilotPressMachineId),
            (DevelopmentPilotIds.ShiftRiskPilotAssessmentGapId,
                DevelopmentPilotIds.ShiftRiskPilotRequirementGapId,
                "PILOT-PO-INJ-002",
                DevelopmentPilotIds.EdgePilotInjectionMachine2Id),
            (DevelopmentPilotIds.ShiftRiskPilotAssessmentCriticalId,
                DevelopmentPilotIds.ShiftRiskPilotRequirementCriticalId,
                "PILOT-PO-INJ-003",
                DevelopmentPilotIds.EdgePilotInjectionMachine3Id),
            (DevelopmentPilotIds.ShiftRiskPilotAssessmentConflictId,
                DevelopmentPilotIds.ShiftRiskPilotRequirementConflictId,
                "PILOT-PO-INJ-CONFLICT",
                DevelopmentPilotIds.EdgePilotInjectionMachine2Id),
            (DevelopmentPilotIds.ShiftRiskPilotAssessmentStaleId,
                DevelopmentPilotIds.ShiftRiskPilotRequirementStaleId,
                "PILOT-PO-INJ-STALE",
                DevelopmentPilotIds.EdgePilotInjectionMachine2Id),
        };

        foreach (var scenario in scenarios)
        {
            var requirement = requirements.Single(r => r.Id == scenario.RequirementId);
            var evaluationContext = new ShiftRiskEvaluationContext(
                requirement.ProductionShiftId,
                requirement.ShiftDate,
                requirement.PlannedStartUtc,
                requirement.PlannedEndUtc,
                scenario.OrderRef,
                scenario.MachineId,
                requirement.MachineFamilyScope,
                requirement.RequiredCompetencyCode);

            var requirementSnapshot = new ShiftCoverageRiskEvaluator.RequirementSnapshot(
                requirement.Id,
                requirement.ProductionShiftId,
                requirement.ShiftDate,
                requirement.PlannedStartUtc,
                requirement.PlannedEndUtc,
                requirement.ProductionOrderReference,
                requirement.MachineAssetId,
                requirement.MachineFamilyScope,
                requirement.RequiredCompetencyCode,
                requirement.MinimumApprovedOperatorCount,
                requirement.IsCriticalRole);

            var scopedAssignments = assignmentEvidence;

            var evaluation = ShiftCoverageRiskEvaluator.Evaluate(
                requirementSnapshot,
                evaluationContext,
                scopedAssignments,
                personEvidence,
                seedTimestampUtc);

            var inputFingerprint = ShiftRiskIdempotencyFingerprint.ComputeEvaluationInputFingerprint(evaluationContext);
            var assessment = new ShiftRiskAssessment
            {
                Id = scenario.AssessmentId,
                TenantId = tenantId,
                FacilityId = facilityId,
                CoverageRequirementId = requirement.Id,
                ProductionShiftId = requirement.ProductionShiftId,
                ShiftDate = requirement.ShiftDate,
                PlannedStartUtc = requirement.PlannedStartUtc,
                PlannedEndUtc = requirement.PlannedEndUtc,
                ProductionOrderReference = scenario.OrderRef,
                MachineAssetId = scenario.MachineId,
                RequiredCompetencyCode = requirement.RequiredCompetencyCode,
                MinimumRequiredCount = requirement.MinimumApprovedOperatorCount,
                IsCriticalRole = requirement.IsCriticalRole,
                ApprovedEligibleCount = evaluation.ApprovedEligibleCount,
                CoverageGap = evaluation.CoverageGap,
                RuleSetVersion = ShiftRiskConstants.RuleSetVersion,
                InputFingerprint = inputFingerprint,
                Outcome = evaluation.Outcome,
                LeadingDriverCategory = evaluation.LeadingDriverCategory,
                LeadingReasonCode = evaluation.LeadingReasonCode,
                StructuredEvidenceJson = """{"pilot":true}""",
                SourceFreshnessSnapshotJson = """{"pilot":true}""",
                EvaluatedAtUtc = seedTimestampUtc,
                EvaluateIdempotencyKey = $"pilot-shift-risk-{scenario.AssessmentId:N}",
                EvaluateIdempotencyPayloadFingerprint = inputFingerprint,
                CreatedAtUtc = seedTimestampUtc,
                CreatedBy = actorSubjectId,
            };

            foreach (var driverDraft in evaluation.Drivers)
            {
                var driver = new ShiftRiskDriver
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    AssessmentId = assessment.Id,
                    ReasonCode = driverDraft.ReasonCode,
                    LabelTr = ShiftRiskReasonLabels.GetLabelTr(driverDraft.ReasonCode),
                    Category = driverDraft.Category,
                    Severity = driverDraft.Severity,
                    RequiredValue = driverDraft.RequiredValue,
                    ObservedValue = driverDraft.ObservedValue,
                    GapValue = driverDraft.GapValue,
                    ProductionShiftId = driverDraft.ProductionShiftId,
                    ProductionOrderReference = driverDraft.ProductionOrderReference,
                    MachineAssetId = driverDraft.MachineAssetId,
                    RequiredCompetencyCode = driverDraft.RequiredCompetencyCode,
                    EvidenceSource = driverDraft.EvidenceSource,
                    EvidenceReference = driverDraft.EvidenceReference,
                    EvidenceTimestampUtc = driverDraft.EvidenceTimestampUtc,
                    FreshnessState = driverDraft.FreshnessState,
                    RuleSetVersion = ShiftRiskConstants.RuleSetVersion,
                    IsLeadingRisk = driverDraft.ReasonCode == evaluation.LeadingReasonCode,
                };
                assessment.Drivers.Add(driver);
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
                PayloadJson = $"{{\"assessmentId\":\"{assessment.Id}\",\"outcome\":\"{assessment.Outcome}\"}}",
                ActorSubjectId = actorSubjectId,
                OccurredAtUtc = seedTimestampUtc,
            });
        }
    }

    private static async Task SeedStaleApprovedAssignmentAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateOnly shiftDate,
        DateTimeOffset plannedStartUtc,
        DateTimeOffset plannedEndUtc,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var staleAssignmentId = Guid.Parse("bb000001-0126-4000-8000-000000000126");
        var exists = await dbContext.ShiftOperatorAssignments.AsNoTracking()
            .AnyAsync(a => a.Id == staleAssignmentId, cancellationToken);

        if (exists)
        {
            return;
        }

        var assessment = await dbContext.ShiftOperatorEligibilityAssessments.AsNoTracking()
            .FirstAsync(a => a.Id == DevelopmentPilotIds.ShiftAssignPilotAssessmentId, cancellationToken);

        var staleResultId = Guid.Parse("bb000001-0127-4000-8000-000000000127");
        if (!await dbContext.ShiftOperatorEligibilityResults.AsNoTracking()
                .AnyAsync(r => r.Id == staleResultId, cancellationToken))
        {
            dbContext.ShiftOperatorEligibilityResults.Add(new ShiftOperatorEligibilityResult
            {
                Id = staleResultId,
                TenantId = tenantId,
                AssessmentId = assessment.Id,
                WorkforcePersonId = DevelopmentPilotIds.ShiftWorkforcePilotPerson003Id,
                Outcome = ShiftAssignmentEligibilityOutcomes.InsufficientData,
                PrimaryReasonCode = ShiftAssignmentReasonCodes.SourceStale,
                EmployeeCodeSnapshot = "PILOT-SWF-003",
                DisplayNameSnapshot = "Pilot Enjeksiyon Operatörü 02",
            });
        }

        dbContext.ShiftOperatorAssignments.Add(new ShiftOperatorAssignment
        {
            Id = staleAssignmentId,
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
            EligibilityResultId = staleResultId,
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
