using System.Globalization;
using NevaxisZentra.Persistence.AdminAccess;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.SaasCommercial;
using NevaxisZentra.Persistence.SaasSubscription;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Development;

/// <summary>
/// Development Pilot Tenant sales-demo profile: full module entitlements and owner permissions.
/// </summary>
internal static class DevelopmentPilotSalesDemoAccessSeed
{
    private static readonly string[] SalesDemoModuleEntitlementCodes =
    [
        SaasSubscriptionModuleEntitlements.EntitlementCodes.Dashboard,
        SaasSubscriptionModuleEntitlements.EntitlementCodes.Teams,
        SaasSubscriptionModuleEntitlements.EntitlementCodes.Orders,
        SaasSubscriptionModuleEntitlements.EntitlementCodes.Planning,
        SaasSubscriptionModuleEntitlements.EntitlementCodes.Production,
        SaasSubscriptionModuleEntitlements.EntitlementCodes.Export,
        SaasSubscriptionModuleEntitlements.EntitlementCodes.ShiftWorkforce,
        SaasSubscriptionModuleEntitlements.EntitlementCodes.ProcessOee,
        SaasSubscriptionModuleEntitlements.EntitlementCodes.ConnectedMachines,
        SaasSubscriptionModuleEntitlements.EntitlementCodes.Mold,
        SaasSubscriptionModuleEntitlements.EntitlementCodes.Material,
        SaasSubscriptionModuleEntitlements.EntitlementCodes.Maintenance,
        SaasSubscriptionModuleEntitlements.EntitlementCodes.Quality,
        SaasSubscriptionModuleEntitlements.EntitlementCodes.Energy,
        SaasSubscriptionModuleEntitlements.EntitlementCodes.IotPlc,
        SaasSubscriptionModuleEntitlements.EntitlementCodes.Reporting,
        SaasSubscriptionModuleEntitlements.EntitlementCodes.ErpIntegration,
        SaasSubscriptionModuleEntitlements.EntitlementCodes.CommercialIntelligence,
        SaasSubscriptionModuleEntitlements.EntitlementCodes.OperationalRisk,
        SaasSubscriptionModuleEntitlements.EntitlementCodes.AiTaskCenter,
    ];

