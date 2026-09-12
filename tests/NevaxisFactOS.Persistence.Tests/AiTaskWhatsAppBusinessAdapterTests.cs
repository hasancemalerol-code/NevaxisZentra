using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using NevaxisFactOS.Persistence.AiTasks.Delivery;
using NevaxisFactOS.Persistence.AiTasks.Delivery.WhatsApp;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class WhatsAppRecipientNormalizerTests
{
    [Theory]
    [InlineData("+905551112233", "+905551112233")]
    [InlineData("+1 (415) 555-0100", "+14155550100")]
    public void ValidRecipientNormalization(string input, string expectedE164)
    {
        var result = WhatsAppRecipientNormalizer.Normalize(input);
        Assert.True(result.Success);
        Assert.Equal(expectedE164, result.E164);
        Assert.Contains("**", result.MaskedDisplay);
    }

    [Theory]
    [InlineData("")]
    [InlineData("5551112233")]
    [InlineData("+12")]
    public void InvalidRecipientRejection(string input)
    {
        var result = WhatsAppRecipientNormalizer.Normalize(input);
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorCode);
    }
}

public sealed class WhatsAppTemplateTests
{
    [Fact]
    public void TemplateResolutionUsesConfiguredValues()
    {
        var template = WhatsAppTaskNotificationTemplateResolver.Resolve("tr", "task_alert", "tr");
        Assert.Equal("task_alert", template.TemplateName);
        Assert.Equal("tr", template.LanguageCode);
    }

