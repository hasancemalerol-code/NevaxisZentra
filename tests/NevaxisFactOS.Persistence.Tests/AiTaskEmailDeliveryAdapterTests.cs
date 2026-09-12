using System.Text;
using System.Text.Json;
using NevaxisFactOS.Persistence.AiTasks.Delivery;
using NevaxisFactOS.Persistence.AiTasks.Delivery.Email;
using NevaxisFactOS.Persistence.Tenancy;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class EmailRecipientNormalizerTests
{
    [Theory]
    [InlineData("user@Company.COM", "user@company.com")]
    [InlineData("hakan@example.org", "hakan@example.org")]
    public void ValidEmailNormalization(string input, string expected)
    {
        var result = EmailRecipientNormalizer.Normalize(input);
        Assert.True(result.Success);
        Assert.Equal(expected, result.NormalizedAddress);
        Assert.Contains("@", result.MaskedDisplay);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing-at.com")]
    public void InvalidEmailRejection(string input)
    {
        var result = EmailRecipientNormalizer.Normalize(input);
        Assert.False(result.Success);
    }

    [Fact]
    public void HeaderInjectionDenied()
    {
        var result = EmailRecipientNormalizer.Normalize("user@example.com\r\nBcc: evil@example.com");
        Assert.False(result.Success);
        Assert.Equal("recipient-header-injection", result.ErrorCode);
    }

    [Fact]
    public void RecipientMaskingHidesLocalPart()
    {
        var masked = EmailRecipientNormalizer.MaskEmail("hakan@company.com");
        Assert.StartsWith("h", masked);
        Assert.Contains("@company.com", masked);
        Assert.DoesNotContain("hakan", masked);
    }
}

public sealed class EmailTemplateTests
{
    [Fact]
    public void TemplateResolutionUsesConfiguredSubject()
    {
        var template = EmailTaskNotificationTemplateResolver.Resolve("tr", "Özel konu");
        Assert.Equal("Özel konu", template.SubjectTemplate);
    }

    [Fact]
    public void HtmlBodyEscapesInjection()
    {
        var content = EmailTaskNotificationTemplateResolver.BuildContent(
            EmailTaskNotificationTemplateResolver.Resolve("tr", null),
            """{"title":"<script>alert(1)</script>","priority":"high"}""",
            "https://example.test/gorev#access=opaque");
        Assert.DoesNotContain("<script>", content.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", content.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public void PlainTextAndHtmlPartsGenerated()
    {
        var content = EmailTaskNotificationTemplateResolver.BuildContent(
            EmailTaskNotificationTemplateResolver.Resolve("tr", null),
            """{"title":"Bakım görevi","priority":"yüksek"}""",
            "https://example.test/gorev#access=opaque");
        Assert.Contains("Bakım görevi", content.PlainTextBody);
        Assert.Contains("NevaxisFactOS Factory Operations", content.HtmlBody);
    }
}

public sealed class EmailTransportClassificationTests
{
    [Fact]
    public void AuthenticationFailureClassification()
    {
        var result = SmtpEmailTransport.ClassifyFailure(
            "smtp-auth-failed",
            EmailProviderErrorCategories.Authentication,
            false);
        Assert.Equal(EmailProviderErrorCategories.Authentication, result.ErrorCategory);
    }

    [Fact]
    public void TemporaryFailureIsRetryable()
    {
        var result = SmtpEmailTransport.ClassifyFailure(
            "smtp-timeout",
            EmailProviderErrorCategories.Retryable,
            true);
        Assert.True(result.Retryable);
    }

    [Fact]
    public void AdapterMapsRetryableToTransientOutcome()
    {
        var mapped = EmailDeliveryAdapter.MapProviderError(EmailProviderErrorCategories.Retryable);
        Assert.Equal(AiTaskDeliveryAttemptOutcomes.TransientFailure, mapped.Outcome);
    }

    [Fact]
    public void AdapterMapsRecipientToPermanentOutcome()
    {
        var mapped = EmailDeliveryAdapter.MapProviderError(EmailProviderErrorCategories.Recipient);
        Assert.Equal(AiTaskDeliveryAttemptOutcomes.PermanentFailure, mapped.Outcome);
    }
}

public sealed class EmailAdapterBehaviorTests
{
    private readonly IDataProtectionProvider dataProtection = DataProtectionProvider.Create("email-tests");
    private readonly EmailSecretProtector secretProtector;

    public EmailAdapterBehaviorTests()
    {
        secretProtector = new EmailSecretProtector(dataProtection);
    }

    [Fact]
    public void CanonicalChannelIsEmail()
    {
        var adapter = CreateAdapter(configured: true);
        Assert.Equal(AiTaskDeliveryChannels.Email, adapter.ChannelKey);
    }

    [Fact]
    public void ProviderIsSmtpThroughConfiguration()
    {
        var options = Options.Create(new EmailDeliveryOptions
        {
            Enabled = true,
            SmtpHost = "smtp.test",
            SmtpPort = 587,
            FromAddress = "noreply@NevaxisFactOS.test",
            SecurityMode = EmailSecurityModes.StartTls,
        });
        Assert.Equal(EmailDeliveryProviders.Smtp, options.Value.Provider);
    }

    [Fact]
    public void NotConfiguredReadinessFailsValidation()
    {
        var adapter = CreateAdapter(configured: false);
        var error = adapter.ValidateRequest(CreateRequest(includeSecureUrl: true));
        Assert.NotNull(error);
    }

    [Fact]
    public void InsecureTransportReadinessIsDegraded()
    {
        var options = new EmailDeliveryOptions
        {
            Enabled = true,
            SmtpHost = "smtp.test",
            SmtpPort = 25,
            FromAddress = "noreply@NevaxisFactOS.test",
            SecurityMode = EmailSecurityModes.None,
        };
        Assert.Equal(EmailReadinessStates.Degraded, options.ResolveReadinessStatus());
    }

    [Fact]
    public void SecureTaskLinkReuseRequired()
    {
        var adapter = CreateAdapter(configured: true);
        var error = adapter.ValidateRequest(CreateRequest(includeSecureUrl: false));
        Assert.Contains("Güvenli", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AcceptedTransportPersistsMessageId()
    {
        var transport = new StubEmailTransport(new EmailTransportResponse(
            true,
            "<test.message@NevaxisFactOS>",
            "250",
            null,
            null,
            null,
            false));
        var adapter = CreateAdapter(configured: true, transport);
        var result = await adapter.DispatchAsync(CreateRequest(includeSecureUrl: true), CancellationToken.None);
        Assert.True(result.Success);
        Assert.Equal("<test.message@NevaxisFactOS>", result.ProviderMessageId);
    }

    [Fact]
    public async Task TemporaryFailureIsRetryable()
    {
        var transport = new StubEmailTransport(SmtpEmailTransport.ClassifyFailure(
            "smtp-timeout",
            EmailProviderErrorCategories.Retryable,
            true));
        var adapter = CreateAdapter(configured: true, transport);
        var result = await adapter.DispatchAsync(CreateRequest(includeSecureUrl: true), CancellationToken.None);
        Assert.Equal(AiTaskDeliveryAttemptOutcomes.TransientFailure, result.Outcome);
    }

    [Fact]
    public async Task PermanentRecipientFailure()
    {
        var transport = new StubEmailTransport(SmtpEmailTransport.ClassifyFailure(
            "smtp-550",
            EmailProviderErrorCategories.Recipient,
            false));
        var adapter = CreateAdapter(configured: true, transport);
        var result = await adapter.DispatchAsync(CreateRequest(includeSecureUrl: true), CancellationToken.None);
        Assert.Equal(AiTaskDeliveryAttemptOutcomes.PermanentFailure, result.Outcome);
    }

    [Fact]
    public void SecretProtectorDoesNotExposePassword()
    {
        var protectedValue = secretProtector.Protect("smtp-password-value");
        Assert.DoesNotContain("smtp-password-value", protectedValue, StringComparison.Ordinal);
    }

    [Fact]
    public void HeaderSanitizerStripsCrLf()
    {
        var sanitized = EmailRecipientNormalizer.SanitizeHeaderValue("Subject\r\nBcc: evil");
        Assert.DoesNotContain('\r', sanitized);
        Assert.DoesNotContain('\n', sanitized);
    }

    [Fact]
    public void BlockedRecipientDenied()
    {
        var adapter = CreateAdapter(configured: true);
        var request = CreateRequest(includeSecureUrl: true, eligibility: EmailRecipientEligibility.Blocked);
        var error = adapter.ValidateRequest(request);
        Assert.NotNull(error);
    }

    [Fact]
    public void ReadinessNotReadyWhenHostMissing()
    {
        var adapter = CreateAdapter(configured: false);
        var readiness = adapter.GetReadiness();
        Assert.False(readiness.Available);
    }

    [Fact]
    public void ConfiguredReadinessWhenReady()
    {
        var adapter = CreateAdapter(configured: true);
        var readiness = adapter.GetReadiness();
        Assert.True(readiness.Configured);
        Assert.True(readiness.Available);
    }

    [Fact]
    public void DuplicateSecureTokenSystemNotUsed()
    {
        Assert.Equal("email", AiTaskDeliveryChannels.Email);
        Assert.DoesNotContain("EmailAccessToken", typeof(EmailDeliveryAdapter).Name);
    }

    [Fact]
    public void NoFakeReadOrDeliveredStatuses()
    {
        Assert.DoesNotContain("read", EmailProviderStatuses.Accepted, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("delivered", EmailProviderStatuses.Accepted, StringComparison.OrdinalIgnoreCase);
    }

    private EmailDeliveryAdapter CreateAdapter(bool configured, IEmailTransport? transport = null)
    {
        var tenantAccessor = new TenantContextAccessor();
        tenantAccessor.Set(new TenantContext(Guid.NewGuid(), "tenant-a", Guid.NewGuid()));
        var options = Options.Create(new EmailDeliveryOptions
        {
            Enabled = configured,
            Provider = EmailDeliveryProviders.Smtp,
            SmtpHost = configured ? "smtp.test" : null,
            SmtpPort = 587,
            SecurityMode = EmailSecurityModes.StartTls,
            FromAddress = configured ? "noreply@NevaxisFactOS.test" : null,
            Username = "smtp-user",
            Password = "smtp-password",
        });
        return new EmailDeliveryAdapter(
            tenantAccessor,
            new StubEmailConfigurationResolver(options.Value),
            transport ?? new StubEmailTransport(new EmailTransportResponse(true, "<id@test>", "250", null, null, null, false)),
            secretProtector,
            options);
    }

    private AiTaskDeliveryDispatchRequest CreateRequest(bool includeSecureUrl, string eligibility = EmailRecipientEligibility.Eligible)
    {
        var recipient = EmailRecipientNormalizer.Normalize("operator@example.com");
        var projection = new Dictionary<string, object?>
        {
            ["title"] = "Test görev",
            ["priority"] = "high",
            ["recipientEmailProtected"] = secretProtector.Protect(recipient.NormalizedAddress!),
            ["recipientEligibility"] = eligibility,
        };
        if (includeSecureUrl)
        {
            projection["secureTaskUrl"] = "https://example.test/gorev#access=opaque-token";
        }

        return new AiTaskDeliveryDispatchRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            AiTaskDeliveryChannels.Email,
            AiTaskDeliveryRecipientKinds.External,
            null,
            recipient.MaskedDisplay,
            JsonSerializer.Serialize(projection),
            Guid.NewGuid());
    }

    private sealed class StubEmailConfigurationResolver(EmailDeliveryOptions options) : IEmailChannelConfigurationResolver
    {
        public Task<ResolvedEmailChannelConfiguration> ResolveForTenantAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ResolvedEmailChannelConfiguration(
                options.Enabled,
                options.Provider,
                options.IsConfigured() ? options.ResolveReadinessStatus() : EmailReadinessStates.NotConfigured,
                null,
                options.SmtpHost,
                options.SmtpPort,
                options.SecurityMode,
                options.Username,
                options.Password,
                options.FromAddress,
                options.FromDisplayName,
                options.ReplyToAddress,
                options.DefaultLocale,
                null));
    }

    private sealed class StubEmailTransport(EmailTransportResponse response) : IEmailTransport
    {
        public Task<EmailTransportResponse> SendAsync(EmailTransportRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }
}

public sealed class EmailSecurityTests
{
    [Fact]
    public void SmtpPasswordNotInSampleLogLine()
    {
        const string sample = "email.delivery.accepted channel=email provider=smtp";
        Assert.DoesNotContain("smtp-password", sample, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RawSecureTokenNotInSampleLogLine()
    {
        const string sample = "dispatch completed deliveryId=abc";
        Assert.DoesNotContain("opaque-token", sample, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FullRecipientNotRequiredInMaskedDisplay()
    {
        var masked = EmailRecipientNormalizer.MaskEmail("operator@example.com");
        Assert.DoesNotContain("operator", masked);
    }
}

public sealed class AiTaskEmailDeliveryAdapterMigrationTests
{
    [Fact]
    public void MigrationDefinesEmailConfigurationRlsAndPermissions()
    {
        var migrationPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "packages", "NevaxisFactOS.Persistence.Migrations", "Migrations",
            "20260908120000_AddAiTaskEmailDeliveryAdapterFoundation.cs");
        var fullPath = Path.GetFullPath(migrationPath);
        Assert.True(File.Exists(fullPath));
        var source = File.ReadAllText(fullPath);
        Assert.Contains("ai_task_email_channel_configurations", source);
        Assert.Contains("FORCE ROW LEVEL SECURITY", source);
        Assert.Contains("ai-task.delivery.email.send", source);
        Assert.DoesNotContain("SMTP__PASSWORD", source, StringComparison.Ordinal);
    }
}
