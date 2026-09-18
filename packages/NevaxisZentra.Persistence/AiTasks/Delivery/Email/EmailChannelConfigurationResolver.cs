using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace NevaxisZentra.Persistence.AiTasks.Delivery.Email;

public sealed record ResolvedEmailChannelConfiguration(
    bool Enabled,
    string Provider,
    string ReadinessStatus,
    string? ReadinessErrorCode,
    string? SmtpHost,
    int SmtpPort,
    string SecurityMode,
    string? Username,
    string? Password,
    string? FromAddress,
    string? FromDisplayName,
    string? ReplyToAddress,
    string DefaultLocale,
    string? TemplateSubject);

public interface IEmailChannelConfigurationResolver
{
    Task<ResolvedEmailChannelConfiguration> ResolveForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

public sealed class EmailChannelConfigurationResolver(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IOptions<EmailDeliveryOptions> options,
    EmailSecretProtector secretProtector) : IEmailChannelConfigurationResolver
{
    public async Task<ResolvedEmailChannelConfiguration> ResolveForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var env = options.Value;
        AiTaskEmailChannelConfiguration? tenantConfig = null;
        if (tenantId != Guid.Empty)
        {
            try
            {
                tenantConfig = await tenantDatabaseSession.ExecuteReadAsync(
                    async ct => await dbContext.AiTaskEmailChannelConfigurations
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
            var password = string.IsNullOrWhiteSpace(tenantConfig.PasswordProtected)
                ? null
                : secretProtector.Unprotect(tenantConfig.PasswordProtected);

            return new ResolvedEmailChannelConfiguration(
                tenantConfig.Enabled,
                tenantConfig.Provider,
                tenantConfig.ReadinessStatus,
                tenantConfig.ReadinessErrorCode,
                tenantConfig.SmtpHost,
                tenantConfig.SmtpPort,
                tenantConfig.SecurityMode,
                tenantConfig.Username,
                password,
                tenantConfig.FromAddress,
                tenantConfig.FromDisplayName,
                tenantConfig.ReplyToAddress,
                tenantConfig.DefaultLocale,
                tenantConfig.TemplateSubject);
        }

        if (!env.IsConfigured())
        {
            return new ResolvedEmailChannelConfiguration(
                false,
                env.Provider,
                EmailReadinessStates.NotConfigured,
                "not-configured",
                null,
                env.SmtpPort,
                env.SecurityMode,
                null,
                null,
                null,
                null,
                null,
                env.DefaultLocale,
                null);
        }

        return new ResolvedEmailChannelConfiguration(
            env.Enabled,
            env.Provider,
            env.ResolveReadinessStatus(),
            env.SecurityMode == EmailSecurityModes.None ? "insecure-transport" : null,
            env.SmtpHost,
            env.SmtpPort,
            env.SecurityMode,
            env.Username,
            env.Password,
            env.FromAddress,
            env.FromDisplayName,
            env.ReplyToAddress,
            env.DefaultLocale,
            null);
    }
}
