namespace CrevaFactOS.Persistence.DigitalWorkforce;

public static class DigitalWorkforceToolCapabilities
{
    public const string ProductionRead = "production.read";
    public const string ProductionPlan = "production.plan";
    public const string MaintenanceRead = "maintenance.read";
    public const string MaintenanceCreate = "maintenance.create";
    public const string MaterialRead = "material.read";
    public const string ProcurementDraft = "procurement.draft";
    public const string QualityRead = "quality.read";
    public const string QualityManage = "quality.manage";
    public const string MoldRead = "mold.read";
    public const string MoldManage = "mold.manage";
    public const string IotRead = "iot.read";
    public const string TaskCenterCreate = "taskcenter.create";
    public const string TaskCenterUpdate = "taskcenter.update";
    public const string TeamsSend = "teams.send";
    public const string EmailDraft = "email.draft";
    public const string WhatsappDraft = "whatsapp.draft";
    public const string IntegrationRead = "integration.read";

    public static readonly string[] MachineSafetyProhibitedActions =
    [
        "plc.write",
        "machine.command",
        "safety.override",
        "estop.override",
        "generic.machine.command",
    ];

    public static readonly string[] AllCapabilities =
    [
        ProductionRead, ProductionPlan, MaintenanceRead, MaintenanceCreate,
        MaterialRead, ProcurementDraft, QualityRead, QualityManage,
        MoldRead, MoldManage, IotRead, TaskCenterCreate, TaskCenterUpdate,
        TeamsSend, EmailDraft, WhatsappDraft, IntegrationRead,
    ];
}
