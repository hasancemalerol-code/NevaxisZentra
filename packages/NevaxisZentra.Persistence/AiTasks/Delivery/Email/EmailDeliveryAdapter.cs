using System.Globalization;
using System.Text.Json;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.Extensions.Options;

namespace NevaxisZentra.Persistence.AiTasks.Delivery.Email;

public sealed class EmailDeliveryAdapter(
    ITenantContextAccessor tenantContextAccessor,
    IEmailChannelConfigurationResolver configurationResolver,
    IEmailTransport emailTransport,
    EmailSecretProtector secretProtector,
    IOptions<EmailDeliveryOptions> options) : IAiTaskDeliveryChannelAdapter
{
    public string ChannelKey => AiTaskDeliveryChannels.Email;

    public AiTaskDeliveryChannelReadiness GetReadiness()
    {
        if (!tenantContextAccessor.IsAvailable)
        {
            return BuildReadiness(EmailReadinessStates.NotConfigured, false, false, "tenant-context-unavailable");
        }

        var config = configurationResolver
            .ResolveForTenantAsync(tenantContextAccessor.RequireTenantId())
            .GetAwaiter()
            .GetResult();

        var available = config.Enabled
            && config.ReadinessStatus == EmailReadinessStates.Ready
            && !string.IsNullOrWhiteSpace(config.SmtpHost)
            && !string.IsNullOrWhiteSpace(config.FromAddress);

        return BuildReadiness(
            config.ReadinessStatus,
            config.Enabled && config.ReadinessStatus != EmailReadinessStates.NotConfigured,
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
            return "E-posta kanalı devre dışı.";
        }

        if (config.ReadinessStatus != EmailReadinessStates.Ready
            && config.ReadinessStatus != EmailReadinessStates.Degraded)
        {
            return "E-posta kanalı henüz yapılandırılmadı.";
        }

        if (string.IsNullOrWhiteSpace(config.SmtpHost) || string.IsNullOrWhiteSpace(config.FromAddress))
        {
            return "E-posta kanalı henüz yapılandırılmadı.";
        }

        var recipient = ResolveRecipientEmail(request);
        if (recipient is null)
        {
            return "Geçerli e-posta adresi gerekli.";
        }

        var eligibility = ResolveRecipientEligibility(request);
        if (eligibility is EmailRecipientEligibility.Blocked or EmailRecipientEligibility.Invalid)
        {
            return "Alıcı e-posta adresi gönderim için uygun değil.";
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
                "email-validation-failed",
                null,
                null,
                null);
        }

        var config = await configurationResolver.ResolveForTenantAsync(request.TenantId, cancellationToken);
        var recipient = ResolveRecipientEmail(request)!;
        var secureUrl = ResolveSecureTaskUrl(request)!;
        var template = EmailTaskNotificationTemplateResolver.Resolve(config.DefaultLocale, config.TemplateSubject);
        var content = EmailTaskNotificationTemplateResolver.BuildContent(
            template,
            request.DeliveryProjectionJson ?? "{}",
            secureUrl);

        var response = await emailTransport.SendAsync(
            new EmailTransportRequest(
                config.SmtpHost!,
                config.SmtpPort,
                config.SecurityMode,
                config.Username,
                config.Password,
                config.FromAddress!,
                config.FromDisplayName,
                config.ReplyToAddress,
                recipient,
                content.Subject,
                content.PlainTextBody,
                content.HtmlBody,
                options.Value.TimeoutSeconds),
            cancellationToken);

        if (response.Success)
        {
            return new AiTaskDeliveryDispatchResult(
                true,
                AiTaskDeliveryAttemptOutcomes.Success,
                null,
                null,
                response.SmtpStatusCode,
                response.TransportMessageId,
                request.DeliveryId.ToString("N"));
        }

        var (outcome, errorCategory) = MapProviderError(response.ErrorCategory);
        return new AiTaskDeliveryDispatchResult(
            false,
            outcome,
            errorCategory,
            response.ErrorCode,
            response.SmtpStatusCode,
            response.TransportMessageId,
            null);
    }

    internal static (string Outcome, string ErrorCategory) MapProviderError(string? providerCategory) =>
        providerCategory switch
        {
            EmailProviderErrorCategories.Retryable or EmailProviderErrorCategories.Network or EmailProviderErrorCategories.RateLimit =>
                (AiTaskDeliveryAttemptOutcomes.TransientFailure, AiTaskDeliveryErrorCategories.Transient),
            EmailProviderErrorCategories.Configuration or EmailProviderErrorCategories.Authentication =>
                (AiTaskDeliveryAttemptOutcomes.ConfigurationFailure, AiTaskDeliveryErrorCategories.Configuration),
            EmailProviderErrorCategories.Recipient or EmailProviderErrorCategories.Permanent =>
                (AiTaskDeliveryAttemptOutcomes.PermanentFailure, AiTaskDeliveryErrorCategories.Permanent),
            _ => (AiTaskDeliveryAttemptOutcomes.PermanentFailure, AiTaskDeliveryErrorCategories.Permanent),
        };

    private static AiTaskDeliveryChannelReadiness BuildReadiness(
        string readinessStatus,
        bool configured,
        bool available,
        string? reasonCode) =>
        new(
            AiTaskDeliveryChannels.Email,
            "E-posta",
            configured,
            available,
            reasonCode ?? readinessStatus);

    private string? ResolveRecipientEmail(AiTaskDeliveryDispatchRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.DeliveryProjectionJson))
        {
            try
            {
                using var document = JsonDocument.Parse(request.DeliveryProjectionJson);
                if (document.RootElement.TryGetProperty("recipientEmailProtected", out var protectedValue)
                    && protectedValue.ValueKind == JsonValueKind.String)
                {
                    var normalized = EmailRecipientNormalizer.Normalize(
                        secretProtector.Unprotect(protectedValue.GetString()!));
                    return normalized.Success ? normalized.NormalizedAddress : null;
                }
            }
            catch (JsonException)
            {
            }
        }

        var fallback = EmailRecipientNormalizer.Normalize(request.MaskedRecipient);
        return fallback.Success ? fallback.NormalizedAddress : null;
    }

    private static string ResolveRecipientEligibility(AiTaskDeliveryDispatchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeliveryProjectionJson))
        {
            return EmailRecipientEligibility.Unknown;
        }

        try
        {
            using var document = JsonDocument.Parse(request.DeliveryProjectionJson);
            if (document.RootElement.TryGetProperty("recipientEligibility", out var eligibility)
                && eligibility.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(eligibility.GetString()))
            {
                return eligibility.GetString()!;
            }
        }
        catch (JsonException)
        {
        }

        return EmailRecipientEligibility.Eligible;
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
}
