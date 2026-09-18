namespace NevaxisZentra.Persistence.AiTasks.Delivery;

public sealed record AiTaskDeliveryChannelReadiness(
    string ChannelKey,
    string DisplayName,
    bool Configured,
    bool Available,
    string? ReasonCode);

public sealed record AiTaskDeliveryDispatchRequest(
    Guid DeliveryId,
    Guid TenantId,
    Guid AiTaskId,
    string Channel,
    string RecipientKind,
    Guid? RecipientUserId,
    string? MaskedRecipient,
    string? DeliveryProjectionJson,
    Guid? SecureAccessGrantId);

public sealed record AiTaskDeliveryDispatchResult(
    bool Success,
    string Outcome,
    string? ErrorCategory,
    string? SanitizedErrorCode,
    string? ProviderStatusCode,
    string? ProviderMessageId,
    string? ProviderCorrelationId);

public interface IAiTaskDeliveryChannelAdapter
{
    string ChannelKey { get; }

    AiTaskDeliveryChannelReadiness GetReadiness();

    string? ValidateRequest(AiTaskDeliveryDispatchRequest request);

    Task<AiTaskDeliveryDispatchResult> DispatchAsync(
        AiTaskDeliveryDispatchRequest request,
        CancellationToken cancellationToken);
}

public interface IAiTaskDeliveryChannelRegistry
{
    IReadOnlyList<AiTaskDeliveryChannelReadiness> ListReadiness(bool includeDevelopmentOnly);

    IAiTaskDeliveryChannelAdapter Resolve(string channelKey);
}

public sealed class AiTaskDeliveryChannelRegistry(
    IEnumerable<IAiTaskDeliveryChannelAdapter> adapters) : IAiTaskDeliveryChannelRegistry
{
    private readonly Dictionary<string, IAiTaskDeliveryChannelAdapter> adapterMap =
        adapters.ToDictionary(adapter => adapter.ChannelKey, StringComparer.Ordinal);

    public IReadOnlyList<AiTaskDeliveryChannelReadiness> ListReadiness(bool includeDevelopmentOnly)
    {
        var results = new List<AiTaskDeliveryChannelReadiness>();

        foreach (var channel in AiTaskDeliveryChannels.FoundationChannels)
        {
            if (adapterMap.TryGetValue(channel, out var adapter))
            {
                results.Add(adapter.GetReadiness());
            }
            else
            {
                results.Add(new AiTaskDeliveryChannelReadiness(
                    channel,
                    GetDisplayName(channel),
                    false,
                    false,
                    "not-configured"));
            }
        }

        if (includeDevelopmentOnly && adapterMap.TryGetValue(AiTaskDeliveryChannels.DevelopmentLoopback, out var loopback))
        {
            results.Add(loopback.GetReadiness());
        }

        return results;
    }

    public IAiTaskDeliveryChannelAdapter Resolve(string channelKey)
    {
        if (!adapterMap.TryGetValue(channelKey, out var adapter))
        {
            throw new InvalidOperationException($"Unknown delivery channel '{channelKey}'.");
        }

        return adapter;
    }

    private static string GetDisplayName(string channelKey) => channelKey switch
    {
        AiTaskDeliveryChannels.NevaxisZentraTeams => "NevaxisZentra Teams",
        AiTaskDeliveryChannels.Email => "E-posta",
        AiTaskDeliveryChannels.WhatsApp => "WhatsApp",
        AiTaskDeliveryChannels.MicrosoftTeams => "Microsoft Teams",
        AiTaskDeliveryChannels.Sms => "SMS",
        AiTaskDeliveryChannels.DevelopmentLoopback => "Development Loopback",
        _ => channelKey,
    };
}
