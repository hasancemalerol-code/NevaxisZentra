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

    // Quality authz negative-test identity (development-only permission denial fixture)
    public static readonly Guid QualityNegativeTestIdentitySubjectId =
        Guid.Parse("bb000001-0070-4000-8000-000000000070");

    public static readonly Guid QualityNegativeTestTenantMembershipId =
        Guid.Parse("bb000001-0071-4000-8000-000000000071");

    public static readonly Guid QualityNegativeTestTenantRoleId =
        Guid.Parse("bb000001-0072-4000-8000-000000000072");

    public static readonly Guid QualityNegativeTestFacilityMembershipId =
        Guid.Parse("bb000001-0073-4000-8000-000000000073");

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

    public static readonly Guid QualityNcrManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000050");

    public static readonly Guid QualityHoldPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000051");

    public static readonly Guid QualityDispositionPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000052");

    // Quality pilot fixture
    public static readonly Guid QualityPilotControlPlanId =
        Guid.Parse("bb000001-0060-4000-8000-000000000060");

    public static readonly Guid QualityPilotInspectionDefinitionId =
        Guid.Parse("bb000001-0061-4000-8000-000000000061");

    // Maintenance permission definitions (053–055)
    public static readonly Guid MaintenanceReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000053");

    public static readonly Guid MaintenanceManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000054");

    public static readonly Guid MaintenanceExecutePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000055");

    // Maintenance pilot fixtures
    public static readonly Guid MaintenancePilotRequestId =
        Guid.Parse("bb000001-0080-4000-8000-000000000080");

    public static readonly Guid MaintenancePilotWorkOrderId =
        Guid.Parse("bb000001-0081-4000-8000-000000000081");

    public static readonly Guid MaintenancePilotPreventivePlanId =
        Guid.Parse("bb000001-0082-4000-8000-000000000082");

    public static readonly Guid MaintenancePilotReliabilityRequest2Id =
        Guid.Parse("bb000001-0083-4000-8000-000000000083");

    public static readonly Guid MaintenancePilotCorrectiveWoAId =
        Guid.Parse("bb000001-0084-4000-8000-000000000084");

    public static readonly Guid MaintenancePilotCorrectiveWoBId =
        Guid.Parse("bb000001-0085-4000-8000-000000000085");

    public static readonly Guid MaintenancePilotOverdueDueId =
        Guid.Parse("bb000001-0086-4000-8000-000000000086");

    public static readonly Guid MaintenancePilotCompletedDueOnTimeId =
        Guid.Parse("bb000001-0087-4000-8000-000000000087");

    public static readonly Guid MaintenancePilotCompletedDueLateId =
        Guid.Parse("bb000001-0088-4000-8000-000000000088");

    // ERP permission definitions (056–058)
    public static readonly Guid ErpReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000056");

    public static readonly Guid ErpManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000057");

    public static readonly Guid ErpSyncPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000058");

    public static readonly Guid ErpWritebackRequestPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000059");

    public static readonly Guid ErpWritebackApprovePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000060");

    public static readonly Guid ErpWritebackExecutePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000061");

    // ERP pilot fixture
    public static readonly Guid ErpPilotSimulatorConnectionId =
        Guid.Parse("bb000001-0090-4000-8000-000000000090");

    public static readonly Guid ErpPilotLogoTestConnectionId =
        Guid.Parse("bb000001-0091-4000-8000-000000000091");

    public static readonly Guid ErpPilotMikroTestConnectionId =
        Guid.Parse("bb000001-0092-4000-8000-000000000092");

    public static readonly Guid ErpPilotNetsisTestConnectionId =
        Guid.Parse("bb000001-0093-4000-8000-000000000093");

    // Edge permission definitions (062–064)
    public static readonly Guid EdgeReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000062");

    public static readonly Guid EdgeManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000063");

    public static readonly Guid EdgeIngestPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000064");

    // Mold permission definitions (065–066)
    public static readonly Guid MoldReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000065");

    public static readonly Guid MoldManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000066");

    // Material permission definitions (067–068)
    public static readonly Guid MaterialReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000067");

    public static readonly Guid MaterialManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000068");

    // Process/OEE permission definitions (069–06a)
    public static readonly Guid ProcessOeeReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000069");

    public static readonly Guid ProcessOeeManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-00000000006a");

    // Process/OEE pilot fixtures
    public static readonly Guid ProcessOeePilotWindowCalculatedId =
        Guid.Parse("bb000001-00c0-4000-8000-0000000000c0");

    public static readonly Guid ProcessOeePilotWindowInsufficientId =
        Guid.Parse("bb000001-00c1-4000-8000-0000000000c1");

    public static readonly Guid ProcessOeePilotWindowReviewRequiredId =
        Guid.Parse("bb000001-00c2-4000-8000-0000000000c2");

    public static readonly Guid ProcessOeePilotAssessmentCalculatedId =
        Guid.Parse("bb000001-00c3-4000-8000-0000000000c3");

    public static readonly Guid ProcessOeePilotAssessmentInsufficientId =
        Guid.Parse("bb000001-00c4-4000-8000-0000000000c4");

    public static readonly Guid ProcessOeePilotAssessmentReviewRequiredId =
        Guid.Parse("bb000001-00c5-4000-8000-0000000000c5");

    // Material pilot fixtures
    public static readonly Guid MaterialPilotPpId =
        Guid.Parse("bb000001-00a2-4000-8000-0000000000a2");

    public static readonly Guid MaterialPilotPpSpecId =
        Guid.Parse("bb000001-00a3-4000-8000-0000000000a3");

    public static readonly Guid MaterialPilotAbsId =
        Guid.Parse("bb000001-00a4-4000-8000-0000000000a4");

    public static readonly Guid MaterialPilotAbsSpecId =
        Guid.Parse("bb000001-00a5-4000-8000-0000000000a5");

    public static readonly Guid MaterialPilotBlockedId =
        Guid.Parse("bb000001-00a6-4000-8000-0000000000a6");

    // Material lot pilot fixtures (MATERIAL-2)
    public static readonly Guid MaterialStorageLocationRawWarehouseId =
        Guid.Parse("bb000001-00a7-4000-8000-0000000000a7");

    public static readonly Guid MaterialStorageLocationQuarantineId =
        Guid.Parse("bb000001-00a8-4000-8000-0000000000a8");

    public static readonly Guid MaterialStorageLocationBlockedBayId =
        Guid.Parse("bb000001-00a9-4000-8000-0000000000a9");

    public static readonly Guid MaterialPilotLotPpAvailableId =
        Guid.Parse("bb000001-00aa-4000-8000-0000000000aa");

    public static readonly Guid MaterialPilotLotAbsQualityHoldId =
        Guid.Parse("bb000001-00ab-4000-8000-0000000000ab");

    public static readonly Guid MaterialPilotLotBlockedExpiryRiskId =
        Guid.Parse("bb000001-00ac-4000-8000-0000000000ac");

    public static readonly Guid MaterialPilotLotPpCertificateId =
        Guid.Parse("bb000001-00ad-4000-8000-0000000000ad");

    // Material requirement pilot fixtures (MATERIAL-3)
    public static readonly Guid MaterialPilotRequirementPpCompleteId =
        Guid.Parse("bb000001-00ae-4000-8000-0000000000ae");

    public static readonly Guid MaterialPilotRequirementAbsPartialId =
        Guid.Parse("bb000001-00af-4000-8000-0000000000af");

    public static readonly Guid MaterialPilotRequirementPpExceedsRegrindId =
        Guid.Parse("bb000001-00b0-4000-8000-0000000000b0");

    public static readonly Guid MaterialPilotConsumptionObservationId =
        Guid.Parse("bb000001-00b1-4000-8000-0000000000b1");

    // Edge pilot fixtures (0094–0098)
    public static readonly Guid EdgePilotSimulatorGatewayId =
        Guid.Parse("bb000001-0094-4000-8000-000000000094");

    public static readonly Guid EdgePilotCncMachineId =
        Guid.Parse("bb000001-0095-4000-8000-000000000095");

    public static readonly Guid EdgePilotPressMachineId =
        Guid.Parse("bb000001-0096-4000-8000-000000000096");

    public static readonly Guid EdgePilotCncAssignmentId =
        Guid.Parse("bb000001-0097-4000-8000-000000000097");

    public static readonly Guid EdgePilotPressAssignmentId =
        Guid.Parse("bb000001-0098-4000-8000-000000000098");

    // Maintenance condition intelligence pilot fixtures (MAINT-PRED-1)
    public static readonly Guid MaintenanceConditionCncMappingId =
        Guid.Parse("bb000001-0099-4000-8000-000000000099");

    public static readonly Guid MaintenanceConditionPressMappingId =
        Guid.Parse("bb000001-009a-4000-8000-00000000009a");

    // MOLD-3 compatibility pilot profiles
    public static readonly Guid MoldCompatibilityPressProfileId =
        Guid.Parse("bb000001-009b-4000-8000-00000000009b");

    public static readonly Guid MoldCompatibilityCncProfileId =
        Guid.Parse("bb000001-009c-4000-8000-00000000009c");

    public static readonly Guid MaintenanceConditionMetricTemperatureId =
        Guid.Parse("bb000001-009b-4000-8000-00000000009b");

    public static readonly Guid MaintenanceConditionMetricPressureId =
        Guid.Parse("bb000001-009c-4000-8000-00000000009c");

    public static readonly Guid MaintenanceConditionMetricCycleTimeId =
        Guid.Parse("bb000001-009d-4000-8000-00000000009d");

    // Development-only facility: exists in tenant but has no pilot operator/reviewer membership.
    public static readonly Guid UnauthorizedFacilityId =
        Guid.Parse("bb000001-0005-4000-8000-000000000099");

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

    // Shift workforce permission definitions (06b–06c)
    public static readonly Guid ShiftWorkforceReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-00000000006b");

    public static readonly Guid ShiftWorkforceManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-00000000006c");

    // Shift workforce pilot fixtures
    public static readonly Guid ShiftWorkforcePilotPerson001Id =
        Guid.Parse("bb000001-00d0-4000-8000-0000000000d0");

    public static readonly Guid ShiftWorkforcePilotPerson002Id =
        Guid.Parse("bb000001-00d1-4000-8000-0000000000d1");

    public static readonly Guid ShiftWorkforcePilotPerson003Id =
        Guid.Parse("bb000001-00d2-4000-8000-0000000000d2");

    public static readonly Guid ShiftWorkforcePilotPerson004Id =
        Guid.Parse("bb000001-00d3-4000-8000-0000000000d3");

    public static readonly Guid ShiftWorkforcePilotPerson005Id =
        Guid.Parse("bb000001-00d4-4000-8000-0000000000d4");

    public static readonly Guid ShiftWorkforcePilotPerson006Id =
        Guid.Parse("bb000001-00d5-4000-8000-0000000000d5");

    public static readonly Guid ShiftWorkforceCompetencyInjectionId =
        Guid.Parse("bb000001-00e0-4000-8000-0000000000e0");

    public static readonly Guid ShiftWorkforceCompetencyMoldChangeId =
        Guid.Parse("bb000001-00e1-4000-8000-0000000000e1");

    public static readonly Guid ShiftWorkforceCompetencyProcessReviewId =
        Guid.Parse("bb000001-00e2-4000-8000-0000000000e2");

    public static readonly Guid ShiftWorkforceCompetencyMaterialHandlingId =
        Guid.Parse("bb000001-00e3-4000-8000-0000000000e3");

    public static readonly Guid ShiftWorkforceCompetencyRobotAuxId =
        Guid.Parse("bb000001-00e4-4000-8000-0000000000e4");

    public static readonly Guid ShiftWorkforceCompetencyQualityCheckId =
        Guid.Parse("bb000001-00e5-4000-8000-0000000000e5");

    public static readonly Guid ShiftWorkforceCompetencyMaintenanceSupportId =
        Guid.Parse("bb000001-00e6-4000-8000-0000000000e6");

    public static readonly Guid ShiftWorkforcePilotAssignmentInjectionId =
        Guid.Parse("bb000001-00f0-4000-8000-0000000000f0");

    public static readonly Guid ShiftWorkforcePilotAssignmentMoldChangeId =
        Guid.Parse("bb000001-00f1-4000-8000-0000000000f1");

    public static readonly Guid ShiftWorkforcePilotAssignmentQualityPartialId =
        Guid.Parse("bb000001-00f2-4000-8000-0000000000f2");

    // Shift operator assignment permission definitions (06d–06e)
    public static readonly Guid ShiftAssignReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-00000000006d");

    public static readonly Guid ShiftAssignManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-00000000006e");

    // Shift operator assignment pilot fixtures
    public static readonly Guid ShiftAssignPilotAssessmentId =
        Guid.Parse("bb000001-0100-4000-8000-000000000100");

    public static readonly Guid ShiftAssignPilotResult002Id =
        Guid.Parse("bb000001-0101-4000-8000-000000000101");

    public static readonly Guid ShiftAssignPilotAssignment002Id =
        Guid.Parse("bb000001-0102-4000-8000-000000000102");
}
