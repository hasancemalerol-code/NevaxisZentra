namespace CrevaFactOS.Persistence.Development;

public static class DevelopmentPilotIds
{
    public static readonly Guid TenantId =
        Guid.Parse("bb000001-0001-4000-8000-000000000001");

    public static readonly Guid IdentitySubjectId =
        Guid.Parse("bb000001-0002-4000-8000-000000000002");

    public static readonly Guid TenantMembershipId =
        Guid.Parse("bb000001-0003-4000-8000-000000000003");

    public static readonly Guid TenantRoleId =
        Guid.Parse("bb000001-0004-4000-8000-000000000004");

    public static readonly Guid FacilityId =
        Guid.Parse("bb000001-0005-4000-8000-000000000005");

    public static readonly Guid FacilityMembershipId =
        Guid.Parse("bb000001-0006-4000-8000-000000000006");

    public static readonly Guid PlanDraftId =
        Guid.Parse("bb000001-0011-4000-8000-000000000011");

    public static readonly Guid PlanNeedsReviewId =
        Guid.Parse("bb000001-0012-4000-8000-000000000012");

    public static readonly Guid PlanScheduledId =
        Guid.Parse("bb000001-0013-4000-8000-000000000013");

    public static readonly Guid PlanBlockingRiskId =
        Guid.Parse("bb000001-0014-4000-8000-000000000014");

    public static readonly Guid PlanningReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000009");

    public static readonly Guid PlanningWritePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000010");

    public static readonly Guid PlanningReviewPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000011");

    public static readonly Guid PlanningApprovePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000012");

    public static readonly Guid PlanningSchedulePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000013");

    public static readonly Guid PlanningTransferPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000014");

    public static readonly Guid PlanningHandoffPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000015");

    // Second dev identity: planning-pilot-reviewer (approve/review role)
    public static readonly Guid ReviewerIdentitySubjectId =
        Guid.Parse("bb000001-0020-4000-8000-000000000020");

    public static readonly Guid ReviewerTenantMembershipId =
        Guid.Parse("bb000001-0021-4000-8000-000000000021");

    public static readonly Guid ReviewerTenantRoleId =
        Guid.Parse("bb000001-0022-4000-8000-000000000022");

    public static readonly Guid ReviewerFacilityMembershipId =
        Guid.Parse("bb000001-0023-4000-8000-000000000023");

    // Production execution permission definitions
    public static readonly Guid ExecutionReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000016");

    public static readonly Guid ExecutionWritePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000017");

    public static readonly Guid ExecutionStartPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000018");

    public static readonly Guid ExecutionPausePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000019");

    public static readonly Guid ExecutionCompletePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000020");

    public static readonly Guid ExecutionRecordPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000021");

    // Third dev identity: production-pilot-operator (execution role)
    public static readonly Guid OperatorIdentitySubjectId =
        Guid.Parse("bb000001-0030-4000-8000-000000000030");

    public static readonly Guid OperatorTenantMembershipId =
        Guid.Parse("bb000001-0031-4000-8000-000000000031");

    public static readonly Guid OperatorTenantRoleId =
        Guid.Parse("bb000001-0032-4000-8000-000000000032");

    public static readonly Guid OperatorFacilityMembershipId =
        Guid.Parse("bb000001-0033-4000-8000-000000000033");

    // 3L — downtime + setup permission definitions
    public static readonly Guid ExecutionDowntimePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000022");

    public static readonly Guid ExecutionSetupPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000023");

    // 4L — exception permission definition
    public static readonly Guid ExecutionExceptionPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000024");

    // 5L — assignment/handover/exception.assign permission definitions
    public static readonly Guid ExecutionAssignPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000025");

    public static readonly Guid ExecutionHandoverPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000026");

    public static readonly Guid ExecutionExceptionAssignPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000027");

    // 5L — dev pilot shift used by bootstrap
    public static readonly Guid PilotShiftV1Id =
        Guid.Parse("bb000001-0040-4000-8000-000000000040");

    public static readonly Guid PilotShiftV2Id =
        Guid.Parse("bb000001-0041-4000-8000-000000000041");

    public static readonly Guid PilotShiftV3Id =
        Guid.Parse("bb000001-0042-4000-8000-000000000042");

    // Export permission definitions
    public static readonly Guid ExportReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000028");

    public static readonly Guid ExportWritePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000029");

    public static readonly Guid ExportDocumentManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000030");

    public static readonly Guid ExportDocumentVerifyPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000031");

    // Export pilot fixture — SourceOrderId only (no commercial Order table)
    public static readonly Guid ExportPilotSourceOrderId =
        Guid.Parse("bb000001-0050-4000-8000-000000000050");

    public static readonly Guid ExportPilotCaseId =
        Guid.Parse("bb000001-0051-4000-8000-000000000051");

    // Export 2L permission definitions (032–036)
    public static readonly Guid ExportPackingManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000032");

    public static readonly Guid ExportPackingVerifyPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000033");

    public static readonly Guid ExportShipmentManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000034");

    public static readonly Guid ExportBrokerManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000035");

    public static readonly Guid ExportBrokerHandoffPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000036");

    // Export 3L permission definitions (037–042)
    public static readonly Guid ExportComplianceReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000037");

    public static readonly Guid ExportComplianceManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000038");

    public static readonly Guid ExportComplianceVerifyPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000039");

    public static readonly Guid ExportCountryResearchReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000040");

    public static readonly Guid ExportCountryResearchManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000041");

    public static readonly Guid ExportCountryResearchVerifyPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000042");

    // Export 4L permission definitions (043–045)
    public static readonly Guid ExportAiReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000043");

    public static readonly Guid ExportAiRunPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000044");

    public static readonly Guid ExportAiReviewPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000045");

    // Quality permission definitions (046–049)
    public static readonly Guid QualityReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000046");

    public static readonly Guid QualityManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000047");

    public static readonly Guid QualityInspectPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000048");

    public static readonly Guid QualityReleasePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000049");

    // Quality pilot fixture
    public static readonly Guid QualityPilotControlPlanId =
        Guid.Parse("bb000001-0060-4000-8000-000000000060");

    public static readonly Guid QualityPilotInspectionDefinitionId =
        Guid.Parse("bb000001-0061-4000-8000-000000000061");

    // Export 3L development demo fixtures (NOT real legal requirements)
    public static readonly Guid ExportDeDemoRulePackId =
        Guid.Parse("bb000001-0058-4000-8000-000000000058");

    public static readonly Guid ExportDeDemoRuleCommercialInvoiceId =
        Guid.Parse("bb000001-0059-4000-8000-000000000059");

    public static readonly Guid ExportDeDemoRuleOriginEvidenceId =
        Guid.Parse("bb000001-005a-4000-8000-00000000005a");

    // Export 2L pilot fixtures
    public static readonly Guid ExportPilotPackingUnitId =
        Guid.Parse("bb000001-0055-4000-8000-000000000055");

    public static readonly Guid ExportPilotShipmentId =
        Guid.Parse("bb000001-0056-4000-8000-000000000056");

    public static readonly Guid ExportPilotBrokerWorkspaceId =
        Guid.Parse("bb000001-0057-4000-8000-000000000057");
}
