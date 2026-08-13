using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Export;
using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Identity;
using CrevaFactOS.Persistence.Production;
using CrevaFactOS.Persistence.ProductionPlanning;
using CrevaFactOS.Persistence.Tenancy;
using CrevaFactOS.Persistence.Quality;
using CrevaFactOS.Persistence.ShiftWorkforce;
using CrevaFactOS.Persistence.ShiftAssignment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace CrevaFactOS.Persistence.Development;

internal sealed class DevelopmentPilotBootstrapService(
    IOptions<DevelopmentPilotOptions> options,
    IOptions<PersistenceOptions> persistenceOptions)
    : IDevelopmentPilotBootstrapService
{
    public async Task<DevelopmentPilotBootstrapResult> BootstrapAsync(
        CancellationToken cancellationToken = default)
    {
        var pilotOptions = options.Value;

        if (!pilotOptions.Enabled)
        {
            return new DevelopmentPilotBootstrapResult(
                Executed: false,
                AlreadyInitialized: false,
                DevelopmentPilotIds.TenantId,
                DevelopmentPilotIds.FacilityId,
                DevelopmentPilotIds.TenantMembershipId,
                ProductionPlanCount: 0);
        }

        var bootstrapContext =
            DevelopmentPilotBootstrapDbContextFactory.Create(persistenceOptions.Value);
        await using var dbContext = bootstrapContext.DbContext;

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var seedTimestampUtc = DateTimeOffset.UtcNow;
        var tenant = await EnsureTenantAsync(
            dbContext,
            pilotOptions,
            seedTimestampUtc,
            cancellationToken);

        bootstrapContext.TenantContextAccessor.Set(
            new TenantContext(
                tenant.Id,
                tenant.TenantKey,
                DevelopmentPilotIds.TenantMembershipId));
        var identitySubject = await EnsureIdentitySubjectAsync(
            dbContext,
            pilotOptions,
            seedTimestampUtc,
            cancellationToken);
        var tenantMembership = await EnsureTenantMembershipAsync(
            dbContext,
            tenant.Id,
            identitySubject.Id,
            seedTimestampUtc,
            cancellationToken);
        var tenantRole = await EnsureTenantRoleAsync(
            dbContext,
            tenant.Id,
            pilotOptions,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningReadPermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            tenantRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningWritePermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            tenantRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningReviewPermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            tenantRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningApprovePermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            tenantRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningSchedulePermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            tenantRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningTransferPermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            tenantRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningHandoffPermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            tenantRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsureExportReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportWritePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportDocumentManagePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportDocumentVerifyPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportPackingManagePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportPackingVerifyPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportShipmentManagePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportBrokerManagePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportBrokerHandoffPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportComplianceReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportComplianceManagePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportComplianceVerifyPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportCountryResearchReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportCountryResearchManagePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportCountryResearchVerifyPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportAiReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportAiRunPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportAiReviewPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureMembershipRoleAsync(
            dbContext,
            tenant.Id,
            tenantMembership.Id,
            tenantRole.Id,
            seedTimestampUtc,
            cancellationToken);
        var facility = await EnsureFacilityAsync(
            dbContext,
            tenant.Id,
            pilotOptions,
            seedTimestampUtc,
            cancellationToken);
        await EnsureUnauthorizedFacilityAsync(
            dbContext,
            tenant.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsureFacilityMembershipAsync(
            dbContext,
            tenant.Id,
            facility.Id,
            tenantMembership.Id,
            seedTimestampUtc,
            cancellationToken);

        // Second identity: planning-pilot-reviewer (approve/review role for maker-checker)
        var reviewerIdentity = await EnsureReviewerIdentitySubjectAsync(
            dbContext,
            pilotOptions,
            seedTimestampUtc,
            cancellationToken);
        var reviewerMembership = await EnsureReviewerTenantMembershipAsync(
            dbContext,
            tenant.Id,
            reviewerIdentity.Id,
            seedTimestampUtc,
            cancellationToken);
        var reviewerRole = await EnsureReviewerTenantRoleAsync(
            dbContext,
            tenant.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningReadPermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            reviewerRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningReviewPermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            reviewerRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningApprovePermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            reviewerRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningSchedulePermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            reviewerRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningTransferPermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            reviewerRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningHandoffPermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            reviewerRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsureExportAiReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportAiReviewPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureMembershipRoleAsync(
            dbContext,
            tenant.Id,
            reviewerMembership.Id,
            reviewerRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsureReviewerFacilityMembershipAsync(
            dbContext,
            tenant.Id,
            facility.Id,
            reviewerMembership.Id,
            seedTimestampUtc,
            cancellationToken);

        // Give planner and reviewer production execution read
        await EnsureExecutionReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExecutionReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);

        // Third identity: production-pilot-operator (full execution permissions)
        var operatorIdentity = await EnsureOperatorIdentitySubjectAsync(
            dbContext, pilotOptions, seedTimestampUtc, cancellationToken);
        var operatorMembership = await EnsureOperatorTenantMembershipAsync(
            dbContext, tenant.Id, operatorIdentity.Id, seedTimestampUtc, cancellationToken);
        var operatorRole = await EnsureOperatorTenantRoleAsync(
            dbContext, tenant.Id, seedTimestampUtc, cancellationToken);
        await EnsureExecutionReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExecutionWritePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExecutionStartPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExecutionPausePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExecutionCompletePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExecutionRecordPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExecutionDowntimePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExecutionSetupPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExecutionExceptionPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        // 5L: assign (supervisor-level), handover, exception.assign
        await EnsureExecutionAssignPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExecutionHandoverPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExecutionExceptionAssignPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        // Shared local web identity (production-pilot-operator) also drives /ihracat.
        await EnsurePlanningReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportWritePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportDocumentManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportDocumentVerifyPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportPackingManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportPackingVerifyPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportShipmentManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportBrokerManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportBrokerHandoffPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportComplianceReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportComplianceManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportComplianceVerifyPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportCountryResearchReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportCountryResearchManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportCountryResearchVerifyPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportAiReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportAiRunPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExportAiReviewPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureQualityReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureQualityReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureQualityReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureQualityManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureQualityInspectPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureQualityReleasePermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureQualityNcrManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureQualityHoldPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureQualityHoldPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureQualityDispositionPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureMaintenanceReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureMaintenanceReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureMaintenanceReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureMaintenanceManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureMaintenanceExecutePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureErpReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureErpReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureErpReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureErpManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureErpSyncPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureErpWritebackRequestPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureErpWritebackRequestPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureErpWritebackApprovePermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureErpWritebackApprovePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureErpWritebackExecutePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureEdgeReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureEdgeManagePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureEdgeIngestPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureEdgeReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureEdgeReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureEdgeManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureEdgeIngestPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureMoldReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureMoldReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureMoldReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureMoldManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureMaterialReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureMaterialReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureMaterialReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureMaterialManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureProcessOeeReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureProcessOeeReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureProcessOeeReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureProcessOeeManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureShiftWorkforceReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureShiftWorkforceReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureShiftWorkforceReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureShiftWorkforceManagePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureShiftAssignReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureShiftAssignReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureShiftAssignReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureShiftAssignManagePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsurePilotProductionShiftsAsync(
            dbContext, tenant.Id, DevelopmentPilotIds.FacilityId, seedTimestampUtc, cancellationToken);
        await EnsureExportPilotCaseAsync(
            dbContext, tenant.Id, DevelopmentPilotIds.FacilityId, seedTimestampUtc, cancellationToken);
        await EnsureExportPilotShipmentAsync(
            dbContext, tenant.Id, DevelopmentPilotIds.FacilityId, seedTimestampUtc, cancellationToken);
        await EnsureExportDeDemoRulePackAsync(
            dbContext, tenant.Id, seedTimestampUtc, cancellationToken);
        await EnsureMembershipRoleAsync(
            dbContext, tenant.Id, operatorMembership.Id, operatorRole.Id,
            seedTimestampUtc, cancellationToken);
        await EnsureOperatorFacilityMembershipAsync(
            dbContext, tenant.Id, facility.Id, operatorMembership.Id, seedTimestampUtc, cancellationToken);

        // Development-only authz negative-test identity: facility member without quality.read.
        var qualityNegativeTestIdentity = await EnsureQualityNegativeTestIdentitySubjectAsync(
            dbContext, pilotOptions, seedTimestampUtc, cancellationToken);
        var qualityNegativeTestMembership = await EnsureQualityNegativeTestTenantMembershipAsync(
            dbContext, tenant.Id, qualityNegativeTestIdentity.Id, seedTimestampUtc, cancellationToken);
        var qualityNegativeTestRole = await EnsureQualityNegativeTestTenantRoleAsync(
            dbContext, tenant.Id, seedTimestampUtc, cancellationToken);
        await EnsureMembershipRoleAsync(
            dbContext,
            tenant.Id,
            qualityNegativeTestMembership.Id,
            qualityNegativeTestRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsureQualityNegativeTestFacilityMembershipAsync(
            dbContext,
            tenant.Id,
            facility.Id,
            qualityNegativeTestMembership.Id,
            seedTimestampUtc,
            cancellationToken);

        var productionPlanCount = 0;
        if (pilotOptions.ProductionPlanSeedEnabled)
        {
            productionPlanCount = await EnsureProductionPlansAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                pilotOptions.DevelopmentSubject,
                seedTimestampUtc,
                cancellationToken);
            await EnsurePlanningLogisticsPilotLinkageAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                cancellationToken);
            await DevelopmentPilotQualitySeed.EnsureQualityPilotControlPlanAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                operatorIdentity.Id.ToString(),
                seedTimestampUtc,
                cancellationToken);
            await DevelopmentPilotMaintenanceSeed.EnsureMaintenancePilotDataAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                operatorIdentity.Id.ToString(),
                seedTimestampUtc,
                cancellationToken);
            await DevelopmentPilotMaintenanceReliabilitySeed.EnsureMaintenanceReliabilityPilotDataAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                operatorIdentity.Id.ToString(),
                seedTimestampUtc,
                cancellationToken);
            await DevelopmentPilotErpSeed.EnsureErpSimulatorConnectionAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                seedTimestampUtc,
                cancellationToken);
            await DevelopmentPilotErpSeed.EnsureErpLogoTestConnectionAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                seedTimestampUtc,
                cancellationToken);
            await DevelopmentPilotErpSeed.EnsureErpMikroTestConnectionAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                seedTimestampUtc,
                cancellationToken);
            await DevelopmentPilotErpSeed.EnsureErpNetsisTestConnectionAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                seedTimestampUtc,
                cancellationToken);
            await DevelopmentPilotEdgeSeed.EnsureEdgeSimulatorGatewayAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                seedTimestampUtc,
                cancellationToken);
            await DevelopmentPilotMoldCompatibilitySeed.EnsureMoldCompatibilityPilotProfilesAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                seedTimestampUtc,
                cancellationToken);
            await DevelopmentPilotMaterialSeed.EnsureMaterialPilotRecordsAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                operatorIdentity.Id.ToString(),
                seedTimestampUtc,
                cancellationToken);
            await DevelopmentPilotMaterialLotSeed.EnsureMaterialLotPilotRecordsAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                operatorIdentity.Id.ToString(),
                seedTimestampUtc,
                cancellationToken);
            await DevelopmentPilotMaterialRequirementSeed.EnsureMaterialRequirementPilotRecordsAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                operatorIdentity.Id.ToString(),
                seedTimestampUtc,
                cancellationToken);
            await DevelopmentPilotProcessOeeFixtureHygiene.CleanAttributableVerifierFixturesAsync(
                dbContext,
                cancellationToken);
            await DevelopmentPilotProcessOeeSeed.EnsureProcessOeePilotRecordsAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                operatorIdentity.Id.ToString(),
                seedTimestampUtc,
                cancellationToken);
            await DevelopmentPilotWorkforceSeed.EnsureShiftWorkforcePilotRecordsAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                operatorIdentity.Id.ToString(),
                seedTimestampUtc,
                cancellationToken);
            await DevelopmentPilotShiftAssignmentSeed.EnsureShiftAssignmentPilotRecordsAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                operatorIdentity.Id.ToString(),
                seedTimestampUtc,
                cancellationToken);
            await DevelopmentPilotMaintenanceConditionSeed.EnsureMaintenanceConditionPilotDataAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                seedTimestampUtc,
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new DevelopmentPilotBootstrapResult(
            Executed: true,
            AlreadyInitialized: false,
            tenant.Id,
            facility.Id,
            tenantMembership.Id,
            productionPlanCount);
    }

    private static async Task<Tenant> EnsureTenantAsync(
        CrevaFactOSDbContext dbContext,
        DevelopmentPilotOptions pilotOptions,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existingTenant = await dbContext.Tenants
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                tenant => tenant.Id == DevelopmentPilotIds.TenantId
                    || tenant.TenantKey == pilotOptions.TenantKey,
                cancellationToken);

        if (existingTenant is not null)
        {
            return existingTenant;
        }

        var tenant = new Tenant
        {
            Id = DevelopmentPilotIds.TenantId,
            TenantKey = pilotOptions.TenantKey.Trim(),
            DisplayName = pilotOptions.TenantName.Trim(),
            Status = TenantStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.Tenants.Add(tenant);
        return tenant;
    }

    private static async Task<IdentitySubject> EnsureIdentitySubjectAsync(
        CrevaFactOSDbContext dbContext,
        DevelopmentPilotOptions pilotOptions,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var issuer = pilotOptions.DevelopmentIssuer.Trim();
        var subject = pilotOptions.DevelopmentSubject.Trim();

        var existingIdentity = await dbContext.IdentitySubjects
            .SingleOrDefaultAsync(
                identity =>
                    identity.ExternalIssuer == issuer
                    && identity.ExternalSubject == subject,
                cancellationToken);

        if (existingIdentity is not null)
        {
            return existingIdentity;
        }

        var identitySubject = new IdentitySubject
        {
            Id = DevelopmentPilotIds.IdentitySubjectId,
            ExternalIssuer = issuer,
            ExternalSubject = subject,
            Status = IdentitySubjectStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.IdentitySubjects.Add(identitySubject);
        return identitySubject;
    }

    private static async Task<TenantMembership> EnsureTenantMembershipAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid identitySubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existingMembership = await dbContext.TenantMemberships
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                membership =>
                    membership.TenantId == tenantId
                    && membership.IdentitySubjectId == identitySubjectId,
                cancellationToken);

        if (existingMembership is not null)
        {
            return existingMembership;
        }

        var membership = new TenantMembership
        {
            Id = DevelopmentPilotIds.TenantMembershipId,
            TenantId = tenantId,
            IdentitySubjectId = identitySubjectId,
            Status = TenantMembershipStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.TenantMemberships.Add(membership);
        return membership;
    }

    private static async Task<TenantRole> EnsureTenantRoleAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        DevelopmentPilotOptions pilotOptions,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var roleKey = pilotOptions.RoleKey.Trim();

        var existingRole = await dbContext.TenantRoles
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                role => role.TenantId == tenantId && role.Key == roleKey,
                cancellationToken);

        if (existingRole is not null)
        {
            return existingRole;
        }

        var tenantRole = new TenantRole
        {
            Id = DevelopmentPilotIds.TenantRoleId,
            TenantId = tenantId,
            Key = roleKey,
            DisplayName = pilotOptions.RoleDisplayName.Trim(),
            Status = AuthorizationStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.TenantRoles.Add(tenantRole);
        return tenantRole;
    }

    private static async Task EnsurePlanningReadPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var permissionDefinition = await dbContext.PermissionDefinitions
            .SingleOrDefaultAsync(
                permission =>
                    permission.Key == ProductionPlanningPermissionKeys.Read,
                cancellationToken)
            ?? await dbContext.PermissionDefinitions
                .SingleOrDefaultAsync(
                    permission =>
                        permission.Id
                        == DevelopmentPilotIds.PlanningReadPermissionDefinitionId,
                    cancellationToken);

        if (permissionDefinition is null)
        {
            throw new InvalidOperationException(
                "production.planning.read permission definition is missing.");
        }

        var existingAssignment = await dbContext.TenantRolePermissions
            .IgnoreQueryFilters()
            .AnyAsync(
                assignment =>
                    assignment.TenantId == tenantId
                    && assignment.TenantRoleId == tenantRoleId
                    && assignment.PermissionDefinitionId == permissionDefinition.Id,
                cancellationToken);

        if (existingAssignment)
        {
            return;
        }

        dbContext.TenantRolePermissions.Add(new TenantRolePermissionAssignment
        {
            TenantId = tenantId,
            TenantRoleId = tenantRoleId,
            PermissionDefinitionId = permissionDefinition.Id,
        });
    }

    private static async Task EnsurePlanningWritePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var permissionDefinition = await dbContext.PermissionDefinitions
            .SingleOrDefaultAsync(
                permission =>
                    permission.Key == ProductionPlanningPermissionKeys.Write,
                cancellationToken)
            ?? await dbContext.PermissionDefinitions
                .SingleOrDefaultAsync(
                    permission =>
                        permission.Id
                        == DevelopmentPilotIds.PlanningWritePermissionDefinitionId,
                    cancellationToken);

        if (permissionDefinition is null)
        {
            return;
        }

        var existingAssignment = await dbContext.TenantRolePermissions
            .IgnoreQueryFilters()
            .AnyAsync(
                assignment =>
                    assignment.TenantId == tenantId
                    && assignment.TenantRoleId == tenantRoleId
                    && assignment.PermissionDefinitionId == permissionDefinition.Id,
                cancellationToken);

        if (existingAssignment)
        {
            return;
        }

        dbContext.TenantRolePermissions.Add(new TenantRolePermissionAssignment
        {
            TenantId = tenantId,
            TenantRoleId = tenantRoleId,
            PermissionDefinitionId = permissionDefinition.Id,
        });
    }

    private static async Task EnsurePlanningReviewPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var permissionDefinition = await dbContext.PermissionDefinitions
            .SingleOrDefaultAsync(
                permission =>
                    permission.Key == ProductionPlanningPermissionKeys.Review,
                cancellationToken)
            ?? await dbContext.PermissionDefinitions
                .SingleOrDefaultAsync(
                    permission =>
                        permission.Id == DevelopmentPilotIds.PlanningReviewPermissionDefinitionId,
                    cancellationToken);

        if (permissionDefinition is null)
        {
            return;
        }

        var existingAssignment = await dbContext.TenantRolePermissions
            .IgnoreQueryFilters()
            .AnyAsync(
                assignment =>
                    assignment.TenantId == tenantId
                    && assignment.TenantRoleId == tenantRoleId
                    && assignment.PermissionDefinitionId == permissionDefinition.Id,
                cancellationToken);

        if (existingAssignment)
        {
            return;
        }

        dbContext.TenantRolePermissions.Add(new TenantRolePermissionAssignment
        {
            TenantId = tenantId,
            TenantRoleId = tenantRoleId,
            PermissionDefinitionId = permissionDefinition.Id,
        });
    }

    private static async Task EnsurePlanningApprovePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var permissionDefinition = await dbContext.PermissionDefinitions
            .SingleOrDefaultAsync(
                permission =>
                    permission.Key == ProductionPlanningPermissionKeys.Approve,
                cancellationToken)
            ?? await dbContext.PermissionDefinitions
                .SingleOrDefaultAsync(
                    permission =>
                        permission.Id == DevelopmentPilotIds.PlanningApprovePermissionDefinitionId,
                    cancellationToken);

        if (permissionDefinition is null)
        {
            return;
        }

        var existingAssignment = await dbContext.TenantRolePermissions
            .IgnoreQueryFilters()
            .AnyAsync(
                assignment =>
                    assignment.TenantId == tenantId
                    && assignment.TenantRoleId == tenantRoleId
                    && assignment.PermissionDefinitionId == permissionDefinition.Id,
                cancellationToken);

        if (existingAssignment)
        {
            return;
        }

        dbContext.TenantRolePermissions.Add(new TenantRolePermissionAssignment
        {
            TenantId = tenantId,
            TenantRoleId = tenantRoleId,
            PermissionDefinitionId = permissionDefinition.Id,
        });
    }

    private static async Task EnsurePlanningSchedulePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var permissionDefinition = await dbContext.PermissionDefinitions
            .SingleOrDefaultAsync(
                permission =>
                    permission.Key == ProductionPlanningPermissionKeys.Schedule,
                cancellationToken)
            ?? await dbContext.PermissionDefinitions
                .SingleOrDefaultAsync(
                    permission =>
                        permission.Id == DevelopmentPilotIds.PlanningSchedulePermissionDefinitionId,
                    cancellationToken);

        if (permissionDefinition is null)
        {
            return;
        }

        var existingAssignment = await dbContext.TenantRolePermissions
            .IgnoreQueryFilters()
            .AnyAsync(
                assignment =>
                    assignment.TenantId == tenantId
                    && assignment.TenantRoleId == tenantRoleId
                    && assignment.PermissionDefinitionId == permissionDefinition.Id,
                cancellationToken);

        if (existingAssignment)
        {
            return;
        }

        dbContext.TenantRolePermissions.Add(new TenantRolePermissionAssignment
        {
            TenantId = tenantId,
            TenantRoleId = tenantRoleId,
            PermissionDefinitionId = permissionDefinition.Id,
        });
    }

    private static async Task EnsurePlanningTransferPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var permissionDefinition = await dbContext.PermissionDefinitions
            .SingleOrDefaultAsync(
                permission =>
                    permission.Key == ProductionPlanningPermissionKeys.Transfer,
                cancellationToken)
            ?? await dbContext.PermissionDefinitions
                .SingleOrDefaultAsync(
                    permission =>
                        permission.Id == DevelopmentPilotIds.PlanningTransferPermissionDefinitionId,
                    cancellationToken);

        if (permissionDefinition is null)
        {
            return;
        }

        var existingAssignment = await dbContext.TenantRolePermissions
            .IgnoreQueryFilters()
            .AnyAsync(
                assignment =>
                    assignment.TenantId == tenantId
                    && assignment.TenantRoleId == tenantRoleId
                    && assignment.PermissionDefinitionId == permissionDefinition.Id,
                cancellationToken);

        if (existingAssignment)
        {
            return;
        }

        dbContext.TenantRolePermissions.Add(new TenantRolePermissionAssignment
        {
            TenantId = tenantId,
            TenantRoleId = tenantRoleId,
            PermissionDefinitionId = permissionDefinition.Id,
        });
    }

    private static async Task EnsureMembershipRoleAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid tenantMembershipId,
        Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existingAssignment = await dbContext.TenantMembershipRoles
            .IgnoreQueryFilters()
            .AnyAsync(
                assignment =>
                    assignment.TenantId == tenantId
                    && assignment.TenantMembershipId == tenantMembershipId
                    && assignment.TenantRoleId == tenantRoleId,
                cancellationToken);

        if (existingAssignment)
        {
            return;
        }

        dbContext.TenantMembershipRoles.Add(new TenantMembershipRole
        {
            TenantId = tenantId,
            TenantMembershipId = tenantMembershipId,
            TenantRoleId = tenantRoleId,
        });
    }

    private static async Task<Facility> EnsureFacilityAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        DevelopmentPilotOptions pilotOptions,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var facilityCode = pilotOptions.FacilityCode.Trim();

        var existingFacility = await dbContext.Facilities
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                facility =>
                    facility.TenantId == tenantId
                    && (facility.Id == DevelopmentPilotIds.FacilityId
                        || facility.Code == facilityCode),
                cancellationToken);

        if (existingFacility is not null)
        {
            return existingFacility;
        }

        var facility = new Facility
        {
            Id = DevelopmentPilotIds.FacilityId,
            TenantId = tenantId,
            Code = facilityCode,
            Name = pilotOptions.FacilityName.Trim(),
            Status = FacilityStatus.Active,
            TimeZoneId = pilotOptions.FacilityTimeZoneId.Trim(),
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            Version = 1,
        };

        dbContext.Facilities.Add(facility);
        return facility;
    }

    private static async Task EnsureUnauthorizedFacilityAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existingFacility = await dbContext.Facilities
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                facility => facility.Id == DevelopmentPilotIds.UnauthorizedFacilityId,
                cancellationToken);

        if (existingFacility is not null)
        {
            return;
        }

        dbContext.Facilities.Add(new Facility
        {
            Id = DevelopmentPilotIds.UnauthorizedFacilityId,
            TenantId = tenantId,
            Code = "pilot-facility-unauthorized",
            Name = "Pilot Unauthorized Facility",
            Status = FacilityStatus.Active,
            TimeZoneId = "UTC",
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            Version = 1,
        });
    }

    private static async Task EnsureFacilityMembershipAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        Guid tenantMembershipId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existingMembership = await dbContext.FacilityMemberships
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                membership =>
                    membership.TenantId == tenantId
                    && membership.FacilityId == facilityId
                    && membership.TenantMembershipId == tenantMembershipId,
                cancellationToken);

        if (existingMembership is not null)
        {
            return;
        }

        dbContext.FacilityMemberships.Add(new FacilityMembership
        {
            Id = DevelopmentPilotIds.FacilityMembershipId,
            TenantId = tenantId,
            FacilityId = facilityId,
            TenantMembershipId = tenantMembershipId,
            Status = FacilityMembershipStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            Version = 1,
        });
    }

    private static async Task EnsurePlanningHandoffPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var permissionDefinition = await dbContext.PermissionDefinitions
            .SingleOrDefaultAsync(
                permission =>
                    permission.Key == ProductionPlanningPermissionKeys.Handoff,
                cancellationToken)
            ?? await dbContext.PermissionDefinitions
                .SingleOrDefaultAsync(
                    permission =>
                        permission.Id == DevelopmentPilotIds.PlanningHandoffPermissionDefinitionId,
                    cancellationToken);

        if (permissionDefinition is null)
        {
            return;
        }

        var existingAssignment = await dbContext.TenantRolePermissions
            .IgnoreQueryFilters()
            .AnyAsync(
                assignment =>
                    assignment.TenantId == tenantId
                    && assignment.TenantRoleId == tenantRoleId
                    && assignment.PermissionDefinitionId == permissionDefinition.Id,
                cancellationToken);

        if (existingAssignment)
        {
            return;
        }

        dbContext.TenantRolePermissions.Add(new TenantRolePermissionAssignment
        {
            TenantId = tenantId,
            TenantRoleId = tenantRoleId,
            PermissionDefinitionId = permissionDefinition.Id,
        });
    }

    private static async Task<IdentitySubject> EnsureReviewerIdentitySubjectAsync(
        CrevaFactOSDbContext dbContext,
        DevelopmentPilotOptions pilotOptions,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        const string reviewerSubject = "planning-pilot-reviewer";
        var issuer = pilotOptions.DevelopmentIssuer.Trim();

        var existingIdentity = await dbContext.IdentitySubjects
            .SingleOrDefaultAsync(
                identity =>
                    identity.ExternalIssuer == issuer
                    && identity.ExternalSubject == reviewerSubject,
                cancellationToken)
            ?? await dbContext.IdentitySubjects
                .SingleOrDefaultAsync(
                    identity => identity.Id == DevelopmentPilotIds.ReviewerIdentitySubjectId,
                    cancellationToken);

        if (existingIdentity is not null)
        {
            return existingIdentity;
        }

        var identitySubject = new IdentitySubject
        {
            Id = DevelopmentPilotIds.ReviewerIdentitySubjectId,
            ExternalIssuer = issuer,
            ExternalSubject = reviewerSubject,
            Status = IdentitySubjectStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.IdentitySubjects.Add(identitySubject);
        return identitySubject;
    }

    private static async Task<TenantMembership> EnsureReviewerTenantMembershipAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid identitySubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existingMembership = await dbContext.TenantMemberships
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                membership =>
                    membership.TenantId == tenantId
                    && membership.IdentitySubjectId == identitySubjectId,
                cancellationToken)
            ?? await dbContext.TenantMemberships
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(
                    membership =>
                        membership.TenantId == tenantId
                        && membership.Id == DevelopmentPilotIds.ReviewerTenantMembershipId,
                    cancellationToken);

        if (existingMembership is not null)
        {
            return existingMembership;
        }

        var membership = new TenantMembership
        {
            Id = DevelopmentPilotIds.ReviewerTenantMembershipId,
            TenantId = tenantId,
            IdentitySubjectId = identitySubjectId,
            Status = TenantMembershipStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.TenantMemberships.Add(membership);
        return membership;
    }

    private static async Task<TenantRole> EnsureReviewerTenantRoleAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        const string reviewerRoleKey = "production-planning-reviewer";

        var existingRole = await dbContext.TenantRoles
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                role =>
                    role.TenantId == tenantId
                    && (role.Key == reviewerRoleKey
                        || role.Id == DevelopmentPilotIds.ReviewerTenantRoleId),
                cancellationToken);

        if (existingRole is not null)
        {
            return existingRole;
        }

        var tenantRole = new TenantRole
        {
            Id = DevelopmentPilotIds.ReviewerTenantRoleId,
            TenantId = tenantId,
            Key = reviewerRoleKey,
            DisplayName = "Production Planning Reviewer",
            Status = AuthorizationStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.TenantRoles.Add(tenantRole);
        return tenantRole;
    }

    private static async Task EnsureReviewerFacilityMembershipAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        Guid tenantMembershipId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existingMembership = await dbContext.FacilityMemberships
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                membership =>
                    membership.TenantId == tenantId
                    && membership.FacilityId == facilityId
                    && membership.TenantMembershipId == tenantMembershipId,
                cancellationToken);

        if (existingMembership is not null)
        {
            return;
        }

        dbContext.FacilityMemberships.Add(new FacilityMembership
        {
            Id = DevelopmentPilotIds.ReviewerFacilityMembershipId,
            TenantId = tenantId,
            FacilityId = facilityId,
            TenantMembershipId = tenantMembershipId,
            Status = FacilityMembershipStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            Version = 1,
        });
    }

    private static Task EnsureExecutionReadPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Read,
            DevelopmentPilotIds.ExecutionReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionWritePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Write,
            DevelopmentPilotIds.ExecutionWritePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionStartPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Start,
            DevelopmentPilotIds.ExecutionStartPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionPausePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Pause,
            DevelopmentPilotIds.ExecutionPausePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionCompletePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Complete,
            DevelopmentPilotIds.ExecutionCompletePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionRecordPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Record,
            DevelopmentPilotIds.ExecutionRecordPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionDowntimePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Downtime,
            DevelopmentPilotIds.ExecutionDowntimePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionSetupPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Setup,
            DevelopmentPilotIds.ExecutionSetupPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionExceptionPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Exception,
            DevelopmentPilotIds.ExecutionExceptionPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionAssignPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Assign,
            DevelopmentPilotIds.ExecutionAssignPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionHandoverPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Handover,
            DevelopmentPilotIds.ExecutionHandoverPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionExceptionAssignPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.ExceptionAssign,
            DevelopmentPilotIds.ExecutionExceptionAssignPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportReadPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.Read,
            DevelopmentPilotIds.ExportReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportWritePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.Write,
            DevelopmentPilotIds.ExportWritePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportDocumentManagePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.DocumentManage,
            DevelopmentPilotIds.ExportDocumentManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportDocumentVerifyPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.DocumentVerify,
            DevelopmentPilotIds.ExportDocumentVerifyPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportPackingManagePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.PackingManage,
            DevelopmentPilotIds.ExportPackingManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportPackingVerifyPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.PackingVerify,
            DevelopmentPilotIds.ExportPackingVerifyPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportShipmentManagePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.ShipmentManage,
            DevelopmentPilotIds.ExportShipmentManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportBrokerManagePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.BrokerManage,
            DevelopmentPilotIds.ExportBrokerManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportBrokerHandoffPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.BrokerHandoff,
            DevelopmentPilotIds.ExportBrokerHandoffPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportComplianceReadPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.ComplianceRead,
            DevelopmentPilotIds.ExportComplianceReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportComplianceManagePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.ComplianceManage,
            DevelopmentPilotIds.ExportComplianceManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportComplianceVerifyPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.ComplianceVerify,
            DevelopmentPilotIds.ExportComplianceVerifyPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportCountryResearchReadPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.CountryResearchRead,
            DevelopmentPilotIds.ExportCountryResearchReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportCountryResearchManagePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.CountryResearchManage,
            DevelopmentPilotIds.ExportCountryResearchManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportCountryResearchVerifyPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.CountryResearchVerify,
            DevelopmentPilotIds.ExportCountryResearchVerifyPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportAiReadPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.AiRead,
            DevelopmentPilotIds.ExportAiReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportAiRunPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.AiRun,
            DevelopmentPilotIds.ExportAiRunPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportAiReviewPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.AiReview,
            DevelopmentPilotIds.ExportAiReviewPermissionDefinitionId,
            cancellationToken);

    private static async Task EnsureExportDeDemoRulePackAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.ExportCountryRulePacks
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Id == DevelopmentPilotIds.ExportDeDemoRulePackId, cancellationToken);

        if (exists)
        {
            return;
        }

        var actorSubjectId = DevelopmentPilotIds.IdentitySubjectId.ToString();
        var rulePack = new ExportCountryRulePack
        {
            Id = DevelopmentPilotIds.ExportDeDemoRulePackId,
            TenantId = tenantId,
            FacilityId = null,
            CountryCode = "DE",
            Name = "DE Development Demo Rule Pack (NOT legal requirements)",
            Status = ExportCountryRulePackStatus.Verified,
            VersionNumber = 1,
            EffectiveFrom = new DateOnly(2026, 1, 1),
            EffectiveTo = null,
            SourceTitle = "CrevaFactOS development fixture — demo workflow only",
            SourceUrl = null,
            SourcePublisher = "CrevaFactOS Development Pilot",
            SourcePublicationDate = new DateOnly(2026, 8, 8),
            SourceAccessedAtUtc = seedTimestampUtc,
            SourceVersion = "demo-v1",
            VerifiedAtUtc = seedTimestampUtc,
            VerifiedBySubjectId = actorSubjectId,
            CreatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = actorSubjectId,
            UpdatedAtUtc = seedTimestampUtc,
            LastModifiedBySubjectId = actorSubjectId,
            Version = 1,
        };

        var commercialInvoiceRule = new ExportCountryComplianceRule
        {
            Id = DevelopmentPilotIds.ExportDeDemoRuleCommercialInvoiceId,
            TenantId = tenantId,
            RulePackId = rulePack.Id,
            RuleCode = "DE-DEMO-001",
            Title = "Verified commercial invoice required (development demo)",
            Description = "Development fixture only — NOT a real German legal requirement.",
            RuleType = ExportComplianceRuleType.Document,
            Severity = ExportComplianceSeverity.Blocking,
            RequirementMode = ExportComplianceRequirementMode.Mandatory,
            Blocking = true,
            RequiredDocumentType = ExportDocumentType.CommercialInvoice,
            SourceReference = "development:de-demo-rule-pack",
            CreatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = actorSubjectId,
            Version = 1,
        };

        var originEvidenceRule = new ExportCountryComplianceRule
        {
            Id = DevelopmentPilotIds.ExportDeDemoRuleOriginEvidenceId,
            TenantId = tenantId,
            RulePackId = rulePack.Id,
            RuleCode = "DE-DEMO-002",
            Title = "Origin evidence required (development demo)",
            Description = "Development fixture only — requires confirmed origin status, NOT legal customs approval.",
            RuleType = ExportComplianceRuleType.Origin,
            Severity = ExportComplianceSeverity.Blocking,
            RequirementMode = ExportComplianceRequirementMode.Mandatory,
            Blocking = true,
            SourceReference = "development:de-demo-rule-pack",
            CreatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = actorSubjectId,
            Version = 1,
        };

        dbContext.ExportCountryRulePacks.Add(rulePack);
        dbContext.ExportCountryComplianceRules.Add(commercialInvoiceRule);
        dbContext.ExportCountryComplianceRules.Add(originEvidenceRule);
    }

    private static async Task EnsureExportPilotCaseAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.ExportCases
            .IgnoreQueryFilters()
            .AnyAsync(
                c =>
                    c.TenantId == tenantId
                    && c.FacilityId == facilityId
                    && c.SourceOrderId == DevelopmentPilotIds.ExportPilotSourceOrderId,
                cancellationToken);

        if (exists)
        {
            return;
        }

        var exportCase = new ExportCase
        {
            Id = DevelopmentPilotIds.ExportPilotCaseId,
            TenantId = tenantId,
            FacilityId = facilityId,
            SourceOrderId = DevelopmentPilotIds.ExportPilotSourceOrderId,
            CaseNumber = "EXP-2026-000001",
            SourceOrderReference = "PILOT-ORD-EXPORT-001",
            CustomerName = "Nordic Components GmbH",
            DestinationCountryCode = "DE",
            OriginCountryCode = "TR",
            IncotermCode = null,
            Status = ExportCaseStatus.Open,
            ReadinessStatus = ExportReadinessStatus.NotReady,
            ReadinessPercent = 0,
            BlockingIssueCount = 0,
            MissingRequirementCount = 0,
            CreatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = DevelopmentPilotIds.IdentitySubjectId.ToString(),
            UpdatedAtUtc = seedTimestampUtc,
            LastModifiedBySubjectId = DevelopmentPilotIds.IdentitySubjectId.ToString(),
            Version = 1,
        };

        var readiness = ExportReadinessEngine.Calculate(exportCase, []);
        exportCase.ReadinessStatus = readiness.ReadinessStatus;
        exportCase.ReadinessPercent = readiness.ReadinessPercent;
        exportCase.BlockingIssueCount = readiness.BlockingIssueCount;
        exportCase.MissingRequirementCount = readiness.MissingRequirementCount;
        exportCase.Status = readiness.SuggestedCaseStatus;

        dbContext.ExportCases.Add(exportCase);
    }

    private static async Task EnsureExportPilotShipmentAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.ExportShipments
            .IgnoreQueryFilters()
            .AnyAsync(
                s => s.Id == DevelopmentPilotIds.ExportPilotShipmentId,
                cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.ExportShipments.Add(new ExportShipment
        {
            Id = DevelopmentPilotIds.ExportPilotShipmentId,
            TenantId = tenantId,
            FacilityId = facilityId,
            ExportCaseId = DevelopmentPilotIds.ExportPilotCaseId,
            ShipmentReference = "SHP-2026-000001",
            Status = ExportShipmentStatus.Preparing,
            TransportMode = "road",
            IncotermCode = "DAP",
            DestinationCountryCode = "DE",
            DestinationCity = "Hamburg",
            ForwarderName = "Pilot Forwarder GmbH",
            CarrierName = "Pilot Carrier Logistics",
            PlannedPickupAtUtc = DateTimeOffset.Parse("2026-08-16T10:00:00.000Z", CultureInfo.InvariantCulture),
            PlannedDepartureAtUtc = DateTimeOffset.Parse("2026-08-17T06:00:00.000Z", CultureInfo.InvariantCulture),
            PlannedArrivalAtUtc = DateTimeOffset.Parse("2026-08-20T14:00:00.000Z", CultureInfo.InvariantCulture),
            BookingReference = "BK-PILOT-001",
            RouteSummary = "TR factory → DE Hamburg",
            OriginLocation = "Pilot Facility TR",
            PortOrAirport = "Ambarli",
            ResponsibilityNotes = "Development pilot shipment — logistics source-of-truth in Export.",
            TotalPackageCount = 12,
            CreatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = DevelopmentPilotIds.IdentitySubjectId.ToString(),
            UpdatedAtUtc = seedTimestampUtc,
            LastModifiedBySubjectId = DevelopmentPilotIds.IdentitySubjectId.ToString(),
            Version = 1,
        });
    }

    private static async Task EnsurePlanningLogisticsPilotLinkageAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        CancellationToken cancellationToken)
    {
        var scheduledPlan = await dbContext.ProductionPlans
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                p =>
                    p.TenantId == tenantId
                    && p.FacilityId == facilityId
                    && p.Id == DevelopmentPilotIds.PlanScheduledId,
                cancellationToken);

        if (scheduledPlan is null)
        {
            return;
        }

        if (!scheduledPlan.DemandsJson.Contains("PILOT-ORD-EXPORT-001", StringComparison.Ordinal))
        {
            scheduledPlan.DemandsJson =
                """
                [{"id":"demand-pilot-003","productId":"product-pom-wheel-55","productCode":"POM-WHEEL-55","productName":"POM dişli çark","requiredQuantity":9600,"unit":"adet","priority":"normal","orderReference":"PILOT-ORD-EXPORT-001","customerName":"Nordic Components GmbH","requestedDeliveryDate":"2026-08-22T16:00:00.000Z"}]
                """;
            scheduledPlan.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }
    }

    private static async Task EnsurePilotProductionShiftsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var shiftDefinitions = new[]
        {
            (DevelopmentPilotIds.PilotShiftV1Id, "V1", "Gündüz Vardiyası", new TimeSpan(8, 0, 0), new TimeSpan(16, 0, 0), 1),
            (DevelopmentPilotIds.PilotShiftV2Id, "V2", "Akşam Vardiyası", new TimeSpan(16, 0, 0), new TimeSpan(0, 0, 0), 2),
            (DevelopmentPilotIds.PilotShiftV3Id, "V3", "Gece Vardiyası", new TimeSpan(0, 0, 0), new TimeSpan(8, 0, 0), 3),
        };

        foreach (var (id, code, name, start, end, sortOrder) in shiftDefinitions)
        {
            var exists = await dbContext.ProductionShifts
                .IgnoreQueryFilters()
                .AnyAsync(s => s.Id == id || (s.TenantId == tenantId && s.FacilityId == facilityId && s.Code == code), cancellationToken);

            if (!exists)
            {
                dbContext.ProductionShifts.Add(new Production.ProductionShift
                {
                    Id = id,
                    TenantId = tenantId,
                    FacilityId = facilityId,
                    Code = code,
                    Name = name,
                    StartLocalTime = start,
                    EndLocalTime = end,
                    IsActive = true,
                    SortOrder = sortOrder,
                    Version = 1,
                    CreatedAtUtc = seedTimestampUtc,
                    UpdatedAtUtc = seedTimestampUtc,
                });
            }
        }
    }

    private static async Task EnsureNamedPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid tenantRoleId,
        string permissionKey,
        Guid fallbackPermissionId,
        CancellationToken cancellationToken)
    {
        var permissionDefinition = await dbContext.PermissionDefinitions
            .SingleOrDefaultAsync(p => p.Key == permissionKey, cancellationToken)
            ?? await dbContext.PermissionDefinitions
                .SingleOrDefaultAsync(p => p.Id == fallbackPermissionId, cancellationToken);

        if (permissionDefinition is null)
        {
            return;
        }

        var exists = await dbContext.TenantRolePermissions
            .IgnoreQueryFilters()
            .AnyAsync(
                a =>
                    a.TenantId == tenantId
                    && a.TenantRoleId == tenantRoleId
                    && a.PermissionDefinitionId == permissionDefinition.Id,
                cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.TenantRolePermissions.Add(new TenantRolePermissionAssignment
        {
            TenantId = tenantId,
            TenantRoleId = tenantRoleId,
            PermissionDefinitionId = permissionDefinition.Id,
        });
    }

    private static async Task<IdentitySubject> EnsureOperatorIdentitySubjectAsync(
        CrevaFactOSDbContext dbContext,
        DevelopmentPilotOptions pilotOptions,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        const string operatorSubject = "production-pilot-operator";
        var issuer = pilotOptions.DevelopmentIssuer.Trim();

        var existing = await dbContext.IdentitySubjects
            .SingleOrDefaultAsync(
                i => i.ExternalIssuer == issuer && i.ExternalSubject == operatorSubject,
                cancellationToken)
            ?? await dbContext.IdentitySubjects
                .SingleOrDefaultAsync(
                    i => i.Id == DevelopmentPilotIds.OperatorIdentitySubjectId,
                    cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var identity = new IdentitySubject
        {
            Id = DevelopmentPilotIds.OperatorIdentitySubjectId,
            ExternalIssuer = issuer,
            ExternalSubject = operatorSubject,
            Status = IdentitySubjectStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.IdentitySubjects.Add(identity);
        return identity;
    }

    private static async Task<TenantMembership> EnsureOperatorTenantMembershipAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid identitySubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.TenantMemberships
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                m => m.TenantId == tenantId && m.IdentitySubjectId == identitySubjectId,
                cancellationToken)
            ?? await dbContext.TenantMemberships
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(
                    m => m.TenantId == tenantId && m.Id == DevelopmentPilotIds.OperatorTenantMembershipId,
                    cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var membership = new TenantMembership
        {
            Id = DevelopmentPilotIds.OperatorTenantMembershipId,
            TenantId = tenantId,
            IdentitySubjectId = identitySubjectId,
            Status = TenantMembershipStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.TenantMemberships.Add(membership);
        return membership;
    }

    private static async Task<TenantRole> EnsureOperatorTenantRoleAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        const string operatorRoleKey = "production-execution-operator";

        var existing = await dbContext.TenantRoles
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                r => r.TenantId == tenantId
                    && (r.Key == operatorRoleKey || r.Id == DevelopmentPilotIds.OperatorTenantRoleId),
                cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var role = new TenantRole
        {
            Id = DevelopmentPilotIds.OperatorTenantRoleId,
            TenantId = tenantId,
            Key = operatorRoleKey,
            DisplayName = "Production Execution Operator",
            Status = AuthorizationStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.TenantRoles.Add(role);
        return role;
    }

    private static async Task EnsureOperatorFacilityMembershipAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        Guid tenantMembershipId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.FacilityMemberships
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                m =>
                    m.TenantId == tenantId
                    && m.FacilityId == facilityId
                    && m.TenantMembershipId == tenantMembershipId,
                cancellationToken);

        if (existing is not null)
        {
            return;
        }

        dbContext.FacilityMemberships.Add(new FacilityMembership
        {
            Id = DevelopmentPilotIds.OperatorFacilityMembershipId,
            TenantId = tenantId,
            FacilityId = facilityId,
            TenantMembershipId = tenantMembershipId,
            Status = FacilityMembershipStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            Version = 1,
        });
    }

    private static async Task<IdentitySubject> EnsureQualityNegativeTestIdentitySubjectAsync(
        CrevaFactOSDbContext dbContext,
        DevelopmentPilotOptions pilotOptions,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        const string negativeTestSubject = "quality-negative-test-user";
        var issuer = pilotOptions.DevelopmentIssuer.Trim();

        var existing = await dbContext.IdentitySubjects
            .SingleOrDefaultAsync(
                i => i.ExternalIssuer == issuer && i.ExternalSubject == negativeTestSubject,
                cancellationToken)
            ?? await dbContext.IdentitySubjects
                .SingleOrDefaultAsync(
                    i => i.Id == DevelopmentPilotIds.QualityNegativeTestIdentitySubjectId,
                    cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var identity = new IdentitySubject
        {
            Id = DevelopmentPilotIds.QualityNegativeTestIdentitySubjectId,
            ExternalIssuer = issuer,
            ExternalSubject = negativeTestSubject,
            Status = IdentitySubjectStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.IdentitySubjects.Add(identity);
        return identity;
    }

    private static async Task<TenantMembership> EnsureQualityNegativeTestTenantMembershipAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid identitySubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.TenantMemberships
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                m => m.TenantId == tenantId && m.IdentitySubjectId == identitySubjectId,
                cancellationToken)
            ?? await dbContext.TenantMemberships
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(
                    m => m.TenantId == tenantId && m.Id == DevelopmentPilotIds.QualityNegativeTestTenantMembershipId,
                    cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var membership = new TenantMembership
        {
            Id = DevelopmentPilotIds.QualityNegativeTestTenantMembershipId,
            TenantId = tenantId,
            IdentitySubjectId = identitySubjectId,
            Status = TenantMembershipStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.TenantMemberships.Add(membership);
        return membership;
    }

    private static async Task<TenantRole> EnsureQualityNegativeTestTenantRoleAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        const string negativeTestRoleKey = "quality-authz-negative-test";

        var existing = await dbContext.TenantRoles
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                r => r.TenantId == tenantId
                    && (r.Key == negativeTestRoleKey
                        || r.Id == DevelopmentPilotIds.QualityNegativeTestTenantRoleId),
                cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var role = new TenantRole
        {
            Id = DevelopmentPilotIds.QualityNegativeTestTenantRoleId,
            TenantId = tenantId,
            Key = negativeTestRoleKey,
            DisplayName = "Quality Authz Negative Test (dev only)",
            Status = AuthorizationStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.TenantRoles.Add(role);
        return role;
    }

    private static async Task EnsureQualityNegativeTestFacilityMembershipAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        Guid tenantMembershipId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.FacilityMemberships
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                membership =>
                    membership.TenantId == tenantId
                    && membership.FacilityId == facilityId
                    && membership.TenantMembershipId == tenantMembershipId,
                cancellationToken);

        if (existing is not null)
        {
            return;
        }

        dbContext.FacilityMemberships.Add(new FacilityMembership
        {
            Id = DevelopmentPilotIds.QualityNegativeTestFacilityMembershipId,
            TenantId = tenantId,
            FacilityId = facilityId,
            TenantMembershipId = tenantMembershipId,
            Status = FacilityMembershipStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            Version = 1,
        });
    }

    private static async Task<int> EnsureProductionPlansAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string createdBy,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var seededCount = 0;
        var seedPlans = DevelopmentPilotProductionPlanSeed.CreatePlans(
            tenantId,
            facilityId,
            createdBy.Trim(),
            seedTimestampUtc);

        foreach (var seedPlan in seedPlans)
        {
            var existingPlan = await dbContext.ProductionPlans
                .IgnoreQueryFilters()
                .AnyAsync(
                    plan =>
                        plan.TenantId == tenantId
                        && plan.FacilityId == facilityId
                        && plan.ReferenceCode == seedPlan.ReferenceCode,
                    cancellationToken);

            if (existingPlan)
            {
                continue;
            }

            dbContext.ProductionPlans.Add(seedPlan);
            seededCount++;
        }

        return seededCount;
    }

    private static Task EnsureQualityReadPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            QualityPermissionKeys.Read,
            DevelopmentPilotIds.QualityReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureQualityManagePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            QualityPermissionKeys.Manage,
            DevelopmentPilotIds.QualityManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureQualityInspectPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            QualityPermissionKeys.Inspect,
            DevelopmentPilotIds.QualityInspectPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureQualityReleasePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            QualityPermissionKeys.Release,
            DevelopmentPilotIds.QualityReleasePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureQualityNcrManagePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            QualityPermissionKeys.NcrManage,
            DevelopmentPilotIds.QualityNcrManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureQualityHoldPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            QualityPermissionKeys.Hold,
            DevelopmentPilotIds.QualityHoldPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureQualityDispositionPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            QualityPermissionKeys.Disposition,
            DevelopmentPilotIds.QualityDispositionPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureMaintenanceReadPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            MaintenancePermissionKeys.Read,
            DevelopmentPilotIds.MaintenanceReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureMaintenanceManagePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            MaintenancePermissionKeys.Manage,
            DevelopmentPilotIds.MaintenanceManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureMaintenanceExecutePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            MaintenancePermissionKeys.Execute,
            DevelopmentPilotIds.MaintenanceExecutePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureErpReadPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ErpPermissionKeys.Read,
            DevelopmentPilotIds.ErpReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureErpManagePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ErpPermissionKeys.Manage,
            DevelopmentPilotIds.ErpManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureErpSyncPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ErpPermissionKeys.Sync,
            DevelopmentPilotIds.ErpSyncPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureErpWritebackRequestPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ErpPermissionKeys.WritebackRequest,
            DevelopmentPilotIds.ErpWritebackRequestPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureErpWritebackApprovePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ErpPermissionKeys.WritebackApprove,
            DevelopmentPilotIds.ErpWritebackApprovePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureErpWritebackExecutePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ErpPermissionKeys.WritebackExecute,
            DevelopmentPilotIds.ErpWritebackExecutePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureEdgeReadPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            EdgePermissionKeys.Read,
            DevelopmentPilotIds.EdgeReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureEdgeManagePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            EdgePermissionKeys.Manage,
            DevelopmentPilotIds.EdgeManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureEdgeIngestPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            EdgePermissionKeys.Ingest,
            DevelopmentPilotIds.EdgeIngestPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureMoldReadPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            MoldPermissionKeys.Read,
            DevelopmentPilotIds.MoldReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureMoldManagePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            MoldPermissionKeys.Manage,
            DevelopmentPilotIds.MoldManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureMaterialReadPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            MaterialPermissionKeys.Read,
            DevelopmentPilotIds.MaterialReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureMaterialManagePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            MaterialPermissionKeys.Manage,
            DevelopmentPilotIds.MaterialManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureProcessOeeReadPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProcessOeePermissionKeys.Read,
            DevelopmentPilotIds.ProcessOeeReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureProcessOeeManagePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProcessOeePermissionKeys.Manage,
            DevelopmentPilotIds.ProcessOeeManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureShiftWorkforceReadPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ShiftWorkforcePermissionKeys.Read,
            DevelopmentPilotIds.ShiftWorkforceReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureShiftWorkforceManagePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ShiftWorkforcePermissionKeys.Manage,
            DevelopmentPilotIds.ShiftWorkforceManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureShiftAssignReadPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ShiftAssignmentPermissionKeys.Read,
            DevelopmentPilotIds.ShiftAssignReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureShiftAssignManagePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ShiftAssignmentPermissionKeys.Manage,
            DevelopmentPilotIds.ShiftAssignManagePermissionDefinitionId,
            cancellationToken);
}
