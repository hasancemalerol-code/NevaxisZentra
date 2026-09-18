namespace NevaxisZentra.Persistence.FactoryDecisionCenter;

internal static class DevelopmentPilotFactoryDecisionCenterIds
{
    public static readonly Guid RootCaseId =
        Guid.Parse("bb000001-04c3-4000-8000-0000000004c3");

    public static readonly Guid ProductionChildCaseId =
        Guid.Parse("bb000001-04c4-4000-8000-0000000004c4");

    public static readonly Guid MaintenanceChildCaseId =
        Guid.Parse("bb000001-04c5-4000-8000-0000000004c5");

    public static readonly Guid MaterialChildCaseId =
        Guid.Parse("bb000001-04c6-4000-8000-0000000004c6");

    public static readonly Guid RootApprovalId =
        Guid.Parse("bb000001-04c7-4000-8000-0000000004c7");

    public static readonly Guid EventCorrelationId =
        Guid.Parse("bb000001-04c8-4000-8000-0000000004c8");

    public static readonly Guid AiTaskApproveRecoveryPlanId =
        Guid.Parse("bb000001-04c9-4000-8000-0000000004c9");

    public static readonly Guid AiTaskProductionRecoveryId =
        Guid.Parse("bb000001-04ca-4000-8000-0000000004ca");

    public static readonly Guid AiTaskMaterialReservationId =
        Guid.Parse("bb000001-04cb-4000-8000-0000000004cb");

    public static readonly Guid AiTaskDeliveryProjectionId =
        Guid.Parse("bb000001-04cc-4000-8000-0000000004cc");

    public static readonly Guid[] LinkedAiTaskIds =
    [
        AiTaskApproveRecoveryPlanId,
        AiTaskProductionRecoveryId,
        AiTaskMaterialReservationId,
        AiTaskDeliveryProjectionId,
    ];
}
