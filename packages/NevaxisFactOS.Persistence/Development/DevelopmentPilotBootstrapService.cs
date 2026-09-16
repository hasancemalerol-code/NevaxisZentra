using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.Export;
using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.Identity;
using NevaxisFactOS.Persistence.Production;
using NevaxisFactOS.Persistence.ProductionPlanning;
using NevaxisFactOS.Persistence.Tenancy;
using NevaxisFactOS.Persistence.Quality;
using NevaxisFactOS.Persistence.ShiftWorkforce;
using NevaxisFactOS.Persistence.ShiftAssignment;
using NevaxisFactOS.Persistence.ShiftRisk;
using NevaxisFactOS.Persistence.Energy;
using NevaxisFactOS.Persistence.IotPlc;
using NevaxisFactOS.Persistence.OperationalRisk;
using NevaxisFactOS.Persistence.AiTasks;
using NevaxisFactOS.Persistence.Teams;
using NevaxisFactOS.Persistence.CommercialIntelligence;
using NevaxisFactOS.Persistence.SaasCommercial;
using NevaxisFactOS.Persistence.SaasSubscription;
using NevaxisFactOS.Persistence.SaasBilling;
using NevaxisFactOS.Persistence.SaasIdentity;
using NevaxisFactOS.Persistence.AdminAccess;
using NevaxisFactOS.Persistence.Integration;
using NevaxisFactOS.Persistence.DigitalWorkforce;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace NevaxisFactOS.Persistence.Development;

