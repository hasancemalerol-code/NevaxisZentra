using NevaxisZentra.Api.SaasBilling.Payments.Iyzico;
using NevaxisZentra.Persistence.SaasBilling.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NevaxisZentra.Api.SaasBilling.Payments;

public static class SaasPaymentServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraSaasPayments(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<SaasPaymentProviderOptions>()
            .Bind(configuration.GetSection(SaasPaymentProviderOptions.SectionName))
            .PostConfigure(options => ApplyEnvironmentOverrides(configuration, options));

        services.AddHttpClient(nameof(IyzicoSaasPaymentProvider));

        services.AddSingleton<ISaasPaymentProvider>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<
                Microsoft.Extensions.Options.IOptions<SaasPaymentProviderOptions>>().Value;
            if (IsSandboxConfigured(options))
            {
                return ActivatorUtilities.CreateInstance<IyzicoSaasPaymentProvider>(serviceProvider);
            }

            return new NotConfiguredSaasPaymentProvider();
        });

        return services;
    }

    private static void ApplyEnvironmentOverrides(IConfiguration configuration, SaasPaymentProviderOptions options)
    {
        options.ProviderMode = GetPaymentEnvironmentValue("PROVIDER_MODE", configuration, options.ProviderMode);
        options.IyzicoBaseUrl = GetPaymentEnvironmentValue("IYZICO_BASE_URL", configuration, options.IyzicoBaseUrl);
        options.IyzicoApiKey = GetPaymentEnvironmentValue("IYZICO_API_KEY", configuration, options.IyzicoApiKey);
        options.IyzicoSecretKey = GetPaymentEnvironmentValue("IYZICO_SECRET_KEY", configuration, options.IyzicoSecretKey);
        options.CallbackBaseUrl = GetPaymentEnvironmentValue("CALLBACK_BASE_URL", configuration, options.CallbackBaseUrl);
    }

    private static string GetPaymentEnvironmentValue(
        string suffix,
        IConfiguration configuration,
        string fallback) =>
        Environment.GetEnvironmentVariable($"NEVAXIS_FACTOS__SAAS_PAYMENT__{suffix}")
        ?? configuration[$"NevaxisZentra:SAAS_PAYMENT:{suffix}"]
        ?? configuration[$"NEVAXIS_FACTOS__SAAS_PAYMENT__{suffix}"]
        ?? fallback;

    private static bool IsSandboxConfigured(SaasPaymentProviderOptions options) =>
        string.Equals(options.ProviderMode, SaasPaymentProviderModes.Sandbox, StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(options.IyzicoBaseUrl)
        && !string.IsNullOrWhiteSpace(options.IyzicoApiKey)
        && !string.IsNullOrWhiteSpace(options.IyzicoSecretKey)
        && !string.IsNullOrWhiteSpace(options.CallbackBaseUrl);
}
