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

    public static readonly Guid QualityPilotNcrConfirmedId =
        Guid.Parse("bb000001-0063-4000-8000-000000000063");

    public static readonly Guid QualityPilotNcrInsufficientEvidenceId =
        Guid.Parse("bb000001-0064-4000-8000-000000000064");

    // MOLD-AI-TASK-INTEGRATION-1 follow-up pilot fixtures
    public static readonly Guid MoldPilotMaintenanceFollowUpId =
        Guid.Parse("bb000001-0070-4000-8000-000000000070");

    public static readonly Guid MoldPilotInsufficientEvidenceFollowUpId =
        Guid.Parse("bb000001-0071-4000-8000-000000000071");

    public static readonly Guid MoldPilotMaintenanceLifecycleEventId =
        Guid.Parse("bb000001-0072-4000-8000-000000000072");

    public static readonly Guid MoldPilotMaintenanceCounterSnapshotId =
        Guid.Parse("bb000001-0073-4000-8000-000000000073");

    // PRODUCTION-AI-TASK-INTEGRATION-1 follow-up pilot fixtures
    public static readonly Guid ProductionPilotFollowUpHandoffId =
        Guid.Parse("bb000001-0074-4000-8000-000000000074");

    public static readonly Guid ProductionPilotFollowUpOrderId =
        Guid.Parse("bb000001-0075-4000-8000-000000000075");

    public static readonly Guid ProductionPilotFollowUpInsufficientOrderId =
        Guid.Parse("bb000001-0076-4000-8000-000000000076");

    public static readonly Guid ProductionPilotExceptionFollowUpId =
        Guid.Parse("bb000001-0077-4000-8000-000000000077");

    public static readonly Guid ProductionPilotExceptionInsufficientEvidenceId =
        Guid.Parse("bb000001-0078-4000-8000-000000000078");

    public static readonly Guid ProductionPilotFollowUpInsufficientHandoffId =
        Guid.Parse("bb000001-0079-4000-8000-000000000079");

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

    // Inventory availability risk pilot fixtures (INVENTORY-RISK-FOUNDATION-1)
    public static readonly Guid ErpPilotPpInventoryBalanceId =
        Guid.Parse("bb000001-00b2-4000-8000-0000000000b2");

    public static readonly Guid MaterialPilotAvailabilityRiskShortageAssessmentId =
        Guid.Parse("bb000001-00b3-4000-8000-0000000000b3");

    public static readonly Guid MaterialPilotAvailabilityRiskInsufficientAssessmentId =
        Guid.Parse("bb000001-00b4-4000-8000-0000000000b4");

    public static readonly Guid MaterialPilotRequirementPpSmallId =
        Guid.Parse("bb000001-00b5-4000-8000-0000000000b5");

    public static readonly Guid MaterialPilotAvailabilityRiskSufficientAssessmentId =
        Guid.Parse("bb000001-00b6-4000-8000-0000000000b6");

    public const string MaterialPilotPpErpProductCode = "PILOT-PP-ERP";

    // Procurement delivery risk pilot fixtures (PROCUREMENT-RISK-FOUNDATION-1)
    public static readonly Guid ErpPilotPurchaseOrderOverdueId =
        Guid.Parse("bb000001-00c0-4000-8000-0000000000c0");

    public static readonly Guid ErpPilotPurchaseOrderOverdueLineId =
        Guid.Parse("bb000001-00c1-4000-8000-0000000000c1");

    public static readonly Guid ErpPilotPurchaseOrderDeliveryRiskId =
        Guid.Parse("bb000001-00c2-4000-8000-0000000000c2");

    public static readonly Guid ErpPilotPurchaseOrderDeliveryRiskLineId =
        Guid.Parse("bb000001-00c3-4000-8000-0000000000c3");

    public static readonly Guid ErpPilotPurchaseOrderInsufficientId =
        Guid.Parse("bb000001-00c4-4000-8000-0000000000c4");

    public static readonly Guid ErpPilotPurchaseOrderInsufficientLineId =
        Guid.Parse("bb000001-00c5-4000-8000-0000000000c5");

    public static readonly Guid ErpPilotPurchaseOrderOnTrackId =
        Guid.Parse("bb000001-00c6-4000-8000-0000000000c6");

    public static readonly Guid ErpPilotPurchaseOrderOnTrackLineId =
        Guid.Parse("bb000001-00c7-4000-8000-0000000000c7");

    public static readonly Guid ProcurementDeliveryRiskOverdueAssessmentId =
        Guid.Parse("bb000001-00c8-4000-8000-0000000000c8");

    public static readonly Guid ProcurementDeliveryRiskDeliveryRiskAssessmentId =
        Guid.Parse("bb000001-00c9-4000-8000-0000000000c9");

    public static readonly Guid ProcurementDeliveryRiskInsufficientAssessmentId =
        Guid.Parse("bb000001-00ca-4000-8000-0000000000ca");

    public static readonly Guid ProcurementDeliveryRiskOnTrackAssessmentId =
        Guid.Parse("bb000001-00cb-4000-8000-0000000000cb");

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

    // Plastic injection pilot machines 2–3 (0103–0106)
    public static readonly Guid EdgePilotInjectionMachine2Id =
        Guid.Parse("bb000001-0103-4000-8000-000000000103");

    public static readonly Guid EdgePilotInjectionMachine3Id =
        Guid.Parse("bb000001-0104-4000-8000-000000000104");

    public static readonly Guid EdgePilotInjection2AssignmentId =
        Guid.Parse("bb000001-0105-4000-8000-000000000105");

    public static readonly Guid EdgePilotInjection3AssignmentId =
        Guid.Parse("bb000001-0106-4000-8000-000000000106");

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

    // Shift coverage risk permission definitions (06f–070)
    public static readonly Guid ShiftRiskReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-00000000006f");

    public static readonly Guid ShiftRiskManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000070");

    // Shift coverage risk pilot fixtures
    public static readonly Guid ShiftRiskPilotRequirementCoveredId =
        Guid.Parse("bb000001-0110-4000-8000-000000000110");

    public static readonly Guid ShiftRiskPilotRequirementGapId =
        Guid.Parse("bb000001-0111-4000-8000-000000000111");

    public static readonly Guid ShiftRiskPilotRequirementCriticalId =
        Guid.Parse("bb000001-0112-4000-8000-000000000112");

    public static readonly Guid ShiftRiskPilotRequirementConflictId =
        Guid.Parse("bb000001-0113-4000-8000-000000000113");

    public static readonly Guid ShiftRiskPilotRequirementStaleId =
        Guid.Parse("bb000001-0114-4000-8000-000000000114");

    public static readonly Guid ShiftRiskPilotAssessmentCoveredId =
        Guid.Parse("bb000001-0120-4000-8000-000000000120");

    public static readonly Guid ShiftRiskPilotAssessmentGapId =
        Guid.Parse("bb000001-0121-4000-8000-000000000121");

    public static readonly Guid ShiftRiskPilotAssessmentCriticalId =
        Guid.Parse("bb000001-0122-4000-8000-000000000122");

    public static readonly Guid ShiftRiskPilotAssessmentConflictId =
        Guid.Parse("bb000001-0123-4000-8000-000000000123");

    public static readonly Guid ShiftRiskPilotAssessmentStaleId =
        Guid.Parse("bb000001-0124-4000-8000-000000000124");

    public static readonly Guid ShiftRiskPilotDraftConflictAssignmentId =
        Guid.Parse("bb000001-0125-4000-8000-000000000125");

    // Shift workforce continuity risk pilot fixtures (0130-0149)
    public static readonly Guid ShiftContinuityPilotAssessmentClearId =
        Guid.Parse("bb000001-0130-4000-8000-000000000130");

    public static readonly Guid ShiftContinuityPilotAssessmentAbsenceId =
        Guid.Parse("bb000001-0131-4000-8000-000000000131");

    public static readonly Guid ShiftContinuityPilotAssessmentExpiredId =
        Guid.Parse("bb000001-0132-4000-8000-000000000132");

    public static readonly Guid ShiftContinuityPilotAssessmentExpiringUrgentId =
        Guid.Parse("bb000001-0133-4000-8000-000000000133");

    public static readonly Guid ShiftContinuityPilotAssessmentExpiringUpcomingId =
        Guid.Parse("bb000001-0134-4000-8000-000000000134");

    public static readonly Guid ShiftContinuityPilotAssessmentPendingVerificationId =
        Guid.Parse("bb000001-0135-4000-8000-000000000135");

    public static readonly Guid ShiftContinuityPilotAssessmentStaleSourceId =
        Guid.Parse("bb000001-0136-4000-8000-000000000136");

    public static readonly Guid ShiftContinuityPilotAssessmentUnknownAvailabilityId =
        Guid.Parse("bb000001-0137-4000-8000-000000000137");

    public static readonly Guid ShiftContinuityPilotAssessmentOverlapId =
        Guid.Parse("bb000001-0138-4000-8000-000000000138");

    public static readonly Guid ShiftContinuityPilotAssessmentWorkloadFactsId =
        Guid.Parse("bb000001-0139-4000-8000-000000000139");

    public static readonly Guid ShiftContinuityPilotAssessmentCriticalCoverageId =
        Guid.Parse("bb000001-0140-4000-8000-000000000140");

    public static readonly Guid ShiftContinuityPilotCompetencyExpiredAssignmentId =
        Guid.Parse("bb000001-0141-4000-8000-000000000141");

    public static readonly Guid ShiftContinuityPilotCompetencyUrgentAssignmentId =
        Guid.Parse("bb000001-0142-4000-8000-000000000142");

    public static readonly Guid ShiftContinuityPilotCompetencyUpcomingAssignmentId =
        Guid.Parse("bb000001-0143-4000-8000-000000000143");

    public static readonly Guid ShiftContinuityPilotCompetencyPendingAssignmentId =
        Guid.Parse("bb000001-0144-4000-8000-000000000144");

    public static readonly Guid ShiftContinuityPilotOverlapAssignmentAId =
        Guid.Parse("bb000001-0145-4000-8000-000000000145");

    public static readonly Guid ShiftContinuityPilotOverlapAssignmentBId =
        Guid.Parse("bb000001-0146-4000-8000-000000000146");

    public static readonly Guid ShiftContinuityPilotWorkloadAssignmentId =
        Guid.Parse("bb000001-0147-4000-8000-000000000147");

    public static readonly Guid ShiftContinuityPilotCriticalRequirementId =
        Guid.Parse("bb000001-0148-4000-8000-000000000148");

    public static readonly Guid ShiftContinuityPilotStaleAssignmentId =
        Guid.Parse("bb000001-0149-4000-8000-000000000149");

    public static readonly Guid ShiftContinuityPilotCompetencyClearAssignmentId =
        Guid.Parse("bb000001-0150-4000-8000-000000000150");

    // SaaS commercial foundation (SAAS-COMMERCIAL-1)
    public static readonly Guid SaasCommercialReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000071");

    public static readonly Guid SaasCommercialDevPlanId =
        Guid.Parse("bb000001-00e0-4000-8000-0000000000e0");

    public static readonly Guid SaasCommercialDevPlanAuditEventId =
        Guid.Parse("bb000001-00e1-4000-8000-0000000000e1");

    public static readonly Guid SaasCommercialDevMonthlyVersionId =
        Guid.Parse("bb000001-00e2-4000-8000-0000000000e2");

    public static readonly Guid SaasCommercialDevAnnualVersionId =
        Guid.Parse("bb000001-00e3-4000-8000-0000000000e3");

    public static readonly Guid SaasCommercialFacilityLimitEntitlementId =
        Guid.Parse("bb000001-00e4-4000-8000-0000000000e4");

    public static readonly Guid SaasCommercialConnectedMachineLimitEntitlementId =
        Guid.Parse("bb000001-00e5-4000-8000-0000000000e5");

    public static readonly Guid SaasCommercialUserLimitEntitlementId =
        Guid.Parse("bb000001-00e6-4000-8000-0000000000e6");

    public static readonly Guid SaasCommercialShiftWorkforceModuleEntitlementId =
        Guid.Parse("bb000001-00e7-4000-8000-0000000000e7");

    public static readonly Guid SaasCommercialDevMonthlyTenantBaseComponentId =
        Guid.Parse("bb000001-00e8-4000-8000-0000000000e8");

    public static readonly Guid SaasCommercialDevMonthlyIncludedFacilityComponentId =
        Guid.Parse("bb000001-00e9-4000-8000-0000000000e9");

    public static readonly Guid SaasCommercialDevMonthlyAdditionalFacilityComponentId =
        Guid.Parse("bb000001-00ea-4000-8000-0000000000ea");

    public static readonly Guid SaasCommercialDevMonthlyIncludedMachineComponentId =
        Guid.Parse("bb000001-00eb-4000-8000-0000000000eb");

    public static readonly Guid SaasCommercialDevMonthlyAdditionalMachineComponentId =
        Guid.Parse("bb000001-00ec-4000-8000-0000000000ec");

    public static readonly Guid SaasCommercialDevMonthlyIncludedUserComponentId =
        Guid.Parse("bb000001-00ed-4000-8000-0000000000ed");

    public static readonly Guid SaasCommercialDevMonthlyAdditionalUserComponentId =
        Guid.Parse("bb000001-00ee-4000-8000-0000000000ee");

    public static readonly Guid SaasCommercialDevMonthlyFacilityGrantId =
        Guid.Parse("bb000001-00ef-4000-8000-0000000000ef");

    public static readonly Guid SaasCommercialDevMonthlyMachineGrantId =
        Guid.Parse("bb000001-00f0-4000-8000-0000000000f0");

    public static readonly Guid SaasCommercialDevMonthlyUserGrantId =
        Guid.Parse("bb000001-00f1-4000-8000-0000000000f1");

    public static readonly Guid SaasCommercialDevMonthlyModuleGrantId =
        Guid.Parse("bb000001-00f2-4000-8000-0000000000f2");

    public static readonly Guid SaasCommercialDevMonthlyCardCapabilityId =
        Guid.Parse("bb000001-00f3-4000-8000-0000000000f3");

    public static readonly Guid SaasCommercialDevMonthlyBankTransferCapabilityId =
        Guid.Parse("bb000001-00f4-4000-8000-0000000000f4");

    public static readonly Guid SaasCommercialDevMonthlyEnterpriseInvoiceCapabilityId =
        Guid.Parse("bb000001-00f5-4000-8000-0000000000f5");

    public static readonly Guid SaasCommercialDevMonthlyGracePolicyId =
        Guid.Parse("bb000001-00f6-4000-8000-0000000000f6");

    public static readonly Guid SaasCommercialDevAnnualTenantBaseComponentId =
        Guid.Parse("bb000001-00f7-4000-8000-0000000000f7");

    public static readonly Guid SaasCommercialDevAnnualIncludedFacilityComponentId =
        Guid.Parse("bb000001-00f8-4000-8000-0000000000f8");

    public static readonly Guid SaasCommercialDevAnnualAdditionalFacilityComponentId =
        Guid.Parse("bb000001-00f9-4000-8000-0000000000f9");

    public static readonly Guid SaasCommercialDevAnnualIncludedMachineComponentId =
        Guid.Parse("bb000001-00fa-4000-8000-0000000000fa");

    public static readonly Guid SaasCommercialDevAnnualAdditionalMachineComponentId =
        Guid.Parse("bb000001-00fb-4000-8000-0000000000fb");

    public static readonly Guid SaasCommercialDevAnnualIncludedUserComponentId =
        Guid.Parse("bb000001-00fc-4000-8000-0000000000fc");

    public static readonly Guid SaasCommercialDevAnnualAdditionalUserComponentId =
        Guid.Parse("bb000001-00fd-4000-8000-0000000000fd");

    public static readonly Guid SaasCommercialDevAnnualFacilityGrantId =
        Guid.Parse("bb000001-00fe-4000-8000-0000000000fe");

    public static readonly Guid SaasCommercialDevAnnualMachineGrantId =
        Guid.Parse("bb000001-00ff-4000-8000-0000000000ff");

    public static readonly Guid SaasCommercialDevAnnualUserGrantId =
        Guid.Parse("bb000001-0100-4000-8000-000000000100");

    public static readonly Guid SaasCommercialDevAnnualModuleGrantId =
        Guid.Parse("bb000001-0101-4000-8000-000000000101");

    public static readonly Guid SaasCommercialDevAnnualCardCapabilityId =
        Guid.Parse("bb000001-0102-4000-8000-000000000102");

    public static readonly Guid SaasCommercialDevAnnualBankTransferCapabilityId =
        Guid.Parse("bb000001-0103-4000-8000-000000000103");

    public static readonly Guid SaasCommercialDevAnnualEnterpriseInvoiceCapabilityId =
        Guid.Parse("bb000001-0104-4000-8000-000000000104");

    public static readonly Guid SaasCommercialDevAnnualGracePolicyId =
        Guid.Parse("bb000001-0105-4000-8000-000000000105");

    // SaaS subscription foundation (SAAS-SUBSCRIPTION-1)
    public static readonly Guid SaasSubscriptionReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000072");

    public static readonly Guid SaasSubscriptionManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000073");

    public static readonly Guid DevPilotTenantSubscriptionId =
        Guid.Parse("bb000002-00e0-4000-8000-0000000000e0");

    public static readonly Guid DevPilotTenantSubscriptionHistoryId =
        Guid.Parse("bb000002-00e1-4000-8000-0000000000e1");

    // SaaS billing foundation (SAAS-BILLING-1)
    public static readonly Guid SaasBillingReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000074");

    public static readonly Guid SaasBillingManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000075");

    public static readonly Guid DevPilotBillingAccountId =
        Guid.Parse("bb000003-00a0-4000-8000-0000000000a0");

    public static readonly Guid DevPilotBillingInvoiceDraftId =
        Guid.Parse("bb000003-00b0-4000-8000-0000000000b0");

    public static readonly Guid DevPilotBillingInvoiceOpenId =
        Guid.Parse("bb000003-00b1-4000-8000-0000000000b1");

    public static readonly Guid DevPilotBillingInvoicePaidId =
        Guid.Parse("bb000003-00b2-4000-8000-0000000000b2");

    public static readonly Guid DevPilotBillingInvoiceOverdueId =
        Guid.Parse("bb000003-00b3-4000-8000-0000000000b3");

    public static readonly Guid DevPilotBillingPaymentAwaitingId =
        Guid.Parse("bb000003-00c0-4000-8000-0000000000c0");

    public static readonly Guid DevPilotBillingPaymentSucceededId =
        Guid.Parse("bb000003-00c1-4000-8000-0000000000c1");

    // Energy permission definitions (076–077)
    public static readonly Guid EnergyReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000076");

    public static readonly Guid EnergyManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000077");

    // Energy pilot fixtures (0160–0189)
    public static readonly Guid EnergyPilotPointInj1Id =
        Guid.Parse("bb000001-0160-4000-8000-000000000160");

    public static readonly Guid EnergyPilotPointInj2Id =
        Guid.Parse("bb000001-0161-4000-8000-000000000161");

    public static readonly Guid EnergyPilotPointInj3Id =
        Guid.Parse("bb000001-0162-4000-8000-000000000162");

    public static readonly Guid EnergyPilotPointChillerId =
        Guid.Parse("bb000001-0163-4000-8000-000000000163");

    public static readonly Guid EnergyPilotPointCompressorId =
        Guid.Parse("bb000001-0164-4000-8000-000000000164");

    public static readonly Guid EnergyPilotPointDryerId =
        Guid.Parse("bb000001-0165-4000-8000-000000000165");

    public static readonly Guid EnergyPilotTariffId =
        Guid.Parse("bb000001-0166-4000-8000-000000000166");

    public static readonly Guid EnergyPilotWindowCalculatedId =
        Guid.Parse("bb000001-0167-4000-8000-000000000167");

    public static readonly Guid EnergyPilotWindowInsufficientId =
        Guid.Parse("bb000001-0168-4000-8000-000000000168");

    public static readonly Guid EnergyPilotWindowConflictId =
        Guid.Parse("bb000001-0169-4000-8000-000000000169");

    public static readonly Guid EnergyPilotAllocationMachineId =
        Guid.Parse("bb000001-016a-4000-8000-00000000016a");

    public static readonly Guid EnergyPilotAllocationResidualId =
        Guid.Parse("bb000001-016b-4000-8000-00000000016b");

    public static readonly Guid EnergyPilotReadingInj1Interval1Id =
        Guid.Parse("bb000001-016c-4000-8000-00000000016c");

    public static readonly Guid EnergyPilotReadingInj1Interval2Id =
        Guid.Parse("bb000001-016d-4000-8000-00000000016d");

    public static readonly Guid EnergyPilotReadingInj1Interval3Id =
        Guid.Parse("bb000001-016e-4000-8000-00000000016e");

    public static readonly Guid EnergyPilotReadingConflict1Id =
        Guid.Parse("bb000001-016f-4000-8000-00000000016f");

    public static readonly Guid EnergyPilotReadingConflict2Id =
        Guid.Parse("bb000001-0170-4000-8000-000000000170");

    // Energy intelligence pilot fixtures (0171–0189)
    public static readonly Guid Energy2PilotBaselineInj1Id =
        Guid.Parse("bb000001-0171-4000-8000-000000000171");

    public static readonly Guid Energy2PilotTargetConsumptionInj1Id =
        Guid.Parse("bb000001-0172-4000-8000-000000000172");

    public static readonly Guid Energy2PilotAssessmentWithinTargetId =
        Guid.Parse("bb000001-0173-4000-8000-000000000173");

    public static readonly Guid Energy2PilotTargetDemandCompressorId =
        Guid.Parse("bb000001-0174-4000-8000-000000000174");

    public static readonly Guid Energy2PilotWindowPeakCompressorId =
        Guid.Parse("bb000001-0175-4000-8000-000000000175");

    public static readonly Guid Energy2PilotAssessmentPeakDemandId =
        Guid.Parse("bb000001-0176-4000-8000-000000000176");

    public static readonly Guid Energy2PilotTargetIdleDryerId =
        Guid.Parse("bb000001-0177-4000-8000-000000000177");

    public static readonly Guid Energy2PilotWindowIdleDryerId =
        Guid.Parse("bb000001-0178-4000-8000-000000000178");

    public static readonly Guid Energy2PilotAssessmentIdleId =
        Guid.Parse("bb000001-0179-4000-8000-000000000179");

    public static readonly Guid Energy2PilotTargetIntensityId =
        Guid.Parse("bb000001-017a-4000-8000-00000000017a");

    public static readonly Guid Energy2PilotWindowIntensityId =
        Guid.Parse("bb000001-017b-4000-8000-00000000017b");

    public static readonly Guid Energy2PilotAssessmentIntensityId =
        Guid.Parse("bb000001-017c-4000-8000-00000000017c");

    public static readonly Guid Energy2PilotAssessmentInsufficientId =
        Guid.Parse("bb000001-017d-4000-8000-00000000017d");

    public static readonly Guid Energy2PilotAssessmentSourceConflictId =
        Guid.Parse("bb000001-017e-4000-8000-00000000017e");

    public static readonly Guid Energy2PilotWindowBaselineSample1Id =
        Guid.Parse("bb000001-017f-4000-8000-00000000017f");

    public static readonly Guid Energy2PilotWindowBaselineSample2Id =
        Guid.Parse("bb000001-0180-4000-8000-000000000180");

    public static readonly Guid Energy2PilotWindowBaselineSample3Id =
        Guid.Parse("bb000001-0181-4000-8000-000000000181");

    public static readonly Guid Energy2PilotWindowBaselineSample4Id =
        Guid.Parse("bb000001-0182-4000-8000-000000000182");

    public static readonly Guid Energy2PilotWindowBaselineSample5Id =
        Guid.Parse("bb000001-0183-4000-8000-000000000183");

    public static readonly Guid Energy2PilotCanonicalTrendWindow1Id =
        Guid.Parse("bb000001-0190-4000-8000-000000000190");

    public static readonly Guid Energy2PilotCanonicalTrendWindow2Id =
        Guid.Parse("bb000001-0191-4000-8000-000000000191");

    public static readonly Guid Energy2PilotCanonicalTrendWindow3Id =
        Guid.Parse("bb000001-0192-4000-8000-000000000192");

    public static readonly Guid Energy2PilotCanonicalTrendWindow4Id =
        Guid.Parse("bb000001-0193-4000-8000-000000000193");

    public static readonly Guid Energy2PilotCanonicalTrendWindow5Id =
        Guid.Parse("bb000001-0194-4000-8000-000000000194");

    public static readonly Guid Energy2PilotCanonicalTrendWindow6Id =
        Guid.Parse("bb000001-0195-4000-8000-000000000195");

    public static readonly Guid Energy2PilotCanonicalTrendWindow7Id =
        Guid.Parse("bb000001-0196-4000-8000-000000000196");

    public static readonly Guid Energy2PilotCanonicalTrendWindow8Id =
        Guid.Parse("bb000001-0197-4000-8000-000000000197");

    public static readonly Guid Energy2PilotCanonicalTrendAllocationMachine1Id =
        Guid.Parse("bb000001-01a0-4000-8000-0000000001a0");

    public static readonly Guid Energy2PilotCanonicalTrendAllocationMachine2Id =
        Guid.Parse("bb000001-01a1-4000-8000-0000000001a1");

    public static readonly Guid Energy2PilotCanonicalTrendAllocationMachine3Id =
        Guid.Parse("bb000001-01a2-4000-8000-0000000001a2");

    public static readonly Guid Energy2PilotCanonicalTrendAllocationMachine4Id =
        Guid.Parse("bb000001-01a3-4000-8000-0000000001a3");

    public static readonly Guid Energy2PilotCanonicalTrendAllocationMachine5Id =
        Guid.Parse("bb000001-01a4-4000-8000-0000000001a4");

    public static readonly Guid Energy2PilotCanonicalTrendAllocationMachine6Id =
        Guid.Parse("bb000001-01a5-4000-8000-0000000001a5");

    public static readonly Guid Energy2PilotCanonicalTrendAllocationMachine7Id =
        Guid.Parse("bb000001-01a6-4000-8000-0000000001a6");

    public static readonly Guid Energy2PilotCanonicalTrendAllocationMachine8Id =
        Guid.Parse("bb000001-01a7-4000-8000-0000000001a7");

    public static readonly Guid Energy2PilotCanonicalTrendAllocationResidual1Id =
        Guid.Parse("bb000001-01a8-4000-8000-0000000001a8");

    public static readonly Guid Energy2PilotCanonicalTrendAllocationResidual2Id =
        Guid.Parse("bb000001-01a9-4000-8000-0000000001a9");

    public static readonly Guid Energy2PilotCanonicalTrendAllocationResidual3Id =
        Guid.Parse("bb000001-01aa-4000-8000-0000000001aa");

    public static readonly Guid Energy2PilotCanonicalTrendAllocationResidual4Id =
        Guid.Parse("bb000001-01ab-4000-8000-0000000001ab");

    public static readonly Guid Energy2PilotCanonicalTrendAllocationResidual5Id =
        Guid.Parse("bb000001-01ac-4000-8000-0000000001ac");

    public static readonly Guid Energy2PilotCanonicalTrendAllocationResidual6Id =
        Guid.Parse("bb000001-01ad-4000-8000-0000000001ad");

    public static readonly Guid Energy2PilotCanonicalTrendAllocationResidual7Id =
        Guid.Parse("bb000001-01ae-4000-8000-0000000001ae");

    public static readonly Guid Energy2PilotCanonicalTrendAllocationResidual8Id =
        Guid.Parse("bb000001-01af-4000-8000-0000000001af");

    public const string Energy2PilotProductionContext = "CTX-PILOT-INJ-001";

    public const string Energy2PilotAssessmentWithinTargetRef = "ASSESS-WITHIN-TARGET";
    public const string Energy2PilotAssessmentPeakDemandRef = "ASSESS-PEAK-DEMAND";
    public const string Energy2PilotAssessmentIdleRef = "ASSESS-IDLE-CONSUMPTION";
    public const string Energy2PilotAssessmentIntensityRef = "ASSESS-ENERGY-INTENSITY";
    public const string Energy2PilotAssessmentInsufficientRef = "ASSESS-INSUFFICIENT-DATA";
    public const string Energy2PilotAssessmentSourceConflictRef = "ASSESS-SOURCE-CONFLICT";

    // IoT & PLC permission definitions (078–079)
    public static readonly Guid IotPlcReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000078");

    public static readonly Guid IotPlcManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000079");

    // IoT & PLC pilot fixtures (01c0–01df)
    public static readonly Guid IotPlcPilotEndpointOpcId =
        Guid.Parse("bb000001-01c0-4000-8000-0000000001c0");

    public static readonly Guid IotPlcPilotEndpointEdgeId =
        Guid.Parse("bb000001-01c1-4000-8000-0000000001c1");

    public static readonly Guid IotPlcPilotEndpointMqttId =
        Guid.Parse("bb000001-01c2-4000-8000-0000000001c2");

    public static readonly Guid IotPlcPilotSignalInj1StateId =
        Guid.Parse("bb000001-01c3-4000-8000-0000000001c3");

    public static readonly Guid IotPlcPilotSignalInj1CycleId =
        Guid.Parse("bb000001-01c4-4000-8000-0000000001c4");

    public static readonly Guid IotPlcPilotSignalInj2GoodId =
        Guid.Parse("bb000001-01c5-4000-8000-0000000001c5");

    public static readonly Guid IotPlcPilotSignalInj3TempId =
        Guid.Parse("bb000001-01c6-4000-8000-0000000001c6");

    public static readonly Guid IotPlcPilotSignalChillerPowerId =
        Guid.Parse("bb000001-01c7-4000-8000-0000000001c7");

    public static readonly Guid IotPlcPilotObsInj1StateId =
        Guid.Parse("bb000001-01c8-4000-8000-0000000001c8");

    public static readonly Guid IotPlcPilotObsInj1CycleId =
        Guid.Parse("bb000001-01c9-4000-8000-0000000001c9");

    public static readonly Guid IotPlcPilotObsInj2GoodId =
        Guid.Parse("bb000001-01ca-4000-8000-0000000001ca");

    public static readonly Guid IotPlcPilotObsInj3TempId =
        Guid.Parse("bb000001-01cb-4000-8000-0000000001cb");

    public static readonly Guid IotPlcPilotObsStaleConflictId =
        Guid.Parse("bb000001-01cc-4000-8000-0000000001cc");

    public static readonly Guid IotPlcPilotHealthOnlineId =
        Guid.Parse("bb000001-01cd-4000-8000-0000000001cd");

    public static readonly Guid IotPlcPilotHealthDegradedId =
        Guid.Parse("bb000001-01ce-4000-8000-0000000001ce");

    public static readonly Guid IotPlcPilotHealthOfflineId =
        Guid.Parse("bb000001-01cf-4000-8000-0000000001cf");

    // IoT & PLC intelligence pilot fixtures (01e0–01ff)
    public static readonly Guid IotPlc2PilotProfileStandardId =
        Guid.Parse("bb000001-01e0-4000-8000-0000000001e0");

    public static readonly Guid IotPlc2PilotProfileStrictId =
        Guid.Parse("bb000001-01e1-4000-8000-0000000001e1");

    public static readonly Guid IotPlc2PilotAssessHealthyConnectivityId =
        Guid.Parse("bb000001-01e2-4000-8000-0000000001e2");

    public static readonly Guid IotPlc2PilotAssessStaleSignalId =
        Guid.Parse("bb000001-01e3-4000-8000-0000000001e3");

    public static readonly Guid IotPlc2PilotAssessOfflineEndpointId =
        Guid.Parse("bb000001-01e4-4000-8000-0000000001e4");

    public static readonly Guid IotPlc2PilotAssessDegradedConnectionId =
        Guid.Parse("bb000001-01e5-4000-8000-0000000001e5");

    public static readonly Guid IotPlc2PilotAssessClockSkewId =
        Guid.Parse("bb000001-01e6-4000-8000-0000000001e6");

    public static readonly Guid IotPlc2PilotAssessSequenceGapId =
        Guid.Parse("bb000001-01e7-4000-8000-0000000001e7");

    public static readonly Guid IotPlc2PilotAssessSourceConflictId =
        Guid.Parse("bb000001-01e8-4000-8000-0000000001e8");

    public static readonly Guid IotPlc2PilotAssessInsufficientDataId =
        Guid.Parse("bb000001-01e9-4000-8000-0000000001e9");

    public static readonly Guid IotPlc2PilotScenarioObsHealthy1Id =
        Guid.Parse("bb000001-01ea-4000-8000-0000000001ea");

    public static readonly Guid IotPlc2PilotScenarioObsHealthy2Id =
        Guid.Parse("bb000001-01eb-4000-8000-0000000001eb");

    public static readonly Guid IotPlc2PilotScenarioObsStaleId =
        Guid.Parse("bb000001-01ec-4000-8000-0000000001ec");

    public static readonly Guid IotPlc2PilotScenarioObsStale2Id =
        Guid.Parse("bb000001-01f6-4000-8000-0000000001f6");

    public static readonly Guid IotPlc2PilotScenarioObsClockSkewId =
        Guid.Parse("bb000001-01ed-4000-8000-0000000001ed");

    public static readonly Guid IotPlc2PilotScenarioObsClockSkewCompanionId =
        Guid.Parse("bb000001-01f7-4000-8000-0000000001f7");

    public static readonly Guid IotPlc2PilotScenarioObsSeq1Id =
        Guid.Parse("bb000001-01ee-4000-8000-0000000001ee");

    public static readonly Guid IotPlc2PilotScenarioObsSeq2Id =
        Guid.Parse("bb000001-01ef-4000-8000-0000000001ef");

    public static readonly Guid IotPlc2PilotScenarioObsSeq4Id =
        Guid.Parse("bb000001-01f0-4000-8000-0000000001f0");

    public static readonly Guid IotPlc2PilotScenarioObsConflict1Id =
        Guid.Parse("bb000001-01f1-4000-8000-0000000001f1");

    public static readonly Guid IotPlc2PilotScenarioObsConflict2Id =
        Guid.Parse("bb000001-01f2-4000-8000-0000000001f2");

    public static readonly Guid IotPlc2PilotHealthOnlineFreshId =
        Guid.Parse("bb000001-01f3-4000-8000-0000000001f3");

    public static readonly Guid IotPlc2PilotHealthDegradedFreshId =
        Guid.Parse("bb000001-01f4-4000-8000-0000000001f4");

    public static readonly Guid IotPlc2PilotHealthOfflineFreshId =
        Guid.Parse("bb000001-01f5-4000-8000-0000000001f5");

    public static readonly DateTimeOffset IotPlc2PilotEvaluationTimeUtc =
        new(2026, 8, 21, 12, 0, 0, TimeSpan.Zero);

    // Operational risk permission definitions (080–081)
    public static readonly Guid OperationalRiskReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000080");

    public static readonly Guid OperationalRiskManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000081");

    // AI task center permission definitions (090–092)
    public static readonly Guid AiTaskReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000090");

    public static readonly Guid AiTaskCreatePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000091");

    public static readonly Guid AiTaskReviewPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000092");

    // Operational risk pilot fixtures (0200–020f)
    public static readonly Guid OperationalRiskPilotMaterialCaseId =
        Guid.Parse("bb000001-0200-4000-8000-000000000200");

    public static readonly Guid OperationalRiskPilotSupplierCaseId =
        Guid.Parse("bb000001-0201-4000-8000-000000000201");

    public static readonly Guid OperationalRiskPilotOrderDeliveryCaseId =
        Guid.Parse("bb000001-0202-4000-8000-000000000202");

    public static readonly Guid OperationalRiskPilotCostDeviationCaseId =
        Guid.Parse("bb000001-0203-4000-8000-000000000203");

    public static readonly Guid OperationalRiskPilotInsufficientDataCaseId =
        Guid.Parse("bb000001-0204-4000-8000-000000000204");

    public static readonly Guid OperationalRiskPilotSourceConflictCaseId =
        Guid.Parse("bb000001-0205-4000-8000-000000000205");

    public static readonly Guid OperationalRiskPilotResolvedCaseId =
        Guid.Parse("bb000001-0206-4000-8000-000000000206");

    public static readonly Guid OperationalRiskPilotMaterialEvidenceId =
        Guid.Parse("bb000001-0207-4000-8000-000000000207");

    public static readonly Guid OperationalRiskPilotResolvedHistoryId =
        Guid.Parse("bb000001-0208-4000-8000-000000000208");

    public static readonly DateTimeOffset OperationalRiskPilotSeedTimeUtc =
        new(2026, 8, 23, 11, 0, 0, TimeSpan.Zero);

    // Operational risk detection pilot fixtures (0210–021f)
    public static readonly Guid OperationalRiskDetectionPilotProfileId =
        Guid.Parse("bb000001-0210-4000-8000-000000000210");

    public static readonly Guid OperationalRiskDetectionPilotRuleEnergyAttentionId =
        Guid.Parse("bb000001-0211-4000-8000-000000000211");

    public static readonly Guid OperationalRiskDetectionPilotRuleEnergyWithinTargetId =
        Guid.Parse("bb000001-0212-4000-8000-000000000212");

    public static readonly Guid OperationalRiskDetectionPilotRuleIotHealthyId =
        Guid.Parse("bb000001-0213-4000-8000-000000000213");

    public static readonly Guid OperationalRiskDetectionPilotRuleIotUnavailableId =
        Guid.Parse("bb000001-0214-4000-8000-000000000214");

    public static readonly Guid OperationalRiskDetectionPilotRuleMaintenanceHighId =
        Guid.Parse("bb000001-0215-4000-8000-000000000215");

    public static readonly Guid OperationalRiskDetectionPilotRuleMaintenanceCriticalId =
        Guid.Parse("bb000001-0216-4000-8000-000000000216");

    public static readonly Guid OperationalRiskDetectionPilotMaintenanceAssessmentId =
        Guid.Parse("bb000001-0217-4000-8000-000000000217");

    public static readonly Guid OperationalRiskDetectionPilotTerminalCaseId =
        Guid.Parse("bb000001-0218-4000-8000-000000000218");

    public static readonly Guid OperationalRiskDetectionPilotBaselineRunId =
        Guid.Parse("bb000001-0219-4000-8000-000000000219");

    public const string OperationalRiskDetectionPilotProfileCode = "OPR2-PILOT-CROSS-MODULE";
    public const string OperationalRiskDetectionPilotTerminalCaseReference = "OR-PILOT-DET-TERM-001";

    // Commercial intelligence permission definitions (082–083)
    public static readonly Guid CommercialIntelligenceReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000082");

    public static readonly Guid CommercialIntelligenceManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000083");

    // Commercial intelligence pilot fixtures (0220–024f)
    public static readonly Guid CommercialIntelligencePilotProfileId =
        Guid.Parse("bb000001-0220-4000-8000-000000000220");

    public static readonly Guid CommercialIntelligencePilotWithinRangeObservation1Id =
        Guid.Parse("bb000001-0221-4000-8000-000000000221");

    public static readonly Guid CommercialIntelligencePilotWithinRangeObservation2Id =
        Guid.Parse("bb000001-0222-4000-8000-000000000222");

    public static readonly Guid CommercialIntelligencePilotWithinRangeObservation3Id =
        Guid.Parse("bb000001-0223-4000-8000-000000000223");

    public static readonly Guid CommercialIntelligencePilotWithinRangeObservation4Id =
        Guid.Parse("bb000001-0224-4000-8000-000000000224");

    public static readonly Guid CommercialIntelligencePilotWithinRangeObservation5Id =
        Guid.Parse("bb000001-0225-4000-8000-000000000225");

    public static readonly Guid CommercialIntelligencePilotWithinRangeObservation6Id =
        Guid.Parse("bb000001-0226-4000-8000-000000000226");

    public static readonly Guid CommercialIntelligencePilotInsufficientObservation1Id =
        Guid.Parse("bb000001-0227-4000-8000-000000000227");

    public static readonly Guid CommercialIntelligencePilotInsufficientObservation2Id =
        Guid.Parse("bb000001-0228-4000-8000-000000000228");

    public static readonly Guid CommercialIntelligencePilotInsufficientObservation3Id =
        Guid.Parse("bb000001-0229-4000-8000-000000000229");

    public static readonly Guid CommercialIntelligencePilotSupplierSpreadSup001Observation1Id =
        Guid.Parse("bb000001-0230-4000-8000-000000000230");

    public static readonly Guid CommercialIntelligencePilotSupplierSpreadSup001Observation2Id =
        Guid.Parse("bb000001-0231-4000-8000-000000000231");

    public static readonly Guid CommercialIntelligencePilotSupplierSpreadSup001Observation3Id =
        Guid.Parse("bb000001-0232-4000-8000-000000000232");

    public static readonly Guid CommercialIntelligencePilotSupplierSpreadSup001Observation4Id =
        Guid.Parse("bb000001-0233-4000-8000-000000000233");

    public static readonly Guid CommercialIntelligencePilotSupplierSpreadSup001Observation5Id =
        Guid.Parse("bb000001-0234-4000-8000-000000000234");

    public static readonly Guid CommercialIntelligencePilotSupplierSpreadSup001Observation6Id =
        Guid.Parse("bb000001-0235-4000-8000-000000000235");

    public static readonly Guid CommercialIntelligencePilotSupplierSpreadSup002Observation1Id =
        Guid.Parse("bb000001-0236-4000-8000-000000000236");

    public static readonly Guid CommercialIntelligencePilotSupplierSpreadSup002Observation2Id =
        Guid.Parse("bb000001-0237-4000-8000-000000000237");

    public static readonly Guid CommercialIntelligencePilotSupplierSpreadSup002Observation3Id =
        Guid.Parse("bb000001-0238-4000-8000-000000000238");

    public static readonly Guid CommercialIntelligencePilotSupplierSpreadSup002Observation4Id =
        Guid.Parse("bb000001-0239-4000-8000-000000000239");

    public static readonly Guid CommercialIntelligencePilotSupplierSpreadSup002Observation5Id =
        Guid.Parse("bb000001-023a-4000-8000-00000000023a");

    public static readonly Guid CommercialIntelligencePilotSupplierSpreadSup002Observation6Id =
        Guid.Parse("bb000001-023b-4000-8000-00000000023b");

    public static readonly Guid CommercialIntelligencePilotConflictCurrencyObservationId =
        Guid.Parse("bb000001-023c-4000-8000-00000000023c");

    public static readonly Guid CommercialIntelligencePilotConflictUnitObservationId =
        Guid.Parse("bb000001-023d-4000-8000-00000000023d");

    public static readonly Guid CommercialIntelligencePilotConflictBasisObservationId =
        Guid.Parse("bb000001-023e-4000-8000-00000000023e");

    public static readonly DateTimeOffset CommercialIntelligencePilotSeedTimeUtc =
        new(2026, 8, 24, 12, 15, 0, TimeSpan.Zero);

    public const string CommercialIntelligencePilotProfileCode = "COMMINT1-PILOT-PP";
    public const string CommercialIntelligencePilotSupplierReferencePrimary = "SUP-001";
    public const string CommercialIntelligencePilotSupplierReferenceSecondary = "SUP-002";
    public const string CommercialIntelligencePilotSupplierReferenceTie = "SUP-003";

    // Commercial intelligence comparison pilot fixtures (0260–027f)
    public static readonly Guid CommercialIntelligenceComparisonPilotStableTemporalRunId =
        Guid.Parse("bb000001-0260-4000-8000-000000000260");

    public static readonly Guid CommercialIntelligenceComparisonPilotUpwardTemporalRunId =
        Guid.Parse("bb000001-0261-4000-8000-000000000261");

    public static readonly Guid CommercialIntelligenceComparisonPilotFavorableTemporalRunId =
        Guid.Parse("bb000001-0262-4000-8000-000000000262");

    public static readonly Guid CommercialIntelligenceComparisonPilotSupplierSpreadRunId =
        Guid.Parse("bb000001-0263-4000-8000-000000000263");

    public static readonly Guid CommercialIntelligenceComparisonPilotSupplierTieRunId =
        Guid.Parse("bb000001-0264-4000-8000-000000000264");

    public static readonly Guid CommercialIntelligenceComparisonPilotInsufficientTemporalRunId =
        Guid.Parse("bb000001-0265-4000-8000-000000000265");

    public static readonly Guid CommercialIntelligenceComparisonPilotSourceConflictRunId =
        Guid.Parse("bb000001-0266-4000-8000-000000000266");

    public static readonly IReadOnlySet<Guid> CommercialIntelligenceComparisonPilotRunIds = new HashSet<Guid>
    {
        CommercialIntelligenceComparisonPilotStableTemporalRunId,
        CommercialIntelligenceComparisonPilotUpwardTemporalRunId,
        CommercialIntelligenceComparisonPilotFavorableTemporalRunId,
        CommercialIntelligenceComparisonPilotSupplierSpreadRunId,
        CommercialIntelligenceComparisonPilotSupplierTieRunId,
        CommercialIntelligenceComparisonPilotInsufficientTemporalRunId,
        CommercialIntelligenceComparisonPilotSourceConflictRunId,
    };

    public static readonly DateTimeOffset CommercialIntelligenceComparisonPilotCurrentWindowStartUtc =
        new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset CommercialIntelligenceComparisonPilotCurrentWindowEndUtc =
        new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset CommercialIntelligenceComparisonPilotReferenceWindowStartUtc =
        new(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset CommercialIntelligenceComparisonPilotReferenceWindowEndUtc =
        new(2026, 6, 30, 23, 59, 59, TimeSpan.Zero);

    public static readonly DateTimeOffset CommercialIntelligenceComparisonPilotSupplierWindowStartUtc =
        new(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset CommercialIntelligenceComparisonPilotSupplierWindowEndUtc =
        new(2026, 8, 24, 0, 0, 0, TimeSpan.Zero);

    public static readonly Guid CommercialSalesPricePilotProfileId =
        Guid.Parse("bb000001-0270-4000-8000-000000000270");

    public static readonly Guid CommercialSalesPricePilotWithinRangeObservation1Id =
        Guid.Parse("bb000001-0271-4000-8000-000000000271");

    public static readonly Guid CommercialSalesPricePilotWithinRangeObservation2Id =
        Guid.Parse("bb000001-0272-4000-8000-000000000272");

    public static readonly Guid CommercialSalesPricePilotWithinRangeObservation3Id =
        Guid.Parse("bb000001-0273-4000-8000-000000000273");

    public static readonly Guid CommercialSalesPricePilotWithinRangeObservation4Id =
        Guid.Parse("bb000001-0274-4000-8000-000000000274");

    public static readonly Guid CommercialSalesPricePilotWithinRangeObservation5Id =
        Guid.Parse("bb000001-0275-4000-8000-000000000275");

    public static readonly Guid CommercialSalesPricePilotWithinRangeObservation6Id =
        Guid.Parse("bb000001-0276-4000-8000-000000000276");

    public static readonly Guid CommercialSalesPricePilotInsufficientObservation1Id =
        Guid.Parse("bb000001-0277-4000-8000-000000000277");

    public static readonly Guid CommercialSalesPricePilotInsufficientObservation2Id =
        Guid.Parse("bb000001-0278-4000-8000-000000000278");

    public static readonly Guid CommercialSalesPricePilotInsufficientObservation3Id =
        Guid.Parse("bb000001-0279-4000-8000-000000000279");

    public static readonly Guid CommercialSalesPricePilotConflictCurrencyObservationId =
        Guid.Parse("bb000001-0280-4000-8000-000000000280");

    public static readonly Guid CommercialSalesPricePilotConflictUnitObservationId =
        Guid.Parse("bb000001-0281-4000-8000-000000000281");

    public static readonly Guid CommercialSalesPricePilotConflictBasisObservationId =
        Guid.Parse("bb000001-0282-4000-8000-000000000282");

    public static readonly Guid CommercialSalesPricePilotCustomerPositionSecondaryObservation1Id =
        Guid.Parse("bb000001-0283-4000-8000-000000000283");

    public static readonly Guid CommercialSalesPricePilotCustomerPositionSecondaryObservation2Id =
        Guid.Parse("bb000001-0284-4000-8000-000000000284");

    public static readonly Guid CommercialSalesPricePilotCustomerPositionSecondaryObservation3Id =
        Guid.Parse("bb000001-0285-4000-8000-000000000285");

    public static readonly Guid CommercialSalesPricePilotCustomerPositionSecondaryObservation4Id =
        Guid.Parse("bb000001-0286-4000-8000-000000000286");

    public static readonly Guid CommercialSalesPricePilotCustomerPositionSecondaryObservation5Id =
        Guid.Parse("bb000001-0287-4000-8000-000000000287");

    public static readonly Guid CommercialSalesPricePilotCustomerPositionSecondaryObservation6Id =
        Guid.Parse("bb000001-0288-4000-8000-000000000288");

    public static readonly Guid CommercialSalesPricePilotTemporalCurrentObservation1Id =
        Guid.Parse("bb000001-0289-4000-8000-000000000289");

    public static readonly Guid CommercialSalesPricePilotTemporalCurrentObservation2Id =
        Guid.Parse("bb000001-028a-4000-8000-00000000028a");

    public static readonly Guid CommercialSalesPricePilotTemporalCurrentObservation3Id =
        Guid.Parse("bb000001-028b-4000-8000-00000000028b");

    public static readonly Guid CommercialSalesPricePilotTemporalCurrentObservation4Id =
        Guid.Parse("bb000001-028c-4000-8000-00000000028c");

    public static readonly Guid CommercialSalesPricePilotTemporalCurrentObservation5Id =
        Guid.Parse("bb000001-028d-4000-8000-00000000028d");

    public static readonly Guid CommercialSalesPricePilotTemporalCurrentObservation6Id =
        Guid.Parse("bb000001-028e-4000-8000-00000000028e");

    public static readonly DateTimeOffset CommercialSalesPricePilotSeedTimeUtc =
        new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    public const string CommercialSalesPricePilotProfileCode = "COMMSALE1-PILOT-SP";
    public const string CommercialSalesPricePilotCustomerReferencePrimary = "CUST-001";
    public const string CommercialSalesPricePilotCustomerReferenceSecondary = "CUST-002";
    public const string CommercialSalesPricePilotCustomerReferenceSpreadPrimary = "CUST-SPREAD-A";
    public const string CommercialSalesPricePilotCustomerReferenceSpreadSecondary = "CUST-SPREAD-B";
    public const string CommercialSalesPricePilotCustomerReferenceTiePrimary = "CUST-TIE-001";
    public const string CommercialSalesPricePilotCustomerReferenceTieSecondary = "CUST-TIE-002";

    // Commercial sales price comparison pilot fixtures (02f0–02f6)
    public static readonly Guid CommercialSalesPriceComparisonPilotStableTemporalRunId =
        Guid.Parse("bb000001-02f0-4000-8000-0000000002f0");

    public static readonly Guid CommercialSalesPriceComparisonPilotDecreaseTemporalRunId =
        Guid.Parse("bb000001-02f1-4000-8000-0000000002f1");

    public static readonly Guid CommercialSalesPriceComparisonPilotPositiveVarianceTemporalRunId =
        Guid.Parse("bb000001-02f2-4000-8000-0000000002f2");

    public static readonly Guid CommercialSalesPriceComparisonPilotCustomerSpreadRunId =
        Guid.Parse("bb000001-02f3-4000-8000-0000000002f3");

    public static readonly Guid CommercialSalesPriceComparisonPilotCustomerTieRunId =
        Guid.Parse("bb000001-02f4-4000-8000-0000000002f4");

    public static readonly Guid CommercialSalesPriceComparisonPilotInsufficientTemporalRunId =
        Guid.Parse("bb000001-02f5-4000-8000-0000000002f5");

    public static readonly Guid CommercialSalesPriceComparisonPilotSourceConflictRunId =
        Guid.Parse("bb000001-02f6-4000-8000-0000000002f6");

    public static readonly IReadOnlySet<Guid> CommercialSalesPriceComparisonPilotRunIds = new HashSet<Guid>
    {
        CommercialSalesPriceComparisonPilotStableTemporalRunId,
        CommercialSalesPriceComparisonPilotDecreaseTemporalRunId,
        CommercialSalesPriceComparisonPilotPositiveVarianceTemporalRunId,
        CommercialSalesPriceComparisonPilotCustomerSpreadRunId,
        CommercialSalesPriceComparisonPilotCustomerTieRunId,
        CommercialSalesPriceComparisonPilotInsufficientTemporalRunId,
        CommercialSalesPriceComparisonPilotSourceConflictRunId,
    };

    public static readonly DateTimeOffset CommercialSalesPriceComparisonPilotCurrentWindowStartUtc =
        new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset CommercialSalesPriceComparisonPilotCurrentWindowEndUtc =
        new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset CommercialSalesPriceComparisonPilotReferenceWindowStartUtc =
        new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset CommercialSalesPriceComparisonPilotReferenceWindowEndUtc =
        new(2026, 6, 30, 23, 59, 59, TimeSpan.Zero);

    public static readonly DateTimeOffset CommercialSalesPriceComparisonPilotCustomerWindowStartUtc =
        new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset CommercialSalesPriceComparisonPilotCustomerWindowEndUtc =
        new(2026, 10, 24, 0, 0, 0, TimeSpan.Zero);

    public static readonly Guid CommercialQuotationRiskPilotProfileId =
        Guid.Parse("bb000001-0280-4000-8000-000000000280");

    public const string CommercialQuotationRiskPilotProfileCode = "COMMQUOTE1-PILOT-QRP";

    public static readonly DateTimeOffset CommercialQuotationRiskPilotSeedTimeUtc =
        new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset CommercialQuotationRiskPilotReferenceWindowStartUtc =
        new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset CommercialQuotationRiskPilotReferenceWindowEndUtc =
        new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    public static readonly Guid CommercialQuotationRiskPilotMaterialWithinThresholdId =
        Guid.Parse("bb000001-0281-4000-8000-000000000281");

    public static readonly Guid CommercialQuotationRiskPilotMaterialBelowReferenceId =
        Guid.Parse("bb000001-0282-4000-8000-000000000282");

    public static readonly Guid CommercialQuotationRiskPilotMaterialAboveReferenceId =
        Guid.Parse("bb000001-0283-4000-8000-000000000283");

    public static readonly Guid CommercialQuotationRiskPilotMaterialExpiringSoonId =
        Guid.Parse("bb000001-0284-4000-8000-000000000284");

    public static readonly Guid CommercialQuotationRiskPilotMaterialExpiredId =
        Guid.Parse("bb000001-0285-4000-8000-000000000285");

    public static readonly Guid CommercialQuotationRiskPilotMaterialPaymentExceededId =
        Guid.Parse("bb000001-0286-4000-8000-000000000286");

    public static readonly Guid CommercialQuotationRiskPilotMaterialInsufficientId =
        Guid.Parse("bb000001-0287-4000-8000-000000000287");

    public static readonly Guid CommercialQuotationRiskPilotMaterialSourceConflictId =
        Guid.Parse("bb000001-0288-4000-8000-000000000288");

    public static readonly Guid CommercialQuotationRiskPilotEvidenceWithinThresholdId =
        Guid.Parse("bb000001-0291-4000-8000-000000000291");

    public static readonly Guid CommercialQuotationRiskPilotEvidenceBelowReferenceId =
        Guid.Parse("bb000001-0292-4000-8000-000000000292");

    public static readonly Guid CommercialQuotationRiskPilotEvidenceAboveReferenceId =
        Guid.Parse("bb000001-0293-4000-8000-000000000293");

    public static readonly Guid CommercialQuotationRiskPilotEvidenceExpiringSoonId =
        Guid.Parse("bb000001-0294-4000-8000-000000000294");

    public static readonly Guid CommercialQuotationRiskPilotEvidenceExpiredId =
        Guid.Parse("bb000001-0295-4000-8000-000000000295");

    public static readonly Guid CommercialQuotationRiskPilotEvidencePaymentExceededId =
        Guid.Parse("bb000001-0296-4000-8000-000000000296");

    public static readonly Guid CommercialQuotationRiskPilotEvidenceInsufficientId =
        Guid.Parse("bb000001-0297-4000-8000-000000000297");

    public static readonly Guid CommercialQuotationRiskPilotEvidenceSourceConflictId =
        Guid.Parse("bb000001-0298-4000-8000-000000000298");

    public static readonly Guid CommercialQuotationRiskPilotAssessmentWithinThresholdId =
        Guid.Parse("bb000001-02a1-4000-8000-0000000002a1");

    public static readonly Guid CommercialQuotationRiskPilotAssessmentBelowReferenceId =
        Guid.Parse("bb000001-02a2-4000-8000-0000000002a2");

    public static readonly Guid CommercialQuotationRiskPilotAssessmentAboveReferenceId =
        Guid.Parse("bb000001-02a3-4000-8000-0000000002a3");

    public static readonly Guid CommercialQuotationRiskPilotAssessmentExpiringSoonId =
        Guid.Parse("bb000001-02a4-4000-8000-0000000002a4");

    public static readonly Guid CommercialQuotationRiskPilotAssessmentExpiredId =
        Guid.Parse("bb000001-02a5-4000-8000-0000000002a5");

    public static readonly Guid CommercialQuotationRiskPilotAssessmentPaymentExceededId =
        Guid.Parse("bb000001-02a6-4000-8000-0000000002a6");

    public static readonly Guid CommercialQuotationRiskPilotAssessmentInsufficientId =
        Guid.Parse("bb000001-02a7-4000-8000-0000000002a7");

    public static readonly Guid CommercialQuotationRiskPilotAssessmentSourceConflictId =
        Guid.Parse("bb000001-02a8-4000-8000-0000000002a8");

    public static readonly IReadOnlySet<Guid> CommercialQuotationRiskPilotAssessmentIds = new HashSet<Guid>
    {
        CommercialQuotationRiskPilotAssessmentWithinThresholdId,
        CommercialQuotationRiskPilotAssessmentBelowReferenceId,
        CommercialQuotationRiskPilotAssessmentAboveReferenceId,
        CommercialQuotationRiskPilotAssessmentExpiringSoonId,
        CommercialQuotationRiskPilotAssessmentExpiredId,
        CommercialQuotationRiskPilotAssessmentPaymentExceededId,
        CommercialQuotationRiskPilotAssessmentInsufficientId,
        CommercialQuotationRiskPilotAssessmentSourceConflictId,
    };

    public const string CommercialQuotationRiskPilotCustomerWithin = "COMMQUOTE1-CUST-WITHIN";
    public const string CommercialQuotationRiskPilotCustomerBelow = "COMMQUOTE1-CUST-BELOW";
    public const string CommercialQuotationRiskPilotCustomerAbove = "COMMQUOTE1-CUST-ABOVE";
    public const string CommercialQuotationRiskPilotCustomerExpiring = "COMMQUOTE1-CUST-EXPIRING";
    public const string CommercialQuotationRiskPilotCustomerExpired = "COMMQUOTE1-CUST-EXPIRED";
    public const string CommercialQuotationRiskPilotCustomerPayment = "COMMQUOTE1-CUST-PAYMENT";
    public const string CommercialQuotationRiskPilotCustomerInsufficient = "COMMQUOTE1-CUST-INSUFFICIENT";
    public const string CommercialQuotationRiskPilotCustomerConflict = "COMMQUOTE1-CUST-CONFLICT";

    public static readonly DateTimeOffset CommercialQuotationRiskComparisonPilotReferenceWindowStartUtc =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset CommercialQuotationRiskComparisonPilotReferenceWindowEndUtc =
        new(2026, 6, 30, 23, 59, 59, TimeSpan.Zero);

    public static readonly DateTimeOffset CommercialQuotationRiskComparisonPilotCurrentWindowStartUtc =
        new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset CommercialQuotationRiskComparisonPilotCurrentWindowEndUtc =
        new(2026, 12, 31, 23, 59, 59, TimeSpan.Zero);

    public static readonly Guid CommercialQuotationRiskComparisonPilotStableTrendRunId =
        Guid.Parse("bb000001-02c1-4000-8000-0000000002c1");

    public static readonly Guid CommercialQuotationRiskComparisonPilotRiskIncreaseRunId =
        Guid.Parse("bb000001-02c2-4000-8000-0000000002c2");

    public static readonly Guid CommercialQuotationRiskComparisonPilotRiskDecreaseRunId =
        Guid.Parse("bb000001-02c3-4000-8000-0000000002c3");

    public static readonly Guid CommercialQuotationRiskComparisonPilotExpiryConcentrationRunId =
        Guid.Parse("bb000001-02c4-4000-8000-0000000002c4");

    public static readonly Guid CommercialQuotationRiskComparisonPilotPaymentConcentrationRunId =
        Guid.Parse("bb000001-02c5-4000-8000-0000000002c5");

    public static readonly Guid CommercialQuotationRiskComparisonPilotCustomerPositionRunId =
        Guid.Parse("bb000001-02c6-4000-8000-0000000002c6");

    public static readonly Guid CommercialQuotationRiskComparisonPilotCustomerTieRunId =
        Guid.Parse("bb000001-02c7-4000-8000-0000000002c7");

    public static readonly Guid CommercialQuotationRiskComparisonPilotInsufficientRunId =
        Guid.Parse("bb000001-02c8-4000-8000-0000000002c8");

    public static readonly Guid CommercialQuotationRiskComparisonPilotSourceConflictRunId =
        Guid.Parse("bb000001-02c9-4000-8000-0000000002c9");

    public static readonly IReadOnlySet<Guid> CommercialQuotationRiskComparisonPilotRunIds = new HashSet<Guid>
    {
        CommercialQuotationRiskComparisonPilotStableTrendRunId,
        CommercialQuotationRiskComparisonPilotRiskIncreaseRunId,
        CommercialQuotationRiskComparisonPilotRiskDecreaseRunId,
        CommercialQuotationRiskComparisonPilotExpiryConcentrationRunId,
        CommercialQuotationRiskComparisonPilotPaymentConcentrationRunId,
        CommercialQuotationRiskComparisonPilotCustomerPositionRunId,
        CommercialQuotationRiskComparisonPilotCustomerTieRunId,
        CommercialQuotationRiskComparisonPilotInsufficientRunId,
        CommercialQuotationRiskComparisonPilotSourceConflictRunId,
    };

    public const string CommercialQuotationRiskComparisonPilotCustomerPrimary = "COMMQUOTE2-CUST-PRIMARY";
    public const string CommercialQuotationRiskComparisonPilotCustomerSecondary = "COMMQUOTE2-CUST-SECONDARY";
    public const string CommercialQuotationRiskComparisonPilotCustomerTieA = "COMMQUOTE2-CUST-TIE-A";
    public const string CommercialQuotationRiskComparisonPilotCustomerTieB = "COMMQUOTE2-CUST-TIE-B";

    public static readonly Guid CommercialQuotationRiskComparisonPilotMaterialStableTrendId =
        Guid.Parse("bb000001-02d1-4000-8000-0000000002d1");

    public static readonly Guid CommercialQuotationRiskComparisonPilotMaterialRiskIncreaseId =
        Guid.Parse("bb000001-02d2-4000-8000-0000000002d2");

    public static readonly Guid CommercialQuotationRiskComparisonPilotMaterialRiskDecreaseId =
        Guid.Parse("bb000001-02d3-4000-8000-0000000002d3");

    public static readonly Guid CommercialQuotationRiskComparisonPilotMaterialExpiryConcentrationId =
        Guid.Parse("bb000001-02d4-4000-8000-0000000002d4");

    public static readonly Guid CommercialQuotationRiskComparisonPilotMaterialPaymentConcentrationId =
        Guid.Parse("bb000001-02d5-4000-8000-0000000002d5");

    public static readonly Guid CommercialQuotationRiskComparisonPilotMaterialCustomerPositionId =
        Guid.Parse("bb000001-02d6-4000-8000-0000000002d6");

    public static readonly Guid CommercialQuotationRiskComparisonPilotMaterialCustomerTieId =
        Guid.Parse("bb000001-02d7-4000-8000-0000000002d7");

    public static readonly Guid CommercialQuotationRiskComparisonPilotMaterialInsufficientId =
        Guid.Parse("bb000001-02d8-4000-8000-0000000002d8");

    public static readonly Guid CommercialQuotationRiskComparisonPilotMaterialSourceConflictId =
        Guid.Parse("bb000001-02d9-4000-8000-0000000002d9");

    public static readonly Guid CommercialProfitabilityPilotProfileId =
        Guid.Parse("bb000001-0310-4000-8000-000000000310");

    public const string CommercialProfitabilityPilotProfileCode = "COMMPROFIT1-PILOT";

    public static readonly DateTimeOffset CommercialProfitabilityPilotSeedTimeUtc =
        new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    public static readonly Guid CommercialProfitabilityPilotAssessmentWithinThresholdId =
        Guid.Parse("bb000001-0391-4000-8000-000000000391");

    public static readonly Guid CommercialProfitabilityPilotAssessmentExactThresholdId =
        Guid.Parse("bb000001-0392-4000-8000-000000000392");

    public static readonly Guid CommercialProfitabilityPilotAssessmentBelowThresholdId =
        Guid.Parse("bb000001-0393-4000-8000-000000000393");

    public static readonly Guid CommercialProfitabilityPilotAssessmentZeroContributionId =
        Guid.Parse("bb000001-0394-4000-8000-000000000394");

    public static readonly Guid CommercialProfitabilityPilotAssessmentNegativeContributionId =
        Guid.Parse("bb000001-0395-4000-8000-000000000395");

    public static readonly Guid CommercialProfitabilityPilotAssessmentMissingCostId =
        Guid.Parse("bb000001-0396-4000-8000-000000000396");

    public static readonly Guid CommercialProfitabilityPilotAssessmentSourceConflictId =
        Guid.Parse("bb000001-0397-4000-8000-000000000397");

    public static readonly Guid CommercialProfitabilityPilotAssessmentIncompatibleId =
        Guid.Parse("bb000001-0398-4000-8000-000000000398");

    public static readonly Guid CommercialProfitabilityPilotAssessmentStaleEvidenceId =
        Guid.Parse("bb000001-0399-4000-8000-000000000399");

    public static readonly Guid CommercialProfitabilityPilotAssessmentMarginMarkupId =
        Guid.Parse("bb000001-039a-4000-8000-00000000039a");

    public static readonly IReadOnlySet<Guid> CommercialProfitabilityPilotAssessmentIds = new HashSet<Guid>
    {
        CommercialProfitabilityPilotAssessmentWithinThresholdId,
        CommercialProfitabilityPilotAssessmentExactThresholdId,
        CommercialProfitabilityPilotAssessmentBelowThresholdId,
        CommercialProfitabilityPilotAssessmentZeroContributionId,
        CommercialProfitabilityPilotAssessmentNegativeContributionId,
        CommercialProfitabilityPilotAssessmentMissingCostId,
        CommercialProfitabilityPilotAssessmentSourceConflictId,
        CommercialProfitabilityPilotAssessmentIncompatibleId,
        CommercialProfitabilityPilotAssessmentStaleEvidenceId,
        CommercialProfitabilityPilotAssessmentMarginMarkupId,
    };

    public static readonly DateTimeOffset CommercialProfitabilityComparisonPilotReferenceWindowStartUtc =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset CommercialProfitabilityComparisonPilotReferenceWindowEndUtc =
        new(2026, 6, 30, 23, 59, 59, TimeSpan.Zero);

    public static readonly DateTimeOffset CommercialProfitabilityComparisonPilotCurrentWindowStartUtc =
        new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset CommercialProfitabilityComparisonPilotCurrentWindowEndUtc =
        new(2026, 12, 31, 23, 59, 59, TimeSpan.Zero);

    public static readonly DateTimeOffset CommercialProfitabilityComparisonPilotPositionWindowStartUtc =
        new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset CommercialProfitabilityComparisonPilotPositionWindowEndUtc =
        new(2026, 12, 31, 23, 59, 59, TimeSpan.Zero);

    public const string CommercialProfitabilityComparisonPilotCustomerPrimary = "COMMPROFIT2-CUST-PRIMARY";
    public const string CommercialProfitabilityComparisonPilotCustomerSecondary = "COMMPROFIT2-CUST-SECONDARY";
    public const string CommercialProfitabilityComparisonPilotCustomerTieA = "COMMPROFIT2-CUST-TIE-A";
    public const string CommercialProfitabilityComparisonPilotCustomerTieB = "COMMPROFIT2-CUST-TIE-B";
    public const string CommercialProfitabilityComparisonPilotCustomerMaterialPosition = "COMMPROFIT2-CUST-MAT-POS";

    public static readonly Guid CommercialProfitabilityComparisonPilotStableRunId =
        Guid.Parse("bb000001-03b0-4000-8000-0000000003b0");

    public static readonly Guid CommercialProfitabilityComparisonPilotDeteriorationRunId =
        Guid.Parse("bb000001-03b1-4000-8000-0000000003b1");

    public static readonly Guid CommercialProfitabilityComparisonPilotImprovementRunId =
        Guid.Parse("bb000001-03b2-4000-8000-0000000003b2");

    public static readonly Guid CommercialProfitabilityComparisonPilotExactThresholdRunId =
        Guid.Parse("bb000001-03b3-4000-8000-0000000003b3");

    public static readonly Guid CommercialProfitabilityComparisonPilotDirectMaterialRunId =
        Guid.Parse("bb000001-03b4-4000-8000-0000000003b4");

    public static readonly Guid CommercialProfitabilityComparisonPilotEnergyRunId =
        Guid.Parse("bb000001-03b5-4000-8000-0000000003b5");

    public static readonly Guid CommercialProfitabilityComparisonPilotCustomerPositionRunId =
        Guid.Parse("bb000001-03b6-4000-8000-0000000003b6");

    public static readonly Guid CommercialProfitabilityComparisonPilotMaterialPositionRunId =
        Guid.Parse("bb000001-03b7-4000-8000-0000000003b7");

    public static readonly Guid CommercialProfitabilityComparisonPilotTieRunId =
        Guid.Parse("bb000001-03b8-4000-8000-0000000003b8");

    public static readonly Guid CommercialProfitabilityComparisonPilotInsufficientRunId =
        Guid.Parse("bb000001-03b9-4000-8000-0000000003b9");

    public static readonly Guid CommercialProfitabilityComparisonPilotIncompleteCoverageRunId =
        Guid.Parse("bb000001-03ba-4000-8000-0000000003ba");

    public static readonly Guid CommercialProfitabilityComparisonPilotSourceConflictRunId =
        Guid.Parse("bb000001-03bb-4000-8000-0000000003bb");

    public static readonly IReadOnlySet<Guid> CommercialProfitabilityComparisonPilotRunIds = new HashSet<Guid>
    {
        CommercialProfitabilityComparisonPilotStableRunId,
        CommercialProfitabilityComparisonPilotDeteriorationRunId,
        CommercialProfitabilityComparisonPilotImprovementRunId,
        CommercialProfitabilityComparisonPilotExactThresholdRunId,
        CommercialProfitabilityComparisonPilotDirectMaterialRunId,
        CommercialProfitabilityComparisonPilotEnergyRunId,
        CommercialProfitabilityComparisonPilotCustomerPositionRunId,
        CommercialProfitabilityComparisonPilotMaterialPositionRunId,
        CommercialProfitabilityComparisonPilotTieRunId,
        CommercialProfitabilityComparisonPilotInsufficientRunId,
        CommercialProfitabilityComparisonPilotIncompleteCoverageRunId,
        CommercialProfitabilityComparisonPilotSourceConflictRunId,
    };

    public static readonly Guid CommercialProfitabilityComparisonPilotMaterialStableId =
        Guid.Parse("bb000001-03c0-4000-8000-0000000003c0");

    public static readonly Guid CommercialProfitabilityComparisonPilotMaterialDeteriorationId =
        Guid.Parse("bb000001-03c1-4000-8000-0000000003c1");

    public static readonly Guid CommercialProfitabilityComparisonPilotMaterialImprovementId =
        Guid.Parse("bb000001-03c2-4000-8000-0000000003c2");

    public static readonly Guid CommercialProfitabilityComparisonPilotMaterialExactThresholdId =
        Guid.Parse("bb000001-03c3-4000-8000-0000000003c3");

    public static readonly Guid CommercialProfitabilityComparisonPilotMaterialDirectMaterialId =
        Guid.Parse("bb000001-03c4-4000-8000-0000000003c4");

    public static readonly Guid CommercialProfitabilityComparisonPilotMaterialEnergyId =
        Guid.Parse("bb000001-03c5-4000-8000-0000000003c5");

    public static readonly Guid CommercialProfitabilityComparisonPilotMaterialCustomerPositionId =
        Guid.Parse("bb000001-03c6-4000-8000-0000000003c6");

    public static readonly Guid CommercialProfitabilityComparisonPilotMaterialPositionAId =
        Guid.Parse("bb000001-03c7-4000-8000-0000000003c7");

    public static readonly Guid CommercialProfitabilityComparisonPilotMaterialPositionBId =
        Guid.Parse("bb000001-03c8-4000-8000-0000000003c8");

    public static readonly Guid CommercialProfitabilityComparisonPilotMaterialTieId =
        Guid.Parse("bb000001-03c9-4000-8000-0000000003c9");

    public static readonly Guid CommercialProfitabilityComparisonPilotMaterialInsufficientId =
        Guid.Parse("bb000001-03ca-4000-8000-0000000003ca");

    public static readonly Guid CommercialProfitabilityComparisonPilotMaterialIncompleteId =
        Guid.Parse("bb000001-03cb-4000-8000-0000000003cb");

    public static readonly Guid CommercialProfitabilityComparisonPilotMaterialSourceConflictId =
        Guid.Parse("bb000001-03cc-4000-8000-0000000003cc");
}
