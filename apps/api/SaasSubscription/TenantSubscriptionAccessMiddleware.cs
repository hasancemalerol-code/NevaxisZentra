using NevaxisZentra.Persistence.SaasSubscription;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Api.SaasSubscription;

public sealed class TenantSubscriptionAccessMiddleware(RequestDelegate next)
{
    private static readonly string[] RecoveryApiPrefixes =
    [
        "/api/saas-subscription",
        "/api/saas-subscription/access-status",
        "/api/saas-billing",
        "/api/admin-access",
        "/api/saas-commercial",
        "/api/identity/me",
        "/api/identity/sessions",
        "/api/identity/sign-out",
        "/api/identity/sign-out-all",
        "/api/identity/mfa/enroll",
    ];

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContextAccessor tenantContextAccessor,
        ITenantEntitlementEvaluator entitlementEvaluator)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (IsExemptPath(path))
        {
            await next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true || !tenantContextAccessor.IsAvailable)
        {
            await next(context);
            return;
        }

        if (IsRecoveryApiPath(path))
        {
            await next(context);
            return;
        }

        var paidAccess = await entitlementEvaluator.EvaluatePaidApplicationAccessAsync(context.RequestAborted);
        if (!paidAccess.Allowed)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                failureCode = paidAccess.ReasonCode,
                explanationKey = paidAccess.ExplanationKey,
                subscriptionStatus = paidAccess.SubscriptionStatus,
                graceState = paidAccess.GraceState,
                evaluatedAtUtc = paidAccess.EvaluatedAtUtc,
            });
            return;
        }

        var modulePath = MapApiPathToModuleRoute(path);
        if (modulePath is not null)
        {
            var moduleAccess = await entitlementEvaluator.EvaluateModuleAccessAsync(
                modulePath,
                context.RequestAborted);
            if (!moduleAccess.Allowed)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    failureCode = moduleAccess.ReasonCode,
                    explanationKey = moduleAccess.ExplanationKey,
                    entitlementCode = moduleAccess.EntitlementCode,
                    evaluatedAtUtc = moduleAccess.EvaluatedAtUtc,
                });
                return;
            }
        }

        await next(context);
    }

    private static bool IsExemptPath(string path) =>
        path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/internal/", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/api/identity", StringComparison.OrdinalIgnoreCase);

    private static bool IsRecoveryApiPath(string path) =>
        RecoveryApiPrefixes.Any(prefix =>
            path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    public static string? MapApiPathToModuleRoute(string path)
    {
        if (path.StartsWith("/api/shift-workforce", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/shift-assignment", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/shift-risk", StringComparison.OrdinalIgnoreCase))
        {
            return "/vardiyalar";
        }

        if (path.StartsWith("/api/process-oee", StringComparison.OrdinalIgnoreCase))
        {
            return "/proses-oee";
        }

        if (path.StartsWith("/api/edge", StringComparison.OrdinalIgnoreCase))
        {
            return "/makineler";
        }

        if (path.StartsWith("/api/mold", StringComparison.OrdinalIgnoreCase))
        {
            return "/kaliplar";
        }

        if (path.StartsWith("/api/material", StringComparison.OrdinalIgnoreCase))
        {
            return "/malzemeler";
        }

        if (path.StartsWith("/api/maintenance", StringComparison.OrdinalIgnoreCase))
        {
            return "/bakim";
        }

        if (path.StartsWith("/api/quality", StringComparison.OrdinalIgnoreCase))
        {
            return "/kalite";
        }

        if (path.StartsWith("/api/erp", StringComparison.OrdinalIgnoreCase))
        {
            return "/erp-entegrasyonlari";
        }

        // Shipping AI routes. The web route map already gates these pages; the API must not be reachable around it.
        if (path.StartsWith("/api/ai-task", StringComparison.OrdinalIgnoreCase))
        {
            return "/ai-gorevleri";
        }

        if (path.StartsWith("/api/export", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/ai/usage", StringComparison.OrdinalIgnoreCase))
        {
            return "/ihracat";
        }

        if (path.StartsWith("/api/factory-copilot", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/model-gateway", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/intelligence-runtime", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/throughput-optimizer", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/throughput-intelligence", StringComparison.OrdinalIgnoreCase))
        {
            return "/fabrika";
        }

        if (path.StartsWith("/api/production", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/production-planning", StringComparison.OrdinalIgnoreCase))
        {
            return "/uretim";
        }

        return null;
    }
}
