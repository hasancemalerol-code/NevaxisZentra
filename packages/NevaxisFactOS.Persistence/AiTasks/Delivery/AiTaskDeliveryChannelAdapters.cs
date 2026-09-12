using Microsoft.Extensions.Hosting;

namespace NevaxisFactOS.Persistence.AiTasks.Delivery;

public sealed class NotConfiguredAiTaskDeliveryChannelAdapter(string channelKey, string displayName)
    : IAiTaskDeliveryChannelAdapter
{
    public string ChannelKey => channelKey;

    public AiTaskDeliveryChannelReadiness GetReadiness() => new(
        channelKey,
        displayName,
        false,
        false,
        "not-configured");

    public string? ValidateRequest(AiTaskDeliveryDispatchRequest request) =>
        "Kanal yapılandırılmadı.";

    public Task<AiTaskDeliveryDispatchResult> DispatchAsync(
        AiTaskDeliveryDispatchRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new AiTaskDeliveryDispatchResult(
            false,
            AiTaskDeliveryAttemptOutcomes.ConfigurationFailure,
            AiTaskDeliveryErrorCategories.Configuration,
            "channel-not-configured",
            null,
            null,
            null));
}

public sealed class DevelopmentLoopbackAiTaskDeliveryChannelAdapter(IHostEnvironment environment)
    : IAiTaskDeliveryChannelAdapter
{
    public string ChannelKey => AiTaskDeliveryChannels.DevelopmentLoopback;

    public AiTaskDeliveryChannelReadiness GetReadiness()
    {
        var enabled = environment.IsDevelopment() || environment.EnvironmentName == "Testing";
        return new AiTaskDeliveryChannelReadiness(
            ChannelKey,
            "Development Loopback",
            enabled,
            enabled,
            enabled ? null : "development-only");
    }

    public string? ValidateRequest(AiTaskDeliveryDispatchRequest request)
    {
        if (!GetReadiness().Available)
        {
            return "Development loopback is not available in this environment.";
        }

        return null;
    }

    public Task<AiTaskDeliveryDispatchResult> DispatchAsync(
        AiTaskDeliveryDispatchRequest request,
        CancellationToken cancellationToken)
    {
        if (!GetReadiness().Available)
        {
            return Task.FromResult(new AiTaskDeliveryDispatchResult(
                false,
                AiTaskDeliveryAttemptOutcomes.ConfigurationFailure,
                AiTaskDeliveryErrorCategories.Configuration,
                "development-only",
                null,
                null,
                null));
        }

        var fixture = ResolveFixture(request);
        if (fixture is not null)
        {
            return Task.FromResult(fixture);
        }

        var messageId = $"loopback-{request.DeliveryId:N}";
        return Task.FromResult(new AiTaskDeliveryDispatchResult(
            true,
            AiTaskDeliveryAttemptOutcomes.Success,
            null,
            null,
            "200",
            messageId,
            request.DeliveryId.ToString("N")));
    }

    private static AiTaskDeliveryDispatchResult? ResolveFixture(AiTaskDeliveryDispatchRequest request)
    {
        if (request.DeliveryProjectionJson?.Contains("\"fixture\":\"transient\"", StringComparison.Ordinal) == true)
        {
            return new AiTaskDeliveryDispatchResult(
                false,
                AiTaskDeliveryAttemptOutcomes.TransientFailure,
                AiTaskDeliveryErrorCategories.Transient,
                "loopback-transient",
                "503",
                null,
                null);
        }

        if (request.DeliveryProjectionJson?.Contains("\"fixture\":\"permanent\"", StringComparison.Ordinal) == true)
        {
            return new AiTaskDeliveryDispatchResult(
                false,
                AiTaskDeliveryAttemptOutcomes.PermanentFailure,
                AiTaskDeliveryErrorCategories.Permanent,
                "loopback-permanent",
                "400",
                null,
                null);
        }

        return null;
    }
}
