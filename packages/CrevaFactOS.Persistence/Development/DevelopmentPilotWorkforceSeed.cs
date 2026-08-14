using CrevaFactOS.Persistence.ShiftWorkforce;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Development;

internal static class DevelopmentPilotWorkforceSeed
{
    internal static async Task EnsureShiftWorkforcePilotRecordsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        await EnsureCompetencyDefinitionsAsync(dbContext, tenantId, cancellationToken);
        await EnsurePilotPersonsAsync(
            dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);
        await EnsurePilotCompetencyAssignmentsAsync(
            dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);
    }

    private static async Task EnsureCompetencyDefinitionsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var definitions = new (Guid Id, string Code, string Label, string? Description, int SortOrder)[]
        {
            (DevelopmentPilotIds.ShiftWorkforceCompetencyInjectionId,
                ShiftWorkforceCompetencyCodes.InjectionMachineOperation,
                "Enjeksiyon makinesi operasyonu",
                "Enjeksiyon makinesi operasyon yetkinliği.",
                10),
            (DevelopmentPilotIds.ShiftWorkforceCompetencyMoldChangeId,
                ShiftWorkforceCompetencyCodes.MoldChangeSetup,
                "Kalıp değişimi ve kurulum",
                "Kalıp değişimi ve kurulum yetkinliği.",
                20),
            (DevelopmentPilotIds.ShiftWorkforceCompetencyProcessReviewId,
                ShiftWorkforceCompetencyCodes.ProcessParameterReview,
                "Proses parametre inceleme",
                "Proses parametre inceleme yetkinliği.",
                30),
            (DevelopmentPilotIds.ShiftWorkforceCompetencyMaterialHandlingId,
                ShiftWorkforceCompetencyCodes.MaterialHandling,
                "Malzeme taşıma",
                "Malzeme taşıma yetkinliği.",
                40),
            (DevelopmentPilotIds.ShiftWorkforceCompetencyRobotAuxId,
                ShiftWorkforceCompetencyCodes.RobotAuxiliaryEquipment,
                "Robot ve yardımcı ekipman",
                "Robot ve yardımcı ekipman yetkinliği.",
                50),
            (DevelopmentPilotIds.ShiftWorkforceCompetencyQualityCheckId,
                ShiftWorkforceCompetencyCodes.QualityCheck,
                "Kalite kontrol",
                "Kalite kontrol yetkinliği.",
                60),
            (DevelopmentPilotIds.ShiftWorkforceCompetencyMaintenanceSupportId,
                ShiftWorkforceCompetencyCodes.MaintenanceSupport,
                "Bakım desteği",
                "Bakım desteği yetkinliği.",
                70),
        };

        foreach (var definition in definitions)
        {
            var exists = await dbContext.ShiftWorkforceCompetencyDefinitions.AsNoTracking()
                .AnyAsync(d => d.Id == definition.Id, cancellationToken);

            if (exists)
            {
                continue;
            }

            dbContext.ShiftWorkforceCompetencyDefinitions.Add(new ShiftWorkforceCompetencyDefinition
            {
                Id = definition.Id,
                TenantId = tenantId,
                Code = definition.Code,
                DisplayLabelTr = definition.Label,
                DescriptionTr = definition.Description,
                IsActive = true,
                SortOrder = definition.SortOrder,
            });
        }
    }

    private static async Task EnsurePilotPersonsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var persons = new (Guid Id, string ExternalRef, string Code, string Name, string Dept, string Title, bool Active, string Availability, string? Shift, string Freshness, string Fingerprint, string Limitations)[]
        {
            (DevelopmentPilotIds.ShiftWorkforcePilotPerson001Id, "pilot-swf-001", "PILOT-SWF-001",
                "Pilot Vardiya Sorumlusu 01", "Üretim", "Vardiya Sorumlusu", true,
                ShiftWorkforceAvailabilityStates.Available, "SHIFT-A",
                ShiftWorkforceFreshnessStates.Fresh, "pilot-swf-001-seed-v1",
                """["Pilot veri — gerçek ERP kaydı değildir."]"""),
            (DevelopmentPilotIds.ShiftWorkforcePilotPerson002Id, "pilot-swf-002", "PILOT-SWF-002",
                "Pilot Enjeksiyon Operatörü 01", "Enjeksiyon", "Operatör", true,
                ShiftWorkforceAvailabilityStates.Available, "V1",
                ShiftWorkforceFreshnessStates.Fresh, "pilot-swf-002-seed-v1", "[]"),
            (DevelopmentPilotIds.ShiftWorkforcePilotPerson003Id, "pilot-swf-003", "PILOT-SWF-003",
                "Pilot Enjeksiyon Operatörü 02", "Enjeksiyon", "Operatör", true,
                ShiftWorkforceAvailabilityStates.Leave, "V1",
                ShiftWorkforceFreshnessStates.Stale, "pilot-swf-003-seed-v1", "[]"),
            (DevelopmentPilotIds.ShiftWorkforcePilotPerson004Id, "pilot-swf-004", "PILOT-SWF-004",
                "Pilot Kalıp Teknisyeni 01", "Kalıphane", "Kalıp Teknisyeni", true,
                ShiftWorkforceAvailabilityStates.Available, "SHIFT-A",
                ShiftWorkforceFreshnessStates.Fresh, "pilot-swf-004-seed-v1", "[]"),
            (DevelopmentPilotIds.ShiftWorkforcePilotPerson005Id, "pilot-swf-005", "PILOT-SWF-005",
                "Pilot Kalite Operatörü 01", "Kalite", "Kalite Operatörü", true,
                ShiftWorkforceAvailabilityStates.Available, "SHIFT-C",
                ShiftWorkforceFreshnessStates.Fresh, "pilot-swf-005-seed-v1",
                """["Bazı yetkinlik doğrulamaları eksik."]"""),
            (DevelopmentPilotIds.ShiftWorkforcePilotPerson006Id, "pilot-swf-006", "PILOT-SWF-006",
                "Pilot Bakım Teknisyeni 01", "Bakım", "Bakım Teknisyeni", false,
                ShiftWorkforceAvailabilityStates.Unknown, null,
                ShiftWorkforceFreshnessStates.Fresh, "pilot-swf-006-seed-v1", "[]"),
        };

        foreach (var person in persons)
        {
            var exists = await dbContext.ShiftWorkforcePersons.AsNoTracking()
                .AnyAsync(p => p.Id == person.Id, cancellationToken);

            if (exists)
            {
                continue;
            }

            dbContext.ShiftWorkforcePersons.Add(new ShiftWorkforcePerson
            {
                Id = person.Id,
                TenantId = tenantId,
                FacilityId = facilityId,
                SourceSystem = ShiftWorkforceSourceSystems.LocalDevelopmentPilot,
                ExternalEmployeeRef = person.ExternalRef,
                EmployeeCode = person.Code,
                EmployeeCodeNormalized = person.Code.ToUpperInvariant(),
                DisplayName = person.Name,
                Department = person.Dept,
                JobTitle = person.Title,
                IsActive = person.Active,
                AvailabilityState = person.Availability,
                SourceShiftReference = person.Shift,
                SourceCalendarReference = "CAL-2026-W33",
                SourceUpdatedAtUtc = seedTimestampUtc,
                SynchronizedAtUtc = seedTimestampUtc,
                SourceFingerprint = person.Fingerprint,
                FreshnessState = person.Freshness,
                SourceLimitationsJson = person.Limitations,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                Version = 1,
            });

            _ = actorSubjectId;
        }
    }

    private static async Task EnsurePilotCompetencyAssignmentsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        await EnsureAssignmentAsync(
            dbContext, tenantId, facilityId,
            DevelopmentPilotIds.ShiftWorkforcePilotAssignmentInjectionId,
            DevelopmentPilotIds.ShiftWorkforcePilotPerson002Id,
            DevelopmentPilotIds.ShiftWorkforceCompetencyInjectionId,
            ShiftWorkforceCompetencyLevels.Competent,
            ShiftWorkforceVerificationStatuses.Verified,
            seedTimestampUtc.AddMonths(-6),
            seedTimestampUtc.AddMonths(12),
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureAssignmentAsync(
            dbContext, tenantId, facilityId,
            DevelopmentPilotIds.ShiftWorkforcePilotAssignmentMoldChangeId,
            DevelopmentPilotIds.ShiftWorkforcePilotPerson004Id,
            DevelopmentPilotIds.ShiftWorkforceCompetencyMoldChangeId,
            ShiftWorkforceCompetencyLevels.Proficient,
            ShiftWorkforceVerificationStatuses.Verified,
            seedTimestampUtc.AddMonths(-3),
            seedTimestampUtc.AddDays(14),
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureAssignmentAsync(
            dbContext, tenantId, facilityId,
            DevelopmentPilotIds.ShiftWorkforcePilotAssignmentQualityPartialId,
            DevelopmentPilotIds.ShiftWorkforcePilotPerson005Id,
            DevelopmentPilotIds.ShiftWorkforceCompetencyQualityCheckId,
            ShiftWorkforceCompetencyLevels.Basic,
            ShiftWorkforceVerificationStatuses.PendingVerification,
            seedTimestampUtc.AddMonths(-1),
            null,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);
    }

    private static async Task EnsureAssignmentAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        Guid assignmentId,
        Guid personId,
        Guid definitionId,
        string level,
        string verificationStatus,
        DateTimeOffset validFrom,
        DateTimeOffset? validUntil,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.ShiftWorkforceCompetencyAssignments.AsNoTracking()
            .AnyAsync(a => a.Id == assignmentId, cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.ShiftWorkforceCompetencyAssignments.Add(new ShiftWorkforceCompetencyAssignment
        {
            Id = assignmentId,
            TenantId = tenantId,
            FacilityId = facilityId,
            WorkforcePersonId = personId,
            CompetencyDefinitionId = definitionId,
            CompetencyLevel = level,
            VerificationStatus = verificationStatus,
            ValidFromUtc = validFrom,
            ValidUntilUtc = validUntil,
            VerifiedBySubjectId = verificationStatus == ShiftWorkforceVerificationStatuses.Verified
                ? actorSubjectId
                : null,
            VerifiedAtUtc = verificationStatus == ShiftWorkforceVerificationStatuses.Verified
                ? seedTimestampUtc
                : null,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
            UpdatedBy = actorSubjectId,
            Version = 1,
        });
    }
}
