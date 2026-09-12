namespace NevaxisFactOS.Persistence.SaasBilling;

public sealed class DevelopmentBillingPaymentProvider : IBillingPaymentProvider
{
    public const string ProviderKeyValue = "local-development-deferred";

    public string ProviderKey => ProviderKeyValue;

    public bool IsProduction => false;

    public Task<BillingPaymentProviderResult> InitiateCardCheckoutAsync(
        Guid invoiceId,
        decimal amount,
        string currencyCode,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new BillingPaymentProviderResult(
            "deferred",
            SaasBillingFailureCodes.ProviderNotConfigured,
            "saas.billing.explanation.provider-not-configured",
            Deferred: true));
}
