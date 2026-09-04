using CrevaFactOS.Persistence.AiTasks;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Edge;
using CrevaFactOS.Persistence.Energy;
using CrevaFactOS.Persistence.IotPlc;
using CrevaFactOS.Persistence.OperationalRisk;
using CrevaFactOS.Persistence.CommercialIntelligence;
using CrevaFactOS.Persistence.SaasBilling;
using CrevaFactOS.Persistence.SaasCommercial;
using CrevaFactOS.Persistence.SaasSubscription;
using CrevaFactOS.Persistence.ShiftAssignment;
using CrevaFactOS.Persistence.ShiftRisk;
using CrevaFactOS.Persistence.ShiftWorkforce;
using CrevaFactOS.Persistence.MarketRates;
using CrevaFactOS.Persistence.Teams;

namespace CrevaFactOS.Persistence.AdminAccess;

public sealed record AdminAccessRoleTemplate(
    Guid RoleId,
    string RoleCode,
    string DisplayNameTr,
    string DescriptionTr,
    bool IsProtected,
    IReadOnlyList<string> PermissionKeys);

public static class AdminAccessRoleTemplates
{
    public static IReadOnlyList<AdminAccessRoleTemplate> DevelopmentTemplates { get; } =
    [
        new(
            DevelopmentAdminAccessIds.TenantOwnerRoleId,
            AdminAccessRoleCodes.TenantOwner,
            "Kiracı sahibi",
            "Korumalı kiracı sahibi. Platform yetkisi vermez.",
            true,
            CustomerCatalogKeys.AllCustomerKeys),
        new(
            DevelopmentAdminAccessIds.TenantAdminRoleId,
            AdminAccessRoleCodes.TenantAdmin,
            "Kiracı yöneticisi",
            "Kiracı yönetimi. Kiracı sahibi atayamaz ve platform yetkisi veremez.",
            false,
            CustomerCatalogKeys.AllCustomerKeys.Where(key => key != AdminAccessPermissionKeys.GrantOwner).ToArray()),
        new(
            DevelopmentAdminAccessIds.FacilityManagerRoleId,
            AdminAccessRoleCodes.FacilityManager,
            "Tesis yöneticisi",
            "Tesis görünürlüğü ve operasyonel okuma.",
            false,
            [
                EdgePermissionKeys.Read, MaterialPermissionKeys.Read, MaintenancePermissionKeys.Read,
                MoldPermissionKeys.Read, ProcessOeePermissionKeys.Read, ShiftWorkforcePermissionKeys.Read,
            ]),
        new(
            DevelopmentAdminAccessIds.ProductionManagerRoleId,
            AdminAccessRoleCodes.ProductionManager,
            "Üretim yöneticisi",
            "Üretim planlama ve yürütme. Faturalama veya makine mutasyonu otomatik verilmez.",
            false,
            [
                ProductionPlanningPermissionKeys.Read, ProductionPlanningPermissionKeys.Write,
                ProductionPlanningPermissionKeys.Review, ProductionPlanningPermissionKeys.Approve,
                ProductionPlanningPermissionKeys.Schedule, ProductionExecutionPermissionKeys.Read,
                ProductionExecutionPermissionKeys.Write, EdgePermissionKeys.Read,
            ]),
        new(
            DevelopmentAdminAccessIds.PlannerRoleId,
            AdminAccessRoleCodes.Planner,
            "Planlamacı",
            "Planlama okuma ve yazma. Onay ve makine mutasyonu otomatik verilmez.",
            false,
            [
                ProductionPlanningPermissionKeys.Read, ProductionPlanningPermissionKeys.Write,
                ProductionPlanningPermissionKeys.Review, ProductionPlanningPermissionKeys.Schedule,
            ]),
        new(
            DevelopmentAdminAccessIds.ShiftSupervisorRoleId,
            AdminAccessRoleCodes.ShiftSupervisor,
            "Vardiya amiri",
            "Vardiya ve atama yönetimi.",
            false,
            [
                ShiftWorkforcePermissionKeys.Read, ShiftWorkforcePermissionKeys.Manage,
                ShiftAssignmentPermissionKeys.Read, ShiftAssignmentPermissionKeys.Manage,
                ShiftRiskPermissionKeys.Read, ProductionExecutionPermissionKeys.Read,
            ]),
        new(
            DevelopmentAdminAccessIds.MachineResponsibleRoleId,
            AdminAccessRoleCodes.MachineResponsible,
            "Makine sorumlusu",
            "Yalnızca Makineler başlangıç şablonu.",
            false,
            [EdgePermissionKeys.Read]),
        new(
            DevelopmentAdminAccessIds.OperatorRoleId,
            AdminAccessRoleCodes.Operator,
            "Operatör",
            "Üretim yürütme kaydı. Menü şablonu Makineler/Malzemeler/Bakım vermez.",
            false,
            [
                ProductionExecutionPermissionKeys.Read, ProductionExecutionPermissionKeys.Record,
                ProductionExecutionPermissionKeys.Start, ProductionExecutionPermissionKeys.Pause,
            ]),
        new(
            DevelopmentAdminAccessIds.MaintenanceTeamRoleId,
            AdminAccessRoleCodes.MaintenanceTeam,
            "Bakım ekibi",
            "Yalnızca Bakım başlangıç şablonu.",
            false,
            [MaintenancePermissionKeys.Read, MaintenancePermissionKeys.Execute]),
        new(
            DevelopmentAdminAccessIds.QualityTeamRoleId,
            AdminAccessRoleCodes.QualityTeam,
            "Kalite ekibi",
            "Kalite okuma ve denetim.",
            false,
            [QualityPermissionKeys.Read, QualityPermissionKeys.Inspect, QualityPermissionKeys.Manage]),
        new(
            DevelopmentAdminAccessIds.MaterialWarehouseRoleId,
            AdminAccessRoleCodes.MaterialWarehouse,
            "Malzeme depo",
            "Malzeme okuma ve yönetim.",
            false,
            [MaterialPermissionKeys.Read, MaterialPermissionKeys.Manage]),
        new(
            DevelopmentAdminAccessIds.SalesOrderRoleId,
            AdminAccessRoleCodes.SalesOrder,
            "Satış sipariş",
            "Yalnızca Malzemeler başlangıç şablonu.",
            false,
            [MaterialPermissionKeys.Read]),
        new(
            DevelopmentAdminAccessIds.PurchasingRoleId,
            AdminAccessRoleCodes.Purchasing,
            "Satın alma",
            "Malzeme okuma.",
            false,
            [MaterialPermissionKeys.Read]),
        new(
            DevelopmentAdminAccessIds.BillingManagerRoleId,
            AdminAccessRoleCodes.BillingManager,
            "Faturalama yöneticisi",
            "Ticari katalog okuma. Üretim veya makine mutasyonu otomatik verilmez.",
            false,
            [SaasCommercialPermissionKeys.Read]),
        new(
            DevelopmentAdminAccessIds.ReadOnlyAuditorRoleId,
            AdminAccessRoleCodes.ReadOnlyAuditor,
            "Salt okunur denetçi",
            "Yalnızca okuma. Yazma, onay ve ERP geri yazma yoktur.",
            false,
            CustomerCatalogKeys.ReadOnlyKeys),
    ];

