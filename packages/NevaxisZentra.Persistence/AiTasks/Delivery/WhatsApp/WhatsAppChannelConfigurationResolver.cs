using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace NevaxisZentra.Persistence.AiTasks.Delivery.WhatsApp;

public sealed record ResolvedWhatsAppChannelConfiguration(
    bool Enabled,
    string Provider,
    string ReadinessStatus,
    string? ReadinessErrorCode,
    string? PhoneNumberId,
    string? BusinessAccountId,
    string? AccessToken,
    string? AppSecret,
    string? WebhookVerifyToken,
    string DefaultLocale,
    string? TemplateName,
    string? TemplateLanguageCode);

public interface IWhatsAppChannelConfigurationResolver
{
    Task<ResolvedWhatsAppChannelConfiguration> ResolveForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

public sealed class WhatsAppChannelConfigurationResolver(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IOptions<WhatsAppBusinessDeliveryOptions> options,
    WhatsAppSecretProtector secretProtector) : IWhatsAppChannelConfigurationResolver
{
    public async Task<ResolvedWhatsAppChannelConfiguration> ResolveForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var env = options.Value;
        AiTaskWhatsAppChannelConfiguration? tenantConfig = null;
        if (tenantId != Guid.Empty)
        {
            try
            {
                tenantConfig = await tenantDatabaseSession.ExecuteReadAsync(
                    async ct => await dbContext.AiTaskWhatsAppChannelConfigurations
                        .IgnoreQueryFilters()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.TenantId == tenantId, ct),
                    cancellationToken);
            }
            catch (PostgresException ex) when (ex.SqlState is "22P02")
            {
                tenantConfig = null;
            }
        }

        if (tenantConfig is not null)
        {
            var accessToken = string.IsNullOrWhiteSpace(tenantConfig.AccessTokenProtected)
                ? null
                : secretProtector.Unprotect(tenantConfig.AccessTokenProtected);
            var webhookToken = string.IsNullOrWhiteSpace(tenantConfig.WebhookVerifyTokenProtected)
                ? null
                : secretProtector.Unprotect(tenantConfig.WebhookVerifyTokenProtected);

            return new ResolvedWhatsAppChannelConfiguration(
                tenantConfig.Enabled,
                tenantConfig.Provider,
                tenantConfig.ReadinessStatus,
                tenantConfig.ReadinessErrorCode,
                tenantConfig.PhoneNumberId,
                tenantConfig.BusinessAccountId,
                accessToken,
                env.AppSecret,
                webhookToken ?? env.WebhookVerifyToken,
                tenantConfig.DefaultLocale,
                tenantConfig.TemplateName,
                tenantConfig.TemplateLanguageCode);
        }

        if (!env.IsConfigured())
        {
            return new ResolvedWhatsAppChannelConfiguration(
                false,
                env.Provider,
                WhatsAppReadinessStates.NotConfigured,
                "not-configured",
                null,
                null,
                null,
                env.AppSecret,
                env.WebhookVerifyToken,
                env.DefaultLocale,
                env.DefaultTemplateName,
                env.DefaultTemplateLanguageCode);
        }

        return new ResolvedWhatsAppChannelConfiguration(
            env.Enabled,
            env.Provider,
            WhatsAppReadinessStates.Ready,
            null,
            env.PhoneNumberId,
            env.BusinessAccountId,
            env.AccessToken,
            env.AppSecret,
            env.WebhookVerifyToken,
            env.DefaultLocale,
            env.DefaultTemplateName,
            env.DefaultTemplateLanguageCode);
    }
}
