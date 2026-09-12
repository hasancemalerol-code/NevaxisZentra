using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace NevaxisFactOS.Persistence.AiTasks.Delivery.WhatsApp;

public sealed record WhatsAppWebhookProcessResult(
    bool Accepted,
    string? FailureCode,
    bool Duplicate = false);

public interface IWhatsAppWebhookProcessor
{
    bool VerifySubscription(string mode, string token, string challenge, out string? responseBody);

    bool VerifySignature(string appSecret, string? signatureHeader, ReadOnlySpan<byte> rawBody);

    Task<WhatsAppWebhookProcessResult> ProcessAsync(
        byte[] rawBody,
        CancellationToken cancellationToken);
}

public sealed class WhatsAppWebhookProcessor(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    TenantContextAccessor tenantContextAccessor,
    IWhatsAppChannelConfigurationResolver configurationResolver,
    IOptions<WhatsAppBusinessDeliveryOptions> options) : IWhatsAppWebhookProcessor
{
    private sealed record WhatsAppDeliveryWebhookLookup(Guid DeliveryId, Guid TenantId, string? ProviderStatus);
    public bool VerifySubscription(string mode, string token, string challenge, out string? responseBody)
    {
        responseBody = null;
        if (!string.Equals(mode, "subscribe", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var verifyToken = options.Value.WebhookVerifyToken;
        if (string.IsNullOrWhiteSpace(verifyToken) || !string.Equals(token, verifyToken, StringComparison.Ordinal))
        {
            return false;
        }

        responseBody = challenge;
        return true;
    }

    public bool VerifySignature(string appSecret, string? signatureHeader, ReadOnlySpan<byte> rawBody)
    {
        if (string.IsNullOrWhiteSpace(appSecret) || string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        if (!signatureHeader.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var expected = signatureHeader["sha256=".Length..];
        var hash = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(appSecret), rawBody));
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected.ToUpperInvariant()),
            Encoding.UTF8.GetBytes(hash.ToUpperInvariant()));
    }

    public async Task<WhatsAppWebhookProcessResult> ProcessAsync(
        byte[] rawBody,
        CancellationToken cancellationToken)
    {
        var appSecret = options.Value.AppSecret;
        if (string.IsNullOrWhiteSpace(appSecret))
        {
            return new WhatsAppWebhookProcessResult(false, "app-secret-missing");
        }

        using var document = JsonDocument.Parse(Encoding.UTF8.GetString(rawBody));
        if (!document.RootElement.TryGetProperty("entry", out var entries) || entries.ValueKind != JsonValueKind.Array)
        {
            return new WhatsAppWebhookProcessResult(false, "invalid-payload");
        }

        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var change in changes.EnumerateArray())
            {
                if (!change.TryGetProperty("value", out var value))
                {
                    continue;
                }

                if (!value.TryGetProperty("statuses", out var statuses) || statuses.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var status in statuses.EnumerateArray())
                {
                    var providerMessageId = status.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                    var providerStatus = status.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;
                    var timestamp = status.TryGetProperty("timestamp", out var tsProp) ? tsProp.GetString() : null;
                    if (string.IsNullOrWhiteSpace(providerMessageId) || string.IsNullOrWhiteSpace(providerStatus))
                    {
                        continue;
                    }

                    var mappedStatus = MapProviderStatus(providerStatus);
                    var lookup = await dbContext.Database
                        .SqlQuery<WhatsAppDeliveryWebhookLookup>($"""
                            SELECT delivery_id AS "DeliveryId", tenant_id AS "TenantId", provider_status AS "ProviderStatus"
                            FROM resolve_ai_task_delivery_by_provider_message({providerMessageId})
                            """)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (lookup is null)
                    {
                        continue;
                    }

                    tenantContextAccessor.Set(new TenantContext(lookup.TenantId, "whatsapp-webhook", Guid.Empty));

                    var config = await configurationResolver.ResolveForTenantAsync(lookup.TenantId, cancellationToken);
                    var eventId = status.TryGetProperty("conversation", out var conv) && conv.TryGetProperty("id", out var convId)
                        ? convId.GetString() ?? providerMessageId
                        : providerMessageId;
                    var eventHash = WhatsAppSecretProtector.ComputeEventHash(
                        config.Provider,
                        eventId!,
                        providerMessageId,
                        mappedStatus);

                    var duplicate = await tenantDatabaseSession.ExecuteReadAsync(
                        async ct => await dbContext.AiTaskDeliveryProviderEvents
                            .AsNoTracking()
                            .AnyAsync(
                                e => e.TenantId == lookup.TenantId && e.EventHash == eventHash,
                                ct),
                        cancellationToken);
                    if (duplicate)
                    {
                        return new WhatsAppWebhookProcessResult(true, null, true);
                    }

                    if (!WhatsAppProviderStatuses.ShouldAdvance(lookup.ProviderStatus, mappedStatus))
                    {
                        return new WhatsAppWebhookProcessResult(true, null, true);
                    }

                    await tenantDatabaseSession.ExecuteWriteAsync(
                        async ct =>
                        {
                            var delivery = await dbContext.AiTaskDeliveries
                                .FirstOrDefaultAsync(d => d.Id == lookup.DeliveryId, ct);
                            if (delivery is null)
                            {
                                return false;
                            }

                            delivery.Provider = config.Provider;
                            delivery.ProviderStatus = mappedStatus;
                            delivery.ProviderStatusAtUtc = ParseTimestamp(timestamp);
                            delivery.UpdatedAtUtc = DateTimeOffset.UtcNow;

                            dbContext.AiTaskDeliveryProviderEvents.Add(new AiTaskDeliveryProviderEvent
                            {
                                Id = Guid.NewGuid(),
                                TenantId = lookup.TenantId,
                                DeliveryId = delivery.Id,
                                Provider = config.Provider,
                                ProviderEventId = eventId!,
                                ProviderMessageId = providerMessageId,
                                EventType = "status",
                                ProviderStatus = mappedStatus,
                                EventHash = eventHash,
                                OccurredAtUtc = ParseTimestamp(timestamp),
                                ReceivedAtUtc = DateTimeOffset.UtcNow,
                                SanitizedPayloadJson = JsonSerializer.Serialize(new { status = mappedStatus }),
                            });

                            await dbContext.SaveChangesAsync(ct);
                            return true;
                        },
                        cancellationToken);
                }
            }
        }

        return new WhatsAppWebhookProcessResult(true, null);
    }

    private static string MapProviderStatus(string providerStatus) => providerStatus switch
    {
        "sent" => WhatsAppProviderStatuses.Sent,
        "delivered" => WhatsAppProviderStatuses.Delivered,
        "read" => WhatsAppProviderStatuses.Read,
        "failed" => WhatsAppProviderStatuses.Failed,
        _ => WhatsAppProviderStatuses.Accepted,
    };

    private static DateTimeOffset ParseTimestamp(string? timestamp) =>
        long.TryParse(timestamp, out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : DateTimeOffset.UtcNow;
}