    public static IReadOnlySet<string> WriteLikeKeys { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        MaterialPermissionKeys.Manage, MoldPermissionKeys.Manage, MaintenancePermissionKeys.Manage,
        MaintenancePermissionKeys.Execute, EdgePermissionKeys.Manage, EdgePermissionKeys.Ingest,
        ProcessOeePermissionKeys.Manage, ShiftWorkforcePermissionKeys.Manage,
        ShiftAssignmentPermissionKeys.Manage, ShiftRiskPermissionKeys.Manage,
        EnergyPermissionKeys.Manage,
        OperationalRiskPermissionKeys.Manage,
        CommercialIntelligencePermissionKeys.Manage,
        AiTaskPermissionKeys.Create, AiTaskPermissionKeys.Review,
        TeamsPermissionKeys.Write, TeamsPermissionKeys.Manage,
        ProductionPlanningPermissionKeys.Write, ProductionPlanningPermissionKeys.Approve,
        ProductionExecutionPermissionKeys.Write, QualityPermissionKeys.Manage,
        ErpPermissionKeys.Manage, ErpPermissionKeys.WritebackRequest, ErpPermissionKeys.WritebackApprove,
        ErpPermissionKeys.WritebackExecute, AdminAccessPermissionKeys.Manage, AdminAccessPermissionKeys.GrantOwner,
    };
}

internal static class CustomerCatalogKeys
{
    public static readonly string[] ReadOnlyKeys =
    [
        AdminAccessPermissionKeys.Read,
        MaterialPermissionKeys.Read, MoldPermissionKeys.Read, MaintenancePermissionKeys.Read,
        EdgePermissionKeys.Read, ProcessOeePermissionKeys.Read, ShiftWorkforcePermissionKeys.Read,
        ShiftAssignmentPermissionKeys.Read, ShiftRiskPermissionKeys.Read,
        ProductionPlanningPermissionKeys.Read, ProductionExecutionPermissionKeys.Read,
        QualityPermissionKeys.Read, ExportPermissionKeys.Read, ErpPermissionKeys.Read,
        EnergyPermissionKeys.Read,
        IotPlcPermissionKeys.Read,
        OperationalRiskPermissionKeys.Read,
        CommercialIntelligencePermissionKeys.Read,
        AiTaskPermissionKeys.Read,
        TeamsPermissionKeys.Read,
        MarketRatesPermissionKeys.Read,
        SaasCommercialPermissionKeys.Read,
    ];

