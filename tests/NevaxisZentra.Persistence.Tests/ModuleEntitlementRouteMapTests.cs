using NevaxisZentra.Persistence.SaasSubscription;
using Xunit;

namespace NevaxisZentra.Persistence.Tests;

public sealed class ModuleEntitlementRouteMapTests
{
    [Theory]
    [InlineData("/yonetici-komuta-merkezi")]
    [InlineData("/yonetici-komuta-merkezi/")]
    [InlineData("/Yonetici-Komuta-Merkezi/ozet")]
    [InlineData("/fabrika-karar-merkezi")]
    [InlineData("/operasyon/yonetim-merkezi")]
    public void ShippedCommandCenterRoutesResolveToTheProductionModule(string route)
    {
        Assert.True(SaasSubscriptionModuleEntitlements.TryResolveEntitlementCode(route, out var code));
        Assert.Equal(SaasSubscriptionModuleEntitlements.EntitlementCodes.Production, code);
    }

    // Shipped customer pages that were fail-closed by the API for every tenant ("module" denial) until mapped.
    [Theory]
    [InlineData("/dijital-is-gucu", SaasSubscriptionModuleEntitlements.EntitlementCodes.AiTaskCenter)]
    [InlineData("/dijital-is-gucu/onaylar", SaasSubscriptionModuleEntitlements.EntitlementCodes.AiTaskCenter)]
    [InlineData("/fabrika-zekasi", SaasSubscriptionModuleEntitlements.EntitlementCodes.Production)]
    [InlineData("/fabrika-agi", SaasSubscriptionModuleEntitlements.EntitlementCodes.Planning)]
    [InlineData("/sunum-modu", SaasSubscriptionModuleEntitlements.EntitlementCodes.Dashboard)]
    [InlineData("/golden-demo/gumruk-dosyasi", SaasSubscriptionModuleEntitlements.EntitlementCodes.Dashboard)]
    [InlineData("/intelligence-runtime", SaasSubscriptionModuleEntitlements.EntitlementCodes.Production)]
    [InlineData("/operational-value", SaasSubscriptionModuleEntitlements.EntitlementCodes.Production)]
    [InlineData("/surekli-optimizasyon", SaasSubscriptionModuleEntitlements.EntitlementCodes.Production)]
    [InlineData("/senaryo-kurtarma", SaasSubscriptionModuleEntitlements.EntitlementCodes.Production)]
    public void ShippedWorkspaceRoutesResolveToAModule(string route, string expectedCode)
    {
        Assert.True(SaasSubscriptionModuleEntitlements.TryResolveEntitlementCode(route, out var code));
        Assert.Equal(expectedCode, code);
    }

    [Theory]
    [InlineData("/internal/erisim-tanilama")]
    [InlineData("/ayarlar/veri-aktarim-merkezi")]
    [InlineData("/uygulamalar")]
    [InlineData("/")]
    public void RecoveryAndDiagnosticRoutesAreExemptFromTheLicenceGate(string route)
    {
        Assert.True(SaasSubscriptionModuleEntitlements.IsRecoveryExemptRoute(route));
        Assert.False(SaasSubscriptionModuleEntitlements.TryResolveEntitlementCode(route, out _));
    }

    [Theory]
    [InlineData("/bilinmeyen-modul")]
    [InlineData("/yonetici-komuta-merkezi-yok")]
    public void UnknownRoutesAreStillNotResolved(string route)
    {
        Assert.False(SaasSubscriptionModuleEntitlements.TryResolveEntitlementCode(route, out _));
    }
}
