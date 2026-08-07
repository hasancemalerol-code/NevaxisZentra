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
}
