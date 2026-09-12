namespace NevaxisFactOS.Persistence.SaasBilling.Payments;

public sealed class SaasPaymentProviderOptions
{
    public const string SectionName = "SaasPayment";

    public string ProviderMode { get; set; } = SaasPaymentProviderModes.NotConfigured;

    public string IyzicoBaseUrl { get; set; } = string.Empty;

    public string IyzicoApiKey { get; set; } = string.Empty;

    public string IyzicoSecretKey { get; set; } = string.Empty;

    public string CallbackBaseUrl { get; set; } = string.Empty;
}