    [Fact]
    public void SafePayloadGenerationExcludesSensitiveFields()
    {
        var content = WhatsAppTaskNotificationTemplateResolver.BuildContent(
            """{"title":"Bakım görevi","priority":"high","dueAtUtc":"2026-09-07T12:00:00Z"}""",
            "https://example.test/gorev#access=opaque");
        Assert.Contains("Bakım görevi", content.TaskTitle);
        Assert.DoesNotContain("tenant", content.SecureTaskUrl, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class WhatsAppProviderErrorClassificationTests
{
    [Theory]
    [InlineData(503, WhatsAppProviderErrorCategories.Retryable, true)]
    [InlineData(429, WhatsAppProviderErrorCategories.RateLimit, true)]
    [InlineData(401, WhatsAppProviderErrorCategories.Authentication, false)]
    [InlineData(400, WhatsAppProviderErrorCategories.Recipient, false)]
    public void ProviderErrorsAreClassified(int statusCode, string expectedCategory, bool retryable)
    {
        var body = statusCode == 400
            ? """{"error":{"code":100,"message":"invalid recipient"}}"""
            : """{"error":{"code":1,"message":"provider"}}""";
        var result = MetaWhatsAppCloudApiClient.ClassifyFailure(statusCode, body, null);
        Assert.Equal(expectedCategory, result.ErrorCategory);
        Assert.Equal(retryable, result.Retryable);
    }

    [Fact]
    public void AdapterMapsRetryableToTransientOutcome()
    {
        var mapped = WhatsAppBusinessDeliveryAdapter.MapProviderError(WhatsAppProviderErrorCategories.Retryable);
        Assert.Equal(AiTaskDeliveryAttemptOutcomes.TransientFailure, mapped.Outcome);
    }

    [Fact]
    public void AdapterMapsAuthenticationToConfigurationOutcome()
    {
        var mapped = WhatsAppBusinessDeliveryAdapter.MapProviderError(WhatsAppProviderErrorCategories.Authentication);
        Assert.Equal(AiTaskDeliveryAttemptOutcomes.ConfigurationFailure, mapped.Outcome);
    }
}

public sealed class WhatsAppWebhookSecurityTests
{
    private readonly WhatsAppWebhookProcessor processor = CreateProcessor();

    [Fact]
    public void WebhookVerificationHandshakeAcceptsValidToken()
    {
        var options = Options.Create(new WhatsAppBusinessDeliveryOptions { WebhookVerifyToken = "verify-token" });
        var localProcessor = new WhatsAppWebhookProcessor(
            null!,
            null!,
            null!,
            null!,
            options);

        var ok = localProcessor.VerifySubscription("subscribe", "verify-token", "challenge-123", out var body);
        Assert.True(ok);
        Assert.Equal("challenge-123", body);
    }

    [Fact]
    public void InvalidWebhookSignatureDenied()
    {
        var body = Encoding.UTF8.GetBytes("""{"entry":[]}""");
        Assert.False(processor.VerifySignature("secret", "sha256=DEADBEEF", body));
    }

    [Fact]
    public void ValidWebhookSignatureAccepted()
    {
        const string secret = "app-secret";
        var body = Encoding.UTF8.GetBytes("""{"entry":[]}""");
        var hash = Convert.ToHexString(System.Security.Cryptography.HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body));
        Assert.True(processor.VerifySignature(secret, $"sha256={hash}", body));
    }

    [Fact]
    public void ProviderStatusDoesNotRegressOnOutOfOrderEvents()
    {
        Assert.False(WhatsAppProviderStatuses.ShouldAdvance(WhatsAppProviderStatuses.Delivered, WhatsAppProviderStatuses.Sent));
        Assert.False(WhatsAppProviderStatuses.ShouldAdvance(WhatsAppProviderStatuses.Read, WhatsAppProviderStatuses.Delivered));
        Assert.True(WhatsAppProviderStatuses.ShouldAdvance(WhatsAppProviderStatuses.Sent, WhatsAppProviderStatuses.Delivered));
    }

    [Fact]
    public void EventHashIsDeterministicForDuplicateDetection()
    {
        var first = WhatsAppSecretProtector.ComputeEventHash("meta", "evt-1", "wamid-1", "delivered");
        var second = WhatsAppSecretProtector.ComputeEventHash("meta", "evt-1", "wamid-1", "delivered");
        Assert.Equal(first, second);
    }

    private static WhatsAppWebhookProcessor CreateProcessor() =>
        new(
            null!,
            null!,
            null!,
            null!,
            Options.Create(new WhatsAppBusinessDeliveryOptions { AppSecret = "secret" }));
}

public sealed class WhatsAppAdapterBehaviorTests
{
    private readonly IDataProtectionProvider dataProtection = DataProtectionProvider.Create("whatsapp-tests");
    private readonly WhatsAppSecretProtector secretProtector;

    public WhatsAppAdapterBehaviorTests()
    {
        secretProtector = new WhatsAppSecretProtector(dataProtection);
    }

    [Fact]
    public void NotConfiguredAdapterValidationFails()
    {
        var adapter = CreateAdapter(configured: false);
        var error = adapter.ValidateRequest(CreateRequest(consentKnown: false));
        Assert.NotNull(error);
    }

    [Fact]
    public void ConsentUnknownBlocksSend()
    {
        var adapter = CreateAdapter(configured: true);
        var error = adapter.ValidateRequest(CreateRequest(consentKnown: false));
        Assert.Contains("onay", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SecureTaskLinkReuseRequired()
    {
        var adapter = CreateAdapter(configured: true);
        var error = adapter.ValidateRequest(CreateRequest(consentKnown: true, includeSecureUrl: false));
        Assert.Contains("Güvenli", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProviderAcceptedSendPersistsProviderMessageId()
    {
        var handler = new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"messages":[{"id":"wamid.TEST"}]}"""),
        });
        var adapter = CreateAdapter(configured: true, handler);
        var result = await adapter.DispatchAsync(CreateRequest(consentKnown: true, includeSecureUrl: true), CancellationToken.None);
        Assert.True(result.Success);
        Assert.Equal("wamid.TEST", result.ProviderMessageId);
    }

    [Fact]
    public async Task TimeoutIsRetryable()
    {
        var handler = new StubHttpHandler(_ => throw new TaskCanceledException());
        var adapter = CreateAdapter(configured: true, handler);
        var result = await adapter.DispatchAsync(CreateRequest(consentKnown: true, includeSecureUrl: true), CancellationToken.None);
        Assert.Equal(AiTaskDeliveryAttemptOutcomes.TransientFailure, result.Outcome);
    }

    [Fact]
    public void SecretProtectorDoesNotExposeRawTokenInProtectedValue()
    {
        var protectedValue = secretProtector.Protect("super-secret-token");
        Assert.DoesNotContain("super-secret-token", protectedValue, StringComparison.Ordinal);
    }

    [Fact]
    public void LogsDoNotContainAccessTokenPlaceholder()
    {
        const string sample = "dispatch completed provider=meta-whatsapp-cloud";
        Assert.DoesNotContain("ACCESS_TOKEN", sample, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("APP_SECRET", sample, StringComparison.OrdinalIgnoreCase);
    }

    private WhatsAppBusinessDeliveryAdapter CreateAdapter(bool configured, HttpMessageHandler? handler = null)
    {
        var tenantAccessor = new TenantContextAccessor();
        tenantAccessor.Set(new TenantContext(Guid.NewGuid(), "tenant-a", Guid.NewGuid()));
        var options = Options.Create(new WhatsAppBusinessDeliveryOptions
        {
            Enabled = configured,
            PhoneNumberId = configured ? "phone-id" : null,
            AccessToken = configured ? "token" : null,
            AppSecret = "secret",
            DefaultTemplateName = "NEVAXIS_FACTOS_task_notification",
        });
        var resolver = new StubWhatsAppConfigurationResolver(options.Value);
        var factory = new StubHttpClientFactory(handler ?? new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"messages":[{"id":"wamid.OK"}]}"""),
        }));
        return new WhatsAppBusinessDeliveryAdapter(
            tenantAccessor,
            resolver,
            new MetaWhatsAppCloudApiClient(factory, options),
            secretProtector);
    }

    private AiTaskDeliveryDispatchRequest CreateRequest(bool consentKnown, bool includeSecureUrl = true)
    {
        var recipient = WhatsAppRecipientNormalizer.Normalize("+905551112233");
        var projection = new Dictionary<string, object?>
        {
            ["title"] = "Test görev",
            ["priority"] = "high",
            ["consentStatus"] = consentKnown ? WhatsAppConsentStatuses.Known : WhatsAppConsentStatuses.Unknown,
            ["recipientE164Protected"] = secretProtector.Protect(recipient.E164!),
        };
        if (includeSecureUrl)
        {
            projection["secureTaskUrl"] = "https://example.test/gorev#access=opaque-token";
        }

        return new AiTaskDeliveryDispatchRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            AiTaskDeliveryChannels.WhatsApp,
            AiTaskDeliveryRecipientKinds.External,
            null,
            recipient.MaskedDisplay,
            JsonSerializer.Serialize(projection),
            Guid.NewGuid());
    }

