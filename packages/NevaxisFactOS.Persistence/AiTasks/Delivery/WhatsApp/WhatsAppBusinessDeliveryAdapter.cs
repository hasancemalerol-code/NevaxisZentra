using System.Globalization;
using System.Text.Json;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.AiTasks.Delivery.WhatsApp;

public sealed class WhatsAppBusinessDeliveryAdapter(
    ITenantContextAccessor tenantContextAccessor,
    IWhatsAppChannelConfigurationResolver configurationResolver,
    IMetaWhatsAppCloudApiClient apiClient,
    WhatsAppSecretProtector secretProtector) : IAiTaskDeliveryChannelAdapter
{
    public string ChannelKey => AiTaskDeliveryChannels.WhatsApp;

    public AiTaskDeliveryChannelReadiness GetReadiness()
    {
        if (!tenantContextAccessor.IsAvailable)
        {
            return BuildReadiness(WhatsAppReadinessStates.NotConfigured, false, false, "tenant-context-unavailable");
        }

        var config = configurationResolver
            .ResolveForTenantAsync(tenantContextAccessor.RequireTenantId())
            .GetAwaiter()
            .GetResult();

        var available = config.Enabled
            && config.ReadinessStatus == WhatsAppReadinessStates.Ready
            && !string.IsNullOrWhiteSpace(config.PhoneNumberId)
            && !string.IsNullOrWhiteSpace(config.AccessToken);

        return BuildReadiness(
            config.ReadinessStatus,
            config.Enabled && config.ReadinessStatus != WhatsAppReadinessStates.NotConfigured,
            available,
            config.ReadinessErrorCode);
    }

    public string? ValidateRequest(AiTaskDeliveryDispatchRequest request)
    {
        var config = configurationResolver
            .ResolveForTenantAsync(request.TenantId)
            .GetAwaiter()
            .GetResult();

        if (!config.Enabled)
        {
            return "WhatsApp Business devre dışı.";
        }

        if (config.ReadinessStatus != WhatsAppReadinessStates.Ready
            || string.IsNullOrWhiteSpace(config.PhoneNumberId)
            || string.IsNullOrWhiteSpace(config.AccessToken))
        {
            return "WhatsApp Business henüz yapılandırılmadı.";
        }

        var recipient = ResolveRecipientE164(request);
        if (recipient is null)
        {
            return "Geçerli uluslararası telefon numarası gerekli.";
        }

        var consentStatus = ResolveConsentStatus(request);
        if (consentStatus is WhatsAppConsentStatuses.Unknown or WhatsAppConsentStatuses.NotVerified)
        {
            return "WhatsApp gönderimi için onay durumu doğrulanmadı.";
        }

        if (string.IsNullOrWhiteSpace(ResolveSecureTaskUrl(request)))
        {
            return "Güvenli görev bağlantısı gerekli.";
        }

        return null;
    }

    public async Task<AiTaskDeliveryDispatchResult> DispatchAsync(
        AiTaskDeliveryDispatchRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return new AiTaskDeliveryDispatchResult(
                false,
                AiTaskDeliveryAttemptOutcomes.ConfigurationFailure,
                AiTaskDeliveryErrorCategories.Configuration,
                "whatsapp-validation-failed",
                null,
                null,
                null);
        }

        var config = await configurationResolver.ResolveForTenantAsync(request.TenantId, cancellationToken);
        var recipient = ResolveRecipientE164(request)!;
        var secureUrl = ResolveSecureTaskUrl(request)!;
        var template = WhatsAppTaskNotificationTemplateResolver.Resolve(
            config.DefaultLocale,
            config.TemplateName,
            config.TemplateLanguageCode);
        var content = WhatsAppTaskNotificationTemplateResolver.BuildContent(
            request.DeliveryProjectionJson ?? "{}",
            secureUrl);

        var response = await apiClient.SendTemplateMessageAsync(
            new MetaWhatsAppSendRequest(
                config.PhoneNumberId!,
                config.AccessToken!,
                recipient,
                template,
                content),
            cancellationToken);

        if (response.Success)
        {
            return new AiTaskDeliveryDispatchResult(
                true,
                AiTaskDeliveryAttemptOutcomes.Success,
                null,
                null,
                response.HttpStatusCode?.ToString(CultureInfo.InvariantCulture),
                response.ProviderMessageId,
                request.DeliveryId.ToString("N"));
        }

        var (outcome, errorCategory) = MapProviderError(response.ErrorCategory);
        return new AiTaskDeliveryDispatchResult(
            false,
            outcome,
            errorCategory,
            response.ErrorCode,
            response.HttpStatusCode?.ToString(CultureInfo.InvariantCulture),
            response.ProviderMessageId,
            null);
    }

    internal static (string Outcome, string ErrorCategory) MapProviderError(string? providerCategory) =>
        providerCategory switch
        {
            WhatsAppProviderErrorCategories.Retryable or WhatsAppProviderErrorCategories.RateLimit =>
                (AiTaskDeliveryAttemptOutcomes.TransientFailure, AiTaskDeliveryErrorCategories.Transient),
            WhatsAppProviderErrorCategories.Configuration or WhatsAppProviderErrorCategories.Authentication =>
                (AiTaskDeliveryAttemptOutcomes.ConfigurationFailure, AiTaskDeliveryErrorCategories.Configuration),
            _ => (AiTaskDeliveryAttemptOutcomes.PermanentFailure, AiTaskDeliveryErrorCategories.Permanent),
        };

    private static AiTaskDeliveryChannelReadiness BuildReadiness(
        string readinessStatus,
        bool configured,
        bool available,
        string? reasonCode) =>
        new(
            AiTaskDeliveryChannels.WhatsApp,
            "WhatsApp",
            configured,
            available,
            reasonCode ?? readinessStatus);

    private string? ResolveRecipientE164(AiTaskDeliveryDispatchRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.DeliveryProjectionJson))
        {
            try
            {
                using var document = JsonDocument.Parse(request.DeliveryProjectionJson);
                if (document.RootElement.TryGetProperty("recipientE164Protected", out var protectedValue)
                    && protectedValue.ValueKind == JsonValueKind.String)
                {
                    return secretProtector.Unprotect(protectedValue.GetString()!);
                }
            }
            catch (JsonException)
            {
            }
        }

        var normalized = WhatsAppRecipientNormalizer.Normalize(request.MaskedRecipient);
        return normalized.Success ? normalized.E164 : null;
    }

    private static string? ResolveSecureTaskUrl(AiTaskDeliveryDispatchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeliveryProjectionJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(request.DeliveryProjectionJson);
            if (document.RootElement.TryGetProperty("secureTaskUrl", out var secureUrl)
                && secureUrl.ValueKind == JsonValueKind.String)
            {
                return secureUrl.GetString();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private static string ResolveConsentStatus(AiTaskDeliveryDispatchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeliveryProjectionJson))
        {
            return WhatsAppConsentStatuses.Unknown;
        }

        try
        {
            using var document = JsonDocument.Parse(request.DeliveryProjectionJson);
            if (document.RootElement.TryGetProperty("consentStatus", out var consent)
                && consent.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(consent.GetString()))
            {
                return consent.GetString()!;
            }
        }
        catch (JsonException)
        {
        }

        return WhatsAppConsentStatuses.Unknown;
    }
}