    public static readonly string[] AllCustomerKeys =
    [
        AdminAccessPermissionKeys.Read, AdminAccessPermissionKeys.Manage, AdminAccessPermissionKeys.GrantOwner,
        MaterialPermissionKeys.Read, MaterialPermissionKeys.Manage,
        MoldPermissionKeys.Read, MoldPermissionKeys.Manage,
        MaintenancePermissionKeys.Read, MaintenancePermissionKeys.Manage, MaintenancePermissionKeys.Execute,
        EdgePermissionKeys.Read, EdgePermissionKeys.Manage, EdgePermissionKeys.Ingest,
        ProcessOeePermissionKeys.Read, ProcessOeePermissionKeys.Manage,
        ShiftWorkforcePermissionKeys.Read, ShiftWorkforcePermissionKeys.Manage,
        ShiftAssignmentPermissionKeys.Read, ShiftAssignmentPermissionKeys.Manage,
        ShiftRiskPermissionKeys.Read, ShiftRiskPermissionKeys.Manage,
        ProductionPlanningPermissionKeys.Read, ProductionPlanningPermissionKeys.Write,
        ProductionPlanningPermissionKeys.Review, ProductionPlanningPermissionKeys.Approve,
        ProductionPlanningPermissionKeys.Schedule, ProductionPlanningPermissionKeys.Transfer,
        ProductionPlanningPermissionKeys.Handoff,
        ProductionExecutionPermissionKeys.Read, ProductionExecutionPermissionKeys.Write,
        ProductionExecutionPermissionKeys.Start, ProductionExecutionPermissionKeys.Pause,
        ProductionExecutionPermissionKeys.Complete, ProductionExecutionPermissionKeys.Record,
        QualityPermissionKeys.Read, QualityPermissionKeys.Manage, QualityPermissionKeys.Inspect,
        ExportPermissionKeys.Read, ExportPermissionKeys.Write,
        ExportPermissionKeys.DocumentManage, ExportPermissionKeys.DocumentVerify,
        ExportPermissionKeys.PackingManage, ExportPermissionKeys.PackingVerify,
        ExportPermissionKeys.ShipmentManage, ExportPermissionKeys.BrokerManage,
        ExportPermissionKeys.BrokerHandoff, ExportPermissionKeys.ComplianceRead,
        ExportPermissionKeys.ComplianceManage, ExportPermissionKeys.ComplianceVerify,
        ExportPermissionKeys.CountryResearchRead, ExportPermissionKeys.CountryResearchManage,
        ExportPermissionKeys.CountryResearchVerify, ExportPermissionKeys.AiRead,
        ExportPermissionKeys.AiRun, ExportPermissionKeys.AiReview,
        EnergyPermissionKeys.Read, EnergyPermissionKeys.Manage,
        IotPlcPermissionKeys.Read, IotPlcPermissionKeys.Manage,
        OperationalRiskPermissionKeys.Read, OperationalRiskPermissionKeys.Manage,
        CommercialIntelligencePermissionKeys.Read, CommercialIntelligencePermissionKeys.Manage,
        AiTaskPermissionKeys.Read, AiTaskPermissionKeys.Create, AiTaskPermissionKeys.Review,
        AiTaskPermissionKeys.Assign, AiTaskPermissionKeys.Acknowledge, AiTaskPermissionKeys.Escalate,
        AiTaskPermissionKeys.Manage, AiTaskPermissionKeys.SlaManage, AiTaskPermissionKeys.EvidenceManage,
        AiTaskPermissionKeys.ClosureRequest, AiTaskPermissionKeys.ClosureApprove, AiTaskPermissionKeys.Reopen,
        AiTaskPermissionKeys.AutomationRead, AiTaskPermissionKeys.AutomationManage, AiTaskPermissionKeys.AutomationSimulate,
        AiTaskPermissionKeys.AiRead, AiTaskPermissionKeys.AiAnalyze, AiTaskPermissionKeys.AiFeedback,
        AiTaskPermissionKeys.PlaybookRead, AiTaskPermissionKeys.PlaybookManage,
        AiTaskPermissionKeys.PlaybookRun, AiTaskPermissionKeys.PlaybookOverride,
        TeamsPermissionKeys.Read, TeamsPermissionKeys.Write, TeamsPermissionKeys.Manage,
        MarketRatesPermissionKeys.Read,
        TeamsPermissionKeys.ApprovalsCreate, TeamsPermissionKeys.ApprovalsReview, TeamsPermissionKeys.ApprovalsManage,
        TeamsPermissionKeys.HandoverRead, TeamsPermissionKeys.HandoverCreate, TeamsPermissionKeys.HandoverAcknowledge, TeamsPermissionKeys.HandoverManage,
        TeamsPermissionKeys.DirectRead, TeamsPermissionKeys.DirectWrite, TeamsPermissionKeys.DirectManage,
        TeamsPermissionKeys.NotificationsRead,
        ShiftHandoverPermissionKeys.Read, ShiftHandoverPermissionKeys.Manage, ShiftHandoverPermissionKeys.Acknowledge,
        ErpPermissionKeys.Read, ErpPermissionKeys.Manage,
        ErpPermissionKeys.WritebackRequest, ErpPermissionKeys.WritebackApprove, ErpPermissionKeys.WritebackExecute,
        SaasCommercialPermissionKeys.Read,
        SaasSubscriptionPermissionKeys.Read, SaasSubscriptionPermissionKeys.Manage,
        SaasBillingPermissionKeys.Read, SaasBillingPermissionKeys.Manage,
    ];
}