    private sealed class StubWhatsAppConfigurationResolver(WhatsAppBusinessDeliveryOptions options)
        : IWhatsAppChannelConfigurationResolver
    {
        public Task<ResolvedWhatsAppChannelConfiguration> ResolveForTenantAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ResolvedWhatsAppChannelConfiguration(
                options.Enabled,
                options.Provider,
                options.IsConfigured() ? WhatsAppReadinessStates.Ready : WhatsAppReadinessStates.NotConfigured,
                null,
                options.PhoneNumberId,
                options.BusinessAccountId,
                options.AccessToken,
                options.AppSecret,
                options.WebhookVerifyToken,
                options.DefaultLocale,
                options.DefaultTemplateName,
                options.DefaultTemplateLanguageCode));
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler);
    }

    private sealed class StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}

public sealed class AiTaskWhatsAppBusinessAdapterMigrationTests
{
    [Fact]
    public void MigrationDefinesRlsProviderEventsAndPermissions()
    {
        var migrationPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "packages", "NevaxisFactOS.Persistence.Migrations", "Migrations",
            "20260907120000_AddAiTaskWhatsAppBusinessAdapterFoundation.cs");
        var fullPath = Path.GetFullPath(migrationPath);
        Assert.True(File.Exists(fullPath));
        var source = File.ReadAllText(fullPath);
        Assert.Contains("ai_task_whatsapp_channel_configurations", source);
        Assert.Contains("ai_task_delivery_provider_events", source);
        Assert.Contains("FORCE ROW LEVEL SECURITY", source);
        Assert.Contains("ai-task.delivery.whatsapp.send", source);
        Assert.DoesNotContain("ACCESS_TOKEN", source, StringComparison.Ordinal);
    }
}
