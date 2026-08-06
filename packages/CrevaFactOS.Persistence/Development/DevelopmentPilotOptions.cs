namespace CrevaFactOS.Persistence.Development;

public sealed class DevelopmentPilotOptions
{
    public const string SectionName = "DevelopmentPilot";

    public bool Enabled { get; set; }

    public string TenantKey { get; set; } = "dev-pilot-tenant";

    public string TenantName { get; set; } = "Development Pilot Tenant";

    public string DevelopmentIssuer { get; set; } = "crevafactos-dev";

    public string DevelopmentSubject { get; set; } = "planning-pilot-user";

    public string RoleKey { get; set; } = "production-planning-reader";

    public string RoleDisplayName { get; set; } = "Production Planning Reader";

    public string FacilityCode { get; set; } = "pilot-facility";

    public string FacilityName { get; set; } = "Pilot Injection Facility";

    public string FacilityTimeZoneId { get; set; } = "Europe/Istanbul";

    public bool ProductionPlanSeedEnabled { get; set; } = true;
}