    internal static async Task EnsureSalesDemoFullAccessAsync(
        NevaxisZentraDbContext dbContext,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        await EnsureSalesDemoModuleEntitlementsAsync(dbContext, seedTimestampUtc, cancellationToken);
        await EnsureTenantOwnerSalesDemoPermissionsAsync(dbContext, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureSalesDemoModuleEntitlementsAsync(
        NevaxisZentraDbContext dbContext,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        foreach (var code in SalesDemoModuleEntitlementCodes)
        {
            var normalized = SaasCommercialValueConversions.NormalizeEntitlementCode(code);
            var definition = await dbContext.SaasCommercialEntitlementDefinitions
                .SingleOrDefaultAsync(d => d.CodeNormalized == normalized, cancellationToken);
            if (definition is null)
            {
                definition = new SaasCommercialEntitlementDefinition
                {
                    Id = CreateDeterministicEntitlementDefinitionId(code),
                    Code = code,
                    CodeNormalized = normalized,
                    DisplayNameTr = $"Satış demo — {code}",
                    DescriptionTr = "Yerel geliştirme pilotu satış demosu için modül erişimi.",
                    ValueKind = SaasCommercialEntitlementValueKinds.ModuleAccess,
                    CreatedAtUtc = seedTimestampUtc,
                };
                dbContext.SaasCommercialEntitlementDefinitions.Add(definition);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await EnsureUnlimitedPlanGrantAsync(
                dbContext,
                DevelopmentPilotIds.SaasCommercialDevMonthlyVersionId,
                definition.Id,
                CreateDeterministicMonthlyGrantId(code),
                cancellationToken);
            await EnsureUnlimitedPlanGrantAsync(
                dbContext,
                DevelopmentPilotIds.SaasCommercialDevAnnualVersionId,
                definition.Id,
                CreateDeterministicAnnualGrantId(code),
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureUnlimitedPlanGrantAsync(
        NevaxisZentraDbContext dbContext,
        Guid planVersionId,
        Guid entitlementDefinitionId,
        Guid grantId,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.SaasCommercialPlanVersionEntitlements
            .SingleOrDefaultAsync(
                grant => grant.PlanVersionId == planVersionId
                    && grant.EntitlementDefinitionId == entitlementDefinitionId,
                cancellationToken);
        if (existing is not null)
        {
            existing.Semantics = SaasCommercialEntitlementSemantics.Unlimited;
            existing.BooleanValue = true;
            existing.IntegerLimitValue = null;
            return;
        }

        var byId = await dbContext.SaasCommercialPlanVersionEntitlements
            .SingleOrDefaultAsync(grant => grant.Id == grantId, cancellationToken);
        if (byId is not null)
        {
            byId.PlanVersionId = planVersionId;
            byId.EntitlementDefinitionId = entitlementDefinitionId;
            byId.Semantics = SaasCommercialEntitlementSemantics.Unlimited;
            byId.BooleanValue = true;
            byId.IntegerLimitValue = null;
            return;
        }

        dbContext.SaasCommercialPlanVersionEntitlements.Add(new SaasCommercialPlanVersionEntitlement
        {
            Id = grantId,
            PlanVersionId = planVersionId,
            EntitlementDefinitionId = entitlementDefinitionId,
            Semantics = SaasCommercialEntitlementSemantics.Unlimited,
            BooleanValue = true,
        });
    }

    private static async Task EnsureTenantOwnerSalesDemoPermissionsAsync(
        NevaxisZentraDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var tenantId = DevelopmentPilotIds.TenantId;
        var roleId = DevelopmentAdminAccessIds.TenantOwnerRoleId;
        foreach (var permissionKey in CustomerCatalogKeys.AllCustomerKeys)
        {
            var permission = await dbContext.PermissionDefinitions
                .SingleOrDefaultAsync(p => p.Key == permissionKey, cancellationToken);
            if (permission is null)
            {
                continue;
            }

            var exists = await dbContext.TenantRolePermissions.IgnoreQueryFilters()
                .AnyAsync(
                    assignment => assignment.TenantId == tenantId
                        && assignment.TenantRoleId == roleId
                        && assignment.PermissionDefinitionId == permission.Id,
                    cancellationToken);
            if (exists)
            {
                continue;
            }

            dbContext.TenantRolePermissions.Add(new TenantRolePermissionAssignment
            {
                TenantId = tenantId,
                TenantRoleId = roleId,
                PermissionDefinitionId = permission.Id,
            });
        }
    }

    private static Guid CreateDeterministicEntitlementDefinitionId(string code) =>
        Guid.Parse($"bb0000a1-{HashCode(code).ToString("x4", CultureInfo.InvariantCulture)}-4000-8000-{HashTail(code)}");

    private static Guid CreateDeterministicMonthlyGrantId(string code) =>
        Guid.Parse($"bb0000a2-{HashCode(code).ToString("x4", CultureInfo.InvariantCulture)}-4000-8000-{HashTail(code)}");

    private static Guid CreateDeterministicAnnualGrantId(string code) =>
        Guid.Parse($"bb0000a3-{HashCode(code).ToString("x4", CultureInfo.InvariantCulture)}-4000-8000-{HashTail(code)}");

    private static int HashCode(string value) =>
        Math.Abs(value.GetHashCode(StringComparison.Ordinal)) & 0xffff;

    private static string HashTail(string value)
    {
        var hash = Math.Abs(value.GetHashCode(StringComparison.Ordinal));
        return hash.ToString("x12", CultureInfo.InvariantCulture).PadLeft(12, '0');
    }
}
