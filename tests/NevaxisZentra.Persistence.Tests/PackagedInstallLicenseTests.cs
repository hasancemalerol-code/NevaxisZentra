using NevaxisZentra.Persistence.Bootstrap;
using NevaxisZentra.Persistence.SaasCommercial;
using NevaxisZentra.Persistence.SaasSubscription;
using Xunit;

namespace NevaxisZentra.Persistence.Tests;

public sealed class PackagedInstallLicenseTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SubscriptionIsActiveCurrentAndGrantsPaidApplicationAccess()
    {
        var tenantId = Guid.NewGuid();
        var license = PackagedInstallLicense.BuildSubscription(tenantId, Now);

        Assert.Equal(tenantId, license.Subscription.TenantId);
        Assert.Equal(SaasSubscriptionStatuses.Active, license.Subscription.SubscriptionStatus);
        Assert.True(license.Subscription.IsCurrent);
        Assert.Equal(PackagedInstallLicense.PlanVersionId, license.Subscription.CommercialPlanVersionId);
        Assert.Equal(Now.AddYears(PackagedInstallLicenseOptions.DefaultPeriodYears), license.Subscription.CurrentPeriodEndUtc);
        Assert.True(SaasSubscriptionValueConversions.ResolvePaidApplicationAccess(
            license.Subscription.SubscriptionStatus,
            Now.AddYears(5),
            license.Subscription.TrialEndsAtUtc,
            license.Subscription.GraceUntilUtc,
            license.Subscription.CurrentPeriodEndUtc));
    }

    [Fact]
    public void HistoryAndAuditAreTenantScopedAndLinkedToTheSubscription()
    {
        var tenantId = Guid.NewGuid();
        var license = PackagedInstallLicense.BuildSubscription(tenantId, Now);

        Assert.Equal(tenantId, license.Transition.TenantId);
        Assert.Equal(license.Subscription.Id, license.Transition.SubscriptionId);
        Assert.Equal(tenantId, license.AuditEvent.TenantId);
        Assert.Equal(license.Subscription.Id.ToString("D"), license.AuditEvent.EntityReference);
        Assert.Equal(PackagedInstallLicense.ActorReference, license.AuditEvent.ActorReference);
    }

    [Fact]
    public void CatalogIsAPublishedUnsellablePlanVersionWithNoInventedEntitlementsOrPrices()
    {
        var catalog = PackagedInstallLicense.BuildCatalog(Now);

        Assert.Equal(PackagedInstallLicense.PlanId, catalog.Plan.Id);
        Assert.Equal(PackagedInstallLicense.PlanId, catalog.PlanVersion.PlanId);
        Assert.Equal(PackagedInstallLicense.PlanVersionId, catalog.PlanVersion.Id);
        Assert.Equal(SaasCommercialPublicationStatuses.Published, catalog.PlanVersion.PublicationStatus);
        Assert.False(catalog.PlanVersion.Sellable);
        Assert.Equal(2, catalog.AuditEvents.Count);
        Assert.All(catalog.AuditEvents, audit => Assert.Equal(PackagedInstallLicense.ActorReference, audit.ActorSubjectId));
        Assert.True(SaasCommercialValueConversions.IsValidBillingCadence(catalog.PlanVersion.BillingCadence));
        Assert.True(SaasCommercialValueConversions.IsValidCurrencyCode(catalog.PlanVersion.CurrencyCode));
    }

    [Fact]
    public void CatalogIdentifiersAreStableSoTheCatalogInsertIsIdempotent()
    {
        var first = PackagedInstallLicense.BuildCatalog(Now);
        var second = PackagedInstallLicense.BuildCatalog(Now.AddDays(1));

        Assert.Equal(first.Plan.Id, second.Plan.Id);
        Assert.Equal(first.PlanVersion.Id, second.PlanVersion.Id);
        Assert.Equal(first.AuditEvents.Select(item => item.Id), second.AuditEvents.Select(item => item.Id));
    }

    [Fact]
    public void EnrollmentPlanCarriesTheLicenseForTheNewTenant()
    {
        var input = FirstAccountEnrollmentValidator.Normalize(
            new FirstAccountEnrollmentRequest("t", "acme-fab", "Acme", "owner@acme.example", "Ada Owner", "Correct-Horse-9-Battery"),
            1,
            Guid.NewGuid());
        var plan = FirstAccountEnrollmentPlan.Build(
            input,
            [Guid.NewGuid()],
            Now,
            new Microsoft.AspNetCore.Identity.PasswordHasher<NevaxisZentra.Persistence.SaasIdentity.SaasIdentityAccount>());

        Assert.Equal(plan.Tenant.Id, plan.License.Subscription.TenantId);
        Assert.DoesNotContain("owner@acme.example", plan.License.AuditEvent.PayloadJson, StringComparison.Ordinal);
    }

    [Fact]
    public void DefaultPeriodIsTheSingleCentralDefault()
    {
        Assert.Equal(10, PackagedInstallLicenseOptions.DefaultPeriodYears);
        Assert.Equal(PackagedInstallLicenseOptions.DefaultPeriodYears, PackagedInstallLicenseOptions.Default.PeriodYears);
    }

    [Theory]
    [InlineData(null, 10)]
    [InlineData("", 10)]
    [InlineData("abc", 10)]
    [InlineData("0", 10)]
    [InlineData("-3", 10)]
    [InlineData("51", 10)]
    [InlineData("1.5", 10)]
    [InlineData("1", 1)]
    [InlineData("5", 5)]
    [InlineData("50", 50)]
    public void PeriodOverrideIsValidatedAndFallsBackToTheDefault(string? value, int expectedYears)
    {
        Assert.Equal(expectedYears, PackagedInstallLicenseOptions.Parse(value).PeriodYears);
    }

    [Fact]
    public void ConfiguredPeriodDrivesTheSubscriptionEndWithoutAnyHardCodedTerm()
    {
        var license = PackagedInstallLicense.BuildSubscription(Guid.NewGuid(), Now, new PackagedInstallLicenseOptions(3));

        Assert.Equal(Now.AddYears(3), license.Subscription.CurrentPeriodEndUtc);
        Assert.Contains("\"periodYears\":3", license.AuditEvent.PayloadJson, StringComparison.Ordinal);
    }
}
