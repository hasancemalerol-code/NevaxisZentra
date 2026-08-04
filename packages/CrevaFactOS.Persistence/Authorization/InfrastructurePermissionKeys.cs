namespace CrevaFactOS.Persistence.Authorization;

public static class InfrastructurePermissionKeys
{
    public const string SystemDiagnosticsRead = "system.diagnostics.read";
    public const string TenantContextRead = "tenant.context.read";
    public const string AuthorizationDiagnosticsRead = "authorization.diagnostics.read";
    public const string SystemAuditRead = "system.audit.read";
    public const string SystemAiDecisionWrite = "system.ai-decision.write";
    public const string SystemAiDecisionRead = "system.ai-decision.read";
    public const string SystemOutboxWrite = "system.outbox.write";
    public const string SystemOutboxRead = "system.outbox.read";
}