internal sealed class DevelopmentPilotBootstrapService(
    IOptions<DevelopmentPilotOptions> options,
    IOptions<PersistenceOptions> persistenceOptions,
    IOptions<DevelopmentSaasIdentityOptions> saasIdentityOptions,
    SaasIdentityMfaSecretProtector mfaSecretProtector)
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
        await EnsureIntegrationReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureIntegrationReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureIntegrationReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureIntegrationManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureIntegrationRunPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureIntegrationMappingManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureIntegrationQuarantineManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureIntegrationSecretConfigurePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureIntegrationReprocessPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureIntegrationEventReplayPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureDigitalWorkforceReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureDigitalWorkforceReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureDigitalWorkforceReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureDigitalWorkforceManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureDigitalWorkforceAutonomyManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureDigitalWorkforcePolicyManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureDigitalWorkforceApprovalDecidePermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureDigitalWorkforceKillPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureDigitalWorkforceCostReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureShiftOperationsReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureShiftOperationsReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureShiftOperationsReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureShiftOperationsManagePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureShiftOperationsManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureShiftOperationsStartPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureShiftOperationsClosePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureShiftOperationsHandoverPreparePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureShiftOperationsHandoverSubmitPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureShiftOperationsHandoverAcceptPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureShiftOperationsOpenItemManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureFactoryOrchestrationReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureFactoryOrchestrationReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureFactoryOrchestrationReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureFactoryOrchestrationManagePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureFactoryOrchestrationManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureFactoryOrchestrationEscalatePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureFactoryOrchestrationVerifyPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureFactoryOrchestrationReopenPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureProductionManagerReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureProductionManagerReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureProductionManagerReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureProductionManagerSimulatePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureProductionManagerDecidePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureProductionManagerApprovePermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureProductionManagerAutonomyManagePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureContinuousImprovementReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureContinuousImprovementReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureContinuousImprovementReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureContinuousImprovementManagePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureContinuousImprovementManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExperimentCreatePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExperimentApprovePermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExperimentStopPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExperimentStopPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureImprovementStandardizePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
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
        await EnsureShiftRiskReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureShiftRiskReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureShiftRiskReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureShiftRiskManagePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureEnergyReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureEnergyReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureEnergyReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureEnergyManagePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureIotPlcReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureIotPlcReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureIotPlcReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureIotPlcManagePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureOperationalRiskReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureOperationalRiskReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureOperationalRiskReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureOperationalRiskManagePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskCreatePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskReviewPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskReviewPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskCreatePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskReviewPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskAcknowledgePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskAcknowledgePermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskAcknowledgePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskAssignPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskAssignPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskEscalatePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskEscalatePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskManagePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskSlaManagePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskSlaManagePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskDeliveryReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskDeliveryReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskDeliveryManagePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskSecureAccessReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskSecureAccessReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureAiTaskSecureAccessManagePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureTeamsReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureTeamsReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureTeamsReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureTeamsWritePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureTeamsWritePermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureTeamsWritePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureTeamsManagePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureTeamsManagePermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureCommercialIntelligenceReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureCommercialIntelligenceReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureCommercialIntelligenceReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureCommercialIntelligenceManagePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureSaasCommercialReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureSaasCommercialReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureSaasCommercialReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureSaasSubscriptionReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureSaasSubscriptionManagePermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureSaasBillingReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureSaasBillingManagePermissionAssignmentAsync(
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

        await DevelopmentPilotSaasCommercialSeed.EnsureSaasCommercialDevelopmentCatalogAsync(
            dbContext,
            operatorIdentity.Id.ToString(),
            seedTimestampUtc,
            cancellationToken);
        await DevelopmentPilotSaasSubscriptionSeed.EnsureDevelopmentSubscriptionAsync(
            dbContext,
            tenant.Id,
            operatorIdentity.Id.ToString(),
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
            await DevelopmentPilotQualitySeed.EnsureQualityPilotNcrFollowUpFixturesAsync(
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
            await DevelopmentPilotMoldSeed.EnsureMoldFollowUpPilotFixturesAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                operatorIdentity.Id.ToString(),
                seedTimestampUtc,
                cancellationToken);
            await DevelopmentPilotProductionExceptionSeed.EnsureProductionFollowUpPilotFixturesAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                operatorIdentity.Id.ToString(),
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
            await DevelopmentPilotErpInventorySeed.EnsureErpInventoryPilotRecordsAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                seedTimestampUtc,
                cancellationToken);
            await DevelopmentPilotProcurementDeliveryRiskSeed.EnsureProcurementDeliveryRiskPilotRecordsAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                operatorIdentity.Id.ToString(),
                seedTimestampUtc,
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await DevelopmentPilotMaterialAvailabilityRiskSeed.EnsureMaterialAvailabilityRiskPilotRecordsAsync(
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
            await DevelopmentPilotGoldenDemoP1Seed.EnsureGoldenDemoP1PilotDataAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                seedTimestampUtc,
                cancellationToken);
            await DevelopmentPilotGoldenDemoCustomsFileSeed.EnsureGoldenDemoCustomsFilePilotDataAsync(
                dbContext,
                tenant.Id,
                facility.Id,
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
            await DevelopmentPilotShiftRiskSeed.EnsureShiftRiskPilotRecordsAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                operatorIdentity.Id.ToString(),
                seedTimestampUtc,
                cancellationToken);
            await DevelopmentPilotEnergySeed.EnsureEnergyPilotRecordsAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                operatorIdentity.Id.ToString(),
                seedTimestampUtc,
                cancellationToken);
            await DevelopmentPilotEnergyIntelligenceSeed.EnsureEnergyIntelligencePilotRecordsAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                operatorIdentity.Id.ToString(),
                seedTimestampUtc,
                cancellationToken);
        await DevelopmentPilotIotPlcSeed.EnsureIotPlcPilotRecordsAsync(
            dbContext,
            tenant.Id,
            facility.Id,
            operatorIdentity.Id.ToString(),
            seedTimestampUtc,
            cancellationToken);
        await DevelopmentPilotIotPlcIntelligenceSeed.EnsureIotPlcIntelligencePilotRecordsAsync(
            dbContext,
            tenant.Id,
            facility.Id,
            operatorIdentity.Id.ToString(),
            seedTimestampUtc,
            cancellationToken);
        await DevelopmentPilotOperationalRiskSeed.EnsureOperationalRiskPilotRecordsAsync(
            dbContext,
            tenant.Id,
            facility.Id,
            operatorIdentity.Id.ToString(),
            seedTimestampUtc,
            cancellationToken);
        await DevelopmentPilotOperationalRiskDetectionSeed.EnsureOperationalRiskDetectionPilotRecordsAsync(
            dbContext,
            tenant.Id,
            facility.Id,
            operatorIdentity.Id.ToString(),
            seedTimestampUtc,
            cancellationToken);
        await DevelopmentPilotCommercialIntelligenceSeed.EnsureCommercialIntelligencePilotRecordsAsync(
            dbContext,
            tenant.Id,
            facility.Id,
            operatorIdentity.Id.ToString(),
            seedTimestampUtc,
            cancellationToken);
        await DevelopmentPilotCommercialIntelligenceComparisonSeed.EnsureCommercialIntelligenceComparisonPilotRecordsAsync(
            dbContext,
            tenant.Id,
            facility.Id,
            operatorIdentity.Id.ToString(),
            seedTimestampUtc,
            cancellationToken);
        await DevelopmentPilotCommercialSalesPriceSeed.EnsureCommercialSalesPricePilotRecordsAsync(
            dbContext,
            tenant.Id,
            facility.Id,
            operatorIdentity.Id.ToString(),
            seedTimestampUtc,
            cancellationToken);
        await DevelopmentPilotCommercialSalesPriceComparisonSeed.EnsureCommercialSalesPriceComparisonPilotRecordsAsync(
            dbContext,
            tenant.Id,
            facility.Id,
            operatorIdentity.Id.ToString(),
            seedTimestampUtc,
            cancellationToken);
        await DevelopmentPilotCommercialQuotationRiskSeed.EnsureCommercialQuotationRiskPilotRecordsAsync(
            dbContext,
            tenant.Id,
            facility.Id,
            operatorIdentity.Id.ToString(),
            seedTimestampUtc,
            cancellationToken);
        await DevelopmentPilotCommercialQuotationRiskComparisonSeed.EnsureCommercialQuotationRiskComparisonPilotRecordsAsync(
            dbContext,
            tenant.Id,
            facility.Id,
            operatorIdentity.Id.ToString(),
            seedTimestampUtc,
            cancellationToken);
        await DevelopmentPilotCommercialProfitabilitySeed.EnsureCommercialProfitabilityPilotRecordsAsync(
            dbContext,
            tenant.Id,
            facility.Id,
            operatorIdentity.Id.ToString(),
            seedTimestampUtc,
            cancellationToken);
        await DevelopmentPilotCommercialProfitabilityComparisonSeed.EnsureCommercialProfitabilityComparisonPilotRecordsAsync(
            dbContext,
            tenant.Id,
            facility.Id,
            operatorIdentity.Id.ToString(),
            seedTimestampUtc,
            cancellationToken);
            await DevelopmentPilotTeamsSeed.EnsureTeamsPilotDataAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                operatorIdentity.Id.ToString(),
                cancellationToken);
            await DevelopmentPilotAiTaskPresentationSeed.EnsurePresentationDemoTasksAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                operatorIdentity.Id.ToString(),
                DevelopmentPilotIds.PresentationDemoAiTaskSeedTimeUtc,
                cancellationToken);
            await DevelopmentPilotSaasBillingSeed.EnsureDevelopmentBillingAsync(
                dbContext,
                tenant.Id,
                operatorIdentity.Id.ToString(),
                seedTimestampUtc,
                cancellationToken);
            await DevelopmentPilotShiftContinuityRiskSeed.EnsureShiftContinuityRiskPilotRecordsAsync(
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
            await DevelopmentPilotDigitalWorkforceSeed.EnsureDigitalWorkforcePilotDataAsync(
                dbContext,
                bootstrapContext.TenantContextAccessor,
                tenant.Id,
                facility.Id,
                cancellationToken);
            await NevaxisFactOS.Persistence.FactoryDecisionCenter.DevelopmentPilotFactoryDecisionCenterSeed.EnsureCriticalOrderRecoveryAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                operatorIdentity.Id.ToString(),
                seedTimestampUtc,
                cancellationToken);
            await DevelopmentPilotShiftOperationsSeed.EnsureShiftOperationsPilotDataAsync(
                dbContext,
                bootstrapContext.TenantContextAccessor,
                tenant.Id,
                facility.Id,
                cancellationToken);
        }

        await DevelopmentSaasIdentitySeed.EnsureDevelopmentSaasIdentityFixturesAsync(
            dbContext,
            mfaSecretProtector,
            saasIdentityOptions,
            seedTimestampUtc,
            cancellationToken);
        await DevelopmentAdminAccessSeed.EnsureDevelopmentAdminAccessFoundationAsync(
            dbContext,
            saasIdentityOptions,
            seedTimestampUtc,
            cancellationToken);
        await DevelopmentPilotSalesDemoAccessSeed.EnsureSalesDemoFullAccessAsync(
            dbContext,
            seedTimestampUtc,
            cancellationToken);

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

    public Task EnsurePaidAccessAsync(CancellationToken cancellationToken = default) =>
        DevelopmentPilotAccessEnsureService.EnsurePaidAccessAsync(
            persistenceOptions,
            options,
            cancellationToken);

    public async Task EnsureSaasIdentityFixturesAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled || !saasIdentityOptions.Value.Enabled
            || string.IsNullOrWhiteSpace(saasIdentityOptions.Value.TenantOwnerPassword))
        {
            return;
        }

        var bootstrapContext = DevelopmentPilotBootstrapDbContextFactory.Create(persistenceOptions.Value);
        await using var dbContext = bootstrapContext.DbContext;
        var seedTimestampUtc = DateTimeOffset.UtcNow;
        var tenant = await dbContext.Tenants
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == DevelopmentPilotIds.TenantId
                    || candidate.TenantKey == options.Value.TenantKey.Trim(),
                cancellationToken);

        if (tenant is null)
        {
            return;
        }

        bootstrapContext.TenantContextAccessor.Set(
            new TenantContext(
                tenant.Id,
                tenant.TenantKey,
                DevelopmentPilotIds.TenantMembershipId));

        await DevelopmentSaasIdentitySeed.EnsureDevelopmentSaasIdentityFixturesAsync(
            dbContext,
            mfaSecretProtector,
            saasIdentityOptions,
            seedTimestampUtc,
            cancellationToken);
    }

    public async Task EnsureAdminAccessFixturesAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled || !saasIdentityOptions.Value.Enabled
            || string.IsNullOrWhiteSpace(saasIdentityOptions.Value.TenantOwnerPassword))
        {
            return;
        }

        var bootstrapContext = DevelopmentPilotBootstrapDbContextFactory.Create(persistenceOptions.Value);
        await using var dbContext = bootstrapContext.DbContext;
        var seedTimestampUtc = DateTimeOffset.UtcNow;
        var tenant = await dbContext.Tenants
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == DevelopmentPilotIds.TenantId
                    || candidate.TenantKey == options.Value.TenantKey.Trim(),
                cancellationToken);

        if (tenant is null)
        {
            return;
        }

        bootstrapContext.TenantContextAccessor.Set(
            new TenantContext(
                tenant.Id,
                tenant.TenantKey,
                DevelopmentSaasIdentityIds.TenantOwnerMembershipId));

        await DevelopmentAdminAccessSeed.EnsureDevelopmentLoginAuthorizationAsync(
            dbContext,
            seedTimestampUtc,
            cancellationToken);
        _ = await EnsureFacilityAsync(
            dbContext,
            tenant.Id,
            options.Value,
            seedTimestampUtc,
            cancellationToken);
        await DevelopmentAdminAccessSeed.EnsureDevelopmentTenantOwnerFacilityMembershipAsync(
            dbContext,
            seedTimestampUtc,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Tenant> EnsureTenantAsync(
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
            MembershipReference = TenantMembership.CreateReference(DevelopmentPilotIds.TenantMembershipId),
            Status = TenantMembershipStatus.Active,
            AuthorizationVersion = 1,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.TenantMemberships.Add(membership);
        return membership;
    }

    private static async Task<TenantRole> EnsureTenantRoleAsync(
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
            MembershipReference = TenantMembership.CreateReference(DevelopmentPilotIds.ReviewerTenantMembershipId),
            Status = TenantMembershipStatus.Active,
            AuthorizationVersion = 1,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.TenantMemberships.Add(membership);
        return membership;
    }

    private static async Task<TenantRole> EnsureReviewerTenantRoleAsync(
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Read,
            DevelopmentPilotIds.ExecutionReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionWritePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Write,
            DevelopmentPilotIds.ExecutionWritePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionStartPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Start,
            DevelopmentPilotIds.ExecutionStartPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionPausePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Pause,
            DevelopmentPilotIds.ExecutionPausePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionCompletePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Complete,
            DevelopmentPilotIds.ExecutionCompletePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionRecordPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Record,
            DevelopmentPilotIds.ExecutionRecordPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionDowntimePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Downtime,
            DevelopmentPilotIds.ExecutionDowntimePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionSetupPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Setup,
            DevelopmentPilotIds.ExecutionSetupPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionExceptionPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Exception,
            DevelopmentPilotIds.ExecutionExceptionPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionAssignPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Assign,
            DevelopmentPilotIds.ExecutionAssignPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionHandoverPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Handover,
            DevelopmentPilotIds.ExecutionHandoverPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionExceptionAssignPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.ExceptionAssign,
            DevelopmentPilotIds.ExecutionExceptionAssignPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.Read,
            DevelopmentPilotIds.ExportReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportWritePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.Write,
            DevelopmentPilotIds.ExportWritePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportDocumentManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.DocumentManage,
            DevelopmentPilotIds.ExportDocumentManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportDocumentVerifyPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.DocumentVerify,
            DevelopmentPilotIds.ExportDocumentVerifyPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportPackingManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.PackingManage,
            DevelopmentPilotIds.ExportPackingManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportPackingVerifyPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.PackingVerify,
            DevelopmentPilotIds.ExportPackingVerifyPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportShipmentManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.ShipmentManage,
            DevelopmentPilotIds.ExportShipmentManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportBrokerManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.BrokerManage,
            DevelopmentPilotIds.ExportBrokerManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportBrokerHandoffPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.BrokerHandoff,
            DevelopmentPilotIds.ExportBrokerHandoffPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportComplianceReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.ComplianceRead,
            DevelopmentPilotIds.ExportComplianceReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportComplianceManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.ComplianceManage,
            DevelopmentPilotIds.ExportComplianceManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportComplianceVerifyPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.ComplianceVerify,
            DevelopmentPilotIds.ExportComplianceVerifyPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportCountryResearchReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.CountryResearchRead,
            DevelopmentPilotIds.ExportCountryResearchReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportCountryResearchManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.CountryResearchManage,
            DevelopmentPilotIds.ExportCountryResearchManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportCountryResearchVerifyPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.CountryResearchVerify,
            DevelopmentPilotIds.ExportCountryResearchVerifyPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportAiReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.AiRead,
            DevelopmentPilotIds.ExportAiReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportAiRunPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.AiRun,
            DevelopmentPilotIds.ExportAiRunPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExportAiReviewPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ExportPermissionKeys.AiReview,
            DevelopmentPilotIds.ExportAiReviewPermissionDefinitionId,
            cancellationToken);

    private static async Task EnsureExportDeDemoRulePackAsync(
        NevaxisFactOSDbContext dbContext,
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
            SourceTitle = "NevaxisFactOS development fixture — demo workflow only",
            SourceUrl = null,
            SourcePublisher = "NevaxisFactOS Development Pilot",
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
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
            MembershipReference = TenantMembership.CreateReference(DevelopmentPilotIds.OperatorTenantMembershipId),
            Status = TenantMembershipStatus.Active,
            AuthorizationVersion = 1,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.TenantMemberships.Add(membership);
        return membership;
    }

    private static async Task<TenantRole> EnsureOperatorTenantRoleAsync(
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
            MembershipReference = TenantMembership.CreateReference(DevelopmentPilotIds.QualityNegativeTestTenantMembershipId),
            Status = TenantMembershipStatus.Active,
            AuthorizationVersion = 1,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.TenantMemberships.Add(membership);
        return membership;
    }

    private static async Task<TenantRole> EnsureQualityNegativeTestTenantRoleAsync(
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext,
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
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            QualityPermissionKeys.Read,
            DevelopmentPilotIds.QualityReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureQualityManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            QualityPermissionKeys.Manage,
            DevelopmentPilotIds.QualityManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureQualityInspectPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            QualityPermissionKeys.Inspect,
            DevelopmentPilotIds.QualityInspectPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureQualityReleasePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            QualityPermissionKeys.Release,
            DevelopmentPilotIds.QualityReleasePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureQualityNcrManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            QualityPermissionKeys.NcrManage,
            DevelopmentPilotIds.QualityNcrManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureQualityHoldPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            QualityPermissionKeys.Hold,
            DevelopmentPilotIds.QualityHoldPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureQualityDispositionPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            QualityPermissionKeys.Disposition,
            DevelopmentPilotIds.QualityDispositionPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureMaintenanceReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            MaintenancePermissionKeys.Read,
            DevelopmentPilotIds.MaintenanceReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureMaintenanceManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            MaintenancePermissionKeys.Manage,
            DevelopmentPilotIds.MaintenanceManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureMaintenanceExecutePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            MaintenancePermissionKeys.Execute,
            DevelopmentPilotIds.MaintenanceExecutePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureErpReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ErpPermissionKeys.Read,
            DevelopmentPilotIds.ErpReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureErpManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ErpPermissionKeys.Manage,
            DevelopmentPilotIds.ErpManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureErpSyncPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ErpPermissionKeys.Sync,
            DevelopmentPilotIds.ErpSyncPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureErpWritebackRequestPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ErpPermissionKeys.WritebackRequest,
            DevelopmentPilotIds.ErpWritebackRequestPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureErpWritebackApprovePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ErpPermissionKeys.WritebackApprove,
            DevelopmentPilotIds.ErpWritebackApprovePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureErpWritebackExecutePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ErpPermissionKeys.WritebackExecute,
            DevelopmentPilotIds.ErpWritebackExecutePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureIntegrationReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            IntegrationPermissionKeys.Read,
            DevelopmentPilotIds.IntegrationReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureIntegrationManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            IntegrationPermissionKeys.Manage,
            DevelopmentPilotIds.IntegrationManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureIntegrationRunPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            IntegrationPermissionKeys.Run,
            DevelopmentPilotIds.IntegrationRunPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureIntegrationMappingManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            IntegrationPermissionKeys.MappingManage,
            DevelopmentPilotIds.IntegrationMappingManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureIntegrationQuarantineManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            IntegrationPermissionKeys.QuarantineManage,
            DevelopmentPilotIds.IntegrationQuarantineManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureIntegrationSecretConfigurePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            IntegrationPermissionKeys.SecretConfigure,
            DevelopmentPilotIds.IntegrationSecretConfigurePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureIntegrationReprocessPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            IntegrationPermissionKeys.Reprocess,
            DevelopmentPilotIds.IntegrationReprocessPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureIntegrationEventReplayPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            IntegrationPermissionKeys.EventReplay,
            DevelopmentPilotIds.IntegrationEventReplayPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureDigitalWorkforceReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            DigitalWorkforcePermissionKeys.Read,
            DevelopmentPilotDigitalWorkforceIds.ReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureDigitalWorkforceManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            DigitalWorkforcePermissionKeys.Manage,
            DevelopmentPilotDigitalWorkforceIds.ManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureDigitalWorkforceAutonomyManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            DigitalWorkforcePermissionKeys.AutonomyManage,
            DevelopmentPilotDigitalWorkforceIds.AutonomyManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureDigitalWorkforcePolicyManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            DigitalWorkforcePermissionKeys.PolicyManage,
            DevelopmentPilotDigitalWorkforceIds.PolicyManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureDigitalWorkforceApprovalDecidePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            DigitalWorkforcePermissionKeys.ApprovalDecide,
            DevelopmentPilotDigitalWorkforceIds.ApprovalDecidePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureDigitalWorkforceKillPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            DigitalWorkforcePermissionKeys.Kill,
            DevelopmentPilotDigitalWorkforceIds.KillPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureDigitalWorkforceCostReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            DigitalWorkforcePermissionKeys.CostRead,
            DevelopmentPilotDigitalWorkforceIds.CostReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureShiftOperationsReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ShiftOperationsPermissionKeys.Read,
            DevelopmentPilotShiftOperationsIds.ReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureShiftOperationsManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ShiftOperationsPermissionKeys.Manage,
            DevelopmentPilotShiftOperationsIds.ManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureShiftOperationsStartPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ShiftOperationsPermissionKeys.Start,
            DevelopmentPilotShiftOperationsIds.StartPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureShiftOperationsClosePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ShiftOperationsPermissionKeys.Close,
            DevelopmentPilotShiftOperationsIds.ClosePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureShiftOperationsHandoverPreparePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ShiftOperationsPermissionKeys.HandoverPrepare,
            DevelopmentPilotShiftOperationsIds.HandoverPreparePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureShiftOperationsHandoverSubmitPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ShiftOperationsPermissionKeys.HandoverSubmit,
            DevelopmentPilotShiftOperationsIds.HandoverSubmitPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureShiftOperationsHandoverAcceptPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ShiftOperationsPermissionKeys.HandoverAccept,
            DevelopmentPilotShiftOperationsIds.HandoverAcceptPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureShiftOperationsOpenItemManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ShiftOperationsPermissionKeys.OpenItemManage,
            DevelopmentPilotShiftOperationsIds.OpenItemManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureFactoryOrchestrationReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            FactoryOrchestrationPermissionKeys.Read,
            DevelopmentPilotFactoryOrchestrationIds.ReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureFactoryOrchestrationManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            FactoryOrchestrationPermissionKeys.Manage,
            DevelopmentPilotFactoryOrchestrationIds.ManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureFactoryOrchestrationEscalatePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            FactoryOrchestrationPermissionKeys.Escalate,
            DevelopmentPilotFactoryOrchestrationIds.EscalatePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureFactoryOrchestrationVerifyPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            FactoryOrchestrationPermissionKeys.Verify,
            DevelopmentPilotFactoryOrchestrationIds.VerifyPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureFactoryOrchestrationReopenPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            FactoryOrchestrationPermissionKeys.Reopen,
            DevelopmentPilotFactoryOrchestrationIds.ReopenPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureProductionManagerReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionManagerPermissionKeys.Read,
            DevelopmentPilotProductionManagerIds.ReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureProductionManagerSimulatePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionManagerPermissionKeys.Simulate,
            DevelopmentPilotProductionManagerIds.SimulatePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureProductionManagerDecidePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionManagerPermissionKeys.Decide,
            DevelopmentPilotProductionManagerIds.DecidePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureProductionManagerApprovePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionManagerPermissionKeys.Approve,
            DevelopmentPilotProductionManagerIds.ApprovePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureProductionManagerAutonomyManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionManagerPermissionKeys.AutonomyManage,
            DevelopmentPilotProductionManagerIds.AutonomyManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureContinuousImprovementReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ContinuousImprovementPermissionKeys.Read,
            DevelopmentPilotContinuousImprovementIds.ReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureContinuousImprovementManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ContinuousImprovementPermissionKeys.Manage,
            DevelopmentPilotContinuousImprovementIds.ManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExperimentCreatePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ContinuousImprovementPermissionKeys.ExperimentCreate,
            DevelopmentPilotContinuousImprovementIds.ExperimentCreatePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExperimentApprovePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ContinuousImprovementPermissionKeys.ExperimentApprove,
            DevelopmentPilotContinuousImprovementIds.ExperimentApprovePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExperimentStopPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ContinuousImprovementPermissionKeys.ExperimentStop,
            DevelopmentPilotContinuousImprovementIds.ExperimentStopPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureImprovementStandardizePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ContinuousImprovementPermissionKeys.ImprovementStandardize,
            DevelopmentPilotContinuousImprovementIds.ImprovementStandardizePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureEdgeReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            EdgePermissionKeys.Read,
            DevelopmentPilotIds.EdgeReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureEdgeManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            EdgePermissionKeys.Manage,
            DevelopmentPilotIds.EdgeManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureEdgeIngestPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            EdgePermissionKeys.Ingest,
            DevelopmentPilotIds.EdgeIngestPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureMoldReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            MoldPermissionKeys.Read,
            DevelopmentPilotIds.MoldReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureMoldManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            MoldPermissionKeys.Manage,
            DevelopmentPilotIds.MoldManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureMaterialReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            MaterialPermissionKeys.Read,
            DevelopmentPilotIds.MaterialReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureMaterialManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            MaterialPermissionKeys.Manage,
            DevelopmentPilotIds.MaterialManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureProcessOeeReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProcessOeePermissionKeys.Read,
            DevelopmentPilotIds.ProcessOeeReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureProcessOeeManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProcessOeePermissionKeys.Manage,
            DevelopmentPilotIds.ProcessOeeManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureShiftWorkforceReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ShiftWorkforcePermissionKeys.Read,
            DevelopmentPilotIds.ShiftWorkforceReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureShiftWorkforceManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ShiftWorkforcePermissionKeys.Manage,
            DevelopmentPilotIds.ShiftWorkforceManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureShiftAssignReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ShiftAssignmentPermissionKeys.Read,
            DevelopmentPilotIds.ShiftAssignReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureShiftAssignManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ShiftAssignmentPermissionKeys.Manage,
            DevelopmentPilotIds.ShiftAssignManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureShiftRiskReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ShiftRiskPermissionKeys.Read,
            DevelopmentPilotIds.ShiftRiskReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureShiftRiskManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ShiftRiskPermissionKeys.Manage,
            DevelopmentPilotIds.ShiftRiskManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureEnergyReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            EnergyPermissionKeys.Read,
            DevelopmentPilotIds.EnergyReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureEnergyManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            EnergyPermissionKeys.Manage,
            DevelopmentPilotIds.EnergyManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureIotPlcReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            IotPlcPermissionKeys.Read,
            DevelopmentPilotIds.IotPlcReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureIotPlcManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            IotPlcPermissionKeys.Manage,
            DevelopmentPilotIds.IotPlcManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureOperationalRiskReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            OperationalRiskPermissionKeys.Read,
            DevelopmentPilotIds.OperationalRiskReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureOperationalRiskManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            OperationalRiskPermissionKeys.Manage,
            DevelopmentPilotIds.OperationalRiskManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureAiTaskReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            AiTaskPermissionKeys.Read,
            DevelopmentPilotIds.AiTaskReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureAiTaskCreatePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            AiTaskPermissionKeys.Create,
            DevelopmentPilotIds.AiTaskCreatePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureAiTaskReviewPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            AiTaskPermissionKeys.Review,
            DevelopmentPilotIds.AiTaskReviewPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureAiTaskAcknowledgePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            AiTaskPermissionKeys.Acknowledge,
            DevelopmentPilotIds.AiTaskAcknowledgePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureAiTaskAssignPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            AiTaskPermissionKeys.Assign,
            DevelopmentPilotIds.AiTaskAssignPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureAiTaskEscalatePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            AiTaskPermissionKeys.Escalate,
            DevelopmentPilotIds.AiTaskEscalatePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureAiTaskManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            AiTaskPermissionKeys.Manage,
            DevelopmentPilotIds.AiTaskManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureAiTaskSlaManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            AiTaskPermissionKeys.SlaManage,
            DevelopmentPilotIds.AiTaskSlaManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureAiTaskDeliveryReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            AiTaskPermissionKeys.DeliveryRead,
            DevelopmentPilotIds.AiTaskDeliveryReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureAiTaskDeliveryManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            AiTaskPermissionKeys.DeliveryManage,
            DevelopmentPilotIds.AiTaskDeliveryManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureAiTaskSecureAccessReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            AiTaskPermissionKeys.SecureAccessRead,
            DevelopmentPilotIds.AiTaskSecureAccessReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureAiTaskSecureAccessManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            AiTaskPermissionKeys.SecureAccessManage,
            DevelopmentPilotIds.AiTaskSecureAccessManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureTeamsReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            TeamsPermissionKeys.Read,
            DevelopmentPilotIds.TeamsReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureTeamsWritePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            TeamsPermissionKeys.Write,
            DevelopmentPilotIds.TeamsWritePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureTeamsManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            TeamsPermissionKeys.Manage,
            DevelopmentPilotIds.TeamsManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureCommercialIntelligenceReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            CommercialIntelligencePermissionKeys.Read,
            DevelopmentPilotIds.CommercialIntelligenceReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureCommercialIntelligenceManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            CommercialIntelligencePermissionKeys.Manage,
            DevelopmentPilotIds.CommercialIntelligenceManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureSaasCommercialReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            SaasCommercialPermissionKeys.Read,
            DevelopmentPilotIds.SaasCommercialReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureSaasSubscriptionReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            SaasSubscriptionPermissionKeys.Read,
            DevelopmentPilotIds.SaasSubscriptionReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureSaasSubscriptionManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            SaasSubscriptionPermissionKeys.Manage,
            DevelopmentPilotIds.SaasSubscriptionManagePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureSaasBillingReadPermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            SaasBillingPermissionKeys.Read,
            DevelopmentPilotIds.SaasBillingReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureSaasBillingManagePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            SaasBillingPermissionKeys.Manage,
            DevelopmentPilotIds.SaasBillingManagePermissionDefinitionId,
            cancellationToken);
}
