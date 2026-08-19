using System.Text.Json;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Development;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Api.SaasBilling;

internal static class SaasBillingWebhookTenantResolver
{
    public static bool TryParseTenantIdFromConversationId(string? conversationId, out Guid tenantId)
    {
        tenantId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            return false;
        }

        var separatorIndex = conversationId.IndexOf('-', StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            return false;
        }

        var tenantPart = conversationId[..separatorIndex];
        return Guid.TryParseExact(tenantPart, "N", out tenantId);
    }

    public static string? ReadPaymentConversationId(string rawBody)
    {
        try
        {
            using var document = JsonDocument.Parse(rawBody);
            if (document.RootElement.TryGetProperty("paymentConversationId", out var conversationElement))
            {
                return conversationElement.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    public static async Task<bool> TryEstablishWebhookTenantContextAsync(
        string rawBody,
        TenantContextAccessor tenantContextAccessor,
        CrevaFactOSDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (tenantContextAccessor.IsAvailable)
        {
            return true;
        }

        var conversationId = ReadPaymentConversationId(rawBody);
        if (!TryParseTenantIdFromConversationId(conversationId, out var tenantId))
        {
            return false;
        }

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('crevafactos.tenant_id', {tenantId.ToString()}, false)",
            cancellationToken);

        var tenant = await dbContext.Tenants.AsNoTracking()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.Id == tenantId, cancellationToken);
        if (tenant is null)
        {
            if (tenantId == DevelopmentPilotIds.TenantId)
            {
                tenantContextAccessor.Set(new TenantContext(tenantId, "dev-pilot-tenant", Guid.Empty));
                return true;
            }

            return false;
        }

        tenantContextAccessor.Set(new TenantContext(tenant.Id, tenant.TenantKey, Guid.Empty));
        return true;
    }
}
