using NevaxisZentra.Persistence.SaasBilling.Payments;

namespace NevaxisZentra.Api.SaasBilling.Payments;

public sealed class NotConfiguredSaasPaymentProvider : ISaasPaymentProvider
{
    public string ProviderKey => SaasPaymentProviderKeys.Iyzico;

    public SaasPaymentProviderStatus GetStatus() =>
        new(
            ProviderKey,
            SaasPaymentProviderModes.NotConfigured,
            false,
            false,
            false,
            "provider-not-configured");

    public Task<SaasPaymentCheckoutInitializationResult> InitializeHostedCheckoutAsync(
        SaasPaymentCheckoutInitializationRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new SaasPaymentCheckoutInitializationResult(
            false,
            SaasPaymentFailureCodes.ProviderNotConfigured,
            null,
            null,
            null));

    public Task<SaasPaymentCheckoutVerificationResult> RetrieveCheckoutResultAsync(
        SaasPaymentCheckoutVerificationRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new SaasPaymentCheckoutVerificationResult(
            false,
            SaasPaymentNormalizedStatuses.Failed,
            SaasPaymentFailureCodes.ProviderNotConfigured,
            null,
            null,
            null,
            null));

    public SaasPaymentWebhookValidationResult ValidateWebhookSignature(
        SaasPaymentWebhookValidationRequest request) =>
        new(false, SaasPaymentFailureCodes.ProviderNotConfigured);

    public SaasPaymentNormalizedEvent? NormalizeProviderEvent(string rawBody) => null;

    public Task<SaasPaymentStateQueryResult> QueryPaymentStateAsync(
        SaasPaymentStateQueryRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new SaasPaymentStateQueryResult(
            false,
            SaasPaymentNormalizedStatuses.Failed,
            SaasPaymentFailureCodes.ProviderNotConfigured,
            null,
            null,
            null));
}
