namespace CrevaFactOS.Persistence.AdminAccess;

public static class DevelopmentAdminAccessIds
{
    public static readonly Guid TenantOwnerRoleId = Guid.Parse("bb000003-0101-4000-8000-000000000101");
    public static readonly Guid TenantAdminRoleId = Guid.Parse("bb000003-0102-4000-8000-000000000102");
    public static readonly Guid FacilityManagerRoleId = Guid.Parse("bb000003-0103-4000-8000-000000000103");
    public static readonly Guid ProductionManagerRoleId = Guid.Parse("bb000003-0104-4000-8000-000000000104");
    public static readonly Guid PlannerRoleId = Guid.Parse("bb000003-0105-4000-8000-000000000105");
    public static readonly Guid ShiftSupervisorRoleId = Guid.Parse("bb000003-0106-4000-8000-000000000106");
    public static readonly Guid MachineResponsibleRoleId = Guid.Parse("bb000003-0107-4000-8000-000000000107");
    public static readonly Guid OperatorRoleId = Guid.Parse("bb000003-0108-4000-8000-000000000108");
    public static readonly Guid MaintenanceTeamRoleId = Guid.Parse("bb000003-0109-4000-8000-000000000109");
    public static readonly Guid QualityTeamRoleId = Guid.Parse("bb000003-010a-4000-8000-00000000010a");
    public static readonly Guid MaterialWarehouseRoleId = Guid.Parse("bb000003-010b-4000-8000-00000000010b");
    public static readonly Guid SalesOrderRoleId = Guid.Parse("bb000003-010c-4000-8000-00000000010c");
    public static readonly Guid PurchasingRoleId = Guid.Parse("bb000003-010d-4000-8000-00000000010d");
    public static readonly Guid BillingManagerRoleId = Guid.Parse("bb000003-010e-4000-8000-00000000010e");
    public static readonly Guid ReadOnlyAuditorRoleId = Guid.Parse("bb000003-010f-4000-8000-00000000010f");

    public static readonly Guid AdminAccessReadPermissionId = Guid.Parse("bb000003-0001-4000-8000-000000000001");
    public static readonly Guid AdminAccessManagePermissionId = Guid.Parse("bb000003-0002-4000-8000-000000000002");
    public static readonly Guid AdminAccessGrantOwnerPermissionId = Guid.Parse("bb000003-0003-4000-8000-000000000003");

    public static readonly Guid MachineResponsibleAccountId = Guid.Parse("bb000003-0201-4000-8000-000000000201");
    public static readonly Guid MachineResponsibleIdentitySubjectId = Guid.Parse("bb000003-0202-4000-8000-000000000202");
    public static readonly Guid MachineResponsibleMembershipId = Guid.Parse("bb000003-0203-4000-8000-000000000203");

    public static readonly Guid MaterialsSalesAccountId = Guid.Parse("bb000003-0211-4000-8000-000000000211");
    public static readonly Guid MaterialsSalesIdentitySubjectId = Guid.Parse("bb000003-0212-4000-8000-000000000212");
    public static readonly Guid MaterialsSalesMembershipId = Guid.Parse("bb000003-0213-4000-8000-000000000213");

    public static readonly Guid MaintenanceTeamAccountId = Guid.Parse("bb000003-0221-4000-8000-000000000221");
    public static readonly Guid MaintenanceTeamIdentitySubjectId = Guid.Parse("bb000003-0222-4000-8000-000000000222");
    public static readonly Guid MaintenanceTeamMembershipId = Guid.Parse("bb000003-0223-4000-8000-000000000223");

    public static readonly Guid TenantOwnerFacilityMembershipId =
        Guid.Parse("bb000003-0231-4000-8000-000000000231");
}
