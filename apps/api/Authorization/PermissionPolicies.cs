using CrevaFactOS.Persistence.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace CrevaFactOS.Api.Authorization;

public static class PermissionPolicies
{
    public const string SystemDiagnosticsRead = "Permission:" + InfrastructurePermissionKeys.SystemDiagnosticsRead;
    public const string TenantContextRead = "Permission:" + InfrastructurePermissionKeys.TenantContextRead;
    public const string AuthorizationDiagnosticsRead = "Permission:" + InfrastructurePermissionKeys.AuthorizationDiagnosticsRead;
    public const string SystemAuditRead = "Permission:" + InfrastructurePermissionKeys.SystemAuditRead;
    public const string SystemAiDecisionWrite = "Permission:" + InfrastructurePermissionKeys.SystemAiDecisionWrite;
    public const string SystemAiDecisionRead = "Permission:" + InfrastructurePermissionKeys.SystemAiDecisionRead;
    public const string SystemOutboxWrite = "Permission:" + InfrastructurePermissionKeys.SystemOutboxWrite;
    public const string SystemOutboxRead = "Permission:" + InfrastructurePermissionKeys.SystemOutboxRead;

    public static void AddInfrastructurePolicies(AuthorizationOptions options)
    {
        Add(options, SystemDiagnosticsRead, InfrastructurePermissionKeys.SystemDiagnosticsRead);
        Add(options, TenantContextRead, InfrastructurePermissionKeys.TenantContextRead);
        Add(options, AuthorizationDiagnosticsRead, InfrastructurePermissionKeys.AuthorizationDiagnosticsRead);
        Add(options, SystemAuditRead, InfrastructurePermissionKeys.SystemAuditRead);
        Add(options, SystemAiDecisionWrite, InfrastructurePermissionKeys.SystemAiDecisionWrite);
        Add(options, SystemAiDecisionRead, InfrastructurePermissionKeys.SystemAiDecisionRead);
        Add(options, SystemOutboxWrite, InfrastructurePermissionKeys.SystemOutboxWrite);
        Add(options, SystemOutboxRead, InfrastructurePermissionKeys.SystemOutboxRead);
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    }

    private static void Add(AuthorizationOptions options, string policyName, string permissionKey) =>
        options.AddPolicy(
            policyName,
            policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(permissionKey)));
}
